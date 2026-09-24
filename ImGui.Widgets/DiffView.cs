// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Styler;

/// <summary>
/// Provides custom ImGui widgets.
/// </summary>
public static partial class ImGuiWidgets
{
	/// <summary>How a diff is laid out.</summary>
	public enum DiffViewMode
	{
		/// <summary>One column, in the order the lines appear, which is the shape a patch has.</summary>
		Unified,

		/// <summary>Two columns, the old side against the new, aligned with filler rows.</summary>
		SideBySide,
	}

	/// <summary>How a diff view is drawn.</summary>
	public sealed record DiffViewOptions
	{
		/// <summary>Gets the layout.</summary>
		public DiffViewMode Mode { get; init; } = DiffViewMode.Unified;

		/// <summary>Gets a value indicating whether the line number columns are drawn.</summary>
		public bool CanShowLineNumbers { get; init; } = true;

		/// <summary>Gets a value indicating whether clicking a hunk's header collapses it.</summary>
		public bool CanCollapseHunks { get; init; } = true;
	}

	/// <summary>
	/// Draws a diff with a checkbox on each hunk, and reports whether the selection changed.
	/// </summary>
	/// <remarks>
	/// The diff is drawn, never computed. Whoever produced it owns that, and mapping their result onto
	/// <see cref="DiffHunk"/> is what connects the two.
	/// <para>
	/// <paramref name="selection"/> is mutated in place and holds indices into <paramref name="hunks"/>.
	/// It is the caller's because it is the part that outlives the frame, which is what a caller hands
	/// on to stage what was ticked. An index no longer naming a hunk is ignored rather than tracked,
	/// so a caller re-reading a diff after staging clears the selection.
	/// </para>
	/// </remarks>
	/// <param name="label">The widget's ImGui id.</param>
	/// <param name="hunks">The hunks to draw.</param>
	/// <param name="selection">The selected hunk indices, mutated in place.</param>
	/// <param name="options">How to draw it, or <see langword="null"/> for the defaults.</param>
	/// <returns><see langword="true"/> when the selection changed this frame.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="hunks"/> or <paramref name="selection"/> is <see langword="null"/>.</exception>
	public static bool DiffView(
		string label,
		IReadOnlyList<DiffHunk> hunks,
		ISet<int> selection,
		DiffViewOptions? options = null)
	{
		Ensure.NotNull(hunks);
		Ensure.NotNull(selection);

		return DiffViewImpl.Draw(label, hunks, selection, options ?? new DiffViewOptions());
	}

	/// <summary>Draws a diff with nothing to select.</summary>
	/// <param name="label">The widget's ImGui id.</param>
	/// <param name="hunks">The hunks to draw.</param>
	/// <param name="options">How to draw it, or <see langword="null"/> for the defaults.</param>
	/// <exception cref="ArgumentNullException"><paramref name="hunks"/> is <see langword="null"/>.</exception>
	public static void DiffView(
		string label,
		IReadOnlyList<DiffHunk> hunks,
		DiffViewOptions? options = null)
	{
		Ensure.NotNull(hunks);

		_ = DiffViewImpl.Draw(label, hunks, selection: null, options ?? new DiffViewOptions());
	}

	internal static class DiffViewImpl
	{
		private static readonly Dictionary<uint, DiffViewState> States = [];

		/// <summary>How opaque a changed line's row tint is.</summary>
		/// <remarks>
		/// Low enough that the text stays the text's color rather than becoming a shade of the tint,
		/// which is the failure mode of drawing a diff over a colored row.
		/// </remarks>
		private const float ChangedRowAlpha = 0.22f;

		/// <summary>How opaque a context row's tint is.</summary>
		private const float ContextRowAlpha = 0.10f;

		public static bool Draw(
			string label,
			IReadOnlyList<DiffHunk> hunks,
			ISet<int>? selection,
			DiffViewOptions options)
		{
			if (hunks.Count == 0)
			{
				return false;
			}

			ImGui.PushID(label);

			try
			{
				uint id = ImGui.GetID(label);

				if (!States.TryGetValue(id, out DiffViewState? state))
				{
					state = new DiffViewState();
					States[id] = state;
				}

				bool changed = false;

				for (int index = 0; index < hunks.Count; index++)
				{
					changed |= DrawHunk(index, hunks[index], selection, options, state);
				}

				return changed;
			}
			finally
			{
				ImGui.PopID();
			}
		}

