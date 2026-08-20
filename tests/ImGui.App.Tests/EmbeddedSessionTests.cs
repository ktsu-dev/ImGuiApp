// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests for the additive embedded/non-blocking hosting surface. These cover the configuration defaults
/// and the synchronous validation that runs before any window is created, so they do not require a GPU.
/// </summary>
[TestClass]
public class EmbeddedSessionTests
{
	[TestInitialize]
	public void Setup()
	{
		ImGuiApp.Reset();
	}

	[TestMethod]
	public void WindowHost_DefaultsToStandalone()
	{
		ImGuiAppConfig config = new();
		Assert.AreEqual(ImGuiAppWindowHost.Standalone, config.WindowHost, "Default host must preserve standalone behavior.");
		Assert.AreEqual(0, config.ParentWindowHandle);
	}

	[TestMethod]
	public void StartEmbedded_NullConfig_Throws()
	{
		Assert.ThrowsExactly<ArgumentNullException>(() => ImGuiApp.StartEmbedded(null!));
	}

	[TestMethod]
	public void StartEmbedded_EmbeddedChildWithoutHandle_Throws()
	{
		ImGuiAppConfig config = new()
		{
			WindowHost = ImGuiAppWindowHost.EmbeddedChild,
			// ParentWindowHandle deliberately left at its default of 0.
		};

		Assert.ThrowsExactly<ArgumentException>(() => ImGuiApp.StartEmbedded(config));
	}
}
