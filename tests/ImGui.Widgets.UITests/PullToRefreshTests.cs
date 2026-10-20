// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.App.Testing;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>Drives <see cref="ImGuiWidgets.PullToRefresh"/> on its own.</summary>
[TestClass]
public sealed class PullToRefreshTests : WidgetTest
{
	private const string Id = "feed";

	private int triggers;
	private bool refreshing;
	private int lines = 5;
	private bool sliderMoved;
	private float slider = 0.5f;
	private bool withSlider;

	private void Draw()
	{
		using ImGuiWidgets.PullToRefresh feed = new(Id, new Vector2(300f, 240f), refreshing);
		if (feed.Triggered)
		{
			triggers++;
		}

		if (withSlider)
		{
			sliderMoved |= ImGui.SliderFloat("##level", ref slider, 0f, 1f);
		}

		for (int i = 0; i < lines; i++)
		{
			ImGui.TextUnformatted($"Post {i + 1}");
		}
	}

	// Pulls from near the bottom of the region, below the content, so no control is under the press.
	private void Pull(float pixels)
	{
		Rectangle rect = RectOf(Id);
		float x = rect.MinX + (rect.Width * 0.5f);
		float y = rect.MaxY - 20f;
		Harness.Mouse.Drag(x, y - pixels, x, y, 40);
		Step(10);
	}

	[TestMethod]
	public void PullToRefresh_LongPull_Triggers()
	{
		Start(Draw);

		Pull(200f);

		Assert.AreEqual(1, triggers);
	}

	[TestMethod]
	public void PullToRefresh_ShortPull_DoesNotTrigger()
	{
		Start(Draw);

		Pull(40f);

		Assert.AreEqual(0, triggers);
	}

	[TestMethod]
	public void PullToRefresh_WhilePulling_MovesTheContentDown()
	{
		Start(Draw);
		Rectangle rect = RectOf(Id);
		float x = rect.MinX + (rect.Width * 0.5f);
		float y = rect.MinY + 150f;
		byte[] before = Snapshot();

		Harness.Mouse.MoveTo(x, y);
		Step();
		HarnessMouse.Down(0);
		Step();
		for (int i = 1; i <= 20; i++)
		{
			Harness.Mouse.MoveTo(x, y + (i * 4f));
			Step();
		}

		Assert.IsTrue(PixelsChangedSince(before) > 0, "Pulling the region did not move anything.");
		HarnessMouse.Up(0);
		Step(60);
		Assert.AreEqual(0, triggers);
	}

	[TestMethod]
	public void PullToRefresh_HoldsOpenWhileRefreshing_ThenCloses()
	{
		Start(Draw);
		byte[] closed = Snapshot();

		Pull(200f);
		refreshing = true;
		Step(60);
		Assert.IsTrue(PixelsChangedSince(closed) > 0, "The region did not stay open while refreshing.");

		refreshing = false;
		Step(90);
		Assert.AreEqual(0, PixelsChangedSince(closed), "The region did not close when the refresh finished.");
	}

	[TestMethod]
	public void PullToRefresh_ScrolledDown_DoesNotPull()
	{
		lines = 60;
		Start(Draw);
		Rectangle rect = RectOf(Id);
		Harness.Mouse.Wheel(rect.MinX + 100f, rect.MinY + 100f, -5);
		Step(5);

		Pull(200f);

		Assert.AreEqual(0, triggers);
	}

	[TestMethod]
	public void PullToRefresh_DragOnAControl_MovesTheControlNotTheRegion()
	{
		withSlider = true;
		Start(Draw);
		Rectangle rect = RectOf(Id);

		// The slider is the first line of the content; drag down and to the right from it.
		float y = rect.MinY + ImGui.GetStyle().WindowPadding.Y + (ImGui.GetFrameHeight() * 0.5f) + 2f;
		Harness.Mouse.Drag(rect.MinX + 60f, y, rect.MinX + 200f, y + 200f, 40);
		Step(10);

		Assert.AreEqual(0, triggers);
		Assert.IsTrue(sliderMoved);
	}

	[TestMethod]
	public void PullToRefresh_RefreshStartedByTheCaller_ShowsTheSpinner()
	{
		Start(Draw);
		byte[] closed = Snapshot();

		refreshing = true;
		Step(60);

		Assert.IsTrue(PixelsChangedSince(closed) > 0);
		Assert.AreEqual(0, triggers);
	}
}
