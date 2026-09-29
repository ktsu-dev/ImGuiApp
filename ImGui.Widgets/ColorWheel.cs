// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Color;
using ktsu.ImGui.Probes;

/// <summary>
/// Provides custom ImGui widgets.
/// </summary>
public static partial class ImGuiWidgets
{
	/// <summary>
	/// Draws a colour-grading wheel: a trackball ringed by the hues, whose handle is pushed towards
	/// the colour to add, over a master slider for luminance.
	/// </summary>
	/// <param name="label">A unique identifier for the wheel, used for the ImGui ID and the probe name. The master slider is probed as <c>label/master</c>.</param>
	/// <param name="value">The wheel's hue, strength and master, updated while dragging.</param>
	/// <param name="diameter">The wheel's diameter in pixels. Non-positive falls back to a default derived from the font.</param>
	/// <returns><see langword="true"/> if the value changed this frame; otherwise <see langword="false"/>.</returns>
	/// <remarks>
	/// <para>
	/// This is <see cref="XYPad"/> in polar form: the angle of the handle is the hue and its distance
	/// from the centre is the strength. Unlike the pad, the drag is relative — pressing anywhere on
	/// the wheel grabs the handle where it is, so a press never jumps the grade. Hold Shift for fine
	/// adjustment, and double-click to return the ball to the centre.
	/// </para>
	/// <para>
	/// The slider under the ball is the master: an equal offset to every channel, from <c>-1</c> at
	/// the left end to <c>1</c> at the right. It drags the same relative way, Shift is fine on it too,
	/// and a double-click returns it to zero. The ball and the slider reset separately, since a grade
	/// that wants its colour back rarely wants its brightness thrown away with it.
	/// </para>
	/// <para>
	/// The hues are laid out where a vectorscope puts them, red up and to the left, so a push on the
	/// wheel moves a scope's trace the same way. The wheel only edits the value; turn it into a
	/// per-channel offset with <see cref="ColorWheelValue.ToRgbOffset"/> and apply it however the
	/// grade in hand calls for. See <see cref="LiftGammaGain"/> for the usual set of three.
	/// </para>
	/// </remarks>
	public static bool ColorWheel(string label, ref ColorWheelValue value, float diameter = 0.0f)
	{
		Ensure.NotNull(label);

		float side = diameter > 0.0f ? diameter : ImGui.GetTextLineHeight() * 8.0f;
		ColorWheelState state = ColorWheelImpl.StateFor(label);

		Vector2 min = ImGui.GetCursorScreenPos();
		ImGui.InvisibleButton(label, new Vector2(side, side));
		ImGuiProbes.MarkItem(label);

		float radius = side * 0.5f;
		Vector2 center = min + new Vector2(radius, radius);
		float ringWidth = MathF.Max(3.0f, radius * 0.08f);
		float travel = MathF.Max(1.0f, radius - ringWidth);

		bool changed = ColorWheelImpl.InteractBall(state, ref value, center, travel);
		ColorWheelImpl.DrawWheel(ImGui.GetWindowDrawList(), value, center, radius, ringWidth, travel);

		// The master slider is scoped under the wheel's own label, so it is probed as label/master
		// and two wheels' sliders stay distinct.
		using (new ScopedId(label))
		{
			changed |= ColorWheelImpl.MasterSlider(state, ref value, side);
		}

		return changed;
	}

	/// <summary>
	/// Draws the three primary grading wheels side by side — lift for the shadows, gamma for the
	/// midtones, gain for the highlights — each captioned with its name and value.
	/// </summary>
	/// <param name="label">A unique identifier for the set. It scopes the three wheels, which are probed as <c>label/Lift</c>, <c>label/Gamma</c> and <c>label/Gain</c>; it is not drawn.</param>
	/// <param name="lift">The shadows wheel.</param>
	/// <param name="gamma">The midtones wheel.</param>
	/// <param name="gain">The highlights wheel.</param>
	/// <param name="diameter">Each wheel's diameter in pixels. Non-positive falls back to a default derived from the font.</param>
	/// <returns><see langword="true"/> if any of the three changed this frame; otherwise <see langword="false"/>.</returns>
	/// <remarks>
	/// Each wheel behaves exactly as <see cref="ColorWheel"/> does. The widget edits the three values
	/// and nothing else: how each is applied to an image is left to the caller, as
	/// <see cref="ColorWheelValue.ToRgbOffset"/> describes.
	/// </remarks>
	public static bool LiftGammaGain(string label, ref ColorWheelValue lift, ref ColorWheelValue gamma, ref ColorWheelValue gain, float diameter = 0.0f)
	{
		Ensure.NotNull(label);

		float side = diameter > 0.0f ? diameter : ImGui.GetTextLineHeight() * 7.0f;
		bool changed = false;

		using (new ScopedId(label))
		{
			changed |= ColorWheelImpl.Captioned("Lift", ref lift, side);
			ImGui.SameLine();
			changed |= ColorWheelImpl.Captioned("Gamma", ref gamma, side);
			ImGui.SameLine();
			changed |= ColorWheelImpl.Captioned("Gain", ref gain, side);
		}

		return changed;
	}

