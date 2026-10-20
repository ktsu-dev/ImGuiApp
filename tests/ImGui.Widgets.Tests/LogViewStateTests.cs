// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using LogViewState = ImGuiWidgets.LogViewState;

/// <summary>
/// Tests the filtering, collapsing, ring-buffer shifting, selection, follow and copy rules behind LogView.
/// All pure: no ImGui context required.
/// </summary>
[TestClass]
public class LogViewStateTests
{
	private static readonly DateTime Start = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);

	private readonly List<LogEntry> entries = [];
	private readonly LogViewState state = new();

	private LogEntry Entry(LogViewLevel level, string message, string? category = null) =>
		new(level, message, Start.AddSeconds(entries.Count), category);

	private void Add(LogViewLevel level, string message, string? category = null) =>
		entries.Add(Entry(level, message, category));

	private void AddInfo(params string[] messages)
	{
		foreach (string message in messages)
		{
			Add(LogViewLevel.Information, message);
		}
	}

	private static void AssertSequence(IEnumerable<int> actual, params int[] expected) =>
		CollectionAssert.AreEqual(expected, actual.ToArray());

	private int[] VisibleSources() =>
		[.. Enumerable.Range(0, state.VisibleCount).Select(state.SourceIndexOf)];

	[TestMethod]
	public void NewState_ShowsEverything()
	{
		Add(LogViewLevel.Trace, "t");
		Add(LogViewLevel.Debug, "d");
		Add(LogViewLevel.Information, "i");
		Add(LogViewLevel.Warning, "w");
		Add(LogViewLevel.Error, "e");

		state.Update(entries);

		Assert.AreEqual(5, state.VisibleCount);
		Assert.IsTrue(state.IsFollowing);
	}

	[TestMethod]
	public void DisabledLevel_IsHidden()
	{
		Add(LogViewLevel.Information, "a");
		Add(LogViewLevel.Debug, "b");
		Add(LogViewLevel.Warning, "c");
		Add(LogViewLevel.Debug, "d");
		Add(LogViewLevel.Error, "e");

		state.SetLevelEnabled(LogViewLevel.Debug, false);
		state.Update(entries);

		Assert.AreEqual(3, state.VisibleCount);
		Assert.IsFalse(state.IsLevelEnabled(LogViewLevel.Debug));
		Assert.IsTrue(VisibleSources().All(source => entries[source].Level != LogViewLevel.Debug));
	}

	[TestMethod]
	public void Filter_MatchesTheMessage()
	{
		AddInfo("load a", "save b", "load c");

		state.FilterText = "load*";
		state.Update(entries);

		AssertSequence(VisibleSources(), 0, 2);
	}

	[TestMethod]
	public void AppendingEntries_FiltersOnlyTheNewOnes()
	{
		AddInfo("a", "b", "c");
		state.Update(entries);

		AddInfo("d", "e");
		state.Update(entries);

		Assert.AreEqual(5, state.VisibleCount);
		Assert.AreEqual(4, state.SourceIndexOf(4));
	}

	[TestMethod]
	public void ShrinkingTheLog_Rebuilds()
	{
		AddInfo("a", "b", "c", "d", "e");
		state.Update(entries);

		entries.Clear();
		AddInfo("f");
		state.Update(entries);

		Assert.AreEqual(1, state.VisibleCount);
	}

	[TestMethod]
	public void ChangingTheFilter_Rebuilds()
	{
		AddInfo("load a", "save b", "load c");
		state.Update(entries);
		Assert.AreEqual(3, state.VisibleCount);

		state.FilterText = "save*";
		state.Update(entries);

		AssertSequence(VisibleSources(), 1);
	}

	[TestMethod]
	public void CollapseRepeats_MergesConsecutiveEqualLines()
	{
		AddInfo("a", "a", "a", "b", "a");

		state.CollapseRepeats = true;
		state.Update(entries);

		Assert.AreEqual(3, state.VisibleCount);
		Assert.AreEqual(3, state.RepeatCountOf(0));
		Assert.AreEqual(1, state.RepeatCountOf(1));
		Assert.AreEqual(1, state.RepeatCountOf(2));
	}

	[TestMethod]
	public void CollapseRepeats_DoesNotMergeAcrossAHiddenLine()
	{
		Add(LogViewLevel.Information, "a");
		Add(LogViewLevel.Debug, "x");
		Add(LogViewLevel.Information, "a");

		state.CollapseRepeats = true;
		state.SetLevelEnabled(LogViewLevel.Debug, false);
		state.Update(entries);

		Assert.AreEqual(2, state.VisibleCount);
		Assert.AreEqual(1, state.RepeatCountOf(0));
		Assert.AreEqual(1, state.RepeatCountOf(1));
	}

	[TestMethod]
	public void CollapseRepeats_DoesNotMergeDifferentLevels()
	{
		Add(LogViewLevel.Information, "a");
		Add(LogViewLevel.Warning, "a");

		state.CollapseRepeats = true;
		state.Update(entries);

		Assert.AreEqual(2, state.VisibleCount);
	}

	[TestMethod]
	public void CollapseRepeats_ContinuesAcrossAnAppend()
	{
		state.CollapseRepeats = true;
		AddInfo("a", "a");
		state.Update(entries);

		AddInfo("a");
		state.Update(entries);

		Assert.AreEqual(1, state.VisibleCount);
		Assert.AreEqual(3, state.RepeatCountOf(0));
	}

	[TestMethod]
	public void EntriesRemovedFromStart_ShiftsRowsAndSelection()
	{
		AddInfo("a", "b", "c", "d", "e");
		state.Update(entries);
		state.Click(3, shift: false, ctrl: false);

		entries.RemoveRange(0, 2);
		state.EntriesRemovedFromStart(2);
		state.Update(entries);

		AssertSequence(state.SelectedSourceIndices, 1);
		Assert.AreEqual(3, state.VisibleCount);
		AssertSequence(VisibleSources(), 0, 1, 2);
	}

	[TestMethod]
	public void EntriesRemovedFromStart_KeepsTheSurvivingTailOfACollapsedRow()
	{
		state.CollapseRepeats = true;
		AddInfo("a", "a", "a", "b");
		state.Update(entries);

		entries.RemoveRange(0, 2);
		state.EntriesRemovedFromStart(2);
		state.Update(entries);

		Assert.AreEqual(2, state.VisibleCount);
		Assert.AreEqual(0, state.SourceIndexOf(0));
		Assert.AreEqual(1, state.RepeatCountOf(0));
		Assert.AreEqual(1, state.SourceIndexOf(1));
	}

	[TestMethod]
	public void EntriesRemovedFromStart_IgnoresZeroAndNegative()
	{
		AddInfo("a", "b");
		state.Update(entries);

		state.EntriesRemovedFromStart(0);
		state.EntriesRemovedFromStart(-3);

		AssertSequence(VisibleSources(), 0, 1);
	}

	[TestMethod]
	public void Invalidate_RebuildsAnEditInPlace()
	{
		AddInfo("load a", "save b");
		state.FilterText = "load*";
		state.Update(entries);
		Assert.AreEqual(1, state.VisibleCount);

		entries[1] = entries[1] with { Message = "load b" };
		state.Update(entries);
		Assert.AreEqual(1, state.VisibleCount, "Without Invalidate the index cannot see an edit that keeps the count.");

		state.Invalidate();
		state.Update(entries);
		Assert.AreEqual(2, state.VisibleCount);
	}

	[TestMethod]
	public void Click_SelectsOne_CtrlToggles_ShiftExtends()
	{
		AddInfo("a", "b", "c", "d", "e");
		state.Update(entries);

		state.Click(1, shift: false, ctrl: false);
		AssertSequence(state.SelectedSourceIndices, 1);

		state.Click(3, shift: false, ctrl: true);
		AssertSequence(state.SelectedSourceIndices, 1, 3);

		state.Click(1, shift: false, ctrl: true);
		AssertSequence(state.SelectedSourceIndices, 3);

		state.Click(4, shift: true, ctrl: false);
		AssertSequence(state.SelectedSourceIndices, 1, 2, 3, 4);
		Assert.IsTrue(state.IsRowSelected(2));
		Assert.IsFalse(state.IsRowSelected(0));
	}

	[TestMethod]
	public void ShiftClick_WithNoAnchor_BehavesAsAPlainClick()
	{
		AddInfo("a", "b", "c");
		state.Update(entries);

		state.Click(2, shift: true, ctrl: false);

		AssertSequence(state.SelectedSourceIndices, 2);
	}

	[TestMethod]
	public void Click_OutOfRange_DoesNothing()
	{
		AddInfo("a");
		state.Update(entries);

		state.Click(5, shift: false, ctrl: false);
		state.Click(-1, shift: false, ctrl: false);

		Assert.AreEqual(0, state.SelectedSourceIndices.Count);
	}

	[TestMethod]
	public void SelectAll_SelectsEveryVisibleRow()
	{
		AddInfo("a", "b", "c", "d", "e");
		state.Update(entries);

		state.SelectAll();

		AssertSequence(state.SelectedSourceIndices, 0, 1, 2, 3, 4);

		state.ClearSelection();
		Assert.AreEqual(0, state.SelectedSourceIndices.Count);
	}

	[TestMethod]
	public void Filtering_DropsSelectionThatIsNoLongerVisible()
	{
		AddInfo("load a", "load b", "save c");
		state.Update(entries);
		state.Click(2, shift: false, ctrl: false);

		state.FilterText = "load*";
		state.Update(entries);

		Assert.AreEqual(0, state.SelectedSourceIndices.Count);
	}

	[TestMethod]
	public void ScrollingUp_StopsFollowing()
	{
		Assert.IsTrue(state.UpdateFollow(100f, 100f, 16f));

		state.UpdateFollow(60f, 100f, 16f);

		Assert.IsFalse(state.IsFollowing);
	}

	[TestMethod]
	public void GrowingContent_DoesNotStopFollowing()
	{
		state.UpdateFollow(100f, 100f, 16f);

		Assert.IsTrue(state.UpdateFollow(100f, 140f, 16f));
	}

	[TestMethod]
	public void ReachingTheBottom_ResumesFollowing()
	{
		state.IsFollowing = false;

		Assert.IsTrue(state.UpdateFollow(135f, 140f, 16f));
	}

	[TestMethod]
	public void BuildCopyText_FormatsSelectedRows()
	{
		entries.Add(new LogEntry(LogViewLevel.Information, "boot", new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Unspecified)));
		entries.Add(new LogEntry(LogViewLevel.Warning, "disk low", new DateTime(2026, 1, 1, 12, 0, 1, 250, DateTimeKind.Unspecified), "io"));
		state.Update(entries);
		state.Click(1, shift: false, ctrl: false);

		Assert.AreEqual("12:00:01.250 [Warning] io: disk low", state.BuildCopyText(entries, selectionOnly: true));
	}

	[TestMethod]
	public void BuildCopyText_MarksRepeatsAndOmitsAnEmptyCategory()
	{
		for (int i = 0; i < 3; i++)
		{
			entries.Add(new LogEntry(LogViewLevel.Information, "tick", Start));
		}

		state.CollapseRepeats = true;
		state.Update(entries);
		state.Click(0, shift: false, ctrl: false);

		Assert.AreEqual("00:00:00.000 [Information] tick (×3)", state.BuildCopyText(entries, selectionOnly: true));
	}

	[TestMethod]
	public void BuildCopyText_WithNoSelection_CopiesEveryVisibleRow()
	{
		AddInfo("a", "b", "c");
		state.Update(entries);

		string text = state.BuildCopyText(entries, selectionOnly: true);

		Assert.AreEqual(3, text.Split('\n').Length);
		Assert.AreEqual(
			"00:00:00.000 [Information] a\n00:00:01.000 [Information] b\n00:00:02.000 [Information] c",
			text);
	}

	[TestMethod]
	public void NullMessage_IsTreatedAsEmpty()
	{
		entries.Add(new LogEntry(LogViewLevel.Information, null!, Start));
		state.Update(entries);

		Assert.AreEqual(1, state.VisibleCount);
		Assert.AreEqual("00:00:00.000 [Information] ", state.BuildCopyText(entries, selectionOnly: false));

		state.FilterText = "x*";
		state.Update(entries);

		Assert.AreEqual(0, state.VisibleCount);
	}
}
