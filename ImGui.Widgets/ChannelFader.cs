// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Probes;

public static partial class ImGuiWidgets
{
	/// <summary>Draws a vertical channel fader with a console dB taper and a tapered level meter beside it.</summary>
	/// <remarks>
	/// <para>
	/// The fader and the meter share one taper, <see cref="FaderTaper"/>, over one vertical span, so
	/// a signal at -10 dB lights the meter to exactly the height of the -10 tick the cap sits on.
	/// </para>
	/// <para>
	/// Drag the cap to move it; pressing the cap never jumps it, while pressing elsewhere on the
	/// track jumps it to the pointer and keeps dragging. The mouse wheel steps 1 dB a notch (0.1 dB
	/// with Ctrl held) and right-click resets to unity.
	/// </para>
	/// <para>
	/// <paramref name="gainDb"/> is written only on a frame that returns <see langword="true"/>. On
	/// every other frame it is left bit-identical, so a stored value never drifts by a round trip
	/// through the taper.
	/// </para>
	/// </remarks>
	/// <param name="label">ImGui id and probe name. Parts are probed as "{label}/track", "{label}/handle", "{label}/meter".</param>
	/// <param name="gainDb">Fader gain in dB, edited in place. <see cref="float.NegativeInfinity"/> is the bottom stop (-inf, silence).</param>
	/// <param name="meterDb">Level to show on the meter, in dB; -inf (the default) shows an empty meter.</param>
	/// <param name="peakDb">Optional held peak for the meter, in dB; -inf (the default) draws none.</param>
	/// <param name="size">Size of the fader-plus-meter body; <c>default</c> means (2.5 x frame height, 10 x text line height). A one-line dB readout is reserved beneath it.</param>
	/// <param name="maxDb">Gain at the top of the travel. Default 6. Clamped to [1, 24].</param>
	/// <returns><see langword="true"/> on a frame in which <paramref name="gainDb"/> changed (drag, track click, wheel or right-click reset).</returns>
	public static bool ChannelFader(string label, ref float gainDb, float meterDb = float.NegativeInfinity, float peakDb = float.NegativeInfinity, Vector2 size = default, float maxDb = 6f) =>
		ChannelFaderImpl.Draw(label, ref gainDb, meterDb, peakDb, size, maxDb);

	internal static class ChannelFaderImpl
	{
		private static readonly Dictionary<uint, ChannelFaderState> States = [];

		// The handle a drag moves, kept across the frames of that drag. Rebuilding it from gainDb each
		// frame would hand the drag a round trip through the taper, which differs from where the
		// drag left it in the last bit and so reports a change on every frame the mouse is held still.
		private static readonly Dictionary<uint, float[]> DragPositions = [];

		public static bool Draw(string label, ref float gainDb, float meterDb, float peakDb, Vector2 size, float maxDb)
		{
			ImGuiStylePtr style = ImGui.GetStyle();
			float frameHeight = ImGui.GetFrameHeight();
			float lineHeight = ImGui.GetTextLineHeight();
			float top = FaderTaper.ClampMaxDb(maxDb);

			size = new Vector2(
				size.X > 0f ? size.X : frameHeight * 2.5f,
				size.Y > 0f ? size.Y : lineHeight * 10f);

			uint id = ImGui.GetID(label);
			Vector2 bodyMin = ImGui.GetCursorScreenPos();
			ImGui.InvisibleButton(label, new Vector2(size.X, size.Y + style.ItemInnerSpacing.Y + lineHeight), ImGuiButtonFlags.MouseButtonLeft | ImGuiButtonFlags.MouseButtonRight);
			ImGuiProbes.MarkItem(label);

			FaderLayout layout = new(bodyMin, size, style.ItemInnerSpacing, frameHeight);

			if (!States.TryGetValue(id, out ChannelFaderState? state))
			{
				state = new ChannelFaderState();
				States[id] = state;
			}

			if (!DragPositions.TryGetValue(id, out float[]? position))
			{
				position = new float[1];
				DragPositions[id] = position;
			}

			bool changed = Interact(label, ref gainDb, top, layout, state, position);

			float shownDb = float.IsNaN(gainDb) ? 0f : gainDb;
			Draw(label, shownDb, meterDb, peakDb, top, layout);

			return changed;
		}

