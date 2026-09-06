// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Popups;

using System;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Probes;

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
		private bool itemChosen;
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
			itemChosen = false;
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

			bool searchChanged = ImGui.InputText("##Search", ref searchTerm, 255, ImGuiInputTextFlags.EnterReturnsTrue);
			ImGuiProbes.MarkItem("searchable-list/search");
			if (searchChanged)
			{
				ConfirmSelectedItem();
			}

			// Keyed on the text the item is drawn with, not on ToString(): items that share a
			// ToString() but display differently (Array(Int) and Array(String), say) stay distinct
			// entries, and the search ranks what is on screen rather than something the user
			// cannot see.
			Dictionary<string, TItem> itemLookup = Items.Select(item => (item, itemString: GetText?.Invoke(item) ?? item.ToString() ?? string.Empty))
				.Where(x => !string.IsNullOrEmpty(x.itemString))
				.DistinctBy(x => x.itemString)
				.ToDictionary(x => x.itemString, x => x.item);

			IEnumerable<string> sortedStrings = TextFilter.Rank(itemLookup.Keys, searchTerm);

			if (ImGui.BeginListBox("##List"))
			{
				DrawItemList(sortedStrings, itemLookup);
				ImGui.EndListBox();
			}

			// Picking an item is the choice, so it confirms rather than waiting for OK. Confirmed
			// after the list box has ended, because closing the popup from inside a child window
			// leaves the child's begin/end unbalanced.
			if (itemChosen)
			{
				itemChosen = false;
				ConfirmSelectedItem();
			}

			bool okClicked = ImGui.Button($"OK###{Modal.Title.ToSnakeCase()}_OK");
			ImGuiProbes.MarkItem("searchable-list/ok");
			if (okClicked)
			{
				ConfirmSelectedItem();
			}

			ImGui.SameLine();
			bool cancelClicked = ImGui.Button($"Cancel###{Modal.Title.ToSnakeCase()}_Cancel");
			ImGuiProbes.MarkItem("searchable-list/cancel");
			if (cancelClicked)
			{
				ImGui.CloseCurrentPopup();
			}
		}

		/// <summary>
		/// Confirms the current selection, invoking the callback and closing the popup if an item is selected.
		/// </summary>
		private void ConfirmSelectedItem()
		{
			TItem? confirmedItem = cachedValue ?? selectedItem;
			if (confirmedItem is not null)
			{
				OnConfirm(confirmedItem);
				ImGui.CloseCurrentPopup();
			}
		}

		/// <summary>
		/// Draws the ranked list of items, tracking the current selection.
		/// </summary>
		/// <param name="sortedStrings">The ranked item strings to display.</param>
		/// <param name="itemLookup">Lookup from item string to item.</param>
		private void DrawItemList(IEnumerable<string> sortedStrings, Dictionary<string, TItem> itemLookup)
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

				// Compared with the default comparer rather than ==, which on an unconstrained type
				// parameter is reference equality: a caller whose list is rebuilt each frame passes
				// an equal but different instance, and the selection never draws as selected.
				bool isSelected = EqualityComparer<TItem>.Default.Equals(item, cachedValue ?? selectedItem);
				bool itemClicked = ImGui.Selectable(itemString, isSelected);
				ImGuiProbes.MarkItem("searchable-list", itemString);
				if (itemClicked)
				{
					cachedValue = item;
					itemChosen = true;
				}
			}
		}

		/// <summary>
		/// Show the modal if it is open.
		/// </summary>
		/// <returns>True if the modal is open.</returns>
		public bool ShowIfOpen() => Modal.ShowIfOpen();
	}
}
