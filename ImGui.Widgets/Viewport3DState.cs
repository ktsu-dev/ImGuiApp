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
	/// An orbit camera: where it is looking, how far away, and which way round.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Deliberately free of ImGui, like <see cref="HandleTrackState"/> and
	/// <see cref="CurveTrackState"/>. The widget converts a mouse drag into the deltas these
	/// methods take, which is what lets every rule here be tested without a graphics context.
	/// </para>
	/// <para>
	/// The camera is spherical about <see cref="Target"/>: <see cref="Yaw"/> turns about the world
	/// up axis and <see cref="Pitch"/> raises above the horizon, so at yaw and pitch of zero the
	/// eye sits on the +Z side looking towards −Z, which is where a reader expects a default camera
	/// to be.
	/// </para>
	/// </remarks>
	public sealed class Viewport3DState
	{
		/// <summary>
		/// How close to straight up or down the pitch may get, in radians.
		/// </summary>
		/// <remarks>
		/// <para>
		/// The margin is the point of this, and what it prevents is not the NaN it looks like.
		/// The float nearest π/2 is slightly <em>above</em> π/2, so a pitch of exactly
		/// <c>(float)(Math.PI / 2.0)</c> puts the eye a fraction past the pole: measured, the cosine
		/// comes out at −4.4e-8 rather than zero, the camera basis mirrors, and a point on the
		/// camera's right renders on its left with nothing non-finite anywhere to give it away.
		/// </para>
		/// <para>
		/// Stopping a thousandth of a radian short holds the cosine at 1e-3 — four orders of
		/// magnitude clear of the rounding that decides that sign — and is about a sixteenth of a
		/// degree, far too small to see. An outright NaN needs the eye exactly over the target,
		/// which no float pitch reaches; a zero <see cref="MinimumDistance"/> does, which is why
		/// that clamp exists too.
		/// </para>
		/// </remarks>
		public const float PitchLimitRadians = (float)(Math.PI / 2.0) - 0.001f;

		/// <summary>The smallest distance the camera may sit from its target.</summary>
		/// <remarks>
		/// Zero puts the eye on the target, and the view matrix is then NaN throughout — measured,
		/// not assumed. This is the degeneracy <see cref="PitchLimitRadians"/> is often assumed to
		/// be guarding against and is not; the two clamps fail differently and both are needed.
		/// </remarks>
		public const float MinimumDistance = 1e-4f;

		/// <summary>Gets or sets the point the camera orbits and looks at.</summary>
		public Vector3 Target { get; set; }

		/// <summary>Gets or sets how far the eye sits from <see cref="Target"/>.</summary>
		/// <remarks>Clamped to at least <see cref="MinimumDistance"/>.</remarks>
		public float Distance
		{
			get;
			set => field = MathF.Max(MinimumDistance, value);
		} = 5f;

		/// <summary>Gets or sets the rotation about the world up axis, in radians.</summary>
		/// <remarks>Not wrapped. A caller that has orbited ten times round reads a large number, which is the honest answer and costs nothing.</remarks>
		public float Yaw { get; set; }

		/// <summary>Gets or sets the elevation above the horizon, in radians.</summary>
		/// <remarks>Clamped to ±<see cref="PitchLimitRadians"/> on every write, not only on <see cref="Orbit"/>.</remarks>
		public float Pitch
		{
			get;
			set => field = Math.Clamp(value, -PitchLimitRadians, PitchLimitRadians);
		}

		/// <summary>Gets or sets the vertical field of view, in radians.</summary>
		/// <exception cref="ArgumentOutOfRangeException">Not strictly between zero and π.</exception>
		public float FieldOfView
		{
			get;
			set
			{
				ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(value, 0f);
				ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(value, (float)Math.PI);
				field = value;
			}
		} = (float)(Math.PI / 4.0);

		/// <summary>Gets or sets the near clip distance.</summary>
		/// <exception cref="ArgumentOutOfRangeException">Not positive.</exception>
		public float NearPlane
		{
			get;
			set
			{
				ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(value, 0f);
				field = value;
			}
		} = 0.1f;

		/// <summary>Gets or sets the far clip distance.</summary>
		/// <exception cref="ArgumentOutOfRangeException">Not positive.</exception>
		public float FarPlane
		{
			get;
			set
			{
				ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(value, 0f);
				field = value;
			}
		} = 1000f;

		/// <summary>Gets or sets the colour the viewport is cleared to, straight alpha in [0, 1].</summary>
		public Vector4 ClearColor { get; set; } = new(0.1f, 0.1f, 0.12f, 1f);

		/// <summary>Gets the eye position the current orientation puts the camera at.</summary>
		public Vector3 Eye
		{
			get
			{
				float cosPitch = MathF.Cos(Pitch);

				return Target + (Distance * new Vector3(
					cosPitch * MathF.Sin(Yaw),
					MathF.Sin(Pitch),
					cosPitch * MathF.Cos(Yaw)));
			}
		}

		/// <summary>Turns the camera about its target.</summary>
		/// <param name="yawRadians">How far to turn about the world up axis.</param>
		/// <param name="pitchRadians">How far to raise, clamped short of the poles.</param>
		public void Orbit(float yawRadians, float pitchRadians)
		{
			Yaw += yawRadians;
			Pitch += pitchRadians;
		}

		/// <summary>
		/// Slides the target across the camera's own plane.
		/// </summary>
		/// <param name="right">How far to move along the camera's right axis, in world units per unit of distance.</param>
		/// <param name="up">How far to move along the camera's up axis, in the same units.</param>
		/// <remarks>
		/// Scaled by <see cref="Distance"/>, so a drag of a given number of pixels moves the scene
		/// by the same fraction of the view whether the camera is close in or far out. Panning that
		/// ignores distance feels glued at one zoom and useless at every other.
		/// </remarks>
		public void Pan(float right, float up)
		{
			Vector3 forward = Vector3.Normalize(Target - Eye);
			Vector3 rightAxis = Vector3.Normalize(Vector3.Cross(forward, Vector3.UnitY));
			Vector3 upAxis = Vector3.Cross(rightAxis, forward);

			Target += Distance * ((rightAxis * right) + (upAxis * up));
		}

		/// <summary>
		/// Moves the eye towards or away from the target.
		/// </summary>
		/// <param name="amount">Positive moves closer, negative further away.</param>
		/// <remarks>
		/// Multiplicative rather than additive, so a wheel notch changes the view by the same
		/// proportion at every scale. Additive dollying crawls when far out and overshoots through
		/// the target when close in.
		/// </remarks>
		public void Dolly(float amount) => Distance *= MathF.Exp(-amount);

		/// <summary>
		/// The matrix taking a model-space position to clip space.
		/// </summary>
		/// <param name="aspect">Viewport width divided by height.</param>
		/// <returns>The combined view and projection.</returns>
		/// <exception cref="ArgumentOutOfRangeException">The aspect ratio is not positive.</exception>
		/// <remarks>
		/// View times projection, in that order, because <c>Vector4.Transform</c> treats the vector
		/// as a row — which is what <c>IRenderer3D</c> implementations consume. Writing the product
		/// the other way round yields a matrix that is not merely wrong but silently plausible.
		/// </remarks>
		public Matrix4x4 ViewProjection(float aspect)
		{
			ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(aspect, 0f);

			return Matrix4x4.CreateLookAt(Eye, Target, Vector3.UnitY)
				* Matrix4x4.CreatePerspectiveFieldOfView(FieldOfView, aspect, NearPlane, FarPlane);
		}
	}
}
