// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using Hexa.NET.ImGui;

using ktsu.ImGui.App.Testing;

using ktsu.ImGui.Widgets.Overlays;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>Drives <see cref="ImGuiWidgets.NavigationDrawer"/> on its own.</summary>
[TestClass]
public sealed class NavigationDrawerTests : WidgetTest
{
	private const string Id = "drawer";

	private readonly OverlayHost host = new();
	private ImGuiWidgets.NavigationDrawer drawer = null!;
	private int insideClicks;

	private void Draw()
	{
		drawer.TrackEdgeSwipe();
		host.Render();
	}

	private void StartDrawer()
	{
		drawer = new ImGuiWidgets.NavigationDrawer(host, Id, () =>
		{
			if (ImGui.Button("Inbox"))
			{
				insideClicks++;
			}

			Mark("Inbox");
		});
		Start(Draw);
	}

	private void OpenDrawer()
	{
		StartDrawer();
		drawer.Open();
		Step(60);
	}

	[TestMethod]
	public void Open_SlidesInFromTheLeft()
	{
		OpenDrawer();

		Rectangle rect = RectOf(Id);
		Assert.AreEqual(0f, rect.MinX, 1f);
		Assert.AreEqual(280f, rect.Width, 1f);
		Assert.AreEqual(480f, rect.Height, 1f);
	}

	[TestMethod]
	public void Content_IsClickable()
	{
		OpenDrawer();

		Click("Inbox");

		Assert.AreEqual(1, insideClicks);
	}

	[TestMethod]
	public void TapOnTheDimming_ClosesIt()
	{
		OpenDrawer();

		Harness.Mouse.Click(500f, 240f);
		Step(60);

		Assert.IsFalse(drawer.IsOpen);
		Assert.IsFalse(host.HasOverlays);
	}

	[TestMethod]
	public void Escape_ClosesIt()
	{
		OpenDrawer();

		Harness.Keyboard.Press(ImGuiKey.Escape);
		Step();

		Assert.IsFalse(drawer.IsOpen);
	}

	[TestMethod]
	public void DragFromTheEdge_PullsItOpen()
	{
		StartDrawer();

		Harness.Mouse.Drag(4f, 240f, 220f, 240f, 30);
		Step(60);

		Assert.IsTrue(drawer.IsOpen);
		Assert.AreEqual(0f, RectOf(Id).MinX, 1f);
	}

	[TestMethod]
	public void ShortDragFromTheEdge_LetsItFallBack()
	{
		StartDrawer();

		Harness.Mouse.Drag(4f, 240f, 60f, 240f, 60);
		Step(60);

		Assert.IsFalse(drawer.IsOpen);
		Assert.IsFalse(host.HasOverlays);
	}

	[TestMethod]
	public void DragAwayFromTheEdge_DoesNothing()
	{
		StartDrawer();

		Harness.Mouse.Drag(200f, 240f, 500f, 240f, 30);
		Step(10);

		Assert.IsFalse(drawer.IsOpen);
		Assert.IsFalse(host.HasOverlays);
	}

	[TestMethod]
	public void DraggingTheDimmingLeft_ClosesIt()
	{
		OpenDrawer();

		Harness.Mouse.Drag(500f, 240f, 200f, 240f, 20);
		Step(60);

		Assert.IsFalse(drawer.IsOpen);
	}
}
