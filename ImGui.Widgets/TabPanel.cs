// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGuiWidgets;

using System;
using System.Collections.ObjectModel;
using Hexa.NET.ImGui;

/// <summary>
/// Provides custom ImGui widgets.
/// </summary>
public static partial class ImGuiWidgets
{
	/// <summary>
	/// A panel with tabs that can be used to organize content.
	/// </summary>
	public class TabPanel
	{
		/// <summary>
		/// The unique identifier for this tab panel.
		/// </summary>
		public string Id { get; private set; }

		/// <summary>
		/// The collection of tabs within this panel.
		/// </summary>
		public Collection<Tab> Tabs { get; private set; } = [];

		/// <summary>
		/// The index of the currently active tab.
		/// </summary>
		public int ActiveTabIndex { get; set; }

		/// <summary>
		/// The ID of the currently active tab.
		/// </summary>
		public string? ActiveTabId => ActiveTabIndex >= 0 && ActiveTabIndex < Tabs.Count
			? Tabs[ActiveTabIndex].Id
			: null;

		/// <summary>
		/// Whether the tab bar is closable (allows closing tabs with an X button).
		/// </summary>
		public bool Closable { get; set; }

		/// <summary>
		/// Whether the tab bar should be reorderable.
		/// </summary>
		public bool Reorderable { get; set; }

		/// <summary>
		/// Gets the currently active tab or null if there are no tabs.
		/// </summary>
		public Tab? ActiveTab => Tabs.Count > 0 && ActiveTabIndex >= 0 && ActiveTabIndex < Tabs.Count
			? Tabs[ActiveTabIndex]
			: null;

		private Action<int>? TabChangedDelegate { get; }
		private Action<string>? TabChangedByIdDelegate { get; }

		/// <summary>
		/// Create a new tab panel.
		/// </summary>
		/// <param name="id">The unique identifier for this tab panel.</param>
		public TabPanel(string id) => Id = id;

		/// <summary>
		/// Create a new tab panel with options.
		/// </summary>
		/// <param name="id">The unique identifier for this tab panel.</param>
		/// <param name="closable">Whether tabs can be closed.</param>
		/// <param name="reorderable">Whether tabs can be reordered.</param>
		public TabPanel(string id, bool closable, bool reorderable)
		{
			Id = id;
			Closable = closable;
			Reorderable = reorderable;
		}

		/// <summary>
		/// Create a new tab panel with a callback for tab changes.
		/// </summary>
		/// <param name="id">The unique identifier for this tab panel.</param>
		/// <param name="tabChangedDelegate">The delegate to call when the active tab changes, passing the tab index.</param>
		public TabPanel(string id, Action<int> tabChangedDelegate)
		{
			Id = id;
			TabChangedDelegate = tabChangedDelegate;
		}

		/// <summary>
		/// Create a new tab panel with a callback for tab changes.
		/// </summary>
		/// <param name="id">The unique identifier for this tab panel.</param>
		/// <param name="tabChangedDelegate">The delegate to call when the active tab changes, passing the tab ID.</param>
		public TabPanel(string id, Action<string> tabChangedDelegate)
		{
			Id = id;
			TabChangedByIdDelegate = tabChangedDelegate;
		}

		/// <summary>
		/// Create a new tab panel with options and a callback for tab changes.
		/// </summary>
		/// <param name="id">The unique identifier for this tab panel.</param>
		/// <param name="closable">Whether tabs can be closed.</param>
		/// <param name="reorderable">Whether tabs can be reordered.</param>
		/// <param name="tabChangedDelegate">The delegate to call when the active tab changes, passing the tab index.</param>
		public TabPanel(string id, bool closable, bool reorderable, Action<int> tabChangedDelegate)
		{
			Id = id;
			Closable = closable;
			Reorderable = reorderable;
			TabChangedDelegate = tabChangedDelegate;
		}

