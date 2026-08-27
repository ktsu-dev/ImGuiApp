// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using System;
using System.Numerics;

using ktsu.ImGui.App;
using ktsu.ImGui.App.Testing;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>Drives <see cref="ImGuiWidgets.ImageCanvas"/> on its own.</summary>
[TestClass]
public sealed class ImageCanvasTests : WidgetTest
{
	private const string Id = "canvas";
	private static readonly Vector2 CanvasSize = new(320f, 240f);
	private static readonly Vector2 ImageSize = new(128f, 128f);

	private ImGuiAppTextureInfo? texture;
	private ImGuiWidgets.ImageCanvasState state = new();

	private void Draw()
	{
		texture ??= CreateTestTexture();
		ImGuiWidgets.ImageCanvas(Id, texture.TextureId, ImageSize, state, CanvasSize);
	}

	[TestMethod]
	public void ImageCanvas_IsDrawnAndMarksItself()
	{
		Start(Draw);

		Assert.IsTrue(IsVisible(Id), "The canvas marked no probe item.");
		AssertSomethingWasDrawn("the canvas");
	}

	[TestMethod]
	public void ImageCanvas_ClaimsTheSizeItIsGiven()
	{
		Start(Draw);

		Rectangle rect = RectOf(Id);

		Assert.IsTrue(Math.Abs(rect.Width - CanvasSize.X) <= 2, $"The canvas claimed {rect.Width}px of width rather than {CanvasSize.X}.");
		Assert.IsTrue(Math.Abs(rect.Height - CanvasSize.Y) <= 2, $"The canvas claimed {rect.Height}px of height rather than {CanvasSize.Y}.");
	}

	[TestMethod]
	public void ImageCanvas_DraggingPansTheView()
	{
		state = new ImGuiWidgets.ImageCanvasState();
		Start(Draw);

		DragAcross(Id, 0.3f, 0.7f);

		Assert.AreNotEqual(0f, state.Pan.X, "Dragging across the canvas did not pan it.");
	}

	[TestMethod]
	public void ImageCanvas_ScrollingZooms()
	{
		state = new ImGuiWidgets.ImageCanvasState();
		Start(Draw);

		Vector2 center = CenterOf(Id);
		Harness.Mouse.Wheel(center.X, center.Y, 3);
		Step();

		Assert.IsTrue(state.Zoom > 1f, $"Scrolling up left the zoom at {state.Zoom}.");
	}

	[TestMethod]
	public void ImageCanvas_ScrollingBackZoomsOut()
	{
		state = new ImGuiWidgets.ImageCanvasState();
		Start(Draw);

		Vector2 center = CenterOf(Id);
		Harness.Mouse.Wheel(center.X, center.Y, -3);
		Step();

		Assert.IsTrue(state.Zoom < 1f, $"Scrolling down left the zoom at {state.Zoom}.");
	}

	[TestMethod]
	public void ImageCanvas_ZoomStaysWithinItsLimits()
	{
		state = new ImGuiWidgets.ImageCanvasState { MinZoom = 0.5f, MaxZoom = 2f };
		Start(Draw);

		Vector2 center = CenterOf(Id);

		for (int i = 0; i < 10; i++)
		{
			Harness.Mouse.Wheel(center.X, center.Y, 3);
		}

		Step();

		Assert.IsTrue(state.Zoom <= 2f, $"The zoom ran past its maximum to {state.Zoom}.");
	}

	[TestMethod]
	public void ImageCanvas_DoubleClickFitsTheImage()
	{
		state = new ImGuiWidgets.ImageCanvasState();
		state.PanBy(new Vector2(40f, 25f));
		Start(Draw);

		Vector2 center = CenterOf(Id);
		Harness.Mouse.Click(center.X, center.Y);
		Harness.Mouse.Click(center.X, center.Y);
		Step();

		Assert.AreEqual(Vector2.Zero, state.Pan, "A double click did not recenter the view.");
	}

	[TestMethod]
	public void ImageCanvas_RedrawsWhenTheViewMoves()
	{
		state = new ImGuiWidgets.ImageCanvasState();
		Start(Draw);
		MoveAway();
		byte[] centered = Snapshot();

		state.PanBy(new Vector2(60f, 0f));
		Step(2);
		MoveAway();

		Assert.IsTrue(PixelsChangedSince(centered) > 0, "Panning the view changed nothing on screen.");
	}
}
