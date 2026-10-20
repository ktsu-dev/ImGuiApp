// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using System.Numerics;

using Hexa.NET.ImGui;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>Drives <see cref="ImGuiWidgets.LongPressMenu"/> on its own.</summary>
[TestClass]
public sealed class LongPressMenuTests : WidgetTest
{
	private const string Id = "photo";
	private static readonly string[] Items = ["Copy", "Share", "Delete"];

	private int chosen = -1;
	private bool buttonClicked;

	private void Draw()
	{
		int picked = ImGuiWidgets.LongPressMenu(Id, Items, () =>
		{
			ImGui.Button("Open##photoButton", new Vector2(160f, 100f));
			buttonClicked |= ImGui.IsItemDeactivated() && ImGui.IsItemHovered();
		});

		if (picked >= 0)
		{
			chosen = picked;
		}
	}

	private void Hold(int frames)
	{
		Vector2 center = CenterOf(Id);
		Harness.Mouse.MoveTo(center.X, center.Y);
		Step();
		App.Testing.HarnessMouse.Down(0);
		Step(frames);
	}

	private void Release()
	{
		App.Testing.HarnessMouse.Up(0);
		Step(2);
	}

	[TestMethod]
	public void LongPressMenu_LongPress_OpensTheMenu()
	{
		Start(Draw);

		Hold(45);
		Release();

		Assert.IsTrue(IsVisible("photo/Copy"), "Holding the target did not open its menu.");
		Assert.IsFalse(buttonClicked, "The long press also clicked the button under it.");
	}

	[TestMethod]
	public void LongPressMenu_ShortPress_ClicksTheContentInstead()
	{
		Start(Draw);

		Click(Id);
		Step(2);

		Assert.IsFalse(IsVisible("photo/Copy"));
		Assert.IsTrue(buttonClicked);
	}

	[TestMethod]
	public void LongPressMenu_ChoosingAnItem_ReportsIt()
	{
		Start(Draw);
		Hold(45);
		Release();

		Click("photo/Delete");
		Step(2);

		Assert.AreEqual(2, chosen);
		Assert.IsFalse(IsVisible("photo/Copy"), "The menu stayed open after a choice.");
	}

	[TestMethod]
	public void LongPressMenu_Escape_ClosesWithoutAChoice()
	{
		Start(Draw);
		Hold(45);
		Release();

		Harness.Keyboard.Press(ImGuiKey.Escape);
		Step(2);

		Assert.IsFalse(IsVisible("photo/Copy"));
		Assert.AreEqual(-1, chosen);
	}

	[TestMethod]
	public void LongPressMenu_RightClick_OpensTheMenu()
	{
		Start(Draw);
		Vector2 center = CenterOf(Id);

		Harness.Mouse.Click(center.X, center.Y, button: 1);
		Step(2);

		Assert.IsTrue(IsVisible("photo/Copy"));
	}

	[TestMethod]
	public void LongPressMenu_WhileHeld_ShrinksTheTarget()
	{
		Start(Draw);
		byte[] before = Snapshot();

		Hold(20);

		Assert.IsTrue(PixelsChangedSince(before) > 0, "Holding the target did not shrink it.");
		Release();
	}

	[TestMethod]
	public void LongPressMenu_PressThatMovesAway_OpensNothing()
	{
		Start(Draw);
		Vector2 center = CenterOf(Id);

		Harness.Mouse.MoveTo(center.X, center.Y);
		Step();
		App.Testing.HarnessMouse.Down(0);
		Step();
		Harness.Mouse.MoveTo(center.X + 40f, center.Y);
		Step(45);
		Release();

		Assert.IsFalse(IsVisible("photo/Copy"));
	}
}
