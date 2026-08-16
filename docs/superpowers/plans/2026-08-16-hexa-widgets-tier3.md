# Hexa Widgets Tier 3 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Wrap Hexa's callback-driven editors — the sequencer, the multi-curve editor, and the two Extras curve fields — behind a ktsu-idiomatic API with no vendor type and no `unsafe` in the public surface.

**Architecture:** Composition throughout, matching Tier 2's `DockedWindow`. Public abstract `SequenceSource` and `CurveSource` carry ktsu-named managed members; a private adapter subclasses Hexa's abstract base and forwards to them. The sequencer's `int**` pointer contract is satisfied by a buffer the static entry point pins with `fixed` for exactly one call, so no disposal contract appears. Logic is deliberately pushed out of the untestable draw path into pure functions.

**Tech Stack:** C# / .NET 10-9-8 multi-target, Hexa.NET.ImGui.Widgets 1.2.18, Hexa.NET.ImGui.Widgets.Extras 1.0.9, Hexa.NET.Math 2.0.6 (namespace `Hexa.NET.Mathematics`), ktsu.Semantics.Color, MSTest.Sdk on Microsoft Testing Platform.

**Spec:** `docs/superpowers/specs/2026-08-16-hexa-widgets-tier3-design.md`

## Global Constraints

- **Every build/test command MUST pass `-p:KtsuSyncStyleConfigFiles=false`.** Without it ktsu.Sdk rewrites `.editorconfig` mid-build and IDE0073 fires non-deterministically.
- **`dotnet test` is broken here.** It reports "Zero tests ran" / exit 5 against these MSTest.Sdk-on-MTP projects. Use `dotnet run --project <testproj>`.
- **Never use `--filter`.** MTP does not accept VSTest filter syntax and silently matches nothing, which reads like a pass.
- **Line endings are LF.** The repo is `* text=auto eol=lf`; a CRLF file fails IDE0055 on every line.
- **File header, exactly:** `// Copyright (c) 2023-2026 ktsu-dev contributors`
- **Code style:** tabs, file-scoped namespaces, usings inside the namespace, explicit types (no `var`), always braces, no `this.`, accessibility modifiers always, XML docs on public and internal members.
- **Validation:** `Ensure.NotNull(param)` from Polyfill.
- **No `unsafe` in public signatures. No Hexa type in the public surface** — including base types, constructors, and `protected` members.
- **No global suppressions.** Targeted `[SuppressMessage]` with a specific justification only. A type-level CA1506 suppression already exists on the `EnumCombo.cs` partial and covers the whole merged `ImGuiWidgets` type.
- **Prefer PowerShell** for shell commands in this environment.
- Baseline before Task 1: **177 tests passing**, solution builds 0 warnings / 0 errors.

## Upstream facts every task depends on

Verified against `C:\dev\HexaEngine\` during planning. Do not re-derive; do verify before relying on anything not listed here.

- Namespace for the math types is **`Hexa.NET.Mathematics`**. `Hexa.NET.Math` is the package name.
- **Two different enums are called `CurveType`.** `ImCurveEdit.CurveType` is `{ None, CurveDiscrete, CurveLinear, CurveSmooth, CurveBezier }`; `Mathematics.CurveType` is `{ Smooth, Freehand }`. They get separate mirrors.
- `SequenceInterface.Get(int index, int** start, int** end, int* type, uint* color)` is called from three sites with three different null combinations: `ImSequencer.cs:362` `(i, null, null, &type, null)`, `:431` `(i, &start, &end, null, &color)`, `:531` `(MovingEntry, &start, &end, null, null)`.
- `ImSequencer.Sequencer(SequenceInterface, ref int currentFrame, ref bool expanded, ref int selectedEntry, ref int firstFrame, SequencerOptions)` returns `bool`.
- `ImCurveEdit.Edit(CurveContext ctx, Vector2 size, uint id, ImVector<EditPoint>* selectedPoints = null)` returns `int`, set to `1` when anything changed.
- `CurveContext.Min` / `.Max` are **inputs**; `Edit` computes `Range = Max - Min` from them.
- `ImGuiCurveEditor.Curve(string label, Vector2 size, ref Curve curve, Vector2 rangeMin, Vector2 rangeMax, ref int selection)` returns `bool`.
- `ImGuiBezierWidget.Bezier(string label, ref BezierCurve P, float size = 128, float curveWidth = 4, float lineWidth = 1, float grabRadius = 8, float grabBorder = 2, bool areaConstrained = true)` returns `bool`.
- `BezierCurve` is `[InlineArray(2)]` over `Vector2` — indexed `this[0]`, `this[1]`, no named fields. Guarded by `#if NET8_0_OR_GREATER`, which all our target frameworks satisfy.
- `Mathematics.CurvePoint` has `float X`, `float Y`, `CurvePointType Type`, and a `Vector2 Pos` property. `CurvePointType` is `{ Smooth, Corner }`.
- `Mathematics.Curve` has `List<CurvePoint> Points`, `float[] Samples`, `CurveType Type`, `void Compute()`, and `static void CalculateCurve(ref Curve)`.
- `SequencerOptions` is `{ EditNone = 0, EditStartend = 1 << 1, ChangeFrame = 1 << 3, Add = 1 << 4, Del = 1 << 5, Copypaste = 1 << 6, EditAll = EditStartend | ChangeFrame }`. Note the gaps at `1 << 0` and `1 << 2`; it is not marked `[Flags]` upstream.

---

### Task 1: Enum mirrors and their mappings

Everything else consumes these. Pure and fully testable.

**Files:**
- Create: `ImGui.Widgets/Editors/EditorEnums.cs`
- Test: `tests/ImGui.Widgets.Tests/EditorEnumTests.cs`

**Interfaces:**
- Consumes: nothing.
- Produces: `CurveInterpolation`, `CurveShape`, `CurvePointKind`, `SequencerFeatures`; and `internal static` mappers on `ImGuiWidgets`: `MapInterpolation`, `MapInterpolationBack`, `MapShape`, `MapShapeBack`, `MapPointKind`, `MapPointKindBack`, `MapFeatures`.

- [ ] **Step 1: Write the failing tests**

Create `tests/ImGui.Widgets.Tests/EditorEnumTests.cs`:

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using HexaCurveEditType = Hexa.NET.ImGui.Widgets.ImCurveEdit.CurveType;
using HexaCurvePointType = Hexa.NET.Mathematics.CurvePointType;
using HexaMathCurveType = Hexa.NET.Mathematics.CurveType;
using HexaSequencerOptions = Hexa.NET.ImGui.Widgets.ImSequencer.SequencerOptions;

/// <summary>
/// Tests the enum mirrors for the Tier 3 editors. Two different upstream enums are both named
/// CurveType, so these tests also pin that the two mirrors do not cross over.
/// </summary>
[TestClass]
public sealed class EditorEnumTests
{
	[TestMethod]
	public void MapInterpolation_CoversEveryMember()
	{
		Assert.AreEqual(HexaCurveEditType.None, ImGuiWidgets.MapInterpolation(CurveInterpolation.None));
		Assert.AreEqual(HexaCurveEditType.CurveDiscrete, ImGuiWidgets.MapInterpolation(CurveInterpolation.Discrete));
		Assert.AreEqual(HexaCurveEditType.CurveLinear, ImGuiWidgets.MapInterpolation(CurveInterpolation.Linear));
		Assert.AreEqual(HexaCurveEditType.CurveSmooth, ImGuiWidgets.MapInterpolation(CurveInterpolation.Smooth));
		Assert.AreEqual(HexaCurveEditType.CurveBezier, ImGuiWidgets.MapInterpolation(CurveInterpolation.Bezier));
	}

	[TestMethod]
	public void MapInterpolation_RoundTripsEveryMember()
	{
		foreach (CurveInterpolation value in Enum.GetValues<CurveInterpolation>())
		{
			Assert.AreEqual(value, ImGuiWidgets.MapInterpolationBack(ImGuiWidgets.MapInterpolation(value)));
		}
	}

	[TestMethod]
	public void MapInterpolationBack_UnknownValue_FallsBackToLinear() =>
		Assert.AreEqual(CurveInterpolation.Linear, ImGuiWidgets.MapInterpolationBack((HexaCurveEditType)99));

	[TestMethod]
	public void MapShape_CoversEveryMember()
	{
		Assert.AreEqual(HexaMathCurveType.Smooth, ImGuiWidgets.MapShape(CurveShape.Smooth));
		Assert.AreEqual(HexaMathCurveType.Freehand, ImGuiWidgets.MapShape(CurveShape.Freehand));
	}

	[TestMethod]
	public void MapShape_RoundTripsEveryMember()
	{
		foreach (CurveShape value in Enum.GetValues<CurveShape>())
		{
			Assert.AreEqual(value, ImGuiWidgets.MapShapeBack(ImGuiWidgets.MapShape(value)));
		}
	}

	[TestMethod]
	public void ShapeAndInterpolation_DoNotShareAMapping()
	{
		// Both upstream enums are named CurveType. CurveShape.Smooth is Mathematics.CurveType.Smooth = 0,
		// while CurveInterpolation.Smooth is ImCurveEdit.CurveType.CurveSmooth = 3. A shared mirror
		// would map one of them wrong; this pins that they are genuinely separate.
		Assert.AreEqual(0, (int)ImGuiWidgets.MapShape(CurveShape.Smooth));
		Assert.AreEqual(3, (int)ImGuiWidgets.MapInterpolation(CurveInterpolation.Smooth));
	}

	[TestMethod]
	public void MapPointKind_RoundTripsEveryMember()
	{
		foreach (CurvePointKind value in Enum.GetValues<CurvePointKind>())
		{
			Assert.AreEqual(value, ImGuiWidgets.MapPointKindBack(ImGuiWidgets.MapPointKind(value)));
		}

		Assert.AreEqual(HexaCurvePointType.Corner, ImGuiWidgets.MapPointKind(CurvePointKind.Corner));
	}

