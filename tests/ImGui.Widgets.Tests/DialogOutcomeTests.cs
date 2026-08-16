// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using HexaDialogMessageBoxType = Hexa.NET.ImGui.Widgets.Dialogs.DialogMessageBoxType;
using HexaDialogResult = Hexa.NET.ImGui.Widgets.Dialogs.DialogResult;
using HexaMessageBoxResult = Hexa.NET.ImGui.Widgets.MessageBoxResult;

/// <summary>
/// Tests the conversion from Hexa's two disagreeing result enums into <see cref="DialogOutcome"/>.
/// </summary>
[TestClass]
public sealed class DialogOutcomeTests
{
	[TestMethod]
	public void MapMessageBoxResult_None_MapsToNone() =>
		Assert.AreEqual(DialogOutcome.None, ImGuiWidgets.MapMessageBoxResult(HexaMessageBoxResult.None));

	[TestMethod]
	public void MapMessageBoxResult_Ok_MapsToOk() =>
		Assert.AreEqual(DialogOutcome.Ok, ImGuiWidgets.MapMessageBoxResult(HexaMessageBoxResult.Ok));

	[TestMethod]
	public void MapMessageBoxResult_Cancel_MapsToCancel() =>
		Assert.AreEqual(DialogOutcome.Cancel, ImGuiWidgets.MapMessageBoxResult(HexaMessageBoxResult.Cancel));

	[TestMethod]
	public void MapMessageBoxResult_Yes_MapsToYes() =>
		Assert.AreEqual(DialogOutcome.Yes, ImGuiWidgets.MapMessageBoxResult(HexaMessageBoxResult.Yes));

	[TestMethod]
	public void MapMessageBoxResult_No_MapsToNo() =>
		Assert.AreEqual(DialogOutcome.No, ImGuiWidgets.MapMessageBoxResult(HexaMessageBoxResult.No));

	[TestMethod]
	public void MapDialogResult_ZeroOnYesFlavouredTypes_MapsToYes()
	{
		// DialogResult.Ok and DialogResult.Yes are both 0; only the dialog type disambiguates.
		Assert.AreEqual(DialogOutcome.Yes, ImGuiWidgets.MapDialogResult(HexaDialogResult.Ok, HexaDialogMessageBoxType.YesNo));
		Assert.AreEqual(DialogOutcome.Yes, ImGuiWidgets.MapDialogResult(HexaDialogResult.Ok, HexaDialogMessageBoxType.YesNoCancel));
		Assert.AreEqual(DialogOutcome.Yes, ImGuiWidgets.MapDialogResult(HexaDialogResult.Ok, HexaDialogMessageBoxType.YesCancel));
	}

	[TestMethod]
	public void MapDialogResult_ZeroOnOkFlavouredTypes_MapsToOk()
	{
		Assert.AreEqual(DialogOutcome.Ok, ImGuiWidgets.MapDialogResult(HexaDialogResult.Ok, HexaDialogMessageBoxType.Ok));
		Assert.AreEqual(DialogOutcome.Ok, ImGuiWidgets.MapDialogResult(HexaDialogResult.Ok, HexaDialogMessageBoxType.OkCancel));
	}

	[TestMethod]
	public void MapDialogResult_NonZeroValues_AreTypeIndependent()
	{
		foreach (HexaDialogMessageBoxType type in new[]
		{
			HexaDialogMessageBoxType.Ok,
			HexaDialogMessageBoxType.OkCancel,
			HexaDialogMessageBoxType.YesNo,
			HexaDialogMessageBoxType.YesNoCancel,
			HexaDialogMessageBoxType.YesCancel,
		})
		{
			Assert.AreEqual(DialogOutcome.None, ImGuiWidgets.MapDialogResult(HexaDialogResult.None, type));
			Assert.AreEqual(DialogOutcome.Cancel, ImGuiWidgets.MapDialogResult(HexaDialogResult.Cancel, type));
			Assert.AreEqual(DialogOutcome.Failed, ImGuiWidgets.MapDialogResult(HexaDialogResult.Failed, type));
			Assert.AreEqual(DialogOutcome.No, ImGuiWidgets.MapDialogResult(HexaDialogResult.No, type));
		}
	}

	[TestMethod]
	public void MapDialogResult_UnknownValue_MapsToNone() =>
		Assert.AreEqual(DialogOutcome.None, ImGuiWidgets.MapDialogResult((HexaDialogResult)99, HexaDialogMessageBoxType.Ok));

	[TestMethod]
	public void MapMessageBoxResult_UnknownValue_MapsToNone() =>
		Assert.AreEqual(DialogOutcome.None, ImGuiWidgets.MapMessageBoxResult((HexaMessageBoxResult)99));
}
