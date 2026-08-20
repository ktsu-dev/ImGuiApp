# Hexa.NET.ImGui.Widgets Tier 1 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Expose 13 Hexa.NET.ImGui.Widgets immediate-mode widgets through a ktsu-idiomatic API on `ImGuiWidgets`, delegating to Hexa's implementation, plus a demo tab comparing the seven that duplicate widgets we already ship.

**Architecture:** Every new file adds members to the existing `public static partial class ImGuiWidgets` in `ktsu.ImGui.Widgets`. Each public method is a thin adapter: validate arguments, convert ktsu semantic types to the primitives Hexa wants, call the Hexa static, convert the result back. Context-dependent logic that needs testing is extracted into pure `internal static` helpers, because the test project never creates an ImGui context.

**Tech Stack:** C# 13 / .NET 10-9-8 multi-target, `Hexa.NET.ImGui` 2.2.9, `Hexa.NET.ImGui.Widgets` 1.2.18, `Hexa.NET.ImGui.Widgets.Extras` 1.0.9, `ktsu.Sdk`, MSTest.Sdk on Microsoft Testing Platform, central package management.

**Spec:** `docs/superpowers/specs/2026-08-14-hexa-widgets-tier1-design.md`

## Global Constraints

Every task's requirements implicitly include this section.

- **File header.** Every new `.cs` file starts with exactly this single line, then a blank line:
  `// Copyright (c) 2023-2026 ktsu-dev contributors`
- **Style.** Tabs for indentation. CRLF line endings. File-scoped namespaces (`namespace ktsu.ImGui.Widgets;`). Using directives *inside* the namespace. Explicit types — never `var`. Always braces on control flow. No `this.` qualifier. Explicit accessibility modifiers.
- **Docs.** Warnings are errors and `GenerateDocumentationFile` is on. Every public member needs a complete XML doc comment including `<param>`, `<returns>`, and `<typeparam>` where applicable. A missing doc fails the build.
- **Validation.** Use `Ensure.NotNull(x)` (Polyfill, global namespace, no using directive needed) — not `ArgumentNullException.ThrowIfNull`. It throws `ArgumentNullException`.
- **Suppressions.** No global suppressions. If a warning must be suppressed, use a targeted `[SuppressMessage]` with a real justification string.
- **Target frameworks.** `ImGui.Widgets` is `net10.0;net9.0;net8.0`. Do not use `#if` framework directives; check API availability against net8.0.
- **Color conversion.** `Srgb` → `uint` uses `srgb.ToImGuiU32()` (applies global style alpha, requires a live ImGui context). `Color` → `Vector4` uses `color.ToImGuiVector4()`. Both come from `ktsu.ImGui.Color`.
- **Never** put `unsafe`, `byte*`, or `ReadOnlySpan<byte>` in a public signature.
- **Commit messages.** No `Co-Authored-By` lines. No version tags (`[minor]` etc.) on these commits.

## Build and test commands

```bash
# Build just the library (fastest feedback)
dotnet build ImGui.Widgets/ImGui.Widgets.csproj -p:KtsuSyncStyleConfigFiles=false

# Build everything
dotnet build ImGui.sln -p:KtsuSyncStyleConfigFiles=false

# Run the widget tests (builds first; whole suite runs in well under a second)
dotnet run --project tests/ImGui.Widgets.Tests/ImGui.Widgets.Tests.csproj -p:KtsuSyncStyleConfigFiles=false
```

**Always pass `-p:KtsuSyncStyleConfigFiles=false`.** ktsu.Sdk's `_KtsuSyncStyleConfigFiles`
target runs `BeforeTargets="PrepareForBuild"` in *every* project: it copies `.editorconfig`
out of the SDK package and then rewrites its `file_header_template` from that project's
`$(Copyright)`. Because projects and target frameworks build in parallel, one project
rewrites `.editorconfig` while another project's analyzers are reading it. The result is
non-deterministic `IDE0073` header failures — observed at 184 errors, then 0, then 45 across
three consecutive runs of unchanged code, with failing and passing files having
byte-identical headers. The flag disables that sync for the build only and changes nothing
on disk. Without it you cannot tell a real failure from a race.

If you see `KtsuRewriteEditorConfigHeader ... '.editorconfig' ... being used by another
process`, that is the same race in its other failure mode. Run `dotnet build-server
shutdown` and retry with the flag.

**Never "fix" an `IDE0073` failure by editing `.editorconfig` or by rewriting file headers.**
The committed headers are correct and match the committed template. Rebuild with the flag
first; the errors will disappear.

**Use `dotnet run --project` for tests, not `dotnet test`.** This repo's test projects are
MSTest.Sdk on Microsoft Testing Platform, and in this environment the `dotnet test` bridge
reports `Zero tests ran` with exit code 5 even though the same assembly passes 108/108 when
run directly. `dotnet run --project` is the MTP-native path and works. Do not spend time
debugging `dotnet test` — it is a known-bad path here.

**Do not filter to a single test.** MTP does not accept the `--filter
"FullyQualifiedName~X"` syntax used by VSTest; it silently matches nothing and reports
`Zero tests ran`, which reads exactly like a passing-but-empty run. Always run the whole
project and read the totals. A run that reports `total: 0` is a broken command, never a
pass.

**If a build fails with `KtsuRewriteEditorConfigHeader ... .editorconfig ... being used by
another process`,** that is a race in ktsu.Sdk between parallel target frameworks, not your
change. Run `dotnet build-server shutdown` and retry. Never edit `.editorconfig` to work
around it — the SDK rewrites that file on every build and your edit will be silently
reverted.

## File Structure

| File | Responsibility |
|---|---|
| `Directory.Packages.props` | *(modify)* Central versions for the two Hexa widget packages |
| `ImGui.Widgets/ImGui.Widgets.csproj` | *(modify)* Package references |
| `ImGui.App/FontHelper.cs` | *(modify)* `AddMaterialIconRanges` |
| `ImGui.Widgets/Spinner.cs` | `Spinner` |
| `ImGui.Widgets/BufferingBar.cs` | `BufferingBar` |
| `ImGui.Widgets/Splitter.cs` | `HorizontalSplitter`, `VerticalSplitter`, `ResolveSplitterMetrics` |
| `ImGui.Widgets/HexaButtons.cs` | `ToggleSwitch`, `ToggleButton`, `TransparentButton`, `InlineButton`, `InlineButtonPlacement` |
| `ImGui.Widgets/IconTreeNode.cs` | `IconTreeNode` |
| `ImGui.Widgets/EnumCombo.cs` | `EnumCombo`, `EnumComboNames` |
| `ImGui.Widgets/TextAlign.cs` | `TextCenteredV/H/VH` |
| `ImGui.Widgets/ImageAlign.cs` | `ImageCenteredV/H/VH`, `ImageScaleTo` |
| `ImGui.Widgets/Tooltip.cs` | `Tooltip` |
| `ImGui.Widgets/Breadcrumb.cs` | `Breadcrumb` |
| `ImGui.Widgets/DatePicker.cs` | `DatePicker`, `YearPicker` |
| `ImGui.Widgets/FileTreeView.cs` | `FileTreeView` |
| `ImGui.Widgets/FlameGraph.cs` | `FlameGraph`, `FlameGraphSample`, `FlameGraphOptions`, `FlattenCaptions`, `ResolveFlameGraphSelection` |
| `tests/ImGui.Widgets.Tests/HexaWidgetTests.cs` | Unit tests for every pure helper above |
| `examples/ImGuiWidgetsDemo/HexaWidgetsDemo.cs` | Comparison tab + net-new gallery |
| `examples/ImGuiWidgetsDemo/ImGuiWidgetsDemo.cs` | *(modify)* Register the new divider zone |
| `examples/ImGuiAppDemo/` | *(modify)* Material Icons font-loading example |

Each widget gets its own file to match the existing one-widget-per-file convention in `ImGui.Widgets/`.

---

### Task 1: Add the Hexa widget package dependencies

**Files:**
- Modify: `Directory.Packages.props`
- Modify: `ImGui.Widgets/ImGui.Widgets.csproj`

**Interfaces:**
- Consumes: nothing
- Produces: the `Hexa.NET.ImGui.Widgets` and `Hexa.NET.ImGui.Widgets.Extras` namespaces become available to `ImGui.Widgets`; `ktsu.Semantics.Paths` becomes available for `AbsoluteDirectoryPath`

There is no test for this task — it is a build-configuration change whose verification is that the solution still compiles and the existing tests still pass.

- [ ] **Step 1: Add central package versions**

In `Directory.Packages.props`, inside the existing `<ItemGroup>`, add these three lines immediately after the `Hexa.NET.ImPlot` line:

```xml
    <PackageVersion Include="Hexa.NET.ImGui.Widgets" Version="1.2.18" />
    <PackageVersion Include="Hexa.NET.ImGui.Widgets.Extras" Version="1.0.9" />
```

`ktsu.Semantics.Paths` already has a `PackageVersion` entry (2.9.1) — do not add a duplicate.

- [ ] **Step 2: Reference the packages from ImGui.Widgets**

In `ImGui.Widgets/ImGui.Widgets.csproj`, add to the existing second `<ItemGroup>`, keeping the list alphabetical among the `Hexa.NET.*` entries:

```xml
    <PackageReference Include="Hexa.NET.ImGui.Widgets" />
    <PackageReference Include="Hexa.NET.ImGui.Widgets.Extras" />
    <PackageReference Include="ktsu.Semantics.Paths" />
```

- [ ] **Step 3: Restore and build**

Run: `dotnet restore && dotnet build ImGui.sln -p:KtsuSyncStyleConfigFiles=false`
Expected: SUCCESS. If NU1605 or a downgrade warning appears for `Hexa.NET.ImGui`, that means CPM did not win — stop and report rather than pinning a lower version, because the spec depends on 2.2.9 being the resolved version.

- [ ] **Step 4: Confirm the existing tests still pass**

Run: `dotnet run --project tests/ImGui.Widgets.Tests/ImGui.Widgets.Tests.csproj -p:KtsuSyncStyleConfigFiles=false`
Expected: PASS, same count as before the change.

- [ ] **Step 5: Commit**

```bash
git add Directory.Packages.props ImGui.Widgets/ImGui.Widgets.csproj
git commit -m "Add Hexa.NET.ImGui.Widgets package references"
```

---

### Task 2: Material Icons glyph ranges in FontHelper

**Files:**
- Modify: `ImGui.App/FontHelper.cs`

**Interfaces:**
- Consumes: nothing
- Produces:
  - `internal static void FontHelper.AddMaterialIconRanges(ImFontGlyphRangesBuilderPtr builder)`
  - `public static unsafe uint* FontHelper.GetMaterialIconRanges()`

Material Icons places its glyphs in the Unicode Private Use Area. Hexa's constants land between `\xe000` and `\xf8ff` — for example `Home = \xe9b2`, `Computer = \xe31e`, `Folder = \xe2c7`, `CalendarToday = \xe935`. `AddMaterialIconRanges` adds that whole block.

It is deliberately **not** called from `GetExtendedUnicodeRanges`. That method already calls `AddNerdFontRanges`, whose Weather Icons range (`0xE300–0xE3EB`) collides with `Computer`. A merged font cannot own a codepoint twice, so this range is for building a *separate* merged font. Wiring it into the default path would silently break the Nerd Font icons that already work.

