// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;
using System.Collections.Generic;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Color;
using ktsu.ImGui.Probes;

/// <summary>Options for <see cref="ImGuiWidgets.Toolbar"/>.</summary>
public sealed record class ToolbarOptions
{
	/// <summary>
	/// Gets the layout every button in the toolbar uses unless it sets its own. Defaults to
	/// <see cref="ToolbarButtonLayout.GlyphLeading"/>. A <see cref="ToolbarButtonLayout.GlyphAbove"/>
	/// toolbar is two lines tall, so its buttons can put their labels under their glyphs.
	/// </summary>
	public ToolbarButtonLayout Layout { get; init; } = ToolbarButtonLayout.GlyphLeading;

	/// <summary>Gets the strip width in pixels. Null or non-positive fills <c>ImGui.GetContentRegionAvail().X</c>.</summary>
	public float? Width { get; init; }

	/// <summary>Gets the padding between the strip edge and its buttons. Null uses <c>style.FramePadding</c>.</summary>
	public Vector2? Padding { get; init; }

	/// <summary>Gets the horizontal gap between adjacent buttons. Null uses <c>style.ItemSpacing.X / 2</c>.</summary>
	public float? Spacing { get; init; }

	/// <summary>Gets the strip background. Null uses <see cref="ImGuiCol.MenuBarBg"/>.</summary>
	public ImColor? Background { get; init; }
}

/// <summary>
/// The toolbar currently being drawn: what its buttons and separators need to know to lay
/// themselves out inside it.
/// </summary>
internal sealed class ToolbarContext(ToolbarButtonLayout layout, float rowHeight, float spacing, float paddingY)
{
	// The toolbars currently open, innermost last. ImGui is itself an ambient-context API and a
	// strip's contents are lexical, so the buttons find their toolbar here rather than being handed
	// it. Thread-static because a context belongs to one thread.
	[ThreadStatic]
	private static Stack<ToolbarContext>? open;

	private int itemCount;

	public static ToolbarContext? Current => open is { Count: > 0 } stack ? stack.Peek() : null;

	public ToolbarButtonLayout Layout { get; } = layout;

	public float RowHeight { get; } = rowHeight;

	public float PaddingY { get; } = paddingY;

	public static void Push(ToolbarContext context) => (open ??= new Stack<ToolbarContext>()).Push(context);

	public static void Pop(ToolbarContext context)
	{
		if (open is { Count: > 0 } stack && ReferenceEquals(stack.Peek(), context))
		{
			stack.Pop();
		}
	}

	/// <summary>Places the next item on the strip's row, after the one before it.</summary>
	public void BeginItem()
	{
		if (itemCount > 0)
		{
			ImGui.SameLine(0.0f, spacing);
		}

		itemCount++;
	}
}

/// <summary>
/// Provides custom ImGui widgets.
/// </summary>
public static partial class ImGuiWidgets
{
	/// <summary>
	/// Opens a toolbar strip. Buttons and separators submitted before the scope is disposed are laid
	/// out left to right inside it. Dispose it in the same frame, normally with <c>using</c>: the scope
	/// pushes an ImGui id, and leaving it undisposed unbalances the id stack.
	/// </summary>
	/// <param name="id">The ImGui id and probe name of the strip. It is pushed as a <see cref="ScopedId"/> for the scope's lifetime.</param>
	/// <param name="options">Optional settings.</param>
	/// <returns>The scope that closes the strip.</returns>
	public static ToolbarScope Toolbar(string id, ToolbarOptions? options = null) => new(id, options ?? new ToolbarOptions());

	/// <summary>Draws a vertical divider between button groups. It does nothing outside a toolbar.</summary>
	public static void ToolbarSeparator()
	{
		ToolbarContext? toolbar = ToolbarContext.Current;
		if (toolbar is null)
		{
			return;
		}

		toolbar.BeginItem();

		ImGuiStylePtr style = ImGui.GetStyle();
		float width = ToolbarGeometry.SeparatorWidth(style.ItemInnerSpacing);
		Vector2 origin = ImGui.GetCursorScreenPos();
		ImGui.Dummy(new Vector2(width, toolbar.RowHeight));

		float x = MathF.Floor(origin.X + (width * 0.5f)) + 0.5f;
		float inset = MathF.Min(toolbar.PaddingY, toolbar.RowHeight * 0.25f);
		ImGui.GetWindowDrawList().AddLine(
			new Vector2(x, origin.Y + inset),
			new Vector2(x, origin.Y + toolbar.RowHeight - inset),
			ImGui.GetColorU32(ImGuiCol.Separator),
			1.0f);
	}

	/// <summary>Closes a toolbar strip. Disposing it more than once has no further effect.</summary>
	public sealed class ToolbarScope : IDisposable
	{
		private readonly string id;
		private readonly Vector2 origin;
		private readonly Vector2 size;
		private readonly ToolbarContext context;
		private readonly ScopedId scopedId;
		private bool disposed;

		internal ToolbarScope(string id, ToolbarOptions options)
		{
			Ensure.NotNull(id);
			this.id = id;

			ImGuiStylePtr style = ImGui.GetStyle();
			Vector2 padding = options.Padding ?? style.FramePadding;
			float spacing = options.Spacing ?? (style.ItemSpacing.X * 0.5f);
			float rowHeight = ToolbarGeometry.RowHeight(options.Layout, ImGui.GetTextLineHeight(),
				style.FramePadding, style.ItemInnerSpacing, ImGui.GetFrameHeight());

			float width = options.Width is float requested && requested > 0.0f ? requested : ImGui.GetContentRegionAvail().X;
			if (width <= 0.0f)
			{
				width = ImGui.GetFrameHeight();
			}

			origin = ImGui.GetCursorScreenPos();
			size = new Vector2(width, rowHeight + (2.0f * padding.Y));
			Vector2 max = origin + size;

			// The background goes down first, so it sits behind the buttons without splitting channels.
			uint background = options.Background is ImColor custom ? custom.ToImGuiU32() : ImGui.GetColorU32(ImGuiCol.MenuBarBg);
			ImGui.GetWindowDrawList().AddRectFilled(origin, max, background, style.FrameRounding);

			scopedId = new ScopedId(id);
			ImGui.PushClipRect(origin, max, true);
			ImGui.SetCursorScreenPos(origin + padding);

			context = new ToolbarContext(options.Layout, rowHeight, spacing, padding.Y);
			ToolbarContext.Push(context);
		}

		/// <summary>Closes the strip and reserves its space as one item.</summary>
		public void Dispose()
		{
			if (disposed)
			{
				return;
			}

			disposed = true;

			ToolbarContext.Pop(context);
			ImGui.PopClipRect();
			scopedId.Dispose();

			ImGui.SetCursorScreenPos(origin);
			ImGui.Dummy(size);
			ImGuiProbes.MarkRegion(id, origin, ImGui.GetItemRectMax());
		}
	}
}
