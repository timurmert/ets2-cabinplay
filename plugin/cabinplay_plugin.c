/* CabinPlay plugin.
 *
 * Loaded by the game from bin\win_x64\plugins through the SCS SDK entry point.
 *
 *  - Finds the Direct3D 11 texture the CabinPlay accessory's screen uses (a flat fill of a
 *    marker colour, shipped in the .scs) and keeps overwriting it with the frames the
 *    companion app publishes in shared memory.
 *  - Finds the texture the game renders the accessory's navigation map into and shows it
 *    through the key-coloured area of the frame while the Maps app is open.
 *  - Reads ignition / pause / navigation state from the telemetry API and passes it on.
 *  - Control mode (Ctrl+Alt+C): draws the screen large over the game and redirects the
 *    game's keyboard and mouse to the companion app.
 */
#define COBJMACROS
#define WIN32_LEAN_AND_MEAN
#define DIRECTINPUT_VERSION 0x0800
#define _WIN32_WINNT 0x0601
#include <windows.h>
#include <d3d11.h>
#include <dinput.h>
#include <dxgi1_2.h>
#include <stdarg.h>
#include <stdio.h>
#include <string.h>

#include "MinHook.h"

typedef unsigned __int64 QWORD; /* used by the NEXTRAWINPUTBLOCK macro, missing from these headers */
#include "frame_protocol.h"

/* Where the plugin and the app leave each other their locations. */
#define REGISTRY_KEY L"Software\\CabinPlay"

#define MAX_ENTRIES 48
#define MAX_SCREEN_TARGETS 4
#define MAX_NAV_PENDING 8
#define VIDEO_MIPS 11 /* 1024x512 down to 1x1 */

enum { FAMILY_BGRA = 0, FAMILY_RGBA = 1, FAMILY_COUNT = 2, FAMILY_NONE = -1 };
enum { STATE_FREE = 0, STATE_PENDING, STATE_TARGET };
enum { KIND_SCREEN = 0, KIND_NAV = 1 };
enum { VERDICT_NO = 0, VERDICT_MAYBE, VERDICT_YES };
enum { XF_NONE = 0, XF_DECODE = 1, XF_ENCODE = 2 }; /* sRGB transfer applied by the shader */

typedef struct entry {
    int state;
    int kind;
    ID3D11Texture2D *tex;          /* strong reference */
    ID3D11ShaderResourceView *srv; /* navigation targets only */
    ID3D11Device *dev;             /* identity only, not referenced */
    UINT width, height, mips;
    DXGI_FORMAT format;
    int family;
    int mip_offset;                /* video mip that matches this texture's mip 0 */
    int flip_v;                    /* navigation: texture rows run bottom-up */
    unsigned born_frame;
    int attempts;
    unsigned serial;               /* confirmation order, newest is highest */
} entry_t;

typedef struct video {
    ID3D11Device *dev;
    ID3D11Texture2D *tex;
    ID3D11ShaderResourceView *srv;
    ID3D11RenderTargetView *rtv;
    int srgb;
} video_t;

/* Everything needed to draw one textured rectangle. */
typedef struct gfx {
    ID3D11Device *dev;
    ID3D11VertexShader *vs;
    ID3D11PixelShader *ps;
    ID3D11Buffer *cb;
    ID3D11SamplerState *sampler;
    ID3D11BlendState *blend;
    ID3D11RasterizerState *raster;
    ID3D11DepthStencilState *depth;
    ID3D11Texture2D *frame_tex; /* the app's frame, bytes untouched */
    ID3D11ShaderResourceView *frame_srv;
    int failed;
} gfx_t;

typedef struct shader_consts {
    float uv_rect[4];  /* texture 0 across the viewport: u0, v0, u1, v1 */
    float nav_rect[4]; /* texture 1 across the viewport */
    float mode[4];     /* composite, transfer for texture 0, transfer for texture 1, alpha */
    float cursor[4];   /* x, y in viewport pixels, visible, corner radius in pixels */
    float size[4];     /* viewport width, height */
    float key[4];
} shader_consts_t;

typedef HRESULT(STDMETHODCALLTYPE *create_texture2d_fn)(ID3D11Device *, const D3D11_TEXTURE2D_DESC *,
                                                        const D3D11_SUBRESOURCE_DATA *, ID3D11Texture2D **);
typedef HRESULT(STDMETHODCALLTYPE *present_fn)(IDXGISwapChain *, UINT, UINT);
typedef HRESULT(STDMETHODCALLTYPE *present1_fn)(IDXGISwapChain1 *, UINT, UINT, const DXGI_PRESENT_PARAMETERS *);
typedef HRESULT(STDMETHODCALLTYPE *di_state_fn)(void *, DWORD, LPVOID);
typedef HRESULT(STDMETHODCALLTYPE *di_data_fn)(void *, DWORD, DIDEVICEOBJECTDATA *, DWORD *, DWORD);

static HMODULE g_module;
static CRITICAL_SECTION g_lock;       /* textures, frames, drawing */
static CRITICAL_SECTION g_input_lock; /* control-mode input state and the event ring */
static int g_locks_ready;
static int g_hooked;
static FILE *g_log;
static int g_log_lines;

static create_texture2d_fn o_CreateTexture2D;
static present_fn o_Present;
static present1_fn o_Present1;
static di_state_fn o_di_state[2];
static di_data_fn o_di_data[2];

static entry_t g_entries[MAX_ENTRIES];
static video_t g_video[FAMILY_COUNT];
static gfx_t g_gfx;
static unsigned g_frame_no;
static unsigned g_serial;
static int g_targets_dirty;
static int g_screen_off_applied;
static int g_map_was_shown;
static __thread int g_in_present;

static HANDLE g_mapping;
static const cabinplay_frame_header_t *g_shared;
static unsigned g_last_map_try;
static int32_t g_last_sequence = -1;
static int g_have_frame;
static int g_blanked;
static int g_frame_tex_stale = 1;
static uint8_t *g_frame;      /* BGRA, CABINPLAY_FRAME_BYTES */
static uint8_t *g_frame_rgba; /* same frame with R and B swapped, built on demand */
static uint8_t *g_black;      /* what the screen shows with the ignition off */
static int g_frame_rgba_valid;

static HANDLE g_state_mapping;
static cabinplay_state_t *g_state;

/* telemetry, written by the game's main thread */
static volatile int g_telemetry_ok;
static volatile int g_paused = 1;
static volatile int g_electric = 1;
static volatile float g_speed_ms, g_limit_ms, g_nav_distance, g_nav_time;
static volatile uint32_t g_game_time;

/* control mode */
static volatile int g_control;
static int g_toggle_was_down;
static float g_cursor_x = CABINPLAY_WIDTH / 2.0f, g_cursor_y = CABINPLAY_HEIGHT / 2.0f;
static int g_control_reported;   /* control mode as the app should see it (see pump) */
static uint8_t g_buttons[3];
static HWND g_game_window;

/* How the mouse is taken over in control mode. The game reads it through DirectInput,
 * through the system cursor or through window messages depending on its settings, so
 * all three are covered. */
typedef BOOL(WINAPI *get_cursor_fn)(LPPOINT);
typedef BOOL(WINAPI *set_cursor_fn)(int, int);
static get_cursor_fn o_GetCursorPos;
static set_cursor_fn o_SetCursorPos;
static POINT g_frozen_cursor;      /* what the game is told the cursor position is */
static POINT g_poll_last;
static int g_poll_valid;
static ULONGLONG g_di_mouse_tick;  /* last time DirectInput reported mouse movement */
static int g_arrow_frames;         /* how long an arrow key has been held (keyboard cursor) */

static int g_cfg_enabled = 1;
static int g_cfg_flip_v = 0;
static int g_cfg_toggle_vk = 'C';
static int g_cfg_cursor_speed = 100; /* percent */
static int g_cfg_overlay_percent = 60;
static int g_cfg_control_mouse = 1; /* 1: the mouse moves the CabinPlay cursor, 0: the arrow keys do */
static int g_cfg_autostart = 1;     /* start the companion app together with the game */
static int g_cfg_pause_game = 1;    /* pause the game while control mode is open */
static int g_cfg_pause_vk = VK_PAUSE; /* the key the game's "pause" control is bound to */
static wchar_t g_cfg_app_path[MAX_PATH]; /* written by the installer */

/* ------------------------------------------------------------------ logging */

static void logf_(const char *fmt, ...)
{
    if (!g_log || g_log_lines > 4000)
        return;
    SYSTEMTIME st;
    GetLocalTime(&st);
    fprintf(g_log, "%02d:%02d:%02d.%03d ", st.wHour, st.wMinute, st.wSecond, st.wMilliseconds);
    va_list ap;
    va_start(ap, fmt);
    vfprintf(g_log, fmt, ap);
    va_end(ap);
    fputc('\n', g_log);
    fflush(g_log);
    g_log_lines++;
}

static void open_log_and_config(void)
{
    wchar_t path[MAX_PATH];
    DWORD n = GetModuleFileNameW(g_module, path, MAX_PATH);
    if (!n || n >= MAX_PATH - 4)
        return;
    wchar_t *dot = wcsrchr(path, L'.');
    if (!dot)
        return;
    wcscpy(dot, L".ini");
    g_cfg_enabled = GetPrivateProfileIntW(L"cabinplay", L"enabled", 1, path);
    g_cfg_flip_v = GetPrivateProfileIntW(L"cabinplay", L"flip_v", 0, path);
    g_cfg_toggle_vk = GetPrivateProfileIntW(L"cabinplay", L"control_key", 'C', path) & 0xFF;
    g_cfg_cursor_speed = GetPrivateProfileIntW(L"cabinplay", L"cursor_speed", 100, path);
    g_cfg_overlay_percent = GetPrivateProfileIntW(L"cabinplay", L"overlay_size", 60, path);
    g_cfg_control_mouse = GetPrivateProfileIntW(L"cabinplay", L"control_mouse", 1, path) != 0;
    g_cfg_autostart = GetPrivateProfileIntW(L"cabinplay", L"autostart", 1, path) != 0;
    g_cfg_pause_game = GetPrivateProfileIntW(L"cabinplay", L"pause_game", 1, path) != 0;
    g_cfg_pause_vk = GetPrivateProfileIntW(L"cabinplay", L"pause_key", VK_PAUSE, path) & 0xFF;
    GetPrivateProfileStringW(L"cabinplay", L"app_path", L"", g_cfg_app_path, MAX_PATH, path);
    if (g_cfg_overlay_percent < 20 || g_cfg_overlay_percent > 100)
        g_cfg_overlay_percent = 60;
    wcscpy(dot, L".log");
    if (!g_log)
        g_log = _wfopen(path, L"w");

    /* Tell the app where this plugin lives, so its error report can include the log. */
    wchar_t ns[4];
    wchar_t *slash = wcsrchr(path, L'\\');
    if (slash && !GetEnvironmentVariableW(L"CABINPLAY_TEST_NAMESPACE", ns, 4)) {
        *slash = 0;
        RegSetKeyValueW(HKEY_CURRENT_USER, REGISTRY_KEY, L"PluginDir", REG_SZ, path,
                        (DWORD)((wcslen(path) + 1) * sizeof(wchar_t)));
    }
}

/* The self-test sets CABINPLAY_TEST_NAMESPACE so it never talks to a running game or app. */
static const wchar_t *mapping_name(const wchar_t *base, wchar_t *buffer, size_t capacity)
{
    wchar_t ns[48];
    DWORD n = GetEnvironmentVariableW(L"CABINPLAY_TEST_NAMESPACE", ns, 48);
    if (!n || n >= 48)
        return base;
    _snwprintf(buffer, capacity, L"%ls.%ls", base, ns);
    buffer[capacity - 1] = 0;
    return buffer;
}

/* ------------------------------------------------------------------ texture matching */

static int format_family(DXGI_FORMAT f)
{
    switch (f) {
    case DXGI_FORMAT_B8G8R8A8_TYPELESS:
    case DXGI_FORMAT_B8G8R8A8_UNORM:
    case DXGI_FORMAT_B8G8R8A8_UNORM_SRGB:
    case DXGI_FORMAT_B8G8R8X8_TYPELESS:
    case DXGI_FORMAT_B8G8R8X8_UNORM:
    case DXGI_FORMAT_B8G8R8X8_UNORM_SRGB:
        return FAMILY_BGRA;
    case DXGI_FORMAT_R8G8B8A8_TYPELESS:
    case DXGI_FORMAT_R8G8B8A8_UNORM:
    case DXGI_FORMAT_R8G8B8A8_UNORM_SRGB:
        return FAMILY_RGBA;
    default:
        return FAMILY_NONE;
    }
}

static int format_is_x8(DXGI_FORMAT f)
{
    return f == DXGI_FORMAT_B8G8R8X8_TYPELESS || f == DXGI_FORMAT_B8G8R8X8_UNORM ||
           f == DXGI_FORMAT_B8G8R8X8_UNORM_SRGB;
}

static int format_is_srgb(DXGI_FORMAT f)
{
    return f == DXGI_FORMAT_B8G8R8A8_UNORM_SRGB || f == DXGI_FORMAT_R8G8B8A8_UNORM_SRGB ||
           f == DXGI_FORMAT_B8G8R8X8_UNORM_SRGB;
}

/* A typed format usable for a shader view of a possibly typeless texture. */
static DXGI_FORMAT view_format(DXGI_FORMAT f)
{
    switch (f) {
    case DXGI_FORMAT_B8G8R8A8_TYPELESS:
        return DXGI_FORMAT_B8G8R8A8_UNORM;
    case DXGI_FORMAT_B8G8R8X8_TYPELESS:
        return DXGI_FORMAT_B8G8R8X8_UNORM;
    case DXGI_FORMAT_R8G8B8A8_TYPELESS:
        return DXGI_FORMAT_R8G8B8A8_UNORM;
    default:
        return f;
    }
}

/* Returns the video mip matching the texture's top level, or -1. The game may drop
 * the largest mips at lower texture quality settings. */
static int mip_offset_for(UINT w, UINT h)
{
    for (int i = 0; i < 3; i++)
        if (w == (CABINPLAY_WIDTH >> i) && h == (CABINPLAY_HEIGHT >> i))
            return i;
    return -1;
}

static int is_screen_desc(const D3D11_TEXTURE2D_DESC *d)
{
    return d->ArraySize == 1 && d->SampleDesc.Count == 1 && format_family(d->Format) != FAMILY_NONE &&
           !format_is_x8(d->Format) && mip_offset_for(d->Width, d->Height) >= 0 &&
           (d->BindFlags & D3D11_BIND_SHADER_RESOURCE) && !(d->BindFlags & D3D11_BIND_RENDER_TARGET) &&
           d->Usage != D3D11_USAGE_STAGING;
}

/* The accessory asks the game for a 1024x512 UI render target (ui_drawable_size). */
static int is_nav_desc(const D3D11_TEXTURE2D_DESC *d)
{
    return d->ArraySize == 1 && d->SampleDesc.Count == 1 && format_family(d->Format) != FAMILY_NONE &&
           d->Width == CABINPLAY_WIDTH && d->Height == CABINPLAY_HEIGHT &&
           (d->BindFlags & D3D11_BIND_SHADER_RESOURCE) && (d->BindFlags & D3D11_BIND_RENDER_TARGET);
}

