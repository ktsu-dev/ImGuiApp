// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGuiApp.Test;

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
		Assert.IsTrue(estimate1 == estimate2);
		Assert.IsFalse(estimate1 == estimate3);
		Assert.IsFalse(estimate1 != estimate2);
		Assert.IsTrue(estimate1 != estimate3);

		// Test Equals with object
		Assert.IsTrue(estimate1.Equals((object)estimate2));
		Assert.IsFalse(estimate1.Equals((object)estimate3));
		Assert.IsFalse(estimate1.Equals(null));
		Assert.IsFalse(estimate1.Equals("not an estimate"));
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

		Assert.IsTrue(estimate.EstimatedBytes > 0);
		Assert.IsTrue(estimate.EstimatedGlyphCount > 0);
		Assert.IsTrue(estimate.RecommendedMaxSizes > 0);
	}

	[TestMethod]
	public void EstimateMemoryUsage_HighScaleFactor_IncreasesMemoryEstimate()
	{
		int[] fontSizes = [14, 16, 18];

		FontMemoryGuard.FontMemoryEstimate lowScaleEstimate = FontMemoryGuard.EstimateMemoryUsage(1, fontSizes, false, false, 1.0f);
		FontMemoryGuard.FontMemoryEstimate highScaleEstimate = FontMemoryGuard.EstimateMemoryUsage(1, fontSizes, false, false, 2.0f);

		Assert.IsTrue(highScaleEstimate.EstimatedBytes > lowScaleEstimate.EstimatedBytes);
	}

	[TestMethod]
	public void EstimateMemoryUsage_WithEmojisAndUnicode_IncreasesGlyphCount()
	{
		int[] fontSizes = [14];

		FontMemoryGuard.FontMemoryEstimate basicEstimate = FontMemoryGuard.EstimateMemoryUsage(1, fontSizes, false, false, 1.0f);
		FontMemoryGuard.FontMemoryEstimate unicodeEstimate = FontMemoryGuard.EstimateMemoryUsage(1, fontSizes, false, true, 1.0f);
		FontMemoryGuard.FontMemoryEstimate emojiEstimate = FontMemoryGuard.EstimateMemoryUsage(1, fontSizes, true, false, 1.0f);
		FontMemoryGuard.FontMemoryEstimate fullEstimate = FontMemoryGuard.EstimateMemoryUsage(1, fontSizes, true, true, 1.0f);

		Assert.IsTrue(unicodeEstimate.EstimatedGlyphCount > basicEstimate.EstimatedGlyphCount);
		Assert.IsTrue(emojiEstimate.EstimatedGlyphCount > basicEstimate.EstimatedGlyphCount);
		Assert.IsTrue(fullEstimate.EstimatedGlyphCount > unicodeEstimate.EstimatedGlyphCount);
		Assert.IsTrue(fullEstimate.EstimatedGlyphCount > emojiEstimate.EstimatedGlyphCount);
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

		Assert.IsTrue(estimate.ExceedsLimits, "Should exceed the very low memory limit");
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

		Assert.AreEqual(3, result.Length);
		Assert.IsTrue(result.Contains(14), "Should always include preferred size");
	}

	[TestMethod]
	public void GetReducedFontSizes_PrioritizesPreferredSize()
	{
		int[] originalSizes = [10, 12, 14, 16, 18, 20, 24, 32, 48];
		int[] result = FontMemoryGuard.GetReducedFontSizes(originalSizes, 4, 16);

		Assert.IsTrue(result.Contains(16), "Should include preferred size 16");
		Assert.AreEqual(4, result.Length);
	}

	[TestMethod]
	public void GetReducedFontSizes_RespectsMinimumSizes()
	{
		FontMemoryGuard.CurrentConfig.MinFontSizesToLoad = 2;
		int[] originalSizes = [12, 14, 16, 18, 20];
		int[] result = FontMemoryGuard.GetReducedFontSizes(originalSizes, 1, 14); // Request only 1, but min is 2

		Assert.IsTrue(result.Length >= 2, "Should respect minimum font sizes setting");
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
			Assert.IsTrue(result[i] > result[i - 1], "Result should be sorted in ascending order");
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
		Assert.IsFalse(result);
	}

	[TestMethod]
	public void TryDetectAndConfigureGpuMemory_DetectionDisabled_ReturnsFalse()
	{
		FontMemoryGuard.CurrentConfig.EnableGpuMemoryDetection = false;

		// Since detection is disabled, the method should return false regardless of GL
		bool result = FontMemoryGuard.TryDetectAndConfigureGpuMemory(null!);
		Assert.IsFalse(result);
	}

	#endregion

	#region GPU Heuristics Unit Tests (without actual GL calls)

	[TestMethod]
	public void FontMemoryGuard_IsIntegratedGpu_DetectsIntelCorrectly()
	{
		// Test Intel GPU detection patterns
		Assert.IsTrue(FontMemoryGuard.IsIntegratedGpu("Intel(R) HD Graphics 530"));
		Assert.IsTrue(FontMemoryGuard.IsIntegratedGpu("Intel(R) UHD Graphics 620"));
		Assert.IsTrue(FontMemoryGuard.IsIntegratedGpu("Intel(R) Xe Graphics"));
		Assert.IsTrue(FontMemoryGuard.IsIntegratedGpu("Intel(R) Iris(R) Xe Graphics"));

		// Should not detect discrete GPUs as integrated
		Assert.IsFalse(FontMemoryGuard.IsIntegratedGpu("NVIDIA GeForce RTX 3060"));
		Assert.IsFalse(FontMemoryGuard.IsIntegratedGpu("AMD Radeon RX 6600 XT"));
	}

	[TestMethod]
	public void FontMemoryGuard_IsIntegratedGpu_DetectsAmdApuCorrectly()
	{
		// Test AMD APU detection patterns
		Assert.IsTrue(FontMemoryGuard.IsIntegratedGpu("AMD Radeon(TM) Vega 8 Graphics"));
		Assert.IsTrue(FontMemoryGuard.IsIntegratedGpu("AMD Radeon(TM) 680M"));
		Assert.IsTrue(FontMemoryGuard.IsIntegratedGpu("AMD Radeon(TM) R7 Graphics"));

		// Should not detect discrete GPUs as integrated
		Assert.IsFalse(FontMemoryGuard.IsIntegratedGpu("AMD Radeon RX 6600 XT"));
		Assert.IsFalse(FontMemoryGuard.IsIntegratedGpu("AMD Radeon RX 7900 XTX"));
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
		Assert.IsTrue(config.EnableGpuMemoryDetection);
		Assert.AreEqual(0.1f, config.MaxGpuMemoryPercentage);
		Assert.IsTrue(config.EnableFallbackStrategies);
		Assert.AreEqual(3, config.MinFontSizesToLoad);
		Assert.IsTrue(config.DisableEmojisOnLowMemory);
		Assert.IsTrue(config.ReduceUnicodeRangesOnLowMemory);
		Assert.IsTrue(config.EnableIntelGpuHeuristics);
		Assert.IsTrue(config.EnableAmdApuHeuristics);
	}

	[TestMethod]
	public void FontMemoryGuard_Constants_HaveExpectedValues()
	{
		Assert.AreEqual(64 * 1024 * 1024, FontMemoryGuard.DefaultMaxAtlasMemoryBytes);
		Assert.AreEqual(8 * 1024 * 1024, FontMemoryGuard.MinAtlasMemoryBytes);
		Assert.AreEqual(4096, FontMemoryGuard.MaxAtlasTextureDimension);
		Assert.AreEqual(128, FontMemoryGuard.EstimatedBytesPerGlyph);
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
