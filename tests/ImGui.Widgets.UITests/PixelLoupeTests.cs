// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.App;
using ktsu.ImGui.App.Testing;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using SemanticColor = ktsu.Semantics.Color.Color;

/// <summary>
/// Drives <see cref="ImGuiWidgets.PixelLoupe"/> beside an <see cref="ImGuiWidgets.ImageCanvas"/>. The
/// 16 pixel image is fitted to a 160 pixel canvas, so one image pixel is ten screen pixels and canvas
/// point (35, 75) is image pixel (3, 7). Pixel (x, y) is coloured (16x, 16y, 0), so a colour on screen
/// names the pixel it came from.
/// </summary>
[TestClass]
public sealed class PixelLoupeTests : WidgetTest
{
	private const string Id = "loupe";
	private const float CellSize = 12f;
	private static readonly Vector2 ImageSize = new(16f, 16f);
	private static readonly Vector2 CanvasSize = new(160f, 160f);

	private ImGuiAppTextureInfo? texture;
	private readonly ImGuiWidgets.ImageCanvasState canvas = new();
	private Vector2 canvasMin;
	private int radius = 4;
	private bool drawLoupe = true;
	private string label = Id;
	private bool over;
	private int px;
	private int py;
	private int callsThisFrame;
	private int callsTotal;
	private bool sawNegative;

	[TestInitialize]
	public void SetUpCanvas() => canvas.FitToViewport(ImageSize, CanvasSize);

	private SemanticColor ReadPixel(int x, int y)
	{
		callsThisFrame++;
		callsTotal++;
		sawNegative |= x < 0 || y < 0;
		return SemanticColor.FromBytes((byte)(x * 16), (byte)(y * 16), 0);
	}

	private void Draw()
	{
		callsThisFrame = 0;
		texture ??= CreateTestTexture(16);
		ImGuiWidgets.ImageCanvas("img", texture.TextureId, ImageSize, canvas, CanvasSize);
		canvasMin = ImGui.GetItemRectMin();
		if (!drawLoupe)
		{
			ImGui.TextUnformatted("no loupe");
			return;
		}

		over = ImGuiWidgets.PixelLoupe(label, canvas, ImageSize, canvasMin, CanvasSize, ReadPixel, out px, out py, radius, CellSize);
	}

	/// <summary>Moves the pointer to a point given relative to the canvas corner, and draws a frame there.</summary>
	private void HoverCanvas(float x, float y)
	{
		Harness.Mouse.MoveTo(canvasMin.X + x, canvasMin.Y + y);
		Step(2);
	}

	/// <summary>The screen pixel at the centre of a grid cell, counted in cells from the centre cell.</summary>
	private Rgba32 GridCell(int i, int j)
	{
		Rectangle panel = RectOf(Id);
		float centre = (radius + 0.5f) * CellSize;
		return Harness.Capture().GetPixel((int)(panel.MinX + centre + (i * CellSize)), (int)(panel.MinY + centre + (j * CellSize)));
	}

	[TestMethod]
	public void PixelLoupe_MarksItsPanel()
	{
		Start(Draw);

		Assert.IsTrue(IsVisible(Id), "The loupe marked no probe item.");
		Assert.AreEqual(108, RectOf(Id).Width, 1, "A radius of 4 at 12 pixels a cell is 9 cells, 108 pixels, wide.");
	}

	[TestMethod]
	public void PixelLoupe_ReportsTheHoveredPixel()
	{
		Start(Draw);

		HoverCanvas(35, 75);

		Assert.IsTrue(over, "Hovering the image did not report a pixel.");
		Assert.AreEqual(3, px);
		Assert.AreEqual(7, py);
	}

	[TestMethod]
	public void PixelLoupe_ShowsTheCentrePixelsColour()
	{
		Start(Draw);
		HoverCanvas(35, 75);

		Rgba32 centre = GridCell(0, 0);

		Assert.AreEqual(48, centre.R, 2, "The centre cell is not pixel (3, 7)'s red.");
		Assert.AreEqual(112, centre.G, 2, "The centre cell is not pixel (3, 7)'s green.");
	}

