// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System.Numerics;

public static partial class ImGuiWidgets
{
	/// <summary>
	/// Pan and zoom state for an image canvas, independent of any rendering.
	/// </summary>
	/// <remarks>
	/// Pan is expressed as an offset in viewport pixels applied to the image's centerd position, so a
	/// pan of zero always means "centerd", whatever the zoom.
	/// </remarks>
	public sealed class ImageCanvasState
	{
		/// <summary>Gets the current zoom factor, where 1.0 is one image pixel per viewport pixel.</summary>
		public float Zoom { get; private set; } = 1f;

		/// <summary>Gets the current pan offset in viewport pixels.</summary>
		public Vector2 Pan { get; private set; }

		/// <summary>Gets or sets the smallest permitted zoom factor.</summary>
		public float MinZoom { get; init; } = 0.01f;

		/// <summary>Gets or sets the largest permitted zoom factor.</summary>
		public float MaxZoom { get; init; } = 64f;

		/// <summary>Scales the image to fit entirely within the viewport and centers it.</summary>
		/// <param name="imageSize">Native image size in pixels.</param>
		/// <param name="viewportSize">Viewport size in pixels.</param>
		public void FitToViewport(Vector2 imageSize, Vector2 viewportSize)
		{
			if (imageSize.X <= 0f || imageSize.Y <= 0f)
			{
				return;
			}

			float scale = Math.Min(viewportSize.X / imageSize.X, viewportSize.Y / imageSize.Y);
			Zoom = Math.Clamp(scale, MinZoom, MaxZoom);
			Pan = Vector2.Zero;
		}

		/// <summary>Sets zoom to 1:1 and recenters.</summary>
		public void ResetToActualSize()
		{
			Zoom = Math.Clamp(1f, MinZoom, MaxZoom);
			Pan = Vector2.Zero;
		}

		/// <summary>Translates the view by a delta in viewport pixels.</summary>
		/// <param name="delta">Translation in viewport pixels.</param>
		public void PanBy(Vector2 delta) => Pan += delta;

		/// <summary>
		/// Multiplies the zoom by <paramref name="factor"/> while holding the image point under
		/// <paramref name="anchor"/> stationary.
		/// </summary>
		/// <param name="factor">Relative zoom change; greater than one zooms in.</param>
		/// <param name="anchor">Anchor position in viewport pixels, relative to the viewport's top-left.</param>
		/// <param name="viewportSize">Viewport size in pixels.</param>
		public void ZoomAt(float factor, Vector2 anchor, Vector2 viewportSize)
		{
			float newZoom = Math.Clamp(Zoom * factor, MinZoom, MaxZoom);
			float applied = newZoom / Zoom;

			// Keep the anchor fixed: the vector from the image center to the anchor scales with the zoom.
			Vector2 center = (viewportSize * 0.5f) + Pan;
			Vector2 centerToAnchor = anchor - center;
			Pan += centerToAnchor - (centerToAnchor * applied);

			Zoom = newZoom;
		}

		/// <summary>Gets the image's axis-aligned bounds in viewport pixels.</summary>
		/// <param name="imageSize">Native image size in pixels.</param>
		/// <param name="viewportSize">Viewport size in pixels.</param>
		/// <returns>The image bounds in viewport pixels.</returns>
		public (Vector2 Min, Vector2 Max) ImageRectInViewport(Vector2 imageSize, Vector2 viewportSize)
		{
			Vector2 scaled = imageSize * Zoom;
			Vector2 min = (viewportSize * 0.5f) + Pan - (scaled * 0.5f);
			return (min, min + scaled);
		}

		/// <summary>Converts a viewport position to image pixel coordinates.</summary>
		/// <param name="viewportPoint">Position in viewport pixels, relative to the viewport's top-left.</param>
		/// <param name="imageSize">Native image size in pixels.</param>
		/// <param name="viewportSize">Viewport size in pixels.</param>
		/// <returns>The corresponding position in image pixels.</returns>
		public Vector2 ViewportToImage(Vector2 viewportPoint, Vector2 imageSize, Vector2 viewportSize)
		{
			(Vector2 min, _) = ImageRectInViewport(imageSize, viewportSize);
			return (viewportPoint - min) / Zoom;
		}
	}
}