static int pixels_have_marker(int family, const uint8_t *data, UINT pitch, UINT w, UINT h)
{
    const uint8_t c0 = family == FAMILY_BGRA ? CABINPLAY_MARKER_B : CABINPLAY_MARKER_R;
    const uint8_t c2 = family == FAMILY_BGRA ? CABINPLAY_MARKER_R : CABINPLAY_MARKER_B;
    for (int sy = 0; sy < 5; sy++) {
        for (int sx = 0; sx < 5; sx++) {
            const uint8_t *p = data + (size_t)((h - 1) * sy / 4) * pitch + (size_t)((w - 1) * sx / 4) * 4;
            if (p[0] != c0 || p[1] != CABINPLAY_MARKER_G || p[2] != c2)
                return 0;
        }
    }
    return 1;
}

/* The navigation UI (ui/dashboard/cabinplay_nav.sii in the .scs) paints a strip right of
 * the map: green in its upper half, magenta in its lower half. Both colours read the
 * same in BGRA and RGBA, and the comparison survives gamma conversion.
 * Returns 0 if absent, 1 if the rows run top-down, 2 if bottom-up. */
static int nav_marker_orientation(const uint8_t *data, UINT pitch)
{
    int upper_green = 1, upper_magenta = 1, lower_green = 1, lower_magenta = 1;
    for (int sx = 0; sx < 3; sx++) {
        UINT x = CABINPLAY_WIDTH - CABINPLAY_DOCK_WIDTH + 20 + sx * 24;
        for (int half = 0; half < 2; half++) {
            for (int sy = 0; sy < 3; sy++) {
                UINT y = half * (CABINPLAY_HEIGHT / 2) + 48 + sy * 80;
                const uint8_t *p = data + (size_t)y * pitch + (size_t)x * 4;
                int green = p[1] > p[0] + 60 && p[1] > p[2] + 60;
                int magenta = p[0] > p[1] + 60 && p[2] > p[1] + 60;
                if (half == 0) {
                    upper_green &= green;
                    upper_magenta &= magenta;
                } else {
                    lower_green &= green;
                    lower_magenta &= magenta;
                }
            }
        }
    }
    if (upper_green && lower_magenta)
        return 1;
    if (upper_magenta && lower_green)
        return 2;
    return 0;
}

static void release_entry(entry_t *e)
{
    if (e->srv)
        ID3D11ShaderResourceView_Release(e->srv);
    if (e->tex)
        ID3D11Texture2D_Release(e->tex);
    memset(e, 0, sizeof(*e));
}

/* Caller holds g_lock. Keeps only the newest `keep` confirmed textures of a kind;
 * older ones belong to trucks that are no longer loaded. */
static void confirm_entry(entry_t *e, int keep)
{
    e->state = STATE_TARGET;
    e->serial = ++g_serial;
    g_targets_dirty = 1;
    logf_("%s texture found: %ux%u, %u mips, format %d%s", e->kind == KIND_NAV ? "navigation" : "screen", e->width,
          e->height, e->mips, (int)e->format, e->flip_v ? ", bottom-up" : "");
    for (;;) {
        int count = 0;
        entry_t *oldest = NULL;
        for (int i = 0; i < MAX_ENTRIES; i++) {
            entry_t *x = &g_entries[i];
            if (x->state != STATE_TARGET || x->kind != e->kind)
                continue;
            count++;
            if (!oldest || x->serial < oldest->serial)
                oldest = x;
        }
        if (count <= keep)
            break;
        release_entry(oldest);
    }
}

static void track_texture(ID3D11Device *dev, ID3D11Texture2D *tex, int kind, int verdict)
{
    D3D11_TEXTURE2D_DESC d;
    ID3D11Texture2D_GetDesc(tex, &d); /* resolves MipLevels == 0 */

    EnterCriticalSection(&g_lock);
    entry_t *slot = NULL, *oldest = NULL;
    int same_kind_pending = 0;
    for (int i = 0; i < MAX_ENTRIES; i++) {
        entry_t *e = &g_entries[i];
        if (e->state == STATE_FREE) {
            if (!slot)
                slot = e;
            continue;
        }
        if (e->state == STATE_PENDING && e->kind == kind) {
            same_kind_pending++;
            if (!oldest || e->born_frame < oldest->born_frame)
                oldest = e;
        }
    }
    /* Unrelated render targets of the same size must not pile up. */
    if (oldest && (!slot || (kind == KIND_NAV && same_kind_pending >= MAX_NAV_PENDING))) {
        release_entry(oldest);
        slot = oldest;
    }
    if (slot) {
        ID3D11Texture2D_AddRef(tex);
        slot->tex = tex;
        slot->kind = kind;
        slot->dev = dev;
        slot->width = d.Width;
        slot->height = d.Height;
        slot->mips = d.MipLevels;
        slot->format = d.Format;
        slot->family = format_family(d.Format);
        slot->mip_offset = mip_offset_for(d.Width, d.Height);
        slot->born_frame = g_frame_no;
        slot->attempts = 0;
        slot->state = STATE_PENDING;
        if (verdict == VERDICT_YES)
            confirm_entry(slot, MAX_SCREEN_TARGETS);
    }
    LeaveCriticalSection(&g_lock);
}

/* Reads the texture's top mip back and looks for its marker. */
static int scan_texture(ID3D11Device *dev, ID3D11DeviceContext *ctx, entry_t *e)
{
    D3D11_TEXTURE2D_DESC d;
    memset(&d, 0, sizeof(d));
    d.Width = e->width;
    d.Height = e->height;
    d.MipLevels = 1;
    d.ArraySize = 1;
    d.Format = e->format;
    d.SampleDesc.Count = 1;
    d.Usage = D3D11_USAGE_STAGING;
    d.CPUAccessFlags = D3D11_CPU_ACCESS_READ;

    ID3D11Texture2D *staging = NULL;
    if (FAILED(o_CreateTexture2D(dev, &d, NULL, &staging)) || !staging)
        return 0;
    int found = 0;
    ID3D11DeviceContext_CopySubresourceRegion(ctx, (ID3D11Resource *)staging, 0, 0, 0, 0,
                                              (ID3D11Resource *)e->tex, 0, NULL);
    D3D11_MAPPED_SUBRESOURCE m;
    if (SUCCEEDED(ID3D11DeviceContext_Map(ctx, (ID3D11Resource *)staging, 0, D3D11_MAP_READ, 0, &m))) {
        if (e->kind == KIND_SCREEN) {
            found = pixels_have_marker(e->family, (const uint8_t *)m.pData, m.RowPitch, e->width, e->height);
        } else {
            int orientation = nav_marker_orientation((const uint8_t *)m.pData, m.RowPitch);
            found = orientation != 0;
            e->flip_v = orientation == 2;
        }
        ID3D11DeviceContext_Unmap(ctx, (ID3D11Resource *)staging, 0);
    }
    ID3D11Texture2D_Release(staging);
    return found;
}

/* ------------------------------------------------------------------ frames */

static void try_open_mapping(void)
{
    if (g_shared || (g_last_map_try && g_frame_no - g_last_map_try < 120))
        return;
    g_last_map_try = g_frame_no ? g_frame_no : 1;
    wchar_t name[128];
    HANDLE h = OpenFileMappingW(FILE_MAP_READ, FALSE, mapping_name(CABINPLAY_MAPPING_NAME, name, 128));
    if (!h)
        return;
    const cabinplay_frame_header_t *p = (const cabinplay_frame_header_t *)MapViewOfFile(h, FILE_MAP_READ, 0, 0,
                                                                                   CABINPLAY_MAPPING_SIZE);
    if (!p) {
        CloseHandle(h);
        return;
    }
    g_mapping = h;
    g_shared = p;
    logf_("connected to the companion app");
}

static int shared_valid(void)
{
    return g_shared && g_shared->magic == CABINPLAY_MAGIC && g_shared->version == CABINPLAY_VERSION &&
           g_shared->width == CABINPLAY_WIDTH && g_shared->height == CABINPLAY_HEIGHT;
}

/* Pulls the newest frame into g_frame. Returns 1 when g_frame changed. */
static int fetch_frame(void)
{
    try_open_mapping();
    if (!shared_valid())
        return 0;

    if (GetTickCount64() - g_shared->heartbeat_ms > 3000) {
        /* Companion closed or hung: switch the screen off rather than freeze a frame. */
        if (g_have_frame && !g_blanked) {
            memcpy(g_frame, g_black, CABINPLAY_FRAME_BYTES);
            g_blanked = 1;
            g_frame_rgba_valid = 0;
            g_frame_tex_stale = 1;
            g_last_sequence = -1;
            return 1;
        }
        return 0;
    }

    int32_t seq = g_shared->sequence;
    if ((seq & 1) || seq == g_last_sequence || seq == 0)
        return 0;
    const uint8_t *src = (const uint8_t *)g_shared + CABINPLAY_HEADER_SIZE;
    const size_t row = CABINPLAY_WIDTH * 4;
    if (g_cfg_flip_v) {
        for (UINT y = 0; y < CABINPLAY_HEIGHT; y++)
            memcpy(g_frame + y * row, src + (CABINPLAY_HEIGHT - 1 - y) * row, row);
    } else {
        memcpy(g_frame, src, CABINPLAY_FRAME_BYTES);
    }
    if (g_shared->sequence != seq)
        return 0; /* writer overtook us; the next Present picks up a clean frame */
    g_last_sequence = seq;
    g_have_frame = 1;
    g_blanked = 0;
    g_frame_rgba_valid = 0;
    g_frame_tex_stale = 1;
    return 1;
}

static void release_video(video_t *v)
{
    if (v->rtv)
        ID3D11RenderTargetView_Release(v->rtv);
    if (v->srv)
        ID3D11ShaderResourceView_Release(v->srv);
    if (v->tex)
        ID3D11Texture2D_Release(v->tex);
    memset(v, 0, sizeof(*v));
}

static int ensure_video(ID3D11Device *dev, int family)
{
    video_t *v = &g_video[family];
    if (v->tex && v->dev == dev)
        return 1;
    release_video(v);

    static const DXGI_FORMAT choices[FAMILY_COUNT][2] = {
        {DXGI_FORMAT_B8G8R8A8_UNORM_SRGB, DXGI_FORMAT_B8G8R8A8_UNORM},
        {DXGI_FORMAT_R8G8B8A8_UNORM_SRGB, DXGI_FORMAT_R8G8B8A8_UNORM},
    };
    for (int i = 0; i < 2 && !v->tex; i++) {
        UINT support = 0;
        if (FAILED(ID3D11Device_CheckFormatSupport(dev, choices[family][i], &support)) ||
            !(support & D3D11_FORMAT_SUPPORT_MIP_AUTOGEN))
            continue;
        D3D11_TEXTURE2D_DESC d;
        memset(&d, 0, sizeof(d));
        d.Width = CABINPLAY_WIDTH;
        d.Height = CABINPLAY_HEIGHT;
        d.MipLevels = 0;
        d.ArraySize = 1;
        d.Format = choices[family][i];
        d.SampleDesc.Count = 1;
        d.Usage = D3D11_USAGE_DEFAULT;
        d.BindFlags = D3D11_BIND_SHADER_RESOURCE | D3D11_BIND_RENDER_TARGET;
        d.MiscFlags = D3D11_RESOURCE_MISC_GENERATE_MIPS;
        if (FAILED(o_CreateTexture2D(dev, &d, NULL, &v->tex)))
            v->tex = NULL;
        else
            v->srgb = i == 0;
    }
    if (!v->tex) {
        logf_("could not create the video texture (family %d)", family);
        return 0;
    }
    if (FAILED(ID3D11Device_CreateShaderResourceView(dev, (ID3D11Resource *)v->tex, NULL, &v->srv)) ||
        FAILED(ID3D11Device_CreateRenderTargetView(dev, (ID3D11Resource *)v->tex, NULL, &v->rtv))) {
        release_video(v);
        return 0;
    }
    v->dev = dev;
    return 1;
}

static const uint8_t *frame_for_family(int family)
{
    if (family == FAMILY_BGRA)
        return g_frame;
    if (!g_frame_rgba_valid) {
        for (size_t i = 0; i < CABINPLAY_FRAME_BYTES; i += 4) {
            g_frame_rgba[i] = g_frame[i + 2];
            g_frame_rgba[i + 1] = g_frame[i + 1];
            g_frame_rgba[i + 2] = g_frame[i];
            g_frame_rgba[i + 3] = g_frame[i + 3];
        }
        g_frame_rgba_valid = 1;
    }
    return g_frame_rgba;
}

/* ------------------------------------------------------------------ drawing */

static const char SHADER_SOURCE[] =
    "cbuffer C : register(b0) { float4 uvRect; float4 navRect; float4 mode; float4 cursor; float4 size; float4 key; };\n"
    "Texture2D t0 : register(t0);\n"
    "Texture2D t1 : register(t1);\n"
    "SamplerState s0 : register(s0);\n"
    "struct VSOut { float4 pos : SV_Position; float2 uv : TEXCOORD0; };\n"
    "VSOut vs(uint id : SV_VertexID) {\n"
    "  VSOut o; float2 p = float2((id << 1) & 2, id & 2);\n"
    "  o.uv = p; o.pos = float4(p * float2(2, -2) + float2(-1, 1), 0, 1); return o; }\n"
    "float3 dec(float3 c) { return c <= 0.04045 ? c / 12.92 : pow(abs((c + 0.055) / 1.055), 2.4); }\n"
    "float3 enc(float3 c) { return c <= 0.0031308 ? c * 12.92 : 1.055 * pow(abs(c), 1 / 2.4) - 0.055; }\n"
    "float3 xf(float3 c, float m) { return m > 1.5 ? enc(c) : (m > 0.5 ? dec(c) : c); }\n"
    "float4 ps(VSOut i) : SV_Target {\n"
    "  float3 c;\n"
    "  if (mode.x > 0.5) {\n"
    /* composite: texture 0 holds the app's frame byte for byte, texture 1 the game's map */
    "    float3 f = t0.Load(int3(i.pos.xy, 0)).rgb;\n"
    "    float3 d = abs(f - key.rgb);\n"
    "    if (max(d.r, max(d.g, d.b)) < 0.0059) {\n"
    "      float2 nuv = lerp(navRect.xy, navRect.zw, i.uv);\n"
    "      c = xf(t1.SampleLevel(s0, nuv, 0).rgb, mode.z);\n"
    "    } else c = xf(f, mode.y);\n"
    "  } else {\n"
    "    c = xf(t0.SampleLevel(s0, lerp(uvRect.xy, uvRect.zw, i.uv), 0).rgb, mode.y);\n"
    "  }\n"
    "  float a = mode.w;\n"
    "  float2 p = i.uv * size.xy;\n"
    "  if (cursor.w > 0) {\n"
    "    float2 q = abs(p - size.xy * 0.5) - (size.xy * 0.5 - cursor.w);\n"
    "    a *= saturate(0.5 - (length(max(q, 0)) - cursor.w));\n"
    "  }\n"
    "  if (cursor.z > 0.5) {\n"
    "    float dc = distance(p, cursor.xy);\n"
    "    c = lerp(c, float3(0, 0, 0), saturate(9.5 - dc) * 0.85);\n"
    "    c = lerp(c, float3(1, 1, 1), saturate(7.0 - dc));\n"
    "  }\n"
    "  return float4(c, a);\n"
    "}\n";

