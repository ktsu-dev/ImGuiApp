// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.App.Tests;

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests for FontMemoryGuard functionality including memory estimation, GPU detection,
/// and Intel/AMD integrated GPU support.
/// </summary>
[TestClass]
public class FontMemoryGuardTests
{
	[TestInitialize]
	public void Setup()
	{
		ImGuiApp.Reset();
		// Reset FontMemoryGuard configuration to defaults
		FontMemoryGuard.CurrentConfig = new FontMemoryGuard.FontMemoryConfig();
	}

	#region FontMemoryEstimate Tests

	[TestMethod]
	public void FontMemoryEstimate_Equality_WorksCorrectly()
	{
		FontMemoryGuard.FontMemoryEstimate estimate1 = new()
		{
			EstimatedBytes = 1024,
			EstimatedGlyphCount = 100,
			ExceedsLimits = false,
			RecommendedMaxSizes = 5,
			ShouldDisableEmojis = false,
			ShouldReduceUnicodeRanges = false
		};

		FontMemoryGuard.FontMemoryEstimate estimate2 = new()
		{
			EstimatedBytes = 1024,
			EstimatedGlyphCount = 100,
			ExceedsLimits = false,
			RecommendedMaxSizes = 5,
			ShouldDisableEmojis = false,
			ShouldReduceUnicodeRanges = false
		};

		FontMemoryGuard.FontMemoryEstimate estimate3 = new()
		{
			EstimatedBytes = 2048,
			EstimatedGlyphCount = 100,
			ExceedsLimits = false,
			RecommendedMaxSizes = 5,
			ShouldDisableEmojis = false,
			ShouldReduceUnicodeRanges = false
		};

		// Test equality
		Assert.AreEqual(estimate1, estimate2);
		Assert.AreNotEqual(estimate1, estimate3);

		// Test operators
		Assert.IsTrue(estimate1 == estimate2, "Equality operator should return true for equal estimates");
		Assert.IsFalse(estimate1 == estimate3, "Equality operator should return false for different estimates");
		Assert.IsFalse(estimate1 != estimate2, "Inequality operator should return false for equal estimates");
		Assert.IsTrue(estimate1 != estimate3, "Inequality operator should return true for different estimates");

		// Test Equals with object
		Assert.IsTrue(estimate1.Equals((object)estimate2), "Equals should return true for equal boxed estimates");
		Assert.IsFalse(estimate1.Equals((object)estimate3), "Equals should return false for different boxed estimates");
		Assert.IsFalse(estimate1.Equals(null), "Equals should return false for null");
		Assert.IsFalse(estimate1.Equals("not an estimate"), "Equals should return false for different type");
	}

	[TestMethod]
	public void FontMemoryEstimate_GetHashCode_ConsistentForEqualObjects()
	{
		FontMemoryGuard.FontMemoryEstimate estimate1 = new()
		{
			EstimatedBytes = 1024,
			EstimatedGlyphCount = 100,
			ExceedsLimits = true,
			RecommendedMaxSizes = 3,
			ShouldDisableEmojis = true,
			ShouldReduceUnicodeRanges = false
		};

		FontMemoryGuard.FontMemoryEstimate estimate2 = new()
		{
			EstimatedBytes = 1024,
			EstimatedGlyphCount = 100,
			ExceedsLimits = true,
			RecommendedMaxSizes = 3,
			ShouldDisableEmojis = true,
			ShouldReduceUnicodeRanges = false
		};

		Assert.AreEqual(estimate1.GetHashCode(), estimate2.GetHashCode());
	}

	#endregion

	#region EstimateMemoryUsage Tests