The public `GetMaterialIconRanges()` exists because that separate font has to be loadable from outside `ImGui.App`. `ImGuiAppConfig.Fonts` routes every font through `GetExtendedUnicodeRanges`, which would give a Material TTF the *Nerd* ranges and leave `\xe9b2` unmapped. The supported path is instead `FontHelper.AddCustomFont(io, ttfBytes, size, FontHelper.GetMaterialIconRanges(), mergeWithPrevious: true)`, which Task 15 demonstrates. This mirrors the existing public `GetEmojiRanges()` exactly, including the build-once caching that keeps the native ranges alive.

- [ ] **Step 1: Add the cache fields**

In `ImGui.App/FontHelper.cs`, find the existing `emojiRanges` / `emojiRangesInitialized` static fields and add a matching pair beside them:

```csharp
	private static ImVector<uint> materialIconRanges;
	private static bool materialIconRangesInitialized;
```

- [ ] **Step 2: Add the range builder**

Add this immediately after the closing brace of `AddNerdFontRanges`:

```csharp
	/// <summary>
	/// Adds the Material Icons private-use glyph range to the glyph ranges builder.
	/// </summary>
	/// <remarks>
	/// Material Icons maps its glyphs into the Unicode Private Use Area (U+E000–U+F8FF).
	/// This range overlaps the Nerd Font ranges added by <see cref="AddNerdFontRanges"/> —
	/// notably Weather Icons at U+E300–U+E3EB, which collides with Material's
	/// <c>Computer</c> glyph at U+E31E. A single merged font cannot own the same codepoint
	/// twice, so this method is intended for building a <em>separate</em> merged font and is
	/// deliberately not called from <see cref="GetExtendedUnicodeRanges"/>. Whichever font is
	/// merged last wins the contested span.
	/// </remarks>
	/// <param name="builder">The glyph ranges builder to add the Material Icons range to.</param>
	internal static void AddMaterialIconRanges(ImFontGlyphRangesBuilderPtr builder)
	{
		// Material Icons occupies the Basic Multilingual Plane Private Use Area in its entirety.
		for (uint c = 0xE000; c <= 0xF8FF; c++)
		{
			builder.AddChar(c);
		}
	}
```

- [ ] **Step 3: Add the public accessor**

Add this immediately after the closing brace of `GetEmojiRanges`:

```csharp
	/// <summary>
	/// Gets the Material Icons glyph ranges for loading a Material Icons font as a separate merged font.
	/// </summary>
	/// <remarks>
	/// Pass the result to <see cref="AddCustomFont"/> with <c>mergeWithPrevious: true</c>. Do not load a
	/// Material Icons font through <c>ImGuiAppConfig.Fonts</c> — that path applies
	/// <see cref="GetExtendedUnicodeRanges"/>, whose Nerd Font mapping does not cover Material's
	/// codepoints. Material and Nerd Font ranges overlap at U+E300–U+E3EB; whichever font is merged
	/// last wins that span.
	/// </remarks>
	/// <returns>Pointer to the Material Icons glyph ranges.</returns>
	[SuppressMessage("Major Code Smell", "S6640:Make sure that using \"unsafe\" is safe here", Justification = "Required for native ImGui interop; the ranges are cached in a static field so the pointer stays valid.")]
	public static unsafe uint* GetMaterialIconRanges()
	{
		// Only build ranges once and store them to prevent memory deallocation
		if (!materialIconRangesInitialized)
		{
			ImFontGlyphRangesBuilderPtr builder = new(ImGui.ImFontGlyphRangesBuilder());

			AddMaterialIconRanges(builder);

			materialIconRanges = new ImVector<uint>();
			fixed (ImVector<uint>* rangesPtr = &materialIconRanges)
			{
				builder.BuildRanges(rangesPtr);
			}

			materialIconRangesInitialized = true;
		}

		return materialIconRanges.Data;
	}
```

- [ ] **Step 4: Build**

Run: `dotnet build ImGui.App/ImGui.App.csproj -p:KtsuSyncStyleConfigFiles=false`
Expected: SUCCESS.

- [ ] **Step 5: Commit**

```bash
git add ImGui.App/FontHelper.cs
git commit -m "Add Material Icons glyph range support to FontHelper"
```

---

### Task 3: Spinner and BufferingBar

**Files:**
- Create: `ImGui.Widgets/Spinner.cs`
- Create: `ImGui.Widgets/BufferingBar.cs`

**Interfaces:**
- Consumes: Task 1's package references
- Produces:
  - `public static void ImGuiWidgets.Spinner(float radius, float thickness, Srgb color)`
  - `public static void ImGuiWidgets.BufferingBar(float value, Vector2 size, Srgb background, Srgb foreground)`

These two establish the color-conversion pattern every later wrapper follows. Both are pure passthroughs with no testable pure logic, so this task has no unit test — the draw output is verified in the demo (Task 14).

Hexa's signatures being wrapped:

```csharp
// Hexa.NET.ImGui.Widgets.ImGuiSpinner
public static unsafe void Spinner(float radius, float thickness, uint color);

// Hexa.NET.ImGui.Widgets.ImGuiProgressBar
public static unsafe void ProgressBar(float value, Vector2 size, uint backgroundColor, uint foregroundColor);
```

- [ ] **Step 1: Write `Spinner.cs`**

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using ktsu.ImGui.Color;
using ktsu.Semantics.Color;

using HexaSpinner = Hexa.NET.ImGui.Widgets.ImGuiSpinner;

public static partial class ImGuiWidgets
{
	/// <summary>
	/// Draws an indeterminate loading spinner that animates from the ImGui frame time.
	/// </summary>
	/// <param name="radius">Radius of the spinner in pixels.</param>
	/// <param name="thickness">Stroke thickness of the spinner arc in pixels.</param>
	/// <param name="color">Color of the spinner arc.</param>
	public static void Spinner(float radius, float thickness, Srgb color) =>
		HexaSpinner.Spinner(radius, thickness, color.ToImGuiU32());
}
```

- [ ] **Step 2: Write `BufferingBar.cs`**

Hexa calls this `ProgressBar`, but its own README documents it as a buffering bar, and we already ship `RadialProgressBar`. The name here reflects what it is.

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System.Numerics;

using ktsu.ImGui.Color;
using ktsu.Semantics.Color;

using HexaProgressBar = Hexa.NET.ImGui.Widgets.ImGuiProgressBar;

public static partial class ImGuiWidgets
{
	/// <summary>
	/// Draws a horizontal buffering bar filled from the left in proportion to <paramref name="value"/>.
	/// </summary>
	/// <param name="value">Fill fraction, clamped by the underlying implementation to the range 0 to 1.</param>
	/// <param name="size">Size of the bar in pixels.</param>
	/// <param name="background">Color of the unfilled portion.</param>
	/// <param name="foreground">Color of the filled portion.</param>
	public static void BufferingBar(float value, Vector2 size, Srgb background, Srgb foreground) =>
		HexaProgressBar.ProgressBar(value, size, background.ToImGuiU32(), foreground.ToImGuiU32());
}
```

- [ ] **Step 3: Build**

Run: `dotnet build ImGui.Widgets/ImGui.Widgets.csproj -p:KtsuSyncStyleConfigFiles=false`
Expected: SUCCESS. If `Srgb` does not resolve, check whether the correct using is `ktsu.Semantics` or `ktsu.Semantics.Color` by looking at how `ImGui.Color/SrgbImGuiExtensions.cs` declares its namespace, and use that in both files.

- [ ] **Step 4: Commit**

```bash
git add ImGui.Widgets/Spinner.cs ImGui.Widgets/BufferingBar.cs
git commit -m "Add Spinner and BufferingBar widgets backed by Hexa"
```

---

### Task 4: Splitters

**Files:**
- Create: `ImGui.Widgets/Splitter.cs`
- Create: `tests/ImGui.Widgets.Tests/HexaWidgetTests.cs`

**Interfaces:**
- Consumes: Task 1's package references
- Produces:
  - `public static bool ImGuiWidgets.HorizontalSplitter(string id, ref float height, float minHeight = float.MinValue, float maxHeight = float.MaxValue, float width = 0f, float thickness = 0f, float tolerance = 0f)`
  - `public static bool ImGuiWidgets.VerticalSplitter(string id, ref float width, float minWidth = float.MinValue, float maxWidth = float.MaxValue, float height = 0f, float thickness = 0f, float tolerance = 0f)`
  - `internal static (float Thickness, float Tolerance) ImGuiWidgets.ResolveSplitterMetrics(float thickness, float tolerance, float grabMinSize)`

Hexa hardcodes `thickness = 2` and `tolerance = 8` in raw pixels, which does not scale with the UI. We use `0f` as a "derive from style" sentinel — a zero-thickness splitter has no legitimate use, so the sentinel is unambiguous. The derivation is `grabMinSize * 0.25f` for thickness and `grabMinSize` for tolerance, which reproduces Hexa's 2 and 8 at the default `GrabMinSize` of 8 while scaling with the style.

This task also creates the test file that Tasks 7 and 13 extend.

Hexa's signature being wrapped:

```csharp
// Hexa.NET.ImGui.Widgets.ImGuiSplitter
public static bool VerticalSplitter(string strId, ref float width, float minWidth, float maxWidth, float height, float thickness, float tolerance);
public static bool HorizontalSplitter(string strId, ref float height, float minHeight, float maxHeight, float width, float thickness, float tolerance);
```

- [ ] **Step 1: Write the failing tests**

Create `tests/ImGui.Widgets.Tests/HexaWidgetTests.cs`:

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests for the pure helpers backing the Hexa-delegated widgets. The draw paths need a live
/// ImGui context and are verified visually in ImGuiWidgetsDemo; these cover the argument
/// resolution and marshaling logic that can run without one.
/// </summary>
[TestClass]
public sealed class HexaWidgetTests
{
	[TestMethod]
	public void ResolveSplitterMetrics_ZeroThickness_DerivesFromGrabMinSize()
	{
		(float thickness, float tolerance) = ImGuiWidgets.ResolveSplitterMetrics(0f, 4f, 8f);

		Assert.AreEqual(2f, thickness);
		Assert.AreEqual(4f, tolerance);
	}

	[TestMethod]
	public void ResolveSplitterMetrics_ZeroTolerance_DerivesFromGrabMinSize()
	{
		(float thickness, float tolerance) = ImGuiWidgets.ResolveSplitterMetrics(3f, 0f, 8f);

		Assert.AreEqual(3f, thickness);
		Assert.AreEqual(8f, tolerance);
	}

	[TestMethod]
	public void ResolveSplitterMetrics_BothZero_DerivesBoth()
	{
		(float thickness, float tolerance) = ImGuiWidgets.ResolveSplitterMetrics(0f, 0f, 16f);

		Assert.AreEqual(4f, thickness);
		Assert.AreEqual(16f, tolerance);
	}

	[TestMethod]
	public void ResolveSplitterMetrics_BothSupplied_PassesThroughUntouched()
	{
		(float thickness, float tolerance) = ImGuiWidgets.ResolveSplitterMetrics(1.5f, 12f, 8f);

		Assert.AreEqual(1.5f, thickness);
		Assert.AreEqual(12f, tolerance);
	}

