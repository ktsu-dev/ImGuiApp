// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;
using System.Numerics;

/// <summary>
/// Provides custom ImGui widgets.
/// </summary>
public static partial class ImGuiWidgets
{
	/// <summary>
	/// The interaction behind one colour-balance trackball: where a value sits on the disk, and
	/// how a drag moves it.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Deliberately free of ImGui. Positions are in <em>disk units</em>: the wheel's centre is the
	/// origin, its rim is at length one, and y runs upward. The widget converts the mouse into
	/// those units before calling in, which is what lets every rule here be tested without a
	/// graphics context.
	/// </para>
	/// <para>
	/// The drag is relative, as a trackball's is: pressing anywhere on the wheel grabs the handle
	/// where it already is, and moving the pointer moves the handle by the same amount. An
	/// absolute drag, as <see cref="XYPad"/> uses, would make every press a jump, and a grade is
	/// something adjusted by a hair from where it is far more often than it is set from scratch.
	/// </para>
	/// </remarks>
	internal sealed class ColorWheelState
	{
		/// <summary>The screen angle, in degrees counter-clockwise from the right, at which red is drawn.</summary>
		/// <remarks>
		/// Where a vectorscope puts its red target. Laying the wheel out the same way means a push
		/// on the wheel moves the trace on the scope in the direction of the push.
		/// </remarks>
		public const float RedAngle = 103.0f;

		/// <summary>How much of the pointer's movement reaches the handle while fine adjustment is held.</summary>
		public const float FineScale = 0.25f;

		private Vector2 anchorPosition;
		private Vector2 anchorPointer;
		private bool anchorFine;

		private float masterAnchorValue;
		private float masterAnchorPointer;
		private bool masterAnchorFine;

		/// <summary>Gets a value indicating whether a drag of the trackball is in progress.</summary>
		public bool IsDragging { get; private set; }

		/// <summary>Gets a value indicating whether a drag of the master slider is in progress.</summary>
		public bool IsDraggingMaster { get; private set; }

		/// <summary>Returns where on the disk a value is drawn, in disk units.</summary>
		public static Vector2 ToDisk(ColorWheelValue value)
		{
			float strength = Math.Clamp(float.IsFinite(value.Strength) ? value.Strength : 0.0f, 0.0f, 1.0f);
			float angle = (ColorWheelValue.WrapHue(value.Hue) + RedAngle) * (MathF.PI / 180.0f);
			return new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * strength;
		}

		/// <summary>Returns the hue and strength a position on the disk stands for, with a master of zero.</summary>
		/// <param name="position">The position in disk units. Anything past the rim is read as the rim.</param>
		/// <param name="previousHue">The hue to keep when <paramref name="position"/> is the centre, which has none.</param>
		public static ColorWheelValue FromDisk(Vector2 position, float previousHue)
		{
			float length = position.Length();

			// The centre has no direction. Keeping the hue the wheel already had is what stops a
			// handle dragged through the middle from reporting a sudden swing to red.
			if (!float.IsFinite(length) || length <= 1e-6f)
			{
				return new ColorWheelValue(ColorWheelValue.WrapHue(previousHue), 0.0f);
			}

			float angle = MathF.Atan2(position.Y, position.X) * (180.0f / MathF.PI);
			return new ColorWheelValue(ColorWheelValue.WrapHue(angle - RedAngle), MathF.Min(length, 1.0f));
		}

		/// <summary>Starts a drag from where <paramref name="value"/> already sits.</summary>
		/// <param name="value">The wheel's value when the press landed.</param>
		/// <param name="pointer">The pointer, in disk units.</param>
		/// <param name="fine">Whether fine adjustment is held.</param>
		public void Begin(ColorWheelValue value, Vector2 pointer, bool fine)
		{
			Anchor(ToDisk(value), pointer, fine);
			IsDragging = true;
		}

