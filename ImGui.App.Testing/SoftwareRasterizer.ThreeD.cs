// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Testing;

using System;
using System.Numerics;

/// <summary>
/// The 3D half of the CPU rasterizer: the same textured-triangle fill with a matrix, a depth
/// buffer, near-plane clipping and perspective-correct interpolation.
/// </summary>
/// <remarks>
/// <para>
/// In its own file rather than beside the 2D fill because the two pipelines differ in their input
/// space — the 2D one takes target pixels, this one takes clip space — and because the 2D fill is
/// on the hot path of every headless frame in the repository and should not grow branches it never
/// takes.
/// </para>
/// <para>
/// <strong>Near-plane clipping is not optional and is the part most likely to be left out.</strong>
/// A vertex behind the eye has a non-positive w, and dividing by it sends the vertex to the wrong
/// side of the screen — so a triangle straddling the eye is drawn as a wildly wrong shape spanning
/// the target rather than as the partial triangle it is. The polygon is clipped against w &gt;= a
/// small epsilon before any divide happens.
/// </para>
/// </remarks>
public static partial class SoftwareRasterizer
{
	/// <summary>A vertex after the model-view-projection matrix and before the perspective divide.</summary>
	/// <param name="ClipPosition">Homogeneous clip-space position.</param>
	/// <param name="Uv">Texture coordinate, normalized.</param>
	/// <param name="Color">Vertex color, straight alpha.</param>
	public readonly record struct ClipVertex(Vector4 ClipPosition, Vector2 Uv, Rgba32 Color);

	/// <summary>Everything below this w is behind the eye, or close enough that the divide is worthless.</summary>
	private const float NearEpsilon = 1e-5f;

	/// <summary>
	/// Transforms, clips, projects and fills one triangle.
	/// </summary>
	/// <param name="target">The color attachment.</param>
	/// <param name="depth">The depth attachment, or null for a target created without one.</param>
	/// <param name="a">First vertex, in clip space.</param>
	/// <param name="b">Second vertex, in clip space.</param>
	/// <param name="c">Third vertex, in clip space.</param>
	/// <param name="texture">Texture to modulate by, or null for vertex color alone.</param>
	/// <param name="state">The draw state. <see cref="DrawState3D.ModelViewProjection"/> is not read here; the caller has already applied it.</param>
	public static void FillTriangle3D(
		Bitmap32 target,
		DepthBuffer? depth,
		in ClipVertex a,
		in ClipVertex b,
		in ClipVertex c,
		TextureSource? texture,
		in DrawState3D state)
	{
		Ensure.NotNull(target);

		Span<ClipVertex> polygon = stackalloc ClipVertex[4];
		int count = ClipAgainstNearPlane(a, b, c, polygon);

		// A triangle clipped by one plane is a triangle or a quad, so at most one extra triangle.
		for (int i = 2; i < count; i++)
		{
			FillProjected(target, depth, polygon[0], polygon[i - 1], polygon[i], texture, state);
		}
	}