		/// <summary>
		/// Create a new tab panel with options and a callback for tab changes.
		/// </summary>
		/// <param name="id">The unique identifier for this tab panel.</param>
		/// <param name="closable">Whether tabs can be closed.</param>
		/// <param name="reorderable">Whether tabs can be reordered.</param>
		/// <param name="tabChangedDelegate">The delegate to call when the active tab changes, passing the tab ID.</param>
		public TabPanel(string id, bool closable, bool reorderable, Action<string> tabChangedDelegate)
		{
			Id = id;
			Closable = closable;
			Reorderable = reorderable;
			TabChangedByIdDelegate = tabChangedDelegate;
		}

		/// <summary>
		/// Find a tab by its ID.
		/// </summary>
		/// <param name="tabId">The ID of the tab to find.</param>
		/// <returns>The tab if found, null otherwise.</returns>
		public Tab? GetTabById(string tabId)
		{
			foreach (Tab tab in Tabs)
			{
				if (tab.Id == tabId)
				{
					return tab;
				}
			}

			return null;
		}

		/// <summary>
		/// Get the index of a tab by its ID.
		/// </summary>
		/// <param name="tabId">The ID of the tab to find.</param>
		/// <returns>The index of the tab if found, -1 otherwise.</returns>
		public int GetTabIndex(string tabId)
		{
			for (int i = 0; i < Tabs.Count; i++)
			{
				if (Tabs[i].Id == tabId)
				{
					return i;
				}
			}

			return -1;
		}

		/// <summary>
		/// Add a new tab to the panel.
		/// </summary>
		/// <param name="label">The label of the tab.</param>
		/// <param name="content">The content drawing delegate for the tab.</param>
		/// <returns>The ID of the newly added tab.</returns>
		public string AddTab(string label, Action content)
		{
			Tab tab = new(label, content);
			Tabs.Add(tab);
			return tab.Id;
		}

		/// <summary>
		/// Add a new tab to the panel with a specific ID.
		/// </summary>
		/// <param name="id">The unique ID for the tab.</param>
		/// <param name="label">The label of the tab.</param>
		/// <param name="content">The content drawing delegate for the tab.</param>
		/// <returns>The ID of the newly added tab.</returns>
		public string AddTab(string id, string label, Action content)
		{
			Tab tab = new(id, label, content);
			Tabs.Add(tab);
			return tab.Id;
		}

		/// <summary>
		/// Add a new tab to the panel with specified dirty state.
		/// </summary>
		/// <param name="label">The label of the tab.</param>
		/// <param name="content">The content drawing delegate for the tab.</param>
		/// <param name="isDirty">Initial dirty state of the tab.</param>
		/// <returns>The ID of the newly added tab.</returns>
		public string AddTab(string label, Action content, bool isDirty)
		{
			Tab tab = new(label, content, isDirty);
			Tabs.Add(tab);
			return tab.Id;
		}

		/// <summary>
		/// Add a new tab to the panel with a specific ID and dirty state.
		/// </summary>
		/// <param name="id">The unique ID for the tab.</param>
		/// <param name="label">The label of the tab.</param>
		/// <param name="content">The content drawing delegate for the tab.</param>
		/// <param name="isDirty">Initial dirty state of the tab.</param>
		/// <returns>The ID of the newly added tab.</returns>
		public string AddTab(string id, string label, Action content, bool isDirty)
		{
			Tab tab = new(id, label, content, isDirty);
			Tabs.Add(tab);
			return tab.Id;
		}

		/// <summary>
		/// Mark a tab as dirty (having unsaved changes).
		/// </summary>
		/// <param name="index">The index of the tab to mark dirty.</param>
		/// <returns>True if successful, false if the index is out of range.</returns>
		public bool MarkTabDirty(int index)
		{
			if (index >= 0 && index < Tabs.Count)
			{
				Tabs[index].MarkDirty();
				return true;
			}

			return false;
		}

		/// <summary>
		/// Mark a tab as dirty (having unsaved changes).
		/// </summary>
		/// <param name="tabId">The ID of the tab to mark dirty.</param>
		/// <returns>True if successful, false if the tab was not found.</returns>
		public bool MarkTabDirty(string tabId)
		{
			Tab? tab = GetTabById(tabId);
			if (tab != null)
			{
				tab.MarkDirty();
				return true;
			}

			return false;
		}

