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

	[TestMethod]
	public void Compose_AndDisplay_NameKeysTheWayAChordIsWritten()
	{
		KeyChordModifiers ctrl = new(Ctrl: true, Alt: false, Shift: false, Meta: false);
		Assert.AreEqual("Ctrl+=", KeyChordMatcher.Display(KeyChordMatcher.Compose(ctrl, ImGuiKey.Equal)));
		Assert.AreEqual("Ctrl+Shift+7", KeyChordMatcher.Display(KeyChordMatcher.Compose(ctrl with { Shift = true }, ImGuiKey.Key7)));
		Assert.AreEqual("Alt+Up", KeyChordMatcher.Display(KeyChordMatcher.Compose(new(Ctrl: false, Alt: true, Shift: false, Meta: false), ImGuiKey.UpArrow)));
		Assert.AreEqual("Ctrl+Alt+Shift+Super+A", KeyChordMatcher.Display(KeyChordMatcher.Compose(new(Ctrl: true, Alt: true, Shift: true, Meta: true), ImGuiKey.A)));
		Assert.AreEqual("F2", KeyChordMatcher.Display(KeyChordMatcher.Compose(default, ImGuiKey.F2)));
		Assert.AreEqual(Chord.Parse("Ctrl+]"), KeyChordMatcher.Compose(ctrl, ImGuiKey.RightBracket));
	}

	/// <summary>Tab shares its value with the NamedKeyBegin range marker, so it is the key a fallback to ToString() misnames.</summary>
	[TestMethod]
	public void Tab_IsNamedTabNotItsRangeMarker()
	{
		KeyChordModifiers ctrl = new(Ctrl: true, Alt: false, Shift: false, Meta: false);
		Assert.AreEqual("Tab", KeyChordMatcher.DisplayName(ImGuiKey.Tab));
		Assert.AreEqual("Shift+Tab", KeyChordMatcher.Display(Chord.Parse("Shift+Tab")));
		Assert.AreEqual("TAB", KeyChordMatcher.Compose(default, ImGuiKey.Tab).ToString());
		Assert.AreEqual(Chord.Parse("Tab"), KeyChordMatcher.Compose(default, ImGuiKey.Tab));
		Assert.AreEqual(Chord.Parse("Ctrl+Tab"), KeyChordMatcher.Compose(ctrl, ImGuiKey.Tab));
		Assert.AreEqual(Chord.Parse("Shift+Tab"), KeyChordMatcher.Compose(new(Ctrl: false, Alt: false, Shift: true, Meta: false), ImGuiKey.Tab));
	}

	/// <summary>A keymap saved while Tab was captured as NAMEDKEYBEGIN still reads back as Tab.</summary>
	[TestMethod]
	public void TryMapKey_StillReadsTheOldNamedKeyBeginSpelling()
	{
		Assert.IsTrue(KeyChordMatcher.TryMapKey("NAMEDKEYBEGIN", out ImGuiKey key));
		Assert.AreEqual(ImGuiKey.Tab, key);
	}

	/// <summary>No bindable key is ever named after one of the enum's range markers.</summary>
	[TestMethod]
	public void NoBindableKey_IsNamedAfterARangeMarker()
	{
		// The markers are the Begin, End and Count names bounding each range, and GamepadStart. End alone is a key.
		string[] markers =
		[
			.. Enum.GetNames<ImGuiKey>().Where(static name => name != nameof(ImGuiKey.End)
				&& (name.EndsWith("Begin", StringComparison.Ordinal) || name.EndsWith("End", StringComparison.Ordinal) || name.EndsWith("Count", StringComparison.Ordinal) || name == nameof(ImGuiKey.GamepadStart))),
		];
		for (ImGuiKey key = ImGuiKey.NamedKeyBegin; key < ImGuiKey.GamepadStart; key++)
		{
			if (!KeyChordMatcher.IsBindable(key))
			{
				continue;
			}

			string display = KeyChordMatcher.DisplayName(key);
			string note = KeyChordMatcher.Compose(default, key).ToString();
			Assert.IsFalse(markers.Contains(display, StringComparer.OrdinalIgnoreCase), $"{(int)key} displays as the range marker {display}.");
			Assert.IsFalse(markers.Contains(note, StringComparer.OrdinalIgnoreCase), $"{(int)key} is stored as the range marker {note}.");
		}
	}

	/// <summary>A chord parsed with an alias reads the way a menu writes it.</summary>
	[TestMethod]
	[DataRow("Control+S", "Ctrl+S")]
	[DataRow("Win+E", "Super+E")]
	[DataRow("Ctrl+Up", "Ctrl+Up")]
	[DataRow("Shift+ArrowLeft", "Shift+Left")]
	[DataRow("Ctrl+Shift+/", "Ctrl+Shift+/")]
	[DataRow("Delete", "Delete")]
	public void Display_NormalizesModifiersAndKeys(string text, string expected) =>
		Assert.AreEqual(expected, KeyChordMatcher.Display(Chord.Parse(text)));

	[TestMethod]
	public void Display_ShowsNothingForNoChord() =>
		Assert.AreEqual(string.Empty, KeyChordMatcher.Display(null));

	[TestMethod]
	public void Display_ShowsAKeyImGuiCannotPressAsWritten()
	{
		Chord chord = Chord.Parse("Ctrl+NotAKey");
		Assert.AreEqual(chord.ToString(), KeyChordMatcher.Display(chord));
	}

	/// <summary>
	/// What a capture writes into a keymap must read back as the same key, for every key a chord can end on.
	/// </summary>
	[TestMethod]
	public void Compose_RoundTripsEveryBindableKey()
	{
		for (ImGuiKey key = ImGuiKey.NamedKeyBegin; key < ImGuiKey.GamepadStart; key++)
		{
			if (!KeyChordMatcher.IsBindable(key))
			{
				continue;
			}

			Chord chord = KeyChordMatcher.Compose(new(Ctrl: true, Alt: false, Shift: true, Meta: false), key);
			Assert.IsTrue(KeyChordMatcher.TrySplit(chord, out KeyChordModifiers modifiers, out System.Collections.Generic.List<ImGuiKey> keys), $"{key} did not read back.");
			Assert.AreEqual(new KeyChordModifiers(Ctrl: true, Alt: false, Shift: true, Meta: false), modifiers);
			Assert.AreEqual(key, keys.Single(), $"{key} read back as another key.");
		}
	}

	[TestMethod]
	[DataRow(ImGuiKey.LeftCtrl)]
	[DataRow(ImGuiKey.RightSuper)]
	[DataRow(ImGuiKey.MouseLeft)]
	[DataRow(ImGuiKey.GamepadStart)]
	public void Compose_RefusesAKeyThatCannotEndAChord(ImGuiKey key) =>
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => KeyChordMatcher.Compose(default, key));

	/// <summary>The one key a parsed chord names besides its modifiers, or None if it doesn't map.</summary>
	private static ImGuiKey KeyOf(string text)
	{
		string name = Chord.Parse(text).Notes
			.Select(note => note.ToString())
			.Single(note => note is not ("CTRL" or "SHIFT" or "ALT"));
		return KeyChordMatcher.TryMapKey(name, out ImGuiKey key) ? key : ImGuiKey.None;
	}
}
