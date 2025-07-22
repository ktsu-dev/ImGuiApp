# Resource Usage Fix for Unfocused Application

## Problem Description

The ImGuiApp was experiencing increased resource usage when the application window became unfocused, contrary to the expected throttling behavior. The application should reduce resource consumption when unfocused (from 30 FPS to 5 FPS) but was instead consuming more resources.

## Root Cause Analysis

The issue was in the `UpdateWindowPerformance()` method in `ImGuiApp.cs`. The problematic logic was:

1. When the window became unfocused, the code would disable VSync to allow precise frame rate control
2. However, disabling VSync could cause the GPU to render frames as fast as possible until the frame rate limiter kicked in
3. This created resource spikes and timing issues where the system would oscillate between high and low resource usage
4. The frame rate limiting mechanism was not as effective without VSync, leading to higher overall resource consumption

## Solution Implemented

### 1. Improved VSync Management Logic

**Before:**
```csharp
if (settings.DisableVSyncWhenThrottling)
{
    window.VSync = false; // Always disabled when throttling
}
```

**After:**
```csharp
if (settings.DisableVSyncWhenThrottling && (!IsFocused || IsIdle))
{
    // Only disable VSync when actually throttling AND target FPS is very low
    desiredVSyncState = requiredFps >= 30; // Keep VSync for higher frame rates
}
else
{
    desiredVSyncState = true; // Enable VSync when focused
}
```

### 2. Added VSync State Tracking

Added a static variable `lastVSyncState` to prevent rapid VSync toggling which could cause resource spikes:

```csharp
private static bool? lastVSyncState = null;

// Update VSync state only if it has changed to prevent rapid toggling
if (lastVSyncState != desiredVSyncState)
{
    window.VSync = desiredVSyncState;
    lastVSyncState = desiredVSyncState;
}
```

### 3. Separated VSync Management from Frame Rate Setting

The new implementation:
1. First determines the optimal VSync state based on focus and target FPS
2. Updates VSync only when the state actually changes
3. Then sets the frame rate independently

## Key Improvements

1. **Smart VSync Control**: VSync is only disabled for very low frame rates (< 30 FPS) when unfocused/idle
2. **Prevents Resource Spikes**: Higher frame rates (≥ 30 FPS) keep VSync enabled to prevent GPU from running at maximum speed
3. **Stable State Management**: VSync state tracking prevents rapid toggling that could cause performance issues
4. **Better Focus Handling**: VSync is always enabled when the application is focused for optimal performance

## Expected Behavior After Fix

- **Focused**: VSync enabled, 30 FPS target, normal resource usage
- **Unfocused**: VSync disabled only for 5 FPS target, significantly reduced resource usage
- **Idle**: VSync disabled only for 10 FPS target, minimal resource usage
- **No Resource Spikes**: Smooth transitions between focus states without resource consumption increases

## Files Modified

1. `ImGuiApp/ImGuiApp.cs`:
   - Updated `UpdateWindowPerformance()` method
   - Added `lastVSyncState` tracking variable
   - Improved VSync management logic
   - Updated `Reset()` method to initialize VSync state

## Testing Recommendations

1. Monitor CPU and GPU usage when switching between focused/unfocused states
2. Verify frame rates match expected values (30 FPS focused, 5 FPS unfocused)
3. Check that resource usage decreases when unfocused rather than increases
4. Test idle detection after 30 seconds (or configured timeout)

This fix should resolve the resource usage issue and ensure proper throttling behavior when the application is unfocused or idle.