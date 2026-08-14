// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

using HexaFlameGraph = Hexa.NET.ImGui.Widgets.ImGuiWidgetFlameGraph;

/// <summary>
/// One bar in a flame graph.
/// </summary>
/// <param name="Start">Start position of the bar on the horizontal axis.</param>
/// <param name="End">End position of the bar on the horizontal axis.</param>
/// <param name="Level">Stack depth of the bar; zero is the bottom row.</param>
/// <param name="Caption">Text drawn inside the bar.</param>
public readonly record struct FlameGraphSample(float Start, float End, byte Level, string Caption);

/// <summary>
/// Optional presentation settings for <see cref="ImGuiWidgets.FlameGraph"/>.
/// </summary>
public sealed record FlameGraphOptions
{
	/// <summary>
	/// Gets a value indicating whether the graph grows downward instead of upward.
	/// </summary>
	public bool Flip { get; init; }

	/// <summary>
	/// Gets the text drawn over the graph, or <see langword="null"/> for none.
	/// </summary>
	public string? OverlayText { get; init; }

	/// <summary>
	/// Gets the lower bound of the horizontal axis, or <see cref="float.MaxValue"/> to fit the data.
	/// </summary>
	public float ScaleMin { get; init; } = float.MaxValue;

	/// <summary>
	/// Gets the upper bound of the horizontal axis, or <see cref="float.MaxValue"/> to fit the data.
	/// </summary>
	public float ScaleMax { get; init; } = float.MaxValue;

	/// <summary>
	/// Gets the size of the graph in pixels, or <see langword="default"/> to fill the content region.
	/// </summary>
	public Vector2 GraphSize { get; init; }
}

public static partial class ImGuiWidgets
{
	/// <summary>
	/// Flattens every sample caption into one null-terminated UTF-8 block.
	/// </summary>
	/// <remarks>
	/// A single block keeps the per-frame cost to one allocation rather than one per sample.
	/// Offsets are byte offsets, not character offsets, so multi-byte captions stay correct.
	/// </remarks>
	/// <param name="samples">The samples whose captions are being flattened.</param>
	/// <returns>The encoded block and the byte offset at which each sample's caption begins.</returns>
	internal static (byte[] Bytes, int[] Offsets) FlattenCaptions(IReadOnlyList<FlameGraphSample> samples)
	{
		int count = samples.Count;
		if (count == 0)
		{
			return ([], []);
		}

		int[] offsets = new int[count];
		int total = 0;
		for (int i = 0; i < count; i++)
		{
			offsets[i] = total;
			total += Encoding.UTF8.GetByteCount(samples[i].Caption ?? string.Empty) + 1;
		}

		byte[] bytes = new byte[total];
		for (int i = 0; i < count; i++)
		{
			string caption = samples[i].Caption ?? string.Empty;
			int written = Encoding.UTF8.GetBytes(caption, 0, caption.Length, bytes, offsets[i]);
			bytes[offsets[i] + written] = 0;
		}

		return (bytes, offsets);
	}

	/// <summary>
	/// Clamps a selection index to a valid sample index, or to -1 when nothing is selectable.
	/// </summary>
	/// <param name="selected">The requested selection index.</param>
	/// <param name="sampleCount">The number of samples available.</param>
	/// <returns>The requested index when it is in range, otherwise -1.</returns>
	internal static int ResolveFlameGraphSelection(int selected, int sampleCount) =>
		selected >= 0 && selected < sampleCount ? selected : -1;

	/// <summary>
	/// Draws a flame graph of hierarchical timing samples.
	/// </summary>
	/// <param name="label">Label for display and identity.</param>
	/// <param name="samples">The bars to plot.</param>
	/// <param name="selected">Index of the selected bar, updated in place when a bar is clicked; -1 for none.</param>
	/// <param name="options">Optional presentation settings, or <see langword="null"/> for defaults.</param>
	/// <exception cref="ArgumentNullException"><paramref name="label"/> or <paramref name="samples"/> is <see langword="null"/>.</exception>
	[SuppressMessage("Major Code Smell", "S6640:Make sure that using \"unsafe\" is safe here", Justification = "Required to satisfy Hexa's pull-based callback; all pinned memory is freed before the method returns.")]
	public static unsafe void FlameGraph(string label, IReadOnlyList<FlameGraphSample> samples, ref int selected, FlameGraphOptions? options = null)
	{
		Ensure.NotNull(label);
		Ensure.NotNull(samples);

		FlameGraphOptions resolvedOptions = options ?? new FlameGraphOptions();
		selected = ResolveFlameGraphSelection(selected, samples.Count);

		(byte[] captionBytes, int[] captionOffsets) = FlattenCaptions(samples);

		GCHandle captionHandle = GCHandle.Alloc(captionBytes, GCHandleType.Pinned);
		try
		{
			// Passing captionBase as PlotFlame's `data` argument (rather than closing over it
			// directly) is required: a byte* cannot be hoisted into the closure class that a
			// delegate conversion generates, so the local function below captures only the
			// managed `samples` list and `captionOffsets` array, and recovers the pointer from
			// `data` inside the callback.
			void* captionData = captionHandle.AddrOfPinnedObject().ToPointer();

			void Getter(float* start, float* end, byte* level, byte** caption, void* data, int idx)
			{
				FlameGraphSample sample = samples[idx];
				*start = sample.Start;
				*end = sample.End;
				*level = sample.Level;
				byte* captionBase = (byte*)data;
				*caption = captionBase + captionOffsets[idx];
			}

			HexaFlameGraph.PlotFlame(
				label,
				Getter,
				captionData,
				samples.Count,
				ref selected,
				resolvedOptions.Flip,
				0,
				resolvedOptions.OverlayText,
				resolvedOptions.ScaleMin,
				resolvedOptions.ScaleMax,
				resolvedOptions.GraphSize);
		}
		finally
		{
			captionHandle.Free();
		}
	}
}
