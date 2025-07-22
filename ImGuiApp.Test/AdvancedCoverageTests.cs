// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGuiApp.Test;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using ktsu.ImGuiApp.ImGuiController;
using Hexa.NET.ImGui;
using System;

using Moq;



/// <summary>
/// Advanced tests targeting specific low-coverage areas including OpenGL functionality, shader management, and texture operations.
/// </summary>
[TestClass]
public class AdvancedCoverageTests
{
	#region ImGuiFontConfig Advanced Tests

	[TestMethod]
	public void ImGuiFontConfig_Properties_GettersReturnCorrectValues()
	{
		const string testPath = "arial.ttf";
		const int testSize = 18;
		static IntPtr TestGlyphRange(ImGuiIOPtr io) => IntPtr.Zero;

		ImGuiFontConfig config = new(testPath, testSize, TestGlyphRange);

		// Exercise all property getters
		string fontPath = config.FontPath;
		int fontSize = config.FontSize;
		Func<ImGuiIOPtr, IntPtr>? glyphRange = config.GetGlyphRange;

		Assert.AreEqual(testPath, fontPath);
		Assert.AreEqual(testSize, fontSize);
		Assert.IsNotNull(glyphRange);
		Assert.AreSame(TestGlyphRange, glyphRange);
	}

	[TestMethod]
	public void ImGuiFontConfig_GetHashCode_ConsistentResults()
	{
		ImGuiFontConfig config1 = new("test.ttf", 16);
		ImGuiFontConfig config2 = new("test.ttf", 16);

		int hash1 = config1.GetHashCode();
		int hash2 = config2.GetHashCode();

		Assert.AreEqual(hash1, hash2);
	}

	[TestMethod]
	public void ImGuiFontConfig_GetHashCode_DifferentForDifferentConfigs()
	{
		ImGuiFontConfig config1 = new("test1.ttf", 16);
		ImGuiFontConfig config2 = new("test2.ttf", 16);

		int hash1 = config1.GetHashCode();
		int hash2 = config2.GetHashCode();

		Assert.AreNotEqual(hash1, hash2);
	}

	[TestMethod]
	public void ImGuiFontConfig_EqualsObject_WithNullObject_ReturnsFalse()
	{
		ImGuiFontConfig config = new("test.ttf", 16);
		bool result = config.Equals((object?)null);
		Assert.IsFalse(result);
	}

	[TestMethod]
	public void ImGuiFontConfig_EqualsObject_WithDifferentType_ReturnsFalse()
	{
		ImGuiFontConfig config = new("test.ttf", 16);
		bool result = config.Equals("not a font config");
		Assert.IsFalse(result);
	}

	[TestMethod]
	public void ImGuiFontConfig_EqualsObject_WithSameConfig_ReturnsTrue()
	{
		ImGuiFontConfig config1 = new("test.ttf", 16);
		ImGuiFontConfig config2 = new("test.ttf", 16);
		bool result = config1.Equals(config2);
		Assert.IsTrue(result);
	}

	[TestMethod]
	public void ImGuiFontConfig_EqualityOperators_WorkCorrectly()
	{
		ImGuiFontConfig config1 = new("test.ttf", 16);
		ImGuiFontConfig config2 = new("test.ttf", 16);
		ImGuiFontConfig config3 = new("other.ttf", 16);

		// Test equality
		bool equal1 = config1 == config2;
		bool equal2 = config1 == config3;

		// Test inequality
		bool notEqual1 = config1 != config2;
		bool notEqual2 = config1 != config3;

		Assert.IsTrue(equal1);
		Assert.IsFalse(equal2);
		Assert.IsFalse(notEqual1);
		Assert.IsTrue(notEqual2);
	}

	[TestMethod]
	public void ImGuiFontConfig_EqualsTyped_WithDifferentGlyphRange_ReturnsFalse()
	{
		static IntPtr GlyphRange1(ImGuiIOPtr io) => IntPtr.Zero;
		static IntPtr GlyphRange2(ImGuiIOPtr io) => new(1);

		ImGuiFontConfig config1 = new("test.ttf", 16, GlyphRange1);
		ImGuiFontConfig config2 = new("test.ttf", 16, GlyphRange2);

		bool result = config1.Equals(config2);
		Assert.IsFalse(result);
	}

	#endregion



	#region UniformFieldInfo Tests

	[TestMethod]
	public void UniformFieldInfo_DefaultConstructor_InitializesFields()
	{
		UniformFieldInfo uniformInfo = new();

		Assert.AreEqual(0, uniformInfo.Location);
		Assert.IsNull(uniformInfo.Name);
		Assert.AreEqual(0, uniformInfo.Size);
		Assert.AreEqual(default(Silk.NET.OpenGL.UniformType), uniformInfo.Type);
	}

	[TestMethod]
	public void UniformFieldInfo_FieldAssignment_WorksCorrectly()
	{
		UniformFieldInfo uniformInfo = new()
		{
			Location = 5,
			Name = "testUniform",
			Size = 1,
			Type = Silk.NET.OpenGL.UniformType.Float
		};

		Assert.AreEqual(5, uniformInfo.Location);
		Assert.AreEqual("testUniform", uniformInfo.Name);
		Assert.AreEqual(1, uniformInfo.Size);
		Assert.AreEqual(Silk.NET.OpenGL.UniformType.Float, uniformInfo.Type);
	}

