# Hexa Widgets Tier 2 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Wrap Hexa's stateful dialog and window types behind a ktsu-idiomatic API, with an explicit per-frame pump that makes their lifecycle work.

**Architecture:** Hexa dialogs register with static managers and are drawn by a pump the application runs every frame. We adopt that model and expose two mutually exclusive pumps: `DrawDeferred()` (dialogs, message boxes, popups, animations) and `DrawDeferredDocked()` (adds a dockspace and `DockedWindow`). Wrappers are instance types nested in `ImGuiWidgets` that convert Hexa's result enums and string paths into semantic ktsu types at the boundary.

**Tech Stack:** C# / .NET 10-9-8 multi-target, Hexa.NET.ImGui.Widgets 1.2.18, ktsu.Semantics.Paths, MSTest.Sdk on Microsoft Testing Platform.

**Spec:** `docs/superpowers/specs/2026-08-15-hexa-widgets-tier2-design.md`

## Global Constraints

- **Every build/test command MUST pass `-p:KtsuSyncStyleConfigFiles=false`.** Without it ktsu.Sdk rewrites `.editorconfig` mid-build and IDE0073 fires non-deterministically (184 / 0 / 45 errors across three runs of identical code).
- **`dotnet test` is broken here.** It reports "Zero tests ran" / exit 5 against these MSTest.Sdk-on-MTP projects. Use `dotnet run --project <testproj>`.
- **Never use `--filter`.** MTP does not accept VSTest filter syntax and silently matches nothing, which reads like a pass.
- **Line endings are LF.** The repo migrated to `* text=auto eol=lf`; `.editorconfig` sets `end_of_line = lf`. Files written with CRLF fail IDE0055 on every line.
- **File header, exactly:** `// Copyright (c) 2023-2026 ktsu-dev contributors`
- **Code style:** tabs, file-scoped namespaces, usings inside namespace, explicit types (no `var`), always braces, no `this.`, accessibility modifiers always.
- **Validation:** `Ensure.NotNull(param)` from Polyfill.
- **No `unsafe` in public signatures. No Hexa type in the public surface.**
- **No global suppressions.** Targeted `[SuppressMessage]` with a specific justification only. If CA1506 fires on `ImGuiWidgets`, note that a type-level suppression already exists on the `EnumCombo.cs` partial and covers the whole merged type.
- **Prefer PowerShell** for shell commands in this environment.

---

### Task 1: DialogOutcome and result mapping

The single most error-prone part of this tier, and the only part that is pure. Do it first so every later task consumes a tested mapping.

**Files:**
- Create: `ImGui.Widgets/Dialogs/DialogOutcome.cs`
- Test: `tests/ImGui.Widgets.Tests/DialogOutcomeTests.cs`

**Interfaces:**
- Consumes: nothing.
- Produces: `public enum DialogOutcome { None, Ok, Cancel, Failed, Yes, No }`; `internal static DialogOutcome ImGuiWidgets.MapMessageBoxResult(MessageBoxResult raw)`; `internal static DialogOutcome ImGuiWidgets.MapDialogResult(DialogResult raw, DialogMessageBoxType type)`.

**Background the implementer needs.** Hexa has two result enums that disagree:

```csharp
// Hexa.NET.ImGui.Widgets.Dialogs.DialogResult — used by Dialog and DialogMessageBox
enum DialogResult { None = -1, Ok = 0, Cancel = 1, Failed = 2, Yes = 0, No = 3 }
//                                                             ^^^^^^^ SAME VALUE as Ok

// Hexa.NET.ImGui.Widgets.MessageBoxResult — used by MessageBox
enum MessageBoxResult { None, Ok, Cancel, Yes, No }   // all distinct
```

`DialogResult.Yes` and `DialogResult.Ok` are literally the same value. You cannot write `case DialogResult.Ok: ... case DialogResult.Yes:` — it will not compile (duplicate case label). The dialog's configured `DialogMessageBoxType` is the only surviving evidence of which one the user pressed.

`DialogMessageBoxType` is `{ Ok, OkCancel, YesNo, YesNoCancel, YesCancel }`.

- [ ] **Step 1: Write the failing tests**

Create `tests/ImGui.Widgets.Tests/DialogOutcomeTests.cs`:

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using HexaDialogMessageBoxType = Hexa.NET.ImGui.Widgets.Dialogs.DialogMessageBoxType;
using HexaDialogResult = Hexa.NET.ImGui.Widgets.Dialogs.DialogResult;
using HexaMessageBoxResult = Hexa.NET.ImGui.Widgets.MessageBoxResult;

/// <summary>
/// Tests the conversion from Hexa's two disagreeing result enums into <see cref="DialogOutcome"/>.
/// </summary>
[TestClass]
public sealed class DialogOutcomeTests
{
	[TestMethod]
	public void MapMessageBoxResult_None_MapsToNone() =>
		Assert.AreEqual(DialogOutcome.None, ImGuiWidgets.MapMessageBoxResult(HexaMessageBoxResult.None));

	[TestMethod]
	public void MapMessageBoxResult_Ok_MapsToOk() =>
		Assert.AreEqual(DialogOutcome.Ok, ImGuiWidgets.MapMessageBoxResult(HexaMessageBoxResult.Ok));

	[TestMethod]
	public void MapMessageBoxResult_Cancel_MapsToCancel() =>
		Assert.AreEqual(DialogOutcome.Cancel, ImGuiWidgets.MapMessageBoxResult(HexaMessageBoxResult.Cancel));

	[TestMethod]
	public void MapMessageBoxResult_Yes_MapsToYes() =>
		Assert.AreEqual(DialogOutcome.Yes, ImGuiWidgets.MapMessageBoxResult(HexaMessageBoxResult.Yes));

	[TestMethod]
	public void MapMessageBoxResult_No_MapsToNo() =>
		Assert.AreEqual(DialogOutcome.No, ImGuiWidgets.MapMessageBoxResult(HexaMessageBoxResult.No));

	[TestMethod]
	public void MapDialogResult_ZeroOnYesFlavouredTypes_MapsToYes()
	{
		// DialogResult.Ok and DialogResult.Yes are both 0; only the dialog type disambiguates.
		Assert.AreEqual(DialogOutcome.Yes, ImGuiWidgets.MapDialogResult(HexaDialogResult.Ok, HexaDialogMessageBoxType.YesNo));
		Assert.AreEqual(DialogOutcome.Yes, ImGuiWidgets.MapDialogResult(HexaDialogResult.Ok, HexaDialogMessageBoxType.YesNoCancel));
		Assert.AreEqual(DialogOutcome.Yes, ImGuiWidgets.MapDialogResult(HexaDialogResult.Ok, HexaDialogMessageBoxType.YesCancel));
	}

	[TestMethod]
	public void MapDialogResult_ZeroOnOkFlavouredTypes_MapsToOk()
	{
		Assert.AreEqual(DialogOutcome.Ok, ImGuiWidgets.MapDialogResult(HexaDialogResult.Ok, HexaDialogMessageBoxType.Ok));
		Assert.AreEqual(DialogOutcome.Ok, ImGuiWidgets.MapDialogResult(HexaDialogResult.Ok, HexaDialogMessageBoxType.OkCancel));
	}

	[TestMethod]
	public void MapDialogResult_NonZeroValues_AreTypeIndependent()
	{
		foreach (HexaDialogMessageBoxType type in new[]
		{
			HexaDialogMessageBoxType.Ok,
			HexaDialogMessageBoxType.OkCancel,
			HexaDialogMessageBoxType.YesNo,
			HexaDialogMessageBoxType.YesNoCancel,
			HexaDialogMessageBoxType.YesCancel,
		})
		{
			Assert.AreEqual(DialogOutcome.None, ImGuiWidgets.MapDialogResult(HexaDialogResult.None, type));
			Assert.AreEqual(DialogOutcome.Cancel, ImGuiWidgets.MapDialogResult(HexaDialogResult.Cancel, type));
			Assert.AreEqual(DialogOutcome.Failed, ImGuiWidgets.MapDialogResult(HexaDialogResult.Failed, type));
			Assert.AreEqual(DialogOutcome.No, ImGuiWidgets.MapDialogResult(HexaDialogResult.No, type));
		}
	}

	[TestMethod]
	public void MapDialogResult_UnknownValue_MapsToNone() =>
		Assert.AreEqual(DialogOutcome.None, ImGuiWidgets.MapDialogResult((HexaDialogResult)99, HexaDialogMessageBoxType.Ok));