	[TestMethod]
	public void VerticalSplitter_NullId_ThrowsBeforeTouchingImGui()
	{
		float width = 100f;

		// Ensure.NotNull must run before any ImGui call, or this would crash with no context
		// rather than throwing.
		Assert.ThrowsExactly<ArgumentNullException>(() =>
		{
			float local = width;
			ImGuiWidgets.VerticalSplitter(null!, ref local);
		});
	}

	[TestMethod]
	public void VerticalSplitter_MinGreaterThanMax_ThrowsBeforeTouchingImGui()
	{
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
		{
			float local = 100f;
			ImGuiWidgets.VerticalSplitter("id", ref local, minWidth: 50f, maxWidth: 10f);
		});
	}

	[TestMethod]
	public void HorizontalSplitter_MinGreaterThanMax_ThrowsBeforeTouchingImGui()
	{
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
		{
			float local = 100f;
			ImGuiWidgets.HorizontalSplitter("id", ref local, minHeight: 50f, maxHeight: 10f);
		});
	}
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet run --project tests/ImGui.Widgets.Tests/ImGui.Widgets.Tests.csproj -p:KtsuSyncStyleConfigFiles=false`
Expected: FAIL to compile with "does not contain a definition for 'ResolveSplitterMetrics'" and "'VerticalSplitter'".

- [ ] **Step 3: Write `Splitter.cs`**

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using Hexa.NET.ImGui;

using HexaSplitter = Hexa.NET.ImGui.Widgets.ImGuiSplitter;

public static partial class ImGuiWidgets
{
	/// <summary>
	/// Resolves the splitter grab metrics, substituting style-derived values for the zero sentinel.
	/// </summary>
	/// <param name="thickness">Requested thickness, or zero to derive from the style.</param>
	/// <param name="tolerance">Requested grab tolerance, or zero to derive from the style.</param>
	/// <param name="grabMinSize">The style's grab minimum size, in pixels.</param>
	/// <returns>The resolved thickness and tolerance.</returns>
	internal static (float Thickness, float Tolerance) ResolveSplitterMetrics(float thickness, float tolerance, float grabMinSize)
	{
		float resolvedThickness = thickness == 0f ? grabMinSize * 0.25f : thickness;
		float resolvedTolerance = tolerance == 0f ? grabMinSize : tolerance;
		return (resolvedThickness, resolvedTolerance);
	}

	/// <summary>
	/// Draws a draggable vertical splitter that adjusts <paramref name="width"/>.
	/// </summary>
	/// <param name="id">Unique identifier for the splitter.</param>
	/// <param name="width">The width being adjusted, updated in place while dragging.</param>
	/// <param name="minWidth">Smallest width the splitter will allow.</param>
	/// <param name="maxWidth">Largest width the splitter will allow.</param>
	/// <param name="height">Height of the splitter bar, or zero to fill the available region.</param>
	/// <param name="thickness">Thickness of the bar in pixels, or zero to derive from the style.</param>
	/// <param name="tolerance">Grab tolerance in pixels, or zero to derive from the style.</param>
	/// <returns><see langword="true"/> if the width changed this frame.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="id"/> is <see langword="null"/>.</exception>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="minWidth"/> exceeds <paramref name="maxWidth"/>.</exception>
	public static bool VerticalSplitter(string id, ref float width, float minWidth = float.MinValue, float maxWidth = float.MaxValue, float height = 0f, float thickness = 0f, float tolerance = 0f)
	{
		Ensure.NotNull(id);
		if (minWidth > maxWidth)
		{
			throw new ArgumentOutOfRangeException(nameof(minWidth), minWidth, "minWidth must not exceed maxWidth.");
		}

		(float resolvedThickness, float resolvedTolerance) = ResolveSplitterMetrics(thickness, tolerance, ImGui.GetStyle().GrabMinSize);
		return HexaSplitter.VerticalSplitter(id, ref width, minWidth, maxWidth, height, resolvedThickness, resolvedTolerance);
	}

	/// <summary>
	/// Draws a draggable horizontal splitter that adjusts <paramref name="height"/>.
	/// </summary>
	/// <param name="id">Unique identifier for the splitter.</param>
	/// <param name="height">The height being adjusted, updated in place while dragging.</param>
	/// <param name="minHeight">Smallest height the splitter will allow.</param>
	/// <param name="maxHeight">Largest height the splitter will allow.</param>
	/// <param name="width">Width of the splitter bar, or zero to fill the available region.</param>
	/// <param name="thickness">Thickness of the bar in pixels, or zero to derive from the style.</param>
	/// <param name="tolerance">Grab tolerance in pixels, or zero to derive from the style.</param>
	/// <returns><see langword="true"/> if the height changed this frame.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="id"/> is <see langword="null"/>.</exception>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="minHeight"/> exceeds <paramref name="maxHeight"/>.</exception>
	public static bool HorizontalSplitter(string id, ref float height, float minHeight = float.MinValue, float maxHeight = float.MaxValue, float width = 0f, float thickness = 0f, float tolerance = 0f)
	{
		Ensure.NotNull(id);
		if (minHeight > maxHeight)
		{
			throw new ArgumentOutOfRangeException(nameof(minHeight), minHeight, "minHeight must not exceed maxHeight.");
		}

		(float resolvedThickness, float resolvedTolerance) = ResolveSplitterMetrics(thickness, tolerance, ImGui.GetStyle().GrabMinSize);
		return HexaSplitter.HorizontalSplitter(id, ref height, minHeight, maxHeight, width, resolvedThickness, resolvedTolerance);
	}
}
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet run --project tests/ImGui.Widgets.Tests/ImGui.Widgets.Tests.csproj -p:KtsuSyncStyleConfigFiles=false`
Expected: PASS, 7 tests.

If the two `MinGreaterThanMax` tests fail with an access violation rather than an `ArgumentOutOfRangeException`, the guard clause is running after an ImGui call — move all validation above the `ImGui.GetStyle()` line.

- [ ] **Step 5: Commit**

```bash
git add ImGui.Widgets/Splitter.cs tests/ImGui.Widgets.Tests/HexaWidgetTests.cs
git commit -m "Add horizontal and vertical splitter widgets backed by Hexa"
```

---

### Task 5: Buttons

**Files:**
- Create: `ImGui.Widgets/HexaButtons.cs`

**Interfaces:**
- Consumes: Task 1's package references
- Produces:
  - `public enum InlineButtonPlacement { None = 0, NoRounding = 1, FillSpace = 2 }` (top-level in `ktsu.ImGui.Widgets`, not nested)
  - `public static bool ImGuiWidgets.ToggleSwitch(string label, ref bool selected)`
  - `public static bool ImGuiWidgets.ToggleButton(string label, ref bool selected, Vector2 size = default)`
  - `public static bool ImGuiWidgets.TransparentButton(string label, Vector2 size = default)`
  - `public static bool ImGuiWidgets.InlineButton(string label, Vector2 min, Vector2 max, Vector2 anchor, InlineButtonPlacement placement = InlineButtonPlacement.None)`

`ToggleSwitch` deliberately does not collide with our existing `ImGuiWidgets.Switch` — both survive until the comparison demo settles which to keep.

`InlineButton` replaces Hexa's `in ImRect bounds` with an explicit `min`/`max` pair, keeping `ImRect` out of our public surface. `ImRect` is constructed from those two corners inside the wrapper.

Hexa's signatures being wrapped:

```csharp
// Hexa.NET.ImGui.Widgets.ImGuiButton
public static bool ToggleSwitch(string label, ref bool selected);
public static bool ToggleButton(string label, ref bool selected, Vector2 sizeArg, ImGuiButtonFlags flags);
public static bool TransparentButton(string label);
public static bool InlineButton(ReadOnlySpan<byte> label, in ImRect bounds, in Vector2 anchor, InlineButtonFlags flags = InlineButtonFlags.None);
```

Note that Hexa's `TransparentButton(string)` has no size overload; the sized path goes through `TransparentButton(byte*, Vector2, ImGuiButtonFlags)`. Our wrapper uses the string overload when `size` is `default` and otherwise encodes the label to UTF-8 and calls the pointer overload — that encoding is the only unsafe block in this file and stays internal.

- [ ] **Step 1: Write `HexaButtons.cs`**

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Text;

using Hexa.NET.ImGui;

using HexaButton = Hexa.NET.ImGui.Widgets.ImGuiButton;
using HexaInlineButtonFlags = Hexa.NET.ImGui.Widgets.InlineButtonFlags;

/// <summary>
/// Placement options for <see cref="ImGuiWidgets.InlineButton"/>.
/// </summary>
[Flags]
public enum InlineButtonPlacement
{
	/// <summary>
	/// Default placement: rounded corners, sized to the label.
	/// </summary>
	None = 0,

	/// <summary>
	/// Draws the button with square corners.
	/// </summary>
	NoRounding = 1 << 0,

	/// <summary>
	/// Expands the button to fill the supplied bounds.
	/// </summary>
	FillSpace = 1 << 1,
}

public static partial class ImGuiWidgets
{
	/// <summary>
	/// Draws a sliding on/off switch.
	/// </summary>
	/// <param name="label">Label for display and identity.</param>
	/// <param name="selected">The switch state, updated in place when toggled.</param>
	/// <returns><see langword="true"/> if the state changed this frame.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="label"/> is <see langword="null"/>.</exception>
	public static bool ToggleSwitch(string label, ref bool selected)
	{
		Ensure.NotNull(label);
		return HexaButton.ToggleSwitch(label, ref selected);
	}

	/// <summary>
	/// Draws a button that shows a highlight ring while <paramref name="selected"/> is set.
	/// </summary>
	/// <param name="label">Label for display and identity.</param>
	/// <param name="selected">The toggle state, updated in place when clicked.</param>
	/// <param name="size">Button size in pixels, or <see langword="default"/> to size to the label.</param>
	/// <returns><see langword="true"/> if the state changed this frame.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="label"/> is <see langword="null"/>.</exception>
	public static bool ToggleButton(string label, ref bool selected, Vector2 size = default)
	{
		Ensure.NotNull(label);
		return HexaButton.ToggleButton(label, ref selected, size, ImGuiButtonFlags.None);
	}

	/// <summary>
	/// Draws a button with no background until it is hovered.
	/// </summary>
	/// <param name="label">Label for display and identity.</param>
	/// <param name="size">Button size in pixels, or <see langword="default"/> to size to the label.</param>
	/// <returns><see langword="true"/> if the button was clicked this frame.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="label"/> is <see langword="null"/>.</exception>
	[SuppressMessage("Major Code Smell", "S6640:Make sure that using \"unsafe\" is safe here", Justification = "Required to reach Hexa's sized TransparentButton overload; the buffer is stack-scoped to the call and not retained.")]
	public static unsafe bool TransparentButton(string label, Vector2 size = default)
	{
		Ensure.NotNull(label);
		if (size == default)
		{
			return HexaButton.TransparentButton(label);
		}

		int byteCount = Encoding.UTF8.GetByteCount(label);
		Span<byte> buffer = byteCount < 256 ? stackalloc byte[byteCount + 1] : new byte[byteCount + 1];
		Encoding.UTF8.GetBytes(label, buffer);
		buffer[byteCount] = 0;
		fixed (byte* pLabel = buffer)
		{
			return HexaButton.TransparentButton(pLabel, size, ImGuiButtonFlags.None);
		}
	}