	[TestMethod]
	public void MapFeatures_PreservesUpstreamNumericGaps()
	{
		// Upstream skips 1<<0 and 1<<2. A naive cast would still work only if our values match
		// exactly, so assert the numbers rather than trusting the names.
		Assert.AreEqual(HexaSequencerOptions.EditNone, ImGuiWidgets.MapFeatures(SequencerFeatures.None));
		Assert.AreEqual(HexaSequencerOptions.EditStartend, ImGuiWidgets.MapFeatures(SequencerFeatures.EditStartEnd));
		Assert.AreEqual(HexaSequencerOptions.ChangeFrame, ImGuiWidgets.MapFeatures(SequencerFeatures.ChangeFrame));
		Assert.AreEqual(HexaSequencerOptions.Add, ImGuiWidgets.MapFeatures(SequencerFeatures.Add));
		Assert.AreEqual(HexaSequencerOptions.Del, ImGuiWidgets.MapFeatures(SequencerFeatures.Delete));
		Assert.AreEqual(HexaSequencerOptions.Copypaste, ImGuiWidgets.MapFeatures(SequencerFeatures.CopyPaste));
	}

	[TestMethod]
	public void MapFeatures_CombinedFlags_MapToCombinedUpstream() =>
		Assert.AreEqual(HexaSequencerOptions.EditAll, ImGuiWidgets.MapFeatures(SequencerFeatures.EditAll));

	[TestMethod]
	public void MapFeatures_ArbitraryCombination_PreservesEveryBit() =>
		Assert.AreEqual(
			HexaSequencerOptions.Add | HexaSequencerOptions.Del | HexaSequencerOptions.ChangeFrame,
			ImGuiWidgets.MapFeatures(SequencerFeatures.Add | SequencerFeatures.Delete | SequencerFeatures.ChangeFrame));
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet run --project tests/ImGui.Widgets.Tests/ImGui.Widgets.Tests.csproj -p:KtsuSyncStyleConfigFiles=false`
Expected: compile failure — none of the enums or mappers exist.

- [ ] **Step 3: Write the implementation**

Create `ImGui.Widgets/Editors/EditorEnums.cs`:

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using HexaCurveEditType = Hexa.NET.ImGui.Widgets.ImCurveEdit.CurveType;
using HexaCurvePointType = Hexa.NET.Mathematics.CurvePointType;
using HexaMathCurveType = Hexa.NET.Mathematics.CurveType;
using HexaSequencerOptions = Hexa.NET.ImGui.Widgets.ImSequencer.SequencerOptions;

/// <summary>
/// How a curve is interpolated between its points.
/// </summary>
public enum CurveInterpolation
{
	/// <summary>No interpolation.</summary>
	None,

	/// <summary>Steps between points without interpolating.</summary>
	Discrete,

	/// <summary>Straight lines between points.</summary>
	Linear,

	/// <summary>Smoothed through the points.</summary>
	Smooth,

	/// <summary>Bezier segments between points.</summary>
	Bezier,
}

/// <summary>
/// How an authored curve is shaped.
/// </summary>
public enum CurveShape
{
	/// <summary>Smoothed through the authored points.</summary>
	Smooth,

	/// <summary>Follows the authored points exactly.</summary>
	Freehand,
}

/// <summary>
/// Whether a curve point smooths through or forms a corner.
/// </summary>
public enum CurvePointKind
{
	/// <summary>The curve passes smoothly through the point.</summary>
	Smooth,

	/// <summary>The curve forms a corner at the point.</summary>
	Corner,
}

/// <summary>
/// Which sequencer interactions are enabled.
/// </summary>
[Flags]
public enum SequencerFeatures
{
	/// <summary>No editing.</summary>
	None = 0,

	/// <summary>Clip start and end may be dragged.</summary>
	EditStartEnd = 1 << 1,

	/// <summary>The current frame may be scrubbed.</summary>
	ChangeFrame = 1 << 3,

	/// <summary>Items may be added.</summary>
	Add = 1 << 4,

	/// <summary>Items may be deleted.</summary>
	Delete = 1 << 5,

	/// <summary>Items may be copied and pasted.</summary>
	CopyPaste = 1 << 6,

	/// <summary>Both editing interactions.</summary>
	EditAll = EditStartEnd | ChangeFrame,
}

public static partial class ImGuiWidgets
{
	/// <summary>
	/// Converts an interpolation mode to Hexa's curve-edit type.
	/// </summary>
	/// <param name="value">The interpolation mode.</param>
	/// <returns>The equivalent Hexa value.</returns>
	internal static HexaCurveEditType MapInterpolation(CurveInterpolation value) => value switch
	{
		CurveInterpolation.None => HexaCurveEditType.None,
		CurveInterpolation.Discrete => HexaCurveEditType.CurveDiscrete,
		CurveInterpolation.Smooth => HexaCurveEditType.CurveSmooth,
		CurveInterpolation.Bezier => HexaCurveEditType.CurveBezier,
		_ => HexaCurveEditType.CurveLinear,
	};

	/// <summary>
	/// Converts Hexa's curve-edit type to an interpolation mode.
	/// </summary>
	/// <param name="value">The Hexa value.</param>
	/// <returns>The equivalent interpolation mode.</returns>
	internal static CurveInterpolation MapInterpolationBack(HexaCurveEditType value) => value switch
	{
		HexaCurveEditType.None => CurveInterpolation.None,
		HexaCurveEditType.CurveDiscrete => CurveInterpolation.Discrete,
		HexaCurveEditType.CurveSmooth => CurveInterpolation.Smooth,
		HexaCurveEditType.CurveBezier => CurveInterpolation.Bezier,
		_ => CurveInterpolation.Linear,
	};

	/// <summary>
	/// Converts a curve shape to Hexa's mathematics curve type.
	/// </summary>
	/// <param name="value">The curve shape.</param>
	/// <returns>The equivalent Hexa value.</returns>
	/// <remarks>
	/// Deliberately separate from <see cref="MapInterpolation"/>. Hexa has two unrelated enums
	/// named <c>CurveType</c>, and neither member set is a subset of the other.
	/// </remarks>
	internal static HexaMathCurveType MapShape(CurveShape value) =>
		value == CurveShape.Freehand ? HexaMathCurveType.Freehand : HexaMathCurveType.Smooth;

	/// <summary>
	/// Converts Hexa's mathematics curve type to a curve shape.
	/// </summary>
	/// <param name="value">The Hexa value.</param>
	/// <returns>The equivalent curve shape.</returns>
	internal static CurveShape MapShapeBack(HexaMathCurveType value) =>
		value == HexaMathCurveType.Freehand ? CurveShape.Freehand : CurveShape.Smooth;

	/// <summary>
	/// Converts a point kind to Hexa's curve point type.
	/// </summary>
	/// <param name="value">The point kind.</param>
	/// <returns>The equivalent Hexa value.</returns>
	internal static HexaCurvePointType MapPointKind(CurvePointKind value) =>
		value == CurvePointKind.Corner ? HexaCurvePointType.Corner : HexaCurvePointType.Smooth;

	/// <summary>
	/// Converts Hexa's curve point type to a point kind.
	/// </summary>
	/// <param name="value">The Hexa value.</param>
	/// <returns>The equivalent point kind.</returns>
	internal static CurvePointKind MapPointKindBack(HexaCurvePointType value) =>
		value == HexaCurvePointType.Corner ? CurvePointKind.Corner : CurvePointKind.Smooth;

	/// <summary>
	/// Converts sequencer features to Hexa's options.
	/// </summary>
	/// <param name="value">The features to enable.</param>
	/// <returns>The equivalent Hexa options.</returns>
	/// <remarks>
	/// The numeric values are chosen to match upstream exactly, including its unused bits at
	/// <c>1 &lt;&lt; 0</c> and <c>1 &lt;&lt; 2</c>, so the cast is faithful for arbitrary combinations.
	/// </remarks>
	internal static HexaSequencerOptions MapFeatures(SequencerFeatures value) => (HexaSequencerOptions)(int)value;
}
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet run --project tests/ImGui.Widgets.Tests/ImGui.Widgets.Tests.csproj -p:KtsuSyncStyleConfigFiles=false`
Expected: PASS, 10 new tests (187 total).

- [ ] **Step 5: Commit**

```powershell
git add ImGui.Widgets/Editors/EditorEnums.cs tests/ImGui.Widgets.Tests/EditorEnumTests.cs
git commit -m "Add the Tier 3 editor enum mirrors and their mappings"
```

---

### Task 2: CurveData

Wraps Hexa's `Curve` struct so the vendor type never reaches the public surface. Pure and fully testable — no ImGui context needed.

**Files:**
- Create: `ImGui.Widgets/Editors/CurveData.cs`
- Test: `tests/ImGui.Widgets.Tests/CurveDataTests.cs`

**Interfaces:**
- Consumes: `CurveShape`, `CurvePointKind`, `MapShape`, `MapShapeBack`, `MapPointKind`, `MapPointKindBack` (Task 1).
- Produces: `CurveKnot` record struct, `CurveData` class, and `internal ref Hexa.NET.Mathematics.Curve CurveData.AsVendorCurve()` for Task 4's entry point.

**Why a wrapper rather than a mirror.** `Mathematics.Curve` holds `List<CurvePoint> Points`, a `float[] Samples` cache and `CurveType Type`, with `Compute()` filling the cache. Mirroring would mean duplicating the curve maths or converting a list plus an array every frame. Wrapping costs neither.

**The stale-cache trap.** `Samples` goes stale on every point edit and upstream expects the caller to call `Compute()`. Making consumers remember that is the kind of contract this suite removes rather than adds, so `Sample` recomputes lazily via a dirty flag.

- [ ] **Step 1: Write the failing tests**

Create `tests/ImGui.Widgets.Tests/CurveDataTests.cs`:

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using System.Numerics;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests the managed wrapper over Hexa's Curve struct. All pure — no ImGui context required.
/// </summary>
[TestClass]
public sealed class CurveDataTests
{
	[TestMethod]
	public void NewCurve_IsEmpty()
	{
		CurveData curve = new();

		Assert.AreEqual(0, curve.PointCount);
	}

	[TestMethod]
	public void AddPoint_IncreasesCountAndRoundTrips()
	{
		CurveData curve = new();
		CurveKnot knot = new(new Vector2(0.25f, 0.75f), CurvePointKind.Corner);

		curve.AddPoint(knot);

		Assert.AreEqual(1, curve.PointCount);
		Assert.AreEqual(knot, curve.GetPoint(0));
	}

