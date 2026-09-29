// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System.Numerics;

public static partial class ImGuiWidgets
{
	/// <summary>
	/// The geometry and drag rules behind <see cref="CropOverlay"/>, with no ImGui dependency. Every point
	/// and size is in image pixels; the two pixel constants are screen pixels, divided by the zoom
	/// wherever they are compared with image distances.
	/// </summary>
	/// <remarks>
	/// Hit-testing and resizing happen in the crop's own unrotated frame: a point is moved there by
	/// subtracting the centre and rotating by the negative angle, and results are rotated back. A drag is
	/// always derived from the crop at press and the total pointer travel, so per-frame rounding cannot
	/// accumulate into drift.
	/// </remarks>
	internal sealed class CropOverlayState
	{
		/// <summary>What a press on the overlay is dragging.</summary>
		internal enum CropHandle
		{
			/// <summary>Nothing is pressed.</summary>
			None,

			/// <summary>The inside of the crop, which moves it.</summary>
			Body,

			/// <summary>The left edge.</summary>
			Left,

			/// <summary>The right edge.</summary>
			Right,

			/// <summary>The top edge.</summary>
			Top,

			/// <summary>The bottom edge.</summary>
			Bottom,

			/// <summary>The top-left corner.</summary>
			TopLeft,

			/// <summary>The top-right corner.</summary>
			TopRight,

			/// <summary>The bottom-left corner.</summary>
			BottomLeft,

			/// <summary>The bottom-right corner.</summary>
			BottomRight,

			/// <summary>The rotate handle above the top edge.</summary>
			Rotate,

			/// <summary>Anywhere outside the crop, which pans the canvas underneath.</summary>
			Pan,
		}

		/// <summary>The smallest crop width or height, in image pixels.</summary>
		public const float MinSize = 1f;

		/// <summary>The largest rotation either way, in degrees.</summary>
		public const float MaxAngle = 45f;

		/// <summary>How close, in screen pixels, a press has to be to grab a handle.</summary>
		public const float HandleGrabPixels = 8f;

		/// <summary>How far above the top edge, in screen pixels, the rotate handle sits.</summary>
		public const float RotateHandleOffsetPixels = 24f;

		/// <summary>How far outside the image, in image pixels, a corner may round without the crop leaving it.</summary>
		private const float InsideTolerance = 1e-3f;

		/// <summary>How many times a rejected drag is halved back towards the last accepted pointer.</summary>
		private const int BisectionSteps = 16;

		private CropRect pressCrop;
		private Vector2 pressPoint;
		private Vector2 lastAccepted;
		private float aspect;

		/// <summary>Gets the handle the current press is dragging.</summary>
		public CropHandle Active { get; private set; }

		/// <summary>
		/// Gets or sets how much of the current pan drag, in screen pixels, has already been handed to the
		/// canvas. The widget pans by the total drag less this, so the travel under ImGui's drag threshold
		/// reaches the canvas once the drag starts rather than being dropped.
		/// </summary>
		public Vector2 PanApplied { get; set; }

		/// <summary>Brings a crop inside the image, applying the aspect lock and the rotation limits.</summary>
		/// <param name="crop">The crop to normalize.</param>
		/// <param name="imageSize">Native image size in pixels; each component must be positive.</param>
		/// <param name="aspectRatio">Width / height to lock to; 0, negative or not finite means free.</param>
		/// <param name="allowRotation">When false the angle is forced to 0.</param>
		/// <returns>The normalized crop, equal to <paramref name="crop"/> when it was already valid.</returns>
		public static CropRect Normalize(CropRect crop, Vector2 imageSize, float aspectRatio, bool allowRotation)
		{
			float ratio = SanitizeAspect(aspectRatio);

			if (!IsFinite(crop.Center) || !IsFinite(crop.Size) || !float.IsFinite(crop.AngleDegrees) || crop.Size.X <= 0f || crop.Size.Y <= 0f)
			{
				return LargestCentred(imageSize, ratio);
			}

			float angle = allowRotation ? Math.Clamp(crop.AngleDegrees, -MaxAngle, MaxAngle) : 0f;

			Vector2 size = new(
				Math.Clamp(crop.Size.X, MinSize, imageSize.X),
				Math.Clamp(crop.Size.Y, MinSize, imageSize.Y));

			if (ratio > 0f)
			{
				float height = size.X / ratio;
				size = height > imageSize.Y ? new Vector2(size.Y * ratio, size.Y) : new Vector2(size.X, height);
			}

			Vector2 bounds = RotatedBounds(size, angle);
			if (bounds.X > imageSize.X || bounds.Y > imageSize.Y)
			{
				size *= MathF.Min(imageSize.X / bounds.X, imageSize.Y / bounds.Y);
				bounds = RotatedBounds(size, angle);
			}

			Vector2 center = new(
				ClampCentre(crop.Center.X, bounds.X, imageSize.X),
				ClampCentre(crop.Center.Y, bounds.Y, imageSize.Y));

			return new CropRect(center, size, angle);
		}