	[TestMethod]
	public void MapMessageBoxResult_UnknownValue_MapsToNone() =>
		Assert.AreEqual(DialogOutcome.None, ImGuiWidgets.MapMessageBoxResult((HexaMessageBoxResult)99));
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet run --project tests/ImGui.Widgets.Tests/ImGui.Widgets.Tests.csproj -p:KtsuSyncStyleConfigFiles=false`
Expected: compile failure — `DialogOutcome` and the two map methods do not exist.

- [ ] **Step 3: Write the implementation**

Create `ImGui.Widgets/Dialogs/DialogOutcome.cs`:

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using HexaDialogMessageBoxType = Hexa.NET.ImGui.Widgets.Dialogs.DialogMessageBoxType;
using HexaDialogResult = Hexa.NET.ImGui.Widgets.Dialogs.DialogResult;
using HexaMessageBoxResult = Hexa.NET.ImGui.Widgets.MessageBoxResult;

/// <summary>
/// How the user dismissed a dialog.
/// </summary>
public enum DialogOutcome
{
	/// <summary>
	/// The dialog was dismissed without a choice, or has not been dismissed yet.
	/// </summary>
	None,

	/// <summary>
	/// The user accepted an OK-flavoured prompt.
	/// </summary>
	Ok,

	/// <summary>
	/// The user canceled.
	/// </summary>
	Cancel,

	/// <summary>
	/// The operation failed. Only Hexa's dialogs report this; message boxes never do.
	/// </summary>
	Failed,

	/// <summary>
	/// The user accepted a Yes-flavoured prompt.
	/// </summary>
	Yes,

	/// <summary>
	/// The user declined a Yes/No prompt.
	/// </summary>
	No,
}

public static partial class ImGuiWidgets
{
	/// <summary>
	/// Converts a message box result. The mapping is 1:1 because Hexa's message box enum keeps
	/// every member distinct.
	/// </summary>
	/// <param name="raw">The result reported by Hexa.</param>
	/// <returns>The equivalent <see cref="DialogOutcome"/>.</returns>
	internal static DialogOutcome MapMessageBoxResult(HexaMessageBoxResult raw) => raw switch
	{
		HexaMessageBoxResult.Ok => DialogOutcome.Ok,
		HexaMessageBoxResult.Cancel => DialogOutcome.Cancel,
		HexaMessageBoxResult.Yes => DialogOutcome.Yes,
		HexaMessageBoxResult.No => DialogOutcome.No,
		_ => DialogOutcome.None,
	};

	/// <summary>
	/// Converts a dialog result, using the dialog's configured type to recover a distinction the
	/// value alone cannot carry.
	/// </summary>
	/// <param name="raw">The result reported by Hexa.</param>
	/// <param name="type">The type the dialog was configured with.</param>
	/// <returns>The equivalent <see cref="DialogOutcome"/>.</returns>
	/// <remarks>
	/// Hexa declares <c>Yes = 0</c> and <c>Ok = 0</c> in the same enum, so the two are the same
	/// value and cannot both appear in a switch. A Yes-flavoured dialog reporting 0 means Yes; any
	/// other dialog reporting 0 means Ok. The configured type is the only place that survives.
	/// </remarks>
	internal static DialogOutcome MapDialogResult(HexaDialogResult raw, HexaDialogMessageBoxType type)
	{
		// Compared numerically on purpose: `case HexaDialogResult.Ok` and `case HexaDialogResult.Yes`
		// are the same label and will not compile together.
		int value = (int)raw;

		if (value == (int)HexaDialogResult.Ok)
		{
			return IsYesFlavoured(type) ? DialogOutcome.Yes : DialogOutcome.Ok;
		}

		if (value == (int)HexaDialogResult.Cancel)
		{
			return DialogOutcome.Cancel;
		}

		if (value == (int)HexaDialogResult.Failed)
		{
			return DialogOutcome.Failed;
		}

		return value == (int)HexaDialogResult.No ? DialogOutcome.No : DialogOutcome.None;
	}

	/// <summary>
	/// Reports whether a dialog type labels its affirmative button "Yes" rather than "OK".
	/// </summary>
	/// <param name="type">The dialog type.</param>
	/// <returns><see langword="true"/> if the affirmative button reads "Yes".</returns>
	private static bool IsYesFlavoured(HexaDialogMessageBoxType type) =>
		type is HexaDialogMessageBoxType.YesNo
			or HexaDialogMessageBoxType.YesNoCancel
			or HexaDialogMessageBoxType.YesCancel;
}
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet run --project tests/ImGui.Widgets.Tests/ImGui.Widgets.Tests.csproj -p:KtsuSyncStyleConfigFiles=false`
Expected: PASS, 10 new tests (144 total).

- [ ] **Step 5: Convert files to LF and commit**

```powershell
# Convert any CRLF the editor introduced, then commit.
git add ImGui.Widgets/Dialogs/DialogOutcome.cs tests/ImGui.Widgets.Tests/DialogOutcomeTests.cs
git commit -m "Add DialogOutcome and the mapping from Hexa's two result enums"
```

---

### Task 2: The pumps and their diagnostics

**Files:**
- Create: `ImGui.Widgets/DeferredDrawing.cs`
- Delete: `ImGui.Widgets/HexaAnimationPump.cs`
- Modify: `ImGui.Widgets/HexaButtons.cs` (remove the `HexaAnimationPump.TickOncePerFrame()` call from `HexaButtonImpl.ToggleSwitch`)
- Modify: `tests/ImGui.Widgets.Tests/HexaWidgetTests.cs` (remove the five `ShouldTick_*` tests, which test a deleted type)
- Test: `tests/ImGui.Widgets.Tests/DeferredDrawingTests.cs`

**Interfaces:**
- Consumes: nothing from Task 1.
- Produces: `public static void ImGuiWidgets.DrawDeferred()`; `public static void ImGuiWidgets.DrawDeferredDocked()`; `internal static void ImGuiWidgets.NotifyDialogShown()`; `internal static PumpState ImGuiWidgets.EvaluatePump(int currentFrame, ref PumpTracker tracker)` and `internal struct PumpTracker` / `internal enum PumpState` for testing.

**Why `HexaAnimationPump` goes.** It was added in PR #303 as an explicitly temporary fix for `ToggleSwitch` rendering inverted, with the documented exit condition "when a real per-frame pump lands, delete this type and its call sites, because `WidgetManager.Draw()` ticks the clock itself and both would double the speed." Both pumps here tick the clock, so that condition is met. Leaving it in place would advance animations at double speed.

- [ ] **Step 1: Write the failing tests**

Create `tests/ImGui.Widgets.Tests/DeferredDrawingTests.cs`:

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests the pure frame-tracking logic behind the deferred-drawing pumps. The pumps themselves
/// need a live ImGui context and are verified visually in the demos.
/// </summary>
[TestClass]
public sealed class DeferredDrawingTests
{
	[TestMethod]
	public void EvaluatePump_FirstCallEver_ReportsOk()
	{
		ImGuiWidgets.PumpTracker tracker = default;

		Assert.AreEqual(ImGuiWidgets.PumpState.Ok, ImGuiWidgets.EvaluatePump(0, ref tracker));
	}

	[TestMethod]
	public void EvaluatePump_SecondCallInSameFrame_ReportsDoublePumped()
	{
		ImGuiWidgets.PumpTracker tracker = default;
		_ = ImGuiWidgets.EvaluatePump(5, ref tracker);

		Assert.AreEqual(ImGuiWidgets.PumpState.DoublePumped, ImGuiWidgets.EvaluatePump(5, ref tracker));
	}

	[TestMethod]
	public void EvaluatePump_AdvancingFrames_ReportsOkEachTime()
	{
		ImGuiWidgets.PumpTracker tracker = default;

		for (int frame = 0; frame < 5; frame++)
		{
			Assert.AreEqual(ImGuiWidgets.PumpState.Ok, ImGuiWidgets.EvaluatePump(frame, ref tracker));
		}
	}

	[TestMethod]
	public void EvaluatePump_FrameCounterResets_ReportsOk()
	{
		// A recreated ImGui context restarts the frame counter; a >= comparison would stall here.
		ImGuiWidgets.PumpTracker tracker = default;
		_ = ImGuiWidgets.EvaluatePump(500, ref tracker);

		Assert.AreEqual(ImGuiWidgets.PumpState.Ok, ImGuiWidgets.EvaluatePump(0, ref tracker));
	}

	[TestMethod]
	public void HasEverPumped_DefaultTracker_IsFalse()
	{
		ImGuiWidgets.PumpTracker tracker = default;

		Assert.IsFalse(tracker.HasEverPumped);
	}

