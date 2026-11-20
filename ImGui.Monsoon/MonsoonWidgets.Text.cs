// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Monsoon;

using System.Numerics;
using Hexa.NET.ImGui;
using ktsu.ImGui.Monsoon.Effects;
using ktsu.ImGui.Probes;

/// <summary>Neon-noir widgets for Dear ImGui: glowing text, glass panels, readouts and signs.</summary>
/// <remarks>
/// Every widget draws with <see cref="MonsoonPalette"/> colours and respects
/// <see cref="MonsoonEffects.Motion"/>, so flicker, glitch and rain can be calmed or switched off.
/// Each one marks itself for UI-test probes under its label or id.
/// </remarks>
public static partial class MonsoonWidgets
{
	/// <summary>Draws a line of text that glows and, now and then, flickers like a failing tube.</summary>
	/// <param name="text">The text.</param>
	/// <param name="color">The colour; cyan when null.</param>
	/// <param name="flicker">How deep a flicker dips, 0 to 1.</param>
	public static void NeonText(string text, Vector4? color = null, float flicker = 0.15f)
	{
		Ensure.NotNull(text);
		Vector4 tint = color ?? MonsoonPalette.Cyan;
		float brightness = MonsoonEffects.AllowsFlicker ? Flicker.Brightness(MonsoonEffects.Time, Noise.SeedOf(text), flicker) : 1f;
		Vector2 position = ImGui.GetCursorScreenPos();
		Vector2 size = ImGui.CalcTextSize(text);
		NeonDraw.Text(ImGui.GetWindowDrawList(), position, tint, text, brightness);
		ImGui.Dummy(size);
		ImGuiProbes.MarkItem(text);
		if (flicker > 0f)
		{
			MonsoonEffects.RequestFrame();
		}
	}

	/// <summary>Draws text that tears into offset slices with a colour split during short bursts.</summary>
	/// <param name="text">The text.</param>
	/// <param name="color">The colour; text white when null.</param>
	/// <param name="intensity">How hard the bursts tear, 0 to 1.</param>
	public static void GlitchText(string text, Vector4? color = null, float intensity = 0.6f)
	{
		Ensure.NotNull(text);
		Vector4 tint = color ?? MonsoonPalette.Text;
		Vector2 position = ImGui.GetCursorScreenPos();
		Vector2 size = ImGui.CalcTextSize(text);
		ImDrawListPtr drawList = ImGui.GetWindowDrawList();
		const int SliceCount = 4;
		float strength = MonsoonEffects.AllowsFlicker ? intensity : 0f;
		GlitchFrame frame = Glitch.Compute(MonsoonEffects.Time, Noise.SeedOf(text), strength, SliceCount);
		if (!frame.Active)
		{
			NeonDraw.Text(drawList, position, tint, text);
		}
		else
		{
			float split = frame.Split * 3f;
			drawList.AddText(position + new Vector2(-split, 0f), NeonDraw.Color(MonsoonPalette.Magenta, 1f, 0.7f), text);
			drawList.AddText(position + new Vector2(split, 0f), NeonDraw.Color(MonsoonPalette.Cyan, 1f, 0.7f), text);
			float sliceHeight = size.Y / SliceCount;
			for (int i = 0; i < SliceCount; i++)
			{
				Vector2 clipMin = new(position.X - 16f, position.Y + (i * sliceHeight));
				Vector2 clipMax = new(position.X + size.X + 16f, clipMin.Y + sliceHeight);
				drawList.PushClipRect(clipMin, clipMax, true);
				drawList.AddText(position + new Vector2(frame.Offsets[i] * 6f, 0f), NeonDraw.Color(tint), text);
				drawList.PopClipRect();
			}
		}

		ImGui.Dummy(size);
		ImGuiProbes.MarkItem(text);
		if (strength > 0f)
		{
			MonsoonEffects.RequestFrame();
		}
	}

	/// <summary>Draws a sign of neon tubes. Katakana are drawn as vector strokes, so no CJK font is needed; any other character is drawn with the current font.</summary>
	/// <param name="text">The text.</param>
	/// <param name="height">The glyph height in pixels.</param>
	/// <param name="color">The tube colour; magenta when null.</param>
	/// <param name="flicker">How deep each glyph's flicker dips, 0 to 1.</param>
	public static void NeonSign(string text, float height = 48f, Vector4? color = null, float flicker = 0.4f)
	{
		Ensure.NotNull(text);
		Vector4 tint = color ?? MonsoonPalette.Magenta;
		uint seed = Noise.SeedOf(text);
		double time = MonsoonEffects.Time;
		bool flickers = MonsoonEffects.AllowsFlicker && flicker > 0f;
		Vector2 origin = ImGui.GetCursorScreenPos();
		Vector2 size = NeonDraw.SignSize(text, height);
		NeonDraw.Sign(ImGui.GetWindowDrawList(), origin, height, tint, text, i => flickers ? Flicker.Brightness(time, seed + (uint)(i * 7919), flicker) : 1f);
		ImGui.Dummy(size);
		ImGuiProbes.MarkItem(text);
		if (flickers)
		{
			MonsoonEffects.RequestFrame();
		}
	}

