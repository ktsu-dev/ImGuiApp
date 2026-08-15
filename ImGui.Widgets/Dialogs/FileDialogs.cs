// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System.Collections.Generic;
using System.Collections.ObjectModel;

using ktsu.Semantics.Paths;
using ktsu.Semantics.Strings;

using HexaDialogMessageBoxType = Hexa.NET.ImGui.Widgets.Dialogs.DialogMessageBoxType;
using HexaDialogResult = Hexa.NET.ImGui.Widgets.Dialogs.DialogResult;
using HexaOpenFileDialog = Hexa.NET.ImGui.Widgets.Dialogs.OpenFileDialog;
using HexaOpenFolderDialog = Hexa.NET.ImGui.Widgets.Dialogs.OpenFolderDialog;
using HexaSaveFileDialog = Hexa.NET.ImGui.Widgets.Dialogs.SaveFileDialog;

/// <summary>
/// The result of a file dialog.
/// </summary>
/// <param name="Outcome">How the dialog was dismissed.</param>
/// <param name="Path">The chosen file, or <see langword="null"/> if none was chosen.</param>
/// <param name="Selection">Every chosen file. Empty unless multiple selection was enabled.</param>
public sealed record FileDialogOutcome(
	DialogOutcome Outcome,
	AbsoluteFilePath? Path,
	IReadOnlyList<AbsoluteFilePath> Selection);

/// <summary>
/// The result of a folder dialog.
/// </summary>
/// <param name="Outcome">How the dialog was dismissed.</param>
/// <param name="Path">The chosen folder, or <see langword="null"/> if none was chosen.</param>
public sealed record FolderDialogOutcome(
	DialogOutcome Outcome,
	AbsoluteDirectoryPath? Path);

public static partial class ImGuiWidgets
{
	/// <summary>
	/// Converts a path string from Hexa into a semantic file path.
	/// </summary>
	/// <param name="raw">The path Hexa reported.</param>
	/// <returns>The parsed path, or <see langword="null"/> if there was none.</returns>
	internal static AbsoluteFilePath? TryParseFilePath(string? raw) =>
		string.IsNullOrWhiteSpace(raw) ? null : raw.As<AbsoluteFilePath>();

	/// <summary>
	/// Converts a path string from Hexa into a semantic directory path.
	/// </summary>
	/// <param name="raw">The path Hexa reported.</param>
	/// <returns>The parsed path, or <see langword="null"/> if there was none.</returns>
	internal static AbsoluteDirectoryPath? TryParseDirectoryPath(string? raw) =>
		string.IsNullOrWhiteSpace(raw) ? null : raw.As<AbsoluteDirectoryPath>();

	/// <summary>
	/// A dialog for choosing one or more existing files.
	/// </summary>
	/// <remarks>
	/// Requires a per-frame pump: call <see cref="DrawDeferred"/> from your render callback.
	/// Needs a Material Icons font in the atlas for its navigation bar; see
	/// <c>ImGuiAppConfig.OnConfigureFonts</c>. The underlying window is identified by a fixed
	/// title, so two of these open at once will collide.
	/// </remarks>
	public sealed class OpenFileDialog
	{
		private readonly HexaOpenFileDialog dialog = new();

		/// <summary>
		/// Gets or sets a value indicating whether more than one file may be chosen.
		/// </summary>
		public bool AllowMultipleSelection
		{
			get => dialog.AllowMultipleSelection;
			set => dialog.AllowMultipleSelection = value;
		}

		/// <summary>
		/// Shows the dialog.
		/// </summary>
		/// <param name="onClosed">Invoked once, when the dialog closes.</param>
		/// <exception cref="ArgumentNullException"><paramref name="onClosed"/> is <see langword="null"/>.</exception>
		/// <exception cref="InvalidOperationException">No deferred-drawing pump has ever run.</exception>
		public void Show(Action<FileDialogOutcome> onClosed)
		{
			Ensure.NotNull(onClosed);
			NotifyDialogShown();

			dialog.Show((_, result) => onClosed(BuildOutcome(dialog, result)));
		}

		/// <summary>
		/// Builds the managed outcome from the dialog's state at close time.
		/// </summary>
		/// <param name="source">The dialog that closed.</param>
		/// <param name="result">The raw result Hexa reported.</param>
		/// <returns>The outcome to hand to the caller.</returns>
		private static FileDialogOutcome BuildOutcome(HexaOpenFileDialog source, HexaDialogResult result)
		{
			Collection<AbsoluteFilePath> selection = [];
			foreach (string entry in source.Selection)
			{
				AbsoluteFilePath? parsed = TryParseFilePath(entry);
				if (parsed is not null)
				{
					selection.Add(parsed);
				}
			}

			// File dialogs have no Yes/No variant, so the Ok flavour is always correct here.
			return new FileDialogOutcome(
				MapDialogResult(result, HexaDialogMessageBoxType.Ok),
				TryParseFilePath(source.SelectedFile),
				selection);
		}
	}

	/// <summary>
	/// A dialog for choosing a file to write to.
	/// </summary>
	/// <remarks>
	/// Requires a per-frame pump: call <see cref="DrawDeferred"/> from your render callback.
	/// Needs a Material Icons font in the atlas for its navigation bar.
	/// </remarks>
	public sealed class SaveFileDialog
	{
		private readonly HexaSaveFileDialog dialog = new();

		/// <summary>
		/// Shows the dialog.
		/// </summary>
		/// <param name="onClosed">Invoked once, when the dialog closes.</param>
		/// <exception cref="ArgumentNullException"><paramref name="onClosed"/> is <see langword="null"/>.</exception>
		/// <exception cref="InvalidOperationException">No deferred-drawing pump has ever run.</exception>
		public void Show(Action<FileDialogOutcome> onClosed)
		{
			Ensure.NotNull(onClosed);
			NotifyDialogShown();

			dialog.Show((_, result) => onClosed(new FileDialogOutcome(
				MapDialogResult(result, HexaDialogMessageBoxType.Ok),
				TryParseFilePath(dialog.SelectedFile),
				[])));
		}
	}

	/// <summary>
	/// A dialog for choosing an existing folder.
	/// </summary>
	/// <remarks>
	/// Requires a per-frame pump: call <see cref="DrawDeferred"/> from your render callback.
	/// Needs a Material Icons font in the atlas for its navigation bar.
	/// </remarks>
	public sealed class OpenFolderDialog
	{
		private readonly HexaOpenFolderDialog dialog = new();

		/// <summary>
		/// Shows the dialog.
		/// </summary>
		/// <param name="onClosed">Invoked once, when the dialog closes.</param>
		/// <exception cref="ArgumentNullException"><paramref name="onClosed"/> is <see langword="null"/>.</exception>
		/// <exception cref="InvalidOperationException">No deferred-drawing pump has ever run.</exception>
		public void Show(Action<FolderDialogOutcome> onClosed)
		{
			Ensure.NotNull(onClosed);
			NotifyDialogShown();

			dialog.Show((_, result) => onClosed(new FolderDialogOutcome(
				MapDialogResult(result, HexaDialogMessageBoxType.Ok),
				TryParseDirectoryPath(dialog.SelectedFolder))));
		}
	}
}