	[TestMethod]
	public void AddPoint_PreservesPointKind()
	{
		// Bare Vector2 points would silently discard this; corner-vs-smooth is real authoring data.
		CurveData curve = new();
		curve.AddPoint(new CurveKnot(Vector2.Zero, CurvePointKind.Corner));
		curve.AddPoint(new CurveKnot(Vector2.One, CurvePointKind.Smooth));

		Assert.AreEqual(CurvePointKind.Corner, curve.GetPoint(0).Kind);
		Assert.AreEqual(CurvePointKind.Smooth, curve.GetPoint(1).Kind);
	}

	[TestMethod]
	public void SetPoint_ReplacesInPlace()
	{
		CurveData curve = new();
		curve.AddPoint(new CurveKnot(Vector2.Zero, CurvePointKind.Smooth));

		curve.SetPoint(0, new CurveKnot(new Vector2(1f, 2f), CurvePointKind.Corner));

		Assert.AreEqual(1, curve.PointCount);
		Assert.AreEqual(new CurveKnot(new Vector2(1f, 2f), CurvePointKind.Corner), curve.GetPoint(0));
	}

	[TestMethod]
	public void RemovePoint_DropsOnlyThatPoint()
	{
		CurveData curve = new();
		curve.AddPoint(new CurveKnot(new Vector2(0f, 0f), CurvePointKind.Smooth));
		curve.AddPoint(new CurveKnot(new Vector2(1f, 1f), CurvePointKind.Smooth));

		curve.RemovePoint(0);

		Assert.AreEqual(1, curve.PointCount);
		Assert.AreEqual(new Vector2(1f, 1f), curve.GetPoint(0).Position);
	}

	[TestMethod]
	public void Clear_EmptiesTheCurve()
	{
		CurveData curve = new();
		curve.AddPoint(new CurveKnot(Vector2.Zero, CurvePointKind.Smooth));

		curve.Clear();

		Assert.AreEqual(0, curve.PointCount);
	}

	[TestMethod]
	public void Shape_RoundTrips()
	{
		CurveData curve = new()
		{
			Shape = CurveShape.Freehand,
		};

		Assert.AreEqual(CurveShape.Freehand, curve.Shape);
	}

	[TestMethod]
	public void GetPoint_OutOfRange_Throws()
	{
		CurveData curve = new();

		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => curve.GetPoint(0));
	}

	[TestMethod]
	public void ConstructedFromPoints_KeepsThemInOrder()
	{
		CurveData curve = new(
		[
			new CurveKnot(new Vector2(0f, 0f), CurvePointKind.Smooth),
			new CurveKnot(new Vector2(1f, 1f), CurvePointKind.Corner),
		]);

		Assert.AreEqual(2, curve.PointCount);
		Assert.AreEqual(CurvePointKind.Corner, curve.GetPoint(1).Kind);
	}

	[TestMethod]
	public void Sample_AfterEdit_ReflectsTheEdit()
	{
		// Upstream's Samples cache goes stale on edit and expects a manual Compute(). This pins
		// that the wrapper recomputes, rather than returning a stale sample.
		CurveData curve = new();
		curve.AddPoint(new CurveKnot(new Vector2(0f, 0f), CurvePointKind.Smooth));
		curve.AddPoint(new CurveKnot(new Vector2(1f, 1f), CurvePointKind.Smooth));
		float before = curve.Sample(0.5f);

		curve.SetPoint(1, new CurveKnot(new Vector2(1f, 10f), CurvePointKind.Smooth));
		float after = curve.Sample(0.5f);

		Assert.AreNotEqual(before, after);
	}
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet run --project tests/ImGui.Widgets.Tests/ImGui.Widgets.Tests.csproj -p:KtsuSyncStyleConfigFiles=false`
Expected: compile failure — `CurveData` and `CurveKnot` do not exist.

- [ ] **Step 3: Write the implementation**

Create `ImGui.Widgets/Editors/CurveData.cs`:

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System.Collections.Generic;
using System.Numerics;

using HexaCurve = Hexa.NET.Mathematics.Curve;
using HexaCurvePoint = Hexa.NET.Mathematics.CurvePoint;

/// <summary>
/// A single authored curve point.
/// </summary>
/// <param name="Position">Where the point sits.</param>
/// <param name="Kind">Whether the curve smooths through the point or forms a corner at it.</param>
public readonly record struct CurveKnot(Vector2 Position, CurvePointKind Kind);

/// <summary>
/// An editable curve. Wraps the curve representation the underlying widget expects, so no vendor
/// type appears in this library's public surface.
/// </summary>
/// <remarks>
/// <see cref="Sample"/> recomputes the underlying sample cache when points have changed, so
/// callers never have to remember to do it themselves.
/// </remarks>
public sealed class CurveData
{
	private HexaCurve curve = new();
	private bool needsCompute = true;

	/// <summary>
	/// Initializes a new instance of the <see cref="CurveData"/> class with no points.
	/// </summary>
	public CurveData()
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="CurveData"/> class from existing points.
	/// </summary>
	/// <param name="points">The points, in order.</param>
	/// <param name="shape">How the curve is shaped between points.</param>
	/// <exception cref="ArgumentNullException"><paramref name="points"/> is <see langword="null"/>.</exception>
	public CurveData(IEnumerable<CurveKnot> points, CurveShape shape = CurveShape.Smooth)
	{
		Ensure.NotNull(points);

		foreach (CurveKnot point in points)
		{
			curve.Points.Add(ToVendor(point));
		}

		curve.Type = MapShape(shape);
	}

	/// <summary>
	/// Gets the number of points on the curve.
	/// </summary>
	public int PointCount => curve.Points.Count;

	/// <summary>
	/// Gets or sets how the curve is shaped between its points.
	/// </summary>
	public CurveShape Shape
	{
		get => MapShapeBack(curve.Type);
		set
		{
			curve.Type = MapShape(value);
			needsCompute = true;
		}
	}

	/// <summary>
	/// Gets a point.
	/// </summary>
	/// <param name="index">Index of the point.</param>
	/// <returns>The point.</returns>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is outside the curve.</exception>
	public CurveKnot GetPoint(int index)
	{
		ThrowIfOutOfRange(index);
		return FromVendor(curve.Points[index]);
	}

	/// <summary>
	/// Appends a point.
	/// </summary>
	/// <param name="point">The point to add.</param>
	public void AddPoint(CurveKnot point)
	{
		curve.Points.Add(ToVendor(point));
		needsCompute = true;
	}

	/// <summary>
	/// Replaces a point.
	/// </summary>
	/// <param name="index">Index of the point to replace.</param>
	/// <param name="point">The replacement.</param>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is outside the curve.</exception>
	public void SetPoint(int index, CurveKnot point)
	{
		ThrowIfOutOfRange(index);
		curve.Points[index] = ToVendor(point);
		needsCompute = true;
	}

	/// <summary>
	/// Removes a point.
	/// </summary>
	/// <param name="index">Index of the point to remove.</param>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is outside the curve.</exception>
	public void RemovePoint(int index)
	{
		ThrowIfOutOfRange(index);
		curve.Points.RemoveAt(index);
		needsCompute = true;
	}

	/// <summary>
	/// Removes every point.
	/// </summary>
	public void Clear()
	{
		curve.Points.Clear();
		needsCompute = true;
	}

	/// <summary>
	/// Samples the computed curve.
	/// </summary>
	/// <param name="t">Position along the curve, from 0 to 1.</param>
	/// <returns>The sampled value, or zero if the curve has no points.</returns>
	public float Sample(float t)
	{
		EnsureComputed();

		if (curve.Samples is null || curve.Samples.Length == 0)
		{
			return 0f;
		}

		int index = (int)(Math.Clamp(t, 0f, 1f) * (curve.Samples.Length - 1));
		return curve.Samples[index];
	}

	/// <summary>
	/// Gets a reference to the underlying curve for the editor to mutate in place.
	/// </summary>
	/// <returns>A reference to the wrapped curve.</returns>
	/// <remarks>
	/// Only for the editor entry point. Callers must invoke <see cref="MarkDirty"/> afterwards,
	/// because the editor edits points directly and the sample cache goes stale.
	/// </remarks>
	internal ref HexaCurve AsVendorCurve() => ref curve;

	/// <summary>
	/// Marks the sample cache stale after an external edit.
	/// </summary>
	internal void MarkDirty() => needsCompute = true;

	/// <summary>
	/// Converts a managed point to the vendor representation.
	/// </summary>
	/// <param name="point">The managed point.</param>
	/// <returns>The vendor point.</returns>
	private static HexaCurvePoint ToVendor(CurveKnot point) =>
		new(point.Position, MapPointKind(point.Kind));

	/// <summary>
	/// Converts a vendor point to the managed representation.
	/// </summary>
	/// <param name="point">The vendor point.</param>
	/// <returns>The managed point.</returns>
	private static CurveKnot FromVendor(HexaCurvePoint point) =>
		new(point.Pos, MapPointKindBack(point.Type));

	/// <summary>
	/// Recomputes the sample cache if anything changed since the last computation.
	/// </summary>
	private void EnsureComputed()
	{
		if (!needsCompute)
		{
			return;
		}

		curve.Compute();
		needsCompute = false;
	}

	/// <summary>
	/// Throws when an index falls outside the curve.
	/// </summary>
	/// <param name="index">The index to check.</param>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is outside the curve.</exception>
	private void ThrowIfOutOfRange(int index)
	{
		if (index < 0 || index >= curve.Points.Count)
		{
			throw new ArgumentOutOfRangeException(nameof(index), index, $"The curve has {curve.Points.Count} points.");
		}
	}
}
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet run --project tests/ImGui.Widgets.Tests/ImGui.Widgets.Tests.csproj -p:KtsuSyncStyleConfigFiles=false`
Expected: PASS, 10 new tests (197 total).

If `Sample_AfterEdit_ReflectsTheEdit` fails because a two-point smooth curve samples identically at 0.5 regardless of the second point, adjust the test to compare `Sample(1f)` instead — the assertion that matters is that an edit is reflected, not which sample position reveals it. Say so in your report if you change it.

- [ ] **Step 5: Commit**

```powershell
git add ImGui.Widgets/Editors/CurveData.cs tests/ImGui.Widgets.Tests/CurveDataTests.cs
git commit -m "Add CurveData wrapping the vendor curve representation"
```

---

### Task 3: BezierControlPoints and the bezier editor

The simplest widget in the tier. Immediate-mode static, no callback object.

**Files:**
- Create: `ImGui.Widgets/Editors/BezierEditor.cs`
- Test: `tests/ImGui.Widgets.Tests/BezierEditorTests.cs`

**Interfaces:**
- Consumes: nothing from earlier tasks.
- Produces: `BezierControlPoints` record struct; `ImGuiWidgets.BezierEditor(string, ref BezierControlPoints, float)`; `internal static` converters `ToVendorBezier` / `FromVendorBezier`.

**Upstream shape.** `BezierCurve` is `[InlineArray(2)]` over `Vector2`, so its control points are reached as `this[0]` and `this[1]` — there are no named `P0`/`P1` fields. Construct it with `new BezierCurve(p0, p1)` and read it back by indexer.

- [ ] **Step 1: Write the failing tests**

Create `tests/ImGui.Widgets.Tests/BezierEditorTests.cs`:

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using System.Numerics;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests the bezier control-point conversion. The widget itself needs a live ImGui context.
/// </summary>
[TestClass]
public sealed class BezierEditorTests
{
	[TestMethod]
	public void ToVendor_RoundTripsBothControlPoints()
	{
		BezierControlPoints points = new(new Vector2(0.1f, 0.2f), new Vector2(0.8f, 0.9f));

		BezierControlPoints result = ImGuiWidgets.FromVendorBezier(ImGuiWidgets.ToVendorBezier(points));

		Assert.AreEqual(points, result);
	}

