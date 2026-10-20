// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.App.Testing;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Drives the Hexa-backed buttons — <see cref="ImGuiWidgets.ToggleSwitch"/>,
/// <see cref="ImGuiWidgets.ToggleButton"/>, <see cref="ImGuiWidgets.TransparentButton"/> and
/// <see cref="ImGuiWidgets.InlineButton"/> — each on its own.
/// </summary>
[TestClass]
public sealed class HexaButtonTests : WidgetTest
{
	private bool toggled;
	private bool clicked;

	[TestMethod]
	public void ToggleSwitch_ClickTogglesIt()
	{
		toggled = false;

		Start(() => ImGuiWidgets.ToggleSwitch("Notifications", ref toggled));

		Click("Notifications");

		Assert.IsTrue(toggled, "Clicking the toggle switch did not turn it on.");
	}

	[TestMethod]
	public void ToggleSwitch_AnimatesTowardItsNewState()
	{
		// The switch animates off Hexa's animation clock, which the pump advances. Whether it also
		// animates in an application that never pumps depends on a process-global flag that any
		// earlier test in this assembly may already have set, so that fallback is covered by the
		// unit tests around EvaluateFallbackTick rather than from here.
		toggled = false;

		Start(() =>
		{
			ImGuiWidgets.ToggleSwitch("Notifications", ref toggled);
			ImGuiWidgets.DrawDeferred();
		});

		Click("Notifications");
		MoveAway();
		byte[] midAnimation = Snapshot();

		Step(30);
		MoveAway();

		Assert.IsTrue(PixelsChangedSince(midAnimation) > 0, "The switch never animated toward its new state.");
	}

	[TestMethod]
	public void ToggleButton_ClickTogglesIt()
	{
		toggled = false;

		Start(() => ImGuiWidgets.ToggleButton("Bold", ref toggled));

		Click("Bold");

		Assert.IsTrue(toggled, "Clicking the toggle button did not select it.");
	}

	[TestMethod]
	public void ToggleButton_SelectedDrawsARing()
	{
		toggled = false;

		Start(() => ImGuiWidgets.ToggleButton("Bold", ref toggled, new Vector2(100f, 30f)));

		MoveAway();
		byte[] unselected = Snapshot();

		toggled = true;
		Step(2);
		MoveAway();

		Assert.IsTrue(PixelsChangedSince(unselected) > 0, "A selected toggle button drew the same as an unselected one.");
	}

	[TestMethod]
	public void TransparentButton_ReportsAClick()
	{
		clicked = false;

		Start(() => clicked |= ImGuiWidgets.TransparentButton("Dismiss"));

		Click("Dismiss");

		Assert.IsTrue(clicked, "The transparent button did not report a click.");
	}

	[TestMethod]
	public void TransparentButton_DrawsNoBackgroundUntilHovered()
	{
		Start(() => ImGuiWidgets.TransparentButton("Dismiss", new Vector2(120f, 30f)));

		MoveAway();
		byte[] idle = Snapshot();

		Hover("Dismiss");

		Assert.IsTrue(PixelsChangedSince(idle) > 0, "The transparent button looked the same hovered as idle.");
	}

	[TestMethod]
	public void InlineButton_AnchorsInsideTheBoundsItIsGiven()
	{
		clicked = false;
		Vector2 min = default;
		Vector2 max = default;

		Start(() =>
		{
			min = ImGui.GetCursorScreenPos();
			max = min + new Vector2(240f, 40f);
			ImGui.Dummy(new Vector2(240f, 40f));
			clicked |= ImGuiWidgets.InlineButton("Edit", min, max, new Vector2(1f, 0.5f));
		});

		Rectangle rect = RectOf("Edit");

		Assert.IsTrue(rect.MinX >= min.X - 1f, "The inline button was placed left of its bounds.");
		Assert.IsTrue(rect.MaxX <= max.X + 1f, $"The inline button ran past the right edge of its bounds ({rect.MaxX} > {max.X}).");
	}

	[TestMethod]
	public void InlineButton_ReportsAClick()
	{
		clicked = false;

		Start(() =>
		{
			Vector2 min = ImGui.GetCursorScreenPos();
			Vector2 max = min + new Vector2(240f, 40f);
			ImGui.Dummy(new Vector2(240f, 40f));
			clicked |= ImGuiWidgets.InlineButton("Edit", min, max, new Vector2(0.5f, 0.5f));
		});

		Click("Edit");

		Assert.IsTrue(clicked, "The inline button did not report a click.");
	}

	[TestMethod]
	public void ToggleSwitch_MarksItself() => _ = AssertMarksItself("Notifications", () => ImGuiWidgets.ToggleSwitch("Notifications", ref toggled));

	[TestMethod]
	public void ToggleButton_MarksItself() => _ = AssertMarksItself("Bold", () => ImGuiWidgets.ToggleButton("Bold", ref toggled));

	[TestMethod]
	public void TransparentButton_MarksItself() => _ = AssertMarksItself("Dismiss", () => ImGuiWidgets.TransparentButton("Dismiss", new Vector2(120f, 30f)));

	[TestMethod]
	public void InlineButton_MarksItself()
	{
		Start(() =>
		{
			Vector2 min = ImGui.GetCursorScreenPos();
			Vector2 max = min + new Vector2(240f, 40f);
			ImGui.Dummy(new Vector2(240f, 40f));
			ImGuiWidgets.InlineButton("Edit", min, max, new Vector2(1f, 0.5f));
		});

		Assert.IsTrue(IsVisible("Edit"), "The inline button did not mark itself.");
		Assert.IsTrue(RectOf("Edit").Width < 240, "The inline button marked its whole bounds rather than itself.");
	}
}