		private static bool Interact(string label, ref float gainDb, float top, FaderLayout layout, ChannelFaderState state, float[] position)
		{
			bool changed = Drag(ref gainDb, top, layout, state, position);

			// Only the track takes a drag; the meter beside it and the readout under it do not.
			if (state.IsDragging || (ImGui.IsItemHovered() && layout.TrackContains(ImGui.GetIO().MousePos)))
			{
				ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeNs);
			}

			if (ImGui.IsItemClicked(ImGuiMouseButton.Right) && !IsUnity(gainDb))
			{
				gainDb = 0f;
				changed = true;
			}

			if (ImGui.IsItemHovered())
			{
				changed |= Hover(label, ref gainDb, top, state);
			}

			return changed;
		}

		private static bool Drag(ref float gainDb, float top, FaderLayout layout, ChannelFaderState state, float[] position)
		{
			Vector2 mouse = ImGui.GetIO().MousePos;
			float pointer = Math.Clamp((layout.TrackBottom - mouse.Y) / layout.Span, 0f, 1f);
			bool leftActive = ImGui.IsItemActive() && ImGui.IsMouseDown(ImGuiMouseButton.Left);

			if (ImGui.IsItemActivated() && ImGui.IsMouseDown(ImGuiMouseButton.Left) && layout.TrackContains(mouse))
			{
				position[0] = FaderTaper.DbToPosition(gainDb, top);
				state.Press(position[0], pointer, layout.CapHeight * 0.5f / layout.Span);
			}

			if (!leftActive)
			{
				state.Release();
				return false;
			}

			if (state.IsDragging && state.Drag(position, pointer))
			{
				gainDb = FaderTaper.PositionToDb(position[0], top);
				return true;
			}

			return false;
		}

		private static bool Hover(string label, ref float gainDb, float top, ChannelFaderState state)
		{
			ImGuiIOPtr io = ImGui.GetIO();
			ImGui.SetItemKeyOwner(ImGuiKey.MouseWheelY);
			bool changed = false;

			// Nudge treats zero notches as a no-op, so an idle wheel changes nothing here. The test is
			// exact on purpose, and CompareTo keeps Equals' reading of NaN and signed zero.
			float nudged = FaderTaper.Nudge(gainDb, io.MouseWheel, io.KeyCtrl ? 0.1f : 1f, top);
			if (nudged.CompareTo(gainDb) is not 0)
			{
				gainDb = nudged;
				changed = true;
			}

			if (!state.IsDragging)
			{
				string visible = VisibleLabel(label);
				string readout = Readout(float.IsNaN(gainDb) ? 0f : gainDb);
				ImGui.SetTooltip(visible.Length > 0 ? $"{visible}\n{readout}" : readout);
			}

			return changed;
		}

		// Exact on purpose: a reset reports a change only when the value is not already exactly unity,
		// and a tolerance would swallow a genuine small offset.
		private static bool IsUnity(float gainDb) => gainDb is 0f;

