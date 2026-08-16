// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using ktsu.Semantics.Paths;

using HexaDialogMessageBoxType = Hexa.NET.ImGui.Widgets.Dialogs.DialogMessageBoxType;
using HexaRenameDialog = Hexa.NET.ImGui.Widgets.Dialogs.RenameDialog;

/// <summary>
/// The result of a rename dialog.
/// </summary>
/// <param name="Outcome">How the dialog was dismissed.</param>
/// <param name="Destination">
/// The new path, or <see langword="null"/> if the rename did not happen. Only ever populated when
/// <paramref name="Outcome"/> is <see cref="DialogOutcome.Ok"/>.
/// </param>
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
		private readonly DialogShowGuard guard = new(nameof(RenameDialog));

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
		/// <exception cref="InvalidOperationException">
		/// No deferred-drawing pump has ever run, or this instance is already shown.
		/// </exception>
		public void Show(Action<RenameOutcome> onClosed)
		{
			Ensure.NotNull(onClosed);
			NotifyDialogShown();
			guard.Enter();

			dialog.Show((_, result) =>
			{
				guard.Exit();
				onClosed(BuildRenameOutcome(
					MapDialogResult(result, HexaDialogMessageBoxType.Ok),
					dialog.DestinationPath,
					dialog.Exception));
			});
		}
	}

	/// <summary>
	/// Builds the managed outcome of a rename from the dialog's state at close time.
	/// </summary>
	/// <param name="mapped">The outcome Hexa's result maps to.</param>
	/// <param name="destinationPath">The destination Hexa reported.</param>
	/// <param name="error">The exception the move threw, if any.</param>
	/// <returns>The outcome to hand to the caller.</returns>
	/// <remarks>
	/// <para>
	/// Hexa catches a failing move into its <c>Exception</c> property and then still closes with
	/// <c>DialogResult.Ok</c>, so the raw result alone reports success for a rename that did not
	/// happen. A captured exception therefore overrides the mapped outcome.
	/// </para>
	/// <para>
	/// Hexa also seeds its destination with the source path and recomputes it on every keystroke, so
	/// it is populated on Cancel and on failure alike. The destination is dropped unless the rename
	/// actually succeeded, which is what <see cref="RenameOutcome.Destination"/> documents.
	/// </para>
	/// </remarks>
	internal static RenameOutcome BuildRenameOutcome(DialogOutcome mapped, string? destinationPath, Exception? error)
	{
		DialogOutcome outcome = error is not null ? DialogOutcome.Failed : mapped;

		return new RenameOutcome(
			outcome,
			outcome == DialogOutcome.Ok ? TryParseFilePath(destinationPath) : null,
			error);
	}
}
