// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.App.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests for font management and UI scaling functionality including FontHelper, FontAppearance, and UIScaler.
/// </summary>
[TestClass]
public class FontAndUITests
{
	[TestInitialize]
	public void Setup()
	{
		ImGuiApp.Reset();
	}

	#region FontHelper Tests

	[TestMethod]
	public void FontHelper_IsStaticClass()
	{
		Type fontHelperType = typeof(FontHelper);
		Assert.IsTrue(fontHelperType.IsAbstract && fontHelperType.IsSealed);
	}

	[TestMethod]
	public void FontHelper_CleanupCustomFonts_DoesNotThrow()
	{
		try
		{
			FontHelper.CleanupCustomFonts();
		}
#pragma warning disable CA1031 // Do not catch general exception types
		catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
		{
			Assert.Fail($"Expected no exception, but got: {ex.Message}");
		}
	}

	[TestMethod]
	public void FontHelper_CleanupGlyphRanges_DoesNotThrow()
	{
		try
		{
			FontHelper.CleanupGlyphRanges();
		}
#pragma warning disable CA1031 // Do not catch general exception types
		catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
		{
			Assert.Fail($"Expected no exception, but got: {ex.Message}");
		}
	}

	[TestMethod]
	public void FontHelper_CleanupMethods_CanBeCalledMultipleTimes()
	{
		try
		{
			FontHelper.CleanupCustomFonts();
			FontHelper.CleanupGlyphRanges();
			FontHelper.CleanupCustomFonts();
			FontHelper.CleanupGlyphRanges();
		}
#pragma warning disable CA1031 // Do not catch general exception types
		catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
		{
			Assert.Fail($"Expected no exception, but got: {ex.Message}");
		}
	}

	#endregion

	#region FontAppearance Tests

	[TestMethod]
	public void FontAppearance_InheritsScopedAction()
	{
		Assert.IsTrue(typeof(FontAppearance).IsSubclassOf(typeof(ScopedAction.ScopedAction)));
	}

	[TestMethod]
	public void FontAppearance_Constants_HaveExpectedValues()
	{
		// Access FontAppearance constants directly using internal access
		Assert.AreEqual("default", FontAppearance.DefaultFontName);
		Assert.AreEqual(14, FontAppearance.DefaultFontPointSize);
	}

	#endregion

	#region UIScaler Tests

	[TestMethod]
	public void UIScaler_InheritsScopedAction()
	{
		Assert.IsTrue(typeof(UIScaler).IsSubclassOf(typeof(ScopedAction.ScopedAction)));
	}

	#endregion

	#region Util Tests

	[TestMethod]
	public void Util_IsStaticClass()
	{
		Type utilType = typeof(ImGuiController.Util);
		Assert.IsTrue(utilType.IsAbstract && utilType.IsSealed);
	}

	[TestMethod]
	public void Util_Clamp_WorksCorrectly()
	{
		// Test clamping at minimum
		Assert.AreEqual(5.0f, ImGuiController.Util.Clamp(3.0f, 5.0f, 10.0f));

		// Test clamping at maximum
		Assert.AreEqual(10.0f, ImGuiController.Util.Clamp(15.0f, 5.0f, 10.0f));

		// Test value in range
		Assert.AreEqual(7.0f, ImGuiController.Util.Clamp(7.0f, 5.0f, 10.0f));

		// Test edge cases
		Assert.AreEqual(5.0f, ImGuiController.Util.Clamp(5.0f, 5.0f, 10.0f));
		Assert.AreEqual(10.0f, ImGuiController.Util.Clamp(10.0f, 5.0f, 10.0f));
	}

	#endregion
}