		private static bool DrawHunk(
			int index,
			DiffHunk hunk,
			ISet<int>? selection,
			DiffViewOptions options,
			DiffViewState state)
		{
			ImGui.PushID(index);

			bool changed = false;

			try
			{
				if (selection is not null)
				{
					bool isSelected = selection.Contains(index);

					if (ImGui.Checkbox("##select", ref isSelected))
					{
						changed = DiffViewState.Toggle(selection, index);
					}

					ImGui.SameLine();
				}

				DrawSummary(hunk);
				ImGui.SameLine();

				if (DrawHeading(index, hunk, options, state))
				{
					state.ToggleCollapsed(index);
				}

				if (!state.IsCollapsed(index))
				{
					DrawLines(hunk, options);
				}
			}
			finally
			{
				ImGui.PopID();
			}

			return changed;
		}

		/// <summary>Draws the hunk's heading, which collapses it when the options allow.</summary>
		/// <returns><see langword="true"/> when the heading was clicked.</returns>
		private static bool DrawHeading(int index, DiffHunk hunk, DiffViewOptions options, DiffViewState state)
		{
			string heading = hunk.Heading.Length == 0
				? string.Create(CultureInfo.InvariantCulture, $"Hunk {index + 1}")
				: hunk.Heading;

			if (!options.CanCollapseHunks)
			{
				ImGui.TextUnformatted(heading);
				return false;
			}

			string marker = state.IsCollapsed(index) ? "▶" : "▼";

			// Selectable rather than a tree node, because the checkbox above has already been
			// submitted on this line and a tree node would take the whole row's width from it.
			return ImGui.Selectable($"{marker} {heading}");
		}

		/// <summary>Draws the proportional bar of removals against additions.</summary>
		private static void DrawSummary(DiffHunk hunk)
		{
			int removedCount = 0;
			int addedCount = 0;

			foreach (DiffLine line in hunk.Lines)
			{
				if (line.Kind == DiffLineKind.Removed)
				{
					removedCount++;
				}
				else if (line.Kind == DiffLineKind.Added)
				{
					addedCount++;
				}
			}

			(int removed, int added) = DiffViewState.Summarize(removedCount, addedCount);

			ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0.0f, 0.0f));

