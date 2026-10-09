/* Shared memory between the companion app and the game plugin.
 *
 *   frame mapping  app -> plugin   the CarPlay picture
 *   state mapping  plugin -> app   game state and input captured in control mode
 *
 * Mirrored in app/FrameWriter.cs and app/GameLink.cs: keep them in sync. */
#ifndef CARPLAY_FRAME_PROTOCOL_H
#define CARPLAY_FRAME_PROTOCOL_H

#include <stdint.h>

/* ------------------------------------------------------------------ frame mapping */

#define CARPLAY_MAPPING_NAME L"Local\\ETS2CarPlayFrame"
#define CARPLAY_MAGIC 0x594C5043u /* "CPLY" */
#define CARPLAY_VERSION 1u
#define CARPLAY_WIDTH 1024u
#define CARPLAY_HEIGHT 512u
#define CARPLAY_HEADER_SIZE 64u
#define CARPLAY_FRAME_BYTES (CARPLAY_WIDTH * CARPLAY_HEIGHT * 4u)
#define CARPLAY_MAPPING_SIZE (CARPLAY_HEADER_SIZE + CARPLAY_FRAME_BYTES)

/* app_flags */
#define CARPLAY_APP_NAV_VISIBLE 0x1u /* replace key-coloured pixels with the game's map */

/* Colour the screen texture in the .scs is filled with (see tools/build_mod.py). */
#define CARPLAY_MARKER_B 0x06
#define CARPLAY_MARKER_G 0x02
#define CARPLAY_MARKER_R 0x04

/* Pixels of exactly this colour are see-through to the map while navigation is visible
 * (see --map-key in app/ui/style.css). */
#define CARPLAY_KEY_R 0x02
#define CARPLAY_KEY_G 0x00
#define CARPLAY_KEY_B 0x02

/* Width of the dock; the map lies under everything to the right of it. */
#define CARPLAY_DOCK_WIDTH 88u

#pragma pack(push, 1)
typedef struct carplay_frame_header {
    uint32_t magic;
    uint32_t version;
    uint32_t width;
    uint32_t height;
    volatile int32_t sequence; /* odd while a frame is being written, bumped to even after */
    uint32_t app_flags;        /* CARPLAY_APP_* */
    uint64_t heartbeat_ms;     /* writer's GetTickCount64, refreshed even with no new frame */
    uint8_t reserved1[32];
} carplay_frame_header_t;      /* followed by width * height BGRA pixels, top row first */
#pragma pack(pop)

/* ------------------------------------------------------------------ state mapping */

#define CARPLAY_STATE_MAPPING_NAME L"Local\\ETS2CarPlayState"
#define CARPLAY_STATE_MAGIC 0x54535043u /* "CPST" */
#define CARPLAY_STATE_VERSION 1u
#define CARPLAY_EVENT_CAPACITY 256u

/* flags */
#define CARPLAY_STATE_TELEMETRY 0x01u /* paused / electric / navigation values are valid */
#define CARPLAY_STATE_PAUSED 0x02u
#define CARPLAY_STATE_ELECTRIC 0x04u
#define CARPLAY_STATE_MOUNTED 0x08u   /* a truck with the screen is loaded */
#define CARPLAY_STATE_CONTROL 0x10u   /* in-game control mode is active */
#define CARPLAY_STATE_NAV_READY 0x20u /* the game's map texture was found */

/* event types; x and y are CarPlay screen pixels unless noted */
#define CARPLAY_EVENT_MOVE 1u
#define CARPLAY_EVENT_BUTTON 2u /* data: button (0 left, 1 right, 2 middle) | down << 8 */
#define CARPLAY_EVENT_WHEEL 3u  /* data: wheel delta, 120 per notch */
#define CARPLAY_EVENT_KEY 4u    /* x: virtual key, y: down | modifiers << 8, data: UTF-16 char or 0 */

#define CARPLAY_MOD_SHIFT 0x1u
#define CARPLAY_MOD_CTRL 0x2u
#define CARPLAY_MOD_ALT 0x4u

#pragma pack(push, 1)
typedef struct carplay_event {
    uint32_t type;
    int32_t x;
    int32_t y;
    int32_t data;
} carplay_event_t;

typedef struct carplay_state {
    uint32_t magic;
    uint32_t version;
    uint64_t heartbeat_ms;         /* plugin's GetTickCount64, refreshed every rendered frame */
    uint32_t flags;                /* CARPLAY_STATE_* */
    volatile uint32_t event_write; /* number of events written so far; slot = n % capacity */
    float speed_kmh;
    float speed_limit_kmh;         /* 0 when unknown */
    float nav_distance_m;          /* 0 when no route is set */
    float nav_time_s;              /* remaining, in game time */
    uint32_t game_time_min;        /* in-game clock, minutes since the game's epoch */
    uint32_t reserved[5];
    carplay_event_t events[CARPLAY_EVENT_CAPACITY];
} carplay_state_t;
#pragma pack(pop)

#define CARPLAY_STATE_MAPPING_SIZE ((uint32_t)sizeof(carplay_state_t))

#endif