		/// <summary>Moves the handle by how far the pointer has moved since the drag began.</summary>
		/// <param name="current">The wheel's value now.</param>
		/// <param name="pointer">The pointer, in disk units.</param>
		/// <param name="fine">Whether fine adjustment is held.</param>
		/// <returns>The new value, or <paramref name="current"/> unchanged when no drag is in progress.</returns>
		public ColorWheelValue Drag(ColorWheelValue current, Vector2 pointer, bool fine)
		{
			if (!IsDragging)
			{
				return current;
			}

			// Toggling fine adjustment mid-drag re-anchors where the handle is now. Scaling the whole
			// distance travelled so far by the new factor would throw the handle a long way on the
			// frame the modifier changed.
			if (fine != anchorFine)
			{
				Anchor(ToDisk(current), pointer, fine);
			}

			Vector2 position = anchorPosition + ((pointer - anchorPointer) * (fine ? FineScale : 1.0f));

			// Past the rim, pin the handle to it and re-anchor there. Without that the overshoot is
			// remembered, and a drag back inward does nothing until the pointer has retraced all of
			// it — the rim would feel sticky.
			if (position.LengthSquared() > 1.0f)
			{
				position = Vector2.Normalize(position);
				Anchor(position, pointer, fine);
			}

			// A pointer held still must hand back the value it was given, not the same position
			// round-tripped through an angle: consumers turn changes into undo entries, and a
			// last-bit difference in the hue would mint one every frame the button is held.
			return Vector2.DistanceSquared(position, ToDisk(current)) <= 1e-10f
				? current
				: FromDisk(position, current.Hue) with { Master = current.Master };
		}

		/// <summary>Ends the drag.</summary>
		public void End() => IsDragging = false;

		/// <summary>Starts a drag of the master slider from where it already sits.</summary>
		/// <param name="master">The master value when the press landed.</param>
		/// <param name="pointer">
		/// The pointer in <em>bar units</em>: the slider's centre is zero and its ends are minus and
		/// plus one, so sweeping the bar's whole width sweeps the whole range.
		/// </param>
		/// <param name="fine">Whether fine adjustment is held.</param>
		public void BeginMaster(float master, float pointer, bool fine)
		{
			AnchorMaster(ClampMaster(master), pointer, fine);
			IsDraggingMaster = true;
		}

		/// <summary>Moves the master slider by how far the pointer has moved since the drag began.</summary>
		/// <param name="current">The master value now.</param>
		/// <param name="pointer">The pointer, in bar units.</param>
		/// <param name="fine">Whether fine adjustment is held.</param>
		/// <returns>The new master value, or <paramref name="current"/> unchanged when no drag is in progress.</returns>
		/// <remarks>Relative, re-anchored when fine adjustment toggles, and re-anchored at either end, for the same reasons as <see cref="Drag"/>.</remarks>
		public float DragMaster(float current, float pointer, bool fine)
		{
			if (!IsDraggingMaster)
			{
				return current;
			}

			if (fine != masterAnchorFine)
			{
				AnchorMaster(ClampMaster(current), pointer, fine);
			}

			float master = masterAnchorValue + ((pointer - masterAnchorPointer) * (fine ? FineScale : 1.0f));
			if (master is < -1.0f or > 1.0f)
			{
				master = Math.Clamp(master, -1.0f, 1.0f);
				AnchorMaster(master, pointer, fine);
			}

			return MathF.Abs(master - current) <= ColorWheelValue.MasterTolerance ? current : master;
		}

		/// <summary>Ends the master slider's drag.</summary>
		public void EndMaster() => IsDraggingMaster = false;

		private static float ClampMaster(float master) => float.IsFinite(master) ? Math.Clamp(master, -1.0f, 1.0f) : 0.0f;

		private void AnchorMaster(float value, float pointer, bool fine)
		{
			masterAnchorValue = value;
			masterAnchorPointer = pointer;
			masterAnchorFine = fine;
		}

		private void Anchor(Vector2 position, Vector2 pointer, bool fine)
		{
			anchorPosition = position;
			anchorPointer = pointer;
			anchorFine = fine;
		}
	}
}