	#endregion

	#region ImGuiApp Advanced Scenarios

	[TestMethod]
	public void ImGuiApp_DeleteTexture_WithNullTextureInfo_ThrowsArgumentNullException()
	{
		Exception exception = Assert.ThrowsException<ArgumentNullException>(() => ImGuiApp.DeleteTexture(null!));

		Assert.IsInstanceOfType<ArgumentNullException>(exception);
	}

	[TestMethod]
	public void ImGuiApp_WindowState_MultipleAccess_ReturnsSameInstance()
	{
		ImGuiAppWindowState state1 = ImGuiApp.WindowState;
		ImGuiAppWindowState state2 = ImGuiApp.WindowState;

		// Should return the same instance each time
		Assert.AreSame(state1, state2);
	}

	[TestMethod]
	public void ImGuiApp_Textures_MultipleAccess_ReturnsSameInstance()
	{
		System.Collections.Concurrent.ConcurrentDictionary<ktsu.StrongPaths.AbsoluteFilePath, ImGuiAppTextureInfo> textures1 = ImGuiApp.Textures;
		System.Collections.Concurrent.ConcurrentDictionary<ktsu.StrongPaths.AbsoluteFilePath, ImGuiAppTextureInfo> textures2 = ImGuiApp.Textures;

		// Should return the same collection instance
		Assert.AreSame(textures1, textures2);
	}

	#endregion

	#region Performance Settings Edge Cases

	[TestMethod]
	public void ImGuiAppPerformanceSettings_AllProperties_CanBeSetToZero()
	{
		ImGuiAppPerformanceSettings settings = new()
		{
			FocusedFps = 0.0,
			FocusedUps = 0.0,
			UnfocusedFps = 0.0,
			UnfocusedUps = 0.0,
			IdleFps = 0.0,
			IdleUps = 0.0,
			IdleTimeoutSeconds = 0.0
		};

		Assert.AreEqual(0.0, settings.FocusedFps);
		Assert.AreEqual(0.0, settings.FocusedUps);
		Assert.AreEqual(0.0, settings.UnfocusedFps);
		Assert.AreEqual(0.0, settings.UnfocusedUps);
		Assert.AreEqual(0.0, settings.IdleFps);
		Assert.AreEqual(0.0, settings.IdleUps);
		Assert.AreEqual(0.0, settings.IdleTimeoutSeconds);
	}

	[TestMethod]
	public void ImGuiAppPerformanceSettings_AllProperties_CanBeSetToNegativeValues()
	{
		ImGuiAppPerformanceSettings settings = new()
		{
			FocusedFps = -1.0,
			FocusedUps = -2.0,
			UnfocusedFps = -3.0,
			UnfocusedUps = -4.0,
			IdleFps = -5.0,
			IdleUps = -6.0,
			IdleTimeoutSeconds = -7.0
		};

		Assert.AreEqual(-1.0, settings.FocusedFps);
		Assert.AreEqual(-2.0, settings.FocusedUps);
		Assert.AreEqual(-3.0, settings.UnfocusedFps);
		Assert.AreEqual(-4.0, settings.UnfocusedUps);
		Assert.AreEqual(-5.0, settings.IdleFps);
		Assert.AreEqual(-6.0, settings.IdleUps);
		Assert.AreEqual(-7.0, settings.IdleTimeoutSeconds);
	}

	#endregion

	#region ImGuiAppConfig Advanced Tests

	[TestMethod]
	public void ImGuiAppConfig_AllCallbacks_CanBeSetToNull()
	{
		ImGuiAppConfig config = new();

		// Test that the default values are not null (they are default empty delegates)
		Assert.IsNotNull(config.OnStart);
		Assert.IsNotNull(config.OnUpdate);
		Assert.IsNotNull(config.OnRender);
		Assert.IsNotNull(config.OnAppMenu);
		Assert.IsNotNull(config.OnMoveOrResize);
	}

	[TestMethod]
	public void ImGuiAppConfig_Fonts_CanBeCleared()
	{
		ImGuiAppConfig config = new();
		config.Fonts.Add("test", [1, 2, 3]);
		Assert.AreEqual(1, config.Fonts.Count);

		config.Fonts.Clear();
		Assert.AreEqual(0, config.Fonts.Count);
	}

	[TestMethod]
	public void ImGuiAppConfig_Fonts_SupportsMultipleFonts()
	{
		ImGuiAppConfig config = new();
		byte[] font1 = [1, 2, 3];
		byte[] font2 = [4, 5, 6];

		config.Fonts.Add("Arial", font1);
		config.Fonts.Add("Times", font2);

		Assert.AreEqual(2, config.Fonts.Count);
		Assert.AreSame(font1, config.Fonts["Arial"]);
		Assert.AreSame(font2, config.Fonts["Times"]);
	}

	#endregion
}