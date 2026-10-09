/* Stand-in for the game: loads the plugin, plays the game's part (Direct3D 11 textures,
 * telemetry callbacks, DirectInput reads) and the companion app's part (frames in shared
 * memory), and checks what the plugin does with them.
 *
 * Usage: test_host.exe <path to cabinplay.dll>
 */
#define COBJMACROS
#define WIN32_LEAN_AND_MEAN
#define DIRECTINPUT_VERSION 0x0800
#include <windows.h>
#include <d3d11.h>
#include <dinput.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

#include "frame_protocol.h"

static ID3D11Device *dev;
static ID3D11DeviceContext *ctx;
static IDXGISwapChain *sc;
static int failures;

static void check(int cond, const char *what)
{
    printf("  %-62s %s\n", what, cond ? "ok" : "FAIL");
    if (!cond)
        failures++;
}

static int near_(int a, int b, int tolerance)
{
    return abs(a - b) <= tolerance;
}

static void read_pixel(ID3D11Texture2D *tex, UINT mip, UINT x, UINT y, uint8_t out[4])
{
    D3D11_TEXTURE2D_DESC d;
    ID3D11Texture2D_GetDesc(tex, &d);
    d.Width = d.Width >> mip ? d.Width >> mip : 1;
    d.Height = d.Height >> mip ? d.Height >> mip : 1;
    d.MipLevels = 1;
    d.Usage = D3D11_USAGE_STAGING;
    d.BindFlags = 0;
    d.MiscFlags = 0;
    d.CPUAccessFlags = D3D11_CPU_ACCESS_READ;
    ID3D11Texture2D *st = NULL;
    ID3D11Device_CreateTexture2D(dev, &d, NULL, &st);
    ID3D11DeviceContext_CopySubresourceRegion(ctx, (ID3D11Resource *)st, 0, 0, 0, 0, (ID3D11Resource *)tex, mip, NULL);
    D3D11_MAPPED_SUBRESOURCE m;
    ID3D11DeviceContext_Map(ctx, (ID3D11Resource *)st, 0, D3D11_MAP_READ, 0, &m);
    memcpy(out, (uint8_t *)m.pData + y * m.RowPitch + x * 4, 4);
    ID3D11DeviceContext_Unmap(ctx, (ID3D11Resource *)st, 0);
    ID3D11Texture2D_Release(st);
}

static ID3D11Texture2D *make_texture(UINT w, UINT h, DXGI_FORMAT fmt, D3D11_USAGE usage, const uint8_t fill[4],
                                     int with_init)
{
    D3D11_TEXTURE2D_DESC d;
    memset(&d, 0, sizeof(d));
    d.Width = w;
    d.Height = h;
    d.ArraySize = 1;
    d.Format = fmt;
    d.SampleDesc.Count = 1;
    d.Usage = usage;
    d.BindFlags = D3D11_BIND_SHADER_RESOURCE;
    UINT mips = 0;
    for (UINT a = w, b = h;; a = a > 1 ? a / 2 : 1, b = b > 1 ? b / 2 : 1) {
        mips++;
        if (a == 1 && b == 1)
            break;
    }
    d.MipLevels = mips;

    uint8_t *px = (uint8_t *)malloc((size_t)w * h * 4);
    for (size_t i = 0; i < (size_t)w * h; i++)
        memcpy(px + i * 4, fill, 4);
    D3D11_SUBRESOURCE_DATA init[16];
    for (UINT m = 0; m < mips; m++) {
        init[m].pSysMem = px;
        init[m].SysMemPitch = (w >> m ? w >> m : 1) * 4;
        init[m].SysMemSlicePitch = 0;
    }
    ID3D11Texture2D *tex = NULL;
    HRESULT hr = ID3D11Device_CreateTexture2D(dev, &d, with_init ? init : NULL, &tex);
    if (FAILED(hr))
        printf("  CreateTexture2D failed 0x%08lx\n", (unsigned long)hr);
    if (tex && !with_init)
        for (UINT m = 0; m < mips; m++)
            ID3D11DeviceContext_UpdateSubresource(ctx, (ID3D11Resource *)tex, m, NULL, px, init[m].SysMemPitch, 0);
    free(px);
    return tex;
}

