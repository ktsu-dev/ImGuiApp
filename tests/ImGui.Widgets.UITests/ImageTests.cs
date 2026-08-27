// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using System;
using System.Numerics;

using ktsu.ImGui.App;
using ktsu.ImGui.App.Testing;
using ktsu.ImGui.Color;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>Drives <c>ImGuiWidgets.Image</c> and its centered variants on their own.</summary>
[TestClass]
public sealed class ImageTests : WidgetTest
{
	private const string Name = "image";
	private static readonly Vector2 Size = new(64f, 64f);

	private ImGuiAppTextureInfo? texture;
	private bool clicked;

	private void DrawImage()
	{
		texture ??= CreateTestTexture();
		clicked |= ImGuiWidgets.Image(texture.TextureId, Size);
		Mark(Name);
	}

	[TestMethod]
	public void Image_DrawsAtTheSizeItIsGiven()
	{
		Start(DrawImage);

		Rectangle rect = RectOf(Name);

		Assert.IsTrue(Math.Abs(rect.Width - Size.X) <= 2, $"The image reserved {rect.Width}px of width rather than {Size.X}.");
		Assert.IsTrue(Math.Abs(rect.Height - Size.Y) <= 2, $"The image reserved {rect.Height}px of height rather than {Size.Y}.");
	}

	[TestMethod]
	public void Image_DrawsTheTexturesPixels()
	{
		Start(DrawImage);
		MoveAway();

		Rectangle rect = RectOf(Name);
		CapturedFrame frame = Harness.Capture();
		Rgba32 pixel = frame.GetPixel(rect.MinX + (rect.Width / 2), rect.MinY + (rect.Height / 2));

		// The generated texture is solid orange, so a sample from the middle of the image should
		// be red-dominant rather than the window background.
		Assert.IsTrue(pixel.R > pixel.B, $"The middle of the image was ({pixel.R}, {pixel.G}, {pixel.B}), not the texture's orange.");
	}

	[TestMethod]
	public void Image_ReportsAClick()
	{
		Start(DrawImage);

		Click(Name);

		Assert.IsTrue(clicked, "The image did not report a click.");
	}

	[TestMethod]
	public void Image_TintChangesWhatIsDrawn()
	{
		Start(DrawImage);
		MoveAway();
		byte[] untinted = Snapshot();

		DisposeHarness();
		Start(() =>
		{
			texture ??= CreateTestTexture();
			ImGuiWidgets.Image(texture.TextureId, Size, new ImGuiVector4(0f, 0f, 1f, 1f));
			Mark(Name);
		});

		MoveAway();

		Assert.IsTrue(PixelsChangedSince(untinted) > 0, "A blue tint drew the same pixels as no tint.");
	}

	// The centering variants leave a zero-width spacer as the last submitted item, so where they
	// drew is measured from the pixels rather than from the probe.
	[TestMethod]
	public void ImageCentered_IsCenteredInTheWindow()
	{
		bool show = false;

		Start(() =>
		{
			texture ??= CreateTestTexture();

			if (show)
			{
				ImGuiWidgets.ImageCentered(texture.TextureId, Size);
			}
		});

		MoveAway();
		byte[] blank = Snapshot();
		show = true;
		Step(2);

		Rectangle drawn = BoundsOfDifference(blank) ?? throw new InvalidOperationException("The centered image drew nothing.");
		int imageCenter = drawn.MinX + (drawn.Width / 2);
		int windowCenter = Harness.Options.Width / 2;

		Assert.IsTrue(
			Math.Abs(imageCenter - windowCenter) <= 20,
			$"The centered image sat at {imageCenter} in a window centered on {windowCenter}.");
	}

	[TestMethod]
	public void ImageCenteredWithin_CentersInsideTheContainer()
	{
		Vector2 container = new(300f, 100f);
		bool show = false;

		Start(() =>
		{
			texture ??= CreateTestTexture();

			if (show)
			{
				ImGuiWidgets.ImageCenteredWithin(texture.TextureId, Size, container);
			}
		});

		MoveAway();
		byte[] blank = Snapshot();
		show = true;
		Step(2);

		Rectangle drawn = BoundsOfDifference(blank) ?? throw new InvalidOperationException("The image drew nothing.");
		int imageCenter = drawn.MinX + (drawn.Width / 2);

		// The container starts at the window's content origin, so its center is half its width in.
		Assert.IsTrue(
			Math.Abs(imageCenter - (int)(container.X / 2f)) <= 20,
			$"The image centered at {imageCenter} inside a {container.X}px container.");
	}
}