	[TestMethod]
	public void ToVendor_KeepsControlPointOrder()
	{
		// The vendor type is an inline array; swapping the two would round-trip through an
		// equality check that only compared the set, so assert each slot individually.
		BezierControlPoints points = new(new Vector2(1f, 2f), new Vector2(3f, 4f));

		BezierControlPoints result = ImGuiWidgets.FromVendorBezier(ImGuiWidgets.ToVendorBezier(points));

		Assert.AreEqual(new Vector2(1f, 2f), result.First);
		Assert.AreEqual(new Vector2(3f, 4f), result.Second);
	}

	[TestMethod]
	public void DefaultControlPoints_AreZero()
	{
		BezierControlPoints points = default;

		Assert.AreEqual(Vector2.Zero, points.First);
		Assert.AreEqual(Vector2.Zero, points.Second);
	}
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet run --project tests/ImGui.Widgets.Tests/ImGui.Widgets.Tests.csproj -p:KtsuSyncStyleConfigFiles=false`
Expected: compile failure — `BezierControlPoints` does not exist.

- [ ] **Step 3: Write the implementation**

Create `ImGui.Widgets/Editors/BezierEditor.cs`:

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System.Numerics;

using HexaBezierCurve = Hexa.NET.Mathematics.BezierCurve;
using HexaBezierWidget = Hexa.NET.ImGui.Widgets.Extras.ImGuiBezierWidget;

/// <summary>
/// The two control points of a cubic easing curve.
/// </summary>
/// <param name="First">The first control point.</param>
/// <param name="Second">The second control point.</param>
public readonly record struct BezierControlPoints(Vector2 First, Vector2 Second);

public static partial class ImGuiWidgets
{
	/// <summary>
	/// Draws an editable bezier easing curve.
	/// </summary>
	/// <param name="label">Label for display and identity.</param>
	/// <param name="points">The control points, updated in place when dragged.</param>
	/// <param name="size">Edge length of the square editor in pixels.</param>
	/// <returns><see langword="true"/> if a control point moved this frame.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="label"/> is <see langword="null"/>.</exception>
	public static bool BezierEditor(string label, ref BezierControlPoints points, float size = 128f)
	{
		Ensure.NotNull(label);

		HexaBezierCurve curve = ToVendorBezier(points);
		bool changed = HexaBezierWidget.Bezier(label, ref curve, size);
		if (changed)
		{
			points = FromVendorBezier(curve);
		}

		return changed;
	}

	/// <summary>
	/// Converts managed control points to the vendor curve.
	/// </summary>
	/// <param name="points">The managed control points.</param>
	/// <returns>The vendor curve.</returns>
	internal static HexaBezierCurve ToVendorBezier(BezierControlPoints points) =>
		new(points.First, points.Second);

	/// <summary>
	/// Converts a vendor curve to managed control points.
	/// </summary>
	/// <param name="curve">The vendor curve.</param>
	/// <returns>The managed control points.</returns>
	/// <remarks>
	/// The vendor type is an inline array of two vectors, so its control points are reached by
	/// index rather than by name.
	/// </remarks>
	internal static BezierControlPoints FromVendorBezier(HexaBezierCurve curve) =>
		new(curve[0], curve[1]);
}
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet run --project tests/ImGui.Widgets.Tests/ImGui.Widgets.Tests.csproj -p:KtsuSyncStyleConfigFiles=false`
Expected: PASS, 3 new tests (200 total).

If the compiler rejects indexing `HexaBezierCurve` because `[InlineArray]` indexing needs a local rather than a parameter, assign it to a local first and index that. Note it in your report.

- [ ] **Step 5: Commit**

```powershell
git add ImGui.Widgets/Editors/BezierEditor.cs tests/ImGui.Widgets.Tests/BezierEditorTests.cs
git commit -m "Add the bezier easing curve editor"
```

---

### Task 4: The single-curve editor field

**Files:**
- Create: `ImGui.Widgets/Editors/CurveField.cs`
- Test: none. See the note below.

**Interfaces:**
- Consumes: `CurveData` and its `internal ref HexaCurve AsVendorCurve()` / `MarkDirty()` (Task 2).
- Produces: `ImGuiWidgets.CurveEditor(CurveData, Vector2, Vector2, Vector2, ref int, string)`.

**No test, deliberately.** Every line of this task is either an argument guard or a call into a vendor function that needs a live ImGui context. The conversion logic it would otherwise own lives in `CurveData` and is already tested there. A test asserting only that a null argument throws would restate `Ensure.NotNull`.

- [ ] **Step 1: Write the implementation**

Create `ImGui.Widgets/Editors/CurveField.cs`:

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System.Numerics;

using HexaCurve = Hexa.NET.Mathematics.Curve;
using HexaCurveEditor = Hexa.NET.ImGui.Widgets.Extras.ImGuiCurveEditor;

public static partial class ImGuiWidgets
{
	/// <summary>
	/// Draws an editable single curve.
	/// </summary>
	/// <param name="curve">The curve to edit, mutated in place.</param>
	/// <param name="size">Size of the editor in pixels.</param>
	/// <param name="rangeMin">Lower bound of the visible value range.</param>
	/// <param name="rangeMax">Upper bound of the visible value range.</param>
	/// <param name="selection">Index of the selected point, updated in place; -1 for none.</param>
	/// <param name="label">Label for display and identity.</param>
	/// <returns><see langword="true"/> if the curve changed this frame.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="curve"/> or <paramref name="label"/> is <see langword="null"/>.</exception>
	public static bool CurveEditor(CurveData curve, Vector2 size, Vector2 rangeMin, Vector2 rangeMax, ref int selection, string label)
	{
		Ensure.NotNull(curve);
		Ensure.NotNull(label);

		ref HexaCurve vendorCurve = ref curve.AsVendorCurve();
		bool changed = HexaCurveEditor.Curve(label, size, ref vendorCurve, rangeMin, rangeMax, ref selection);

		if (changed)
		{
			// The editor moved points directly in the wrapped curve, so the sample cache is stale.
			curve.MarkDirty();
		}

		return changed;
	}
}
```

- [ ] **Step 2: Build to verify it compiles**

Run: `dotnet build ImGui.Widgets/ImGui.Widgets.csproj -p:KtsuSyncStyleConfigFiles=false`
Expected: Build succeeded, 0 warnings, 0 errors.

- [ ] **Step 3: Run the full suite**

Run: `dotnet run --project tests/ImGui.Widgets.Tests/ImGui.Widgets.Tests.csproj -p:KtsuSyncStyleConfigFiles=false`
Expected: PASS, 200 total (no new tests).

- [ ] **Step 4: Commit**

```powershell
git add ImGui.Widgets/Editors/CurveField.cs
git commit -m "Add the single-curve editor field"
```

---

### Task 5: CurveSource and the multi-curve editor

**Files:**
- Create: `ImGui.Widgets/Editors/CurveSource.cs`
- Create: `ImGui.Widgets/Editors/CurveEditor.cs`
- Test: `tests/ImGui.Widgets.Tests/CurveSourceTests.cs`

**Interfaces:**
- Consumes: `CurveInterpolation`, `MapInterpolation` (Task 1).
- Produces: `ImGuiWidgets.CurveSource` abstract class; `ImGuiWidgets.CurveEditor(CurveSource, Vector2, string)`.

**Why no interface layer.** The Tier 1 spec assumed this tier needed a managed interface over an unsafe callback surface. For `CurveContext` that premise is wrong: it is declared `abstract unsafe` only because of public fields the library populates (`ImDrawList* DrawList`, `ImGuiIOPtr Io`), while **every member a consumer implements is already managed**. The adapter exists solely to keep the vendor base type out of our public surface, exactly as `DockedWindow` does.

`Min` and `Max` on the context are **inputs** — `Edit` computes `Range = Max - Min` — so `CurveSource` must supply the view range.

- [ ] **Step 1: Write the failing test**

Create `tests/ImGui.Widgets.Tests/CurveSourceTests.cs`:

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using System.Numerics;

using ktsu.Semantics.Color;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests the defaults a CurveSource subclass inherits. The editor itself needs a live ImGui
/// context and is verified visually in ImGuiWidgetsDemo.
/// </summary>
[TestClass]
public sealed class CurveSourceTests
{
	[TestMethod]
	public void Defaults_AreUsableWithoutOverriding()
	{
		StubCurveSource source = new();

		Assert.IsTrue(source.IsVisible(0));
		Assert.AreEqual(CurveInterpolation.Linear, source.GetInterpolation(0));
	}

	[TestMethod]
	public void GetPoints_ReturnsTheSourcePoints()
	{
		StubCurveSource source = new();

		Span<Vector2> points = source.GetPoints(0);

		Assert.AreEqual(2, points.Length);
		Assert.AreEqual(new Vector2(1f, 1f), points[1]);
	}

	private sealed class StubCurveSource : ImGuiWidgets.CurveSource
	{
		private readonly Vector2[] points = [new Vector2(0f, 0f), new Vector2(1f, 1f)];

		public override int CurveCount => 1;

		public override Vector2 ViewMin => Vector2.Zero;

		public override Vector2 ViewMax => Vector2.One;

		public override int GetPointCount(int curveIndex) => points.Length;

		public override Span<Vector2> GetPoints(int curveIndex) => points;

		public override Srgb GetCurveColor(int curveIndex) => new(1f, 0f, 0f);

		public override int EditPoint(int curveIndex, int pointIndex, Vector2 value)
		{
			points[pointIndex] = value;
			return pointIndex;
		}

		public override void AddPoint(int curveIndex, Vector2 value)
		{
		}
	}
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet run --project tests/ImGui.Widgets.Tests/ImGui.Widgets.Tests.csproj -p:KtsuSyncStyleConfigFiles=false`
Expected: compile failure — `ImGuiWidgets.CurveSource` does not exist.

- [ ] **Step 3: Write CurveSource**

Create `ImGui.Widgets/Editors/CurveSource.cs`:

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System.Numerics;

using ktsu.Semantics.Color;

public static partial class ImGuiWidgets
{
	/// <summary>
	/// Supplies curves to <see cref="CurveEditor(CurveSource, Vector2, string)"/>. Subclass it and
	/// override the members describing your curves.
	/// </summary>
	/// <remarks>
	/// The editor interrogates this object while drawing, so every member must be cheap and must
	/// not mutate anything other than in response to <see cref="EditPoint"/> or
	/// <see cref="AddPoint"/>.
	/// </remarks>
	public abstract class CurveSource
	{
		/// <summary>
		/// Gets how many curves to draw.
		/// </summary>
		public abstract int CurveCount { get; }

