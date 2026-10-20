// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Probes;
using ktsu.ImGui.Styler;

public static partial class ImGuiWidgets
{
	/// <summary>Options for <see cref="LogView"/>. Immutable; reuse one instance.</summary>
	public sealed class LogViewOptions
	{
		/// <summary>Gets a value indicating whether the Category column is shown. Defaults to <see langword="true"/>.</summary>
		public bool ShowCategory { get; init; } = true;

		/// <summary>
		/// Gets a value indicating whether the toolbar (filter, level toggles, collapse, follow and count) is
		/// drawn. Defaults to <see langword="true"/>.
		/// </summary>
		public bool ShowToolbar { get; init; } = true;

		/// <summary>Gets the outer size of the list. Zero on an axis fills the available space.</summary>
		public Vector2 Size { get; init; }

		/// <summary>Gets a callback that receives copied text instead of the clipboard. Null copies with <see cref="ImGui.SetClipboardText(string)"/>.</summary>
		public Action<string>? OnCopy { get; init; }
	}

	/// <summary>
	/// Draws a filterable, virtualized log list with level toggles, repeat collapsing, auto-follow, selection
	/// and copy.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Rows go through <see cref="VirtualTable(string, int, ReadOnlySpan{VirtualTableColumn}, Action{int}, VirtualTableOptions?)"/>,
	/// so a log of a hundred thousand lines costs only the rows on screen, and the filter is the same
	/// <see cref="SearchBox(ref SearchBoxOptions, ref string)"/> used elsewhere, matched against each
	/// entry's message.
	/// </para>
	/// <para>
	/// While the list is hovered or focused, Ctrl+C copies the selection, Ctrl+A selects every visible row and
	/// Escape clears the selection; a right click opens Copy, Copy all visible and Clear selection.
	/// </para>
	/// <para>
	/// The widget is marked under <paramref name="label"/>, the filter under <c>label/filter</c>, each
	/// toolbar checkbox under its own label (<c>label/Warn</c>, <c>label/Follow</c>) and each drawn row under
	/// <c>label/rows/[i]</c>, where <c>i</c> is the visible row.
	/// </para>
	/// </remarks>
	/// <param name="label">ImGui id and probe scope; not drawn.</param>
	/// <param name="entries">The log, owned and appended to by the caller, oldest first. Never copied.</param>
	/// <param name="state">Caller-owned view state; one per view.</param>
	/// <param name="options">Null uses the defaults.</param>
	/// <returns><see langword="true"/> on a frame where a click, a keyboard shortcut or a context-menu item changed the selection.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="label"/>, <paramref name="entries"/> or <paramref name="state"/> is <see langword="null"/>.</exception>
	public static bool LogView(string label, IReadOnlyList<LogEntry> entries, LogViewState state, LogViewOptions? options = null)
	{
		Ensure.NotNull(label);
		Ensure.NotNull(entries);
		Ensure.NotNull(state);

		return LogViewImpl.Draw(label, entries, state, options ?? DefaultLogViewOptions);
	}

	private static readonly LogViewOptions DefaultLogViewOptions = new();

	internal static class LogViewImpl
	{
		private const string RowsLabel = "rows";

		/// <summary>How far a column may drift from its measured width, in pixels, before the columns are rebuilt.</summary>
		private const float WidthTolerance = 0.01f;
		private const string ContextPopup = "##context";

		private static readonly (LogViewLevel Level, string Label)[] LevelToggles =
		[
			(LogViewLevel.Trace, "Trace"),
			(LogViewLevel.Debug, "Debug"),
			(LogViewLevel.Information, "Info"),
			(LogViewLevel.Warning, "Warn"),
			(LogViewLevel.Error, "Error"),
			(LogViewLevel.Critical, "Critical"),
		];

		public static bool Draw(string label, IReadOnlyList<LogEntry> entries, LogViewState state, LogViewOptions options)
		{
			LogViewState.ViewData view = ViewFor(state, options);

			ImGui.BeginGroup();

			bool changed;

			using (new ScopedId(label))
			{
				state.Update(entries);

				if (options.ShowToolbar)
				{
					DrawToolbar(entries, state, options, view);

					// The toolbar may have changed the filter, the levels or collapsing; the list below
					// should show what the toolbar now says, not what it said a moment ago.
					state.Update(entries);
				}

				changed = DrawList(entries, state, options, view);
			}

			ImGui.EndGroup();
			ImGuiProbes.MarkItem(label);

			return changed;
		}