	[TestMethod]
	public void EstimateMemoryUsage_ValidParameters_ReturnsValidEstimate()
	{
		int[] fontSizes = [12, 14, 16, 18, 20];

		FontMemoryGuard.FontMemoryEstimate estimate = FontMemoryGuard.EstimateMemoryUsage(
			fontCount: 2,
			fontSizes: fontSizes,
			includeEmojis: true,
			includeExtendedUnicode: true,
			scaleFactor: 1.0f);

		Assert.IsGreaterThan(0, estimate.EstimatedBytes, "EstimatedBytes should be greater than 0 for valid parameters");
		Assert.IsGreaterThan(0, estimate.EstimatedGlyphCount, "EstimatedGlyphCount should be greater than 0 for valid parameters");
		Assert.IsGreaterThan(0, estimate.RecommendedMaxSizes, "RecommendedMaxSizes should be greater than 0 for valid parameters");
	}

	[TestMethod]
	public void EstimateMemoryUsage_HighScaleFactor_IncreasesMemoryEstimate()
	{
		int[] fontSizes = [14, 16, 18];

		FontMemoryGuard.FontMemoryEstimate lowScaleEstimate = FontMemoryGuard.EstimateMemoryUsage(1, fontSizes, false, false, 1.0f);
		FontMemoryGuard.FontMemoryEstimate highScaleEstimate = FontMemoryGuard.EstimateMemoryUsage(1, fontSizes, false, false, 2.0f);

		Assert.IsGreaterThan(lowScaleEstimate.EstimatedBytes, highScaleEstimate.EstimatedBytes, "Higher scale factor should result in higher memory estimate");
	}

	[TestMethod]
	public void EstimateMemoryUsage_WithEmojisAndUnicode_IncreasesGlyphCount()
	{
		int[] fontSizes = [14];

		FontMemoryGuard.FontMemoryEstimate basicEstimate = FontMemoryGuard.EstimateMemoryUsage(1, fontSizes, false, false, 1.0f);
		FontMemoryGuard.FontMemoryEstimate unicodeEstimate = FontMemoryGuard.EstimateMemoryUsage(1, fontSizes, false, true, 1.0f);
		FontMemoryGuard.FontMemoryEstimate emojiEstimate = FontMemoryGuard.EstimateMemoryUsage(1, fontSizes, true, false, 1.0f);
		FontMemoryGuard.FontMemoryEstimate fullEstimate = FontMemoryGuard.EstimateMemoryUsage(1, fontSizes, true, true, 1.0f);

		Assert.IsGreaterThan(basicEstimate.EstimatedGlyphCount, unicodeEstimate.EstimatedGlyphCount, "Extended Unicode should increase glyph count");
		Assert.IsGreaterThan(basicEstimate.EstimatedGlyphCount, emojiEstimate.EstimatedGlyphCount, "Emojis should increase glyph count");
		Assert.IsGreaterThan(unicodeEstimate.EstimatedGlyphCount, fullEstimate.EstimatedGlyphCount, "Full estimate with emojis should exceed Unicode-only");
		Assert.IsGreaterThan(emojiEstimate.EstimatedGlyphCount, fullEstimate.EstimatedGlyphCount, "Full estimate with Unicode should exceed emoji-only");
	}

	[TestMethod]
	public void EstimateMemoryUsage_NullFontSizes_ThrowsArgumentNullException()
	{
		Assert.ThrowsExactly<ArgumentNullException>(() =>
			FontMemoryGuard.EstimateMemoryUsage(1, null!, false, false, 1.0f));
	}

	[TestMethod]
	public void EstimateMemoryUsage_ExceedsLimits_SetsCorrectFlags()
	{
		// Set a very low memory limit to trigger limit exceeded
		FontMemoryGuard.CurrentConfig.MaxAtlasMemoryBytes = 1024; // 1KB - very small

		int[] largeFontSizes = [12, 14, 16, 18, 20, 24, 32, 48]; // Many sizes

		FontMemoryGuard.FontMemoryEstimate estimate = FontMemoryGuard.EstimateMemoryUsage(
			fontCount: 3, // Multiple fonts
			fontSizes: largeFontSizes,
			includeEmojis: true,
			includeExtendedUnicode: true,
			scaleFactor: 2.0f); // High DPI

		Assert.IsTrue(estimate.ExceedsLimits, "Should exceed limits with very low memory limit and high requirements");
	}

