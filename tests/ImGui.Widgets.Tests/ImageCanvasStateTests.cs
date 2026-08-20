// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using System.Numerics;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests the pan/zoom/fit view math behind the image canvas. All pure — no ImGui context required.
/// </summary>
[TestClass]
public class ImageCanvasStateTests
{
	[TestMethod]
	public void FitToViewport_WiderImage_FitsToWidth()
	{
		ImGuiWidgets.ImageCanvasState state = new();

		state.FitToViewport(new Vector2(400, 100), new Vector2(200, 200));

		Assert.AreEqual(0.5f, state.Zoom, 0.0001f);
	}

	[TestMethod]
	public void FitToViewport_TallerImage_FitsToHeight()
	{
		ImGuiWidgets.ImageCanvasState state = new();

		state.FitToViewport(new Vector2(100, 400), new Vector2(200, 200));

		Assert.AreEqual(0.5f, state.Zoom, 0.0001f);
	}

	[TestMethod]
	public void FitToViewport_CentersImage()
	{
		ImGuiWidgets.ImageCanvasState state = new();

		state.FitToViewport(new Vector2(400, 100), new Vector2(200, 200));
		(Vector2 min, Vector2 max) = state.ImageRectInViewport(new Vector2(400, 100), new Vector2(200, 200));

		Assert.AreEqual(0f, min.X, 0.0001f);
		Assert.AreEqual(200f, max.X, 0.0001f);
		Assert.AreEqual(75f, min.Y, 0.0001f);
		Assert.AreEqual(125f, max.Y, 0.0001f);
	}

	[TestMethod]
	public void ResetToActualSize_SetsZoomToOne()
	{
		ImGuiWidgets.ImageCanvasState state = new();
		state.FitToViewport(new Vector2(400, 100), new Vector2(200, 200));

		state.ResetToActualSize();

		Assert.AreEqual(1f, state.Zoom, 0.0001f);
	}

	[TestMethod]
	public void ZoomAt_KeepsAnchorPointStationary()
	{
		// Zooming under the cursor must leave the image pixel under the cursor unmoved.
		ImGuiWidgets.ImageCanvasState state = new();
		Vector2 imageSize = new(400, 400);
		Vector2 viewportSize = new(200, 200);
		state.FitToViewport(imageSize, viewportSize);
		Vector2 anchor = new(150, 50);

		Vector2 imagePointBefore = state.ViewportToImage(anchor, imageSize, viewportSize);
		state.ZoomAt(2f, anchor, viewportSize);
		Vector2 imagePointAfter = state.ViewportToImage(anchor, imageSize, viewportSize);

		Assert.AreEqual(imagePointBefore.X, imagePointAfter.X, 0.001f);
		Assert.AreEqual(imagePointBefore.Y, imagePointAfter.Y, 0.001f);
	}

	[TestMethod]
	public void ZoomAt_ClampsToConfiguredRange()
	{
		ImGuiWidgets.ImageCanvasState state = new() { MinZoom = 0.1f, MaxZoom = 4f };

		state.ZoomAt(1000f, new Vector2(100, 100), new Vector2(200, 200));
		Assert.AreEqual(4f, state.Zoom, 0.0001f);

		state.ZoomAt(0.00001f, new Vector2(100, 100), new Vector2(200, 200));
		Assert.AreEqual(0.1f, state.Zoom, 0.0001f);
	}

	[TestMethod]
	public void PanBy_TranslatesImageRect()
	{
		ImGuiWidgets.ImageCanvasState state = new();
		state.FitToViewport(new Vector2(200, 200), new Vector2(200, 200));
		(Vector2 minBefore, _) = state.ImageRectInViewport(new Vector2(200, 200), new Vector2(200, 200));

		state.PanBy(new Vector2(10, -5));
		(Vector2 minAfter, _) = state.ImageRectInViewport(new Vector2(200, 200), new Vector2(200, 200));

		Assert.AreEqual(minBefore.X + 10f, minAfter.X, 0.0001f);
		Assert.AreEqual(minBefore.Y - 5f, minAfter.Y, 0.0001f);
	}
}