	[TestMethod]
	public void HasEverPumped_AfterFirstPump_IsTrue()
	{
		ImGuiWidgets.PumpTracker tracker = default;
		_ = ImGuiWidgets.EvaluatePump(0, ref tracker);

		Assert.IsTrue(tracker.HasEverPumped);
	}
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet run --project tests/ImGui.Widgets.Tests/ImGui.Widgets.Tests.csproj -p:KtsuSyncStyleConfigFiles=false`
Expected: compile failure — `PumpTracker`, `PumpState` and `EvaluatePump` do not exist.

- [ ] **Step 3: Write the implementation**

Create `ImGui.Widgets/DeferredDrawing.cs`:

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System.Diagnostics;

using Hexa.NET.ImGui;

using HexaAnimationManager = Hexa.NET.ImGui.Widgets.AnimationManager;
using HexaDialogManager = Hexa.NET.ImGui.Widgets.Dialogs.DialogManager;
using HexaMessageBoxes = Hexa.NET.ImGui.Widgets.MessageBoxes;
using HexaPopupManager = Hexa.NET.ImGui.Widgets.Dialogs.PopupManager;
using HexaWidgetManager = Hexa.NET.ImGui.Widgets.WidgetManager;

public static partial class ImGuiWidgets
{
	/// <summary>
	/// Tracks which frame a pump last ran on.
	/// </summary>
	internal struct PumpTracker
	{
		/// <summary>
		/// Gets or sets the frame a pump last ran on.
		/// </summary>
		public int LastFrame { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether a pump has ever run.
		/// </summary>
		public bool HasEverPumped { get; set; }
	}

	/// <summary>
	/// The result of evaluating a pump call.
	/// </summary>
	internal enum PumpState
	{
		/// <summary>
		/// The first pump of this frame.
		/// </summary>
		Ok,

		/// <summary>
		/// A pump already ran this frame; running another draws everything twice.
		/// </summary>
		DoublePumped,
	}

	private static PumpTracker pumpTracker;

	/// <summary>
	/// Draws every dialog, message box and popup that is currently open, and advances the
	/// animation clock. Call once per frame, at the end of your render callback.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Hexa's dialogs are stateful: showing one registers it with a static manager, and it is only
	/// drawn when a pump runs. Without this call no dialog ever appears and the manager's internal
	/// collections grow for the life of the process.
	/// </para>
	/// <para>
	/// Mutually exclusive with <see cref="DrawDeferredDocked"/>, which already does everything this
	/// does. Calling both in one frame draws every dialog twice.
	/// </para>
	/// </remarks>
	public static void DrawDeferred()
	{
		ReportPumpState();

		HexaDialogManager.Draw();
		HexaMessageBoxes.Draw();
		HexaPopupManager.Draw();
		HexaAnimationManager.Tick();
	}

	/// <summary>
	/// Draws a dockspace over the main viewport, every registered <see cref="DockedWindow"/>, and
	/// everything <see cref="DrawDeferred"/> draws. Call once per frame, at the end of your render
	/// callback.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Only needed if the application uses <see cref="DockedWindow"/>. This creates a dockspace
	/// over the main viewport, which is a layout decision — prefer <see cref="DrawDeferred"/>
	/// unless you want that.
	/// </para>
	/// <para>
	/// Mutually exclusive with <see cref="DrawDeferred"/>. Calling both in one frame draws every
	/// dialog twice.
	/// </para>
	/// </remarks>
	public static void DrawDeferredDocked()
	{
		ReportPumpState();

		// WidgetManager.Draw internally calls DialogManager.Draw, MessageBoxes.Draw,
		// PopupManager.Draw and AnimationManager.Tick, so this must not also call them.
		HexaWidgetManager.Draw();
	}

	/// <summary>
	/// Records that a dialog was shown, so a missing pump can be reported at the point it matters.
	/// </summary>
	/// <exception cref="InvalidOperationException">No pump has ever run.</exception>
	internal static void NotifyDialogShown()
	{
		if (!pumpTracker.HasEverPumped)
		{
			throw new InvalidOperationException(
				"A dialog was shown but neither ImGuiWidgets.DrawDeferred() nor " +
				"ImGuiWidgets.DrawDeferredDocked() has ever run. Hexa's dialogs are only drawn by " +
				"a per-frame pump; call ImGuiWidgets.DrawDeferred() at the end of your render " +
				"callback. Without it the dialog never appears and the manager's internal " +
				"collections grow for the life of the process.");
		}
	}

	/// <summary>
	/// Updates the pump tracker and reports whether this call is the first of the frame.
	/// </summary>
	/// <param name="currentFrame">The current ImGui frame number.</param>
	/// <param name="tracker">The tracker to update.</param>
	/// <returns>The state of this pump call.</returns>
	/// <remarks>
	/// Any change in frame number counts, not just an increase, so a reset frame counter (a
	/// recreated ImGui context) does not stall the pump.
	/// </remarks>
	internal static PumpState EvaluatePump(int currentFrame, ref PumpTracker tracker)
	{
		if (tracker.HasEverPumped && tracker.LastFrame == currentFrame)
		{
			return PumpState.DoublePumped;
		}

		tracker.LastFrame = currentFrame;
		tracker.HasEverPumped = true;
		return PumpState.Ok;
	}

	/// <summary>
	/// Evaluates the pump for this frame and traces a warning if it was already pumped.
	/// </summary>
	private static void ReportPumpState()
	{
		if (EvaluatePump(ImGui.GetFrameCount(), ref pumpTracker) == PumpState.DoublePumped)
		{
			Trace.TraceWarning(
				"ImGuiWidgets: a deferred-drawing pump already ran this frame. DrawDeferred() and " +
				"DrawDeferredDocked() are mutually exclusive - DrawDeferredDocked() already draws " +
				"everything DrawDeferred() does. Every dialog is being drawn twice this frame.");
		}
	}
}
```

- [ ] **Step 4: Remove the superseded stopgap**

Delete `ImGui.Widgets/HexaAnimationPump.cs`.

In `ImGui.Widgets/HexaButtons.cs`, replace the `HexaButtonImpl.ToggleSwitch` body:

```csharp
		internal static bool ToggleSwitch(string label, ref bool selected) => HexaButton.ToggleSwitch(label, ref selected);
```

In `tests/ImGui.Widgets.Tests/HexaWidgetTests.cs`, delete these five test methods, which cover the deleted type: `ShouldTick_FirstCallOnNeverTickedState_Ticks`, `ShouldTick_SecondCallInSameFrame_DoesNotTick`, `ShouldTick_ManyCallsInSameFrame_TicksExactlyOnce`, `ShouldTick_AdvancingFrames_TicksOncePerFrame`, `ShouldTick_FrameCounterResets_StillTicks`.

- [ ] **Step 5: Run the tests to verify they pass**

Run: `dotnet run --project tests/ImGui.Widgets.Tests/ImGui.Widgets.Tests.csproj -p:KtsuSyncStyleConfigFiles=false`
Expected: PASS. 144 - 5 removed + 6 new = 145 total.

- [ ] **Step 6: Commit**

```powershell
git add ImGui.Widgets/DeferredDrawing.cs ImGui.Widgets/HexaButtons.cs tests/ImGui.Widgets.Tests/DeferredDrawingTests.cs tests/ImGui.Widgets.Tests/HexaWidgetTests.cs
git rm ImGui.Widgets/HexaAnimationPump.cs
git commit -m "Add the deferred-drawing pumps and retire the animation stopgap"
```

---

### Task 3: File and folder dialogs

**Files:**
- Create: `ImGui.Widgets/Dialogs/FileDialogs.cs`
- Test: `tests/ImGui.Widgets.Tests/FileDialogTests.cs`

**Interfaces:**
- Consumes: `DialogOutcome`, `ImGuiWidgets.MapDialogResult` (Task 1); `ImGuiWidgets.NotifyDialogShown()` (Task 2).
- Produces: `ImGuiWidgets.OpenFileDialog`, `ImGuiWidgets.SaveFileDialog`, `ImGuiWidgets.OpenFolderDialog`, and the records `FileDialogOutcome`, `FolderDialogOutcome`; `internal static AbsoluteFilePath? ImGuiWidgets.TryParseFilePath(string?)`; `internal static AbsoluteDirectoryPath? ImGuiWidgets.TryParseDirectoryPath(string?)`.

**Upstream facts the implementer needs.**
- `OpenFileDialog` — `string? SelectedFile`, `IReadOnlyList<string> Selection`, `bool AllowMultipleSelection`, `Name` is `"File Picker"`.
- `SaveFileDialog` — `string SelectedFile` (not nullable), `Name` is `"Save File"`.
- `OpenFolderDialog` — `string? SelectedFolder`, `Name` is `"Folder Picker"`.
- All three derive from `FileDialogBase : Dialog`. `Show(DialogCallback)` registers with `DialogManager`; the callback is `void (object? sender, DialogResult result)`.
- These dialogs are configured with `DialogMessageBoxType.Ok` semantics — they have no Yes/No — so pass `DialogMessageBoxType.Ok` to `MapDialogResult`.
- `Name` is both the window title and the ImGui identity, so two open `OpenFileDialog`s collide. Document it; do not try to fix it.

- [ ] **Step 1: Write the failing tests**

Create `tests/ImGui.Widgets.Tests/FileDialogTests.cs`:

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests the path conversion at the file dialog boundary. The dialogs themselves need a live
/// ImGui context and are verified visually in ImGuiWidgetsDemo.
/// </summary>
[TestClass]
public sealed class FileDialogTests
{
	[TestMethod]
	public void TryParseFilePath_Null_ReturnsNull() =>
		Assert.IsNull(ImGuiWidgets.TryParseFilePath(null));