	/// <summary>
	/// Transforms, clips, projects and draws one line segment, one pixel wide.
	/// </summary>
	/// <param name="target">The color attachment.</param>
	/// <param name="depth">The depth attachment, or null.</param>
	/// <param name="a">First endpoint, in clip space.</param>
	/// <param name="b">Second endpoint, in clip space.</param>
	/// <param name="state">The draw state.</param>
	/// <remarks>
	/// Lines carry no texture: a one-pixel line has nowhere to put a texture coordinate that would
	/// mean anything, and every consumer asking for <see cref="PrimitiveTopology.LineList"/> wants
	/// a wireframe or an axis marker.
	/// </remarks>
	public static void DrawLine3D(
		Bitmap32 target,
		DepthBuffer? depth,
		in ClipVertex a,
		in ClipVertex b,
		in DrawState3D state)
	{
		Ensure.NotNull(target);

		ClipVertex near = a;
		ClipVertex far = b;

		if (near.ClipPosition.W < NearEpsilon && far.ClipPosition.W < NearEpsilon)
		{
			return;
		}

		if (near.ClipPosition.W < NearEpsilon)
		{
			near = Lerp(near, far, IntersectionTowards(near, far));
		}
		else if (far.ClipPosition.W < NearEpsilon)
		{
			far = Lerp(far, near, IntersectionTowards(far, near));
		}

		(Vector3 p0, float invW0) = Project(near.ClipPosition, target);
		(Vector3 p1, float invW1) = Project(far.ClipPosition, target);

		int steps = (int)MathF.Ceiling(MathF.Max(MathF.Abs(p1.X - p0.X), MathF.Abs(p1.Y - p0.Y)));

		for (int i = 0; i <= steps; i++)
		{
			float t = steps == 0 ? 0f : (float)i / steps;
			int x = (int)MathF.Round(p0.X + ((p1.X - p0.X) * t));
			int y = (int)MathF.Round(p0.Y + ((p1.Y - p0.Y) * t));

			if (x < 0 || y < 0 || x >= target.Width || y >= target.Height)
			{
				continue;
			}

			float z = p0.Z + ((p1.Z - p0.Z) * t);

			if (!DepthTestAndWrite(depth, x, y, z, state.Depth))
			{
				continue;
			}

			// Perspective-correct colour: the attribute over w interpolates linearly in screen
			// space, 1/w does too, and the quotient is the attribute.
			float invW = invW0 + ((invW1 - invW0) * t);
			float weight = invW <= 0f ? 0.5f : invW1 * t / invW;

			Write(target, x, y, Interpolate2(near.Color, far.Color, weight), state.Blend);
		}
	}

	private static void FillProjected(
		Bitmap32 target,
		DepthBuffer? depth,
		in ClipVertex a,
		in ClipVertex b,
		in ClipVertex c,
		TextureSource? texture,
		in DrawState3D state)
	{
		(Vector3 s0, float invW0) = Project(a.ClipPosition, target);
		(Vector3 s1, float invW1) = Project(b.ClipPosition, target);
		(Vector3 s2, float invW2) = Project(c.ClipPosition, target);

		float signedArea = ((s1.X - s0.X) * (s2.Y - s0.Y)) - ((s1.Y - s0.Y) * (s2.X - s0.X));

		if (MathF.Abs(signedArea) < 1e-7f)
		{
			return;
		}

		// The y axis points down in target space, so a counter-clockwise winding in the usual
		// y-up sense comes out with a negative signed area here. Front-facing is therefore
		// negative, and getting this backwards would cull exactly the geometry meant to be drawn —
		// which is why CullTests pins a single triangle's visibility under both modes.
		bool frontFacing = signedArea < 0f;

		if ((state.Cull == CullMode.Back && !frontFacing) || (state.Cull == CullMode.Front && frontFacing))
		{
			return;
		}

		int minX = Math.Max(0, (int)MathF.Floor(Min3(s0.X, s1.X, s2.X)));
		int minY = Math.Max(0, (int)MathF.Floor(Min3(s0.Y, s1.Y, s2.Y)));
		int maxX = Math.Min(target.Width, (int)MathF.Ceiling(Max3(s0.X, s1.X, s2.X)));
		int maxY = Math.Min(target.Height, (int)MathF.Ceiling(Max3(s0.Y, s1.Y, s2.Y)));

		if (minX >= maxX || minY >= maxY)
		{
			return;
		}

		float area = signedArea;

		for (int y = minY; y < maxY; y++)
		{
			for (int x = minX; x < maxX; x++)
			{
				Vector2 p = new(x + 0.5f, y + 0.5f);

				float w0 = EdgeXY(s1, s2, p) / area;
				float w1 = EdgeXY(s2, s0, p) / area;
				float w2 = EdgeXY(s0, s1, p) / area;

				if (w0 < 0f || w1 < 0f || w2 < 0f)
				{
					continue;
				}

				// Depth is already a projective quantity, so it interpolates linearly in screen
				// space and must not be perspective-corrected. The attributes below must.
				float z = (s0.Z * w0) + (s1.Z * w1) + (s2.Z * w2);

				if (!DepthTestAndWrite(depth, x, y, z, state.Depth))
				{
					continue;
				}

				float invW = (invW0 * w0) + (invW1 * w1) + (invW2 * w2);

				if (invW <= 0f)
				{
					continue;
				}

				float c0 = invW0 * w0 / invW;
				float c1 = invW1 * w1 / invW;
				float c2 = invW2 * w2 / invW;

				Rgba32 source = Interpolate(a.Color, b.Color, c.Color, c0, c1, c2);

				if (texture is not null)
				{
					Vector2 uv = (a.Uv * c0) + (b.Uv * c1) + (c.Uv * c2);
					source = Modulate(source, texture.Sample(uv.X, uv.Y));
				}

				Write(target, x, y, source, state.Blend);
			}
		}
	}

