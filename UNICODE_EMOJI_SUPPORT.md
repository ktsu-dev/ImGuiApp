# Unicode and Emoji Support in ImGuiApp

This document explains the Unicode characters and emoji support that's now **enabled by default** in your ImGuiApp applications.

## Problem Solved

Previously, ImGui only loaded basic ASCII characters (0-127), which meant that UTF-8 characters like accented letters (café, naïve), mathematical symbols (∞, ≠, ≈), and emojis (😀, 🚀, 🌟) would render as question mark glyphs (�).

## Solution (Now Default!)

The ImGuiApp library now provides built-in support for extended Unicode character ranges that works with your existing font configuration **and is enabled by default**:

1. **Automatic Unicode support** - Enabled by default (`EnableUnicodeSupport = true`)
2. **Works with your fonts** - Uses whatever fonts you've configured, no system font dependencies
3. **Includes emojis** - Emoji ranges are automatically included
4. **Extended `ImGuiFontConfig`** with helper methods for advanced use cases
5. **`FontHelper` class** for custom font loading and testing

## Quick Start

### Method 1: Use Default Configuration (Recommended)

**Unicode and emoji support is enabled by default!** Just create your app normally:

```csharp
private static void Main() => ImGuiApp.Start(new()
{
    Title = "My Unicode App",
    // EnableUnicodeSupport = true by default - no configuration needed!
    Fonts = new Dictionary<string, byte[]>
    {
        { "MyFont", File.ReadAllBytes("path/to/your/font.ttf") }
    },
    // ... other settings
});
```

That's it! Your fonts automatically include extended Unicode character ranges and emojis (if your font supports them).

**To disable Unicode support** (ASCII only), set `EnableUnicodeSupport = false`.

### Method 2: Using ImGuiFontConfig for External Fonts

If you want to load fonts from file paths instead of byte arrays:

```csharp
private static void Main() => ImGuiApp.Start(new()
{
    Title = "My Unicode App",
    FontConfig = ImGuiFontConfig.WithUnicodeSupport("path/to/font.ttf", 16),
    // ... other settings
});
```

### Method 3: Custom Font Loading

For advanced scenarios where you need precise control:

```csharp
private static void Main() => ImGuiApp.Start(new()
{
    Title = "My Unicode App",
    OnConfigureIO = ConfigureCustomFonts,
    // ... other settings
});

private static void ConfigureCustomFonts()
{
    var io = ImGui.GetIO();
    var fontData = File.ReadAllBytes("path/to/font.ttf");
    
    // Load with extended Unicode ranges
    unsafe
    {
        uint* unicodeRanges = FontHelper.GetExtendedUnicodeRanges(io.Fonts);
        FontHelper.AddCustomFont(io, fontData, 16.0f, unicodeRanges);
    }
}
```

## Supported Character Ranges

### EnableUnicodeSupport = true (Default)
- ASCII (U+0000-U+007F) - Basic Latin
- Latin Extended-A (U+0100-U+017F) - Accented characters
- Latin Extended-B (U+0180-U+024F) - Additional accented characters
- General Punctuation (U+2000-U+206F) - Em dash, quotes, etc.
- Currency Symbols (U+20A0-U+20CF) - €, £, ¥, ₿, etc.
- Mathematical Operators (U+2200-U+22FF) - ∞, ≠, ≈, ∑, ∏, etc.
- Geometric Shapes (U+25A0-U+25FF) - ■, ●, ▲, etc.
- Miscellaneous Symbols (U+2600-U+26FF) - ☀, ♠, ♪, etc.
- **Emoji Ranges** (U+1F300-U+1FAFF) - 😀🚀🌟💻🎨🌈⚡🔥 (if font supports them)

### WithFullUnicodeSupport()
- All of the above, plus:
- Cyrillic characters
- Additional technical and mathematical symbols
- Box drawing and block elements
- (CJK support available but commented out due to memory usage)

### WithEmojiSupport()
- Basic Emoticons (U+1F600-U+1F64F) - 😀😃😄😁😆😅😂🤣
- Miscellaneous Symbols (U+1F300-U+1F5FF) - 🌈🌞🌙⭐🌍
- Transport Symbols (U+1F680-U+1F6FF) - 🚀🚂🚗✈️🚲
- Additional Symbols (U+1F900-U+1F9FF) - 🤔🤖🦋🌮