		/// <summary>Reports whether every rotated corner of a crop lies inside the image.</summary>
		/// <param name="crop">The crop to test.</param>
		/// <param name="imageSize">Native image size in pixels.</param>
		/// <returns>True when all four corners are within the image, to a tolerance of 1e-3.</returns>
		public static bool IsInside(CropRect crop, Vector2 imageSize)
		{
			foreach (Vector2 corner in crop.Corners())
			{
				if (corner.X < -InsideTolerance || corner.Y < -InsideTolerance || corner.X > imageSize.X + InsideTolerance || corner.Y > imageSize.Y + InsideTolerance)
				{
					return false;
				}
			}

			return true;
		}

		/// <summary>Finds the handle under a point: rotate, then corners, then edges, then the body, else pan.</summary>
		/// <param name="crop">The crop being hit-tested.</param>
		/// <param name="imagePoint">The pointer in image pixels.</param>
		/// <param name="zoom">Screen pixels per image pixel, which converts the grab radius.</param>
		/// <param name="allowRotation">Whether the rotate handle exists.</param>
		/// <returns>The first handle within reach.</returns>
		public static CropHandle HitTest(CropRect crop, Vector2 imagePoint, float zoom, bool allowRotation)
		{
			float scale = zoom > 0f && float.IsFinite(zoom) ? zoom : 1f;
			float reach = HandleGrabPixels / scale;
			Vector2 local = ToLocal(crop, imagePoint);
			Vector2 half = crop.Size / 2f;

			if (allowRotation && Vector2.Distance(local, new Vector2(0f, -half.Y - (RotateHandleOffsetPixels / scale))) <= reach)
			{
				return CropHandle.Rotate;
			}

			if (Vector2.Distance(local, new Vector2(-half.X, -half.Y)) <= reach)
			{
				return CropHandle.TopLeft;
			}

			if (Vector2.Distance(local, new Vector2(half.X, -half.Y)) <= reach)
			{
				return CropHandle.TopRight;
			}

			if (Vector2.Distance(local, new Vector2(-half.X, half.Y)) <= reach)
			{
				return CropHandle.BottomLeft;
			}

			if (Vector2.Distance(local, half) <= reach)
			{
				return CropHandle.BottomRight;
			}

			bool alongVertical = MathF.Abs(local.Y) <= half.Y;
			bool alongHorizontal = MathF.Abs(local.X) <= half.X;

			if (alongVertical && MathF.Abs(local.X + half.X) <= reach)
			{
				return CropHandle.Left;
			}

			if (alongVertical && MathF.Abs(local.X - half.X) <= reach)
			{
				return CropHandle.Right;
			}

			if (alongHorizontal && MathF.Abs(local.Y + half.Y) <= reach)
			{
				return CropHandle.Top;
			}

			if (alongHorizontal && MathF.Abs(local.Y - half.Y) <= reach)
			{
				return CropHandle.Bottom;
			}

			return MathF.Abs(local.X) < half.X && MathF.Abs(local.Y) < half.Y ? CropHandle.Body : CropHandle.Pan;
		}

