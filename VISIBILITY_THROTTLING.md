# Window Visibility Throttling

## Feature Added

Added ultra-low resource usage when the window is not visible (minimized, hidden, etc.) by dropping to 1 Hz.

## Implementation

### Priority Order for Performance Throttling

The application now checks states in this priority order:

1. **🔍 Visibility** (Highest Priority)
   - **Not Visible**: 1 FPS / 1 UPS (minimized, hidden, etc.)

2. **💤 Idle Detection**
   - **Idle**: 10 FPS / 10 UPS (no user input for configured timeout)

3. **🎯 Focus State**
   - **Focused**: 30 FPS / 30 UPS (window has focus)
   - **Unfocused**: 5 FPS / 5 UPS (window visible but not focused)

### Code Changes

**In `UpdateWindowPerformance()`:**
```csharp
// Determine required FPS and UPS based on visibility, focus and idle state
double requiredFps, requiredUps;
if (!IsVisible)
{
    // Window is not visible (minimized, etc.) - drop to 1 Hz to save maximum resources
    requiredFps = 1.0;
    requiredUps = 1.0;
}
else if (IsIdle && settings.EnableIdleDetection)
{
    requiredFps = settings.IdleFps;      // 10 FPS
    requiredUps = settings.IdleUps;      // 10 UPS
}
else if (IsFocused)
{
    requiredFps = settings.FocusedFps;   // 30 FPS
    requiredUps = settings.FocusedUps;   // 30 UPS
}
else
{
    requiredFps = settings.UnfocusedFps; // 5 FPS
    requiredUps = settings.UnfocusedUps; // 5 UPS
}
```

**IsVisible Property:**
```csharp
public static bool IsVisible => (window?.WindowState != Silk.NET.Windowing.WindowState.Minimized) && (window?.IsVisible ?? false);
```

## Benefits

### Maximum Resource Savings
- **Minimized windows**: Drop to 1 FPS instead of 5 FPS (5x reduction)
- **Hidden windows**: Virtually no GPU/CPU usage for rendering
- **Background operation**: Minimal system impact when not in use

### Responsive Recovery
- **Instant response**: When window becomes visible again, immediately returns to appropriate FPS
- **No delays**: No startup lag when restoring from minimized state

### User Experience
- **Battery life**: Significant improvement on laptops when minimized
- **System performance**: Other applications get more resources
- **Thermal management**: Reduced heat generation when not in use

## Testing

### Manual Testing
1. **Minimize window**: Should drop to 1 FPS
2. **Restore window**: Should return to focused FPS (30)
3. **Hide behind other windows**: Should maintain unfocused FPS (5)
4. **Task switching**: Alt+Tab away should reduce to unfocused FPS

### Performance Tab
The demo application shows current state:
- `Window Focused: true/false`
- `Application Idle: true/false`
- `Window Visible: true/false`

## Expected Behavior

| Window State | FPS | UPS | Use Case |
|-------------|-----|-----|----------|
| **Focused & Visible** | 30 | 30 | Active use |
| **Unfocused & Visible** | 5 | 5 | Background monitoring |
| **Idle & Visible** | 10 | 10 | No user input |
| **Not Visible** | 1 | 1 | Minimized/hidden |

This feature provides the most aggressive resource savings possible while maintaining application responsiveness.