/* The game's navigation render target, as the UI in the .scs lays it out: map on the
 * left, marker strip on the right. Stored bottom-up when `flipped`. The map's green
 * channel encodes the row so the orientation can be checked. */
static ID3D11Texture2D *make_nav_target(int flipped)
{
    D3D11_TEXTURE2D_DESC d;
    memset(&d, 0, sizeof(d));
    d.Width = CABINPLAY_WIDTH;
    d.Height = CABINPLAY_HEIGHT;
    d.MipLevels = 1;
    d.ArraySize = 1;
    d.Format = DXGI_FORMAT_R8G8B8A8_UNORM;
    d.SampleDesc.Count = 1;
    d.Usage = D3D11_USAGE_DEFAULT;
    d.BindFlags = D3D11_BIND_SHADER_RESOURCE | D3D11_BIND_RENDER_TARGET;
    ID3D11Texture2D *tex = NULL;
    ID3D11Device_CreateTexture2D(dev, &d, NULL, &tex);
    uint8_t *px = (uint8_t *)malloc(CABINPLAY_FRAME_BYTES);
    for (UINT row = 0; row < CABINPLAY_HEIGHT; row++) {
        UINT y = flipped ? CABINPLAY_HEIGHT - 1 - row : row; /* y = position on screen, top = 0 */
        for (UINT x = 0; x < CABINPLAY_WIDTH; x++) {
            uint8_t *p = px + (row * CABINPLAY_WIDTH + x) * 4;
            if (x >= CABINPLAY_WIDTH - CABINPLAY_DOCK_WIDTH) {
                int upper = y < CABINPLAY_HEIGHT / 2;
                p[0] = upper ? 0x10 : 0xC0;
                p[1] = upper ? 0xC0 : 0x10;
                p[2] = upper ? 0x10 : 0xC0;
            } else {
                p[0] = 200;                /* R */
                p[1] = (uint8_t)(y / 4);   /* G */
                p[2] = 40;                 /* B */
            }
            p[3] = 0xFF;
        }
    }
    ID3D11DeviceContext_UpdateSubresource(ctx, (ID3D11Resource *)tex, 0, NULL, px, CABINPLAY_WIDTH * 4, 0);
    free(px);
    return tex;
}

/* ---- the game's telemetry API */

typedef void(__stdcall *event_cb)(uint32_t, const void *, void *);
typedef struct { uint32_t type, pad; union { uint8_t b; uint32_t u32; float f; uint8_t raw[40]; } v; } value_t;
typedef void(__stdcall *channel_cb)(const char *, uint32_t, const value_t *, void *);

static event_cb g_event_cb[8];
static struct { char name[64]; channel_cb cb; void *context; } g_channels[16];
static int g_channel_count;

static int32_t __stdcall register_event(uint32_t event, event_cb cb, void *context)
{
    (void)context;
    g_event_cb[event & 7] = cb;
    return 0;
}
static int32_t __stdcall unregister_event(uint32_t event) { (void)event; return 0; }
static int32_t __stdcall register_channel(const char *name, uint32_t index, uint32_t type, uint32_t flags, channel_cb cb,
                                          void *context)
{
    (void)index; (void)type; (void)flags;
    strncpy(g_channels[g_channel_count].name, name, 63);
    g_channels[g_channel_count].cb = cb;
    g_channels[g_channel_count].context = context;
    g_channel_count++;
    return 0;
}
static int32_t __stdcall unregister_channel(const char *name, uint32_t index, uint32_t type)
{
    (void)name; (void)index; (void)type;
    return 0;
}
static void __stdcall game_log(int32_t type, const char *message) { (void)type; printf("game log: %s\n", message); }

static void send_bool(const char *name, int value)
{
    value_t v;
    memset(&v, 0, sizeof(v));
    v.type = 1;
    v.v.b = (uint8_t)value;
    for (int i = 0; i < g_channel_count; i++)
        if (!strcmp(g_channels[i].name, name))
            g_channels[i].cb(name, 0xFFFFFFFFu, &v, g_channels[i].context);
}
static void send_float(const char *name, float value)
{
    value_t v;
    memset(&v, 0, sizeof(v));
    v.type = 5;
    v.v.f = value;
    for (int i = 0; i < g_channel_count; i++)
        if (!strcmp(g_channels[i].name, name))
            g_channels[i].cb(name, 0xFFFFFFFFu, &v, g_channels[i].context);
}