		/// <summary>
		/// Gets the lower corner of the visible value range.
		/// </summary>
		public abstract Vector2 ViewMin { get; }

		/// <summary>
		/// Gets the upper corner of the visible value range.
		/// </summary>
		public abstract Vector2 ViewMax { get; }

		/// <summary>
		/// Gets how many points a curve has.
		/// </summary>
		/// <param name="curveIndex">Index of the curve.</param>
		/// <returns>The point count.</returns>
		public abstract int GetPointCount(int curveIndex);

		/// <summary>
		/// Gets a curve's points.
		/// </summary>
		/// <param name="curveIndex">Index of the curve.</param>
		/// <returns>The points, in order.</returns>
		public abstract Span<Vector2> GetPoints(int curveIndex);

		/// <summary>
		/// Gets the colour a curve is drawn in.
		/// </summary>
		/// <param name="curveIndex">Index of the curve.</param>
		/// <returns>The curve colour.</returns>
		public abstract Srgb GetCurveColor(int curveIndex);

		/// <summary>
		/// Moves a point.
		/// </summary>
		/// <param name="curveIndex">Index of the curve.</param>
		/// <param name="pointIndex">Index of the point being moved.</param>
		/// <param name="value">The point's new position.</param>
		/// <returns>The point's index after any re-sort the source performs.</returns>
		/// <remarks>
		/// Returning a different index is how a source reports that moving a point past its
		/// neighbour reordered the curve. The editor tracks the point by the returned index.
		/// </remarks>
		public abstract int EditPoint(int curveIndex, int pointIndex, Vector2 value);

		/// <summary>
		/// Adds a point to a curve.
		/// </summary>
		/// <param name="curveIndex">Index of the curve.</param>
		/// <param name="value">Where to add it.</param>
		public abstract void AddPoint(int curveIndex, Vector2 value);

		/// <summary>
		/// Gets whether a curve is drawn.
		/// </summary>
		/// <param name="curveIndex">Index of the curve.</param>
		/// <returns><see langword="true"/> to draw it.</returns>
		public virtual bool IsVisible(int curveIndex) => true;

		/// <summary>
		/// Gets how a curve interpolates between its points.
		/// </summary>
		/// <param name="curveIndex">Index of the curve.</param>
		/// <returns>The interpolation mode.</returns>
		public virtual CurveInterpolation GetInterpolation(int curveIndex) => CurveInterpolation.Linear;

		/// <summary>
		/// Gets the editor's background colour.
		/// </summary>
		public virtual Srgb BackgroundColor => new(0.125f, 0.125f, 0.125f);

		/// <summary>
		/// Called before a drag begins, so the source can open an undo scope.
		/// </summary>
		/// <param name="curveIndex">Index of the curve being edited.</param>
		public virtual void BeginEdit(int curveIndex)
		{
		}

		/// <summary>
		/// Called after a drag ends, so the source can close its undo scope.
		/// </summary>
		public virtual void EndEdit()
		{
		}
	}
}
```

- [ ] **Step 4: Write the adapter and entry point**

Create `ImGui.Widgets/Editors/CurveEditor.cs`:

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System.Diagnostics.CodeAnalysis;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Color;

using HexaCurveContext = Hexa.NET.ImGui.Widgets.ImCurveEdit.CurveContext;
using HexaCurveEdit = Hexa.NET.ImGui.Widgets.ImCurveEdit.ImCurveEdit;
using HexaCurveEditType = Hexa.NET.ImGui.Widgets.ImCurveEdit.CurveType;

public static partial class ImGuiWidgets
{
	/// <summary>
	/// Draws an editable multi-curve graph.
	/// </summary>
	/// <param name="source">Supplies the curves and receives edits.</param>
	/// <param name="size">Size of the editor in pixels.</param>
	/// <param name="id">Unique identifier for the editor.</param>
	/// <returns><see langword="true"/> if a curve changed this frame.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="id"/> is <see langword="null"/>.</exception>
	[SuppressMessage("Major Code Smell", "S6640:Make sure that using \"unsafe\" is safe here", Justification = "Hexa's Edit takes an optional selected-points pointer; we pass null and no pointer is created, retained or dereferenced here.")]
	public static unsafe bool CurveEditor(CurveSource source, Vector2 size, string id)
	{
		Ensure.NotNull(source);
		Ensure.NotNull(id);

		CurveAdapter adapter = new(source);
		return HexaCurveEdit.Edit(adapter, size, ImGui.GetID(id), null) != 0;
	}

	/// <summary>
	/// Presents a <see cref="CurveSource"/> to Hexa's curve editor. Kept private so the vendor base
	/// type never reaches this library's public surface.
	/// </summary>
	/// <param name="source">The source to forward to.</param>
	private sealed class CurveAdapter(CurveSource source) : HexaCurveContext
	{
		/// <inheritdoc/>
		public override int GetCurveCount()
		{
			// Min and Max are inputs to Edit, which derives Range from them, so they must be
			// refreshed from the source before the editor reads them.
			Min = source.ViewMin;
			Max = source.ViewMax;
			return source.CurveCount;
		}

		/// <inheritdoc/>
		public override int GetPointCount(int curveIndex) => source.GetPointCount(curveIndex);

		/// <inheritdoc/>
		public override Span<Vector2> GetPoints(int curveIndex) => source.GetPoints(curveIndex);

		/// <inheritdoc/>
		public override uint GetCurveColor(int curveIndex) => source.GetCurveColor(curveIndex).ToImGuiU32();

		/// <inheritdoc/>
		public override int EditPoint(int curveIndex, int pointIndex, Vector2 value) =>
			source.EditPoint(curveIndex, pointIndex, value);

		/// <inheritdoc/>
		public override void AddPoint(int curveIndex, Vector2 value) => source.AddPoint(curveIndex, value);

		/// <inheritdoc/>
		public override bool IsVisible(int curveIndex) => source.IsVisible(curveIndex);

		/// <inheritdoc/>
		public override HexaCurveEditType GetCurveType(int curveIndex) =>
			MapInterpolation(source.GetInterpolation(curveIndex));

		/// <inheritdoc/>
		public override uint GetBackgroundColor() => source.BackgroundColor.ToImGuiU32();

		/// <inheritdoc/>
		public override void BeginEdit(int index) => source.BeginEdit(index);

		/// <inheritdoc/>
		public override void EndEdit() => source.EndEdit();
	}
}
```

If `GetCurveCount` turns out not to be the first member the editor calls, move the `Min`/`Max` refresh into the entry point by setting `adapter.Min` / `adapter.Max` before calling `Edit`, and say so in your report. Verify by reading `ImCurveEdit.Edit` at `C:\dev\HexaEngine\Hexa.NET.ImGui.Widgets\Hexa.NET.ImGui.Widgets\ImCurveEdit\ImCurveEdit.cs`.

- [ ] **Step 5: Run the tests to verify they pass**

Run: `dotnet run --project tests/ImGui.Widgets.Tests/ImGui.Widgets.Tests.csproj -p:KtsuSyncStyleConfigFiles=false`
Expected: PASS, 2 new tests (202 total).

- [ ] **Step 6: Commit**

```powershell
git add ImGui.Widgets/Editors/CurveSource.cs ImGui.Widgets/Editors/CurveEditor.cs tests/ImGui.Widgets.Tests/CurveSourceTests.cs
git commit -m "Add CurveSource and the multi-curve editor"
```

---

### Task 6: Sequencer source, items, and the pure buffer helpers

The riskiest logic in the tier, isolated here so it can be tested without an ImGui context. Task 7 does the unsafe wiring.

**Files:**
- Create: `ImGui.Widgets/Editors/SequenceSource.cs`
- Test: `tests/ImGui.Widgets.Tests/SequenceSourceTests.cs`

**Interfaces:**
- Consumes: nothing from earlier tasks.
- Produces: `SequenceItem` record; `ImGuiWidgets.SequenceSource` abstract class; `internal struct SequenceRange { public int Start; public int End; }`; `internal static void FillRanges(SequenceSource, Span<SequenceRange>)`; `internal static IEnumerable<(int Index, int Start, int End)> ComputeRangeEdits(ReadOnlySpan<SequenceRange>, ReadOnlySpan<SequenceRange>)`.

`ComputeRangeEdits` cannot return an iterator over `ReadOnlySpan` parameters — spans cannot be captured by an iterator state machine. Return a materialised `List<(int, int, int)>` instead.

- [ ] **Step 1: Write the failing tests**

Create `tests/ImGui.Widgets.Tests/SequenceSourceTests.cs`:

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using System.Collections.Generic;

using ktsu.Semantics.Color;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests the pure helpers behind the sequencer. These decide which clip edits reach the caller,
/// so a fault here silently drops or invents user edits.
/// </summary>
[TestClass]
public sealed class SequenceSourceTests
{
	[TestMethod]
	public void FillRanges_CopiesEveryItemInOrder()
	{
		StubSequenceSource source = new([(10, 20), (30, 40), (50, 60)]);
		Span<ImGuiWidgets.SequenceRange> ranges = new ImGuiWidgets.SequenceRange[3];

		ImGuiWidgets.FillRanges(source, ranges);

		Assert.AreEqual(10, ranges[0].Start);
		Assert.AreEqual(20, ranges[0].End);
		Assert.AreEqual(50, ranges[2].Start);
		Assert.AreEqual(60, ranges[2].End);
	}

