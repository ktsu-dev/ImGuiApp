// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using Hexa.NET.ImGui;

using HexaSplitter = Hexa.NET.ImGui.Widgets.ImGuiSplitter;

public static partial class ImGuiWidgets
{
	/// <summary>
	/// Resolves the splitter grab metrics, substituting style-derived values for the zero sentinel.
	/// </summary>
	/// <param name="thickness">Requested thickness, or zero to derive from the style.</param>
	/// <param name="tolerance">Requested grab tolerance, or zero to derive from the style.</param>
	/// <param name="grabMinSize">The style's grab minimum size, in pixels.</param>
	/// <returns>The resolved thickness and tolerance.</returns>
	internal static (float Thickness, float Tolerance) ResolveSplitterMetrics(float thickness, float tolerance, float grabMinSize)
	{
		float resolvedThickness = thickness == 0f ? grabMinSize * 0.25f : thickness;
		float resolvedTolerance = tolerance == 0f ? grabMinSize : tolerance;
		return (resolvedThickness, resolvedTolerance);
	}

	/// <summary>
	/// Draws a draggable vertical splitter that adjusts <paramref name="width"/>.
	/// </summary>
	/// <param name="id">Unique identifier for the splitter.</param>
	/// <param name="width">The width being adjusted, updated in place while dragging.</param>
	/// <param name="minWidth">Smallest width the splitter will allow.</param>
	/// <param name="maxWidth">Largest width the splitter will allow.</param>
	/// <param name="height">Height of the splitter bar, or zero to fill the available region.</param>
	/// <param name="thickness">Thickness of the bar in pixels, or zero to derive from the style.</param>
	/// <param name="tolerance">Grab tolerance in pixels, or zero to derive from the style.</param>
	/// <returns><see langword="true"/> if the width changed this frame.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="id"/> is <see langword="null"/>.</exception>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="minWidth"/> exceeds <paramref name="maxWidth"/>.</exception>
	public static bool VerticalSplitter(string id, ref float width, float minWidth = float.MinValue, float maxWidth = float.MaxValue, float height = 0f, float thickness = 0f, float tolerance = 0f)
	{
		Ensure.NotNull(id);
		if (minWidth > maxWidth)
		{
			throw new ArgumentOutOfRangeException(nameof(minWidth), minWidth, "minWidth must not exceed maxWidth.");
		}

		(float resolvedThickness, float resolvedTolerance) = ResolveSplitterMetrics(thickness, tolerance, ImGui.GetStyle().GrabMinSize);
		return HexaSplitter.VerticalSplitter(id, ref width, minWidth, maxWidth, height, resolvedThickness, resolvedTolerance);
	}

	/// <summary>
	/// Draws a draggable horizontal splitter that adjusts <paramref name="height"/>.
	/// </summary>
	/// <param name="id">Unique identifier for the splitter.</param>
	/// <param name="height">The height being adjusted, updated in place while dragging.</param>
	/// <param name="minHeight">Smallest height the splitter will allow.</param>
	/// <param name="maxHeight">Largest height the splitter will allow.</param>
	/// <param name="width">Width of the splitter bar, or zero to fill the available region.</param>
	/// <param name="thickness">Thickness of the bar in pixels, or zero to derive from the style.</param>
	/// <param name="tolerance">Grab tolerance in pixels, or zero to derive from the style.</param>
	/// <returns><see langword="true"/> if the height changed this frame.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="id"/> is <see langword="null"/>.</exception>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="minHeight"/> exceeds <paramref name="maxHeight"/>.</exception>
	public static bool HorizontalSplitter(string id, ref float height, float minHeight = float.MinValue, float maxHeight = float.MaxValue, float width = 0f, float thickness = 0f, float tolerance = 0f)
	{
		Ensure.NotNull(id);
		if (minHeight > maxHeight)
		{
			throw new ArgumentOutOfRangeException(nameof(minHeight), minHeight, "minHeight must not exceed maxHeight.");
		}

		(float resolvedThickness, float resolvedTolerance) = ResolveSplitterMetrics(thickness, tolerance, ImGui.GetStyle().GrabMinSize);
		return HexaSplitter.HorizontalSplitter(id, ref height, minHeight, maxHeight, width, resolvedThickness, resolvedTolerance);
	}
}