	/// <summary>
	/// Draws a compact button anchored inside an existing rectangle, for use inside rows and headers.
	/// </summary>
	/// <param name="label">Label for display and identity.</param>
	/// <param name="min">Top-left corner of the bounds to anchor within, in screen space.</param>
	/// <param name="max">Bottom-right corner of the bounds to anchor within, in screen space.</param>
	/// <param name="anchor">Normalized anchor point within the bounds, where (0,0) is top-left and (1,1) is bottom-right.</param>
	/// <param name="placement">Placement options.</param>
	/// <returns><see langword="true"/> if the button was clicked this frame.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="label"/> is <see langword="null"/>.</exception>
	[SuppressMessage("Major Code Smell", "S6640:Make sure that using \"unsafe\" is safe here", Justification = "Required to encode the label for Hexa's span overload; the buffer is stack-scoped to the call and not retained.")]
	public static unsafe bool InlineButton(string label, Vector2 min, Vector2 max, Vector2 anchor, InlineButtonPlacement placement = InlineButtonPlacement.None)
	{
		Ensure.NotNull(label);

		int byteCount = Encoding.UTF8.GetByteCount(label);
		Span<byte> buffer = byteCount < 256 ? stackalloc byte[byteCount + 1] : new byte[byteCount + 1];
		Encoding.UTF8.GetBytes(label, buffer);
		buffer[byteCount] = 0;

		ImRect bounds = new(min, max);
		return HexaButton.InlineButton(buffer, bounds, anchor, (HexaInlineButtonFlags)placement);
	}
}
```

- [ ] **Step 2: Build**

Run: `dotnet build ImGui.Widgets/ImGui.Widgets.csproj -p:KtsuSyncStyleConfigFiles=false`
Expected: SUCCESS.

If `new ImRect(min, max)` does not compile, check `ImRect`'s available constructors in the Hexa.NET.ImGui bindings and use the field-initializer form (`ImRect bounds = new() { Min = min, Max = max };`) instead.

- [ ] **Step 3: Commit**

```bash
git add ImGui.Widgets/HexaButtons.cs
git commit -m "Add toggle, transparent and inline button widgets backed by Hexa"
```

---

### Task 6: IconTreeNode

**Files:**
- Create: `ImGui.Widgets/IconTreeNode.cs`

**Interfaces:**
- Consumes: Task 1's package references
- Produces: `public static bool ImGuiWidgets.IconTreeNode(string label, string icon, Color iconColor, ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.None)`

Named `IconTreeNode` rather than `Tree` because `ImGuiWidgets.Tree` is already a nested type in `Tree.cs`.

The `icon` parameter is a glyph string — typically a single character from an icon font such as `Hexa.NET.ImGui.Widgets.MaterialIcons`, which consumers reach transitively.

Hexa's signature being wrapped:

```csharp
// Hexa.NET.ImGui.Widgets.ImGuiTreeNode
public static bool IconTreeNode(string label, string icon, Vector4 iconColor, ImGuiTreeNodeFlags flags);
```

- [ ] **Step 1: Write `IconTreeNode.cs`**

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using Hexa.NET.ImGui;

using ktsu.ImGui.Color;
using ktsu.Semantics.Color;

using HexaTreeNode = Hexa.NET.ImGui.Widgets.ImGuiTreeNode;

public static partial class ImGuiWidgets
{
	/// <summary>
	/// Draws a tree node with a colored icon glyph before its label.
	/// </summary>
	/// <param name="label">Label for display and identity.</param>
	/// <param name="icon">Icon glyph to draw before the label, typically a single character from an icon font.</param>
	/// <param name="iconColor">Color applied to the icon glyph only; the label uses the current text color.</param>
	/// <param name="flags">Tree node behavior flags.</param>
	/// <returns><see langword="true"/> if the node is open and its children should be drawn.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="label"/> or <paramref name="icon"/> is <see langword="null"/>.</exception>
	public static bool IconTreeNode(string label, string icon, Color iconColor, ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.None)
	{
		Ensure.NotNull(label);
		Ensure.NotNull(icon);
		return HexaTreeNode.IconTreeNode(label, icon, iconColor.ToImGuiVector4(), flags);
	}
}
```

- [ ] **Step 2: Build**

Run: `dotnet build ImGui.Widgets/ImGui.Widgets.csproj -p:KtsuSyncStyleConfigFiles=false`
Expected: SUCCESS.

If `ToImGuiVector4()` returns an `ImGuiVector4` rather than a `System.Numerics.Vector4` and Hexa rejects it, check the alias at the top of `ImGui.Color/ColorImGuiExtensions.cs` — `ImGuiVector4` is an alias for `System.Numerics.Vector4` in this repo and should convert implicitly.

- [ ] **Step 3: Commit**

```bash
git add ImGui.Widgets/IconTreeNode.cs
git commit -m "Add IconTreeNode widget backed by Hexa"
```

---

### Task 7: EnumCombo

**Files:**
- Create: `ImGui.Widgets/EnumCombo.cs`
- Modify: `tests/ImGui.Widgets.Tests/HexaWidgetTests.cs`

**Interfaces:**
- Consumes: Task 4's `HexaWidgetTests` class
- Produces:
  - `public static bool ImGuiWidgets.EnumCombo<T>(string label, ref T value) where T : struct, Enum`
  - `internal static IReadOnlyList<string> ImGuiWidgets.EnumComboNames<T>() where T : struct, Enum`

Named `EnumCombo` because `ImGuiWidgets.Combo<TEnum>(string, ref TEnum) where TEnum : Enum` already exists in `Combo.cs` and would be ambiguous at the call site. Both survive until the comparison demo settles which to keep.

`EnumComboNames` exists so the name resolution is testable without an ImGui context; it delegates to Hexa's `ComboEnumHelper<T>.GetName`, which is pure.

Hexa's signature being wrapped:

```csharp
// Hexa.NET.ImGui.Widgets.ComboEnumHelper<T> where T : struct, Enum
public static bool Combo(string label, ref T value);
public static string GetName(T value);
```

- [ ] **Step 1: Write the failing tests**

Add these members to `HexaWidgetTests`, inside the existing class body:

```csharp
	private enum PlainEnum
	{
		First,
		Second,
		Third,
	}

	private enum ExplicitValueEnum
	{
		Ten = 10,
		Twenty = 20,
	}

	[Flags]
	private enum FlagsEnum
	{
		None = 0,
		Alpha = 1 << 0,
		Beta = 1 << 1,
	}

	[TestMethod]
	public void EnumComboNames_PlainEnum_ReturnsAllMembersInDeclarationOrder()
	{
		IReadOnlyList<string> names = ImGuiWidgets.EnumComboNames<PlainEnum>();

		CollectionAssert.AreEqual(new[] { "First", "Second", "Third" }, names.ToArray());
	}

	[TestMethod]
	public void EnumComboNames_ExplicitValueEnum_ReturnsAllMembers()
	{
		IReadOnlyList<string> names = ImGuiWidgets.EnumComboNames<ExplicitValueEnum>();

		CollectionAssert.AreEqual(new[] { "Ten", "Twenty" }, names.ToArray());
	}

	[TestMethod]
	public void EnumComboNames_FlagsEnum_ReturnsDeclaredMembersNotCombinations()
	{
		IReadOnlyList<string> names = ImGuiWidgets.EnumComboNames<FlagsEnum>();

		CollectionAssert.AreEqual(new[] { "None", "Alpha", "Beta" }, names.ToArray());
	}

	[TestMethod]
	public void EnumCombo_NullLabel_ThrowsBeforeTouchingImGui()
	{
		Assert.ThrowsExactly<ArgumentNullException>(() =>
		{
			PlainEnum local = PlainEnum.First;
			ImGuiWidgets.EnumCombo(null!, ref local);
		});
	}
```

Add `using System.Linq;` to the file's using block if it is not already present.

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet run --project tests/ImGui.Widgets.Tests/ImGui.Widgets.Tests.csproj -p:KtsuSyncStyleConfigFiles=false`
Expected: FAIL to compile with "does not contain a definition for 'EnumComboNames'".

- [ ] **Step 3: Write `EnumCombo.cs`**

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System.Collections.Generic;

// Note: no `using HexaComboEnumHelper = ...` alias here. C# cannot alias an open generic type,
// so Hexa's ComboEnumHelper<T> is written out in full at each call site below.

public static partial class ImGuiWidgets
{
	/// <summary>
	/// Returns the display names Hexa's enum combo shows, in declaration order.
	/// </summary>
	/// <typeparam name="T">The enum type to enumerate.</typeparam>
	/// <returns>The display name of every declared member of <typeparamref name="T"/>.</returns>
	internal static IReadOnlyList<string> EnumComboNames<T>() where T : struct, Enum
	{
		T[] values = Enum.GetValues<T>();
		List<string> names = new(values.Length);
		foreach (T value in values)
		{
			names.Add(Hexa.NET.ImGui.Widgets.ComboEnumHelper<T>.GetName(value));
		}

		return names;
	}

	/// <summary>
	/// Draws a combo box listing every member of an enum type.
	/// </summary>
	/// <typeparam name="T">The enum type to list.</typeparam>
	/// <param name="label">Label for display and identity.</param>
	/// <param name="value">The selected value, updated in place when a new member is chosen.</param>
	/// <returns><see langword="true"/> if a new value was selected this frame.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="label"/> is <see langword="null"/>.</exception>
	public static bool EnumCombo<T>(string label, ref T value) where T : struct, Enum
	{
		Ensure.NotNull(label);
		return Hexa.NET.ImGui.Widgets.ComboEnumHelper<T>.Combo(label, ref value);
	}
}
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet run --project tests/ImGui.Widgets.Tests/ImGui.Widgets.Tests.csproj -p:KtsuSyncStyleConfigFiles=false`
Expected: PASS, 11 tests.

If `EnumComboNames_FlagsEnum` fails because `Enum.GetValues<T>()` orders by value rather than declaration, adjust the expected array to match the actual ordering and note it in the test comment — the ordering guarantee belongs to the runtime, not to us.

- [ ] **Step 5: Commit**

```bash
git add ImGui.Widgets/EnumCombo.cs tests/ImGui.Widgets.Tests/HexaWidgetTests.cs
git commit -m "Add EnumCombo widget backed by Hexa"
```

---

### Task 8: Text alignment and tooltip

**Files:**
- Create: `ImGui.Widgets/TextAlign.cs`
- Create: `ImGui.Widgets/Tooltip.cs`

**Interfaces:**
- Consumes: Task 1's package references
- Produces:
  - `public static void ImGuiWidgets.TextCenteredV(string text)`
  - `public static void ImGuiWidgets.TextCenteredH(string text)`
  - `public static void ImGuiWidgets.TextCenteredVH(string text)`
  - `public static void ImGuiWidgets.Tooltip(string description)`

The `V`/`H`/`VH` suffixes are Hexa's and are kept, because `ImGuiWidgets.TextCentered` and `TextCenteredWithin` already exist in `Text.cs` with different semantics. Both survive until the comparison demo settles which to keep.

