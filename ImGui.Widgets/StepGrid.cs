// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Probes;

/// <summary>
/// Provides custom ImGui widgets.
/// </summary>
public static partial class ImGuiWidgets
{
	/// <summary>Draws a step-sequencer grid of on/off cells with an optional playing column.</summary>
	/// <remarks>
	/// <para>
	/// One row per voice, one column per time step. A press toggles a cell, and dragging from it
	/// paints that cell's new value across every cell the pointer passes over. The grid never
	/// reflows: its column count is the pattern length, so column <c>i</c> is always step <c>i</c>.
	/// It reserves exactly the label column plus the grid on one line and never clips or scrolls;
	/// wrap a long pattern in a child window of your own.
	/// </para>
	/// <para>
	/// The pattern belongs to the caller and is edited in place, never copied. The host owns the
	/// clock and passes <paramref name="playingStep"/>.
	/// </para>
	/// </remarks>
	/// <param name="label">ImGui id and probe name. Cells are probed as <c>"{label}/r{row}s{step}"</c>.</param>
	/// <param name="steps">Row-major cell values: cell (row, step) is <c>steps[row * stepCount + step]</c>. Edited in place.</param>
	/// <param name="rows">Number of rows (voices).</param>
	/// <param name="stepCount">Number of columns (time steps). The grid never reflows: column i is always step i.</param>
	/// <param name="playingStep">Column to highlight as playing; -1 (the default), or any value outside [0, stepCount), highlights none.</param>
	/// <param name="stepsPerBeat">Every column where <c>step % stepsPerBeat == 0</c> is shaded as a beat start. Default 4; 0 or less disables shading.</param>
	/// <param name="cellSize">Size of one cell; <c>default</c> means a square of <c>ImGui.GetFrameHeight()</c>.</param>
	/// <param name="rowLabels">Optional per-row names drawn to the left of the grid. Missing entries draw nothing; extra entries are ignored.</param>
	/// <returns><see langword="true"/> on any frame in which at least one cell of <paramref name="steps"/> changed.</returns>
	/// <exception cref="ArgumentException"><paramref name="steps"/>.Length is not <paramref name="rows"/> * <paramref name="stepCount"/>.</exception>
	[SuppressMessage("Major Code Smell", "S107:Methods should not have too many parameters", Justification = "Mirrors Dear ImGui's parameter-rich immediate-mode API; every parameter after the pattern has a default, and the signature is the one specified for the widget.")]
	public static bool StepGrid(string label, Span<bool> steps, int rows, int stepCount, int playingStep = -1, int stepsPerBeat = 4, Vector2 cellSize = default, ReadOnlySpan<string> rowLabels = default) =>
		StepGridImpl.Draw(label, steps, rows, stepCount, playingStep, stepsPerBeat, cellSize, rowLabels);

	internal static class StepGridImpl
	{
		private static readonly Dictionary<uint, StepGridState> States = [];

		[SuppressMessage("Major Code Smell", "S107:Methods should not have too many parameters", Justification = "A pass-through of the public StepGrid widget's parameters.")]
		public static bool Draw(string label, Span<bool> steps, int rows, int stepCount, int playingStep, int stepsPerBeat, Vector2 cellSize, ReadOnlySpan<string> rowLabels)
		{
			// Nothing to draw comes before the length check, so an empty grid never throws.
			if (rows <= 0 || stepCount <= 0)
			{
				return false;
			}

			long expected = (long)rows * stepCount;
			if (steps.Length != expected)
			{
				throw new ArgumentException(
					string.Format(
						CultureInfo.InvariantCulture,
						"StepGrid '{0}' needs {1} cells ({2} rows x {3} steps), but the pattern has {4}.",
						label,
						expected,
						rows,
						stepCount,
						steps.Length),
					nameof(steps));
			}

			Vector2 cell = IsUsableCellSize(cellSize) ? cellSize : new Vector2(ImGui.GetFrameHeight());
			float gap = MathF.Max(1f, MathF.Round(ImGui.GetFontSize() * 0.125f));
			StepGridLayout layout = new(rows, stepCount, cell, gap);

			Vector2 origin = ImGui.GetCursorScreenPos();
			float labelColumn = LabelColumnWidth(rowLabels, rows);
			Vector2 gridOrigin = origin + new Vector2(labelColumn, 0f);

			uint id = ImGui.GetID(label);
			if (!States.TryGetValue(id, out StepGridState? state))
			{
				state = new StepGridState();
				States[id] = state;
			}

			// The one interaction surface covers the grid only; the labels are drawn, not submitted.
			ImGui.SetCursorScreenPos(gridOrigin);
			ImGui.InvisibleButton(label, layout.Size, ImGuiButtonFlags.MouseButtonLeft);
			ImGuiProbes.MarkItem(label);
			WidgetCursor.OnLastItem(ImGuiMouseCursor.Hand);

			bool hovered = ImGui.IsItemHovered();
			Vector2 local = ImGui.GetIO().MousePos - gridOrigin;

			bool changed = false;
			if (ImGui.IsItemActivated())
			{
				changed |= state.Press(steps, layout, local);
			}

			if (ImGui.IsItemActive())
			{
				changed |= state.PaintTo(steps, layout, local);
			}
			else
			{
				state.Release();
			}

			MarkCells(label, layout, gridOrigin);
			DrawLabels(rowLabels, layout, origin, gridOrigin);
			DrawCells(steps, layout, gridOrigin, playingStep, stepsPerBeat);

			if (hovered && !state.IsPainting)
			{
				DrawHover(layout, gridOrigin, local, rowLabels);
			}

			return changed;
		}

