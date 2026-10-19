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
#define CABINPLAY_APP_NAV_RECTS 0x2u   /* nav_source / nav_target say which part goes where */

/* The texture the game draws the accessory's UI into (see tools/build_mod.py): the map in
 * the upper half, the truck's mirror views in the lower half, and down the right edge a
 * strip the plugin recognises it by. */
#define CABINPLAY_NAV_WIDTH 1024u
#define CABINPLAY_NAV_HEIGHT 1024u

/* Colour the screen texture in the .scs is filled with (see tools/build_mod.py). */
#define CABINPLAY_MARKER_B 0x06
#define CABINPLAY_MARKER_G 0x02
#define CABINPLAY_MARKER_R 0x04

/* Pixels of exactly this colour are see-through to the map while navigation is visible
 * (see --map-key in app/ui/style.css). */
#define CABINPLAY_KEY_R 0x02
#define CABINPLAY_KEY_G 0x00
#define CABINPLAY_KEY_B 0x02

/* Width of the dock; by default the map lies under everything to the right of it. */
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
    uint16_t nav_source[4];    /* x, y, width, height in the UI texture, top row first */
    uint16_t nav_target[4];    /* x, y, width, height on the screen it is stretched over */
    uint32_t control_requests; /* bumped to ask for control mode to be opened or closed */
    uint16_t overlay_percent;  /* control mode's screen, percent of the game's width; 0: cabinplay.ini's */
    uint16_t cursor_speed;     /* control mode's cursor speed, percent; 0: cabinplay.ini's */
    uint8_t reserved1[8];
} cabinplay_frame_header_t;      /* followed by width * height BGRA pixels, top row first */
#pragma pack(pop)

/* ------------------------------------------------------------------ notice mapping */

/* A message the app wants the player to see over the game itself, for when there is no
 * CabinPlay screen in the cabin to show it on (the mod is off, or the truck has none). The
 * plugin draws it at the top of the game's picture while it is visible and the app is
 * running. Mirrored in app/GameNotice.cs. */
#define CABINPLAY_NOTICE_MAPPING_NAME L"Local\\CabinPlayNotice"
#define CABINPLAY_NOTICE_MAGIC 0x544E5043u /* "CPNT" */
#define CABINPLAY_NOTICE_VERSION 1u
#define CABINPLAY_NOTICE_MAX_W 1024u
#define CABINPLAY_NOTICE_MAX_H 256u
#define CABINPLAY_NOTICE_HEADER_SIZE 32u
#define CABINPLAY_NOTICE_MAPPING_SIZE (CABINPLAY_NOTICE_HEADER_SIZE + CABINPLAY_NOTICE_MAX_W * CABINPLAY_NOTICE_MAX_H * 4u)

/* ------------------------------------------------------------------ state mapping */

#define CABINPLAY_STATE_MAPPING_NAME L"Local\\CabinPlayState"
#define CABINPLAY_STATE_MAGIC 0x54535043u /* "CPST" */
#define CABINPLAY_STATE_VERSION 2u
#define CABINPLAY_EVENT_CAPACITY 256u

/* flags */
#define CABINPLAY_STATE_TELEMETRY 0x01u /* paused / electric / navigation values are valid */
#define CABINPLAY_STATE_PAUSED 0x02u
#define CABINPLAY_STATE_ELECTRIC 0x04u
#define CABINPLAY_STATE_MOUNTED 0x08u   /* a truck with the screen is loaded */
#define CABINPLAY_STATE_CONTROL 0x10u   /* in-game control mode is active */
#define CABINPLAY_STATE_NAV_READY 0x20u /* the game's map texture was found */
#define CABINPLAY_STATE_FOREGROUND 0x40u /* the game has the keyboard: shortcuts come as events */