		private static LogViewState.ViewData ViewFor(LogViewState state, LogViewOptions options)
		{
			LogViewState.ViewData view = state.View;

			if (view.Table.OuterSize != options.Size)
			{
				view.Table = new VirtualTableOptions
				{
					Flags = ImGuiTableFlags.ScrollY | ImGuiTableFlags.RowBg | ImGuiTableFlags.BordersInnerV
						| ImGuiTableFlags.Resizable | ImGuiTableFlags.SizingFixedFit,
					OuterSize = options.Size,
					ShowHeaders = true,
					ScrollToBottom = true,
				};
			}

			float timeWidth = ImGui.CalcTextSize("00:00:00.000").X;
			float levelWidth = ImGui.CalcTextSize("Information").X;
			int columnCount = options.ShowCategory ? 4 : 3;

			if (view.Columns.Length != columnCount || MathF.Abs(view.Columns[0].Width - timeWidth) > WidthTolerance || MathF.Abs(view.Columns[1].Width - levelWidth) > WidthTolerance)
			{
				view.Columns = options.ShowCategory
					?
					[
						new("Time", ImGuiTableColumnFlags.WidthFixed, timeWidth),
						new("Level", ImGuiTableColumnFlags.WidthFixed, levelWidth),
						new("Category", ImGuiTableColumnFlags.WidthFixed, 120f),
						new("Message", ImGuiTableColumnFlags.WidthStretch),
					]
					:
					[
						new("Time", ImGuiTableColumnFlags.WidthFixed, timeWidth),
						new("Level", ImGuiTableColumnFlags.WidthFixed, levelWidth),
						new("Message", ImGuiTableColumnFlags.WidthStretch),
					];
			}

			return view;
		}

		private static void DrawToolbar(IReadOnlyList<LogEntry> entries, LogViewState state, LogViewOptions options, LogViewState.ViewData view)
		{
			float right = ToolbarRight(options);
			// Drawn under a hidden label, so no "filter" caption sits beside the box, and marked under the
			// visible name afterwards, so the probe name stays label/filter whatever the options' label is.
			string probeName = state.FilterOptions.Label;
			SearchBoxOptions filterOptions = state.FilterOptions with { Label = "##" + probeName };
			string filterText = state.FilterText ?? string.Empty;

			ImGui.SetNextItemWidth(200f);
			SearchBox(ref filterOptions, ref filterText);
			ImGuiProbes.MarkItem(probeName);

			state.FilterOptions = filterOptions with { Label = probeName };
			state.FilterText = filterText;

			foreach ((LogViewLevel level, string text) in LevelToggles)
			{
				SameLineIfFits(CheckboxWidth(text), right);

				bool enabled = state.IsLevelEnabled(level);

				if (ImGui.Checkbox(text, ref enabled))
				{
					state.SetLevelEnabled(level, enabled);
				}

				ImGuiProbes.MarkItem(text);
			}

			SameLineIfFits(CheckboxWidth("Collapse repeats"), right);

			bool collapse = state.CollapseRepeats;

			if (ImGui.Checkbox("Collapse repeats", ref collapse))
			{
				state.CollapseRepeats = collapse;
			}

			ImGuiProbes.MarkItem("Collapse repeats");

			SameLineIfFits(CheckboxWidth("Follow"), right);

			bool follow = state.IsFollowing;

			if (ImGui.Checkbox("Follow", ref follow))
			{
				state.IsFollowing = follow;

				if (follow)
				{
					view.Table.ScrollToBottom = true;
				}
			}

			ImGuiProbes.MarkItem("Follow");

			string count = string.Create(CultureInfo.InvariantCulture, $"{state.VisibleCount} of {entries.Count}");
			SameLineIfFits(ImGui.CalcTextSize(count).X, right);
			ImGui.TextDisabled(count);
		}

		/// <summary>
		/// Where the toolbar has to wrap: the window's edge, or the list's when the list is narrower, so the
		/// toolbar never sticks out past the list it belongs to.
		/// </summary>
		private static float ToolbarRight(LogViewOptions options)
		{
			float left = ImGui.GetCursorScreenPos().X;
			float windowRight = left + ImGui.GetContentRegionAvail().X;
			return options.Size.X > 0f ? MathF.Min(windowRight, left + options.Size.X) : windowRight;
		}

		private static float CheckboxWidth(string label)
		{
			ImGuiStylePtr style = ImGui.GetStyle();
			return ImGui.GetFrameHeight() + style.ItemInnerSpacing.X + ImGui.CalcTextSize(label).X;
		}

