// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using Hexa.NET.ImGui;

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
}
