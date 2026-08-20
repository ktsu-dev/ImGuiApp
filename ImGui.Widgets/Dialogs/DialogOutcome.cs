// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using HexaDialogMessageBoxType = Hexa.NET.ImGui.Widgets.Dialogs.DialogMessageBoxType;
using HexaDialogResult = Hexa.NET.ImGui.Widgets.Dialogs.DialogResult;
using HexaMessageBoxResult = Hexa.NET.ImGui.Widgets.MessageBoxResult;

/// <summary>
/// How the user dismissed a dialog.
/// </summary>
public enum DialogOutcome
{
	/// <summary>
	/// The dialog was dismissed without a choice, or has not been dismissed yet.
	/// </summary>
	None,

	/// <summary>
	/// The user accepted an OK-flavoured prompt.
	/// </summary>
	Ok,

	/// <summary>
	/// The user canceled.
	/// </summary>
	Cancel,

	/// <summary>
	/// The operation failed. Only Hexa's dialogs report this; message boxes never do.
	/// </summary>
	Failed,

	/// <summary>
	/// The user accepted a Yes-flavoured prompt.
	/// </summary>
	Yes,

	/// <summary>
	/// The user declined a Yes/No prompt.
	/// </summary>
	No,
}

public static partial class ImGuiWidgets
{
	/// <summary>
	/// Converts a message box result. The mapping is 1:1 because Hexa's message box enum keeps
	/// every member distinct.
	/// </summary>
	/// <param name="raw">The result reported by Hexa.</param>
	/// <returns>The equivalent <see cref="DialogOutcome"/>.</returns>
	internal static DialogOutcome MapMessageBoxResult(HexaMessageBoxResult raw) => raw switch
	{
		HexaMessageBoxResult.Ok => DialogOutcome.Ok,
		HexaMessageBoxResult.Cancel => DialogOutcome.Cancel,
		HexaMessageBoxResult.Yes => DialogOutcome.Yes,
		HexaMessageBoxResult.No => DialogOutcome.No,
		_ => DialogOutcome.None,
	};

	/// <summary>
	/// Converts a dialog result, using the dialog's configured type to recover a distinction the
	/// value alone cannot carry.
	/// </summary>
	/// <param name="raw">The result reported by Hexa.</param>
	/// <param name="type">The type the dialog was configured with.</param>
	/// <returns>The equivalent <see cref="DialogOutcome"/>.</returns>
	/// <remarks>
	/// Hexa declares <c>Yes = 0</c> and <c>Ok = 0</c> in the same enum, so the two are the same
	/// value and cannot both appear in a switch. A Yes-flavoured dialog reporting 0 means Yes; any
	/// other dialog reporting 0 means Ok. The configured type is the only place that survives.
	/// </remarks>
	internal static DialogOutcome MapDialogResult(HexaDialogResult raw, HexaDialogMessageBoxType type)
	{
		// Compared numerically on purpose: `case HexaDialogResult.Ok` and `case HexaDialogResult.Yes`
		// are the same label and will not compile together.
		int value = (int)raw;

		if (value == (int)HexaDialogResult.Ok)
		{
			return IsYesFlavoured(type) ? DialogOutcome.Yes : DialogOutcome.Ok;
		}

		if (value == (int)HexaDialogResult.Cancel)
		{
			return DialogOutcome.Cancel;
		}

		if (value == (int)HexaDialogResult.Failed)
		{
			return DialogOutcome.Failed;
		}

		return value == (int)HexaDialogResult.No ? DialogOutcome.No : DialogOutcome.None;
	}

	/// <summary>
	/// Reports whether a dialog type labels its affirmative button "Yes" rather than "OK".
	/// </summary>
	/// <param name="type">The dialog type.</param>
	/// <returns><see langword="true"/> if the affirmative button reads "Yes".</returns>
	private static bool IsYesFlavoured(HexaDialogMessageBoxType type) =>
		type is HexaDialogMessageBoxType.YesNo
			or HexaDialogMessageBoxType.YesNoCancel
			or HexaDialogMessageBoxType.YesCancel;
}
