// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.App;

using System;
using System.Collections.Generic;
using System.Linq;
using Hexa.NET.ImGui;
using Silk.NET.OpenGL;

/// <summary>
/// Provides memory guards and limits for font loading to prevent excessive texture memory allocation
/// on small GPUs or high-resolution displays.
///
/// <para><strong>Intel &amp; AMD Integrated GPU Support:</strong></para>
/// <para>Special handling for integrated graphics which are the primary targets for memory constraints:</para>
/// <list type="bullet">
/// <item><strong>Intel Graphics:</strong> HD Graphics, UHD Graphics, Iris, Xe Graphics - Often don't expose memory query extensions</item>
/// <item><strong>AMD APUs:</strong> Vega, RDNA integrated graphics - Share system RAM with limited bandwidth</item>
/// <item><strong>Automatic Detection:</strong> Uses renderer string analysis when OpenGL memory extensions aren't available</item>
/// <item><strong>Conservative Limits:</strong> 16-96MB font atlas limits for integrated GPUs vs 64-128MB for discrete</item>
/// <item><strong>Generation-Aware:</strong> Newer integrated GPUs (Xe, RDNA2+) get higher limits than older ones</item>
/// </list>
///
/// <para><strong>Why This Matters:</strong></para>
/// <para>Integrated GPUs share system RAM and have limited memory bandwidth. A 4K display with full Unicode
/// font support can easily create 200MB+ font atlases, which can cause:</para>
/// <list type="bullet">
/// <item>Application crashes due to GPU memory exhaustion</item>
/// <item>Severe performance degradation from memory pressure</item>
/// <item>System-wide slowdowns as integrated GPU competes with CPU for RAM bandwidth</item>
/// </list>
/// </summary>
public static class FontMemoryGuard
{
	/// <summary>
	/// Default maximum font atlas texture size in bytes (64MB).
	/// This is conservative for integrated GPUs and high-DPI displays.
	/// </summary>
	public const long DefaultMaxAtlasMemoryBytes = 64 * 1024 * 1024; // 64MB

	/// <summary>
	/// Minimum font atlas texture size in bytes (8MB).
	/// Below this threshold, basic functionality may be compromised.
	/// </summary>
	public const long MinAtlasMemoryBytes = 8 * 1024 * 1024; // 8MB

	/// <summary>
	/// Maximum font atlas texture dimension (e.g., 4096x4096).
	/// Most GPUs support at least this size.
	/// </summary>
	public const int MaxAtlasTextureDimension = 4096;

	/// <summary>
	/// Estimated bytes per glyph for memory calculations.
	/// This is a rough estimate based on typical font rasterization.
	/// </summary>
	public const int EstimatedBytesPerGlyph = 128;

	/// <summary>
	/// Configuration for font memory limits.
	/// </summary>
	public class FontMemoryConfig
	{
		/// <summary>
		/// Maximum memory to allocate for font atlas textures in bytes.
		/// </summary>
		public long MaxAtlasMemoryBytes { get; set; } = DefaultMaxAtlasMemoryBytes;

		/// <summary>
		/// Whether to enable automatic GPU memory detection.
		/// </summary>
		public bool EnableGpuMemoryDetection { get; set; } = true;

		/// <summary>
		/// Maximum percentage of available GPU memory to use for font textures.
		/// Note: Integrated GPUs automatically use lower percentages (3-5%) regardless of this setting.
		/// </summary>
		public float MaxGpuMemoryPercentage { get; set; } = 0.1f; // 10% for discrete GPUs

		/// <summary>
		/// Whether to enable special handling for Intel integrated GPUs.
		/// Intel GPUs often don't expose memory query extensions, so we use heuristics.
		/// </summary>
		public bool EnableIntelGpuHeuristics { get; set; } = true;

		/// <summary>
		/// Whether to enable special handling for AMD integrated GPUs (APUs).
		/// </summary>
		public bool EnableAmdApuHeuristics { get; set; } = true;

		/// <summary>
		/// Whether to enable fallback strategies when memory limits are exceeded.
		/// </summary>
		public bool EnableFallbackStrategies { get; set; } = true;