		/// <summary>
		/// Mark a tab as clean (having no unsaved changes).
		/// </summary>
		/// <param name="index">The index of the tab to mark clean.</param>
		/// <returns>True if successful, false if the index is out of range.</returns>
		public bool MarkTabClean(int index)
		{
			if (index >= 0 && index < Tabs.Count)
			{
				Tabs[index].MarkClean();
				return true;
			}

			return false;
		}

		/// <summary>
		/// Mark a tab as clean (having no unsaved changes).
		/// </summary>
		/// <param name="tabId">The ID of the tab to mark clean.</param>
		/// <returns>True if successful, false if the tab was not found.</returns>
		public bool MarkTabClean(string tabId)
		{
			Tab? tab = GetTabById(tabId);
			if (tab != null)
			{
				tab.MarkClean();
				return true;
			}

			return false;
		}

		/// <summary>
		/// Mark the currently active tab as dirty.
		/// </summary>
		/// <returns>True if there is an active tab and it was marked dirty, false otherwise.</returns>
		public bool MarkActiveTabDirty()
		{
			if (ActiveTab != null)
			{
				ActiveTab.MarkDirty();
				return true;
			}

			return false;
		}

		/// <summary>
		/// Mark the currently active tab as clean.
		/// </summary>
		/// <returns>True if there is an active tab and it was marked clean, false otherwise.</returns>
		public bool MarkActiveTabClean()
		{
			if (ActiveTab != null)
			{
				ActiveTab.MarkClean();
				return true;
			}

			return false;
		}

		/// <summary>
		/// Check if the tab at the specified index is dirty.
		/// </summary>
		/// <param name="index">The index of the tab to check.</param>
		/// <returns>True if the tab is dirty, false otherwise or if the index is out of range.</returns>
		public bool IsTabDirty(int index) => index >= 0 && index < Tabs.Count && Tabs[index].IsDirty;

		/// <summary>
		/// Check if the tab with the specified ID is dirty.
		/// </summary>
		/// <param name="tabId">The ID of the tab to check.</param>
		/// <returns>True if the tab is dirty, false otherwise or if the tab was not found.</returns>
		public bool IsTabDirty(string tabId)
		{
			Tab? tab = GetTabById(tabId);
			return tab?.IsDirty ?? false;
		}

		/// <summary>
		/// Check if the active tab is dirty.
		/// </summary>
		/// <returns>True if the active tab is dirty, false otherwise or if there is no active tab.</returns>
		public bool IsActiveTabDirty() => ActiveTab?.IsDirty ?? false;

		/// <summary>
		/// Remove a tab from the panel by index.
		/// </summary>
		/// <param name="index">The index of the tab to remove.</param>
		/// <returns>True if the tab was removed, false otherwise.</returns>
		public bool RemoveTab(int index)
		{
			if (index >= 0 && index < Tabs.Count)
			{
				Tabs.RemoveAt(index);
				if (ActiveTabIndex >= Tabs.Count)
				{
					ActiveTabIndex = Math.Max(0, Tabs.Count - 1);
					TabChangedDelegate?.Invoke(ActiveTabIndex);
					TabChangedByIdDelegate?.Invoke(ActiveTabId ?? string.Empty);
				}

				return true;
			}

			return false;
		}

		/// <summary>
		/// Remove a tab from the panel by ID.
		/// </summary>
		/// <param name="tabId">The ID of the tab to remove.</param>
		/// <returns>True if the tab was removed, false otherwise.</returns>
		public bool RemoveTab(string tabId)
		{
			int index = GetTabIndex(tabId);
			return index >= 0 && RemoveTab(index);
		}

