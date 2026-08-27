// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using System.Collections.ObjectModel;

using Hexa.NET.ImGui;

using ktsu.ImGui.App.Testing;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>Drives <c>ImGuiWidgets.Combo</c> on its own.</summary>
[TestClass]
public sealed class ComboTests : WidgetTest
{
	private const string Label = "Difficulty";

	/// <summary>The enum the enum overload is driven with.</summary>
	internal enum Difficulty
	{
		/// <summary>The first member, which is the starting selection.</summary>
		Easy,

		/// <summary>The second member.</summary>
		Normal,

		/// <summary>The third member.</summary>
		Hard,
	}

	private static readonly Collection<string> Options = ["Alpha", "Beta", "Gamma"];

	private Difficulty difficulty;
	private string selected = "Alpha";

	// Popup geometry, read from the live style while a frame is being drawn. The combo's popup is
	// placed directly below the combo, padded by the window padding, with one selectable per option
	// spaced by the item spacing -- so an option's center can be aimed at without ImGui reporting
	// where it went.
	private float rowPitch;
	private float popupPadding;

	private void CaptureMetrics()
	{
		rowPitch = ImGui.GetTextLineHeightWithSpacing();
		popupPadding = ImGui.GetStyle().WindowPadding.Y;
	}

	private void DrawEnumCombo()
	{
		CaptureMetrics();
		ImGuiWidgets.Combo(Label, ref difficulty);
	}

	private void DrawStringCombo()
	{
		CaptureMetrics();
		ImGuiWidgets.Combo(Label, ref selected, Options);
	}

	private void ClickOption(int index)
	{
		Rectangle rect = RectOf(Label);
		Harness.Mouse.Click(rect.MinX + 12f, rect.MaxY + popupPadding + (rowPitch * (index + 0.5f)));
		Step();
	}

	[TestMethod]
	public void Combo_IsDrawnAndMarksItself()
	{
		Start(DrawEnumCombo);

		Assert.IsTrue(IsVisible(Label), "The combo marked no probe item.");
		AssertSomethingWasDrawn("the combo");
	}

	[TestMethod]
	public void Combo_ClickingOpensAListOfOptions()
	{
		Start(DrawStringCombo);
		byte[] closed = Snapshot();

		Click(Label);

		Assert.IsTrue(PixelsChangedSince(closed) > 0, "Clicking the combo opened nothing.");
	}

	[TestMethod]
	public void Combo_ChoosingAnOptionSelectsIt()
	{
		Start(DrawStringCombo);

		Click(Label);
		ClickOption(2);

		Assert.AreEqual("Gamma", selected, "Choosing the third option did not select it.");
	}

	[TestMethod]
	public void Combo_ChoosingTheSameOptionKeepsIt()
	{
		selected = "Beta";
		Start(DrawStringCombo);

		Click(Label);
		ClickOption(1);

		Assert.AreEqual("Beta", selected);
	}

	[TestMethod]
	public void Combo_EnumOverload_SelectsByMember()
	{
		difficulty = Difficulty.Easy;
		Start(DrawEnumCombo);

		Click(Label);
		ClickOption(2);

		Assert.AreEqual(Difficulty.Hard, difficulty, "Choosing the third enum member did not select it.");
	}

	[TestMethod]
	public void Combo_ClosedAgain_ShowsTheSelection()
	{
		Start(DrawStringCombo);

		Click(Label);
		ClickOption(2);
		MoveAway();
		byte[] showingGamma = Snapshot();

		selected = "Alpha";
		Step(2);
		MoveAway();

		Assert.IsTrue(PixelsChangedSince(showingGamma) > 0, "The combo showed the same label for two different selections.");
	}
}
