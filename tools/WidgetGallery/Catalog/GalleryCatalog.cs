// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.WidgetGallery.Catalog;

using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Every tile in the gallery. Each category lives in its own file; adding a widget means adding one
/// entry to the file for its category.
/// </summary>
/// <remarks>
/// The catalogue is rebuilt for every run, so an entry keeps whatever state it needs as locals
/// captured by its lambdas rather than in fields that would outlive the run.
/// </remarks>
internal static class GalleryCatalog
{
	/// <summary>Builds every entry, in gallery order.</summary>
	/// <returns>The entries.</returns>
	public static IReadOnlyList<GalleryEntry> Build() =>
	[
		.. InputAndControlsTiles.Build(),
		.. DisplayAndStatusTiles.Build(),
		.. ProgressAndLoadingTiles.Build(),
		.. DataAndSignalsTiles.Build(),
		.. LayoutAndContainersTiles.Build(),
		.. EditorTiles.Build(),
		.. DialogsAndWindowsTiles.Build(),
	];

	/// <summary>Gets every <c>ImGuiWidgets</c> member some entry claims to show.</summary>
	/// <param name="entries">The entries.</param>
	/// <returns>The covered member names.</returns>
	public static ISet<string> CoveredMembers(IEnumerable<GalleryEntry> entries) =>
		entries.SelectMany(entry => entry.Covers).ToHashSet();
}
