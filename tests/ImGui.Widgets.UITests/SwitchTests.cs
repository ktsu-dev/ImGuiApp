// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>Drives <see cref="ImGuiWidgets.Switch"/> on its own.</summary>
[TestClass]
public sealed class SwitchTests : WidgetTest
{
	private const string Label = "Wi-Fi";

	private bool value;

	private void Draw() => ImGuiWidgets.Switch(Label, ref value);

	[TestMethod]
	public void Switch_IsDrawnAndMarksItself()
	{
		Start(Draw);

		Assert.IsTrue(IsVisible(Label), "The switch marked no probe item, so nothing can address it.");
		AssertSomethingWasDrawn("the switch");
	}

	[TestMethod]
	public void Switch_ClickTogglesOn()
	{
		value = false;
		Start(Draw);

		Click(Label);

		Assert.IsTrue(value, "Clicking an off switch did not turn it on.");
	}

	[TestMethod]
	public void Switch_ClickTogglesOffAgain()
	{
		value = true;
		Start(Draw);

		Click(Label);

		Assert.IsFalse(value, "Clicking an on switch did not turn it off.");
	}

	[TestMethod]
	public void Switch_LooksDifferentWhenOn()
	{
		value = false;
		Start(Draw);

		// The switch animates its knob, so the two states are only reliably comparable once the
		// animation has run out.
		Step(30);
		byte[] off = Snapshot();

		Click(Label);
		Step(30);

		Assert.IsTrue(PixelsChangedSince(off) > 0, "The switch drew identically on and off.");
	}

	[TestMethod]
	public void Switch_UnclickedElsewhere_KeepsItsValue()
	{
		value = false;
		Start(Draw);

		ClickAwayFrom(Label);

		Assert.IsFalse(value, "A click outside the switch toggled it.");
	}
}