		private static void Draw(string label, float gainDb, float meterDb, float peakDb, float top, FaderLayout layout)
		{
			ImDrawListPtr drawList = ImGui.GetWindowDrawList();
			ImGuiStylePtr style = ImGui.GetStyle();
			Span<Vector4> colors = style.Colors;
			uint borderColor = ImGui.GetColorU32(colors[(int)ImGuiCol.Border]);
			uint textColor = ImGui.GetColorU32(colors[(int)ImGuiCol.Text]);

			// Ticks, placed through the same taper the cap and the meter use. Unity (index 1) spans the
			// whole fader column; the rest mark only its left edge.
			ReadOnlySpan<float> ticks = [top, 0f, -10f, -20f, -40f, FaderTaper.FloorDb];
			for (int i = 0; i < ticks.Length; i++)
			{
				float y = layout.YOf(FaderTaper.DbToPosition(ticks[i], top));
				float width = i == 1 ? layout.FaderWidth : layout.FaderWidth * 0.3f;
				drawList.AddLine(new Vector2(layout.BodyMin.X, y), new Vector2(layout.BodyMin.X + width, y), borderColor, 1f);
			}

			float trackThickness = MathF.Max(ImGui.GetFrameHeight() * 0.18f, 2f);
			drawList.AddLine(new Vector2(layout.CentreX, layout.TrackTop), new Vector2(layout.CentreX, layout.TrackBottom), ImGui.GetColorU32(colors[(int)ImGuiCol.FrameBg]), trackThickness);

			MeterScale.DrawVerticalMeter(drawList, layout.MeterMin, layout.MeterMax, meterDb, peakDb, db => FaderTaper.DbToPosition(db, top));

			float capCentreY = layout.YOf(FaderTaper.DbToPosition(gainDb, top));
			Vector2 capMin = new(layout.CentreX - (layout.CapWidth * 0.5f), capCentreY - (layout.CapHeight * 0.5f));
			Vector2 capMax = new(layout.CentreX + (layout.CapWidth * 0.5f), capCentreY + (layout.CapHeight * 0.5f));
			bool hot = ImGui.IsItemHovered() || ImGui.IsItemActive();
			drawList.AddRectFilled(capMin, capMax, ImGui.GetColorU32(colors[(int)(hot ? ImGuiCol.SliderGrabActive : ImGuiCol.SliderGrab)]), style.GrabRounding);
			drawList.AddLine(new Vector2(capMin.X, capCentreY), new Vector2(capMax.X, capCentreY), textColor, 1f);

			string readout = Readout(gainDb);
			Vector2 readoutSize = ImGui.CalcTextSize(readout);
			Vector2 readoutPos = new(layout.BodyMin.X + ((layout.Size.X - readoutSize.X) * 0.5f), layout.BodyMax.Y + style.ItemInnerSpacing.Y);
			drawList.AddText(readoutPos, textColor, readout);

			ImGuiProbes.MarkRegion($"{label}/track", layout.BodyMin, new Vector2(layout.BodyMin.X + layout.FaderWidth, layout.BodyMax.Y));
			ImGuiProbes.MarkRegion($"{label}/handle", capMin, capMax);
			ImGuiProbes.MarkRegion($"{label}/meter", layout.MeterMin, layout.MeterMax);
		}

		private static string Readout(float gainDb) =>
			float.IsNegativeInfinity(gainDb)
				? "-inf dB"
				: string.Format(CultureInfo.CurrentCulture, "{0:+0.0;-0.0;0.0} dB", gainDb);
	}

	/// <summary>The geometry of one channel fader, worked out once per frame.</summary>
	private readonly struct FaderLayout
	{
		public FaderLayout(Vector2 bodyMin, Vector2 size, Vector2 innerSpacing, float frameHeight)
		{
			BodyMin = bodyMin;
			Size = size;
			BodyMax = bodyMin + size;
			FaderWidth = MathF.Round(size.X * 0.6f);
			CapWidth = FaderWidth * 0.8f;
			CapHeight = MathF.Max(frameHeight * 0.6f, 8f);
			CentreX = bodyMin.X + (FaderWidth * 0.5f);
			TrackTop = bodyMin.Y + (CapHeight * 0.5f);
			TrackBottom = BodyMax.Y - (CapHeight * 0.5f);
			Span = MathF.Max(TrackBottom - TrackTop, 1f);

			float meterX = bodyMin.X + FaderWidth + innerSpacing.X;
			float meterWidth = MathF.Max(size.X - FaderWidth - innerSpacing.X, 4f);
			MeterMin = new Vector2(meterX, TrackTop);
			MeterMax = new Vector2(meterX + meterWidth, TrackTop + Span);
		}

		public Vector2 BodyMin { get; }

		public Vector2 BodyMax { get; }

		public Vector2 Size { get; }

		public float FaderWidth { get; }

		public float CapWidth { get; }

		public float CapHeight { get; }

		public float CentreX { get; }

		public float TrackTop { get; }

		public float TrackBottom { get; }

		public float Span { get; }

		public Vector2 MeterMin { get; }

		public Vector2 MeterMax { get; }

		public float YOf(float position) => TrackBottom - (position * Span);

		public bool TrackContains(Vector2 point) =>
			point.X >= BodyMin.X && point.X <= BodyMin.X + FaderWidth && point.Y >= BodyMin.Y && point.Y <= BodyMax.Y;
	}
}
