// Copyright (c) 2023-2026 ktsu-dev contributors

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
		Assert.IsTrue(fontHelperType.IsAbstract && fontHelperType.IsSealed, "FontHelper should be a static class (abstract and sealed)");
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
	public unsafe void FontHelper_ExtendedRanges_IncludeSuperscripts()
	{
		// Units such as km³ and s⁻¹ are written with superscript digits and signs. ³ is Latin-1, but
		// ⁻ (U+207B) and ⁴ (U+2074) live in Superscripts and Subscripts, which the ranges used to skip.
		Hexa.NET.ImGui.ImGuiContextPtr context = Hexa.NET.ImGui.ImGui.CreateContext();
		try
		{
			FontHelper.CleanupGlyphRanges();
			uint* ranges = FontHelper.GetExtendedUnicodeRanges(Hexa.NET.ImGui.ImGui.GetIO().Fonts);

			Assert.IsTrue(RangesContain(ranges, 0x207B), "U+207B SUPERSCRIPT MINUS is not in the extended ranges.");
			Assert.IsTrue(RangesContain(ranges, 0x2074), "U+2074 SUPERSCRIPT FOUR is not in the extended ranges.");
		}
		finally
		{
			FontHelper.CleanupGlyphRanges();
			Hexa.NET.ImGui.ImGui.DestroyContext(context);
		}
	}

	private static unsafe bool RangesContain(uint* ranges, uint codePoint)
	{
		for (uint* pair = ranges; pair[0] != 0; pair += 2)
		{
			if (codePoint >= pair[0] && codePoint <= pair[1])
			{
				return true;
			}
		}

		return false;
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
		Assert.IsTrue(typeof(FontAppearance).IsSubclassOf(typeof(ScopedAction.ScopedAction)), "FontAppearance should inherit from ScopedAction");
	}

	[TestMethod]
	public void FontAppearance_Constants_HaveExpectedValues()
	{
#pragma warning disable MSTEST0032 // Assertion condition is always true
#pragma warning disable MSTEST0025 // Use 'Assert.Fail' instead of an always-failing assert
		// Access FontAppearance constants directly using internal access
		Assert.AreEqual("default", FontAppearance.DefaultFontName);
		Assert.AreEqual(14, FontAppearance.DefaultFontPointSize);
#pragma warning restore MSTEST0025 // Use 'Assert.Fail' instead of an always-failing assert
#pragma warning restore MSTEST0032 // Assertion condition is always true
	}

	#endregion

	#region UIScaler Tests

	[TestMethod]
	public void UIScaler_InheritsScopedAction()
	{
		Assert.IsTrue(typeof(UIScaler).IsSubclassOf(typeof(ScopedAction.ScopedAction)), "UIScaler should inherit from ScopedAction");
	}

	#endregion

	#region Util Tests

	[TestMethod]
	public void Util_IsStaticClass()
	{
		Type utilType = typeof(ImGuiController.Util);
		Assert.IsTrue(utilType.IsAbstract && utilType.IsSealed, "Util should be a static class (abstract and sealed)");
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