/* event types; x and y are CabinPlay screen pixels unless noted */
#define CABINPLAY_EVENT_MOVE 1u
#define CABINPLAY_EVENT_BUTTON 2u /* data: button (0 left, 1 right, 2 middle) | down << 8 */
#define CABINPLAY_EVENT_WHEEL 3u  /* data: wheel delta, 120 per notch */
#define CABINPLAY_EVENT_KEY 4u    /* x: virtual key, y: down | modifiers << 8, data: UTF-16 char or 0 */
#define CABINPLAY_EVENT_SHORTCUT 5u /* x: virtual key pressed with Ctrl + left Alt */

#define CABINPLAY_MOD_SHIFT 0x1u
#define CABINPLAY_MOD_CTRL 0x2u
#define CABINPLAY_MOD_ALT 0x4u

/* vehicle flags */
#define CABINPLAY_VEHICLE_TRUCK 0x01u          /* the truck's configuration is known */
#define CABINPLAY_VEHICLE_ENGINE 0x02u         /* engine running */
#define CABINPLAY_VEHICLE_PARKING_BRAKE 0x04u
#define CABINPLAY_VEHICLE_TRAILER 0x08u        /* a trailer is part of the configuration */
#define CABINPLAY_VEHICLE_TRAILER_COUPLED 0x10u
#define CABINPLAY_VEHICLE_JOB 0x20u            /* a job is active */
#define CABINPLAY_VEHICLE_CARGO_LOADED 0x40u
#define CABINPLAY_VEHICLE_SPECIAL 0x80u        /* special transport */
#define CABINPLAY_VEHICLE_ATS 0x100u           /* American Truck Simulator: dollars */

#pragma pack(push, 1)
typedef struct cabinplay_notice_header {
    uint32_t magic;
    uint32_t version;
    volatile int32_t sequence; /* odd while the picture is being written, bumped to even after */
    uint32_t visible;          /* 1: draw it over the game */
    uint32_t width;            /* of the picture, at most CABINPLAY_NOTICE_MAX_W */
    uint32_t height;           /* at most CABINPLAY_NOTICE_MAX_H */
    uint32_t reserved[2];
} cabinplay_notice_header_t;   /* followed by BGRA pixels in rows of CABINPLAY_NOTICE_MAX_W, top row first */

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

/* Truck, trailer and job, from the telemetry API. Strings are UTF-8, NUL-terminated.
 * Wear and damage run from 0 (as new) to 1 (wrecked). */
typedef struct cabinplay_vehicle {
    volatile uint32_t sequence;    /* odd while being written */
    uint32_t flags;                /* CABINPLAY_VEHICLE_* */
    float fuel_l, fuel_capacity_l, fuel_range_km, fuel_average_l_per_km;
    float adblue_l, adblue_capacity_l;
    float odometer_km;
    float oil_temperature_c, water_temperature_c, battery_voltage;
    float wear_engine, wear_transmission, wear_cabin, wear_chassis, wear_wheels;
    float trailer_wear_chassis, trailer_wear_wheels, trailer_wear_body;
    float cargo_damage;
    float cargo_mass_kg;
    uint32_t delivery_time_min;    /* in game time, like game_time_min; 0 when unknown */
    uint32_t planned_distance_km;
    uint64_t income;
    char truck_brand[32];
    char truck_name[48];
    char truck_plate[24];
    char trailer_name[48];
    char trailer_plate[24];
    char cargo[64];
    char source_city[48];
    char source_company[48];
    char destination_city[48];
    char destination_company[48];
    char job_market[32];
    char source_city_id[32];       /* the game's city names are in the local language; the */
    char destination_city_id[32];  /* app looks these ids up in its own (see app/ui/places.js) */
} cabinplay_vehicle_t;             /* 632 bytes */

/* The state mapping: version 2 adds the vehicle block after the events. */
typedef struct cabinplay_state_mapping {
    cabinplay_state_t state;
    cabinplay_vehicle_t vehicle;
} cabinplay_state_mapping_t;
#pragma pack(pop)

#define CABINPLAY_VEHICLE_OFFSET ((uint32_t)sizeof(cabinplay_state_t)) /* 4160 */

#define CABINPLAY_STATE_MAPPING_SIZE ((uint32_t)sizeof(cabinplay_state_mapping_t))

#endif