	[TestMethod]
	public void TryParseFilePath_Empty_ReturnsNull() =>
		Assert.IsNull(ImGuiWidgets.TryParseFilePath(string.Empty));

	[TestMethod]
	public void TryParseFilePath_Whitespace_ReturnsNull() =>
		Assert.IsNull(ImGuiWidgets.TryParseFilePath("   "));

	[TestMethod]
	public void TryParseFilePath_AbsolutePath_RoundTrips()
	{
		string input = System.IO.Path.Combine(System.AppContext.BaseDirectory, "example.txt");

		Assert.IsNotNull(ImGuiWidgets.TryParseFilePath(input));
	}

	[TestMethod]
	public void TryParseDirectoryPath_Null_ReturnsNull() =>
		Assert.IsNull(ImGuiWidgets.TryParseDirectoryPath(null));

	[TestMethod]
	public void TryParseDirectoryPath_BaseDirectory_RoundTrips() =>
		Assert.IsNotNull(ImGuiWidgets.TryParseDirectoryPath(System.AppContext.BaseDirectory));

	[TestMethod]
	public void FileDialogOutcome_DefaultSelection_IsEmptyNotNull()
	{
		FileDialogOutcome outcome = new(DialogOutcome.Cancel, null, []);

		Assert.IsNotNull(outcome.Selection);
		Assert.AreEqual(0, outcome.Selection.Count);
	}
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet run --project tests/ImGui.Widgets.Tests/ImGui.Widgets.Tests.csproj -p:KtsuSyncStyleConfigFiles=false`
Expected: compile failure — the types do not exist.

- [ ] **Step 3: Write the implementation**

Create `ImGui.Widgets/Dialogs/FileDialogs.cs`:

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System.Collections.Generic;
using System.Collections.ObjectModel;

using ktsu.Semantics.Paths;
using ktsu.Semantics.Strings;

using HexaDialogMessageBoxType = Hexa.NET.ImGui.Widgets.Dialogs.DialogMessageBoxType;
using HexaDialogResult = Hexa.NET.ImGui.Widgets.Dialogs.DialogResult;
using HexaOpenFileDialog = Hexa.NET.ImGui.Widgets.Dialogs.OpenFileDialog;
using HexaOpenFolderDialog = Hexa.NET.ImGui.Widgets.Dialogs.OpenFolderDialog;
using HexaSaveFileDialog = Hexa.NET.ImGui.Widgets.Dialogs.SaveFileDialog;

/// <summary>
/// The result of a file dialog.
/// </summary>
/// <param name="Outcome">How the dialog was dismissed.</param>
/// <param name="Path">The chosen file, or <see langword="null"/> if none was chosen.</param>
/// <param name="Selection">Every chosen file. Empty unless multiple selection was enabled.</param>
public sealed record FileDialogOutcome(
	DialogOutcome Outcome,
	AbsoluteFilePath? Path,
	IReadOnlyList<AbsoluteFilePath> Selection);

/// <summary>
/// The result of a folder dialog.
/// </summary>
/// <param name="Outcome">How the dialog was dismissed.</param>
/// <param name="Path">The chosen folder, or <see langword="null"/> if none was chosen.</param>
public sealed record FolderDialogOutcome(
	DialogOutcome Outcome,
	AbsoluteDirectoryPath? Path);

public static partial class ImGuiWidgets
{
	/// <summary>
	/// Converts a path string from Hexa into a semantic file path.
	/// </summary>
	/// <param name="raw">The path Hexa reported.</param>
	/// <returns>The parsed path, or <see langword="null"/> if there was none.</returns>
	internal static AbsoluteFilePath? TryParseFilePath(string? raw) =>
		string.IsNullOrWhiteSpace(raw) ? null : raw.As<AbsoluteFilePath>();

	/// <summary>
	/// Converts a path string from Hexa into a semantic directory path.
	/// </summary>
	/// <param name="raw">The path Hexa reported.</param>
	/// <returns>The parsed path, or <see langword="null"/> if there was none.</returns>
	internal static AbsoluteDirectoryPath? TryParseDirectoryPath(string? raw) =>
		string.IsNullOrWhiteSpace(raw) ? null : raw.As<AbsoluteDirectoryPath>();

	/// <summary>
	/// A dialog for choosing one or more existing files.
	/// </summary>
	/// <remarks>
	/// Requires a per-frame pump: call <see cref="DrawDeferred"/> from your render callback.
	/// Needs a Material Icons font in the atlas for its navigation bar; see
	/// <c>ImGuiAppConfig.OnConfigureFonts</c>. The underlying window is identified by a fixed
	/// title, so two of these open at once will collide.
	/// </remarks>
	public sealed class OpenFileDialog
	{
		private readonly HexaOpenFileDialog dialog = new();

		/// <summary>
		/// Gets or sets a value indicating whether more than one file may be chosen.
		/// </summary>
		public bool AllowMultipleSelection
		{
			get => dialog.AllowMultipleSelection;
			set => dialog.AllowMultipleSelection = value;
		}

		/// <summary>
		/// Shows the dialog.
		/// </summary>
		/// <param name="onClosed">Invoked once, when the dialog closes.</param>
		/// <exception cref="ArgumentNullException"><paramref name="onClosed"/> is <see langword="null"/>.</exception>
		/// <exception cref="InvalidOperationException">No deferred-drawing pump has ever run.</exception>
		public void Show(Action<FileDialogOutcome> onClosed)
		{
			Ensure.NotNull(onClosed);
			NotifyDialogShown();

			dialog.Show((_, result) => onClosed(BuildOutcome(dialog, result)));
		}

		/// <summary>
		/// Builds the managed outcome from the dialog's state at close time.
		/// </summary>
		/// <param name="source">The dialog that closed.</param>
		/// <param name="result">The raw result Hexa reported.</param>
		/// <returns>The outcome to hand to the caller.</returns>
		private static FileDialogOutcome BuildOutcome(HexaOpenFileDialog source, HexaDialogResult result)
		{
			Collection<AbsoluteFilePath> selection = [];
			foreach (string entry in source.Selection)
			{
				AbsoluteFilePath? parsed = TryParseFilePath(entry);
				if (parsed is not null)
				{
					selection.Add(parsed);
				}
			}

			// File dialogs have no Yes/No variant, so the Ok flavour is always correct here.
			return new FileDialogOutcome(
				MapDialogResult(result, HexaDialogMessageBoxType.Ok),
				TryParseFilePath(source.SelectedFile),
				selection);
		}
	}

	/// <summary>
	/// A dialog for choosing a file to write to.
	/// </summary>
	/// <remarks>
	/// Requires a per-frame pump: call <see cref="DrawDeferred"/> from your render callback.
	/// Needs a Material Icons font in the atlas for its navigation bar.
	/// </remarks>
	public sealed class SaveFileDialog
	{
		private readonly HexaSaveFileDialog dialog = new();

		/// <summary>
		/// Shows the dialog.
		/// </summary>
		/// <param name="onClosed">Invoked once, when the dialog closes.</param>
		/// <exception cref="ArgumentNullException"><paramref name="onClosed"/> is <see langword="null"/>.</exception>
		/// <exception cref="InvalidOperationException">No deferred-drawing pump has ever run.</exception>
		public void Show(Action<FileDialogOutcome> onClosed)
		{
			Ensure.NotNull(onClosed);
			NotifyDialogShown();

			dialog.Show((_, result) => onClosed(new FileDialogOutcome(
				MapDialogResult(result, HexaDialogMessageBoxType.Ok),
				TryParseFilePath(dialog.SelectedFile),
				[])));
		}
	}

	/// <summary>
	/// A dialog for choosing an existing folder.
	/// </summary>
	/// <remarks>
	/// Requires a per-frame pump: call <see cref="DrawDeferred"/> from your render callback.
	/// Needs a Material Icons font in the atlas for its navigation bar.
	/// </remarks>
	public sealed class OpenFolderDialog
	{
		private readonly HexaOpenFolderDialog dialog = new();

		/// <summary>
		/// Shows the dialog.
		/// </summary>
		/// <param name="onClosed">Invoked once, when the dialog closes.</param>
		/// <exception cref="ArgumentNullException"><paramref name="onClosed"/> is <see langword="null"/>.</exception>
		/// <exception cref="InvalidOperationException">No deferred-drawing pump has ever run.</exception>
		public void Show(Action<FolderDialogOutcome> onClosed)
		{
			Ensure.NotNull(onClosed);
			NotifyDialogShown();

			dialog.Show((_, result) => onClosed(new FolderDialogOutcome(
				MapDialogResult(result, HexaDialogMessageBoxType.Ok),
				TryParseDirectoryPath(dialog.SelectedFolder))));
		}
	}
}
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet run --project tests/ImGui.Widgets.Tests/ImGui.Widgets.Tests.csproj -p:KtsuSyncStyleConfigFiles=false`
Expected: PASS, 7 new tests (152 total).

- [ ] **Step 5: Commit**

```powershell
git add ImGui.Widgets/Dialogs/FileDialogs.cs tests/ImGui.Widgets.Tests/FileDialogTests.cs
git commit -m "Add the file and folder dialog wrappers"
```

---

### Task 4: RenameDialog

**Files:**
- Create: `ImGui.Widgets/Dialogs/RenameDialog.cs`
- Test: `tests/ImGui.Widgets.Tests/RenameDialogTests.cs`

**Interfaces:**
- Consumes: `DialogOutcome`, `MapDialogResult`, `TryParseFilePath`, `NotifyDialogShown`.
- Produces: `ImGuiWidgets.RenameDialog`, `RenameOutcome`.

**Upstream facts.** `RenameDialog(string sourcePath, RenameDialogFlags flags = Default)`; `string SourcePath`; `string? DestinationPath`; `Exception? Exception`; `bool Overwrite`; `bool NoAutomaticMove`; `bool SourceMustExist`; `Name` is `"Rename"`.

**This dialog performs the move itself** unless `NoAutomaticMove` is set, and captures any failure into `Exception` instead of throwing. A wrapper that returned only an outcome enum would silently discard why a rename failed, so `RenameOutcome` carries it.

- [ ] **Step 1: Write the failing test**

Create `tests/ImGui.Widgets.Tests/RenameDialogTests.cs`:

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests the rename dialog's outcome record. The dialog itself needs a live ImGui context.
/// </summary>
[TestClass]
public sealed class RenameDialogTests
{
	[TestMethod]
	public void RenameOutcome_CarriesTheFailureReason()
	{
		InvalidOperationException error = new("target exists");
		RenameOutcome outcome = new(DialogOutcome.Failed, null, error);

		Assert.AreEqual(DialogOutcome.Failed, outcome.Outcome);
		Assert.AreSame(error, outcome.Error);
	}