	/// <summary>Draws a label and a value joined by a dotted leader, the way a terminal lists readings.</summary>
	/// <param name="label">The label, also the probe name.</param>
	/// <param name="value">The value.</param>
	/// <param name="valueColor">The value colour; amber when null.</param>
	public static void DataReadout(string label, string value, Vector4? valueColor = null)
	{
		Ensure.NotNull(label);
		Ensure.NotNull(value);
		Vector2 position = ImGui.GetCursorScreenPos();
		float width = MathF.Max(ImGui.GetContentRegionAvail().X, 1f);
		float lineHeight = ImGui.GetTextLineHeight();
		ImDrawListPtr drawList = ImGui.GetWindowDrawList();
		Vector2 labelSize = ImGui.CalcTextSize(label);
		Vector2 valueSize = ImGui.CalcTextSize(value);
		drawList.AddText(position, NeonDraw.Color(MonsoonPalette.TextDim), label);
		float valueX = position.X + MathF.Max(labelSize.X + 8f, width - valueSize.X);
		NeonDraw.Text(drawList, new Vector2(valueX, position.Y), valueColor ?? MonsoonPalette.Amber, value);
		float dotY = position.Y + (lineHeight * 0.7f);
		uint dot = NeonDraw.Color(MonsoonPalette.TextDim, 1f, 0.6f);
		for (float x = position.X + labelSize.X + 6f; x < valueX - 6f; x += 5f)
		{
			drawList.AddRectFilled(new Vector2(x, dotY), new Vector2(x + 1f, dotY + 1f), dot);
		}

		ImGui.Dummy(new Vector2(width, lineHeight));
		ImGuiProbes.MarkItem(label);
	}

	/// <summary>Draws a scrolling column of hexadecimal telemetry, the newest row brightest.</summary>
	/// <param name="id">The id, also the probe name.</param>
	/// <param name="lines">How many rows to show.</param>
	/// <param name="rowsPerSecond">How fast new rows arrive.</param>
	public static void HexStream(string id, int lines = 8, float rowsPerSecond = 6f)
	{
		Ensure.NotNull(id);
		int rows = Math.Max(1, lines);
		uint seed = Noise.SeedOf(id);
		long newest = (long)Math.Floor(MonsoonEffects.Time * rowsPerSecond * MonsoonEffects.RainSpeedScale);
		Vector2 position = ImGui.GetCursorScreenPos();
		float lineHeight = ImGui.GetTextLineHeight();
		ImDrawListPtr drawList = ImGui.GetWindowDrawList();
		float width = 0f;
		for (int i = 0; i < rows; i++)
		{
			long row = newest - (rows - 1 - i);
			string text = HexRow(seed, row);
			float age = (rows - 1 - i) / (float)rows;
			Vector4 color = i == rows - 1 ? MonsoonPalette.Cyan : MonsoonPalette.Mix(MonsoonPalette.Cyan, MonsoonPalette.TextDim, 0.4f + (age * 0.6f));
			drawList.AddText(position + new Vector2(0f, i * lineHeight), NeonDraw.Color(color, 1f, 1f - (age * 0.6f)), text);
			width = MathF.Max(width, ImGui.CalcTextSize(text).X);
		}

		ImGui.Dummy(new Vector2(width, rows * lineHeight));
		ImGuiProbes.MarkItem(id);
		if (rowsPerSecond > 0f && MonsoonEffects.Motion != MonsoonMotion.None)
		{
			MonsoonEffects.RequestFrame();
		}
	}

	/// <summary>Builds one row of the hex stream: an address and four words, all from the noise hash.</summary>
	/// <param name="seed">The stream seed.</param>
	/// <param name="row">The row number.</param>
	/// <returns>The row text.</returns>
	internal static string HexRow(uint seed, long row)
	{
		uint address = (uint)(row * 16) & 0xFFFFu;
		System.Text.StringBuilder builder = new();
		builder.Append(address.ToString("X4", System.Globalization.CultureInfo.InvariantCulture)).Append(" :");
		for (int w = 0; w < 4; w++)
		{
			uint word = (uint)(Noise.Hash(seed + (uint)w, row) * 65536f) & 0xFFFFu;
			builder.Append(' ').Append(word.ToString("X4", System.Globalization.CultureInfo.InvariantCulture));
		}

		return builder.ToString();
	}
}
