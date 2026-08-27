// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using System.Collections.Generic;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.App.Testing;
using ktsu.Semantics.Color;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>Drives <see cref="ImGuiWidgets.Sequencer"/> on its own, against a source the test owns.</summary>
[TestClass]
public sealed class SequencerTests : WidgetTest
{
	// The timeline is wide, so this suite gets a wider viewport than the default.
	private static readonly HarnessOptions WideViewport = new() { Width = 900, Height = 420 };

	/// <summary>A two-clip timeline that records every edit the sequencer sends it.</summary>
	private sealed class TestSource : ImGuiWidgets.SequenceSource
	{
		private readonly List<SequenceItem> items =
		[
			new SequenceItem(0, 40, 0, new Srgb(0.9f, 0.4f, 0.2f)),
			new SequenceItem(50, 90, 1, new Srgb(0.2f, 0.6f, 0.9f)),
		];

		public List<(int Index, int Start, int End)> RangeEdits { get; } = [];

		public List<int> Added { get; } = [];

		public List<int> Deleted { get; } = [];

		public override int FrameMin => 0;

		public override int FrameMax => 100;

		public override int ItemCount => items.Count;

		public override SequenceItem GetItem(int index) => items[index];

		public override void SetItemRange(int index, int start, int endFrame)
		{
			RangeEdits.Add((index, start, endFrame));
			items[index] = items[index] with { Start = start, End = endFrame };
		}

		public override string GetItemLabel(int index) => index == 0 ? "Intro" : "Body";

		public override IReadOnlyList<string> ItemTypeNames => ["Video", "Audio"];

		public override void AddItem(int typeIndex)
		{
			Added.Add(typeIndex);
			items.Add(new SequenceItem(0, 10, typeIndex, new Srgb(0.5f, 0.5f, 0.5f)));
		}

		public override void DeleteItem(int index)
		{
			Deleted.Add(index);
			items.RemoveAt(index);
		}
	}

	private const string Name = "sequencer";

	private TestSource source = null!;
	private int currentFrame;
	private bool expanded = true;
	private int selectedEntry = -1;
	private int firstFrame;
	private bool changed;

	private void Draw()
	{
		Vector2 origin = ImGui.GetCursorScreenPos();
		changed |= ImGuiWidgets.Sequencer(source, ref currentFrame, ref expanded, ref selectedEntry, ref firstFrame);
		MarkSpan(Name, origin);
	}

	[TestMethod]
	public void Sequencer_DrawsItsTimeline()
	{
		source = new TestSource();
		Start(Draw, WideViewport);

		Assert.IsTrue(IsVisible(Name), "The sequencer drew nothing.");
		AssertSomethingWasDrawn("the sequencer");
	}

	[TestMethod]
	public void Sequencer_ShowsTheClipsItsSourceReports()
	{
		source = new TestSource();
		Start(Draw, WideViewport);
		MoveAway();
		byte[] twoClips = Snapshot();

		source.SetItemRange(1, 60, 100);
		Step(2);
		MoveAway();

		Assert.IsTrue(PixelsChangedSince(twoClips) > 0, "Moving a clip in the source redrew the same timeline.");
	}

	[TestMethod]
	public void Sequencer_Collapsed_DrawsLessThanExpanded()
	{
		source = new TestSource();
		expanded = true;
		Start(Draw, WideViewport);
		MoveAway();
		byte[] open = Snapshot();

		expanded = false;
		Step(2);
		MoveAway();

		Assert.IsTrue(PixelsChangedSince(open) > 0, "A collapsed sequencer drew the same as an expanded one.");
	}

	[TestMethod]
	public void Sequencer_ClickingAClipSelectsIt()
	{
		source = new TestSource();
		selectedEntry = -1;
		Start(Draw, WideViewport);

		// The clip rows sit just under the header, near the top of the timeline. The first clip
		// runs from frame 0 to 40 of a 0..100 timeline, so it covers the left third of its row.
		ClickFraction(Name, 0.3f, 0.1f);

		Assert.AreEqual(0, selectedEntry, "Clicking the first clip did not select it.");
	}

	[TestMethod]
	public void Sequencer_ClickingTheOtherClipSelectsThatOne()
	{
		source = new TestSource();
		selectedEntry = -1;
		Start(Draw, WideViewport);

		// The second clip runs from frame 50 to 90, so it covers the right of the row below.
		ClickFraction(Name, 0.9f, 0.15f);

		Assert.AreEqual(1, selectedEntry, "Clicking the second clip did not select it.");
	}

	[TestMethod]
	public void Sequencer_DraggingAClipEditsItsRange()
	{
		source = new TestSource();
		Start(Draw, WideViewport);

		DragAcross(Name, 0.3f, 0.45f, 0.1f);

		Assert.IsTrue(source.RangeEdits.Count > 0, "Dragging a clip sent no range edit to the source.");
	}

	[TestMethod]
	public void Sequencer_LeftAlone_EditsNothing()
	{
		source = new TestSource();
		Start(Draw, WideViewport);
		Step(5);

		Assert.AreEqual(0, source.RangeEdits.Count, "The sequencer edited a clip with no input.");
		Assert.AreEqual(0, source.Added.Count, "The sequencer added a clip with no input.");
		Assert.AreEqual(0, source.Deleted.Count, "The sequencer deleted a clip with no input.");
		Assert.IsFalse(changed, "The sequencer reported a change with no input.");
	}

	[TestMethod]
	public void Sequencer_WithoutEditFeatures_LeavesTheClipsAlone()
	{
		source = new TestSource();

		Start(
			() =>
			{
				Vector2 origin = ImGui.GetCursorScreenPos();
				changed |= ImGuiWidgets.Sequencer(source, ref currentFrame, ref expanded, ref selectedEntry, ref firstFrame, SequencerFeatures.None);
				MarkSpan(Name, origin);
			},
			WideViewport);

		DragAcross(Name, 0.3f, 0.5f, 0.1f);

		Assert.AreEqual(0, source.RangeEdits.Count, "A read-only sequencer still edited a clip.");
	}

	[TestMethod]
	public void Sequencer_AnEmptyTimelineStillDraws()
	{
		source = new TestSource();
		source.DeleteItem(1);
		source.DeleteItem(0);
		Start(Draw, WideViewport);

		Assert.IsTrue(IsVisible(Name), "An empty sequencer drew nothing.");
		Assert.AreEqual(0, source.ItemCount, "The test source was not emptied.");
	}
}
