// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Phosphor;

using System;
using System.Collections.Generic;
using System.Numerics;
using Hexa.NET.ImGui;
using ktsu.ImGui.Probes;

public static partial class ImGuiPhosphor
{
	/// <summary>Draws an oscilloscope: a graticule with a glowing trace through the samples.</summary>
	/// <param name="label">The scope's identity.</param>
	/// <param name="samples">The samples, spread evenly across the width.</param>
	/// <param name="size">Size of the screen; the available width and a third of it high where a component is zero or less.</param>
	/// <param name="minimum">The value drawn at the bottom edge.</param>
	/// <param name="maximum">The value drawn at the top edge.</param>
	public static void Oscilloscope(string label, ReadOnlySpan<float> samples, Vector2 size = default, float minimum = -1f, float maximum = 1f)
	{
		Ensure.NotNull(label);
		PhosphorPalette palette = PhosphorStyle.Palette;
		Vector2 actual = PhosphorDraw.Resolve(size, 0f);
		if (actual.Y <= 0f)
		{
			actual.Y = MathF.Max(actual.X / 3f, ImGui.GetFrameHeight());
		}

		Vector2 min = ImGui.GetCursorScreenPos();
		Vector2 max = min + actual;
		ImDrawListPtr drawList = ImGui.GetWindowDrawList();
		drawList.AddRectFilled(min, max, PhosphorDraw.U32(palette.Screen));
		DrawGraticule(drawList, min, max, palette);

		float span = maximum - minimum;
		if (samples.Length > 1 && span > 0f)
		{
			drawList.PushClipRect(min, max, true);
			Vector2 previous = Vector2.Zero;
			for (int i = 0; i < samples.Length; i++)
			{
				float x = min.X + (actual.X * i / (samples.Length - 1));
				float t = Math.Clamp((samples[i] - minimum) / span, 0f, 1f);
				Vector2 point = new(x, max.Y - (t * actual.Y));
				if (i > 0)
				{
					PhosphorDraw.GlowLine(drawList, previous, point, palette.Hot, 1.5f);
				}

				previous = point;
			}

			drawList.PopClipRect();
		}

		PhosphorDraw.CornerBrackets(drawList, min, max, MathF.Min(actual.X, actual.Y) / 8f, palette.Normal);
		ImGui.Dummy(actual);
		ImGuiProbes.MarkItem("oscilloscope", label);
	}

	/// <summary>
	/// Draws a circular motion-tracker sweep: range rings, a crosshair, a rotating beam with a fading
	/// wake, and contacts that flare as the beam passes and fade until it comes round again.
	/// </summary>
	/// <param name="label">The sweep's identity.</param>
	/// <param name="radius">Radius of the outer ring; half the available width where zero or less.</param>
	/// <param name="contacts">Contacts to show, or null for none.</param>
	/// <param name="sweepSeconds">Seconds the beam takes to go round once.</param>
	public static void RadarSweep(string label, float radius = 0f, IReadOnlyList<RadarContact>? contacts = null, float sweepSeconds = 3f)
	{
		Ensure.NotNull(label);
		PhosphorPalette palette = PhosphorStyle.Palette;
		float r = radius > 0f ? radius : MathF.Max(ImGui.GetContentRegionAvail().X / 2f, ImGui.GetFrameHeight());
		Vector2 min = ImGui.GetCursorScreenPos();
		Vector2 centre = min + new Vector2(r, r);
		ImDrawListPtr drawList = ImGui.GetWindowDrawList();
		const int ringSegments = 64;

		drawList.AddCircleFilled(centre, r, PhosphorDraw.U32(palette.Screen), ringSegments);
		for (int ring = 1; ring <= 4; ring++)
		{
			drawList.AddCircle(centre, r * ring / 4f, PhosphorDraw.U32(ring == 4 ? palette.Normal : palette.Faint), ringSegments, 1f);
		}

		drawList.AddLine(centre - new Vector2(r, 0f), centre + new Vector2(r, 0f), PhosphorDraw.U32(palette.Faint), 1f);
		drawList.AddLine(centre - new Vector2(0f, r), centre + new Vector2(0f, r), PhosphorDraw.U32(palette.Faint), 1f);

		float period = sweepSeconds > 0f ? sweepSeconds : 3f;
		float beam = (float)(ImGui.GetTime() / period % 1.0) * MathF.Tau;
		const int wakeSteps = 24;
		const float wakeAngle = MathF.PI / 3f;
		for (int i = wakeSteps; i > 0; i--)
		{
			float a0 = beam - (wakeAngle * i / wakeSteps);
			float a1 = beam - (wakeAngle * (i - 1) / wakeSteps);
			float alpha = 0.35f * (1f - ((float)i / wakeSteps));
			drawList.AddTriangleFilled(centre, centre + Polar(a0, r), centre + Polar(a1, r), PhosphorDraw.U32(palette.Bright, alpha));
		}

		PhosphorDraw.GlowLine(drawList, centre, centre + Polar(beam, r), palette.Hot, 1.5f);

		if (contacts is not null)
		{
			float blip = MathF.Max(r / 30f, 2f);
			foreach (RadarContact contact in contacts)
			{
				float since = PositiveAngle(beam - contact.Bearing) / MathF.Tau;
				float alpha = 1f - since;
				Vector2 position = centre + Polar(contact.Bearing, Math.Clamp(contact.Range, 0f, 1f) * r);
				drawList.AddCircleFilled(position, blip * 2f, PhosphorDraw.U32(palette.Hot, alpha * PhosphorDraw.HaloAlpha), 12);
				drawList.AddCircleFilled(position, blip, PhosphorDraw.U32(palette.Hot, alpha), 12);
			}
		}

		AnimationActivity.RequestActiveFrame();
		ImGui.Dummy(new Vector2(r * 2f, r * 2f));
		ImGuiProbes.MarkItem("radar", label);
	}