static void present(cabinplay_frame_header_t *hdr, int frames)
{
    for (int i = 0; i < frames; i++) {
        hdr->heartbeat_ms = GetTickCount64();
        IDXGISwapChain_Present(sc, 0, 0);
    }
}

int main(int argc, char **argv)
{
    if (argc < 2) {
        printf("usage: test_host <plugin dll>\n");
        return 2;
    }
    /* Private mapping names: a game or companion app may be running on this machine. */
    SetEnvironmentVariableW(L"CABINPLAY_TEST_NAMESPACE", L"selftest");
    HMODULE plugin = LoadLibraryA(argv[1]);
    if (!plugin) {
        printf("cannot load %s (%lu)\n", argv[1], GetLastError());
        return 2;
    }
    typedef int32_t (*init_fn)(uint32_t, const void *);
    typedef void (*shutdown_fn)(void);
    typedef void (*control_fn)(int);
    init_fn init = (init_fn)(void *)GetProcAddress(plugin, "scs_telemetry_init");
    shutdown_fn shutdown = (shutdown_fn)(void *)GetProcAddress(plugin, "scs_telemetry_shutdown");
    control_fn control = (control_fn)(void *)GetProcAddress(plugin, "cabinplay_debug_control");
    if (!init || !shutdown || !control) {
        printf("plugin exports missing\n");
        return 2;
    }
    struct {
        const char *game_name, *game_id;
        uint32_t game_version, padding;
        void *log, *reg_event, *unreg_event, *reg_channel, *unreg_channel;
    } params = {"Euro Truck Simulator 2", "eut2", 0x00010012, 0, (void *)game_log, (void *)register_event,
                (void *)unregister_event, (void *)register_channel, (void *)unregister_channel};
    int32_t result = init(0x00010001, &params);
    printf("scs_telemetry_init -> %d (%d channels registered)\n", result, g_channel_count);

    /* companion side: publish one frame whose pixels encode their own coordinates */
    HANDLE map = CreateFileMappingW(INVALID_HANDLE_VALUE, NULL, PAGE_READWRITE, 0, CABINPLAY_MAPPING_SIZE,
                                    CABINPLAY_MAPPING_NAME L".selftest");
    cabinplay_frame_header_t *hdr = (cabinplay_frame_header_t *)MapViewOfFile(map, FILE_MAP_ALL_ACCESS, 0, 0, 0);
    uint8_t *pixels = (uint8_t *)hdr + CABINPLAY_HEADER_SIZE;
    for (UINT y = 0; y < CABINPLAY_HEIGHT; y++)
        for (UINT x = 0; x < CABINPLAY_WIDTH; x++) {
            uint8_t *p = pixels + (y * CABINPLAY_WIDTH + x) * 4;
            p[0] = (uint8_t)(x / 4);  /* B */
            p[1] = (uint8_t)(y / 2);  /* G */
            p[2] = 0x90;              /* R */
            p[3] = 0xFF;
        }
    hdr->magic = CABINPLAY_MAGIC;
    hdr->version = CABINPLAY_VERSION;
    hdr->width = CABINPLAY_WIDTH;
    hdr->height = CABINPLAY_HEIGHT;
    hdr->app_flags = 0;
    hdr->heartbeat_ms = GetTickCount64();
    hdr->sequence = 2;

    HANDLE state_map = OpenFileMappingW(FILE_MAP_READ, FALSE, CABINPLAY_STATE_MAPPING_NAME L".selftest");
    const cabinplay_state_t *state = state_map ? (const cabinplay_state_t *)MapViewOfFile(state_map, FILE_MAP_READ, 0, 0, 0)
                                             : NULL;

    WNDCLASSEXW wc;
    memset(&wc, 0, sizeof(wc));
    wc.cbSize = sizeof(wc);
    wc.lpfnWndProc = DefWindowProcW;
    wc.hInstance = GetModuleHandleW(NULL);
    wc.lpszClassName = L"CabinPlayTestHost";
    RegisterClassExW(&wc);
    HWND wnd = CreateWindowExW(0, wc.lpszClassName, L"", WS_OVERLAPPED, 0, 0, 1280, 720, NULL, NULL, wc.hInstance, NULL);
    DXGI_SWAP_CHAIN_DESC scd;
    memset(&scd, 0, sizeof(scd));
    scd.BufferCount = 1;
    scd.BufferDesc.Width = 1280;
    scd.BufferDesc.Height = 720;
    scd.BufferDesc.Format = DXGI_FORMAT_R8G8B8A8_UNORM;
    scd.BufferUsage = DXGI_USAGE_RENDER_TARGET_OUTPUT;
    scd.OutputWindow = wnd;
    scd.SampleDesc.Count = 1;
    scd.Windowed = TRUE;
    HRESULT hr = D3D11CreateDeviceAndSwapChain(NULL, D3D_DRIVER_TYPE_HARDWARE, NULL, 0, NULL, 0, D3D11_SDK_VERSION,
                                               &scd, &sc, &dev, NULL, &ctx);
    if (FAILED(hr)) {
        printf("no D3D11 device (0x%08lx)\n", (unsigned long)hr);
        return 2;
    }

    const uint8_t marker_bgra[4] = {CABINPLAY_MARKER_B, CABINPLAY_MARKER_G, CABINPLAY_MARKER_R, 0xFF};
    const uint8_t marker_rgba[4] = {CABINPLAY_MARKER_R, CABINPLAY_MARKER_G, CABINPLAY_MARKER_B, 0xFF};
    const uint8_t other[4] = {0x40, 0x41, 0x42, 0xFF};

    ID3D11Texture2D *a = make_texture(1024, 512, DXGI_FORMAT_B8G8R8A8_UNORM_SRGB, D3D11_USAGE_IMMUTABLE, marker_bgra, 1);
    ID3D11Texture2D *b = make_texture(512, 256, DXGI_FORMAT_B8G8R8A8_UNORM, D3D11_USAGE_DEFAULT, marker_bgra, 0);
    ID3D11Texture2D *c = make_texture(1024, 512, DXGI_FORMAT_R8G8B8A8_UNORM_SRGB, D3D11_USAGE_DEFAULT, marker_rgba, 1);
    ID3D11Texture2D *d = make_texture(1024, 512, DXGI_FORMAT_B8G8R8A8_UNORM_SRGB, D3D11_USAGE_DEFAULT, other, 1);

    /* The game is "driving" with the ignition on. */
    g_event_cb[4](4, NULL, NULL);
    send_bool("truck.electric.enabled", 1);
    present(hdr, 40);

    uint8_t px[4];
    printf("A: immutable BGRA sRGB 1024x512 with initial data\n");
    read_pixel(a, 0, 400, 200, px);
    check(px[0] == 100 && px[1] == 100 && px[2] == 0x90, "mip 0 holds the published frame");
    read_pixel(a, 3, 60, 30, px);
    check(px[2] > 0x80 && !(px[0] == CABINPLAY_MARKER_B && px[1] == CABINPLAY_MARKER_G), "mip 3 was regenerated");

    printf("B: BGRA 512x256 filled after creation (reduced texture quality)\n");
    read_pixel(b, 0, 200, 100, px);
    check(near_(px[0], 100, 2) && near_(px[1], 100, 2) && px[2] > 0x80, "mip 0 holds the half-size frame");

    printf("C: RGBA 1024x512\n");
    read_pixel(c, 0, 400, 200, px);
    check(px[2] == 100 && px[1] == 100 && px[0] == 0x90, "mip 0 holds the frame with channels swapped");

    printf("D: unrelated texture of the same size\n");
    read_pixel(d, 0, 400, 200, px);
    check(memcmp(px, other, 4) == 0, "left untouched");

    printf("state shared with the app\n");
    send_float("truck.speed", 25.0f);
    send_float("truck.navigation.distance", 12345.0f);
    present(hdr, 1);
    check(state && state->magic == CABINPLAY_STATE_MAGIC, "state mapping exists");
    check(state && (state->flags & CABINPLAY_STATE_TELEMETRY) && (state->flags & CABINPLAY_STATE_ELECTRIC) &&
              !(state->flags & CABINPLAY_STATE_PAUSED) && (state->flags & CABINPLAY_STATE_MOUNTED),
          "flags: telemetry, ignition on, running, screen mounted");
    check(state && near_((int)state->speed_kmh, 90, 1) && near_((int)state->nav_distance_m, 12345, 1),
          "speed and route distance passed on");

    printf("ignition\n");
    send_bool("truck.electric.enabled", 0);
    present(hdr, 2);
    read_pixel(a, 0, 400, 200, px);
    check(px[0] == 0 && px[1] == 0 && px[2] == 0, "screen is black with the ignition off");
    check(state && !(state->flags & CABINPLAY_STATE_ELECTRIC), "app is told the ignition is off");
    send_bool("truck.electric.enabled", 1);
    present(hdr, 2);
    read_pixel(a, 0, 400, 200, px);
    check(px[0] == 100 && px[1] == 100 && px[2] == 0x90, "picture returns with the ignition on");

    printf("pause\n");
    g_event_cb[3](3, NULL, NULL);
    present(hdr, 1);
    check(state && (state->flags & CABINPLAY_STATE_PAUSED), "app is told the game is paused");
    g_event_cb[4](4, NULL, NULL);
    present(hdr, 1);
    check(state && !(state->flags & CABINPLAY_STATE_PAUSED), "and that it resumed");

    for (int flipped = 0; flipped < 2; flipped++) {
        printf(flipped ? "navigation map, render target stored bottom-up\n" : "navigation map\n");
        /* frame: key colour right of the dock, except a "card" block that must stay on top */
        hdr->sequence |= 1;
        for (UINT y = 0; y < CABINPLAY_HEIGHT; y++)
            for (UINT x = 0; x < CABINPLAY_WIDTH; x++) {
                uint8_t *p = pixels + (y * CABINPLAY_WIDTH + x) * 4;
                int card = x >= 120 && x < 320 && y >= 300 && y < 480;
                if (x >= CABINPLAY_DOCK_WIDTH && !card) {
                    p[0] = CABINPLAY_KEY_B;
                    p[1] = CABINPLAY_KEY_G;
                    p[2] = CABINPLAY_KEY_R;
                } else {
                    p[0] = 30;
                    p[1] = 60;
                    p[2] = 90;
                }
            }
        hdr->sequence++;
        hdr->app_flags = CABINPLAY_APP_NAV_VISIBLE;
        ID3D11Texture2D *nav = make_nav_target(flipped);
        present(hdr, 40);
        check(state && (state->flags & CABINPLAY_STATE_NAV_READY), "map texture recognised");
        read_pixel(a, 0, 600, 100, px); /* BGRA target; map is R=200, G=row/4, B=40 */
        check(near_(px[2], 200, 2) && near_(px[1], 25, 2) && near_(px[0], 40, 2), "map shows through the key colour");
        read_pixel(a, 0, 600, 400, px);
        check(near_(px[1], 100, 2), "map is the right way up");
        read_pixel(a, 0, 200, 400, px);
        check(near_(px[0], 30, 1) && near_(px[1], 60, 1) && near_(px[2], 90, 1), "card stays on top of the map");
        read_pixel(a, 0, 40, 100, px);
        check(near_(px[0], 30, 1) && near_(px[1], 60, 1) && near_(px[2], 90, 1), "dock is untouched");
        read_pixel(c, 0, 600, 100, px); /* RGBA target */
        check(near_(px[0], 200, 2) && near_(px[1], 25, 2) && near_(px[2], 40, 2), "same on the RGBA texture");
        hdr->app_flags = 0;
        present(hdr, 1);
        read_pixel(a, 0, 600, 100, px);
        check(px[0] == CABINPLAY_KEY_B && px[1] == CABINPLAY_KEY_G && px[2] == CABINPLAY_KEY_R, "map hidden when the app says so");
        ID3D11Texture2D_Release(nav);
    }

    printf("control mode\n");
    hdr->sequence |= 1;
    for (size_t i = 0; i < CABINPLAY_FRAME_BYTES; i += 4) {
        pixels[i] = 0x11;
        pixels[i + 1] = 0x22;
        pixels[i + 2] = 0x33;
    }
    hdr->sequence++;
    present(hdr, 1);
    read_pixel(a, 0, 5, 5, px);
    check(px[0] == 0x11 && px[1] == 0x22 && px[2] == 0x33, "new frame replaces the old one");

    IDirectInput8W *di = NULL;
    IDirectInputDevice8W *kb = NULL, *mouse = NULL;
    DirectInput8Create(GetModuleHandleW(NULL), DIRECTINPUT_VERSION, &IID_IDirectInput8W, (void **)&di, NULL);
    if (di) {
        IDirectInput8_CreateDevice(di, &GUID_SysKeyboard, &kb, NULL);
        IDirectInput8_CreateDevice(di, &GUID_SysMouse, &mouse, NULL);
    }
    if (kb && mouse) {
        IDirectInputDevice8_SetDataFormat(kb, &c_dfDIKeyboard);
        IDirectInputDevice8_SetCooperativeLevel(kb, wnd, DISCL_BACKGROUND | DISCL_NONEXCLUSIVE);
        IDirectInputDevice8_Acquire(kb);
        IDirectInputDevice8_SetDataFormat(mouse, &c_dfDIMouse);
        IDirectInputDevice8_SetCooperativeLevel(mouse, wnd, DISCL_BACKGROUND | DISCL_NONEXCLUSIVE);
        IDirectInputDevice8_Acquire(mouse);
        BYTE keys[256];
        DIMOUSESTATE ms;
        check(SUCCEEDED(IDirectInputDevice8_GetDeviceState(kb, sizeof(keys), keys)), "keyboard read passes through");
        control(1);
        present(hdr, 2);
        check(state && (state->flags & CABINPLAY_STATE_CONTROL), "control mode switches on");
        memset(keys, 0xEE, sizeof(keys));
        memset(&ms, 0xEE, sizeof(ms));
        HRESULT hk = IDirectInputDevice8_GetDeviceState(kb, sizeof(keys), keys);
        HRESULT hm = IDirectInputDevice8_GetDeviceState(mouse, sizeof(ms), &ms);
        int blank = SUCCEEDED(hk) && SUCCEEDED(hm) && ms.lX == 0 && ms.lY == 0 && ms.rgbButtons[0] == 0;
        for (int i = 0; i < 256; i++)
            if (i != DIK_PAUSE) /* that one the plugin presses itself, to pause the game */
                blank &= keys[i] == 0;
        check(blank, "game sees no keys or mouse movement meanwhile");

        /* overlay: the screen, enlarged, in the middle of the back buffer */
        ID3D11Texture2D *back = NULL;
        IDXGISwapChain_GetBuffer(sc, 0, &IID_ID3D11Texture2D, (void **)&back);
        const FLOAT clear[4] = {0, 1, 0, 1};
        ID3D11RenderTargetView *rtv = NULL;
        ID3D11Device_CreateRenderTargetView(dev, (ID3D11Resource *)back, NULL, &rtv);
        ID3D11DeviceContext_ClearRenderTargetView(ctx, rtv, clear);
        /* Present swaps buffers away, so draw the overlay by presenting with the test flag off
         * and read the buffer the plugin drew into right before the swap: use a copy. */
        ID3D11DeviceContext_OMSetRenderTargets(ctx, 1, &rtv, NULL);
        present(hdr, 1);
        read_pixel(back, 0, 640 + 200, 360 + 60, px); /* RGBA back buffer, off the cursor */
        check(near_(px[0], 0x33, 2) && near_(px[1], 0x22, 2) && near_(px[2], 0x11, 2), "screen is drawn over the game");
        read_pixel(back, 0, 20, 20, px);
        check(px[0] == 0 && px[1] == 255 && px[2] == 0, "rest of the game picture is untouched");
        ID3D11RenderTargetView *bound = NULL;
        ID3D11DeviceContext_OMGetRenderTargets(ctx, 1, &bound, NULL);
        check(bound == rtv, "game's render target is bound again afterwards");
        if (bound)
            ID3D11RenderTargetView_Release(bound);
        ID3D11RenderTargetView_Release(rtv);
        ID3D11Texture2D_Release(back);

        /* typing: key and character messages to the game window go to the app instead */
        uint32_t before = state->event_write;
        SendMessageW(wnd, WM_KEYDOWN, 'A', 0);
        SendMessageW(wnd, WM_CHAR, L'a', 0);
        SendMessageW(wnd, WM_KEYUP, 'A', 0);
        SendMessageW(wnd, WM_KEYDOWN, VK_LEFT, 0);
        int typed = 0, arrow = 0;
        for (uint32_t i = before; i < state->event_write; i++) {
            const cabinplay_event_t *e = &state->events[i % CABINPLAY_EVENT_CAPACITY];
            if (e->type == CABINPLAY_EVENT_KEY && e->x == 'A' && (e->y & 1) && e->data == 'a') typed++;
            if (e->type == CABINPLAY_EVENT_KEY && e->x == VK_LEFT && (e->y & 1) && e->data == 0) arrow++;
        }
        check(typed == 1 && arrow == 1, "typed keys reach the app, each once");

        /* pausing: the plugin presses the game's pause key (here through DirectInput, since
         * this window is not in front) and control mode survives the pause it asked for */
        int pause_pressed = 0;
        for (int i = 0; i < 120 && !pause_pressed; i++) {
            present(hdr, 1);
            if (SUCCEEDED(IDirectInputDevice8_GetDeviceState(kb, sizeof(keys), keys)) && (keys[DIK_PAUSE] & 0x80))
                pause_pressed = 1;
        }
        check(pause_pressed, "the game's pause key is pressed when control mode opens");
        g_event_cb[3](3, NULL, NULL); /* the game reports it is paused */
        for (int i = 0; i < 12; i++) {
            present(hdr, 1);
            IDirectInputDevice8_GetDeviceState(kb, sizeof(keys), keys);
        }
        check(state && (state->flags & CABINPLAY_STATE_CONTROL) && (state->flags & CABINPLAY_STATE_PAUSED),
              "control mode stays open while the game is paused for it");

        control(0);
        pause_pressed = 0;
        for (int i = 0; i < 120 && !pause_pressed; i++) {
            present(hdr, 1);
            if (SUCCEEDED(IDirectInputDevice8_GetDeviceState(kb, sizeof(keys), keys)) && (keys[DIK_PAUSE] & 0x80))
                pause_pressed = 1;
        }
        check(pause_pressed, "the pause key is pressed again when control mode closes");
        check(state && (state->flags & CABINPLAY_STATE_CONTROL), "the app still sees control mode until the game resumes");
        g_event_cb[4](4, NULL, NULL); /* the game reports it is running again */
        for (int i = 0; i < 12; i++) {
            present(hdr, 1);
            IDirectInputDevice8_GetDeviceState(kb, sizeof(keys), keys);
        }
        check(state && !(state->flags & CABINPLAY_STATE_CONTROL) && !(state->flags & CABINPLAY_STATE_PAUSED),
              "control mode is fully closed afterwards");

        /* a pause that did not come from control mode (a menu) closes it */
        control(1);
        g_event_cb[4](4, NULL, NULL);
        for (int i = 0; i < 200; i++) {            /* let the pause attempt time out unanswered */
            present(hdr, 1);
            IDirectInputDevice8_GetDeviceState(kb, sizeof(keys), keys);
        }
        check(state && (state->flags & CABINPLAY_STATE_CONTROL), "control mode works even if the game cannot be paused");
        g_event_cb[3](3, NULL, NULL);
        present(hdr, 2);
        check(state && !(state->flags & CABINPLAY_STATE_CONTROL), "a pause from elsewhere ends control mode");
        g_event_cb[4](4, NULL, NULL);
        present(hdr, 2);        IDirectInputDevice8_Release(kb);
        IDirectInputDevice8_Release(mouse);
    } else {
        check(0, "DirectInput devices available for the test");
    }
    if (di)
        IDirectInput8_Release(di);

    printf("screen blanks when the companion stops\n");
    hdr->heartbeat_ms = GetTickCount64() - 10000;
    IDXGISwapChain_Present(sc, 0, 0);
    read_pixel(a, 0, 5, 5, px);
    check(px[0] == 0 && px[1] == 0 && px[2] == 0, "black after the heartbeat went stale");

    ID3D11Texture2D_Release(a);
    ID3D11Texture2D_Release(b);
    ID3D11Texture2D_Release(c);
    ID3D11Texture2D_Release(d);
    shutdown();
    IDXGISwapChain_Release(sc);
    ID3D11DeviceContext_Release(ctx);
    ID3D11Device_Release(dev);
    printf(failures ? "\n%d check(s) FAILED\n" : "\nall checks passed\n", failures);
    return failures ? 1 : 0;
}
