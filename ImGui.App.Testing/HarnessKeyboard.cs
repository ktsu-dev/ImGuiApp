// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Testing;

using Hexa.NET.ImGui;

/// <summary>
/// Injects keyboard events straight into ImGui's event queue. Shortcuts go through key events,
/// while typed text goes through the character queue, which is what ImGui text widgets read.
/// </summary>
/// <param name="harness">The harness whose frames these events feed.</param>
public sealed class HarnessKeyboard(ImGuiAppHarness harness)
{
	/// <summary>Presses a key down without advancing a frame.</summary>
	/// <param name="key">The key to press.</param>
	public static void KeyDown(ImGuiKey key) => ImGui.GetIO().AddKeyEvent(key, true);

	/// <summary>Releases a key without advancing a frame.</summary>
	/// <param name="key">The key to release.</param>
	public static void KeyUp(ImGuiKey key) => ImGui.GetIO().AddKeyEvent(key, false);

	/// <summary>
	/// Presses and releases a key with optional modifiers, advancing the frames the application
	/// needs in order to observe the press.
	/// </summary>
	/// <param name="key">The key to press.</param>
	/// <param name="ctrl">Whether control is held.</param>
	/// <param name="shift">Whether shift is held.</param>
	/// <param name="alt">Whether alt is held.</param>
	public void Press(ImGuiKey key, bool ctrl = false, bool shift = false, bool alt = false)
	{
		Ensure.NotNull(harness);

		ImGuiIOPtr io = ImGui.GetIO();

		SetModifiers(io, ctrl, shift, alt, down: true);
		io.AddKeyEvent(key, true);
		harness.Step();

		io.AddKeyEvent(key, false);
		SetModifiers(io, ctrl, shift, alt, down: false);
		harness.Step();
	}

	/// <summary>
	/// Types text one character per frame, through the same character queue a real keyboard feeds.
	/// </summary>
	/// <param name="text">The text to type.</param>
	public void Type(string text)
	{
		Ensure.NotNull(harness);
		Ensure.NotNull(text);

		foreach (char character in text)
		{
			ImGui.GetIO().AddInputCharacter(character);
			harness.Step();
		}
	}

	private static void SetModifiers(ImGuiIOPtr io, bool ctrl, bool shift, bool alt, bool down)
	{
		if (ctrl)
		{
			io.AddKeyEvent(ImGuiKey.ModCtrl, down);
		}

		if (shift)
		{
			io.AddKeyEvent(ImGuiKey.ModShift, down);
		}

		if (alt)
		{
			io.AddKeyEvent(ImGuiKey.ModAlt, down);
		}
	}
}