			try
			{
				if (removed > 0)
				{
					using (new ScopedColor(ImGuiCol.Text, Palette.Semantic.Error))
					{
						ImGui.TextUnformatted(new string('-', removed));
					}
				}

				if (added > 0)
				{
					if (removed > 0)
					{
						ImGui.SameLine();
					}

					using (new ScopedColor(ImGuiCol.Text, Palette.Semantic.Success))
					{
						ImGui.TextUnformatted(new string('+', added));
					}
				}
			}
			finally
			{
				ImGui.PopStyleVar();
			}
		}

		/// <summary>Draws a hunk's lines in the layout the options ask for.</summary>
		/// <remarks>
		/// Clipped rather than drawn whole, because a hunk of a few thousand lines is ordinary on a
		/// generated file and the cost of a frame should follow what is on screen.
		/// </remarks>
		private static void DrawLines(DiffHunk hunk, DiffViewOptions options)
		{
			if (options.Mode == DiffViewMode.SideBySide)
			{
				DrawPairedLines(hunk, options);
				return;
			}

			DrawUnifiedLines(hunk, options);
		}

		private static void DrawUnifiedLines(DiffHunk hunk, DiffViewOptions options)
		{
			int columns = options.CanShowLineNumbers ? 4 : 2;

			if (!ImGui.BeginTable("lines", columns, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.ScrollX))
			{
				return;
			}

			try
			{
				if (options.CanShowLineNumbers)
				{
					ImGui.TableSetupColumn("old", ImGuiTableColumnFlags.WidthFixed);
					ImGui.TableSetupColumn("new", ImGuiTableColumnFlags.WidthFixed);
				}

				ImGui.TableSetupColumn("marker", ImGuiTableColumnFlags.WidthFixed);
				ImGui.TableSetupColumn("text", ImGuiTableColumnFlags.WidthStretch);

				ImGuiListClipper clipper = default;
				clipper.Begin(hunk.Lines.Count);

				while (clipper.Step())
				{
					for (int row = clipper.DisplayStart; row < clipper.DisplayEnd; row++)
					{
						DrawLine(hunk.Lines[row], options);
					}
				}

				clipper.End();
			}
			finally
			{
				ImGui.EndTable();
			}
		}

		/// <summary>Draws a hunk as an old side against a new side, aligned by filler.</summary>
		/// <remarks>
		/// One table with both sides in it rather than two scrolling panes, so they stay level by
		/// construction. Two panes would need their scroll positions kept in step, which is a state
		/// machine that exists only to undo a layout choice.
		/// </remarks>
		private static void DrawPairedLines(DiffHunk hunk, DiffViewOptions options)
		{
			IReadOnlyList<DiffRowPair> rows = DiffViewState.Pair(hunk);

			if (rows.Count == 0)
			{
				return;
			}

			int perSide = options.CanShowLineNumbers ? 3 : 2;

			if (!ImGui.BeginTable("paired", perSide * 2, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.ScrollX | ImGuiTableFlags.BordersInnerV))
			{
				return;
			}

			try
			{
				SetUpSideColumns(options, "old");
				SetUpSideColumns(options, "new");

				ImGuiListClipper clipper = default;
				clipper.Begin(rows.Count);

				while (clipper.Step())
				{
					for (int row = clipper.DisplayStart; row < clipper.DisplayEnd; row++)
					{
						DrawPairedRow(rows[row], options);
					}
				}

				clipper.End();
			}
			finally
			{
				ImGui.EndTable();
			}
		}

		private static void SetUpSideColumns(DiffViewOptions options, string side)
		{
			if (options.CanShowLineNumbers)
			{
				ImGui.TableSetupColumn($"{side}#", ImGuiTableColumnFlags.WidthFixed);
			}

			ImGui.TableSetupColumn($"{side}marker", ImGuiTableColumnFlags.WidthFixed);
			ImGui.TableSetupColumn($"{side}text", ImGuiTableColumnFlags.WidthStretch);
		}

		private static void DrawPairedRow(DiffRowPair pair, DiffViewOptions options)
		{
			ImGui.TableNextRow();

			DrawSide(pair.Left, isOldSide: true, options);
			DrawSide(pair.Right, isOldSide: false, options);
		}

		/// <summary>Draws one side of a paired row, or an empty tinted gap where it has no line.</summary>
		private static void DrawSide(DiffLine? line, bool isOldSide, DiffViewOptions options)
		{
			if (options.CanShowLineNumbers)
			{
				_ = ImGui.TableNextColumn();
				DrawNumber(isOldSide ? line?.OldNumber : line?.NewNumber);
			}

			_ = ImGui.TableNextColumn();

			if (line is null)
			{
				// Two more empty cells keep the row's shape, tinted fainter than a real change so the
				// gap reads as an absence rather than as content.
				ImGui.TableSetBgColor(ImGuiTableBgTarget.CellBg, FillerTint());
				_ = ImGui.TableNextColumn();
				ImGui.TableSetBgColor(ImGuiTableBgTarget.CellBg, FillerTint());
				return;
			}

			ImGui.TableSetBgColor(ImGuiTableBgTarget.CellBg, TintFor(line.Kind));
			ImGui.TextUnformatted(MarkerFor(line.Kind));

			_ = ImGui.TableNextColumn();
			ImGui.TableSetBgColor(ImGuiTableBgTarget.CellBg, TintFor(line.Kind));
			ImGui.TextUnformatted(line.Text);
		}

		/// <summary>The background for a row that exists on one side only.</summary>
		internal static uint FillerTint()
		{
			Vector4 color = ImGui.GetStyle().Colors[(int)ImGuiCol.FrameBg];
			color.W = ContextRowAlpha * 0.5f;

			return ImGui.ColorConvertFloat4ToU32(color);
		}

		private static void DrawLine(DiffLine line, DiffViewOptions options)
		{
			ImGui.TableNextRow();
			ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, TintFor(line.Kind));

			if (options.CanShowLineNumbers)
			{
				_ = ImGui.TableNextColumn();
				DrawNumber(line.OldNumber);

				_ = ImGui.TableNextColumn();
				DrawNumber(line.NewNumber);
			}

			_ = ImGui.TableNextColumn();
			ImGui.TextUnformatted(MarkerFor(line.Kind));

			_ = ImGui.TableNextColumn();
			ImGui.TextUnformatted(line.Text);
		}

		private static void DrawNumber(int? number)
		{
			if (number is int value)
			{
				ImGui.TextUnformatted(value.ToString(CultureInfo.InvariantCulture));
			}
		}

		internal static string MarkerFor(DiffLineKind kind) => kind switch
		{
			DiffLineKind.Added => "+",
			DiffLineKind.Removed => "-",
			_ => " ",
		};

		/// <summary>The row background for a line of this kind, resolved against the current theme.</summary>
		internal static uint TintFor(DiffLineKind kind)
		{
			Vector4 color = kind switch
			{
				DiffLineKind.Added => Palette.Semantic.Success.Value,
				DiffLineKind.Removed => Palette.Semantic.Error.Value,
				_ => ImGui.GetStyle().Colors[(int)ImGuiCol.FrameBg],
			};

			color.W = kind == DiffLineKind.Context ? ContextRowAlpha : ChangedRowAlpha;

			return ImGui.ColorConvertFloat4ToU32(color);
		}
	}
}
