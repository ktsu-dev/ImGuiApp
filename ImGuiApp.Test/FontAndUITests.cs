// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGuiApp.Test;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;

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
		FontHelper.CleanupCustomFonts();
		Assert.IsTrue(true); // If we get here, it didn't throw
	}

	[TestMethod]
	public void FontHelper_CleanupGlyphRanges_DoesNotThrow()
	{
		FontHelper.CleanupGlyphRanges();
		Assert.IsTrue(true); // If we get here, it didn't throw
	}

	[TestMethod]
	public void FontHelper_CleanupMethods_CanBeCalledMultipleTimes()
	{
		FontHelper.CleanupCustomFonts();
		FontHelper.CleanupGlyphRanges();
		FontHelper.CleanupCustomFonts();
		FontHelper.CleanupGlyphRanges();
		Assert.IsTrue(true);
	}

	#endregion

	#region FontAppearance Tests

	[TestMethod]
	public void FontAppearance_InheritsScopedAction()
	{
		Assert.IsTrue(typeof(FontAppearance).IsSubclassOf(typeof(ktsu.ScopedAction.ScopedAction)));
	}

	[TestMethod]
	public void FontAppearance_Constants_HaveExpectedValues()
	{
		FieldInfo? defaultFontName = typeof(FontAppearance)
			.GetField("DefaultFontName", BindingFlags.NonPublic | BindingFlags.Static);
		FieldInfo? defaultFontPointSize = typeof(FontAppearance)
			.GetField("DefaultFontPointSize", BindingFlags.NonPublic | BindingFlags.Static);

		Assert.IsNotNull(defaultFontName);
		Assert.IsNotNull(defaultFontPointSize);
		Assert.AreEqual("default", defaultFontName.GetValue(null));
		Assert.AreEqual(14, defaultFontPointSize.GetValue(null));
	}

	#endregion

	#region UIScaler Tests

	[TestMethod]
	public void UIScaler_InheritsScopedAction()
	{
		Assert.IsTrue(typeof(UIScaler).IsSubclassOf(typeof(ktsu.ScopedAction.ScopedAction)));
	}

	#endregion

	#region Util Tests

	[TestMethod]
	public void Util_IsStaticClass()
	{
		Type utilType = typeof(ktsu.ImGuiApp.ImGuiController.Util);
		Assert.IsTrue(utilType.IsAbstract && utilType.IsSealed);
	}

	[TestMethod]
	public void Util_Clamp_WorksCorrectly()
	{
		// Test clamping at minimum
		Assert.AreEqual(5.0f, ktsu.ImGuiApp.ImGuiController.Util.Clamp(3.0f, 5.0f, 10.0f));

		// Test clamping at maximum
		Assert.AreEqual(10.0f, ktsu.ImGuiApp.ImGuiController.Util.Clamp(15.0f, 5.0f, 10.0f));

		// Test value in range
		Assert.AreEqual(7.0f, ktsu.ImGuiApp.ImGuiController.Util.Clamp(7.0f, 5.0f, 10.0f));

		// Test edge cases
		Assert.AreEqual(5.0f, ktsu.ImGuiApp.ImGuiController.Util.Clamp(5.0f, 5.0f, 10.0f));
		Assert.AreEqual(10.0f, ktsu.ImGuiApp.ImGuiController.Util.Clamp(10.0f, 5.0f, 10.0f));
	}

	#endregion
}