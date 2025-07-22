// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGuiApp.Test;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using ktsu.ImGuiApp.ImGuiController;
using Silk.NET.OpenGL;
using Texture = ktsu.ImGuiApp.ImGuiController.Texture;

[TestClass]
public class TextureTests
{
	[TestMethod]
	public void TextureCoordinate_EnumValues_HaveCorrectMappings()
	{
		Assert.AreEqual(0, (int)TextureCoordinate.None);
		Assert.AreEqual((int)TextureParameterName.TextureWrapS, (int)TextureCoordinate.S);
		Assert.AreEqual((int)TextureParameterName.TextureWrapT, (int)TextureCoordinate.T);
		Assert.AreEqual((int)TextureParameterName.TextureWrapR, (int)TextureCoordinate.R);
	}

	[TestMethod]
	public void TextureCoordinate_AllEnumValues_AreDefined()
	{
		// Verify all enum values are properly defined
		TextureCoordinate[] values = Enum.GetValues<TextureCoordinate>();

		Assert.IsTrue(values.Length >= 4);
		Assert.IsTrue(values.Contains(TextureCoordinate.None));
		Assert.IsTrue(values.Contains(TextureCoordinate.S));
		Assert.IsTrue(values.Contains(TextureCoordinate.T));
		Assert.IsTrue(values.Contains(TextureCoordinate.R));
	}

	[TestMethod]
	public void Texture_Constants_HaveExpectedValues()
	{
		// Test that texture constants are properly defined
		Assert.AreEqual((SizedInternalFormat)GLEnum.Srgb8Alpha8, Texture.Srgb8Alpha8);
		Assert.AreEqual((SizedInternalFormat)GLEnum.Rgb32f, Texture.Rgb32F);
		Assert.AreEqual((GLEnum)0x84FF, Texture.MaxTextureMaxAnisotropy);
	}
}