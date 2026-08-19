// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Testing.Tests;

using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.App;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Covers the harness installing its rasterizer as ImGuiApp's renderer backend.
/// </summary>
/// <remarks>
/// An application that shows an image it generated uploads it through ImGuiApp rather than loading
/// it from a file, and every one of those calls routes through the backend. A harness that leaves
/// no backend installed cannot render such an application at all, so these tests guard the wiring
/// rather than the rasterizer.
/// </remarks>
[TestClass]
public sealed class RendererBackendTests
{
	private static HarnessOptions Window() => new() { Width = 200, Height = 120 };

	/// <summary>Four opaque red pixels, tightly packed.</summary>
	private static byte[] RedPixels(int width, int height)
	{
		byte[] rgba = new byte[width * height * 4];
		for (int i = 0; i < rgba.Length; i += 4)
		{
			rgba[i] = 255;
			rgba[i + 3] = 255;
		}

		return rgba;
	}

	[TestMethod]
	public void CreateTexture_SucceedsWhileAHarnessIsRunning()
	{
		using ImGuiAppHarness harness = ImGuiAppHarness.Start(new ImGuiAppConfig(), Window());

		ImGuiAppTextureInfo texture = ImGuiApp.CreateTexture(RedPixels(2, 2), 2, 2);

		Assert.AreEqual(2, texture.Width);
		Assert.AreEqual(2, texture.Height);
		Assert.AreNotEqual(nint.Zero, texture.TextureId, "A texture with no handle cannot be drawn.");
	}

	[TestMethod]
	public void UpdateTexture_ReusesTheHandleWhenTheSizeIsUnchanged()
	{
		using ImGuiAppHarness harness = ImGuiAppHarness.Start(new ImGuiAppConfig(), Window());
		ImGuiAppTextureInfo texture = ImGuiApp.CreateTexture(RedPixels(2, 2), 2, 2);
		nint original = texture.TextureId;

		ImGuiApp.UpdateTexture(texture, RedPixels(2, 2), 2, 2);

		Assert.AreEqual(original, texture.TextureId, "A same-size update should have been made in place.");
	}

	[TestMethod]
	public void UpdateTexture_RecreatesTheTextureWhenTheSizeChanges()
	{
		using ImGuiAppHarness harness = ImGuiAppHarness.Start(new ImGuiAppConfig(), Window());
		ImGuiAppTextureInfo texture = ImGuiApp.CreateTexture(RedPixels(2, 2), 2, 2);

		ImGuiApp.UpdateTexture(texture, RedPixels(4, 3), 4, 3);

		Assert.AreEqual(4, texture.Width);
		Assert.AreEqual(3, texture.Height);
	}

	[TestMethod]
	public void UploadedTexture_ReachesTheRasterizer()
	{
		ImGuiAppTextureInfo? texture = null;
		using ImGuiAppHarness harness = ImGuiAppHarness.Start(
			new ImGuiAppConfig
			{
				OnRender = _ =>
				{
					texture ??= ImGuiApp.CreateTexture(RedPixels(8, 8), 8, 8);

					ImGui.SetNextWindowPos(Vector2.Zero);
					ImGui.SetNextWindowSize(new Vector2(160, 100));
					ImGui.Begin("image", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoSavedSettings);
					ImGui.Image(texture.TextureRef, new Vector2(64, 64));
					ImGui.End();
				},
			},
			Window());

		harness.Step(2);

		// The rasterizer only has these pixels if the upload reached it, so counting them proves
		// the whole path rather than just that the call returned a handle.
		int red = harness.Capture().CountPixels(p => p.R > 200 && p.G < 80 && p.B < 80);
		Assert.IsGreaterThan(1000, red, "The uploaded texture was not drawn, so the backend never received it.");
	}

	[TestMethod]
	public void ASecondHarness_InstallsItsOwnBackend()
	{
		using (ImGuiAppHarness first = ImGuiAppHarness.Start(new ImGuiAppConfig(), Window()))
		{
			ImGuiApp.CreateTexture(RedPixels(2, 2), 2, 2);
		}

		// The session releases the backend rather than disposing it, and installs a fresh one next
		// time. A harness that left the previous, disposed rasterizer in place would fail here, so
		// this is what keeps a suite of many scenarios from poisoning itself after the first.
		using ImGuiAppHarness second = ImGuiAppHarness.Start(new ImGuiAppConfig(), Window());
		ImGuiAppTextureInfo texture = ImGuiApp.CreateTexture(RedPixels(2, 2), 2, 2);

		Assert.AreNotEqual(nint.Zero, texture.TextureId);
	}
}
