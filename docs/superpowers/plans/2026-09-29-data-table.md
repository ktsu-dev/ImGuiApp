# Data table widget implementation plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Give this library a table of typed rows that sorts and filters itself, moves an active cell with the keyboard, edits one cell at a time in place, and reports each edit to the caller instead of writing it.

**Architecture:** A state class with no ImGui calls owns the view (sorted and filtered source indices), the active cell, the selection, and the editing state, and is driven by commands. A renderer draws one frame from it on `BeginTable` and the list clipper, turns input into commands, and hosts the one editor that exists at a time. Keyboard commands follow a `ktsu.Keybinding` keymap when one is given, through a chord matcher shared with the node editor.

**Tech Stack:** C#, net10.0, net9.0 and net8.0, MSTest, `Hexa.NET.ImGui`, `ktsu.TextFilter`, `ktsu.Keybinding`, `ktsu.ImGui.App.Testing` for the headless UI suite, and `ktsu.UndoRedo` in the demo only.

**Spec:** `docs/superpowers/specs/2026-09-29-data-table-design.md`

## Global Constraints

Every task's requirements implicitly include this section.

- Copyright header on every new file, exactly: `// Copyright (c) 2023-2026 ktsu-dev contributors`
- Namespaces: `ktsu.ImGui.Widgets` for library files, `ktsu.ImGui.Widgets.Tests` and `ktsu.ImGui.Widgets.UITests` for tests. File-scoped, with every `using` **inside** the namespace declaration.
- Indent with tabs, never spaces. CRLF line endings.
- Every widget file is `public static partial class ImGuiWidgets`, and every public type the widget exposes is nested inside it. Internal helpers are nested there too, except `KeyChordMatcher`, which isn't part of any one widget.
- `DataTableState<TRow>` contains **no ImGui calls at all**. That's what lets its rules be tested without a graphics context.
- Every public member has an XML doc comment. Comments explain why, not what.
- US spelling in identifiers, comments and user-facing strings.
- **No semicolons or dashes joining clauses in prose**, including XML doc comments and code comments.
- Booleans are named `Is`, `Can` or `Has` followed by the thing they describe. The one exception is `DataTableCommands.Register`'s `bindDefaultChords`, which matches `NodeEditorCommands.Register` exactly so the two read as a pair.
- Null guards are `Ensure.NotNull(x)` from Polyfill. `KTSU0003` rejects `ArgumentNullException.ThrowIfNull` at error level in library code.
- Always use braces. No nested ternaries (Sonar flags them, and warnings are errors).
- Warnings are errors. The build fails on an unused `using` and on formatting.
- Adding public members needs no `CompatibilitySuppressions.xml` change. Only removing or changing one does.
- Commit messages carry a version tag. Tasks that add public API use `[minor]`, the rest `[patch]`. **Do not add `Co-Authored-By` lines.**
- Do not edit `VERSION.md`, `CHANGELOG.md` or `LICENSE.md`.
- Build a project with `dotnet build <path to csproj>`. The test projects are Microsoft Testing Platform executables. Build them, then run:
  - `./tests/ImGui.Widgets.Tests/bin/Debug/net10.0/ktsu.ImGui.Widgets.Tests`
  - `./tests/ImGui.Widgets.UITests/bin/Debug/net10.0/ktsu.ImGui.Widgets.UITests`
  - `./tests/ImGui.NodeEditor.Tests/bin/Debug/net10.0/ktsu.ImGui.NodeEditor.Tests`
  - each optionally with `--filter "FullyQualifiedName~<ClassName>"`
- Column labels are unique within a table. They're both ImGui ids and probe names.
- Probe names: the table is `label`, a header is `label/<column>`, a filter box is `label/filter/<column>`, a cell is `label/[<sourceIndex>]/<column>`.

### Refinements to the spec

Each of these was decided while planning, and the spec should be read with them applied.

1. **Boolean names follow the repo rule.** The spec's `Sortable` becomes `CanSort` and moves to the base `DataTableColumn<TRow>`, because sorting doesn't depend on the value type. `ShowFilterRow` becomes `HasFilterRow`. The matcher's `repeat` parameter becomes `canRepeat`.
2. **The matcher gains `isShiftIgnored` as well as `canRepeat`.** The spec says Shift with a movement chord extends the selection, but the matcher requires modifiers to match exactly, so without this a Shift+Down would never match the Down chord.
3. **String and numeric cells edit through a text box, parsed with `InvariantCulture` on commit.** The spec said the built-in editors use `PropertyGrid`'s widgets. `InputInt` and `InputFloat` can't hold a partial entry such as `-` or `1.`, and can't start from a typed character, so text it is. Text that doesn't parse commits nothing. `bool` cells use a checkbox, and enum cells use a combo.
4. **The filter row defaults to fuzzy matching**, which is a case-insensitive match of the typed characters in order (`ob` matches `Bob`). The search box's own right-click menu switches a column to glob or regex. The spec left the default open.
5. **The table starts unsorted.** It sets `ImGuiTableFlags.SortTristate`, so a header click cycles ascending, descending, and back to source order. Without it ImGui sorts on the first column the moment the table appears.
6. **Tab stops are switched off inside the table** with `ImGuiItemFlags.NoTabStop`. Otherwise ImGui's own tabbing would hand the keyboard to a filter box when Tab moves out of an editor.
7. **Copied rows are joined with `\n`**, with no trailing newline.

## Review Focus

Five inputs the spec implies that no task's happy path exercises, most likely to bite first.

1. **An `OnEdit` callback that applies the edit and calls `Refresh()`, or that throws.** The first is the documented way to use the table. The state must have left editing before the callback runs, or a rebuild inside it sees a half-finished edit. Tests in Task 5.
2. **Typing into a filter box while a cell is active.** The arrow keys and typed characters must go to the filter, not move the active cell or start an edit in it. Test in Task 9.
3. **Pressing Tab while editing.** The active cell must move right and the next arrow key must still move the table, rather than ImGui's tabbing handing the keyboard to some other item. Test in Task 9.
4. **Typing a character to begin editing, then carrying on typing.** ImGui selects all the text of an input it focuses, so without care the second character replaces the first. Test in Task 9.
5. **The caller removing rows while a cell near the end is active.** The active cell's source index can then be past the end of the list, and drawing must not index past it. Test in Task 4.

---

## File Structure

| File | Responsibility |
|---|---|
| `ImGui.Widgets/KeyChordMatcher.cs` | **Create.** Moved from the node editor. Chord to ImGui key checks, with repeat and optional Shift |
| `ImGui.Widgets/ImGui.Widgets.csproj` | **Modify.** Reference `ktsu.Keybinding`, open internals to the node editor and the UI tests |
| `ImGui.NodeEditor/NodeEditorCommands.cs` | **Modify.** Remove the matcher |
| `ImGui.NodeEditor/NodeEditorInputHandler.cs` | **Modify.** Use the shared matcher |
| `ImGui.Widgets/DataTableColumn.cs` | **Create.** `DataTableCell`, `DataTableEdit`, `DataTableEditor`, both column classes |
| `ImGui.Widgets/DataTableText.cs` | **Create.** Invariant formatting and parsing, and quoting for copy |
| `ImGui.Widgets/DataTableEditSession.cs` | **Create.** The typed editing sessions |
| `ImGui.Widgets/DataTableEditors.cs` | **Create.** The built-in text and enum editors |
| `ImGui.Widgets/DataTableState.cs` | **Create.** Columns, filters, sorting, and the view |
| `ImGui.Widgets/DataTableState.Navigation.cs` | **Create.** Active cell, selection, movement, copy |
| `ImGui.Widgets/DataTableState.Editing.cs` | **Create.** Begin, commit, cancel, toggle, context menu snapshot |
| `ImGui.Widgets/DataTableCommands.cs` | **Create.** Command ids, default chords, registration |
| `ImGui.Widgets/TableClipping.cs` | **Create.** Clipper setup and row naming shared with `VirtualTable` |
| `ImGui.Widgets/VirtualTable.cs` | **Modify.** Use `TableClipping` |
| `ImGui.Widgets/DataTable.cs` | **Create.** `DataTable`, `DataTableOptions`, and the renderer |
| `ImGui.Widgets/DataTable.Input.cs` | **Create.** Keyboard handling and the context menu |
| `tests/ImGui.Widgets.Tests/KeyChordMatcherTests.cs` | **Create.** Key name mapping |
| `tests/ImGui.Widgets.Tests/DataTableMood.cs`, `DataTablePerson.cs`, `DataTableFixture.cs` | **Create.** Shared test data |
| `tests/ImGui.Widgets.Tests/DataTableColumnTests.cs` | **Create.** Columns, text, sessions |
| `tests/ImGui.Widgets.Tests/DataTableStateViewTests.cs` | **Create.** Filtering, sorting, rebuild triggers |
| `tests/ImGui.Widgets.Tests/DataTableStateNavigationTests.cs` | **Create.** Movement, selection, identity, copy |
| `tests/ImGui.Widgets.Tests/DataTableStateEditingTests.cs` | **Create.** The editing state machine |
| `tests/ImGui.Widgets.Tests/DataTableCommandsTests.cs` | **Create.** Registration and default chords |
| `tests/ImGui.Widgets.UITests/KeyChordMatcherTests.cs` | **Create.** Repeat and Shift, in a real frame |
| `tests/ImGui.Widgets.UITests/DataTablePerson.cs` | **Create.** UI test data |
| `tests/ImGui.Widgets.UITests/DataTableTests.cs` | **Create.** The widget end to end |
| `examples/ImGuiWidgetsDemo/DataTableDemo.cs` | **Create.** A page with undo and redo |
| `examples/ImGuiWidgetsDemo/ImGuiWidgetsDemo.cs`, `ImGuiWidgetsDemo.csproj` | **Modify.** Call the page, reference `ktsu.UndoRedo` |
| `tests/ImGuiWidgetsDemo.UITests/WidgetsDemoUITests.cs` | **Modify.** Register the section |
| `ImGui.Widgets/README.md`, `CLAUDE.md` | **Modify.** Document the widget and the dependency |

---

## Task 1: Share the key chord matcher, with repeat and optional Shift

**Files:**
- Create: `ImGui.Widgets/KeyChordMatcher.cs`
- Modify: `ImGui.Widgets/ImGui.Widgets.csproj`
- Modify: `ImGui.NodeEditor/NodeEditorCommands.cs:97-211` (remove the matcher)
- Modify: `ImGui.NodeEditor/NodeEditorInputHandler.cs:3-11` (usings)
- Modify: `CLAUDE.md:129` (key files line)
- Test: `tests/ImGui.Widgets.Tests/KeyChordMatcherTests.cs`
- Test: `tests/ImGui.Widgets.UITests/KeyChordMatcherTests.cs`

**Interfaces:**
- Consumes: `ktsu.Keybinding.Core.Models.Chord` (with `Notes`, and `static Chord Parse(string)`).
- Produces:
  - `internal static class KeyChordMatcher` in namespace `ktsu.ImGui.Widgets`
  - `public static bool IsPressed(Chord chord, bool canRepeat = false, bool isShiftIgnored = false)`
  - `public static bool TryMapKey(string name, out ImGuiKey key)`

- [ ] **Step 1: Record the baseline**

Build and run all three suites before touching anything, and note the pass counts:

```bash
dotnet build ImGui.sln
./tests/ImGui.Widgets.Tests/bin/Debug/net10.0/ktsu.ImGui.Widgets.Tests
./tests/ImGui.Widgets.UITests/bin/Debug/net10.0/ktsu.ImGui.Widgets.UITests
./tests/ImGui.NodeEditor.Tests/bin/Debug/net10.0/ktsu.ImGui.NodeEditor.Tests
```

Expected: all pass, 0 warnings. If anything fails already, stop and report it rather than building on it.

- [ ] **Step 2: Write the failing unit test for key names**

Create `tests/ImGui.Widgets.Tests/KeyChordMatcherTests.cs`:

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using Hexa.NET.ImGui;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests how the shared matcher names keys. Pure, so no ImGui context is required.
/// </summary>
[TestClass]
public class KeyChordMatcherTests
{
	[TestMethod]
	[DataRow("A", ImGuiKey.A)]
	[DataRow("7", ImGuiKey.Key7)]
	[DataRow("UP", ImGuiKey.UpArrow)]
	[DataRow("PAGEUP", ImGuiKey.PageUp)]
	[DataRow("HOME", ImGuiKey.Home)]
	[DataRow("F2", ImGuiKey.F2)]
	[DataRow("ESCAPE", ImGuiKey.Escape)]
	[DataRow("SPACE", ImGuiKey.Space)]
	public void TryMapKey_MapsTheNamesAKeymapUses(string name, ImGuiKey expected)
	{
		Assert.IsTrue(KeyChordMatcher.TryMapKey(name, out ImGuiKey key), $"'{name}' did not map to any key.");
		Assert.AreEqual(expected, key);
	}

	[TestMethod]
	[DataRow("LEFTCTRL")]
	[DataRow("NOTAKEY")]
	public void TryMapKey_RejectsModifiersAndUnknownNames(string name) =>
		Assert.IsFalse(KeyChordMatcher.TryMapKey(name, out _), $"'{name}' should not map to a key a chord can press.");
}
```

- [ ] **Step 3: Write the failing UI test for repeat and Shift**

Create `tests/ImGui.Widgets.UITests/KeyChordMatcherTests.cs`:

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using Hexa.NET.ImGui;

using ktsu.ImGui.App.Testing;
using ktsu.Keybinding.Core.Models;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Drives the shared chord matcher through real frames, because repeat and modifier state only
/// exist inside an ImGui context.
/// </summary>
[TestClass]
public sealed class KeyChordMatcherTests : WidgetTest
{
	private Chord chord = Chord.Parse("Down");
	private bool canRepeat;
	private bool isShiftIgnored;
	private int presses;

	private void CountPresses()
	{
		if (KeyChordMatcher.IsPressed(chord, canRepeat, isShiftIgnored))
		{
			presses++;
		}
	}

	private void StartCounting()
	{
		Start(CountPresses);
		presses = 0;
	}

	/// <summary>
	/// Holds the key for a full second at the harness's fixed sixty frames a second, which is well past
	/// ImGui's repeat delay.
	/// </summary>
	private void HoldDownArrow()
	{
		HarnessKeyboard.KeyDown(ImGuiKey.DownArrow);
		Step(60);
		HarnessKeyboard.KeyUp(ImGuiKey.DownArrow);
		Step();
	}

	[TestMethod]
	public void IsPressed_HeldWithoutRepeat_FiresOnce()
	{
		StartCounting();

		HoldDownArrow();

		Assert.AreEqual(1, presses, "A chord that cannot repeat fired more than once while held.");
	}

	[TestMethod]
	public void IsPressed_HeldWithRepeat_KeepsFiring()
	{
		canRepeat = true;
		StartCounting();

		HoldDownArrow();

		Assert.IsGreaterThan(1, presses, "A repeating chord fired only once in a second of holding.");
	}

	[TestMethod]
	public void IsPressed_WithShiftHeld_DoesNotMatchAChordWithoutShift()
	{
		StartCounting();

		Harness.Keyboard.Press(ImGuiKey.DownArrow, shift: true);

		Assert.AreEqual(0, presses, "Shift+Down matched a Down chord, so modifiers are not matched exactly.");
	}

	[TestMethod]
	public void IsPressed_WithShiftIgnored_MatchesWithShiftHeld()
	{
		isShiftIgnored = true;
		StartCounting();

		Harness.Keyboard.Press(ImGuiKey.DownArrow, shift: true);

		Assert.AreEqual(1, presses, "Shift+Down did not match a Down chord that ignores Shift.");
	}

	[TestMethod]
	public void IsPressed_WithShiftIgnored_StillRequiresOtherModifiersToMatch()
	{
		isShiftIgnored = true;
		StartCounting();

		Harness.Keyboard.Press(ImGuiKey.DownArrow, ctrl: true);

		Assert.AreEqual(0, presses, "Ignoring Shift also ignored Ctrl.");
	}

	[TestMethod]
	public void IsPressed_AChordWithShift_MatchesOnlyWithShiftHeld()
	{
		chord = Chord.Parse("Shift+Tab");
		StartCounting();

		Harness.Keyboard.Press(ImGuiKey.Tab);
		Assert.AreEqual(0, presses, "Tab alone matched Shift+Tab.");

		Harness.Keyboard.Press(ImGuiKey.Tab, shift: true);
		Assert.AreEqual(1, presses, "Shift+Tab did not match Shift+Tab.");
	}
}
```

- [ ] **Step 4: Run the tests to verify they fail**

Run: `dotnet build tests/ImGui.Widgets.Tests/ImGui.Widgets.Tests.csproj`
Expected: FAIL to compile, `KeyChordMatcher` does not exist in `ktsu.ImGui.Widgets`.

- [ ] **Step 5: Reference the keymap package and open internals**

In `ImGui.Widgets/ImGui.Widgets.csproj`, add to the `InternalsVisibleTo` item group:

```xml
    <InternalsVisibleTo Include="ktsu.ImGui.NodeEditor" />
    <InternalsVisibleTo Include="ktsu.ImGui.Widgets.UITests" />
```

and add to the package item group, after `ktsu.Extensions`:

```xml
    <PackageReference Include="ktsu.Keybinding" />
```

The version is already pinned in `Directory.Packages.props`.

- [ ] **Step 6: Create the shared matcher**

Create `ImGui.Widgets/KeyChordMatcher.cs`:

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;
using System.Collections.Generic;
using System.Linq;

using Hexa.NET.ImGui;

using ktsu.Keybinding.Core.Models;

/// <summary>
/// Answers whether a <c>ktsu.Keybinding</c> chord was pressed this frame, in ImGui's terms.
/// </summary>
/// <remarks>
/// A chord matches when its modifiers are exactly the ones held, so Ctrl+Z does not fire for
/// Ctrl+Shift+Z, every other key in it is down, and at least one of them went down this frame.
/// Shared by the node editor and the data table, which is why it lives here rather than in either.
/// </remarks>
internal static class KeyChordMatcher
{
	private static readonly Dictionary<string, ImGuiKey> Aliases = new(StringComparer.OrdinalIgnoreCase)
	{
		["ESC"] = ImGuiKey.Escape,
		["RETURN"] = ImGuiKey.Enter,
		["DEL"] = ImGuiKey.Delete,
		["INS"] = ImGuiKey.Insert,
		["UP"] = ImGuiKey.UpArrow,
		["DOWN"] = ImGuiKey.DownArrow,
		["LEFT"] = ImGuiKey.LeftArrow,
		["RIGHT"] = ImGuiKey.RightArrow,
		["ARROWUP"] = ImGuiKey.UpArrow,
		["ARROWDOWN"] = ImGuiKey.DownArrow,
		["ARROWLEFT"] = ImGuiKey.LeftArrow,
		["ARROWRIGHT"] = ImGuiKey.RightArrow,
		["PGUP"] = ImGuiKey.PageUp,
		["PGDN"] = ImGuiKey.PageDown,
		["SPACEBAR"] = ImGuiKey.Space,
	};

	/// <summary>Reports whether a chord was pressed this frame.</summary>
	/// <param name="chord">The chord to test.</param>
	/// <param name="canRepeat">
	/// Whether a held chord fires again at the platform's repeat rate. Off for commands that should act
	/// once per press, such as undo, and on for movement, where holding an arrow should keep moving.
	/// </param>
	/// <param name="isShiftIgnored">
	/// Whether Shift may be held or not without affecting the match. Lets one movement chord serve for
	/// moving and, with Shift, for extending a selection. Every other modifier must still match.
	/// </param>
	/// <returns>True when the chord was pressed this frame.</returns>
	public static bool IsPressed(Chord chord, bool canRepeat = false, bool isShiftIgnored = false)
	{
		Ensure.NotNull(chord);

		bool ctrl = false;
		bool alt = false;
		bool shift = false;
		bool meta = false;
		List<ImGuiKey> keys = [];

		foreach (string name in chord.Notes.Select(note => note.ToString()))
		{
			switch (name)
			{
				case "CTRL" or "CONTROL":
					ctrl = true;
					break;

				case "ALT":
					alt = true;
					break;

				case "SHIFT":
					shift = true;
					break;

				case "META" or "WIN" or "WINDOWS" or "CMD" or "COMMAND" or "SUPER":
					meta = true;
					break;

				default:
					if (!TryMapKey(name, out ImGuiKey key))
					{
						// A key ImGui has no name for can never be pressed, so neither can the chord.
						return false;
					}

					keys.Add(key);
					break;
			}
		}

		if (keys.Count == 0)
		{
			return false;
		}

		ImGuiIOPtr io = ImGui.GetIO();
		if (io.KeyCtrl != ctrl || io.KeyAlt != alt || io.KeySuper != meta || (!isShiftIgnored && io.KeyShift != shift))
		{
			return false;
		}

		return keys.All(ImGui.IsKeyDown) && keys.Any(key => ImGui.IsKeyPressed(key, canRepeat));
	}

	/// <summary>Finds the ImGui key a note names.</summary>
	/// <param name="name">The note's name, upper-cased as <c>ktsu.Keybinding</c> stores it.</param>
	/// <param name="key">The key.</param>
	/// <returns>True if ImGui has such a key.</returns>
	public static bool TryMapKey(string name, out ImGuiKey key)
	{
		if (name.Length == 1 && name[0] is >= 'A' and <= 'Z')
		{
			key = (ImGuiKey)((int)ImGuiKey.A + (name[0] - 'A'));
			return true;
		}

		if (name.Length == 1 && name[0] is >= '0' and <= '9')
		{
			key = (ImGuiKey)((int)ImGuiKey.Key0 + (name[0] - '0'));
			return true;
		}

		if (Aliases.TryGetValue(name, out key))
		{
			return true;
		}

		// Everything else by ImGui's own name, such as DELETE, BACKSPACE, F5, PAGEUP and COMMA, but never
		// a modifier, a mouse button or one of the range markers, none of which is a key to press.
		return Enum.TryParse(name, ignoreCase: true, out key)
			&& key > ImGuiKey.NamedKeyBegin
			&& key < ImGuiKey.GamepadStart
			&& key is not (ImGuiKey.LeftCtrl or ImGuiKey.RightCtrl or ImGuiKey.LeftShift or ImGuiKey.RightShift
				or ImGuiKey.LeftAlt or ImGuiKey.RightAlt or ImGuiKey.LeftSuper or ImGuiKey.RightSuper);
	}
}
```

- [ ] **Step 7: Remove the node editor's copy**

In `ImGui.NodeEditor/NodeEditorCommands.cs`, delete everything from the `/// <summary>` that begins "Answers whether a `ktsu.Keybinding` chord was pressed" (line 97) to the end of the file, so the file ends after `NodeEditorCommands`' closing brace. Then remove `using Hexa.NET.ImGui;` and `using System;`, which nothing left in the file uses.

In the same file's class remarks, nothing mentions the matcher, so no other edit is needed there.

In `ImGui.NodeEditor/NodeEditorInputHandler.cs`, add after `using Hexa.NET.ImNodes;`:

```csharp
using ktsu.ImGui.Widgets;
```

Its call `KeyChordMatcher.IsPressed(chord)` keeps working unchanged, because both new parameters default to the old behavior.

- [ ] **Step 8: Update the key files line in CLAUDE.md**

Replace line 129 of `CLAUDE.md`:

```markdown
- `ImGui.NodeEditor/NodeEditorCommands.cs` - The keyboard commands as `ktsu.Keybinding` commands
- `ImGui.Widgets/KeyChordMatcher.cs` - The chord-to-ImGui matcher shared by the node editor and the data table, with optional key repeat and optional Shift
```

- [ ] **Step 9: Run the tests to verify they pass**

