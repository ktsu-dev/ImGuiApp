// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGuiApp.Test;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using ktsu.ImGuiApp.ImGuiController;
using Hexa.NET.ImGui;
using System;
using Moq;
using Silk.NET.Windowing;

/// <summary>
/// Tests for ImGuiController components including font configuration, factories, and providers.
/// </summary>
[TestClass]
public class ImGuiControllerComponentTests
{
	#region ImGuiFontConfig Tests

	[TestMethod]
	public void ImGuiFontConfig_Constructor_ValidParameters_InitializesCorrectly()
	{
		const string testPath = "test.ttf";
		const int testSize = 16;

		ImGuiFontConfig config = new(testPath, testSize);

		// These calls should hit the property getters
		string actualPath = config.FontPath;
		int actualSize = config.FontSize;
		Func<ImGuiIOPtr, IntPtr>? actualGlyphRange = config.GetGlyphRange;

		Assert.AreEqual(testPath, actualPath);
		Assert.AreEqual(testSize, actualSize);
		Assert.IsNull(actualGlyphRange);
	}

	[TestMethod]
	public void ImGuiFontConfig_Constructor_WithGlyphRange_InitializesCorrectly()
	{
		const string testPath = "test.ttf";
		const int testSize = 16;
		static IntPtr TestGlyphRange(ImGuiIOPtr io) => IntPtr.Zero;

		ImGuiFontConfig config = new(testPath, testSize, TestGlyphRange);

		Assert.AreEqual(testPath, config.FontPath);
		Assert.AreEqual(testSize, config.FontSize);
		Assert.IsNotNull(config.GetGlyphRange);
		Assert.AreSame(TestGlyphRange, config.GetGlyphRange);
	}

	[TestMethod]
	public void ImGuiFontConfig_Constructor_ZeroFontSize_ThrowsException()
	{
		Assert.ThrowsException<ArgumentOutOfRangeException>(() => new ImGuiFontConfig("test.ttf", 0));
	}

	[TestMethod]
	public void ImGuiFontConfig_Constructor_NegativeFontSize_ThrowsException()
	{
		Assert.ThrowsException<ArgumentOutOfRangeException>(() => new ImGuiFontConfig("test.ttf", -1));
	}

	[TestMethod]
	public void ImGuiFontConfig_Constructor_NullFontPath_ThrowsException()
	{
		Assert.ThrowsException<ArgumentNullException>(() => new ImGuiFontConfig(null!, 16));
	}

	[TestMethod]
	public void ImGuiFontConfig_Equals_SameValues_ReturnsTrue()
	{
		ImGuiFontConfig config1 = new("test.ttf", 16);
		ImGuiFontConfig config2 = new("test.ttf", 16);

		Assert.IsTrue(config1.Equals(config2));
		Assert.IsTrue(config1 == config2);
		Assert.IsFalse(config1 != config2);
	}

	[TestMethod]
	public void ImGuiFontConfig_Equals_DifferentValues_ReturnsFalse()
	{
		ImGuiFontConfig config1 = new("test1.ttf", 16);
		ImGuiFontConfig config2 = new("test2.ttf", 16);
		ImGuiFontConfig config3 = new("test1.ttf", 18);

		Assert.IsFalse(config1.Equals(config2));
		Assert.IsFalse(config1.Equals(config3));
		Assert.IsFalse(config1 == config2);
		Assert.IsTrue(config1 != config2);
	}

	[TestMethod]
	public void ImGuiFontConfig_GetHashCode_SameValues_ReturnsSameHash()
	{
		ImGuiFontConfig config1 = new("test.ttf", 16);
		ImGuiFontConfig config2 = new("test.ttf", 16);

		Assert.AreEqual(config1.GetHashCode(), config2.GetHashCode());
	}

	[TestMethod]
	public void ImGuiFontConfig_Equals_WithObject_WorksCorrectly()
	{
		ImGuiFontConfig config1 = new("test.ttf", 16);
		ImGuiFontConfig config2 = new("test.ttf", 16);
		object config2AsObject = config2;

		Assert.IsTrue(config1.Equals(config2AsObject));
		Assert.IsFalse(config1.Equals("not a font config"));
		Assert.IsFalse(config1.Equals(null));
	}

	[TestMethod]
	public void ImGuiFontConfig_IsStruct()
	{
		Type configType = typeof(ImGuiFontConfig);
		Assert.IsTrue(configType.IsValueType);
		Assert.IsFalse(configType.IsClass);
	}

