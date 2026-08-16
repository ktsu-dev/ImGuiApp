// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System.Diagnostics.CodeAnalysis;
using System.Text;

using ktsu.ImGui.Color;

using HexaSequenceInterface = Hexa.NET.ImGui.Widgets.ImSequencer.SequenceInterface;
using HexaSequencer = Hexa.NET.ImGui.Widgets.ImSequencer.ImSequencer;

public static partial class ImGuiWidgets
{
	/// <summary>
	/// Item counts at or below this are buffered on the stack; larger timelines use a heap array,
	/// which <c>fixed</c> pins just the same.
	/// </summary>
	private const int SequencerStackAllocLimit = 64;

	/// <summary>
	/// Draws an editable timeline.
	/// </summary>
	/// <param name="source">Supplies the clips and receives edits.</param>
	/// <param name="currentFrame">The playhead frame, updated in place.</param>
	/// <param name="expanded">Whether the timeline is expanded, updated in place.</param>
	/// <param name="selectedEntry">Index of the selected clip, updated in place; -1 for none.</param>
	/// <param name="firstFrame">Leftmost visible frame, updated in place.</param>
	/// <param name="features">Which interactions to enable.</param>
	/// <returns><see langword="true"/> if the sequencer reports a change this frame.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
	[SuppressMessage("Major Code Smell", "S6640:Make sure that using \"unsafe\" is safe here", Justification = "Hexa's SequenceInterface.Get hands back int* into caller-owned storage; the buffer is pinned for exactly the duration of the call and the adapter is unbound in a finally.")]
	public static unsafe bool Sequencer(
		SequenceSource source,
		ref int currentFrame,
		ref bool expanded,
		ref int selectedEntry,
		ref int firstFrame,
		SequencerFeatures features = SequencerFeatures.EditAll)
	{
		Ensure.NotNull(source);

		int count = source.ItemCount;
		Span<SequenceRange> ranges = count <= SequencerStackAllocLimit
			? stackalloc SequenceRange[count]
			: new SequenceRange[count];

		FillRanges(source, ranges);

		SequenceRange[] before = ranges.ToArray();
		SequenceAdapter adapter = new(source);
		bool changed;

		fixed (SequenceRange* pinned = ranges)
		{
			adapter.Bind(pinned, count);
			try
			{
				changed = HexaSequencer.Sequencer(
					adapter,
					ref currentFrame,
					ref expanded,
					ref selectedEntry,
					ref firstFrame,
					MapFeatures(features));
			}
			finally
			{
				// Must run even if Sequencer throws: leaving the adapter bound would hold a
				// pointer into a span that is no longer pinned.
				adapter.Unbind();
			}
		}

		foreach ((int index, int start, int end) in ComputeRangeEdits(before, ranges))
		{
			source.SetItemRange(index, start, end);
		}

		return changed;
	}

	/// <summary>
	/// Presents a <see cref="SequenceSource"/> to Hexa's sequencer. Kept private so the vendor base
	/// type never reaches this library's public surface.
	/// </summary>
	/// <param name="source">The source to forward to.</param>
	private sealed unsafe class SequenceAdapter(SequenceSource source) : HexaSequenceInterface
	{
		private SequenceRange* ranges;
		private int count;
		private byte[] labelBuffer = new byte[128];

		/// <summary>
		/// Points the adapter at the pinned range buffer for the duration of one call.
		/// </summary>
		/// <param name="buffer">The pinned buffer.</param>
		/// <param name="length">How many entries it holds.</param>
		internal void Bind(SequenceRange* buffer, int length)
		{
			ranges = buffer;
			count = length;
		}

		/// <summary>
		/// Releases the pinned buffer. Must be called before the pin is released.
		/// </summary>
		internal void Unbind()
		{
			ranges = null;
			count = 0;
		}

		/// <inheritdoc/>
		public override int GetFrameMin() => source.FrameMin;

		/// <inheritdoc/>
		public override int GetFrameMax() => source.FrameMax;

		/// <inheritdoc/>
		public override int GetItemCount() => source.ItemCount;

		/// <inheritdoc/>
		/// <remarks>
		/// Upstream calls this with different subsets of the out-parameters set to null, so every
		/// one is checked before writing. Writing unconditionally dereferences null.
		/// </remarks>
		public override void Get(int index, int** start, int** end, int* type, uint* color)
		{
			if (ranges is null || index < 0 || index >= count)
			{
				return;
			}

			if (start is not null)
			{
				*start = &ranges[index].Start;
			}

			if (end is not null)
			{
				*end = &ranges[index].End;
			}

			if (type is not null || color is not null)
			{
				SequenceItem item = source.GetItem(index);

				if (type is not null)
				{
					*type = item.TypeIndex;
				}

				if (color is not null)
				{
					*color = item.Color.ToImGuiU32();
				}
			}
		}

		/// <inheritdoc/>
		public override ReadOnlySpan<byte> GetItemLabel(int index) => EncodeLabel(source.GetItemLabel(index));

		/// <inheritdoc/>
		public override int GetItemTypeCount() => source.ItemTypeNames.Count;

		/// <inheritdoc/>
		public override ReadOnlySpan<byte> GetItemTypeName(int typeIndex) => EncodeLabel(source.ItemTypeNames[typeIndex]);

		/// <inheritdoc/>
		public override void Add(int type) => source.AddItem(type);

		/// <inheritdoc/>
		public override void Del(int index) => source.DeleteItem(index);

		/// <inheritdoc/>
		public override void Duplicate(int index) => source.DuplicateItem(index);

		/// <inheritdoc/>
		public override void Copy() => source.Copy();

		/// <inheritdoc/>
		public override void Paste() => source.Paste();

		/// <inheritdoc/>
		public override nuint GetCustomHeight(int index) => (nuint)Math.Max(0, source.GetCustomHeight(index));

		/// <inheritdoc/>
		public override void DoubleClick(int index) => source.DoubleClick(index);

		/// <inheritdoc/>
		public override void BeginEdit(int index) => source.BeginEdit(index);

		/// <inheritdoc/>
		public override void EndEdit() => source.EndEdit();

		/// <summary>
		/// Encodes a label as NUL-terminated UTF-8 into a reusable buffer.
		/// </summary>
		/// <param name="value">The label.</param>
		/// <returns>The encoded bytes, including the terminator.</returns>
		/// <remarks>
		/// The returned span is only valid until the next call, which is sufficient because
		/// upstream consumes it immediately.
		/// </remarks>
		private ReadOnlySpan<byte> EncodeLabel(string value)
		{
			int required = Encoding.UTF8.GetByteCount(value) + 1;
			if (labelBuffer.Length < required)
			{
				labelBuffer = new byte[required];
			}

			int written = Encoding.UTF8.GetBytes(value, labelBuffer);
			labelBuffer[written] = 0;
			return labelBuffer.AsSpan(0, written + 1);
		}
	}
}