```bash
dotnet build ImGui.sln
./tests/ImGui.Widgets.Tests/bin/Debug/net10.0/ktsu.ImGui.Widgets.Tests --filter "FullyQualifiedName~KeyChordMatcherTests"
./tests/ImGui.Widgets.UITests/bin/Debug/net10.0/ktsu.ImGui.Widgets.UITests --filter "FullyQualifiedName~KeyChordMatcherTests"
./tests/ImGui.NodeEditor.Tests/bin/Debug/net10.0/ktsu.ImGui.NodeEditor.Tests
```

Expected: the new tests pass, the node editor suite still passes in full, 0 warnings. If `Assert.IsGreaterThan` doesn't exist in this MSTest version, use `Assert.IsTrue(presses > 1, ...)` and say so in the commit message.

- [ ] **Step 10: Commit**

```bash
git add ImGui.Widgets/KeyChordMatcher.cs ImGui.Widgets/ImGui.Widgets.csproj ImGui.NodeEditor/NodeEditorCommands.cs ImGui.NodeEditor/NodeEditorInputHandler.cs CLAUDE.md tests/ImGui.Widgets.Tests/KeyChordMatcherTests.cs tests/ImGui.Widgets.UITests/KeyChordMatcherTests.cs
git commit -m "[patch] Share the key chord matcher and let held chords repeat"
```

---

## Task 2: Columns, cell text, and edit sessions

**Files:**
- Create: `ImGui.Widgets/DataTableColumn.cs`
- Create: `ImGui.Widgets/DataTableText.cs`
- Create: `ImGui.Widgets/DataTableEditSession.cs`
- Create: `ImGui.Widgets/DataTableEditors.cs`
- Create: `tests/ImGui.Widgets.Tests/DataTableMood.cs`
- Create: `tests/ImGui.Widgets.Tests/DataTablePerson.cs`
- Create: `tests/ImGui.Widgets.Tests/DataTableFixture.cs`
- Test: `tests/ImGui.Widgets.Tests/DataTableColumnTests.cs`

**Interfaces:**
- Consumes: nothing from earlier tasks.
- Produces (all nested in `ImGuiWidgets`):
  - `public readonly record struct DataTableCell(int SourceIndex, int Column)`
  - `public readonly record struct DataTableEdit<TRow, TValue>(int SourceIndex, TRow Row, TValue OldValue, TValue NewValue)`
  - `public delegate bool DataTableEditor<TValue>(string id, ref TValue value)`
  - `public abstract class DataTableColumn<TRow>` with `Label`, `Flags`, `Width`, `CanSort`, `GetText(TRow)`, and internal `IsEditable`, `IsToggle`, `Compare`, `Validate()`, `CreateSession(int column, int sourceIndex, TRow row, char? firstCharacter)`, `Toggle(int sourceIndex, TRow row)`, `DrawContent(TRow row)`
  - `public sealed class DataTableColumn<TRow, TValue>` with `Value`, `Format`, `Comparer`, `OnEdit`, `Editor`, `DrawCell`
  - `internal static class DataTableText` with `FormatInvariant<TValue>`, `TryParse<TValue>`, `IsTextEditable(Type)`, `HasBuiltInEditor(Type)`, `QuoteForCopy(string)`
  - `internal abstract class DataTableEditSession` with `Column`, `SourceIndex`, `FramesDrawn`, `Draw(string id)`, `Commit()`
  - `internal sealed class DataTableTextSession<TRow, TValue>` with settable `Text`
  - `internal sealed class DataTableValueSession<TRow, TValue>` with settable `Value`
  - `internal static class DataTableEditors` with `Text(string id, ref string text, bool isCaretForcedToEnd)` and `EnumValue<TValue>(string id, ref TValue value)`

- [ ] **Step 1: Create the shared test data**

Create `tests/ImGui.Widgets.Tests/DataTableMood.cs`:

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

/// <summary>An enum for the data table tests' enum column.</summary>
internal enum DataTableMood
{
	Calm,
	Cheerful,
	Grumpy,
}
```

Create `tests/ImGui.Widgets.Tests/DataTablePerson.cs`:

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

/// <summary>A row for the data table tests. Mutable, so a test can play the caller applying an edit.</summary>
internal sealed class DataTablePerson(string name, int age, bool isActive, DataTableMood mood)
{
	public string Name { get; set; } = name;

	public int Age { get; set; } = age;

	public bool IsActive { get; set; } = isActive;

	public DataTableMood Mood { get; set; } = mood;
}
```

Create `tests/ImGui.Widgets.Tests/DataTableFixture.cs`:

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using System.Collections.Generic;

/// <summary>
/// Four people and five columns, with every edit recorded rather than applied, which is the contract
/// the table has with its caller.
/// </summary>
/// <remarks>
/// Ages 30, 25, 35 and 25 give one tie, which is what the sorting tests need to show ties keep their
/// source order. Bob is the one inactive person, and Length is the one read-only column.
/// </remarks>
internal sealed class DataTableFixture
{
	public const int NameColumn = 0;
	public const int AgeColumn = 1;
	public const int ActiveColumn = 2;
	public const int MoodColumn = 3;
	public const int LengthColumn = 4;

	public DataTableFixture()
	{
		Name = new() { Label = "Name", Value = person => person.Name, OnEdit = NameEdits.Add };
		Age = new() { Label = "Age", Value = person => person.Age, OnEdit = AgeEdits.Add };
		Active = new() { Label = "Active", Value = person => person.IsActive, OnEdit = ActiveEdits.Add };
		Mood = new() { Label = "Mood", Value = person => person.Mood, OnEdit = MoodEdits.Add };
		Length = new() { Label = "Length", Value = person => person.Name.Length };
	}

	public List<DataTablePerson> People { get; } =
	[
		new("Alice", 30, true, DataTableMood.Calm),
		new("Bob", 25, false, DataTableMood.Grumpy),
		new("Carol", 35, true, DataTableMood.Cheerful),
		new("Dave", 25, true, DataTableMood.Calm),
	];

	public List<ImGuiWidgets.DataTableEdit<DataTablePerson, string>> NameEdits { get; } = [];

	public List<ImGuiWidgets.DataTableEdit<DataTablePerson, int>> AgeEdits { get; } = [];

	public List<ImGuiWidgets.DataTableEdit<DataTablePerson, bool>> ActiveEdits { get; } = [];

	public List<ImGuiWidgets.DataTableEdit<DataTablePerson, DataTableMood>> MoodEdits { get; } = [];

	public ImGuiWidgets.DataTableColumn<DataTablePerson, string> Name { get; }

	public ImGuiWidgets.DataTableColumn<DataTablePerson, int> Age { get; }

	public ImGuiWidgets.DataTableColumn<DataTablePerson, bool> Active { get; }

	public ImGuiWidgets.DataTableColumn<DataTablePerson, DataTableMood> Mood { get; }

	public ImGuiWidgets.DataTableColumn<DataTablePerson, int> Length { get; }
}
```

- [ ] **Step 2: Write the failing tests**

Create `tests/ImGui.Widgets.Tests/DataTableColumnTests.cs`:

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using System;
using System.Globalization;
using System.Linq;
using System.Numerics;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests the data table's columns and editing sessions. Nothing here draws, so no ImGui context is
/// required.
/// </summary>
[TestClass]
public class DataTableColumnTests
{
	private readonly DataTableFixture fixture = new();

	private DataTablePerson Alice => fixture.People[0];

	private DataTablePerson Bob => fixture.People[1];

	private static void InCulture(string name, Action action)
	{
		CultureInfo original = CultureInfo.CurrentCulture;
		try
		{
			CultureInfo.CurrentCulture = new CultureInfo(name);
			action();
		}
		finally
		{
			CultureInfo.CurrentCulture = original;
		}
	}

	[TestMethod]
	public void GetText_WithoutFormat_UsesTheInvariantCulture() =>
		InCulture("de-DE", () =>
		{
			ImGuiWidgets.DataTableColumn<DataTablePerson, double> score = new() { Label = "Score", Value = _ => 1234.5 };

			Assert.AreEqual("1234.5", score.GetText(Alice), "Cell text followed the current culture.");
		});

	[TestMethod]
	public void GetText_WithFormat_UsesIt()
	{
		ImGuiWidgets.DataTableColumn<DataTablePerson, int> age = new()
		{
			Label = "Age",
			Value = person => person.Age,
			Format = value => string.Create(CultureInfo.InvariantCulture, $"{value} years"),
		};

		Assert.AreEqual("30 years", age.GetText(Alice));
	}

	[TestMethod]
	public void GetText_OfANullValue_IsEmpty()
	{
		ImGuiWidgets.DataTableColumn<DataTablePerson, string?> nickname = new() { Label = "Nickname", Value = _ => null };

		Assert.AreEqual(string.Empty, nickname.GetText(Alice));
	}

	[TestMethod]
	public void GetText_OfAnEnum_IsItsName() =>
		Assert.AreEqual("Cheerful", fixture.Mood.GetText(fixture.People[2]));

	[TestMethod]
	public void Compare_UsesTheSuppliedComparer()
	{
		ImGuiWidgets.DataTableColumn<DataTablePerson, string> name = new()
		{
			Label = "Name",
			Value = person => person.Name,
			Comparer = StringComparer.OrdinalIgnoreCase,
		};

		DataTablePerson lower = new("alice", 1, true, DataTableMood.Calm);

		// Ordinal puts every lowercase letter after every uppercase one, so only a case-insensitive
		// comparer can put "alice" before "Bob".
		Assert.IsLessThan(0, name.Compare(lower, Bob), "The column ignored its comparer.");
	}

	[TestMethod]
	public void Validate_AnEditableColumnOfATypeWithNoBuiltInEditor_Throws()
	{
		ImGuiWidgets.DataTableColumn<DataTablePerson, Vector2> position = new()
		{
			Label = "Position",
			Value = _ => Vector2.Zero,
			OnEdit = _ => { },
		};

		Assert.ThrowsExactly<ArgumentException>(position.Validate);
	}

	[TestMethod]
	public void Validate_AReadOnlyColumnOfATypeWithNoBuiltInEditor_Passes()
	{
		ImGuiWidgets.DataTableColumn<DataTablePerson, Vector2> position = new() { Label = "Position", Value = _ => Vector2.Zero };

		position.Validate();
	}

	[TestMethod]
	public void Validate_ATypeWithNoBuiltInEditorButAnEditor_Passes()
	{
		ImGuiWidgets.DataTableColumn<DataTablePerson, Vector2> position = new()
		{
			Label = "Position",
			Value = _ => Vector2.Zero,
			OnEdit = _ => { },
			Editor = (string _, ref Vector2 _) => false,
		};

		position.Validate();
	}

	[TestMethod]
	public void Validate_EveryBuiltInType_Passes()
	{
		fixture.Name.Validate();
		fixture.Age.Validate();
		fixture.Active.Validate();
		fixture.Mood.Validate();
	}

	[TestMethod]
	public void IsToggle_OnlyForAnEditableBoolWithNoEditor()
	{
		ImGuiWidgets.DataTableColumn<DataTablePerson, bool> readOnly = new() { Label = "Active", Value = person => person.IsActive };

		Assert.IsTrue(fixture.Active.IsToggle, "An editable bool column is not a toggle.");
		Assert.IsFalse(readOnly.IsToggle, "A read-only bool column is a toggle.");
		Assert.IsFalse(fixture.Name.IsToggle, "A string column is a toggle.");
	}

	[TestMethod]
	public void CreateSession_OfATextColumn_StartsFromTheInvariantText()
	{
		ImGuiWidgets.DataTableEditSession? session = fixture.Age.CreateSession(DataTableFixture.AgeColumn, 0, Alice, null);

		ImGuiWidgets.DataTableTextSession<DataTablePerson, int> text = Assert.IsInstanceOfType<ImGuiWidgets.DataTableTextSession<DataTablePerson, int>>(session);
		Assert.AreEqual("30", text.Text);
	}

	[TestMethod]
	public void CreateSession_WithATypedCharacter_StartsFromIt()
	{
		ImGuiWidgets.DataTableEditSession? session = fixture.Name.CreateSession(DataTableFixture.NameColumn, 1, Bob, 'Q');

		ImGuiWidgets.DataTableTextSession<DataTablePerson, string> text = Assert.IsInstanceOfType<ImGuiWidgets.DataTableTextSession<DataTablePerson, string>>(session);
		Assert.AreEqual("Q", text.Text);
	}

	[TestMethod]
	public void CreateSession_OfAReadOnlyColumn_IsNull() =>
		Assert.IsNull(fixture.Length.CreateSession(DataTableFixture.LengthColumn, 0, Alice, null));

	[TestMethod]
	public void CreateSession_OfAToggle_IsNull() =>
		Assert.IsNull(fixture.Active.CreateSession(DataTableFixture.ActiveColumn, 0, Alice, null));

	[TestMethod]
	public void CreateSession_OfAnEnumColumn_EditsTheValue()
	{
		ImGuiWidgets.DataTableEditSession? session = fixture.Mood.CreateSession(DataTableFixture.MoodColumn, 0, Alice, null);

		ImGuiWidgets.DataTableValueSession<DataTablePerson, DataTableMood> value = Assert.IsInstanceOfType<ImGuiWidgets.DataTableValueSession<DataTablePerson, DataTableMood>>(session);
		Assert.AreEqual(DataTableMood.Calm, value.Value);
	}

	[TestMethod]
	public void TextSession_CommittingAChangedValue_RaisesOneEdit()
	{
		ImGuiWidgets.DataTableTextSession<DataTablePerson, string> session =
			(ImGuiWidgets.DataTableTextSession<DataTablePerson, string>)fixture.Name.CreateSession(DataTableFixture.NameColumn, 1, Bob, null)!;

		session.Text = "Robert";

		Assert.IsTrue(session.Commit());
		Assert.AreEqual(new ImGuiWidgets.DataTableEdit<DataTablePerson, string>(1, Bob, "Bob", "Robert"), fixture.NameEdits.Single());
	}

	[TestMethod]
	public void TextSession_CommittingAnUnchangedValue_RaisesNothing()
	{
		ImGuiWidgets.DataTableEditSession session = fixture.Name.CreateSession(DataTableFixture.NameColumn, 1, Bob, null)!;

		Assert.IsFalse(session.Commit());
		Assert.IsEmpty(fixture.NameEdits, "Opening and closing an editor recorded a change.");
	}

	[TestMethod]
	public void TextSession_CommittingTextThatDoesNotParse_RaisesNothing()
	{
		ImGuiWidgets.DataTableTextSession<DataTablePerson, int> session =
			(ImGuiWidgets.DataTableTextSession<DataTablePerson, int>)fixture.Age.CreateSession(DataTableFixture.AgeColumn, 0, Alice, null)!;

		session.Text = "thirty";

		Assert.IsFalse(session.Commit());
		Assert.IsEmpty(fixture.AgeEdits);
	}

	[TestMethod]
	public void TextSession_ParsesWithTheInvariantCulture() =>
		InCulture("de-DE", () =>
		{
			double? committed = null;
			ImGuiWidgets.DataTableColumn<DataTablePerson, double> score = new()
			{
				Label = "Score",
				Value = _ => 1.0,
				OnEdit = edit => committed = edit.NewValue,
			};

			ImGuiWidgets.DataTableTextSession<DataTablePerson, double> session =
				(ImGuiWidgets.DataTableTextSession<DataTablePerson, double>)score.CreateSession(0, 0, Alice, null)!;
			session.Text = "2.5";
			session.Commit();

			Assert.AreEqual(2.5, committed);
		});

	[TestMethod]
	public void ValueSession_CommittingAChangedValue_RaisesOneEdit()
	{
		ImGuiWidgets.DataTableValueSession<DataTablePerson, DataTableMood> session =
			(ImGuiWidgets.DataTableValueSession<DataTablePerson, DataTableMood>)fixture.Mood.CreateSession(DataTableFixture.MoodColumn, 0, Alice, null)!;

		session.Value = DataTableMood.Grumpy;

		Assert.IsTrue(session.Commit());
		Assert.AreEqual(
			new ImGuiWidgets.DataTableEdit<DataTablePerson, DataTableMood>(0, Alice, DataTableMood.Calm, DataTableMood.Grumpy),
			fixture.MoodEdits.Single());
	}

	[TestMethod]
	public void Toggle_RaisesTheOppositeValue()
	{
		fixture.Active.Toggle(1, Bob);

		Assert.AreEqual(new ImGuiWidgets.DataTableEdit<DataTablePerson, bool>(1, Bob, false, true), fixture.ActiveEdits.Single());
	}

	[TestMethod]
	public void Toggle_OfAReadOnlyBool_RaisesNothing()
	{
		ImGuiWidgets.DataTableColumn<DataTablePerson, bool> readOnly = new() { Label = "Active", Value = person => person.IsActive };

		readOnly.Toggle(1, Bob);

		Assert.IsEmpty(fixture.ActiveEdits);
	}

	[TestMethod]
	[DataRow("plain", "plain")]
	[DataRow("tab\there", "\"tab\there\"")]
	[DataRow("two\nlines", "\"two\nlines\"")]
	[DataRow("say \"hi\"", "\"say \"\"hi\"\"\"")]
	public void QuoteForCopy_QuotesOnlyWhatASpreadsheetWouldSplit(string text, string expected) =>
		Assert.AreEqual(expected, ImGuiWidgets.DataTableText.QuoteForCopy(text));
}
```

If this MSTest version's `Assert.IsInstanceOfType<T>` returns `void` rather than the cast value, cast with `(T)session` after the assertion instead. If `Assert.IsEmpty` doesn't exist, use `Assert.AreEqual(0, list.Count)`.

- [ ] **Step 3: Run the tests to verify they fail**

Run: `dotnet build tests/ImGui.Widgets.Tests/ImGui.Widgets.Tests.csproj`
Expected: FAIL to compile, `DataTableColumn` and the rest do not exist.

- [ ] **Step 4: Create the column types**

Create `ImGui.Widgets/DataTableColumn.cs`:

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;
using System.Collections.Generic;

using Hexa.NET.ImGui;

public static partial class ImGuiWidgets
{
	/// <summary>A cell of a data table, addressed by its row's place in the caller's list and its column.</summary>
	/// <param name="SourceIndex">The row's index in the list the caller passed, never its position on screen.</param>
	/// <param name="Column">The column's index in <see cref="DataTableState{TRow}.Columns"/>.</param>
	public readonly record struct DataTableCell(int SourceIndex, int Column);

	/// <summary>
	/// An edit to one cell, reported to the caller to apply. The table never writes to the caller's
	/// data itself.
	/// </summary>
	/// <typeparam name="TRow">The type of one row.</typeparam>
	/// <typeparam name="TValue">The type of the edited column's values.</typeparam>
	/// <param name="SourceIndex">The row's index in the list the caller passed.</param>
	/// <param name="Row">The row that was edited.</param>
	/// <param name="OldValue">The value when editing began, which is what an undo restores.</param>
	/// <param name="NewValue">The value to apply.</param>
	public readonly record struct DataTableEdit<TRow, TValue>(int SourceIndex, TRow Row, TValue OldValue, TValue NewValue);

	/// <summary>Draws an editor for one cell's value, filling the cell.</summary>
	/// <typeparam name="TValue">The type of the value.</typeparam>
	/// <param name="id">The ImGui id to give the editor. Not drawn.</param>
	/// <param name="value">The working value, updated as the person edits it.</param>
	/// <returns>True when the value changed this frame.</returns>
	public delegate bool DataTableEditor<TValue>(string id, ref TValue value);

	/// <summary>
	/// One column of a data table. Create a <see cref="DataTableColumn{TRow, TValue}"/>, which knows
	/// the column's value type.
	/// </summary>
	/// <typeparam name="TRow">The type of one row.</typeparam>
	public abstract class DataTableColumn<TRow>
	{
		// Only the typed column below derives from this, which is what lets the internal members be
		// abstract without anyone outside having to implement them.
		private protected DataTableColumn()
		{
		}

		/// <summary>Gets the heading. Also the column's ImGui id and probe name, so it must be unique in its table.</summary>
		public required string Label { get; init; }

		/// <summary>Gets the sizing and behavior flags passed to <c>TableSetupColumn</c>.</summary>
		public ImGuiTableColumnFlags Flags { get; init; }

		/// <summary>Gets the initial width or weight, read according to <see cref="Flags"/>. Zero leaves it to ImGui.</summary>
		public float Width { get; init; }

		/// <summary>Gets a value indicating whether clicking the heading sorts on this column. Defaults to <see langword="true"/>.</summary>
		public bool CanSort { get; init; } = true;

		/// <summary>Gets the text a row shows in this column, which is also what's filtered and copied.</summary>
		/// <param name="row">The row.</param>
		/// <returns>The cell's text.</returns>
		public abstract string GetText(TRow row);

		internal abstract bool IsEditable { get; }

		internal abstract bool IsToggle { get; }

		internal abstract int Compare(TRow left, TRow right);

		internal abstract void Validate();

		internal abstract DataTableEditSession? CreateSession(int column, int sourceIndex, TRow row, char? firstCharacter);

		internal abstract void Toggle(int sourceIndex, TRow row);

		/// <summary>Draws the cell's content when it isn't being edited.</summary>
		/// <returns>True when an inline toggle was clicked.</returns>
		internal abstract bool DrawContent(TRow row);
	}

	/// <summary>A column of a data table whose values are of one type.</summary>
	/// <typeparam name="TRow">The type of one row.</typeparam>
	/// <typeparam name="TValue">The type of this column's values.</typeparam>
	public sealed class DataTableColumn<TRow, TValue> : DataTableColumn<TRow>
	{
		/// <summary>Gets the function that reads this column's value from a row.</summary>
		public required Func<TRow, TValue> Value { get; init; }

		/// <summary>
		/// Gets the function that turns a value into cell text, or <see langword="null"/> to use the
		/// value's own formatting in the invariant culture.
		/// </summary>
		public Func<TValue, string>? Format { get; init; }

		/// <summary>Gets the comparer used to sort on this column, or <see langword="null"/> for the type's default.</summary>
		public IComparer<TValue>? Comparer { get; init; }

		/// <summary>
		/// Gets the callback that receives each committed edit, or <see langword="null"/> for a read-only
		/// column. The callback applies the edit. The table never writes to the row.
		/// </summary>
		public Action<DataTableEdit<TRow, TValue>>? OnEdit { get; init; }

		/// <summary>
		/// Gets the editor to use instead of the built-in one. Required for an editable column whose
		/// value type isn't <see cref="bool"/>, <see cref="int"/>, <see cref="long"/>, <see cref="float"/>,
		/// <see cref="double"/>, <see cref="string"/>, or an enum.
		/// </summary>
		public DataTableEditor<TValue>? Editor { get; init; }

		/// <summary>
		/// Gets a function that draws a cell's content in place of its text, for icons or colored values,
		/// or <see langword="null"/> to draw the text.
		/// </summary>
		public Action<TRow, TValue>? DrawCell { get; init; }

		/// <inheritdoc/>
		public override string GetText(TRow row)
		{
			TValue value = Value(row);
			return Format is null ? DataTableText.FormatInvariant(value) : Format(value);
		}

		internal override bool IsEditable => OnEdit is not null;

		internal override bool IsToggle => OnEdit is not null && Editor is null && typeof(TValue) == typeof(bool);

		internal override int Compare(TRow left, TRow right) =>
			(Comparer ?? Comparer<TValue>.Default).Compare(Value(left), Value(right));

		internal override void Validate()
		{
			if (OnEdit is not null && Editor is null && !DataTableText.HasBuiltInEditor(typeof(TValue)))
			{
				throw new ArgumentException(
					$"Column '{Label}' is editable, but {typeof(TValue).Name} has no built-in editor. Supply an Editor, or remove OnEdit to make the column read-only.");
			}
		}

