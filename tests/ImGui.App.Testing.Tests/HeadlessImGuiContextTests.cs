// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Testing.Tests;

using System.Numerics;

using Hexa.NET.ImGui;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class HeadlessImGuiContextTests
{
	[TestMethod]
	public void Constructor_SetsDisplaySize()
	{
		using SoftwareRenderer renderer = new(320, 240);
		using HeadlessImGuiContext context = new(320, 240, 1.0f, renderer);

		Assert.AreEqual(320f, HeadlessImGuiContext.IO.DisplaySize.X, "Display width should match the requested size.");
		Assert.AreEqual(240f, HeadlessImGuiContext.IO.DisplaySize.Y, "Display height should match the requested size.");
	}

	[TestMethod]
	public void Dispose_IsIdempotent()
	{
		using SoftwareRenderer renderer = new(64, 64);
		HeadlessImGuiContext context = new(64, 64, 1.0f, renderer);

		context.Dispose();

		// A second dispose must not destroy the ImGui context twice; the harness is held in using
		// blocks that can overlap an explicit dispose in a failing test.
		context.Dispose();
	}

	[TestMethod]
	public void Constructor_AdvertisesDynamicTextures()
	{
		using SoftwareRenderer renderer = new(64, 64);
		using HeadlessImGuiContext context = new(64, 64, 1.0f, renderer);

		Assert.IsTrue(
			HeadlessImGuiContext.IO.BackendFlags.HasFlag(ImGuiBackendFlags.RendererHasTextures),
			"Without this flag ImGui bakes one atlas up front and no glyph can be rasterized at a size that was not registered.");
	}

	[TestMethod]
	public void FirstFrame_CreatesTheAtlasTexture()
	{
		using SoftwareRenderer renderer = new(200, 120);
		using HeadlessImGuiContext context = new(200, 120, 1.0f, renderer);

		DrawText(context, sizePixels: null);

		Assert.AreEqual(ImTextureStatus.Ok, HeadlessImGuiContext.IO.Fonts.TexData.Status, "The renderer should have reconciled the atlas texture.");
		Assert.AreNotEqual(0, (nint)(nuint)HeadlessImGuiContext.IO.Fonts.TexData.GetTexID(), "The renderer should have been handed a texture id.");
	}

	[TestMethod]
	public void DrawingAtAnUnseenSize_RasterizesOnDemand()
	{
		using SoftwareRenderer renderer = new(200, 200);
		using HeadlessImGuiContext context = new(200, 200, 1.0f, renderer);

		// Settle: the atlas is created on the first frame and holds the body size after the second.
		DrawText(context, sizePixels: null);
		DrawText(context, sizePixels: null);
		int settled = context.TextureUploadCount;

		DrawText(context, sizePixels: 48f);

		Assert.IsGreaterThan(settled, context.TextureUploadCount, "A size never drawn before should have been rasterized into the atlas, which no baked-up-front atlas could do.");

		DrawText(context, sizePixels: 48f);

		Assert.AreEqual(settled + 1, context.TextureUploadCount, "Drawing the same size again should not re-rasterize anything.");
	}

	private static void DrawText(HeadlessImGuiContext context, float? sizePixels)
	{
		context.BeginFrame(1f / 60f);
		ImGui.Begin("probe", ImGuiWindowFlags.NoSavedSettings);
		if (sizePixels.HasValue)
		{
			ImGui.PushFont(ImGui.GetFont(), sizePixels.Value);
		}

		ImGui.TextUnformatted("hello");
		if (sizePixels.HasValue)
		{
			ImGui.PopFont();
		}

		ImGui.End();
		context.EndFrame();
	}

	[TestMethod]
	public void Constructor_DisablesIniPersistence()
	{
		using SoftwareRenderer renderer = new(64, 64);
		using HeadlessImGuiContext context = new(64, 64, 1.0f, renderer);

		unsafe
		{
			Assert.IsTrue(HeadlessImGuiContext.IO.Handle->IniFilename is null, "Persisting layout would let one test inherit another's state.");
		}
	}

	[TestMethod]
	public void BeginFrame_ThenEndFrame_ProducesDrawData()
	{
		using SoftwareRenderer renderer = new(200, 120);
		using HeadlessImGuiContext context = new(200, 120, 1.0f, renderer);

		context.BeginFrame(1f / 60f);
		ImGui.SetNextWindowPos(Vector2.Zero);
		ImGui.SetNextWindowSize(new Vector2(150, 80));
		ImGui.Begin("probe", ImGuiWindowFlags.NoSavedSettings);
		ImGui.TextUnformatted("hello");
		ImGui.End();
		context.EndFrame();

		Assert.IsTrue(ImGui.GetDrawData().CmdListsCount > 0, "A window with text should produce at least one command list.");
	}

	[TestMethod]
	public void EndFrame_RasterizesIntoTheRenderTarget()
	{
		using SoftwareRenderer renderer = new(200, 120);
		using HeadlessImGuiContext context = new(200, 120, 1.0f, renderer);

		Rgba32 background = new(0, 0, 0, 255);
		renderer.Clear(background);

		context.BeginFrame(1f / 60f);
		ImGui.SetNextWindowPos(Vector2.Zero);
		ImGui.SetNextWindowSize(new Vector2(150, 80));
		ImGui.Begin("probe", ImGuiWindowFlags.NoSavedSettings);
		ImGui.TextUnformatted("hello");
		ImGui.End();
		context.EndFrame();

		int changed = 0;
		for (int y = 0; y < renderer.Target.Height; y++)
		{
			for (int x = 0; x < renderer.Target.Width; x++)
			{
				if (renderer.Target.GetPixel(x, y) != background)
				{
					changed++;
				}
			}
		}

		// This is the end-to-end proof that ImGui geometry actually reaches pixels through the
		// software path, which every later assertion depends on.
		Assert.IsTrue(changed > 100, $"Rendering a window should change many pixels, but only {changed} changed.");
	}

	[TestMethod]
	public void Constructor_NullRenderer_Throws() =>
		Assert.ThrowsExactly<ArgumentNullException>(() => new HeadlessImGuiContext(8, 8, 1f, null!));
}