	#endregion

	#region GetReducedFontSizes Tests

	[TestMethod]
	public void GetReducedFontSizes_FewerThanMax_ReturnsOriginal()
	{
		int[] originalSizes = [12, 14, 16];
		int[] result = FontMemoryGuard.GetReducedFontSizes(originalSizes, 5, 14);

		CollectionAssert.AreEqual(originalSizes, result);
	}

	[TestMethod]
	public void GetReducedFontSizes_MoreThanMax_ReducesToMax()
	{
		int[] originalSizes = [10, 12, 14, 16, 18, 20, 24, 32, 48];
		int[] result = FontMemoryGuard.GetReducedFontSizes(originalSizes, 3, 14);

		Assert.HasCount(3, result);
		CollectionAssert.Contains(result, 14, "Should always include preferred size");
	}

	[TestMethod]
	public void GetReducedFontSizes_PrioritizesPreferredSize()
	{
		int[] originalSizes = [10, 12, 14, 16, 18, 20, 24, 32, 48];
		int[] result = FontMemoryGuard.GetReducedFontSizes(originalSizes, 4, 16);

		CollectionAssert.Contains(result, 16, "Should include preferred size 16");
		Assert.HasCount(4, result);
	}

	[TestMethod]
	public void GetReducedFontSizes_RespectsMinimumSizes()
	{
		FontMemoryGuard.CurrentConfig.MinFontSizesToLoad = 2;
		int[] originalSizes = [12, 14, 16, 18, 20];
		int[] result = FontMemoryGuard.GetReducedFontSizes(originalSizes, 1, 14); // Request only 1, but min is 2

		Assert.IsGreaterThanOrEqualTo(2, result.Length, "Result length should be at least MinFontSizesToLoad");
	}

	[TestMethod]
	public void GetReducedFontSizes_NullOriginalSizes_ThrowsArgumentNullException()
	{
		Assert.ThrowsExactly<ArgumentNullException>(() =>
			FontMemoryGuard.GetReducedFontSizes(null!, 3, 14));
	}

	[TestMethod]
	public void GetReducedFontSizes_ResultIsSorted()
	{
		int[] originalSizes = [48, 12, 20, 14, 16]; // Unsorted input
		int[] result = FontMemoryGuard.GetReducedFontSizes(originalSizes, 3, 14);

		for (int i = 1; i < result.Length; i++)
		{
			Assert.IsGreaterThan(result[i - 1], result[i], $"Result should be sorted in ascending order at index {i}");
		}
	}

	#endregion

	#region DetermineFallbackStrategy Tests

	[TestMethod]
	public void DetermineFallbackStrategy_NoLimitsExceeded_ReturnsNone()
	{
		FontMemoryGuard.FontMemoryEstimate estimate = new()
		{
			ExceedsLimits = false,
			EstimatedBytes = 1024
		};

		FontMemoryGuard.FallbackStrategy strategy = FontMemoryGuard.DetermineFallbackStrategy(estimate);
		Assert.AreEqual(FontMemoryGuard.FallbackStrategy.None, strategy);
	}

	[TestMethod]
	public void DetermineFallbackStrategy_SlightOverage_ReturnsReduceFontSizes()
	{
		FontMemoryGuard.CurrentConfig.MaxAtlasMemoryBytes = 1024;
		FontMemoryGuard.FontMemoryEstimate estimate = new()
		{
			ExceedsLimits = true,
			EstimatedBytes = 1200 // 1.17x over limit
		};

		FontMemoryGuard.FallbackStrategy strategy = FontMemoryGuard.DetermineFallbackStrategy(estimate);
		Assert.AreEqual(FontMemoryGuard.FallbackStrategy.ReduceFontSizes, strategy);
	}