	[TestMethod]
	public void ComputeRangeEdits_NothingChanged_ReturnsNoEdits()
	{
		ImGuiWidgets.SequenceRange[] before = [new() { Start = 1, End = 2 }, new() { Start = 3, End = 4 }];
		ImGuiWidgets.SequenceRange[] after = [new() { Start = 1, End = 2 }, new() { Start = 3, End = 4 }];

		List<(int Index, int Start, int End)> edits = ImGuiWidgets.ComputeRangeEdits(before, after);

		Assert.AreEqual(0, edits.Count);
	}

	[TestMethod]
	public void ComputeRangeEdits_StartMoved_ReportsThatItemOnly()
	{
		ImGuiWidgets.SequenceRange[] before = [new() { Start = 1, End = 2 }, new() { Start = 3, End = 4 }];
		ImGuiWidgets.SequenceRange[] after = [new() { Start = 1, End = 2 }, new() { Start = 99, End = 4 }];

		List<(int Index, int Start, int End)> edits = ImGuiWidgets.ComputeRangeEdits(before, after);

		Assert.AreEqual(1, edits.Count);
		Assert.AreEqual((1, 99, 4), edits[0]);
	}

	[TestMethod]
	public void ComputeRangeEdits_EndMoved_IsDetected()
	{
		ImGuiWidgets.SequenceRange[] before = [new() { Start = 1, End = 2 }];
		ImGuiWidgets.SequenceRange[] after = [new() { Start = 1, End = 7 }];

		List<(int Index, int Start, int End)> edits = ImGuiWidgets.ComputeRangeEdits(before, after);

		Assert.AreEqual(1, edits.Count);
		Assert.AreEqual((0, 1, 7), edits[0]);
	}

	[TestMethod]
	public void ComputeRangeEdits_SeveralChanged_ReportsEachOnce()
	{
		ImGuiWidgets.SequenceRange[] before = [new() { Start = 1, End = 2 }, new() { Start = 3, End = 4 }, new() { Start = 5, End = 6 }];
		ImGuiWidgets.SequenceRange[] after = [new() { Start = 0, End = 2 }, new() { Start = 3, End = 4 }, new() { Start = 5, End = 9 }];

		List<(int Index, int Start, int End)> edits = ImGuiWidgets.ComputeRangeEdits(before, after);

		Assert.AreEqual(2, edits.Count);
		Assert.AreEqual((0, 0, 2), edits[0]);
		Assert.AreEqual((2, 5, 9), edits[1]);
	}

	[TestMethod]
	public void ComputeRangeEdits_EmptySpans_ReturnsNoEdits()
	{
		List<(int Index, int Start, int End)> edits = ImGuiWidgets.ComputeRangeEdits([], []);

		Assert.AreEqual(0, edits.Count);
	}

	[TestMethod]
	public void SequenceSource_Defaults_AreUsableWithoutOverriding()
	{
		StubSequenceSource source = new([(0, 1)]);

		Assert.AreEqual(string.Empty, source.GetItemLabel(0));
		Assert.AreEqual(0, source.ItemTypeNames.Count);
		Assert.AreEqual(0, source.GetCustomHeight(0));
	}

	private sealed class StubSequenceSource(IReadOnlyList<(int Start, int End)> items) : ImGuiWidgets.SequenceSource
	{
		public override int FrameMin => 0;

		public override int FrameMax => 100;

		public override int ItemCount => items.Count;

		public override SequenceItem GetItem(int index) =>
			new(items[index].Start, items[index].End, 0, new Srgb(1f, 1f, 1f));

		public override void SetItemRange(int index, int start, int end)
		{
		}
	}
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet run --project tests/ImGui.Widgets.Tests/ImGui.Widgets.Tests.csproj -p:KtsuSyncStyleConfigFiles=false`
Expected: compile failure — `SequenceSource`, `SequenceItem`, `SequenceRange`, `FillRanges` and `ComputeRangeEdits` do not exist.

- [ ] **Step 3: Write the implementation**

Create `ImGui.Widgets/Editors/SequenceSource.cs`:

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System.Collections.Generic;

using ktsu.Semantics.Color;

/// <summary>
/// One clip on a sequencer track.
/// </summary>
/// <param name="Start">Frame the clip starts on.</param>
/// <param name="End">Frame the clip ends on.</param>
/// <param name="TypeIndex">Index into the source's type names.</param>
/// <param name="Color">Colour the clip is drawn in.</param>
public sealed record SequenceItem(int Start, int End, int TypeIndex, Srgb Color);

public static partial class ImGuiWidgets
{
	/// <summary>
	/// A clip's frame range, laid out so its two fields sit at a stable address while the
	/// sequencer edits them in place.
	/// </summary>
	internal struct SequenceRange
	{
		/// <summary>
		/// Frame the clip starts on.
		/// </summary>
		public int Start;

		/// <summary>
		/// Frame the clip ends on.
		/// </summary>
		public int End;
	}

	/// <summary>
	/// Supplies clips to <see cref="Sequencer"/> and receives edits. Subclass it and override the
	/// members describing your timeline.
	/// </summary>
	public abstract class SequenceSource
	{
		/// <summary>
		/// Gets the first frame on the timeline.
		/// </summary>
		public abstract int FrameMin { get; }

		/// <summary>
		/// Gets the last frame on the timeline.
		/// </summary>
		public abstract int FrameMax { get; }

		/// <summary>
		/// Gets how many clips the timeline holds.
		/// </summary>
		public abstract int ItemCount { get; }

		/// <summary>
		/// Gets a clip.
		/// </summary>
		/// <param name="index">Index of the clip.</param>
		/// <returns>The clip.</returns>
		public abstract SequenceItem GetItem(int index);

		/// <summary>
		/// Applies a range edit the user made by dragging.
		/// </summary>
		/// <param name="index">Index of the clip that moved.</param>
		/// <param name="start">Its new start frame.</param>
		/// <param name="end">Its new end frame.</param>
		public abstract void SetItemRange(int index, int start, int end);

		/// <summary>
		/// Gets the label drawn on a clip.
		/// </summary>
		/// <param name="index">Index of the clip.</param>
		/// <returns>The label.</returns>
		public virtual string GetItemLabel(int index) => string.Empty;

		/// <summary>
		/// Gets the clip types the user may add.
		/// </summary>
		public virtual IReadOnlyList<string> ItemTypeNames => [];

		/// <summary>
		/// Adds a clip of the given type.
		/// </summary>
		/// <param name="typeIndex">Index into <see cref="ItemTypeNames"/>.</param>
		public virtual void AddItem(int typeIndex)
		{
		}

		/// <summary>
		/// Deletes a clip.
		/// </summary>
		/// <param name="index">Index of the clip.</param>
		public virtual void DeleteItem(int index)
		{
		}

		/// <summary>
		/// Duplicates a clip.
		/// </summary>
		/// <param name="index">Index of the clip.</param>
		public virtual void DuplicateItem(int index)
		{
		}

		/// <summary>
		/// Copies the current selection.
		/// </summary>
		public virtual void Copy()
		{
		}

		/// <summary>
		/// Pastes the copied selection.
		/// </summary>
		public virtual void Paste()
		{
		}

		/// <summary>
		/// Gets extra vertical space to reserve under a clip, for custom content.
		/// </summary>
		/// <param name="index">Index of the clip.</param>
		/// <returns>Height in pixels.</returns>
		public virtual int GetCustomHeight(int index) => 0;

		/// <summary>
		/// Called when a clip is double-clicked.
		/// </summary>
		/// <param name="index">Index of the clip.</param>
		public virtual void DoubleClick(int index)
		{
		}

		/// <summary>
		/// Called before a drag begins, so the source can open an undo scope.
		/// </summary>
		/// <param name="index">Index of the clip being edited.</param>
		public virtual void BeginEdit(int index)
		{
		}

		/// <summary>
		/// Called after a drag ends, so the source can close its undo scope.
		/// </summary>
		public virtual void EndEdit()
		{
		}
	}

	/// <summary>
	/// Copies every clip's frame range out of a source into a buffer.
	/// </summary>
	/// <param name="source">The source to read.</param>
	/// <param name="ranges">Buffer to fill; must be at least as long as the source's item count.</param>
	/// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
	internal static void FillRanges(SequenceSource source, Span<SequenceRange> ranges)
	{
		Ensure.NotNull(source);

		for (int i = 0; i < ranges.Length; i++)
		{
			SequenceItem item = source.GetItem(i);
			ranges[i].Start = item.Start;
			ranges[i].End = item.End;
		}
	}

	/// <summary>
	/// Finds which clips the sequencer moved.
	/// </summary>
	/// <param name="before">Ranges as they were before the sequencer ran.</param>
	/// <param name="after">Ranges as they are afterwards.</param>
	/// <returns>One entry per changed clip, in index order.</returns>
	/// <remarks>
	/// The sequencer edits ranges in place through pointers, so a diff is the only way to learn
	/// which clips actually moved. Reporting an unchanged clip would fire a spurious edit;
	/// missing a changed one would silently discard the user's drag.
	/// </remarks>
	internal static List<(int Index, int Start, int End)> ComputeRangeEdits(ReadOnlySpan<SequenceRange> before, ReadOnlySpan<SequenceRange> after)
	{
		List<(int Index, int Start, int End)> edits = [];
		int count = Math.Min(before.Length, after.Length);

		for (int i = 0; i < count; i++)
		{
			if (before[i].Start != after[i].Start || before[i].End != after[i].End)
			{
				edits.Add((i, after[i].Start, after[i].End));
			}
		}

		return edits;
	}
}
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet run --project tests/ImGui.Widgets.Tests/ImGui.Widgets.Tests.csproj -p:KtsuSyncStyleConfigFiles=false`
Expected: PASS, 7 new tests (209 total).

- [ ] **Step 5: Commit**

```powershell
git add ImGui.Widgets/Editors/SequenceSource.cs tests/ImGui.Widgets.Tests/SequenceSourceTests.cs
git commit -m "Add SequenceSource and the pure sequencer range helpers"
```

---

### Task 7: The sequencer adapter and entry point

The unsafe seam. Everything testable was moved into Task 6; what remains is pointer wiring that must be right by construction.

**Files:**
- Create: `ImGui.Widgets/Editors/Sequencer.cs`
- Test: none beyond Task 6's. Every line here either dereferences a pointer or calls into the vendor, both of which need a live ImGui context.

**Interfaces:**
- Consumes: `SequenceSource`, `SequenceItem`, `SequenceRange`, `FillRanges`, `ComputeRangeEdits` (Task 6); `SequencerFeatures`, `MapFeatures` (Task 1).
- Produces: `ImGuiWidgets.Sequencer(SequenceSource, ref int, ref bool, ref int, ref int, SequencerFeatures)`.

**The contract, restated because getting it wrong is a crash.**

`SequenceInterface.Get(int index, int** start, int** end, int* type, uint* color)` is called from three upstream sites with three different null combinations:

```
ImSequencer.cs:362   Get(i, null, null, &type, null)
ImSequencer.cs:431   Get(i, &start, &end, null, &color)
ImSequencer.cs:531   Get(MovingEntry, &start, &end, null, null)
```

**Every out-parameter must be null-checked before writing.** Writing unconditionally is the exact shape of the Tier 1 `FlameGraph` defect, which was a guaranteed process crash invisible to both the compiler and the test suite.

`start` and `end` are `int**`: we write *pointers into our pinned buffer*, and the sequencer both reads and writes through them while the user drags. That is why the buffer must stay at a fixed address for the whole call.

- [ ] **Step 1: Write the implementation**

Create `ImGui.Widgets/Editors/Sequencer.cs`:

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

using ktsu.ImGui.Color;

using HexaSequenceInterface = Hexa.NET.ImGui.Widgets.ImSequencer.SequenceInterface;
using HexaSequencer = Hexa.NET.ImGui.Widgets.ImSequencer.ImSequencer;

public static partial class ImGuiWidgets
{
	/// <summary>
	/// Item counts at or below this are buffered on the stack; larger timelines use a heap array,
	/// which <c>fixed</c> pins just the same.
	/// </summary>
	private const int SequencerStackAllocLimit = 64;