Hexa's signatures being wrapped:

```csharp
// Hexa.NET.ImGui.Widgets.TextHelper
public static void TextCenteredV(string text);
public static void TextCenteredH(string text);
public static void TextCenteredVH(string text);

// Hexa.NET.ImGui.Widgets.TooltipHelper
public static void Tooltip(string desc);
```

- [ ] **Step 1: Write `TextAlign.cs`**

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using HexaTextHelper = Hexa.NET.ImGui.Widgets.TextHelper;

public static partial class ImGuiWidgets
{
	/// <summary>
	/// Draws text centered vertically within the current line.
	/// </summary>
	/// <param name="text">The text to draw.</param>
	/// <exception cref="ArgumentNullException"><paramref name="text"/> is <see langword="null"/>.</exception>
	public static void TextCenteredV(string text)
	{
		Ensure.NotNull(text);
		HexaTextHelper.TextCenteredV(text);
	}

	/// <summary>
	/// Draws text centered horizontally within the available content region.
	/// </summary>
	/// <param name="text">The text to draw.</param>
	/// <exception cref="ArgumentNullException"><paramref name="text"/> is <see langword="null"/>.</exception>
	public static void TextCenteredH(string text)
	{
		Ensure.NotNull(text);
		HexaTextHelper.TextCenteredH(text);
	}

	/// <summary>
	/// Draws text centered both vertically and horizontally within the available content region.
	/// </summary>
	/// <param name="text">The text to draw.</param>
	/// <exception cref="ArgumentNullException"><paramref name="text"/> is <see langword="null"/>.</exception>
	public static void TextCenteredVH(string text)
	{
		Ensure.NotNull(text);
		HexaTextHelper.TextCenteredVH(text);
	}
}
```

- [ ] **Step 2: Write `Tooltip.cs`**

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using HexaTooltipHelper = Hexa.NET.ImGui.Widgets.TooltipHelper;

public static partial class ImGuiWidgets
{
	/// <summary>
	/// Shows a tooltip for the preceding item while it is hovered.
	/// </summary>
	/// <param name="description">The tooltip text.</param>
	/// <exception cref="ArgumentNullException"><paramref name="description"/> is <see langword="null"/>.</exception>
	public static void Tooltip(string description)
	{
		Ensure.NotNull(description);
		HexaTooltipHelper.Tooltip(description);
	}
}
```

- [ ] **Step 3: Build**

Run: `dotnet build ImGui.Widgets/ImGui.Widgets.csproj -p:KtsuSyncStyleConfigFiles=false`
Expected: SUCCESS.

- [ ] **Step 4: Commit**

```bash
git add ImGui.Widgets/TextAlign.cs ImGui.Widgets/Tooltip.cs
git commit -m "Add text alignment and tooltip helpers backed by Hexa"
```

---

### Task 9: Image alignment

**Files:**
- Create: `ImGui.Widgets/ImageAlign.cs`

**Interfaces:**
- Consumes: Task 1's package references
- Produces:
  - `public static void ImGuiWidgets.ImageCenteredV(nint textureId, Vector2 size)`
  - `public static void ImGuiWidgets.ImageCenteredH(nint textureId, Vector2 size)`
  - `public static void ImGuiWidgets.ImageCenteredVH(nint textureId, Vector2 size)`
  - `public static void ImGuiWidgets.ImageScaleTo(nint textureId, Vector2 imageSize, Vector2 destinationSize)`

Hexa takes `ImTextureRef`; we take `nint textureId` to match our existing `ImGuiWidgets.Image` and `ImageCentered`. `new ImTextureRef(texId: textureId)` is the conversion idiom already used by `Icon.cs` and `Avatar.cs`.

Hexa's signatures being wrapped:

```csharp
// Hexa.NET.ImGui.Widgets.ImageHelper
public static void ImageCenteredV(ImTextureRef image, Vector2 size);
public static void ImageCenteredH(ImTextureRef image, Vector2 size);
public static void ImageCenteredVH(ImTextureRef image, Vector2 size);
public static void ImageScaleTo(ImTextureRef image, Vector2 imgSize, Vector2 destSize);
```

- [ ] **Step 1: Write `ImageAlign.cs`**

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System.Numerics;

using Hexa.NET.ImGui;

using HexaImageHelper = Hexa.NET.ImGui.Widgets.ImageHelper;

public static partial class ImGuiWidgets
{
	/// <summary>
	/// Draws an image centered vertically within the current line.
	/// </summary>
	/// <param name="textureId">Native texture handle to draw.</param>
	/// <param name="size">Size to draw the image at, in pixels.</param>
	public static void ImageCenteredV(nint textureId, Vector2 size) =>
		HexaImageHelper.ImageCenteredV(new ImTextureRef(texId: textureId), size);

	/// <summary>
	/// Draws an image centered horizontally within the available content region.
	/// </summary>
	/// <param name="textureId">Native texture handle to draw.</param>
	/// <param name="size">Size to draw the image at, in pixels.</param>
	public static void ImageCenteredH(nint textureId, Vector2 size) =>
		HexaImageHelper.ImageCenteredH(new ImTextureRef(texId: textureId), size);

	/// <summary>
	/// Draws an image centered both vertically and horizontally within the available content region.
	/// </summary>
	/// <param name="textureId">Native texture handle to draw.</param>
	/// <param name="size">Size to draw the image at, in pixels.</param>
	public static void ImageCenteredVH(nint textureId, Vector2 size) =>
		HexaImageHelper.ImageCenteredVH(new ImTextureRef(texId: textureId), size);

	/// <summary>
	/// Draws an image scaled to fit inside a destination box while preserving its aspect ratio.
	/// </summary>
	/// <param name="textureId">Native texture handle to draw.</param>
	/// <param name="imageSize">The image's natural size, in pixels.</param>
	/// <param name="destinationSize">The box to fit the image inside, in pixels.</param>
	public static void ImageScaleTo(nint textureId, Vector2 imageSize, Vector2 destinationSize) =>
		HexaImageHelper.ImageScaleTo(new ImTextureRef(texId: textureId), imageSize, destinationSize);
}
```

- [ ] **Step 2: Build**

Run: `dotnet build ImGui.Widgets/ImGui.Widgets.csproj -p:KtsuSyncStyleConfigFiles=false`
Expected: SUCCESS.

- [ ] **Step 3: Commit**

```bash
git add ImGui.Widgets/ImageAlign.cs
git commit -m "Add image alignment helpers backed by Hexa"
```

---

### Task 10: Breadcrumb

**Files:**
- Create: `ImGui.Widgets/Breadcrumb.cs`

**Interfaces:**
- Consumes: Task 1's package references
- Produces: `public static bool ImGuiWidgets.Breadcrumb(string id, ref string path)`

This deliberately takes `ref string` rather than a `ktsu.Semantics.Paths` type, unlike `FileTreeView` in Task 12. Hexa only tokenizes the value on `/` and `\` for display; the path need not exist on disk, and a validated path type would reject legitimate virtual paths. This exception is recorded in the spec's conversion rules.

Hexa's signature being wrapped:

```csharp
// Hexa.NET.ImGui.Widgets.ImGuiBreadcrumb
public static bool Breadcrumb(string strId, ref string path);
```

- [ ] **Step 1: Write `Breadcrumb.cs`**

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using HexaBreadcrumb = Hexa.NET.ImGui.Widgets.ImGuiBreadcrumb;

public static partial class ImGuiWidgets
{
	/// <summary>
	/// Draws a clickable breadcrumb trail from a separator-delimited path.
	/// </summary>
	/// <remarks>
	/// The path is tokenized on both forward and back slashes for display only and is not
	/// required to exist on disk, so this accepts a plain string rather than a semantic path type.
	/// </remarks>
	/// <param name="id">Unique identifier for the breadcrumb.</param>
	/// <param name="path">The path to display, truncated in place to the clicked segment.</param>
	/// <returns><see langword="true"/> if a segment was clicked and the path changed this frame.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="id"/> or <paramref name="path"/> is <see langword="null"/>.</exception>
	public static bool Breadcrumb(string id, ref string path)
	{
		Ensure.NotNull(id);
		Ensure.NotNull(path);
		return HexaBreadcrumb.Breadcrumb(id, ref path);
	}
}
```

- [ ] **Step 2: Build**

Run: `dotnet build ImGui.Widgets/ImGui.Widgets.csproj -p:KtsuSyncStyleConfigFiles=false`
Expected: SUCCESS.

- [ ] **Step 3: Commit**

```bash
git add ImGui.Widgets/Breadcrumb.cs
git commit -m "Add Breadcrumb widget backed by Hexa"
```

---

### Task 11: DatePicker and YearPicker

**Files:**
- Create: `ImGui.Widgets/DatePicker.cs`

**Interfaces:**
- Consumes: Task 1's package references
- Produces:
  - `public static bool ImGuiWidgets.DatePicker(string label, ref DateTime date)`
  - `public static bool ImGuiWidgets.YearPicker(string label, ref DateTime date)`

Hexa's `DatePicker.Draw` returns `void`; ours returns `bool` by comparing the value before and after, matching `YearPicker`, which already returns `bool`.

Both widgets need a Material Icons font in the atlas — `DatePicker` draws `MaterialIcons.CalendarToday` (`\xe935`). Without one it renders a tofu box. Task 2 added the range support and Task 15 demonstrates loading the font. The XML docs must say so.

Hexa's signatures being wrapped:

```csharp
// Hexa.NET.ImGui.Widgets.DatePicker
public static void Draw(ReadOnlySpan<byte> label, ref DateTime time);

// Hexa.NET.ImGui.Widgets.YearPicker
public static bool Draw(ReadOnlySpan<byte> label, ref DateTime time);
```

Both take `ReadOnlySpan<byte>`, so the label is UTF-8 encoded inside the wrapper.

- [ ] **Step 1: Write `DatePicker.cs`**

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System.Text;

using HexaDatePicker = Hexa.NET.ImGui.Widgets.DatePicker;
using HexaYearPicker = Hexa.NET.ImGui.Widgets.YearPicker;

public static partial class ImGuiWidgets
{
	/// <summary>
	/// Encodes a label to a null-terminated UTF-8 buffer for Hexa's span-based overloads.
	/// </summary>
	/// <param name="label">The label to encode.</param>
	/// <returns>The encoded bytes, including a trailing null terminator.</returns>
	private static byte[] EncodeLabel(string label)
	{
		int byteCount = Encoding.UTF8.GetByteCount(label);
		byte[] buffer = new byte[byteCount + 1];
		Encoding.UTF8.GetBytes(label, buffer);
		buffer[byteCount] = 0;
		return buffer;
	}

	/// <summary>
	/// Draws a calendar control for picking a date.
	/// </summary>
	/// <remarks>
	/// Requires a Material Icons font in the atlas — the control draws the Material
	/// <c>CalendarToday</c> glyph at U+E935 and renders a placeholder box without one. See
	/// <c>FontHelper.AddMaterialIconRanges</c> in ktsu.ImGui.App.
	/// </remarks>
	/// <param name="label">Label for display and identity.</param>
	/// <param name="date">The selected date, updated in place when a new date is picked.</param>
	/// <returns><see langword="true"/> if the date changed this frame.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="label"/> is <see langword="null"/>.</exception>
	public static bool DatePicker(string label, ref DateTime date)
	{
		Ensure.NotNull(label);

		DateTime before = date;
		HexaDatePicker.Draw(EncodeLabel(label), ref date);
		return date != before;
	}