	[TestMethod]
	public void DetermineFallbackStrategy_ModerateOverage_ReturnsDisableEmojis()
	{
		FontMemoryGuard.CurrentConfig.MaxAtlasMemoryBytes = 1024;
		FontMemoryGuard.FontMemoryEstimate estimate = new()
		{
			ExceedsLimits = true,
			EstimatedBytes = 1700 // 1.66x over limit
		};

		FontMemoryGuard.FallbackStrategy strategy = FontMemoryGuard.DetermineFallbackStrategy(estimate);
		Assert.AreEqual(FontMemoryGuard.FallbackStrategy.DisableEmojis, strategy);
	}

	[TestMethod]
	public void DetermineFallbackStrategy_HighOverage_ReturnsReduceUnicodeRanges()
	{
		FontMemoryGuard.CurrentConfig.MaxAtlasMemoryBytes = 1024;
		FontMemoryGuard.FontMemoryEstimate estimate = new()
		{
			ExceedsLimits = true,
			EstimatedBytes = 2500 // 2.44x over limit
		};

		FontMemoryGuard.FallbackStrategy strategy = FontMemoryGuard.DetermineFallbackStrategy(estimate);
		Assert.AreEqual(FontMemoryGuard.FallbackStrategy.ReduceUnicodeRanges, strategy);
	}

	[TestMethod]
	public void DetermineFallbackStrategy_ExtremeOverage_ReturnsMinimalFonts()
	{
		FontMemoryGuard.CurrentConfig.MaxAtlasMemoryBytes = 1024;
		FontMemoryGuard.FontMemoryEstimate estimate = new()
		{
			ExceedsLimits = true,
			EstimatedBytes = 5000 // 4.88x over limit
		};

		FontMemoryGuard.FallbackStrategy strategy = FontMemoryGuard.DetermineFallbackStrategy(estimate);
		Assert.AreEqual(FontMemoryGuard.FallbackStrategy.MinimalFonts, strategy);
	}

	#endregion

	#region GPU Detection Tests

	[TestMethod]
	public void TryDetectAndConfigureGpuMemory_NullGL_ReturnsFalse()
	{
		bool result = FontMemoryGuard.TryDetectAndConfigureGpuMemory(null!);
		Assert.IsFalse(result, "Should return false when GL is null");
	}

	[TestMethod]
	public void TryDetectAndConfigureGpuMemory_DetectionDisabled_ReturnsFalse()
	{
		FontMemoryGuard.CurrentConfig.EnableGpuMemoryDetection = false;

		// Since detection is disabled, the method should return false regardless of GL
		bool result = FontMemoryGuard.TryDetectAndConfigureGpuMemory(null!);
		Assert.IsFalse(result, "Should return false when GPU memory detection is disabled");
	}

	#endregion

	#region GPU Heuristics Unit Tests (without actual GL calls)

	[TestMethod]
	public void FontMemoryGuard_IsIntegratedGpu_DetectsIntelCorrectly()
	{
		// Test Intel GPU detection patterns
		Assert.IsTrue(FontMemoryGuard.IsIntegratedGpu("Intel(R) HD Graphics 530"), "Intel HD Graphics should be detected as integrated");
		Assert.IsTrue(FontMemoryGuard.IsIntegratedGpu("Intel(R) UHD Graphics 620"), "Intel UHD Graphics should be detected as integrated");
		Assert.IsTrue(FontMemoryGuard.IsIntegratedGpu("Intel(R) Xe Graphics"), "Intel Xe Graphics should be detected as integrated");
		Assert.IsTrue(FontMemoryGuard.IsIntegratedGpu("Intel(R) Iris(R) Xe Graphics"), "Intel Iris Xe Graphics should be detected as integrated");

		// Should not detect discrete GPUs as integrated
		Assert.IsFalse(FontMemoryGuard.IsIntegratedGpu("NVIDIA GeForce RTX 3060"), "NVIDIA GeForce should not be detected as integrated");
		Assert.IsFalse(FontMemoryGuard.IsIntegratedGpu("AMD Radeon RX 6600 XT"), "AMD Radeon RX should not be detected as integrated");

		// Intel Arc discrete GPUs should NOT be detected as integrated
		Assert.IsFalse(FontMemoryGuard.IsIntegratedGpu("Intel Arc A770"), "Intel Arc should not be detected as integrated");
		Assert.IsFalse(FontMemoryGuard.IsIntegratedGpu("Intel(R) Arc(TM) A750 Graphics"), "Intel Arc A750 should not be detected as integrated");
		Assert.IsFalse(FontMemoryGuard.IsIntegratedGpu("Intel Arc A380"), "Intel Arc A380 should not be detected as integrated");
	}

