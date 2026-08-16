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
