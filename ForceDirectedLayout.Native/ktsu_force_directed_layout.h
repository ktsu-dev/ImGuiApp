/*
 * ktsu.ForceDirectedLayout - C ABI reference header
 *
 * This header is hand-authored and intended as documentation / a starting point for native consumers.
 * NativeAOT also emits a header at publish time; prefer the generated one when integrating with
 * non-trivial codebases since it tracks struct sizes and pack rules exactly.
 *
 * Conventions:
 *   - All entry points return a status code (LAYOUT_OK = 0, negative = error).
 *   - Buffers crossing the boundary are caller-owned. The library writes into the buffer but
 *     never retains a pointer past the call.
 *   - Handles are opaque. Pair every Layout_Create with a Layout_Destroy.
 *   - All exceptions are caught at the boundary - none escape into native code.
 *   - Last-error messages are thread-local and queried via Layout_GetLastErrorMessage.
 */

#ifndef KTSU_FORCE_DIRECTED_LAYOUT_H
#define KTSU_FORCE_DIRECTED_LAYOUT_H

#include <stdint.h>

#ifdef __cplusplus
extern "C" {
#endif

/* Return codes. */
#define LAYOUT_OK              0
#define LAYOUT_INVALID_HANDLE -1
#define LAYOUT_NULL_ARGUMENT  -2
#define LAYOUT_OUT_OF_RANGE   -3
#define LAYOUT_INTERNAL_ERROR -4
#define LAYOUT_UNKNOWN_NODE   -5

/* POD types. Sequential layout, no managed references, fixed-size primitives. */

typedef struct {
    double x;
    double y;
} LayoutVec2D;

typedef struct {
    uint8_t  enabled;       /* 0 = no-op step, non-zero = run simulation */
    uint8_t  _pad0;
    uint8_t  _pad1;
    uint8_t  _pad2;
    int32_t  _pad3;
    double   repulsion_strength;
    double   link_spring_strength;
    double   directional_bias;
    double   gravity_strength;
    double   origin_anchor_weight;
    double   damping_factor;
    double   min_repulsion_distance;
    double   rest_link_length;
    double   max_force;
    double   max_velocity;
    double   target_physics_hz;
    double   stability_threshold;
} LayoutSettings;

typedef struct {
    int32_t      id;
    uint8_t      is_pinned;
    uint8_t      _pad0;
    uint8_t      _pad1;
    uint8_t      _pad2;
    LayoutVec2D  position;
    LayoutVec2D  dimensions;
} NodeInit;

typedef struct {
    int32_t      source_body_id;
    int32_t      target_body_id;
    LayoutVec2D  anisotropy; /* reserved, set to {0,0} for now */
} EdgeInit;

typedef struct {
    int32_t      id;
    int32_t      _pad0;
    LayoutVec2D  position;
    LayoutVec2D  velocity;
} NodePosition;

/* Opaque handle. */
typedef void* LayoutHandle;

/* Lifecycle. config may be NULL (defaults applied). */
LayoutHandle Layout_Create(const LayoutSettings* config);
int32_t      Layout_Destroy(LayoutHandle handle);

/* Mutation. */
int32_t Layout_SetSettings(LayoutHandle handle, const LayoutSettings* config);
int32_t Layout_SetNodes(LayoutHandle handle, const NodeInit* nodes, int32_t count);
int32_t Layout_SetEdges(LayoutHandle handle, const EdgeInit* edges, int32_t count);
int32_t Layout_SetPinned(LayoutHandle handle, int32_t node_index, uint8_t pinned);

/* Simulation. */
int32_t Layout_Step(LayoutHandle handle, double dt);
int32_t Layout_Solve(LayoutHandle handle, int32_t max_iterations, double tolerance);

/* Query. */
int32_t Layout_GetPositions(LayoutHandle handle, NodePosition* out_buffer, int32_t buffer_len);
int32_t Layout_GetIndexOf(LayoutHandle handle, int32_t node_id, int32_t* out_index);
int32_t Layout_GetNodeCount(LayoutHandle handle, int32_t* out_count);

/*
 * Diagnostics. Returns the UTF-8 byte length of the last error message and (if out_buffer
 * is non-null) copies up to buffer_len bytes into out_buffer. The string is NOT null-terminated.
 * Errors are thread-local.
 */
int32_t Layout_GetLastErrorMessage(uint8_t* out_buffer, int32_t buffer_len);

#ifdef __cplusplus
} /* extern "C" */
#endif

#endif /* KTSU_FORCE_DIRECTED_LAYOUT_H */
