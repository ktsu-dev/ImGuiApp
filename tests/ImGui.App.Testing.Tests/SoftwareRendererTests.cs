// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Testing.Tests;

using System.Collections.Generic;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class SoftwareRendererTests
{
	[TestMethod]
	public void CreateTexture_ReturnsDistinctNonZeroHandles()
	{
		using SoftwareRenderer renderer = new(8, 8);
		byte[] rgba = new byte[4];

		nint first = renderer.CreateTexture(rgba, 1, 1);
		nint second = renderer.CreateTexture(rgba, 1, 1);

		Assert.AreNotEqual(first, second, "Each texture needs its own handle.");
		Assert.AreNotEqual(0, first, "Zero is reserved for 'no texture'.");
	}

	[TestMethod]
	public void UpdateTexture_ReplacesPixels()
	{
		using SoftwareRenderer renderer = new(8, 8);
		byte[] red = [255, 0, 0, 255];
		byte[] green = [0, 255, 0, 255];

		nint id = renderer.CreateTexture(red, 1, 1);
		bool updated = renderer.UpdateTexture(id, green, 1, 1);

		Assert.IsTrue(updated, "A CPU texture can always be replaced in place.");
		Assert.AreEqual(new Rgba32(0, 255, 0, 255), renderer.GetTexture(id).Sample(0.5f, 0.5f));
	}

	[TestMethod]
	public void DeleteTexture_RemovesTheHandle()
	{
		using SoftwareRenderer renderer = new(8, 8);
		nint id = renderer.CreateTexture(new byte[4], 1, 1);

		renderer.DeleteTexture(id);

		Assert.ThrowsExactly<KeyNotFoundException>(() => renderer.GetTexture(id));
	}

	[TestMethod]
	public void Clear_FillsTheRenderTarget()
	{
		using SoftwareRenderer renderer = new(4, 4);

		renderer.Clear(new Rgba32(9, 9, 9, 255));

		Assert.AreEqual(new Rgba32(9, 9, 9, 255), renderer.Target.GetPixel(0, 0));
		Assert.AreEqual(new Rgba32(9, 9, 9, 255), renderer.Target.GetPixel(3, 3));
	}

	[TestMethod]
	public void RenderDrawData_EmptyDrawData_DoesNothing()
	{
		using SoftwareRenderer renderer = new(4, 4);
		renderer.Clear(new Rgba32(1, 2, 3, 255));

		renderer.RenderDrawData(default);

		Assert.AreEqual(new Rgba32(1, 2, 3, 255), renderer.Target.GetPixel(0, 0), "A null draw-data pointer should be ignored rather than throwing.");
	}

	[TestMethod]
	public void CreateTexture_SourceLargerThanNeeded_CopiesOnlyTheUsedBytes()
	{
		using SoftwareRenderer renderer = new(4, 4);
		byte[] oversized = new byte[64];
		oversized[0] = 255;
		oversized[3] = 255;

		nint id = renderer.CreateTexture(oversized, 1, 1);

		Assert.AreEqual(new Rgba32(255, 0, 0, 255), renderer.GetTexture(id).Sample(0f, 0f));
	}
}
