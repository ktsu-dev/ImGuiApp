# Input Focus Fix - Proper Idle Detection

## Problem Identified

The issue you observed was caused by **incorrect idle detection for unfocused windows**:

1. **Mouse over unfocused window** → `OnUserInput()` called → idle timer reset → 5 FPS ✅
2. **Stop mouse movement** → idle timer runs → becomes "idle" after 5 seconds → 10 FPS ❌

The problem was that **input events were being counted even when the window was unfocused**, which caused incorrect idle state management.

## Root Cause

All input handlers were calling `ImGuiApp.OnUserInput()` regardless of focus state:

```csharp
// BEFORE (Problematic)
private void OnMouseMove(IMouse _, Vector2 position)
{
    ImGuiApp.OnUserInput(); // Called even when unfocused!
    // ... rest of handler
}
```

This meant:
- **Mouse movement over unfocused window** → resets idle timer
- **Stop movement** → window becomes "idle" after 5 seconds
- **If focus detection issues exist** → thinks it's "focused + idle" → 10 FPS instead of 5 FPS

## Solution Applied

Modified all input handlers to **only count as user input when the window is actually focused**:

```csharp
// AFTER (Fixed)
private void OnMouseMove(IMouse _, Vector2 position)
{
    // Only count as user input if the window is actually focused
    if (ImGuiApp.IsFocused)
    {
        ImGuiApp.OnUserInput();
    }
    // ... rest of handler
}
```

### Input Handlers Fixed:
- ✅ `OnMouseMove()` - Mouse movement
- ✅ `OnMouseDown()` - Mouse button press
- ✅ `OnMouseUp()` - Mouse button release  
- ✅ `OnMouseScroll()` - Mouse wheel
- ✅ `OnKeyDown()` - Key press
- ✅ `OnKeyUp()` - Key release
- ✅ `OnKeyChar()` - Character input

## Expected Behavior After Fix

### Unfocused Window Scenarios:
| Action | Idle Timer | Expected FPS | Reason |
|--------|------------|--------------|---------|
| **Mouse over unfocused window** | Not reset | **5 FPS** | Only unfocused rate applies |
| **Stop mouse movement** | Continues | **5 FPS** | Still only unfocused rate |
| **Leave for 30+ seconds** | Continues | **5 FPS** | Unfocused windows can't become "idle" |

### Focused Window Scenarios:
| Action | Idle Timer | Expected FPS | Reason |
|--------|------------|--------------|---------|
| **Mouse movement** | Reset | **30 FPS** | Focused and active |
| **Stop for 5 seconds** | Expires | **10 FPS** | Focused but idle |
| **Move mouse again** | Reset | **30 FPS** | Active again |

## Key Improvements

1. **🎯 Accurate Idle Detection**: Only focused windows can become "idle"
2. **🔒 Proper Isolation**: Unfocused windows stay at unfocused rate regardless of mouse activity
3. **⚡ Consistent Behavior**: No more unexpected FPS changes from background mouse movement
4. **🧠 Logical Separation**: Idle state only applies to windows the user is actually interacting with

## Testing

**Before Fix:**
- Unfocus window → move mouse over it → 5 FPS
- Stop mouse movement → wait 5 seconds → **10 FPS** ❌

**After Fix:**
- Unfocus window → move mouse over it → 5 FPS  
- Stop mouse movement → wait 5+ seconds → **5 FPS** ✅

This fix ensures that unfocused windows consistently stay at the unfocused frame rate (5 FPS) regardless of mouse activity over them.