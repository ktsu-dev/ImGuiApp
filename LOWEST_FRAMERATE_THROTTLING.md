# Lowest Frame Rate Throttling System

## Approach

Instead of using a priority system, the application evaluates **all applicable throttling conditions** and automatically selects the **lowest frame rate** to maximize resource savings.

## Implementation

### Algorithm

```csharp
// Evaluate all throttling conditions and use the lowest frame rate
var candidateRates = new List<(double fps, double ups, string reason)>();

// Always include the base rate based on focus
if (IsFocused)
{
    candidateRates.Add((settings.FocusedFps, settings.FocusedUps, "focused"));     // 30 FPS
}
else
{
    candidateRates.Add((settings.UnfocusedFps, settings.UnfocusedUps, "unfocused")); // 5 FPS
}

// Add idle rate if applicable
if (IsIdle && settings.EnableIdleDetection)
{
    candidateRates.Add((settings.IdleFps, settings.IdleUps, "idle"));              // 10 FPS
}

// Add not visible rate if applicable
if (!IsVisible)
{
    candidateRates.Add((0.1, 0.1, "not visible"));                                 // 0.1 FPS
}

// Select the lowest frame rate and update rate
var selectedRate = candidateRates.OrderBy(r => r.fps).ThenBy(r => r.ups).First();
```

## Behavior Examples

### Single Conditions
- **Focused & Visible & Active**: 30 FPS (only focused rate applies)
- **Unfocused & Visible & Active**: 5 FPS (only unfocused rate applies)
- **Focused & Visible & Idle**: 10 FPS (min of focused=30, idle=10)
- **Focused & Not Visible**: 0.1 FPS (min of focused=30, not visible=0.1)

### Combined Conditions
- **Unfocused & Idle**: 5 FPS (min of unfocused=5, idle=10)
- **Unfocused & Not Visible**: 0.1 FPS (min of unfocused=5, not visible=0.1)
- **Idle & Not Visible**: 0.1 FPS (min of idle=10, not visible=0.1)
- **Unfocused & Idle & Not Visible**: 0.1 FPS (min of all rates)

## Benefits

### Maximum Resource Efficiency
- **Always uses the most aggressive throttling possible**
- **No missed opportunities** for resource savings
- **Automatic optimization** without manual tuning

### Flexible Combinations
- **Multiple conditions stack** naturally
- **Handles edge cases** automatically (e.g., minimized but still focused)
- **Future-proof** - easy to add new throttling conditions

### Predictable Behavior
- **Lowest rate always wins** - simple to understand
- **Consistent logic** across all scenarios
- **Easy to debug** with candidate rate evaluation

## Real-World Scenarios

| Scenario | Conditions | Candidates | Selected | Result |
|----------|------------|------------|----------|---------|
| **Active Use** | Focused, Visible, Active | [30] | 30 FPS | Normal performance |
| **Background Monitor** | Unfocused, Visible, Active | [5] | 5 FPS | Light throttling |
| **No User Input** | Focused, Visible, Idle | [30, 10] | 10 FPS | Idle throttling |
| **Alt-Tab Away** | Unfocused, Visible, Active | [5] | 5 FPS | Background throttling |
| **Minimized** | Focused, Not Visible | [30, 0.1] | 0.1 FPS | Maximum savings |
| **Background + Idle** | Unfocused, Visible, Idle | [5, 10] | 5 FPS | Most restrictive wins |
| **Minimized + Idle** | Unfocused, Not Visible, Idle | [5, 10, 0.1] | 0.1 FPS | Ultra-low power |

## Code Advantages

### Extensibility
```csharp
// Easy to add new throttling conditions
if (IsBatteryLow)
{
    candidateRates.Add((2.0, 2.0, "battery low"));
}

if (IsSystemUnderLoad)
{
    candidateRates.Add((1.0, 1.0, "system load"));
}
```

### Debugging
```csharp
// Can easily log which rate was selected and why
DebugLogger.Log($"Selected rate: {selectedRate.fps} FPS ({selectedRate.reason})");
DebugLogger.Log($"Candidates were: {string.Join(", ", candidateRates.Select(r => $"{r.fps}fps({r.reason})"))}");
```

This approach ensures the application always runs at the most resource-efficient rate possible for the current conditions.