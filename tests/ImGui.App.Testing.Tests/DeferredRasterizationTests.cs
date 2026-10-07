// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Testing.Tests;

using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.App;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Covers the harness rasterizing a frame only when its pixels are read.
/// </summary>
/// <remarks>
/// Rasterizing is nearly all a headless frame costs and most frames a test steps through are never
/// looked at, so the harness defers it. What these pin is that deferring changes nothing a test can
/// see: a read shows the last frame stepped, drawn with the textures it was submitted with, even
/// when a texture changes between the step and the read.
/// </remarks>
[TestClass]
public sealed class DeferredRasterizationTests
{
	private static HarnessOptions Window() => new() { Width = 200, Height = 120 };

	private static byte[] SolidPixels(int width, int height, byte r, byte g, byte b)
	{
		byte[] rgba = new byte[width * height * 4];
		for (int i = 0; i < rgba.Length; i += 4)
		{
			rgba[i] = r;
			rgba[i + 1] = g;
			rgba[i + 2] = b;
			rgba[i + 3] = 255;
		}

		return rgba;
	}

	private static int Red(CapturedFrame frame) => frame.CountPixels(p => p.R > 200 && p.G < 80 && p.B < 80);

	private static int Green(CapturedFrame frame) => frame.CountPixels(p => p.G > 200 && p.R < 80 && p.B < 80);

	private static void DrawImage(ImTextureRef texture)
	{
		ImGui.SetNextWindowPos(Vector2.Zero);
		ImGui.SetNextWindowSize(new Vector2(160, 100));
		ImGui.Begin("image", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoSavedSettings);
		ImGui.Image(texture, new Vector2(64, 64));
		ImGui.End();
	}

	[TestMethod]
	public void FramesNobodyReads_AreNeverRasterized()
	{
		using ImGuiAppHarness harness = ImGuiAppHarness.Start(
			new ImGuiAppConfig { OnRender = _ => ImGui.Text("hello") },
			Window());

		harness.Step(5);
		Assert.AreEqual(0, harness.Renderer.DeferredFramesRasterized, "Stepping alone should rasterize nothing.");

		harness.Capture();
		_ = harness.Target.Pixels;
		Assert.AreEqual(1, harness.Renderer.DeferredFramesRasterized, "A frame read twice should be rasterized once.");

		harness.Step(3);
		_ = harness.Target.Pixels;
		Assert.AreEqual(2, harness.Renderer.DeferredFramesRasterized, "Only the last of the three frames should have been rasterized.");
	}

	[TestMethod]
	public void ARead_ShowsTheLastFrameStepped()
	{
		int frame = 0;
		using ImGuiAppHarness harness = ImGuiAppHarness.Start(
			new ImGuiAppConfig
			{
				OnRender = _ =>
				{
					frame++;
					ImGui.GetForegroundDrawList().AddRectFilled(Vector2.Zero, new Vector2(20 * frame, 10), 0xFFFFFFFFu);
				},
			},
			Window());

		harness.Step(4);

		Rectangle? drawn = harness.Capture().FindBounds(p => p.R == 255 && p.G == 255 && p.B == 255);
		Assert.IsNotNull(drawn);
		Assert.AreEqual(80, drawn.Value.MaxX, "The read should show the fourth frame's rectangle, not an earlier one.");
	}

	[TestMethod]
	public void ATextureUpdatedBetweenStepAndRead_IsDrawnAsSubmitted()
	{
		ImGuiAppTextureInfo? texture = null;
		using ImGuiAppHarness harness = ImGuiAppHarness.Start(
			new ImGuiAppConfig
			{
				OnRender = _ =>
				{
					texture ??= ImGuiApp.CreateTexture(SolidPixels(8, 8, 255, 0, 0), 8, 8);
					DrawImage(texture.TextureRef);
				},
			},
			Window());

		harness.Step(2);
		ImGuiApp.UpdateTexture(texture!, SolidPixels(8, 8, 0, 255, 0), 8, 8);

		CapturedFrame stepped = harness.Capture();
		Assert.IsGreaterThan(1000, Red(stepped), "The stepped frame sampled the red texture, so a later update must not repaint it.");
		Assert.AreEqual(0, Green(stepped));

		harness.Step();
		Assert.IsGreaterThan(1000, Green(harness.Capture()), "The next frame should sample the updated texture.");
	}

	[TestMethod]
	public void ARenderTargetClearedBetweenStepAndRead_IsDrawnAsSubmitted()
	{
		nint target = 0;
		IRenderer3D? renderer3D = null;
		using ImGuiAppHarness harness = ImGuiAppHarness.Start(
			new ImGuiAppConfig
			{
				OnStart = () =>
				{
					Assert.IsTrue(ImGuiApp.TryGetRenderer3D(out renderer3D));
					target = renderer3D!.CreateRenderTarget(8, 8, depth: false);
					renderer3D.Clear(target, new Vector4(1, 0, 0, 1), 1f);
				},
				OnRender = _ => DrawImage(new ImTextureRef { TexID = renderer3D!.GetTargetTexture(target) }),
			},
			Window());

		harness.Step(2);
		renderer3D!.Clear(target, new Vector4(0, 1, 0, 1), 1f);

		CapturedFrame stepped = harness.Capture();
		Assert.IsGreaterThan(1000, Red(stepped), "The stepped frame sampled the red target, so clearing it afterwards must not repaint it.");
		Assert.AreEqual(0, Green(stepped));

		harness.Step();
		Assert.IsGreaterThan(1000, Green(harness.Capture()));
	}
}
