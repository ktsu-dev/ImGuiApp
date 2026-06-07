// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

#if IOS

namespace ktsu.ImGui.App;

using Hexa.NET.ImGui;

using UIKit;

/// <summary>
/// Maps hardware-keyboard <see cref="UIKeyboardHidUsage"/> codes (delivered via <c>pressesBegan</c> /
/// <c>pressesEnded</c>) to Dear ImGui <see cref="ImGuiKey"/> values, mirroring the desktop key table.
/// Keys ImGui has no slot for return <see cref="ImGuiKey.None"/> and are ignored by the caller.
/// </summary>
internal static class IOSKeyMap
{
	/// <summary>Translates a UIKit keyboard usage code to the matching <see cref="ImGuiKey"/>.</summary>
	/// <param name="usage">The hardware key usage from <c>UIKey.KeyCode</c>.</param>
	/// <returns>The mapped <see cref="ImGuiKey"/>, or <see cref="ImGuiKey.None"/> if unmapped.</returns>
	public static ImGuiKey Map(UIKeyboardHidUsage usage) => usage switch
	{
		UIKeyboardHidUsage.KeyboardTab => ImGuiKey.Tab,
		UIKeyboardHidUsage.KeyboardLeftArrow => ImGuiKey.LeftArrow,
		UIKeyboardHidUsage.KeyboardRightArrow => ImGuiKey.RightArrow,
		UIKeyboardHidUsage.KeyboardUpArrow => ImGuiKey.UpArrow,
		UIKeyboardHidUsage.KeyboardDownArrow => ImGuiKey.DownArrow,
		UIKeyboardHidUsage.KeyboardPageUp => ImGuiKey.PageUp,
		UIKeyboardHidUsage.KeyboardPageDown => ImGuiKey.PageDown,
		UIKeyboardHidUsage.KeyboardHome => ImGuiKey.Home,
		UIKeyboardHidUsage.KeyboardEnd => ImGuiKey.End,
		UIKeyboardHidUsage.KeyboardInsert => ImGuiKey.Insert,
		UIKeyboardHidUsage.KeyboardDeleteForward => ImGuiKey.Delete,
		UIKeyboardHidUsage.KeyboardDeleteOrBackspace => ImGuiKey.Backspace,
		UIKeyboardHidUsage.KeyboardSpacebar => ImGuiKey.Space,
		UIKeyboardHidUsage.KeyboardReturnOrEnter => ImGuiKey.Enter,
		UIKeyboardHidUsage.KeyboardEscape => ImGuiKey.Escape,
		UIKeyboardHidUsage.KeyboardQuote => ImGuiKey.Apostrophe,
		UIKeyboardHidUsage.KeyboardComma => ImGuiKey.Comma,
		UIKeyboardHidUsage.KeyboardHyphen => ImGuiKey.Minus,
		UIKeyboardHidUsage.KeyboardPeriod => ImGuiKey.Period,
		UIKeyboardHidUsage.KeyboardSlash => ImGuiKey.Slash,
		UIKeyboardHidUsage.KeyboardSemicolon => ImGuiKey.Semicolon,
		UIKeyboardHidUsage.KeyboardEqualSign => ImGuiKey.Equal,
		UIKeyboardHidUsage.KeyboardOpenBracket => ImGuiKey.LeftBracket,
		UIKeyboardHidUsage.KeyboardBackslash => ImGuiKey.Backslash,
		UIKeyboardHidUsage.KeyboardCloseBracket => ImGuiKey.RightBracket,
		UIKeyboardHidUsage.KeyboardGraveAccentAndTilde => ImGuiKey.GraveAccent,
		UIKeyboardHidUsage.KeyboardCapsLock => ImGuiKey.CapsLock,
		UIKeyboardHidUsage.KeyboardScrollLock => ImGuiKey.ScrollLock,
		UIKeyboardHidUsage.KeypadNumLock => ImGuiKey.NumLock,
		UIKeyboardHidUsage.KeyboardPrintScreen => ImGuiKey.PrintScreen,
		UIKeyboardHidUsage.KeyboardPause => ImGuiKey.Pause,
		UIKeyboardHidUsage.Keypad0 => ImGuiKey.Keypad0,
		UIKeyboardHidUsage.Keypad1 => ImGuiKey.Keypad1,
		UIKeyboardHidUsage.Keypad2 => ImGuiKey.Keypad2,
		UIKeyboardHidUsage.Keypad3 => ImGuiKey.Keypad3,
		UIKeyboardHidUsage.Keypad4 => ImGuiKey.Keypad4,
		UIKeyboardHidUsage.Keypad5 => ImGuiKey.Keypad5,
		UIKeyboardHidUsage.Keypad6 => ImGuiKey.Keypad6,
		UIKeyboardHidUsage.Keypad7 => ImGuiKey.Keypad7,
		UIKeyboardHidUsage.Keypad8 => ImGuiKey.Keypad8,
		UIKeyboardHidUsage.Keypad9 => ImGuiKey.Keypad9,
		UIKeyboardHidUsage.KeypadPeriod => ImGuiKey.KeypadDecimal,
		UIKeyboardHidUsage.KeypadSlash => ImGuiKey.KeypadDivide,
		UIKeyboardHidUsage.KeypadAsterisk => ImGuiKey.KeypadMultiply,
		UIKeyboardHidUsage.KeypadHyphen => ImGuiKey.KeypadSubtract,
		UIKeyboardHidUsage.KeypadPlus => ImGuiKey.KeypadAdd,
		UIKeyboardHidUsage.KeypadEnter => ImGuiKey.KeypadEnter,
		UIKeyboardHidUsage.KeypadEqualSign => ImGuiKey.KeypadEqual,
		UIKeyboardHidUsage.KeyboardLeftShift => ImGuiKey.LeftShift,
		UIKeyboardHidUsage.KeyboardLeftControl => ImGuiKey.LeftCtrl,
		UIKeyboardHidUsage.KeyboardLeftAlt => ImGuiKey.LeftAlt,
		UIKeyboardHidUsage.KeyboardLeftGui => ImGuiKey.LeftSuper,
		UIKeyboardHidUsage.KeyboardRightShift => ImGuiKey.RightShift,
		UIKeyboardHidUsage.KeyboardRightControl => ImGuiKey.RightCtrl,
		UIKeyboardHidUsage.KeyboardRightAlt => ImGuiKey.RightAlt,
		UIKeyboardHidUsage.KeyboardRightGui => ImGuiKey.RightSuper,
		UIKeyboardHidUsage.KeyboardApplication => ImGuiKey.Menu,
		UIKeyboardHidUsage.Keyboard0 => ImGuiKey.Key0,
		UIKeyboardHidUsage.Keyboard1 => ImGuiKey.Key1,
		UIKeyboardHidUsage.Keyboard2 => ImGuiKey.Key2,
		UIKeyboardHidUsage.Keyboard3 => ImGuiKey.Key3,
		UIKeyboardHidUsage.Keyboard4 => ImGuiKey.Key4,
		UIKeyboardHidUsage.Keyboard5 => ImGuiKey.Key5,
		UIKeyboardHidUsage.Keyboard6 => ImGuiKey.Key6,
		UIKeyboardHidUsage.Keyboard7 => ImGuiKey.Key7,
		UIKeyboardHidUsage.Keyboard8 => ImGuiKey.Key8,
		UIKeyboardHidUsage.Keyboard9 => ImGuiKey.Key9,
		UIKeyboardHidUsage.KeyboardA => ImGuiKey.A,
		UIKeyboardHidUsage.KeyboardB => ImGuiKey.B,
		UIKeyboardHidUsage.KeyboardC => ImGuiKey.C,
		UIKeyboardHidUsage.KeyboardD => ImGuiKey.D,
		UIKeyboardHidUsage.KeyboardE => ImGuiKey.E,
		UIKeyboardHidUsage.KeyboardF => ImGuiKey.F,
		UIKeyboardHidUsage.KeyboardG => ImGuiKey.G,
		UIKeyboardHidUsage.KeyboardH => ImGuiKey.H,
		UIKeyboardHidUsage.KeyboardI => ImGuiKey.I,
		UIKeyboardHidUsage.KeyboardJ => ImGuiKey.J,
		UIKeyboardHidUsage.KeyboardK => ImGuiKey.K,
		UIKeyboardHidUsage.KeyboardL => ImGuiKey.L,
		UIKeyboardHidUsage.KeyboardM => ImGuiKey.M,
		UIKeyboardHidUsage.KeyboardN => ImGuiKey.N,
		UIKeyboardHidUsage.KeyboardO => ImGuiKey.O,
		UIKeyboardHidUsage.KeyboardP => ImGuiKey.P,
		UIKeyboardHidUsage.KeyboardQ => ImGuiKey.Q,
		UIKeyboardHidUsage.KeyboardR => ImGuiKey.R,
		UIKeyboardHidUsage.KeyboardS => ImGuiKey.S,
		UIKeyboardHidUsage.KeyboardT => ImGuiKey.T,
		UIKeyboardHidUsage.KeyboardU => ImGuiKey.U,
		UIKeyboardHidUsage.KeyboardV => ImGuiKey.V,
		UIKeyboardHidUsage.KeyboardW => ImGuiKey.W,
		UIKeyboardHidUsage.KeyboardX => ImGuiKey.X,
		UIKeyboardHidUsage.KeyboardY => ImGuiKey.Y,
		UIKeyboardHidUsage.KeyboardZ => ImGuiKey.Z,
		UIKeyboardHidUsage.KeyboardF1 => ImGuiKey.F1,
		UIKeyboardHidUsage.KeyboardF2 => ImGuiKey.F2,
		UIKeyboardHidUsage.KeyboardF3 => ImGuiKey.F3,
		UIKeyboardHidUsage.KeyboardF4 => ImGuiKey.F4,
		UIKeyboardHidUsage.KeyboardF5 => ImGuiKey.F5,
		UIKeyboardHidUsage.KeyboardF6 => ImGuiKey.F6,
		UIKeyboardHidUsage.KeyboardF7 => ImGuiKey.F7,
		UIKeyboardHidUsage.KeyboardF8 => ImGuiKey.F8,
		UIKeyboardHidUsage.KeyboardF9 => ImGuiKey.F9,
		UIKeyboardHidUsage.KeyboardF10 => ImGuiKey.F10,
		UIKeyboardHidUsage.KeyboardF11 => ImGuiKey.F11,
		UIKeyboardHidUsage.KeyboardF12 => ImGuiKey.F12,
		_ => ImGuiKey.None,
	};
}

#endif
