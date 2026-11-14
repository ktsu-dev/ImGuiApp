# ktsu.ImGui.Bulkhead

A starship-console look for Dear ImGui, inspired by LCARS-style interfaces: a black-backed theme with warm pastel accents, and widgets built from rounded end caps, elbows, pill buttons, segmented gauges and framed panels.

Everything here is drawn from primitives on the window draw list. No fonts, artwork or names are taken from any existing interface.

## Theme

```csharp
ImGuiApp.Start(new ImGuiAppConfig
{
	Title = "Console",
	OnStart = BulkheadStyle.Apply,   // or: using (BulkheadStyle.Scope()) { ... } for one region
	OnRender = _ => Render(),
});
```

`BulkheadStyle.Apply` sets the ImGui colours and the square, borderless style the look needs. `BulkheadTheme.Instance` is the same palette as a `ktsu.ThemeProvider` `ISemanticTheme`, so `ktsu.ImGui.Styler` can apply it too. The raw colours are on `BulkheadPalette` (`Amber`, `Lilac`, `Periwinkle`, `Ice`, `Ember` and the rest).

## Widgets

All on `ImGuiBulkhead`. Labels are drawn in upper case, and every widget marks itself for `ImGui.Probes` under its label or id.

| Widget | What it is |
|---|---|
| `Bar(id, size, color, caps, label)` | A flat bar, optionally rounded at either end, with a label against its right edge |
| `Button(label, size, color, caps)` | A pill button; returns `true` on the frame it is clicked |
| `Toggle(label, ref value, size, onColor, offColor)` | A pill that switches colour when on |
| `Elbow(id, size, corner, columnWidth, barThickness, color, label)` | The bent frame piece joining a column to a bar, in any of four corners |
| `Header(title, width, color)` | A section heading: a capped bar with the title cut into it |
| `Gauge(id, fraction, size, segments, color)` | A segmented level meter with rounded ends |
| `Panel(title, size, options)` | A framed region: two elbows, a sidebar column and a content area. Returns a `BulkheadPanel` scope whose `Sidebar()` and `Content()` each return a scope for that child window |

```csharp
using (BulkheadPanel panel = ImGuiBulkhead.Panel("Main Console", Vector2.Zero))
{
	using (panel.Sidebar())
	{
		DrawSidebar();
	}

	using (panel.Content())
	{
		DrawContent();
	}
}

static void DrawSidebar()
{
	foreach (string section in Sections)
	{
		if (ImGuiBulkhead.Button(section, new(ImGui.GetContentRegionAvail().X, 32), caps: BulkheadCaps.None))
		{
			current = section;
		}
	}
}
```

`BulkheadPanelOptions` sets the column width, the bar thickness, the gap between pieces and the colours. Left at zero, sizes come from the frame height, so the panel scales with the font.

The demo is `examples/ImGuiBulkheadDemo`, and `tests/ImGui.Bulkhead.UITests` drives it and every widget headlessly.

Every Begin/End pair is a scope (`ktsu.ScopedAction`), so it is closed by `Dispose` and cannot be left unbalanced. Each region opens at most once per panel, in either order; a panel whose sidebar is never opened fills the column instead.