static void release_gfx(void)
{
    gfx_t *g = &g_gfx;
    if (g->frame_srv) ID3D11ShaderResourceView_Release(g->frame_srv);
    if (g->frame_tex) ID3D11Texture2D_Release(g->frame_tex);
    if (g->depth) ID3D11DepthStencilState_Release(g->depth);
    if (g->raster) ID3D11RasterizerState_Release(g->raster);
    if (g->blend) ID3D11BlendState_Release(g->blend);
    if (g->sampler) ID3D11SamplerState_Release(g->sampler);
    if (g->cb) ID3D11Buffer_Release(g->cb);
    if (g->ps) ID3D11PixelShader_Release(g->ps);
    if (g->vs) ID3D11VertexShader_Release(g->vs);
    memset(g, 0, sizeof(*g));
    g_frame_tex_stale = 1;
}

typedef HRESULT(WINAPI *d3dcompile_fn)(LPCVOID, SIZE_T, LPCSTR, const void *, void *, LPCSTR, LPCSTR, UINT, UINT,
                                       ID3D10Blob **, ID3D10Blob **);

static int ensure_gfx(ID3D11Device *dev)
{
    gfx_t *g = &g_gfx;
    if (g->dev == dev)
        return !g->failed;
    release_gfx();
    g->dev = dev;
    g->failed = 1;

    HMODULE compiler = LoadLibraryW(L"d3dcompiler_47.dll");
    d3dcompile_fn compile = compiler ? (d3dcompile_fn)(void *)GetProcAddress(compiler, "D3DCompile") : NULL;
    if (!compile) {
        logf_("d3dcompiler_47.dll is not available: the map and control mode are disabled");
        return 0;
    }
    ID3D10Blob *vs = NULL, *ps = NULL, *err = NULL;
    HRESULT a = compile(SHADER_SOURCE, sizeof(SHADER_SOURCE) - 1, "cabinplay", NULL, NULL, "vs", "vs_4_0", 0, 0, &vs, &err);
    if (FAILED(a) && err)
        logf_("vertex shader: %s", (const char *)ID3D10Blob_GetBufferPointer(err));
    if (err) {
        ID3D10Blob_Release(err);
        err = NULL;
    }
    HRESULT b = compile(SHADER_SOURCE, sizeof(SHADER_SOURCE) - 1, "cabinplay", NULL, NULL, "ps", "ps_4_0", 0, 0, &ps, &err);
    if (FAILED(b) && err)
        logf_("pixel shader: %s", (const char *)ID3D10Blob_GetBufferPointer(err));
    if (err)
        ID3D10Blob_Release(err);

    int ok = SUCCEEDED(a) && SUCCEEDED(b) && vs && ps;
    if (ok)
        ok = SUCCEEDED(ID3D11Device_CreateVertexShader(dev, ID3D10Blob_GetBufferPointer(vs),
                                                       ID3D10Blob_GetBufferSize(vs), NULL, &g->vs)) &&
             SUCCEEDED(ID3D11Device_CreatePixelShader(dev, ID3D10Blob_GetBufferPointer(ps),
                                                      ID3D10Blob_GetBufferSize(ps), NULL, &g->ps));
    if (vs) ID3D10Blob_Release(vs);
    if (ps) ID3D10Blob_Release(ps);

    if (ok) {
        D3D11_BUFFER_DESC bd;
        memset(&bd, 0, sizeof(bd));
        bd.ByteWidth = sizeof(shader_consts_t);
        bd.BindFlags = D3D11_BIND_CONSTANT_BUFFER;
        ok = SUCCEEDED(ID3D11Device_CreateBuffer(dev, &bd, NULL, &g->cb));
    }
    if (ok) {
        D3D11_SAMPLER_DESC sd;
        memset(&sd, 0, sizeof(sd));
        sd.Filter = D3D11_FILTER_MIN_MAG_MIP_LINEAR;
        sd.AddressU = sd.AddressV = sd.AddressW = D3D11_TEXTURE_ADDRESS_CLAMP;
        sd.MaxLOD = D3D11_FLOAT32_MAX;
        ok = SUCCEEDED(ID3D11Device_CreateSamplerState(dev, &sd, &g->sampler));
    }
    if (ok) {
        D3D11_BLEND_DESC bl;
        memset(&bl, 0, sizeof(bl));
        bl.RenderTarget[0].BlendEnable = TRUE;
        bl.RenderTarget[0].SrcBlend = D3D11_BLEND_SRC_ALPHA;
        bl.RenderTarget[0].DestBlend = D3D11_BLEND_INV_SRC_ALPHA;
        bl.RenderTarget[0].BlendOp = D3D11_BLEND_OP_ADD;
        bl.RenderTarget[0].SrcBlendAlpha = D3D11_BLEND_ONE;
        bl.RenderTarget[0].DestBlendAlpha = D3D11_BLEND_INV_SRC_ALPHA;
        bl.RenderTarget[0].BlendOpAlpha = D3D11_BLEND_OP_ADD;
        bl.RenderTarget[0].RenderTargetWriteMask = D3D11_COLOR_WRITE_ENABLE_ALL;
        ok = SUCCEEDED(ID3D11Device_CreateBlendState(dev, &bl, &g->blend));
    }
    if (ok) {
        D3D11_RASTERIZER_DESC rd;
        memset(&rd, 0, sizeof(rd));
        rd.FillMode = D3D11_FILL_SOLID;
        rd.CullMode = D3D11_CULL_NONE;
        rd.DepthClipEnable = TRUE;
        ok = SUCCEEDED(ID3D11Device_CreateRasterizerState(dev, &rd, &g->raster));
    }
    if (ok) {
        D3D11_DEPTH_STENCIL_DESC dd;
        memset(&dd, 0, sizeof(dd));
        dd.DepthFunc = D3D11_COMPARISON_ALWAYS;
        ok = SUCCEEDED(ID3D11Device_CreateDepthStencilState(dev, &dd, &g->depth));
    }
    if (ok) {
        D3D11_TEXTURE2D_DESC td;
        memset(&td, 0, sizeof(td));
        td.Width = CABINPLAY_WIDTH;
        td.Height = CABINPLAY_HEIGHT;
        td.MipLevels = 1;
        td.ArraySize = 1;
        td.Format = DXGI_FORMAT_B8G8R8A8_UNORM; /* not sRGB: the shader compares raw bytes */
        td.SampleDesc.Count = 1;
        td.Usage = D3D11_USAGE_DEFAULT;
        td.BindFlags = D3D11_BIND_SHADER_RESOURCE;
        ok = SUCCEEDED(o_CreateTexture2D(dev, &td, NULL, &g->frame_tex)) &&
             SUCCEEDED(ID3D11Device_CreateShaderResourceView(dev, (ID3D11Resource *)g->frame_tex, NULL, &g->frame_srv));
    }
    if (!ok)
        logf_("could not create drawing resources: the map and control mode are disabled");
    g->failed = !ok;
    return ok;
}

/* The game caches pipeline state, so everything touched here is put back afterwards. */
typedef struct saved_state {
    ID3D11RenderTargetView *rtv[D3D11_SIMULTANEOUS_RENDER_TARGET_COUNT];
    ID3D11DepthStencilView *dsv;
    D3D11_VIEWPORT viewports[D3D11_VIEWPORT_AND_SCISSORRECT_OBJECT_COUNT_PER_PIPELINE];
    UINT viewport_count;
    ID3D11RasterizerState *raster;
    ID3D11BlendState *blend;
    FLOAT blend_factor[4];
    UINT sample_mask;
    ID3D11DepthStencilState *depth;
    UINT stencil_ref;
    ID3D11InputLayout *layout;
    D3D11_PRIMITIVE_TOPOLOGY topology;
    ID3D11VertexShader *vs;
    ID3D11PixelShader *ps;
    ID3D11GeometryShader *gs;
    ID3D11HullShader *hs;
    ID3D11DomainShader *ds;
    ID3D11Buffer *ps_cb;
    ID3D11ShaderResourceView *ps_srv[2];
    ID3D11SamplerState *ps_sampler;
} saved_state_t;

static void save_state(ID3D11DeviceContext *ctx, saved_state_t *s)
{
    memset(s, 0, sizeof(*s));
    ID3D11DeviceContext_OMGetRenderTargets(ctx, D3D11_SIMULTANEOUS_RENDER_TARGET_COUNT, s->rtv, &s->dsv);
    s->viewport_count = D3D11_VIEWPORT_AND_SCISSORRECT_OBJECT_COUNT_PER_PIPELINE;
    ID3D11DeviceContext_RSGetViewports(ctx, &s->viewport_count, s->viewports);
    ID3D11DeviceContext_RSGetState(ctx, &s->raster);
    ID3D11DeviceContext_OMGetBlendState(ctx, &s->blend, s->blend_factor, &s->sample_mask);
    ID3D11DeviceContext_OMGetDepthStencilState(ctx, &s->depth, &s->stencil_ref);
    ID3D11DeviceContext_IAGetInputLayout(ctx, &s->layout);
    ID3D11DeviceContext_IAGetPrimitiveTopology(ctx, &s->topology);
    ID3D11DeviceContext_VSGetShader(ctx, &s->vs, NULL, NULL);
    ID3D11DeviceContext_PSGetShader(ctx, &s->ps, NULL, NULL);
    ID3D11DeviceContext_GSGetShader(ctx, &s->gs, NULL, NULL);
    ID3D11DeviceContext_HSGetShader(ctx, &s->hs, NULL, NULL);
    ID3D11DeviceContext_DSGetShader(ctx, &s->ds, NULL, NULL);
    ID3D11DeviceContext_PSGetConstantBuffers(ctx, 0, 1, &s->ps_cb);
    ID3D11DeviceContext_PSGetShaderResources(ctx, 0, 2, s->ps_srv);
    ID3D11DeviceContext_PSGetSamplers(ctx, 0, 1, &s->ps_sampler);
}

static void restore_state(ID3D11DeviceContext *ctx, saved_state_t *s)
{
    ID3D11DeviceContext_PSSetShaderResources(ctx, 0, 2, s->ps_srv);
    ID3D11DeviceContext_OMSetRenderTargets(ctx, D3D11_SIMULTANEOUS_RENDER_TARGET_COUNT, s->rtv, s->dsv);
    ID3D11DeviceContext_RSSetViewports(ctx, s->viewport_count, s->viewports);
    ID3D11DeviceContext_RSSetState(ctx, s->raster);
    ID3D11DeviceContext_OMSetBlendState(ctx, s->blend, s->blend_factor, s->sample_mask);
    ID3D11DeviceContext_OMSetDepthStencilState(ctx, s->depth, s->stencil_ref);
    ID3D11DeviceContext_IASetInputLayout(ctx, s->layout);
    ID3D11DeviceContext_IASetPrimitiveTopology(ctx, s->topology);
    ID3D11DeviceContext_VSSetShader(ctx, s->vs, NULL, 0);
    ID3D11DeviceContext_PSSetShader(ctx, s->ps, NULL, 0);
    ID3D11DeviceContext_GSSetShader(ctx, s->gs, NULL, 0);
    ID3D11DeviceContext_HSSetShader(ctx, s->hs, NULL, 0);
    ID3D11DeviceContext_DSSetShader(ctx, s->ds, NULL, 0);
    ID3D11DeviceContext_PSSetConstantBuffers(ctx, 0, 1, &s->ps_cb);
    ID3D11DeviceContext_PSSetSamplers(ctx, 0, 1, &s->ps_sampler);

    for (int i = 0; i < D3D11_SIMULTANEOUS_RENDER_TARGET_COUNT; i++)
        if (s->rtv[i]) ID3D11RenderTargetView_Release(s->rtv[i]);
    if (s->dsv) ID3D11DepthStencilView_Release(s->dsv);
    if (s->raster) ID3D11RasterizerState_Release(s->raster);
    if (s->blend) ID3D11BlendState_Release(s->blend);
    if (s->depth) ID3D11DepthStencilState_Release(s->depth);
    if (s->layout) ID3D11InputLayout_Release(s->layout);
    if (s->vs) ID3D11VertexShader_Release(s->vs);
    if (s->ps) ID3D11PixelShader_Release(s->ps);
    if (s->gs) ID3D11GeometryShader_Release(s->gs);
    if (s->hs) ID3D11HullShader_Release(s->hs);
    if (s->ds) ID3D11DomainShader_Release(s->ds);
    if (s->ps_cb) ID3D11Buffer_Release(s->ps_cb);
    for (int i = 0; i < 2; i++)
        if (s->ps_srv[i]) ID3D11ShaderResourceView_Release(s->ps_srv[i]);
    if (s->ps_sampler) ID3D11SamplerState_Release(s->ps_sampler);
}

/* Draws one rectangle covering the viewport. */
static void draw_rect(ID3D11DeviceContext *ctx, ID3D11RenderTargetView *rtv, float x, float y, float w, float h,
                      ID3D11ShaderResourceView *t0, ID3D11ShaderResourceView *t1, const shader_consts_t *consts,
                      int blend)
{
    gfx_t *g = &g_gfx;
    saved_state_t saved;
    save_state(ctx, &saved);

    ID3D11DeviceContext_UpdateSubresource(ctx, (ID3D11Resource *)g->cb, 0, NULL, consts, 0, 0);
    ID3D11DeviceContext_OMSetRenderTargets(ctx, 1, &rtv, NULL);
    D3D11_VIEWPORT vp = {x, y, w, h, 0.0f, 1.0f};
    ID3D11DeviceContext_RSSetViewports(ctx, 1, &vp);
    ID3D11DeviceContext_RSSetState(ctx, g->raster);
    const FLOAT factor[4] = {0, 0, 0, 0};
    ID3D11DeviceContext_OMSetBlendState(ctx, blend ? g->blend : NULL, factor, 0xFFFFFFFF);
    ID3D11DeviceContext_OMSetDepthStencilState(ctx, g->depth, 0);
    ID3D11DeviceContext_IASetInputLayout(ctx, NULL);
    ID3D11DeviceContext_IASetPrimitiveTopology(ctx, D3D11_PRIMITIVE_TOPOLOGY_TRIANGLELIST);
    ID3D11DeviceContext_VSSetShader(ctx, g->vs, NULL, 0);
    ID3D11DeviceContext_PSSetShader(ctx, g->ps, NULL, 0);
    ID3D11DeviceContext_GSSetShader(ctx, NULL, NULL, 0);
    ID3D11DeviceContext_HSSetShader(ctx, NULL, NULL, 0);
    ID3D11DeviceContext_DSSetShader(ctx, NULL, NULL, 0);
    ID3D11DeviceContext_PSSetConstantBuffers(ctx, 0, 1, &g->cb);
    ID3D11ShaderResourceView *views[2] = {t0, t1};
    ID3D11DeviceContext_PSSetShaderResources(ctx, 0, 2, views);
    ID3D11DeviceContext_PSSetSamplers(ctx, 0, 1, &g->sampler);
    ID3D11DeviceContext_Draw(ctx, 3, 0);

    restore_state(ctx, &saved);
}

static int transfer_between(int source_linear, int target_wants_linear)
{
    if (source_linear == target_wants_linear)
        return XF_NONE;
    return source_linear ? XF_ENCODE : XF_DECODE;
}

