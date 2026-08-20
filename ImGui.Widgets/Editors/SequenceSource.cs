// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System.Collections.Generic;

using ktsu.Semantics.Color;

/// <summary>
/// One clip on a sequencer track.
/// </summary>
/// <param name="Start">Frame the clip starts on.</param>
/// <param name="End">Frame the clip ends on.</param>
/// <param name="TypeIndex">Index into the source's type names.</param>
/// <param name="Color">Color the clip is drawn in.</param>
public sealed record SequenceItem(int Start, int End, int TypeIndex, Srgb Color);

public static partial class ImGuiWidgets
{
	/// <summary>
	/// A clip's frame range, laid out so its two fields sit at a stable address while the
	/// sequencer edits them in place.
	/// </summary>
	internal struct SequenceRange
	{
		/// <summary>
		/// Frame the clip starts on.
		/// </summary>
		public int Start;

		/// <summary>
		/// Frame the clip ends on.
		/// </summary>
		public int End;
	}

	/// <summary>
	/// Supplies clips to the sequencer widget and receives edits. Subclass it and override the
	/// members describing your timeline.
	/// </summary>
	public abstract class SequenceSource
	{
		/// <summary>
		/// Gets the first frame on the timeline.
		/// </summary>
		public abstract int FrameMin { get; }

		/// <summary>
		/// Gets the last frame on the timeline.
		/// </summary>
		public abstract int FrameMax { get; }

		/// <summary>
		/// Gets how many clips the timeline holds.
		/// </summary>
		public abstract int ItemCount { get; }

		/// <summary>
		/// Gets a clip.
		/// </summary>
		/// <param name="index">Index of the clip.</param>
		/// <returns>The clip.</returns>
		public abstract SequenceItem GetItem(int index);

		/// <summary>
		/// Applies a range edit the user made by dragging.
		/// </summary>
		/// <param name="index">Index of the clip that moved.</param>
		/// <param name="start">Its new start frame.</param>
		/// <param name="endFrame">Its new end frame.</param>
		public abstract void SetItemRange(int index, int start, int endFrame);

		/// <summary>
		/// Gets the label drawn on a clip.
		/// </summary>
		/// <param name="index">Index of the clip.</param>
		/// <returns>The label.</returns>
		public virtual string GetItemLabel(int index) => string.Empty;

		/// <summary>
		/// Gets the clip types the user may add.
		/// </summary>
		public virtual IReadOnlyList<string> ItemTypeNames => [];

		/// <summary>
		/// Adds a clip of the given type.
		/// </summary>
		/// <param name="typeIndex">Index into <see cref="ItemTypeNames"/>.</param>
		public virtual void AddItem(int typeIndex)
		{
		}

		/// <summary>
		/// Deletes a clip.
		/// </summary>
		/// <param name="index">Index of the clip.</param>
		public virtual void DeleteItem(int index)
		{
		}

		/// <summary>
		/// Duplicates a clip.
		/// </summary>
		/// <param name="index">Index of the clip.</param>
		public virtual void DuplicateItem(int index)
		{
		}

		/// <summary>
		/// Copies the current selection.
		/// </summary>
		public virtual void Copy()
		{
		}

		/// <summary>
		/// Pastes the copied selection.
		/// </summary>
		public virtual void Paste()
		{
		}

		/// <summary>
		/// Gets extra vertical space to reserve under a clip, for custom content.
		/// </summary>
		/// <param name="index">Index of the clip.</param>
		/// <returns>Height in pixels.</returns>
		public virtual int GetCustomHeight(int index) => 0;

		/// <summary>
		/// Called when a clip is double-clicked.
		/// </summary>
		/// <param name="index">Index of the clip.</param>
		public virtual void DoubleClick(int index)
		{
		}

		/// <summary>
		/// Called before a drag begins, so the source can open an undo scope.
		/// </summary>
		/// <param name="index">Index of the clip being edited.</param>
		public virtual void BeginEdit(int index)
		{
		}

		/// <summary>
		/// Called after a drag ends, so the source can close its undo scope.
		/// </summary>
		public virtual void EndEdit()
		{
		}
	}

	/// <summary>
	/// Copies every clip's frame range out of a source into a buffer.
	/// </summary>
	/// <param name="source">The source to read.</param>
	/// <param name="ranges">
	/// Buffer to fill. Its length need not match the source's item count exactly: if it is longer,
	/// the extra entries are left untouched; if it is shorter, only the entries that fit are filled.
	/// </param>
	/// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
	internal static void FillRanges(SequenceSource source, Span<SequenceRange> ranges)
	{
		Ensure.NotNull(source);

		// The buffer may legitimately be longer than the source's item count (a caller reusing a
		// fixed-capacity buffer across frames rather than reallocating as clips are added), so the
		// fill is bounded by whichever is smaller — never index source.GetItem past ItemCount.
		int count = Math.Min(ranges.Length, source.ItemCount);

		for (int i = 0; i < count; i++)
		{
			SequenceItem item = source.GetItem(i);
			ranges[i].Start = item.Start;
			ranges[i].End = item.End;
		}
	}

	/// <summary>
	/// Finds which clips the sequencer moved.
	/// </summary>
	/// <param name="before">Ranges as they were before the sequencer ran.</param>
	/// <param name="after">Ranges as they are afterwards.</param>
	/// <returns>One entry per changed clip, in index order.</returns>
	/// <remarks>
	/// The sequencer edits ranges in place through pointers, so a diff is the only way to learn
	/// which clips actually moved. Reporting an unchanged clip would fire a spurious edit;
	/// missing a changed one would silently discard the user's drag.
	/// </remarks>
	internal static List<(int Index, int Start, int End)> ComputeRangeEdits(ReadOnlySpan<SequenceRange> before, ReadOnlySpan<SequenceRange> after)
	{
		List<(int Index, int Start, int End)> edits = [];

		// before and after are two snapshots of the same buffer taken immediately around a single
		// call, so they are only ever different lengths because of a caller bug, not a real edit.
		// Truncating to the shorter span keeps this helper crash-free without inventing edits for
		// entries that only exist on one side.
		int count = Math.Min(before.Length, after.Length);

		for (int i = 0; i < count; i++)
		{
			if (before[i].Start != after[i].Start || before[i].End != after[i].End)
			{
				edits.Add((i, after[i].Start, after[i].End));
			}
		}

		return edits;
	}
}