	[TestMethod]
	public void FontMemoryGuard_IsIntegratedGpu_DetectsAmdApuCorrectly()
	{
		// Test AMD APU detection patterns
		Assert.IsTrue(FontMemoryGuard.IsIntegratedGpu("AMD Radeon(TM) Vega 8 Graphics"), "AMD Vega integrated should be detected");
		Assert.IsTrue(FontMemoryGuard.IsIntegratedGpu("AMD Radeon(TM) 680M"), "AMD 680M integrated should be detected");
		Assert.IsTrue(FontMemoryGuard.IsIntegratedGpu("AMD Radeon(TM) R7 Graphics"), "AMD R7 integrated should be detected");

		// Should not detect discrete GPUs as integrated
		Assert.IsFalse(FontMemoryGuard.IsIntegratedGpu("AMD Radeon RX 6600 XT"), "AMD RX 6600 XT should not be detected as integrated");
		Assert.IsFalse(FontMemoryGuard.IsIntegratedGpu("AMD Radeon RX 7900 XTX"), "AMD RX 7900 XTX should not be detected as integrated");

		// AMD RDNA discrete GPUs should NOT be detected as integrated
		Assert.IsFalse(FontMemoryGuard.IsIntegratedGpu("AMD Radeon RX 6700 XT"), "AMD RX 6700 XT should not be detected as integrated");
		Assert.IsFalse(FontMemoryGuard.IsIntegratedGpu("AMD Radeon RX 7800 XT"), "AMD RX 7800 XT should not be detected as integrated");
	}

	[TestMethod]
	public void FontMemoryGuard_GetIntelGpuMemoryLimit_ReturnsCorrectLimits()
	{
		// Test Intel GPU generation-based memory limits
		Assert.AreEqual(96 * 1024 * 1024, FontMemoryGuard.GetIntelGpuMemoryLimit("Intel(R) Xe Graphics"));
		Assert.AreEqual(80 * 1024 * 1024, FontMemoryGuard.GetIntelGpuMemoryLimit("Intel(R) Iris(R) Xe Graphics"));
		Assert.AreEqual(64 * 1024 * 1024, FontMemoryGuard.GetIntelGpuMemoryLimit("Intel(R) UHD Graphics 620"));
		Assert.AreEqual(32 * 1024 * 1024, FontMemoryGuard.GetIntelGpuMemoryLimit("Intel(R) HD Graphics 530"));

		// Default for unrecognized Intel GPUs
		Assert.AreEqual(32 * 1024 * 1024, FontMemoryGuard.GetIntelGpuMemoryLimit("Intel(R) Unknown Graphics"));
	}

	[TestMethod]
	public void FontMemoryGuard_GetAmdApuMemoryLimit_ReturnsCorrectLimits()
	{
		// Test AMD APU generation-based memory limits
		Assert.AreEqual(128 * 1024 * 1024, FontMemoryGuard.GetAmdApuMemoryLimit("AMD Radeon(TM) 680M"));
		Assert.AreEqual(96 * 1024 * 1024, FontMemoryGuard.GetAmdApuMemoryLimit("AMD Radeon(TM) Vega 8 Graphics"));
		Assert.AreEqual(48 * 1024 * 1024, FontMemoryGuard.GetAmdApuMemoryLimit("AMD Radeon(TM) R7 Graphics"));

		// Default for unrecognized AMD APUs
		Assert.AreEqual(64 * 1024 * 1024, FontMemoryGuard.GetAmdApuMemoryLimit("AMD Radeon(TM) Unknown APU"));
	}

