// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;
using System.Collections.Generic;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Probes;

/// <summary>A button on an app bar.</summary>
/// <param name="Icon">The glyph drawn on the button.</param>
/// <param name="Name">What the button does, shown as its tooltip and used as its probe name.</param>
public readonly record struct AppBarAction(string Icon, string Name);

/// <summary>What an app bar holds besides its title.</summary>
public sealed record AppBarOptions
{
	/// <summary>Gets the button before the title, such as back or a menu, or null for none.</summary>
	public AppBarAction? Leading { get; init; }

	/// <summary>Gets the buttons after the title, at the trailing end of the bar.</summary>
	public IReadOnlyList<AppBarAction> Actions { get; init; } = [];

	/// <summary>
	/// Gets a value indicating whether the bar slides away as the content under it scrolls down and comes
	/// back as it scrolls up. The content's scroll position is passed to each call.
	/// </summary>
	public bool CollapseOnScroll { get; init; }

	/// <summary>Gets the bar's height, in pixels at 100% interface scale.</summary>
	public float Height { get; init; } = 56f;
}

/// <summary>Which of an app bar's buttons was clicked.</summary>
/// <param name="LeadingClicked">Whether the leading button was clicked.</param>
/// <param name="ActionClicked">The index of the trailing button clicked, or -1.</param>
public readonly record struct AppBarResult(bool LeadingClicked, int ActionClicked)
{
	/// <summary>Gets the result of a frame on which nothing was clicked.</summary>
	public static AppBarResult None { get; } = new(false, -1);

	/// <summary>Gets a value indicating whether any button was clicked.</summary>
	public bool AnyClicked => LeadingClicked || ActionClicked >= 0;
}

/// <summary>
/// Provides custom ImGui widgets.
/// </summary>
public static partial class ImGuiWidgets
{
	/// <summary>
	/// A bar across the top of the window with a title, an optional leading button such as back or a
	/// menu, and buttons at the trailing end, as a phone app's top bar is.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The bar takes the full width at the cursor. With <see cref="AppBarOptions.CollapseOnScroll"/> it
	/// slides away as <paramref name="scrollOffset"/> grows and comes back as it shrinks, and takes only
	/// the height still showing, so the content below moves up into the room; pass the scroll position
	/// of the region under the bar, read on the previous frame.
	/// </para>
	/// <para>
	/// The bar is probe-marked under <paramref name="id"/>, its leading button under <c>id/name</c> and
	/// each trailing button under <c>id/name</c>, by the <see cref="AppBarAction.Name"/> given.
	/// </para>
	/// </remarks>
	/// <param name="id">A unique id for the bar.</param>
	/// <param name="title">The title.</param>
	/// <param name="options">The bar's buttons and behaviour; null for a bar with only a title.</param>
	/// <param name="scrollOffset">How far the content under the bar is scrolled down, in pixels.</param>
	/// <returns>Which button was clicked on this frame.</returns>
	public static AppBarResult AppBar(string id, string title, AppBarOptions? options = null, float scrollOffset = 0f) =>
		AppBarImpl.Draw(id, title, options ?? new AppBarOptions(), scrollOffset);

	internal static class AppBarImpl
	{
		private sealed class BarEntry
		{
			public AppBarState State { get; } = new();

			public int LastFrame { get; set; } = int.MinValue;
		}

		private static readonly Dictionary<uint, BarEntry> Entries = [];

		internal static AppBarResult Draw(string id, string title, AppBarOptions options, float scrollOffset)
		{
			Ensure.NotNull(id);
			Ensure.NotNull(title);

			uint key = ImGui.GetID(id);
			int frame = ImGui.GetFrameCount();
			if (!Entries.TryGetValue(key, out BarEntry? entry) || frame - entry.LastFrame != 1)
			{
				entry = new BarEntry();
				Entries[key] = entry;
			}

			entry.LastFrame = frame;

			float height = InterfaceScale.Pixels(options.Height);
			float showing = options.CollapseOnScroll
				? entry.State.Track(scrollOffset, height, ImGui.GetIO().DeltaTime)
				: height;

			Vector2 origin = ImGui.GetCursorScreenPos();
			float width = MathF.Max(ImGui.GetContentRegionAvail().X, 1f);
			Vector2 max = origin + new Vector2(width, showing);
			ImGuiProbes.MarkRegion(id, origin, max);

			AppBarResult result = AppBarResult.None;
			if (showing >= 1f)
			{
				ImGui.GetWindowDrawList().AddRectFilled(origin, max, ImGui.GetColorU32(ImGuiCol.MenuBarBg));
				ImGui.PushClipRect(origin, max, true);
				ImGui.PushID(id);
				result = DrawContent(id, title, options, origin - new Vector2(0f, height - showing), width, height);
				ImGui.PopID();
				ImGui.PopClipRect();
			}

			ImGui.SetCursorScreenPos(origin);
			ImGui.Dummy(new Vector2(width, showing));
			return result;
		}

		private static AppBarResult DrawContent(string id, string title, AppBarOptions options, Vector2 origin, float width, float height)
		{
			float button = MathF.Min(height, ImGui.GetFrameHeight() + InterfaceScale.Pixels(12f));
			float inset = InterfaceScale.Pixels(4f);
			float buttonTop = origin.Y + ((height - button) * 0.5f);
			bool leadingClicked = false;
			float titleLeft = origin.X + InterfaceScale.Pixels(16f);

			ImGui.PushStyleColor(ImGuiCol.Button, 0u);
			ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, button * 0.5f);
			if (options.Leading is { } leading)
			{
				leadingClicked = DrawButton(id, leading, new Vector2(origin.X + inset, buttonTop), button);
				titleLeft = origin.X + inset + button + InterfaceScale.Pixels(8f);
			}

			int actionClicked = -1;
			float right = origin.X + width - inset;
			for (int i = options.Actions.Count - 1; i >= 0; i--)
			{
				right -= button;
				if (DrawButton(id, options.Actions[i], new Vector2(right, buttonTop), button))
				{
					actionClicked = i;
				}
			}

			ImGui.PopStyleVar();
			ImGui.PopStyleColor();

			Vector2 titleSize = ImGui.CalcTextSize(title);
			Vector2 titleAt = new(titleLeft, origin.Y + ((height - titleSize.Y) * 0.5f));
			ImDrawListPtr drawList = ImGui.GetWindowDrawList();
			drawList.PushClipRect(new Vector2(titleLeft, origin.Y), new Vector2(MathF.Max(titleLeft, right - InterfaceScale.Pixels(8f)), origin.Y + height), true);
			drawList.AddText(titleAt, ImGui.GetColorU32(ImGuiCol.Text), title);
			drawList.PopClipRect();

			return new AppBarResult(leadingClicked, actionClicked);
		}

		private static bool DrawButton(string id, AppBarAction action, Vector2 position, float size)
		{
			ImGui.SetCursorScreenPos(position);
			bool clicked = ImGui.Button($"{action.Icon}##{action.Name}", new Vector2(size, size));
			ImGuiProbes.MarkItem(id, action.Name);
			ImGui.SetItemTooltip(action.Name);
			return clicked;
		}
	}
}