	/// <summary>
	/// Draws an editable timeline.
	/// </summary>
	/// <param name="source">Supplies the clips and receives edits.</param>
	/// <param name="currentFrame">The playhead frame, updated in place.</param>
	/// <param name="expanded">Whether the timeline is expanded, updated in place.</param>
	/// <param name="selectedEntry">Index of the selected clip, updated in place; -1 for none.</param>
	/// <param name="firstFrame">Leftmost visible frame, updated in place.</param>
	/// <param name="features">Which interactions to enable.</param>
	/// <returns><see langword="true"/> if the sequencer reports a change this frame.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
	[SuppressMessage("Major Code Smell", "S6640:Make sure that using \"unsafe\" is safe here", Justification = "Hexa's SequenceInterface.Get hands back int* into caller-owned storage; the buffer is pinned for exactly the duration of the call and the adapter is unbound in a finally.")]
	public static unsafe bool Sequencer(
		SequenceSource source,
		ref int currentFrame,
		ref bool expanded,
		ref int selectedEntry,
		ref int firstFrame,
		SequencerFeatures features = SequencerFeatures.EditAll)
	{
		Ensure.NotNull(source);

		int count = source.ItemCount;
		Span<SequenceRange> ranges = count <= SequencerStackAllocLimit
			? stackalloc SequenceRange[count]
			: new SequenceRange[count];

		FillRanges(source, ranges);

		SequenceRange[] before = ranges.ToArray();
		SequenceAdapter adapter = new(source);
		bool changed;

		fixed (SequenceRange* pinned = ranges)
		{
			adapter.Bind(pinned, count);
			try
			{
				changed = HexaSequencer.Sequencer(
					adapter,
					ref currentFrame,
					ref expanded,
					ref selectedEntry,
					ref firstFrame,
					MapFeatures(features));
			}
			finally
			{
				// Must run even if Sequencer throws: leaving the adapter bound would hold a
				// pointer into a span that is no longer pinned.
				adapter.Unbind();
			}
		}

		foreach ((int index, int start, int end) in ComputeRangeEdits(before, ranges))
		{
			source.SetItemRange(index, start, end);
		}

		return changed;
	}

	/// <summary>
	/// Presents a <see cref="SequenceSource"/> to Hexa's sequencer. Kept private so the vendor base
	/// type never reaches this library's public surface.
	/// </summary>
	/// <param name="source">The source to forward to.</param>
	private sealed unsafe class SequenceAdapter(SequenceSource source) : HexaSequenceInterface
	{
		private SequenceRange* ranges;
		private int count;
		private byte[] labelBuffer = new byte[128];

		/// <summary>
		/// Points the adapter at the pinned range buffer for the duration of one call.
		/// </summary>
		/// <param name="buffer">The pinned buffer.</param>
		/// <param name="length">How many entries it holds.</param>
		internal void Bind(SequenceRange* buffer, int length)
		{
			ranges = buffer;
			count = length;
		}

		/// <summary>
		/// Releases the pinned buffer. Must be called before the pin is released.
		/// </summary>
		internal void Unbind()
		{
			ranges = null;
			count = 0;
		}

		/// <inheritdoc/>
		public override int GetFrameMin() => source.FrameMin;

		/// <inheritdoc/>
		public override int GetFrameMax() => source.FrameMax;

		/// <inheritdoc/>
		public override int GetItemCount() => source.ItemCount;

		/// <inheritdoc/>
		/// <remarks>
		/// Upstream calls this with different subsets of the out-parameters set to null, so every
		/// one is checked before writing. Writing unconditionally dereferences null.
		/// </remarks>
		public override void Get(int index, int** start, int** end, int* type, uint* color)
		{
			if (ranges is null || index < 0 || index >= count)
			{
				return;
			}

			if (start is not null)
			{
				*start = &ranges[index].Start;
			}

			if (end is not null)
			{
				*end = &ranges[index].End;
			}

			if (type is not null || color is not null)
			{
				SequenceItem item = source.GetItem(index);

				if (type is not null)
				{
					*type = item.TypeIndex;
				}

				if (color is not null)
				{
					*color = item.Color.ToImGuiU32();
				}
			}
		}

		/// <inheritdoc/>
		public override ReadOnlySpan<byte> GetItemLabel(int index) => EncodeLabel(source.GetItemLabel(index));

		/// <inheritdoc/>
		public override int GetItemTypeCount() => source.ItemTypeNames.Count;

		/// <inheritdoc/>
		public override ReadOnlySpan<byte> GetItemTypeName(int typeIndex) => EncodeLabel(source.ItemTypeNames[typeIndex]);

		/// <inheritdoc/>
		public override void Add(int type) => source.AddItem(type);

		/// <inheritdoc/>
		public override void Del(int index) => source.DeleteItem(index);

		/// <inheritdoc/>
		public override void Duplicate(int index) => source.DuplicateItem(index);

		/// <inheritdoc/>
		public override void Copy() => source.Copy();

		/// <inheritdoc/>
		public override void Paste() => source.Paste();

		/// <inheritdoc/>
		public override nuint GetCustomHeight(int index) => (nuint)Math.Max(0, source.GetCustomHeight(index));

		/// <inheritdoc/>
		public override void DoubleClick(int index) => source.DoubleClick(index);

		/// <inheritdoc/>
		public override void BeginEdit(int index) => source.BeginEdit(index);

		/// <inheritdoc/>
		public override void EndEdit() => source.EndEdit();

		/// <summary>
		/// Encodes a label as NUL-terminated UTF-8 into a reusable buffer.
		/// </summary>
		/// <param name="value">The label.</param>
		/// <returns>The encoded bytes, including the terminator.</returns>
		/// <remarks>
		/// The returned span is only valid until the next call, which is sufficient because
		/// upstream consumes it immediately.
		/// </remarks>
		private ReadOnlySpan<byte> EncodeLabel(string value)
		{
			int required = Encoding.UTF8.GetByteCount(value) + 1;
			if (labelBuffer.Length < required)
			{
				labelBuffer = new byte[required];
			}

			int written = Encoding.UTF8.GetBytes(value, labelBuffer);
			labelBuffer[written] = 0;
			return labelBuffer.AsSpan(0, written + 1);
		}
	}
}
```

- [ ] **Step 2: Build to verify it compiles**

Run: `dotnet build ImGui.Widgets/ImGui.Widgets.csproj -p:KtsuSyncStyleConfigFiles=false`
Expected: Build succeeded, 0 warnings, 0 errors.

`stackalloc` with a zero count is legal and yields an empty span, so an empty timeline needs no special case. If the compiler objects to `stackalloc` in a conditional expression on any target framework, hoist it into an explicit `if`/`else` and say so in your report.

- [ ] **Step 3: Run the full suite**

Run: `dotnet run --project tests/ImGui.Widgets.Tests/ImGui.Widgets.Tests.csproj -p:KtsuSyncStyleConfigFiles=false`
Expected: PASS, 209 total (no new tests).

- [ ] **Step 4: Commit**

```powershell
git add ImGui.Widgets/Editors/Sequencer.cs
git commit -m "Add the sequencer adapter and entry point"
```

---

### Task 8: Demo

**Files:**
- Modify: `examples/ImGuiWidgetsDemo/HexaWidgetsDemo.cs`

**Interfaces:**
- Consumes: everything public from Tasks 1-7.

There is no ktsu counterpart to any of these, so nothing goes in the comparison table. All four land in the Net New gallery.

- [ ] **Step 1: Add demo state and sources**

In `examples/ImGuiWidgetsDemo/HexaWidgetsDemo.cs`, add these fields alongside the existing demo state:

```csharp
	private static readonly CurveData DemoCurve = new(
	[
		new CurveKnot(new Vector2(0f, 0f), CurvePointKind.Smooth),
		new CurveKnot(new Vector2(0.5f, 0.8f), CurvePointKind.Smooth),
		new CurveKnot(new Vector2(1f, 0.2f), CurvePointKind.Corner),
	]);