		internal override DataTableEditSession? CreateSession(int column, int sourceIndex, TRow row, char? firstCharacter)
		{
			if (OnEdit is null || IsToggle)
			{
				return null;
			}

			TValue current = Value(row);

			if (Editor is not null)
			{
				return new DataTableValueSession<TRow, TValue>(this, column, sourceIndex, row, current, Editor);
			}

			if (DataTableText.IsTextEditable(typeof(TValue)))
			{
				string text = firstCharacter is char typed
					? typed.ToString(System.Globalization.CultureInfo.InvariantCulture)
					: DataTableText.FormatInvariant(current);

				return new DataTableTextSession<TRow, TValue>(this, column, sourceIndex, row, current, text, isCaretForcedToEnd: firstCharacter is not null);
			}

			// Validate has already ruled out every other type, so what's left is an enum.
			return new DataTableValueSession<TRow, TValue>(this, column, sourceIndex, row, current, DataTableEditors.EnumValue);
		}

		internal override void Toggle(int sourceIndex, TRow row)
		{
			if (!IsToggle)
			{
				return;
			}

			TValue current = Value(row);
			TValue flipped = (TValue)(object)!(current is bool isOn && isOn);
			OnEdit!(new DataTableEdit<TRow, TValue>(sourceIndex, row, current, flipped));
		}

		internal override bool DrawContent(TRow row)
		{
			TValue value = Value(row);

			if (IsToggle)
			{
				bool isChecked = value is bool isOn && isOn;
				return ImGui.Checkbox("##toggle", ref isChecked);
			}

			if (DrawCell is not null)
			{
				DrawCell(row, value);
				return false;
			}

			ImGui.TextUnformatted(Format is null ? DataTableText.FormatInvariant(value) : Format(value));
			return false;
		}
	}
}
```

- [ ] **Step 5: Create the text helpers**

Create `ImGui.Widgets/DataTableText.cs`:

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

public static partial class ImGuiWidgets
{
	/// <summary>Turns cell values into text and back, always in the invariant culture.</summary>
	/// <remarks>
	/// Invariant so that a value copied or typed on one machine means the same on every other. A
	/// column that wants local formatting for display supplies its own <c>Format</c>.
	/// </remarks>
	internal static class DataTableText
	{
		private static readonly char[] CharactersNeedingQuotes = ['\t', '\n', '\r', '"'];

		internal static string FormatInvariant<TValue>(TValue value) => value switch
		{
			null => string.Empty,
			IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
			_ => value.ToString() ?? string.Empty,
		};

		internal static bool IsTextEditable(Type type) =>
			type == typeof(string) || type == typeof(int) || type == typeof(long) || type == typeof(float) || type == typeof(double);

		internal static bool HasBuiltInEditor(Type type) =>
			IsTextEditable(type) || type == typeof(bool) || type.IsEnum;

		internal static bool TryParse<TValue>(string text, [MaybeNullWhen(false)] out TValue value)
		{
			object? parsed = null;

			if (typeof(TValue) == typeof(string))
			{
				parsed = text;
			}
			else if (typeof(TValue) == typeof(int) && int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int asInt))
			{
				parsed = asInt;
			}
			else if (typeof(TValue) == typeof(long) && long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out long asLong))
			{
				parsed = asLong;
			}
			else if (typeof(TValue) == typeof(float) && float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float asFloat))
			{
				parsed = asFloat;
			}
			else if (typeof(TValue) == typeof(double) && double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double asDouble))
			{
				parsed = asDouble;
			}

			if (parsed is TValue typed)
			{
				value = typed;
				return true;
			}

			value = default;
			return false;
		}

		/// <summary>
		/// Quotes a value the way a spreadsheet reads it back, so a tab, a line break or a quote inside
		/// it pastes as part of one cell rather than splitting it.
		/// </summary>
		internal static string QuoteForCopy(string text) =>
			text.IndexOfAny(CharactersNeedingQuotes) < 0
				? text
				: $"\"{text.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
	}
}
```

- [ ] **Step 6: Create the edit sessions**

Create `ImGui.Widgets/DataTableEditSession.cs`:

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System.Collections.Generic;

public static partial class ImGuiWidgets
{
	/// <summary>
	/// The one cell being edited: the value it started with and the working copy. Typed underneath,
	/// so the state stays generic only over the row type and nothing is boxed.
	/// </summary>
	internal abstract class DataTableEditSession(int column, int sourceIndex)
	{
		public int Column { get; } = column;

		public int SourceIndex { get; } = sourceIndex;

		/// <summary>Gets how many frames the editor has been drawn, which is how the renderer knows to focus it once.</summary>
		public int FramesDrawn { get; private set; }

		public void Draw(string id)
		{
			DrawEditor(id, FramesDrawn);
			FramesDrawn++;
		}

		/// <summary>Reports the edit to the caller if the value changed.</summary>
		/// <returns>True when an edit was reported.</returns>
		public abstract bool Commit();

		protected abstract void DrawEditor(string id, int framesDrawn);
	}

	/// <summary>Edits a string or a number as text, parsed when it's committed.</summary>
	internal sealed class DataTableTextSession<TRow, TValue>(
		DataTableColumn<TRow, TValue> owner,
		int column,
		int sourceIndex,
		TRow row,
		TValue original,
		string text,
		bool isCaretForcedToEnd) : DataTableEditSession(column, sourceIndex)
	{
		// ImGui selects all of an input's text when it takes the keyboard. A session started by typing
		// holds the caret after that first character for the few frames activation takes, so the next
		// character follows it instead of replacing it.
		private const int CaretFrames = 3;

		public string Text { get; set; } = text;

		public override bool Commit()
		{
			if (!DataTableText.TryParse(Text, out TValue? parsed) || EqualityComparer<TValue>.Default.Equals(original, parsed))
			{
				return false;
			}

			owner.OnEdit!(new DataTableEdit<TRow, TValue>(SourceIndex, row, original, parsed));
			return true;
		}

		protected override void DrawEditor(string id, int framesDrawn)
		{
			string current = Text;
			DataTableEditors.Text(id, ref current, isCaretForcedToEnd && framesDrawn < CaretFrames);
			Text = current;
		}
	}

	/// <summary>Edits a value directly, through the column's own editor or the built-in enum combo.</summary>
	internal sealed class DataTableValueSession<TRow, TValue>(
		DataTableColumn<TRow, TValue> owner,
		int column,
		int sourceIndex,
		TRow row,
		TValue original,
		DataTableEditor<TValue> editor) : DataTableEditSession(column, sourceIndex)
	{
		private readonly TValue originalValue = original;
		private TValue current = original;

		public TValue Value
		{
			get => current;
			set => current = value;
		}

		public override bool Commit()
		{
			if (EqualityComparer<TValue>.Default.Equals(originalValue, current))
			{
				return false;
			}

			owner.OnEdit!(new DataTableEdit<TRow, TValue>(SourceIndex, row, originalValue, current));
			return true;
		}

		protected override void DrawEditor(string id, int framesDrawn) => editor(id, ref current);
	}
}
```

In `DataTableTextSession`, `original` is used only inside `Commit`, so the compiler captures it once and raises no CS9124. In `DataTableValueSession` it seeds two fields and is never captured, which is why that class copies it into `originalValue`. If the compiler still warns about a captured parameter, copy it into a `private readonly` field the same way.

- [ ] **Step 7: Create the built-in editors**

Create `ImGui.Widgets/DataTableEditors.cs`:

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System.Diagnostics.CodeAnalysis;

using Hexa.NET.ImGui;

public static partial class ImGuiWidgets
{
	/// <summary>The editors a cell gets when its column doesn't supply one.</summary>
	internal static class DataTableEditors
	{
		private const int TextCapacity = 1024;

		private static readonly unsafe ImGuiInputTextCallback PlaceCaretAtEnd = PlaceCaretAtEndCallback;

		/// <summary>Draws a text box filling the cell.</summary>
		/// <param name="id">The ImGui id.</param>
		/// <param name="text">The text being edited.</param>
		/// <param name="isCaretForcedToEnd">
		/// Whether to hold the caret after the last character this frame, undoing ImGui's select-all on
		/// activation. Only for the first frames of a session started by typing.
		/// </param>
		/// <returns>True when the text changed this frame.</returns>
		[SuppressMessage("Major Code Smell", "S6640:Make sure that using \"unsafe\" is safe here", Justification = "The callback only moves the caret inside ImGui's own buffer, and ImGui owns the pointer for the duration of the call.")]
		internal static unsafe bool Text(string id, ref string text, bool isCaretForcedToEnd)
		{
			ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);

			return isCaretForcedToEnd
				? ImGui.InputText(id, ref text, TextCapacity, ImGuiInputTextFlags.CallbackAlways, PlaceCaretAtEnd)
				: ImGui.InputText(id, ref text, TextCapacity);
		}

		/// <summary>Draws a combo of an enum's names, filling the cell.</summary>
		/// <typeparam name="TValue">The enum type. Not constrained, because the column only knows it at run time.</typeparam>
		/// <param name="id">The ImGui id.</param>
		/// <param name="value">The value being edited.</param>
		/// <returns>True when a different value was chosen this frame.</returns>
		internal static bool EnumValue<TValue>(string id, ref TValue value)
		{
			System.Type type = typeof(TValue);
			string current = System.Enum.GetName(type, value!) ?? DataTableText.FormatInvariant(value);
			bool isChanged = false;

			ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
			if (ImGui.BeginCombo(id, current))
			{
				foreach (string name in System.Enum.GetNames(type))
				{
					if (ImGui.Selectable(name, name == current) && name != current)
					{
						value = (TValue)System.Enum.Parse(type, name);
						isChanged = true;
					}
				}

				ImGui.EndCombo();
			}

			return isChanged;
		}

		[SuppressMessage("Major Code Smell", "S6640:Make sure that using \"unsafe\" is safe here", Justification = "ImGui passes a pointer valid for the call, and it is not retained.")]
		private static unsafe int PlaceCaretAtEndCallback(ImGuiInputTextCallbackData* data)
		{
			data->CursorPos = data->BufTextLen;
			data->SelectionStart = data->BufTextLen;
			data->SelectionEnd = data->BufTextLen;
			return 0;
		}
	}
}
```

**Binding check.** Confirm with Go To Definition that `Hexa.NET.ImGui` has an `InputText(string, ref string, nuint, ImGuiInputTextFlags, ImGuiInputTextCallback)` overload and that `ImGuiInputTextCallback` is `unsafe delegate int (ImGuiInputTextCallbackData*)`. If the overload also takes a `void* userData`, pass `null`. If the field names differ (`CursorPos`, `SelectionStart`, `SelectionEnd`, `BufTextLen`), use the binding's names. The behavior to preserve is the one Task 9's typing test pins.

- [ ] **Step 8: Run the tests to verify they pass**

```bash
dotnet build tests/ImGui.Widgets.Tests/ImGui.Widgets.Tests.csproj
./tests/ImGui.Widgets.Tests/bin/Debug/net10.0/ktsu.ImGui.Widgets.Tests --filter "FullyQualifiedName~DataTableColumnTests"
```

Expected: PASS, 0 warnings.

- [ ] **Step 9: Commit**

```bash
git add ImGui.Widgets/DataTableColumn.cs ImGui.Widgets/DataTableText.cs ImGui.Widgets/DataTableEditSession.cs ImGui.Widgets/DataTableEditors.cs tests/ImGui.Widgets.Tests/DataTableMood.cs tests/ImGui.Widgets.Tests/DataTablePerson.cs tests/ImGui.Widgets.Tests/DataTableFixture.cs tests/ImGui.Widgets.Tests/DataTableColumnTests.cs
git commit -m "[minor] Add typed data table columns and their edit sessions"
```

---

## Task 3: The state's view, with filtering, sorting, and when it rebuilds

**Files:**
- Create: `ImGui.Widgets/DataTableState.cs`
- Modify: `tests/ImGui.Widgets.Tests/DataTableFixture.cs` (add `CreateState`)
- Test: `tests/ImGui.Widgets.Tests/DataTableStateViewTests.cs`

**Interfaces:**
- Consumes: `DataTableColumn<TRow>` with `Validate`, `Compare`, `GetText`, `CanSort`, `Label` (Task 2). `SearchBoxOptions` (existing, `SearchBox.cs`).
- Produces (on `DataTableState<TRow>`, nested in `ImGuiWidgets`, declared `sealed partial`):
  - `public DataTableState(IReadOnlyList<DataTableColumn<TRow>> columns)`
  - `public IReadOnlyList<DataTableColumn<TRow>> Columns { get; }`
  - `public Func<TRow, bool>? RowFilter { get; set; }`
  - `public void Refresh()`
  - `internal IReadOnlyList<int> View`, `internal int SortColumn`, `internal bool IsSortAscending`
  - `internal TRow RowAt(int sourceIndex)`, `internal int ViewPositionOf(int sourceIndex)`
  - `internal bool IsColumnVisible(int column)`, `internal void SetColumnVisible(int column, bool isVisible)`
  - `internal string GetFilter(int column)`, `internal SearchBoxOptions GetFilterOptions(int column)`
  - `internal void Sync(IReadOnlyList<TRow> source)`, `internal void SetSort(int column, bool isAscending)`, `internal void SetFilter(int column, string text, SearchBoxOptions? options = null)`
  - `private void Rebuild()`, `private int syncedCount` (Task 4 edits both)
  - `internal static class DataTableFilter` with `DefaultOptions`

- [ ] **Step 1: Give the fixture a state**

Add to `tests/ImGui.Widgets.Tests/DataTableFixture.cs`, after the `Length` property:

```csharp
	public ImGuiWidgets.DataTableState<DataTablePerson> CreateState()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = new([Name, Age, Active, Mood, Length]);
		state.Sync(People);
		return state;
	}
```

- [ ] **Step 2: Write the failing tests**

Create `tests/ImGui.Widgets.Tests/DataTableStateViewTests.cs`:

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using System;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests how a data table's state builds its view of the rows. No ImGui context is required.
/// </summary>
[TestClass]
public class DataTableStateViewTests
{
	private readonly DataTableFixture fixture = new();

	private static void AssertView(ImGuiWidgets.DataTableState<DataTablePerson> state, params int[] expected) =>
		Assert.AreSequenceEqual(expected, state.View);

	[TestMethod]
	public void NewState_ShowsEveryRowInSourceOrder() =>
		AssertView(fixture.CreateState(), 0, 1, 2, 3);

	[TestMethod]
	public void ColumnFilter_KeepsOnlyMatchingRows()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = fixture.CreateState();

		state.SetFilter(DataTableFixture.NameColumn, "ob");

		AssertView(state, 1);
	}

	/// <summary>
	/// The default is fuzzy: the typed characters in order, any case. "al" is in Alice and, spread out,
	/// in Carol, and in neither Bob nor Dave.
	/// </summary>
	[TestMethod]
	public void ColumnFilter_MatchesTypedCharactersInOrderIgnoringCase()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = fixture.CreateState();

		state.SetFilter(DataTableFixture.NameColumn, "AL");

		AssertView(state, 0, 2);
	}

	[TestMethod]
	public void FiltersOnSeveralColumns_MustAllMatch()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = fixture.CreateState();

		state.SetFilter(DataTableFixture.NameColumn, "a");
		state.SetFilter(DataTableFixture.AgeColumn, "25");

		AssertView(state, 3);
	}

	[TestMethod]
	public void ClearingAFilter_RestoresTheRows()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = fixture.CreateState();
		state.SetFilter(DataTableFixture.NameColumn, "ob");

		state.SetFilter(DataTableFixture.NameColumn, string.Empty);

		AssertView(state, 0, 1, 2, 3);
	}

	[TestMethod]
	public void RowFilter_IsAppliedAsSoonAsItIsSet()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = fixture.CreateState();

		state.RowFilter = person => person.IsActive;

		AssertView(state, 0, 2, 3);
	}

	[TestMethod]
	public void SortAscending_BreaksTiesBySourceOrder()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = fixture.CreateState();

		state.SetSort(DataTableFixture.AgeColumn, isAscending: true);

		AssertView(state, 1, 3, 0, 2);
	}

	/// <summary>
	/// Descending reverses the values but not the tie-break, so Bob still comes before Dave. Reversing
	/// the whole ascending list would put Dave first and make equal rows swap on every click.
	/// </summary>
	[TestMethod]
	public void SortDescending_StillBreaksTiesBySourceOrder()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = fixture.CreateState();

		state.SetSort(DataTableFixture.AgeColumn, isAscending: false);

		AssertView(state, 2, 0, 1, 3);
	}

	[TestMethod]
	public void SortOnNoColumn_RestoresSourceOrder()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = fixture.CreateState();
		state.SetSort(DataTableFixture.AgeColumn, isAscending: true);

		state.SetSort(-1, isAscending: true);

		AssertView(state, 0, 1, 2, 3);
	}

	[TestMethod]
	public void SortOnAColumnThatCannotSort_IsIgnored()
	{
		ImGuiWidgets.DataTableColumn<DataTablePerson, int> age = new() { Label = "Age", Value = person => person.Age, CanSort = false };
		ImGuiWidgets.DataTableState<DataTablePerson> state = new([fixture.Name, age]);
		state.Sync(fixture.People);

		state.SetSort(1, isAscending: true);

		AssertView(state, 0, 1, 2, 3);
	}

	/// <summary>
	/// The rule that keeps an edited row in place: a value changing is not a trigger, because the table
	/// can't see it happen and because a row jumping away mid-edit is disorienting.
	/// </summary>
	[TestMethod]
	public void AValueChangingInPlace_DoesNotMoveItsRowUntilRefresh()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = fixture.CreateState();
		state.SetSort(DataTableFixture.AgeColumn, isAscending: true);

		fixture.People[1].Age = 99;
		state.Sync(fixture.People);

		AssertView(state, 1, 3, 0, 2);

		state.Refresh();

		AssertView(state, 3, 0, 2, 1);
	}

	[TestMethod]
	public void ARowCountChange_RebuildsTheView()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = fixture.CreateState();
		state.SetSort(DataTableFixture.AgeColumn, isAscending: true);

		fixture.People.Add(new("Eve", 20, true, DataTableMood.Calm));
		state.Sync(fixture.People);

		AssertView(state, 4, 1, 3, 0, 2);
	}

	[TestMethod]
	public void NoRows_GiveAnEmptyView()
	{
		fixture.People.Clear();

		AssertView(fixture.CreateState());
	}

	[TestMethod]
	public void ViewPositionOf_ReportsWhereASourceRowIsShownOrMinusOne()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = fixture.CreateState();
		state.SetFilter(DataTableFixture.NameColumn, "ob");

		Assert.AreEqual(0, state.ViewPositionOf(1));
		Assert.AreEqual(-1, state.ViewPositionOf(0), "A filtered out row reported a position.");
		Assert.AreEqual(-1, state.ViewPositionOf(99), "A row past the end reported a position.");
	}

	[TestMethod]
	public void Constructor_WithNoColumns_Throws() =>
		Assert.ThrowsExactly<ArgumentException>(() => new ImGuiWidgets.DataTableState<DataTablePerson>([]));

	[TestMethod]
	public void Constructor_WithTwoColumnsOfOneLabel_Throws()
	{
		ImGuiWidgets.DataTableColumn<DataTablePerson, int> secondName = new() { Label = "Name", Value = person => person.Age };

		Assert.ThrowsExactly<ArgumentException>(() => new ImGuiWidgets.DataTableState<DataTablePerson>([fixture.Name, secondName]));
	}

	[TestMethod]
	public void Constructor_ValidatesEachColumn()
	{
		ImGuiWidgets.DataTableColumn<DataTablePerson, Version> version = new() { Label = "Version", Value = _ => new Version(1, 0), OnEdit = _ => { } };

		Assert.ThrowsExactly<ArgumentException>(() => new ImGuiWidgets.DataTableState<DataTablePerson>([version]));
	}
}
```

- [ ] **Step 3: Run the tests to verify they fail**

Run: `dotnet build tests/ImGui.Widgets.Tests/ImGui.Widgets.Tests.csproj`
Expected: FAIL to compile, `DataTableState` does not exist.

- [ ] **Step 4: Create the state core**

Create `ImGui.Widgets/DataTableState.cs`:

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;
using System.Collections.Generic;
using System.Linq;

using ktsu.TextFilter;

public static partial class ImGuiWidgets
{
	/// <summary>
	/// What a data table keeps between frames: the sorted and filtered view of the caller's rows, the
	/// active cell, the selected rows, and the cell being edited. Keep one per table, and pass the same
	/// one every frame.
	/// </summary>
	/// <remarks>
	/// Makes no ImGui calls. The renderer drives it with commands and reads it back, which is what lets
	/// every rule here be tested without a window.
	/// </remarks>
	/// <typeparam name="TRow">The type of one row.</typeparam>
	public sealed partial class DataTableState<TRow>
	{
		private readonly string[] filterTexts;
		private readonly SearchBoxOptions[] filterOptions;
		private readonly bool[] visibleColumns;
		private IReadOnlyList<TRow> rows = [];
		private int[] view = [];
		private int[] viewPositions = [];
		private int syncedCount = -1;
		private Func<TRow, bool>? rowFilter;

		/// <summary>Creates the state for a table with the given columns.</summary>
		/// <param name="columns">The columns, left to right. Labels must be unique.</param>
		/// <exception cref="ArgumentNullException"><paramref name="columns"/> or one of its items is <see langword="null"/>.</exception>
		/// <exception cref="ArgumentException">
		/// There are no columns, two share a label, or an editable column's value type has no built-in
		/// editor and no <c>Editor</c> was supplied.
		/// </exception>
		public DataTableState(IReadOnlyList<DataTableColumn<TRow>> columns)
		{
			Ensure.NotNull(columns);

			if (columns.Count == 0)
			{
				throw new ArgumentException("A data table needs at least one column.", nameof(columns));
			}

			HashSet<string> labels = new(StringComparer.Ordinal);
			foreach (DataTableColumn<TRow> column in columns)
			{
				Ensure.NotNull(column);
				column.Validate();

				// The label is the column's ImGui id and its probe name, so two alike would be one column
				// to ImGui and to a test.
				if (!labels.Add(column.Label))
				{
					throw new ArgumentException($"Two columns are labeled '{column.Label}'. Labels must be unique.", nameof(columns));
				}
			}

			Columns = [.. columns];
			filterTexts = [.. Enumerable.Repeat(string.Empty, columns.Count)];
			filterOptions = [.. Enumerable.Repeat(DataTableFilter.DefaultOptions, columns.Count)];
			visibleColumns = [.. Enumerable.Repeat(true, columns.Count)];
		}

		/// <summary>Gets the columns, left to right.</summary>
		public IReadOnlyList<DataTableColumn<TRow>> Columns { get; }

		/// <summary>
		/// Gets or sets a condition a row must meet to be shown, for filters the filter row can't
		/// express. Setting it rebuilds the view.
		/// </summary>
		public Func<TRow, bool>? RowFilter
		{
			get => rowFilter;
			set
			{
				rowFilter = value;
				Rebuild();
			}
		}

		internal IReadOnlyList<int> View => view;

		internal int SortColumn { get; private set; } = -1;

		internal bool IsSortAscending { get; private set; } = true;

		/// <summary>
		/// Rebuilds the view from the rows as they are now. Call it after applying an edit to a value the
		/// table sorts or filters on, to move the row to where it now belongs.
		/// </summary>
		/// <remarks>
		/// The table rebuilds by itself when the sort, a filter, or the row count changes. It doesn't
		/// rebuild when a value changes in place, because it can't see that happen, and because a row
		/// that jumps away while it's being edited is disorienting.
		/// </remarks>
		public void Refresh() => Rebuild();

		internal TRow RowAt(int sourceIndex) => rows[sourceIndex];

		internal int ViewPositionOf(int sourceIndex) =>
			sourceIndex >= 0 && sourceIndex < viewPositions.Length ? viewPositions[sourceIndex] : -1;

		internal bool IsColumnVisible(int column) => visibleColumns[column];

		internal void SetColumnVisible(int column, bool isVisible) => visibleColumns[column] = isVisible;

		internal string GetFilter(int column) => filterTexts[column];

		internal SearchBoxOptions GetFilterOptions(int column) => filterOptions[column];

		/// <summary>Takes the caller's rows for this frame, and rebuilds if their number changed.</summary>
		internal void Sync(IReadOnlyList<TRow> source)
		{
			Ensure.NotNull(source);
			rows = source;

			if (source.Count == syncedCount)
			{
				return;
			}

			syncedCount = source.Count;
			Rebuild();
		}

		internal void SetSort(int column, bool isAscending)
		{
			SortColumn = column;
			IsSortAscending = isAscending;
			Rebuild();
		}

		internal void SetFilter(int column, string text, SearchBoxOptions? options = null)
		{
			Ensure.NotNull(text);
			filterTexts[column] = text;

			if (options is not null)
			{
				filterOptions[column] = options;
			}

			Rebuild();
		}

		private void Rebuild()
		{
			int[] next = [.. Enumerable.Range(0, rows.Count).Where(Survives)];

			if (SortColumn >= 0 && SortColumn < Columns.Count && Columns[SortColumn].CanSort)
			{
				DataTableColumn<TRow> column = Columns[SortColumn];
				int direction = IsSortAscending ? 1 : -1;

				// Ties fall back to the source index in both directions, so rows that compare equal keep
				// their order and never trade places from one rebuild to the next.
				Array.Sort(next, (left, right) =>
				{
					int order = Math.Sign(column.Compare(rows[left], rows[right])) * direction;
					return order != 0 ? order : left.CompareTo(right);
				});
			}

			view = next;
			viewPositions = new int[rows.Count];
			Array.Fill(viewPositions, -1);

			for (int position = 0; position < view.Length; position++)
			{
				viewPositions[view[position]] = position;
			}
		}

		private bool Survives(int sourceIndex)
		{
			TRow row = rows[sourceIndex];

			if (rowFilter is not null && !rowFilter(row))
			{
				return false;
			}

			for (int column = 0; column < Columns.Count; column++)
			{
				string filter = filterTexts[column];
				if (filter.Length == 0)
				{
					continue;
				}

				SearchBoxOptions options = filterOptions[column];
				if (!TextFilter.IsMatch(Columns[column].GetText(row), filter, options.FilterType, options.MatchOptions))
				{
					return false;
				}
			}

			return true;
		}
	}

	/// <summary>Defaults shared by every data table's filter row.</summary>
	internal static class DataTableFilter
	{
		/// <summary>
		/// Gets the options each filter box starts with. Fuzzy, which matches the typed characters in
		/// order and ignores case, because that's what a person typing part of a value expects. Glob and
		/// regex are a right-click away through the search box's own menu.
		/// </summary>
		internal static SearchBoxOptions DefaultOptions { get; } = new(
			Label: "##filter",
			FilterType: TextFilterType.Fuzzy,
			MatchOptions: TextFilterMatchOptions.ByWholeString,
			FullWidth: true);
	}
}
```