## Font Requirements

**No special font installation required!** The Unicode support works with whatever fonts you configure in your application. The system uses extended glyph ranges that include:

- Your existing font files (TTF, OTF, etc.)
- Embedded font resources in your application
- Default fonts provided by ImGuiApp

For best results, choose fonts that include the Unicode characters you need:
- **For European languages**: Most modern fonts include Latin Extended characters
- **For mathematical symbols**: Fonts like DejaVu Sans, Liberation Sans, or Noto Sans
- **For emojis**: Specialized emoji fonts like Noto Color Emoji, Apple Color Emoji, or Segoe UI Emoji

## Testing

Use the built-in test window to verify Unicode and emoji support:

```csharp
// In your render loop
FontHelper.ShowUnicodeTestWindow("My Unicode Test");
```

Or run the ImGuiAppDemo project and select "View → Unicode & Emoji Test" from the menu.

## Memory Considerations

- **EnableUnicodeSupport = true** (default): ~3-8 MB additional memory (includes Unicode symbols + emoji ranges)
- **EnableUnicodeSupport = false**: Minimal memory usage (ASCII only)
- **Custom font with full CJK support**: ~20-50 MB additional memory (if using Asian character sets)

## Troubleshooting

### Characters Still Show as Question Marks

1. **Check Unicode support is enabled**:
   ```csharp
   private static void Main() => ImGuiApp.Start(new()
   {
       EnableUnicodeSupport = true, // Make sure this is set to true
       // ... other settings
   });
   ```

2. **Verify your font supports the characters**: Not all fonts include all Unicode characters. Try using a font known to have good Unicode coverage like:
   - DejaVu Sans (included with most Linux distributions)
   - Liberation Sans
   - Noto Sans (if available)

3. **Check if the character is in the supported ranges**: The current implementation includes common Unicode blocks, but not all possible characters. You can add custom ranges using `FontHelper.AddCustomFont()`.

### Emojis Not Displaying

**Note**: Emoji ranges are automatically included when `EnableUnicodeSupport = true` (which is the default).

1. **Font limitation**: Most regular fonts don't include emoji characters. If emojis show as question marks, your font doesn't support them.

2. **Solutions**:
   - **Option A**: Use a font that includes emoji support (like Noto Sans with Noto Color Emoji)
   - **Option B**: Load an emoji font alongside your regular font:
     ```csharp
     private static void ConfigureEmojis()
     {
         var io = ImGui.GetIO();
         var emojiFont = File.ReadAllBytes("path/to/emoji-font.ttf");
         
         unsafe
         {
             uint* emojiRanges = FontHelper.GetEmojiRanges(io.Fonts);
             FontHelper.AddCustomFont(io, emojiFont, 16.0f, emojiRanges, mergeWithPrevious: true);
         }
     }
     ```

### Performance Issues

1. **Reduce glyph ranges**: Use `WithUnicodeSupport()` instead of `WithFullUnicodeSupport()`
2. **Smaller font sizes**: Larger fonts require more texture memory
3. **Font atlas optimization**: ImGui automatically optimizes the font atlas, but very large character sets may impact performance

## Summary

🎉 **Unicode and emoji support is now enabled by default in ImGuiApp!**

**Key Benefits:**
- ✅ **Zero configuration required** - Works out of the box
- ✅ **Uses your existing fonts** - No system font dependencies
- ✅ **Backward compatible** - Existing apps automatically get Unicode support
- ✅ **Includes emojis** - Emoji ranges included automatically
- ✅ **Memory efficient** - Only loads glyphs your font actually contains
- ✅ **Easy to disable** - Set `EnableUnicodeSupport = false` if you need ASCII-only

**What you get automatically:**
- Accented characters: café, naïve, résumé
- Mathematical symbols: ∞ ≠ ≈ ≤ ≥ ± × ÷ ∂ ∑ ∏ √ ∫
- Currency symbols: € £ ¥ ₹ ₿
- Arrows and shapes: ← → ↑ ↓ ■ ● ▲ ★
- Emojis (if your font supports them): 😀 🚀 🌟 💻 🎨 🌈 ⚡ 🔥

## Examples

See the `ImGuiAppDemo` project for a complete working example with Unicode and emoji support enabled by default.