		/// <summary>Continues the toolbar's line when the next item fits on it, and starts a new line when it does not.</summary>
		private static void SameLineIfFits(float width, float right)
		{
			float next = ImGui.GetItemRectMax().X + ImGui.GetStyle().ItemSpacing.X;

			if (next + width <= right)
			{
				ImGui.SameLine();
			}
		}

		private static bool DrawList(IReadOnlyList<LogEntry> entries, LogViewState state, LogViewOptions options, LogViewState.ViewData view)
		{
			VirtualTableOptions table = view.Table;
			int selectionBefore = state.SelectionVersion;
			bool scrollRequested = table.ScrollToBottom;

			view.ListHovered = false;
			view.ListFocused = false;
			table.SelectedRow = -1;

			Vector2 listMin = ImGui.GetCursorScreenPos();

			VirtualTable(RowsLabel, state.VisibleCount, view.Columns, row => DrawRow(entries, state, options, view, row), table);

			Vector2 listMax = ImGui.GetItemRectMax();
			ImGuiIOPtr io = ImGui.GetIO();
			bool hovered = view.ListHovered
				|| (ImGui.IsMouseHoveringRect(listMin, listMax) && ImGui.IsWindowHovered(ImGuiHoveredFlags.ChildWindows));

			if (table.SelectedRow >= 0)
			{
				state.Click(table.SelectedRow, io.KeyShift, io.KeyCtrl);
			}

			UpdateFollow(state, view, scrollRequested, hovered && io.MouseWheel > 0f);

			if ((hovered || view.ListFocused) && !ImGui.IsAnyItemActive())
			{
				HandleKeys(entries, state, options, io);
			}

			if (hovered && ImGui.IsMouseClicked(ImGuiMouseButton.Right))
			{
				ImGui.OpenPopup(ContextPopup);
			}

			DrawContextMenu(entries, state, options);

			view.PreviousVisibleCount = state.VisibleCount;

			return state.SelectionVersion != selectionBefore;
		}

		/// <summary>
		/// Feeds this frame's scroll to the state, accounting for ImGui applying a scroll a frame after it is
		/// asked for.
		/// </summary>
		/// <remarks>
		/// On the frame a scroll to the bottom is issued, the reading is still the position before it, which
		/// would look exactly like the user having scrolled up. The widget's own scroll only ever moves down,
		/// so a reading that has not gone up is read as being at the bottom; one that has gone up is the user,
		/// as is the wheel turned upwards over the list, which would otherwise be overridden by the next
		/// scroll to the bottom while lines are streaming in.
		/// </remarks>
		private static void UpdateFollow(LogViewState state, LogViewState.ViewData view, bool scrollRequested, bool wheelUp)
		{
			VirtualTableOptions table = view.Table;
			float rowHeight = ImGui.GetTextLineHeightWithSpacing();
			float scrollY = scrollRequested && table.ScrollY >= view.PreviousScrollY ? table.ScrollMaxY : table.ScrollY;

			if (wheelUp)
			{
				state.IsFollowing = false;
			}

			bool rowsAdded = state.VisibleCount > view.PreviousVisibleCount;

			if (state.UpdateFollow(scrollY, table.ScrollMaxY, rowHeight) && rowsAdded && !wheelUp)
			{
				table.ScrollToBottom = true;
			}

			view.PreviousScrollY = table.ScrollY;
		}

		private static void DrawRow(IReadOnlyList<LogEntry> entries, LogViewState state, LogViewOptions options, LogViewState.ViewData view, int row)
		{
			// Inside the table's scrolling child, which is the window the list's hover and focus belong to.
			view.ListHovered |= ImGui.IsWindowHovered();
			view.ListFocused |= ImGui.IsWindowFocused();

			int source = state.SourceIndexOf(row);

			if (source >= entries.Count)
			{
				return;
			}

			LogEntry entry = entries[source];
			int repeats = state.RepeatCountOf(row);

			if (state.IsRowSelected(row))
			{
				ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg1, ImGui.GetColorU32(ImGuiCol.Header));
			}
			else if (entry.Level == LogViewLevel.Critical)
			{
				Vector4 tint = Palette.Semantic.Error.Value;
				tint.W *= 0.2f;
				ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg1, ImGui.GetColorU32(tint));
			}

			PushLevelColor(entry.Level);

			try
			{
				ImGui.TextUnformatted(entry.Timestamp.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture));
				ImGui.TableNextColumn();
				ImGui.TextUnformatted(LevelName(entry.Level));

				if (options.ShowCategory)
				{
					ImGui.TableNextColumn();
					ImGui.TextUnformatted(entry.Category ?? string.Empty);
				}

