# A diff view widget, with selectable hunks

Status: approved design, not yet implemented.

## Why this exists

A caller with a diff in hand has nowhere to draw it. Nothing in this library renders one, so every
application that wants to show a change builds its own table, picks its own colors, and works out its
own alignment rules. Two have already done so.

The immediate consumer is a launcher's source control tab, which reads a patch from
`ktsu.GitIntegration`, shows it, lets a person tick the hunks they want, and hands those back to be
staged. Nothing in this widget is specific to that. It draws lines someone else computed.

## Scope

In scope:

- A model of hunks and lines that any diff can be mapped onto.
- A unified view, which is the shape a patch already has.
- A side-by-side view of the same hunks, aligned with filler rows.
- Selecting whole hunks, as caller-owned state the widget mutates.

Out of scope, deliberately:

- Computing a diff. The widget draws one it is given. Callers already have DiffPlex, git, or their
  own comparison, and putting a differ here would mean choosing one for everybody.
- Word-level highlighting inside a changed line. Worth having one day, and it needs an intra-line
  comparison this widget has said it will not do.
- Syntax highlighting. `ktsu.ImGui.SyntaxHighlighting` exists and can layer over the text column
  later without this widget knowing.
- Editing, take-across arrows, and three-way merge. That is ProjectDirector's feature, it solves
  propagating a file between repositories rather than staging part of one, and it stays there.
- Selecting individual lines. Staging a line subset means synthesizing a hunk with recomputed `@@`
  counts, which `ktsu.GitIntegration` deliberately does not do, so offering it here would promise
  something no consumer could honor.

## The model

```
DiffLineKind      Context | Added | Removed
DiffLine          Kind, Text, OldNumber, NewNumber
DiffHunk          Lines, Heading
```

```csharp
public enum DiffLineKind
{
	Context,
	Added,
	Removed,
}

public sealed record DiffLine
{
	public required DiffLineKind Kind { get; init; }
	public required string Text { get; init; }
	public int? OldNumber { get; init; }
	public int? NewNumber { get; init; }
}

public sealed record DiffHunk
{
	public required IReadOnlyList<DiffLine> Lines { get; init; }
	public string Heading { get; init; } = string.Empty;
}
```

`Text` carries the line's content without the leading `+`, `-` or space. The widget draws the marker
itself, in its own column, so a caller mapping from a source that keeps the marker strips it once
rather than having the widget guess whether a line beginning with `-` is a removal or a line of
content that happens to start with a hyphen.

`OldNumber` and `NewNumber` are null on the side where the line does not exist. A caller with no line
numbers to give leaves both null and the number columns render empty.

`Heading` is whatever the source puts after a hunk's range, which for git is the enclosing function.
It is shown beside the hunk and is otherwise unused.

### Why this model and not the patch itself

The obvious alternative is to take unified diff text and parse it here. That is rejected for two
reasons. It would put a diff parser in a widgets library, duplicating one that
`ktsu.GitIntegration` already ships and tests against fixtures captured from a real git. And it would
tie the widget to git's format, when DiffPlex, a managed comparison, or a hand-built change list are
all things a caller legitimately has.

The model is shaped deliberately like `GitIntegration`'s `GitHunk` and `GitPatchLine` so that
mapping is a field-for-field copy, while carrying none of its concepts: no patch text, no file path,
no binary or conflicted flags, no change kind. Those belong to the patch, and the patch belongs to
the caller.

## The API

```csharp
public static bool DiffView(
	string label,
	IReadOnlyList<DiffHunk> hunks,
	ISet<int> selection,
	DiffViewOptions? options = null);

public static void DiffView(
	string label,
	IReadOnlyList<DiffHunk> hunks,
	DiffViewOptions? options = null);
```

The first returns whether the selection changed this frame and mutates `selection` in place, holding
the indices into `hunks` that are ticked. The second draws without any selection at all.

This follows `CurveTrack`, which takes the caller's points and mutates them, rather than handing back
a state object. The reason is the same: the selection is the one thing the caller needs after the
frame ends, since it is what feeds `PatchFor`, and everything else the widget remembers is transient.

The selection holds indices, so it goes stale when the hunks change underneath it, which happens
every time the caller re-reads the patch after staging something. The widget does not try to track
that. It draws checkboxes for the indices it is given and ignores any that are out of range, and a
caller re-reading a patch clears the selection, because a tick against hunk 3 of the old patch means
nothing about hunk 3 of the new one. Saying so here is cheaper than a heuristic that is wrong
occasionally and silently.

```csharp
public sealed record DiffViewOptions
{
	public DiffViewMode Mode { get; init; } = DiffViewMode.Unified;
	public bool CanShowLineNumbers { get; init; } = true;
	public bool CanCollapseHunks { get; init; } = true;
}

public enum DiffViewMode
{
	Unified,
	SideBySide,
}
```

One entry point with a mode rather than two methods, so a caller offering a toggle changes a field
instead of branching, and so the two modes cannot drift apart as they are maintained.

### State

