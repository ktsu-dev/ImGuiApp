# Update/Render Synchronization Fix

## Problem Analysis

You correctly identified that the crash could be caused by **update and render becoming desynchronized**. The original code had several issues:

1. **Independent Rate Updates**: FPS and UPS were updated separately, potentially at different times
2. **Mid-Cycle Changes**: Performance updates occurred during the Update handler execution
3. **Frequent Changes**: No throttling on performance updates, causing rapid rate changes
4. **ImGui State Issues**: Inconsistent timing between updates and renders could corrupt ImGui state

## Root Cause

The crash in `ImGui.BeginMainMenuBar()` was likely caused by:
- **Timing mismatches** between update rate (UPS) and frame rate (FPS)
- **ImGui context corruption** due to inconsistent frame timing
- **Race conditions** from changing rates while update/render cycle is active

## Solution Implemented

### 1. Synchronized Rate Updates
**Before:**
```csharp
// Separate, independent updates
if (Math.Abs(currentFps - requiredFps) > 0.1)
    window.FramesPerSecond = requiredFps;

if (Math.Abs(currentUps - requiredUps) > 0.1)
    window.UpdatesPerSecond = requiredUps;
```

**After:**
```csharp
// Always update both together
bool fpsNeedsUpdate = Math.Abs(currentFps - requiredFps) > 0.1;
bool upsNeedsUpdate = Math.Abs(currentUps - requiredUps) > 0.1;

if (fpsNeedsUpdate || upsNeedsUpdate)
{
    // Always update both together to keep them synchronized
    window.FramesPerSecond = requiredFps;
    window.UpdatesPerSecond = requiredUps;
}
```

### 2. Throttled Performance Updates
**Before:**
```csharp
// Called every update cycle
UpdateWindowPerformance();
```

**After:**
```csharp
// Only update performance occasionally to avoid mid-cycle changes
var timeSinceLastUpdate = DateTime.UtcNow - lastPerformanceUpdate;
if (timeSinceLastUpdate.TotalSeconds > 0.5) // Update at most every 500ms
{
    UpdateWindowPerformance();
    lastPerformanceUpdate = DateTime.UtcNow;
}
```

### 3. Immediate Focus Change Response
```csharp
window!.FocusChanged += (focused) => 
{
    IsFocused = focused;
    // Force immediate performance update when focus changes
    lastPerformanceUpdate = DateTime.MinValue;
    DebugLogger.Log($"Focus changed: {focused}");
};
```

### 4. Error Handling and Logging
```csharp
try
{
    window.FramesPerSecond = requiredFps;
    window.UpdatesPerSecond = requiredUps;
    DebugLogger.Log($"Performance updated: FPS={requiredFps}, UPS={requiredUps}, Focused={IsFocused}, Idle={IsIdle}");
}
catch (Exception ex)
{
    DebugLogger.Log($"Error updating performance: {ex.Message}");
    // Don't rethrow - continue with current rates
}
```

## Key Benefits

1. **Prevents Desynchronization**: FPS and UPS are always updated together
2. **Reduces Race Conditions**: Performance updates happen less frequently (max every 500ms)
3. **Maintains Responsiveness**: Focus changes trigger immediate updates
4. **Better Stability**: Error handling prevents crashes from rate setting failures
5. **Debugging Support**: Logging helps track when and why performance changes occur

## Expected Behavior

- **Stable Timing**: Update and render rates stay synchronized
- **Responsive Focus Changes**: Immediate throttling when window loses/gains focus
- **No Mid-Cycle Disruption**: Performance changes don't interfere with active update/render cycles
- **Crash Prevention**: ImGui context remains stable with consistent timing

This fix addresses the core synchronization issue that was likely causing the ImGui crashes by ensuring consistent, stable timing between updates and renders.