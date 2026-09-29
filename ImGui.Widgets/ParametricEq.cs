// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Probes;

/// <summary>
/// Provides custom ImGui widgets.
/// </summary>
public static partial class ImGuiWidgets
{
	/// <summary>
	/// Draws an EQ response and lets the bands be dragged. Reserves <paramref name="size"/> at the cursor.
	/// </summary>
	/// <param name="label">ID and probe name. Each band's node is also recorded as <c>{label}/band{i}</c>.</param>
	/// <param name="bands">The caller's bands, edited in place. The count never changes.</param>
	/// <param name="responseDb">
	/// The total response in dB at a frequency in Hz; this is what is plotted. Called across the width
	/// every frame, so it must be cheap. <see cref="EqResponse.TotalDb"/> is the ready-made one.
	/// </param>
	/// <param name="axis">The frequency axis.</param>
	/// <param name="selectedBand">
	/// The selected band index, or -1. Set on press; cleared by a press away from every node. A value
	/// outside the bands is reset to -1.
	/// </param>
	/// <param name="size">A component &lt;= 0 means: width = available content width, height = 10 text line heights.</param>
	/// <param name="minDb">Bottom of the dB axis.</param>
	/// <param name="maxDb">Top of the dB axis.</param>
	/// <returns>True on a frame where any band's value actually changed (drag, wheel or normalisation).</returns>
	/// <remarks>
	/// <para>
	/// Drag a node to move its band: horizontally for frequency, vertically for gain. The wheel over a
	/// node changes its Q, 1.2x per detent. <see cref="EqBandType.LowCut"/>,
	/// <see cref="EqBandType.HighCut"/> and <see cref="EqBandType.Notch"/> have no gain, so their nodes
	/// sit on the 0 dB line and move only sideways.
	/// </para>
	/// <para>
	/// <b>The caller owns the response.</b> The widget plots <paramref name="responseDb"/> and knows
	/// nothing about filters, so the curve on screen is the curve the caller's DSP applies. Pass
	/// <see cref="EqResponse.TotalDb"/> when there is no filter math of your own.
	/// </para>
	/// <para>
	/// Every band is held inside the axes each frame: a frequency outside <paramref name="axis"/>, a
	/// gain outside the dB range, or a Q outside <see cref="EqBand.MinQ"/>..<see cref="EqBand.MaxQ"/>
	/// is clamped, and a NaN is replaced. That counts as a change and returns true.
	/// </para>
	/// <para>
	/// A <see cref="SpectrumAnalyzer(string, SpectrumAnalyzerState, Vector2, float, float, SpectrumAnalyzerStyle, float)"/>
	/// whose state's <see cref="SpectrumAnalyzerState.Axis"/> is passed here places every frequency at
	/// the same fraction of its width, so the two read against each other.
	/// </para>
	/// </remarks>
	public static bool ParametricEq(
		string label,
		Span<EqBand> bands,
		Func<float, float> responseDb,
		LogFrequencyAxis axis,
		ref int selectedBand,
		Vector2 size = default,
		float minDb = -24f,
		float maxDb = 24f) =>
		ParametricEqImpl.Draw(label, bands, responseDb, axis, ref selectedBand, size, minDb, maxDb);

	internal static class ParametricEqImpl
	{
		private static readonly Dictionary<uint, ParametricEqState> States = [];

		/// <summary>How far apart the plotted samples are, in pixels.</summary>
		private const float SampleSpacing = 2f;

		/// <summary>The spacing of the horizontal gridlines, in decibels.</summary>
		private const float GridStepDb = 6f;

		/// <summary>
		/// The most dB gridlines drawn. A range wide enough to need more would draw a solid block, and
		/// the loop would scale with the caller's numbers rather than with the screen.
		/// </summary>
		private const int MaxDbGridLines = 256;

		/// <summary>Up to this many bands have their node positions on the stack.</summary>
		private const int StackNodeLimit = 64;

