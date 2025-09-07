// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.App.Test;

using System;
using System.Collections.Generic;
using ktsu.ImGuiApp.ImGuiController;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests for error handling, edge cases, and boundary conditions to maximize code coverage.
/// </summary>
[TestClass]
public class ErrorHandlingAndEdgeCaseTests
{
	[TestInitialize]
	public void Setup()
	{
		ImGuiApp.Reset();
	}

	#region ImGuiFontConfig Error Handling

	[TestMethod]
	public void ImGuiFontConfig_Constructor_EmptyPath_DoesNotThrow()
	{
		// Empty string is a valid font path, should not throw
		ImGuiFontConfig config = new("", 16);
		Assert.AreEqual("", config.FontPath);
		Assert.AreEqual(16, config.FontSize);
	}

	[TestMethod]
	public void ImGuiFontConfig_Constructor_WhitespacePath_DoesNotThrow()
	{
		// Whitespace is a valid path, should not throw
		ImGuiFontConfig config = new("   ", 16);
		Assert.AreEqual("   ", config.FontPath);
	}

	[TestMethod]
	public void ImGuiFontConfig_Constructor_MaxIntSize_DoesNotThrow()
	{
		ImGuiFontConfig config = new("test.ttf", int.MaxValue);
		Assert.AreEqual(int.MaxValue, config.FontSize);
	}

	[TestMethod]
	public void ImGuiFontConfig_Constructor_MinValidSize_DoesNotThrow()
	{
		ImGuiFontConfig config = new("test.ttf", 1);
		Assert.AreEqual(1, config.FontSize);
	}

	#endregion

	#region Texture Coordinate Edge Cases

	[TestMethod]
	public void TextureCoordinate_CastToInt_ReturnsCorrectValues()
	{
		int noneValue = (int)TextureCoordinate.None;
		int sValue = (int)TextureCoordinate.S;
		int tValue = (int)TextureCoordinate.T;
		int rValue = (int)TextureCoordinate.R;

		Assert.AreEqual(0, noneValue);
		Assert.AreNotEqual(0, sValue);
		Assert.AreNotEqual(0, tValue);
		Assert.AreNotEqual(0, rValue);
	}

	[TestMethod]
	public void TextureCoordinate_AllValues_AreUnique()
	{
		TextureCoordinate[] values = Enum.GetValues<TextureCoordinate>();
		HashSet<int> uniqueValues = [];

		foreach (TextureCoordinate coord in values)
		{
			int intValue = (int)coord;
			Assert.IsTrue(uniqueValues.Add(intValue), $"Duplicate value found: {intValue}");
		}
	}

	#endregion

	#region ImGuiApp Edge Cases

	[TestMethod]
	public void ImGuiApp_OnUserInput_CalledRapidly_UpdatesTime()
	{
		DateTime startTime = DateTime.UtcNow;

		for (int i = 0; i < 100; i++)
		{
			ImGuiApp.OnUserInput();
		}

		// Verify that the internal time was updated using direct access
		DateTime lastTime = ImGuiApp.lastInputTime;

		Assert.IsTrue(lastTime >= startTime);
	}

	[TestMethod]
	public void ImGuiApp_Reset_CalledMultipleTimes_DoesNotThrow()
	{
		try
		{
			// Should be safe to call Reset multiple times
			ImGuiApp.Reset();
			ImGuiApp.Reset();
			ImGuiApp.Reset();
		}
#pragma warning disable CA1031 // Do not catch general exception types
		catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
		{
			Assert.Fail($"Expected no exception, but got: {ex.Message}");
		}
	}

	[TestMethod]
	public void ImGuiApp_Properties_AfterReset_HaveConsistentValues()
	{
		ImGuiApp.Reset();
		bool focused1 = ImGuiApp.IsFocused;
		bool idle1 = ImGuiApp.IsIdle;
		float scale1 = ImGuiApp.ScaleFactor;

		ImGuiApp.Reset();
		bool focused2 = ImGuiApp.IsFocused;
		bool idle2 = ImGuiApp.IsIdle;
		float scale2 = ImGuiApp.ScaleFactor;

		Assert.AreEqual(focused1, focused2);
		Assert.AreEqual(idle1, idle2);
		Assert.AreEqual(scale1, scale2);
	}

