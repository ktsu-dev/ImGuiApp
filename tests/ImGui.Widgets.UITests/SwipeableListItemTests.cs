// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using Hexa.NET.ImGui;

using ktsu.ImGui.App.Testing;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>Drives <see cref="ImGuiWidgets.SwipeableListItem"/> on its own.</summary>
[TestClass]
public sealed class SwipeableListItemTests : WidgetTest
{
	private const string Id = "mail";
	private const float RowHeight = 48f;

	private static readonly SwipeAction[] Leading = [new("Read")];
	private static readonly SwipeAction[] Trailing = [new("Delete", Destructive: true), new("Archive")];

	private SwipeAction[] leading = Leading;
	private SwipeActionResult? chosen;

	private void Draw()
	{
		SwipeActionResult? result = ImGuiWidgets.SwipeableListItem(
			Id,
			RowHeight,
			_ => ImGui.TextUnformatted("Lunch on Friday?"),
			leading,
			Trailing);

		chosen ??= result;
	}

	// Drags the row horizontally by a number of pixels. Many small steps keep the release slow, so it
	// is judged by how far it went; few large ones make it a flick.
	private void DragRow(float pixels, int steps)
	{
		Rectangle rect = RectOf(Id);
		float x = rect.MinX + (rect.Width * 0.5f);
		float y = rect.MinY + (rect.Height * 0.5f);
		Harness.Mouse.Drag(x, y, x + pixels, y, steps);
		Step(40);
	}

	[TestMethod]
	public void SwipeableListItem_DrawsItsContent()
	{
		Start(Draw);

		Assert.IsTrue(IsVisible(Id), "The row was not drawn.");
		Assert.AreEqual(RowHeight, RectOf(Id).Height, 1f);
		AssertSomethingWasDrawn("the row");
		Assert.IsFalse(IsVisible("mail/Delete"), "A closed row showed its actions.");
	}

	[TestMethod]
	public void SwipeableListItem_SlowDragLeft_OpensTheTrailingActions()
	{
		Start(Draw);

		DragRow(-110f, 60);

		Assert.IsTrue(IsVisible("mail/Delete"), "Dragging left past half the actions did not open them.");
		Assert.IsTrue(IsVisible("mail/Archive"));
		Assert.IsNull(chosen, "Opening the row chose an action.");
	}

	[TestMethod]
	public void SwipeableListItem_ShortDrag_SnapsBackClosed()
	{
		Start(Draw);

		DragRow(-30f, 60);

		Assert.IsFalse(IsVisible("mail/Delete"), "A short drag left the row open.");
	}

	[TestMethod]
	public void SwipeableListItem_ClickingARevealedAction_ReportsIt()
	{
		Start(Draw);
		DragRow(-110f, 60);

		Click("mail/Archive");

		Assert.AreEqual(new SwipeActionResult(SwipeEdge.Trailing, 1), chosen);
	}

	[TestMethod]
	public void SwipeableListItem_ChoosingAnAction_ClosesTheRow()
	{
		Start(Draw);
		DragRow(-110f, 60);

		Click("mail/Delete");
		Step(40);

		Assert.AreEqual(new SwipeActionResult(SwipeEdge.Trailing, 0), chosen);
		Assert.IsFalse(IsVisible("mail/Delete"), "The row stayed open after an action was chosen.");
	}

	[TestMethod]
	public void SwipeableListItem_FullSwipe_CommitsTheOutermostAction()
	{
		Start(Draw);
		float width = RectOf(Id).Width;

		DragRow(-width * 0.75f, 60);

		Assert.AreEqual(new SwipeActionResult(SwipeEdge.Trailing, 0), chosen, "A long drag did not commit the outermost action.");
	}

	[TestMethod]
	public void SwipeableListItem_DragRight_OpensTheLeadingAction()
	{
		Start(Draw);

		DragRow(60f, 60);

		Assert.IsTrue(IsVisible("mail/Read"), "Dragging right did not reveal the leading action.");
		Assert.IsFalse(IsVisible("mail/Delete"));
	}

	[TestMethod]
	public void SwipeableListItem_WithNoLeadingActions_DoesNotMoveRight()
	{
		leading = [];
		Start(Draw);
		MoveAway();
		byte[] before = Snapshot();

		DragRow(80f, 60);
		MoveAway();

		Assert.AreEqual(0, PixelsChangedSince(before), "A row with nothing on its leading side moved right.");
	}

	[TestMethod]
	public void SwipeableListItem_TapOnAnOpenRow_ClosesIt()
	{
		Start(Draw);
		DragRow(-110f, 60);
		Assert.IsTrue(IsVisible("mail/Delete"));

		ClickFraction(Id, 0.5f);
		Step(40);

		Assert.IsFalse(IsVisible("mail/Delete"), "Tapping the open row did not close it.");
		Assert.IsNull(chosen);
	}

	[TestMethod]
	public void SwipeableListItem_VerticalDrag_LeavesTheRowAlone()
	{
		Start(Draw);
		Rectangle rect = RectOf(Id);
		float x = rect.MinX + (rect.Width * 0.5f);

		Harness.Mouse.Drag(x, rect.MinY + 4f, x - 20f, rect.MinY + 90f, 30);
		Step(40);

		Assert.IsFalse(IsVisible("mail/Delete"), "A mostly vertical drag opened the row.");
	}

	[TestMethod]
	public void SwipeableListItem_ActionsShowAHand()
	{
		Start(Draw);
		DragRow(-110f, 60);

		Hover("mail/Archive");

		Assert.AreEqual(ImGuiMouseCursor.Hand, Harness.MouseCursor);
	}
}