	/// <summary>
	/// Draws a grid control for picking a year.
	/// </summary>
	/// <param name="label">Label for display and identity.</param>
	/// <param name="date">The selected date, whose year is updated in place when a new year is picked.</param>
	/// <returns><see langword="true"/> if the year changed this frame.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="label"/> is <see langword="null"/>.</exception>
	public static bool YearPicker(string label, ref DateTime date)
	{
		Ensure.NotNull(label);
		return HexaYearPicker.Draw(EncodeLabel(label), ref date);
	}
}
```

- [ ] **Step 2: Build**

Run: `dotnet build ImGui.Widgets/ImGui.Widgets.csproj -p:KtsuSyncStyleConfigFiles=false`
Expected: SUCCESS.

- [ ] **Step 3: Commit**

```bash
git add ImGui.Widgets/DatePicker.cs
git commit -m "Add DatePicker and YearPicker widgets backed by Hexa"
```

---

### Task 12: FileTreeView

**Files:**
- Create: `ImGui.Widgets/FileTreeView.cs`

**Interfaces:**
- Consumes: Task 1's `ktsu.Semantics.Paths` reference
- Produces: `public static bool ImGuiWidgets.FileTreeView(string id, Vector2 size, ref AbsoluteDirectoryPath currentFolder, AbsoluteDirectoryPath homeFolder)`

Unlike `Breadcrumb`, this enumerates the real filesystem, so it takes semantic path types. Hexa's `ref string currentFolder` is bridged with `.ToString()` in and `.As<AbsoluteDirectoryPath>()` out.

Like `DatePicker`, this needs a Material Icons font — it draws `MaterialIcons.Home` (`\xe9b2`) and `MaterialIcons.Computer` (`\xe31e`).

Hexa's signature being wrapped:

```csharp
// Hexa.NET.ImGui.Widgets.ImGuiFileTreeView
public static bool FileTreeView(string strId, Vector2 size, ref string currentFolder, string homeFolder);
```

- [ ] **Step 1: Write `FileTreeView.cs`**

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System.Numerics;

using ktsu.Semantics.Paths;

using HexaFileTreeView = Hexa.NET.ImGui.Widgets.ImGuiFileTreeView;

public static partial class ImGuiWidgets
{
	/// <summary>
	/// Draws a navigable tree of the filesystem rooted at the machine's drives.
	/// </summary>
	/// <remarks>
	/// Requires a Material Icons font in the atlas — the control draws the Material <c>Home</c>
	/// glyph at U+E9B2 and <c>Computer</c> at U+E31E, and renders placeholder boxes without one.
	/// See <c>FontHelper.AddMaterialIconRanges</c> in ktsu.ImGui.App.
	/// </remarks>
	/// <param name="id">Unique identifier for the tree view.</param>
	/// <param name="size">Size of the tree view region, in pixels.</param>
	/// <param name="currentFolder">The selected folder, updated in place when a new folder is chosen.</param>
	/// <param name="homeFolder">The folder the home shortcut navigates to.</param>
	/// <returns><see langword="true"/> if the selected folder changed this frame.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="id"/> is <see langword="null"/>.</exception>
	public static bool FileTreeView(string id, Vector2 size, ref AbsoluteDirectoryPath currentFolder, AbsoluteDirectoryPath homeFolder)
	{
		Ensure.NotNull(id);

		string current = currentFolder.ToString();
		bool changed = HexaFileTreeView.FileTreeView(id, size, ref current, homeFolder.ToString());
		if (changed)
		{
			currentFolder = current.As<AbsoluteDirectoryPath>();
		}

		return changed;
	}
}
```

- [ ] **Step 2: Build**

Run: `dotnet build ImGui.Widgets/ImGui.Widgets.csproj -p:KtsuSyncStyleConfigFiles=false`
Expected: SUCCESS.

If `.As<AbsoluteDirectoryPath>()` does not resolve, check which namespace supplies the `As` extension by looking at `ImGui.Popups/FilesystemBrowser.cs:100`, which uses the same idiom, and match its using directives.

- [ ] **Step 3: Commit**

```bash
git add ImGui.Widgets/FileTreeView.cs
git commit -m "Add FileTreeView widget backed by Hexa"
```

---

### Task 13: FlameGraph

**Files:**
- Create: `ImGui.Widgets/FlameGraph.cs`
- Modify: `tests/ImGui.Widgets.Tests/HexaWidgetTests.cs`

**Interfaces:**
- Consumes: Task 4's `HexaWidgetTests` class
- Produces (the first two are **top-level** types in `ktsu.ImGui.Widgets`, not nested in `ImGuiWidgets` — reference them unqualified):
  - `public readonly record struct FlameGraphSample(float Start, float End, byte Level, string Caption)`
  - `public sealed record FlameGraphOptions`
  - `public static void ImGuiWidgets.FlameGraph(string label, IReadOnlyList<FlameGraphSample> samples, ref int selected, FlameGraphOptions? options = null)`
  - `internal static (byte[] Bytes, int[] Offsets) ImGuiWidgets.FlattenCaptions(IReadOnlyList<FlameGraphSample> samples)`
  - `internal static int ImGuiWidgets.ResolveFlameGraphSelection(int selected, int sampleCount)`

This is the largest adaptation in Tier 1. Hexa's API is an unsafe pull-based callback:

```csharp
// Hexa.NET.ImGui.Widgets.ImGuiWidgetFlameGraph
public delegate void ValuesGetter(float* start, float* end, byte* level, byte** caption, void* data, int idx);
public static void PlotFlame(string label, ValuesGetter valuesGetter, void* data, int valuesCount,
                             ref int selected, bool flip = false, int valuesOffset = 0,
                             string? overlayText = null, float scaleMin = float.MaxValue,
                             float scaleMax = float.MaxValue, Vector2 graphSize = default);
```

Our wrapper flattens all captions into a single null-terminated UTF-8 block up front — one allocation per call rather than one per sample — and the callback hands out interior pointers into that block.

- [ ] **Step 1: Write the failing tests**

Add these members to `HexaWidgetTests`, inside the existing class body:

```csharp
	[TestMethod]
	public void FlattenCaptions_EmptyList_ReturnsEmpty()
	{
		(byte[] bytes, int[] offsets) = ImGuiWidgets.FlattenCaptions([]);

		Assert.AreEqual(0, bytes.Length);
		Assert.AreEqual(0, offsets.Length);
	}

	[TestMethod]
	public void FlattenCaptions_SingleAsciiCaption_WritesNullTerminatedBytes()
	{
		(byte[] bytes, int[] offsets) = ImGuiWidgets.FlattenCaptions(
			[new FlameGraphSample(0f, 1f, 0, "ab")]);

		CollectionAssert.AreEqual(new byte[] { 0x61, 0x62, 0x00 }, bytes);
		CollectionAssert.AreEqual(new[] { 0 }, offsets);
	}

	[TestMethod]
	public void FlattenCaptions_MultipleCaptions_OffsetsPointPastEachTerminator()
	{
		(byte[] bytes, int[] offsets) = ImGuiWidgets.FlattenCaptions(
		[
			new FlameGraphSample(0f, 1f, 0, "ab"),
			new FlameGraphSample(1f, 2f, 1, "c"),
		]);

		// "ab\0c\0"
		CollectionAssert.AreEqual(new byte[] { 0x61, 0x62, 0x00, 0x63, 0x00 }, bytes);
		CollectionAssert.AreEqual(new[] { 0, 3 }, offsets);
	}

	[TestMethod]
	public void FlattenCaptions_MultiByteUtf8Caption_UsesByteLengthNotCharLength()
	{
		// U+00E9 encodes as two bytes (0xC3 0xA9), so the next offset must be 3, not 2.
		(byte[] bytes, int[] offsets) = ImGuiWidgets.FlattenCaptions(
		[
			new FlameGraphSample(0f, 1f, 0, "\u00e9"),
			new FlameGraphSample(1f, 2f, 1, "x"),
		]);

		CollectionAssert.AreEqual(new byte[] { 0xC3, 0xA9, 0x00, 0x78, 0x00 }, bytes);
		CollectionAssert.AreEqual(new[] { 0, 3 }, offsets);
	}

	[TestMethod]
	public void FlattenCaptions_EmptyStringCaption_WritesOnlyTerminator()
	{
		(byte[] bytes, int[] offsets) = ImGuiWidgets.FlattenCaptions(
		[
			new FlameGraphSample(0f, 1f, 0, ""),
			new FlameGraphSample(1f, 2f, 1, "y"),
		]);

		CollectionAssert.AreEqual(new byte[] { 0x00, 0x79, 0x00 }, bytes);
		CollectionAssert.AreEqual(new[] { 0, 1 }, offsets);
	}

	[TestMethod]
	public void ResolveFlameGraphSelection_InRange_PassesThrough()
	{
		Assert.AreEqual(2, ImGuiWidgets.ResolveFlameGraphSelection(2, 5));
	}

	[TestMethod]
	public void ResolveFlameGraphSelection_Negative_ClampsToMinusOne()
	{
		Assert.AreEqual(-1, ImGuiWidgets.ResolveFlameGraphSelection(-7, 5));
	}

	[TestMethod]
	public void ResolveFlameGraphSelection_PastEnd_ClampsToMinusOne()
	{
		Assert.AreEqual(-1, ImGuiWidgets.ResolveFlameGraphSelection(5, 5));
	}

	[TestMethod]
	public void ResolveFlameGraphSelection_EmptySampleList_ClampsToMinusOne()
	{
		Assert.AreEqual(-1, ImGuiWidgets.ResolveFlameGraphSelection(0, 0));
	}

	[TestMethod]
	public void FlameGraph_NullSamples_ThrowsBeforeTouchingImGui()
	{
		Assert.ThrowsExactly<ArgumentNullException>(() =>
		{
			int selected = -1;
			ImGuiWidgets.FlameGraph("label", null!, ref selected);
		});
	}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet run --project tests/ImGui.Widgets.Tests/ImGui.Widgets.Tests.csproj -p:KtsuSyncStyleConfigFiles=false`
Expected: FAIL to compile with "does not contain a definition for 'FlattenCaptions'".

- [ ] **Step 3: Write `FlameGraph.cs`**

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

using HexaFlameGraph = Hexa.NET.ImGui.Widgets.ImGuiWidgetFlameGraph;

/// <summary>
/// One bar in a flame graph.
/// </summary>
/// <param name="Start">Start position of the bar on the horizontal axis.</param>
/// <param name="End">End position of the bar on the horizontal axis.</param>
/// <param name="Level">Stack depth of the bar; zero is the bottom row.</param>
/// <param name="Caption">Text drawn inside the bar.</param>
public readonly record struct FlameGraphSample(float Start, float End, byte Level, string Caption);

