// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using System.Collections.Generic;

using Hexa.NET.ImGui;

using ktsu.ImGui.App.Testing;
using ktsu.ImGui.Probes;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Drives <see cref="ImGuiWidgets.DiffView(string, IReadOnlyList{ImGuiWidgets.DiffHunk}, ISet{int}, ImGuiWidgets.DiffViewOptions?)"/>
/// alone: selecting hunks, collapsing them, the two layouts, and reaching the end of a line wider
/// than the hunk's box.
/// </summary>
[TestClass]
public sealed class DiffViewTests : WidgetTest
{
	private static int nextLabel;

	// The widget keeps each hunk's collapse in a table that outlives a harness, keyed by label, so
	// every test draws under a label of its own rather than inheriting the last test's collapses.
	private readonly string Label = $"##diff{System.Threading.Interlocked.Increment(ref nextLabel)}";

	private readonly HashSet<int> selection = [];

	private ImGuiWidgets.DiffViewOptions options = new();

	// The scroll state of whatever window the pointer is over, read at the end of each frame. The
	// hunk tables are child windows the widget creates for itself, so this is how a test reaches them.
	private float hoveredScrollX;
	private float hoveredScrollMaxX;

	private static ImGuiWidgets.DiffLine Line(ImGuiWidgets.DiffLineKind kind, string text, int? oldNumber, int? newNumber) =>
		new() { Kind = kind, Text = text, OldNumber = oldNumber, NewNumber = newNumber };

	private static List<ImGuiWidgets.DiffHunk> ShortHunks() =>
	[
		new()
		{
			Heading = "first",
			Lines =
			[
				Line(ImGuiWidgets.DiffLineKind.Context, "int a = 1;", 1, 1),
				Line(ImGuiWidgets.DiffLineKind.Removed, "int b = 2;", 2, null),
				Line(ImGuiWidgets.DiffLineKind.Added, "int b = 3;", null, 2),
			],
		},
		new()
		{
			Heading = "second",
			Lines = [Line(ImGuiWidgets.DiffLineKind.Added, "return a + b;", null, 9)],
		},
	];

	private static List<ImGuiWidgets.DiffHunk> LongLineHunks() =>
	[
		new()
		{
			Heading = "long",
			Lines =
			[
				Line(ImGuiWidgets.DiffLineKind.Removed, new string('x', 400) + "END", 1, null),
				Line(ImGuiWidgets.DiffLineKind.Added, new string('y', 400) + "END", null, 1),
			],
		},
	];

	/// <summary>Draws the widget over a fixed list of hunks, then a button whose position shows how tall it was.</summary>
	private void StartWith(IReadOnlyList<ImGuiWidgets.DiffHunk> hunks) =>
		StartRebuilding(() => hunks);

	/// <summary>Draws the widget over hunks the callback builds afresh each frame, as an immediate-mode caller does.</summary>
	private void StartRebuilding(System.Func<IReadOnlyList<ImGuiWidgets.DiffHunk>> hunks)
	{
		Start(() =>
		{
			_ = ImGuiWidgets.DiffView(Label, hunks(), selection, options);
			_ = ImGui.Button("after");
			ImGuiProbes.MarkItem("after");

			ImGuiWindowPtr hovered = ImGui.GetCurrentContext().HoveredWindow;
			if (!hovered.IsNull)
			{
				hoveredScrollX = hovered.Scroll.X;
				hoveredScrollMaxX = hovered.ScrollMax.X;
			}
		});

		// A hunk's table is a child window, sized and placed from the frame before, so the layout
		// below it takes a few more frames than a plain widget to come to rest.
		Step(3);
	}

	/// <summary>Points at the first hunk's lines, just under its heading, and lets hover settle.</summary>
	private void PointAtFirstHunkLines()
	{
		Rectangle heading = RectOf($"{Label}/[0]/heading");
		Harness.Mouse.MoveTo(heading.MinX + 60, heading.MaxY + 10);
		Step(2);
	}

	[TestMethod]
	public void ClickingAHunkCheckbox_SelectsItAndClickingAgainDeselectsIt()
	{
		StartWith(ShortHunks());

		Click($"{Label}/[1]/select");
		Assert.IsTrue(selection.SetEquals([1]), "Ticking the second hunk's checkbox did not select it.");

		Click($"{Label}/[1]/select");
		Assert.IsEmpty(selection, "Ticking it again did not deselect it.");
	}

