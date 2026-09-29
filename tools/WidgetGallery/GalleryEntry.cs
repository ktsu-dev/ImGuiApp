// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.WidgetGallery;

using System;
using System.Collections.Generic;
using System.Numerics;

/// <summary>The groups the gallery is laid out in, in the order the README's feature list uses.</summary>
internal enum GalleryCategory
{
	/// <summary>Controls a user drags, clicks or types into.</summary>
	InputAndControls,

	/// <summary>Things that show a value or a status without being edited.</summary>
	DisplayAndStatus,

	/// <summary>Determinate and indeterminate progress, and loading placeholders.</summary>
	ProgressAndLoading,

	/// <summary>Plots, meters and overlay tracks for signals and distributions.</summary>
	DataAndSignals,

	/// <summary>Containers and layout helpers that hold other content.</summary>
	LayoutAndContainers,

	/// <summary>Callback-driven editors for curves, timelines and text differences.</summary>
	Editors,

	/// <summary>Dialogs and windows drawn by the deferred-drawing pump.</summary>
	DialogsAndWindows,
}

/// <summary>
/// One tile of the gallery: how to draw a widget on its own, and what to do to it before it is
/// photographed.
/// </summary>
/// <remarks>
/// <para>
/// An entry is drawn alone in a fresh harness, exactly as a widget test is, so no entry can leak
/// state into another's picture. The tile is cropped to the union of two rectangles: the layout
/// rectangle ImGui reports around everything the entry submitted, and the pixels that differ from a
/// frame drawn without it. The first catches a widget drawn in the window's own colour; the second
/// catches everything drawn outside the layout, such as an open popup, a tooltip or a dialog.
/// </para>
/// <para>
/// <see cref="Covers"/> names the members of <c>ImGuiWidgets</c> the tile shows. The coverage check
/// compares those names with the widget surface, so a widget that lands later without a tile is
/// reported rather than silently missing from the gallery.
/// </para>
/// </remarks>
/// <param name="Name">The caption, and the file name the tile is written under.</param>
/// <param name="Category">The group the tile is laid out in.</param>
/// <param name="Covers">The <c>ImGuiWidgets</c> members this tile shows.</param>
/// <param name="Draw">Draws the widget. Called once per frame.</param>
internal sealed record GalleryEntry(
	string Name,
	GalleryCategory Category,
	IReadOnlyList<string> Covers,
	Action<GalleryContext> Draw)
{
	/// <summary>
	/// Gets the size of a child region to draw inside, for widgets that fill whatever space they are
	/// given. Null draws straight into the window.
	/// </summary>
	public Vector2? Bounds { get; init; }

	/// <summary>Gets the viewport width. Large enough that nothing the entry draws is clipped.</summary>
	public int ViewportWidth { get; init; } = 640;

	/// <summary>Gets the viewport height.</summary>
	public int ViewportHeight { get; init; } = 420;

	/// <summary>Gets a value indicating whether the docking flag is set before the first frame.</summary>
	public bool EnableDocking { get; init; }

	/// <summary>Gets how many frames to draw before interacting, so layout and animation settle.</summary>
	public int SettleFrames { get; init; } = 4;

	/// <summary>
	/// Gets what to do once the widget has settled: hover it to raise a tooltip, click it to open a
	/// popup, show a dialog. Runs against the live harness.
	/// </summary>
	public Action<GalleryContext>? Interact { get; init; }

	/// <summary>
	/// Gets what to do after the picture is taken. Dialogs are process-static, so an entry that shows
	/// one has to close it again or it is drawn over every later dialog's tile.
	/// </summary>
	public Action<GalleryContext>? Cleanup { get; init; }

	/// <summary>
	/// Gets a value indicating whether the tile appears in the composite images. A tile left out is
	/// still written on its own; this is for near-duplicates that would only make the gallery longer.
	/// </summary>
	public bool InComposite { get; init; } = true;

	/// <summary>Gets the file name the tile is written under, without an extension.</summary>
	public string Slug => MakeSlug(Name);

	/// <summary>Turns a caption into a lowercase, hyphenated file name.</summary>
	/// <param name="name">The caption.</param>
	/// <returns>The file name.</returns>
	internal static string MakeSlug(string name)
	{
		System.Text.StringBuilder builder = new(name.Length);
		bool pendingHyphen = false;

		foreach (char c in name)
		{
			if (char.IsLetterOrDigit(c))
			{
				if (pendingHyphen && builder.Length > 0)
				{
					builder.Append('-');
				}

				builder.Append(char.ToLowerInvariant(c));
				pendingHyphen = false;
			}
			else
			{
				pendingHyphen = true;
			}
		}

		return builder.ToString();
	}
}