Transient interaction state lives in a `Dictionary<uint, DiffViewState>` inside
`DiffViewImpl`, keyed by the widget's ImGui id, which is how `CurveTrack` and `HandleTrack` already
do it. It holds which hunks are collapsed, and nothing else. The side-by-side view is one table rather than
two panes, so there are no scroll positions to keep in step. None of this is worth a caller
persisting, and a caller who wants a hunk to start collapsed can collapse it by not expanding it.

`DiffViewState` itself is free of ImGui, like every other state type here, which is what lets its
rules be tested without a graphics context.

## Rendering

### Unified

Each hunk draws a header, then its lines.

The header carries the selection checkbox when the widget is selectable, the heading text, and a
summary bar: a run of `-` and a run of `+` proportional to the hunk's removed and added counts,
scaled to at most ten characters. That bar is taken from ProjectDirector, where it reads at a glance
in a way two numbers do not. Clicking the header collapses the hunk when `CanCollapseHunks`.

The lines are a table of four columns: old number, new number, marker, and text. Each row's
background is tinted by kind, green for added, red for removed, and a faint neutral for context.

The tints are built rather than hardcoded. A fixed green over a light theme is either invisible or
garish, and this library ships `ktsu.ImGui.Color` and `ktsu.ImGui.Styler` precisely so a widget does
not guess. Added and removed take a fixed hue with their saturation and value derived from the
current style's `ImGuiCol.FrameBg`, so they read as a tint of the surface they sit on, and context
takes `ImGuiCol.FrameBg` itself at a low alpha. Filler is the same as context at half that alpha.

### Side by side

The same hunks, drawn as two columns of the same table so both sides scroll together and no
scroll synchronization is needed.

Pairing is the interesting part. Walk a hunk's lines and accumulate runs:

- A context line emits one row carrying that line on both sides.
- A run of `R` removed lines followed by `A` added lines emits `max(R, A)` rows, pairing them by
  index, with a filler row on whichever side runs out first.

A filler row is empty, has no line number, and is tinted more faintly than a real change, so the two
sides stay vertically aligned without the gap reading as content. This is ProjectDirector's
alignment trick, applied to one hunk rather than to a whole file, which is what makes it work from a
patch: a hunk carries both sides of its own change, so nothing outside it is needed.

### Large hunks

Each hunk's line table is drawn through `ImGuiListClipper`, so a hunk of several thousand lines costs
what is on screen rather than what exists. A staging view meets large hunks routinely, on a generated
file or a bulk edit, and the alternative is a frame that grows with the diff.

The cost is that clipping and per-row background colors interact awkwardly, and that neither can be
verified by a unit test. This is the one part of the widget where the demo application is the test.

## What is logic and what is drawing

Three rules go in `DiffViewState`, which has no ImGui in it and is unit tested:

- **Pairing.** A hunk in, the side-by-side rows out, including filler. Every alignment question is
  settled here rather than inside a draw loop.
- **Summarizing.** Removed and added counts in, the two bar lengths out, clamped so their sum never
  exceeds ten and neither is zero when its count is not.
- **Selection.** Toggling one hunk, selecting all, selecting none, and reporting whether a change
  occurred.

Everything else is drawing and is exercised by the demo application rather than asserted.

## Testing

Unit tests in `tests/ImGui.Widgets.Tests` cover the three rules. The cases that matter are the ones
where a naive implementation is wrong:

- A hunk of only added lines, and one of only removed lines, where pairing has nothing to pair with
  and every row on one side is filler.
- Unequal runs in both directions, where `max(R, A)` decides the row count.
- Two separate runs in one hunk, so the walk resets its accumulation rather than pairing across a
  context line between them.
- A hunk with a single changed line and no context at all, which is what zero-context callers produce.
- Summarizing when one side is zero, which must not round the other side down to nothing.
- Summarizing counts far above ten, where the proportions hold and the total stays at ten.

A page in `ImGuiWidgetsDemo` shows both modes over a fixture diff with a selection, several hunks, an
addition-only hunk, and one long enough to exercise clipping. It is how the drawing gets looked at,
which matters because nothing else here can look at it.

## File layout

| File | Responsibility |
|---|---|
| `ImGui.Widgets/DiffView.cs` | `DiffView`, its options, and `DiffViewImpl` |
| `ImGui.Widgets/DiffViewState.cs` | Pairing, summarizing, selection, free of ImGui |
| `ImGui.Widgets/DiffViewModel.cs` | `DiffLine`, `DiffHunk`, `DiffLineKind` |
| `tests/ImGui.Widgets.Tests/DiffViewStateTests.cs` | The three rules |
| `examples/ImGuiWidgetsDemo/` | A page showing both modes |
| `ImGui.Widgets/README.md` | An entry under Display and Status |

## Implementation order

1. The model and `DiffViewState`, with no ImGui and no drawing. Pairing and summarizing are the only
   real logic here and are worth having tested before anything renders them.
2. The unified view, which is what the first consumer needs.
3. The side-by-side view, which is the same data through the pairing rule from step 1.
4. The demo page and the README entry.

Step 1 needs no graphics context at all. Step 3 is where a mistake in the pairing rule becomes
visible, which is why the rule is built and tested before either view draws it.
