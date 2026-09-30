// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Color;
using ktsu.ImGui.Probes;

/// <summary>Options for <see cref="ImGuiWidgets.ToolbarButton"/> and <see cref="ImGuiWidgets.ToolbarToggleButton"/>.</summary>
public sealed record class ToolbarButtonOptions
{
	/// <summary>
	/// Gets the layout. Null means the enclosing toolbar's <see cref="ToolbarOptions.Layout"/>, or
	/// <see cref="ToolbarButtonLayout.GlyphLeading"/> outside a toolbar.
	/// </summary>
	public ToolbarButtonLayout? Layout { get; init; }

	/// <summary>Gets a value indicating whether the button can be clicked. Defaults to true.</summary>
	public bool Enabled { get; init; } = true;

	/// <summary>
	/// Gets the hover tooltip. Null shows the visible label for a <see cref="ToolbarButtonLayout.GlyphOnly"/>
	/// button and nothing otherwise.
	/// </summary>
	public string? Tooltip { get; init; }

	/// <summary>Gets a shortcut hint such as "Ctrl+S", shown on the tooltip's second line. Display only; nothing is bound.</summary>
	public string? Shortcut { get; init; }

	/// <summary>Gets the glyph colour. Null uses <see cref="ImGuiCol.Text"/>, or <see cref="ImGuiCol.TextDisabled"/> when disabled.</summary>
	public ImColor? GlyphColor { get; init; }

	/// <summary>Gets the minimum button width in pixels. Content is centred in any extra width. Defaults to 0.</summary>
	public float MinWidth { get; init; }

	/// <summary>
	/// Gets a value indicating whether the idle frame is left unfilled, so the frame only appears on
	/// hover, press or selection. Defaults to false.
	/// </summary>
	public bool Flat { get; init; }
}

/// <summary>
/// Provides custom ImGui widgets.
/// </summary>
public static partial class ImGuiWidgets
{
	/// <summary>Draws a button holding a glyph and a label inside one frame.</summary>
	/// <param name="label">The label and ImGui id. Text after <c>##</c> is not drawn.</param>
	/// <param name="glyph">The glyph string, drawn in the current font. Empty means no glyph.</param>
	/// <param name="options">Optional settings.</param>
	/// <returns>True on the frame the button is clicked.</returns>
	public static bool ToolbarButton(string label, string glyph, ToolbarButtonOptions? options = null) =>
		ToolbarButtonImpl.Draw(label, glyph, options, selected: false);

	/// <summary>
	/// Draws a toolbar button that toggles <paramref name="value"/> when clicked, and draws as
	/// selected while it is true.
	/// </summary>
	/// <param name="label">The label and ImGui id. Text after <c>##</c> is not drawn.</param>
	/// <param name="glyph">The glyph string, drawn in the current font. Empty means no glyph.</param>
	/// <param name="value">The toggled value.</param>
	/// <param name="options">Optional settings.</param>
	/// <returns>True on the frame <paramref name="value"/> changed.</returns>
	public static bool ToolbarToggleButton(string label, string glyph, ref bool value, ToolbarButtonOptions? options = null)
	{
		if (!ToolbarButtonImpl.Draw(label, glyph, options, selected: value))
		{
			return false;
		}

		value = !value;
		return true;
	}

	internal static class ToolbarButtonImpl
	{
		private static readonly ToolbarButtonOptions Defaults = new();

