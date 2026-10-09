/* Shared memory between the companion app and the game plugin.
 *
 *   frame mapping  app -> plugin   the CabinPlay picture
 *   state mapping  plugin -> app   game state and input captured in control mode
 *
 * Mirrored in app/FrameWriter.cs and app/GameLink.cs: keep them in sync. */
#ifndef CABINPLAY_FRAME_PROTOCOL_H
#define CABINPLAY_FRAME_PROTOCOL_H

#include <stdint.h>

/* ------------------------------------------------------------------ frame mapping */

#define CABINPLAY_MAPPING_NAME L"Local\\CabinPlayFrame"
#define CABINPLAY_MAGIC 0x594C5043u /* "CPLY" */
#define CABINPLAY_VERSION 1u
#define CABINPLAY_WIDTH 1024u
#define CABINPLAY_HEIGHT 512u
#define CABINPLAY_HEADER_SIZE 64u
#define CABINPLAY_FRAME_BYTES (CABINPLAY_WIDTH * CABINPLAY_HEIGHT * 4u)
#define CABINPLAY_MAPPING_SIZE (CABINPLAY_HEADER_SIZE + CABINPLAY_FRAME_BYTES)

/* app_flags */
#define CABINPLAY_APP_NAV_VISIBLE 0x1u /* replace key-coloured pixels with the game's map */

/* Colour the screen texture in the .scs is filled with (see tools/build_mod.py). */
#define CABINPLAY_MARKER_B 0x06
#define CABINPLAY_MARKER_G 0x02
#define CABINPLAY_MARKER_R 0x04

/* Pixels of exactly this colour are see-through to the map while navigation is visible
 * (see --map-key in app/ui/style.css). */
#define CABINPLAY_KEY_R 0x02
#define CABINPLAY_KEY_G 0x00
#define CABINPLAY_KEY_B 0x02

/* Width of the dock; the map lies under everything to the right of it. */
#define CABINPLAY_DOCK_WIDTH 88u

#pragma pack(push, 1)
typedef struct cabinplay_frame_header {
    uint32_t magic;
    uint32_t version;
    uint32_t width;
    uint32_t height;
    volatile int32_t sequence; /* odd while a frame is being written, bumped to even after */
    uint32_t app_flags;        /* CABINPLAY_APP_* */
    uint64_t heartbeat_ms;     /* writer's GetTickCount64, refreshed even with no new frame */
    uint8_t reserved1[32];
} cabinplay_frame_header_t;      /* followed by width * height BGRA pixels, top row first */
#pragma pack(pop)

/* ------------------------------------------------------------------ state mapping */

#define CABINPLAY_STATE_MAPPING_NAME L"Local\\CabinPlayState"
#define CABINPLAY_STATE_MAGIC 0x54535043u /* "CPST" */
#define CABINPLAY_STATE_VERSION 1u
#define CABINPLAY_EVENT_CAPACITY 256u

/* flags */
#define CABINPLAY_STATE_TELEMETRY 0x01u /* paused / electric / navigation values are valid */
#define CABINPLAY_STATE_PAUSED 0x02u
#define CABINPLAY_STATE_ELECTRIC 0x04u
#define CABINPLAY_STATE_MOUNTED 0x08u   /* a truck with the screen is loaded */
#define CABINPLAY_STATE_CONTROL 0x10u   /* in-game control mode is active */
#define CABINPLAY_STATE_NAV_READY 0x20u /* the game's map texture was found */

/* event types; x and y are CabinPlay screen pixels unless noted */
#define CABINPLAY_EVENT_MOVE 1u
#define CABINPLAY_EVENT_BUTTON 2u /* data: button (0 left, 1 right, 2 middle) | down << 8 */
#define CABINPLAY_EVENT_WHEEL 3u  /* data: wheel delta, 120 per notch */
#define CABINPLAY_EVENT_KEY 4u    /* x: virtual key, y: down | modifiers << 8, data: UTF-16 char or 0 */

#define CABINPLAY_MOD_SHIFT 0x1u
#define CABINPLAY_MOD_CTRL 0x2u
#define CABINPLAY_MOD_ALT 0x4u

#pragma pack(push, 1)
typedef struct cabinplay_event {
    uint32_t type;
    int32_t x;
    int32_t y;
    int32_t data;
} cabinplay_event_t;

typedef struct cabinplay_state {
    uint32_t magic;
    uint32_t version;
    uint64_t heartbeat_ms;         /* plugin's GetTickCount64, refreshed every rendered frame */
    uint32_t flags;                /* CABINPLAY_STATE_* */
    volatile uint32_t event_write; /* number of events written so far; slot = n % capacity */
    float speed_kmh;
    float speed_limit_kmh;         /* 0 when unknown */
    float nav_distance_m;          /* 0 when no route is set */
    float nav_time_s;              /* remaining, in game time */
    uint32_t game_time_min;        /* in-game clock, minutes since the game's epoch */
    uint32_t reserved[5];
    cabinplay_event_t events[CABINPLAY_EVENT_CAPACITY];
} cabinplay_state_t;
#pragma pack(pop)

#define CABINPLAY_STATE_MAPPING_SIZE ((uint32_t)sizeof(cabinplay_state_t))

#endif
