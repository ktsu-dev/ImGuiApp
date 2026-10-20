// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Popups;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Probes;

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

	/// <summary>One button in a <see cref="Prompt"/>.</summary>
	/// <param name="Label">The button's text.</param>
	/// <param name="Action">What choosing it does, or null for nothing. Runs after the prompt has closed, so it may open another popup.</param>
	/// <param name="Probe">The probe name the button is marked with, or null for <c>prompt/</c> followed by its label.</param>
	/// <param name="IsCancel">
	/// Whether this is the answer the prompt gives when it closes without a button being clicked: by
	/// Escape, by its close button, or because another popup took its place. At most one button may be.
	/// </param>
	public readonly record struct PromptButton(string Label, Action? Action = null, string? Probe = null, bool IsCancel = false);

	/// <summary>A checkbox drawn above a <see cref="Prompt"/>'s buttons, unticked when it opens, such as "Don't ask again".</summary>
	/// <param name="Label">The checkbox's text.</param>
	/// <param name="Probe">The probe name it is marked with, or null for <c>prompt/checkbox</c>.</param>
	public sealed record PromptCheckbox(string Label, string? Probe = null);

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
		/// Gets or sets the buttons, in the order they are drawn.
		/// </summary>
		private IReadOnlyList<PromptButton> Buttons { get; set; } = [];

		/// <summary>Gets or sets the checkbox above the buttons, or null for none.</summary>
		private PromptCheckbox? Checkbox { get; set; }

		/// <summary>The checkbox's state while the prompt is open.</summary>
		private bool checkboxValue;

		/// <summary>The action of the button clicked this frame, run once the popup has ended.</summary>
		private Action? pendingAction;

		/// <summary>Whether a button was clicked this frame, whether or not it has an action.</summary>
		private bool answered;

		/// <summary>
		/// Gets a value indicating whether the prompt is waiting for an answer: from <c>Open</c> until a
		/// button is clicked or the prompt closes for any other reason.
		/// </summary>
		public bool IsAwaitingAnswer { get; private set; }

		/// <summary>Gets a value indicating whether the checkbox was ticked when the prompt was answered.</summary>
		/// <remarks>Read from a button's action, which runs after the prompt has closed. False when the prompt has no checkbox.</remarks>
		public bool Checked => Checkbox is not null && checkboxValue;

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

			Ensure.NotNull(buttons);
			Open(title, label, [.. buttons.Select(pair => new PromptButton(pair.Key, pair.Value))], checkbox: null, textLayoutType, size);
		}

		/// <summary>
		/// Opens the prompt with buttons that carry their own probe names, an optional checkbox, and an
		/// optional answer for when it closes without one being clicked.
		/// </summary>
		/// <param name="title">The title of the popup window.</param>
		/// <param name="label">The text of the prompt.</param>
		/// <param name="buttons">The buttons, in order. At most one may be <see cref="PromptButton.IsCancel"/>.</param>
		/// <param name="checkbox">A checkbox above the buttons, or null for none. Actions read it through <see cref="Checked"/>.</param>
		/// <param name="textLayoutType">The layout type for the prompt text.</param>
		/// <param name="size">Custom size of the popup, or zero to fit the content.</param>
		/// <exception cref="ArgumentException">More than one button is the cancel button.</exception>
		/// <remarks>
		/// A button's action runs after the popup has ended rather than inside it, so it can open the next
		/// prompt. ImGui closes a modal when another opens at the same level, as well as on Escape or its
		/// close button; whoever asked is still waiting for an answer then, so the cancel button's action
		/// runs, exactly once, as if it had been clicked.
		/// </remarks>
		public void Open(string title, string label, IReadOnlyList<PromptButton> buttons, PromptCheckbox? checkbox = null, PromptTextLayoutType textLayoutType = PromptTextLayoutType.Unformatted, Vector2 size = default)
		{
			// Wrapping text without a custom size will result in an incorrectly sized
			// popup as the text will wrap based on the popup and the popup will size
			// based on the text.
			Debug.Assert((textLayoutType == PromptTextLayoutType.Unformatted) || (size != Vector2.Zero));
			Ensure.NotNull(buttons);
			if (buttons.Count(button => button.IsCancel) > 1)
			{
				throw new ArgumentException("A prompt can have at most one cancel button, the answer it gives when dismissed.", nameof(buttons));
			}

			Label = label;
			Buttons = buttons;
			Checkbox = checkbox;
			checkboxValue = false;
			TextLayoutType = textLayoutType;
			pendingAction = null;
			answered = false;
			IsAwaitingAnswer = true;
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
					// TextWrapped would read the label as a printf format.
					ImGui.PushTextWrapPos(0f);
					ImGui.TextUnformatted(Label);
					ImGui.PopTextWrapPos();
					break;

				default:
					throw new NotImplementedException();
			}

			ImGui.NewLine();

			if (Checkbox is not null)
			{
				_ = ImGui.Checkbox(Checkbox.Label, ref checkboxValue);
				ImGuiProbes.MarkItem(Checkbox.Probe ?? "prompt/checkbox");
				ImGui.Spacing();
			}

			foreach (PromptButton button in Buttons)
			{
				bool clicked = ImGui.Button(button.Label);
				ImGuiProbes.MarkItem(button.Probe ?? $"prompt/{button.Label}");
				if (clicked && !answered)
				{
					answered = true;
					pendingAction = button.Action;
					ImGui.CloseCurrentPopup();
				}

				ImGui.SameLine();
			}
		}

		/// <summary>
		/// Displays the modal if it is open, and runs the answer once it has closed.
		/// </summary>
		/// <returns>True if the modal is open; otherwise, false.</returns>
		public bool ShowIfOpen()
		{
			bool open = Modal.ShowIfOpen();
			if (open || !IsAwaitingAnswer)
			{
				return open;
			}

			// Closed: by a button, whose action waited for the popup to end, or by anything else, which
			// is the cancel button's answer.
			IsAwaitingAnswer = false;
			Action? action = answered ? pendingAction : Buttons.FirstOrDefault(button => button.IsCancel).Action;
			pendingAction = null;
			answered = false;
			action?.Invoke();
			return false;
		}
	}
}