- [ ] **Step 5: Run the tests to verify they pass**

```bash
dotnet build tests/ImGui.Widgets.Tests/ImGui.Widgets.Tests.csproj
./tests/ImGui.Widgets.Tests/bin/Debug/net10.0/ktsu.ImGui.Widgets.Tests --filter "FullyQualifiedName~DataTableStateViewTests"
```

Expected: PASS, 0 warnings. If `Assert.AreSequenceEqual` doesn't accept an `IReadOnlyList<int>` against an `int[]`, compare with `CollectionAssert.AreEqual(expected, state.View.ToArray())`.

- [ ] **Step 6: Commit**

```bash
git add ImGui.Widgets/DataTableState.cs tests/ImGui.Widgets.Tests/DataTableFixture.cs tests/ImGui.Widgets.Tests/DataTableStateViewTests.cs
git commit -m "[minor] Sort and filter a data table's rows into a view"
```

---

## Task 4: Active cell, selection, movement, and copy

**Files:**
- Create: `ImGui.Widgets/DataTableState.Navigation.cs`
- Modify: `ImGui.Widgets/DataTableState.cs` (`Sync` and `Rebuild`)
- Test: `tests/ImGui.Widgets.Tests/DataTableStateNavigationTests.cs`

**Interfaces:**
- Consumes: everything from Task 3, and `DataTableText.QuoteForCopy` (Task 2).
- Produces:
  - `internal enum DataTableMove { Up, Down, Left, Right, PageUp, PageDown, Home, End, First, Last, Next, Previous }` nested in `ImGuiWidgets`
  - `public DataTableCell? ActiveCell { get; }`, `public IReadOnlySet<int> SelectedRows { get; }`, `public void ScrollToRow(int sourceIndex)`
  - `internal int PageSize { get; set; }` (at least 1), `internal int ActiveViewRow { get; }`
  - `internal int TakeScrollRequest()`
  - `internal void Click(DataTableCell cell, bool isCtrlHeld, bool isShiftHeld)`
  - `internal void Move(DataTableMove move, bool isExtending = false)`
  - `internal void SelectAll()`
  - `internal string BuildCopyText()`
  - Partial method declarations Task 5 implements: `partial void FinishEditingUnless(DataTableCell? cellKeptOpen)`, `partial void OnRowIdentityLost()`, `partial void OnViewRebuilt()`

- [ ] **Step 1: Write the failing tests**

Create `tests/ImGui.Widgets.Tests/DataTableStateNavigationTests.cs`:

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using System;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests the active cell, the selection, keyboard movement and copying. No ImGui context is required.
/// </summary>
[TestClass]
public class DataTableStateNavigationTests
{
	private const int Name = DataTableFixture.NameColumn;
	private const int Age = DataTableFixture.AgeColumn;
	private const int Length = DataTableFixture.LengthColumn;

	private readonly DataTableFixture fixture = new();

	private static ImGuiWidgets.DataTableCell Cell(int sourceIndex, int column) => new(sourceIndex, column);

	private static void AssertSelected(ImGuiWidgets.DataTableState<DataTablePerson> state, params int[] expected) =>
		Assert.AreSequenceEqual(expected.Order(), state.SelectedRows.Order());

	private ImGuiWidgets.DataTableState<DataTablePerson> StateWithActive(int sourceIndex, int column)
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = fixture.CreateState();
		state.Click(Cell(sourceIndex, column), isCtrlHeld: false, isShiftHeld: false);
		return state;
	}

	[TestMethod]
	public void Click_ActivatesTheCellAndSelectsOnlyItsRow()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = StateWithActive(2, Age);

		Assert.AreEqual(Cell(2, Age), state.ActiveCell);
		AssertSelected(state, 2);
	}

	[TestMethod]
	public void CtrlClick_TogglesOneRow()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = StateWithActive(0, Name);

		state.Click(Cell(2, Name), isCtrlHeld: true, isShiftHeld: false);
		AssertSelected(state, 0, 2);

		state.Click(Cell(0, Name), isCtrlHeld: true, isShiftHeld: false);
		AssertSelected(state, 2);
	}

	/// <summary>
	/// Sorted by age, the view is Bob, Dave, Alice, Carol. A range from Bob to Alice is those three as
	/// shown, where a range in source order would have been Alice and Bob.
	/// </summary>
	[TestMethod]
	public void ShiftClick_SelectsTheRangeAsShown()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = fixture.CreateState();
		state.SetSort(Age, isAscending: true);

		state.Click(Cell(1, Name), isCtrlHeld: false, isShiftHeld: false);
		state.Click(Cell(0, Name), isCtrlHeld: false, isShiftHeld: true);

		AssertSelected(state, 0, 1, 3);
	}

	[TestMethod]
	public void Click_OnAFilteredOutRow_IsIgnored()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = fixture.CreateState();
		state.SetFilter(Name, "ob");

		state.Click(Cell(0, Name), isCtrlHeld: false, isShiftHeld: false);

		Assert.IsNull(state.ActiveCell);
	}

	[TestMethod]
	public void Move_WithNoActiveCell_ActivatesTheFirstCell()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = fixture.CreateState();

		state.Move(ImGuiWidgets.DataTableMove.Down);

		Assert.AreEqual(Cell(0, Name), state.ActiveCell);
	}

	// DataTableMove is internal, and a public test method can't take an internal type, so the rows
	// name the move and the test parses it.
	private static ImGuiWidgets.DataTableMove Parse(string move) => Enum.Parse<ImGuiWidgets.DataTableMove>(move);

	[TestMethod]
	[DataRow("Up", 0, Age)]
	[DataRow("Down", 2, Age)]
	[DataRow("Left", 1, Name)]
	[DataRow("Right", 1, DataTableFixture.ActiveColumn)]
	[DataRow("Home", 1, Name)]
	[DataRow("End", 1, Length)]
	[DataRow("First", 0, Name)]
	[DataRow("Last", 3, Length)]
	public void Move_FromTheMiddle_GoesWhereItSays(string move, int sourceIndex, int column)
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = StateWithActive(1, Age);

		state.Move(Parse(move));

		Assert.AreEqual(Cell(sourceIndex, column), state.ActiveCell);
	}

	[TestMethod]
	[DataRow("Up")]
	[DataRow("Left")]
	[DataRow("PageUp")]
	[DataRow("Previous")]
	public void Move_PastTheFirstCell_StaysPut(string move)
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = StateWithActive(0, Name);

		state.Move(Parse(move));

		Assert.AreEqual(Cell(0, Name), state.ActiveCell);
	}

	[TestMethod]
	[DataRow("Down")]
	[DataRow("Right")]
	[DataRow("PageDown")]
	[DataRow("Next")]
	public void Move_PastTheLastCell_StaysPut(string move)
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = StateWithActive(3, Length);

		state.Move(Parse(move));

		Assert.AreEqual(Cell(3, Length), state.ActiveCell);
	}

	[TestMethod]
	public void Next_AtTheEndOfARow_WrapsToTheStartOfTheNext()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = StateWithActive(1, Length);

		state.Move(ImGuiWidgets.DataTableMove.Next);

		Assert.AreEqual(Cell(2, Name), state.ActiveCell);
	}

	[TestMethod]
	public void Previous_AtTheStartOfARow_WrapsToTheEndOfThePrevious()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = StateWithActive(1, Name);

		state.Move(ImGuiWidgets.DataTableMove.Previous);

		Assert.AreEqual(Cell(0, Length), state.ActiveCell);
	}

	[TestMethod]
	public void Move_SkipsHiddenColumns()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = StateWithActive(0, Name);
		state.SetColumnVisible(Age, isVisible: false);

		state.Move(ImGuiWidgets.DataTableMove.Right);

		Assert.AreEqual(Cell(0, DataTableFixture.ActiveColumn), state.ActiveCell);
	}

	[TestMethod]
	public void PageDown_MovesByThePageSizeAndClamps()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = StateWithActive(0, Name);
		state.PageSize = 2;

		state.Move(ImGuiWidgets.DataTableMove.PageDown);
		Assert.AreEqual(Cell(2, Name), state.ActiveCell);

		state.Move(ImGuiWidgets.DataTableMove.PageDown);
		Assert.AreEqual(Cell(3, Name), state.ActiveCell);
	}

	[TestMethod]
	public void Move_CollapsesTheSelectionToTheNewRow()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = StateWithActive(0, Name);
		state.Click(Cell(2, Name), isCtrlHeld: true, isShiftHeld: false);

		state.Move(ImGuiWidgets.DataTableMove.Down);

		AssertSelected(state, 3);
	}

	[TestMethod]
	public void MoveExtending_SelectsFromTheAnchor()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = StateWithActive(1, Name);

		state.Move(ImGuiWidgets.DataTableMove.Down, isExtending: true);
		state.Move(ImGuiWidgets.DataTableMove.Down, isExtending: true);

		AssertSelected(state, 1, 2, 3);
	}

	[TestMethod]
	public void SelectAll_SelectsOnlyTheRowsShown()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = fixture.CreateState();
		state.RowFilter = person => person.IsActive;

		state.SelectAll();

		AssertSelected(state, 0, 2, 3);
	}

	[TestMethod]
	public void ActiveCell_FollowsItsRowThroughASort()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = StateWithActive(0, Age);

		state.SetSort(Age, isAscending: true);

		Assert.AreEqual(Cell(0, Age), state.ActiveCell);
		Assert.AreEqual(2, state.ActiveViewRow, "Alice should be third when sorted by age.");
	}

	[TestMethod]
	public void ActiveCell_WhenFilteredOut_MovesToTheSamePositionClamped()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = StateWithActive(3, Age);

		state.SetFilter(Name, "ob");

		Assert.AreEqual(Cell(1, Age), state.ActiveCell);
	}

	[TestMethod]
	public void ActiveCell_WhenEveryRowIsFilteredOut_IsCleared()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = StateWithActive(0, Name);

		state.SetFilter(Name, "zzz");

		Assert.IsNull(state.ActiveCell);
	}

	[TestMethod]
	public void Selection_LosesRowsAFilterHides()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = fixture.CreateState();
		state.SelectAll();

		state.SetFilter(Name, "ob");

		AssertSelected(state, 1);
	}

	[TestMethod]
	public void ARowCountChange_ClearsTheSelection()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = fixture.CreateState();
		state.SelectAll();

		fixture.People.Add(new("Eve", 20, true, DataTableMood.Calm));
		state.Sync(fixture.People);

		Assert.IsEmpty(state.SelectedRows);
	}

	/// <summary>
	/// Review focus: the caller removes rows while one near the end is active. The active cell must end
	/// up on a row that still exists, or the next draw indexes past the end of the list.
	/// </summary>
	[TestMethod]
	public void RemovingRowsBelowTheActiveCell_KeepsItInRange()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = StateWithActive(3, Name);

		fixture.People.RemoveRange(2, 2);
		state.Sync(fixture.People);

		Assert.IsNotNull(state.ActiveCell);
		Assert.IsLessThan(2, state.ActiveCell.Value.SourceIndex, "The active cell points past the end of the rows.");
	}

	[TestMethod]
	public void TakeScrollRequest_ReportsTheMovedToRowOnce()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = StateWithActive(0, Name);

		state.Move(ImGuiWidgets.DataTableMove.Last);

		Assert.AreEqual(3, state.TakeScrollRequest());
		Assert.AreEqual(-1, state.TakeScrollRequest(), "A scroll request survived being taken.");
	}

	[TestMethod]
	public void ScrollToRow_OfAFilteredOutRow_IsDropped()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = fixture.CreateState();
		state.SetFilter(Name, "ob");

		state.ScrollToRow(0);

		Assert.AreEqual(-1, state.TakeScrollRequest());
	}

	[TestMethod]
	public void BuildCopyText_JoinsSelectedRowsAsShownWithVisibleColumns()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = fixture.CreateState();
		state.SetSort(Age, isAscending: true);
		state.SetColumnVisible(DataTableFixture.MoodColumn, isVisible: false);
		state.SetColumnVisible(Length, isVisible: false);
		state.Click(Cell(0, Name), isCtrlHeld: false, isShiftHeld: false);
		state.Click(Cell(1, Name), isCtrlHeld: true, isShiftHeld: false);

		Assert.AreEqual("Bob\t25\tFalse\nAlice\t30\tTrue", state.BuildCopyText());
	}

	[TestMethod]
	public void BuildCopyText_QuotesValuesThatWouldSplit()
	{
		fixture.People[0].Name = "Al\tice";
		ImGuiWidgets.DataTableState<DataTablePerson> state = StateWithActive(0, Name);
		for (int column = 1; column < state.Columns.Count; column++)
		{
			state.SetColumnVisible(column, isVisible: false);
		}

		Assert.AreEqual("\"Al\tice\"", state.BuildCopyText());
	}

	[TestMethod]
	public void BuildCopyText_WithNothingSelected_IsEmpty() =>
		Assert.AreEqual(string.Empty, fixture.CreateState().BuildCopyText());
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet build tests/ImGui.Widgets.Tests/ImGui.Widgets.Tests.csproj`
Expected: FAIL to compile, `Click`, `Move` and the rest do not exist.

- [ ] **Step 3: Create the navigation partial**

Create `ImGui.Widgets/DataTableState.Navigation.cs`:

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;
using System.Collections.Generic;
using System.Linq;

public static partial class ImGuiWidgets
{
	/// <summary>A movement of a data table's active cell.</summary>
	internal enum DataTableMove
	{
		Up,
		Down,
		Left,
		Right,
		PageUp,
		PageDown,
		Home,
		End,
		First,
		Last,
		Next,
		Previous,
	}

	public sealed partial class DataTableState<TRow>
	{
		private readonly HashSet<int> selectedRows = [];
		private DataTableCell? activeCell;
		private int anchorSource = -1;
		private int scrollRequest = -1;
		private int pageSize = 10;

		/// <summary>Gets the cell keyboard input goes to, or <see langword="null"/> until one is chosen.</summary>
		public DataTableCell? ActiveCell => activeCell;

		/// <summary>Gets the selected rows, as indices into the caller's list.</summary>
		public IReadOnlySet<int> SelectedRows => selectedRows;

		internal int PageSize
		{
			get => pageSize;
			set => pageSize = Math.Max(1, value);
		}

		internal int ActiveViewRow => activeCell is DataTableCell cell ? ViewPositionOf(cell.SourceIndex) : -1;

		/// <summary>Brings a row into view the next time the table is drawn.</summary>
		/// <param name="sourceIndex">The row's index in the caller's list. Ignored if that row isn't shown.</param>
		public void ScrollToRow(int sourceIndex) => scrollRequest = sourceIndex;

		/// <summary>Takes the pending scroll request, so it's acted on once.</summary>
		/// <returns>The source index to scroll to, or -1 for none.</returns>
		internal int TakeScrollRequest()
		{
			int requested = scrollRequest;
			scrollRequest = -1;
			return ViewPositionOf(requested) >= 0 ? requested : -1;
		}

		internal void Click(DataTableCell cell, bool isCtrlHeld, bool isShiftHeld)
		{
			if (!IsShown(cell))
			{
				return;
			}

			FinishEditingUnless(cell);
			activeCell = cell;

			if (isShiftHeld && ViewPositionOf(anchorSource) >= 0)
			{
				SelectRange(anchorSource, cell.SourceIndex);
			}
			else if (isCtrlHeld)
			{
				if (!selectedRows.Remove(cell.SourceIndex))
				{
					selectedRows.Add(cell.SourceIndex);
				}

				anchorSource = cell.SourceIndex;
			}
			else
			{
				SelectOnly(cell.SourceIndex);
			}
		}

		internal void Move(DataTableMove move, bool isExtending = false)
		{
			FinishEditingUnless(null);

			if (view.Length == 0)
			{
				return;
			}

			DataTableCell destination = activeCell is DataTableCell current && ActiveViewRow >= 0
				? Step(move, ActiveViewRow, current.Column)
				: new DataTableCell(view[0], FirstVisibleColumn());

			activeCell = destination;
			scrollRequest = destination.SourceIndex;

			if (isExtending && ViewPositionOf(anchorSource) >= 0)
			{
				SelectRange(anchorSource, destination.SourceIndex);
			}
			else
			{
				SelectOnly(destination.SourceIndex);
			}
		}

		internal void SelectAll()
		{
			selectedRows.Clear();
			selectedRows.UnionWith(view);
		}

		/// <summary>
		/// Builds the text Ctrl+C puts on the clipboard: the selected rows as shown, visible columns only,
		/// tab separated, one row per line.
		/// </summary>
		internal string BuildCopyText()
		{
			int[] columns = [.. Enumerable.Range(0, Columns.Count).Where(IsColumnVisible)];
			IEnumerable<string> lines = view
				.Where(selectedRows.Contains)
				.Select(source => string.Join('\t', columns.Select(column => DataTableText.QuoteForCopy(Columns[column].GetText(rows[source])))));

			return string.Join('\n', lines);
		}

		/// <summary>Commits the edit in progress, unless it's in the given cell.</summary>
		partial void FinishEditingUnless(DataTableCell? cellKeptOpen);

		/// <summary>Called when source indices stop meaning the rows they did.</summary>
		partial void OnRowIdentityLost();

		/// <summary>Called after every rebuild, once the active cell and the selection are settled.</summary>
		partial void OnViewRebuilt();

		/// <summary>
		/// Forgets everything held by source index, for when rows were inserted or deleted and every
		/// index after the change now names a different row.
		/// </summary>
		private void ForgetRowIdentity()
		{
			selectedRows.Clear();
			anchorSource = -1;
			OnRowIdentityLost();
		}

		private void AfterRebuild(int previousPosition)
		{
			selectedRows.RemoveWhere(source => ViewPositionOf(source) < 0);

			if (activeCell is DataTableCell cell && ViewPositionOf(cell.SourceIndex) < 0)
			{
				activeCell = view.Length == 0
					? null
					: cell with { SourceIndex = view[Math.Clamp(previousPosition, 0, view.Length - 1)] };
			}

			OnViewRebuilt();
		}

		private bool IsShown(DataTableCell cell) =>
			ViewPositionOf(cell.SourceIndex) >= 0 && cell.Column >= 0 && cell.Column < Columns.Count;

		private void SelectOnly(int sourceIndex)
		{
			selectedRows.Clear();
			selectedRows.Add(sourceIndex);
			anchorSource = sourceIndex;
		}

		private void SelectRange(int fromSource, int toSource)
		{
			int from = ViewPositionOf(fromSource);
			int to = ViewPositionOf(toSource);

			selectedRows.Clear();
			for (int position = Math.Min(from, to); position <= Math.Max(from, to); position++)
			{
				selectedRows.Add(view[position]);
			}
		}

		private DataTableCell Step(DataTableMove move, int row, int column)
		{
			int lastRow = view.Length - 1;

			(int Row, int Column) target = move switch
			{
				DataTableMove.Up => (Math.Max(0, row - 1), column),
				DataTableMove.Down => (Math.Min(lastRow, row + 1), column),
				DataTableMove.PageUp => (Math.Max(0, row - PageSize), column),
				DataTableMove.PageDown => (Math.Min(lastRow, row + PageSize), column),
				DataTableMove.Left => (row, VisibleColumnBefore(column) ?? column),
				DataTableMove.Right => (row, VisibleColumnAfter(column) ?? column),
				DataTableMove.Home => (row, FirstVisibleColumn()),
				DataTableMove.End => (row, LastVisibleColumn()),
				DataTableMove.First => (0, FirstVisibleColumn()),
				DataTableMove.Last => (lastRow, LastVisibleColumn()),
				DataTableMove.Next => StepNext(row, column, lastRow),
				DataTableMove.Previous => StepPrevious(row, column),
				_ => (row, column),
			};

			return new DataTableCell(view[target.Row], target.Column);
		}

		private (int Row, int Column) StepNext(int row, int column, int lastRow)
		{
			if (VisibleColumnAfter(column) is int next)
			{
				return (row, next);
			}

			if (row < lastRow)
			{
				return (row + 1, FirstVisibleColumn());
			}

			return (row, column);
		}

		private (int Row, int Column) StepPrevious(int row, int column)
		{
			if (VisibleColumnBefore(column) is int previous)
			{
				return (row, previous);
			}

			if (row > 0)
			{
				return (row - 1, LastVisibleColumn());
			}

			return (row, column);
		}

		private int? VisibleColumnAfter(int column)
		{
			for (int candidate = column + 1; candidate < Columns.Count; candidate++)
			{
				if (IsColumnVisible(candidate))
				{
					return candidate;
				}
			}

			return null;
		}

		private int? VisibleColumnBefore(int column)
		{
			for (int candidate = column - 1; candidate >= 0; candidate--)
			{
				if (IsColumnVisible(candidate))
				{
					return candidate;
				}
			}

			return null;
		}

		private int FirstVisibleColumn() => VisibleColumnAfter(-1) ?? 0;

		private int LastVisibleColumn() => VisibleColumnBefore(Columns.Count) ?? Columns.Count - 1;
	}
}
```

- [ ] **Step 4: Wire the partial into the core**

In `ImGui.Widgets/DataTableState.cs`, in `Sync`, replace:

```csharp
			syncedCount = source.Count;
			Rebuild();
```

with:

```csharp
			// Inserting or deleting rows shifts every source index after the change, so the selection
			// no longer names the rows it did. The first sync has nothing to forget.
			if (syncedCount >= 0)
			{
				ForgetRowIdentity();
			}

			syncedCount = source.Count;
			Rebuild();
```

