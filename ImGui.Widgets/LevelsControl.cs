// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;
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
	/// Draws a levels control: a histogram with black, grey and white input handles, an output ramp
	/// with black and white handles, and a numeric readout.
	/// </summary>
	/// <param name="label">Unique label; pushed as an ID scope and used as the probe prefix.</param>
	/// <param name="bins">Histogram bins, passed to <see cref="Histogram"/> unchanged (interleaved series).</param>
	/// <param name="seriesCount">Series count, passed to <see cref="Histogram"/> unchanged (1 for luminance, 3 for RGB).</param>
	/// <param name="levels">The adjustment, edited in place. Normalized on entry.</param>
	/// <param name="size">Histogram size. Non-positive components default as <see cref="Histogram"/> does (CalcItemWidth wide, six text lines tall).</param>
	/// <param name="seriesColors">Series colours, passed to <see cref="Histogram"/> unchanged.</param>
	/// <returns>
	/// <see langword="true"/> if <paramref name="levels"/> changed this frame, including a change made
	/// only by normalization on entry.
	/// </returns>
	/// <remarks>
	/// The caller owns <paramref name="levels"/> and keeps it between frames; apply it to pixels with
	/// <see cref="LevelsAdjustment.Apply"/>, the same function the widget's handles describe. Both
	/// handle rows are <see cref="HandleTrack"/>, which keeps them ordered and
	/// <see cref="LevelsAdjustment.MinGap"/> apart. A held drag that does not move returns
	/// <see langword="false"/>, so a host recording undo on change does not record every frame.
	/// Probe names: <c>label/histogram</c>, <c>label/input</c>, <c>label/outputRamp</c> and
	/// <c>label/output</c>.
	/// </remarks>
	public static bool LevelsControl(
		string label,
		ReadOnlySpan<float> bins,
		int seriesCount,
		ref LevelsAdjustment levels,
		Vector2 size = default,
		ReadOnlySpan<uint> seriesColors = default) =>
		LevelsControlImpl.Draw(label, bins, seriesCount, ref levels, size, seriesColors);

	internal static class LevelsControlImpl
	{
		private const float HandleRadius = 6.0f;
		private const float RampGap = 4.0f;
		private const float RampHeight = 12.0f;

		// Packed as ImGui packs colours, alpha in the top byte: opaque black and opaque white.
		private const uint RampBlack = 0xFF000000u;
		private const uint RampWhite = 0xFFFFFFFFu;

		public static bool Draw(
			string label,
			ReadOnlySpan<float> bins,
			int seriesCount,
			ref LevelsAdjustment levels,
			Vector2 size,
			ReadOnlySpan<uint> seriesColors)
		{
			LevelsAdjustment original = levels;
			levels = levels.Normalized();

			using (new ScopedId(label))
			{
				Histogram("histogram", bins, seriesCount, size, seriesColors);
				Vector2 min = ImGui.GetItemRectMin();
				Vector2 max = ImGui.GetItemRectMax();

				Span<float> input = [levels.InputBlack, levels.GreyPoint, levels.InputWhite];
				if (HandleTrack("input", input, min, max, 0.0f, 1.0f, minGap: LevelsAdjustment.MinGap, handleRadius: HandleRadius, handleCenterY: max.Y))
				{
					levels = levels.WithInputHandles(input);
				}

				ImGui.Dummy(new Vector2(0.0f, RampGap));

				float boxWidth = max.X - min.X;
				ImGui.Dummy(new Vector2(boxWidth, RampHeight));
				ImGuiProbes.MarkItem("outputRamp");
				Vector2 rampMin = ImGui.GetItemRectMin();
				Vector2 rampMax = ImGui.GetItemRectMax();
				DrawRamp(rampMin, rampMax);

				Span<float> output = [levels.OutputBlack, levels.OutputWhite];
				if (HandleTrack("output", output, rampMin, rampMax, 0.0f, 1.0f, minGap: LevelsAdjustment.MinGap, handleRadius: HandleRadius))
				{
					levels = levels.WithOutputHandles(output);
				}

				ImGui.TextUnformatted(FormatReadout(levels));
			}

			return levels != original;
		}

		/// <summary>Formats the one-line readout: input black, gamma and white, then output black and white.</summary>
		internal static string FormatReadout(LevelsAdjustment levels) =>
			string.Create(
				CultureInfo.InvariantCulture,
				$"Input {ToByte(levels.InputBlack)} / {levels.Gamma:0.00} / {ToByte(levels.InputWhite)}   Output {ToByte(levels.OutputBlack)} / {ToByte(levels.OutputWhite)}");

		private static int ToByte(float value) => (int)MathF.Round(value * 255.0f);

		private static void DrawRamp(Vector2 rampMin, Vector2 rampMax)
		{
			// Literal black and white rather than style colours: the ramp depicts output luminance,
			// which does not change with the theme.
			ImDrawListPtr drawList = ImGui.GetWindowDrawList();
			drawList.AddRectFilledMultiColor(rampMin, rampMax, RampBlack, RampWhite, RampWhite, RampBlack);
			drawList.AddRect(rampMin, rampMax, ImGui.GetColorU32(ImGuiCol.Border), 0.0f, ImDrawFlags.None, 1.0f);
		}
	}
}
