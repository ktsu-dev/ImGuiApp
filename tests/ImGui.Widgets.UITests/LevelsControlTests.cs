// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using System.Linq;
using System.Numerics;

using ktsu.ImGui.App.Testing;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>Drives <see cref="ImGuiWidgets.LevelsControl"/> on its own.</summary>
[TestClass]
public sealed class LevelsControlTests : WidgetTest
{
	private const string Label = "Levels";

	private readonly float[] bins = [.. Enumerable.Repeat(1f, 256)];
	private LevelsAdjustment levels = LevelsAdjustment.Identity;
	private bool changed;

	private void Draw() => changed |= ImGuiWidgets.LevelsControl(Label, bins, 1, ref levels, new Vector2(400f, 160f));

	[TestMethod]
	public void LevelsControl_MarksItsParts()
	{
		Start(Draw);

		Assert.IsTrue(IsVisible("Levels/histogram"), "The histogram was not marked.");
		Assert.IsTrue(IsVisible("Levels/input"), "The input handle row was not marked.");
		Assert.IsTrue(IsVisible("Levels/outputRamp"), "The output ramp was not marked.");
		Assert.IsTrue(IsVisible("Levels/output"), "The output handle row was not marked.");
	}

	[TestMethod]
	public void LevelsControl_IdleFramesReportNoChange()
	{
		Start(Draw);
		Step(3);

		Assert.IsFalse(changed, "The control reported a change with nobody touching it.");
		Assert.AreEqual(LevelsAdjustment.Identity, levels);
	}

	[TestMethod]
	public void LevelsControl_DraggingTheBlackHandleRaisesInputBlack()
	{
		Start(Draw);

		DragAcross("Levels/input", 0.01f, 0.25f);

		Assert.IsTrue(levels.InputBlack is >= 0.23f and <= 0.27f, $"Input black landed at {levels.InputBlack}.");
		Assert.AreEqual(1f, levels.Gamma, 1e-4f, "Moving black changed gamma.");
		Assert.IsTrue(changed, "Dragging the black handle reported no change.");
	}

	[TestMethod]
	public void LevelsControl_DraggingTheGreyHandleChangesGamma()
	{
		Start(Draw);

		DragAcross("Levels/input", 0.5f, 0.25f);

		Assert.IsTrue(levels.Gamma is >= 1.9f and <= 2.1f, $"Gamma landed at {levels.Gamma}.");
		Assert.AreEqual(0f, levels.InputBlack, "Moving grey moved black.");
		Assert.AreEqual(1f, levels.InputWhite, "Moving grey moved white.");
	}

	[TestMethod]
	public void LevelsControl_HandlesStayOrdered()
	{
		Start(Draw);

		DragAcross("Levels/input", 0.01f, 0.99f);

		Assert.IsTrue(levels.InputBlack < levels.GreyPoint, $"Grey {levels.GreyPoint} is not above black {levels.InputBlack}.");
		Assert.IsTrue(levels.GreyPoint < levels.InputWhite, $"Grey {levels.GreyPoint} is not below white {levels.InputWhite}.");
		Assert.IsTrue(levels.InputWhite - levels.InputBlack >= (2f / 255f) - 1e-6f, "Black and white closed up past the grey handle's room.");
	}

	[TestMethod]
	public void LevelsControl_DraggingTheOutputWhiteHandleLowersOutputWhite()
	{
		Start(Draw);

		DragAcross("Levels/output", 0.99f, 0.75f);

		Assert.IsTrue(levels.OutputWhite is >= 0.73f and <= 0.77f, $"Output white landed at {levels.OutputWhite}.");
		Assert.AreEqual(0f, levels.OutputBlack, "Moving output white moved output black.");
	}

	[TestMethod]
	public void LevelsControl_DrawsADarkToLightRamp()
	{
		Start(Draw);

		Rectangle ramp = RectOf("Levels/outputRamp");
		int y = ramp.MinY + (ramp.Height / 2);
		Rgba32 dark = Harness.Target.GetPixel(ramp.MinX + (ramp.Width / 10), y);
		Rgba32 light = Harness.Target.GetPixel(ramp.MinX + (ramp.Width * 9 / 10), y);

		Assert.IsTrue(dark.R < light.R, $"The ramp is not dark to light: {dark.R} on the left, {light.R} on the right.");
	}
}
