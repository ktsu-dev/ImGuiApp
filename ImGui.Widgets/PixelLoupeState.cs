// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System.Numerics;

public static partial class ImGuiWidgets
{
	/// <summary>
	/// The pixel a <see cref="PixelLoupe"/> is inspecting, and the rules for picking it, with no ImGui
	/// dependency. Bounds are half-open and taken against the floored image size, so a non-integer size
	/// never admits a pixel the image does not have.
	/// </summary>
	internal sealed class PixelLoupeState
	{
		/// <summary>The largest radius the loupe will draw.</summary>
		internal const int MaxRadius = 32;

		/// <summary>The cell size used when the caller passes NaN.</summary>
		internal const float DefaultCellSize = 12f;

		/// <summary>Gets a value indicating whether a pixel has been inspected since the image last lost it.</summary>
		public bool HasPixel { get; private set; }

		/// <summary>Gets the inspected pixel's x, or -1 when there is none.</summary>
		public int X { get; private set; } = -1;

		/// <summary>Gets the inspected pixel's y, or -1 when there is none.</summary>
		public int Y { get; private set; } = -1;

		/// <summary>Finds the image pixel containing a point.</summary>
		/// <param name="imagePoint">A position in image pixels.</param>
		/// <param name="imageSize">Native image size in pixels.</param>
		/// <param name="x">The pixel's x, or -1 when the point is outside the image.</param>
		/// <param name="y">The pixel's y, or -1 when the point is outside the image.</param>
		/// <returns>True if the point lies on a pixel of the image.</returns>
		public static bool TryPixelAt(Vector2 imagePoint, Vector2 imageSize, out int x, out int y)
		{
			x = -1;
			y = -1;
			if (!float.IsFinite(imagePoint.X) || !float.IsFinite(imagePoint.Y))
			{
				return false;
			}

			float floorX = MathF.Floor(imagePoint.X);
			float floorY = MathF.Floor(imagePoint.Y);
			if (!IsInside(floorX, floorY, imageSize))
			{
				return false;
			}

			x = (int)floorX;
			y = (int)floorY;
			return true;
		}

		/// <summary>Takes the pointer's position for this frame.</summary>
		/// <param name="pointerOverCanvas">Whether the pointer is over the canvas.</param>
		/// <param name="imagePoint">The pointer's position in image pixels.</param>
		/// <param name="imageSize">Native image size in pixels.</param>
		/// <returns>True if the pointer is over an image pixel, which is then the inspected one.</returns>
		/// <remarks>
		/// Otherwise the last pixel is kept, unless the image no longer has it, in which case it is
		/// forgotten.
		/// </remarks>
		public bool Update(bool pointerOverCanvas, Vector2 imagePoint, Vector2 imageSize)
		{
			if (pointerOverCanvas && TryPixelAt(imagePoint, imageSize, out int x, out int y))
			{
				X = x;
				Y = y;
				HasPixel = true;
				return true;
			}

			if (HasPixel && !InImage(X, Y, imageSize))
			{
				X = -1;
				Y = -1;
				HasPixel = false;
			}

			return false;
		}

		/// <summary>Clamps a radius to what the loupe will draw.</summary>
		/// <param name="radius">The requested radius.</param>
		/// <returns>The radius, clamped to [0, 32].</returns>
		public static int ClampRadius(int radius) => Math.Clamp(radius, 0, MaxRadius);

		/// <summary>Clamps a cell size to what the loupe will draw.</summary>
		/// <param name="cellSize">The requested on-screen size of one magnified pixel.</param>
		/// <returns>12 for NaN, otherwise the size clamped to [2, 64].</returns>
		public static float ClampCellSize(float cellSize) =>
			float.IsNaN(cellSize) ? DefaultCellSize : Math.Clamp(cellSize, 2f, 64f);

		/// <summary>Whether a pixel belongs to the image.</summary>
		/// <param name="x">The pixel's x.</param>
		/// <param name="y">The pixel's y.</param>
		/// <param name="imageSize">Native image size in pixels.</param>
		/// <returns>True if 0 ≤ x &lt; floor(width) and 0 ≤ y &lt; floor(height).</returns>
		public static bool InImage(int x, int y, Vector2 imageSize) => IsInside(x, y, imageSize);

		/// <summary>
		/// The bounds test in floating point, so a point is tested before it is converted to an integer
		/// that could not hold it.
		/// </summary>
		private static bool IsInside(float x, float y, Vector2 imageSize)
		{
			// Written so NaN fails every comparison and so is outside.
			float width = MathF.Floor(imageSize.X);
			float height = MathF.Floor(imageSize.Y);
			return x >= 0f && x < width && y >= 0f && y < height;
		}
	}
}
