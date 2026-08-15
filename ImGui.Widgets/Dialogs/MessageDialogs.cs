// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using HexaDialogMessageBox = Hexa.NET.ImGui.Widgets.Dialogs.DialogMessageBox;
using HexaDialogMessageBoxType = Hexa.NET.ImGui.Widgets.Dialogs.DialogMessageBoxType;
using HexaMessageBox = Hexa.NET.ImGui.Widgets.MessageBox;
using HexaMessageBoxType = Hexa.NET.ImGui.Widgets.MessageBoxType;

/// <summary>
/// The button set a message dialog offers.
/// </summary>
public enum MessageBoxButtons
{
	/// <summary>
	/// A single OK button.
	/// </summary>
	Ok,

	/// <summary>
	/// OK and Cancel.
	/// </summary>
	OkCancel,

	/// <summary>
	/// Yes and No.
	/// </summary>
	YesNo,

	/// <summary>
	/// Yes, No and Cancel.
	/// </summary>
	YesNoCancel,

	/// <summary>
	/// Yes and Cancel.
	/// </summary>
	YesCancel,
}

public static partial class ImGuiWidgets
{
	/// <summary>
	/// Converts our button set to Hexa's message box type.
	/// </summary>
	/// <param name="buttons">The button set.</param>
	/// <returns>The equivalent Hexa type.</returns>
	internal static HexaMessageBoxType MapButtons(MessageBoxButtons buttons) => buttons switch
	{
		MessageBoxButtons.OkCancel => HexaMessageBoxType.OkCancel,
		MessageBoxButtons.YesNo => HexaMessageBoxType.YesNo,
		MessageBoxButtons.YesNoCancel => HexaMessageBoxType.YesNoCancel,
		MessageBoxButtons.YesCancel => HexaMessageBoxType.YesCancel,
		_ => HexaMessageBoxType.Ok,
	};

	/// <summary>
	/// Converts our button set to Hexa's dialog message box type.
	/// </summary>
	/// <param name="buttons">The button set.</param>
	/// <returns>The equivalent Hexa type.</returns>
	internal static HexaDialogMessageBoxType MapDialogButtons(MessageBoxButtons buttons) => buttons switch
	{
		MessageBoxButtons.OkCancel => HexaDialogMessageBoxType.OkCancel,
		MessageBoxButtons.YesNo => HexaDialogMessageBoxType.YesNo,
		MessageBoxButtons.YesNoCancel => HexaDialogMessageBoxType.YesNoCancel,
		MessageBoxButtons.YesCancel => HexaDialogMessageBoxType.YesCancel,
		_ => HexaDialogMessageBoxType.Ok,
	};

	/// <summary>
	/// Shows a modal message box.
	/// </summary>
	/// <param name="title">The window title, which is also the dialog's identity.</param>
	/// <param name="message">The message body.</param>
	/// <param name="buttons">The button set to offer.</param>
	/// <param name="onClosed">Invoked once, when the box closes.</param>
	/// <exception cref="ArgumentNullException">Any argument is <see langword="null"/>.</exception>
	/// <exception cref="InvalidOperationException">No deferred-drawing pump has ever run.</exception>
	/// <remarks>
	/// The answer arrives through <paramref name="onClosed"/> only. Hexa's <c>MessageBox</c> is a
	/// struct that its registry stores and redraws by value, so the instance returned by its own
	/// <c>Show</c> is a detached copy whose result is never updated. This wrapper deliberately
	/// discards that return value.
	/// </remarks>
	public static void ShowMessageBox(string title, string message, MessageBoxButtons buttons, Action<DialogOutcome> onClosed)
	{
		Ensure.NotNull(title);
		Ensure.NotNull(message);
		Ensure.NotNull(onClosed);
		NotifyDialogShown();

		_ = HexaMessageBox.Show(
			title,
			message,
			userdata: null,
			callback: (box, _) => onClosed(MapMessageBoxResult(box.Result)),
			type: MapButtons(buttons));
	}

	/// <summary>
	/// A message box that behaves like the other dialogs: a movable window rather than a
	/// re-centring modal popup.
	/// </summary>
	/// <remarks>
	/// Requires a per-frame pump: call <see cref="DrawDeferred"/> from your render callback.
	/// </remarks>
	public sealed class DialogMessageBox
	{
		private readonly HexaDialogMessageBox dialog;
		private readonly HexaDialogMessageBoxType type;

		/// <summary>
		/// Initializes a new instance of the <see cref="DialogMessageBox"/> class.
		/// </summary>
		/// <param name="title">The window title, which is also the dialog's identity.</param>
		/// <param name="message">The message body.</param>
		/// <param name="buttons">The button set to offer.</param>
		/// <exception cref="ArgumentNullException"><paramref name="title"/> or <paramref name="message"/> is <see langword="null"/>.</exception>
		public DialogMessageBox(string title, string message, MessageBoxButtons buttons)
		{
			Ensure.NotNull(title);
			Ensure.NotNull(message);

			type = MapDialogButtons(buttons);
			dialog = new HexaDialogMessageBox(title, message, type);
		}

		/// <summary>
		/// Shows the dialog.
		/// </summary>
		/// <param name="onClosed">Invoked once, when the dialog closes.</param>
		/// <exception cref="ArgumentNullException"><paramref name="onClosed"/> is <see langword="null"/>.</exception>
		/// <exception cref="InvalidOperationException">No deferred-drawing pump has ever run.</exception>
		public void Show(Action<DialogOutcome> onClosed)
		{
			Ensure.NotNull(onClosed);
			NotifyDialogShown();

			// The configured type is passed through because Hexa aliases Yes to Ok in DialogResult;
			// it is the only surviving evidence of which button the user pressed.
			dialog.Show((_, result) => onClosed(MapDialogResult(result, type)));
		}
	}
}