		/// <summary>
		/// Minimum number of font sizes to load even under memory constraints.
		/// </summary>
		public int MinFontSizesToLoad { get; set; } = 3; // e.g., 12, 16, 20

		/// <summary>
		/// Whether to disable emoji fonts under memory constraints.
		/// </summary>
		public bool DisableEmojisOnLowMemory { get; set; } = true;

		/// <summary>
		/// Whether to reduce Unicode glyph ranges under memory constraints.
		/// </summary>
		public bool ReduceUnicodeRangesOnLowMemory { get; set; } = true;
	}

	/// <summary>
	/// Result of font memory estimation.
	/// </summary>
	public struct FontMemoryEstimate : IEquatable<FontMemoryEstimate>
	{
		/// <summary>
		/// Estimated memory usage in bytes.
		/// </summary>
		public long EstimatedBytes { get; set; }

		/// <summary>
		/// Number of glyphs estimated.
		/// </summary>
		public int EstimatedGlyphCount { get; set; }

		/// <summary>
		/// Whether the estimate exceeds memory limits.
		/// </summary>
		public bool ExceedsLimits { get; set; }

		/// <summary>
		/// Recommended maximum number of font sizes to load.
		/// </summary>
		public int RecommendedMaxSizes { get; set; }

		/// <summary>
		/// Whether emoji fonts should be disabled.
		/// </summary>
		public bool ShouldDisableEmojis { get; set; }

		/// <summary>
		/// Whether Unicode ranges should be reduced.
		/// </summary>
		public bool ShouldReduceUnicodeRanges { get; set; }

		/// <summary>
		/// Determines whether the specified object is equal to the current object.
		/// </summary>
		/// <param name="obj">The object to compare with the current object.</param>
		/// <returns>true if the specified object is equal to the current object; otherwise, false.</returns>
		public override readonly bool Equals(object? obj) => obj is FontMemoryEstimate estimate && Equals(estimate);

		/// <summary>
		/// Serves as the default hash function.
		/// </summary>
		/// <returns>A hash code for the current object.</returns>
		public override readonly int GetHashCode() => HashCode.Combine(EstimatedBytes, EstimatedGlyphCount, ExceedsLimits, RecommendedMaxSizes, ShouldDisableEmojis, ShouldReduceUnicodeRanges);

		/// <summary>
		/// Indicates whether the current object is equal to another object of the same type.
		/// </summary>
		/// <param name="other">An object to compare with this object.</param>
		/// <returns>true if the current object is equal to the other parameter; otherwise, false.</returns>
		public readonly bool Equals(FontMemoryEstimate other) =>
			EstimatedBytes == other.EstimatedBytes &&
			EstimatedGlyphCount == other.EstimatedGlyphCount &&
			ExceedsLimits == other.ExceedsLimits &&
			RecommendedMaxSizes == other.RecommendedMaxSizes &&
			ShouldDisableEmojis == other.ShouldDisableEmojis &&
			ShouldReduceUnicodeRanges == other.ShouldReduceUnicodeRanges;

		/// <summary>
		/// Determines whether two specified instances of FontMemoryEstimate are equal.
		/// </summary>
		/// <param name="left">The first FontMemoryEstimate to compare.</param>
		/// <param name="right">The second FontMemoryEstimate to compare.</param>
		/// <returns>true if the two FontMemoryEstimate instances are equal; otherwise, false.</returns>
		public static bool operator ==(FontMemoryEstimate left, FontMemoryEstimate right) => left.Equals(right);

		/// <summary>
		/// Determines whether two specified instances of FontMemoryEstimate are not equal.
		/// </summary>
		/// <param name="left">The first FontMemoryEstimate to compare.</param>
		/// <param name="right">The second FontMemoryEstimate to compare.</param>
		/// <returns>true if the two FontMemoryEstimate instances are not equal; otherwise, false.</returns>
		public static bool operator !=(FontMemoryEstimate left, FontMemoryEstimate right) => !(left == right);
	}

	/// <summary>
	/// Fallback strategy for font loading under memory constraints.
	/// </summary>
	public enum FallbackStrategy
	{
		/// <summary>
		/// No fallback needed.
		/// </summary>
		None,

