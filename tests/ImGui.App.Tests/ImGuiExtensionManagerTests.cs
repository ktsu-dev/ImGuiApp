// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.App.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using ktsu.ImGui.App;

/// <summary>
/// Tests for ImGuiExtensionManager covering initialization, availability detection, and lifecycle methods.
/// </summary>
[TestClass]
public sealed class ImGuiExtensionManagerTests
{
	[TestMethod]
	public void Initialize_DoesNotThrow()
	{
		// Initialize uses reflection to detect extensions - should not throw even without extensions loaded
		ImGuiExtensionManager.Initialize();
	}

	[TestMethod]
	public void Initialize_CalledTwice_DoesNotThrow()
	{
		ImGuiExtensionManager.Initialize();
		ImGuiExtensionManager.Initialize();
	}

	[TestMethod]
	public void IsImGuizmoAvailable_ReturnsBoolean()
	{
		ImGuiExtensionManager.Initialize();
		// Should return true or false without throwing
		_ = ImGuiExtensionManager.IsImGuizmoAvailable;
	}

	[TestMethod]
	public void IsImNodesAvailable_ReturnsBoolean()
	{
		ImGuiExtensionManager.Initialize();
		_ = ImGuiExtensionManager.IsImNodesAvailable;
	}

	[TestMethod]
	public void IsImPlotAvailable_ReturnsBoolean()
	{
		ImGuiExtensionManager.Initialize();
		_ = ImGuiExtensionManager.IsImPlotAvailable;
	}

	[TestMethod]
	public void IsImNodesContextCreated_ReturnsFalseBeforeContextCreation()
	{
		// Without a valid ImGui context, extension contexts can't be created
		Assert.IsFalse(ImGuiExtensionManager.IsImNodesContextCreated);
	}

	[TestMethod]
	public void IsImPlotContextCreated_ReturnsFalseBeforeContextCreation()
	{
		Assert.IsFalse(ImGuiExtensionManager.IsImPlotContextCreated);
	}

	[TestMethod]
	public void BeginFrame_DoesNotThrow_WhenNoExtensionsLoaded()
	{
		// BeginFrame should gracefully handle the case where no extensions are available
		ImGuiExtensionManager.BeginFrame();
	}

	[TestMethod]
	public void Cleanup_DoesNotThrow_WhenNotInitialized()
	{
		// Cleanup should be safe to call even if no contexts were created
		ImGuiExtensionManager.Cleanup();
	}

	[TestMethod]
	public void Cleanup_DoesNotThrow_AfterInitialize()
	{
		ImGuiExtensionManager.Initialize();
		ImGuiExtensionManager.Cleanup();
	}
}