	[TestMethod]
	public void ClickingAHeading_CollapsesTheHunksLines()
	{
		StartWith(ShortHunks());
		float expanded = RectOf("after").MinY;

		Click($"{Label}/[0]/heading");
		Step();

		Assert.IsLessThan(expanded, RectOf("after").MinY, "Collapsing the first hunk did not take its lines out of the layout.");

		Click($"{Label}/[0]/heading");
		Step();

		Assert.AreEqual(expanded, RectOf("after").MinY, "Expanding it again did not bring its lines back.");
	}

	[TestMethod]
	public void ACollapsedHunk_StaysCollapsed_WhenTheCallerRebuildsItsHunksEveryFrame()
	{
		StartRebuilding(ShortHunks);
		float expanded = RectOf("after").MinY;

		Click($"{Label}/[0]/heading");
		Step(3);

		Assert.IsLessThan(
			expanded,
			RectOf("after").MinY,
			"A hunk handed over as a new instance with the same content each frame lost its collapse.");
	}

	[TestMethod]
	public void SideBySide_PairsARemovalWithItsAddition_WhereUnifiedGivesEachARow()
	{
		StartWith(ShortHunks());
		Rectangle heading = RectOf($"{Label}/[1]/heading");
		float unified = heading.MinY;

		options = new() { Mode = ImGuiWidgets.DiffViewMode.SideBySide };
		Step(3);

		// The first hunk is a context line then a one-for-one change: three rows unified, two paired.
		Assert.IsLessThan(
			unified,
			RectOf($"{Label}/[1]/heading").MinY,
			"Side by side should put the removal and its addition on one row, so the first hunk is shorter.");
	}

	[TestMethod]
	public void SideBySide_DrawsTheNewSideInTheRightHalf()
	{
		StartWith(ShortHunks());
		options = new() { Mode = ImGuiWidgets.DiffViewMode.SideBySide };
		Step(2);
		Rectangle heading = RectOf($"{Label}/[0]/heading");
		CapturedFrame paired = Harness.Capture();

		options = new();
		Step(2);
		CapturedFrame unified = Harness.Capture();

		int middle = heading.MinX + (heading.Width / 2);
		Rectangle rightHalf = new(middle + 10, heading.MaxY + 2, heading.MaxX - 10, heading.MaxY + 60);

		Assert.IsTrue(HasText(paired, rightHalf), "Side by side drew no text in the right half, where the new side belongs.");
		Assert.IsFalse(HasText(unified, rightHalf), "Unified drew text in the right half, so it is not the single column it should be.");
	}

	/// <summary>Whether any pixel in the rectangle is as bright as the default theme's text.</summary>
	private static bool HasText(CapturedFrame frame, Rectangle area)
	{
		for (int y = area.MinY; y < area.MaxY; y++)
		{
			for (int x = area.MinX; x < area.MaxX; x++)
			{
				Rgba32 pixel = frame.GetPixel(x, y);
				if (pixel.R > 150 && pixel.G > 150 && pixel.B > 150)
				{
					return true;
				}
			}
		}

		return false;
	}

	[TestMethod]
	public void TheWidget_MarksTheRegionItDrew()
	{
		StartWith(ShortHunks());

		Rectangle widget = RectOf(Label);
		Rectangle second = RectOf($"{Label}/[1]/heading");

		Assert.IsLessThanOrEqualTo(second.MinY, widget.MinY, "The widget's region does not start above its hunks.");
		Assert.IsGreaterThanOrEqualTo(second.MaxY, widget.MaxY, "The widget's region does not reach its last hunk.");
	}

	[TestMethod]
	[DataRow(ImGuiWidgets.DiffViewMode.Unified)]
	[DataRow(ImGuiWidgets.DiffViewMode.SideBySide)]
	public void ALineWiderThanTheBox_CanBeScrolledTo(ImGuiWidgets.DiffViewMode mode)
	{
		options = new() { Mode = mode };
		StartWith(LongLineHunks());
		PointAtFirstHunkLines();

		Assert.IsGreaterThan(0f, hoveredScrollMaxX, "A hunk with a line wider than its box has nothing to scroll sideways to.");

		for (int i = 0; i < 40; i++)
		{
			ImGui.GetIO().AddMouseWheelEvent(-20f, 0f);
			Step();
		}

		Assert.AreEqual(
			hoveredScrollMaxX,
			hoveredScrollX,
			1f,
			"Scrolling sideways stopped short of the end of the line.");
	}

	[TestMethod]
	public void ShortLines_LeaveNothingToScrollSideways()
	{
		StartWith(ShortHunks());
		PointAtFirstHunkLines();

		Assert.AreEqual(0f, hoveredScrollMaxX, "Lines that fit the box still made the table wider than it, so short lines no longer fill it.");
	}
}
