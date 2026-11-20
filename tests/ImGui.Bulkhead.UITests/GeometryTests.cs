// Copyright (c) 2023-2026 ktsu-dev contributors

// ImGui contexts are global and the harness refuses to start while another is live, so every test
// in this assembly must have the process to itself.
[assembly: Microsoft.VisualStudio.TestTools.UnitTesting.DoNotParallelize]

namespace ktsu.ImGui.Bulkhead.UITests;

using System.Numerics;

using ktsu.ImGui.Bulkhead;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>The arithmetic behind the widgets, checked without a frame.</summary>
[TestClass]
public sealed class GeometryTests
{
	[TestMethod]
	public void Elbow_OutlineStaysInsideItsBox()
	{
		Vector2 size = new(200f, 80f);
		foreach (BulkheadCorner corner in Enum.GetValues<BulkheadCorner>())
		{
			IReadOnlyList<Vector2> outline = BulkheadDraw.ElbowOutline(size, corner, 60f, 20f);
			Assert.IsGreaterThan(8, outline.Count, $"{corner} produced too few points for its curves.");
			foreach (Vector2 point in outline)
			{
				Assert.IsTrue(point.X >= -0.01f && point.X <= size.X + 0.01f && point.Y >= -0.01f && point.Y <= size.Y + 0.01f, $"{corner} put {point} outside its box.");
			}
		}
	}

	[TestMethod]
	public void Elbow_MirroredCornersAreMirrorImages()
	{
		Vector2 size = new(200f, 80f);
		IReadOnlyList<Vector2> topLeft = BulkheadDraw.ElbowOutline(size, BulkheadCorner.TopLeft, 60f, 20f);
		IReadOnlyList<Vector2> topRight = BulkheadDraw.ElbowOutline(size, BulkheadCorner.TopRight, 60f, 20f);
		HashSet<(int, int)> mirrored = [.. topLeft.Select(p => ((int)MathF.Round(size.X - p.X), (int)MathF.Round(p.Y)))];
		HashSet<(int, int)> actual = [.. topRight.Select(p => ((int)MathF.Round(p.X), (int)MathF.Round(p.Y)))];
		Assert.IsTrue(mirrored.SetEquals(actual), "The top-right elbow is not the top-left one reflected.");
	}

	[TestMethod]
	public void Elbow_EmptySizeHasNoOutline() =>
		Assert.IsEmpty(BulkheadDraw.ElbowOutline(Vector2.Zero, BulkheadCorner.TopLeft, 10f, 10f));

	[TestMethod]
	[DataRow(0f, 10, 0)]
	[DataRow(1f, 10, 10)]
	[DataRow(0.5f, 10, 5)]
	[DataRow(0.05f, 10, 1)]
	[DataRow(-3f, 10, 0)]
	[DataRow(7f, 10, 10)]
	[DataRow(float.NaN, 10, 0)]
	public void Gauge_FillsRoundedSegments(float fraction, int segments, int expected) =>
		Assert.AreEqual(expected, ImGuiBulkhead.FilledSegments(fraction, segments));

	[TestMethod]
	public void DisplayText_HidesTheIdAndUpperCases() =>
		Assert.AreEqual("ENGAGE", BulkheadDraw.DisplayText("Engage##button"));

	[TestMethod]
	public void Panel_ContentSitsInsideTheFrame()
	{
		Vector2 size = new(600f, 400f);
		PanelLayout layout = PanelLayout.For(size, 20f, new BulkheadPanelOptions());
		Assert.IsGreaterThan(layout.ColumnWidth, layout.ContentOffset.X, "Content overlaps the column.");
		Assert.IsGreaterThan(layout.BarThickness, layout.ContentOffset.Y, "Content overlaps the top bar.");
		Assert.IsLessThanOrEqualTo(size.X, layout.ContentOffset.X + layout.ContentSize.X);
		Assert.IsLessThanOrEqualTo(size.Y - layout.BarThickness, layout.ContentOffset.Y + layout.ContentSize.Y);
		Assert.IsGreaterThan(0f, layout.ColumnHeight);
	}

	[TestMethod]
	public void Panel_TooSmallStillHasNoNegativeSizes()
	{
		PanelLayout layout = PanelLayout.For(new Vector2(30f, 20f), 20f, new BulkheadPanelOptions());
		Assert.IsGreaterThanOrEqualTo(0f, layout.ContentSize.X);
		Assert.IsGreaterThanOrEqualTo(0f, layout.ContentSize.Y);
		Assert.IsGreaterThanOrEqualTo(0f, layout.ColumnHeight);
	}
}
