// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.App.Tests;

using System;
using Hexa.NET.ImGui;
using ktsu.ImGui.App.ImGuiController;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new ImGuiFontConfig("test.ttf", 0));
	}

	[TestMethod]
	public void ImGuiFontConfig_Constructor_NegativeFontSize_ThrowsException()
	{
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new ImGuiFontConfig("test.ttf", -1));
	}

	[TestMethod]
	public void ImGuiFontConfig_Constructor_NullFontPath_ThrowsException()
	{
		Assert.ThrowsExactly<ArgumentNullException>(() => new ImGuiFontConfig(null!, 16));
	}

	[TestMethod]
	public void ImGuiFontConfig_Equals_SameValues_ReturnsTrue()
	{
		ImGuiFontConfig config1 = new("test.ttf", 16);
		ImGuiFontConfig config2 = new("test.ttf", 16);

		Assert.IsTrue(config1.Equals(config2), "Configs with same values should be equal");
		Assert.IsTrue(config1 == config2, "Equality operator should return true for equal configs");
		Assert.IsFalse(config1 != config2, "Inequality operator should return false for equal configs");
	}

	[TestMethod]
	public void ImGuiFontConfig_Equals_DifferentValues_ReturnsFalse()
	{
		ImGuiFontConfig config1 = new("test1.ttf", 16);
		ImGuiFontConfig config2 = new("test2.ttf", 16);
		ImGuiFontConfig config3 = new("test1.ttf", 18);

		Assert.IsFalse(config1.Equals(config2), "Configs with different paths should not be equal");
		Assert.IsFalse(config1.Equals(config3), "Configs with different sizes should not be equal");
		Assert.IsFalse(config1 == config2, "Equality operator should return false for different configs");
		Assert.IsTrue(config1 != config2, "Inequality operator should return true for different configs");
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

		Assert.IsTrue(config1.Equals(config2AsObject), "Equals should work with boxed config object");
		Assert.IsFalse(config1.Equals("not a font config"), "Equals should return false for different type");
		Assert.IsFalse(config1.Equals(null), "Equals should return false for null");
	}

	[TestMethod]
	public void ImGuiFontConfig_IsStruct()
	{
		Type configType = typeof(ImGuiFontConfig);
		Assert.IsTrue(configType.IsValueType, "ImGuiFontConfig should be a value type");
		Assert.IsFalse(configType.IsClass, "ImGuiFontConfig should not be a class");
	}

	[TestMethod]
	public void ImGuiFontConfig_ImplementsIEquatable()
	{
		Type configType = typeof(ImGuiFontConfig);
		Type equatableType = typeof(IEquatable<ImGuiFontConfig>);
		Assert.IsTrue(equatableType.IsAssignableFrom(configType), "ImGuiFontConfig should implement IEquatable<ImGuiFontConfig>");
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
		Assert.ThrowsExactly<ArgumentNullException>(() => new WindowOpenGLFactory(null!));
	}

	[TestMethod]
	public void WindowOpenGLFactory_ImplementsIOpenGLFactory()
	{
		Type factoryType = typeof(WindowOpenGLFactory);
		Assert.IsTrue(typeof(IOpenGLFactory).IsAssignableFrom(factoryType), "WindowOpenGLFactory should implement IOpenGLFactory");
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
		Assert.ThrowsExactly<ArgumentNullException>(() => new OpenGLProvider(null!));
	}

	[TestMethod]
	public void OpenGLProvider_Dispose_CanBeCalledMultipleTimes()
	{
		try
		{
			Mock<IOpenGLFactory> mockFactory = new();
			OpenGLProvider provider = new(mockFactory.Object);

			provider.Dispose();
			provider.Dispose();
		}
#pragma warning disable CA1031 // Do not catch general exception types
		catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
		{
			Assert.Fail($"Expected no exception, but got: {ex.Message}");
		}
	}

	[TestMethod]
	public void OpenGLProvider_GetGL_AfterDispose_ThrowsException()
	{
		Mock<IOpenGLFactory> mockFactory = new();
		using OpenGLProvider provider = new(mockFactory.Object);

		provider.Dispose();
		Assert.ThrowsExactly<ObjectDisposedException>(provider.GetGL);
	}

	[TestMethod]
	public void OpenGLProvider_ImplementsIDisposable()
	{
		Type providerType = typeof(OpenGLProvider);
		Assert.IsTrue(typeof(IDisposable).IsAssignableFrom(providerType), "OpenGLProvider should implement IDisposable");
	}

	#endregion

	#region Texture and TextureCoordinate Tests

	[TestMethod]
	public void TextureCoordinate_EnumValues_HaveCorrectMappings()
	{
#pragma warning disable MSTEST0032 // Assertion condition is always true
#pragma warning disable MSTEST0025 // Use 'Assert.Fail' instead of an always-failing assert
		Assert.AreEqual(0, (int)TextureCoordinate.None);
		Assert.AreEqual((int)Silk.NET.OpenGL.TextureParameterName.TextureWrapS, (int)TextureCoordinate.S);
		Assert.AreEqual((int)Silk.NET.OpenGL.TextureParameterName.TextureWrapT, (int)TextureCoordinate.T);
		Assert.AreEqual((int)Silk.NET.OpenGL.TextureParameterName.TextureWrapR, (int)TextureCoordinate.R);
#pragma warning restore MSTEST0025 // Use 'Assert.Fail' instead of an always-failing assert
#pragma warning restore MSTEST0032 // Assertion condition is always true
	}

	[TestMethod]
	public void TextureCoordinate_AllEnumValues_AreDefined()
	{
		TextureCoordinate[] values = Enum.GetValues<TextureCoordinate>();

		Assert.IsGreaterThanOrEqualTo(4, values.Length, "TextureCoordinate enum should have at least 4 values");
		CollectionAssert.Contains(values, TextureCoordinate.None, "TextureCoordinate should contain None");
		CollectionAssert.Contains(values, TextureCoordinate.S, "TextureCoordinate should contain S");
		CollectionAssert.Contains(values, TextureCoordinate.T, "TextureCoordinate should contain T");
		CollectionAssert.Contains(values, TextureCoordinate.R, "TextureCoordinate should contain R");
	}

	[TestMethod]
	public void TextureCoordinate_IsEnum()
	{
		Type coordType = typeof(TextureCoordinate);
		Assert.IsTrue(coordType.IsEnum, "TextureCoordinate should be an enum type");
		Assert.AreEqual(typeof(int), Enum.GetUnderlyingType(coordType));
	}

	[TestMethod]
	public void Texture_Constants_HaveExpectedValues()
	{
#pragma warning disable MSTEST0032 // Assertion condition is always true
#pragma warning disable MSTEST0025 // Use 'Assert.Fail' instead of an always-failing assert
		Assert.AreEqual((Silk.NET.OpenGL.SizedInternalFormat)Silk.NET.OpenGL.GLEnum.Srgb8Alpha8, Texture.Srgb8Alpha8);
		Assert.AreEqual((Silk.NET.OpenGL.SizedInternalFormat)Silk.NET.OpenGL.GLEnum.Rgb32f, Texture.Rgb32F);
		Assert.AreEqual((Silk.NET.OpenGL.GLEnum)0x84FF, Texture.MaxTextureMaxAnisotropy);
#pragma warning restore MSTEST0025 // Use 'Assert.Fail' instead of an always-failing assert
#pragma warning restore MSTEST0032 // Assertion condition is always true
	}

	#endregion
}