		/// <summary>
		/// Reduce number of font sizes.
		/// </summary>
		ReduceFontSizes,

		/// <summary>
		/// Disable emoji fonts.
		/// </summary>
		DisableEmojis,

		/// <summary>
		/// Reduce Unicode glyph ranges.
		/// </summary>
		ReduceUnicodeRanges,

		/// <summary>
		/// Aggressive fallback: minimal fonts only.
		/// </summary>
		MinimalFonts
	}

	/// <summary>
	/// Gets the current font memory configuration.
	/// </summary>
	public static FontMemoryConfig CurrentConfig { get; set; } = new();

	/// <summary>
	/// Estimates the memory usage for font loading based on the provided parameters.
	/// </summary>
	/// <param name="fontCount">Number of font files to load. Must be greater than zero.</param>
	/// <param name="fontSizes">Array of font sizes to load. Cannot be null or empty.</param>
	/// <param name="includeEmojis">Whether emoji fonts will be included.</param>
	/// <param name="includeExtendedUnicode">Whether extended Unicode ranges will be included.</param>
	/// <param name="scaleFactor">Current DPI scale factor. Typically between 1.0 and 4.0.</param>
	/// <returns>Font memory estimate.</returns>
	/// <exception cref="ArgumentNullException">Thrown when fontSizes is null.</exception>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when fontCount is less than or equal to zero.</exception>
	public static FontMemoryEstimate EstimateMemoryUsage(
		int fontCount,
		int[] fontSizes,
		bool includeEmojis,
		bool includeExtendedUnicode,
		float scaleFactor)
	{
		ArgumentNullException.ThrowIfNull(fontSizes);

		if (fontCount <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(fontCount), fontCount, "Font count must be greater than zero.");
		}

		// Estimate glyph count based on configuration
		int baseGlyphCount = 128; // ASCII
		if (includeExtendedUnicode)
		{
			baseGlyphCount += GetExtendedUnicodeGlyphCount();
		}
		if (includeEmojis)
		{
			baseGlyphCount += GetEmojiGlyphCount();
		}

		// Calculate total glyph count across all fonts and sizes
		int totalGlyphCount = fontCount * fontSizes.Length * baseGlyphCount;

		// Apply scale factor impact (higher DPI = larger textures)
		float scaleImpact = scaleFactor * scaleFactor; // Quadratic impact on texture size
		long estimatedBytes = (long)(totalGlyphCount * EstimatedBytesPerGlyph * scaleImpact);

		// Determine if limits are exceeded and recommend fallbacks
		bool exceedsLimits = estimatedBytes > CurrentConfig.MaxAtlasMemoryBytes;
		int recommendedMaxSizes = CalculateRecommendedMaxSizes(estimatedBytes, fontCount, baseGlyphCount, scaleFactor);
		bool shouldDisableEmojis = exceedsLimits && CurrentConfig.DisableEmojisOnLowMemory && includeEmojis;
		bool shouldReduceUnicode = exceedsLimits && CurrentConfig.ReduceUnicodeRangesOnLowMemory && includeExtendedUnicode;

