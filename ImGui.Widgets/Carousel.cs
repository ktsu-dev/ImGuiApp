// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;
using System.Collections.Generic;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Probes;
using ktsu.ImGui.Widgets.Gestures;

/// <summary>
/// Provides custom ImGui widgets.
/// </summary>
public static partial class ImGuiWidgets
{
	/// <summary>
	/// Draws a paged horizontal scroller: one page at a time, dragged or flicked sideways to the next,
	/// with a row of page dots underneath.
	/// </summary>
	/// <remarks>
	/// <para>
	/// A released drag settles on the nearest page to where its momentum would carry it, so a quick
	/// flick turns the page even when the drag was short, and never moves more than one page from where
	/// the drag began. Dragging past the first or last page pulls a little and springs back. Clicking a
	/// dot, or changing <paramref name="page"/> from outside, slides to that page.
	/// </para>
	/// <para>
	/// The pages are drawn by <paramref name="drawPage"/>, which is called only for the one or two pages
	/// in view, with the cursor at the page's top left corner and the page's size. Controls inside a
	/// page work as usual; a drag that starts on one moves the control rather than the carousel. The
	/// carousel is probe-marked under <paramref name="id"/>, and its dots under <c>id/dots</c>.
	/// </para>
	/// </remarks>
	/// <param name="id">A unique id for the carousel.</param>
	/// <param name="page">The page shown. Updated when the user turns the page.</param>
	/// <param name="pageCount">How many pages there are.</param>
	/// <param name="size">The size of one page, which is also the size of the carousel above its dots.</param>
	/// <param name="drawPage">Draws one page, given its index and size.</param>
	/// <param name="options">How the carousel behaves, or null for the defaults.</param>
	/// <returns><see langword="true"/> on the frame the user turned to another page; otherwise <see langword="false"/>.</returns>
	public static bool Carousel(string id, ref int page, int pageCount, Vector2 size, Action<int, Vector2> drawPage, CarouselOptions? options = null) =>
		CarouselImpl.Draw(id, ref page, pageCount, size, drawPage, options ?? new CarouselOptions());

	internal static class CarouselImpl
	{
		private sealed class CarouselEntry(int page)
		{
			public CarouselState State { get; } = new(page);

			public GestureMachine Gestures { get; } = new();

			public int LastFrame { get; set; } = int.MinValue;
		}

		private static readonly Dictionary<uint, CarouselEntry> Entries = [];

		internal static bool Draw(string id, ref int page, int pageCount, Vector2 size, Action<int, Vector2> drawPage, CarouselOptions options)
		{
			Ensure.NotNull(id);
			Ensure.NotNull(drawPage);

			page = ClampPage(page, pageCount);
			uint key = ImGui.GetID(id);
			int frame = ImGui.GetFrameCount();

			// A carousel not drawn last frame starts on its page at rest, rather than finishing a slide
			// the user never saw, or one left by an earlier ImGui context.
			if (!Entries.TryGetValue(key, out CarouselEntry? entry) || frame - entry.LastFrame != 1)
			{
				entry = new CarouselEntry(page);
				Entries[key] = entry;
			}

			entry.LastFrame = frame;
			CarouselState state = entry.State;

			if (!state.IsDragging && page != state.TargetPage)
			{
				state.GoTo(page, pageCount);
			}

			state.Update(ImGui.GetIO().DeltaTime);

			Vector2 origin = ImGui.GetCursorScreenPos();
			size = Vector2.Max(size, Vector2.One);

			ImGui.PushID(id);
			ImGui.SetNextItemAllowOverlap();
			ImGui.InvisibleButton("##pages", size);
			ImGuiProbes.MarkItem(id);
			bool activated = ImGui.IsItemActivated();
			GestureResult gesture = entry.Gestures.Update(ImGui.IsItemActive(), ImGui.GetMousePos(), ImGui.GetIO().DeltaTime);

			bool changed = false;
			if (activated)
			{
				state.BeginDrag();
			}

			if ((gesture.Gestures & GestureFlags.Pan) != 0)
			{
				state.Drag(gesture.Delta.X, size.X, pageCount);
			}

			if (state.IsDragging && !gesture.IsPressed)
			{
				int settled = state.Release((gesture.Gestures & GestureFlags.PanEnd) != 0 ? gesture.Velocity.X : 0f, size.X, pageCount);
				changed = settled != page;
				page = settled;
			}

			DrawPages(origin, size, pageCount, state.Position, drawPage);

			if (options.ShowIndicator && pageCount > 1)
			{
				ImGui.SetCursorScreenPos(new Vector2(origin.X, origin.Y + size.Y));
				changed |= DrawIndicator(id, ref page, pageCount, size.X, state);
			}

			ImGui.PopID();
			return changed;
		}

		private static void DrawPages(Vector2 origin, Vector2 size, int pageCount, float position, Action<int, Vector2> drawPage)
		{
			ImGui.PushClipRect(origin, origin + size, true);

			int first = Math.Max(0, (int)MathF.Floor(position));
			int last = Math.Min(pageCount - 1, (int)MathF.Ceiling(position));
			for (int i = first; i <= last; i++)
			{
				Vector2 pageOrigin = new(origin.X + ((i - position) * size.X), origin.Y);
				ImGui.SetCursorScreenPos(pageOrigin);
				ImGui.PushID(i);
				ImGui.BeginGroup();
				drawPage(i, size);
				ImGui.EndGroup();
				ImGui.PopID();
			}

			ImGui.PopClipRect();

			ImGui.SetCursorScreenPos(origin);
			ImGui.Dummy(size);
		}

		private static bool DrawIndicator(string id, ref int page, int pageCount, float width, CarouselState state)
		{
			// The dots sit centred under the pages. Their width is measured from a draw that is thrown
			// away, which PageIndicator allows because it is stateless.
			float spacing = ImGui.GetStyle().ItemSpacing.Y;
			Vector2 rowStart = ImGui.GetCursorScreenPos() + new Vector2(0f, spacing);
			float dotsWidth = IndicatorWidth(pageCount);
			ImGui.SetCursorScreenPos(new Vector2(rowStart.X + MathF.Max(0f, (width - dotsWidth) * 0.5f), rowStart.Y));

			Vector2 dotsMin = ImGui.GetCursorScreenPos();
			int clicked = PageIndicator("##dots", state.TargetPage, pageCount, interactive: true);
			ImGuiProbes.MarkRegion($"{id}/dots", dotsMin, ImGui.GetItemRectMax());

			if (clicked == state.TargetPage)
			{
				return false;
			}

			state.GoTo(clicked, pageCount);
			page = state.TargetPage;
			return true;
		}

		// Mirrors PageIndicator's own layout arithmetic, so the dots can be centred before they are drawn.
		private static float IndicatorWidth(int pageCount)
		{
			float radius = ImGui.GetTextLineHeight() * 0.4f * 0.5f;
			float activeRadius = radius * 1.35f;
			float spacing = ImGui.GetStyle().ItemInnerSpacing.X + (radius * 2.0f);
			float slot = (activeRadius * 2.0f) + spacing;
			return (slot * pageCount) - spacing;
		}
	}
}
