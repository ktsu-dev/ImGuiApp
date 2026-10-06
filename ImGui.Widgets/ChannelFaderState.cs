// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;

public static partial class ImGuiWidgets
{
	/// <summary>
	/// The drag behind <see cref="ChannelFader"/>: one handle on a 0..1 travel, and the grab offset
	/// that keeps a press on the cap from jumping it.
	/// </summary>
	/// <remarks>
	/// Free of ImGui, like the <see cref="HandleTrackState"/> it drives. The widget converts the
	/// mouse to a travel position before calling in.
	/// </remarks>
	internal sealed class ChannelFaderState
	{
		private readonly HandleTrackState track = new();

		/// <summary>Gets a value indicating whether a drag is in progress: true between a press on the fader track and <see cref="Release"/>.</summary>
		public bool IsDragging { get; private set; }

		/// <summary>Gets the handle position minus the pointer position at press time; 0 when the press jumped.</summary>
		public float GrabOffset { get; private set; }

		/// <summary>
		/// Starts a drag. A press within <paramref name="grabHalfExtent"/> of the handle keeps the
		/// offset; any other press jumps.
		/// </summary>
		/// <param name="handlePosition">Where the handle sits, as a travel position.</param>
		/// <param name="pointerPosition">Where the pointer pressed, as a travel position.</param>
		/// <param name="grabHalfExtent">Half the cap's height, as a travel distance.</param>
		public void Press(float handlePosition, float pointerPosition, float grabHalfExtent)
		{
			GrabOffset = MathF.Abs(pointerPosition - handlePosition) <= grabHalfExtent
				? handlePosition - pointerPosition
				: 0f;

			track.Activate([handlePosition], handlePosition);
			IsDragging = true;
		}

		/// <summary>
		/// Moves the single handle in <paramref name="position"/> (length 1) to pointer + <see cref="GrabOffset"/>,
		/// clamped to [0, 1].
		/// </summary>
		/// <param name="position">The handle, as a one-element span, moved in place.</param>
		/// <param name="pointerPosition">Where the pointer is now, as a travel position.</param>
		/// <returns><see langword="true"/> if the handle moved; otherwise <see langword="false"/>.</returns>
		public bool Drag(Span<float> position, float pointerPosition) =>
			IsDragging && track.Drag(position, pointerPosition + GrabOffset, 0f, 1f, 0f);

		/// <summary>Ends the drag.</summary>
		public void Release()
		{
			IsDragging = false;
			GrabOffset = 0f;
			track.Release();
		}
	}
}