		private static bool IsUsableCellSize(Vector2 size) => size.X > 0f && size.Y > 0f;

		private static float LabelColumnWidth(ReadOnlySpan<string> rowLabels, int rows)
		{
			if (rowLabels.IsEmpty)
			{
				return 0f;
			}

			float widest = 0f;
			int count = Math.Min(rows, rowLabels.Length);
			for (int row = 0; row < count; row++)
			{
				string? name = rowLabels[row];
				if (!string.IsNullOrEmpty(name))
				{
					widest = MathF.Max(widest, ImGui.CalcTextSize(name).X);
				}
			}

			return widest + ImGui.GetStyle().ItemInnerSpacing.X;
		}

		private static void MarkCells(string label, StepGridLayout layout, Vector2 gridOrigin)
		{
			if (!ImGuiProbes.IsRecording)
			{
				return;
			}

			for (int row = 0; row < layout.Rows; row++)
			{
				for (int step = 0; step < layout.Steps; step++)
				{
					Vector2 min = gridOrigin + layout.CellMin(row, step);
					ImGuiProbes.MarkRegion(
						string.Create(CultureInfo.InvariantCulture, $"{label}/r{row}s{step}"),
						min,
						min + layout.CellSize);
				}
			}
		}

		private static void DrawLabels(ReadOnlySpan<string> rowLabels, StepGridLayout layout, Vector2 origin, Vector2 gridOrigin)
		{
			ImDrawListPtr drawList = ImGui.GetWindowDrawList();
			uint color = ImGui.GetColorU32(ImGuiCol.Text);
			float textHeight = ImGui.GetTextLineHeight();

			int count = Math.Min(layout.Rows, rowLabels.Length);
			for (int row = 0; row < count; row++)
			{
				string? name = rowLabels[row];
				if (string.IsNullOrEmpty(name))
				{
					continue;
				}

				float y = gridOrigin.Y + layout.CellMin(row, 0).Y + ((layout.CellSize.Y - textHeight) * 0.5f);
				drawList.AddText(new Vector2(origin.X, y), color, name);
			}
		}

		private static void DrawCells(ReadOnlySpan<bool> steps, StepGridLayout layout, Vector2 gridOrigin, int playingStep, int stepsPerBeat)
		{
			ImDrawListPtr drawList = ImGui.GetWindowDrawList();
			float rounding = ImGui.GetStyle().FrameRounding;
			bool hasPlaying = playingStep >= 0 && playingStep < layout.Steps;

			uint off = ImGui.GetColorU32(ImGuiCol.FrameBg);
			uint offBeat = ImGui.GetColorU32(ImGuiCol.FrameBgHovered);
			uint on = ImGui.GetColorU32(ImGuiCol.CheckMark);
			uint onPlaying = ImGui.GetColorU32(ImGuiCol.SliderGrabActive);

			for (int row = 0; row < layout.Rows; row++)
			{
				for (int step = 0; step < layout.Steps; step++)
				{
					bool isOn = steps[(row * layout.Steps) + step];
					uint fill = isOn
						? (hasPlaying && step == playingStep ? onPlaying : on)
						: (StepGridLayout.IsBeatStart(step, stepsPerBeat) ? offBeat : off);

					Vector2 min = gridOrigin + layout.CellMin(row, step);
					drawList.AddRectFilled(min, min + layout.CellSize, fill, rounding);
				}
			}

			if (hasPlaying)
			{
				// Translucent in every built-in style, so the cells show through the highlight.
				Vector2 min = gridOrigin + layout.CellMin(0, playingStep);
				Vector2 max = new(min.X + layout.CellSize.X, gridOrigin.Y + layout.Size.Y);
				drawList.AddRectFilled(min, max, ImGui.GetColorU32(ImGuiCol.TextSelectedBg), rounding);
			}
		}

		private static void DrawHover(StepGridLayout layout, Vector2 gridOrigin, Vector2 local, ReadOnlySpan<string> rowLabels)
		{
			int index = layout.HitTest(local);
			if (index < 0)
			{
				return;
			}

			int row = index / layout.Steps;
			int step = index % layout.Steps;

			Vector2 min = gridOrigin + layout.CellMin(row, step);
			ImGui.GetWindowDrawList().AddRect(
				min,
				min + layout.CellSize,
				ImGui.GetColorU32(ImGuiCol.Text),
				ImGui.GetStyle().FrameRounding,
				ImDrawFlags.None,
				1f);

			string? name = row < rowLabels.Length ? rowLabels[row] : null;
			string text = string.IsNullOrEmpty(name)
				? string.Format(CultureInfo.CurrentCulture, "Row {0}, step {1}", row + 1, step + 1)
				: string.Format(CultureInfo.CurrentCulture, "{0}, step {1}", name, step + 1);

			// Unformatted rather than SetTooltip, which reads its argument as a printf format: a row
			// named "100%" must not be taken for a conversion.
			if (ImGui.BeginTooltip())
			{
				ImGui.TextUnformatted(text);
				ImGui.EndTooltip();
			}
		}
	}
}