In `Rebuild`, add as the first line:

```csharp
			int previousPosition = ActiveViewRow;
```

and as the last line, after the `viewPositions` loop:

```csharp
			AfterRebuild(previousPosition);
```

- [ ] **Step 5: Run the tests to verify they pass**

```bash
dotnet build tests/ImGui.Widgets.Tests/ImGui.Widgets.Tests.csproj
./tests/ImGui.Widgets.Tests/bin/Debug/net10.0/ktsu.ImGui.Widgets.Tests --filter "FullyQualifiedName~DataTableState"
```

Expected: PASS for both the navigation and the view tests, 0 warnings.

- [ ] **Step 6: Commit**

```bash
git add ImGui.Widgets/DataTableState.cs ImGui.Widgets/DataTableState.Navigation.cs tests/ImGui.Widgets.Tests/DataTableStateNavigationTests.cs
git commit -m "[minor] Move a data table's active cell and select its rows"
```

---

## Task 5: Editing, toggling, and the context menu snapshot

**Files:**
- Create: `ImGui.Widgets/DataTableState.Editing.cs`
- Test: `tests/ImGui.Widgets.Tests/DataTableStateEditingTests.cs`

**Interfaces:**
- Consumes: `DataTableColumn<TRow>.CreateSession` and `.Toggle`, `DataTableEditSession` (Task 2). `Click`, `activeCell`, `selectedRows`, the three partial methods (Task 4).
- Produces:
  - `public readonly record struct DataTableContextMenu(DataTableCell Cell, IReadOnlySet<int> SelectedRows)` nested in `ImGuiWidgets`
  - `public bool IsEditing { get; }`
  - `internal DataTableEditSession? EditSession { get; }`
  - `internal DataTableContextMenu? ContextMenu { get; }`
  - `internal bool BeginEdit(char? firstCharacter = null)`, `internal void CommitEdit()`, `internal void CancelEdit()`, `internal void Toggle()`
  - `internal void OpenContextMenu(DataTableCell cell)`, `internal bool TakeContextMenuRequest()`

- [ ] **Step 1: Write the failing tests**

Create `tests/ImGui.Widgets.Tests/DataTableStateEditingTests.cs`:

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using System;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests the data table's editing state machine. No ImGui context is required, because a session's
/// working value can be set directly instead of typed.
/// </summary>
[TestClass]
public class DataTableStateEditingTests
{
	private const int Name = DataTableFixture.NameColumn;
	private const int Age = DataTableFixture.AgeColumn;

	private readonly DataTableFixture fixture = new();

	private static ImGuiWidgets.DataTableCell Cell(int sourceIndex, int column) => new(sourceIndex, column);

	private ImGuiWidgets.DataTableState<DataTablePerson> StateWithActive(int sourceIndex, int column)
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = fixture.CreateState();
		state.Click(Cell(sourceIndex, column), isCtrlHeld: false, isShiftHeld: false);
		return state;
	}

	/// <summary>Stands in for typing: sets the open text session's working text directly.</summary>
	private static void Type(ImGuiWidgets.DataTableState<DataTablePerson> state, string text)
	{
		switch (state.EditSession)
		{
			case ImGuiWidgets.DataTableTextSession<DataTablePerson, string> name:
				name.Text = text;
				break;

			case ImGuiWidgets.DataTableTextSession<DataTablePerson, int> age:
				age.Text = text;
				break;

			default:
				Assert.Fail("There is no open text session to type into.");
				break;
		}
	}

	[TestMethod]
	public void BeginEdit_WithNoActiveCell_DoesNothing()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = fixture.CreateState();

		Assert.IsFalse(state.BeginEdit());
		Assert.IsFalse(state.IsEditing);
	}

	[TestMethod]
	public void BeginEdit_OnAReadOnlyColumn_DoesNothing()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = StateWithActive(0, DataTableFixture.LengthColumn);

		Assert.IsFalse(state.BeginEdit());
	}

	[TestMethod]
	public void BeginEdit_OnAToggle_DoesNothing()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = StateWithActive(0, DataTableFixture.ActiveColumn);

		Assert.IsFalse(state.BeginEdit(), "A bool cell opened an editor instead of toggling.");
	}

	[TestMethod]
	public void BeginEdit_OnTheActiveCell_OpensASessionThere()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = StateWithActive(1, Name);

		Assert.IsTrue(state.BeginEdit());
		Assert.IsTrue(state.IsEditing);
		Assert.AreEqual(1, state.EditSession!.SourceIndex);
		Assert.AreEqual(Name, state.EditSession.Column);
	}

	[TestMethod]
	public void CommitEdit_WithAChangedValue_RaisesOneEditAndStopsEditing()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = StateWithActive(1, Name);
		state.BeginEdit();
		Type(state, "Robert");

		state.CommitEdit();

		Assert.IsFalse(state.IsEditing);
		Assert.AreEqual("Robert", fixture.NameEdits.Single().NewValue);
	}

	[TestMethod]
	public void CommitEdit_WithAnUnchangedValue_RaisesNothing()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = StateWithActive(1, Name);
		state.BeginEdit();

		state.CommitEdit();

		Assert.IsEmpty(fixture.NameEdits);
	}

	[TestMethod]
	public void CancelEdit_RaisesNothing()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = StateWithActive(1, Name);
		state.BeginEdit();
		Type(state, "Robert");

		state.CancelEdit();

		Assert.IsFalse(state.IsEditing);
		Assert.IsEmpty(fixture.NameEdits);
	}

	[TestMethod]
	public void Move_WhileEditing_CommitsFirst()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = StateWithActive(1, Name);
		state.BeginEdit();
		Type(state, "Robert");

		state.Move(ImGuiWidgets.DataTableMove.Down);

		Assert.IsFalse(state.IsEditing);
		Assert.AreEqual("Robert", fixture.NameEdits.Single().NewValue);
		Assert.AreEqual(Cell(2, Name), state.ActiveCell);
	}

	[TestMethod]
	public void ClickingAnotherCell_CommitsFirst()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = StateWithActive(1, Name);
		state.BeginEdit();
		Type(state, "Robert");

		state.Click(Cell(3, Age), isCtrlHeld: false, isShiftHeld: false);

		Assert.IsFalse(state.IsEditing);
		Assert.HasCount(1, fixture.NameEdits);
	}

	[TestMethod]
	public void ClickingTheCellBeingEdited_KeepsEditing()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = StateWithActive(1, Name);
		state.BeginEdit();

		state.Click(Cell(1, Name), isCtrlHeld: false, isShiftHeld: false);

		Assert.IsTrue(state.IsEditing);
	}

	[TestMethod]
	public void ARowCountChange_CancelsTheEdit()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = StateWithActive(1, Name);
		state.BeginEdit();
		Type(state, "Robert");

		fixture.People.Add(new("Eve", 20, true, DataTableMood.Calm));
		state.Sync(fixture.People);

		Assert.IsFalse(state.IsEditing);
		Assert.IsEmpty(fixture.NameEdits, "An edit was committed to a row whose index may no longer name it.");
	}

	[TestMethod]
	public void AFilterHidingTheEditedRow_CommitsTheEdit()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = StateWithActive(1, Name);
		state.BeginEdit();
		Type(state, "Robert");

		state.SetFilter(Name, "Alice");

		Assert.IsFalse(state.IsEditing);
		Assert.HasCount(1, fixture.NameEdits);
	}

	[TestMethod]
	public void Toggle_OnABoolCell_RaisesTheOppositeValue()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = StateWithActive(1, DataTableFixture.ActiveColumn);

		state.Toggle();

		Assert.IsTrue(fixture.ActiveEdits.Single().NewValue, "Bob is inactive, so a toggle should make him active.");
	}

	[TestMethod]
	public void Toggle_OnAnythingElse_DoesNothing()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = StateWithActive(1, Name);

		state.Toggle();

		Assert.IsEmpty(fixture.ActiveEdits);
		Assert.IsEmpty(fixture.NameEdits);
	}

	/// <summary>
	/// Review focus: the documented way to use the table is an edit callback that applies the edit and
	/// calls Refresh. The state must have left editing before the callback runs.
	/// </summary>
	[TestMethod]
	public void AnEditCallbackThatAppliesAndRefreshes_ReordersTheView()
	{
		ImGuiWidgets.DataTableState<DataTablePerson>? state = null;
		ImGuiWidgets.DataTableColumn<DataTablePerson, int> age = new()
		{
			Label = "Age",
			Value = person => person.Age,
			OnEdit = edit =>
			{
				Assert.IsFalse(state!.IsEditing, "The callback ran while the state was still editing.");
				edit.Row.Age = edit.NewValue;
				state.Refresh();
			},
		};

		state = new([fixture.Name, age]);
		state.Sync(fixture.People);
		state.SetSort(1, isAscending: true);
		state.Click(Cell(1, 1), isCtrlHeld: false, isShiftHeld: false);
		state.BeginEdit();
		Type(state, "99");

		state.CommitEdit();

		Assert.AreSequenceEqual(new[] { 3, 0, 2, 1 }, state.View);
	}

	/// <summary>Review focus: a callback that throws must not leave the table stuck editing.</summary>
	[TestMethod]
	public void AnEditCallbackThatThrows_LeavesTheStateNotEditing()
	{
		ImGuiWidgets.DataTableColumn<DataTablePerson, string> name = new()
		{
			Label = "Name",
			Value = person => person.Name,
			OnEdit = _ => throw new InvalidOperationException("The caller refused the edit."),
		};

		ImGuiWidgets.DataTableState<DataTablePerson> state = new([name]);
		state.Sync(fixture.People);
		state.Click(Cell(0, 0), isCtrlHeld: false, isShiftHeld: false);
		state.BeginEdit();
		Type(state, "Alicia");

		Assert.ThrowsExactly<InvalidOperationException>(state.CommitEdit);
		Assert.IsFalse(state.IsEditing);
	}

	[TestMethod]
	public void OpenContextMenu_OnAnUnselectedRow_SelectsItAlone()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = StateWithActive(0, Name);

		state.OpenContextMenu(Cell(2, Age));

		Assert.AreEqual(Cell(2, Age), state.ActiveCell);
		Assert.AreSequenceEqual(new[] { 2 }, state.ContextMenu!.Value.SelectedRows.Order());
	}

	[TestMethod]
	public void OpenContextMenu_OnASelectedRow_KeepsTheSelection()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = fixture.CreateState();
		state.SelectAll();

		state.OpenContextMenu(Cell(2, Age));

		Assert.HasCount(4, state.ContextMenu!.Value.SelectedRows);
	}

	[TestMethod]
	public void ContextMenu_KeepsTheSelectionAsItWasWhenOpened()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = fixture.CreateState();
		state.SelectAll();
		state.OpenContextMenu(Cell(2, Age));

		state.Click(Cell(0, Name), isCtrlHeld: false, isShiftHeld: false);

		Assert.HasCount(4, state.ContextMenu!.Value.SelectedRows);
	}

	[TestMethod]
	public void TakeContextMenuRequest_IsOneShot()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = fixture.CreateState();
		state.OpenContextMenu(Cell(0, Name));

		Assert.IsTrue(state.TakeContextMenuRequest());
		Assert.IsFalse(state.TakeContextMenuRequest());
	}
}
```

If `Assert.HasCount` doesn't exist in this MSTest version, use `Assert.AreEqual(n, collection.Count)`.

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet build tests/ImGui.Widgets.Tests/ImGui.Widgets.Tests.csproj`
Expected: FAIL to compile, `BeginEdit`, `IsEditing` and the rest do not exist.

- [ ] **Step 3: Create the editing partial**

Create `ImGui.Widgets/DataTableState.Editing.cs`:

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System.Collections.Generic;

public static partial class ImGuiWidgets
{
	/// <summary>What a data table's context menu was opened on.</summary>
	/// <param name="Cell">The cell that was right-clicked.</param>
	/// <param name="SelectedRows">
	/// The selected rows at the moment the menu opened, as indices into the caller's list. A copy, so it
	/// doesn't change under the menu while it's open.
	/// </param>
	public readonly record struct DataTableContextMenu(DataTableCell Cell, IReadOnlySet<int> SelectedRows);

	public sealed partial class DataTableState<TRow>
	{
		private bool isContextMenuRequested;

		/// <summary>Gets a value indicating whether a cell is being edited.</summary>
		public bool IsEditing => EditSession is not null;

		internal DataTableEditSession? EditSession { get; private set; }

		internal DataTableContextMenu? ContextMenu { get; private set; }

		/// <summary>Opens an editor on the active cell.</summary>
		/// <param name="firstCharacter">
		/// The character typed to begin editing, which replaces the value, or <see langword="null"/> to
		/// begin from the current value.
		/// </param>
		/// <returns>True when an editor opened. False for a read-only cell, a toggle, or no active cell.</returns>
		internal bool BeginEdit(char? firstCharacter = null)
		{
			if (EditSession is not null || activeCell is not DataTableCell cell || ViewPositionOf(cell.SourceIndex) < 0)
			{
				return false;
			}

			EditSession = Columns[cell.Column].CreateSession(cell.Column, cell.SourceIndex, rows[cell.SourceIndex], firstCharacter);
			return EditSession is not null;
		}

		/// <summary>Closes the editor and reports the edit if the value changed.</summary>
		/// <remarks>
		/// The session is cleared before the caller hears about the edit, so a callback that applies the
		/// edit and calls <see cref="Refresh"/> sees a table that has finished editing, and one that throws
		/// doesn't leave the table stuck mid-edit.
		/// </remarks>
		internal void CommitEdit()
		{
			DataTableEditSession? session = EditSession;
			EditSession = null;
			session?.Commit();
		}

		internal void CancelEdit() => EditSession = null;

		/// <summary>Flips the active cell if it's an editable <see cref="bool"/>. Does nothing otherwise.</summary>
		internal void Toggle()
		{
			if (EditSession is null && activeCell is DataTableCell cell && ViewPositionOf(cell.SourceIndex) >= 0)
			{
				Columns[cell.Column].Toggle(cell.SourceIndex, rows[cell.SourceIndex]);
			}
		}

		/// <summary>
		/// Makes the right-clicked cell active and records what the menu is for. A row that wasn't
		/// selected becomes the only selected row, which is how file explorers behave.
		/// </summary>
		internal void OpenContextMenu(DataTableCell cell)
		{
			if (!IsShown(cell))
			{
				return;
			}

			if (selectedRows.Contains(cell.SourceIndex))
			{
				FinishEditingUnless(cell);
				activeCell = cell;
			}
			else
			{
				Click(cell, isCtrlHeld: false, isShiftHeld: false);
			}

			ContextMenu = new DataTableContextMenu(cell, new HashSet<int>(selectedRows));
			isContextMenuRequested = true;
		}

		/// <summary>Takes the request to open the context menu, so the popup is opened once.</summary>
		internal bool TakeContextMenuRequest()
		{
			bool isRequested = isContextMenuRequested;
			isContextMenuRequested = false;
			return isRequested;
		}

		partial void FinishEditingUnless(DataTableCell? cellKeptOpen)
		{
			if (EditSession is not DataTableEditSession session)
			{
				return;
			}

			bool isKeptOpen = cellKeptOpen is DataTableCell kept
				&& kept.SourceIndex == session.SourceIndex
				&& kept.Column == session.Column;

			if (!isKeptOpen)
			{
				CommitEdit();
			}
		}

		// An edit to a row whose index may now name a different row can't be committed safely, so it's
		// dropped rather than applied to the wrong row.
		partial void OnRowIdentityLost() => CancelEdit();

		// A filter that hides the row being edited takes the editor off screen, so the edit is kept by
		// committing it rather than left open where nobody can see it.
		partial void OnViewRebuilt()
		{
			if (EditSession is DataTableEditSession session && ViewPositionOf(session.SourceIndex) < 0)
			{
				CommitEdit();
			}
		}
	}
}
```

- [ ] **Step 4: Run the tests to verify they pass**

```bash
dotnet build tests/ImGui.Widgets.Tests/ImGui.Widgets.Tests.csproj
./tests/ImGui.Widgets.Tests/bin/Debug/net10.0/ktsu.ImGui.Widgets.Tests --filter "FullyQualifiedName~DataTable"
```

Expected: PASS for every data table suite so far, 0 warnings.

- [ ] **Step 5: Commit**

```bash
git add ImGui.Widgets/DataTableState.Editing.cs tests/ImGui.Widgets.Tests/DataTableStateEditingTests.cs
git commit -m "[minor] Edit one data table cell at a time and report each edit"
```

---

## Task 6: The keyboard commands

**Files:**
- Create: `ImGui.Widgets/DataTableCommands.cs`
- Test: `tests/ImGui.Widgets.Tests/DataTableCommandsTests.cs`

**Interfaces:**
- Consumes: `KeyChordMatcher.TryMapKey` (Task 1). `ICommandRegistry`, `IKeybindingService`, `Command`, `Chord` from `ktsu.Keybinding`.
- Produces: `public static class DataTableCommands` nested in `ImGuiWidgets`, with:
  - `public const string Category`, and one `public const string` per command: `MoveUp`, `MoveDown`, `MoveLeft`, `MoveRight`, `PageUp`, `PageDown`, `Home`, `End`, `First`, `Last`, `Next`, `Previous`, `BeginEdit`, `Commit`, `Cancel`, `Toggle`, `Copy`, `SelectAll`
  - `public static IReadOnlyList<Command> All { get; }`
  - `public static IReadOnlyDictionary<string, string> DefaultChords { get; }`
  - `public static int Register(ICommandRegistry registry, IKeybindingService keybindings, bool bindDefaultChords = true)`
  - `internal static Chord? DefaultChordOf(string commandId)`

- [ ] **Step 1: Write the failing tests**

Create `tests/ImGui.Widgets.Tests/DataTableCommandsTests.cs`:

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using System.Linq;

using ktsu.Keybinding.Core.Models;
using ktsu.Keybinding.Core.Services;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>Tests the data table's keyboard commands as a keymap sees them.</summary>
[TestClass]
public class DataTableCommandsTests
{
	private static (CommandRegistry Registry, KeybindingService Keybindings) CreateKeymap()
	{
		CommandRegistry registry = new();
		KeybindingService keybindings = new(registry, new ProfileManager());
		keybindings.CreateProfile("default", "Default");
		keybindings.SetActiveProfile("default");
		return (registry, keybindings);
	}

	[TestMethod]
	public void EveryCommand_HasADefaultChord() =>
		Assert.AreSequenceEqual(
			ImGuiWidgets.DataTableCommands.All.Select(command => command.Id.ToString()).Order(),
			ImGuiWidgets.DataTableCommands.DefaultChords.Keys.Order());

	/// <summary>
	/// A default chord naming a key ImGui doesn't have would parse, register and bind without complaint,
	/// and then never fire.
	/// </summary>
	[TestMethod]
	public void EveryDefaultChord_NamesKeysImGuiHas()
	{
		foreach ((string command, string text) in ImGuiWidgets.DataTableCommands.DefaultChords)
		{
			foreach (string note in Chord.Parse(text).Notes.Select(note => note.ToString()))
			{
				if (note is "CTRL" or "SHIFT" or "ALT")
				{
					continue;
				}

				Assert.IsTrue(KeyChordMatcher.TryMapKey(note, out _), $"{command} is bound to '{text}', and ImGui has no key named {note}.");
			}
		}
	}

	[TestMethod]
	public void Register_BindsEveryDefaultOnce()
	{
		(CommandRegistry registry, KeybindingService keybindings) = CreateKeymap();

		Assert.AreEqual(ImGuiWidgets.DataTableCommands.DefaultChords.Count, ImGuiWidgets.DataTableCommands.Register(registry, keybindings));
		Assert.AreEqual(0, ImGuiWidgets.DataTableCommands.Register(registry, keybindings), "Registering again rebound something.");
	}

	[TestMethod]
	public void Register_LeavesAChordTheUserBoundAlone()
	{
		(CommandRegistry registry, KeybindingService keybindings) = CreateKeymap();
		keybindings.BindChord(ImGuiWidgets.DataTableCommands.Copy, keybindings.ParseChord("Ctrl+Insert"));

		ImGuiWidgets.DataTableCommands.Register(registry, keybindings);

		Assert.AreEqual(keybindings.ParseChord("Ctrl+Insert"), keybindings.GetChord(ImGuiWidgets.DataTableCommands.Copy));
	}

	[TestMethod]
	public void Register_WithoutDefaults_BindsNothing()
	{
		(CommandRegistry registry, KeybindingService keybindings) = CreateKeymap();

		Assert.AreEqual(0, ImGuiWidgets.DataTableCommands.Register(registry, keybindings, bindDefaultChords: false));
		Assert.IsTrue(registry.IsCommandRegistered(ImGuiWidgets.DataTableCommands.Copy), "The commands were not registered.");
	}

	[TestMethod]
	public void DefaultChordOf_ParsesTheDefault() =>
		Assert.AreEqual(Chord.Parse("Ctrl+C"), ImGuiWidgets.DataTableCommands.DefaultChordOf(ImGuiWidgets.DataTableCommands.Copy));

	[TestMethod]
	public void DefaultChordOf_AnUnknownCommand_IsNull() =>
		Assert.IsNull(ImGuiWidgets.DataTableCommands.DefaultChordOf("datatable.nothing"));
}
```