		public static bool Draw(string label, string glyph, ToolbarButtonOptions? options, bool selected)
		{
			Ensure.NotNull(label);
			glyph ??= string.Empty;
			options ??= Defaults;

			string visible = VisibleLabel(label);
			ToolbarContext? toolbar = ToolbarContext.Current;

			ToolbarButtonLayout requested = options.Layout ?? toolbar?.Layout ?? ToolbarButtonLayout.GlyphLeading;
			if (toolbar is not null)
			{
				requested = ToolbarGeometry.ResolveInToolbar(requested, toolbar.Layout);
				toolbar.BeginItem();
			}

			ToolbarButtonLayout layout = ToolbarGeometry.Resolve(requested, glyph.Length > 0, visible.Length > 0);
			bool drawsGlyph = layout is ToolbarButtonLayout.GlyphLeading or ToolbarButtonLayout.GlyphAbove or ToolbarButtonLayout.GlyphOnly;
			bool drawsLabel = layout is ToolbarButtonLayout.GlyphLeading or ToolbarButtonLayout.GlyphAbove or ToolbarButtonLayout.LabelOnly;

			ImGuiStylePtr style = ImGui.GetStyle();
			Vector2 glyphSize = drawsGlyph ? ImGui.CalcTextSize(glyph) : Vector2.Zero;
			Vector2 labelSize = drawsLabel ? ImGui.CalcTextSize(visible) : Vector2.Zero;

			ToolbarButtonMetrics metrics = ToolbarGeometry.Measure(glyphSize, labelSize, layout,
				style.FramePadding, style.ItemInnerSpacing, ImGui.GetFrameHeight(), options.MinWidth, toolbar?.RowHeight ?? 0.0f);

			bool enabled = options.Enabled;
			Vector2 origin = ImGui.GetCursorScreenPos();

			ImGui.BeginDisabled(!enabled);
			bool clicked = ImGui.InvisibleButton(label, metrics.Size);
			ImGuiProbes.MarkItem(label);
			bool hovered = ImGui.IsItemHovered();
			bool held = ImGui.IsItemActive();

			DrawFrame(style, origin, metrics.Size, hovered, held, selected, options.Flat);

			ImDrawListPtr drawList = ImGui.GetWindowDrawList();
			uint textColor = ImGui.GetColorU32(enabled ? ImGuiCol.Text : ImGuiCol.TextDisabled);
			if (drawsGlyph)
			{
				uint glyphColor = enabled && options.GlyphColor is ImColor custom ? custom.ToImGuiU32() : textColor;
				drawList.AddText(origin + metrics.GlyphOffset, glyphColor, glyph);
			}

			if (drawsLabel)
			{
				drawList.AddText(origin + metrics.LabelOffset, textColor, visible);
			}

			// The tooltip is opened after the disabled block ends, so it is not dimmed with the button.
			// IsItemHovered still answers for the button: ending a disabled block submits no item.
			ImGui.EndDisabled();
			DrawTooltip(label, visible, layout, options);

			return clicked && enabled;
		}

		private static void DrawFrame(ImGuiStylePtr style, Vector2 origin, Vector2 size, bool hovered, bool held, bool selected, bool flat)
		{
			Vector2 max = origin + size;
			float rounding = style.FrameRounding;
			ImDrawListPtr drawList = ImGui.GetWindowDrawList();

			ImGuiCol? fill = (held && hovered) ? ImGuiCol.ButtonActive
				: hovered ? ImGuiCol.ButtonHovered
				: selected ? ImGuiCol.ButtonActive
				: flat ? null
				: ImGuiCol.Button;

			if (fill is ImGuiCol fillColor)
			{
				drawList.AddRectFilled(origin, max, ImGui.GetColorU32(fillColor), rounding);
			}

			// A selected button keeps a visible outline whatever the style's border size, so "on" still
			// reads while it is hovered and its fill matches every other hovered button.
			if (selected)
			{
				drawList.AddRect(origin, max, ImGui.GetColorU32(ImGuiCol.Border), rounding, ImDrawFlags.None, 1.0f);
			}
			else if (style.FrameBorderSize > 0.0f)
			{
				drawList.AddRect(origin, max, ImGui.GetColorU32(ImGuiCol.Border), rounding, ImDrawFlags.None, style.FrameBorderSize);
			}
		}

		private static void DrawTooltip(string label, string visible, ToolbarButtonLayout layout, ToolbarButtonOptions options)
		{
			string? text = options.Tooltip ?? (layout == ToolbarButtonLayout.GlyphOnly && visible.Length > 0 ? visible : null);
			if (text is null || !ImGui.IsItemHovered(ImGuiHoveredFlags.ForTooltip | ImGuiHoveredFlags.AllowWhenDisabled))
			{
				return;
			}

			if (!ImGui.BeginTooltip())
			{
				return;
			}

			ImGui.TextUnformatted(text);
			ImGuiProbes.MarkItem($"{label}/tooltip");
			if (!string.IsNullOrEmpty(options.Shortcut))
			{
				ImGui.TextDisabled(options.Shortcut);
			}

			ImGui.EndTooltip();
		}
	}
}