	/// <summary>
	/// Clips a triangle against the near plane, producing a triangle or a quad.
	/// </summary>
	/// <param name="a">First vertex.</param>
	/// <param name="b">Second vertex.</param>
	/// <param name="c">Third vertex.</param>
	/// <param name="output">Receives the clipped polygon. Must hold at least four vertices.</param>
	/// <returns>How many vertices were written: zero, three or four.</returns>
	private static int ClipAgainstNearPlane(in ClipVertex a, in ClipVertex b, in ClipVertex c, Span<ClipVertex> output)
	{
		Span<ClipVertex> input = [a, b, c];
		int count = 0;

		for (int i = 0; i < 3; i++)
		{
			ClipVertex current = input[i];
			ClipVertex next = input[(i + 1) % 3];
			bool currentInside = current.ClipPosition.W >= NearEpsilon;
			bool nextInside = next.ClipPosition.W >= NearEpsilon;

			if (currentInside)
			{
				output[count++] = current;
			}

			if (currentInside != nextInside)
			{
				ClipVertex outside = currentInside ? next : current;
				ClipVertex inside = currentInside ? current : next;

				output[count++] = Lerp(outside, inside, IntersectionTowards(outside, inside));
			}
		}

		return count < 3 ? 0 : count;
	}

	/// <summary>How far from an outside vertex towards an inside one the near plane sits.</summary>
	/// <param name="outside">The vertex with too small a w.</param>
	/// <param name="inside">The vertex with an acceptable w.</param>
	/// <returns>A fraction in [0, 1].</returns>
	private static float IntersectionTowards(in ClipVertex outside, in ClipVertex inside) =>
		(NearEpsilon - outside.ClipPosition.W) / (inside.ClipPosition.W - outside.ClipPosition.W);

	private static ClipVertex Lerp(in ClipVertex from, in ClipVertex to, float t) => new(
		Vector4.Lerp(from.ClipPosition, to.ClipPosition, t),
		Vector2.Lerp(from.Uv, to.Uv, t),
		Interpolate2(from.Color, to.Color, t));

	/// <summary>Perspective divide and viewport transform.</summary>
	/// <param name="clip">The clip-space position.</param>
	/// <param name="target">The target, for its dimensions.</param>
	/// <returns>The position in target pixels with depth in [0, 1], and 1/w.</returns>
	private static (Vector3 Screen, float InverseW) Project(Vector4 clip, Bitmap32 target)
	{
		float invW = 1f / clip.W;
		float ndcX = clip.X * invW;
		float ndcY = clip.Y * invW;
		float ndcZ = clip.Z * invW;

		// y is negated: normalized device coordinates point up, target rows go down.
		return (
			new Vector3(
				(ndcX + 1f) * 0.5f * target.Width,
				(1f - ndcY) * 0.5f * target.Height,
				ndcZ),
			invW);
	}

	private static bool DepthTestAndWrite(DepthBuffer? depth, int x, int y, float z, DepthMode mode)
	{
		if (depth is null || mode == DepthMode.None)
		{
			return true;
		}

		if (z >= depth[x, y])
		{
			return false;
		}

		if (mode == DepthMode.TestAndWrite)
		{
			depth.Depths[(y * depth.Width) + x] = z;
		}

		return true;
	}

	private static void Write(Bitmap32 target, int x, int y, Rgba32 source, BlendMode blend)
	{
		Rgba32 final = blend == BlendMode.Alpha && source.A != 255
			? BlendOver(source, target.GetPixel(x, y))
			: source;

		target.SetPixel(x, y, final);
	}

	private static float EdgeXY(Vector3 a, Vector3 b, Vector2 p) =>
		((b.X - a.X) * (p.Y - a.Y)) - ((b.Y - a.Y) * (p.X - a.X));

	private static Rgba32 Interpolate2(Rgba32 a, Rgba32 b, float t) => Interpolate(a, b, b, 1f - t, t, 0f);
}