		public static bool Draw(
			string label,
			Span<EqBand> bands,
			Func<float, float> responseDb,
			LogFrequencyAxis axis,
			ref int selectedBand,
			Vector2 size,
			float minDb,
			float maxDb)
		{
			Ensure.NotNull(label);
			Ensure.NotNull(responseDb);
			Ensure.NotNull(axis);

			float lineHeight = ImGui.GetTextLineHeight();
			if (size.X <= 0f)
			{
				size.X = ImGui.GetContentRegionAvail().X;
			}

			if (size.Y <= 0f)
			{
				size.Y = lineHeight * 10f;
			}

			if (selectedBand < -1 || selectedBand >= bands.Length)
			{
				selectedBand = -1;
			}

			if (!(size.X >= 1f && size.Y >= 1f))
			{
				ImGui.Dummy(Vector2.One);
				ImGuiProbes.MarkItem(label);
				return false;
			}

			Vector2 min = ImGui.GetCursorScreenPos();
			Vector2 max = min + size;
			ImGui.InvisibleButton(label, size);
			ImGuiProbes.MarkItem(label);

			ImDrawListPtr drawList = ImGui.GetWindowDrawList();
			Span<Vector4> colors = ImGui.GetStyle().Colors;
			uint border = ImGui.GetColorU32(colors[(int)ImGuiCol.Border]);

			drawList.PushClipRect(min, max, true);
			drawList.AddRectFilled(min, max, ImGui.GetColorU32(colors[(int)ImGuiCol.FrameBg]));

			// The same guard SpectrumAnalyzer has: a reversed, empty or non-finite range has nowhere to
			// put a gain, so there is nothing to draw and nothing to normalise towards.
			if (minDb >= maxDb || !float.IsFinite(minDb) || !float.IsFinite(maxDb))
			{
				drawList.AddRect(min, max, border);
				drawList.PopClipRect();
				return false;
			}

			bool changed = ParametricEqState.Normalize(bands, axis, minDb, maxDb);

			// Half a line, floored at six pixels, as CurveTrack's points are: the same radius draws a
			// node and catches the press on it.
			float radius = MathF.Max(lineHeight * 0.5f, 6f);

			Span<Vector2> nodes = bands.Length <= StackNodeLimit ? stackalloc Vector2[bands.Length] : new Vector2[bands.Length];
			PlaceNodes(bands, nodes, axis, minDb, maxDb, min, size);

			uint id = ImGui.GetID(label);
			if (!States.TryGetValue(id, out ParametricEqState? state))
			{
				state = new ParametricEqState();
				States[id] = state;
			}

			ImGuiIOPtr io = ImGui.GetIO();
			Vector2 pointer = io.MousePos;

			if (ImGui.IsItemActivated())
			{
				selectedBand = state.Activate(nodes, pointer, radius) ? state.ActiveBand : -1;
			}

			if (ImGui.IsItemActive())
			{
				changed |= state.Drag(bands, pointer, axis, minDb, maxDb, min, size);
			}

			if (ImGui.IsItemDeactivated())
			{
				state.Release();
			}

			bool hovered = ImGui.IsItemHovered();
			if (hovered && MathF.Abs(io.MouseWheel) > float.Epsilon)
			{
				int under = ParametricEqState.Pick(nodes, pointer, radius);
				if (under >= 0)
				{
					changed |= ParametricEqState.AdjustQ(bands, under, io.MouseWheel);

					// The wheel changed a Q, so it must not scroll the window as well. Over empty space
					// it is left alone and the window scrolls as usual.
					ImGui.SetItemKeyOwner(ImGuiKey.MouseWheelY);
				}
			}

			// The drag and the wheel may have moved the bands, so draw and record where they are now.
			PlaceNodes(bands, nodes, axis, minDb, maxDb, min, size);

			int hot = state.ActiveBand >= 0 && state.ActiveBand < bands.Length && ImGui.IsItemActive()
				? state.ActiveBand
				: hovered ? ParametricEqState.Pick(nodes, pointer, radius) : -1;

			DrawGrid(drawList, axis, minDb, maxDb, min, max, size, border);
			PlotResponse(drawList, responseDb, axis, minDb, maxDb, min, size, colors);
			DrawNodes(drawList, bands, nodes, radius, hot, selectedBand, colors);
			drawList.AddRect(min, max, border);
			drawList.PopClipRect();

			for (int i = 0; i < nodes.Length; i++)
			{
				ImGuiProbes.MarkRegion($"{label}/band{i}", nodes[i] - new Vector2(radius), nodes[i] + new Vector2(radius));
			}

			if (hot >= 0)
			{
				ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeAll);
				ShowTooltip(hot, bands[hot]);
			}

			return changed;
		}

		private static void PlaceNodes(ReadOnlySpan<EqBand> bands, Span<Vector2> nodes, LogFrequencyAxis axis, float minDb, float maxDb, Vector2 min, Vector2 size)
		{
			for (int i = 0; i < bands.Length; i++)
			{
				nodes[i] = ParametricEqState.NodePosition(bands[i], axis, minDb, maxDb, min, size);
			}
		}

		private static float GainToY(float gainDb, float minDb, float maxDb, Vector2 min, Vector2 size) =>
			min.Y + ((maxDb - gainDb) / (maxDb - minDb) * size.Y);

