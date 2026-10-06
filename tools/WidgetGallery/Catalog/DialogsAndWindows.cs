// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.WidgetGallery.Catalog;

using System;
using System.Collections.Generic;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.App.Testing;
using ktsu.ImGui.Widgets;
using ktsu.Semantics.Paths;
using ktsu.Semantics.Strings;

/// <summary>The tiles in the DialogsAndWindows group.</summary>
internal static class DialogsAndWindowsTiles
{
	/// <summary>Where Hexa opens its 1000x600 file pickers, and so how big their viewport has to be.</summary>
	private const int PickerViewportWidth = 1100;
	private const int PickerViewportHeight = 700;

	/// <summary>Builds the group's entries.</summary>
	/// <returns>The entries, in gallery order.</returns>
	public static IEnumerable<GalleryEntry> Build()
	{
		const GalleryCategory Category = GalleryCategory.DialogsAndWindows;

		AbsoluteDirectoryPath treeFolder = SampleFolder.Root.As<AbsoluteDirectoryPath>();
		AbsoluteDirectoryPath treeHome = SampleFolder.Root.As<AbsoluteDirectoryPath>();
		yield return new("FileTreeView", Category, [nameof(ImGuiWidgets.FileTreeView)], _ =>
			ImGuiWidgets.FileTreeView("##files", new Vector2(240f, 200f), ref treeFolder, treeHome));

		ImGuiWidgets.DialogMessageBox messageBox = new("Unsaved changes", "Save your changes before closing?", MessageBoxButtons.YesNo);
		yield return Dialog(
			"Message box",
			[nameof(ImGuiWidgets.DialogMessageBox), nameof(ImGuiWidgets.ShowMessageBox)],
			() => messageBox.Show(_ => { }),
			context => context.ClickFirstDialogButton());

		ImGuiWidgets.RenameDialog? rename = null;
		yield return Dialog(
			"RenameDialog",
			[nameof(ImGuiWidgets.RenameDialog)],
			() =>
			{
				rename = new ImGuiWidgets.RenameDialog(SampleFolder.PathOf("notes.txt").As<AbsoluteFilePath>());
				rename.Show(_ => { });
			},
			// Cancel is the leftmost button, so this closes the dialog without renaming anything.
			context => context.ClickFirstDialogButton());

		yield return Picker("OpenFileDialog", nameof(ImGuiWidgets.OpenFileDialog), () => new ImGuiWidgets.OpenFileDialog().Show(_ => { }));

		// The three pickers are one window with different buttons, and each is the size of every other
		// tile in the group together, so only the first goes into the composites.
		yield return Picker("SaveFileDialog", nameof(ImGuiWidgets.SaveFileDialog), () => new ImGuiWidgets.SaveFileDialog().Show(_ => { })) with { InComposite = false };
		yield return Picker("OpenFolderDialog", nameof(ImGuiWidgets.OpenFolderDialog), () => new ImGuiWidgets.OpenFolderDialog().Show(_ => { })) with { InComposite = false };

		GalleryWindow window = new();
		yield return new("DockedWindow", Category, [nameof(ImGuiWidgets.DockedWindow)], _ => ImGuiWidgets.DrawDeferredDocked())
		{
			EnableDocking = true,
			ViewportWidth = 520,
			ViewportHeight = 320,
			Interact = context =>
			{
				window.Show();
				context.Harness.Step(4);
			},
			Cleanup = _ => window.Close(),
		};
	}

	/// <summary>A small dialog: shown once the pump is running, answered after it is photographed.</summary>
	private static GalleryEntry Dialog(string name, IReadOnlyList<string> covers, Action show, Action<GalleryContext> close) =>
		new(name, GalleryCategory.DialogsAndWindows, covers, _ => ImGuiWidgets.DrawDeferred())
		{
			Interact = context =>
			{
				show();
				context.Harness.Step(4);
			},
			Cleanup = close,
		};

	/// <summary>
	/// A file picker, opened on the sample folder and cancelled after it is photographed.
	/// </summary>
	/// <remarks>
	/// Hexa's pickers open on the process's working directory, so it is pointed at the sample folder
	/// for the length of the capture. Their buttons cannot be found by colour, so the picker is
	/// cancelled by position: Cancel sits 240 pixels in from the window's right edge on its bottom row.
	/// </remarks>
	private static GalleryEntry Picker(string name, string covers, Action show)
	{
		string previousDirectory = Environment.CurrentDirectory;

		return new(name, GalleryCategory.DialogsAndWindows, [covers], _ => ImGuiWidgets.DrawDeferred())
		{
			ViewportWidth = PickerViewportWidth,
			ViewportHeight = PickerViewportHeight,
			Interact = context =>
			{
				previousDirectory = Environment.CurrentDirectory;
				Environment.CurrentDirectory = SampleFolder.Root;
				show();
				context.Harness.Step(6);
			},
			Cleanup = context =>
			{
				if (context.ChangedBounds() is Rectangle picker)
				{
					context.ClickAt(new Vector2(picker.MaxX - 240f, picker.MaxY - 20f));
				}

				Environment.CurrentDirectory = previousDirectory;
			},
		};
	}

	/// <summary>A dockable tool window with a little content in it.</summary>
	private sealed class GalleryWindow : ImGuiWidgets.DockedWindow
	{
		private float exposure = 0.4f;

		protected override string Title => "Inspector";

		protected override void DrawContent()
		{
			ImGui.TextUnformatted("Docked windows open floating");
			ImGui.TextDisabled("and dock where the user drags them.");
			ImGui.SliderFloat("Exposure", ref exposure, -2f, 2f);
		}
	}
}
