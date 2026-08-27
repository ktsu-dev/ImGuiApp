// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.App.Testing;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>Drives <c>ImGuiWidgets.RadialProgressBar</c> and its countdown variants on their own.</summary>
[TestClass]
public sealed class RadialProgressBarTests : WidgetTest
{
	private const string Name = "radial";
	private const float Radius = 48f;

	private float progress = 0.5f;
	private ImGuiRadialProgressBarTextMode textMode = ImGuiRadialProgressBarTextMode.Percentage;

	private void Draw()
	{
		Vector2 origin = ImGui.GetCursorScreenPos();
		ImGuiWidgets.RadialProgressBar(progress, Radius, textMode: textMode);
		MarkSpan(Name, origin);
	}

	[TestMethod]
	public void RadialProgressBar_DrawsItsRing()
	{
		Start(Draw);

		Assert.IsTrue(IsVisible(Name), "The progress bar drew nothing.");
		AssertSomethingWasDrawn("the radial progress bar");
	}

	[TestMethod]
	public void RadialProgressBar_ReservesRoomForItsRadius()
	{
		Start(Draw);

		Rectangle rect = RectOf(Name);

		Assert.IsTrue(
			rect.Width >= Radius,
			$"A bar of radius {Radius} reserved only {rect.Width}px across.");
	}

	[TestMethod]
	public void RadialProgressBar_FillsFurtherAsProgressRises()
	{
		progress = 0.1f;
		Start(Draw);
		MoveAway();
		byte[] barelyStarted = Snapshot();

		progress = 0.9f;
		Step(2);
		MoveAway();

		Assert.IsTrue(PixelsChangedSince(barelyStarted) > 0, "A nearly full bar drew the same as a nearly empty one.");
	}

	[TestMethod]
	public void RadialProgressBar_ClampsPastTheEnds()
	{
		progress = 5f;
		Start(Draw);
		MoveAway();
		byte[] overfull = Snapshot();

		progress = 1f;
		Step(2);
		MoveAway();

		Assert.AreEqual(0, PixelsChangedSince(overfull), "A progress of 5.0 drew differently from a full bar, so it was not clamped.");
	}

	[TestMethod]
	public void RadialProgressBar_TextModeChangesTheLabel()
	{
		textMode = ImGuiRadialProgressBarTextMode.Percentage;
		Start(Draw);
		MoveAway();
		byte[] percentage = Snapshot();

		textMode = ImGuiRadialProgressBarTextMode.Time;
		Step(2);
		MoveAway();

		Assert.IsTrue(PixelsChangedSince(percentage) > 0, "A time label drew the same as a percentage one.");
	}

	[TestMethod]
	public void RadialCountdown_DrawsTheRemainingTime()
	{
		float remaining = 30f;

		Start(() =>
		{
			Vector2 origin = ImGui.GetCursorScreenPos();
			ImGuiWidgets.RadialCountdown(remaining, 60f, Radius);
			MarkSpan(Name, origin);
		});

		MoveAway();
		byte[] halfway = Snapshot();

		remaining = 5f;
		Step(2);
		MoveAway();

		Assert.IsTrue(PixelsChangedSince(halfway) > 0, "A countdown near its end drew the same as one halfway through.");
	}

	[TestMethod]
	public void RadialCountUp_DrawsTheElapsedTime()
	{
		float elapsed = 5f;

		Start(() =>
		{
			Vector2 origin = ImGui.GetCursorScreenPos();
			ImGuiWidgets.RadialCountUp(elapsed, 60f, Radius);
			MarkSpan(Name, origin);
		});

		MoveAway();
		byte[] early = Snapshot();

		elapsed = 55f;
		Step(2);
		MoveAway();

		Assert.IsTrue(PixelsChangedSince(early) > 0, "A nearly finished count-up drew the same as one just started.");
	}
}
