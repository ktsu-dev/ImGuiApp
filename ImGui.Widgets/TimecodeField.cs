// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;
using System.Collections.Generic;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Probes;

/// <summary>
/// Provides custom ImGui widgets.
/// </summary>
public static partial class ImGuiWidgets
{
	/// <summary>
	/// Edits a frame count shown as SMPTE timecode, <c>HH:MM:SS:FF</c> (<c>;FF</c> for drop-frame), laid
	/// out as <c>[-] [value] [+] label</c>.
	/// </summary>
	/// <remarks>
	/// The buttons step one frame, or one second while Shift is held, and repeat while held. Dragging
	/// across the value scrubs one frame per pixel, ten with Shift. Clicking the value without dragging
	/// opens it for typing: Enter commits text <see cref="Timecode.TryParse"/> accepts, clamped to the
	/// range, and Escape or text it rejects leaves the value as it was. Up and Down step while the
	/// pointer rests on the value. The value is a frame count throughout, so drop-frame labels are
	/// skipped by stepping rather than landed on.
	/// </remarks>
	/// <param name="label">The label. Text after <c>##</c> is hidden and only identifies the widget.</param>
	/// <param name="frame">The frame count to edit.</param>
	/// <param name="rate">The frame rate the value is shown and typed at.</param>
	/// <param name="min">The lowest frame the widget will produce.</param>
	/// <param name="max">The highest frame the widget will produce.</param>
	/// <returns><see langword="true"/> on any frame the value changed; otherwise <see langword="false"/>.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="label"/> is <see langword="null"/>.</exception>
	/// <exception cref="ArgumentException"><paramref name="rate"/> is <see langword="default"/>.</exception>
	public static bool TimecodeField(string label, ref int frame, TimecodeRate rate, int min = 0, int max = int.MaxValue)
	{
		Ensure.NotNull(label);
		Timecode.EnsureValid(rate, nameof(rate));
		return TimecodeFieldImpl.Draw(label, ref frame, rate, min, max);
	}

	/// <summary>
	/// What a <see cref="TimecodeField"/> remembers between frames, and the arithmetic its gestures use.
	/// The static members are pure and need no ImGui context.
	/// </summary>
	internal sealed class TimecodeFieldState
	{
		/// <summary>Gets or sets a value indicating whether the value is open for typing.</summary>
		public bool IsEditing { get; set; }

		/// <summary>Gets or sets the text being typed.</summary>
		public string EditText { get; set; } = string.Empty;

		/// <summary>Gets or sets a value indicating whether the editor still has to take keyboard focus.</summary>
		public bool FocusPending { get; set; }

		/// <summary>Gets or sets a value indicating whether the current press has become a scrub.</summary>
		public bool IsDragging { get; set; }

		/// <summary>Gets or sets the fraction of a frame a scrub has moved and not yet applied.</summary>
		public float Remainder { get; set; }

		/// <summary>
		/// Whole frames a scrub moves by: <c>remainder + deltaX * framesPerPixel</c>, truncated toward zero,
		/// with the fraction carried in <paramref name="newRemainder"/>. Non-finite input moves nothing and
		/// clears the remainder.
		/// </summary>
		/// <param name="remainder">The fraction carried from earlier frames.</param>
		/// <param name="deltaX">The pointer's horizontal movement in pixels.</param>
		/// <param name="framesPerPixel">Frames per pixel of movement.</param>
		/// <param name="newRemainder">The fraction to carry into the next frame.</param>
		/// <returns>The whole frames to move by.</returns>
		public static int ScrubStep(float remainder, float deltaX, float framesPerPixel, out float newRemainder)
		{
			float total = remainder + (deltaX * framesPerPixel);
			if (!float.IsFinite(total))
			{
				newRemainder = 0.0f;
				return 0;
			}

			// Beyond the int range the fraction means nothing; saturate rather than wrap.
			if (total >= int.MaxValue)
			{
				newRemainder = 0.0f;
				return int.MaxValue;
			}

			if (total <= int.MinValue)
			{
				newRemainder = 0.0f;
				return int.MinValue;
			}

			int whole = (int)MathF.Truncate(total);
			newRemainder = total - whole;
			return whole;
		}

