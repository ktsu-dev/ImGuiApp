// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests the value type behind LevelsControl: the grey point and gamma mapping, the transfer
/// function and normalization. All pure — no ImGui context required.
/// </summary>
[TestClass]
public class LevelsAdjustmentTests
{
	[TestMethod]
	public void Identity_ApplyReturnsItsInput() =>
		Assert.AreEqual(0.3f, LevelsAdjustment.Identity.Apply(0.3f), 1e-6f);

	[TestMethod]
	public void GammaFromGreyPoint_MidpointIsGammaOne() =>
		Assert.AreEqual(1f, LevelsAdjustment.GammaFromGreyPoint(0f, 1f, 0.5f), 1e-5f);

	[TestMethod]
	public void GammaFromGreyPoint_QuarterWayIsGammaTwo() =>
		Assert.AreEqual(2f, LevelsAdjustment.GammaFromGreyPoint(0f, 1f, 0.25f), 1e-4f);

	[TestMethod]
	public void GammaFromGreyPoint_ClampsToTheGammaRange()
	{
		Assert.AreEqual(LevelsAdjustment.MaxGamma, LevelsAdjustment.GammaFromGreyPoint(0f, 1f, 0.0001f));
		Assert.AreEqual(LevelsAdjustment.MinGamma, LevelsAdjustment.GammaFromGreyPoint(0f, 1f, 0.9999f));
	}

	[TestMethod]
	[DataRow(0.5f)]
	[DataRow(1f)]
	[DataRow(2f)]
	[DataRow(4f)]
	public void GreyPointFromGamma_InvertsGammaFromGreyPoint(float gamma)
	{
		float grey = LevelsAdjustment.GreyPointFromGamma(0.1f, 0.9f, gamma);

		Assert.AreEqual(gamma, LevelsAdjustment.GammaFromGreyPoint(0.1f, 0.9f, grey), 1e-4f);
	}

	[TestMethod]
	public void GreyPoint_FollowsBlackAtConstantGamma() =>
		Assert.AreEqual(0.6f, new LevelsAdjustment(0.2f, 1f, 1f, 0f, 1f).GreyPoint, 1e-6f);

	[TestMethod]
	public void Apply_GreyPointMapsToHalfOutput()
	{
		LevelsAdjustment levels = new(0.2f, 0.8f, 2f, 0f, 1f);

		Assert.AreEqual(0.5f, levels.Apply(levels.GreyPoint), 1e-5f);
	}

	[TestMethod]
	public void Apply_ClampsBelowBlackAndAboveWhite()
	{
		LevelsAdjustment levels = new(0.2f, 0.8f, 1f, 0.1f, 0.9f);

		Assert.AreEqual(0.1f, levels.Apply(0.1f), 1e-6f);
		Assert.AreEqual(0.9f, levels.Apply(0.95f), 1e-6f);
	}

	[TestMethod]
	public void Apply_RemapsToTheOutputRange() =>
		Assert.AreEqual(0.4f, new LevelsAdjustment(0f, 1f, 1f, 0.2f, 0.6f).Apply(0.5f), 1e-6f);

	[TestMethod]
	public void Apply_NaNReturnsOutputBlack() =>
		Assert.AreEqual(0.2f, new LevelsAdjustment(0f, 1f, 1f, 0.2f, 0.6f).Apply(float.NaN));

	[TestMethod]
	public void Normalized_ReplacesNonFiniteFieldsWithIdentity() =>
		Assert.AreEqual(
			LevelsAdjustment.Identity,
			new LevelsAdjustment(float.NaN, float.PositiveInfinity, float.NaN, float.NaN, float.NaN).Normalized());

	[TestMethod]
	public void Normalized_ClampsGamma()
	{
		Assert.AreEqual(9.99f, new LevelsAdjustment(0f, 1f, 50f, 0f, 1f).Normalized().Gamma);
		Assert.AreEqual(0.1f, new LevelsAdjustment(0f, 1f, 0.01f, 0f, 1f).Normalized().Gamma);
	}

	[TestMethod]
	public void Normalized_OpensACollapsedInputRange()
	{
		LevelsAdjustment levels = new LevelsAdjustment(0.5f, 0.5f, 1f, 0f, 1f).Normalized();

		Assert.AreEqual(0.5f, levels.InputBlack, 1e-6f);
		Assert.AreEqual(0.5f + (2f / 255f), levels.InputWhite, 1e-6f);
	}

	[TestMethod]
	public void Normalized_OpensACollapsedInputRangeAtTheTopEdge()
	{
		LevelsAdjustment levels = new LevelsAdjustment(1f, 1f, 1f, 0f, 1f).Normalized();

		Assert.AreEqual(1f, levels.InputWhite, 1e-6f);
		Assert.AreEqual(1f - (2f / 255f), levels.InputBlack, 1e-6f);
	}

	[TestMethod]
	public void Normalized_OpensACollapsedOutputRange() =>
		Assert.AreEqual(0.7f + (1f / 255f), new LevelsAdjustment(0f, 1f, 1f, 0.7f, 0.7f).Normalized().OutputWhite, 1e-6f);

	[TestMethod]
	public void WithInputHandles_GreyMoveChangesOnlyGamma()
	{
		LevelsAdjustment levels = LevelsAdjustment.Identity.WithInputHandles([0f, 0.25f, 1f]);

		Assert.AreEqual(2f, levels.Gamma, 1e-4f);
		Assert.AreEqual(0f, levels.InputBlack);
		Assert.AreEqual(1f, levels.InputWhite);
	}

	[TestMethod]
	public void WithInputHandles_BlackMoveKeepsGamma()
	{
		LevelsAdjustment levels = new LevelsAdjustment(0f, 1f, 2f, 0f, 1f).WithInputHandles([0.2f, 0.5f, 1f]);

		Assert.AreEqual(0.2f, levels.InputBlack, 1e-6f);
		Assert.AreEqual(2f, levels.Gamma);
	}

	[TestMethod]
	public void WithOutputHandles_SetsTheOutputRange()
	{
		LevelsAdjustment levels = LevelsAdjustment.Identity.WithOutputHandles([0.1f, 0.9f]);

		Assert.AreEqual(0.1f, levels.OutputBlack, 1e-6f);
		Assert.AreEqual(0.9f, levels.OutputWhite, 1e-6f);
	}
}
