// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;
using System.Diagnostics.CodeAnalysis;

using Hexa.NET.ImGui;

public static partial class ImGuiWidgets
{
	/// <summary>The editors a cell gets when its column doesn't supply one.</summary>
	internal static class DataTableEditors
	{
		private const int TextCapacity = 1024;

		private static readonly ImGuiInputTextCallback PlaceCaretAtEnd = CreatePlaceCaretAtEndCallback();

		/// <summary>Draws a text box filling the cell.</summary>
		/// <param name="id">The ImGui id.</param>
		/// <param name="text">The text being edited.</param>
		/// <param name="isCaretForcedToEnd">
		/// Whether to hold the caret after the last character this frame, undoing ImGui's select-all on
		/// activation. Only for the first frames of a session started by typing.
		/// </param>
		/// <returns>True when the text changed this frame.</returns>
		internal static bool Text(string id, ref string text, bool isCaretForcedToEnd)
		{
			ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);

			return isCaretForcedToEnd
				? ImGui.InputText(id, ref text, TextCapacity, ImGuiInputTextFlags.CallbackAlways, PlaceCaretAtEnd)
				: ImGui.InputText(id, ref text, TextCapacity);
		}

		/// <summary>Draws a combo of an enum's names, filling the cell.</summary>
		/// <typeparam name="TValue">The enum type. Not constrained, because the column only knows it at run time.</typeparam>
		/// <param name="id">The ImGui id.</param>
		/// <param name="value">The value being edited.</param>
		/// <returns>True when a different value was chosen this frame.</returns>
		internal static bool EnumValue<TValue>(string id, ref TValue value)
		{
			Type type = typeof(TValue);
			string current = Enum.GetName(type, value!) ?? DataTableText.FormatInvariant(value);
			bool isChanged = false;

			ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
			if (ImGui.BeginCombo(id, current))
			{
				// Every name is drawn, because the list shows them all, so the loop can't be filtered. Only a
				// click on a name other than the current one is a change.
				foreach (string name in Enum.GetNames(type))
				{
					bool isClicked = ImGui.Selectable(name, name == current);
					if (isClicked && name != current)
					{
						value = (TValue)Enum.Parse(type, name);
						isChanged = true;
					}
				}

				ImGui.EndCombo();
			}

			return isChanged;
		}

		[SuppressMessage("Major Code Smell", "S6640:Make sure that using \"unsafe\" is safe here", Justification = "Converting the method group to the callback delegate needs an unsafe context because the delegate type takes a pointer, but no pointer is touched here.")]
		private static unsafe ImGuiInputTextCallback CreatePlaceCaretAtEndCallback() => PlaceCaretAtEndCallback;

		[SuppressMessage("Major Code Smell", "S6640:Make sure that using \"unsafe\" is safe here", Justification = "ImGui passes a pointer valid for the call, and it is not retained.")]
		private static unsafe int PlaceCaretAtEndCallback(ImGuiInputTextCallbackData* data)
		{
			data->CursorPos = data->BufTextLen;
			data->SelectionStart = data->BufTextLen;
			data->SelectionEnd = data->BufTextLen;
			return 0;
		}
	}
}