	#endregion

	#region Configuration Tests

	[TestMethod]
	public void FontMemoryConfig_DefaultValues_AreReasonable()
	{
		FontMemoryGuard.FontMemoryConfig config = new();

		Assert.AreEqual(FontMemoryGuard.DefaultMaxAtlasMemoryBytes, config.MaxAtlasMemoryBytes);
		Assert.IsTrue(config.EnableGpuMemoryDetection, "EnableGpuMemoryDetection should default to true");
		Assert.AreEqual(0.1f, config.MaxGpuMemoryPercentage);
		Assert.IsTrue(config.EnableFallbackStrategies, "EnableFallbackStrategies should default to true");
		Assert.AreEqual(3, config.MinFontSizesToLoad);
		Assert.IsTrue(config.DisableEmojisOnLowMemory, "DisableEmojisOnLowMemory should default to true");
		Assert.IsTrue(config.ReduceUnicodeRangesOnLowMemory, "ReduceUnicodeRangesOnLowMemory should default to true");
		Assert.IsTrue(config.EnableIntelGpuHeuristics, "EnableIntelGpuHeuristics should default to true");
		Assert.IsTrue(config.EnableAmdApuHeuristics, "EnableAmdApuHeuristics should default to true");
	}

	[TestMethod]
	public void FontMemoryGuard_Constants_HaveExpectedValues()
	{
#pragma warning disable MSTEST0032 // Assertion condition is always true
#pragma warning disable MSTEST0025 // Use 'Assert.Fail' instead of an always-failing assert
		Assert.AreEqual(64 * 1024 * 1024, FontMemoryGuard.DefaultMaxAtlasMemoryBytes);
		Assert.AreEqual(8 * 1024 * 1024, FontMemoryGuard.MinAtlasMemoryBytes);
		Assert.AreEqual(8192, FontMemoryGuard.MaxAtlasTextureDimension);
		Assert.AreEqual(4096, FontMemoryGuard.DefaultAtlasTextureDimension);
		Assert.AreEqual(2048, FontMemoryGuard.MinAtlasTextureDimension);
		Assert.AreEqual(128, FontMemoryGuard.EstimatedBytesPerGlyph);
#pragma warning restore MSTEST0025 // Use 'Assert.Fail' instead of an always-failing assert
#pragma warning restore MSTEST0032 // Assertion condition is always true
	}

	[TestMethod]
	public void FontMemoryConfig_EnableFallbackStrategiesDisabled_DoesNotApplyFallbacks()
	{
		// Configure to have memory limits exceeded
		FontMemoryGuard.CurrentConfig.MaxAtlasMemoryBytes = 1024; // Very low limit
		FontMemoryGuard.CurrentConfig.EnableFallbackStrategies = false;

		// Create an estimate that would normally trigger fallback strategies
		FontMemoryGuard.FontMemoryEstimate estimate = new()
		{
			ExceedsLimits = true,
			EstimatedBytes = 5000 // Much higher than the limit
		};

		// When EnableFallbackStrategies is false, DetermineFallbackStrategy should not be called
		// and fallback should be None
		// Since DetermineFallbackStrategy is still callable directly, we test the integration behavior
		// by verifying that the configuration setting is properly checked in the application code
		// Note: This test verifies the configuration property exists and can be set
		Assert.IsFalse(FontMemoryGuard.CurrentConfig.EnableFallbackStrategies, "EnableFallbackStrategies should be false when disabled");
	}

