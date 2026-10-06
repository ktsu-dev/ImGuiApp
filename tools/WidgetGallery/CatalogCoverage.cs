// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.WidgetGallery;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using ktsu.ImGui.Widgets;

/// <summary>
/// Compares the gallery with the widget surface, so a widget added to <c>ImGuiWidgets</c> without a
/// tile is reported instead of quietly missing from the README.
/// </summary>
internal static class CatalogCoverage
{
	/// <summary>
	/// Public members of <c>ImGuiWidgets</c> that are not widgets, with the reason each has no tile.
	/// </summary>
	private static readonly Dictionary<string, string> NotWidgets = new(StringComparer.Ordinal)
	{
		[nameof(ImGuiWidgets.DrawDeferred)] = "the per-frame pump the dialog tiles call",
		[nameof(ImGuiWidgets.DrawDeferredDocked)] = "the per-frame pump the docked window tile calls",
		[nameof(ImGuiWidgets.GestureDetector)] = "an invisible input region with nothing to see",
		[nameof(ImGuiWidgets.ResetGestureDetector)] = "state management for the gesture detector",
		[nameof(ImGuiWidgets.SelectAllHunks)] = "selection helper for the diff view",
		[nameof(ImGuiWidgets.SelectedHunkIndices)] = "selection helper for the diff view",
		[nameof(ImGuiWidgets.ClearHunkSelection)] = "selection helper for the diff view",
		[nameof(ImGuiWidgets.ScopedId)] = "a scope that pushes an ImGui id, not a widget",
		[nameof(ImGuiWidgets.DividerZone)] = "a zone of the divider container tile",
		[nameof(ImGuiWidgets.Tab)] = "a tab of the tab panel tile",
		[nameof(ImGuiWidgets.SequenceSource)] = "the data source of the sequencer tile",
		[nameof(ImGuiWidgets.CurveSource)] = "the data source of the multi-curve editor tile",
		[nameof(ImGuiWidgets.ImageCanvasState)] = "the state of the image canvas tile",
		[nameof(ImGuiWidgets.AssetBrowserState)] = "the selection behind the asset browser tile",
		[nameof(ImGuiWidgets.TryAcceptAssetPayload)] = "the drop-target half of the asset browser tile's drag",
		[nameof(ImGuiWidgets.Viewport3DState)] = "the state behind a 3D viewport that has no widget yet",
		[nameof(ImGuiWidgets.CalcIconSize)] = "layout arithmetic for the icon tile",
		[nameof(ImGuiWidgets.ClampPage)] = "arithmetic for the page indicator",
		[nameof(ImGuiWidgets.FormatBadgeCount)] = "formatting for the badge",
		[nameof(ImGuiWidgets.Initials)] = "formatting for the avatar",
		[nameof(ImGuiWidgets.NormalizePin)] = "input handling for the PIN input",
		[nameof(ImGuiWidgets.SetPinSlot)] = "input handling for the PIN input",
		[nameof(ImGuiWidgets.RatingValueFromOffset)] = "hit testing for the rating",
		[nameof(ImGuiWidgets.StarFillFraction)] = "arithmetic for the rating",
		[nameof(ImGuiWidgets.PianoKeyboardLayout)] = "the key geometry of the piano keyboard tile",
		[nameof(ImGuiWidgets.DataTableCommands)] = "keymap command registration for the data table tile",
	};

	/// <summary>Finds the widgets no tile covers.</summary>
	/// <param name="covered">The member names the catalogue's tiles cover.</param>
	/// <returns>The uncovered member names, sorted.</returns>
	public static IReadOnlyList<string> Uncovered(ISet<string> covered) =>
		[.. WidgetMembers().Where(name => !covered.Contains(name) && !NotWidgets.ContainsKey(name)).Order(StringComparer.Ordinal)];

	/// <summary>
	/// Lists the widget surface: the public static methods of <c>ImGuiWidgets</c> and the public
	/// classes nested in it, less options, results and other plain data types.
	/// </summary>
	private static IEnumerable<string> WidgetMembers()
	{
		Type widgets = typeof(ImGuiWidgets);

		IEnumerable<string> methods = widgets
			.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
			.Where(method => !method.IsSpecialName)
			.Select(method => method.Name);

		IEnumerable<string> classes = widgets
			.GetNestedTypes(BindingFlags.Public)
			.Where(type => type.IsClass && !IsPlainData(type))
			.Select(type => type.Name);

		return methods.Concat(classes).Distinct(StringComparer.Ordinal);
	}

	/// <summary>
	/// Options, records and delegates are how widgets are configured, not widgets themselves.
	/// </summary>
	private static bool IsPlainData(Type type) =>
		typeof(Delegate).IsAssignableFrom(type)
		|| type.GetMethod("<Clone>$") is not null
		|| type.Name.EndsWith("Options", StringComparison.Ordinal)
		|| type.Name.EndsWith("Colors", StringComparison.Ordinal)
		|| type.Name.EndsWith("Request", StringComparison.Ordinal)
		|| type.Name.EndsWith("Scope", StringComparison.Ordinal);
}