				ImGui.TableNextColumn();
				DrawMessage(entry.Message ?? string.Empty, repeats);
			}
			finally
			{
				ImGui.PopStyleColor();
			}
		}

		private static void DrawMessage(string message, int repeats)
		{
			int lineEnd = message.AsSpan().IndexOfAny('\r', '\n');
			bool multiLine = lineEnd >= 0;
			string shown = multiLine ? string.Concat(message.AsSpan(0, lineEnd), " …") : message;
			float available = ImGui.GetContentRegionAvail().X;

			ImGui.TextUnformatted(shown);

			bool clipped = ImGui.GetItemRectSize().X > available;
			bool hovered = ImGui.IsItemHovered();

			if (repeats > 1)
			{
				ImGui.SameLine();
				ImGui.PushStyleColor(ImGuiCol.Text, ImGui.GetColorU32(ImGuiCol.TextDisabled));
				ImGui.TextUnformatted(string.Create(CultureInfo.InvariantCulture, $" ×{repeats}"));
				ImGui.PopStyleColor();
			}

			if (hovered && (multiLine || clipped))
			{
				// Still under the row's level colour, so the tooltip reads as the same line.
				ImGui.BeginTooltip();
				ImGui.PushTextWrapPos(ImGui.GetFontSize() * 40f);
				ImGui.TextUnformatted(message);
				ImGui.PopTextWrapPos();
				ImGui.EndTooltip();
			}
		}

		private static void PushLevelColor(LogViewLevel level)
		{
			switch (level)
			{
				case LogViewLevel.Trace:
				case LogViewLevel.Debug:
					ImGui.PushStyleColor(ImGuiCol.Text, ImGui.GetColorU32(ImGuiCol.TextDisabled));
					break;
				case LogViewLevel.Warning:
					ImGui.PushStyleColor(ImGuiCol.Text, Palette.Semantic.Warning.Value);
					break;
				case LogViewLevel.Error:
				case LogViewLevel.Critical:
					ImGui.PushStyleColor(ImGuiCol.Text, Palette.Semantic.Error.Value);
					break;
				default:
					ImGui.PushStyleColor(ImGuiCol.Text, ImGui.GetColorU32(ImGuiCol.Text));
					break;
			}
		}

		private static string LevelName(LogViewLevel level) => level switch
		{
			LogViewLevel.Trace => "Trace",
			LogViewLevel.Debug => "Debug",
			LogViewLevel.Information => "Information",
			LogViewLevel.Warning => "Warning",
			LogViewLevel.Error => "Error",
			LogViewLevel.Critical => "Critical",
			_ => level.ToString(),
		};

		private static void HandleKeys(IReadOnlyList<LogEntry> entries, LogViewState state, LogViewOptions options, ImGuiIOPtr io)
		{
			if (io.KeyCtrl && ImGui.IsKeyPressed(ImGuiKey.C, false))
			{
				if (state.SelectedCount > 0)
				{
					Copy(options, state.BuildCopyText(entries, selectionOnly: true));
				}
			}
			else if (io.KeyCtrl && ImGui.IsKeyPressed(ImGuiKey.A, false))
			{
				state.SelectAll();
			}
			else if (ImGui.IsKeyPressed(ImGuiKey.Escape, false))
			{
				state.ClearSelection();
			}
		}

		private static void DrawContextMenu(IReadOnlyList<LogEntry> entries, LogViewState state, LogViewOptions options)
		{
			if (!ImGui.BeginPopup(ContextPopup))
			{
				return;
			}

			bool hasSelection = state.SelectedCount > 0;

			if (ImGui.MenuItem("Copy", "Ctrl+C", false, hasSelection))
			{
				Copy(options, state.BuildCopyText(entries, selectionOnly: true));
			}

			ImGuiProbes.MarkItem("Copy");

			if (ImGui.MenuItem("Copy all visible"))
			{
				Copy(options, state.BuildCopyText(entries, selectionOnly: false));
			}

			ImGuiProbes.MarkItem("Copy all visible");

			if (ImGui.MenuItem("Clear selection", "Escape", false, hasSelection))
			{
				state.ClearSelection();
			}

			ImGuiProbes.MarkItem("Clear selection");

			ImGui.EndPopup();
		}

		private static void Copy(LogViewOptions options, string text)
		{
			if (options.OnCopy is not null)
			{
				options.OnCopy(text);
			}
			else
			{
				ImGui.SetClipboardText(text);
			}
		}
	}
}
