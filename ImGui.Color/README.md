# ktsu.ImGui.Color

[![NuGet](https://img.shields.io/nuget/v/ktsu.ImGui.Color?logo=nuget)](https://nuget.org/packages/ktsu.ImGui.Color)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](https://github.com/ktsu-dev/ImGuiApp/blob/main/LICENSE.md)

ImGui.Color is the bridge between [`ktsu.Semantics.Color`](https://github.com/ktsu-dev/Semantics) and Dear ImGui. Colors are held as the semantic `Color` (linear) and `Srgb` types and converted only at the ImGui seam, so color math happens in a space where it is meaningful and ImGui still receives the gamma-encoded values it expects. It is a standalone package with no dependency on `ktsu.ImGui.App`, so it can be dropped into any Hexa.NET.ImGui application.

## Features

- **Conversions at the seam only**: `Color` ↔ `ImColor`, `ImGuiVector4` and packed `ImU32`, with the sRGB/linear boundary handled for you
- **Direct sRGB path**: `Srgb` values pack straight through with no linear round-trip, for colors authored as sRGB (fixed UI colors, overlays, HSL-derived hues)
- **A strong vector type**: `ImGuiVector4` states that a vector is sRGB-encoded and ImGui-ready, and widens implicitly to `System.Numerics.Vector4`
- **ImColor operations**: lighten/darken, saturate/desaturate, hue offset, grayscale, invert, alpha
- **Accessibility analysis**: relative luminance, WCAG contrast ratio, perceptual distance
- **Contrast heuristics**: `MostReadableTextColor` and `AdjustForSufficientContrast` pick text that stays legible on a given background

All color math delegates to `ktsu.Semantics.Color`; this package only adapts it.

## Installation

### Package Manager Console

```powershell
Install-Package ktsu.ImGui.Color
```

### .NET CLI

```bash
dotnet add package ktsu.ImGui.Color
```

### Package Reference

```xml
<PackageReference Include="ktsu.ImGui.Color" Version="x.y.z" />
```

## Usage Examples

### Basic Example

```csharp
using ktsu.ImGui.Color;
using ktsu.Semantics.Color;

using Hexa.NET.ImGui;

Color accent = Color.FromHex("#ff6b6b");

ImGui.PushStyleColor(ImGuiCol.Button, accent.ToImGuiVector4());
ImGui.Button("Danger");
ImGui.PopStyleColor();

// Drawing straight into a draw list wants a packed color
ImGui.GetWindowDrawList().AddRectFilled(min, max, accent.ToImGuiU32());
```

### Colors authored as sRGB

`Srgb` carries gamma-encoded channels, so it packs directly with no linear round-trip. Alpha is supplied at the conversion, since `Srgb` holds only RGB.

```csharp
uint shadow = new Srgb(0.0, 0.0, 0.0).ToImGuiU32(alpha: 0.3f);
ImGui.GetWindowDrawList().AddRectFilled(min, max, shadow);
```

### Readable text on any background

```csharp
Span<Vector4> style = ImGui.GetStyle().Colors;
ImColor background = new() { Value = style[(int)ImGuiCol.WindowBg] };
ImColor label = background.MostReadableTextColor();

// Or keep an intended color and only push it as far as it needs to go
ImColor adjusted = background.AdjustForSufficientContrast(brandColor);
```

### Adjusting a color

```csharp
ImColor hovered = baseColor.LightenBy(0.1f);
ImColor pressed = baseColor.DarkenBy(0.1f);
ImColor muted = baseColor.DesaturateBy(0.4f);
ImColor disabled = baseColor.WithAlpha(0.4f);
```

## API Reference

### `ColorImGuiExtensions`

Conversions between the semantic linear `Color` and ImGui's types. Every conversion emits or accepts gamma-encoded sRGB.

| Name | Return Type | Description |
| ---- | ----------- | ----------- |
| `ToImColor(this Color)` | `ImColor` | Converts to an ImGui color |
| `FromImColor(this ImColor)` | `Color` | Reads an ImGui color back, interpreting its channels as sRGB |
| `ToImGuiVector4(this Color)` | `ImGuiVector4` | Converts to the strong sRGB vector |
| `FromImGuiVector4(ImGuiVector4)` / `FromImGuiVector4(Vector4)` | `Color` | Reads a vector back as sRGB |
| `ToImGuiU32(this Color)` | `uint` | Packs to `ImU32` (`0xAABBGGRR`), matching `ImGui.ColorConvertFloat4ToU32`. A pure pack — the global style alpha is not applied |
| `ToImGuiU32(this ImColor)` | `uint` | Packs an `ImColor`, applying the global style alpha exactly like `ImGui.GetColorU32` |

### `SrgbImGuiExtensions`

Direct conversions from a gamma-encoded `Srgb`, with no linear round-trip. Each takes an optional `alpha` (default opaque).

| Name | Return Type | Description |
| ---- | ----------- | ----------- |
| `ToImColor(this Srgb, float)` | `ImColor` | Packs the sRGB channels straight through |
| `ToImGuiVector4(this Srgb, float)` | `ImGuiVector4` | The strong sRGB vector |
| `ToImGuiU32(this Srgb, float)` | `uint` | Packed `ImU32`, applying the global style alpha like `ImGui.GetColorU32` |

### `ImColorExtensions`

Operations on an `ImColor`, all delegating to `ktsu.Semantics.Color`.

| Group | Members |
| ----- | ------- |
| Adjustment | `WithSaturation`, `SaturateBy`, `DesaturateBy`, `MultiplySaturation`, `WithLightness`, `LightenBy`, `DarkenBy`, `MultiplyLightness`, `OffsetHue`, `ToGrayscale`, `Invert`, `WithAlpha` |
| Analysis | `GetRelativeLuminance`, `GetContrastRatioOver`, `GetColorDistance` |
| Contrast | `MostReadableTextColor`, `AdjustForSufficientContrast` |
| Conversion | `ToSrgb`, `ToHsl`, `ToImGuiVector4` |

### `ImGuiVector4`

A readonly record struct of `X`, `Y`, `Z`, `W` stating that the vector is sRGB-encoded and ImGui-ready. Widens implicitly to `System.Numerics.Vector4` (or explicitly via `ToVector4()`), and offers `One` and `Zero`.

There is no `ImColor` factory class in this package: construct colors as `Color` or `Srgb` and convert.

## Acknowledgments

- [Dear ImGui](https://github.com/ocornut/imgui) - The immediate mode GUI library these colors are handed to
- [Hexa.NET.ImGui](https://github.com/HexaEngine/Hexa.NET.ImGui) - The .NET bindings for Dear ImGui that define `ImColor`
- [ktsu.Semantics](https://github.com/ktsu-dev/Semantics) - The `Color`, `Srgb`, `Hsl` and Oklab types, and all of the color math behind this adapter

## Contributing

Contributions are welcome! For feature requests, bug reports, or questions, please open an issue on the GitHub repository. If you would like to contribute code, please open a pull request with your changes.

## License

ImGui.Color is licensed under the MIT License. See [LICENSE.md](https://github.com/ktsu-dev/ImGuiApp/blob/main/LICENSE.md) for more information.
