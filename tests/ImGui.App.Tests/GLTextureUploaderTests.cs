// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Tests;

using System;
using System.Linq;

using Hexa.NET.ImGui;

using ktsu.ImGui.App.ImGuiController;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Covers the OpenGL calls behind <see cref="ITextureUploader"/>. These run against
/// <see cref="TestGL"/>, so the call sequence is checked without a graphics context.
/// </summary>
[TestClass]
public sealed class GLTextureUploaderTests
{
	[TestMethod]
	public void Create_ConfiguresFilteringAndUploadsTheWholeImage()
	{
		using TestGL gl = new() { NextTextureName = 9 };
		GLTextureUploader uploader = new(gl);

		nint id = uploader.Create(64, 32, 0);

		Assert.AreEqual(9, id, "The caller needs the name OpenGL handed out, not a guess.");
		Assert.Contains("TexParameter(TextureMinFilter,9729)", gl.Calls);
		Assert.Contains("TexParameter(TextureWrapS,33071)", gl.Calls);
		Assert.Contains("PixelStore(UnpackRowLength,0)", gl.Calls);
		Assert.Contains("TexImage2D(64x32)", gl.Calls);
	}

	[TestMethod]
	public void Create_BindsBeforeConfiguringSoTheStateLandsOnTheNewTexture()
	{
		using TestGL gl = new() { NextTextureName = 4 };
		GLTextureUploader uploader = new(gl);

		uploader.Create(8, 8, 0);

		int bind = gl.Calls.IndexOf("BindTexture(4)");
		int firstParam = gl.Calls.ToList().FindIndex(c => c.StartsWith("TexParameter", StringComparison.Ordinal));
		int upload = gl.Calls.IndexOf("TexImage2D(8x8)");
		Assert.IsGreaterThan(-1, bind, "The new texture must be bound.");
		Assert.IsLessThan(firstParam, bind, "Parameters set before binding would land on whatever was bound before.");
		Assert.IsLessThan(upload, firstParam);
	}

	[TestMethod]
	public void Update_SetsTheAtlasStrideAroundTheUploadAndResetsIt()
	{
		using TestGL gl = new();
		GLTextureUploader uploader = new(gl);

		uploader.Update(3, sourceRowPixels: 512, new ImTextureRect(2, 4, 6, 8), 0);

		// A dirty rectangle is read out of the middle of the full atlas buffer, so the stride must
		// describe the atlas while the upload happens and go back to 0 afterwards, or every later
		// upload misreads its rows.
		int set = gl.Calls.IndexOf("PixelStore(UnpackRowLength,512)");
		int upload = gl.Calls.IndexOf("TexSubImage2D(6x8@2,4)");
		int reset = gl.Calls.IndexOf("PixelStore(UnpackRowLength,0)");
		Assert.IsGreaterThan(-1, set, "The atlas stride was never set.");
		Assert.IsLessThan(upload, set, "The stride must be in force before the upload.");
		Assert.IsLessThan(reset, upload, "The stride must be reset after the upload.");
	}

	[TestMethod]
	public void Update_TargetsTheTextureItWasGiven()
	{
		using TestGL gl = new();
		GLTextureUploader uploader = new(gl);

		uploader.Update(11, 64, new ImTextureRect(0, 0, 1, 1), 0);

		Assert.Contains("BindTexture(11)", gl.Calls);
	}

	[TestMethod]
	public void Destroy_ReleasesTheNamedTexture()
	{
		using TestGL gl = new();
		GLTextureUploader uploader = new(gl);

		uploader.Destroy(5);

		Assert.Contains("DeleteTexture(5)", gl.Calls);
	}
}
