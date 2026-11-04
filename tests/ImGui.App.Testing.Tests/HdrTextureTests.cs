// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Testing.Tests;

using System;

using Hexa.NET.ImGui;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Half-float textures through <see cref="ImGuiApp"/>, and the HDR screen a harness can stand in for.
/// </summary>
[TestClass]
public sealed class HdrTextureTests
{
	private static readonly HdrDisplayInfo Bright = HdrDisplayInfo.FromLuminance(203, 1015);

	private static ImGuiAppHarness StartHarness(Action render, HdrDisplayInfo? display = null) =>
		ImGuiAppHarness.Start(
			new ImGuiAppConfig { OnRender = _ => render() },
			new HarnessOptions { Width = 64, Height = 48, HdrDisplay = display ?? HdrDisplayInfo.Sdr });

	[TestMethod]
	public void AHarness_TellsTheApplicationWhatScreenItStandsIn()
	{
		using (ImGuiAppHarness harness = StartHarness(() => { }, Bright))
		{
			Assert.AreEqual(Bright, ImGuiApp.HdrDisplay);
			harness.HdrDisplay = HdrDisplayInfo.Sdr;
			Assert.IsFalse(ImGuiApp.HdrDisplay.IsHdr, "Turning HDR off reaches the application.");
		}

		Assert.AreEqual(HdrDisplayInfo.Sdr, ImGuiApp.HdrDisplay, "The next session starts on an SDR screen.");
	}

	[TestMethod]
	public void AHalfFloatTexture_KeepsItsValues_AndIsDrawnAsAnSdrScreenShowsIt()
	{
		ImGuiAppTextureInfo? texture = null;
		using ImGuiAppHarness harness = StartHarness(() =>
		{
			if (texture is not null)
			{
				ImGui.SetNextWindowPos(new(0, 0));
				ImGui.SetNextWindowSize(new(64, 48));
				ImGui.Begin("hdr", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoBackground);
				ImGui.Image(texture.TextureRef, new(32, 32));
				ImGui.End();
			}
		});

		Half bright = (Half)HdrEncoding.Encode(4);
		Half[] pixels = [bright, bright, bright, (Half)1];
		texture = ImGuiApp.CreateTexture(pixels, 1, 1);
		harness.Step(2);

		Assert.IsTrue(harness.TryGetHalfFloatPixels(texture.TextureId, out Half[]? kept));
		CollectionAssert.AreEqual(pixels, kept);
		Rgba32 shown = harness.Capture().GetPixel(20, 20);
		Assert.AreEqual(new Rgba32(255, 255, 255, 255), shown, "Four times white is white on an SDR target.");
	}

	[TestMethod]
	public void UpdatingAHalfFloatTexture_ToAnotherSize_RemakesIt()
	{
		using ImGuiAppHarness harness = StartHarness(() => { });
		ImGuiAppTextureInfo texture = ImGuiApp.CreateTexture(new Half[4], 1, 1);

		ImGuiApp.UpdateTexture(texture, new Half[4 * 6], 3, 2);

		Assert.AreEqual((3, 2), (texture.Width, texture.Height));
		Assert.IsTrue(harness.TryGetHalfFloatPixels(texture.TextureId, out Half[]? kept));
		Assert.HasCount(24, kept);
		ImGuiApp.DeleteTexture(texture);
		Assert.IsFalse(harness.TryGetHalfFloatPixels(texture.TextureId, out _));
	}

	[TestMethod]
	public void AHalfFloatTexture_OfTheWrongLength_IsRefused()
	{
		using ImGuiAppHarness harness = StartHarness(() => { });

		Assert.ThrowsExactly<ArgumentException>(() => ImGuiApp.CreateTexture(new Half[3], 1, 1));
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => ImGuiApp.CreateTexture(ReadOnlySpan<Half>.Empty, 0, 1));
	}
}