	internal static class ColorWheelImpl
	{
		private static readonly Dictionary<uint, ColorWheelState> States = [];

		/// <summary>How many segments the hue ring is drawn in.</summary>
		/// <remarks>Six degrees apiece, which is finer than the eye can pick out a step in hue at this size.</remarks>
		private const int RingSegments = 60;

		/// <summary>Converts a screen position to disk units: centre at the origin, rim at one, y up.</summary>
		internal static Vector2 ToDiskUnits(Vector2 screen, Vector2 center, float travel) =>
			new((screen.X - center.X) / travel, (center.Y - screen.Y) / travel);

		/// <summary>Converts a position in disk units back to the screen.</summary>
		internal static Vector2 ToScreen(Vector2 disk, Vector2 center, float travel) =>
			new(center.X + (disk.X * travel), center.Y - (disk.Y * travel));

		internal static ColorWheelState StateFor(string label)
		{
			uint id = ImGui.GetID(label);
			if (!States.TryGetValue(id, out ColorWheelState? state))
			{
				state = new ColorWheelState();
				States[id] = state;
			}

			return state;
		}

		internal static bool InteractBall(ColorWheelState state, ref ColorWheelValue value, Vector2 center, float travel)
		{
			ImGuiIOPtr io = ImGui.GetIO();
			Vector2 pointer = ToDiskUnits(io.MousePos, center, travel);
			bool fine = io.KeyShift;
			bool changed = false;

			if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
			{
				// Keep the hue: a reset wheel pushed again starts from the direction it had, which
				// is the direction it is most likely to be pushed. The master slider is its own
				// control, with its own double-click, so it is left alone.
				ColorWheelValue reset = value with { Strength = 0.0f };
				changed = reset != value;
				value = reset;
				state.End();
				return changed;
			}

			if (ImGui.IsItemActivated())
			{
				state.Begin(value, pointer, fine);
			}

			if (ImGui.IsItemActive())
			{
				ColorWheelValue next = state.Drag(value, pointer, fine);
				changed = next != value;
				value = next;
			}
			else
			{
				state.End();
			}

			return changed;
		}

		internal static void DrawWheel(ImDrawListPtr drawList, ColorWheelValue value, Vector2 center, float radius, float ringWidth, float travel)
		{
			Span<Vector4> colors = ImGui.GetStyle().Colors;
			bool hot = ImGui.IsItemHovered() || ImGui.IsItemActive();

			// The ring: each segment in the hue that points that way. Disk angles run counter-clockwise
			// with y up and ImGui's run clockwise with y down, hence the negated angle.
			float ringRadius = radius - (ringWidth * 0.5f);
			float step = MathF.PI * 2.0f / RingSegments;
			for (int i = 0; i < RingSegments; i++)
			{
				float diskAngle = (i + 0.5f) * step;
				float hue = (diskAngle * (180.0f / MathF.PI)) - ColorWheelState.RedAngle;
				uint color = ColorWheelValue.HueToSrgb(hue).ToImGuiU32();

				// A little past each end so the joins do not show as hairline gaps.
				drawList.PathArcTo(center, ringRadius, -(i * step) + 0.01f, -((i + 1) * step) - 0.01f, 4);
				drawList.PathStroke(color, ImDrawFlags.None, ringWidth);
			}

			drawList.AddCircleFilled(center, radius - ringWidth, ImGui.GetColorU32(colors[(int)ImGuiCol.FrameBg]), 48);

			// Crosshair through neutral, so "this wheel does nothing" reads at a glance.
			uint guide = ImGui.GetColorU32(colors[(int)ImGuiCol.Border]);
			float inner = radius - ringWidth;
			drawList.AddLine(center - new Vector2(inner, 0.0f), center + new Vector2(inner, 0.0f), guide);
			drawList.AddLine(center - new Vector2(0.0f, inner), center + new Vector2(0.0f, inner), guide);

			Vector2 handle = ToScreen(ColorWheelState.ToDisk(value), center, travel);
			uint hueColor = ColorWheelValue.HueToSrgb(value.Hue).ToImGuiU32();
			bool pushed = value.Strength > 0.0f;
			if (pushed)
			{
				drawList.AddLine(center, handle, hueColor, 2.0f);
			}

			float handleRadius = MathF.Max(4.0f, radius * 0.07f);
			uint fill = ImGui.GetColorU32(colors[(int)(hot ? ImGuiCol.SliderGrabActive : ImGuiCol.SliderGrab)]);
			drawList.AddCircleFilled(handle, handleRadius, fill, 16);
			drawList.AddCircle(handle, handleRadius, pushed ? hueColor : guide, 16, 2.0f);
		}

