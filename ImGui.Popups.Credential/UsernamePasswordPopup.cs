// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGuiCredentialPopups;

using ImGuiNET;
using ktsu.CredentialCache;
using ktsu.Extensions;

/// <summary>
/// Popup dialog for collecting username and password credentials.
/// </summary>
public class UsernamePasswordPopup : CredentialPopup
{
	private string username = string.Empty;
	private string password = string.Empty;

	/// <summary>
	/// Creates a credential object with username and password from the current input values.
	/// </summary>
	/// <returns>A CredentialWithUsernamePassword object containing the username and password.</returns>
	protected override Credential MakeCredential() =>
		new CredentialWithUsernamePassword()
		{
			Username = username.As<CredentialUsername>(),
			Password = password.As<CredentialPassword>(),
		};

	/// <summary>
	/// Displays username and password input fields.
	/// </summary>
	/// <returns>True if the user completed the input via shortcuts, otherwise false.</returns>
	protected override bool ShowEdit()
	{
		ImGui.InputText("Username", ref username, 100);
		ImGui.InputText("Password", ref password, 100, ImGuiInputTextFlags.Password);
		return false;
	}
}