If `Command.Id` isn't a type whose `ToString()` is the id string, use whatever property `Command` exposes for it (check `Keybinding.Core/Models/Command.cs`).

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet build tests/ImGui.Widgets.Tests/ImGui.Widgets.Tests.csproj`
Expected: FAIL to compile, `DataTableCommands` does not exist.

- [ ] **Step 3: Create the commands**

Create `ImGui.Widgets/DataTableCommands.cs`:

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System.Collections.Generic;
using System.Linq;

using ktsu.Keybinding.Core.Contracts;
using ktsu.Keybinding.Core.Models;

public static partial class ImGuiWidgets
{
	/// <summary>The data table's keyboard commands, in the form <c>ktsu.Keybinding</c> registers and binds.</summary>
	/// <remarks>
	/// A table given an <see cref="IKeybindingService"/> through <see cref="DataTableOptions.Keybindings"/>
	/// reads each command's chord from the service's active profile, so the host's keymap decides which
	/// keys drive it. Without a service the table uses <see cref="DefaultChords"/>, plus Enter to begin
	/// editing, which a profile holding one chord per command can't express alongside F2.
	/// </remarks>
	public static class DataTableCommands
	{
		/// <summary>The category the commands are registered under.</summary>
		public const string Category = "Data Table";

		/// <summary>Move the active cell up one row.</summary>
		public const string MoveUp = "datatable.moveup";

		/// <summary>Move the active cell down one row.</summary>
		public const string MoveDown = "datatable.movedown";

		/// <summary>Move the active cell left one column.</summary>
		public const string MoveLeft = "datatable.moveleft";

		/// <summary>Move the active cell right one column.</summary>
		public const string MoveRight = "datatable.moveright";

		/// <summary>Move the active cell up one page.</summary>
		public const string PageUp = "datatable.pageup";

		/// <summary>Move the active cell down one page.</summary>
		public const string PageDown = "datatable.pagedown";

		/// <summary>Move to the first column of the row.</summary>
		public const string Home = "datatable.home";

		/// <summary>Move to the last column of the row.</summary>
		public const string End = "datatable.end";

		/// <summary>Move to the first cell of the table.</summary>
		public const string First = "datatable.first";

		/// <summary>Move to the last cell of the table.</summary>
		public const string Last = "datatable.last";

		/// <summary>Move to the next cell, wrapping to the next row.</summary>
		public const string Next = "datatable.next";

		/// <summary>Move to the previous cell, wrapping to the previous row.</summary>
		public const string Previous = "datatable.previous";

		/// <summary>Begin editing the active cell.</summary>
		public const string BeginEdit = "datatable.beginedit";

		/// <summary>Commit the edit and move down.</summary>
		public const string Commit = "datatable.commit";

		/// <summary>Cancel the edit.</summary>
		public const string Cancel = "datatable.cancel";

		/// <summary>Flip the active cell when it's a checkbox.</summary>
		public const string Toggle = "datatable.toggle";

		/// <summary>Copy the selected rows as tab-separated text.</summary>
		public const string Copy = "datatable.copy";

		/// <summary>Select every row shown.</summary>
		public const string SelectAll = "datatable.selectall";

		/// <summary>Gets every command, ready to register.</summary>
		public static IReadOnlyList<Command> All { get; } =
		[
			new(MoveUp, "Move Up", "Move the active cell up one row", Category),
			new(MoveDown, "Move Down", "Move the active cell down one row", Category),
			new(MoveLeft, "Move Left", "Move the active cell left one column", Category),
			new(MoveRight, "Move Right", "Move the active cell right one column", Category),
			new(PageUp, "Page Up", "Move the active cell up one page", Category),
			new(PageDown, "Page Down", "Move the active cell down one page", Category),
			new(Home, "Row Start", "Move to the first column of the row", Category),
			new(End, "Row End", "Move to the last column of the row", Category),
			new(First, "Table Start", "Move to the first cell of the table", Category),
			new(Last, "Table End", "Move to the last cell of the table", Category),
			new(Next, "Next Cell", "Move to the next cell, wrapping to the next row", Category),
			new(Previous, "Previous Cell", "Move to the previous cell, wrapping to the previous row", Category),
			new(BeginEdit, "Edit Cell", "Begin editing the active cell", Category),
			new(Commit, "Commit Edit", "Commit the edit and move down", Category),
			new(Cancel, "Cancel Edit", "Cancel the edit", Category),
			new(Toggle, "Toggle Cell", "Flip the active cell when it is a checkbox", Category),
			new(Copy, "Copy Rows", "Copy the selected rows as tab-separated text", Category),
			new(SelectAll, "Select All Rows", "Select every row shown", Category),
		];

		/// <summary>Gets the chord each command is bound to unless the user says otherwise.</summary>
		public static IReadOnlyDictionary<string, string> DefaultChords { get; } = new Dictionary<string, string>
		{
			[MoveUp] = "Up",
			[MoveDown] = "Down",
			[MoveLeft] = "Left",
			[MoveRight] = "Right",
			[PageUp] = "PageUp",
			[PageDown] = "PageDown",
			[Home] = "Home",
			[End] = "End",
			[First] = "Ctrl+Home",
			[Last] = "Ctrl+End",
			[Next] = "Tab",
			[Previous] = "Shift+Tab",
			[BeginEdit] = "F2",
			[Commit] = "Enter",
			[Cancel] = "Escape",
			[Toggle] = "Space",
			[Copy] = "Ctrl+C",
			[SelectAll] = "Ctrl+A",
		};

		// Parsed once, after DefaultChords, which static initialization order guarantees because both are
		// initialized in the order they're written.
		private static readonly Dictionary<string, Chord> ParsedDefaults =
			DefaultChords.ToDictionary(pair => pair.Key, pair => Chord.Parse(pair.Value));

		/// <summary>Registers the commands, and binds each one that has no chord yet to its default.</summary>
		/// <param name="registry">Where the host's commands are registered.</param>
		/// <param name="keybindings">The host's keybindings.</param>
		/// <param name="bindDefaultChords">False to register the commands and leave every chord to the host.</param>
		/// <returns>How many chords were bound.</returns>
		/// <remarks>
		/// Safe to call on every start-up. A command already registered is left alone, and a chord the
		/// user already bound is not overwritten. Chords are bound in the active profile, so there has to
		/// be one for any to be bound.
		/// </remarks>
		public static int Register(ICommandRegistry registry, IKeybindingService keybindings, bool bindDefaultChords = true)
		{
			Ensure.NotNull(registry);
			Ensure.NotNull(keybindings);

			foreach (Command command in All.Where(command => !registry.IsCommandRegistered(command.Id)))
			{
				registry.RegisterCommand(command);
			}

			if (!bindDefaultChords)
			{
				return 0;
			}

			int bound = 0;
			foreach ((string commandId, string chord) in DefaultChords)
			{
				if (!keybindings.HasChordBinding(commandId) && keybindings.BindChord(commandId, keybindings.ParseChord(chord)))
				{
					bound++;
				}
			}

			return bound;
		}

		/// <summary>Gets a command's default chord, for a table with no keymap.</summary>
		internal static Chord? DefaultChordOf(string commandId) =>
			ParsedDefaults.TryGetValue(commandId, out Chord? chord) ? chord : null;
	}
}
```

`registry.IsCommandRegistered(command.Id)` mirrors `NodeEditorCommands.Register` exactly. If `Command.Id` needs converting for that call, do what `NodeEditorCommands` does.

- [ ] **Step 4: Run the tests to verify they pass**

```bash
dotnet build tests/ImGui.Widgets.Tests/ImGui.Widgets.Tests.csproj
./tests/ImGui.Widgets.Tests/bin/Debug/net10.0/ktsu.ImGui.Widgets.Tests --filter "FullyQualifiedName~DataTableCommandsTests"
```

Expected: PASS, 0 warnings.

- [ ] **Step 5: Commit**

```bash
git add ImGui.Widgets/DataTableCommands.cs tests/ImGui.Widgets.Tests/DataTableCommandsTests.cs
git commit -m "[minor] Declare the data table's keyboard commands for ktsu.Keybinding"
```

---

## Task 7: The renderer, with rows, cells, headers, sorting, and clicks

**Files:**
- Create: `ImGui.Widgets/TableClipping.cs`
- Modify: `ImGui.Widgets/VirtualTable.cs:275-296` (clipper setup) and `:364-365` (`RowName`)
- Create: `ImGui.Widgets/DataTable.cs`
- Create: `tests/ImGui.Widgets.UITests/DataTablePerson.cs`
- Test: `tests/ImGui.Widgets.UITests/DataTableTests.cs`

**Interfaces:**
- Consumes: everything on `DataTableState<TRow>` and `DataTableColumn<TRow>` from Tasks 2 to 5.
- Produces:
  - `internal static class TableClipping` with `Begin(ref ImGuiListClipper clipper, int rowCount, float rowHeight, int forcedRow)` and `RowName(string label, int row)`
  - `public sealed class DataTableOptions` with `Flags`, `OuterSize`, `RowHeight`, `HasFilterRow`, `FrozenColumns`, `Keybindings`, `OnContextMenu`
  - `public static void DataTable<TRow>(string label, IReadOnlyList<TRow> rows, DataTableState<TRow> state, DataTableOptions? options = null)`
  - `internal static partial class DataTableImpl<TRow>` with `Draw`, `SetupColumns`, `DrawHeaders`, `ReadSort`, `ReadColumnVisibility`, `DrawRows`, `DrawRow`, `DrawCell`, `HandleCellMouse` (Tasks 8 to 10 edit it)

- [ ] **Step 1: Write the failing UI tests**

Create `tests/ImGui.Widgets.UITests/DataTablePerson.cs`:

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

/// <summary>A row for the data table UI tests. Immutable, because these tests only watch edits being reported.</summary>
internal sealed record DataTablePerson(string Name, int Age, bool IsActive);
```

Create `tests/ImGui.Widgets.UITests/DataTableTests.cs`:

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.App.Testing;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>Drives <c>ImGuiWidgets.DataTable</c> on its own.</summary>
[TestClass]
public sealed class DataTableTests : WidgetTest
{
	private const string Label = "people";

	private readonly List<DataTablePerson> people = [];
	private readonly List<ImGuiWidgets.DataTableEdit<DataTablePerson, string>> nameEdits = [];
	private readonly List<ImGuiWidgets.DataTableEdit<DataTablePerson, bool>> activeEdits = [];
	private readonly List<ImGuiWidgets.DataTableContextMenu> menus = [];

	private ImGuiWidgets.DataTableState<DataTablePerson> state = null!;
	private ImGuiWidgets.DataTableOptions options = null!;

	[TestInitialize]
	public void CreateTable()
	{
		people.Clear();
		people.AddRange(
		[
			new("Alice", 30, true),
			new("Bob", 25, false),
			new("Carol", 35, true),
			new("Dave", 25, true),
		]);

		state = new(
		[
			new ImGuiWidgets.DataTableColumn<DataTablePerson, string> { Label = "Name", Value = person => person.Name, OnEdit = nameEdits.Add },
			new ImGuiWidgets.DataTableColumn<DataTablePerson, int> { Label = "Age", Value = person => person.Age },
			new ImGuiWidgets.DataTableColumn<DataTablePerson, bool> { Label = "Active", Value = person => person.IsActive, OnEdit = activeEdits.Add },
		]);

		// A fixed height, so there's window below the table to click away into.
		options = new() { RowHeight = 22f, OuterSize = new Vector2(0f, 300f), OnContextMenu = menus.Add };
	}

	private void Draw() => ImGuiWidgets.DataTable(Label, people, state, options);

	private static string Cell(int sourceIndex, string column) =>
		string.Create(CultureInfo.InvariantCulture, $"{Label}/[{sourceIndex}]/{column}");

	private static string Header(string column) => $"{Label}/{column}";

	private void AddPeople(int count)
	{
		for (int index = people.Count; index < count; index++)
		{
			people.Add(new(string.Create(CultureInfo.InvariantCulture, $"Person {index}"), index % 90, index % 2 == 0));
		}
	}

	private void DoubleClick(string name)
	{
		Vector2 center = CenterOf(name);
		Harness.Mouse.Click(center.X, center.Y);
		Harness.Mouse.Click(center.X, center.Y);
		Step();
	}

	private void WithCtrlHeld(System.Action action)
	{
		HarnessKeyboard.KeyDown(ImGuiKey.ModCtrl);
		action();
		HarnessKeyboard.KeyUp(ImGuiKey.ModCtrl);
		Step();
	}

	[TestMethod]
	public void DataTable_MarksTheTableAndItsHeaders()
	{
		Start(Draw);

		Assert.IsTrue(IsVisible(Label), "The table was not marked.");
		Assert.IsTrue(IsVisible(Header("Age")), "The Age header was not marked.");
	}

	[TestMethod]
	public void DataTable_MarksCellsBySourceIndex()
	{
		Start(Draw);

		Assert.IsTrue(IsVisible(Cell(2, "Name")), "Carol's name cell was not marked.");
	}

	[TestMethod]
	public void DataTable_DrawsOnlyTheRowsOnScreen()
	{
		AddPeople(10_000);
		Start(Draw);

		Assert.IsTrue(IsVisible(Cell(0, "Name")), "The first row was not drawn.");
		Assert.IsFalse(IsVisible(Cell(9_000, "Name")), "A row far off screen was drawn, so the table is not virtualizing.");
	}

	[TestMethod]
	public void DataTable_StartsInSourceOrder()
	{
		Start(Draw);

		Assert.IsLessThan(RectOf(Cell(1, "Name")).MinY, RectOf(Cell(0, "Name")).MinY, "The table sorted itself before anyone asked.");
	}

	[TestMethod]
	public void ClickingACell_MakesItActiveAndSelectsItsRow()
	{
		Start(Draw);

		Click(Cell(1, "Age"));

		Assert.AreEqual(new ImGuiWidgets.DataTableCell(1, 1), state.ActiveCell);
		Assert.AreSequenceEqual(new[] { 1 }, state.SelectedRows.Order());
	}

	[TestMethod]
	public void CtrlClickingACell_AddsItsRow()
	{
		Start(Draw);
		Click(Cell(0, "Name"));

		WithCtrlHeld(() => Click(Cell(2, "Name")));

		Assert.AreSequenceEqual(new[] { 0, 2 }, state.SelectedRows.Order());
	}

	[TestMethod]
	public void ClickingAHeader_SortsTheRows()
	{
		Start(Draw);

		Click(Header("Age"));

		// Ascending by age puts Bob, who is 25, above Alice, who is 30.
		Assert.IsLessThan(RectOf(Cell(0, "Name")).MinY, RectOf(Cell(1, "Name")).MinY, "Clicking Age did not sort by age.");
	}

	[TestMethod]
	public void ClickingAHeaderTwice_ReversesTheSort()
	{
		Start(Draw);

		Click(Header("Age"));
		Click(Header("Age"));

		// Descending puts Carol, who is 35, above Alice, who is 30.
		Assert.IsLessThan(RectOf(Cell(0, "Name")).MinY, RectOf(Cell(2, "Name")).MinY, "A second click did not reverse the sort.");
	}

	[TestMethod]
	public void DoubleClickingAnEditableCell_BeginsEditing()
	{
		Start(Draw);

		DoubleClick(Cell(1, "Name"));

		Assert.IsTrue(state.IsEditing, "A double-click did not begin editing.");
	}
}
```

`Assert.IsLessThan(upperBound, value)` asserts `value < upperBound`. Every assertion above reads "the second cell's top is above the first cell's top".

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet build tests/ImGui.Widgets.UITests/ImGui.Widgets.UITests.csproj`
Expected: FAIL to compile, `DataTable` and `DataTableOptions` do not exist.

- [ ] **Step 3: Extract the shared clipping helper**

Create `ImGui.Widgets/TableClipping.cs`:

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System.Globalization;

using Hexa.NET.ImGui;

public static partial class ImGuiWidgets
{
	/// <summary>
	/// The clipper setup and row naming both virtualized tables share, so their rows scroll and are
	/// addressed the same way.
	/// </summary>
	internal static class TableClipping
	{
		/// <summary>Begins a clipper over a table's rows.</summary>
		/// <param name="clipper">The clipper to begin.</param>
		/// <param name="rowCount">How many rows there are in all.</param>
		/// <param name="rowHeight">The height of every row, or zero to let the clipper measure the first.</param>
		/// <param name="forcedRow">A row to draw whether or not it's visible, or -1 for none.</param>
		internal static void Begin(ref ImGuiListClipper clipper, int rowCount, float rowHeight, int forcedRow)
		{
			// An explicit height lets the clipper skip its measuring pass. Left to ImGui when there isn't
			// one, rather than guessed at from the font: a guess that disagreed with the real row pitch
			// would put every skipped row at the wrong offset.
			if (rowHeight > 0f)
			{
				clipper.Begin(rowCount, rowHeight);
			}
			else
			{
				clipper.Begin(rowCount);
			}

			// A row far outside the visible range isn't drawn, and a scroll anchored on a row can only
			// happen if the row is drawn. This is what makes a scroll land on the first frame.
			if (forcedRow >= 0)
			{
				clipper.IncludeItemByIndex(forcedRow);
			}
		}

		/// <summary>
		/// Builds a row's probe name, in the <c>Tags/[0]</c> shape the property grid's lists already use,
		/// so a bracketed index means the same thing across the library.
		/// </summary>
		internal static string RowName(string label, int row) =>
			string.Create(CultureInfo.InvariantCulture, $"{label}/[{row}]");
	}
}
```

In `ImGui.Widgets/VirtualTable.cs`, replace the clipper setup in `DrawRows`, from `ImGuiListClipper clipper = default;` through the closing brace of the `if (forcedRow >= 0)` block, with:

```csharp
			ImGuiListClipper clipper = default;
			TableClipping.Begin(ref clipper, rowCount, options.RowHeight, forcedRow);
```

and replace `RowName`'s body with a call to the shared one:

```csharp
		internal static string RowName(string label, int row) => TableClipping.RowName(label, row);
```

Keep its doc comment. Remove `using System.Globalization;` from `VirtualTable.cs` only if nothing else there uses it after this change (`RowId` still does, so it stays).

- [ ] **Step 4: Create the renderer**

Create `ImGui.Widgets/DataTable.cs`:

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Probes;
using ktsu.Keybinding.Core.Contracts;

public static partial class ImGuiWidgets
{
	private static readonly DataTableOptions DefaultDataTableOptions = new();

	/// <summary>Options for <see cref="DataTable{TRow}"/>.</summary>
	public sealed class DataTableOptions
	{
		/// <summary>
		/// Gets the table flags. Defaults to banded, bordered, resizable columns a person can hide.
		/// </summary>
		/// <remarks>
		/// <see cref="ImGuiTableFlags.ScrollX"/> and <see cref="ImGuiTableFlags.ScrollY"/> are always added,
		/// because the rows are virtualized and the header and filter rows are frozen. Sorting flags are
		/// added when any column can sort.
		/// </remarks>
		public ImGuiTableFlags Flags { get; init; } =
			ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.Resizable | ImGuiTableFlags.Hideable;

		/// <summary>Gets the size of the table. Either axis may be zero to take the space available.</summary>
		public Vector2 OuterSize { get; init; }

		/// <summary>
		/// Gets the height of every row, or zero for one frame height, which fits an editor. Every row is
		/// this tall, for the same reason as <see cref="VirtualTableOptions.RowHeight"/>.
		/// </summary>
		public float RowHeight { get; init; }

		/// <summary>Gets a value indicating whether a row of filter boxes sits under the headers. Defaults to <see langword="true"/>.</summary>
		public bool HasFilterRow { get; init; } = true;

		/// <summary>Gets how many leading columns stay in place while the rest scroll sideways.</summary>
		public int FrozenColumns { get; init; }

		/// <summary>
		/// Gets the keymap the table reads its commands' chords from, or <see langword="null"/> for the
		/// defaults. Register the commands with <see cref="DataTableCommands.Register"/>.
		/// </summary>
		public IKeybindingService? Keybindings { get; init; }

		/// <summary>
		/// Gets the callback that draws the context menu's items, or <see langword="null"/> for no menu.
		/// Called every frame the menu is open, inside the popup, so it draws <c>MenuItem</c>s directly.
		/// </summary>
		public Action<DataTableContextMenu>? OnContextMenu { get; init; }
	}

	/// <summary>
	/// Draws a table of typed rows that sorts and filters itself, moves an active cell with the
	/// keyboard, and edits one cell at a time, reporting each edit through its column's
	/// <c>OnEdit</c> rather than writing it.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Only the rows on screen are drawn, so tens of thousands cost the same per frame as a screenful.
	/// Every row is the same height.
	/// </para>
	/// <para>
	/// Rows are addressed by their index in <paramref name="rows"/>, never by where they're shown, so
	/// selection and edits mean the same before and after a sort. Each cell is marked for tests as
	/// <c>&lt;label&gt;/[&lt;sourceIndex&gt;]/&lt;column&gt;</c>, each header as
	/// <c>&lt;label&gt;/&lt;column&gt;</c>, and each filter box as <c>&lt;label&gt;/filter/&lt;column&gt;</c>.
	/// A row must be on screen to be addressed, and <see cref="DataTableState{TRow}.ScrollToRow"/> is how
	/// a test puts it there.
	/// </para>
	/// <para>
	/// A host that turns on ImGui's keyboard navigation will find it competing with the table for the
	/// arrow keys. The table is designed for navigation to be off, which is ImGui's default.
	/// </para>
	/// </remarks>
	/// <typeparam name="TRow">The type of one row.</typeparam>
	/// <param name="label">A unique label for the table's ImGui id and probe name. Not drawn.</param>
	/// <param name="rows">The rows. Read every frame and never written.</param>
	/// <param name="state">The table's state, kept by the caller between frames.</param>
	/// <param name="options">Sizing, filtering, keymap and context menu. Defaults are used when null.</param>
	/// <exception cref="ArgumentNullException"><paramref name="label"/>, <paramref name="rows"/> or <paramref name="state"/> is <see langword="null"/>.</exception>
	public static void DataTable<TRow>(string label, IReadOnlyList<TRow> rows, DataTableState<TRow> state, DataTableOptions? options = null)
	{
		Ensure.NotNull(label);
		Ensure.NotNull(rows);
		Ensure.NotNull(state);

		DataTableImpl<TRow>.Draw(label, rows, state, options ?? DefaultDataTableOptions);
	}

	internal static partial class DataTableImpl<TRow>
	{
		public static void Draw(string label, IReadOnlyList<TRow> rows, DataTableState<TRow> state, DataTableOptions options)
		{
			state.Sync(rows);
			IReadOnlyList<DataTableColumn<TRow>> columns = state.Columns;

			ImGuiTableFlags flags = options.Flags | ImGuiTableFlags.ScrollX | ImGuiTableFlags.ScrollY;
			if (columns.Any(column => column.CanSort))
			{
				// Tristate so the table starts in the caller's order. Without it ImGui sorts on the first
				// column as soon as the table appears.
				flags |= ImGuiTableFlags.Sortable | ImGuiTableFlags.SortTristate;
			}

			// Captured before the table so the mark covers the whole widget, as VirtualTable does.
			Vector2 origin = ImGui.GetCursorScreenPos();

			if (!ImGui.BeginTable(label, columns.Count, flags, options.OuterSize))
			{
				return;
			}

			try
			{
				float rowHeight = options.RowHeight > 0f ? options.RowHeight : ImGui.GetFrameHeight();
				int frozenRows = options.HasFilterRow ? 2 : 1;

				ImGui.TableSetupScrollFreeze(Math.Clamp(options.FrozenColumns, 0, columns.Count), frozenRows);
				SetupColumns(columns);
				DrawHeaders(label, columns);
				ReadSort(state);
				ReadColumnVisibility(state);
				DrawRows(label, state, rowHeight);
				state.PageSize = (int)(ImGui.GetWindowHeight() / rowHeight) - frozenRows;
			}
			finally
			{
				ImGui.EndTable();
			}

			ImGuiProbes.MarkRegion(label, origin, ImGui.GetItemRectMax());
		}

		private static void SetupColumns(IReadOnlyList<DataTableColumn<TRow>> columns)
		{
			foreach (DataTableColumn<TRow> column in columns)
			{
				ImGuiTableColumnFlags flags = column.Flags;
				if (!column.CanSort)
				{
					flags |= ImGuiTableColumnFlags.NoSort;
				}

				ImGui.TableSetupColumn(column.Label, flags, column.Width);
			}
		}

		/// <summary>Submits the headers one at a time, so each can be marked for tests, as VirtualTable does.</summary>
		private static void DrawHeaders(string label, IReadOnlyList<DataTableColumn<TRow>> columns)
		{
			ImGui.TableNextRow(ImGuiTableRowFlags.Headers);

			for (int column = 0; column < columns.Count; column++)
			{
				if (!ImGui.TableSetColumnIndex(column))
				{
					continue;
				}

				ImGui.TableHeader(columns[column].Label);
				ImGuiProbes.MarkItem(label, columns[column].Label);
			}
		}

		private static void ReadSort(DataTableState<TRow> state)
		{
			ImGuiTableSortSpecsPtr specs = ImGui.TableGetSortSpecs();
			if (specs.IsNull || !specs.SpecsDirty)
			{
				return;
			}

			if (specs.SpecsCount > 0)
			{
				ImGuiTableColumnSortSpecsPtr primary = specs.Specs;
				state.SetSort(primary.ColumnIndex, primary.SortDirection == ImGuiSortDirection.Ascending);
			}
			else
			{
				state.SetSort(-1, isAscending: true);
			}

			specs.SpecsDirty = false;
		}

		/// <summary>Tells the state which columns the person has hidden, so movement and copying skip them.</summary>
		private static void ReadColumnVisibility(DataTableState<TRow> state)
		{
			for (int column = 0; column < state.Columns.Count; column++)
			{
				bool isVisible = (ImGui.TableGetColumnFlags(column) & ImGuiTableColumnFlags.IsEnabled) != 0;
				state.SetColumnVisible(column, isVisible);
			}
		}

		private static void DrawRows(string label, DataTableState<TRow> state, float rowHeight)
		{
			IReadOnlyList<int> view = state.View;
			int scrollSource = state.TakeScrollRequest();

			ImGuiListClipper clipper = default;
			TableClipping.Begin(ref clipper, view.Count, rowHeight, state.ViewPositionOf(scrollSource));

			while (clipper.Step())
			{
				for (int position = clipper.DisplayStart; position < clipper.DisplayEnd; position++)
				{
					int source = view[position];
					DrawRow(label, state, source, rowHeight, source == scrollSource);
				}
			}

			clipper.End();
		}