/* Rebuilds the video texture from the app's frame with the map showing through. */
static void composite_with_map(ID3D11DeviceContext *ctx, video_t *v, const entry_t *nav)
{
    gfx_t *g = &g_gfx;
    if (g_frame_tex_stale) {
        ID3D11DeviceContext_UpdateSubresource(ctx, (ID3D11Resource *)g->frame_tex, 0, NULL, g_frame, CABINPLAY_WIDTH * 4,
                                              0);
        g_frame_tex_stale = 0;
    }
    shader_consts_t c;
    memset(&c, 0, sizeof(c));
    /* The map sits under everything right of the dock, one texel per screen pixel. */
    c.nav_rect[0] = -(float)CABINPLAY_DOCK_WIDTH / CABINPLAY_WIDTH;
    c.nav_rect[2] = (float)(CABINPLAY_WIDTH - CABINPLAY_DOCK_WIDTH) / CABINPLAY_WIDTH;
    c.nav_rect[1] = nav->flip_v ? 1.0f : 0.0f;
    c.nav_rect[3] = nav->flip_v ? 0.0f : 1.0f;
    c.mode[0] = 1.0f;
    c.mode[1] = (float)transfer_between(0, v->srgb);
    c.mode[2] = (float)transfer_between(format_is_srgb(view_format(nav->format)), v->srgb);
    c.mode[3] = 1.0f;
    c.size[0] = CABINPLAY_WIDTH;
    c.size[1] = CABINPLAY_HEIGHT;
    c.key[0] = CABINPLAY_KEY_R / 255.0f;
    c.key[1] = CABINPLAY_KEY_G / 255.0f;
    c.key[2] = CABINPLAY_KEY_B / 255.0f;
    draw_rect(ctx, v->rtv, 0, 0, CABINPLAY_WIDTH, CABINPLAY_HEIGHT, g->frame_srv, nav->srv, &c, 0);
}

/* Control mode: the screen, enlarged, in the middle of the game's picture. */
static void draw_overlay(IDXGISwapChain *sc, ID3D11Device *dev, ID3D11DeviceContext *ctx, video_t *v)
{
    ID3D11Texture2D *back = NULL;
    if (FAILED(IDXGISwapChain_GetBuffer(sc, 0, &IID_ID3D11Texture2D, (void **)&back)) || !back)
        return;
    D3D11_TEXTURE2D_DESC bd;
    ID3D11Texture2D_GetDesc(back, &bd);
    ID3D11RenderTargetView *rtv = NULL;
    if (SUCCEEDED(ID3D11Device_CreateRenderTargetView(dev, (ID3D11Resource *)back, NULL, &rtv)) && rtv) {
        float scale = bd.Width * (g_cfg_overlay_percent / 100.0f) / CABINPLAY_WIDTH;
        float fit = bd.Height * 0.9f / CABINPLAY_HEIGHT;
        if (scale > fit)
            scale = fit;
        float w = CABINPLAY_WIDTH * scale, h = CABINPLAY_HEIGHT * scale;
        float x = (float)(int)((bd.Width - w) / 2), y = (float)(int)((bd.Height - h) / 2);

        shader_consts_t c;
        memset(&c, 0, sizeof(c));
        c.uv_rect[2] = c.uv_rect[3] = 1.0f;
        c.mode[1] = (float)transfer_between(v->srgb, format_is_srgb(bd.Format));
        c.mode[3] = 1.0f;
        c.cursor[0] = g_cursor_x * scale;
        c.cursor[1] = g_cursor_y * scale;
        c.cursor[2] = 1.0f;
        c.cursor[3] = 16.0f * scale;
        c.size[0] = w;
        c.size[1] = h;
        draw_rect(ctx, rtv, x, y, w, h, v->srv, NULL, &c, 1);
        ID3D11RenderTargetView_Release(rtv);
    }
    ID3D11Texture2D_Release(back);
}

/* ------------------------------------------------------------------ state for the app */

static void open_state_mapping(void)
{
    if (g_state)
        return;
    wchar_t name[128];
    g_state_mapping = CreateFileMappingW(INVALID_HANDLE_VALUE, NULL, PAGE_READWRITE, 0, CABINPLAY_STATE_MAPPING_SIZE,
                                         mapping_name(CABINPLAY_STATE_MAPPING_NAME, name, 128));
    if (!g_state_mapping)
        return;
    g_state = (cabinplay_state_t *)MapViewOfFile(g_state_mapping, FILE_MAP_ALL_ACCESS, 0, 0, CABINPLAY_STATE_MAPPING_SIZE);
    if (!g_state) {
        CloseHandle(g_state_mapping);
        g_state_mapping = NULL;
        return;
    }
    g_state->magic = CABINPLAY_STATE_MAGIC;
    g_state->version = CABINPLAY_STATE_VERSION;
}

static void publish_state(int mounted, int nav_ready)
{
    if (!g_state)
        return;
    uint32_t flags = 0;
    if (g_telemetry_ok) flags |= CABINPLAY_STATE_TELEMETRY;
    if (g_paused) flags |= CABINPLAY_STATE_PAUSED;
    if (g_electric) flags |= CABINPLAY_STATE_ELECTRIC;
    if (mounted) flags |= CABINPLAY_STATE_MOUNTED;
    if (g_control_reported) flags |= CABINPLAY_STATE_CONTROL;
    if (nav_ready) flags |= CABINPLAY_STATE_NAV_READY;
    g_state->flags = flags;
    float speed = g_speed_ms * 3.6f;
    g_state->speed_kmh = speed < 0 ? -speed : speed;
    g_state->speed_limit_kmh = g_limit_ms * 3.6f;
    g_state->nav_distance_m = g_nav_distance;
    g_state->nav_time_s = g_nav_time;
    g_state->game_time_min = g_game_time;
    g_state->heartbeat_ms = GetTickCount64();
}

/* Caller holds g_input_lock. */
static void push_event(uint32_t type, int32_t x, int32_t y, int32_t data)
{
    if (!g_state)
        return;
    uint32_t n = g_state->event_write;
    cabinplay_event_t *e = &g_state->events[n % CABINPLAY_EVENT_CAPACITY];
    e->type = type;
    e->x = x;
    e->y = y;
    e->data = data;
    g_state->event_write = n + 1; /* published last, so the reader never sees a half-written slot */
}

/* ------------------------------------------------------------------ control mode input */

/* Control mode hands the mouse and keyboard to CabinPlay. Games read them in different
 * ways depending on their settings, so every route is covered:
 *
 *   keyboard   window messages (also the source of what is typed: they carry the right
 *              characters for any layout, and key repeat), GetAsyncKeyState / GetKeyState,
 *              DirectInput, raw input
 *   mouse      DirectInput, raw input, the system cursor, window messages
 *
 * The game's own pause key is the one thing let through, because control mode may press
 * it to pause the game while the screen is being used. */

typedef SHORT(WINAPI *key_state_fn)(int);
typedef UINT(WINAPI *raw_data_fn)(HRAWINPUT, UINT, LPVOID, PUINT, UINT);
typedef UINT(WINAPI *raw_buffer_fn)(PRAWINPUT, PUINT, UINT);
typedef BOOL(WINAPI *cursor_info_fn)(PCURSORINFO);
static key_state_fn o_GetAsyncKeyState, o_GetKeyState;
static raw_data_fn o_GetRawInputData;
static raw_buffer_fn o_GetRawInputBuffer;
static get_cursor_fn o_GetPhysicalCursorPos;
static cursor_info_fn o_GetCursorInfo;

/* Windows whose messages are filtered in control mode: the game's own, and the one its
 * raw input is delivered to if that is a different one. */
#define MAX_SUBCLASSED 4
static struct { HWND wnd; WNDPROC original; } g_windows[MAX_SUBCLASSED];
static int g_raw_keyboard_only;       /* the game gets no key messages, only raw keyboard input */

/* How often each route was used during one control session; written to the log when it
 * ends, so a report from someone else's PC shows how their game reads its input. */
static struct {
    LONG di_mouse, di_keyboard, raw_message, raw_buffer, raw_data, cursor_pos, key_state, key_message,
        mouse_message;
} g_routes;

typedef struct scs_input_event {
    uint32_t input_index;
    union {
        uint8_t b;
        float f;
        float sizing[6];
    } value;
} scs_input_event_t; /* 28 bytes, as in scssdk_input_event.h */

/* The game's "pause" control, reached through the input API (see scs_input_init). */
static int g_semantic_registered;
static volatile int g_semantic_active;
static volatile LONG g_semantic_taps; /* presses of "pause" still to deliver */
static int g_semantic_held;

static uint8_t g_vk_down[256];        /* keys held, from window messages */
static UINT g_pending_vk;             /* key whose character has not arrived yet */

static int g_pause_want = -1;         /* 1: pause the game, 0: resume it, -1: nothing to do */
static int g_pause_method;            /* PAUSE_BY_* */
static int g_pause_method_worked;
static int g_pause_stage;             /* frames into the current attempt */
static int g_we_paused;               /* the game is paused because control mode asked for it */
static int g_resume_on_exit;
static volatile LONG g_di_pause_reads; /* keyboard reads left in which the pause key is reported */

enum { PAUSE_BY_INPUT_API = 0, PAUSE_BY_KEY_PRESS, PAUSE_BY_DIRECTINPUT };
static const char *const PAUSE_METHOD_NAME[] = {"input API", "key press", "DirectInput"};

static void on_mouse_move(long dx, long dy, long dz);
static void on_mouse_button(int button, int down);

static SHORT real_async_key(int vk)
{
    return o_GetAsyncKeyState ? o_GetAsyncKeyState(vk) : GetAsyncKeyState(vk);
}

static SHORT real_key_state(int vk)
{
    return o_GetKeyState ? o_GetKeyState(vk) : GetKeyState(vk);
}

static BOOL real_get_cursor(POINT *p)
{
    return o_GetCursorPos ? o_GetCursorPos(p) : GetCursorPos(p);
}

static BOOL real_set_cursor(int x, int y)
{
    return o_SetCursorPos ? o_SetCursorPos(x, y) : SetCursorPos(x, y);
}

static int is_pause_dik(DWORD dik)
{
    return dik == DIK_PAUSE || dik == DIK_NUMLOCK; /* the two share a scan code */
}

static void subclass_window(HWND wnd);

/* Looks at what raw input the game asked Windows for, and makes sure the window it is
 * delivered to is filtered too. */
static void inspect_raw_input(void)
{
    RAWINPUTDEVICE devices[16];
    UINT count = 16;
    g_raw_keyboard_only = 0;
    UINT n = GetRegisteredRawInputDevices(devices, &count, sizeof(RAWINPUTDEVICE));
    if (n == (UINT)-1)
        return;
    for (UINT i = 0; i < n; i++) {
        int mouse = devices[i].usUsagePage == 1 && devices[i].usUsage == 2;
        int keyboard = devices[i].usUsagePage == 1 && devices[i].usUsage == 6;
        if (!mouse && !keyboard)
            continue;
        logf_("raw input registered: %s, flags 0x%lx", mouse ? "mouse" : "keyboard", (unsigned long)devices[i].dwFlags);
        if (keyboard && (devices[i].dwFlags & RIDEV_NOLEGACY))
            g_raw_keyboard_only = 1;
        if (devices[i].hwndTarget)
            subclass_window(devices[i].hwndTarget);
    }
}

static void set_control(int on)
{
    EnterCriticalSection(&g_input_lock);
    if (on != g_control) {
        if (on) {
            memset(g_vk_down, 0, sizeof(g_vk_down));
            g_pending_vk = 0;
            real_get_cursor(&g_frozen_cursor);
            g_poll_valid = 0;
            g_arrow_frames = 0;
            g_resume_on_exit = 0;
            memset(&g_routes, 0, sizeof(g_routes));
            inspect_raw_input();
            /* Pause the game so the truck does not drive on while someone types. */
            if (g_cfg_pause_game && g_telemetry_ok && !g_paused && g_pause_want < 0) {
                g_pause_want = 1;
                g_pause_method = PAUSE_BY_INPUT_API;
                g_pause_stage = 0;
            }
        } else {
            if (g_cfg_control_mouse && g_poll_valid)
                real_set_cursor(g_frozen_cursor.x, g_frozen_cursor.y); /* no jump for the game */
            for (int b = 0; b < 3; b++) /* never leave a button stuck down in the page */
                if (g_buttons[b])
                    push_event(CABINPLAY_EVENT_BUTTON, (int)g_cursor_x, (int)g_cursor_y, b);
            g_resume_on_exit = 1;
            logf_("input routes used: DirectInput mouse %ld keyboard %ld, raw message %ld buffer %ld data %ld, "
                  "cursor %ld, key state %ld, key messages %ld, mouse messages %ld",
                  g_routes.di_mouse, g_routes.di_keyboard, g_routes.raw_message, g_routes.raw_buffer,
                  g_routes.raw_data, g_routes.cursor_pos, g_routes.key_state, g_routes.key_message,
                  g_routes.mouse_message);
        }
        memset(g_buttons, 0, sizeof(g_buttons));
        g_control = on;
        logf_("control mode %s", on ? "on" : "off");
    }
    LeaveCriticalSection(&g_input_lock);
}

/* ---- pausing the game */

static void send_pause_key(int down)
{
    INPUT in;
    memset(&in, 0, sizeof(in));
    in.type = INPUT_KEYBOARD;
    in.ki.wVk = (WORD)g_cfg_pause_vk;
    in.ki.wScan = (WORD)MapVirtualKeyW((UINT)g_cfg_pause_vk, MAPVK_VK_TO_VSC);
    in.ki.dwFlags = down ? 0 : KEYEVENTF_KEYUP;
    SendInput(1, &in, sizeof(in));
}

/* Once per frame. Asks the game to pause (or resume) and watches the telemetry to see
 * whether it did, moving on to the next way of asking if not:
 *   1. the game's own "pause" control through the input API: no key binding involved
 *   2. a synthetic press of the pause key
 *   3. the same press fed in through DirectInput */
static void drive_pause(void)
{
    if (g_pause_want < 0) {
        if (g_resume_on_exit && !g_control) {
            g_resume_on_exit = 0;
            if (g_we_paused && g_paused) {
                g_pause_want = 0;
                g_pause_method = g_pause_method_worked;
                g_pause_stage = 0;
            } else {
                g_we_paused = 0;
            }
        }
        return;
    }
    if ((g_paused != 0) == (g_pause_want == 1)) {
        if (g_pause_want == 1) {
            g_we_paused = 1;
            g_pause_method_worked = g_pause_method;
            logf_("game paused for control mode (%s)", PAUSE_METHOD_NAME[g_pause_method]);
        } else {
            g_we_paused = 0;
        }
        g_pause_want = -1;
        return;
    }

    for (;;) {
        int skip = 0;
        if (g_pause_method == PAUSE_BY_INPUT_API) {
            if (!g_semantic_registered || !g_semantic_active)
                skip = 1;
            else if (g_pause_stage == 0)
                InterlockedExchange(&g_semantic_taps, 1);
        } else if (g_pause_method == PAUSE_BY_KEY_PRESS) {
            /* A synthetic key goes to whichever window has the keyboard: only ever the game. */
            if (!g_cfg_pause_vk || !g_game_window || GetForegroundWindow() != g_game_window)
                skip = 1;
            else if (g_pause_stage == 0)
                send_pause_key(1);
            else if (g_pause_stage == 3)
                send_pause_key(0);
        } else {
            if (g_pause_stage == 0)
                InterlockedExchange(&g_di_pause_reads, 6);
        }
        if (!skip && g_pause_stage <= 45)
            break;
        if (g_pause_method == PAUSE_BY_DIRECTINPUT) {
            logf_("could not %s the game (see pause_key in the ini)", g_pause_want == 1 ? "pause" : "resume");
            g_pause_want = -1;
            return;
        }
        g_pause_method++;
        g_pause_stage = 0;
    }
    g_pause_stage++;
}

