# Deferred Update Fix - Preventing Mid-Cycle Rate Changes

## The Critical Issue

You correctly identified that the crash was happening because **FPS and UPS changes were occurring during the active update/render cycle**. This caused:

1. **ImGui Context Corruption**: Changing frame rates while ImGui is actively rendering
2. **Timing Race Conditions**: Update and render cycles becoming desynchronized mid-execution  
3. **OpenGL Context Issues**: Graphics driver confusion from rate changes during active rendering

## The Fatal Flaw in Previous Approaches

All previous attempts were still making this critical mistake:

```csharp
// WRONG: Changing rates during the update cycle
window!.Update += (delta) => {
    UpdateWindowPerformance(); // This changes FPS/UPS while updating!
    // ... rest of update logic
};
```

The crash in `ImGui.BeginMainMenuBar()` occurred because:
- ImGui expected consistent timing between frames
- We were changing the frame rate **while ImGui was trying to render**
- This corrupted ImGui's internal state and caused the assertion failure

## The Solution: Deferred Updates

### 1. Queue Changes Instead of Applying Immediately

**Before (Dangerous):**
```csharp
if (fpsNeedsUpdate || upsNeedsUpdate) {
    window.FramesPerSecond = requiredFps;  // CRASH RISK!
    window.UpdatesPerSecond = requiredUps; // CRASH RISK!
}
```

**After (Safe):**
```csharp
if (fpsNeedsUpdate || upsNeedsUpdate) {
    // Queue the changes - don't apply them yet
    pendingFps = requiredFps;
    pendingUps = requiredUps;
    performanceUpdatePending = true;
}
```

### 2. Apply Changes After Render Completes

```csharp
window!.Render += delta => {
    // ... do all rendering ...
    controller?.Render();
    
    // NOW it's safe to change rates - rendering is done
    ApplyPendingPerformanceChanges();
};
```

### 3. Safe Application with Error Handling

```csharp
private static void ApplyPendingPerformanceChanges() {
    if (performanceUpdatePending && pendingFps.HasValue && pendingUps.HasValue) {
        try {
            window!.FramesPerSecond = pendingFps.Value;
            window.UpdatesPerSecond = pendingUps.Value;
            // Clear pending changes
            pendingFps = null;
            pendingUps = null;
            performanceUpdatePending = false;
        }
        catch (Exception ex) {
            // Handle errors gracefully and clear pending state
            DebugLogger.Log($"Error applying performance changes: {ex.Message}");
            performanceUpdatePending = false;
        }
    }
}
```

## Key Architecture Changes

### State Variables Added:
```csharp
private static double? pendingFps = null;
private static double? pendingUps = null;
private static bool performanceUpdatePending = false;
```

### Update Cycle (Safe):
1. **Update Handler**: Queues performance changes, never applies them
2. **Render Handler**: Completes all rendering first
3. **Post-Render**: Applies queued changes when it's safe

### Timeline:
```
Frame N:   [Update] -> [Render] -> [Apply Pending Changes]
Frame N+1: [Update] -> [Render] -> [Apply Pending Changes] 
```

## Why This Fixes the Crash

1. **No Mid-Cycle Changes**: Rate changes never happen during active update/render
2. **Consistent ImGui State**: ImGui sees stable timing throughout each frame
3. **Safe Timing**: Changes apply between frames when no rendering is active
4. **Atomic Updates**: FPS and UPS always change together
5. **Error Recovery**: Failed changes don't crash the application

## Expected Behavior

- **No More Crashes**: ImGui context remains stable
- **Smooth Throttling**: Focus changes still work, but safely deferred
- **Consistent Performance**: Update and render stay synchronized
- **Better Logging**: Track when changes are queued vs. applied

The crash should now be completely eliminated because we never touch the frame rate or update rate while the graphics system is actively rendering.