// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>Drives <see cref="ImGuiWidgets.Rating"/> on its own.</summary>
[TestClass]
public sealed class RatingTests : WidgetTest
{
	private const string Id = "quality";
	private const int StarCount = 5;

	private float value;
	private bool allowHalf;
	private bool readOnly;

	private void Draw() => ImGuiWidgets.Rating(Id, ref value, StarCount, allowHalf, readOnly);

	// Stars are laid out at an even pitch across the marked rectangle, so the center of star n is
	// that fraction of the way across it. Clicking rounds up, which is why the center of a star
	// selects that star rather than the one before it.
	private void ClickStar(int oneBasedStar) =>
		ClickFraction(Id, (oneBasedStar - 0.5f) / StarCount);

	[TestMethod]
	public void Rating_IsDrawnAndMarksItself()
	{
		Start(Draw);

		Assert.IsTrue(IsVisible(Id), "The rating marked no probe item.");
		AssertSomethingWasDrawn("the rating");
	}

	[TestMethod]
	public void Rating_ClickingAStarSetsThatValue()
	{
		Start(Draw);

		ClickStar(3);

		Assert.AreEqual(3f, value, "Clicking the third star did not set a rating of three.");
	}

	[TestMethod]
	public void Rating_ClickingTheLastStarSetsTheMaximum()
	{
		Start(Draw);

		ClickStar(StarCount);

		Assert.AreEqual(StarCount, value);
	}

	[TestMethod]
	public void Rating_AllowingHalves_SnapsToTheHalfStar()
	{
		allowHalf = true;
		Start(Draw);

		// The left half of the second star is a half-star selection when halves are allowed.
		ClickFraction(Id, (1f + 0.2f) / StarCount);

		Assert.AreEqual(1.5f, value, "A click on the left half of the second star did not snap to 1.5.");
	}

	[TestMethod]
	public void Rating_ReadOnly_IgnoresClicks()
	{
		value = 2f;
		readOnly = true;
		Start(Draw);

		ClickStar(5);

		Assert.AreEqual(2f, value, "A read-only rating was changed by a click.");
	}

	[TestMethod]
	public void Rating_DrawsMoreFillAsTheValueRises()
	{
		value = 0f;
		Start(Draw);
		MoveAway();
		byte[] empty = Snapshot();

		value = StarCount;
		Step(2);
		MoveAway();

		Assert.IsTrue(PixelsChangedSince(empty) > 0, "A full rating drew the same as an empty one.");
	}
}
