// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using System.Numerics;

using ktsu.ImGui.App;
using ktsu.ImGui.App.Testing;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>Drives <see cref="ImGuiWidgets.ImageCompare"/> on its own.</summary>
[TestClass]
public sealed class ImageCompareTests : WidgetTest
{
	private const string Id = "Compare";
	private static readonly Vector2 CanvasSize = new(320f, 240f);
	private static readonly Vector2 ImageSize = new(32f, 32f);

	private ImGuiAppTextureInfo? before;
	private ImGuiAppTextureInfo? after;
	private readonly ImGuiWidgets.ImageCanvasState state = new();
	private float split = 0.5f;
	private bool changed;
	private ImGuiWidgets.ImageCompareMode mode = ImGuiWidgets.ImageCompareMode.Wipe;

	public ImageCompareTests() => state.FitToViewport(ImageSize, CanvasSize);

	private void Draw()
	{
		before ??= CreateTestTexture(32);
		after ??= CreateSolidTexture(0, 0, 255);
		changed |= ImGuiWidgets.ImageCompare(Id, before.TextureId, after.TextureId, ImageSize, state, ref split, CanvasSize, mode);
	}

	[TestMethod]
	public void ImageCompare_MarksItsRegions()
	{
		Start(Draw);

		Assert.IsTrue(IsVisible($"{Id}/canvas"), "The canvas was not marked.");
		Assert.IsTrue(IsVisible($"{Id}/before"), "The before region was not marked.");
		Assert.IsTrue(IsVisible($"{Id}/after"), "The after region was not marked.");
		Assert.IsTrue(IsVisible($"{Id}/divider"), "The divider was not marked.");
	}

	[TestMethod]
	public void ImageCompare_ShowsBeforeLeftAndAfterRight()
	{
		Start(Draw);

		AssertOrange(PixelAt(120, 120), "left of the divider");
		AssertBlue(PixelAt(200, 120), "right of the divider");
	}

	[TestMethod]
	public void ImageCompare_DraggingTheDividerMovesTheSplit()
	{
		Start(Draw);

		Vector2 from = CenterOf($"{Id}/divider");
		Harness.Mouse.Drag(from.X, from.Y, from.X + 64f, from.Y);
		Step();

		Assert.IsTrue(split is >= 0.68f and <= 0.72f, $"The split was {split} rather than about 0.7.");
		Assert.IsTrue(changed, "Moving the divider reported no change.");
	}

	[TestMethod]
	public void ImageCompare_TheDividerIsClampedToTheCanvas()
	{
		Start(Draw);

		Vector2 from = CenterOf($"{Id}/divider");
		Harness.Mouse.Drag(from.X, from.Y, from.X - 400f, from.Y);
		Step();

		Assert.AreEqual(0f, split);
	}

	[TestMethod]
	public void ImageCompare_DraggingAwayFromTheDividerPans()
	{
		Start(Draw);

		Rectangle canvas = RectOf($"{Id}/canvas");
		Harness.Mouse.Drag(canvas.MinX + 80f, canvas.MinY + 120f, canvas.MinX + 140f, canvas.MinY + 120f);
		Step();

		Assert.IsGreaterThan(30f, state.Pan.X, $"Dragging the image panned it by {state.Pan.X}.");
		Assert.AreEqual(0.5f, split);
		Assert.IsFalse(changed, "Panning reported a change to the split.");
	}

	[TestMethod]
	public void ImageCompare_WheelZoomsBothSidesTogether()
	{
		Start(Draw);

		Vector2 centre = CenterOf($"{Id}/canvas");
		Harness.Mouse.Wheel(centre.X, centre.Y, 1);
		Step();

		Assert.AreEqual(8.25f, state.Zoom, 1e-3f);
		AssertOrange(PixelAt(150, 120), "left of the divider after zooming");
		AssertBlue(PixelAt(170, 120), "right of the divider after zooming");
	}

	[TestMethod]
	public void ImageCompare_NaNSplitIsNormalized()
	{
		split = float.NaN;
		Start(Draw);

		Assert.AreEqual(0.5f, split);
		Assert.IsTrue(changed, "Normalizing the split reported no change.");
	}

	[TestMethod]
	public void ImageCompare_SideBySideDrawsBothPanes()
	{
		mode = ImGuiWidgets.ImageCompareMode.SideBySide;
		Start(Draw);

		Vector2 left = CenterOf($"{Id}/before");
		Vector2 right = CenterOf($"{Id}/after");
		AssertOrange(Harness.Target.GetPixel((int)left.X, (int)left.Y), "the left pane");
		AssertBlue(Harness.Target.GetPixel((int)right.X, (int)right.Y), "the right pane");
		Assert.IsFalse(IsVisible($"{Id}/divider"), "Side by side marked a divider.");
	}

	[TestMethod]
	public void ImageCompare_SideBySideIgnoresTheSplit()
	{
		mode = ImGuiWidgets.ImageCompareMode.SideBySide;
		Start(Draw);

		Vector2 centre = CenterOf($"{Id}/canvas");
		Harness.Mouse.Drag(centre.X, centre.Y, centre.X + 40f, centre.Y);
		Step();

		Assert.AreEqual(0.5f, split);
		Assert.IsFalse(changed, "A side-by-side drag reported a change to the split.");
	}

	private Rgba32 PixelAt(int canvasX, int canvasY)
	{
		Rectangle canvas = RectOf($"{Id}/canvas");
		return Harness.Target.GetPixel(canvas.MinX + canvasX, canvas.MinY + canvasY);
	}

	private static void AssertOrange(Rgba32 pixel, string where) =>
		Assert.IsTrue(pixel.R > 200 && pixel.B < 60, $"Expected the orange before image {where}, found ({pixel.R}, {pixel.G}, {pixel.B}).");

	private static void AssertBlue(Rgba32 pixel, string where) =>
		Assert.IsTrue(pixel.B > 200 && pixel.R < 60, $"Expected the blue after image {where}, found ({pixel.R}, {pixel.G}, {pixel.B}).");

	private static ImGuiAppTextureInfo CreateSolidTexture(byte r, byte g, byte b)
	{
		const int Size = 32;
		byte[] rgba = new byte[Size * Size * 4];
		for (int i = 0; i < Size * Size; i++)
		{
			rgba[(i * 4) + 0] = r;
			rgba[(i * 4) + 1] = g;
			rgba[(i * 4) + 2] = b;
			rgba[(i * 4) + 3] = 255;
		}

		return ImGuiApp.CreateTexture(rgba, Size, Size);
	}
}