		/// <summary>Starts a press on a handle.</summary>
		/// <param name="handle">The handle pressed, usually from <see cref="HitTest"/>.</param>
		/// <param name="crop">The crop at press.</param>
		/// <param name="imagePoint">The pointer at press, in image pixels.</param>
		public void Begin(CropHandle handle, CropRect crop, Vector2 imagePoint)
		{
			Active = handle;
			pressCrop = crop;
			pressPoint = imagePoint;
			lastAccepted = imagePoint;
			PanApplied = Vector2.Zero;
		}

		/// <summary>Moves the active handle to a pointer position.</summary>
		/// <param name="imagePoint">The pointer now, in image pixels.</param>
		/// <param name="imageSize">Native image size in pixels.</param>
		/// <param name="aspectRatio">Width / height to lock to; 0, negative or not finite means free.</param>
		/// <returns>
		/// The crop for this pointer. When that would leave the image, the furthest crop towards it that
		/// still fits, so the crop stops flush with the edge. A pan, or no press, returns the crop at press.
		/// </returns>
		public CropRect Update(Vector2 imagePoint, Vector2 imageSize, float aspectRatio)
		{
			if (Active is CropHandle.None or CropHandle.Pan)
			{
				return pressCrop;
			}

			aspect = SanitizeAspect(aspectRatio);

			CropRect candidate = Candidate(imagePoint);
			if (IsInside(candidate, imageSize))
			{
				lastAccepted = imagePoint;
				return candidate;
			}

			Vector2 valid = lastAccepted;
			Vector2 invalid = imagePoint;
			CropRect best = Candidate(valid);
			for (int i = 0; i < BisectionSteps; i++)
			{
				Vector2 middle = (valid + invalid) / 2f;
				CropRect trial = Candidate(middle);
				if (IsInside(trial, imageSize))
				{
					valid = middle;
					best = trial;
				}
				else
				{
					invalid = middle;
				}
			}

			lastAccepted = valid;
			return best;
		}

		/// <summary>Ends the press.</summary>
		public void End()
		{
			Active = CropHandle.None;
			PanApplied = Vector2.Zero;
		}

		/// <summary>Rotates a vector clockwise (on a y-down screen) by an angle in degrees.</summary>
		/// <param name="vector">The vector to rotate.</param>
		/// <param name="degrees">The clockwise angle.</param>
		/// <returns>The rotated vector.</returns>
		internal static Vector2 Rotate(Vector2 vector, float degrees)
		{
			float radians = degrees * (MathF.PI / 180f);
			(float sin, float cos) = MathF.SinCos(radians);
			return new Vector2((vector.X * cos) - (vector.Y * sin), (vector.X * sin) + (vector.Y * cos));
		}

		/// <summary>Maps an image point into the crop's unrotated frame, centred on the crop.</summary>
		/// <param name="crop">The crop whose frame to use.</param>
		/// <param name="imagePoint">The point in image pixels.</param>
		/// <returns>The point relative to the crop's centre, with the crop's rotation undone.</returns>
		internal static Vector2 ToLocal(CropRect crop, Vector2 imagePoint) => Rotate(imagePoint - crop.Center, -crop.AngleDegrees);

		private CropRect Candidate(Vector2 imagePoint)
		{
			Vector2 offset = imagePoint - pressPoint;

			return Active switch
			{
				CropHandle.Body => pressCrop with { Center = pressCrop.Center + offset },
				CropHandle.Rotate => pressCrop with { AngleDegrees = AngleFromPointer(imagePoint - pressCrop.Center) },
				CropHandle.Left or CropHandle.Right or CropHandle.Top or CropHandle.Bottom => DragEdge(Rotate(offset, -pressCrop.AngleDegrees)),
				_ => DragCorner(Rotate(offset, -pressCrop.AngleDegrees)),
			};
		}