		private static void DrawRow(string label, DataTableState<TRow> state, int source, float rowHeight, bool isScrollTarget)
		{
			ImGui.TableNextRow(ImGuiTableRowFlags.None, rowHeight);

			if (state.SelectedRows.Contains(source))
			{
				ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg1, ImGui.GetColorU32(ImGuiCol.Header));
			}

			// The column the scroll anchors on: the active cell's when it's in this row, otherwise the first.
			int scrollColumn = -1;
			if (isScrollTarget)
			{
				scrollColumn = state.ActiveCell is DataTableCell active && active.SourceIndex == source ? active.Column : 0;
			}

			ImGui.PushID(source);
			try
			{
				for (int column = 0; column < state.Columns.Count; column++)
				{
					bool isScrollCell = column == scrollColumn;

					// A cell scrolled sideways out of view reports itself hidden, but the scroll target is
					// drawn anyway, because a scroll can only anchor on something drawn.
					if (ImGui.TableSetColumnIndex(column) || isScrollCell)
					{
						DrawCell(label, state, new DataTableCell(source, column), rowHeight, isScrollCell);
					}
				}
			}
			finally
			{
				ImGui.PopID();
			}
		}

		private static void DrawCell(string label, DataTableState<TRow> state, DataTableCell cell, float rowHeight, bool isScrollTarget)
		{
			DataTableColumn<TRow> column = state.Columns[cell.Column];
			bool isActive = state.ActiveCell == cell;

			ImGui.PushID(cell.Column);
			try
			{
				Vector2 start = ImGui.GetCursorScreenPos();

				if (isActive)
				{
					ImGui.TableSetBgColor(ImGuiTableBgTarget.CellBg, ImGui.GetColorU32(ImGuiCol.HeaderActive));
				}

				// A selectable spanning the cell, drawn under the content: it's the hit target for clicks
				// and double-clicks, and the one item that knows the cell's true rectangle.
				bool isClicked = ImGui.Selectable(
					"##cell",
					false,
					ImGuiSelectableFlags.AllowOverlap | ImGuiSelectableFlags.AllowDoubleClick,
					new Vector2(0f, rowHeight));

				ImGuiProbes.MarkItem(TableClipping.RowName(label, cell.SourceIndex), column.Label);
				Vector2 end = ImGui.GetItemRectMax();

				if (isScrollTarget && !ImGui.IsItemVisible())
				{
					ImGui.SetScrollHereY(0.5f);
					ImGui.SetScrollHereX(0.5f);
				}

				HandleCellMouse(state, cell, isClicked);

				ImGui.SetCursorScreenPos(start);
				if (column.DrawContent(state.RowAt(cell.SourceIndex)))
				{
					state.Click(cell, isCtrlHeld: false, isShiftHeld: false);
					state.Toggle();
				}

				if (isActive)
				{
					ImGui.GetWindowDrawList().AddRect(start, end, ImGui.GetColorU32(ImGuiCol.SeparatorActive));
				}
			}
			finally
			{
				ImGui.PopID();
			}
		}

		private static void HandleCellMouse(DataTableState<TRow> state, DataTableCell cell, bool isClicked)
		{
			if (!isClicked)
			{
				return;
			}

			ImGuiIOPtr io = ImGui.GetIO();
			state.Click(cell, io.KeyCtrl, io.KeyShift);

			if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
			{
				state.BeginEdit();
			}
		}
	}
}
```

`MarkItem(prefix, label)` is the two-argument form `VirtualTable` uses for its headers. With a row name as the prefix it records `label/[source]/column`.

- [ ] **Step 5: Run the tests to verify they pass**

```bash
dotnet build ImGui.sln
./tests/ImGui.Widgets.UITests/bin/Debug/net10.0/ktsu.ImGui.Widgets.UITests --filter "FullyQualifiedName~DataTableTests|FullyQualifiedName~VirtualTableTests"
```

Expected: the new tests pass, `VirtualTableTests` still passes in full, 0 warnings.

- [ ] **Step 6: Commit**

```bash
git add ImGui.Widgets/TableClipping.cs ImGui.Widgets/VirtualTable.cs ImGui.Widgets/DataTable.cs tests/ImGui.Widgets.UITests/DataTablePerson.cs tests/ImGui.Widgets.UITests/DataTableTests.cs
git commit -m "[minor] Draw a data table with sortable headers and clickable cells"
```

---

## Task 8: The filter row

**Files:**
- Modify: `ImGui.Widgets/DataTable.cs` (`Draw`, and a new `DrawFilterRow`)
- Test: `tests/ImGui.Widgets.UITests/DataTableTests.cs`

**Interfaces:**
- Consumes: `SearchBox(ref SearchBoxOptions, ref string)` (existing), `GetFilter`, `GetFilterOptions`, `SetFilter` (Task 3).
- Produces: `private static void DrawFilterRow(string label, DataTableState<TRow> state)` on `DataTableImpl<TRow>`.

- [ ] **Step 1: Write the failing tests**

Add to `DataTableTests`, with the helper beside `Header`:

```csharp
	private static string Filter(string column) => $"{Label}/filter/{column}";

	[TestMethod]
	public void TypingInAFilter_NarrowsTheRows()
	{
		Start(Draw);

		Click(Filter("Name"));
		Harness.Keyboard.Type("ob");
		Step();

		Assert.IsTrue(IsVisible(Cell(1, "Name")), "Bob was filtered out by 'ob'.");
		Assert.IsFalse(IsVisible(Cell(0, "Name")), "Alice survived a filter of 'ob'.");
	}

	[TestMethod]
	public void WithoutAFilterRow_NoFilterBoxesAreDrawn()
	{
		options = new() { RowHeight = 22f, HasFilterRow = false };
		Start(Draw);

		Assert.IsFalse(IsVisible(Filter("Name")), "A filter box was drawn with the filter row turned off.");
	}
```

- [ ] **Step 2: Run the tests to verify they fail**

```bash
dotnet build tests/ImGui.Widgets.UITests/ImGui.Widgets.UITests.csproj
./tests/ImGui.Widgets.UITests/bin/Debug/net10.0/ktsu.ImGui.Widgets.UITests --filter "FullyQualifiedName~DataTableTests"
```

Expected: `TypingInAFilter_NarrowsTheRows` FAILS with "No item matching 'people/filter/Name' was ever marked".

- [ ] **Step 3: Draw the filter row**

In `DataTable.cs`, in `Draw`, insert after `ReadColumnVisibility(state);`:

```csharp
				if (options.HasFilterRow)
				{
					DrawFilterRow(label, state);
				}
```

Add to `DataTableImpl<TRow>`, after `ReadColumnVisibility`:

```csharp
		/// <summary>
		/// Draws a search box under each header. The search box's own right-click menu switches a column
		/// between fuzzy, glob and regex, and a change of mode is passed on as a change of filter.
		/// </summary>
		private static void DrawFilterRow(string label, DataTableState<TRow> state)
		{
			ImGui.TableNextRow();
			string prefix = $"{label}/filter";

			for (int column = 0; column < state.Columns.Count; column++)
			{
				if (!ImGui.TableSetColumnIndex(column))
				{
					continue;
				}

				ImGui.PushID(column);
				try
				{
					SearchBoxOptions filterOptions = state.GetFilterOptions(column);
					string text = state.GetFilter(column);

					bool isChanged = SearchBox(ref filterOptions, ref text);
					ImGuiProbes.MarkItem(prefix, state.Columns[column].Label);

					if (isChanged || filterOptions != state.GetFilterOptions(column))
					{
						state.SetFilter(column, text, filterOptions);
					}
				}
				finally
				{
					ImGui.PopID();
				}
			}
		}
```

`SearchBox` marks its input as `##filter` as well. The mark added here, right after it, is the one tests use. Nothing `SearchBox` draws after its input is an item unless its context menu is open, so the last item is still the input box.

- [ ] **Step 4: Run the tests to verify they pass**

Same commands as Step 2. Expected: PASS, 0 warnings.

- [ ] **Step 5: Commit**

```bash
git add ImGui.Widgets/DataTable.cs tests/ImGui.Widgets.UITests/DataTableTests.cs
git commit -m "[minor] Filter a data table's columns from a row under the headers"
```

---

## Task 9: Keyboard input and in-place editing

**Files:**
- Create: `ImGui.Widgets/DataTable.Input.cs`
- Modify: `ImGui.Widgets/DataTable.cs` (`Draw`, `DrawRows`, `DrawRow`, `DrawCell`)
- Test: `tests/ImGui.Widgets.UITests/DataTableTests.cs`

**Interfaces:**
- Consumes: `KeyChordMatcher.IsPressed` (Task 1), `DataTableCommands` (Task 6), `Move`, `SelectAll`, `BuildCopyText` (Task 4), `BeginEdit`, `CommitEdit`, `CancelEdit`, `Toggle`, `EditSession` (Task 5).
- Produces:
  - `internal readonly record struct CellRect(Vector2 Min, Vector2 Max)` nested in `ImGuiWidgets`
  - `internal static class DataTableInput` with `Handle<TRow>(DataTableState<TRow> state, DataTableOptions options, CellRect? editorRect)`
  - `DrawRows` now returns `CellRect?`, the rectangle of the editor drawn this frame

- [ ] **Step 1: Write the failing tests**

Add to `DataTableTests`:

```csharp
	private void BeginEditing(int sourceIndex, string column)
	{
		Click(Cell(sourceIndex, column));
		Harness.Keyboard.Press(ImGuiKey.F2);
		Step(2);
	}

	[TestMethod]
	public void ArrowDown_MovesTheActiveCell()
	{
		Start(Draw);
		Click(Cell(0, "Name"));

		Harness.Keyboard.Press(ImGuiKey.DownArrow);

		Assert.AreEqual(new ImGuiWidgets.DataTableCell(1, 0), state.ActiveCell);
	}

	[TestMethod]
	public void ShiftArrowDown_ExtendsTheSelection()
	{
		Start(Draw);
		Click(Cell(0, "Name"));

		Harness.Keyboard.Press(ImGuiKey.DownArrow, shift: true);

		Assert.AreSequenceEqual(new[] { 0, 1 }, state.SelectedRows.Order());
	}

	[TestMethod]
	public void CtrlEnd_ScrollsTheLastRowIntoView()
	{
		AddPeople(1_000);
		Start(Draw);
		Click(Cell(0, "Name"));

		Harness.Keyboard.Press(ImGuiKey.End, ctrl: true);
		Step();

		Assert.IsTrue(IsVisible(Cell(999, "Name")), "Ctrl+End did not bring the last row into view.");
	}

	[TestMethod]
	public void F2ThenTypingThenEnter_ReportsTheEditAndMovesDown()
	{
		Start(Draw);
		BeginEditing(1, "Name");

		Harness.Keyboard.Press(ImGuiKey.A, ctrl: true);
		Harness.Keyboard.Type("Robert");
		Harness.Keyboard.Press(ImGuiKey.Enter);
		Step();

		Assert.AreEqual(new ImGuiWidgets.DataTableEdit<DataTablePerson, string>(1, people[1], "Bob", "Robert"), nameEdits.Single());
		Assert.AreEqual(new ImGuiWidgets.DataTableCell(2, 0), state.ActiveCell);
	}

	[TestMethod]
	public void Escape_CancelsTheEdit()
	{
		Start(Draw);
		BeginEditing(1, "Name");

		Harness.Keyboard.Type("zzz");
		Harness.Keyboard.Press(ImGuiKey.Escape);
		Step();

		Assert.IsFalse(state.IsEditing);
		Assert.IsEmpty(nameEdits);
	}

	[TestMethod]
	public void ClickingAway_CommitsTheEdit()
	{
		Start(Draw);
		BeginEditing(1, "Name");
		Harness.Keyboard.Press(ImGuiKey.A, ctrl: true);
		Harness.Keyboard.Type("Robert");

		// Below the table, inside the window, where nothing else is drawn.
		Harness.Mouse.Click(RectOf(Label).MinX + 20f, RectOf(Label).MaxY + 20f);
		Step();

		Assert.IsFalse(state.IsEditing);
		Assert.AreEqual("Robert", nameEdits.Single().NewValue);
	}

	/// <summary>
	/// Review focus: ImGui selects all of an input's text when it takes the keyboard, so without care the
	/// second typed character replaces the first.
	/// </summary>
	[TestMethod]
	public void TypingACharacter_BeginsAnEditThatKeepsTyping()
	{
		Start(Draw);
		Click(Cell(1, "Name"));

		Harness.Keyboard.Type("Q");
		Step(3);
		Harness.Keyboard.Type("ux");
		Harness.Keyboard.Press(ImGuiKey.Enter);
		Step();

		Assert.AreEqual("Qux", nameEdits.Single().NewValue);
	}

	/// <summary>
	/// Review focus: Tab must move the active cell and leave the keyboard with the table, rather than
	/// ImGui's own tabbing handing it to another item.
	/// </summary>
	[TestMethod]
	public void Tab_WhileEditing_MovesRightAndKeepsTheKeyboard()
	{
		Start(Draw);
		BeginEditing(1, "Name");

		Harness.Keyboard.Press(ImGuiKey.Tab);
		Step();

		Assert.IsFalse(state.IsEditing);
		Assert.AreEqual(new ImGuiWidgets.DataTableCell(1, 1), state.ActiveCell);

		Harness.Keyboard.Press(ImGuiKey.DownArrow);

		Assert.AreEqual(new ImGuiWidgets.DataTableCell(2, 1), state.ActiveCell, "After Tab, the arrow keys no longer reached the table.");
	}

	/// <summary>
	/// Review focus: keys typed into a filter box belong to the filter, not to the active cell.
	/// </summary>
	[TestMethod]
	public void KeysTypedIntoAFilter_DoNotMoveOrEditTheActiveCell()
	{
		Start(Draw);
		Click(Cell(0, "Name"));
		Click(Filter("Age"));

		Harness.Keyboard.Press(ImGuiKey.DownArrow);
		Harness.Keyboard.Type("3");
		Step();

		Assert.AreEqual(new ImGuiWidgets.DataTableCell(0, 0), state.ActiveCell);
		Assert.IsFalse(state.IsEditing, "Typing into a filter began editing the active cell.");
	}

	[TestMethod]
	public void Space_TogglesABoolCell()
	{
		Start(Draw);

		// Clicked near the right edge, clear of the checkbox, so the click only activates the cell.
		ClickFraction(Cell(1, "Active"), 0.9f);
		Harness.Keyboard.Press(ImGuiKey.Space);

		Assert.AreEqual(new ImGuiWidgets.DataTableEdit<DataTablePerson, bool>(1, people[1], false, true), activeEdits.Single());
	}

	[TestMethod]
	public void ClickingTheCheckbox_TogglesTheCell()
	{
		Start(Draw);

		ClickWithin(Cell(2, "Active"), 8f, RectOf(Cell(2, "Active")).Height / 2f);

		Assert.AreEqual(new ImGuiWidgets.DataTableEdit<DataTablePerson, bool>(2, people[2], true, false), activeEdits.Single());
	}

	[TestMethod]
	public void CtrlC_CopiesTheSelectedRows()
	{
		Start(Draw);
		Click(Cell(1, "Name"));

		Harness.Keyboard.Press(ImGuiKey.C, ctrl: true);

		Assert.AreEqual("Bob\t25\tFalse", ImGui.GetClipboardText());
	}
```

If the harness has no clipboard backend and `CtrlC_CopiesTheSelectedRows` reads an empty string, check how `ImGuiApp` installs clipboard functions headless and report it rather than weakening the test. `BuildCopyText` is already covered by the unit tests.

- [ ] **Step 2: Run the tests to verify they fail**

```bash
dotnet build tests/ImGui.Widgets.UITests/ImGui.Widgets.UITests.csproj
./tests/ImGui.Widgets.UITests/bin/Debug/net10.0/ktsu.ImGui.Widgets.UITests --filter "FullyQualifiedName~DataTableTests"
```

Expected: the new keyboard tests FAIL, for example `ArrowDown_MovesTheActiveCell` with the active cell still `(0, 0)`.

- [ ] **Step 3: Create the input handler**

Create `ImGui.Widgets/DataTable.Input.cs`:

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.Keybinding.Core.Contracts;
using ktsu.Keybinding.Core.Models;

public static partial class ImGuiWidgets
{
	/// <summary>A cell's rectangle on screen, in display pixels.</summary>
	internal readonly record struct CellRect(Vector2 Min, Vector2 Max);

	/// <summary>Turns the keyboard and clicks outside the editor into data table commands.</summary>
	internal static class DataTableInput
	{
		// Movements that Shift extends into a selection, which is why they're matched with Shift
		// ignored. Tab and Shift+Tab are separate commands and are not in this list.
		private static readonly (string Command, DataTableMove Move)[] ExtendableMoves =
		[
			(DataTableCommands.MoveUp, DataTableMove.Up),
			(DataTableCommands.MoveDown, DataTableMove.Down),
			(DataTableCommands.MoveLeft, DataTableMove.Left),
			(DataTableCommands.MoveRight, DataTableMove.Right),
			(DataTableCommands.PageUp, DataTableMove.PageUp),
			(DataTableCommands.PageDown, DataTableMove.PageDown),
			(DataTableCommands.Home, DataTableMove.Home),
			(DataTableCommands.End, DataTableMove.End),
			(DataTableCommands.First, DataTableMove.First),
			(DataTableCommands.Last, DataTableMove.Last),
		];

		/// <summary>Handles this frame's input. Called inside the table, after the rows are drawn.</summary>
		/// <param name="state">The table's state.</param>
		/// <param name="options">The table's options, for the keymap.</param>
		/// <param name="editorRect">Where the editor was drawn this frame, or <see langword="null"/> if it wasn't.</param>
		internal static void Handle<TRow>(DataTableState<TRow> state, DataTableOptions options, CellRect? editorRect)
		{
			// Checked before focus, because a click outside the table is exactly the case that takes focus
			// away from it.
			if (state.IsEditing && editorRect is CellRect rect && IsClickedAway(rect))
			{
				state.CommitEdit();
			}

			if (!ImGui.IsWindowFocused(ImGuiFocusedFlags.ChildWindows))
			{
				return;
			}

			if (state.IsEditing)
			{
				HandleEditingKeys(state, options);
				return;
			}

			// A filter box has the keyboard. Its keys are its own.
			if (ImGui.GetIO().WantTextInput)
			{
				return;
			}

			HandleIdleKeys(state, options);
		}

		private static void HandleEditingKeys<TRow>(DataTableState<TRow> state, DataTableOptions options)
		{
			if (IsPressed(options, DataTableCommands.Cancel))
			{
				state.CancelEdit();
			}
			else if (IsPressed(options, DataTableCommands.Commit))
			{
				state.CommitEdit();
				state.Move(DataTableMove.Down);
			}
			else if (IsPressed(options, DataTableCommands.Next))
			{
				state.Move(DataTableMove.Next);
			}
			else if (IsPressed(options, DataTableCommands.Previous))
			{
				state.Move(DataTableMove.Previous);
			}
		}

		private static void HandleIdleKeys<TRow>(DataTableState<TRow> state, DataTableOptions options)
		{
			bool isShiftHeld = ImGui.GetIO().KeyShift;

			foreach ((string command, DataTableMove move) in ExtendableMoves)
			{
				if (IsPressed(options, command, canRepeat: true, isShiftIgnored: true))
				{
					state.Move(move, isExtending: isShiftHeld);
					return;
				}
			}

			if (IsPressed(options, DataTableCommands.Next, canRepeat: true))
			{
				state.Move(DataTableMove.Next);
			}
			else if (IsPressed(options, DataTableCommands.Previous, canRepeat: true))
			{
				state.Move(DataTableMove.Previous);
			}
			else if (IsPressed(options, DataTableCommands.BeginEdit) || IsEnterWithoutKeymap(options))
			{
				state.BeginEdit();
			}
			else if (IsPressed(options, DataTableCommands.Toggle))
			{
				state.Toggle();
			}
			else if (IsPressed(options, DataTableCommands.Copy))
			{
				string text = state.BuildCopyText();
				if (text.Length > 0)
				{
					ImGui.SetClipboardText(text);
				}
			}
			else if (IsPressed(options, DataTableCommands.SelectAll))
			{
				state.SelectAll();
			}
			else if (TryTakeTypedCharacter(out char typed))
			{
				state.BeginEdit(typed);
			}
		}

		private static bool IsPressed(DataTableOptions options, string command, bool canRepeat = false, bool isShiftIgnored = false)
		{
			Chord? chord = options.Keybindings is IKeybindingService keybindings
				? keybindings.GetChord(command)
				: DataTableCommands.DefaultChordOf(command);

			return chord is not null && KeyChordMatcher.IsPressed(chord, canRepeat, isShiftIgnored);
		}

		// Enter begins editing as well as F2 when there's no keymap. A profile holds one chord per
		// command, so with a keymap only the bound chord counts.
		private static bool IsEnterWithoutKeymap(DataTableOptions options) =>
			options.Keybindings is null && ImGui.IsKeyPressed(ImGuiKey.Enter, false);

		/// <summary>Takes the first printable character typed this frame, which begins an edit with it.</summary>
		private static bool TryTakeTypedCharacter(out char typed)
		{
			typed = default;
			ImGuiIOPtr io = ImGui.GetIO();

			// Space is the toggle chord, and a character typed with Ctrl or Alt held is a shortcut.
			if (io.KeyCtrl || io.KeyAlt || io.KeySuper || io.InputQueueCharacters.Size == 0)
			{
				return false;
			}

			typed = (char)io.InputQueueCharacters[0];
			return !char.IsControl(typed) && !char.IsWhiteSpace(typed);
		}

		private static bool IsClickedAway(CellRect rect) =>
			ImGui.IsMouseClicked(ImGuiMouseButton.Left)
			&& !ImGui.IsMouseHoveringRect(rect.Min, rect.Max, false)
			&& !ImGui.IsPopupOpen(string.Empty, ImGuiPopupFlags.AnyPopupId | ImGuiPopupFlags.AnyPopupLevel);
	}
}
```

**Binding checks.** Confirm each of these with Go To Definition and adapt the call if the binding differs. Keep the behavior the tests pin.
- `io.InputQueueCharacters` has `Size` and an indexer. If it exposes `Data` instead, read `io.InputQueueCharacters.Data[0]` in an `unsafe` block with the same S6640 suppression `DataTableEditors` uses.
- `ImGui.IsMouseHoveringRect(Vector2, Vector2, bool clip)` exists. The `false` stops the table's current clip rectangle from hiding the editor.
- `ImGui.IsPopupOpen(string, ImGuiPopupFlags)` exists. It keeps a click on an open enum combo's list from counting as a click away.
- `ImGui.PushItemFlag(ImGuiItemFlags, bool)`, `ImGui.PopItemFlag()` and `ImGuiItemFlags.NoTabStop` exist (used in Step 4).

- [ ] **Step 4: Host the editor and handle input in the renderer**

In `ImGui.Widgets/DataTable.cs`, replace the whole `try` block in `Draw`, and the `finally` block after it, with:

```csharp
			// No tab stops inside the table. Tab is the table's own command, and if ImGui's tabbing could
			// move the keyboard, Tab out of an editor would land in a filter box.
			ImGui.PushItemFlag(ImGuiItemFlags.NoTabStop, true);

			try
			{
				float rowHeight = options.RowHeight > 0f ? options.RowHeight : ImGui.GetFrameHeight();
				int frozenRows = options.HasFilterRow ? 2 : 1;

				ImGui.TableSetupScrollFreeze(Math.Clamp(options.FrozenColumns, 0, columns.Count), frozenRows);
				SetupColumns(columns);
				DrawHeaders(label, columns);
				ReadSort(state);
				ReadColumnVisibility(state);

				if (options.HasFilterRow)
				{
					DrawFilterRow(label, state);
				}

				CellRect? editorRect = DrawRows(label, state, rowHeight);
				state.PageSize = (int)(ImGui.GetWindowHeight() / rowHeight) - frozenRows;
				DataTableInput.Handle(state, options, editorRect);
			}
			finally
			{
				ImGui.PopItemFlag();
				ImGui.EndTable();
			}
