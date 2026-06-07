// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

#if IOS

namespace ktsu.ImGui.App;

using System.Text;

using Foundation;

using Hexa.NET.ImGui;

using ObjCRuntime;

using UIKit;

/// <summary>
/// An invisible, zero-sized first-responder that surfaces the iOS soft keyboard and funnels typed
/// text into Dear ImGui. The view controller makes it the first responder while
/// <see cref="ImGuiApp.WantsTextInput"/> is set (an ImGui input widget is active) and resigns it
/// otherwise. Conforming to <see cref="IUIKeyInput"/> is enough for UIKit to present the keyboard and
/// route <c>insertText:</c> / <c>deleteBackward</c> here.
/// </summary>
internal sealed class SoftKeyboardView : UIView, IUIKeyInput
{
	/// <summary>Gets a value indicating that this view can become first responder (required to type).</summary>
	public override bool CanBecomeFirstResponder => true;

	/// <summary>Gets a value indicating there is always "text", so UIKit keeps delivering deletes.</summary>
	[Export("hasText")]
	public bool HasText => true;

	/// <summary>Receives typed text from the keyboard and forwards each codepoint to ImGui.</summary>
	/// <param name="text">The inserted text (usually one character; more on paste / IME commit).</param>
	[Export("insertText:")]
	public void InsertText(string text)
	{
		foreach (Rune rune in (text ?? string.Empty).EnumerateRunes())
		{
			if (rune.Value is '\n' or '\r')
			{
				ImGuiApp.OnKey(ImGuiKey.Enter, down: true);
				ImGuiApp.OnKey(ImGuiKey.Enter, down: false);
			}
			else if (rune.Value >= 0x20 && rune.Value != 0x7f)
			{
				ImGuiApp.OnTextInput((uint)rune.Value);
			}
		}
	}

	/// <summary>Receives a backspace from the keyboard and forwards it to ImGui as a key press.</summary>
	[Export("deleteBackward")]
	public void DeleteBackward()
	{
		ImGuiApp.OnKey(ImGuiKey.Backspace, down: true);
		ImGuiApp.OnKey(ImGuiKey.Backspace, down: false);
	}
}

#endif
