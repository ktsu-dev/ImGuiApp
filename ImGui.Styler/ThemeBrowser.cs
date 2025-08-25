// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGuiStyler;

using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Hexa.NET.ImGui;
using ktsu.ThemeProvider;
using ktsu.ImGuiPopups;

/// <summary>
/// A modal theme browser that allows users to select themes from a visual grid.
/// </summary>
public class ThemeBrowser
{
	/// <summary>
	/// The underlying modal instance for managing popup behavior using ktsu.ImGuiPopups.
	/// </summary>
	private readonly ImGuiPopups.Modal modal = new();

	/// <summary>
	/// The selected theme family filter index.
	/// </summary>
	private int selectedFamilyFilter;

	/// <summary>
	/// The action to invoke when a theme is selected.
	/// </summary>
	private Action<string>? onThemeSelected;

	/// <summary>
	/// The action to invoke when the default theme is requested.
	/// </summary>
	private Action? onDefaultRequested;

	/// <summary>
	/// Tracks whether a theme change occurred during the current modal session.
	/// </summary>
	private bool themeChanged;

	/// <summary>
	/// Opens the theme browser modal.
	/// </summary>
	/// <param name="title">The title of the modal window.</param>
	/// <param name="onThemeSelected">Action to invoke when a theme is selected. Parameter is the theme name.</param>
	/// <param name="onDefaultRequested">Action to invoke when default theme is requested.</param>
	/// <param name="customSize">Custom size of the modal. If not specified, uses a default size.</param>
	public void Open(string title = "🎨 Theme Browser", Action<string>? onThemeSelected = null, Action? onDefaultRequested = null, Vector2? customSize = null)
	{
		this.onThemeSelected = onThemeSelected;
		this.onDefaultRequested = onDefaultRequested;
		themeChanged = false; // Reset theme change tracking for this modal session
		Vector2 size = customSize ?? new Vector2(900, 650); // Increased width to accommodate wider theme cards
		modal.Open(title, ShowContent, size);
	}

	/// <summary>
	/// Shows the theme browser modal if it's open and returns whether a theme was changed.
	/// </summary>
	/// <returns>True if a theme was changed during modal interaction, false otherwise.</returns>
	public bool ShowIfOpen()
	{
		bool wasOpen = modal.WasOpen;
		bool isOpen = modal.ShowIfOpen();

		// If the modal was just closed and a theme was changed, return true and reset the flag
		if (wasOpen && !isOpen && themeChanged)
		{
			themeChanged = false;
			return true;
		}

		// If modal is still open but theme was changed this frame, return true (but don't reset flag yet)
		return themeChanged;
	}

	/// <summary>
	/// Shows the content of the theme browser modal.
	/// </summary>
	private void ShowContent()
	{
		// Header with current theme info
		ImGui.Text("Choose a theme from the gallery below:");
		if (Theme.CurrentThemeName is not null)
		{
			ImGui.SameLine();
			using (Text.Color.Success())
			{
				ImGui.Text($"Current: {Theme.CurrentThemeName}");
			}
		}
		else
		{
			ImGui.SameLine();
			using (Text.Color.Info())
			{
				ImGui.Text("Current: Default");
			}
		}

		ImGui.Separator();

		// Theme family filter
		ImGui.Text("Filter by Family:");
		ImGui.SameLine();

		// Create family list for filtering
		List<string> familyList = ["All Themes", .. Theme.Families.OrderBy(f => f)];

		if (ImGui.Combo("##FamilyFilter", ref selectedFamilyFilter, [.. familyList], familyList.Count))
		{
			// Family filter changed - could add logic here if needed
		}

		ImGui.Separator();

		// Get filtered themes
		IEnumerable<ThemeRegistry.ThemeInfo> filteredThemes = selectedFamilyFilter == 0
			? Theme.AllThemes
			: Theme.AllThemes.Where(t => t.Family == familyList[selectedFamilyFilter]);

		List<ThemeRegistry.ThemeInfo> themesToShow = [.. filteredThemes.OrderBy(t => t.Family).ThenBy(t => t.Name)];

		// Theme count info
		ImGui.Text($"Themes ({themesToShow.Count}):");

		// Theme grid in a scrollable area
		Vector2 childSize = new(0, ImGui.GetContentRegionAvail().Y - 60); // Leave space for buttons
		ImGui.BeginChild("ThemeGridScrollArea", childSize, ImGuiChildFlags.Borders);

		// Use ThemeCard.RenderGrid with appropriate sizing for the modal
		Vector2 cardSize = new(200, 90); // Increased width to accommodate longer theme names
		ThemeCard.RenderGrid(themesToShow, selectedTheme =>
		{
			// Apply the selected theme
			if (Theme.Apply(selectedTheme.Name))
			{
				themeChanged = true; // Set flag when theme is successfully applied
				onThemeSelected?.Invoke(selectedTheme.Name);
				//ImGui.CloseCurrentPopup();
			}
		}, cardSize);

		ImGui.EndChild();

		ImGui.NewLine();

		// Modal buttons
		float buttonWidth = 100.0f;
		float totalButtonsWidth = (buttonWidth * 2) + ImGui.GetStyle().ItemSpacing.X;
		ImGui.SetCursorPosX((ImGui.GetContentRegionAvail().X - totalButtonsWidth) * 0.5f);

		// Reset button
		using (Theme.FromColor(Color.Palette.Semantic.Warning))
		{
			if (ImGui.Button("Reset", new Vector2(buttonWidth, 0)))
			{
				Theme.ResetToDefault();
				themeChanged = true; // Set flag when default is applied
				onDefaultRequested?.Invoke();
				//ImGui.CloseCurrentPopup();
			}
		}

		ImGui.SameLine();

		// Close button
		if (ImGui.Button("Close", new Vector2(buttonWidth, 0)))
		{
			ImGui.CloseCurrentPopup();
		}
	}
}
