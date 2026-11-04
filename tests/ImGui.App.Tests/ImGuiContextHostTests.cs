// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Tests;

using System;
using System.Collections.Generic;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.App.ImGuiController;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// The context host is what the desktop controller and the headless harness share (#313), so these
/// drive it with nothing behind it but a recording uploader: no window, no OpenGL, no rasterizer.
/// </summary>
[TestClass]
public sealed class ImGuiContextHostTests
{
	/// <summary>Hands out texture ids and records what it was asked to do.</summary>
	private sealed class CountingUploader : ITextureUploader
	{
		private nint nextId = 100;

		public List<string> Calls { get; } = [];

		public nint Create(int width, int height, nint pixels)
		{
			Calls.Add("create");
			return nextId++;
		}

		public void Update(nint textureId, int width, int height, ImTextureRect rect, nint pixels) => Calls.Add("update");

		public void Destroy(nint textureId) => Calls.Add($"destroy {textureId}");
	}

	[TestMethod]
	public void Constructor_AdvertisesWhatEveryBackendSupports()
	{
		using ImGuiContextHost host = new(new CountingUploader(), ImGuiAppExtensions.None);

		ImGuiBackendFlags flags = ImGui.GetIO().BackendFlags;
		Assert.IsTrue(flags.HasFlag(ImGuiBackendFlags.RendererHasTextures), "Without this ImGui bakes one atlas up front and no glyph can be rasterized at a size that was not registered.");
		Assert.IsTrue(flags.HasFlag(ImGuiBackendFlags.RendererHasVtxOffset), "Both renderers honour a command's vertex offset, so large draw lists need not be split.");
		Assert.IsTrue(flags.HasFlag(ImGuiBackendFlags.HasMouseCursors), "Without this ImGui stops offering window edges to resize.");
	}

	[TestMethod]
	public void NewFrame_SetsTheDisplayMetricsItIsGiven()
	{
		using ImGuiContextHost host = new(new CountingUploader(), ImGuiAppExtensions.None);

		host.NewFrame(new Vector2(320, 240), new Vector2(2, 2), 0.25f);

		ImGuiIOPtr io = ImGui.GetIO();
		Assert.AreEqual(new Vector2(320, 240), io.DisplaySize);
		Assert.AreEqual(new Vector2(2, 2), io.DisplayFramebufferScale);
		Assert.AreEqual(0.25f, io.DeltaTime);
		host.EndFrame();
	}

	[TestMethod]
	public void FirstFrame_CreatesTheAtlasTexture()
	{
		CountingUploader uploader = new();
		using ImGuiContextHost host = new(uploader, ImGuiAppExtensions.None);

		DrawText(host, sizePixels: null);

		Assert.AreEqual(ImTextureStatus.Ok, ImGui.GetIO().Fonts.TexData.Status, "The host should have reconciled the atlas texture.");
		Assert.AreEqual(100, (nint)(nuint)ImGui.GetIO().Fonts.TexData.GetTexID(), "The atlas should carry the id the uploader handed out.");
		Assert.AreEqual("create", uploader.Calls[0]);
	}

	[TestMethod]
	public void DrawingAtAnUnseenSize_RasterizesOnDemand()
	{
		using ImGuiContextHost host = new(new CountingUploader(), ImGuiAppExtensions.None);

		// Settle: the atlas is created on the first frame and holds the body size after the second.
		DrawText(host, sizePixels: null);
		DrawText(host, sizePixels: null);
		int settled = host.TextureUploadCount;

		DrawText(host, sizePixels: 48f);

		Assert.IsGreaterThan(settled, host.TextureUploadCount, "A size never drawn before should have been rasterized into the atlas, which no baked-up-front atlas could do.");

		DrawText(host, sizePixels: 48f);

		Assert.AreEqual(settled + 1, host.TextureUploadCount, "Drawing the same size again should not re-rasterize anything.");
	}

	[TestMethod]
	public void EndFrame_ReturnsTheFramesDrawData()
	{
		using ImGuiContextHost host = new(new CountingUploader(), ImGuiAppExtensions.None);

		host.NewFrame(new Vector2(200, 120), Vector2.One, 1f / 60f);
		ImGui.SetNextWindowPos(Vector2.Zero);
		ImGui.SetNextWindowSize(new Vector2(150, 80));
		ImGui.Begin("probe", ImGuiWindowFlags.NoSavedSettings);
		ImGui.TextUnformatted("hello");
		ImGui.End();
		ImDrawDataPtr drawData = host.EndFrame();

		Assert.IsGreaterThan(0, drawData.CmdListsCount, "A window with text should produce at least one command list.");
	}

	[TestMethod]
	public void Dispose_ReleasesTheTexturesAndTheContext()
	{
		CountingUploader uploader = new();
		ImGuiContextHost host = new(uploader, ImGuiAppExtensions.None);
		DrawText(host, sizePixels: null);

		host.Dispose();

		Assert.Contains("destroy 100", uploader.Calls, "The atlas belongs to the renderer and must be released with the context.");
		unsafe
		{
			Assert.IsTrue(ImGui.GetCurrentContext().Handle is null, "The context should be gone.");
		}
	}

	[TestMethod]
	public void Dispose_IsIdempotent()
	{
		ImGuiContextHost host = new(new CountingUploader(), ImGuiAppExtensions.None);

		host.Dispose();

		// A second dispose must not destroy the ImGui context twice.
		host.Dispose();
	}

	[TestMethod]
	public void NewFrame_AfterDispose_Throws()
	{
		ImGuiContextHost host = new(new CountingUploader(), ImGuiAppExtensions.None);
		host.Dispose();

		Assert.ThrowsExactly<ObjectDisposedException>(() => host.NewFrame(Vector2.One, Vector2.One, 1f));
	}

	[TestMethod]
	public void Constructor_NullUploader_Throws() =>
		Assert.ThrowsExactly<ArgumentNullException>(() => new ImGuiContextHost(null!, ImGuiAppExtensions.None));

	private static void DrawText(ImGuiContextHost host, float? sizePixels)
	{
		host.NewFrame(new Vector2(200, 200), Vector2.One, 1f / 60f);
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
		host.EndFrame();
	}
}
