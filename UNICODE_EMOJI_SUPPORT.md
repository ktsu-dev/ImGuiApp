# Unicode and Emoji Support in ImGuiApp

This document explains how to enable and use Unicode characters and emoji support in your ImGuiApp applications.

## Problem

By default, ImGui only loads basic ASCII characters (0-127), which means that UTF-8 characters like accented letters (café, naïve), mathematical symbols (∞, ≠, ≈), and emojis (😀, 🚀, 🌟) render as question mark glyphs (�).

## Solution

The ImGuiApp library now provides built-in support for extended Unicode character ranges and emoji fonts through:

1. **Extended `ImGuiFontConfig`** with helper methods for Unicode support
2. **`FontHelper` class** for easy font configuration
3. **Automatic system font detection** for cross-platform compatibility

## Quick Start

### Method 1: Using FontHelper (Recommended)

The easiest way to add Unicode and emoji support:

```csharp
private static void Main() => ImGuiApp.Start(new()
{
    Title = "My Unicode App",
    OnConfigureIO = ConfigureIO,
    // ... other settings
});

private static void ConfigureIO()
{
    var io = ImGui.GetIO();
    
    // This automatically finds and configures the best available fonts
    bool success = FontHelper.ConfigureUnicodeAndEmojiSupport(io, 16);
    
    if (!success)
    {
        Console.WriteLine("Warning: Unicode/emoji fonts not available");
    }
}
```

### Method 2: Using ImGuiFontConfig Directly

For more control over font configuration:

```csharp
private static void Main() => ImGuiApp.Start(new()
{
    Title = "My Unicode App",
    FontConfig = CreateUnicodeFontConfig(),
    // ... other settings
});

private static ImGuiFontConfig CreateUnicodeFontConfig()
{
    // Option 1: Extended Unicode support (recommended for most apps)
    string fontPath = "/usr/share/fonts/truetype/noto/NotoSans-Regular.ttf";
    return ImGuiFontConfig.WithUnicodeSupport(fontPath, 16);
    
    // Option 2: Full Unicode support (includes CJK characters - larger memory usage)
    // return ImGuiFontConfig.WithFullUnicodeSupport(fontPath, 16);
    
    // Option 3: Emoji-only font (for merging with existing fonts)
    // string emojiFont = "/usr/share/fonts/truetype/noto/NotoColorEmoji.ttf";
    // return ImGuiFontConfig.WithEmojiSupport(emojiFont, 16);
}
```

## Supported Character Ranges

### WithUnicodeSupport()
- ASCII (U+0000-U+007F) - Basic Latin
- Latin Extended-A (U+0100-U+017F) - Accented characters
- Latin Extended-B (U+0180-U+024F) - Additional accented characters
- General Punctuation (U+2000-U+206F) - Em dash, quotes, etc.
- Currency Symbols (U+20A0-U+20CF) - €, £, ¥, ₿, etc.
- Mathematical Operators (U+2200-U+22FF) - ∞, ≠, ≈, ∑, ∏, etc.
- Geometric Shapes (U+25A0-U+25FF) - ■, ●, ▲, etc.
- Miscellaneous Symbols (U+2600-U+26FF) - ☀, ♠, ♪, etc.

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

The library automatically detects and uses system fonts in this order:

### Linux (Ubuntu/Debian)
1. **Noto Sans** - `/usr/share/fonts/truetype/noto/NotoSans-Regular.ttf`
2. **Noto Color Emoji** - `/usr/share/fonts/truetype/noto/NotoColorEmoji.ttf`
3. **DejaVu Sans** - `/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf` (fallback)

### Windows
1. **Noto Sans** - `C:\Windows\Fonts\NotoSans-Regular.ttf`
2. **Segoe UI Emoji** - `C:\Windows\Fonts\seguiemj.ttf`

### macOS
1. **Noto Sans** - `/System/Library/Fonts/NotoSans.ttf`
2. **Apple Color Emoji** - `/System/Library/Fonts/Apple Color Emoji.ttc`

## Installation

If you're on Linux and don't have the required fonts, install them:

```bash
# Ubuntu/Debian
sudo apt update
sudo apt install -y fonts-noto fonts-noto-cjk fonts-noto-color-emoji fonts-liberation fonts-dejavu-core

# Refresh font cache
fc-cache -fv
```

## Testing

Use the built-in test window to verify Unicode and emoji support:

```csharp
// In your render loop
FontHelper.ShowUnicodeTestWindow("My Unicode Test");
```

Or run the ImGuiAppDemo project and select "View → Unicode & Emoji Test" from the menu.

## Memory Considerations

- **WithUnicodeSupport()**: ~2-5 MB additional memory (recommended for most apps)
- **WithFullUnicodeSupport()**: ~10-20 MB additional memory
- **WithEmojiSupport()**: ~5-10 MB additional memory (varies by emoji font)

## Troubleshooting

### Characters Still Show as Question Marks

1. **Check font availability**:
   ```csharp
   string? fontPath = FontHelper.GetNotoSansFontPath();
   if (fontPath == null)
   {
       Console.WriteLine("Noto Sans font not found!");
   }
   ```

2. **Verify font installation** (Linux):
   ```bash
   fc-list | grep -i noto
   ```

3. **Check glyph ranges**: Make sure you're using the appropriate `WithUnicodeSupport()` or `WithEmojiSupport()` method.

### Emojis Not Displaying

1. **Color emoji fonts**: Some emoji fonts (like Noto Color Emoji) may not render properly in all contexts. Try using a different emoji font or fallback to monochrome emoji symbols.

2. **Font merging**: Emoji fonts need to be merged with regular text fonts. Use `FontHelper.ConfigureUnicodeAndEmojiSupport()` which handles this automatically.

### Performance Issues

1. **Reduce glyph ranges**: Use `WithUnicodeSupport()` instead of `WithFullUnicodeSupport()`
2. **Smaller font sizes**: Larger fonts require more texture memory
3. **Font atlas optimization**: ImGui automatically optimizes the font atlas, but very large character sets may impact performance

## Examples

See the `ImGuiAppDemo` project for a complete working example with Unicode and emoji support enabled.