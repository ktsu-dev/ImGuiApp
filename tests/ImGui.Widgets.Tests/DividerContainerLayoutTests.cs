// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>The arithmetic behind <see cref="ImGuiWidgets.DividerContainer"/>'s layout (#724).</summary>
[TestClass]
public sealed class DividerContainerLayoutTests
{
	private const float Gap = 8f;

	private static void AssertEven(DividerZoneSpan[] spans, float length)
	{
		float leading = spans[0].Start;
		float trailing = length - spans[^1].End;
		Assert.AreEqual(Gap / 2f, leading, "The first zone does not start half a gap in.");
		Assert.AreEqual(leading, trailing, "The outer margins differ.");

		for (int i = 0; i < spans.Length - 1; i++)
		{
			Assert.AreEqual(Gap, spans[i + 1].Start - spans[i].End, $"The gap after zone {i} is not one gap wide.");
			Assert.AreEqual((spans[i].End + spans[i + 1].Start) / 2f, DividerContainerLayout.DividerPosition(spans, i), $"The divider after zone {i} is not in the middle of its gap.");
		}
	}

	[TestMethod]
	[DataRow(1000f)]
	[DataRow(1001f)]
	[DataRow(437f)]
	public void ThreeZones_HaveEqualGapsAndMargins(float length)
	{
		DividerZoneSpan[] spans = DividerContainerLayout.Spans([0.24f, 0.50f, 0.26f], length, Gap);

		AssertEven(spans, length);
	}

	[TestMethod]
	[DataRow(3)]
	[DataRow(4)]
	[DataRow(7)]
	public void AnyNumberOfEqualZones_HaveEqualGapsAndMargins(int count)
	{
		float[] fractions = new float[count];
		for (int i = 0; i < count; i++)
		{
			fractions[i] = 1f / count;
		}

		DividerZoneSpan[] spans = DividerContainerLayout.Spans(fractions, 997f, Gap);

		AssertEven(spans, 997f);
	}

	[TestMethod]
	public void TheZonesShareWhatTheGapsLeave_ByTheirFractions()
	{
		DividerZoneSpan[] spans = DividerContainerLayout.Spans([0.25f, 0.75f], 416f, Gap);

		// 416 less two gaps leaves 400, shared 100 and 300.
		Assert.AreEqual(new DividerZoneSpan(4f, 100f), spans[0]);
		Assert.AreEqual(new DividerZoneSpan(112f, 300f), spans[1]);
		Assert.AreEqual(108f, DividerContainerLayout.DividerPosition(spans, 0));
	}

	[TestMethod]
	public void ASingleZone_FillsTheContainer()
	{
		DividerZoneSpan[] spans = DividerContainerLayout.Spans([1f], 300f, Gap);

		Assert.AreEqual(new DividerZoneSpan(0f, 300f), spans[0]);
	}

	[TestMethod]
	public void NoZones_LayOutNothing() =>
		Assert.IsEmpty(DividerContainerLayout.Spans([], 300f, Gap));

	[TestMethod]
	public void AContainerSmallerThanItsGaps_GivesZonesNoLengthRatherThanANegativeOne()
	{
		DividerZoneSpan[] spans = DividerContainerLayout.Spans([0.5f, 0.5f], 10f, Gap);

		Assert.IsTrue(spans[0].Length >= 0f && spans[1].Length >= 0f);
	}

	[TestMethod]
	[DataRow(0)]
	[DataRow(1)]
	public void FractionForDividerAt_IsTheInverseOfTheLayout(int index)
	{
		float[] fractions = [0.24f, 0.50f, 0.26f];
		float[] exact = [.. fractions];
		DividerZoneSpan[] spans = DividerContainerLayout.Spans(fractions, 1000f, Gap);

		float fraction = DividerContainerLayout.FractionForDividerAt(fractions, index, DividerContainerLayout.DividerPosition(spans, index), 1000f, Gap);

		// The divider's position was rounded to a whole pixel, so the fraction is within one pixel's worth.
		Assert.AreEqual(exact[index], fraction, 1f / (1000f - (3 * Gap)));
	}
}
