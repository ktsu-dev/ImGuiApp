// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Widgets;

using System.Collections.Generic;
using System.Text;

using Hexa.NET.ImGui;

/// <summary>
/// Provides custom ImGui widgets.
/// </summary>
public static partial class ImGuiWidgets
{
	/// <summary>
	/// Draws an N-box PIN / one-time-passcode entry. Typing fills the boxes left to right and auto-advances
	/// to the next box; pressing Backspace in an empty box clears the previous box and moves focus back.
	/// The value is kept left-packed (no gaps) and, when <paramref name="digitsOnly"/> is set, restricted to digits.
	/// </summary>
	/// <param name="id">A unique identifier for the widget group.</param>
	/// <param name="value">The current entry. Updated in place; never longer than <paramref name="length"/>.</param>
	/// <param name="length">The number of boxes.</param>
	/// <param name="masked">When <see langword="true"/> the characters are rendered as dots instead of the typed glyphs.</param>
	/// <param name="digitsOnly">When <see langword="true"/> only decimal digits are accepted.</param>
	/// <returns><see langword="true"/> if the value changed this frame; otherwise <see langword="false"/>.</returns>
	public static bool PinInput(string id, ref string value, int length = 6, bool masked = false, bool digitsOnly = true) =>
		PinInputImpl.Draw(id, ref value, length, masked, digitsOnly);

	/// <summary>
	/// Cleans an arbitrary string into a left-packed PIN value: optionally strips non-digit characters and
	/// truncates to <paramref name="length"/>. Pure helper backing <see cref="PinInput"/>; safe to call without a GL context.
	/// </summary>
	/// <param name="value">The raw text to clean (may be <see langword="null"/>).</param>
	/// <param name="length">The maximum number of characters to keep.</param>
	/// <param name="digitsOnly">When <see langword="true"/> only decimal digits are retained.</param>
	/// <returns>The cleaned, length-clamped value.</returns>
	public static string NormalizePin(string? value, int length, bool digitsOnly)
	{
		if (string.IsNullOrEmpty(value) || length <= 0)
		{
			return string.Empty;
		}

		StringBuilder builder = new(length);
		foreach (char c in value)
		{
			if (digitsOnly && !char.IsDigit(c))
			{
				continue;
			}

			builder.Append(c);
			if (builder.Length >= length)
			{
				break;
			}
		}

		return builder.ToString();
	}

	/// <summary>
	/// Returns a copy of <paramref name="value"/> with the slot at <paramref name="index"/> set to
	/// <paramref name="character"/>, or cleared when <paramref name="character"/> is <see langword="null"/>. The result
	/// stays left-packed: setting beyond the current end appends, clearing removes (shifting following characters left).
	/// Pure helper backing <see cref="PinInput"/>; safe to call without a GL context.
	/// </summary>
	/// <param name="value">The current left-packed value.</param>
	/// <param name="index">The zero-based slot to modify.</param>
	/// <param name="character">The character to place, or <see langword="null"/> to clear the slot.</param>
	/// <param name="length">The total number of slots.</param>
	/// <returns>The updated value, never longer than <paramref name="length"/>.</returns>
	public static string SetPinSlot(string value, int index, char? character, int length)
	{
		value ??= string.Empty;
		if (index < 0 || index >= length)
		{
			return value;
		}

		List<char> chars = [.. value];

		if (character.HasValue)
		{
			if (index < chars.Count)
			{
				chars[index] = character.Value;
			}
			else if (chars.Count < length)
			{
				// Append at the next free slot; clicking ahead of the cursor still fills contiguously.
				chars.Add(character.Value);
			}
		}
		else if (index < chars.Count)
		{
			chars.RemoveAt(index);
		}

		if (chars.Count > length)
		{
			chars.RemoveRange(length, chars.Count - length);
		}

		return new string([.. chars]);
	}

	internal static class PinInputImpl
	{
		// Per-ID box index that should grab keyboard focus next frame; -1 when there is no pending request.
		private static readonly Dictionary<uint, int> FocusRequest = [];

		public static bool Draw(string id, ref string value, int length, bool masked, bool digitsOnly)
		{
			if (length < 1)
			{
				length = 1;
			}

			value = NormalizePin(value, length, digitsOnly);

			uint groupId = ImGui.GetID(id);
			int focusReq = FocusRequest.GetValueOrDefault(groupId, -1);

			ImGui.PushID(id);

			float box = ImGui.GetFrameHeight() * 1.2f;
			ImGuiInputTextFlags flags = ImGuiInputTextFlags.AutoSelectAll;
			if (digitsOnly)
			{
				flags |= ImGuiInputTextFlags.CharsDecimal;
			}

			if (masked)
			{
				flags |= ImGuiInputTextFlags.Password;
			}

			bool changed = false;

			for (int i = 0; i < length; i++)
			{
				changed |= DrawPinBox(groupId, ref value, i, length, focusReq, box, digitsOnly, flags);
			}

			// Drop the focus request only if it was consumed (not replaced by a new one this frame).
			if (focusReq >= 0 && FocusRequest.GetValueOrDefault(groupId, -1) == focusReq)
			{
				FocusRequest.Remove(groupId);
			}

			ImGui.PopID();
			return changed;
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S107:Methods should not have too many parameters", Justification = "Private rendering helper extracted from Draw to reduce cognitive complexity; the parameters thread the per-frame PIN box state from the caller and bundling them would not improve readability.")]
		private static bool DrawPinBox(uint groupId, ref string value, int i, int length, int focusReq, float box, bool digitsOnly, ImGuiInputTextFlags flags)
		{
			if (i > 0)
			{
				ImGui.SameLine(0.0f, ImGui.GetStyle().ItemInnerSpacing.X);
			}

			ImGui.PushID(i);
			ImGui.SetNextItemWidth(box);

			if (focusReq == i)
			{
				ImGui.SetKeyboardFocusHere();
			}

			bool empty = i >= value.Length;
			string slot = empty ? string.Empty : value[i].ToString();
			string edited = slot;

			bool changed = false;
			if (ImGui.InputText("##slot", ref edited, 8u, flags))
			{
				string cleaned = NormalizePin(edited, 2, digitsOnly);
				char? typed = cleaned.Length > 0 ? cleaned[^1] : null;
				value = SetPinSlot(value, i, typed, length);
				changed = true;

				// Auto-advance after a successful keystroke.
				if (typed.HasValue && i + 1 < length)
				{
					FocusRequest[groupId] = i + 1;
				}
			}
			else if (empty && i > 0 && ImGui.IsItemFocused() && ImGui.IsKeyPressed(ImGuiKey.Backspace))
			{
				// Backspace in an empty box clears the previous box and steps focus back.
				value = SetPinSlot(value, i - 1, null, length);
				FocusRequest[groupId] = i - 1;
				changed = true;
			}

			ImGui.PopID();
			return changed;
		}
	}
}
