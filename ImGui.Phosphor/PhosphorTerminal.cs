// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Phosphor;

using System;
using System.Collections.Generic;
using System.Numerics;
using Hexa.NET.ImGui;
using ktsu.ImGui.Probes;

/// <summary>
/// A teletype console: lines written to it are typed out a character at a time behind a blinking block
/// cursor, and the oldest lines scroll away once it holds more than <see cref="MaxLines"/>.
/// </summary>
/// <param name="charactersPerSecond">How fast text is typed out; zero or less shows it at once.</param>
public sealed class PhosphorTerminal(float charactersPerSecond = 60f)
{
	private readonly List<string> lines = [];
	private int revealed;
	private double carry;

	/// <summary>Gets or sets how fast text is typed out, in characters per second; zero or less shows it at once.</summary>
	public float CharactersPerSecond { get; set; } = charactersPerSecond;

	/// <summary>Gets or sets how many lines the terminal keeps before the oldest scroll away.</summary>
	public int MaxLines { get; set; } = 200;

	/// <summary>Gets a value indicating whether text is still being typed out.</summary>
	public bool IsTyping => revealed < TotalCharacters;

	/// <summary>Gets the lines written so far, whether or not they have been typed out yet.</summary>
	public IReadOnlyList<string> Lines => lines;

	private int TotalCharacters
	{
		get
		{
			int total = 0;
			foreach (string line in lines)
			{
				total += line.Length;
			}

			return total;
		}
	}

	/// <summary>Queues a line to be typed out.</summary>
	/// <param name="text">The line; any line breaks in it start further lines.</param>
	public void WriteLine(string text)
	{
		Ensure.NotNull(text);
		foreach (string line in text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
		{
			lines.Add(line);
		}

		while (lines.Count > Math.Max(MaxLines, 1))
		{
			revealed = Math.Max(revealed - lines[0].Length, 0);
			lines.RemoveAt(0);
		}
	}

	/// <summary>Empties the terminal.</summary>
	public void Clear()
	{
		lines.Clear();
		revealed = 0;
		carry = 0;
	}

	/// <summary>Shows everything written so far without waiting for it to be typed.</summary>
	public void SkipToEnd() => revealed = TotalCharacters;

	/// <summary>Advances the typing by a span of time.</summary>
	/// <param name="seconds">Seconds elapsed.</param>
	public void Advance(float seconds)
	{
		int total = TotalCharacters;
		if (CharactersPerSecond <= 0f)
		{
			revealed = total;
			return;
		}

		carry += seconds * CharactersPerSecond;
		int whole = (int)carry;
		carry -= whole;
		revealed = Math.Min(revealed + whole, total);
		if (revealed == total)
		{
			carry = 0;
		}
	}

	/// <summary>Gets the text typed out so far, one entry per line begun.</summary>
	/// <returns>The visible lines.</returns>
	public IReadOnlyList<string> VisibleLines()
	{
		List<string> visible = [];
		int remaining = revealed;
		foreach (string line in lines)
		{
			if (remaining <= 0 && visible.Count > 0)
			{
				break;
			}

			int shown = Math.Min(line.Length, remaining);
			visible.Add(line[..shown]);
			remaining -= shown;
			if (shown < line.Length)
			{
				break;
			}
		}

		return visible;
	}

	/// <summary>Draws the terminal, advancing the typing by the frame's elapsed time.</summary>
	/// <param name="id">The terminal's identity.</param>
	/// <param name="size">Size of the terminal; the available space where a component is zero or less.</param>
	public void Draw(string id, Vector2 size = default)
	{
		Ensure.NotNull(id);
		Advance(ImGui.GetIO().DeltaTime);
		PhosphorPalette palette = PhosphorStyle.Palette;

		ImGui.PushStyleColor(ImGuiCol.ChildBg, palette.Screen);
		bool visible = ImGui.BeginChild(id, size, ImGuiChildFlags.Borders);
		if (visible)
		{
			ImDrawListPtr drawList = ImGui.GetWindowDrawList();
			IReadOnlyList<string> shown = VisibleLines();
			for (int i = 0; i < shown.Count; i++)
			{
				Vector2 position = ImGui.GetCursorScreenPos();
				PhosphorDraw.GlowText(drawList, position, palette.Bright, shown[i]);
				ImGui.Dummy(new Vector2(ImGui.CalcTextSize(shown[i]).X, ImGui.GetTextLineHeight()));
				if (i == shown.Count - 1)
				{
					DrawCursor(drawList, position + new Vector2(ImGui.CalcTextSize(shown[i]).X + 1f, 0f), palette);
				}
			}

			if (shown.Count == 0)
			{
				DrawCursor(drawList, ImGui.GetCursorScreenPos(), palette);
			}

			if (IsTyping)
			{
				ImGui.SetScrollHereY(1f);
			}
		}

		ImGui.EndChild();
		ImGui.PopStyleColor();
		ImGuiProbes.MarkItem("terminal", id);
		AnimationActivity.RequestActiveFrame();
	}

	private static void DrawCursor(ImDrawListPtr drawList, Vector2 position, PhosphorPalette palette)
	{
		if (PhosphorDraw.BlinkPhase(2f))
		{
			float height = ImGui.GetTextLineHeight();
			drawList.AddRectFilled(position, position + new Vector2(height * 0.55f, height), PhosphorDraw.U32(palette.Hot));
		}
	}
}
