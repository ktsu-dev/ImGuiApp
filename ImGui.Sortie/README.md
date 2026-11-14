# ktsu.ImGui.Sortie

[![NuGet](https://img.shields.io/nuget/v/ktsu.ImGui.Sortie?logo=nuget)](https://nuget.org/packages/ktsu.ImGui.Sortie)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](https://github.com/ktsu-dev/ImGuiApp/blob/main/LICENSE.md)

ImGui.Sortie is a theme and widget set for Dear ImGui in the style of an anime command room under attack: orange and red readouts on black, heavy condensed lettering, hazard stripes, hexagonal sector maps and a countdown that matters. It is an original homage. Nothing in it is taken from any franchise: no names, logos, fonts or artwork.

## Features

- **Two conditions**: `SortieCondition.Standby` (orange on black) and `SortieCondition.Emergency` (red), switched for the whole style with `SortieStyle.Apply` or for one block with `SortieStyle.Scope`. Every widget reads the current palette, so one call turns the whole screen red
- **Emergency banner**: a full-width banner with hazard stripes and a heavy caption that flashes
- **Countdown**: a seven-segment `mm:ss:cc` timer that goes red below a warning threshold
- **Sync gauge**: a large percentage readout over a segmented bar with tick marks
- **Hex grid**: a clickable hexagonal sector map, with cells lit by a caller's `HexCell → SortieLamp` dictionary
- **Panels**: framed child windows with a title tab, a corner code and cut corners
- **Status lamps, readouts and pattern monitors**: labelled lamps that can blink, label/value/unit rows, and a waveform trace over a grid
- **Heavy text**: condensed lettering made by squeezing and over-striking the current font, with no font file shipped
- **Buttons**: chamfered command buttons with a lamp stripe
- **Semantic theme**: `SortieTheme` implements `ISemanticTheme`, so the standby palette works with `Theme.Apply` and `ScopedTheme`

## Installation

```bash
dotnet add package ktsu.ImGui.Sortie
```

## Quick Start

```csharp
using ktsu.ImGui.Sortie;

SortieStyle.Apply(SortieCondition.Standby);

ImGuiSortie.BeginPanel("RESERVE POWER", new Vector2(320, 240), "03");
ImGuiSortie.Countdown("ACTIVE LIMIT", remaining, digitHeight: 44f);
ImGuiSortie.SyncGauge("SYNC RATIO", 0.85f);
ImGuiSortie.StatusLamp("UMBILICAL", SortieLamp.Alert, blink: true);
ImGuiSortie.EndPanel();

if (ImGuiSortie.Button("SORTIE", new Vector2(180, 40), SortieLamp.Alert))
{
	SortieStyle.Apply(SortieCondition.Emergency);
}

using (SortieStyle.Scope(SortieCondition.Emergency))
{
	ImGuiSortie.EmergencyBanner("EMERGENCY");
}
```

## Widgets

| Method | What it draws | Probe name |
|---|---|---|
| `EmergencyBanner(text, flashing, height)` | Hazard-striped banner | `emergency/{text}` |
| `Button(label, size, lamp)` | Chamfered command button | the label |
| `Countdown(label, remaining, digitHeight, warnBelow)` | Seven-segment timer | `countdown/{label}` |
| `SyncGauge(label, ratio, size)` | Percentage and segmented bar | `sync-gauge/{label}` |
| `StatusLamp(label, lamp, blink)` | Labelled lamp | `status-lamp/{label}` |
| `PatternMonitor(label, samples, size, lamp)` | Waveform over a grid | `pattern-monitor/{label}` |
| `HexGrid(label, size, radius, lit, out clicked)` | Clickable hex map | the label |
| `BeginPanel(title, size, code)` / `EndPanel()` | Framed panel | `panel/{title}` |
| `HeavyText(text, pixelHeight)` | Condensed heavy lettering | `heavy-text/{text}` |
| `Readout(label, value, unit, lamp)` | Label, value and unit row | `readout/{label}` |

Every widget marks itself through `ktsu.ImGui.Probes`, so a headless test can find it by name.

## Demo

`examples/ImGuiSortieDemo` puts every widget on one screen, and `tests/ImGuiSortieDemo.UITests` drives it headlessly. Set `IMGUI_SORTIE_SHOTS` to a folder to have the tests write standby and emergency screenshots there.

## License

MIT
