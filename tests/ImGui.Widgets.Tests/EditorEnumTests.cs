// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using HexaCurveEditType = Hexa.NET.ImGui.Widgets.ImCurveEdit.CurveType;
using HexaCurvePointType = Hexa.NET.Mathematics.CurvePointType;
using HexaMathCurveType = Hexa.NET.Mathematics.CurveType;
using HexaSequencerOptions = Hexa.NET.ImGui.Widgets.ImSequencer.SequencerOptions;

/// <summary>
/// Tests the enum mirrors for the Tier 3 editors. Two different upstream enums are both named
/// CurveType, so these tests also pin that the two mirrors do not cross over.
/// </summary>
[TestClass]
public sealed class EditorEnumTests
{
	[TestMethod]
	public void MapInterpolation_CoversEveryMember()
	{
		Assert.AreEqual(HexaCurveEditType.None, ImGuiWidgets.MapInterpolation(CurveInterpolation.None));
		Assert.AreEqual(HexaCurveEditType.CurveDiscrete, ImGuiWidgets.MapInterpolation(CurveInterpolation.Discrete));
		Assert.AreEqual(HexaCurveEditType.CurveLinear, ImGuiWidgets.MapInterpolation(CurveInterpolation.Linear));
		Assert.AreEqual(HexaCurveEditType.CurveSmooth, ImGuiWidgets.MapInterpolation(CurveInterpolation.Smooth));
		Assert.AreEqual(HexaCurveEditType.CurveBezier, ImGuiWidgets.MapInterpolation(CurveInterpolation.Bezier));
	}

	[TestMethod]
	public void MapInterpolation_RoundTripsEveryMember()
	{
		foreach (CurveInterpolation value in Enum.GetValues<CurveInterpolation>())
		{
			Assert.AreEqual(value, ImGuiWidgets.MapInterpolationBack(ImGuiWidgets.MapInterpolation(value)));
		}
	}

	[TestMethod]
	public void MapInterpolationBack_UnknownValue_FallsBackToLinear() =>
		Assert.AreEqual(CurveInterpolation.Linear, ImGuiWidgets.MapInterpolationBack((HexaCurveEditType)99));

	[TestMethod]
	public void MapShape_CoversEveryMember()
	{
		Assert.AreEqual(HexaMathCurveType.Smooth, ImGuiWidgets.MapShape(CurveShape.Smooth));
		Assert.AreEqual(HexaMathCurveType.Freehand, ImGuiWidgets.MapShape(CurveShape.Freehand));
	}

	[TestMethod]
	public void MapShape_RoundTripsEveryMember()
	{
		foreach (CurveShape value in Enum.GetValues<CurveShape>())
		{
			Assert.AreEqual(value, ImGuiWidgets.MapShapeBack(ImGuiWidgets.MapShape(value)));
		}
	}

	[TestMethod]
	public void ShapeAndInterpolation_DoNotShareAMapping()
	{
		// Both upstream enums are named CurveType. CurveShape.Smooth is Mathematics.CurveType.Smooth = 0,
		// while CurveInterpolation.Smooth is ImCurveEdit.CurveType.CurveSmooth = 3. A shared mirror
		// would map one of them wrong; this pins that they are genuinely separate.
		Assert.AreEqual(0, (int)ImGuiWidgets.MapShape(CurveShape.Smooth));
		Assert.AreEqual(3, (int)ImGuiWidgets.MapInterpolation(CurveInterpolation.Smooth));
	}

	[TestMethod]
	public void MapPointKind_RoundTripsEveryMember()
	{
		foreach (CurvePointKind value in Enum.GetValues<CurvePointKind>())
		{
			Assert.AreEqual(value, ImGuiWidgets.MapPointKindBack(ImGuiWidgets.MapPointKind(value)));
		}

		Assert.AreEqual(HexaCurvePointType.Corner, ImGuiWidgets.MapPointKind(CurvePointKind.Corner));
	}

	[TestMethod]
	public void MapFeatures_PreservesUpstreamNumericGaps()
	{
		// Upstream skips 1<<0 and 1<<2. A naive cast would still work only if our values match
		// exactly, so assert the numbers rather than trusting the names.
		Assert.AreEqual(HexaSequencerOptions.EditNone, ImGuiWidgets.MapFeatures(SequencerFeatures.None));
		Assert.AreEqual(HexaSequencerOptions.EditStartend, ImGuiWidgets.MapFeatures(SequencerFeatures.EditStartEnd));
		Assert.AreEqual(HexaSequencerOptions.ChangeFrame, ImGuiWidgets.MapFeatures(SequencerFeatures.ChangeFrame));
		Assert.AreEqual(HexaSequencerOptions.Add, ImGuiWidgets.MapFeatures(SequencerFeatures.Add));
		Assert.AreEqual(HexaSequencerOptions.Del, ImGuiWidgets.MapFeatures(SequencerFeatures.Delete));
		Assert.AreEqual(HexaSequencerOptions.Copypaste, ImGuiWidgets.MapFeatures(SequencerFeatures.CopyPaste));
	}

	[TestMethod]
	public void MapFeatures_CombinedFlags_MapToCombinedUpstream() =>
		Assert.AreEqual(HexaSequencerOptions.EditAll, ImGuiWidgets.MapFeatures(SequencerFeatures.EditAll));

	[TestMethod]
	public void MapFeatures_ArbitraryCombination_PreservesEveryBit() =>
		Assert.AreEqual(
			HexaSequencerOptions.Add | HexaSequencerOptions.Del | HexaSequencerOptions.ChangeFrame,
			ImGuiWidgets.MapFeatures(SequencerFeatures.Add | SequencerFeatures.Delete | SequencerFeatures.ChangeFrame));
}