/* The input API asks for events once or more per frame until there are none. */
static int32_t __stdcall on_semantic_event(scs_input_event_t *event, uint32_t flags, void *context)
{
    (void)context;
    if (!(flags & 1)) /* only at the start of a frame: one change per frame */
        return -4;    /* SCS_RESULT_not_found */
    memset(event, 0, sizeof(*event));
    if (g_semantic_held) {
        g_semantic_held = 0; /* release what was pressed in the previous frame */
        return 0;
    }
    if (InterlockedCompareExchange(&g_semantic_taps, 0, 1) == 1) {
        event->value.b = 1;
        g_semantic_held = 1;
        return 0;
    }
    return -4;
}

static void __stdcall on_semantic_active(uint8_t active, void *context)
{
    (void)context;
    g_semantic_active = active != 0;
}
/* ---- mouse */

/* Caller holds g_input_lock. */
static void on_mouse_move(long dx, long dy, long dz)
{
    if (dx || dy) {
        float k = g_cfg_cursor_speed / 100.0f;
        g_cursor_x += dx * k;
        g_cursor_y += dy * k;
        if (g_cursor_x < 0) g_cursor_x = 0;
        if (g_cursor_y < 0) g_cursor_y = 0;
        if (g_cursor_x > CABINPLAY_WIDTH - 1) g_cursor_x = CABINPLAY_WIDTH - 1;
        if (g_cursor_y > CABINPLAY_HEIGHT - 1) g_cursor_y = CABINPLAY_HEIGHT - 1;
        push_event(CABINPLAY_EVENT_MOVE, (int)g_cursor_x, (int)g_cursor_y, 0);
    }
    if (dz)
        push_event(CABINPLAY_EVENT_WHEEL, (int)g_cursor_x, (int)g_cursor_y, (int)dz);
}

/* Caller holds g_input_lock. */
static void on_mouse_button(int button, int down)
{
    if (button < 0 || button > 2 || down == g_buttons[button])
        return;
    g_buttons[button] = (uint8_t)down;
    push_event(CABINPLAY_EVENT_BUTTON, (int)g_cursor_x, (int)g_cursor_y, button | (down << 8));
}

/* The system cursor, for games that steer or look with it instead of DirectInput:
 * measure how far it moved since the last frame, then put it back in the middle of the
 * window so it never runs into an edge. Caller holds g_input_lock. */
static void poll_cursor(void)
{
    if (!g_game_window || GetForegroundWindow() != g_game_window) {
        g_poll_valid = 0;
        return;
    }
    POINT now, centre;
    RECT rc;
    if (!real_get_cursor(&now) || !GetClientRect(g_game_window, &rc))
        return;
    centre.x = (rc.left + rc.right) / 2;
    centre.y = (rc.top + rc.bottom) / 2;
    ClientToScreen(g_game_window, &centre);
    /* DirectInput already reports this movement when the game uses it; do not count it twice. */
    if (g_poll_valid && GetTickCount64() - g_di_mouse_tick > 500)
        on_mouse_move(now.x - g_poll_last.x, now.y - g_poll_last.y, 0);
    real_set_cursor(centre.x, centre.y);
    g_poll_last = centre;
    g_poll_valid = 1;
}

/* While the mouse belongs to CabinPlay the game is told the cursor has not moved. */
static BOOL WINAPI hk_GetCursorPos(LPPOINT p)
{
    if (g_control && g_cfg_control_mouse && p) {
        InterlockedIncrement(&g_routes.cursor_pos);
        *p = g_frozen_cursor;
        return TRUE;
    }
    return o_GetCursorPos(p);
}

static BOOL WINAPI hk_GetPhysicalCursorPos(LPPOINT p)
{
    if (g_control && g_cfg_control_mouse && p) {
        InterlockedIncrement(&g_routes.cursor_pos);
        *p = g_frozen_cursor;
        return TRUE;
    }
    return o_GetPhysicalCursorPos(p);
}

static BOOL WINAPI hk_GetCursorInfo(PCURSORINFO info)
{
    BOOL ok = o_GetCursorInfo(info);
    if (ok && info && g_control && g_cfg_control_mouse) {
        InterlockedIncrement(&g_routes.cursor_pos);
        info->ptScreenPos = g_frozen_cursor;
    }
    return ok;
}

static BOOL WINAPI hk_SetCursorPos(int x, int y)
{
    if (g_control && g_cfg_control_mouse)
        return TRUE;
    return o_SetCursorPos(x, y);
}

/* ---- keyboard */

/* Polled key state: nothing is held as far as the game is concerned. */
static SHORT WINAPI hk_GetAsyncKeyState(int vk)
{
    if (g_control && vk != g_cfg_pause_vk) {
        InterlockedIncrement(&g_routes.key_state);
        return 0;
    }
    return o_GetAsyncKeyState(vk);
}

static SHORT WINAPI hk_GetKeyState(int vk)
{
    if (g_control && vk != g_cfg_pause_vk) {
        InterlockedIncrement(&g_routes.key_state);
        return 0;
    }
    return o_GetKeyState(vk);
}

static uint32_t current_modifiers(void)
{
    uint32_t m = 0;
    if (real_key_state(VK_SHIFT) & 0x8000) m |= CABINPLAY_MOD_SHIFT;
    if (real_key_state(VK_CONTROL) & 0x8000) m |= CABINPLAY_MOD_CTRL;
    if (real_key_state(VK_LMENU) & 0x8000) m |= CABINPLAY_MOD_ALT;
    return m;
}

static int is_modifier_vk(UINT vk)
{
    return vk == VK_SHIFT || vk == VK_CONTROL || vk == VK_MENU || vk == VK_LWIN || vk == VK_RWIN ||
           vk == VK_CAPITAL || vk == VK_NUMLOCK || vk == VK_SCROLL;
}

/* Keys whose key-down is followed by a character message that carries what was typed. */
static int key_makes_character(UINT vk)
{
    if (vk == VK_BACK || vk == VK_TAB || vk == VK_RETURN || vk == VK_ESCAPE)
        return 0;
    return MapVirtualKeyW(vk, MAPVK_VK_TO_CHAR) != 0;
}

/* Keyboard cursor (control_mouse=0): the mouse stays with the game. The arrows move the
 * cursor (see update_control), Enter clicks, Page Up / Down scroll, Shift+Enter still
 * types a real Enter. Returns 1 when the key was used for that. Caller holds g_input_lock. */
static int keyboard_cursor_key(UINT vk, int down)
{
    if (g_cfg_control_mouse)
        return 0;
    if (vk == VK_UP || vk == VK_DOWN || vk == VK_LEFT || vk == VK_RIGHT)
        return 1;
    if (vk == VK_RETURN && !(real_key_state(VK_SHIFT) & 0x8000)) {
        on_mouse_button(0, down);
        return 1;
    }
    if (vk == VK_PRIOR || vk == VK_NEXT) {
        if (down)
            on_mouse_move(0, 0, vk == VK_PRIOR ? 240 : -240);
        return 1;
    }
    return 0;
}

/* Returns 1 when the message was taken for CabinPlay and must not reach the game. */
static int handle_key_message(UINT msg, WPARAM wp)
{
    UINT vk = (UINT)wp & 0xFF;
    int down = msg == WM_KEYDOWN || msg == WM_SYSKEYDOWN;
    int up = msg == WM_KEYUP || msg == WM_SYSKEYUP;
    int character = msg == WM_CHAR || msg == WM_SYSCHAR;
    if ((down || up) && (int)vk == g_cfg_pause_vk)
        return 0; /* the game's pause key stays with the game */
    if (msg == WM_SYSKEYDOWN && vk == VK_F4)
        return 0; /* Alt+F4 still closes the game */

    int taken = 0;
    EnterCriticalSection(&g_input_lock);
    if (g_control) {
        taken = 1;
        /* Ctrl+Alt chords are the companion app's global hotkeys, not typing. AltGr, which
         * Windows reports as Ctrl + right Alt, is typing. */
        int chord = (real_key_state(VK_CONTROL) & 0x8000) && (real_key_state(VK_LMENU) & 0x8000) &&
                    !(real_key_state(VK_RMENU) & 0x8000);
        if (down) {
            g_vk_down[vk] = 1;
            if (vk == VK_ESCAPE)
                set_control(0); /* Esc always hands the keyboard back to the game */
            else if (chord || is_modifier_vk(vk) || keyboard_cursor_key(vk, 1))
                ;
            else if (key_makes_character(vk))
                g_pending_vk = vk;
            else
                push_event(CABINPLAY_EVENT_KEY, (int32_t)vk, 1 | (int32_t)(current_modifiers() << 8), 0);
        } else if (up) {
            g_vk_down[vk] = 0;
            if (vk != VK_ESCAPE && !is_modifier_vk(vk) && !keyboard_cursor_key(vk, 0))
                push_event(CABINPLAY_EVENT_KEY, (int32_t)vk, (int32_t)(current_modifiers() << 8), 0);
        } else if (character && !chord) {
            WCHAR ch = (WCHAR)wp;
            UINT key = g_pending_vk;
            if (ch >= 0x20 && ch != 0x7F) {
                /* A typed character: Ctrl and Alt were part of producing it (AltGr), so
                 * only Shift is passed on. */
                push_event(CABINPLAY_EVENT_KEY, (int32_t)key,
                           1 | (int32_t)((current_modifiers() & CABINPLAY_MOD_SHIFT) << 8), ch);
            } else if (key) {
                /* Ctrl+letter and the like arrive as control characters: send the key. */
                push_event(CABINPLAY_EVENT_KEY, (int32_t)key, 1 | (int32_t)(current_modifiers() << 8), 0);
            }
        }
    }
    LeaveCriticalSection(&g_input_lock);
    return taken;
}

/* ---- DirectInput */

/* 1 = mouse, 2 = keyboard, 0 = anything else (wheels and pads are never touched). */
static int device_kind(void *dev)
{
    static struct { void *dev; int kind; } cache[16];
    static int next;
    for (int i = 0; i < 16; i++)
        if (cache[i].dev == dev)
            return cache[i].kind;
    DIDEVCAPS caps;
    memset(&caps, 0, sizeof(caps));
    caps.dwSize = sizeof(caps);
    int kind = 0;
    /* The capability call has the same slot and layout in the ANSI and Unicode interfaces. */
    if (SUCCEEDED(IDirectInputDevice8_GetCapabilities((IDirectInputDevice8W *)dev, &caps))) {
        BYTE type = (BYTE)(caps.dwDevType & 0xFF);
        kind = type == DI8DEVTYPE_MOUSE ? 1 : type == DI8DEVTYPE_KEYBOARD ? 2 : 0;
    }
    cache[next].dev = dev;
    cache[next].kind = kind;
    next = (next + 1) % 16;
    return kind;
}

/* Takes one of the keyboard reads in which the pause key is reported. Returns how many
 * were left before this one, 0 if none. */
static LONG take_pause_read(void)
{
    for (;;) {
        LONG left = g_di_pause_reads;
        if (left <= 0)
            return 0;
        if (InterlockedCompareExchange(&g_di_pause_reads, left - 1, left) == left)
            return left;
    }
}

/* In control mode the game's keyboard and mouse reads are blanked, so the truck does not
 * react; the mouse data is used for CabinPlay first. */
static HRESULT di_state_common(di_state_fn original, void *dev, DWORD size, LPVOID data)
{
    HRESULT hr = original(dev, size, data);
    if ((!g_control && g_di_pause_reads <= 0) || FAILED(hr) || !data)
        return hr;
    int kind = device_kind(dev);
    if (kind == 2 && size >= 256) {
        BYTE *k = (BYTE *)data;
        if (g_control) {
            InterlockedIncrement(&g_routes.di_keyboard);
            BYTE pause = k[DIK_PAUSE], numlock = k[DIK_NUMLOCK];
            memset(data, 0, size);
            k[DIK_PAUSE] = pause;
            k[DIK_NUMLOCK] = numlock;
        }
        if (take_pause_read() > 1)
            k[DIK_PAUSE] = 0x80;
    } else if (kind == 1 && g_control && g_cfg_control_mouse) {
        InterlockedIncrement(&g_routes.di_mouse);
        EnterCriticalSection(&g_input_lock);
        if (g_control && size >= sizeof(DIMOUSESTATE)) {
            const DIMOUSESTATE *m = (const DIMOUSESTATE *)data;
            if (m->lX || m->lY || m->lZ)
                g_di_mouse_tick = GetTickCount64();
            on_mouse_move(m->lX, m->lY, m->lZ);
            for (int b = 0; b < 3; b++)
                on_mouse_button(b, (m->rgbButtons[b] & 0x80) != 0);
        }
        LeaveCriticalSection(&g_input_lock);
        memset(data, 0, size);
    }
    return hr;
}

static HRESULT di_data_common(di_data_fn original, void *dev, DWORD size, DIDEVICEOBJECTDATA *items, DWORD *count,
                              DWORD flags)
{
    DWORD capacity = count ? *count : 0;
    HRESULT hr = original(dev, size, items, count, flags);
    if ((!g_control && g_di_pause_reads <= 0) || FAILED(hr) || !count)
        return hr;
    int kind = device_kind(dev);
    if (!kind || size < sizeof(DIDEVICEOBJECTDATA) - sizeof(UINT_PTR))
        return hr;

    DWORD kept = *count;
    if (kind == 1 && g_control && g_cfg_control_mouse) {
        InterlockedIncrement(&g_routes.di_mouse);
        if (items && !(flags & DIGDD_PEEK)) {
            EnterCriticalSection(&g_input_lock);
            if (g_control) {
                for (DWORD i = 0; i < *count; i++) {
                    const DIDEVICEOBJECTDATA *d = (const DIDEVICEOBJECTDATA *)((const BYTE *)items + (size_t)i * size);
                    if (d->dwOfs <= DIMOFS_Z && d->dwData)
                        g_di_mouse_tick = GetTickCount64();
                    if (d->dwOfs == DIMOFS_X) on_mouse_move((long)d->dwData, 0, 0);
                    else if (d->dwOfs == DIMOFS_Y) on_mouse_move(0, (long)d->dwData, 0);
                    else if (d->dwOfs == DIMOFS_Z) on_mouse_move(0, 0, (long)d->dwData);
                    else if (d->dwOfs >= DIMOFS_BUTTON0 && d->dwOfs <= DIMOFS_BUTTON2)
                        on_mouse_button((int)(d->dwOfs - DIMOFS_BUTTON0), (d->dwData & 0x80) != 0);
                }
            }
            LeaveCriticalSection(&g_input_lock);
        }
        kept = 0;
    } else if (kind == 2) {
        if (g_control) {
            InterlockedIncrement(&g_routes.di_keyboard);
            /* Drop everything but the pause key. */
            kept = 0;
            if (items) {
                for (DWORD i = 0; i < *count; i++) {
                    BYTE *d = (BYTE *)items + (size_t)i * size;
                    if (is_pause_dik(((DIDEVICEOBJECTDATA *)d)->dwOfs)) {
                        if (kept != i)
                            memmove((BYTE *)items + (size_t)kept * size, d, size);
                        kept++;
                    }
                }
            }
        }
        LONG left = (items && !(flags & DIGDD_PEEK)) ? take_pause_read() : 0;
        if ((left == 6 || left == 1) && kept < capacity) {
            DIDEVICEOBJECTDATA *d = (DIDEVICEOBJECTDATA *)((BYTE *)items + (size_t)kept * size);
            memset(d, 0, size);
            d->dwOfs = DIK_PAUSE;
            d->dwData = left == 6 ? 0x80 : 0; /* pressed on the first read, released on the last */
            d->dwTimeStamp = GetTickCount();
            kept++;
        }
    } else {
        return hr;
    }
    *count = kept;
    return hr == DI_BUFFEROVERFLOW ? DI_OK : hr;
}