		/// <summary>Moves a frame count by <paramref name="delta"/>, clamped to the range without overflowing.</summary>
		/// <param name="frame">The frame count.</param>
		/// <param name="delta">The frames to move by.</param>
		/// <param name="min">The lowest frame.</param>
		/// <param name="max">The highest frame.</param>
		/// <returns>The moved frame count.</returns>
		public static int Step(int frame, int delta, int min, int max) =>
			(int)Math.Clamp((long)frame + delta, min, max);

		/// <summary>The frames one second's step moves: the rate's nominal frames per second.</summary>
		/// <param name="rate">The frame rate.</param>
		/// <returns>The frames in a labelled second.</returns>
		public static int OneSecond(TimecodeRate rate) => rate.NominalFramesPerSecond;

		/// <summary>Swaps a reversed range so <c>Min</c> is never above <c>Max</c>.</summary>
		/// <param name="min">The lower bound as given.</param>
		/// <param name="max">The upper bound as given.</param>
		/// <returns>The range in order.</returns>
		public static (int Min, int Max) Normalize(int min, int max) => min <= max ? (min, max) : (max, min);
	}

	internal static class TimecodeFieldImpl
	{
		// The widest two-digit-hours label, which sets the box's width so it does not change as the
		// value does.
		private const string WidthSample = "00:00:00:00";

		private const float ShiftFramesPerPixel = 10.0f;

		private static readonly Dictionary<uint, TimecodeFieldState> States = [];

		internal static TimecodeFieldState StateFor(string label)
		{
			uint id = ImGui.GetID(label);
			if (!States.TryGetValue(id, out TimecodeFieldState? state))
			{
				state = new TimecodeFieldState();
				States[id] = state;
			}

			return state;
		}

		public static bool Draw(string label, ref int frame, TimecodeRate rate, int min, int max)
		{
			(min, max) = TimecodeFieldState.Normalize(min, max);
			TimecodeFieldState state = StateFor(label);
			int before = frame;

			using (new ScopedId(label))
			{
				ImGuiStylePtr style = ImGui.GetStyle();
				float height = ImGui.GetFrameHeight();
				Vector2 buttonSize = new(height, height);
				bool shift = ImGui.GetIO().KeyShift;
				int stepSize = shift ? TimecodeFieldState.OneSecond(rate) : 1;

				using (new ScopedDisable(frame <= min))
				{
					if (StepperImpl.RepeatButton("-##dec", buttonSize))
					{
						frame = TimecodeFieldState.Step(frame, -stepSize, min, max);
					}

					ImGuiProbes.MarkItem("dec");
				}

				ImGui.SameLine(0.0f, style.ItemInnerSpacing.X);

				string text = Timecode.Format(frame, rate);
				float width = MathF.Max(ImGui.CalcTextSize(WidthSample).X, ImGui.CalcTextSize(text).X) + (style.FramePadding.X * 2.0f);

				if (state.IsEditing)
				{
					DrawEditor(state, ref frame, rate, min, max, width);
				}
				else
				{
					DrawValue(state, ref frame, text, min, max, stepSize, shift, rate, new Vector2(width, height));
				}

				ImGui.SameLine(0.0f, style.ItemInnerSpacing.X);

				using (new ScopedDisable(frame >= max))
				{
					if (StepperImpl.RepeatButton("+##inc", buttonSize))
					{
						frame = TimecodeFieldState.Step(frame, stepSize, min, max);
					}

					ImGuiProbes.MarkItem("inc");
				}

				string visible = VisibleLabel(label);
				if (visible.Length > 0)
				{
					ImGui.SameLine();
					ImGui.AlignTextToFramePadding();
					ImGui.TextUnformatted(visible);
				}
			}

			return frame != before;
		}