		private CropRect DragEdge(Vector2 localOffset)
		{
			Vector2 half = pressCrop.Size / 2f;
			Vector2 size = pressCrop.Size;
			Vector2 centre;

			if (Active is CropHandle.Left or CropHandle.Right)
			{
				float fixedX = Active == CropHandle.Right ? -half.X : half.X;
				float movingX = Active == CropHandle.Right
					? MathF.Max(half.X + localOffset.X, fixedX + MinSize)
					: MathF.Min(-half.X + localOffset.X, fixedX - MinSize);
				size.X = MathF.Abs(movingX - fixedX);
				if (aspect > 0f)
				{
					size.Y = size.X / aspect;
				}

				centre = new Vector2((movingX + fixedX) / 2f, 0f);
			}
			else
			{
				float fixedY = Active == CropHandle.Bottom ? -half.Y : half.Y;
				float movingY = Active == CropHandle.Bottom
					? MathF.Max(half.Y + localOffset.Y, fixedY + MinSize)
					: MathF.Min(-half.Y + localOffset.Y, fixedY - MinSize);
				size.Y = MathF.Abs(movingY - fixedY);
				if (aspect > 0f)
				{
					size.X = size.Y * aspect;
				}

				centre = new Vector2(0f, (movingY + fixedY) / 2f);
			}

			return pressCrop with { Center = pressCrop.Center + Rotate(centre, pressCrop.AngleDegrees), Size = size };
		}

		private CropRect DragCorner(Vector2 localOffset)
		{
			Vector2 half = pressCrop.Size / 2f;

			// The direction from the fixed corner to the dragged one, per axis.
			Vector2 direction = Active switch
			{
				CropHandle.TopLeft => new Vector2(-1f, -1f),
				CropHandle.TopRight => new Vector2(1f, -1f),
				CropHandle.BottomLeft => new Vector2(-1f, 1f),
				_ => new Vector2(1f, 1f),
			};

			Vector2 fixedCorner = -direction * half;
			Vector2 moving = (direction * half) + localOffset;
			Vector2 size = new(
				MathF.Max((moving.X - fixedCorner.X) * direction.X, MinSize),
				MathF.Max((moving.Y - fixedCorner.Y) * direction.Y, MinSize));

			if (aspect > 0f)
			{
				float width = MathF.Max(size.X, size.Y * aspect);
				size = new Vector2(width, width / aspect);
			}

			Vector2 centre = fixedCorner + (direction * size / 2f);
			return pressCrop with { Center = pressCrop.Center + Rotate(centre, pressCrop.AngleDegrees), Size = size };
		}

		private static float AngleFromPointer(Vector2 fromCentre)
		{
			float degrees = (MathF.Atan2(fromCentre.Y, fromCentre.X) * (180f / MathF.PI)) + 90f;

			// Atan2 plus a quarter turn spans -90..270; fold it back so a pointer below-left reads as a
			// small anticlockwise turn rather than a large clockwise one.
			if (degrees > 180f)
			{
				degrees -= 360f;
			}

			return Math.Clamp(degrees, -MaxAngle, MaxAngle);
		}

		private static CropRect LargestCentred(Vector2 imageSize, float ratio)
		{
			CropRect full = CropRect.FullImage(imageSize);
			if (ratio <= 0f)
			{
				return full;
			}

			Vector2 size = imageSize.X / ratio <= imageSize.Y
				? new Vector2(imageSize.X, imageSize.X / ratio)
				: new Vector2(imageSize.Y * ratio, imageSize.Y);
			return full with { Size = size };
		}

		private static Vector2 RotatedBounds(Vector2 size, float angle)
		{
			float radians = angle * (MathF.PI / 180f);
			float cos = MathF.Abs(MathF.Cos(radians));
			float sin = MathF.Abs(MathF.Sin(radians));
			return new Vector2((size.X * cos) + (size.Y * sin), (size.X * sin) + (size.Y * cos));
		}

		private static float ClampCentre(float centre, float extent, float imageExtent)
		{
			float half = extent / 2f;
			return half >= imageExtent - half ? imageExtent / 2f : Math.Clamp(centre, half, imageExtent - half);
		}

		private static float SanitizeAspect(float aspectRatio) => aspectRatio > 0f && float.IsFinite(aspectRatio) ? aspectRatio : 0f;

		private static bool IsFinite(Vector2 value) => float.IsFinite(value.X) && float.IsFinite(value.Y);
	}
}
