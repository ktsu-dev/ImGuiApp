# Hexa widgets Tier 3 — curve editors and the sequencer

Status: proposed
Date: 2026-08-16
Follows `2026-08-15-hexa-widgets-tier2-design.md`. Named and deferred by
`2026-08-14-hexa-widgets-tier1-design.md`.

## Summary

Tier 1 wrapped Hexa's immediate-mode widgets. Tier 2 wrapped the stateful dialogs that need a
per-frame pump. Tier 3 wraps the callback-driven editors: the caller supplies an object the
widget interrogates while drawing.

Four things ship:

| Component | Upstream | Shape |
|---|---|---|
| Sequencer | `ImSequencer` + `SequenceInterface` | Abstract base with a pointer-lifetime contract |
| Curve editor | `ImCurveEdit` + `CurveContext` | Abstract base, already managed |
| Curve field | Extras `ImGuiCurveEditor` | Static, takes `Hexa.NET.Math.Curve` by ref |
| Bezier field | Extras `ImGuiBezierWidget` | Static, takes `Hexa.NET.Math.BezierCurve` by ref |

## Decisions (locked during brainstorming)

- **Scope**: all four in one spec, rather than splitting the sequencer out.
- **Verification**: unit tests on pure helpers plus careful review — no headless ImGui harness.
  See [Testing](#testing) for what this costs and how the design compensates.
- **Shape**: composition throughout, matching Tier 2's `DockedWindow`. Public abstract sources
  with ktsu-named managed members, each paired with a private adapter that subclasses Hexa's
  base. Rejected: interfaces (Hexa's bases carry many virtuals with useful defaults that an
  interface would force every implementer to restate).
- **`Hexa.NET.Math` types stay out of the public surface.** `Curve` is wrapped, not mirrored;
  `BezierCurve` is small enough to mirror. Rejected: exposing them directly — Tier 2 spent a fix
  round removing a vendor type from the public surface, and re-admitting one immediately would
  make the rule meaningless.

`Hexa.NET.ImGui.Widgets.Extras` and `Hexa.NET.Math` are already referenced by `ImGui.Widgets`, so
this tier adds no new dependency weight.

## The sequencer

### Why it is the hard one

`SequenceInterface` has one member that cannot be expressed in managed code:

```csharp
public abstract void Get(int index, int** start, int** end, int* type, uint* color);
```

The implementation must hand back **pointers to its own `int` storage**. Upstream then reads
*and writes* through them: `ImSequencer.cs:531` re-reads `start`/`end` inside the drag path, and
dragging a clip edits the values in place.

Upstream calls it three times with three different null combinations:

```
ImSequencer.cs:362   Get(i, null, null, &type, null)       // type only
ImSequencer.cs:431   Get(i, &start, &end, null, &color)    // no type
ImSequencer.cs:531   Get(MovingEntry, &start, &end, null, null)  // drag path
```

**Every out-parameter must be null-checked.** This is the exact shape of the Tier 1 `FlameGraph`
Critical, where writing unconditionally was a guaranteed process crash that neither the compiler
nor the test suite could see.

### The buffer, and why it needs no disposal contract

The adapter needs `int` storage that stays at a fixed address for the duration of one
`Sequencer(...)` call. Allocating native memory would force an `IDisposable` contract onto
`SequenceSource` — a contract this library has deliberately avoided since Tier 1.

Instead the static entry point owns the buffer for exactly the call. The buffer element is a
two-field struct laid out so its `Start` and `End` are adjacent `int`s at a stable address:

```csharp
internal struct SequenceRange
{
    public int Start;
    public int End;
}

/// Item counts at or below this use the stack; above it, a heap array (still pinned by `fixed`).
private const int StackAllocLimit = 64;
```

```csharp
Span<SequenceRange> ranges = count <= StackAllocLimit
    ? stackalloc SequenceRange[count]
    : new SequenceRange[count];

FillRanges(source, ranges);                 // pure, tested
SequenceRange[] before = ranges.ToArray();  // snapshot for the diff

fixed (SequenceRange* pinned = ranges)
{
    adapter.Bind(pinned, count);
    try { HexaSequencer.Sequencer(adapter, ...); }
    finally { adapter.Unbind(); }
}

foreach ((int index, int start, int end) in ComputeRangeEdits(before, ranges))  // pure, tested
{
    source.SetItemRange(index, start, end);
}
```

`fixed` pins for precisely the call's lifetime. `Get` returns interior pointers into the pinned
span. Nothing outlives the call, nothing needs freeing, and no disposal contract appears.

`Unbind()` in a `finally` matters: if `Sequencer` throws, the adapter must not retain a dangling
pointer into a span that is no longer pinned.

### Public API

```csharp
public abstract class SequenceSource
{
    public abstract int FrameMin { get; }
    public abstract int FrameMax { get; }
    public abstract int ItemCount { get; }

    public abstract SequenceItem GetItem(int index);
    public abstract void SetItemRange(int index, int start, int end);

    public virtual string GetItemLabel(int index) => string.Empty;
    public virtual IReadOnlyList<string> ItemTypeNames => [];
    public virtual void AddItem(int typeIndex) { }
    public virtual void DeleteItem(int index) { }
    public virtual void DuplicateItem(int index) { }
    public virtual void Copy() { }
    public virtual void Paste() { }
    public virtual int GetCustomHeight(int index) => 0;
    public virtual void DoubleClick(int index) { }
    public virtual void BeginEdit(int index) { }
    public virtual void EndEdit() { }
}

public sealed record SequenceItem(int Start, int End, int TypeIndex, Srgb Color);

[Flags]
public enum SequencerFeatures
{
    None = 0,
    EditStartEnd = 1 << 1,
    ChangeFrame = 1 << 3,
    Add = 1 << 4,
    Delete = 1 << 5,
    CopyPaste = 1 << 6,
    EditAll = EditStartEnd | ChangeFrame,
}

// entry point
public static bool Sequencer(
    SequenceSource source,
    ref int currentFrame,
    ref bool expanded,
    ref int selectedEntry,
    ref int firstFrame,
    SequencerFeatures features = SequencerFeatures.EditAll);
```

`SequencerFeatures` mirrors upstream's `SequencerOptions` numerically, including its gaps at
`1 << 0` and `1 << 2`. Upstream does not mark it `[Flags]` despite using it as one; ours does.

### Deliberately not exposed

`FormatCollapse(ref StrBuilder)` and both `CustomDraw(ImDrawList*, …)` virtuals. Each takes a
vendor or pointer type, each has a working upstream default, and none is needed to use a
sequencer. Leaving them unoverridden is what keeps `unsafe` out of the public surface entirely.

## The curve editor

`CurveContext` is `abstract unsafe`, but only because of public fields the library populates
(`ImDrawList* DrawList`, `ImGuiIOPtr Io`). **Every member a consumer implements is already
managed.** The Tier 1 spec assumed this tier needed a managed interface layer over an unsafe
callback surface; for the curve editor that premise is wrong, and building one would restate a
surface that is already clean.

Note `Min` and `Max` are *inputs* — `Edit` computes `Range = Max - Min` from them — so the source
must supply the view range or the editor has nothing to scale to.

```csharp
public abstract class CurveSource
{
    public abstract int CurveCount { get; }
    public abstract Vector2 ViewMin { get; }
    public abstract Vector2 ViewMax { get; }

    public abstract int GetPointCount(int curveIndex);
    public abstract Span<Vector2> GetPoints(int curveIndex);
    public abstract Srgb GetCurveColor(int curveIndex);

    /// Returns the point's index after any re-sort the source performs.
    public abstract int EditPoint(int curveIndex, int pointIndex, Vector2 value);
    public abstract void AddPoint(int curveIndex, Vector2 value);

    public virtual bool IsVisible(int curveIndex) => true;
    public virtual CurveInterpolation GetInterpolation(int curveIndex) => CurveInterpolation.Linear;
    public virtual Srgb BackgroundColor => new(0.125f, 0.125f, 0.125f);
    public virtual void BeginEdit(int curveIndex) { }
    public virtual void EndEdit() { }
}

public enum CurveInterpolation { None, Discrete, Linear, Smooth, Bezier }
```

`CurveInterpolation` mirrors `Hexa.NET.ImGui.Widgets.ImCurveEdit.CurveType`, dropping the prefix
that made `CurveType.CurveLinear` stutter.

### Two different upstream enums are both called `CurveType`

This is a trap worth stating explicitly, because the names are identical and the meanings are not:

| Upstream type | Members | Used by |
|---|---|---|
| `Hexa.NET.ImGui.Widgets.ImCurveEdit.CurveType` | `None, CurveDiscrete, CurveLinear, CurveSmooth, CurveBezier` | `CurveContext.GetCurveType` — interpolation between points |
| `Hexa.NET.Mathematics.CurveType` | `Smooth, Freehand` | `Curve.Type` — how the Extras field authors the curve |

They get **separate mirrors**: `CurveInterpolation` for the first, `CurveShape { Smooth, Freehand }`
for the second. A single shared mirror would silently mis-map, since neither member set is a
subset of the other.

Note also the namespace is `Hexa.NET.Mathematics`; `Hexa.NET.Math` is the *package* name.

`Edit` returns `int ret`, set to `1` when anything changed. Our entry point returns `bool`,
matching every other widget in the suite.

Not exposed in v1: the `ImVector<EditPoint>* selectedPoints` out-parameter, and `CurveContext`'s
coordinate helpers (`PointToCanvas`, `ScreenToCanvas`, …). Both are pointer- or
internals-flavoured and neither is needed to edit a curve.

## The Extras fields

`ImGuiCurveEditor.Curve` and `ImGuiBezierWidget.Bezier` are immediate-mode statics — Tier-1
shaped. The only problem is their data types.

**`BezierCurve`** is a small struct of two `Vector2` control points. Mirrored as
`BezierControlPoints`, a plain record struct, converted at the seam.

**`Curve`** carries `List<CurvePoint> Points`, a `float[] Samples` cache, `CurveType Type`, and a
`Compute()` that fills the cache. Mirroring it would mean duplicating the curve maths or
converting a list and an array every frame. Instead `CurveData` **wraps** a private vendor
`Curve`, so there is no duplicated maths, no per-frame conversion, and no vendor type in the
public surface:

```csharp
public sealed class CurveData
{
    public CurveData();
    public CurveData(IEnumerable<CurveKnot> points, CurveShape shape = CurveShape.Smooth);

    public int PointCount { get; }
    public CurveShape Shape { get; set; }

    public CurveKnot GetPoint(int index);
    public void AddPoint(CurveKnot point);
    public void SetPoint(int index, CurveKnot point);
    public void RemovePoint(int index);
    public void Clear();

    /// Samples the computed curve. Recomputes the cache if points changed since the last read.
    public float Sample(float t);
}

/// A single authored point. Mirrors Hexa.NET.Mathematics.CurvePoint.
public readonly record struct CurveKnot(Vector2 Position, CurvePointKind Kind);

public enum CurvePointKind { Smooth, Corner }

public enum CurveShape { Smooth, Freehand }
```

`CurveKnot` exists because upstream's `CurvePoint` carries a per-point `CurvePointType`
(`Smooth` / `Corner`) alongside its coordinates. Exposing points as bare `Vector2` would silently
drop that, and corner-versus-smooth is a real authoring distinction the widget honours. The name
avoids colliding with the `CurvePoint` in `Hexa.NET.Mathematics`.

`Sample` recomputing lazily matters: upstream's `Compute()` fills a `float[]` cache that goes
stale on every edit, and making the caller remember to call it is the kind of contract this
suite has been removing rather than adding.

### Entry points

```csharp
public static bool CurveEditor(CurveSource source, Vector2 size, string id);
public static bool CurveEditor(CurveData curve, Vector2 size, Vector2 rangeMin, Vector2 rangeMax, ref int selection, string label);
public static bool BezierEditor(string label, ref BezierControlPoints points, float size = 128f);
```

The two `CurveEditor` overloads are unambiguous by parameter type. That reads better than
inventing `SimpleCurveEditor` or `CurvePlot` purely to dodge a name collision.

## Testing

The chosen verification approach is unit tests on pure helpers plus careful review. No headless
ImGui harness.

**What this costs, stated plainly.** Both Tier 2 Criticals lived in draw paths no test could
reach. Tier 3's pointer contract is riskier than anything in Tier 2. This tier will ship with its
highest-risk code — the `Get` callback under live drag — verified by reading alone.

**How the design compensates.** Logic is deliberately pushed out of the draw path into pure
functions, so the untestable surface is as thin as it can be:

| Pure, tested | Why it matters |
|---|---|
| `FillRanges(source, span)` | Wrong fill means every clip renders at the wrong position |
| `ComputeRangeEdits(before, after)` | Decides which `SetItemRange` calls fire; a wrong diff silently drops or invents user edits |
| Null-guard dispatch in `Get` | The Tier 1 crash shape; must survive all three upstream null combinations |
| UTF-8 label encoding | `GetItemLabel` returns `ReadOnlySpan<byte>` upstream |
| `SequencerFeatures` ↔ `SequencerOptions` | Numeric gaps at `1 << 0` and `1 << 2` make a naive cast wrong |
| `CurveInterpolation` ↔ `ImCurveEdit.CurveType` | Every member, so a new upstream value cannot silently mis-map |
| `CurveShape` ↔ `Mathematics.CurveType` | The *other* `CurveType`; a shared mirror would mis-map |
| `CurvePointKind` ↔ `CurvePointType` | Dropping it would silently discard corner-vs-smooth authoring |
| `Srgb` ↔ packed `uint` | Already covered by Tier 1's conversion rules; reused here |
| `CurveData` point operations | Add / move / remove / sample, independent of any ImGui context |

**Standing rules carried forward, both earned:**

1. For any callback- or marshalling-shaped upstream API, read **every call site of the callback**,
   not just its signature. The Tier 1 `FlameGraph` Critical was a guaranteed crash found only this
   way — and this tier has three `Get` call sites with three different null combinations.
2. Verifying that callback state is *populated* when it fires is not enough. Tier 2's Criticals
   were both about whether the value was *well-formed for the type we declare it as*. For Tier 3
   that means: is the pointer we hand back still valid, and does the value we read back through it
   round-trip into the consumer's model.

## Demo

There is no ktsu counterpart to any of these, so nothing goes in the "Hexa vs ktsu" comparison
tab. All four land in the Net New gallery in `examples/ImGuiWidgetsDemo`:

- A sequencer over a small in-memory clip list, with drag-to-edit writing back through
  `SetItemRange` and the result printed below so the round-trip is visible.
- A multi-curve editor over two or three curves with distinct colours.
- A single-curve `CurveData` field and a `BezierControlPoints` field.

The demo already calls `ImGuiWidgets.DrawDeferred()`; none of these widgets need a pump — they are
immediate-mode calls that happen to take a callback object.

## Out of scope

- Tier 4 (`TextEditor`, `TextEditorTab`, `TextSource`, `SyntaxHighlight`) — ~4,600 LOC trafficking
  in `StdWString*`, an application component rather than a widget.
- `ImSequencer`'s custom-draw virtuals and `FormatCollapse`.
- `ImCurveEdit`'s selected-points out-parameter and coordinate helpers.
- A headless ImGui test harness. Considered and declined for this tier; it remains the one change
  that would retroactively close the Tier 1–3 verification gap.
- Retiring or deprecating any existing ktsu widget.