		private static void DrawGrid(ImDrawListPtr drawList, LogFrequencyAxis axis, float minDb, float maxDb, Vector2 min, Vector2 max, Vector2 size, uint color)
		{
			for (float decade = axis.FirstDecade; decade < axis.MaxFrequency; decade *= 10f)
			{
				// Snapped to a whole pixel: ImGui centres a line on the pixel it names, so a one-pixel
				// line at a fractional position smears across two rows at half strength.
				float x = MathF.Floor(min.X + (axis.FrequencyToPosition(decade) * size.X));
				drawList.AddLine(new Vector2(x, min.Y), new Vector2(x, max.Y), color);
			}

			float first = MathF.Ceiling(minDb / GridStepDb);
			float last = MathF.Floor(maxDb / GridStepDb);
			if (last - first >= MaxDbGridLines)
			{
				// Too dense to read. Keep only the line that says "unchanged", when it is in range.
				first = 0f;
				last = minDb <= 0f && maxDb >= 0f ? 0f : -1f;
			}

			for (float step = first; step <= last; step++)
			{
				float gain = step * GridStepDb;
				float y = GainToY(gain, minDb, maxDb, min, size);
				if (MathF.Abs(gain) < GridStepDb * 0.5f)
				{
					// Two pixels wide, so centred on a pixel boundary rather than a pixel.
					y = MathF.Round(y) - 0.5f;
					drawList.AddLine(new Vector2(min.X, y), new Vector2(max.X, y), color, 2f);
				}
				else
				{
					y = MathF.Floor(y);
					drawList.AddLine(new Vector2(min.X, y), new Vector2(max.X, y), color);
				}
			}
		}

		private static void PlotResponse(
			ImDrawListPtr drawList,
			Func<float, float> responseDb,
			LogFrequencyAxis axis,
			float minDb,
			float maxDb,
			Vector2 min,
			Vector2 size,
			ReadOnlySpan<Vector4> colors)
		{
			int steps = Math.Max(2, (int)(size.X / SampleSpacing));
			uint color = ImGui.GetColorU32(colors[(int)ImGuiCol.PlotLines]);

			Vector2 previous = Vector2.Zero;
			bool havePrevious = false;

			for (int i = 0; i <= steps; i++)
			{
				float t = i / (float)steps;
				float db = responseDb(axis.PositionToFrequency(t));

				if (!float.IsFinite(db))
				{
					// Break the line rather than drawing a segment to nowhere.
					havePrevious = false;
					continue;
				}

				Vector2 point = new(min.X + (t * size.X), GainToY(Math.Clamp(db, minDb, maxDb), minDb, maxDb, min, size));
				if (havePrevious)
				{
					drawList.AddLine(previous, point, color, 2f);
				}

				previous = point;
				havePrevious = true;
			}
		}

		private static void DrawNodes(
			ImDrawListPtr drawList,
			ReadOnlySpan<EqBand> bands,
			ReadOnlySpan<Vector2> nodes,
			float radius,
			int hot,
			int selectedBand,
			ReadOnlySpan<Vector4> colors)
		{
			uint idle = ImGui.GetColorU32(colors[(int)ImGuiCol.SliderGrab]);
			uint active = ImGui.GetColorU32(colors[(int)ImGuiCol.SliderGrabActive]);
			uint text = ImGui.GetColorU32(colors[(int)ImGuiCol.Text]);

			// Band order, so a later band is drawn over an earlier one, matching Pick's tie rule.
			for (int i = 0; i < bands.Length; i++)
			{
				Vector2 node = nodes[i];
				drawList.AddCircleFilled(node, radius, i == hot || i == selectedBand ? active : idle, 16);

				if (i == selectedBand)
				{
					drawList.AddCircle(node, radius + 2f, text, 16, 1.5f);
				}

				string number = (i + 1).ToString(CultureInfo.InvariantCulture);
				Vector2 textSize = ImGui.CalcTextSize(number);
				drawList.AddText(new Vector2(node.X - (textSize.X * 0.5f), node.Y - radius - 2f - textSize.Y), text, number);
			}
		}

		private static void ShowTooltip(int index, EqBand band)
		{
			if (!ImGui.BeginTooltip())
			{
				return;
			}

			ImGui.TextUnformatted(string.Format(CultureInfo.InvariantCulture, "Band {0} · {1}", index + 1, band.Type));
			ImGui.TextUnformatted(DescribeValues(band));
			ImGui.EndTooltip();
		}

		/// <summary>The tooltip's second line: frequency, gain when the band has one, and Q.</summary>
		internal static string DescribeValues(EqBand band)
		{
			string frequency = band.Frequency < 1000f
				? string.Format(CultureInfo.InvariantCulture, "{0:0} Hz", band.Frequency)
				: string.Format(CultureInfo.InvariantCulture, "{0:0.00} kHz", band.Frequency / 1000f);
			string q = string.Format(CultureInfo.InvariantCulture, "Q {0:0.00}", band.Q);

			return band.HasGain
				? string.Format(CultureInfo.InvariantCulture, "{0}  {1:+0.0;-0.0} dB  {2}", frequency, band.GainDb, q)
				: string.Format(CultureInfo.InvariantCulture, "{0}  {1}", frequency, q);
		}
	}
}