	#endregion

	#region Data Structure Boundary Tests

	[TestMethod]
	public void ImGuiAppTextureInfo_MaxValues_CanBeSet()
	{
		ImGuiAppTextureInfo textureInfo = new()
		{
			TextureId = uint.MaxValue,
			Width = int.MaxValue,
			Height = int.MaxValue
		};

		Assert.AreEqual(uint.MaxValue, textureInfo.TextureId);
		Assert.AreEqual(int.MaxValue, textureInfo.Width);
		Assert.AreEqual(int.MaxValue, textureInfo.Height);
	}

	[TestMethod]
	public void ImGuiAppTextureInfo_MinValues_CanBeSet()
	{
		ImGuiAppTextureInfo textureInfo = new()
		{
			TextureId = uint.MinValue,
			Width = int.MinValue,
			Height = int.MinValue
		};

		Assert.AreEqual(uint.MinValue, textureInfo.TextureId);
		Assert.AreEqual(int.MinValue, textureInfo.Width);
		Assert.AreEqual(int.MinValue, textureInfo.Height);
	}

	[TestMethod]
	public void ImGuiAppWindowState_ExtremeValues_CanBeSet()
	{
		ImGuiAppWindowState windowState = new()
		{
			Size = new System.Numerics.Vector2(float.MaxValue, float.MaxValue),
			Pos = new System.Numerics.Vector2(float.MinValue, float.MinValue)
		};

		Assert.AreEqual(float.MaxValue, windowState.Size.X);
		Assert.AreEqual(float.MaxValue, windowState.Size.Y);
		Assert.AreEqual(float.MinValue, windowState.Pos.X);
		Assert.AreEqual(float.MinValue, windowState.Pos.Y);
	}

	#endregion

	#region Performance Settings Extreme Values

	[TestMethod]
	public void ImGuiAppPerformanceSettings_ExtremeValues_CanBeSet()
	{
		ImGuiAppPerformanceSettings settings = new()
		{
			FocusedFps = double.MaxValue,
			UnfocusedFps = double.MinValue,
			IdleFps = double.PositiveInfinity,
			NotVisibleFps = double.NegativeInfinity,
			IdleTimeoutSeconds = double.NaN,
			EnableThrottledRendering = true,
			EnableIdleDetection = false
		};

		Assert.AreEqual(double.MaxValue, settings.FocusedFps);
		Assert.AreEqual(double.MinValue, settings.UnfocusedFps);
		Assert.AreEqual(double.PositiveInfinity, settings.IdleFps);
		Assert.AreEqual(double.NegativeInfinity, settings.NotVisibleFps);
		Assert.IsTrue(double.IsNaN(settings.IdleTimeoutSeconds));
		Assert.IsTrue(settings.EnableThrottledRendering);
		Assert.IsFalse(settings.EnableIdleDetection);
	}

	#endregion

	#region Config Edge Cases

	[TestMethod]
	public void ImGuiAppConfig_EmptyStrings_CanBeSet()
	{
		ImGuiAppConfig config = new()
		{
			Title = "",
			IconPath = ""
		};

		Assert.AreEqual("", config.Title);
		Assert.AreEqual("", config.IconPath);
	}

	[TestMethod]
	public void ImGuiAppConfig_VeryLongStrings_CanBeSet()
	{
		string longTitle = new('A', 10000);
		string longIconPath = new('B', 10000);

		ImGuiAppConfig config = new()
		{
			Title = longTitle,
			IconPath = longIconPath
		};

		Assert.AreEqual(longTitle, config.Title);
		Assert.AreEqual(longIconPath, config.IconPath);
	}

