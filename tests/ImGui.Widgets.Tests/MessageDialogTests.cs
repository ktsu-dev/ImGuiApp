// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using HexaMessageBoxType = Hexa.NET.ImGui.Widgets.MessageBoxType;

/// <summary>
/// Tests the button-set mapping for message dialogs. The dialogs need a live ImGui context.
/// </summary>
[TestClass]
public sealed class MessageDialogTests
{
	[TestMethod]
	public void MapButtons_CoversEveryMember()
	{
		Assert.AreEqual(HexaMessageBoxType.Ok, ImGuiWidgets.MapButtons(MessageBoxButtons.Ok));
		Assert.AreEqual(HexaMessageBoxType.OkCancel, ImGuiWidgets.MapButtons(MessageBoxButtons.OkCancel));
		Assert.AreEqual(HexaMessageBoxType.YesNo, ImGuiWidgets.MapButtons(MessageBoxButtons.YesNo));
		Assert.AreEqual(HexaMessageBoxType.YesNoCancel, ImGuiWidgets.MapButtons(MessageBoxButtons.YesNoCancel));
		Assert.AreEqual(HexaMessageBoxType.YesCancel, ImGuiWidgets.MapButtons(MessageBoxButtons.YesCancel));
	}

	[TestMethod]
	public void MapButtons_UnknownValue_FallsBackToOk() =>
		Assert.AreEqual(HexaMessageBoxType.Ok, ImGuiWidgets.MapButtons((MessageBoxButtons)99));
}
