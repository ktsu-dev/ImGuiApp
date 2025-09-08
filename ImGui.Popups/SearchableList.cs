// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Popups;

using System;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.CaseConverter;
using ktsu.TextFilter;

public partial class ImGuiPopups
{
	/// <summary>
	/// A popup window to allow the user to search and select an item from a list
	/// </summary>
	/// <typeparam name="TItem">The type of the list elements</typeparam>
	public class SearchableList<TItem> where TItem : class
	{
		private TItem? cachedValue;
		private TItem? selectedItem;
		private string searchTerm = string.Empty;
		private Action<TItem> OnConfirm { get; set; } = null!;
		private Func<TItem, string>? GetText { get; set; }
		private string Label { get; set; } = string.Empty;
		private IEnumerable<TItem> Items { get; set; } = [];
		private Modal Modal { get; } = new();

		/// <summary>
		/// Open the popup and set the title, label, and default value.
		/// </summary>
		/// <param name="title">The title of the popup window.</param>
		/// <param name="label">The label of the input field.</param>
		/// /// <param name="items">The items to select from.</param>
		/// <param name="defaultItem">The default value of the input field.</param>
		/// <param name="getText">A delegate to get the text representation of an item.</param>
		/// <param name="onConfirm">A callback to handle the new input value.</param>
		/// <param name="customSize">Custom size of the popup.</param>
		public void Open(string title, string label, IEnumerable<TItem> items, TItem? defaultItem, Func<TItem, string>? getText, Action<TItem> onConfirm, Vector2 customSize)
		{
			searchTerm = string.Empty;
			Label = label;
			OnConfirm = onConfirm;
			GetText = getText;
			cachedValue = defaultItem;
			Items = items;
			Modal.Open(title, ShowContent, customSize);
		}

		/// <summary>
		/// Open the popup and set the title, label, and default value.
		/// </summary>
		/// <param name="title">The title of the popup window.</param>
		/// <param name="label">The label of the input field.</param>
		/// <param name="items">The items to select from.</param>
		/// <param name="defaultItem">The default value of the input field.</param>
		/// <param name="getText">A delegate to get the text representation of an item.</param>
		/// <param name="onConfirm">A callback to handle the new input value.</param>
		public void Open(string title, string label, IEnumerable<TItem> items, TItem? defaultItem, Func<TItem, string>? getText, Action<TItem> onConfirm) => Open(title, label, items, defaultItem, getText, onConfirm, Vector2.Zero);

		/// <summary>
		/// Open the popup and set the title, label, and default value.
		/// </summary>
		/// <param name="title">The title of the popup window.</param>
		/// <param name="label">The label of the input field.</param>
		/// <param name="items">The items to select from.</param>
		/// <param name="onConfirm">A callback to handle the new input value.</param>
		public void Open(string title, string label, IEnumerable<TItem> items, Action<TItem> onConfirm) => Open(title, label, items, null, null, onConfirm);

		/// <summary>
		/// Open the popup and set the title, label, and default value.
		/// </summary>
		/// <param name="title">The title of the popup window.</param>
		/// <param name="label">The label of the input field.</param>
		/// <param name="items">The items to select from.</param>
		/// <param name="getText">A delegate to get the text representation of an item.</param>
		/// <param name="onConfirm">A callback to handle the new input value.</param>
		public void Open(string title, string label, IEnumerable<TItem> items, Func<TItem, string> getText, Action<TItem> onConfirm) => Open(title, label, items, null, getText, onConfirm);

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

			if (ImGui.InputText("##Search", ref searchTerm, 255, ImGuiInputTextFlags.EnterReturnsTrue))
			{
				TItem? confirmedItem = cachedValue ?? selectedItem;
				if (confirmedItem is not null)
				{
					OnConfirm(confirmedItem);
					ImGui.CloseCurrentPopup();
				}
			}

			Dictionary<string, TItem> itemLookup = Items.Select(item => (item, itemString: item.ToString() ?? string.Empty))
				.Where(x => !string.IsNullOrEmpty(x.itemString))
				.DistinctBy(x => x.itemString)
				.ToDictionary(x => x.itemString, x => x.item);

			IEnumerable<string> sortedStrings = TextFilter.Rank(itemLookup.Keys, searchTerm);

			if (ImGui.BeginListBox("##List"))
			{
				selectedItem = null;
				foreach (string itemString in sortedStrings)
				{
					if (!itemLookup.TryGetValue(itemString, out TItem? item))
					{
						continue;
					}

					//if nothing has been explicitly selected, select the first item which will be the best match
					if (selectedItem is null && cachedValue is null)
					{
						selectedItem = item;
					}

					string displayText = GetText?.Invoke(item) ?? item.ToString() ?? string.Empty;

					if (ImGui.Selectable(displayText, item == (cachedValue ?? selectedItem)))
					{
						cachedValue = item;
					}
				}

				ImGui.EndListBox();
			}

			if (ImGui.Button($"OK###{Modal.Title.ToSnakeCase()}_OK"))
			{
				TItem? confirmedItem = cachedValue ?? selectedItem;
				if (confirmedItem is not null)
				{
					OnConfirm(confirmedItem);
					ImGui.CloseCurrentPopup();
				}
			}

			ImGui.SameLine();
			if (ImGui.Button($"Cancel###{Modal.Title.ToSnakeCase()}_Cancel"))
			{
				ImGui.CloseCurrentPopup();
			}
		}

		/// <summary>
		/// Show the modal if it is open.
		/// </summary>
		/// <returns>True if the modal is open.</returns>
		public bool ShowIfOpen() => Modal.ShowIfOpen();
	}
}