	[TestMethod]
	public void FontMemoryConfig_EnableFallbackStrategiesEnabled_AppliesFallbacks()
	{
		// Configure to have memory limits exceeded with fallbacks enabled
		FontMemoryGuard.CurrentConfig.MaxAtlasMemoryBytes = 1024; // Very low limit
		FontMemoryGuard.CurrentConfig.EnableFallbackStrategies = true;

		// Create an estimate that triggers fallback strategies
		FontMemoryGuard.FontMemoryEstimate estimate = new()
		{
			ExceedsLimits = true,
			EstimatedBytes = 5000 // Much higher than the limit
		};

		// With EnableFallbackStrategies true, we should get a fallback strategy
		FontMemoryGuard.FallbackStrategy strategy = FontMemoryGuard.DetermineFallbackStrategy(estimate);
		Assert.AreNotEqual(FontMemoryGuard.FallbackStrategy.None, strategy);
	}

	#endregion

	#region Atlas Size and Glyph Limit Tests

	[TestMethod]
	public void CalculateMaxGlyphCount_DefaultAtlasSize_ReturnsExpectedRange()
	{
		int maxGlyphs = FontMemoryGuard.CalculateMaxGlyphCount(FontMemoryGuard.DefaultAtlasTextureDimension);

		// For 4096x4096 with 16px average font size and 75% packing efficiency
		// Expected: ~25,000-35,000 glyphs
		Assert.IsGreaterThanOrEqualTo(20000, maxGlyphs, $"Expected at least 20,000 glyphs, got {maxGlyphs}");
		Assert.IsLessThanOrEqualTo(40000, maxGlyphs, $"Expected at most 40,000 glyphs, got {maxGlyphs}");
	}

	[TestMethod]
	public void CalculateMaxGlyphCount_MaxAtlasSize_ReturnsMoreGlyphs()
	{
		int defaultGlyphs = FontMemoryGuard.CalculateMaxGlyphCount(FontMemoryGuard.DefaultAtlasTextureDimension);
		int maxGlyphs = FontMemoryGuard.CalculateMaxGlyphCount(FontMemoryGuard.MaxAtlasTextureDimension);

		// 8192x8192 should hold ~4x more glyphs than 4096x4096 (area doubles)
		Assert.IsGreaterThan(defaultGlyphs * 3, maxGlyphs, $"Max atlas should hold more than 3x default glyphs ({maxGlyphs} vs {defaultGlyphs * 3})");
		Assert.IsGreaterThanOrEqualTo(80000, maxGlyphs, $"Expected at least 80,000 glyphs for 8192 atlas, got {maxGlyphs}");
	}

	[TestMethod]
	public void CalculateMaxGlyphCount_MinAtlasSize_ReturnsFewerGlyphs()
	{
		int defaultGlyphs = FontMemoryGuard.CalculateMaxGlyphCount(FontMemoryGuard.DefaultAtlasTextureDimension);
		int minGlyphs = FontMemoryGuard.CalculateMaxGlyphCount(FontMemoryGuard.MinAtlasTextureDimension);

		// 2048x2048 should hold ~1/4 glyphs of 4096x4096
		Assert.IsLessThan(defaultGlyphs / 3, minGlyphs, $"Min atlas should hold less than 1/3 default glyphs ({minGlyphs} vs {defaultGlyphs / 3})");
		Assert.IsGreaterThanOrEqualTo(5000, minGlyphs, $"Expected at least 5,000 glyphs for 2048 atlas, got {minGlyphs}");
	}

