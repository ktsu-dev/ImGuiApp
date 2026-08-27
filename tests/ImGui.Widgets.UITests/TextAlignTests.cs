// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.App.Testing;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Drives the Hexa-backed alignment helpers <see cref="ImGuiWidgets.TextCenteredV"/>,
/// <see cref="ImGuiWidgets.TextCenteredH"/> and <see cref="ImGuiWidgets.TextCenteredVH"/> on their own.
/// </summary>
[TestClass]
public sealed class TextAlignTests : WidgetTest
{
	private const string Sample = "Aligned";

	private bool show;

	[TestMethod]
	public void TextCenteredH_SitsInTheMiddleOfTheWindow()
	{
		show = false;

		Start(() =>
		{
			if (show)
			{
				ImGuiWidgets.TextCenteredH(Sample);
			}
		});

		byte[] blank = Snapshot();
		show = true;
		Step(2);

		Rectangle drawn = BoundsOfDifference(blank) ?? throw new InvalidOperationException("Centered text drew nothing.");
		int center = drawn.MinX + (drawn.Width / 2);

		Assert.IsTrue(
			Math.Abs(center - (Harness.Options.Width / 2)) <= 30,
			$"Horizontally centered text sat at {center} in a {Harness.Options.Width}px window.");
	}

	[TestMethod]
	public void TextCenteredV_OffsetsTheTextDownFromTheCursor()
	{
		show = false;

		Start(() =>
		{
			if (show)
			{
				ImGuiWidgets.TextCenteredV(Sample);
			}
			else
			{
				ImGui.Dummy(Vector2.Zero);
			}
		});

		byte[] blank = Snapshot();
		show = true;
		Step(2);

		Rectangle drawn = BoundsOfDifference(blank) ?? throw new InvalidOperationException("Vertically centered text drew nothing.");

		// Vertical centering pushes the text down from where the cursor sits, which is a few pixels
		// below the top of the window's content region.
		Assert.IsTrue(drawn.MinY > 40, $"Vertically centered text was drawn at {drawn.MinY}, no lower than plain text would be.");
		Assert.IsTrue(drawn.MaxY < Harness.Options.Height, "Vertically centered text was drawn off the bottom of the window.");
	}

	[TestMethod]
	public void TextCenteredVH_DrawsSomething()
	{
		show = false;

		Start(() =>
		{
			if (show)
			{
				ImGuiWidgets.TextCenteredVH(Sample);
			}
		});

		byte[] blank = Snapshot();
		show = true;
		Step(2);

		Assert.IsNotNull(BoundsOfDifference(blank), "Doubly centered text drew nothing.");
	}

	[TestMethod]
	public void ImageAlignment_HelpersDrawWithoutABackendOfTheirOwn()
	{
		ktsu.ImGui.App.ImGuiAppTextureInfo? texture = null;

		Start(() =>
		{
			texture ??= CreateTestTexture();
			ImGui.Button("Tall", new Vector2(80f, 60f));
			ImGui.SameLine();
			ImGuiWidgets.ImageCenteredV(texture.TextureId, new Vector2(32f, 32f));
			Mark("centered-v");
			ImGuiWidgets.ImageCenteredH(texture.TextureId, new Vector2(32f, 32f));
			Mark("centered-h");
			ImGuiWidgets.ImageCenteredVH(texture.TextureId, new Vector2(32f, 32f));
			Mark("centered-vh");
			ImGuiWidgets.ImageScaleTo(texture.TextureId, new Vector2(64f, 32f), new Vector2(32f, 32f));
			Mark("scaled");
		});

		Assert.IsTrue(IsVisible("centered-v"), "A vertically centered image drew no item.");
		Assert.IsTrue(IsVisible("centered-h"), "A horizontally centered image drew no item.");
		Assert.IsTrue(IsVisible("centered-vh"), "A doubly centered image drew no item.");
		Assert.IsTrue(IsVisible("scaled"), "A scaled image drew no item.");
	}

	[TestMethod]
	public void ImageScaleTo_PreservesAspectRatio()
	{
		ktsu.ImGui.App.ImGuiAppTextureInfo? texture = null;

		Start(() =>
		{
			texture ??= CreateTestTexture();
			ImGuiWidgets.ImageScaleTo(texture.TextureId, new Vector2(64f, 32f), new Vector2(64f, 64f));
			Mark("scaled");
		});

		Rectangle rect = RectOf("scaled");

		// A 2:1 image fitted into a square box is drawn half as tall as it is wide.
		Assert.IsTrue(
			rect.Width >= rect.Height,
			$"A 2:1 image was drawn {rect.Width}x{rect.Height}, taller than it is wide.");
	}
}
