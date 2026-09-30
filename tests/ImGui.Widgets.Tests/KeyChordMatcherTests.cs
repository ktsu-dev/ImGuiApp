// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using System;
using System.Linq;

using Hexa.NET.ImGui;

using ktsu.Keybinding.Core.Models;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests how the shared matcher names keys. Pure, so no ImGui context is required.
/// </summary>
[TestClass]
public class KeyChordMatcherTests
{
	[TestMethod]
	[DataRow("A", ImGuiKey.A)]
	[DataRow("7", ImGuiKey.Key7)]
	[DataRow("UP", ImGuiKey.UpArrow)]
	[DataRow("PAGEUP", ImGuiKey.PageUp)]
	[DataRow("HOME", ImGuiKey.Home)]
	[DataRow("F2", ImGuiKey.F2)]
	[DataRow("ESCAPE", ImGuiKey.Escape)]
	[DataRow("SPACE", ImGuiKey.Space)]
	[DataRow("TAB", ImGuiKey.Tab)]
	[DataRow("COMMA", ImGuiKey.Comma)]
	public void TryMapKey_MapsTheNamesAKeymapUses(string name, ImGuiKey expected)
	{
		Assert.IsTrue(KeyChordMatcher.TryMapKey(name, out ImGuiKey key), $"'{name}' did not map to any key.");
		Assert.AreEqual(expected, key);
	}

	[TestMethod]
	[DataRow("LEFTCTRL")]
	[DataRow("NOTAKEY")]
	public void TryMapKey_RejectsModifiersAndUnknownNames(string name) =>
		Assert.IsFalse(KeyChordMatcher.TryMapKey(name, out _), $"'{name}' should not map to a key a chord can press.");

	/// <summary>
	/// Tab is the first named key, so it sits exactly on the lower bound (ktsu-dev/ImGuiApp#477).
	/// </summary>
	[TestMethod]
	[DataRow("Tab")]
	[DataRow("Ctrl+Tab")]
	[DataRow("Ctrl+Shift+Tab")]
	public void TryMapKey_MapsTabInAChord(string text) =>
		Assert.AreEqual(ImGuiKey.Tab, KeyOf(text));

	/// <summary>
	/// Every keyboard key ImGui names maps back to itself by that name, so no bound drops one.
	/// </summary>
	[TestMethod]
	public void TryMapKey_RoundTripsEveryNamedKeyboardKey()
	{
		ImGuiKey[] modifiers = [ImGuiKey.LeftCtrl, ImGuiKey.RightCtrl, ImGuiKey.LeftShift, ImGuiKey.RightShift,
			ImGuiKey.LeftAlt, ImGuiKey.RightAlt, ImGuiKey.LeftSuper, ImGuiKey.RightSuper];

		for (ImGuiKey expected = ImGuiKey.NamedKeyBegin; expected < ImGuiKey.GamepadStart; expected++)
		{
			if (modifiers.Contains(expected))
			{
				continue;
			}

			string name = expected.ToString().ToUpperInvariant();
			Assert.IsTrue(KeyChordMatcher.TryMapKey(name, out ImGuiKey key), $"'{name}' did not map to any key.");
			Assert.AreEqual(expected, key, $"'{name}' mapped to the wrong key.");
		}
	}

	/// <summary>
	/// A chord written with a symbol keeps the symbol as its key's name, which must still map.
	/// </summary>
	[TestMethod]
	[DataRow("Ctrl+=", ImGuiKey.Equal)]
	[DataRow("Ctrl+,", ImGuiKey.Comma)]
	[DataRow("Ctrl+/", ImGuiKey.Slash)]
	[DataRow("Ctrl+.", ImGuiKey.Period)]
	[DataRow("Ctrl+;", ImGuiKey.Semicolon)]
	[DataRow("Ctrl+[", ImGuiKey.LeftBracket)]
	[DataRow("Ctrl+]", ImGuiKey.RightBracket)]
	[DataRow("Ctrl+\\", ImGuiKey.Backslash)]
	[DataRow("Ctrl+'", ImGuiKey.Apostrophe)]
	[DataRow("Ctrl+`", ImGuiKey.GraveAccent)]
	public void TryMapKey_MapsSymbolSpellings(string text, ImGuiKey expected) =>
		Assert.AreEqual(expected, KeyOf(text));

	/// <summary>The one key a parsed chord names besides its modifiers, or None if it doesn't map.</summary>
	private static ImGuiKey KeyOf(string text)
	{
		string name = Chord.Parse(text).Notes
			.Select(note => note.ToString())
			.Single(note => note is not ("CTRL" or "SHIFT" or "ALT"));
		return KeyChordMatcher.TryMapKey(name, out ImGuiKey key) ? key : ImGuiKey.None;
	}
}
