# ktsu.ImGui.Monsoon

A neon-noir theme and widget library for Dear ImGui. It draws magenta, cyan and amber light on blue-black: translucent glass panels with rain running down them, text that glitches and flickers, dense data readouts, a targeting grid, and katakana signs drawn as neon tubes.

It is the third sibling of `ktsu.ImGui.Bulkhead` and `ktsu.ImGui.Phosphor`. Everything in it is original. It ships no fonts, images or other assets.

Run `examples/ImGuiMonsoonDemo` to see every widget on one screen.

## Quick start

```csharp
using ktsu.ImGui.App;
using ktsu.ImGui.Monsoon;

ImGuiApp.Start(new ImGuiAppConfig
{
	Title = "Operator Console",
	OnStart = MonsoonTheme.Apply,
	OnRender = _ =>
	{
		MonsoonWidgets.NeonSign("モンスーン", 44f);

		using MonsoonGlassPanel panel = MonsoonWidgets.GlassPanel("status", "STATUS // ステータス", new(420f, 320f), MonsoonPalette.Cyan);
		if (panel.IsVisible)
		{
			MonsoonWidgets.DataReadout("HUMIDITY", "98 %", MonsoonPalette.Cyan);
			MonsoonWidgets.SegmentedBar("POWER", 0.82f);
			MonsoonWidgets.StatusLamp("NETWORK", MonsoonLampState.Ok);
			if (MonsoonWidgets.NeonButton("ENHANCE", MonsoonPalette.Magenta))
			{
				// ...
			}
		}
	},
});
```

`MonsoonTheme.Apply()` sets the whole ImGui style. `MonsoonTheme.Scoped()` applies it for one `using` block and restores the previous style afterwards. `MonsoonTheme.Colors()` returns the colour map, so you can read the theme without applying it.

## Widgets

All widgets are on `MonsoonWidgets`.

| Widget | What it draws |
|---|---|
| `GlassPanel(id, title, size, accent, rain)` | A translucent child window with a neon header and rain on the glass. Use it in a `using` block and check `IsVisible`. |
| `NeonText(text, color, flicker)` | Glowing text, with optional flicker. |
| `GlitchText(text)` | Text that occasionally splits into offset colour channels. |
| `NeonSign(text, height, color, flicker)` | Katakana drawn as neon tubes. Each glyph flickers on its own. |
| `DataReadout(label, value, valueColor)` | One label and value row, for dense status columns. |
| `SegmentedBar(label, value, segments, color)` | A meter made of lit segments. The last segments of the scale light red. |
| `StatusLamp(label, state)` | A lamp for `Ok`, `Warn` or `Alarm`. The alarm lamp pulses. |
| `NeonButton(label, color)` | A button outlined in neon. It returns `true` when clicked. |
| `ScanGrid(id, size, ref target)` | A targeting grid with a sweep line. Click or drag on it to move the target. |
| `HexStream(id, lines, rowsPerSecond)` | A scrolling hex dump. |

`MonsoonPalette` holds the colours (`Background`, `Panel`, `Surface`, `Magenta`, `Cyan`, `Amber`, `Violet`, `Text`, `TextDim`, `Alarm`) and has the helpers `WithAlpha`, `Scale`, `Mix` and `FromHex`.

## Motion

`MonsoonEffects.Motion` controls the animation for the whole library:

- `Full`: rain, flicker, glitching, the sweep line and the hex stream all move.
- `Reduced`: nothing flickers or glitches, and the rain falls slowly. Use this for people who are sensitive to flashing.
- `None`: every frame is identical, because `MonsoonEffects.Time` stays at zero. Use this for screenshots and tests.

## Katakana without a font

Dear ImGui's default font has no Japanese glyphs. Monsoon draws katakana as hand-made vector strokes instead, so signs and panel titles work without loading a CJK font. This covers the full katakana block, including dakuten, handakuten, small kana and the long-vowel mark.

Kanji and hiragana are not drawn this way. To show them, load a font that has them.
