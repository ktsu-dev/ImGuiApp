// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGuiCredentialPopups;

using ImGuiNET;
using ktsu.CredentialCache;
using ktsu.Extensions;

/// <summary>
/// Popup dialog for collecting token-based credentials.
/// </summary>
public class TokenPopup : CredentialPopup
{
	private string token = string.Empty;

	/// <summary>
	/// Creates a credential object with token from the current input value.
	/// </summary>
	/// <returns>A CredentialWithToken object containing the token.</returns>
	protected override Credential MakeCredential() =>
		new CredentialWithToken()
		{
			Token = token.As<CredentialToken>(),
		};

	/// <summary>
	/// Displays a token input field.
	/// </summary>
	/// <returns>True if the user completed the input via shortcuts, otherwise false.</returns>
	protected override bool ShowEdit()
	{
		ImGui.InputText("Token", ref token, 100, ImGuiInputTextFlags.Password);
		return false;
	}
}
