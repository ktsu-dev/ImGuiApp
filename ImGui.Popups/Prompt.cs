// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGuiPopups;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;

using Hexa.NET.ImGui;

/// <summary>
/// Contains classes for displaying various popup windows using ImGui.
/// </summary>
public static partial class ImGuiPopups
{
	/// <summary>
	/// Defines the layout type for prompt text.
	/// </summary>
	public enum PromptTextLayoutType
	{
		/// <summary>
		/// The text is displayed without any formatting.
		/// </summary>
		Unformatted,

		/// <summary>
		/// The text is wrapped based on the popup's size.
		/// </summary>
		Wrapped
	}

	/// <summary>
	/// A class for displaying a prompt popup window.
	/// </summary>
	public class Prompt
	{
		/// <summary>
		/// Gets the underlying modal instance.
		/// </summary>
		private Modal Modal { get; } = new();

		/// <summary>
		/// Gets or sets the label of the prompt.
		/// </summary>
		private string Label { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the dictionary of button labels and their corresponding actions.
		/// </summary>
		private Dictionary<string, Action?> Buttons { get; set; } = [];

		/// <summary>
		/// Gets or sets the text layout type for the prompt.
		/// </summary>
		private PromptTextLayoutType TextLayoutType { get; set; }

		/// <summary>
		/// Opens the prompt popup with the specified title, label, and buttons.
		/// </summary>
		/// <param name="title">The title of the popup window.</param>
		/// <param name="label">The label of the input field.</param>
		/// <param name="buttons">The names and actions of the buttons.</param>
		public virtual void Open(string title, string label, Dictionary<string, Action?> buttons)
			=> Open(title, label, buttons, customSize: Vector2.Zero);

		/// <summary>
		/// Opens the prompt popup with the specified title, label, buttons, and custom size.
		/// </summary>
		/// <param name="title">The title of the popup window.</param>
		/// <param name="label">The label of the input field.</param>
		/// <param name="buttons">The names and actions of the buttons.</param>
		/// <param name="customSize">Custom size of the popup.</param>
		public virtual void Open(string title, string label, Dictionary<string, Action?> buttons, Vector2 customSize)
			=> Open(title, label, buttons, textLayoutType: PromptTextLayoutType.Unformatted, customSize);

		/// <summary>
		/// Opens the prompt popup with the specified parameters.
		/// </summary>
		/// <param name="title">The title of the popup window.</param>
		/// <param name="label">The label of the input field.</param>
		/// <param name="buttons">The names and actions of the buttons.</param>
		/// <param name="textLayoutType">The layout type for the prompt text.</param>
		/// <param name="size">Custom size of the popup.</param>
		public void Open(string title, string label, Dictionary<string, Action?> buttons, PromptTextLayoutType textLayoutType, Vector2 size)
		{
			// Wrapping text without a custom size will result in an incorrectly sized
			// popup as the text will wrap based on the popup and the popup will size
			// based on the text.
			Debug.Assert((textLayoutType == PromptTextLayoutType.Unformatted) || (size != Vector2.Zero));

			Label = label;
			Buttons = buttons;
			TextLayoutType = textLayoutType;
			Modal.Open(title, ShowContent, size);
		}

		/// <summary>
		/// Displays the content of the prompt popup based on the text layout type.
		/// </summary>
		private void ShowContent()
		{
			switch (TextLayoutType)
			{
				case PromptTextLayoutType.Unformatted:
					ImGui.TextUnformatted(Label);
					break;

				case PromptTextLayoutType.Wrapped:
					ImGui.TextWrapped(Label);
					break;

				default:
					throw new NotImplementedException();
			}

			ImGui.NewLine();

			foreach ((string text, Action? action) in Buttons)
			{
				if (ImGui.Button(text))
				{
					action?.Invoke();
					ImGui.CloseCurrentPopup();
				}

				ImGui.SameLine();
			}
		}

		/// <summary>
		/// Displays the modal if it is open.
		/// </summary>
		/// <returns>True if the modal is open; otherwise, false.</returns>
		public bool ShowIfOpen() => Modal.ShowIfOpen();
	}
}