```

Replace `DrawRows`, `DrawRow` and `DrawCell` with these versions, which thread the editor's rectangle out and draw the editor in its cell:

```csharp
		private static CellRect? DrawRows(string label, DataTableState<TRow> state, float rowHeight)
		{
			IReadOnlyList<int> view = state.View;
			int scrollSource = state.TakeScrollRequest();
			CellRect? editorRect = null;

			ImGuiListClipper clipper = default;
			TableClipping.Begin(ref clipper, view.Count, rowHeight, state.ViewPositionOf(scrollSource));

			while (clipper.Step())
			{
				for (int position = clipper.DisplayStart; position < clipper.DisplayEnd; position++)
				{
					int source = view[position];
					DrawRow(label, state, source, rowHeight, source == scrollSource, ref editorRect);
				}
			}

			clipper.End();
			return editorRect;
		}

		private static void DrawRow(string label, DataTableState<TRow> state, int source, float rowHeight, bool isScrollTarget, ref CellRect? editorRect)
		{
			ImGui.TableNextRow(ImGuiTableRowFlags.None, rowHeight);

			if (state.SelectedRows.Contains(source))
			{
				ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg1, ImGui.GetColorU32(ImGuiCol.Header));
			}

			// The column the scroll anchors on: the active cell's when it's in this row, otherwise the first.
			int scrollColumn = -1;
			if (isScrollTarget)
			{
				scrollColumn = state.ActiveCell is DataTableCell active && active.SourceIndex == source ? active.Column : 0;
			}

			ImGui.PushID(source);
			try
			{
				for (int column = 0; column < state.Columns.Count; column++)
				{
					bool isScrollCell = column == scrollColumn;

					// A cell scrolled sideways out of view reports itself hidden, but the scroll target is
					// drawn anyway, because a scroll can only anchor on something drawn.
					if (ImGui.TableSetColumnIndex(column) || isScrollCell)
					{
						DrawCell(label, state, new DataTableCell(source, column), rowHeight, isScrollCell, ref editorRect);
					}
				}
			}
			finally
			{
				ImGui.PopID();
			}
		}

		private static void DrawCell(string label, DataTableState<TRow> state, DataTableCell cell, float rowHeight, bool isScrollTarget, ref CellRect? editorRect)
		{
			DataTableColumn<TRow> column = state.Columns[cell.Column];
			bool isActive = state.ActiveCell == cell;

			ImGui.PushID(cell.Column);
			try
			{
				Vector2 start = ImGui.GetCursorScreenPos();

				if (isActive)
				{
					ImGui.TableSetBgColor(ImGuiTableBgTarget.CellBg, ImGui.GetColorU32(ImGuiCol.HeaderActive));
				}

				// A selectable spanning the cell, drawn under the content: it's the hit target for clicks
				// and double-clicks, and the one item that knows the cell's true rectangle.
				bool isClicked = ImGui.Selectable(
					"##cell",
					false,
					ImGuiSelectableFlags.AllowOverlap | ImGuiSelectableFlags.AllowDoubleClick,
					new Vector2(0f, rowHeight));

				ImGuiProbes.MarkItem(TableClipping.RowName(label, cell.SourceIndex), column.Label);
				Vector2 end = ImGui.GetItemRectMax();

				if (isScrollTarget && !ImGui.IsItemVisible())
				{
					ImGui.SetScrollHereY(0.5f);
					ImGui.SetScrollHereX(0.5f);
				}

				HandleCellMouse(state, cell, isClicked);

				ImGui.SetCursorScreenPos(start);

				// Read after the mouse, so a double-click shows its editor on the next frame, and a click on
				// another cell that committed this one draws it as text again straight away.
				DataTableEditSession? session = state.EditSession;
				if (session is not null && session.SourceIndex == cell.SourceIndex && session.Column == cell.Column)
				{
					if (session.FramesDrawn == 0)
					{
						ImGui.SetKeyboardFocusHere();
					}

					session.Draw("##editor");
					editorRect = new CellRect(start, end);
				}
				else if (column.DrawContent(state.RowAt(cell.SourceIndex)))
				{
					state.Click(cell, isCtrlHeld: false, isShiftHeld: false);
					state.Toggle();
				}

				if (isActive)
				{
					ImGui.GetWindowDrawList().AddRect(start, end, ImGui.GetColorU32(ImGuiCol.SeparatorActive));
				}
			}
			finally
			{
				ImGui.PopID();
			}
		}
```

- [ ] **Step 5: Run the tests to verify they pass**

```bash
dotnet build ImGui.sln
./tests/ImGui.Widgets.UITests/bin/Debug/net10.0/ktsu.ImGui.Widgets.UITests --filter "FullyQualifiedName~DataTableTests"
```

Expected: PASS, 0 warnings. If `TypingACharacter_BeginsAnEditThatKeepsTyping` produces `ux`, the caret callback isn't taking effect: check the binding notes in Task 2 Step 7. If `Tab_WhileEditing_MovesRightAndKeepsTheKeyboard` fails on the arrow key, ImGui is still tabbing: confirm `NoTabStop` is pushed before any item in the table, including the filter boxes.

- [ ] **Step 6: Commit**

```bash
git add ImGui.Widgets/DataTable.Input.cs ImGui.Widgets/DataTable.cs tests/ImGui.Widgets.UITests/DataTableTests.cs
git commit -m "[minor] Drive a data table from the keyboard and edit cells in place"
```

---

## Task 10: The context menu

**Files:**
- Modify: `ImGui.Widgets/DataTable.Input.cs` (a new `DataTableContextMenus` class)
- Modify: `ImGui.Widgets/DataTable.cs` (`Draw`, `HandleCellMouse`)
- Test: `tests/ImGui.Widgets.UITests/DataTableTests.cs`

**Interfaces:**
- Consumes: `OpenContextMenu`, `TakeContextMenuRequest`, `ContextMenu` (Task 5), `DataTableOptions.OnContextMenu` (Task 7).
- Produces: `internal static class DataTableContextMenus` with `Draw<TRow>(DataTableState<TRow> state, DataTableOptions options)`.

- [ ] **Step 1: Write the failing tests**

Add to `DataTableTests`:

```csharp
	private void RightClick(string name)
	{
		Vector2 center = CenterOf(name);
		Harness.Mouse.Click(center.X, center.Y, button: 1);
		Step();
	}

	[TestMethod]
	public void RightClickingACell_OpensTheContextMenuForIt()
	{
		Start(Draw);

		RightClick(Cell(2, "Age"));

		Assert.IsNotEmpty(menus, "The context menu callback never ran.");
		Assert.AreEqual(new ImGuiWidgets.DataTableCell(2, 1), menus[^1].Cell);
		Assert.AreSequenceEqual(new[] { 2 }, menus[^1].SelectedRows.Order());
	}

	[TestMethod]
	public void RightClickingASelectedRow_KeepsTheSelection()
	{
		Start(Draw);
		Click(Cell(0, "Name"));
		WithCtrlHeld(() => Click(Cell(2, "Name")));

		RightClick(Cell(2, "Age"));

		Assert.AreSequenceEqual(new[] { 0, 2 }, menus[^1].SelectedRows.Order());
	}

	[TestMethod]
	public void RightClickingWithNoMenu_StillActivatesTheCell()
	{
		options = new() { RowHeight = 22f };
		Start(Draw);

		RightClick(Cell(3, "Name"));

		Assert.AreEqual(new ImGuiWidgets.DataTableCell(3, 0), state.ActiveCell);
	}
```

If `Assert.IsNotEmpty` doesn't exist in this MSTest version, use `Assert.AreNotEqual(0, menus.Count, ...)`.

- [ ] **Step 2: Run the tests to verify they fail**

```bash
dotnet build tests/ImGui.Widgets.UITests/ImGui.Widgets.UITests.csproj
./tests/ImGui.Widgets.UITests/bin/Debug/net10.0/ktsu.ImGui.Widgets.UITests --filter "FullyQualifiedName~DataTableTests"
```

Expected: the three new tests FAIL, because nothing reacts to a right-click yet.

- [ ] **Step 3: Open and draw the menu**

Add to `ImGui.Widgets/DataTable.Input.cs`, inside `ImGuiWidgets`, after `DataTableInput`:

```csharp
	/// <summary>Opens and draws a data table's context menu.</summary>
	internal static class DataTableContextMenus
	{
		private const string PopupId = "##dataTableContextMenu";

		/// <summary>
		/// Draws the menu. Called inside the table, so the popup is opened and drawn in the same id scope
		/// wherever in the table the right-click landed.
		/// </summary>
		internal static void Draw<TRow>(DataTableState<TRow> state, DataTableOptions options)
		{
			// Taken whether or not there's a menu, so a request never lingers into a later frame.
			if (state.TakeContextMenuRequest() && options.OnContextMenu is not null)
			{
				ImGui.OpenPopup(PopupId);
			}

			if (options.OnContextMenu is null || state.ContextMenu is not DataTableContextMenu menu)
			{
				return;
			}

			if (ImGui.BeginPopup(PopupId))
			{
				options.OnContextMenu(menu);
				ImGui.EndPopup();
			}
		}
	}
```

In `ImGui.Widgets/DataTable.cs`, in `Draw`, add after `DataTableInput.Handle(state, options, editorRect);`:

```csharp
				DataTableContextMenus.Draw(state, options);
```

and replace `HandleCellMouse` with:

```csharp
		private static void HandleCellMouse(DataTableState<TRow> state, DataTableCell cell, bool isClicked)
		{
			if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
			{
				state.OpenContextMenu(cell);
				return;
			}

			if (!isClicked)
			{
				return;
			}

			ImGuiIOPtr io = ImGui.GetIO();
			state.Click(cell, io.KeyCtrl, io.KeyShift);

			if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
			{
				state.BeginEdit();
			}
		}
```

`IsItemClicked` reads the last item, which is still the cell's selectable at this point, because `HandleCellMouse` runs straight after it and its mark.

- [ ] **Step 4: Run the tests to verify they pass**

Same commands as Step 2. Expected: PASS, 0 warnings.

- [ ] **Step 5: Commit**

```bash
git add ImGui.Widgets/DataTable.Input.cs ImGui.Widgets/DataTable.cs tests/ImGui.Widgets.UITests/DataTableTests.cs
git commit -m "[minor] Open a caller-drawn context menu on a data table cell"
```

---

## Task 11: Demo, documentation, and the demo UI suite

**Files:**
- Create: `examples/ImGuiWidgetsDemo/DataTableDemo.cs`
- Modify: `examples/ImGuiWidgetsDemo/ImGuiWidgetsDemo.csproj`
- Modify: `examples/ImGuiWidgetsDemo/ImGuiWidgetsDemo.cs:133` (`ResetState`) and `:478` (`ShowAdvancedDemos`)
- Modify: `tests/ImGuiWidgetsDemo.UITests/WidgetsDemoUITests.cs:50` (`AdvancedDemoSections`)
- Modify: `ImGui.Widgets/README.md`
- Modify: `CLAUDE.md`

**Interfaces:**
- Consumes: the whole public surface from Tasks 2 to 10. `IUndoRedoService`, `UndoRedoService`, `StackManager`, `SaveBoundaryManager`, `CommandMerger`, `UndoRedoOptions`, `DelegateCommand` from `ktsu.UndoRedo`. `DemoProbe.Header` and `DemoProbe.Button` (existing).
- Produces: `internal static class DataTableDemo` with `Show()`, `ResetState()`, `EditCount`.

- [ ] **Step 1: Reference the undo package**

In `examples/ImGuiWidgetsDemo/ImGuiWidgetsDemo.csproj`, add after `ktsu.TextFilter`:

```xml
    <PackageReference Include="ktsu.UndoRedo" />
```

- [ ] **Step 2: Create the demo page**

Create `examples/ImGuiWidgetsDemo/DataTableDemo.cs`:

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Examples.Widgets;

using System.Collections.Generic;
using System.Globalization;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Widgets;
using ktsu.UndoRedo;
using ktsu.UndoRedo.Contracts;
using ktsu.UndoRedo.Core.Services;
using ktsu.UndoRedo.Models;

/// <summary>
/// Demonstrates <see cref="ImGuiWidgets.DataTable{TRow}"/> over a parts list, with every column editable
/// and every edit recorded on an undo stack.
/// </summary>
/// <remarks>
/// In its own class rather than in <c>ImGuiWidgetsDemo</c>, which is already at the class-coupling
/// limit the analyzers enforce. The undo stack is the point: the table reports edits rather than
/// writing them, so the demo applies each one as a command it can take back.
/// </remarks>
internal static class DataTableDemo
{
	private const int PartCount = 2_000;

	private static readonly List<DemoPart> Parts = [];

	private static readonly ImGuiWidgets.DataTableOptions Options = new()
	{
		RowHeight = 22.0f,
		OuterSize = new Vector2(0.0f, 300.0f),
		FrozenColumns = 1,
		OnContextMenu = ShowContextMenu,
	};

	private static IUndoRedoService history = CreateHistory();
	private static ImGuiWidgets.DataTableState<DemoPart> state = CreateState();

	private enum PartCategory
	{
		Fastener,
		Bearing,
		Seal,
		Spring,
	}

	/// <summary>Gets how many edits the table has reported since the demo was reset.</summary>
	internal static int EditCount { get; private set; }

	/// <summary>Puts the demo back as it started, for the demo UI tests.</summary>
	internal static void ResetState()
	{
		Parts.Clear();
		history = CreateHistory();
		state = CreateState();
		EditCount = 0;
	}

	/// <summary>Draws the demo section.</summary>
	public static void Show()
	{
		if (!DemoProbe.Header("Data Table"))
		{
			return;
		}

		if (Parts.Count == 0)
		{
			Parts.AddRange(CreateParts());
		}

		ImGui.TextUnformatted(string.Create(
			CultureInfo.InvariantCulture,
			$"{Parts.Count:N0} parts, {state.SelectedRows.Count} selected, {EditCount} edits."));
		ImGui.TextDisabled("Double-click, F2 or start typing to edit. Enter commits, Escape cancels, Tab moves on.");

		ImGui.BeginDisabled(!history.CanUndo);
		if (DemoProbe.Button("Undo"))
		{
			history.Undo();
			state.Refresh();
		}

		ImGui.EndDisabled();
		ImGui.SameLine();

		ImGui.BeginDisabled(!history.CanRedo);
		if (DemoProbe.Button("Redo"))
		{
			history.Redo();
			state.Refresh();
		}

		ImGui.EndDisabled();

		ImGuiWidgets.DataTable("parts", Parts, state, Options);
	}

	private static IUndoRedoService CreateHistory() =>
		new UndoRedoService(new StackManager(), new SaveBoundaryManager(), new CommandMerger(), UndoRedoOptions.Create(maxStackSize: 200));

	private static ImGuiWidgets.DataTableState<DemoPart> CreateState() => new(
	[
		new ImGuiWidgets.DataTableColumn<DemoPart, string>
		{
			Label = "Name",
			Value = part => part.Name,
			Flags = ImGuiTableColumnFlags.WidthFixed,
			Width = 140.0f,
			OnEdit = edit => Apply("Rename part", edit, (part, value) => part.Name = value),
		},
		new ImGuiWidgets.DataTableColumn<DemoPart, int>
		{
			Label = "Quantity",
			Value = part => part.Quantity,
			OnEdit = edit => Apply("Change quantity", edit, (part, value) => part.Quantity = value),
		},
		new ImGuiWidgets.DataTableColumn<DemoPart, double>
		{
			Label = "Mass (kg)",
			Value = part => part.MassKilograms,
			Format = value => value.ToString("0.000", CultureInfo.InvariantCulture),
			OnEdit = edit => Apply("Change mass", edit, (part, value) => part.MassKilograms = value),
		},
		new ImGuiWidgets.DataTableColumn<DemoPart, bool>
		{
			Label = "In stock",
			Value = part => part.IsInStock,
			OnEdit = edit => Apply("Change stock", edit, (part, value) => part.IsInStock = value),
		},
		new ImGuiWidgets.DataTableColumn<DemoPart, PartCategory>
		{
			Label = "Category",
			Value = part => part.Category,
			OnEdit = edit => Apply("Change category", edit, (part, value) => part.Category = value),
		},
	]);

	/// <summary>
	/// Applies an edit as an undoable command. The row is captured rather than its index, so undo still
	/// finds the right part after the table has been sorted or filtered.
	/// </summary>
	private static void Apply<TValue>(string description, ImGuiWidgets.DataTableEdit<DemoPart, TValue> edit, System.Action<DemoPart, TValue> set)
	{
		DemoPart part = edit.Row;
		history.Execute(new DelegateCommand(description, () => set(part, edit.NewValue), () => set(part, edit.OldValue)));
		EditCount++;
	}

	private static void ShowContextMenu(ImGuiWidgets.DataTableContextMenu menu)
	{
		ImGui.TextDisabled(string.Create(CultureInfo.InvariantCulture, $"{menu.SelectedRows.Count} selected"));

		if (ImGui.MenuItem("Mark in stock"))
		{
			foreach (int sourceIndex in menu.SelectedRows)
			{
				DemoPart part = Parts[sourceIndex];
				bool wasInStock = part.IsInStock;
				history.Execute(new DelegateCommand("Mark in stock", () => part.IsInStock = true, () => part.IsInStock = wasInStock));
				EditCount++;
			}

			state.Refresh();
		}
	}

	private static IEnumerable<DemoPart> CreateParts()
	{
		for (int index = 0; index < PartCount; index++)
		{
			yield return new DemoPart
			{
				Name = string.Create(CultureInfo.InvariantCulture, $"Part {index:D4}"),
				Quantity = (index * 37) % 500,
				MassKilograms = ((index * 13) % 1000) / 100.0,
				IsInStock = index % 3 != 0,
				Category = (PartCategory)(index % 4),
			};
		}
	}

	private sealed class DemoPart
	{
		public string Name { get; set; } = string.Empty;

		public int Quantity { get; set; }

		public double MassKilograms { get; set; }

		public bool IsInStock { get; set; }

		public PartCategory Category { get; set; }
	}
}
```

If any of the four `ktsu.UndoRedo` namespaces turns out unused, the build says so. `NodeEditorHistory.cs` builds the same service from the same four, so they should all be needed.

- [ ] **Step 3: Show the page and reset it**

In `examples/ImGuiWidgetsDemo/ImGuiWidgetsDemo.cs`, add after `DiffViewDemo.ResetState();` in `ResetState`:

```csharp
		DataTableDemo.ResetState();
```

and after `VirtualTableDemo.Show();` in `ShowAdvancedDemos`:

```csharp
		DataTableDemo.Show();
```

- [ ] **Step 4: Register the section with the demo UI suite**

In `tests/ImGuiWidgetsDemo.UITests/WidgetsDemoUITests.cs`, add `"Data Table"` to `AdvancedDemoSections` after `"Virtual Table"`, so the suite opens the section headlessly like every other.

- [ ] **Step 5: Document the widget in the README**

In `ImGui.Widgets/README.md`, add to the **Data and Signals** list, after `Scope`:

```markdown
- **`DataTable`**: A table of typed rows that sorts and filters itself, moves an active cell with the keyboard, and edits one cell at a time, reporting each edit for you to apply
```

Then add a section after **Property Grid**'s `#### Paths and thumbnails` subsection:

````markdown
### Data Table

`DataTable` draws typed rows, only the ones on screen, with sortable headers and a filter box under each. A person moves an active cell with the arrow keys, Tab and Page Down, and edits a cell by double-clicking it, pressing F2, or typing. Enter commits and moves down, and Escape cancels.

The table never writes to your rows. Each editable column gets an `OnEdit` callback, and you apply the edit, which is what makes undo straightforward:

```csharp
private static readonly List<Part> parts = LoadParts();

private static readonly ImGuiWidgets.DataTableState<Part> state = new(
[
	new ImGuiWidgets.DataTableColumn<Part, string>
	{
		Label = "Name",
		Value = part => part.Name,
		OnEdit = edit => edit.Row.Name = edit.NewValue,
	},
	new ImGuiWidgets.DataTableColumn<Part, int>
	{
		Label = "Quantity",
		Value = part => part.Quantity,
	},
]);

ImGuiWidgets.DataTable("parts", parts, state);
```

A column with no `OnEdit` is read-only. Strings, numbers, `bool` and enums get built-in editors, and any other type supplies an `Editor`.

An edited row stays where it is until you call `state.Refresh()`, even if its new value sorts elsewhere, so it doesn't jump away while someone is working on it. The table rebuilds by itself when the sort, a filter, or the row count changes.

Pass `DataTableOptions.Keybindings` to drive the keys from a `ktsu.Keybinding` keymap, after registering the commands with `ImGuiWidgets.DataTableCommands.Register`.
````

- [ ] **Step 6: Record the dependency and the design in CLAUDE.md**

In `CLAUDE.md`, replace the `ktsu.Keybinding` line under **Dependencies**:

```markdown
- **ktsu.Keybinding** (2.0.3) - Keymap `NodeEditorInputHandler` and `DataTable` read their commands from (`NodeEditorCommands`, `DataTableCommands`). Referenced by `ImGui.Widgets` since the data table, so every consumer of `ktsu.ImGui.Widgets` restores it, and with it `Microsoft.Extensions.DependencyInjection.Abstractions` and `System.Text.Json`. Accepted because it's small and the table's keys have to follow the host's keymap.
```

and add to **Key Files**, after the `KeyChordMatcher.cs` line from Task 1:

```markdown
- `ImGui.Widgets/DataTableState.cs` - The data table's state, split across `DataTableState.Navigation.cs` and `DataTableState.Editing.cs`, with no ImGui calls; `DataTable.cs` and `DataTable.Input.cs` draw it
```

Then add a section under **Architecture**, after **Property grid**:

```markdown
### Data table

`DataTable` is two pieces. `DataTableState<TRow>` holds the view (source indices, filtered and then sorted, with ties broken by source index), the active cell, the selection, and the one edit session, and makes no ImGui calls, so its rules are unit tests. The renderer draws a frame from it and turns input into its commands.

Rows are always addressed by source index. The view rebuilds only when the sort, a filter, or the row count changes, or when the caller calls `Refresh()`, so an edited row stays put. A change in row count clears the selection and cancels an edit, because the table can't tell which rows the old indices meant.

Edits are reported, never applied. The session is cleared before `OnEdit` runs, so a callback can apply the edit and call `Refresh()`.

Tab stops are off inside the table (`ImGuiItemFlags.NoTabStop`), because Tab is the table's own command and ImGui's tabbing would otherwise hand the keyboard to a filter box.
```

- [ ] **Step 7: Build and run everything**

```bash
dotnet build ImGui.sln
./tests/ImGui.Widgets.Tests/bin/Debug/net10.0/ktsu.ImGui.Widgets.Tests
./tests/ImGui.Widgets.UITests/bin/Debug/net10.0/ktsu.ImGui.Widgets.UITests
./tests/ImGui.NodeEditor.Tests/bin/Debug/net10.0/ktsu.ImGui.NodeEditor.Tests
```

and run the demo UI suite's executable in `tests/ImGuiWidgetsDemo.UITests/bin/Debug/net10.0/`.

Expected: every suite passes, the counts are the baseline from Task 1 plus the new tests, 0 warnings. Then run the widgets demo, open **Advanced Demos**, expand **Data Table**, and check by hand that sorting, filtering, editing, undo and the context menu all behave as the README says.

- [ ] **Step 8: Commit**

```bash
git add examples/ImGuiWidgetsDemo/DataTableDemo.cs examples/ImGuiWidgetsDemo/ImGuiWidgetsDemo.csproj examples/ImGuiWidgetsDemo/ImGuiWidgetsDemo.cs tests/ImGuiWidgetsDemo.UITests/WidgetsDemoUITests.cs ImGui.Widgets/README.md CLAUDE.md
git commit -m "[patch] Show the data table in the widgets demo and document it"
```