/// <summary>
/// Optional presentation settings for <see cref="ImGuiWidgets.FlameGraph"/>.
/// </summary>
public sealed record FlameGraphOptions
{
	/// <summary>
	/// Gets a value indicating whether the graph grows downward instead of upward.
	/// </summary>
	public bool Flip { get; init; }

	/// <summary>
	/// Gets the text drawn over the graph, or <see langword="null"/> for none.
	/// </summary>
	public string? OverlayText { get; init; }

	/// <summary>
	/// Gets the lower bound of the horizontal axis, or <see cref="float.MaxValue"/> to fit the data.
	/// </summary>
	public float ScaleMin { get; init; } = float.MaxValue;

	/// <summary>
	/// Gets the upper bound of the horizontal axis, or <see cref="float.MaxValue"/> to fit the data.
	/// </summary>
	public float ScaleMax { get; init; } = float.MaxValue;

	/// <summary>
	/// Gets the size of the graph in pixels, or <see langword="default"/> to fill the content region.
	/// </summary>
	public Vector2 GraphSize { get; init; }
}

public static partial class ImGuiWidgets
{
	/// <summary>
	/// Flattens every sample caption into one null-terminated UTF-8 block.
	/// </summary>
	/// <remarks>
	/// A single block keeps the per-frame cost to one allocation rather than one per sample.
	/// Offsets are byte offsets, not character offsets, so multi-byte captions stay correct.
	/// </remarks>
	/// <param name="samples">The samples whose captions are being flattened.</param>
	/// <returns>The encoded block and the byte offset at which each sample's caption begins.</returns>
	internal static (byte[] Bytes, int[] Offsets) FlattenCaptions(IReadOnlyList<FlameGraphSample> samples)
	{
		int count = samples.Count;
		if (count == 0)
		{
			return ([], []);
		}

		int[] offsets = new int[count];
		int total = 0;
		for (int i = 0; i < count; i++)
		{
			offsets[i] = total;
			total += Encoding.UTF8.GetByteCount(samples[i].Caption ?? string.Empty) + 1;
		}

		byte[] bytes = new byte[total];
		for (int i = 0; i < count; i++)
		{
			string caption = samples[i].Caption ?? string.Empty;
			int written = Encoding.UTF8.GetBytes(caption, 0, caption.Length, bytes, offsets[i]);
			bytes[offsets[i] + written] = 0;
		}

		return (bytes, offsets);
	}

	/// <summary>
	/// Clamps a selection index to a valid sample index, or to -1 when nothing is selectable.
	/// </summary>
	/// <param name="selected">The requested selection index.</param>
	/// <param name="sampleCount">The number of samples available.</param>
	/// <returns>The requested index when it is in range, otherwise -1.</returns>
	internal static int ResolveFlameGraphSelection(int selected, int sampleCount) =>
		selected >= 0 && selected < sampleCount ? selected : -1;

	/// <summary>
	/// Draws a flame graph of hierarchical timing samples.
	/// </summary>
	/// <param name="label">Label for display and identity.</param>
	/// <param name="samples">The bars to plot.</param>
	/// <param name="selected">Index of the selected bar, updated in place when a bar is clicked; -1 for none.</param>
	/// <param name="options">Optional presentation settings, or <see langword="null"/> for defaults.</param>
	/// <exception cref="ArgumentNullException"><paramref name="label"/> or <paramref name="samples"/> is <see langword="null"/>.</exception>
	[SuppressMessage("Major Code Smell", "S6640:Make sure that using \"unsafe\" is safe here", Justification = "Required to satisfy Hexa's pull-based callback; all pinned memory is freed before the method returns.")]
	public static unsafe void FlameGraph(string label, IReadOnlyList<FlameGraphSample> samples, ref int selected, FlameGraphOptions? options = null)
	{
		Ensure.NotNull(label);
		Ensure.NotNull(samples);

		FlameGraphOptions resolvedOptions = options ?? new FlameGraphOptions();
		selected = ResolveFlameGraphSelection(selected, samples.Count);

		(byte[] captionBytes, int[] captionOffsets) = FlattenCaptions(samples);

		GCHandle captionHandle = GCHandle.Alloc(captionBytes, GCHandleType.Pinned);
		try
		{
			byte* captionBase = (byte*)captionHandle.AddrOfPinnedObject();

			void Getter(float* start, float* end, byte* level, byte** caption, void* data, int idx)
			{
				FlameGraphSample sample = samples[idx];
				*start = sample.Start;
				*end = sample.End;
				*level = sample.Level;
				*caption = captionBase + captionOffsets[idx];
			}

			HexaFlameGraph.PlotFlame(
				label,
				Getter,
				null,
				samples.Count,
				ref selected,
				resolvedOptions.Flip,
				0,
				resolvedOptions.OverlayText,
				resolvedOptions.ScaleMin,
				resolvedOptions.ScaleMax,
				resolvedOptions.GraphSize);
		}
		finally
		{
			captionHandle.Free();
		}
	}
}
```

Note that `captionBytes` is empty when `samples` is empty; `GCHandle.Alloc` on a zero-length array is valid and `AddrOfPinnedObject` is never dereferenced because `valuesCount` is zero.

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet run --project tests/ImGui.Widgets.Tests/ImGui.Widgets.Tests.csproj -p:KtsuSyncStyleConfigFiles=false`
Expected: PASS, 21 tests.

If the local function `Getter` will not convert to Hexa's `ValuesGetter` delegate, declare it explicitly instead: `HexaFlameGraph.ValuesGetter getter = Getter;` and pass `getter`.

- [ ] **Step 5: Run the whole test suite**

Run: `dotnet run --project tests/ImGui.Widgets.Tests/ImGui.Widgets.Tests.csproj -p:KtsuSyncStyleConfigFiles=false`
Expected: PASS, all tests including the pre-existing ones.

- [ ] **Step 6: Commit**

```bash
git add ImGui.Widgets/FlameGraph.cs tests/ImGui.Widgets.Tests/HexaWidgetTests.cs
git commit -m "Add FlameGraph widget with managed sample marshaling"
```

---

### Task 14: Comparison demo tab

**Files:**
- Create: `examples/ImGuiWidgetsDemo/HexaWidgetsDemo.cs`
- Modify: `examples/ImGuiWidgetsDemo/ImGuiWidgetsDemo.cs`

**Interfaces:**
- Consumes: every public member produced by Tasks 3–13
- Produces: `internal static void HexaWidgetsDemo.Show(float size)`

The existing demo lays out two `DividerZone`s registered in `OnStart`. This adds a third zone containing an `ImGui.BeginTabBar` with two tabs: **"Hexa vs ktsu"** for the overlapping pairs and **"Net New"** for the widgets with no counterpart.

The comparison tab's contract: each row binds both implementations to the *same* backing field, so divergent behavior shows up live rather than being inferred from two independent demos.

- [ ] **Step 1: Write `HexaWidgetsDemo.cs`**

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Examples.Widgets;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Widgets;
using ktsu.Semantics.Color;
using ktsu.Semantics.Paths;

/// <summary>
/// Side-by-side comparison of the widgets that exist in both ktsu.ImGui.Widgets and
/// Hexa.NET.ImGui.Widgets, plus a gallery of the Hexa widgets that have no ktsu counterpart.
/// Each comparison row drives both implementations from the same backing field so behavioral
/// differences are visible rather than inferred.
/// </summary>
internal static class HexaWidgetsDemo
{
	// Shared state: each field feeds BOTH implementations of its row.
	private static bool sharedToggle = true;
	private static EnumValues sharedEnum = EnumValues.Value1;
	private static float sharedSplitWidth = 200f;
	private static float sharedProgress = 0.45f;

	// Net-new widget state.
	private static string breadcrumbPath = @"C:\dev\ktsu-dev\ImGuiApp\ImGui.Widgets";
	private static DateTime pickedDate = new(2026, 8, 14);
	private static DateTime pickedYear = new(2026, 1, 1);
	private static AbsoluteDirectoryPath treeFolder = AppContext.BaseDirectory.As<AbsoluteDirectoryPath>();
	private static bool toggleButtonState;
	private static int flameSelected = -1;

	private static readonly Collection<FlameGraphSample> FlameSamples =
	[
		new(0f, 10f, 0, "Frame"),
		new(0f, 4f, 1, "Update"),
		new(4f, 9.5f, 1, "Render"),
		new(4.2f, 6f, 2, "Cull"),
		new(6f, 9.4f, 2, "Draw"),
	];

	internal static void Show(float size)
	{
		if (ImGui.BeginTabBar("HexaWidgetsTabs"))
		{
			if (ImGui.BeginTabItem("Hexa vs ktsu"))
			{
				ShowComparison();
				ImGui.EndTabItem();
			}

			if (ImGui.BeginTabItem("Net New"))
			{
				ShowNetNew();
				ImGui.EndTabItem();
			}

			ImGui.EndTabBar();
		}
	}

