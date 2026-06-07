// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

#if IOS

namespace ktsu.ImGui.App;

using System.Numerics;

using Hexa.NET.ImGui;

/// <summary>
/// iOS input bridge for <see cref="ImGuiApp"/>: forwards touch, hardware-keyboard, and soft-keyboard
/// events (raised by <c>ImGuiAppViewController</c>) into Dear ImGui's IO. Touch maps to the single
/// ImGui mouse (§2.3 of the port plan); a multi-touch sidecar is deferred. Each event also refreshes
/// the idle timer so input wakes the app from its idle frame-rate.
/// </summary>
public static partial class ImGuiApp
{
	/// <summary>Reports the primary pointer (touch / pencil) position, in logical points.</summary>
	/// <param name="position">The pointer location in view points, matching <c>io.DisplaySize</c>.</param>
	internal static void OnPointerMoved(Vector2 position)
	{
		OnUserInput();
		ImGui.GetIO().AddMousePosEvent(position.X, position.Y);
	}

	/// <summary>Reports a primary-pointer press or release as the left ImGui mouse button.</summary>
	/// <param name="down"><see langword="true"/> on touch-down, <see langword="false"/> on touch-up.</param>
	internal static void OnPointerButton(bool down)
	{
		OnUserInput();
		ImGui.GetIO().AddMouseButtonEvent(0, down);
	}

	/// <summary>Reports a key press or release, translated to an <see cref="ImGuiKey"/>.</summary>
	/// <param name="key">The mapped ImGui key; <see cref="ImGuiKey.None"/> is ignored.</param>
	/// <param name="down"><see langword="true"/> for key-down, <see langword="false"/> for key-up.</param>
	internal static void OnKey(ImGuiKey key, bool down)
	{
		if (key == ImGuiKey.None)
		{
			return;
		}

		OnUserInput();
		ImGui.GetIO().AddKeyEvent(key, down);
	}

	/// <summary>Updates the four ImGui keyboard modifier states from the current hardware-key flags.</summary>
	/// <param name="ctrl">Whether a Control key is held.</param>
	/// <param name="shift">Whether a Shift key is held.</param>
	/// <param name="alt">Whether an Alt/Option key is held.</param>
	/// <param name="super">Whether a Command/Super key is held.</param>
	internal static void OnModifiers(bool ctrl, bool shift, bool alt, bool super)
	{
		ImGuiIOPtr io = ImGui.GetIO();
		io.AddKeyEvent(ImGuiKey.ModCtrl, ctrl);
		io.AddKeyEvent(ImGuiKey.ModShift, shift);
		io.AddKeyEvent(ImGuiKey.ModAlt, alt);
		io.AddKeyEvent(ImGuiKey.ModSuper, super);
	}

	/// <summary>Feeds a typed Unicode codepoint into ImGui's text-input queue.</summary>
	/// <param name="codepoint">The UTF-32 codepoint entered by the user.</param>
	internal static void OnTextInput(uint codepoint)
	{
		OnUserInput();
		ImGui.GetIO().AddInputCharacter(codepoint);
	}

	/// <summary>
	/// Gets a value indicating whether ImGui currently wants text input (an input widget is active),
	/// so the view controller can present or dismiss the soft keyboard.
	/// </summary>
	internal static bool WantsTextInput => Renderer is not null && ImGui.GetIO().WantTextInput;
}

#endif
