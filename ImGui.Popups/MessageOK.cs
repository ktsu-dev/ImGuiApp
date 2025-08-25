// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGuiPopups;

using System.Numerics;

public partial class ImGuiPopups
{
	/// <summary>
	/// A class for displaying a prompt popup window.
	/// </summary>
	public class MessageOK : Prompt
	{
		/// <summary>
		/// Open the popup and set the title and message.
		/// </summary>
		/// <param name="title">The title of the popup window.</param>
		/// <param name="message">The message to show.</param>
		public void Open(string title, string message) => Open(title, message, customSize: Vector2.Zero);
		/// <summary>
		/// Open the popup and set the title and message.
		/// </summary>
		/// <param name="title">The title of the popup window.</param>
		/// <param name="message">The message to show.</param>
		/// <param name="customSize">Custom size of the popup.</param>
		public void Open(string title, string message, Vector2 customSize) => Open(title, message, PromptTextLayoutType.Unformatted, customSize);
		/// <summary>
		/// Open the popup and set the title and message.
		/// </summary>
		/// <param name="title">The title of the popup window.</param>
		/// <param name="message">The message to show.</param>
		/// <param name="textLayoutType">Which text layout method should be used.</param>
		/// <param name="size">Custom size of the popup.</param>
		public void Open(string title, string message, PromptTextLayoutType textLayoutType, Vector2 size) => Open(title, message, new() { { "OK", null } }, textLayoutType, size);
	}
}