		private static void DrawValue(TimecodeFieldState state, ref int frame, string text, int min, int max, int stepSize, bool shift, TimecodeRate rate, Vector2 size)
		{
			Vector2 origin = ImGui.GetCursorScreenPos();
			Vector2 end = origin + size;
			ImGui.InvisibleButton("##value", size);
			ImGuiProbes.MarkItem("value");

			bool hovered = ImGui.IsItemHovered();
			bool active = ImGui.IsItemActive();

			if (active && ImGui.IsMouseDragging(ImGuiMouseButton.Left))
			{
				float deltaX;
				if (!state.IsDragging)
				{
					// The first frame past the drag threshold applies everything moved so far, so the
					// threshold does not eat the start of the scrub.
					state.IsDragging = true;
					state.Remainder = 0.0f;
					deltaX = ImGui.GetMouseDragDelta(ImGuiMouseButton.Left).X;
				}
				else
				{
					deltaX = ImGui.GetIO().MouseDelta.X;
				}

				int whole = TimecodeFieldState.ScrubStep(state.Remainder, deltaX, shift ? ShiftFramesPerPixel : 1.0f, out float remainder);
				state.Remainder = remainder;
				if (whole != 0)
				{
					frame = TimecodeFieldState.Step(frame, whole, min, max);
					text = Timecode.Format(frame, rate);
				}
			}

			if (ImGui.IsItemDeactivated())
			{
				// A press that never became a scrub, released over the box, is a click: open for typing.
				if (!state.IsDragging && ImGui.IsMouseHoveringRect(origin, end))
				{
					state.IsEditing = true;
					state.EditText = Timecode.Format(frame, rate);
					state.FocusPending = true;
				}

				state.IsDragging = false;
			}

			if (hovered && !state.IsEditing)
			{
				ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeEw);

				if (ImGui.IsKeyPressed(ImGuiKey.UpArrow, true))
				{
					frame = TimecodeFieldState.Step(frame, stepSize, min, max);
					text = Timecode.Format(frame, rate);
				}

				if (ImGui.IsKeyPressed(ImGuiKey.DownArrow, true))
				{
					frame = TimecodeFieldState.Step(frame, -stepSize, min, max);
					text = Timecode.Format(frame, rate);
				}

				if (!state.IsDragging)
				{
					ImGui.BeginTooltip();
					ImGui.TextUnformatted($"{rate} · drag to scrub, click to type");
					ImGui.EndTooltip();
				}
			}

			Span<Vector4> colors = ImGui.GetStyle().Colors;
			ImGuiCol background = active ? ImGuiCol.FrameBgActive : hovered ? ImGuiCol.FrameBgHovered : ImGuiCol.FrameBg;
			ImDrawListPtr drawList = ImGui.GetWindowDrawList();
			drawList.AddRectFilled(origin, end, ImGui.GetColorU32(colors[(int)background]), ImGui.GetStyle().FrameRounding);
			Vector2 textSize = ImGui.CalcTextSize(text);
			drawList.AddText(
				new Vector2(origin.X + ((size.X - textSize.X) * 0.5f), origin.Y + ((size.Y - textSize.Y) * 0.5f)),
				ImGui.GetColorU32(colors[(int)ImGuiCol.Text]),
				text);
		}

		private static void DrawEditor(TimecodeFieldState state, ref int frame, TimecodeRate rate, int min, int max, float width)
		{
			ImGui.SetNextItemWidth(width);
			if (state.FocusPending)
			{
				ImGui.SetKeyboardFocusHere();
				state.FocusPending = false;
			}

			string edited = state.EditText;
			ImGui.InputText("##edit", ref edited, 32u, ImGuiInputTextFlags.AutoSelectAll | ImGuiInputTextFlags.CharsNoBlank);
			state.EditText = edited;
			ImGuiProbes.MarkItem("edit");

			if (ImGui.IsItemDeactivated())
			{
				bool escaped = ImGui.IsKeyPressed(ImGuiKey.Escape, false);
				if (!escaped && Timecode.TryParse(state.EditText, rate, out int parsed))
				{
					frame = Math.Clamp(parsed, min, max);
				}

				state.IsEditing = false;
			}
		}
	}
}
