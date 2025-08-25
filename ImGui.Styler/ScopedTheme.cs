// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGuiStyler;

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Numerics;
using Hexa.NET.ImGui;
using ktsu.ScopedAction;
using ktsu.ThemeProvider;

/// <summary>
/// Represents a scoped action that applies a complete semantic theme to ImGui elements.
/// This provides semantic theme-based styling that automatically reverts when disposed.
/// </summary>
public class ScopedTheme : ScopedAction
{
	/// <summary>
	/// Cache for storing computed color mappings per theme to avoid repeated computation.
	/// </summary>
	private static readonly ConcurrentDictionary<string, ImmutableDictionary<ImGuiCol, Vector4>> colorMappingCache = new();

	/// <summary>
	/// Initializes a new instance of the <see cref="ScopedTheme"/> class.
	/// </summary>
	/// <param name="theme">The semantic theme to apply.</param>
	public ScopedTheme(ISemanticTheme theme)
	{
		ArgumentNullException.ThrowIfNull(theme);

		// Create a cache key based on the theme type name (assumes one theme per type)
		string cacheKey = theme.GetType().FullName ?? theme.GetType().Name;

		// Get the color mapping from cache or compute it
		ImmutableDictionary<ImGuiCol, Vector4> colorMapping = colorMappingCache.GetOrAdd(cacheKey, _ => Theme.GetColorMapping(theme));

		int numStyles = 0;

		// Apply all mapped colors using PushStyleAndCount
		foreach ((ImGuiCol imguiCol, Vector4 color) in colorMapping)
		{
			PushStyleAndCount(imguiCol, color, ref numStyles);
		}

		OnClose = () => ImGui.PopStyleColor(numStyles);
	}

	/// <summary>
	/// Clears the color mapping cache. This can be useful if themes have been modified
	/// or to free memory if many different themes have been used.
	/// </summary>
	public static void ClearCache() => colorMappingCache.Clear();

	private static void PushStyleAndCount(ImGuiCol style, Vector4 color, ref int numStyles)
	{
		ImGui.PushStyleColor(style, color);
		++numStyles;
	}
}