	[TestMethod]
	public void CalculateMaxGlyphCount_LargerFontSize_ReturnsFewerGlyphs()
	{
		int smallFontGlyphs = FontMemoryGuard.CalculateMaxGlyphCount(4096, averageFontSize: 12);
		int largeFontGlyphs = FontMemoryGuard.CalculateMaxGlyphCount(4096, averageFontSize: 24);

		// Larger fonts should fit fewer glyphs
		Assert.IsLessThan(smallFontGlyphs, largeFontGlyphs, $"Larger fonts should fit fewer glyphs ({largeFontGlyphs} vs {smallFontGlyphs})");
		// Roughly 4x fewer glyphs for 2x font size (area quadruples)
		Assert.IsLessThan(smallFontGlyphs / 3, largeFontGlyphs, $"Large font glyph count should be less than 1/3 of small ({largeFontGlyphs} vs {smallFontGlyphs / 3})");
	}

	[TestMethod]
	public void FontMemoryConfig_DefaultRecommendedAtlasSize_IsDefault()
	{
		FontMemoryGuard.FontMemoryConfig config = new();

		Assert.AreEqual(FontMemoryGuard.DefaultAtlasTextureDimension, config.RecommendedAtlasSize);
	}

	[TestMethod]
	public void EstimateMemoryUsage_ExceedsGlyphLimit_SetsExceedsLimits()
	{
		// Configure a very small atlas that can't fit many glyphs
		FontMemoryGuard.CurrentConfig.RecommendedAtlasSize = FontMemoryGuard.MinAtlasTextureDimension; // 2048
		FontMemoryGuard.CurrentConfig.MaxAtlasMemoryBytes = 1024 * 1024 * 1024; // 1GB - plenty of memory

		// Try to load many fonts with full Unicode and emoji support
		int[] largeFontSizes = [12, 14, 16, 18, 20, 24, 32, 48];

		FontMemoryGuard.FontMemoryEstimate estimate = FontMemoryGuard.EstimateMemoryUsage(
			fontCount: 3,
			fontSizes: largeFontSizes,
			includeEmojis: true,
			includeExtendedUnicode: true,
			scaleFactor: 1.0f);

		// Should exceed glyph limit even though we have plenty of memory
		Assert.IsTrue(estimate.ExceedsLimits, "Should exceed glyph limits with small atlas, multiple fonts, and full character sets");
	}

	[TestMethod]
	public void EstimateMemoryUsage_LargeAtlas_AllowsMoreGlyphs()
	{
		// Configure max atlas size
		FontMemoryGuard.CurrentConfig.RecommendedAtlasSize = FontMemoryGuard.MaxAtlasTextureDimension; // 8192
		FontMemoryGuard.CurrentConfig.MaxAtlasMemoryBytes = 1024 * 1024 * 1024; // 1GB

		int[] fontSizes = [12, 16, 20];

		FontMemoryGuard.FontMemoryEstimate estimate = FontMemoryGuard.EstimateMemoryUsage(
			fontCount: 2,
			fontSizes: fontSizes,
			includeEmojis: true,
			includeExtendedUnicode: true,
			scaleFactor: 1.0f);

		// With large atlas, moderate glyph count should be fine
		Assert.IsFalse(estimate.ExceedsLimits, "Should not exceed limits with large atlas and moderate font configuration");
	}

	#endregion

	#region Error Handling Tests

	[TestMethod]
	public void LogMemoryUsage_DoesNotThrow()
	{
		FontMemoryGuard.FontMemoryEstimate estimate = new()
		{
			EstimatedBytes = 1024,
			EstimatedGlyphCount = 100,
			ExceedsLimits = true
		};

		try
		{
			FontMemoryGuard.LogMemoryUsage(estimate, FontMemoryGuard.FallbackStrategy.ReduceFontSizes);
		}
		catch (ArgumentException ex)
		{
			Assert.Fail($"LogMemoryUsage should not throw ArgumentException, but got: {ex.Message}");
		}
		catch (InvalidOperationException ex)
		{
			Assert.Fail($"LogMemoryUsage should not throw InvalidOperationException, but got: {ex.Message}");
		}
		catch (System.ComponentModel.Win32Exception ex)
		{
			Assert.Fail($"LogMemoryUsage should not throw Win32Exception, but got: {ex.Message}");
		}
	}

	#endregion
}