	/// <summary>
	/// Draws a vector floor grid in perspective, receding to a horizon and scrolling towards the viewer,
	/// the way a navigation display shows a surface rushing past.
	/// </summary>
	/// <param name="label">The grid's identity.</param>
	/// <param name="size">Size of the display; the available width and half of it high where a component is zero or less.</param>
	/// <param name="speed">Grid rows passed per second; zero holds it still.</param>
	public static void VectorGrid(string label, Vector2 size = default, float speed = 1f)
	{
		Ensure.NotNull(label);
		PhosphorPalette palette = PhosphorStyle.Palette;
		Vector2 actual = PhosphorDraw.Resolve(size, 0f);
		if (actual.Y <= 0f)
		{
			actual.Y = MathF.Max(actual.X / 2f, ImGui.GetFrameHeight());
		}

		Vector2 min = ImGui.GetCursorScreenPos();
		Vector2 max = min + actual;
		ImDrawListPtr drawList = ImGui.GetWindowDrawList();
		drawList.AddRectFilled(min, max, PhosphorDraw.U32(palette.Screen));
		drawList.PushClipRect(min, max, true);

		float horizon = min.Y + (actual.Y * 0.35f);
		float centreX = min.X + (actual.X / 2f);
		float floorHeight = max.Y - horizon;
		PhosphorDraw.GlowLine(drawList, new Vector2(min.X, horizon), new Vector2(max.X, horizon), palette.Bright, 1f);

		const int columns = 12;
		for (int i = -columns; i <= columns; i++)
		{
			float bottomX = centreX + (i * actual.X / columns);
			drawList.AddLine(new Vector2(centreX, horizon), new Vector2(bottomX, max.Y), PhosphorDraw.U32(palette.Dim), 1f);
		}

		float offset = (float)(ImGui.GetTime() * speed % 1.0);
		const int rows = 12;
		for (int i = 0; i < rows; i++)
		{
			float depth = i + 1f - offset;
			float y = horizon + (floorHeight / depth);
			if (y > max.Y || y <= horizon)
			{
				continue;
			}

			float fade = Math.Clamp((y - horizon) / floorHeight * 1.5f, 0.15f, 1f);
			drawList.AddLine(new Vector2(min.X, y), new Vector2(max.X, y), PhosphorDraw.U32(palette.Normal, fade), 1f);
		}

		drawList.PopClipRect();
		if (MathF.Abs(speed) > float.Epsilon)
		{
			AnimationActivity.RequestActiveFrame();
		}

		ImGui.Dummy(actual);
		ImGuiProbes.MarkItem("vector-grid", label);
	}

	private static void DrawGraticule(ImDrawListPtr drawList, Vector2 min, Vector2 max, PhosphorPalette palette)
	{
		const int divisions = 8;
		Vector2 size = max - min;
		uint faint = PhosphorDraw.U32(palette.Faint);
		for (int i = 1; i < divisions; i++)
		{
			float x = min.X + (size.X * i / divisions);
			float y = min.Y + (size.Y * i / divisions);
			drawList.AddLine(new Vector2(x, min.Y), new Vector2(x, max.Y), faint, i == divisions / 2 ? 1.5f : 1f);
			drawList.AddLine(new Vector2(min.X, y), new Vector2(max.X, y), faint, i == divisions / 2 ? 1.5f : 1f);
		}
	}

	private static Vector2 Polar(float bearing, float distance) => new(MathF.Sin(bearing) * distance, -MathF.Cos(bearing) * distance);

	private static float PositiveAngle(float angle)
	{
		float wrapped = angle % MathF.Tau;
		return wrapped < 0f ? wrapped + MathF.Tau : wrapped;
	}
}