		internal static bool MasterSlider(ColorWheelState state, ref ColorWheelValue value, float width)
		{
			float height = MathF.Max(6.0f, ImGui.GetFrameHeight() * 0.5f);
			Vector2 min = ImGui.GetCursorScreenPos();
			ImGui.InvisibleButton("master", new Vector2(width, height));
			ImGuiProbes.MarkItem("master");

			ImGuiIOPtr io = ImGui.GetIO();
			float half = MathF.Max(1.0f, width * 0.5f);
			float centerX = min.X + half;
			float pointer = (io.MousePos.X - centerX) / half;
			bool fine = io.KeyShift;
			bool changed = false;

			if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
			{
				changed = MathF.Abs(value.Master) > ColorWheelValue.MasterTolerance;
				value = value with { Master = 0.0f };
				state.EndMaster();
			}
			else
			{
				if (ImGui.IsItemActivated())
				{
					state.BeginMaster(value.Master, pointer, fine);
				}

				if (ImGui.IsItemActive())
				{
					float next = state.DragMaster(value.Master, pointer, fine);
					changed = MathF.Abs(next - value.Master) > ColorWheelValue.MasterTolerance;
					value = value with { Master = next };
				}
				else
				{
					state.EndMaster();
				}
			}

			// A bar filled from the centre towards the value, so zero reads as nothing drawn and the
			// sign reads as a side.
			ImDrawListPtr drawList = ImGui.GetWindowDrawList();
			Span<Vector4> colors = ImGui.GetStyle().Colors;
			bool hot = ImGui.IsItemHovered() || ImGui.IsItemActive();
			Vector2 max = min + new Vector2(width, height);
			float rounding = height * 0.5f;
			drawList.AddRectFilled(min, max, ImGui.GetColorU32(colors[(int)ImGuiCol.FrameBg]), rounding);

			float master = float.IsFinite(value.Master) ? Math.Clamp(value.Master, -1.0f, 1.0f) : 0.0f;
			float valueX = centerX + (master * half);
			if (MathF.Abs(master) > ColorWheelValue.MasterTolerance)
			{
				uint fill = ImGui.GetColorU32(colors[(int)(hot ? ImGuiCol.SliderGrabActive : ImGuiCol.SliderGrab)]);
				drawList.AddRectFilled(new Vector2(MathF.Min(centerX, valueX), min.Y), new Vector2(MathF.Max(centerX, valueX), max.Y), fill);
			}

			uint guide = ImGui.GetColorU32(colors[(int)ImGuiCol.Border]);
			drawList.AddLine(new Vector2(centerX, min.Y), new Vector2(centerX, max.Y), guide);
			drawList.AddRect(min, max, guide, rounding);

			return changed;
		}

		internal static bool Captioned(string name, ref ColorWheelValue value, float side)
		{
			ImGui.BeginGroup();
			bool changed = ColorWheel(name, ref value, side);

			CenteredText(name, side);
			CenteredText(
				value.IsNeutral
					? "Neutral"
					: string.Format(CultureInfo.InvariantCulture, "{0:0}\u00b0 {1:0.00} {2:+0.00;-0.00;0.00}", ColorWheelValue.WrapHue(value.Hue), value.Strength, value.Master),
				side);

			ImGui.EndGroup();
			return changed;
		}

		private static void CenteredText(string text, float width)
		{
			float textWidth = ImGui.CalcTextSize(text).X;
			float start = ImGui.GetCursorPosX();
			ImGui.SetCursorPosX(start + MathF.Max(0.0f, (width - textWidth) * 0.5f));
			ImGui.TextUnformatted(text);
		}
	}
}
