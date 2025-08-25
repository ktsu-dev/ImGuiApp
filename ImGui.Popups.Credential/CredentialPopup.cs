// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGuiCredentialPopups;

using ImGuiNET;

using ImGuiPopups;

using ktsu.CaseConverter;
using ktsu.CredentialCache;

/// <summary>
/// Abstract base class for credential input popups with ImGui.
/// Provides common functionality for displaying modal dialogs to collect credentials.
/// </summary>
public abstract class CredentialPopup
{
	private Action<Credential> OnConfirm { get; set; } = null!;
	/// <summary>
	/// Gets or sets the title of the popup window.
	/// </summary>
	protected string Title { get; set; } = string.Empty;
	/// <summary>
	/// Gets or sets the label for the input field.
	/// </summary>
	protected string Label { get; set; } = string.Empty;
	/// <summary>
	/// Gets the modal popup instance used to display the credential dialog.
	/// </summary>
	protected ImGuiPopups.Modal Modal { get; } = new();

	/// <summary>
	/// Open the popup and set the title, label, and button definitions.
	/// </summary>
	/// <param name="title">The title of the popup window.</param>
	/// <param name="label">The label of the input field.</param>
	/// <param name="onConfirm">The delegate to invoke when the popup has been confirmed</param>
	public virtual void Open(string title, string label, Action<Credential> onConfirm)
	{
		OnConfirm = onConfirm;
		Title = title;
		Label = label;
		Modal.Open(title, ShowContent);
	}

	/// <summary>
	/// Show the content of the popup.
	/// </summary>
	private void ShowContent()
	{
		ImGui.TextUnformatted(Label);
		ImGui.NewLine();

		if (!Modal.WasOpen && !ImGui.IsItemFocused())
		{
			ImGui.SetKeyboardFocusHere();
		}

		if (ShowEdit())
		{
			OnConfirm(MakeCredential());
			ImGui.CloseCurrentPopup();
		}

		ImGui.SameLine();
		if (ImGui.Button($"OK###{Title.ToSnakeCase()}_OK"))
		{
			OnConfirm(MakeCredential());
			ImGui.CloseCurrentPopup();
		}
	}

	/// <summary>
	/// Shows the credential input fields.
	/// </summary>
	/// <returns>True if the edit process was completed and the popup should close, otherwise false.</returns>
	protected abstract bool ShowEdit();

	/// <summary>
	/// Creates a credential object from the current input values.
	/// </summary>
	/// <returns>A credential object containing the user input.</returns>
	protected abstract Credential MakeCredential();

	/// <summary>
	/// Show the modal if it is open.
	/// </summary>
	/// <returns>True if the modal is open.</returns>
	public bool ShowIfOpen() => Modal.ShowIfOpen();
}
