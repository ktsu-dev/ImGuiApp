// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System.Numerics;

public static partial class ImGuiWidgets
{
	/// <summary>
	/// The drag target and layout rules behind <see cref="ImageCompare"/>, with no ImGui dependency. All
	/// x values are relative to the canvas's left edge.
	/// </summary>
	internal sealed class ImageCompareState
	{
		/// <summary>What a press on the canvas is dragging.</summary>
		internal enum DragTarget
		{
			/// <summary>Nothing is pressed.</summary>
			None,

			/// <summary>The press started on the wipe divider and moves it.</summary>
			Divider,

			/// <summary>The press started anywhere else and pans the view.</summary>
			Pan,
		}

		/// <summary>How far either side of the divider, in pixels, a press still grabs it.</summary>
		public const float DividerGrabRadius = 6f;

		/// <summary>Space between the two panes in side-by-side mode, in pixels.</summary>
		public const float SideBySideGap = 4f;

		/// <summary>Gets what the current press is dragging.</summary>
		public DragTarget Target { get; private set; }

		/// <summary>Starts a press: the divider when it is within reach in wipe mode, otherwise a pan.</summary>
		/// <param name="pointerX">Pointer x relative to the canvas.</param>
		/// <param name="dividerX">Divider x relative to the canvas.</param>
		/// <param name="mode">The widget's mode; side by side has no divider, so every press pans.</param>
		public void Press(float pointerX, float dividerX, ImageCompareMode mode) =>
			Target = mode == ImageCompareMode.Wipe && HitsDivider(pointerX, dividerX) ? DragTarget.Divider : DragTarget.Pan;

		/// <summary>Ends the press.</summary>
		public void Release() => Target = DragTarget.None;

		/// <summary>Brings a split into range: NaN becomes one half, anything else is clamped to [0, 1].</summary>
		/// <param name="split">The split to normalize.</param>
		/// <returns>The normalized split.</returns>
		public static float NormalizeSplit(float split) => float.IsNaN(split) ? 0.5f : Math.Clamp(split, 0f, 1f);

		/// <summary>The split that puts the divider under the pointer.</summary>
		/// <param name="pointerX">Pointer x relative to the canvas.</param>
		/// <param name="width">Canvas width in pixels.</param>
		/// <returns>The pointer's fraction of the width, clamped to [0, 1]; one half for a non-positive width or a NaN pointer.</returns>
		public static float SplitFromPointer(float pointerX, float width)
		{
			if (!(width > 0f) || float.IsNaN(pointerX))
			{
				return 0.5f;
			}

			return Math.Clamp(pointerX / width, 0f, 1f);
		}

		/// <summary>Whether a pointer is close enough to the divider to grab it.</summary>
		/// <param name="pointerX">Pointer x relative to the canvas.</param>
		/// <param name="dividerX">Divider x relative to the canvas.</param>
		/// <returns>True within <see cref="DividerGrabRadius"/>, inclusive.</returns>
		public static bool HitsDivider(float pointerX, float dividerX) => MathF.Abs(pointerX - dividerX) <= DividerGrabRadius;

		/// <summary>Lays out the two side-by-side panes: equal halves with <see cref="SideBySideGap"/> between them.</summary>
		/// <param name="canvasSize">The whole widget's size in pixels.</param>
		/// <returns>Each pane's origin relative to the canvas, and the size both share, at least 1 pixel wide.</returns>
		public static (Vector2 LeftOrigin, Vector2 RightOrigin, Vector2 PaneSize) SideBySideLayout(Vector2 canvasSize)
		{
			Vector2 paneSize = new(MathF.Max(1f, (canvasSize.X - SideBySideGap) / 2f), canvasSize.Y);
			return (Vector2.Zero, new Vector2(paneSize.X + SideBySideGap, 0f), paneSize);
		}
	}
}
