// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Popups;

using System.Numerics;

using Hexa.NET.ImGui;
using ktsu.CaseConverter;

public partial class ImGuiPopups
{
	/// <summary>
	/// Base class for a popup input window.
	/// </summary>
	/// <typeparam name="TInput">The type of the input value.</typeparam>
	/// <typeparam name="TDerived">The type of the derived class for CRTP.</typeparam>
	public abstract class Input<TInput, TDerived> where TDerived : Input<TInput, TDerived>, new()
	{
		private TInput? cachedValue;
		private Action<TInput> OnConfirm { get; set; } = null!;
		private string Label { get; set; } = string.Empty;
		/// <summary>
		/// Gets the modal instance used to display the popup input window.
		/// </summary>
		protected Modal Modal { get; } = new();

		/// <summary>
		/// Open the popup and set the title, label, and default value.
		/// </summary>
		/// <param name="title">The title of the popup window.</param>
		/// <param name="label">The label of the input field.</param>
		/// <param name="defaultValue">The default value of the input field.</param>
		/// <param name="onConfirm">A callback to handle the new input value.</param>
		public void Open(string title, string label, TInput defaultValue, Action<TInput> onConfirm) => Open(title, label, defaultValue, onConfirm, customSize: Vector2.Zero);

		/// <summary>
		/// Open the popup and set the title, label, and default value.
		/// </summary>
		/// <param name="title">The title of the popup window.</param>
		/// <param name="label">The label of the input field.</param>
		/// <param name="defaultValue">The default value of the input field.</param>
		/// <param name="onConfirm">A callback to handle the new input value.</param>
		/// <param name="customSize">Custom size of the popup.</param>
		public void Open(string title, string label, TInput defaultValue, Action<TInput> onConfirm, Vector2 customSize)
		{
			Label = label;
			OnConfirm = onConfirm;
			cachedValue = defaultValue;
			Modal.Open(title, ShowContent, customSize);
		}

		/// <summary>
		/// Show the content of the popup.
		/// </summary>
		private void ShowContent()
		{
			if (cachedValue is not null)
			{
				ImGui.TextUnformatted(Label);
				ImGui.NewLine();

				if (!Modal.WasOpen && !ImGui.IsItemFocused())
				{
					ImGui.SetKeyboardFocusHere();
				}

				if (ShowEdit(ref cachedValue))
				{
					OnConfirm(cachedValue);
					ImGui.CloseCurrentPopup();
				}

				ImGui.SameLine();
				if (ImGui.Button($"OK###{Modal.Title.ToSnakeCase()}_OK"))
				{
					OnConfirm(cachedValue);
					ImGui.CloseCurrentPopup();
				}
			}
		}

		/// <summary>
		/// Show the input field for the derived class.
		/// </summary>
		/// <param name="value">The input value.</param>
		/// <returns>True if the input field is changed.</returns>
		protected abstract bool ShowEdit(ref TInput value);

		/// <summary>
		/// Show the modal if it is open.
		/// </summary>
		/// <returns>True if the modal is open.</returns>
		public bool ShowIfOpen() => Modal.ShowIfOpen();
	}

	/// <summary>
	/// A popup input window for strings.
	/// </summary>
	public class InputString : Input<string, InputString>
	{
		/// <summary>
		/// Show the input field for strings.
		/// </summary>
		/// <param name="value">The input value.</param>
		/// <returns>True if Enter is pressed.</returns>
		protected override bool ShowEdit(ref string value) => ImGui.InputText($"###{Modal.Title.ToSnakeCase()}_INPUT", ref value, 100, ImGuiInputTextFlags.EnterReturnsTrue);
	}

	/// <summary>
	/// A popup input window for integers.
	/// </summary>
	public class InputInt : Input<int, InputInt>
	{
		/// <summary>
		/// Show the input field for integers.
		/// </summary>
		/// <param name="value">The input value.</param>
		/// <returns>False</returns>
		protected override bool ShowEdit(ref int value)
		{
			ImGui.InputInt($"###{Modal.Title.ToSnakeCase()}_INPUT", ref value);
			return false;
		}
	}

	/// <summary>
	/// A popup input window for floats.
	/// </summary>
	public class InputFloat : Input<float, InputFloat>
	{
		/// <summary>
		/// Show the input field for floats.
		/// </summary>
		/// <param name="value">The input value.</param>
		/// <returns>False</returns>
		protected override bool ShowEdit(ref float value)
		{
			ImGui.InputFloat($"###{Modal.Title.ToSnakeCase()}_INPUT", ref value);
			return false;
		}
	}
}
