// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using Hexa.NET.ImGui;

using ktsu.ImGui.App.Testing;

using ktsu.ImGui.Widgets.Overlays;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>Drives <see cref="ImGuiWidgets.ActionSheet"/> on its own.</summary>
[TestClass]
public sealed class ActionSheetTests : WidgetTest
{
	private const string Id = "sheet";

	private static readonly ActionSheetButton[] Buttons = [new("Share"), new("Duplicate"), new("Delete", IsDestructive: true)];

	private readonly OverlayHost host = new();
	private ImGuiWidgets.ActionSheet sheet = null!;
	private int answer = int.MinValue;
	private int answers;
	private int buttonClicks;

	private void Draw()
	{
		if (ImGui.Button("Underneath"))
		{
			buttonClicks++;
		}

		Mark("Underneath");
		host.Render();
	}

	private void OpenSheet()
	{
		sheet = new ImGuiWidgets.ActionSheet(host, Id);
		Start(Draw);
		sheet.Open(Buttons, index =>
		{
			answer = index;
			answers++;
		}, "Photo");
		Step(30);
	}

	[TestMethod]
	public void Button_AnswersWithItsIndex()
	{
		OpenSheet();

		Click($"{Id}/Delete");

		Assert.AreEqual(2, answer);
		Assert.AreEqual(1, answers);
		Assert.IsFalse(sheet.IsOpen);
	}

	[TestMethod]
	public void CancelButton_AnswersMinusOne()
	{
		OpenSheet();

		Click($"{Id}/cancel");

		Assert.AreEqual(-1, answer);
	}

	[TestMethod]
	public void TapOnTheDimming_Cancels()
	{
		OpenSheet();

		Harness.Mouse.Click(320f, 40f);
		Step();

		Assert.AreEqual(-1, answer);
	}

	[TestMethod]
	public void Escape_Cancels()
	{
		OpenSheet();

		Harness.Keyboard.Press(ImGuiKey.Escape);
		Step();

		Assert.AreEqual(-1, answer);
	}

	[TestMethod]
	public void OpenSheet_BlocksWhatIsBehindIt()
	{
		OpenSheet();

		Click("Underneath");

		Assert.AreEqual(0, buttonClicks);
		Assert.AreEqual(1, answers, "The click should have landed on the dimming and cancelled.");
	}

	[TestMethod]
	public void Sheet_SlidesAwayAndLeavesTheHost()
	{
		OpenSheet();

		Click($"{Id}/Share");
		Step(30);

		Assert.IsFalse(IsVisible($"{Id}/Share"));
		Assert.IsFalse(host.HasOverlays);
	}

	[TestMethod]
	public void Sheet_StacksItsButtonsAboveCancel_AtTheBottom()
	{
		OpenSheet();

		Rectangle share = RectOf($"{Id}/Share");
		Rectangle delete = RectOf($"{Id}/Delete");
		Rectangle cancel = RectOf($"{Id}/cancel");

		Assert.IsTrue(share.MaxY <= delete.MinY + 1f);
		Assert.IsTrue(delete.MaxY < cancel.MinY);
		Assert.AreEqual(480f, cancel.MaxY, 12f);
	}
}