	[TestMethod]
	public void RenameOutcome_SuccessHasNoError()
	{
		RenameOutcome outcome = new(DialogOutcome.Ok, null, null);

		Assert.IsNull(outcome.Error);
	}
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet run --project tests/ImGui.Widgets.Tests/ImGui.Widgets.Tests.csproj -p:KtsuSyncStyleConfigFiles=false`
Expected: compile failure — `RenameOutcome` does not exist.

- [ ] **Step 3: Write the implementation**

Create `ImGui.Widgets/Dialogs/RenameDialog.cs`:

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using ktsu.Semantics.Paths;

using HexaDialogMessageBoxType = Hexa.NET.ImGui.Widgets.Dialogs.DialogMessageBoxType;
using HexaRenameDialog = Hexa.NET.ImGui.Widgets.Dialogs.RenameDialog;

/// <summary>
/// The result of a rename dialog.
/// </summary>
/// <param name="Outcome">How the dialog was dismissed.</param>
/// <param name="Destination">The new path, or <see langword="null"/> if the rename did not happen.</param>
/// <param name="Error">Why the rename failed, or <see langword="null"/> if it did not fail.</param>
public sealed record RenameOutcome(
	DialogOutcome Outcome,
	AbsoluteFilePath? Destination,
	Exception? Error);

public static partial class ImGuiWidgets
{
	/// <summary>
	/// A dialog for renaming a file or folder.
	/// </summary>
	/// <remarks>
	/// This dialog performs the move itself unless <see cref="SkipAutomaticMove"/> is set, and
	/// reports a failure through <see cref="RenameOutcome.Error"/> rather than by throwing.
	/// Requires a per-frame pump: call <see cref="DrawDeferred"/> from your render callback.
	/// </remarks>
	public sealed class RenameDialog
	{
		private readonly HexaRenameDialog dialog;

		/// <summary>
		/// Initializes a new instance of the <see cref="RenameDialog"/> class.
		/// </summary>
		/// <param name="source">The path being renamed.</param>
		/// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
		public RenameDialog(AbsoluteFilePath source)
		{
			Ensure.NotNull(source);
			dialog = new HexaRenameDialog(source.ToString());
		}

		/// <summary>
		/// Gets or sets a value indicating whether an existing destination may be replaced.
		/// </summary>
		public bool Overwrite
		{
			get => dialog.Overwrite;
			set => dialog.Overwrite = value;
		}

		/// <summary>
		/// Gets or sets a value indicating whether the dialog should collect a new name without
		/// moving anything on disk.
		/// </summary>
		public bool SkipAutomaticMove
		{
			get => dialog.NoAutomaticMove;
			set => dialog.NoAutomaticMove = value;
		}

		/// <summary>
		/// Shows the dialog.
		/// </summary>
		/// <param name="onClosed">Invoked once, when the dialog closes.</param>
		/// <exception cref="ArgumentNullException"><paramref name="onClosed"/> is <see langword="null"/>.</exception>
		/// <exception cref="InvalidOperationException">No deferred-drawing pump has ever run.</exception>
		public void Show(Action<RenameOutcome> onClosed)
		{
			Ensure.NotNull(onClosed);
			NotifyDialogShown();

			dialog.Show((_, result) => onClosed(new RenameOutcome(
				MapDialogResult(result, HexaDialogMessageBoxType.Ok),
				TryParseFilePath(dialog.DestinationPath),
				dialog.Exception)));
		}
	}
}
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet run --project tests/ImGui.Widgets.Tests/ImGui.Widgets.Tests.csproj -p:KtsuSyncStyleConfigFiles=false`
Expected: PASS, 2 new tests (154 total).

- [ ] **Step 5: Commit**

```powershell
git add ImGui.Widgets/Dialogs/RenameDialog.cs tests/ImGui.Widgets.Tests/RenameDialogTests.cs
git commit -m "Add the rename dialog wrapper"
```

---

### Task 5: Message dialogs

**Files:**
- Create: `ImGui.Widgets/Dialogs/MessageDialogs.cs` (named to avoid confusion with Hexa's own `MessageBoxes` type)
- Test: `tests/ImGui.Widgets.Tests/MessageDialogTests.cs`

**Interfaces:**
- Consumes: `DialogOutcome`, `MapDialogResult`, `MapMessageBoxResult`, `NotifyDialogShown`.
- Produces: `ImGuiWidgets.DialogMessageBox`, `ImGuiWidgets.ShowMessageBox(...)`, `MessageBoxButtons`.

**Critical upstream fact: `MessageBox` is a `struct`, not a class.**

```csharp
public static MessageBox Show(string title, string message, MessageBoxType type, IUIElement? parent)
{
    MessageBox box = new(title, message, type, parent: parent);
    MessageBoxes.Show(box);   // passes a COPY by value into the registry
    return box;               // returns a DIFFERENT copy
}
```

The registry stores a copy, and `MessageBoxes.Draw()` copies it out again (`MessageBox box = messageBoxes[i]`), mutates the local copy, and never writes it back. So **`MessageBox.Show(...).Result` never observes the user's answer** — the returned struct is a detached copy that nothing ever updates.

The callback *does* work: `MessageBox.Draw()` invokes `Callback?.Invoke(this, Userdata)` on the copy that has the correct `Result` set. **The wrapper must use the callback exclusively and must never read the returned struct.**

A related upstream quirk to document but not fix: because the mutated copy is never written back, the private `shown` latch resets every frame, so `Draw()` re-runs `ImGui.OpenPopup(Title)` and its `SetNextWindowPos(..., Appearing, ...)` every frame. The message box therefore re-centers and cannot be dragged away. That is a real behavioral difference from `ImGuiPopups.MessageOK` and the comparison tab should show it.

- [ ] **Step 1: Write the failing test**

Create `tests/ImGui.Widgets.Tests/MessageDialogTests.cs`:

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using HexaMessageBoxType = Hexa.NET.ImGui.Widgets.MessageBoxType;

/// <summary>
/// Tests the button-set mapping for message dialogs. The dialogs need a live ImGui context.
/// </summary>
[TestClass]
public sealed class MessageDialogTests
{
	[TestMethod]
	public void MapButtons_CoversEveryMember()
	{
		Assert.AreEqual(HexaMessageBoxType.Ok, ImGuiWidgets.MapButtons(MessageBoxButtons.Ok));
		Assert.AreEqual(HexaMessageBoxType.OkCancel, ImGuiWidgets.MapButtons(MessageBoxButtons.OkCancel));
		Assert.AreEqual(HexaMessageBoxType.YesNo, ImGuiWidgets.MapButtons(MessageBoxButtons.YesNo));
		Assert.AreEqual(HexaMessageBoxType.YesNoCancel, ImGuiWidgets.MapButtons(MessageBoxButtons.YesNoCancel));
		Assert.AreEqual(HexaMessageBoxType.YesCancel, ImGuiWidgets.MapButtons(MessageBoxButtons.YesCancel));
	}

	[TestMethod]
	public void MapButtons_UnknownValue_FallsBackToOk() =>
		Assert.AreEqual(HexaMessageBoxType.Ok, ImGuiWidgets.MapButtons((MessageBoxButtons)99));
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet run --project tests/ImGui.Widgets.Tests/ImGui.Widgets.Tests.csproj -p:KtsuSyncStyleConfigFiles=false`
Expected: compile failure — `MessageBoxButtons` and `MapButtons` do not exist.

- [ ] **Step 3: Write the implementation**

Create `ImGui.Widgets/Dialogs/MessageDialogs.cs`:

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using HexaDialogMessageBox = Hexa.NET.ImGui.Widgets.Dialogs.DialogMessageBox;
using HexaDialogMessageBoxType = Hexa.NET.ImGui.Widgets.Dialogs.DialogMessageBoxType;
using HexaMessageBox = Hexa.NET.ImGui.Widgets.MessageBox;
using HexaMessageBoxType = Hexa.NET.ImGui.Widgets.MessageBoxType;

/// <summary>
/// The button set a message dialog offers.
/// </summary>
public enum MessageBoxButtons
{
	/// <summary>
	/// A single OK button.
	/// </summary>
	Ok,

	/// <summary>
	/// OK and Cancel.
	/// </summary>
	OkCancel,

	/// <summary>
	/// Yes and No.
	/// </summary>
	YesNo,

	/// <summary>
	/// Yes, No and Cancel.
	/// </summary>
	YesNoCancel,

	/// <summary>
	/// Yes and Cancel.
	/// </summary>
	YesCancel,
}

public static partial class ImGuiWidgets
{
	/// <summary>
	/// Converts our button set to Hexa's message box type.
	/// </summary>
	/// <param name="buttons">The button set.</param>
	/// <returns>The equivalent Hexa type.</returns>
	internal static HexaMessageBoxType MapButtons(MessageBoxButtons buttons) => buttons switch
	{
		MessageBoxButtons.OkCancel => HexaMessageBoxType.OkCancel,
		MessageBoxButtons.YesNo => HexaMessageBoxType.YesNo,
		MessageBoxButtons.YesNoCancel => HexaMessageBoxType.YesNoCancel,
		MessageBoxButtons.YesCancel => HexaMessageBoxType.YesCancel,
		_ => HexaMessageBoxType.Ok,
	};

	/// <summary>
	/// Converts our button set to Hexa's dialog message box type.
	/// </summary>
	/// <param name="buttons">The button set.</param>
	/// <returns>The equivalent Hexa type.</returns>
	internal static HexaDialogMessageBoxType MapDialogButtons(MessageBoxButtons buttons) => buttons switch
	{
		MessageBoxButtons.OkCancel => HexaDialogMessageBoxType.OkCancel,
		MessageBoxButtons.YesNo => HexaDialogMessageBoxType.YesNo,
		MessageBoxButtons.YesNoCancel => HexaDialogMessageBoxType.YesNoCancel,
		MessageBoxButtons.YesCancel => HexaDialogMessageBoxType.YesCancel,
		_ => HexaDialogMessageBoxType.Ok,
	};

	/// <summary>
	/// Shows a modal message box.
	/// </summary>
	/// <param name="title">The window title, which is also the dialog's identity.</param>
	/// <param name="message">The message body.</param>
	/// <param name="buttons">The button set to offer.</param>
	/// <param name="onClosed">Invoked once, when the box closes.</param>
	/// <exception cref="ArgumentNullException">Any argument is <see langword="null"/>.</exception>
	/// <exception cref="InvalidOperationException">No deferred-drawing pump has ever run.</exception>
	/// <remarks>
	/// The answer arrives through <paramref name="onClosed"/> only. Hexa's <c>MessageBox</c> is a
	/// struct that its registry stores and redraws by value, so the instance returned by its own
	/// <c>Show</c> is a detached copy whose result is never updated. This wrapper deliberately
	/// discards that return value.
	/// </remarks>
	public static void ShowMessageBox(string title, string message, MessageBoxButtons buttons, Action<DialogOutcome> onClosed)
	{
		Ensure.NotNull(title);
		Ensure.NotNull(message);
		Ensure.NotNull(onClosed);
		NotifyDialogShown();

		_ = HexaMessageBox.Show(
			title,
			message,
			userdata: null,
			callback: (box, _) => onClosed(MapMessageBoxResult(box.Result)),
			type: MapButtons(buttons));
	}

	/// <summary>
	/// A message box that behaves like the other dialogs: a movable window rather than a
	/// re-centring modal popup.
	/// </summary>
	/// <remarks>
	/// Requires a per-frame pump: call <see cref="DrawDeferred"/> from your render callback.
	/// </remarks>
	public sealed class DialogMessageBox
	{
		private readonly HexaDialogMessageBox dialog;
		private readonly HexaDialogMessageBoxType type;

		/// <summary>
		/// Initializes a new instance of the <see cref="DialogMessageBox"/> class.
		/// </summary>
		/// <param name="title">The window title, which is also the dialog's identity.</param>
		/// <param name="message">The message body.</param>
		/// <param name="buttons">The button set to offer.</param>
		/// <exception cref="ArgumentNullException"><paramref name="title"/> or <paramref name="message"/> is <see langword="null"/>.</exception>
		public DialogMessageBox(string title, string message, MessageBoxButtons buttons)
		{
			Ensure.NotNull(title);
			Ensure.NotNull(message);

			type = MapDialogButtons(buttons);
			dialog = new HexaDialogMessageBox(title, message, type);
		}

		/// <summary>
		/// Shows the dialog.
		/// </summary>
		/// <param name="onClosed">Invoked once, when the dialog closes.</param>
		/// <exception cref="ArgumentNullException"><paramref name="onClosed"/> is <see langword="null"/>.</exception>
		/// <exception cref="InvalidOperationException">No deferred-drawing pump has ever run.</exception>
		public void Show(Action<DialogOutcome> onClosed)
		{
			Ensure.NotNull(onClosed);
			NotifyDialogShown();

			// The configured type is passed through because Hexa aliases Yes to Ok in DialogResult;
			// it is the only surviving evidence of which button the user pressed.
			dialog.Show((_, result) => onClosed(MapDialogResult(result, type)));
		}
	}
}
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet run --project tests/ImGui.Widgets.Tests/ImGui.Widgets.Tests.csproj -p:KtsuSyncStyleConfigFiles=false`
Expected: PASS, 2 new tests (156 total).

- [ ] **Step 5: Commit**

```powershell
git add ImGui.Widgets/Dialogs/MessageDialogs.cs tests/ImGui.Widgets.Tests/MessageDialogTests.cs
git commit -m "Add the message box wrappers"
```

---

### Task 6: DockedWindow

**Files:**
- Create: `ImGui.Widgets/DockedWindow.cs`

**Interfaces:**
- Consumes: nothing.
- Produces: `public abstract class ImGuiWidgets.DockedWindow` with `protected abstract string Title { get; }`, `protected abstract void DrawContent()`, `public void Show()`, `public void Close()`.

**Upstream facts.** `ImWindow` is `public abstract class ImWindow : IImGuiWindow` with `public abstract string Name { get; }`, `public abstract void DrawContent()`, `public virtual void Show()`. `Show()` calls the `internal` `WidgetManager.Register(this)` — that works because the call happens inside Hexa's own assembly, so subclassing from here is fine. `ImWindow` docks itself with `ImGui.SetNextWindowDockID(WidgetManager.DockSpaceId)`, and only `WidgetManager.Draw()` iterates the registered list.

**This type only renders under `DrawDeferredDocked()`.** Under `DrawDeferred()` it is registered but never drawn.

- [ ] **Step 1: Write the implementation**

There is no pure logic here to test — the type is an abstract adapter whose every member forwards to a base class that needs a live ImGui context. Adding a test that only asserts "a subclass can be constructed" would assert nothing about behavior.

Create `ImGui.Widgets/DockedWindow.cs`:

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using HexaImWindow = Hexa.NET.ImGui.Widgets.ImWindow;

public static partial class ImGuiWidgets
{
	/// <summary>
	/// A window that docks into the dockspace created by <see cref="DrawDeferredDocked"/>.
	/// Subclass it and override <see cref="Title"/> and <see cref="DrawContent"/>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Only drawn by <see cref="DrawDeferredDocked"/>. Under <see cref="DrawDeferred"/> the window
	/// is registered but never rendered, because the dockspace it attaches to does not exist.
	/// </para>
	/// <para>
	/// <see cref="Title"/> is both the window caption and its identity, so two windows sharing a
	/// title will collide.
	/// </para>
	/// </remarks>
	public abstract class DockedWindow : HexaImWindow
	{
		/// <summary>
		/// Gets the window caption, which is also its identity.
		/// </summary>
		protected abstract string Title { get; }

		/// <summary>
		/// Gets the window caption. Forwards to <see cref="Title"/>.
		/// </summary>
		public sealed override string Name => Title;

		/// <summary>
		/// Draws the window's contents. Called once per frame while the window is open.
		/// </summary>
		public abstract override void DrawContent();
	}
}
```

- [ ] **Step 2: Build to verify it compiles**

Run: `dotnet build ImGui.Widgets/ImGui.Widgets.csproj -p:KtsuSyncStyleConfigFiles=false`
Expected: Build succeeded, 0 warnings, 0 errors.

If `ImWindow` turns out to expose additional abstract members beyond `Name` and `DrawContent`, the compiler will name them; implement each by forwarding to a `protected abstract` member on `DockedWindow`, following the `Title`/`Name` pattern above.

- [ ] **Step 3: Run the full test suite**

Run: `dotnet run --project tests/ImGui.Widgets.Tests/ImGui.Widgets.Tests.csproj -p:KtsuSyncStyleConfigFiles=false`
Expected: PASS, 156 total (no new tests).

- [ ] **Step 4: Commit**

```powershell
git add ImGui.Widgets/DockedWindow.cs
git commit -m "Add the DockedWindow base type"
```

---

### Task 7: Comparison demo rows

**Files:**
- Modify: `examples/ImGuiWidgetsDemo/HexaWidgetsDemo.cs`
- Modify: `examples/ImGuiWidgetsDemo/ImGuiWidgetsDemo.cs` (add the pump call to `OnRender`)

**Interfaces:**
- Consumes: every public type from Tasks 2-5.
- Produces: nothing consumed by later tasks.

**The pump call is mandatory.** Without it every dialog in this demo throws `InvalidOperationException` on the first `Show()`. Add to the end of `ImGuiWidgetsDemo.OnRender`:

```csharp
		ImGuiWidgets.DrawDeferred();
```

- [ ] **Step 1: Add the pump call**

In `examples/ImGuiWidgetsDemo/ImGuiWidgetsDemo.cs`, find `private static void OnRender(float dt)` and add `ImGuiWidgets.DrawDeferred();` as its last statement, with this comment:

```csharp
		// Hexa's dialogs are stateful: Show() registers them with a static manager and they are
		// only drawn by this pump. Without it no dialog appears.
		ImGuiWidgets.DrawDeferred();
```

- [ ] **Step 2: Add the dialog comparison section**

In `examples/ImGuiWidgetsDemo/HexaWidgetsDemo.cs`, add these fields alongside the existing shared state:

```csharp
	// Shared state: the ktsu and Hexa dialogs of each row write to the same field.
	private static string sharedChosenFile = "(none)";
	private static string sharedChosenFolder = "(none)";
	private static string sharedSaveTarget = "(none)";
	private static string sharedMessageAnswer = "(none)";
```

Add a `ShowDialogComparison()` method and call it from a new tab item in `Show`:

```csharp
	private static void ShowDialogComparison()
	{
		ImGui.TextWrapped("Dialogs are windows, not inline widgets, so each row is a pair of buttons writing to one shared field. Open ktsu's, then Hexa's, and compare what comes back.");
		ImGui.Separator();

		if (!ImGui.BeginTable("HexaDialogComparison", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
		{
			return;
		}

		ImGui.TableSetupColumn("Dialog", ImGuiTableColumnFlags.WidthFixed, 140f);
		ImGui.TableSetupColumn("ktsu");
		ImGui.TableSetupColumn("Hexa");
		ImGui.TableHeadersRow();

		BeginRow("Open file");
		ImGui.TextUnformatted("FilesystemBrowser");
		ImGui.TableNextColumn();
		if (ImGui.Button("Open##hexaOpenFile"))
		{
			ImGuiWidgets.OpenFileDialog dialog = new();
			dialog.Show(outcome => sharedChosenFile = outcome.Path?.ToString() ?? $"({outcome.Outcome})");
		}

		BeginRow("Pick folder");
		ImGui.TextUnformatted("FilesystemBrowser");
		ImGui.TableNextColumn();
		if (ImGui.Button("Pick##hexaFolder"))
		{
			ImGuiWidgets.OpenFolderDialog dialog = new();
			dialog.Show(outcome => sharedChosenFolder = outcome.Path?.ToString() ?? $"({outcome.Outcome})");
		}

		BeginRow("Save file");
		ImGui.TextUnformatted("FilesystemBrowser");
		ImGui.TableNextColumn();
		if (ImGui.Button("Save##hexaSave"))
		{
			ImGuiWidgets.SaveFileDialog dialog = new();
			dialog.Show(outcome => sharedSaveTarget = outcome.Path?.ToString() ?? $"({outcome.Outcome})");
		}

		BeginRow("Message");
		ImGui.TextUnformatted("MessageOK");
		ImGui.TableNextColumn();
		if (ImGui.Button("Ask##hexaMessage"))
		{
			ImGuiWidgets.ShowMessageBox("Confirm", "Keep both implementations?", MessageBoxButtons.YesNo,
				outcome => sharedMessageAnswer = outcome.ToString());
		}

		ImGui.EndTable();

		ImGui.Separator();
		ImGui.TextUnformatted($"File:    {sharedChosenFile}");
		ImGui.TextUnformatted($"Folder:  {sharedChosenFolder}");
		ImGui.TextUnformatted($"Save to: {sharedSaveTarget}");
		ImGui.TextUnformatted($"Answer:  {sharedMessageAnswer}");

		ImGui.Separator();
		ImGui.TextWrapped("Hexa's file dialogs need a Material Icons font for their navigation bar, and block the UI thread briefly when closing while the async directory scan unwinds. Hexa's message box re-centers itself every frame and cannot be dragged.");
	}
```

Register the tab in `Show`, after the existing "Net New" tab item:

```csharp
			if (ImGui.BeginTabItem("Dialogs"))
			{
				ShowDialogComparison();
				ImGui.EndTabItem();
			}
```

- [ ] **Step 3: Add the net-new dialog gallery**

In `ShowNetNew()`, add:

```csharp
		if (ImGui.CollapsingHeader("Rename and message dialogs"))
		{
			if (ImGui.Button("Rename a file"))
			{
				ImGuiWidgets.RenameDialog dialog = new(
					(AppContext.BaseDirectory.As<AbsoluteDirectoryPath>() / "ktsu.png".As<FileName>()))
				{
					SkipAutomaticMove = true,
				};
				dialog.Show(outcome => renameResult = outcome.Error?.Message ?? outcome.Destination?.ToString() ?? $"({outcome.Outcome})");
			}

			ImGui.SameLine();
			if (ImGui.Button("Dialog message box"))
			{
				ImGuiWidgets.DialogMessageBox box = new("Question", "Movable, unlike the modal message box.", MessageBoxButtons.YesNoCancel);
				box.Show(outcome => renameResult = outcome.ToString());
			}

			ImGui.TextUnformatted(renameResult);
		}
```

Add the backing field alongside the others:

```csharp
	private static string renameResult = "(none)";
```

`SkipAutomaticMove` is set so the demo does not move a real file on disk.

- [ ] **Step 4: Build and run the demo**

Run: `dotnet build examples/ImGuiWidgetsDemo/ImGuiWidgetsDemo.csproj -p:KtsuSyncStyleConfigFiles=false`
Expected: Build succeeded, 0 warnings, 0 errors.

Then run `dotnet run --project examples/ImGuiWidgetsDemo -p:KtsuSyncStyleConfigFiles=false` and confirm in the "Hexa Widgets" pane, "Dialogs" tab: each button opens its dialog, the dialog is dismissable, and the shared field below the table updates with the result.

- [ ] **Step 5: Commit**

```powershell
git add examples/ImGuiWidgetsDemo/HexaWidgetsDemo.cs examples/ImGuiWidgetsDemo/ImGuiWidgetsDemo.cs
git commit -m "Add the dialog comparison rows and wire up the pump"
```

---

### Task 8: DockedWindow demo

**Files:**
- Modify: `examples/ImGuiAppDemo/ImGuiAppDemo.cs`
- Create: `examples/ImGuiAppDemo/Demos/DockedWindowDemo.cs`

**Interfaces:**
- Consumes: `ImGuiWidgets.DockedWindow`, `ImGuiWidgets.DrawDeferredDocked`.

`DockedWindow` is demoed here rather than in `ImGuiWidgetsDemo` because the two pumps cannot both run in one application, and a dockspace is an app-shell concern.

**`ImGuiAppDemo` must reference `ImGui.Widgets`.** Check `examples/ImGuiAppDemo/ImGuiAppDemo.csproj` for `<ProjectReference Include="..\..\ImGui.Widgets\ImGui.Widgets.csproj" />` and add it if absent.

- [ ] **Step 1: Create the demo window**

Create `examples/ImGuiAppDemo/Demos/DockedWindowDemo.cs`:

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Examples.App.Demos;

using Hexa.NET.ImGui;

using ktsu.ImGui.Widgets;

/// <summary>
/// A window that docks into the dockspace created by ImGuiWidgets.DrawDeferredDocked.
/// </summary>
internal sealed class DockedWindowDemo : ImGuiWidgets.DockedWindow
{
	private int clicks;

	/// <inheritdoc/>
	protected override string Title => "Docked Window";

	/// <inheritdoc/>
	public override void DrawContent()
	{
		ImGui.TextWrapped("This window is managed by Hexa's WidgetManager and docks into the dockspace that DrawDeferredDocked creates. Drag its tab to re-dock it.");

		if (ImGui.Button("Click me"))
		{
			clicks++;
		}

		ImGui.TextUnformatted($"Clicks: {clicks}");
	}
}
```

- [ ] **Step 2: Wire it into the demo app**

In `examples/ImGuiAppDemo/ImGuiAppDemo.cs`, add a field:

```csharp
	private static readonly DockedWindowDemo DockedWindow = new();
```

In `OnStart`, show it:

```csharp
		// Registers with Hexa's WidgetManager; only rendered by DrawDeferredDocked below.
		DockedWindow.Show();
```

At the end of `OnRender`, add the docked pump:

```csharp
		// DrawDeferredDocked creates a dockspace over the main viewport and draws registered
		// DockedWindows, plus every dialog, message box and popup. It is mutually exclusive with
		// DrawDeferred, which it already includes.
		ImGuiWidgets.DrawDeferredDocked();
```

- [ ] **Step 3: Build and run**

Run: `dotnet build examples/ImGuiAppDemo/ImGuiAppDemo.csproj -p:KtsuSyncStyleConfigFiles=false`
Expected: Build succeeded, 0 warnings, 0 errors.

Then `dotnet run --project examples/ImGuiAppDemo -p:KtsuSyncStyleConfigFiles=false` and confirm the "Docked Window" appears and its button counts up.

- [ ] **Step 4: Commit**

```powershell
git add examples/ImGuiAppDemo/Demos/DockedWindowDemo.cs examples/ImGuiAppDemo/ImGuiAppDemo.cs examples/ImGuiAppDemo/ImGuiAppDemo.csproj
git commit -m "Demo DockedWindow and the docked pump in ImGuiAppDemo"
```

---

### Task 9: Documentation

**Files:**
- Modify: `CLAUDE.md`
- Modify: `ImGui.Widgets/README.md`

- [ ] **Step 1: Document the pump in CLAUDE.md**

In the `ImGui.Widgets` bullet under "Libraries", append to the widget list: `OpenFileDialog`, `SaveFileDialog`, `OpenFolderDialog`, `RenameDialog`, `DialogMessageBox`, `ShowMessageBox`, `DockedWindow`.

Add a new subsection after "Font Configuration":

```markdown
### Deferred Drawing (dialogs and docked windows)

Hexa-backed dialogs are stateful: `Show()` registers the dialog with a static manager and it is
only drawn when a per-frame pump runs. Call one of these once per frame, at the end of `OnRender`:

- `ImGuiWidgets.DrawDeferred()` — draws every dialog, message box and popup, and advances the
  animation clock. No layout opinion.
- `ImGuiWidgets.DrawDeferredDocked()` — additionally creates a dockspace over the main viewport
  and draws registered `DockedWindow`s. Required for `DockedWindow`.

They are mutually exclusive: `DrawDeferredDocked()` already does everything `DrawDeferred()` does,
so calling both draws every dialog twice.

Showing a dialog when no pump has ever run throws `InvalidOperationException`, because the dialog
would otherwise never appear and the manager's collections would grow for the life of the process.

The file dialogs need a Material Icons font in the atlas for their navigation bar; register one
via `OnConfigureFonts` as described above. `FileDialogBase.Close()` briefly blocks the UI thread
while its async directory scan unwinds.
```

- [ ] **Step 2: Mirror it in the widget README**

Add the same widget names and a shortened form of the pump note to `ImGui.Widgets/README.md`, matching the structure already used there for the Tier 1 widgets.

- [ ] **Step 3: Verify the claims**

Re-read each sentence against the code. In particular confirm that the names listed exist with those exact spellings, and that the pump names match `DeferredDrawing.cs`.

- [ ] **Step 4: Commit**

```powershell
git add CLAUDE.md ImGui.Widgets/README.md
git commit -m "Document the Tier 2 dialogs and the deferred-drawing pumps"
```

---

## Self-review notes

**Spec coverage.** Every spec section maps to a task: the two pumps and diagnostics (Task 2), the
result-enum disagreement (Task 1), the outcome records (Tasks 3-4), `MessageBox`'s struct
semantics (Task 5), `DockedWindow` (Task 6), the comparison demo (Task 7), the `DockedWindow` demo
split (Task 8), documentation (Task 9). The spec's "upstream behavior we surface rather than
hide" section is covered by the doc comments in Tasks 3 and 5 plus the demo note in Task 7.

**`PopupManager.Remove` is deliberately unused.** The spec records it as broken upstream — it
returns when the popup *is* found and otherwise calls `RemoveAt(-1)`. No task calls it; the pumps
only call `PopupManager.Draw()`, which has its own working cleanup path.

**Test counts** assume the suite is at 134 before Task 1 and that Task 2 removes five superseded
tests. Verify the actual starting count rather than trusting these numbers.

**Task 6 has no unit test** by design, and says why: the type is an abstract adapter with no pure
logic, and a construction-only test would assert nothing about behavior.

**Standing rule from Tier 1.** For any callback- or marshaling-shaped upstream API, read every
call site of the callback, not just its signature. The Tier 1 `FlameGraph` Critical was a
guaranteed process crash invisible to both the compiler and the test suite, found only by reading
upstream's four call sites. Tasks 3, 4 and 5 all pass callbacks into Hexa.