	[TestMethod]
	public void ImGuiFontConfig_ImplementsIEquatable()
	{
		Type configType = typeof(ImGuiFontConfig);
		Type equatableType = typeof(IEquatable<ImGuiFontConfig>);
		Assert.IsTrue(equatableType.IsAssignableFrom(configType));
	}

	#endregion

	#region WindowOpenGLFactory Tests

	[TestMethod]
	public void WindowOpenGLFactory_Constructor_ValidWindow_InitializesCorrectly()
	{
		Mock<IWindow> mockWindow = new();
		WindowOpenGLFactory factory = new(mockWindow.Object);

		Assert.IsNotNull(factory);
	}

	[TestMethod]
	public void WindowOpenGLFactory_Constructor_NullWindow_ThrowsException()
	{
		Assert.ThrowsException<ArgumentNullException>(() => new WindowOpenGLFactory(null!));
	}

	[TestMethod]
	public void WindowOpenGLFactory_ImplementsIOpenGLFactory()
	{
		Type factoryType = typeof(WindowOpenGLFactory);
		Assert.IsTrue(typeof(IOpenGLFactory).IsAssignableFrom(factoryType));
	}

	#endregion

	#region OpenGLProvider Tests

	[TestMethod]
	public void OpenGLProvider_Constructor_ValidFactory_InitializesCorrectly()
	{
		Mock<IOpenGLFactory> mockFactory = new();
		using OpenGLProvider provider = new(mockFactory.Object);

		Assert.IsNotNull(provider);
	}

	[TestMethod]
	public void OpenGLProvider_Constructor_NullFactory_ThrowsException()
	{
		Assert.ThrowsException<ArgumentNullException>(() => new OpenGLProvider(null!));
	}

	[TestMethod]
	public void OpenGLProvider_Dispose_CanBeCalledMultipleTimes()
	{
		Mock<IOpenGLFactory> mockFactory = new();
		OpenGLProvider provider = new(mockFactory.Object);

		provider.Dispose();
		provider.Dispose();
		// If we reach here without exception, the test passes
	}

	[TestMethod]
	public void OpenGLProvider_GetGL_AfterDispose_ThrowsException()
	{
		Mock<IOpenGLFactory> mockFactory = new();
		using OpenGLProvider provider = new(mockFactory.Object);

		provider.Dispose();
		Assert.ThrowsException<ObjectDisposedException>(provider.GetGL);
	}

	[TestMethod]
	public void OpenGLProvider_ImplementsIDisposable()
	{
		Type providerType = typeof(OpenGLProvider);
		Assert.IsTrue(typeof(IDisposable).IsAssignableFrom(providerType));
	}

	#endregion

	#region Texture and TextureCoordinate Tests

	[TestMethod]
	public void TextureCoordinate_EnumValues_HaveCorrectMappings()
	{
		Assert.AreEqual(0, (int)TextureCoordinate.None);
		Assert.AreEqual((int)Silk.NET.OpenGL.TextureParameterName.TextureWrapS, (int)TextureCoordinate.S);
		Assert.AreEqual((int)Silk.NET.OpenGL.TextureParameterName.TextureWrapT, (int)TextureCoordinate.T);
		Assert.AreEqual((int)Silk.NET.OpenGL.TextureParameterName.TextureWrapR, (int)TextureCoordinate.R);
	}

	[TestMethod]
	public void TextureCoordinate_AllEnumValues_AreDefined()
	{
		TextureCoordinate[] values = Enum.GetValues<TextureCoordinate>();

		Assert.IsTrue(values.Length >= 4);
		Assert.IsTrue(values.Contains(TextureCoordinate.None));
		Assert.IsTrue(values.Contains(TextureCoordinate.S));
		Assert.IsTrue(values.Contains(TextureCoordinate.T));
		Assert.IsTrue(values.Contains(TextureCoordinate.R));
	}

	[TestMethod]
	public void TextureCoordinate_IsEnum()
	{
		Type coordType = typeof(TextureCoordinate);
		Assert.IsTrue(coordType.IsEnum);
		Assert.AreEqual(typeof(int), Enum.GetUnderlyingType(coordType));
	}

	[TestMethod]
	public void Texture_Constants_HaveExpectedValues()
	{
		Assert.AreEqual((Silk.NET.OpenGL.SizedInternalFormat)Silk.NET.OpenGL.GLEnum.Srgb8Alpha8, Texture.Srgb8Alpha8);
		Assert.AreEqual((Silk.NET.OpenGL.SizedInternalFormat)Silk.NET.OpenGL.GLEnum.Rgb32f, Texture.Rgb32F);
		Assert.AreEqual((Silk.NET.OpenGL.GLEnum)0x84FF, Texture.MaxTextureMaxAnisotropy);
	}

	#endregion
}