static HRESULT STDMETHODCALLTYPE hk_di_state0(void *d, DWORD n, LPVOID p) { return di_state_common(o_di_state[0], d, n, p); }
static HRESULT STDMETHODCALLTYPE hk_di_state1(void *d, DWORD n, LPVOID p) { return di_state_common(o_di_state[1], d, n, p); }
static HRESULT STDMETHODCALLTYPE hk_di_data0(void *d, DWORD n, DIDEVICEOBJECTDATA *p, DWORD *c, DWORD f)
{
    return di_data_common(o_di_data[0], d, n, p, c, f);
}
static HRESULT STDMETHODCALLTYPE hk_di_data1(void *d, DWORD n, DIDEVICEOBJECTDATA *p, DWORD *c, DWORD f)
{
    return di_data_common(o_di_data[1], d, n, p, c, f);
}

/* ---- raw input */

static UINT real_raw_data(HRAWINPUT handle, UINT command, LPVOID data, PUINT size, UINT header)
{
    return o_GetRawInputData ? o_GetRawInputData(handle, command, data, size, header)
                             : GetRawInputData(handle, command, data, size, header);
}

/* A key from raw input, for games that get no key messages at all. The character has
 * to be worked out here. Caller holds g_input_lock. */
static void raw_key(UINT vk, UINT scan, int down)
{
    vk &= 0xFF;
    g_vk_down[vk] = (uint8_t)down;
    int shift = g_vk_down[VK_SHIFT], ctrl = g_vk_down[VK_CONTROL], alt = g_vk_down[VK_MENU];
    uint32_t mods = (shift ? CABINPLAY_MOD_SHIFT : 0) | (ctrl ? CABINPLAY_MOD_CTRL : 0) | (alt ? CABINPLAY_MOD_ALT : 0);
    if (!down) {
        if (vk != VK_ESCAPE && !is_modifier_vk(vk) && !keyboard_cursor_key(vk, 0))
            push_event(CABINPLAY_EVENT_KEY, (int32_t)vk, (int32_t)(mods << 8), 0);
        return;
    }
    if (vk == VK_ESCAPE) {
        set_control(0);
        return;
    }
    if ((ctrl && alt) || is_modifier_vk(vk) || keyboard_cursor_key(vk, 1))
        return;
    WCHAR ch = 0;
    if (key_makes_character(vk) && !ctrl) {
        BYTE state[256];
        memset(state, 0, sizeof(state));
        if (shift) state[VK_SHIFT] = 0x80;
        if (real_key_state(VK_CAPITAL) & 1) state[VK_CAPITAL] = 1;
        WCHAR buf[4];
        HKL layout = GetKeyboardLayout(g_game_window ? GetWindowThreadProcessId(g_game_window, NULL) : 0);
        if (ToUnicodeEx(vk, scan, state, buf, 4, 0, layout) == 1 && buf[0] >= 0x20 && buf[0] != 0x7F)
            ch = buf[0];
    }
    push_event(CABINPLAY_EVENT_KEY, (int32_t)vk, 1 | (int32_t)((ch ? (mods & CABINPLAY_MOD_SHIFT) : mods) << 8), ch);
}

/* Uses one raw input record for CabinPlay. Caller holds g_input_lock. */
static void consume_raw(const RAWINPUT *raw)
{
    if (raw->header.dwType == RIM_TYPEMOUSE && g_cfg_control_mouse) {
        const RAWMOUSE *m = &raw->data.mouse;
        if (!(m->usFlags & MOUSE_MOVE_ABSOLUTE) && (m->lLastX || m->lLastY)) {
            g_di_mouse_tick = GetTickCount64(); /* relative movement seen: do not also use the cursor */
            on_mouse_move(m->lLastX, m->lLastY, 0);
        }
        USHORT f = m->usButtonFlags;
        if (f & RI_MOUSE_LEFT_BUTTON_DOWN) on_mouse_button(0, 1);
        if (f & RI_MOUSE_LEFT_BUTTON_UP) on_mouse_button(0, 0);
        if (f & RI_MOUSE_RIGHT_BUTTON_DOWN) on_mouse_button(1, 1);
        if (f & RI_MOUSE_RIGHT_BUTTON_UP) on_mouse_button(1, 0);
        if (f & RI_MOUSE_MIDDLE_BUTTON_DOWN) on_mouse_button(2, 1);
        if (f & RI_MOUSE_MIDDLE_BUTTON_UP) on_mouse_button(2, 0);
        if (f & RI_MOUSE_WHEEL) {
            g_di_mouse_tick = GetTickCount64();
            on_mouse_move(0, 0, (short)m->usButtonData);
        }
    } else if (raw->header.dwType == RIM_TYPEKEYBOARD && g_raw_keyboard_only) {
        const RAWKEYBOARD *k = &raw->data.keyboard;
        if (k->VKey && k->VKey < 255)
            raw_key(k->VKey, k->MakeCode, !(k->Flags & RI_KEY_BREAK));
    }
}

static int raw_is_for_game(const RAWINPUT *raw)
{
    if (raw->header.dwType == RIM_TYPEKEYBOARD)
        return (int)raw->data.keyboard.VKey == g_cfg_pause_vk; /* the pause key stays with the game */
    return raw->header.dwType != RIM_TYPEMOUSE || !g_cfg_control_mouse; /* wheels and pads always do */
}

/* Makes a record the game is about to read say that nothing happened. */
static void blank_raw(RAWINPUT *raw)
{
    if (raw->header.dwType == RIM_TYPEMOUSE) {
        raw->data.mouse.lLastX = raw->data.mouse.lLastY = 0;
        raw->data.mouse.usButtonFlags = 0;
        raw->data.mouse.usButtonData = 0;
    } else if (raw->header.dwType == RIM_TYPEKEYBOARD) {
        raw->data.keyboard.VKey = 0xFF;
        raw->data.keyboard.MakeCode = 0;
        raw->data.keyboard.Message = WM_NULL;
    }
}

/* Reached only if the game reads raw input somewhere other than a filtered window. */
static UINT WINAPI hk_GetRawInputData(HRAWINPUT handle, UINT command, LPVOID data, PUINT size, UINT header)
{
    UINT result = o_GetRawInputData(handle, command, data, size, header);
    if (g_control && command == RID_INPUT && data && result != (UINT)-1 && result >= sizeof(RAWINPUTHEADER)) {
        RAWINPUT *raw = (RAWINPUT *)data;
        if (!raw_is_for_game(raw)) {
            InterlockedIncrement(&g_routes.raw_data);
            EnterCriticalSection(&g_input_lock);
            if (g_control)
                consume_raw(raw);
            LeaveCriticalSection(&g_input_lock);
            blank_raw(raw);
        }
    }
    return result;
}

static UINT WINAPI hk_GetRawInputBuffer(PRAWINPUT data, PUINT size, UINT header)
{
    UINT result = o_GetRawInputBuffer(data, size, header);
    if (g_control && data && result != (UINT)-1 && result > 0) {
        InterlockedIncrement(&g_routes.raw_buffer);
        EnterCriticalSection(&g_input_lock);
        PRAWINPUT raw = data;
        for (UINT i = 0; i < result; i++) {
            if (!raw_is_for_game(raw)) {
                if (g_control)
                    consume_raw(raw);
                blank_raw(raw);
            }
            raw = NEXTRAWINPUTBLOCK(raw);
        }
        LeaveCriticalSection(&g_input_lock);
    }
    return result;
}

/* ---- the game's windows */

static WNDPROC original_proc(HWND wnd)
{
    for (int i = 0; i < MAX_SUBCLASSED; i++)
        if (g_windows[i].wnd == wnd)
            return g_windows[i].original;
    return NULL;
}

static LRESULT CALLBACK hk_wndproc(HWND wnd, UINT msg, WPARAM wp, LPARAM lp)
{
    if (g_control) {
        if (msg == WM_INPUT) {
            RAWINPUT raw;
            UINT size = sizeof(raw);
            if (real_raw_data((HRAWINPUT)lp, RID_INPUT, &raw, &size, sizeof(RAWINPUTHEADER)) != (UINT)-1 &&
                !raw_is_for_game(&raw)) {
                InterlockedIncrement(&g_routes.raw_message);
                EnterCriticalSection(&g_input_lock);
                if (g_control)
                    consume_raw(&raw);
                LeaveCriticalSection(&g_input_lock);
                return DefWindowProcW(wnd, msg, wp, lp); /* Windows still needs to clean up */
            }
        } else if (msg >= WM_KEYFIRST && msg <= WM_KEYLAST) {
            InterlockedIncrement(&g_routes.key_message);
            if (handle_key_message(msg, wp))
                return 0;
        } else if (g_cfg_control_mouse && msg >= WM_MOUSEFIRST && msg <= WM_MOUSELAST) {
            InterlockedIncrement(&g_routes.mouse_message);
            int button = -1, down = 0;
            switch (msg) {
            case WM_LBUTTONDOWN: case WM_LBUTTONDBLCLK: button = 0; down = 1; break;
            case WM_LBUTTONUP: button = 0; break;
            case WM_RBUTTONDOWN: case WM_RBUTTONDBLCLK: button = 1; down = 1; break;
            case WM_RBUTTONUP: button = 1; break;
            case WM_MBUTTONDOWN: case WM_MBUTTONDBLCLK: button = 2; down = 1; break;
            case WM_MBUTTONUP: button = 2; break;
            }
            EnterCriticalSection(&g_input_lock);
            if (g_control) {
                if (button >= 0)
                    on_mouse_button(button, down);
                else if (msg == WM_MOUSEWHEEL && GetTickCount64() - g_di_mouse_tick > 500)
                    on_mouse_move(0, 0, GET_WHEEL_DELTA_WPARAM(wp));
            }
            LeaveCriticalSection(&g_input_lock);
            return 0;
        }
    }
    WNDPROC original = original_proc(wnd);
    if (!original)
        return DefWindowProcW(wnd, msg, wp, lp);
    return IsWindowUnicode(wnd) ? CallWindowProcW(original, wnd, msg, wp, lp)
                                : CallWindowProcA(original, wnd, msg, wp, lp);
}

static void subclass_window(HWND wnd)
{
    DWORD process = 0;
    if (!wnd || !GetWindowThreadProcessId(wnd, &process) || process != GetCurrentProcessId())
        return;
    int slot = -1;
    for (int i = 0; i < MAX_SUBCLASSED; i++) {
        if (g_windows[i].wnd == wnd)
            return;
        if (!g_windows[i].wnd && slot < 0)
            slot = i;
    }
    if (slot < 0)
        return;
    /* The table entry must exist before the window procedure can be called. */
    g_windows[slot].wnd = wnd;
    g_windows[slot].original = (WNDPROC)(IsWindowUnicode(wnd) ? GetWindowLongPtrW(wnd, GWLP_WNDPROC)
                                                               : GetWindowLongPtrA(wnd, GWLP_WNDPROC));
    LONG_PTR previous = IsWindowUnicode(wnd) ? SetWindowLongPtrW(wnd, GWLP_WNDPROC, (LONG_PTR)hk_wndproc)
                                             : SetWindowLongPtrA(wnd, GWLP_WNDPROC, (LONG_PTR)hk_wndproc);
    if (previous)
        g_windows[slot].original = (WNDPROC)previous;
    else
        g_windows[slot].wnd = NULL;
}

static void subclass_game_window(void)
{
    subclass_window(g_game_window);
}

static void unsubclass_game_window(void)
{
    for (int i = 0; i < MAX_SUBCLASSED; i++) {
        HWND wnd = g_windows[i].wnd;
        if (!wnd)
            continue;
        /* Only undo it if nobody subclassed the window after this plugin did. */
        if (IsWindow(wnd) && (WNDPROC)GetWindowLongPtrW(wnd, GWLP_WNDPROC) == hk_wndproc) {
            if (IsWindowUnicode(wnd))
                SetWindowLongPtrW(wnd, GWLP_WNDPROC, (LONG_PTR)g_windows[i].original);
            else
                SetWindowLongPtrA(wnd, GWLP_WNDPROC, (LONG_PTR)g_windows[i].original);
            g_windows[i].wnd = NULL;
        }
    }
}
static int start_companion(int asked);

/* Once per frame: the Ctrl+Alt+<key> toggle, the cursor, and pausing the game. */
static void update_control(int allowed)
{
    int down = (real_async_key(VK_CONTROL) & 0x8000) && (real_async_key(VK_MENU) & 0x8000) &&
               (real_async_key(g_cfg_toggle_vk) & 0x8000);
    /* If the player closed the app from its settings, the same key brings it back. */
    if (down && !g_toggle_was_down && !start_companion(1))
        set_control(!g_control && allowed);
    g_toggle_was_down = down;
    if (g_control && !allowed)
        set_control(0);

    if (g_control) {
        EnterCriticalSection(&g_input_lock);
        if (g_cfg_control_mouse) {
            poll_cursor();
        } else {
            int dx = (g_vk_down[VK_RIGHT] ? 1 : 0) - (g_vk_down[VK_LEFT] ? 1 : 0);
            int dy = (g_vk_down[VK_DOWN] ? 1 : 0) - (g_vk_down[VK_UP] ? 1 : 0);
            if (dx || dy) {
                /* starts slow for precision, speeds up while the key is held */
                float step = 1.5f + g_arrow_frames * 0.3f;
                if (step > 14.0f)
                    step = 14.0f;
                g_arrow_frames++;
                on_mouse_move((long)(dx * step), (long)(dy * step), 0);
            } else {
                g_arrow_frames = 0;
            }
        }
        LeaveCriticalSection(&g_input_lock);
    }
    drive_pause();
}

/* ------------------------------------------------------------------ per-frame work */