	private static int demoCurveSelection = -1;
	private static BezierControlPoints demoBezier = new(new Vector2(0.25f, 0.1f), new Vector2(0.75f, 0.9f));

	private static readonly DemoCurveSource MultiCurve = new();
	private static readonly DemoSequence Timeline = new();
	private static int timelineFrame;
	private static bool timelineExpanded = true;
	private static int timelineSelected = -1;
	private static int timelineFirstFrame;
	private static string timelineLastEdit = "(none)";
```

Add these nested types at the end of the class:

```csharp
	/// <summary>
	/// Two curves over a shared view range, for the multi-curve editor demo.
	/// </summary>
	private sealed class DemoCurveSource : ImGuiWidgets.CurveSource
	{
		private readonly Vector2[][] curves =
		[
			[new Vector2(0f, 0f), new Vector2(0.5f, 0.6f), new Vector2(1f, 0.3f)],
			[new Vector2(0f, 0.8f), new Vector2(0.5f, 0.2f), new Vector2(1f, 0.9f)],
		];

		public override int CurveCount => curves.Length;

		public override Vector2 ViewMin => Vector2.Zero;

		public override Vector2 ViewMax => Vector2.One;

		public override int GetPointCount(int curveIndex) => curves[curveIndex].Length;

		public override Span<Vector2> GetPoints(int curveIndex) => curves[curveIndex];

		public override Srgb GetCurveColor(int curveIndex) =>
			curveIndex == 0 ? new Srgb(0.9f, 0.4f, 0.3f) : new Srgb(0.3f, 0.7f, 0.9f);

		public override int EditPoint(int curveIndex, int pointIndex, Vector2 value)
		{
			curves[curveIndex][pointIndex] = value;
			return pointIndex;
		}

		public override void AddPoint(int curveIndex, Vector2 value)
		{
			// The demo keeps a fixed point count so the editor's add gesture is a no-op here.
		}
	}

	/// <summary>
	/// A short in-memory timeline, for the sequencer demo. Drag edits write back through
	/// SetItemRange, which is what the label below the widget reports.
	/// </summary>
	private sealed class DemoSequence : ImGuiWidgets.SequenceSource
	{
		private readonly List<(int Start, int End, int Type)> clips =
		[
			(0, 20, 0),
			(25, 60, 1),
			(70, 95, 0),
		];

		public override int FrameMin => 0;

		public override int FrameMax => 100;

		public override int ItemCount => clips.Count;

		public override IReadOnlyList<string> ItemTypeNames => ["Camera", "Audio"];

		public override SequenceItem GetItem(int index) => new(
			clips[index].Start,
			clips[index].End,
			clips[index].Type,
			clips[index].Type == 0 ? new Srgb(0.8f, 0.5f, 0.2f) : new Srgb(0.3f, 0.6f, 0.8f));

		public override string GetItemLabel(int index) => $"{ItemTypeNames[clips[index].Type]} {index}";

		public override void SetItemRange(int index, int start, int end)
		{
			clips[index] = (start, end, clips[index].Type);
			timelineLastEdit = $"item {index} -> [{start}, {end}]";
		}

		public override void AddItem(int typeIndex) => clips.Add((0, 10, typeIndex));

		public override void DeleteItem(int index) => clips.RemoveAt(index);
	}
```

- [ ] **Step 2: Add the gallery section**

In `ShowNetNew()`, add:

```csharp
		if (ImGui.CollapsingHeader("Curve and bezier fields"))
		{
			_ = ImGuiWidgets.CurveEditor(DemoCurve, new Vector2(0f, 120f), Vector2.Zero, Vector2.One, ref demoCurveSelection, "##demoCurve");
			ImGui.TextUnformatted($"Sample at 0.5: {DemoCurve.Sample(0.5f):F3}");

			_ = ImGuiWidgets.BezierEditor("Easing", ref demoBezier);
			ImGui.TextUnformatted($"Control points: {demoBezier.First:F2} {demoBezier.Second:F2}");
		}

		if (ImGui.CollapsingHeader("Multi-curve editor"))
		{
			ImGui.TextWrapped("Two curves over one view range. Drag a point to move it.");
			_ = ImGuiWidgets.CurveEditor(MultiCurve, new Vector2(0f, 160f), "##multiCurve");
		}

		if (ImGui.CollapsingHeader("Sequencer"))
		{
			ImGui.TextWrapped("Drag a clip's edges to retime it. Edits are written back through SetItemRange, which is what the line below reports.");
			_ = ImGuiWidgets.Sequencer(Timeline, ref timelineFrame, ref timelineExpanded, ref timelineSelected, ref timelineFirstFrame);
			ImGui.TextUnformatted($"Frame {timelineFrame}, selected {timelineSelected}, last edit: {timelineLastEdit}");
		}
```

- [ ] **Step 3: Build and verify**

Run: `dotnet build examples/ImGuiWidgetsDemo/ImGuiWidgetsDemo.csproj -p:KtsuSyncStyleConfigFiles=false`
Expected: Build succeeded, 0 warnings, 0 errors.

Check the file's existing `using` directives include `System.Collections.Generic`, `System.Numerics`, and `ktsu.Semantics.Color`; add whichever are missing.

- [ ] **Step 4: Run the full suite**

Run: `dotnet run --project tests/ImGui.Widgets.Tests/ImGui.Widgets.Tests.csproj -p:KtsuSyncStyleConfigFiles=false`
Expected: PASS, 209 total.

- [ ] **Step 5: Commit**

```powershell
git add examples/ImGuiWidgetsDemo/HexaWidgetsDemo.cs
git commit -m "Demo the Tier 3 editors"
```

---

### Task 9: Documentation

**Files:**
- Modify: `CLAUDE.md`
- Modify: `ImGui.Widgets/README.md`

**Accuracy is the job.** Tier 1 shipped two documentation errors and Tier 2's docs review caught a third before merge. Verify every claim against the code as it exists after Tasks 1-8; do not copy from this plan.

- [ ] **Step 1: Update CLAUDE.md**

Append the new type names to the `ImGui.Widgets` bullet: `Sequencer`, `SequenceSource`, `CurveEditor`, `CurveSource`, `CurveData`, `BezierEditor`.

Add a subsection after "Deferred Drawing":

```markdown
### Callback-driven editors

`ImGuiWidgets.Sequencer` and the multi-curve `ImGuiWidgets.CurveEditor` take a source object they
interrogate while drawing, rather than a value:

- Subclass `ImGuiWidgets.SequenceSource` for a timeline: `FrameMin`, `FrameMax`, `ItemCount`,
  `GetItem`, and `SetItemRange`, which receives drag edits.
- Subclass `ImGuiWidgets.CurveSource` for a multi-curve graph: `CurveCount`, `ViewMin`, `ViewMax`,
  `GetPoints`, `GetCurveColor`, `EditPoint`, `AddPoint`.

Neither needs a per-frame pump — they are immediate-mode calls that happen to take a callback
object. Both source types are plain abstract classes with managed members; no vendor type and no
`unsafe` appears in either surface.

`ImGuiWidgets.CurveEditor` also has a single-curve overload taking `CurveData`, and
`ImGuiWidgets.BezierEditor` edits a `BezierControlPoints` pair. `CurveData` wraps the curve
representation the widget expects and recomputes its sample cache automatically, so `Sample`
never returns a stale value after an edit.
```

- [ ] **Step 2: Mirror into the widget README**

Add the same type names and a shortened form of the note to `ImGui.Widgets/README.md`, matching the structure already used there for the Tier 1 and Tier 2 widgets.

- [ ] **Step 3: Verify every claim**

Re-read each sentence against the code. Confirm each type name exists with that exact spelling, that the member lists match the abstract members actually declared, and that the "no pump needed" claim is true — check that neither entry point calls `DrawDeferred` or requires it.

- [ ] **Step 4: Build and commit**

```powershell
dotnet build ImGui.sln -p:KtsuSyncStyleConfigFiles=false
git add CLAUDE.md ImGui.Widgets/README.md
git commit -m "Document the Tier 3 editors"
```

---

## Self-review notes

**Spec coverage.** Every spec section maps to a task: enum mirrors including the two-`CurveType` trap (Task 1), `CurveData` and `CurveKnot` (Task 2), Extras bezier (Task 3), Extras curve (Task 4), `CurveSource` and its adapter (Task 5), `SequenceSource` plus the pure buffer helpers (Task 6), the pinned-buffer pointer contract and null-guarded `Get` (Task 7), demo (Task 8), docs (Task 9).

**Deliberately not built**, per the spec's out-of-scope list: `FormatCollapse`, both `CustomDraw` virtuals, the `ImVector<EditPoint>*` selected-points parameter, and `CurveContext`'s coordinate helpers. Each takes a vendor or pointer type and each has a working upstream default.

**Tasks 4 and 7 have no tests, and both say why.** Task 4's only logic lives in `CurveData`, already tested in Task 2. Task 7's logic was deliberately moved into Task 6 so it could be tested without an ImGui context; what remains is pointer wiring. That split is the main structural decision in this plan and exists because the chosen verification approach has no way to exercise a draw path.

**Test counts** assume 177 before Task 1. Verify the actual starting count rather than trusting these numbers.

**Standing rules, both earned in earlier tiers.** For any callback-shaped upstream API, read every call site of the callback rather than only its signature — Task 7's `Get` has three call sites with three different null combinations. And verifying that callback state is *populated* is not sufficient; Tier 2's two Criticals were both about whether a value was *well-formed for the type we declare it as*. For this tier that means checking the pointers we hand back stay valid and the values read back through them round-trip into the consumer's model.
