// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;

using ktsu.Semantics.Paths;

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
	/// <returns>
	/// The parsed path, or <see langword="null"/> if there was none or it was not a valid absolute
	/// file path.
	/// </returns>
	/// <remarks>
	/// Deliberately non-throwing. Hexa hands these values to us from inside its own draw loop, so an
	/// exception here would unwind through <c>ImGui.End()</c> and Hexa's dialog bookkeeping and
	/// leave the dialog manager permanently wedged. Hexa does not guarantee an absolute path: a
	/// bare filename typed into <c>SaveFileDialog</c>'s text box reaches us verbatim.
	/// </remarks>
	internal static AbsoluteFilePath? TryParseFilePath(string? raw) =>
		!string.IsNullOrWhiteSpace(raw) && AbsoluteFilePath.TryCreate(raw, out AbsoluteFilePath? parsed)
			? parsed
			: null;

	/// <summary>
	/// Converts a path string from Hexa into a semantic directory path.
	/// </summary>
	/// <param name="raw">The path Hexa reported.</param>
	/// <returns>
	/// The parsed path, or <see langword="null"/> if there was none or it was not a valid absolute
	/// directory path.
	/// </returns>
	/// <remarks>Non-throwing, for the same reason as <see cref="TryParseFilePath"/>.</remarks>
	internal static AbsoluteDirectoryPath? TryParseDirectoryPath(string? raw) =>
		!string.IsNullOrWhiteSpace(raw) && AbsoluteDirectoryPath.TryCreate(raw, out AbsoluteDirectoryPath? parsed)
			? parsed
			: null;

	/// <summary>
	/// Resolves the target a save dialog reported into an absolute file path.
	/// </summary>
	/// <param name="selectedFile">The value Hexa's <c>SelectedFile</c> holds at close time.</param>
	/// <param name="currentFolder">The folder the dialog was browsing.</param>
	/// <returns>The resolved path, or <see langword="null"/> if it cannot be resolved.</returns>
	/// <remarks>
	/// Hexa's save dialog binds its text box straight to the private backing field, bypassing the
	/// property setter that would have joined the typed name to the current folder. A bare filename
	/// is therefore the normal case, not an edge case, so it is joined to the browsed folder here.
	/// </remarks>
	internal static AbsoluteFilePath? ResolveSaveTarget(string? selectedFile, string? currentFolder)
	{
		if (string.IsNullOrWhiteSpace(selectedFile))
		{
			return null;
		}

		if (Path.IsPathFullyQualified(selectedFile))
		{
			return TryParseFilePath(selectedFile);
		}

		return string.IsNullOrWhiteSpace(currentFolder) || !Path.IsPathFullyQualified(currentFolder)
			? null
			: TryParseFilePath(Path.Combine(currentFolder, selectedFile));
	}

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
		private readonly DialogShowGuard guard = new(nameof(OpenFileDialog));

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
		/// <exception cref="InvalidOperationException">
		/// No deferred-drawing pump has ever run, or this instance is already shown.
		/// </exception>
		public void Show(Action<FileDialogOutcome> onClosed)
		{
			Ensure.NotNull(onClosed);
			NotifyDialogShown();
			guard.Enter();

			dialog.Show((_, result) =>
			{
				guard.Exit();
				onClosed(BuildOutcome(dialog, result));
			});
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
		private readonly DialogShowGuard guard = new(nameof(SaveFileDialog));

		/// <summary>
		/// Shows the dialog.
		/// </summary>
		/// <param name="onClosed">Invoked once, when the dialog closes.</param>
		/// <exception cref="ArgumentNullException"><paramref name="onClosed"/> is <see langword="null"/>.</exception>
		/// <exception cref="InvalidOperationException">
		/// No deferred-drawing pump has ever run, or this instance is already shown.
		/// </exception>
		public void Show(Action<FileDialogOutcome> onClosed)
		{
			Ensure.NotNull(onClosed);
			NotifyDialogShown();
			guard.Enter();

			dialog.Show((_, result) =>
			{
				guard.Exit();
				onClosed(new FileDialogOutcome(
					MapDialogResult(result, HexaDialogMessageBoxType.Ok),
					ResolveSaveTarget(dialog.SelectedFile, dialog.CurrentFolder),
					[]));
			});
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
		private readonly DialogShowGuard guard = new(nameof(OpenFolderDialog));

		/// <summary>
		/// Shows the dialog.
		/// </summary>
		/// <param name="onClosed">Invoked once, when the dialog closes.</param>
		/// <exception cref="ArgumentNullException"><paramref name="onClosed"/> is <see langword="null"/>.</exception>
		/// <exception cref="InvalidOperationException">
		/// No deferred-drawing pump has ever run, or this instance is already shown.
		/// </exception>
		public void Show(Action<FolderDialogOutcome> onClosed)
		{
			Ensure.NotNull(onClosed);
			NotifyDialogShown();
			guard.Enter();

			dialog.Show((_, result) =>
			{
				guard.Exit();
				onClosed(new FolderDialogOutcome(
					MapDialogResult(result, HexaDialogMessageBoxType.Ok),
					TryParseDirectoryPath(dialog.SelectedFolder)));
			});
		}
	}
}