/* Runs once per presented frame on the render thread, with g_lock held. */
static void pump(IDXGISwapChain *sc, ID3D11Device *dev, ID3D11DeviceContext *ctx)
{
    static const unsigned screen_scan_age[] = {2, 30, 120, 600};
    entry_t *nav = NULL, *first_screen = NULL;

    for (int i = 0; i < MAX_ENTRIES; i++) {
        entry_t *e = &g_entries[i];
        if (e->state == STATE_FREE || e->dev != dev)
            continue;
        if (e->state == STATE_PENDING && e->kind == KIND_SCREEN &&
            g_frame_no - e->born_frame >= screen_scan_age[e->attempts]) {
            if (scan_texture(dev, ctx, e))
                confirm_entry(e, MAX_SCREEN_TARGETS);
            else if (++e->attempts == (int)(sizeof(screen_scan_age) / sizeof(screen_scan_age[0])))
                release_entry(e);
        } else if (e->state == STATE_PENDING && e->kind == KIND_NAV) {
            /* The game only draws the map once a truck is on the road, so keep checking. */
            unsigned interval = e->attempts < 4 ? 15u << e->attempts : 300u;
            if (g_frame_no - e->born_frame >= interval) {
                e->born_frame = g_frame_no;
                if (e->attempts < 100)
                    e->attempts++;
                if (scan_texture(dev, ctx, e)) {
                    D3D11_SHADER_RESOURCE_VIEW_DESC vd;
                    memset(&vd, 0, sizeof(vd));
                    vd.Format = view_format(e->format);
                    vd.ViewDimension = D3D11_SRV_DIMENSION_TEXTURE2D;
                    vd.Texture2D.MipLevels = 1;
                    if (SUCCEEDED(ID3D11Device_CreateShaderResourceView(dev, (ID3D11Resource *)e->tex, &vd, &e->srv)))
                        confirm_entry(e, 1);
                    else
                        release_entry(e);
                }
            }
        }
        if (e->state == STATE_TARGET && e->kind == KIND_NAV)
            nav = e;
        if (e->state == STATE_TARGET && e->kind == KIND_SCREEN && !first_screen)
            first_screen = e;
    }

    int screen_on = g_electric || !g_telemetry_ok;
    /* A pause that control mode asked for must not end control mode; any other pause
     * (a menu, the pause key) does, so the game is never left without its keyboard. */
    int our_pause = g_we_paused || g_pause_want == 1;
    int usable = first_screen && screen_on && !(g_telemetry_ok && g_paused && !our_pause);
    if (!first_screen) {
        update_control(0);
        g_control_reported = g_control || g_pause_want == 0 || (g_resume_on_exit && g_we_paused);
        publish_state(0, nav != NULL);
        return;
    }

    int fresh = fetch_frame();
    update_control(usable && g_have_frame && !g_blanked);
    /* Still "in control" for the app until the game is running again, so it does not
     * take the last moment of our own pause for a real one and stop the video. */
    g_control_reported = g_control || g_pause_want == 0 || (g_resume_on_exit && g_we_paused);
    publish_state(1, nav != NULL);

    if (!screen_on) {
        if (!g_screen_off_applied || g_targets_dirty) {
            for (int i = 0; i < MAX_ENTRIES; i++) {
                entry_t *e = &g_entries[i];
                if (e->state != STATE_TARGET || e->kind != KIND_SCREEN || e->dev != dev || !ensure_video(dev, e->family))
                    continue;
                video_t *v = &g_video[e->family];
                ID3D11DeviceContext_UpdateSubresource(ctx, (ID3D11Resource *)v->tex, 0, NULL, g_black, CABINPLAY_WIDTH * 4, 0);
                ID3D11DeviceContext_GenerateMips(ctx, v->srv);
                for (UINT m = 0; m < e->mips && m + e->mip_offset < VIDEO_MIPS; m++)
                    ID3D11DeviceContext_CopySubresourceRegion(ctx, (ID3D11Resource *)e->tex, m, 0, 0, 0,
                                                              (ID3D11Resource *)v->tex, m + e->mip_offset, NULL);
            }
            g_screen_off_applied = 1;
            g_targets_dirty = 0;
        }
        return;
    }
    if (g_screen_off_applied) {
        g_screen_off_applied = 0;
        g_targets_dirty = 1;
    }
    if (!g_have_frame)
        return;

    int show_map = nav && !g_blanked && shared_valid() && (g_shared->app_flags & CABINPLAY_APP_NAV_VISIBLE) &&
                   ensure_gfx(dev);
    /* Leaving the map needs one more upload to put the plain frame back. */
    if (fresh || g_targets_dirty || show_map || g_map_was_shown) {
        g_targets_dirty = 0;
        g_map_was_shown = show_map;
        int uploaded[FAMILY_COUNT] = {0, 0};
        for (int i = 0; i < MAX_ENTRIES; i++) {
            entry_t *e = &g_entries[i];
            if (e->state != STATE_TARGET || e->kind != KIND_SCREEN || e->dev != dev || !ensure_video(dev, e->family))
                continue;
            video_t *v = &g_video[e->family];
            if (!uploaded[e->family]) {
                if (show_map)
                    composite_with_map(ctx, v, nav);
                else
                    ID3D11DeviceContext_UpdateSubresource(ctx, (ID3D11Resource *)v->tex, 0, NULL,
                                                          frame_for_family(e->family), CABINPLAY_WIDTH * 4, 0);
                ID3D11DeviceContext_GenerateMips(ctx, v->srv);
                uploaded[e->family] = 1;
            }
            for (UINT m = 0; m < e->mips && m + e->mip_offset < VIDEO_MIPS; m++)
                ID3D11DeviceContext_CopySubresourceRegion(ctx, (ID3D11Resource *)e->tex, m, 0, 0, 0,
                                                          (ID3D11Resource *)v->tex, m + e->mip_offset, NULL);
        }
    }

    if (g_control && g_video[first_screen->family].tex && ensure_gfx(dev))
        draw_overlay(sc, dev, ctx, &g_video[first_screen->family]);
}

static void on_present(IDXGISwapChain *sc)
{
    ID3D11Device *dev = NULL;
    if (FAILED(IDXGISwapChain_GetDevice(sc, &IID_ID3D11Device, (void **)&dev)) || !dev)
        return;
    if (!g_game_window) {
        DXGI_SWAP_CHAIN_DESC d;
        if (SUCCEEDED(IDXGISwapChain_GetDesc(sc, &d)))
            g_game_window = d.OutputWindow;
    }
    subclass_game_window();
    ID3D11DeviceContext *ctx = NULL;
    ID3D11Device_GetImmediateContext(dev, &ctx);
    if (ctx) {
        EnterCriticalSection(&g_lock);
        g_frame_no++;
        pump(sc, dev, ctx);
        LeaveCriticalSection(&g_lock);
        ID3D11DeviceContext_Release(ctx);
    }
    ID3D11Device_Release(dev);
}

/* ------------------------------------------------------------------ Direct3D hooks */

static HRESULT STDMETHODCALLTYPE hk_CreateTexture2D(ID3D11Device *dev, const D3D11_TEXTURE2D_DESC *desc,
                                                    const D3D11_SUBRESOURCE_DATA *init, ID3D11Texture2D **out)
{
    D3D11_TEXTURE2D_DESC patched;
    int verdict = VERDICT_NO, kind = KIND_SCREEN;
    if (desc && out && is_screen_desc(desc)) {
        if (init && init[0].pSysMem)
            verdict = pixels_have_marker(format_family(desc->Format), (const uint8_t *)init[0].pSysMem,
                                         init[0].SysMemPitch, desc->Width, desc->Height)
                          ? VERDICT_YES
                          : VERDICT_NO;
        else
            verdict = VERDICT_MAYBE; /* filled in later; checked by reading it back */
        if (verdict == VERDICT_YES && desc->Usage == D3D11_USAGE_IMMUTABLE) {
            patched = *desc; /* must stay writable so frames can be copied in */
            patched.Usage = D3D11_USAGE_DEFAULT;
            desc = &patched;
        }
    } else if (desc && out && is_nav_desc(desc)) {
        verdict = VERDICT_MAYBE;
        kind = KIND_NAV;
    } else if (desc && desc->Width == CABINPLAY_WIDTH && desc->Height == CABINPLAY_HEIGHT &&
               (desc->BindFlags & D3D11_BIND_RENDER_TARGET)) {
        /* Right size for the map but not a kind this plugin can read: worth knowing about. */
        static int reported;
        if (reported++ < 8)
            logf_("ignored a %ux%u render target: format %d, %u samples, bind flags 0x%x", desc->Width, desc->Height,
                  (int)desc->Format, desc->SampleDesc.Count, desc->BindFlags);
    }
    HRESULT hr = o_CreateTexture2D(dev, desc, init, out);
    if (SUCCEEDED(hr) && verdict != VERDICT_NO && *out)
        track_texture(dev, *out, kind, verdict);
    return hr;
}

static HRESULT STDMETHODCALLTYPE hk_Present(IDXGISwapChain *sc, UINT sync, UINT flags)
{
    if (!g_in_present && !(flags & DXGI_PRESENT_TEST)) {
        g_in_present = 1;
        on_present(sc);
        HRESULT hr = o_Present(sc, sync, flags);
        g_in_present = 0;
        return hr;
    }
    return o_Present(sc, sync, flags);
}

static HRESULT STDMETHODCALLTYPE hk_Present1(IDXGISwapChain1 *sc, UINT sync, UINT flags,
                                             const DXGI_PRESENT_PARAMETERS *params)
{
    if (!g_in_present && !(flags & DXGI_PRESENT_TEST)) {
        g_in_present = 1;
        on_present((IDXGISwapChain *)sc);
        HRESULT hr = o_Present1(sc, sync, flags, params);
        g_in_present = 0;
        return hr;
    }
    return o_Present1(sc, sync, flags, params);
}

/* The Direct3D runtime shares its method implementations between all devices and
 * swap chains, so their addresses can be taken from throwaway objects. */
static int install_d3d_hooks(void)
{
    WNDCLASSEXW wc;
    memset(&wc, 0, sizeof(wc));
    wc.cbSize = sizeof(wc);
    wc.lpfnWndProc = DefWindowProcW;
    wc.hInstance = g_module;
    wc.lpszClassName = L"CabinPlayProbe";
    RegisterClassExW(&wc);
    HWND wnd = CreateWindowExW(0, wc.lpszClassName, L"", WS_OVERLAPPED, 0, 0, 64, 64, NULL, NULL, g_module, NULL);

    DXGI_SWAP_CHAIN_DESC scd;
    memset(&scd, 0, sizeof(scd));
    scd.BufferCount = 1;
    scd.BufferDesc.Width = 64;
    scd.BufferDesc.Height = 64;
    scd.BufferDesc.Format = DXGI_FORMAT_R8G8B8A8_UNORM;
    scd.BufferUsage = DXGI_USAGE_RENDER_TARGET_OUTPUT;
    scd.OutputWindow = wnd;
    scd.SampleDesc.Count = 1;
    scd.Windowed = TRUE;

    ID3D11Device *dev = NULL;
    IDXGISwapChain *sc = NULL;
    HRESULT hr = E_FAIL;
    const D3D_DRIVER_TYPE drivers[] = {D3D_DRIVER_TYPE_HARDWARE, D3D_DRIVER_TYPE_WARP};
    for (int i = 0; i < 2 && FAILED(hr); i++)
        hr = D3D11CreateDeviceAndSwapChain(NULL, drivers[i], NULL, 0, NULL, 0, D3D11_SDK_VERSION, &scd, &sc, &dev,
                                           NULL, NULL);
    int ok = 0;
    if (SUCCEEDED(hr) && dev && sc) {
        IDXGISwapChain1 *sc1 = NULL;
        IDXGISwapChain_QueryInterface(sc, &IID_IDXGISwapChain1, (void **)&sc1);
        MH_STATUS a = MH_CreateHook((LPVOID)dev->lpVtbl->CreateTexture2D, (LPVOID)hk_CreateTexture2D,
                                    (LPVOID *)&o_CreateTexture2D);
        MH_STATUS b = MH_CreateHook((LPVOID)sc->lpVtbl->Present, (LPVOID)hk_Present, (LPVOID *)&o_Present);
        MH_STATUS c = MH_OK;
        if (sc1)
            c = MH_CreateHook((LPVOID)sc1->lpVtbl->Present1, (LPVOID)hk_Present1, (LPVOID *)&o_Present1);
        logf_("hooks: CreateTexture2D=%d Present=%d Present1=%d", (int)a, (int)b, (int)c);
        ok = a == MH_OK && b == MH_OK;
        if (sc1)
            IDXGISwapChain1_Release(sc1);
    } else {
        logf_("could not create the probe device (hr=0x%08lx)", (unsigned long)hr);
    }
    if (sc)
        IDXGISwapChain_Release(sc);
    if (dev)
        ID3D11Device_Release(dev);
    if (wnd)
        DestroyWindow(wnd);
    UnregisterClassW(wc.lpszClassName, g_module);
    return ok;
}

/* The game reads keyboard and mouse through DirectInput 8. Its device methods are shared
 * by all devices too; the ANSI and Unicode interfaces may or may not share them. */
static void install_input_hooks(void)
{
    typedef HRESULT(WINAPI *create_fn)(HINSTANCE, DWORD, REFIID, LPVOID *, LPUNKNOWN);
    HMODULE lib = LoadLibraryW(L"dinput8.dll");
    create_fn create = lib ? (create_fn)(void *)GetProcAddress(lib, "DirectInput8Create") : NULL;
    if (!create) {
        logf_("DirectInput is not available: control mode is disabled");
        return;
    }
    void *state_fn[2] = {NULL, NULL}, *data_fn[2] = {NULL, NULL};

    IDirectInput8W *diw = NULL;
    if (SUCCEEDED(create(g_module, DIRECTINPUT_VERSION, &IID_IDirectInput8W, (LPVOID *)&diw, NULL)) && diw) {
        IDirectInputDevice8W *dev = NULL;
        if (SUCCEEDED(IDirectInput8_CreateDevice(diw, &GUID_SysKeyboard, &dev, NULL)) && dev) {
            state_fn[0] = (void *)dev->lpVtbl->GetDeviceState;
            data_fn[0] = (void *)dev->lpVtbl->GetDeviceData;
            IDirectInputDevice8_Release(dev);
        }
        IDirectInput8_Release(diw);
    }
    IDirectInput8A *dia = NULL;
    if (SUCCEEDED(create(g_module, DIRECTINPUT_VERSION, &IID_IDirectInput8A, (LPVOID *)&dia, NULL)) && dia) {
        IDirectInputDevice8A *dev = NULL;
        if (SUCCEEDED(IDirectInput8_CreateDevice(dia, &GUID_SysKeyboard, &dev, NULL)) && dev) {
            state_fn[1] = (void *)dev->lpVtbl->GetDeviceState;
            data_fn[1] = (void *)dev->lpVtbl->GetDeviceData;
            IDirectInputDevice8_Release(dev);
        }
        IDirectInput8_Release(dia);
    }
    if (state_fn[1] == state_fn[0]) state_fn[1] = NULL;
    if (data_fn[1] == data_fn[0]) data_fn[1] = NULL;

    static const LPVOID state_detours[2] = {(LPVOID)hk_di_state0, (LPVOID)hk_di_state1};
    static const LPVOID data_detours[2] = {(LPVOID)hk_di_data0, (LPVOID)hk_di_data1};
    int hooked = 0;
    for (int i = 0; i < 2; i++) {
        if (state_fn[i] && MH_CreateHook(state_fn[i], state_detours[i], (LPVOID *)&o_di_state[i]) == MH_OK)
            hooked++;
        if (data_fn[i] && MH_CreateHook(data_fn[i], data_detours[i], (LPVOID *)&o_di_data[i]) == MH_OK)
            hooked++;
    }
    HMODULE user32 = GetModuleHandleW(L"user32.dll");
    if (user32) {
        void *async = (void *)GetProcAddress(user32, "GetAsyncKeyState"), *state = (void *)GetProcAddress(user32, "GetKeyState");
        if (async && MH_CreateHook(async, (LPVOID)hk_GetAsyncKeyState, (LPVOID *)&o_GetAsyncKeyState) == MH_OK)
            hooked++;
        if (state && MH_CreateHook(state, (LPVOID)hk_GetKeyState, (LPVOID *)&o_GetKeyState) == MH_OK)
            hooked++;
        void *raw_data = (void *)GetProcAddress(user32, "GetRawInputData");
        void *raw_buffer = (void *)GetProcAddress(user32, "GetRawInputBuffer");
        void *physical = (void *)GetProcAddress(user32, "GetPhysicalCursorPos");
        void *info = (void *)GetProcAddress(user32, "GetCursorInfo");
        if (raw_data && MH_CreateHook(raw_data, (LPVOID)hk_GetRawInputData, (LPVOID *)&o_GetRawInputData) == MH_OK)
            hooked++;
        if (raw_buffer && MH_CreateHook(raw_buffer, (LPVOID)hk_GetRawInputBuffer, (LPVOID *)&o_GetRawInputBuffer) == MH_OK)
            hooked++;
        if (physical && MH_CreateHook(physical, (LPVOID)hk_GetPhysicalCursorPos, (LPVOID *)&o_GetPhysicalCursorPos) == MH_OK)
            hooked++;
        if (info && MH_CreateHook(info, (LPVOID)hk_GetCursorInfo, (LPVOID *)&o_GetCursorInfo) == MH_OK)
            hooked++;
        void *get = (void *)GetProcAddress(user32, "GetCursorPos"), *set = (void *)GetProcAddress(user32, "SetCursorPos");
        if (get && MH_CreateHook(get, (LPVOID)hk_GetCursorPos, (LPVOID *)&o_GetCursorPos) == MH_OK)
            hooked++;
        if (set && MH_CreateHook(set, (LPVOID)hk_SetCursorPos, (LPVOID *)&o_SetCursorPos) == MH_OK)
            hooked++;
    }
    logf_("input hooks: %d installed, control with the %s", hooked, g_cfg_control_mouse ? "mouse" : "keyboard");

}

