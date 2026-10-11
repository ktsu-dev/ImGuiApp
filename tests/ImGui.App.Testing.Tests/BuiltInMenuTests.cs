// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Testing.Tests;

using System.Collections.Generic;

using Hexa.NET.ImGui;

using ktsu.ImGui.App;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// <see cref="ImGuiAppConfig.BuiltInMenus"/> decides which of ImGuiApp's own menus follow the
/// application's in the main menu bar. ImGui lists every menu submitted in a frame, by id, so the
/// tests read that list rather than the pixels.
/// </summary>
[TestClass]
public sealed class BuiltInMenuTests
{
	private static readonly string[] Labels = ["File", "Accessibility", "Debug"];

	/// <summary>Renders one frame with an application menu holding File, and returns the labels of the menus submitted.</summary>
	private static List<string> SubmittedMenus(ImGuiAppMenus menus)
	{
		Dictionary<uint, string> ids = [];
		List<string> submitted = [];
		ImGuiAppConfig config = new()
		{
			BuiltInMenus = menus,
			OnAppMenu = () =>
			{
				// The built-in menus are submitted right after this delegate, on the same id stack,
				// so their ids are what these labels hash to here.
				foreach (string label in Labels)
				{
					ids[ImGui.GetID(label)] = label;
				}

				if (ImGui.BeginMenu("File"))
				{
					ImGui.EndMenu();
				}
			},
			OnRender = _ =>
			{
				ImGuiContextPtr context = ImGui.GetCurrentContext();
				for (int i = 0; i < context.MenusIdSubmittedThisFrame.Size; i++)
				{
					if (ids.TryGetValue(context.MenusIdSubmittedThisFrame[i], out string? label))
					{
						submitted.Add(label);
					}
				}
			},
		};

		using ImGuiAppHarness harness = ImGuiAppHarness.Start(config, new HarnessOptions { Width = 800, Height = 200 });
		harness.Step();
		return submitted;
	}

	[TestMethod]
	public void BuiltInMenus_DefaultsToAll()
	{
		// Every application before this setting showed both menus, so the default keeps them.
		Assert.AreEqual(ImGuiAppMenus.All, new ImGuiAppConfig().BuiltInMenus);
	}

	[TestMethod]
	public void All_AppendsBothMenus_AfterTheApplicationsOwn() =>
		Assert.AreEqual("File, Accessibility, Debug", string.Join(", ", SubmittedMenus(ImGuiAppMenus.All)));

	[TestMethod]
	public void None_LeavesOnlyTheApplicationsMenus() =>
		Assert.AreEqual("File", string.Join(", ", SubmittedMenus(ImGuiAppMenus.None)));

	[TestMethod]
	public void Accessibility_AddsOnlyTheAccessibilityMenu() =>
		Assert.AreEqual("File, Accessibility", string.Join(", ", SubmittedMenus(ImGuiAppMenus.Accessibility)));

	[TestMethod]
	public void Debug_AddsOnlyTheDebugMenu() =>
		Assert.AreEqual("File, Debug", string.Join(", ", SubmittedMenus(ImGuiAppMenus.Debug)));
}
