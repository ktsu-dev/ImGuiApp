// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using ktsu.Semantics.Paths;

using HexaDialogMessageBoxType = Hexa.NET.ImGui.Widgets.Dialogs.DialogMessageBoxType;
using HexaRenameDialog = Hexa.NET.ImGui.Widgets.Dialogs.RenameDialog;

/// <summary>
/// The result of a rename dialog.
/// </summary>
/// <param name="Outcome">How the dialog was dismissed.</param>
/// <param name="Destination">The new path, or <see langword="null"/> if the rename did not happen.</param>
/// <param name="Error">Why the rename failed, or <see langword="null"/> if it did not fail.</param>
public sealed record RenameOutcome(
	DialogOutcome Outcome,
	AbsoluteFilePath? Destination,
	Exception? Error);

public static partial class ImGuiWidgets
{
	/// <summary>
	/// A dialog for renaming a file or folder.
	/// </summary>
	/// <remarks>
	/// This dialog performs the move itself unless <see cref="SkipAutomaticMove"/> is set, and
	/// reports a failure through <see cref="RenameOutcome.Error"/> rather than by throwing.
	/// Requires a per-frame pump: call <see cref="DrawDeferred"/> from your render callback.
	/// </remarks>
	public sealed class RenameDialog
	{
		private readonly HexaRenameDialog dialog;

		/// <summary>
		/// Initializes a new instance of the <see cref="RenameDialog"/> class.
		/// </summary>
		/// <param name="source">The path being renamed.</param>
		/// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
		public RenameDialog(AbsoluteFilePath source)
		{
			Ensure.NotNull(source);
			dialog = new HexaRenameDialog(source.ToString());
		}

		/// <summary>
		/// Gets or sets a value indicating whether an existing destination may be replaced.
		/// </summary>
		public bool Overwrite
		{
			get => dialog.Overwrite;
			set => dialog.Overwrite = value;
		}

		/// <summary>
		/// Gets or sets a value indicating whether the dialog should collect a new name without
		/// moving anything on disk.
		/// </summary>
		public bool SkipAutomaticMove
		{
			get => dialog.NoAutomaticMove;
			set => dialog.NoAutomaticMove = value;
		}

		/// <summary>
		/// Shows the dialog.
		/// </summary>
		/// <param name="onClosed">Invoked once, when the dialog closes.</param>
		/// <exception cref="ArgumentNullException"><paramref name="onClosed"/> is <see langword="null"/>.</exception>
		/// <exception cref="InvalidOperationException">No deferred-drawing pump has ever run.</exception>
		public void Show(Action<RenameOutcome> onClosed)
		{
			Ensure.NotNull(onClosed);
			NotifyDialogShown();

			dialog.Show((_, result) => onClosed(new RenameOutcome(
				MapDialogResult(result, HexaDialogMessageBoxType.Ok),
				TryParseFilePath(dialog.DestinationPath),
				dialog.Exception)));
		}
	}
}
