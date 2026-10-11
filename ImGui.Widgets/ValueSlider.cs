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
	/// Draws a slider as a frame filled from the left up to the value, with the value written in the
	/// middle and nothing drawn over it. Drag anywhere on the frame to change it, hold Shift to change it
	/// finely, and Ctrl+click to type a value. A double-click returns the slider to
	/// <see cref="ValueSliderOptions.ResetValue"/> when one is set, and opens it for typing otherwise.
	/// </summary>
	/// <remarks>
	/// A drag moves the value by how far the pointer travels, the frame's full width covering the full
	/// range, rather than jumping to where the press landed, so a click on its own changes nothing. The
	/// frame and its label are one ImGui item, as they are for <c>ImGui.SliderFloat</c>, so
	/// <c>ImGui.IsItemActivated</c>, <c>IsItemDeactivatedAfterEdit</c> and a probe mark taken straight
	/// after the call all see the slider.
	/// </remarks>
	/// <param name="label">A unique label; text after <c>##</c> is hidden but used for the ID. Visible text is drawn to the right.</param>
	/// <param name="value">The value. Updated in place.</param>
	/// <param name="min">The lowest value.</param>
	/// <param name="max">The highest value.</param>
	/// <param name="options">The format, unit, fill and reset value, or null for the defaults.</param>
	/// <returns><see langword="true"/> on a frame the value changed; otherwise <see langword="false"/>.</returns>
	public static bool ValueSlider(string label, ref float value, float min, float max, ValueSliderOptions? options = null)
	{
		double current = value;
		bool changed = ValueSliderImpl.Draw(label, ref current, min, max, isInteger: false, options ?? ValueSliderImpl.DefaultOptions);
		if (changed)
		{
			value = (float)current;
		}

		return changed;
	}

	/// <summary>
	/// Draws a whole-number slider as a frame filled up to the value, with the value written in the
	/// middle. It behaves as the <see cref="float"/> overload does, and rounds what a drag or typing gives
	/// it to the nearest whole number.
	/// </summary>
	/// <param name="label">A unique label; text after <c>##</c> is hidden but used for the ID. Visible text is drawn to the right.</param>
	/// <param name="value">The value. Updated in place.</param>
	/// <param name="min">The lowest value.</param>
	/// <param name="max">The highest value.</param>
	/// <param name="options">The format, unit, fill and reset value, or null for the defaults.</param>
	/// <returns><see langword="true"/> on a frame the value changed; otherwise <see langword="false"/>.</returns>
	public static bool ValueSlider(string label, ref int value, int min, int max, ValueSliderOptions? options = null)
	{
		double current = value;
		bool changed = ValueSliderImpl.Draw(label, ref current, min, max, isInteger: true, options ?? ValueSliderImpl.DefaultOptions);
		if (changed)
		{
			value = (int)current;
		}

		return changed;
	}

	internal static class ValueSliderImpl
	{
		private const uint EditCapacity = 64u;

		// Frames the editor may go without the keyboard before it gives up: focus lands a frame after it
		// is asked for, so one frame without it is expected.
		private const int UnfocusedFramesAllowed = 2;

		private static readonly Dictionary<uint, ValueSliderState> States = [];
		private static readonly Dictionary<(uint Fill, uint Text), uint> ReadableText = [];

		internal static ValueSliderOptions DefaultOptions { get; } = new();

		/// <summary>What one call asked for, carried together through the drawing helpers.</summary>
		private readonly record struct Spec(double Min, double Max, bool IsInteger, string Format, ValueSliderOptions Options)
		{
			/// <summary>Gets the smallest magnitude a logarithmic range starting at zero reaches before zero.</summary>
			public double Epsilon { get; } = Options.Logarithmic ? ValueSliderState.LogarithmicEpsilon(Format, IsInteger) : 0.0;
		}

		internal static ValueSliderState StateFor(uint id)
		{
			if (!States.TryGetValue(id, out ValueSliderState? state))
			{
				state = new ValueSliderState();
				States[id] = state;
			}

			return state;
		}

		public static bool Draw(string label, ref double value, double min, double max, bool isInteger, ValueSliderOptions options)
		{
			if (min > max)
			{
				(min, max) = (max, min);
			}

			uint id = ImGui.GetID(label);
			ValueSliderState state = StateFor(id);
			string format = options.Format ?? (isInteger ? "0" : ValueSliderState.DefaultFormat(min, max));
			Spec spec = new(min, max, isInteger, format, options);
			double before = value;

			if (state.IsEditing)
			{
				DrawEditor(label, state, ref value, spec);
			}
			else
			{
				DrawFrame(label, id, state, ref value, spec);
			}

			ImGuiProbes.MarkItem(label);

			// A value the caller passed outside the range is shown as it is until something edits it.
			return !IsSameValue(value, before);
		}

		private static void DrawFrame(string label, uint id, ValueSliderState state, ref double value, Spec spec)
		{
			ImGuiStylePtr style = ImGui.GetStyle();
			float height = ImGui.GetFrameHeight();
			float width = MathF.Max(ImGui.CalcItemWidth(), height);
			string visible = VisibleLabel(label);
			Vector2 labelSize = visible.Length > 0 ? ImGui.CalcTextSize(visible) : Vector2.Zero;

			Vector2 origin = ImGui.GetCursorScreenPos();
			ImRect frame = new(origin, origin + new Vector2(width, height));
			float labelWidth = visible.Length > 0 ? style.ItemInnerSpacing.X + labelSize.X : 0.0f;
			ImRect total = new(origin, frame.Max + new Vector2(labelWidth, 0.0f));

			// The frame and label are one item, so the caller's IsItemActivated and probe mark see the
			// slider, but only the frame takes the pointer, as with ImGui's own sliders.
			ImGuiP.ItemSize(total, style.FramePadding.Y);
			if (!ImGuiP.ItemAdd(total, id, ref frame))
			{
				return;
			}

			bool hovered = false;
			bool held = false;
			ImGuiP.ButtonBehavior(frame, id, ref hovered, ref held);

			if (hovered || held)
			{
				ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeEw);
			}

			if (HandlePointer(state, ref value, spec, width))
			{
				ImGuiP.MarkItemEdited(id);
			}

			if (state.IsEditing)
			{
				state.EditText = ValueSliderState.Format(value, spec.Format, spec.Options.Unit, CultureInfo.CurrentCulture);
				state.EditStartText = state.EditText;
			}

			DrawContents(frame, hovered, held, value, spec);

			if (visible.Length > 0)
			{
				ImGui.GetWindowDrawList().AddText(
					new Vector2(frame.Max.X + style.ItemInnerSpacing.X, frame.Min.Y + style.FramePadding.Y),
					ImGui.GetColorU32(ImGuiCol.Text),
					visible);
			}
		}

		private static bool HandlePointer(ValueSliderState state, ref double value, Spec spec, float width)
		{
			bool changed = false;

			if (ImGui.IsItemActivated())
			{
				changed = Activate(state, ref value, spec);
			}

			if (ImGui.IsItemActive() && ImGui.IsMouseDragging(ImGuiMouseButton.Left))
			{
				changed |= Drag(state, ref value, spec, width);
			}

			if (ImGui.IsItemDeactivated())
			{
				if (state.OpenOnRelease && !state.IsDragging)
				{
					state.IsEditing = true;
					state.FocusPending = true;
					state.EditFramesUnfocused = 0;
				}

				state.IsDragging = false;
				state.OpenOnRelease = false;
			}

			return changed;
		}

		/// <summary>Starts a press: Ctrl or a double-click without a reset value types on release, and a double-click with one resets.</summary>
		private static bool Activate(ValueSliderState state, ref double value, Spec spec)
		{
			state.DragValue = Math.Clamp(value, spec.Min, spec.Max);
			state.IsDragging = false;
			state.OpenOnRelease = ImGui.GetIO().KeyCtrl;

			if (!ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
			{
				return false;
			}

			if (spec.Options.ResetValue is not double reset)
			{
				state.OpenOnRelease = true;
				return false;
			}

			bool changed = SetValue(ref value, reset, spec.Min, spec.Max, spec.IsInteger);
			state.DragValue = value;
			return changed;
		}

		/// <summary>Moves the value by the frame's pointer movement, finely while Shift is held.</summary>
		private static bool Drag(ValueSliderState state, ref double value, Spec spec, float width)
		{
			ImGuiIOPtr io = ImGui.GetIO();
			float deltaX;
			if (!state.IsDragging)
			{
				// The first frame past the drag threshold applies everything moved so far, so the
				// threshold does not eat the start of the drag.
				state.IsDragging = true;
				deltaX = ImGui.GetMouseDragDelta(ImGuiMouseButton.Left).X;
			}
			else
			{
				deltaX = io.MouseDelta.X;
			}

			float scale = io.KeyShift ? spec.Options.FineScale : 1.0f;
			state.DragValue = ValueSliderState.Drag(state.DragValue, deltaX, width, spec.Min, spec.Max, scale, spec.Options.Logarithmic, spec.Epsilon);
			return SetValue(ref value, state.DragValue, spec.Min, spec.Max, spec.IsInteger);
		}

		private static bool SetValue(ref double value, double target, double min, double max, bool isInteger)
		{
			double next = Math.Clamp(isInteger ? Math.Round(target, MidpointRounding.AwayFromZero) : target, min, max);
			if (IsSameValue(next, value))
			{
				return false;
			}

			value = next;
			return true;
		}

		/// <summary>
		/// Whether two values are the same number, bit for bit. The question is "did anything write a new
		/// value", not "are these close", so a tolerance would hide a real change.
		/// </summary>
		private static bool IsSameValue(double a, double b) =>
			BitConverter.DoubleToInt64Bits(a) == BitConverter.DoubleToInt64Bits(b);

		private static void DrawContents(ImRect frame, bool hovered, bool held, double value, Spec spec)
		{
			(double min, double max, _, string format, ValueSliderOptions options) = spec;
			ImDrawListPtr drawList = ImGui.GetWindowDrawList();
			ImGuiStylePtr style = ImGui.GetStyle();
			float rounding = style.FrameRounding;
			ImGuiCol background = held ? ImGuiCol.FrameBgActive : hovered ? ImGuiCol.FrameBgHovered : ImGuiCol.FrameBg;
			drawList.AddRectFilled(frame.Min, frame.Max, ImGui.GetColorU32(background), rounding);

			(float start, float end) = ValueSliderState.FillSpan(value, min, max, options.Fill, options.Logarithmic, spec.Epsilon);
			float width = frame.Max.X - frame.Min.X;
			float fillMinX = frame.Min.X + (start * width);
			float fillMaxX = frame.Min.X + (end * width);

			string text = options.Prefix + ValueSliderState.Format(value, format, options.Unit, CultureInfo.CurrentCulture);
			Vector2 textSize = ImGui.CalcTextSize(text);
			Vector2 textPos = new(
				MathF.Round(frame.Min.X + ((width - textSize.X) * 0.5f)),
				MathF.Round(frame.Min.Y + ((frame.Max.Y - frame.Min.Y - textSize.Y) * 0.5f)));

			// The number keeps a little clear space either side, so nothing thin can sit right against it.
			float textGap = MathF.Max(1.0f, style.ItemInnerSpacing.X * 0.5f);
			ImRect textBox = new(
				new Vector2(textPos.X - textGap, textPos.Y),
				new Vector2(textPos.X + textSize.X + textGap, textPos.Y + textSize.Y));

			ImGuiCol fillColor = held ? ImGuiCol.SliderGrabActive : ImGuiCol.SliderGrab;
			uint fill = ImGui.GetColorU32(fillColor);
			bool fillAvoidsText = ValueSliderState.FillAvoidsText(fillMinX, fillMaxX, textBox.Min.X, textBox.Max.X);
			if (fillMaxX > fillMinX)
			{
				// The whole frame shape clipped to the fill keeps the frame's rounded corners at whichever
				// ends the fill reaches, and square edges everywhere else. A fill lying wholly under the
				// number is drawn above and below it instead, or it would run through the digits.
				DrawClipped(drawList, frame, new Vector2(fillMinX, frame.Min.Y), new Vector2(fillMaxX, frame.Max.Y), fillAvoidsText ? textBox : null, fill, rounding);
			}

			if (options.Fill == ValueSliderFill.FromZero && min < 0.0 && max > 0.0)
			{
				float zeroX = MathF.Round(frame.Min.X + (ValueSliderState.Fraction(0.0, min, max, options.Logarithmic, spec.Epsilon) * width));
				uint zeroColor = ImGui.GetColorU32(ImGuiCol.TextDisabled);
				if (zeroX >= textBox.Min.X && zeroX <= textBox.Max.X)
				{
					// The zero mark is cut where the number sits, leaving a tick above it and one below.
					drawList.AddLine(new Vector2(zeroX, frame.Min.Y + 1.0f), new Vector2(zeroX, textBox.Min.Y), zeroColor);
					drawList.AddLine(new Vector2(zeroX, textBox.Max.Y), new Vector2(zeroX, frame.Max.Y - 1.0f), zeroColor);
				}
				else
				{
					drawList.AddLine(new Vector2(zeroX, frame.Min.Y + 1.0f), new Vector2(zeroX, frame.Max.Y - 1.0f), zeroColor);
				}
			}

			if (fillAvoidsText)
			{
				// Nothing lies behind the number, so it reads against the frame alone.
				fillMinX = fillMaxX = frame.Min.X;
			}

			uint textColor = ImGui.GetColorU32(ImGuiCol.Text);
			uint fillTextColor = ReadableOver(ComposeOver(style.Colors[(int)fillColor], style.Colors[(int)background]), textColor);

			// The text is written twice, each clipped to its own side of the fill's edges, so the part
			// over the fill reads against the fill and the rest against the frame.
			drawList.PushClipRect(frame.Min, frame.Max, true);
			drawList.PushClipRect(new Vector2(fillMinX, frame.Min.Y), new Vector2(fillMaxX, frame.Max.Y), true);
			drawList.AddText(textPos, fillTextColor, text);
			drawList.PopClipRect();
			DrawTextOutside(drawList, frame, fillMinX, fillMaxX, textPos, textColor, text);
			drawList.PopClipRect();
		}

		/// <summary>Fills the frame's shape inside a clip rectangle, leaving out a hole when one is given.</summary>
		private static void DrawClipped(ImDrawListPtr drawList, ImRect frame, Vector2 clipMin, Vector2 clipMax, ImRect? hole, uint color, float rounding)
		{
			if (hole is not ImRect cut)
			{
				drawList.PushClipRect(clipMin, clipMax, true);
				drawList.AddRectFilled(frame.Min, frame.Max, color, rounding);
				drawList.PopClipRect();
				return;
			}

			if (cut.Min.Y > clipMin.Y)
			{
				DrawClipped(drawList, frame, clipMin, new Vector2(clipMax.X, cut.Min.Y), null, color, rounding);
			}

			if (cut.Max.Y < clipMax.Y)
			{
				DrawClipped(drawList, frame, new Vector2(clipMin.X, cut.Max.Y), clipMax, null, color, rounding);
			}
		}

		private static void DrawTextOutside(ImDrawListPtr drawList, ImRect frame, float fillMinX, float fillMaxX, Vector2 textPos, uint color, string text)
		{
			if (fillMinX > frame.Min.X)
			{
				drawList.PushClipRect(frame.Min, new Vector2(fillMinX, frame.Max.Y), true);
				drawList.AddText(textPos, color, text);
				drawList.PopClipRect();
			}

			if (fillMaxX < frame.Max.X)
			{
				drawList.PushClipRect(new Vector2(fillMaxX, frame.Min.Y), frame.Max, true);
				drawList.AddText(textPos, color, text);
				drawList.PopClipRect();
			}
		}

		/// <summary>The fill as it appears over the frame, which is what the text has to read against.</summary>
		private static Vector4 ComposeOver(Vector4 top, Vector4 bottom)
		{
			float a = top.W;
			return new Vector4(
				(top.X * a) + (bottom.X * (1.0f - a)),
				(top.Y * a) + (bottom.Y * (1.0f - a)),
				(top.Z * a) + (bottom.Z * (1.0f - a)),
				1.0f);
		}

		/// <summary>
		/// The theme's own text color when it reads against the fill, and the most readable color over the
		/// fill when it does not, cached because the search behind that is not free.
		/// </summary>
		private static uint ReadableOver(Vector4 fill, uint textColor)
		{
			uint fillKey = ImGui.ColorConvertFloat4ToU32(fill);
			if (ReadableText.TryGetValue((fillKey, textColor), out uint cached))
			{
				return cached;
			}

			ImColor background = new() { Value = fill };
			ImColor text = new() { Value = ImGui.ColorConvertU32ToFloat4(textColor) };
			uint readable = text.GetContrastRatioOver(background) >= ImColorExtensions.OptimalTextContrastRatio
				? textColor
				: ImGui.ColorConvertFloat4ToU32(background.MostReadableTextColor().Value with { W = text.Value.W });

			ReadableText[(fillKey, textColor)] = readable;
			return readable;
		}

		private static void DrawEditor(string label, ValueSliderState state, ref double value, Spec spec)
		{
			ImGui.SetNextItemWidth(MathF.Max(ImGui.CalcItemWidth(), ImGui.GetFrameHeight()));
			if (state.FocusPending)
			{
				ImGui.SetKeyboardFocusHere();
				state.FocusPending = false;
			}

			string edited = state.EditText;

			// The editor takes the slider's own label, and so its id: it draws the label in the same
			// place, and the caller's last-item queries keep pointing at the slider while it is open.
			ImGui.InputText(label, ref edited, EditCapacity, ImGuiInputTextFlags.AutoSelectAll);
			state.EditText = edited;

			if (ImGui.IsItemDeactivated())
			{
				bool escaped = ImGui.IsKeyPressed(ImGuiKey.Escape, false);
				if (!escaped && state.EditText != state.EditStartText
					&& ValueSliderState.TryParse(state.EditText, spec.Options.Unit, CultureInfo.CurrentCulture, out double typed, spec.Options.Prefix))
				{
					SetValue(ref value, typed, spec.Min, spec.Max, spec.IsInteger);
				}

				state.IsEditing = false;
				return;
			}

			if (ImGui.IsItemActive())
			{
				state.EditFramesUnfocused = 0;
			}
			else if (++state.EditFramesUnfocused > UnfocusedFramesAllowed)
			{
				// Focus never arrived, so there is nothing to type into: show the slider again.
				state.IsEditing = false;
			}
		}
	}
}