	private static void ShowComparison()
	{
		ImGui.TextWrapped("Both columns of each row are bound to the same value. Change one and the other follows.");
		ImGui.Separator();

		if (!ImGui.BeginTable("HexaComparison", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
		{
			return;
		}

		ImGui.TableSetupColumn("Widget", ImGuiTableColumnFlags.WidthFixed, 140f);
		ImGui.TableSetupColumn("ktsu");
		ImGui.TableSetupColumn("Hexa");
		ImGui.TableHeadersRow();

		BeginRow("Toggle");
		ImGuiWidgets.Switch("##ktsuSwitch", ref sharedToggle);
		ImGui.TableNextColumn();
		ImGuiWidgets.ToggleSwitch("##hexaToggle", ref sharedToggle);

		BeginRow("Enum combo");
		ImGuiWidgets.Combo("##ktsuCombo", ref sharedEnum);
		ImGui.TableNextColumn();
		ImGuiWidgets.EnumCombo("##hexaCombo", ref sharedEnum);

		BeginRow("Tree node");
		using (ImGuiWidgets.Tree ktsuTree = new())
		{
			using (ktsuTree.Child)
			{
				ImGui.TextUnformatted("ktsu tree child");
			}
		}

		ImGui.TableNextColumn();
		// U+E2C7 is Material Icons' Folder glyph; renders as a placeholder box without that font.
		if (ImGuiWidgets.IconTreeNode("Hexa tree", "\uE2C7", Color.FromHex("#e6b333")))
		{
			ImGui.TextUnformatted("Hexa tree child");
			ImGui.TreePop();
		}

		BeginRow("Progress");
		ImGuiWidgets.RadialProgressBar(sharedProgress);
		ImGui.TableNextColumn();
		ImGuiWidgets.BufferingBar(sharedProgress, new Vector2(160f, 12f), new Srgb(0.2f, 0.2f, 0.2f), new Srgb(0.2f, 0.7f, 1f));
		ImGuiWidgets.Spinner(10f, 3f, new Srgb(0.2f, 0.7f, 1f));

		BeginRow("Text centering");
		ImGuiWidgets.TextCentered("ktsu centered");
		ImGui.TableNextColumn();
		ImGuiWidgets.TextCenteredH("Hexa centered");

		BeginRow("Splitter");
		ImGui.TextUnformatted($"DividerContainer: see the Advanced pane ({sharedSplitWidth:F0}px)");
		ImGui.TableNextColumn();
		ImGuiWidgets.VerticalSplitter("##hexaSplitter", ref sharedSplitWidth, 80f, 400f, 40f);
		ImGui.SameLine();
		ImGui.TextUnformatted($"{sharedSplitWidth:F0}px");

		ImGui.EndTable();

		ImGui.Separator();
		ImGui.SliderFloat("Shared progress", ref sharedProgress, 0f, 1f);
	}

	private static void BeginRow(string name)
	{
		ImGui.TableNextRow();
		ImGui.TableNextColumn();
		ImGui.TextUnformatted(name);
		ImGui.TableNextColumn();
	}

	private static void ShowNetNew()
	{
		ImGui.TextWrapped("Hexa widgets with no ktsu counterpart.");
		ImGui.Separator();

		if (ImGui.CollapsingHeader("Breadcrumb"))
		{
			ImGuiWidgets.Breadcrumb("##breadcrumb", ref breadcrumbPath);
			ImGui.TextUnformatted(breadcrumbPath);
		}

		if (ImGui.CollapsingHeader("Buttons"))
		{
			ImGuiWidgets.ToggleButton("Toggle me", ref toggleButtonState);
			ImGui.SameLine();
			if (ImGuiWidgets.TransparentButton("Transparent"))
			{
				toggleButtonState = !toggleButtonState;
			}
		}

		if (ImGui.CollapsingHeader("Date and year pickers"))
		{
			ImGui.TextWrapped("Needs a Material Icons font in the atlas; see ImGuiAppDemo. Without one the calendar button shows a placeholder box.");
			ImGuiWidgets.DatePicker("Date", ref pickedDate);
			ImGuiWidgets.YearPicker("Year", ref pickedYear);
			ImGui.TextUnformatted($"Picked: {pickedDate:yyyy-MM-dd}, year {pickedYear:yyyy}");
		}

		if (ImGui.CollapsingHeader("Flame graph"))
		{
			ImGuiWidgets.FlameGraph("Frame timing", FlameSamples, ref flameSelected,
				new FlameGraphOptions { GraphSize = new Vector2(0f, 120f) });
			ImGui.TextUnformatted($"Selected: {flameSelected}");
		}

		if (ImGui.CollapsingHeader("File tree view"))
		{
			ImGui.TextWrapped("Needs a Material Icons font in the atlas; see ImGuiAppDemo.");
			ImGuiWidgets.FileTreeView("##fileTree", new Vector2(0f, 200f), ref treeFolder, treeFolder);
			ImGui.TextUnformatted(treeFolder.ToString());
		}
	}
}
```

- [ ] **Step 2: Register the zone**

In `examples/ImGuiWidgetsDemo/ImGuiWidgetsDemo.cs`, inside `OnStart`, change the two existing `DividerContainer.Add` calls to make room for a third and add the new zone:

```csharp
		// Create main layout with dedicated demo sections
		DividerContainer.Add(new("Widget Demos", 0.4f, ShowWidgetDemos));
		DividerContainer.Add(new("Advanced Demos", 0.3f, ShowAdvancedDemos));
		DividerContainer.Add(new("Hexa Widgets", 0.3f, HexaWidgetsDemo.Show));
```

- [ ] **Step 3: Build and run**

Run: `dotnet build ImGui.sln -p:KtsuSyncStyleConfigFiles=false`
Expected: SUCCESS.

Run: `dotnet run --project examples/ImGuiWidgetsDemo -p:KtsuSyncStyleConfigFiles=false`
Expected: the window opens with a third pane titled "Hexa Widgets" containing both tabs. Toggle the ktsu switch and confirm the Hexa toggle follows, and vice versa. The date picker's calendar button and the file tree's home icon will show placeholder boxes until Task 15 is done — that is expected here.

If any API in this file does not compile — particularly `ImGuiWidgets.Tree()`, `ImGuiWidgets.RadialProgressBar`, `ImGuiWidgets.TextCentered`, `Color.FromRgb` or `Srgb.FromRgb` — read the actual signature in the corresponding source file and adjust the call. The comparison rows matter; the exact construction syntax does not.

- [ ] **Step 4: Commit**

```bash
git add examples/ImGuiWidgetsDemo/HexaWidgetsDemo.cs examples/ImGuiWidgetsDemo/ImGuiWidgetsDemo.cs
git commit -m "Add Hexa vs ktsu widget comparison demo tab"
```

---

### Task 15: Material Icons font example in ImGuiAppDemo

**Files:**
- Modify: `examples/ImGuiAppDemo/` (the file containing the `ImGuiAppConfig` construction)

**Interfaces:**
- Consumes: Task 2's `FontHelper.GetMaterialIconRanges()`
- Produces: a worked example of registering a Material Icons font

Do **not** use `ImGuiAppConfig.Fonts` for this. That dictionary routes every font through `GetExtendedUnicodeRanges`, which supplies the *Nerd Font* mapping — a Material TTF loaded that way still leaves `\xe9b2` unmapped. The correct path is the public `FontHelper.AddCustomFont`, whose signature is:

```csharp
public static unsafe ImFontPtr? AddCustomFont(ImGuiIOPtr io, byte[] fontData, float fontSize, uint* glyphRanges = null, bool mergeWithPrevious = false);
```

- [ ] **Step 1: Locate the demo's startup hook**

Run: `grep -rn "ImGuiAppConfig\|OnStart" examples/ImGuiAppDemo/*.cs | head -20`

Identify the file and the `OnStart` delegate assigned in the `ImGuiAppConfig` initializer. The font must be merged during startup, after ImGui's context and IO exist but before the atlas is first built.

- [ ] **Step 2: Add the example**

Add this to the demo's `OnStart` body. It is guarded so the demo still runs when the TTF is absent, which it will be by default since the repo ships no font file:

```csharp
		// Material Icons lives in the Unicode Private Use Area (U+E000-U+F8FF), which overlaps the
		// Nerd Font ranges ImGuiApp loads by default -- Material's Computer glyph at U+E31E falls
		// inside the Nerd Font Weather Icons range. A merged font cannot own a codepoint twice, so
		// whichever font is merged last wins the contested span.
		//
		// Note this deliberately does NOT go through ImGuiAppConfig.Fonts: that path applies the
		// Nerd Font glyph ranges, which do not cover Material's codepoints, so the icons would
		// still be missing. Hexa-backed widgets such as ImGuiWidgets.DatePicker and
		// ImGuiWidgets.FileTreeView need this font; without it they render placeholder boxes.
		AbsoluteFilePath materialIconsPath =
			AppContext.BaseDirectory.As<AbsoluteDirectoryPath>() / "MaterialIcons-Regular.ttf".As<FileName>();

		if (File.Exists(materialIconsPath.ToString()))
		{
			ImGuiIOPtr io = ImGui.GetIO();
			byte[] fontData = File.ReadAllBytes(materialIconsPath.ToString());
			unsafe
			{
				FontHelper.AddCustomFont(io, fontData, 16f, FontHelper.GetMaterialIconRanges(), mergeWithPrevious: true);
			}
		}
```

Add whatever using directives the demo file is missing: `System.IO`, `Hexa.NET.ImGui`, `ktsu.ImGui.App`, `ktsu.Semantics.Paths`.

If the demo project does not already set `<AllowUnsafeBlocks>`, add it to `examples/ImGuiAppDemo/ImGuiAppDemo.csproj` — `GetMaterialIconRanges()` returns a `uint*`, so the call site needs an unsafe context.

- [ ] **Step 3: Build and run**

Run: `dotnet build ImGui.sln && dotnet run --project examples/ImGuiAppDemo -p:KtsuSyncStyleConfigFiles=false`
Expected: SUCCESS, and the demo runs whether or not the TTF is present.

To verify the icons actually resolve, drop a `MaterialIcons-Regular.ttf` (Apache 2.0, from the google/material-design-icons repository) next to the demo binary and re-run `examples/ImGuiWidgetsDemo` — the date picker's calendar button and the file tree's home icon should render as glyphs rather than placeholder boxes. Do not commit the TTF.

- [ ] **Step 4: Commit**

```bash
git add examples/ImGuiAppDemo
git commit -m "Add Material Icons font registration example to ImGuiAppDemo"
```

---

### Task 16: Documentation

**Files:**
- Modify: `CLAUDE.md`
- Modify: `ImGui.Widgets/README.md`
- Modify: `ImGui.Widgets/DESCRIPTION.md`

**Interfaces:**
- Consumes: every public member produced by Tasks 3–13
- Produces: nothing consumed by later tasks

Do not edit `VERSION.md`, `CHANGELOG.md` or `LICENSE.md` — they are auto-generated.

- [ ] **Step 1: Update CLAUDE.md**

In the **Libraries** section, extend the `ImGui.Widgets` bullet to name the Hexa-backed additions: `Spinner`, `BufferingBar`, `HorizontalSplitter`/`VerticalSplitter`, `IconTreeNode`, `ToggleSwitch`, `ToggleButton`, `TransparentButton`, `InlineButton`, `EnumCombo`, `TextCenteredV/H/VH`, `ImageCenteredV/H/VH`, `ImageScaleTo`, `Tooltip`, `Breadcrumb`, `DatePicker`, `YearPicker`, `FlameGraph`, `FileTreeView` — noting they delegate to `Hexa.NET.ImGui.Widgets`, and that `DatePicker` and `FileTreeView` need a Material Icons font.

In the **Dependencies** section, add:

```
- **Hexa.NET.ImGui.Widgets** (1.2.18) - Upstream widget collection backing the Hexa-delegated widgets
- **Hexa.NET.ImGui.Widgets.Extras** (1.0.9) - Curve editor, bezier and text editor extras
```

In **Key Files**, add `ImGui.Widgets/FlameGraph.cs` and `ImGui.Widgets/Splitter.cs`.

- [ ] **Step 2: Update the widget README and DESCRIPTION**

Add the new widgets to `ImGui.Widgets/README.md`'s widget list with a one-line description each, and mention the Material Icons requirement for `DatePicker` and `FileTreeView`. Update `ImGui.Widgets/DESCRIPTION.md` only if it enumerates widgets; if it is a single summary paragraph, leave it alone.

- [ ] **Step 3: Verify the whole solution still builds and tests pass**

Run: `dotnet build ImGui.sln && dotnet run --project tests/ImGui.Widgets.Tests/ImGui.Widgets.Tests.csproj -p:KtsuSyncStyleConfigFiles=false`
Expected: SUCCESS, all tests pass.

- [ ] **Step 4: Commit**

```bash
git add CLAUDE.md ImGui.Widgets/README.md ImGui.Widgets/DESCRIPTION.md
git commit -m "Document Hexa-backed widgets"
```

---

## Notes for the executor

- **Do not** wire `AddMaterialIconRanges` into `GetExtendedUnicodeRanges`. It would break the Nerd Font icons that already work. Task 2 explains why.
- **Do not** rename or delete any existing ktsu widget that a Hexa widget duplicates. Deciding which to keep happens after the comparison demo and is a separate, breaking change.
- **Guard clauses must run before any ImGui call.** Several tests depend on this — they assert an exception from a process with no ImGui context, which only works if validation happens first.
- If a Hexa signature does not match what this plan shows, trust the source at `C:\dev\HexaEngine\Hexa.NET.ImGui.Widgets` over this document and note the discrepancy in the commit message.
