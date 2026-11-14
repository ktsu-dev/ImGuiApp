# ktsu.ImGui.Phosphor

[![NuGet](https://img.shields.io/nuget/v/ktsu.ImGui.Phosphor?logo=nuget)](https://nuget.org/packages/ktsu.ImGui.Phosphor)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](https://github.com/ktsu-dev/ImGuiApp/blob/main/LICENSE.md)

A retro CRT look for Dear ImGui, inspired by the ship computers of late-70s science fiction: one
phosphor colour on black glass, text that glows, thin vector lines, blocky readouts and a terminal
that types. It is an original homage with its own palette and drawing; it bundles no fonts or
artwork.

## Features

- **Two tints**: green and amber phosphor, each a full ImGui theme with five shades (`Hot`,
  `Bright`, `Normal`, `Dim`, `Faint`) drawn from one hue
- **Scoped switching**: `PhosphorStyle.Apply` sets the theme, `PhosphorStyle.Scope` draws one
  region in the other tint and restores the first
- **CRT overlay**: scanlines, a vignette and optional brightness flicker over a window or any
  rectangle, configured through `CrtOptions`
- **Text**: `GlowText` with a soft halo, `Readout` label-over-value boxes, and a flashing
  `AlertBanner`
- **Controls**: bracketed `Button`, blinking `StatusLight`, segmented `SegmentBar`
- **Instruments**: `Oscilloscope` over a sample span, `RadarSweep` with fading `RadarContact`
  blips, and a scrolling perspective `VectorGrid`
- **Frames**: `BeginFrame`/`EndFrame` draw a titled, bordered panel
- **Terminal**: `PhosphorTerminal` types queued lines behind a blinking block cursor and scrolls
  old lines away

Animated widgets ask `ktsu.ImGui.App` for an active frame while they move, so a throttled app keeps
animating them. With `Flicker` left at zero the overlay is static and the app can idle.

## Installation

```bash
dotnet add package ktsu.ImGui.Phosphor
```

## Quick Start

```csharp
using ktsu.ImGui.Phosphor;
using Hexa.NET.ImGui;

PhosphorTerminal terminal = new(charactersPerSecond: 60f);
terminal.WriteLine("SYSTEMS ONLINE. AWAITING QUERY.");

// Once, at start-up
PhosphorStyle.Apply(PhosphorTint.Green);

// Every frame
ImGui.Begin("Console");
if (ImGuiPhosphor.BeginFrame("COMMS", new(400, 200)))
{
	terminal.Draw("log");
}

ImGuiPhosphor.EndFrame();

ImGuiPhosphor.Readout("DRIVE A", "STANDBY");
ImGuiPhosphor.StatusLight("O2", lit: true, blinkHertz: 1f);
ImGuiPhosphor.SegmentBar("FUEL", 0.62f);
ImGuiPhosphor.AlertBanner("PROXIMITY WARNING");

using (PhosphorStyle.Scope(PhosphorTint.Amber))
{
	ImGuiPhosphor.RadarSweep("radar", radius: 80f);
}

ImGuiPhosphor.CrtOverlay();
ImGui.End();
```

`CrtOverlay` draws over the current window, so call it last.

## Demo

```bash
dotnet run --project examples/ImGuiPhosphorDemo
```

## License

MIT
