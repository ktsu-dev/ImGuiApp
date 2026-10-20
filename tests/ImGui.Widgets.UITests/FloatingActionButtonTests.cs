// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using Hexa.NET.ImGui;

using ktsu.ImGui.App.Testing;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>Drives <see cref="ImGuiWidgets.FloatingActionButton"/> and <see cref="ImGuiWidgets.SpeedDial"/> on their own.</summary>
[TestClass]
public sealed class FloatingActionButtonTests : WidgetTest
{
	private const string Id = "fab";

	private static readonly SpeedDialAction[] Actions = [new("A", "Photo"), new("B", "Note"), new("C", "Reminder")];

	private int clicks;
	private int chosen = -1;
	private int underneathClicks;
	private FloatingActionButtonOptions options = new();

	private void DrawButton()
	{
		// A button the size of the window, so a click on the floating button that fell through would land on it.
		if (ImGui.Button("Underneath", ImGui.GetContentRegionAvail()))
		{
			underneathClicks++;
		}

		if (ImGuiWidgets.FloatingActionButton(Id, "+", options))
		{
			clicks++;
		}
	}

	private void DrawDial()
	{
		ImGui.TextUnformatted("Content");
		int index = ImGuiWidgets.SpeedDial(Id, "+", Actions, options);
		if (index >= 0)
		{
			chosen = index;
		}
	}

	[TestMethod]
	public void Click_ReportsTrue()
	{
		Start(DrawButton);

		Click(Id);

		Assert.AreEqual(1, clicks);
	}

	[TestMethod]
	public void Click_DoesNotReachWhatIsUnderneath()
	{
		Start(DrawButton);

		Click(Id);

		Assert.AreEqual(0, underneathClicks);
	}

	[TestMethod]
	public void Button_SitsInTheBottomRightCorner()
	{
		Start(DrawButton);

		Rectangle rect = RectOf(Id);
		Assert.AreEqual(56f, rect.Width, 1f);
		Assert.AreEqual(56f, rect.Height, 1f);
		Assert.IsTrue(rect.MaxX is > 600 and < 640, $"The button ended at x {rect.MaxX}.");
		Assert.IsTrue(rect.MaxY is > 440 and < 480, $"The button ended at y {rect.MaxY}.");
	}

	[TestMethod]
	public void Corner_CanBeTopLeft()
	{
		options = new FloatingActionButtonOptions { Corner = FloatingActionButtonCorner.TopLeft };
		Start(DrawButton);

		Rectangle rect = RectOf(Id);
		Assert.IsTrue(rect.MinX < 40f && rect.MinY < 40f, $"The button started at {rect.MinX}, {rect.MinY}.");
	}

	[TestMethod]
	public void SpeedDial_OpensItsActions()
	{
		Start(DrawDial);
		Assert.IsFalse(IsVisible($"{Id}/Photo"));

		Click(Id);
		Step(20);

		Assert.IsTrue(IsVisible($"{Id}/Photo"));
		Assert.IsTrue(RectOf($"{Id}/Reminder").MinY < RectOf($"{Id}/Photo").MinY, "Actions should stack upwards from a bottom corner.");
	}

	[TestMethod]
	public void SpeedDial_ChoosingAnAction_ReportsIt_AndFolds()
	{
		Start(DrawDial);
		Click(Id);
		Step(20);

		Click($"{Id}/Note");
		Step(20);

		Assert.AreEqual(1, chosen);
		Assert.IsFalse(IsVisible($"{Id}/Note"));
	}

	[TestMethod]
	public void SpeedDial_ClickingElsewhere_Folds()
	{
		Start(DrawDial);
		Click(Id);
		Step(20);

		Harness.Mouse.Click(40f, 200f);
		Step(20);

		Assert.AreEqual(-1, chosen);
		Assert.IsFalse(IsVisible($"{Id}/Photo"));
	}

	[TestMethod]
	public void SpeedDial_ClickingTheButtonAgain_Folds()
	{
		Start(DrawDial);
		Click(Id);
		Step(20);

		Click(Id);
		Step(20);

		Assert.IsFalse(IsVisible($"{Id}/Photo"));
	}
}