	[TestMethod]
	public void ImGuiAppConfig_Fonts_WithEmptyKey_CanBeAdded()
	{
		ImGuiAppConfig config = new();
		byte[] fontData = [1, 2, 3, 4, 5];

		config.Fonts.Add("", fontData);

		Assert.AreEqual(1, config.Fonts.Count);
		Assert.IsTrue(config.Fonts.ContainsKey(""));
		Assert.AreSame(fontData, config.Fonts[""]);
	}

	[TestMethod]
	public void ImGuiAppConfig_Fonts_WithEmptyArray_CanBeAdded()
	{
		ImGuiAppConfig config = new();
		byte[] emptyFont = [];

		config.Fonts.Add("empty", emptyFont);

		Assert.AreEqual(1, config.Fonts.Count);
		Assert.AreSame(emptyFont, config.Fonts["empty"]);
		Assert.AreEqual(0, config.Fonts["empty"].Length);
	}

	[TestMethod]
	public void ImGuiAppConfig_Fonts_WithLargeArray_CanBeAdded()
	{
		ImGuiAppConfig config = new();
		byte[] largeFont = new byte[1000000]; // 1MB font

		config.Fonts.Add("large", largeFont);

		Assert.AreEqual(1, config.Fonts.Count);
		Assert.AreSame(largeFont, config.Fonts["large"]);
		Assert.AreEqual(1000000, config.Fonts["large"].Length);
	}

	#endregion

	#region Static Class Verification

	[TestMethod]
	public void NativeMethods_HasOnlyStaticMembers()
	{
		// Test NativeMethods through direct access instead of reflection
		// Since NativeMethods is an internal static partial class,
		// the fact that this compiles means it's accessible and static
		Assert.IsNotNull(typeof(NativeMethods));
		Assert.IsTrue(typeof(NativeMethods).IsClass);
		Assert.IsTrue(typeof(NativeMethods).IsAbstract && typeof(NativeMethods).IsSealed);
	}

	[TestMethod]
	public void GdiPlusHelper_HasOnlyStaticMembers()
	{
		// Test GdiPlusHelper through direct access instead of reflection
		// Since GdiPlusHelper is an internal static class,
		// the fact that this compiles means it's accessible and static
		Assert.IsNotNull(typeof(GdiPlusHelper));
		Assert.IsTrue(typeof(GdiPlusHelper).IsClass);
		Assert.IsTrue(typeof(GdiPlusHelper).IsAbstract && typeof(GdiPlusHelper).IsSealed);
	}

	[TestMethod]
	public void FontHelper_HasOnlyStaticMembers()
	{
		// Test FontHelper through direct access instead of reflection
		// Since FontHelper is a public static class,
		// the fact that this compiles means it's accessible and static
		Assert.IsNotNull(typeof(FontHelper));
		Assert.IsTrue(typeof(FontHelper).IsClass);
		Assert.IsTrue(typeof(FontHelper).IsAbstract && typeof(FontHelper).IsSealed);
	}

	#endregion

	#region DPI Awareness Edge Cases

	[TestMethod]
	public void ForceDpiAware_GetWindowScaleFactor_ConsistentResults()
	{
		double scale1 = ForceDpiAware.GetWindowScaleFactor();
		double scale2 = ForceDpiAware.GetWindowScaleFactor();

		// Should return consistent results when called multiple times
		Assert.AreEqual(scale1, scale2);
	}

	[TestMethod]
	public void ForceDpiAware_GetActualScaleFactor_ConsistentResults()
	{
		double scale1 = ForceDpiAware.GetActualScaleFactor();
		double scale2 = ForceDpiAware.GetActualScaleFactor();

		// Should return consistent results when called multiple times
		Assert.AreEqual(scale1, scale2);
	}

	[TestMethod]
	public void ForceDpiAware_ScaleFactors_ArePositive()
	{
		double windowScale = ForceDpiAware.GetWindowScaleFactor();
		double actualScale = ForceDpiAware.GetActualScaleFactor();

		Assert.IsTrue(windowScale > 0, "Window scale factor should be positive");
		Assert.IsTrue(actualScale > 0, "Actual scale factor should be positive");
	}

	#endregion
}
