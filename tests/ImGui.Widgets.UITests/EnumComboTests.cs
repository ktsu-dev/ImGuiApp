// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using Hexa.NET.ImGui;

using ktsu.ImGui.App.Testing;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>Drives <see cref="ImGuiWidgets.EnumCombo{T}"/>, the Hexa-backed enum combo, on its own.</summary>
[TestClass]
public sealed class EnumComboTests : WidgetTest
{
	private const string Label = "Mode";

	/// <summary>The enum the combo is driven with.</summary>
	internal enum Mode
	{
		/// <summary>The first member, which is the starting selection.</summary>
		Draft,

		/// <summary>The second member.</summary>
		Review,

		/// <summary>The third member.</summary>
		Published,
	}

	private Mode value;
	private float rowPitch;
	private float popupPadding;

	private void Draw()
	{
		rowPitch = ImGui.GetTextLineHeightWithSpacing();
		popupPadding = ImGui.GetStyle().WindowPadding.Y;
		ImGuiWidgets.EnumCombo(Label, ref value);
	}

	private void ClickOption(int index)
	{
		Rectangle rect = RectOf(Label);
		Harness.Mouse.Click(rect.MinX + 12f, rect.MaxY + popupPadding + (rowPitch * (index + 0.5f)));
		Step();
	}

	[TestMethod]
	public void EnumCombo_IsDrawn()
	{
		Start(() =>
		{
			Draw();
			Mark(Label);
		});

		Assert.IsTrue(IsVisible(Label), "The enum combo drew no item.");
		AssertSomethingWasDrawn("the enum combo");
	}

	[TestMethod]
	public void EnumCombo_ChoosingAMemberSelectsIt()
	{
		value = Mode.Draft;

		Start(() =>
		{
			Draw();
			Mark(Label);
		});

		Click(Label);
		ClickOption(2);

		Assert.AreEqual(Mode.Published, value, "Choosing the third member did not select it.");
	}

	[TestMethod]
	public void EnumCombo_LeftAlone_KeepsItsValue()
	{
		value = Mode.Review;

		Start(() =>
		{
			Draw();
			Mark(Label);
		});

		Step(5);

		Assert.AreEqual(Mode.Review, value);
	}
}