		return new FontMemoryEstimate
		{
			EstimatedBytes = estimatedBytes,
			EstimatedGlyphCount = totalGlyphCount,
			ExceedsLimits = exceedsLimits,
			RecommendedMaxSizes = recommendedMaxSizes,
			ShouldDisableEmojis = shouldDisableEmojis,
			ShouldReduceUnicodeRanges = shouldReduceUnicode
		};
	}

	/// <summary>
	/// Attempts to detect available GPU memory and update configuration accordingly.
	/// Special handling for Intel and AMD integrated GPUs which are primary targets for memory constraints.
	/// </summary>
	/// <param name="gl">OpenGL context for querying GPU information.</param>
	/// <returns>True if GPU memory was successfully detected and configuration updated.</returns>
	public static unsafe bool TryDetectAndConfigureGpuMemory(GL gl)
	{
		if (!CurrentConfig.EnableGpuMemoryDetection || gl == null)
		{
			return false;
		}

		try
		{
			// Get GPU vendor and renderer information
			string vendor = gl.GetStringS(GLEnum.Vendor) ?? "";
			string renderer = gl.GetStringS(GLEnum.Renderer) ?? "";
			bool isIntelGpu = vendor.Contains("Intel", StringComparison.OrdinalIgnoreCase);
			bool isAmdGpu = vendor.Contains("AMD", StringComparison.OrdinalIgnoreCase) || vendor.Contains("ATI", StringComparison.OrdinalIgnoreCase);
			bool isNvidiaGpu = vendor.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase);
			bool isIntegratedGpu = IsIntegratedGpu(renderer);

			DebugLogger.Log($"FontMemoryGuard: Detected GPU - Vendor: {vendor}, Renderer: {renderer}");
			DebugLogger.Log($"FontMemoryGuard: GPU Classification - Intel: {isIntelGpu}, AMD: {isAmdGpu}, NVIDIA: {isNvidiaGpu}, Integrated: {isIntegratedGpu}");

			// Try to get GPU memory information using OpenGL extensions
			bool memoryDetected = false;
			long availableMemoryKB = 0;

			// Try NVIDIA extension first (works on discrete NVIDIA GPUs)
			// Note: NVIDIA memory extensions typically aren't available as TryGetExtension in Silk.NET
			// We'll use direct OpenGL calls instead
			if (isNvidiaGpu && gl.IsExtensionPresent("GL_NVX_gpu_memory_info"))
			{
				gl.GetInteger((GLEnum)0x9048, out int totalMemKB); // GL_GPU_MEMORY_TOTAL_AVAILABLE_MEMORY_NVX
				gl.GetInteger((GLEnum)0x9049, out int currentMemKB); // GL_GPU_MEMORY_CURRENT_AVAILABLE_MEMORY_NVX
				availableMemoryKB = currentMemKB;
				memoryDetected = true;
				DebugLogger.Log($"FontMemoryGuard: NVIDIA GPU memory: {totalMemKB}KB total, {currentMemKB}KB available");
			}
			// Try ATI extension (works on AMD discrete and some integrated GPUs)
			else if (isAmdGpu && gl.IsExtensionPresent("GL_ATI_meminfo"))
			{
				Span<int> memInfo = stackalloc int[4];
				gl.GetInteger((GLEnum)0x87FC, memInfo); // GL_TEXTURE_FREE_MEMORY_ATI
				availableMemoryKB = memInfo[0];
				memoryDetected = true;
				DebugLogger.Log($"FontMemoryGuard: AMD GPU texture memory: {availableMemoryKB}KB available");
			}

			// Apply memory-based configuration if detected
			if (memoryDetected && availableMemoryKB > 0)
			{
				long recommendedFontMemory = CalculateRecommendedMemoryFromDetection(availableMemoryKB, isIntegratedGpu);
				CurrentConfig.MaxAtlasMemoryBytes = recommendedFontMemory;
				DebugLogger.Log($"FontMemoryGuard: Set font memory limit to {recommendedFontMemory / (1024 * 1024)}MB based on GPU memory detection");
				return true;
			}

			// Fallback to heuristic-based configuration for Intel and undetected GPUs
			if (ApplyIntegratedGpuHeuristics(isIntelGpu, isAmdGpu, isIntegratedGpu, renderer))
			{
				return true;
			}
		}
		catch (InvalidOperationException ex)
		{
			DebugLogger.Log($"FontMemoryGuard: GPU memory detection failed with InvalidOperationException: {ex.Message}");
		}
		catch (ArgumentException ex)
		{
			DebugLogger.Log($"FontMemoryGuard: GPU memory detection failed with ArgumentException: {ex.Message}");
		}
		catch (System.ComponentModel.Win32Exception ex)
		{
			DebugLogger.Log($"FontMemoryGuard: GPU memory detection failed with Win32Exception: {ex.Message}");
		}

		return false;
	}

	/// <summary>
	/// Determines if a GPU is integrated based on renderer string analysis.
	/// </summary>
	/// <param name="renderer">GPU renderer string from OpenGL.</param>
	/// <returns>True if the GPU appears to be integrated.</returns>
	public static bool IsIntegratedGpu(string renderer)
	{
		if (string.IsNullOrEmpty(renderer))
		{
			return false;
		}

		string rendererLower = renderer.ToLowerInvariant();

		// Intel integrated GPU patterns
		string[] intelIntegratedPatterns = [
			"intel", "hd graphics", "uhd graphics", "iris", "xe graphics"
		];

		// AMD integrated GPU patterns
		string[] amdIntegratedPatterns = [
			"radeon(tm)", "vega", "rdna", "apu", "r5 graphics", "r7 graphics"
		];

		// General integrated GPU indicators
		string[] integratedPatterns = [
			"integrated", "onboard", "shared"
		];

		return intelIntegratedPatterns.Any(rendererLower.Contains) ||
			   amdIntegratedPatterns.Any(rendererLower.Contains) ||
			   integratedPatterns.Any(rendererLower.Contains);
	}

	/// <summary>
	/// Applies memory configuration heuristics for integrated GPUs when direct memory detection fails.
	/// This is especially important for Intel integrated GPUs which often don't expose memory extensions.
	/// </summary>
	/// <param name="isIntelGpu">Whether this is an Intel GPU.</param>
	/// <param name="isAmdGpu">Whether this is an AMD GPU.</param>
	/// <param name="isIntegratedGpu">Whether this appears to be integrated graphics.</param>
	/// <param name="renderer">GPU renderer string for additional analysis.</param>
	/// <returns>True if heuristics were applied and configuration updated.</returns>
	private static bool ApplyIntegratedGpuHeuristics(bool isIntelGpu, bool isAmdGpu, bool isIntegratedGpu, string renderer)
	{
		if (!isIntegratedGpu && !isIntelGpu)
		{
			return false; // Only apply heuristics for suspected integrated GPUs
		}

		long recommendedMemory = DefaultMaxAtlasMemoryBytes; // Default 64MB

		if (isIntelGpu)
		{
			// Intel integrated GPU memory recommendations
			recommendedMemory = GetIntelGpuMemoryLimit(renderer);
			DebugLogger.Log($"FontMemoryGuard: Applied Intel integrated GPU heuristics: {recommendedMemory / (1024 * 1024)}MB limit");
		}
		else if (isAmdGpu && isIntegratedGpu)
		{
			// AMD integrated GPU (APU) memory recommendations
			recommendedMemory = GetAmdApuMemoryLimit(renderer);
			DebugLogger.Log($"FontMemoryGuard: Applied AMD integrated GPU heuristics: {recommendedMemory / (1024 * 1024)}MB limit");
		}
		else if (isIntegratedGpu)
		{
			// Generic integrated GPU - be conservative
			recommendedMemory = 32 * 1024 * 1024; // 32MB
			DebugLogger.Log($"FontMemoryGuard: Applied generic integrated GPU heuristics: {recommendedMemory / (1024 * 1024)}MB limit");
		}

		CurrentConfig.MaxAtlasMemoryBytes = recommendedMemory;

		// Enable more aggressive fallbacks for integrated GPUs
		CurrentConfig.DisableEmojisOnLowMemory = true;
		CurrentConfig.ReduceUnicodeRangesOnLowMemory = true;
		CurrentConfig.MaxGpuMemoryPercentage = 0.05f; // Use only 5% for integrated GPUs

		return true;
	}

	/// <summary>
	/// Gets the recommended memory limit for Intel integrated graphics based on the renderer string.
	/// </summary>
	/// <param name="renderer">The Intel GPU renderer string.</param>
	/// <returns>Recommended memory limit in bytes.</returns>
	public static long GetIntelGpuMemoryLimit(string renderer)
	{
		if (string.IsNullOrEmpty(renderer))
		{
			return 32 * 1024 * 1024; // Conservative 32MB for unknown Intel GPU
		}

		string rendererLower = renderer.ToLowerInvariant();

		// Iris graphics (higher end integrated) - check first before Xe to get correct priority
		if (rendererLower.Contains("iris"))
		{
			return 80 * 1024 * 1024; // 80MB - Iris has better performance
		}

		// Modern Intel GPUs (12th gen+, Xe Graphics)
		if (rendererLower.Contains("xe") || rendererLower.Contains("arc"))
		{
			return 96 * 1024 * 1024; // 96MB - Modern Intel has better memory bandwidth
		}

		// Recent Intel GPUs (UHD Graphics series)
		if (rendererLower.Contains("uhd"))
		{
			return 64 * 1024 * 1024; // 64MB - Standard limit
		}

		// Older Intel GPUs (HD Graphics series)
		if (rendererLower.Contains("hd graphics"))
		{
			// Extract generation number if possible
			if (rendererLower.Contains("630") || rendererLower.Contains("620") || rendererLower.Contains("610"))
			{
				return 48 * 1024 * 1024; // 48MB - 7th/8th gen
			}
			else if (rendererLower.Contains("530") || rendererLower.Contains("520") || rendererLower.Contains("510"))
			{
				return 32 * 1024 * 1024; // 32MB - 6th gen and older
			}
			else
			{
				return 24 * 1024 * 1024; // 24MB - Very old Intel GPUs
			}
		}

		// Default for unknown Intel GPU
		return 32 * 1024 * 1024; // 32MB conservative
	}

	/// <summary>
	/// Gets the recommended memory limit for AMD APU graphics based on the renderer string.
	/// </summary>
	/// <param name="renderer">The AMD APU renderer string.</param>
	/// <returns>Recommended memory limit in bytes.</returns>
	public static long GetAmdApuMemoryLimit(string renderer)
	{
		if (string.IsNullOrEmpty(renderer))
		{
			return 48 * 1024 * 1024; // Conservative 48MB for unknown AMD APU
		}

		string rendererLower = renderer.ToLowerInvariant();

		// Modern RDNA2/3 APUs (Ryzen 6000+, Steam Deck, 680M/780M, Radeon(TM) Graphics)
		if (rendererLower.Contains("680m") || rendererLower.Contains("780m") || rendererLower.Contains("steam deck") ||
			(rendererLower.Contains("radeon(tm)") && rendererLower.Contains("graphics")))
		{
			return 128 * 1024 * 1024; // 128MB - Modern AMD APUs are quite capable
		}

		// Vega-based APUs (good performance)
		if (rendererLower.Contains("vega"))
		{
			return 96 * 1024 * 1024; // 96MB - Vega integrated graphics are decent
		}

		// Older GCN-based APUs
		if (rendererLower.Contains("radeon") && (rendererLower.Contains("r5") || rendererLower.Contains("r7")))
		{
			return 48 * 1024 * 1024; // 48MB - Older AMD APUs
		}

		// Default for unknown AMD APU
		return 64 * 1024 * 1024; // 64MB standard
	}

	/// <summary>
	/// Calculates recommended memory limit based on detected GPU memory and type.
	/// </summary>
	/// <param name="availableMemoryKB">Available memory in KB from GPU detection.</param>
	/// <param name="isIntegratedGpu">Whether this is an integrated GPU.</param>
	/// <returns>Recommended font memory limit in bytes.</returns>
	private static long CalculateRecommendedMemoryFromDetection(long availableMemoryKB, bool isIntegratedGpu)
	{
		// Convert KB to bytes
		long availableMemoryBytes = availableMemoryKB * 1024;

		// Use different percentages for integrated vs discrete GPUs
		float percentage = isIntegratedGpu ? 0.03f : CurrentConfig.MaxGpuMemoryPercentage; // 3% for integrated, configurable for discrete
		long recommendedFontMemory = (long)(availableMemoryBytes * percentage);

		// Apply bounds based on GPU type
		long minMemory = isIntegratedGpu ? 16 * 1024 * 1024 : MinAtlasMemoryBytes; // 16MB min for integrated
		long maxMemory = isIntegratedGpu ? 96 * 1024 * 1024 : DefaultMaxAtlasMemoryBytes * 2; // 96MB max for integrated

		return Math.Clamp(recommendedFontMemory, minMemory, maxMemory);
	}

	/// <summary>
	/// Determines the best fallback strategy based on memory constraints.
	/// </summary>
	/// <param name="estimate">Current memory estimate.</param>
	/// <returns>Recommended fallback strategy.</returns>
	public static FallbackStrategy DetermineFallbackStrategy(FontMemoryEstimate estimate)
	{
		if (!estimate.ExceedsLimits)
		{
			return FallbackStrategy.None;
		}

		// Calculate how much we're over the limit
		double overageRatio = (double)estimate.EstimatedBytes / CurrentConfig.MaxAtlasMemoryBytes;

		if (overageRatio > 4.0) // More than 4x over limit
		{
			return FallbackStrategy.MinimalFonts;
		}
		else if (overageRatio > 2.0) // More than 2x over limit
		{
			return FallbackStrategy.ReduceUnicodeRanges;
		}
		else if (overageRatio > 1.5) // More than 1.5x over limit
		{
			return FallbackStrategy.DisableEmojis;
		}
		else // Less than 1.5x over limit
		{
			return FallbackStrategy.ReduceFontSizes;
		}
	}

	/// <summary>
	/// Gets a reduced set of font sizes based on memory constraints.
	/// </summary>
	/// <param name="originalSizes">Original font sizes to load.</param>
	/// <param name="maxSizes">Maximum number of sizes to include.</param>
	/// <param name="preferredSize">Preferred size to always include (e.g., default font size).</param>
	/// <returns>Reduced array of font sizes.</returns>
	public static int[] GetReducedFontSizes(int[] originalSizes, int maxSizes, int preferredSize = 14)
	{
		ArgumentNullException.ThrowIfNull(originalSizes);

		if (originalSizes.Length <= maxSizes)
		{
			return originalSizes;
		}

		maxSizes = Math.Max(CurrentConfig.MinFontSizesToLoad, maxSizes);
		List<int> selectedSizes = [];

		// Always include the preferred size
		if (originalSizes.Contains(preferredSize))
		{
			selectedSizes.Add(preferredSize);
		}

		// Add other sizes, prioritizing medium sizes over very large or very small ones
		IEnumerable<int> remainingSizes = originalSizes.Where(s => s != preferredSize)
			.OrderBy(s => Math.Abs(s - preferredSize)) // Sort by distance from preferred size
			.Take(maxSizes - selectedSizes.Count);

		selectedSizes.AddRange(remainingSizes);

		return [.. selectedSizes.OrderBy(s => s)];
	}

	/// <summary>
	/// Gets reduced Unicode glyph ranges for memory-constrained scenarios.
	/// </summary>
	/// <param name="fontAtlasPtr">Font atlas for building ranges.</param>
	/// <returns>Pointer to reduced glyph ranges.</returns>
	public static unsafe uint* GetReducedUnicodeRanges(ImFontAtlasPtr fontAtlasPtr)
	{
		// Create a minimal set of ranges that still provides good functionality
		ImFontGlyphRangesBuilderPtr builder = new(ImGui.ImFontGlyphRangesBuilder());

		// Add default ranges (ASCII) - always needed
		builder.AddRanges(fontAtlasPtr.GetGlyphRangesDefault());

		// Add only the most essential extended ranges
		AddEssentialLatinExtended(builder);
		AddEssentialSymbols(builder);
		AddEssentialNerdFontRanges(builder);

		// Build and cache the ranges
		ImVector<uint> reducedRanges = new();
		builder.BuildRanges(ref reducedRanges);

		return reducedRanges.Data;
	}

	/// <summary>
	/// Logs memory usage information for debugging.
	/// </summary>
	/// <param name="estimate">Memory estimate to log.</param>
	/// <param name="strategy">Applied fallback strategy.</param>
	public static void LogMemoryUsage(FontMemoryEstimate estimate, FallbackStrategy strategy)
	{
		DebugLogger.Log($"FontMemoryGuard: Estimated font memory usage: {estimate.EstimatedBytes / (1024 * 1024)}MB");
		DebugLogger.Log($"FontMemoryGuard: Memory limit: {CurrentConfig.MaxAtlasMemoryBytes / (1024 * 1024)}MB");
		DebugLogger.Log($"FontMemoryGuard: Estimated glyph count: {estimate.EstimatedGlyphCount:N0}");

		if (estimate.ExceedsLimits)
		{
			DebugLogger.Log($"FontMemoryGuard: Memory limit exceeded, applying fallback strategy: {strategy}");
			if (estimate.ShouldDisableEmojis)
			{
				DebugLogger.Log("FontMemoryGuard: Disabling emoji fonts to reduce memory usage");
			}
			if (estimate.ShouldReduceUnicodeRanges)
			{
				DebugLogger.Log("FontMemoryGuard: Reducing Unicode ranges to reduce memory usage");
			}
			if (estimate.RecommendedMaxSizes > 0)
			{
				DebugLogger.Log($"FontMemoryGuard: Recommending maximum {estimate.RecommendedMaxSizes} font sizes");
			}
		}
		else
		{
			DebugLogger.Log("FontMemoryGuard: Memory usage within limits, no restrictions applied");
		}
	}

	#region Private Helper Methods

	private static int GetExtendedUnicodeGlyphCount() =>
		// Estimate glyph count for extended Unicode ranges
		// This is based on the ranges defined in FontHelper.AddSymbolRanges and AddNerdFontRanges
		2000; // Conservative estimate

	private static int GetEmojiGlyphCount() =>
		// Estimate glyph count for emoji ranges
		// This is based on the ranges defined in FontHelper.AddEmojiRanges
		3000; // Conservative estimate

	private static int CalculateRecommendedMaxSizes(long estimatedBytes, int fontCount, int baseGlyphCount, float scaleFactor)
	{
		if (estimatedBytes <= CurrentConfig.MaxAtlasMemoryBytes)
		{
			return int.MaxValue; // No limit needed
		}

		// Calculate how many sizes we can fit within the memory limit
		long bytesPerFontPerSize = (long)(baseGlyphCount * EstimatedBytesPerGlyph * scaleFactor * scaleFactor);
		long availableBytesPerFont = CurrentConfig.MaxAtlasMemoryBytes / fontCount;
		int maxSizesPerFont = (int)(availableBytesPerFont / bytesPerFontPerSize);

		return Math.Max(CurrentConfig.MinFontSizesToLoad, maxSizesPerFont);
	}

	private static void AddEssentialLatinExtended(ImFontGlyphRangesBuilderPtr builder)
	{
		// Add only the most commonly used Latin Extended characters
		// Latin Extended-A (U+0100–U+017F) - partial
		for (uint c = 0x00C0; c <= 0x00FF; c++) // Latin-1 Supplement (most common)
		{
			builder.AddChar(c);
		}
		for (uint c = 0x0100; c <= 0x017F; c += 2) // Every other character in Latin Extended-A
		{
			builder.AddChar(c);
		}
	}

	private static void AddEssentialSymbols(ImFontGlyphRangesBuilderPtr builder)
	{
		// Add only the most essential symbols
		(uint start, uint end)[] essentialRanges = [
			(0x2000, 0x206F), // General Punctuation
			(0x20A0, 0x20CF), // Currency Symbols
			(0x2190, 0x21FF), // Arrows
			(0x2500, 0x257F), // Box Drawing
		];

		foreach ((uint start, uint end) in essentialRanges)
		{
			for (uint c = start; c <= end; c += 2) // Every other character to reduce count
			{
				builder.AddChar(c);
			}
		}
	}

	private static void AddEssentialNerdFontRanges(ImFontGlyphRangesBuilderPtr builder)
	{
		// Add only the most commonly used Nerd Font icons
		(uint start, uint end)[] essentialNerdRanges = [
			(0xE0A0, 0xE0A2), // Powerline symbols
			(0xE0B0, 0xE0B3), // Powerline symbols
			(0xF000, 0xF0FF), // First 256 Font Awesome icons (most common)
		];

		foreach ((uint start, uint end) in essentialNerdRanges)
		{
			for (uint c = start; c <= end; c++)
			{
				builder.AddChar(c);
			}
		}
	}

	#endregion
}
