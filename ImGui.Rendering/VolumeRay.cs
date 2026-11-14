// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Rendering;

using System;
using System.Numerics;

/// <summary>
/// The arithmetic of <see cref="IRenderer3D.DrawVolume"/> for one pixel, which every renderer's
/// implementation must match, so a test can state the expected value without a graphics context.
/// </summary>
/// <remarks>
/// A pixel's ray runs from the near plane to the far plane: both points come from the inverse of the
/// view-projection, taking normalized device coordinates with y up and the target's first row at the
/// top, and depth 0 at the near plane and 1 at the far one, as
/// <see cref="Matrix4x4.CreatePerspectiveFieldOfView"/> produces. It is carried into the volume's unit
/// cube by the inverse of <see cref="VolumeDrawState.VolumeToWorld"/> and clipped to the cube.
/// </remarks>
public sealed class VolumeRay
{
	private readonly Matrix4x4 clipToWorld;
	private readonly Matrix4x4 worldToVolume;
	private readonly VolumeDrawState state;

	private VolumeRay(Matrix4x4 clipToWorld, Matrix4x4 worldToVolume, VolumeDrawState state)
	{
		this.clipToWorld = clipToWorld;
		this.worldToVolume = worldToVolume;
		this.state = state;
	}

	/// <summary>Gets whether both matrices were invertible; a ray from one that was not integrates to zero.</summary>
	public bool IsValid { get; init; }

	/// <summary>Prepares the per-pass inverses.</summary>
	/// <param name="state">The pass.</param>
	/// <returns>The ray builder.</returns>
	public static VolumeRay Create(in VolumeDrawState state)
	{
		bool clip = Matrix4x4.Invert(state.ViewProjection, out Matrix4x4 clipToWorld);
		bool volume = Matrix4x4.Invert(state.VolumeToWorld, out Matrix4x4 worldToVolume);
		return new VolumeRay(clipToWorld, worldToVolume, state) { IsValid = clip && volume };
	}

	/// <summary>
	/// A per-pixel offset in [0, 1) for where the ray's samples start within their step, so a fixed step
	/// does not band. Interleaved gradient noise, which the shader computes identically.
	/// </summary>
	/// <param name="x">Column.</param>
	/// <param name="y">Row.</param>
	/// <returns>The offset.</returns>
	public static float Jitter(int x, int y)
	{
		float a = 0.06711056f * (x + 0.5f);
		float b = 0.00583715f * (y + 0.5f);
		float inner = (a + b) % 1f;
		float value = 52.9829189f * inner % 1f;
		return value < 0 ? value + 1 : value;
	}

	/// <summary>Integrates the volume's value over world distance along one pixel's ray, with the fade applied.</summary>
	/// <param name="x">Column.</param>
	/// <param name="y">Row, 0 at the top.</param>
	/// <param name="targetWidth">The target's width in pixels.</param>
	/// <param name="targetHeight">The target's height in pixels.</param>
	/// <param name="sample">Samples the volume at texture coordinates in [0, 1]³.</param>
	/// <param name="width">Voxels along X.</param>
	/// <param name="height">Voxels along Y.</param>
	/// <param name="depth">Voxels along Z.</param>
	/// <returns>∫ value · fade · ds, in value × world units.</returns>
	public float Integrate(int x, int y, int targetWidth, int targetHeight, Func<Vector3, float> sample, int width, int height, int depth)
	{
		Ensure.NotNull(sample);
		if (!IsValid)
		{
			return 0f;
		}

		float ndcX = ((x + 0.5f) / targetWidth * 2f) - 1f;
		float ndcY = 1f - ((y + 0.5f) / targetHeight * 2f);
		Vector3 near = Unproject(new Vector4(ndcX, ndcY, 0f, 1f));
		Vector3 far = Unproject(new Vector4(ndcX, ndcY, 1f, 1f));

		Vector3 origin = Vector3.Transform(near, worldToVolume);
		Vector3 direction = Vector3.Transform(far, worldToVolume) - origin;
		if (!ClipToUnitCube(origin, direction, out float enter, out float exit))
		{
			return 0f;
		}

		Vector3 voxels = direction * (exit - enter) * new Vector3(width, height, depth);
		int samples = Math.Clamp((int)MathF.Ceiling(voxels.Length() * state.EffectiveSamplesPerVoxel), 1, state.EffectiveMaxSamples);
		float step = (exit - enter) / samples;
		float worldStep = step * Vector3.Distance(near, far);
		float jitter = Jitter(x, y);

		float sum = 0f;
		for (int i = 0; i < samples; i++)
		{
			float t = enter + ((i + jitter) * step);
			float value = sample(origin + (direction * t));
			if (value != 0f)
			{
				sum += value * Fade(Vector3.Distance(Vector3.Lerp(near, far, t), state.Eye));
			}
		}

		return sum * worldStep;
	}

	/// <summary>The emission weight at a distance from the eye: 0 inside the fade's start, rising smoothly to 1 at its end.</summary>
	/// <param name="distance">The distance.</param>
	/// <returns>The weight.</returns>
	public float Fade(float distance)
	{
		if (state.FadeInEnd <= 0f && state.FadeInStart <= 0f)
		{
			return 1f;
		}

		float t = Math.Clamp((distance - state.FadeInStart) / MathF.Max(state.FadeInEnd - state.FadeInStart, 1e-6f), 0f, 1f);
		return t * t * (3f - (2f * t));
	}

	/// <summary>Where a ray enters and leaves the unit cube, as fractions of its length, clipped to [0, 1].</summary>
	private static bool ClipToUnitCube(Vector3 origin, Vector3 direction, out float enter, out float exit)
	{
		enter = 0f;
		exit = 1f;
		for (int axis = 0; axis < 3; axis++)
		{
			float o = axis == 0 ? origin.X : axis == 1 ? origin.Y : origin.Z;
			float d = axis == 0 ? direction.X : axis == 1 ? direction.Y : direction.Z;
			if (MathF.Abs(d) < 1e-12f)
			{
				if (o is < 0f or > 1f)
				{
					return false;
				}

				continue;
			}

			float t0 = -o / d;
			float t1 = (1f - o) / d;
			enter = MathF.Max(enter, MathF.Min(t0, t1));
			exit = MathF.Min(exit, MathF.Max(t0, t1));
		}

		return exit > enter;
	}

	private Vector3 Unproject(Vector4 clip)
	{
		Vector4 world = Vector4.Transform(clip, clipToWorld);
		return new Vector3(world.X, world.Y, world.Z) / world.W;
	}
}