		/// <summary>
		/// Draw the tab panel.
		/// </summary>
		public void Draw()
		{
			if (Tabs.Count == 0)
			{
				return;
			}

			ImGuiTabBarFlags flags = ImGuiTabBarFlags.None;
			if (Reorderable)
			{
				flags |= ImGuiTabBarFlags.Reorderable;
			}

			if (ImGui.BeginTabBar(Id, flags))
			{
				for (int i = 0; i < Tabs.Count; i++)
				{
					Tab tab = Tabs[i];
					ImGuiTabItemFlags tabFlags = ImGuiTabItemFlags.None;

					// Use the UnsavedDocument flag for dirty indicator
					if (tab.IsDirty)
					{
						tabFlags |= ImGuiTabItemFlags.UnsavedDocument;
					}

					bool tabOpen = true;

					if (ImGui.BeginTabItem($"{tab.Label}##{tab.Id}", ref tabOpen, tabFlags))
					{
						if (ActiveTabIndex != i)
						{
							ActiveTabIndex = i;
							TabChangedDelegate?.Invoke(i);
							TabChangedByIdDelegate?.Invoke(tab.Id);
						}

						tab.Content?.Invoke();
						ImGui.EndTabItem();
					}

					if (Closable && !tabOpen)
					{
						RemoveTab(i);
						i--; // Adjust index since we removed an item
					}
				}

				ImGui.EndTabBar();
			}
		}
	}

	/// <summary>
	/// Represents a single tab within a TabPanel.
	/// </summary>
	public class Tab
	{
		private static int nextId = 1;

		/// <summary>
		/// Gets a unique identifier for this tab.
		/// </summary>
		public string Id { get; }

		/// <summary>
		/// The label displayed on the tab.
		/// </summary>
		public string Label { get; set; }

		/// <summary>
		/// The action to invoke to draw the tab content.
		/// </summary>
		public Action? Content { get; set; }

		/// <summary>
		/// Gets or sets whether this tab's content has unsaved changes.
		/// </summary>
		public bool IsDirty { get; set; }

		/// <summary>
		/// Create a new tab with an auto-generated ID.
		/// </summary>
		/// <param name="label">The label of the tab.</param>
		public Tab(string label)
		{
			Id = $"tab_{nextId++}";
			Label = label;
		}

		/// <summary>
		/// Create a new tab with a specific ID.
		/// </summary>
		/// <param name="id">The unique ID for the tab.</param>
		/// <param name="label">The label of the tab.</param>
		public Tab(string id, string label)
		{
			Id = id;
			Label = label;
		}

		/// <summary>
		/// Create a new tab with content and an auto-generated ID.
		/// </summary>
		/// <param name="label">The label of the tab.</param>
		/// <param name="content">The content drawing delegate for the tab.</param>
		public Tab(string label, Action content)
		{
			Id = $"tab_{nextId++}";
			Label = label;
			Content = content;
		}

		/// <summary>
		/// Create a new tab with a specific ID and content.
		/// </summary>
		/// <param name="id">The unique ID for the tab.</param>
		/// <param name="label">The label of the tab.</param>
		/// <param name="content">The content drawing delegate for the tab.</param>
		public Tab(string id, string label, Action content)
		{
			Id = id;
			Label = label;
			Content = content;
		}

		/// <summary>
		/// Create a new tab with content, dirty state, and an auto-generated ID.
		/// </summary>
		/// <param name="label">The label of the tab.</param>
		/// <param name="content">The content drawing delegate for the tab.</param>
		/// <param name="isDirty">Initial dirty state of the tab.</param>
		public Tab(string label, Action content, bool isDirty)
		{
			Id = $"tab_{nextId++}";
			Label = label;
			Content = content;
			IsDirty = isDirty;
		}

		/// <summary>
		/// Create a new tab with a specific ID, content, and dirty state.
		/// </summary>
		/// <param name="id">The unique ID for the tab.</param>
		/// <param name="label">The label of the tab.</param>
		/// <param name="content">The content drawing delegate for the tab.</param>
		/// <param name="isDirty">Initial dirty state of the tab.</param>
		public Tab(string id, string label, Action content, bool isDirty)
		{
			Id = id;
			Label = label;
			Content = content;
			IsDirty = isDirty;
		}

		/// <summary>
		/// Marks the tab as dirty (having unsaved changes).
		/// </summary>
		public void MarkDirty() => IsDirty = true;

		/// <summary>
		/// Marks the tab as clean (no unsaved changes).
		/// </summary>
		public void MarkClean() => IsDirty = false;
	}
}
