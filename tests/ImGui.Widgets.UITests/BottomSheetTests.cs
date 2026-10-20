// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.App.Testing;

using ktsu.ImGui.Widgets.Overlays;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>Drives <see cref="ImGuiWidgets.BottomSheet"/> on its own.</summary>
[TestClass]
public sealed class BottomSheetTests : WidgetTest
{
	private const string Id = "sheet";

	private readonly OverlayHost host = new();
	private ImGuiWidgets.BottomSheet sheet = null!;
	private int insideClicks;

	private void Draw() => host.Render();

	private void OpenSheet(BottomSheetDetent detent = BottomSheetDetent.Half)
	{
		sheet = new ImGuiWidgets.BottomSheet(host, Id, _ =>
		{
			if (ImGui.Button("Inside"))
			{
				insideClicks++;
			}

			Mark("Inside");
		});
		Start(Draw);
		sheet.Open(detent);
		Step(60);
	}

	[TestMethod]
	public void Open_RisesToHalfTheWindow()
	{
		OpenSheet();

		Rectangle rect = RectOf(Id);
		Assert.AreEqual(240f, rect.Height, 3f);
		Assert.AreEqual(480f, rect.MaxY, 1f);
	}

	[TestMethod]
	public void Content_IsDrawnAndClickable()
	{
		OpenSheet();

		Click("Inside");

		Assert.AreEqual(1, insideClicks);
	}

	[TestMethod]
	public void DraggingTheHandleUp_OpensItFully()
	{
		OpenSheet();
		Vector2 handle = CenterOf($"{Id}/handle");

		Harness.Mouse.Drag(handle.X, handle.Y, handle.X, handle.Y - 150f, 40);
		Step(60);

		Assert.AreEqual(BottomSheetDetent.Full, sheet.Detent);
		Assert.AreEqual(432f, RectOf(Id).Height, 3f);
	}

	[TestMethod]
	public void DraggingTheHandleDown_LowersItToPeek()
	{
		OpenSheet();
		Vector2 handle = CenterOf($"{Id}/handle");

		Harness.Mouse.Drag(handle.X, handle.Y, handle.X, handle.Y + 110f, 40);
		Step(60);

		Assert.AreEqual(BottomSheetDetent.Peek, sheet.Detent);
	}

	[TestMethod]
	public void DraggingItRightDown_DismissesIt()
	{
		OpenSheet(BottomSheetDetent.Peek);
		Vector2 handle = CenterOf($"{Id}/handle");

		Harness.Mouse.Drag(handle.X, handle.Y, handle.X, 478f, 30);
		Step(60);

		Assert.IsFalse(sheet.IsOpen);
		Assert.IsFalse(host.HasOverlays);
	}

	[TestMethod]
	public void TapOnTheDimming_ClosesIt()
	{
		OpenSheet();

		Harness.Mouse.Click(320f, 40f);
		Step(60);

		Assert.IsFalse(sheet.IsOpen);
		Assert.IsFalse(IsVisible("Inside"));
	}

	[TestMethod]
	public void Escape_ClosesIt()
	{
		OpenSheet();

		Harness.Keyboard.Press(ImGuiKey.Escape);
		Step();

		Assert.IsFalse(sheet.IsOpen);
	}
}