	[TestMethod]
	public void PixelLoupe_ShowsNeighbours()
	{
		Start(Draw);
		HoverCanvas(35, 75);

		Assert.AreEqual(64, GridCell(1, 0).R, 2, "The cell right of the centre is not pixel (4, 7).");
	}

	[TestMethod]
	public void PixelLoupe_OnlyReadsInsideTheImage()
	{
		Start(Draw);

		HoverCanvas(5, 5);

		Assert.AreEqual(0, px);
		Assert.IsFalse(sawNegative, "The loupe asked for a pixel left of or above the image.");
		Assert.AreEqual(25, callsThisFrame, "Only the 5×5 quadrant of cells at pixel (0, 0) is in the image.");
	}

	[TestMethod]
	public void PixelLoupe_KeepsTheLastPixelAfterLeaving()
	{
		Start(Draw);
		HoverCanvas(35, 75);

		MoveAway();

		Assert.IsFalse(over, "The pointer is off the canvas, so no pixel is hovered this frame.");
		Assert.AreEqual(3, px);
		Assert.AreEqual(7, py);
	}

	[TestMethod]
	public void PixelLoupe_BeforeAnyHoverReportsNoPixel()
	{
		// The harness's pointer starts at the window corner, which is on the canvas, so the loupe is held
		// back until the pointer has been moved off it. The loupe keeps its pixel per id for the life of
		// the process, so this test uses an id no other test has hovered with.
		label = "fresh_loupe";
		drawLoupe = false;
		Start(Draw);
		MoveAway();
		drawLoupe = true;
		Step(2);

		Assert.IsFalse(over);
		Assert.AreEqual(-1, px);
		Assert.AreEqual(-1, py);
		Assert.AreEqual(0, callsTotal, "The loupe read pixels before any had been hovered.");
	}

	[TestMethod]
	public void PixelLoupe_MarksTheHoveredPixelOnTheCanvas()
	{
		Start(Draw);
		HoverCanvas(35, 75);

		Rectangle marker = RectOf("loupe/pixel");

		Assert.AreEqual(30f, marker.MinX - canvasMin.X, 1f);
		Assert.AreEqual(70f, marker.MinY - canvasMin.Y, 1f);
		Assert.AreEqual(40f, marker.MaxX - canvasMin.X, 1f);
		Assert.AreEqual(80f, marker.MaxY - canvasMin.Y, 1f);
	}

	[TestMethod]
	public void PixelLoupe_RadiusZeroIsOneCell()
	{
		radius = 0;
		Start(Draw);

		Assert.AreEqual(12, RectOf(Id).Width, 1);
	}

	[TestMethod]
	public void PixelLoupe_DoesNotStealTheCanvasPan()
	{
		// The canvas loses the travel inside its drag threshold, so the loupe is judged against the same
		// drag made with no loupe drawn rather than against the raw distance.
		drawLoupe = false;
		Start(Draw);
		DragCanvas();
		float withoutLoupe = canvas.Pan.X;
		Assert.IsTrue(withoutLoupe > 10f, "The drag did not pan the canvas on its own.");

		canvas.FitToViewport(ImageSize, CanvasSize);
		drawLoupe = true;
		Step(2);
		DragCanvas();

		Assert.AreEqual(withoutLoupe, canvas.Pan.X, 1f, "Dragging the canvas under the loupe did not pan it as far.");
	}

	/// <summary>Drags from canvas point (80, 80) twenty pixels to the right.</summary>
	private void DragCanvas()
	{
		Vector2 from = canvasMin + new Vector2(80, 80);
		Harness.Mouse.Drag(from.X, from.Y, from.X + 20, from.Y);
		Step(2);
	}
}