/* ------------------------------------------------------------------ SCS telemetry */

typedef struct scs_value {
    uint32_t type;
    uint32_t padding;
    union {
        uint8_t b;
        uint32_t u32;
        float f;
        uint8_t raw[40];
    } v;
} scs_value_t; /* 48 bytes, as in scssdk_value.h */

typedef void(__stdcall *scs_event_cb)(uint32_t event, const void *info, void *context);
typedef void(__stdcall *scs_channel_cb)(const char *name, uint32_t index, const scs_value_t *value, void *context);

typedef struct scs_telemetry_init_params {
    const char *game_name;
    const char *game_id;
    uint32_t game_version;
    uint32_t padding;
    void(__stdcall *log)(int32_t type, const char *message);
    int32_t(__stdcall *register_for_event)(uint32_t event, scs_event_cb callback, void *context);
    int32_t(__stdcall *unregister_from_event)(uint32_t event);
    int32_t(__stdcall *register_for_channel)(const char *name, uint32_t index, uint32_t type, uint32_t flags,
                                             scs_channel_cb callback, void *context);
    int32_t(__stdcall *unregister_from_channel)(const char *name, uint32_t index, uint32_t type);
} scs_telemetry_init_params_t; /* 64 bytes: scs_telemetry_init_params_v100_t */

enum { SCS_EVENT_PAUSED = 3, SCS_EVENT_STARTED = 4 };
enum { SCS_TYPE_BOOL = 1, SCS_TYPE_U32 = 3, SCS_TYPE_FLOAT = 5 };
enum { SCS_CHANNEL_EACH_FRAME = 1 };

static void __stdcall on_scs_event(uint32_t event, const void *info, void *context)
{
    (void)info;
    (void)context;
    if (event == SCS_EVENT_PAUSED)
        g_paused = 1;
    else if (event == SCS_EVENT_STARTED)
        g_paused = 0;
}

static void __stdcall on_scs_bool(const char *name, uint32_t index, const scs_value_t *value, void *context)
{
    (void)name;
    (void)index;
    if (value && value->type == SCS_TYPE_BOOL)
        *(volatile int *)context = value->v.b != 0;
}

static void __stdcall on_scs_float(const char *name, uint32_t index, const scs_value_t *value, void *context)
{
    (void)name;
    (void)index;
    if (value && value->type == SCS_TYPE_FLOAT)
        *(volatile float *)context = value->v.f;
}

static void __stdcall on_scs_u32(const char *name, uint32_t index, const scs_value_t *value, void *context)
{
    (void)name;
    (void)index;
    if (value && value->type == SCS_TYPE_U32)
        *(volatile uint32_t *)context = value->v.u32;
}

static void register_telemetry(const scs_telemetry_init_params_t *p)
{
    if (!p || !p->register_for_event || !p->register_for_channel)
        return;
    const uint32_t none = 0xFFFFFFFFu; /* SCS_U32_NIL: not an array channel */
    int ok = p->register_for_event(SCS_EVENT_PAUSED, on_scs_event, NULL) == 0 &&
             p->register_for_event(SCS_EVENT_STARTED, on_scs_event, NULL) == 0 &&
             p->register_for_channel("truck.electric.enabled", none, SCS_TYPE_BOOL, SCS_CHANNEL_EACH_FRAME, on_scs_bool,
                                     (void *)&g_electric) == 0;
    p->register_for_channel("truck.speed", none, SCS_TYPE_FLOAT, SCS_CHANNEL_EACH_FRAME, on_scs_float, (void *)&g_speed_ms);
    p->register_for_channel("truck.navigation.speed.limit", none, SCS_TYPE_FLOAT, SCS_CHANNEL_EACH_FRAME, on_scs_float,
                            (void *)&g_limit_ms);
    p->register_for_channel("truck.navigation.distance", none, SCS_TYPE_FLOAT, SCS_CHANNEL_EACH_FRAME, on_scs_float,
                            (void *)&g_nav_distance);
    p->register_for_channel("truck.navigation.time", none, SCS_TYPE_FLOAT, SCS_CHANNEL_EACH_FRAME, on_scs_float,
                            (void *)&g_nav_time);
    p->register_for_channel("game.time", none, SCS_TYPE_U32, SCS_CHANNEL_EACH_FRAME, on_scs_u32, (void *)&g_game_time);
    g_paused = 1; /* the game starts in its menus */
    g_telemetry_ok = ok;
    logf_("telemetry: %s", ok ? "registered" : "not available, ignition and pause are ignored");
}

/* Starts the companion app in the background so the player never has to. The app closes
 * itself when the game goes away. "asked" is the player pressing the control key, which
 * starts the app even with autostart off. Returns nonzero if the app is being started. */
static int start_companion(int asked)
{
    static ULONGLONG started_ms;
    wchar_t ns[4];
    if ((!asked && !g_cfg_autostart) || GetEnvironmentVariableW(L"CABINPLAY_TEST_NAMESPACE", ns, 4))
        return 0;
    HANDLE running = OpenMutexW(SYNCHRONIZE, FALSE, L"Local\\CabinPlayApp");
    if (running) {
        CloseHandle(running);
        if (!asked)
            logf_("companion app is already running");
        return 0;
    }
    /* The app takes a moment to come up: a second press must not start another one. */
    if (started_ms && GetTickCount64() - started_ms < 10000)
        return 1;
    if (!g_cfg_app_path[0]) {
        /* Installed by hand: the app notes where it is every time it runs. */
        DWORD bytes = sizeof(g_cfg_app_path);
        if (RegGetValueW(HKEY_CURRENT_USER, REGISTRY_KEY, L"AppPath", RRF_RT_REG_SZ, NULL, g_cfg_app_path, &bytes) !=
            ERROR_SUCCESS)
            g_cfg_app_path[0] = 0;
    }
    if (!g_cfg_app_path[0]) {
        logf_("the companion app's location is not known yet: start CabinPlay once by hand");
        return 0;
    }
    wchar_t command[MAX_PATH + 32], folder[MAX_PATH];
    _snwprintf(command, MAX_PATH + 32, L"\"%ls\" --background", g_cfg_app_path);
    command[MAX_PATH + 31] = 0;
    wcsncpy(folder, g_cfg_app_path, MAX_PATH);
    folder[MAX_PATH - 1] = 0;
    wchar_t *slash = wcsrchr(folder, L'\\');
    if (slash)
        *slash = 0;

    STARTUPINFOW si;
    PROCESS_INFORMATION pi;
    memset(&si, 0, sizeof(si));
    si.cb = sizeof(si);
    si.dwFlags = STARTF_USESHOWWINDOW;
    si.wShowWindow = SW_SHOWNOACTIVATE; /* must not take focus from the game */
    if (CreateProcessW(NULL, command, NULL, NULL, FALSE, 0, NULL, slash ? folder : NULL, &si, &pi)) {
        CloseHandle(pi.hThread);
        CloseHandle(pi.hProcess);
        started_ms = GetTickCount64();
        logf_(asked ? "started the companion app again" : "started the companion app");
        return 1;
    }
    logf_("could not start the companion app (error %lu): %ls", GetLastError(), g_cfg_app_path);
    return 0;
}

/* ------------------------------------------------------------------ SCS SDK entry points */

__declspec(dllexport) int32_t scs_telemetry_init(uint32_t version, const scs_telemetry_init_params_t *params)
{
    (void)version;
    if (g_hooked)
        return 0;
    if (!g_locks_ready) {
        InitializeCriticalSection(&g_lock);
        InitializeCriticalSection(&g_input_lock);
        g_locks_ready = 1;
    }
    open_log_and_config();
    logf_("CabinPlay plugin starting (game: %s)", params && params->game_id ? params->game_id : "?");

    const char *status = "[cabinplay] plugin active";
    if (!g_cfg_enabled) {
        status = "[cabinplay] disabled in cabinplay.ini";
    } else {
        if (!g_frame)
            g_frame = (uint8_t *)VirtualAlloc(NULL, CABINPLAY_FRAME_BYTES * 3, MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);
        if (!g_frame) {
            status = "[cabinplay] out of memory, plugin inactive";
        } else {
            g_frame_rgba = g_frame + CABINPLAY_FRAME_BYTES;
            g_black = g_frame_rgba + CABINPLAY_FRAME_BYTES;
            for (size_t i = 3; i < CABINPLAY_FRAME_BYTES; i += 4)
                g_black[i] = 0xFF;
            if (MH_Initialize() == MH_OK && install_d3d_hooks()) {
                install_input_hooks();
                open_state_mapping();
                register_telemetry(params);
                start_companion(0);
                logf_("pause control: %s", g_semantic_registered ? "input API" : "pause key only");
                if (MH_EnableHook(MH_ALL_HOOKS) == MH_OK)
                    g_hooked = 1;
            }
            if (!g_hooked)
                status = "[cabinplay] could not hook Direct3D 11, plugin inactive";
        }
    }
    logf_("%s", status);
    if (params && params->log)
        params->log(0, status);
    return 0; /* SCS_RESULT_ok: never block the game from starting */
}

__declspec(dllexport) void scs_telemetry_shutdown(void)
{
    if (g_hooked) {
        set_control(0);
        unsubclass_game_window();
        MH_DisableHook(MH_ALL_HOOKS);
        MH_Uninitialize();
        g_hooked = 0;
        o_GetCursorPos = NULL;
        o_GetAsyncKeyState = NULL;
        o_GetKeyState = NULL;
        o_GetRawInputData = NULL;
        o_GetRawInputBuffer = NULL;
        o_GetPhysicalCursorPos = NULL;
        o_GetCursorInfo = NULL;
        g_pause_want = -1;
        g_we_paused = 0;
        g_resume_on_exit = 0;
        o_SetCursorPos = NULL;
        memset(o_di_state, 0, sizeof(o_di_state));
        memset(o_di_data, 0, sizeof(o_di_data));
        EnterCriticalSection(&g_lock);
        for (int i = 0; i < MAX_ENTRIES; i++)
            if (g_entries[i].state != STATE_FREE)
                release_entry(&g_entries[i]);
        for (int i = 0; i < FAMILY_COUNT; i++)
            release_video(&g_video[i]);
        release_gfx();
        LeaveCriticalSection(&g_lock);
    }
    if (g_shared) {
        UnmapViewOfFile(g_shared);
        CloseHandle(g_mapping);
        g_shared = NULL;
        g_mapping = NULL;
    }
    if (g_state) {
        g_state->heartbeat_ms = 0; /* tells the app the game is gone */
        UnmapViewOfFile(g_state);
        CloseHandle(g_state_mapping);
        g_state = NULL;
        g_state_mapping = NULL;
    }
    g_telemetry_ok = 0;
    g_last_map_try = 0;
    g_last_sequence = -1;
    g_have_frame = 0;
    g_screen_off_applied = 0;
    logf_("plugin stopped");
    if (g_log) {
        fclose(g_log);
        g_log = NULL;
    }
}

/* The input API. Registers a "semantical" device: its inputs are tied to the game's own
 * controls by name, with nothing for the player to bind. The one input, "pause", is how
 * control mode pauses and resumes the game. */

typedef struct scs_input_device_input {
    const char *name;
    const char *display_name;
    uint32_t value_type;
    uint32_t padding;
} scs_input_device_input_t; /* 24 bytes */

typedef struct scs_input_device {
    const char *name;
    const char *display_name;
    uint32_t type;
    uint32_t input_count;
    const scs_input_device_input_t *inputs;
    void *callback_context;
    void(__stdcall *input_active_callback)(uint8_t active, void *context);
    int32_t(__stdcall *input_event_callback)(scs_input_event_t *event, uint32_t flags, void *context);
} scs_input_device_t; /* 56 bytes */

typedef struct scs_input_init_params {
    const char *game_name;
    const char *game_id;
    uint32_t game_version;
    uint32_t padding;
    void(__stdcall *log)(int32_t type, const char *message);
    int32_t(__stdcall *register_device)(const scs_input_device_t *device);
} scs_input_init_params_t; /* 40 bytes: scs_input_init_params_v100_t */

__declspec(dllexport) int32_t scs_input_init(uint32_t version, const scs_input_init_params_t *params)
{
    (void)version;
    static const scs_input_device_input_t inputs[] = {{"pause", "Pause", SCS_TYPE_BOOL, 0}};
    scs_input_device_t device;
    memset(&device, 0, sizeof(device));
    device.name = "cabinplay";
    device.display_name = "CabinPlay";
    device.type = 2; /* SCS_INPUT_DEVICE_TYPE_semantical */
    device.input_count = 1;
    device.inputs = inputs;
    device.input_active_callback = on_semantic_active;
    device.input_event_callback = on_semantic_event;
    g_semantic_registered = params && params->register_device && params->register_device(&device) == 0;
    g_semantic_active = 0;
    logf_("input API: pause control %s", g_semantic_registered ? "registered" : "not available");
    return 0; /* SCS_RESULT_ok either way: the key press remains as a fallback */
}

__declspec(dllexport) void scs_input_shutdown(void)
{
    g_semantic_registered = 0;
    g_semantic_active = 0;
}

/* Test aid for plugin/test_host.c: switches control mode without the keyboard. */
__declspec(dllexport) void cabinplay_debug_control(int on)
{
    set_control(on);
}

BOOL WINAPI DllMain(HINSTANCE module, DWORD reason, LPVOID reserved)
{
    (void)reserved;
    if (reason == DLL_PROCESS_ATTACH) {
        g_module = module;
        DisableThreadLibraryCalls(module);
    }
    return TRUE;
}
