// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.App.Tests;

using ktsu.ImGui.App;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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
		// Without CreateExtensionContexts(), no contexts should be created
		Assert.IsFalse(ImGuiExtensionManager.IsImNodesContextCreated);
	}

	[TestMethod]
	public void Initialize_CalledTwice_DoesNotThrow()
	{
		ImGuiExtensionManager.Initialize();
		bool firstAvailability = ImGuiExtensionManager.IsImGuizmoAvailable;
		ImGuiExtensionManager.Initialize();
		// Second call should be idempotent - availability should not change
		Assert.AreEqual(firstAvailability, ImGuiExtensionManager.IsImGuizmoAvailable);
	}

	[TestMethod]
	public void IsImGuizmoAvailable_ReturnsBoolean()
	{
		ImGuiExtensionManager.Initialize();
		bool firstRead = ImGuiExtensionManager.IsImGuizmoAvailable;
		bool secondRead = ImGuiExtensionManager.IsImGuizmoAvailable;
		// Property should return a stable value on repeated reads
		Assert.AreEqual(firstRead, secondRead);
	}

	[TestMethod]
	public void IsImNodesAvailable_ReturnsBoolean()
	{
		ImGuiExtensionManager.Initialize();
		bool firstRead = ImGuiExtensionManager.IsImNodesAvailable;
		bool secondRead = ImGuiExtensionManager.IsImNodesAvailable;
		// Property should return a stable value on repeated reads
		Assert.AreEqual(firstRead, secondRead);
	}

	[TestMethod]
	public void IsImPlotAvailable_ReturnsBoolean()
	{
		ImGuiExtensionManager.Initialize();
		bool firstRead = ImGuiExtensionManager.IsImPlotAvailable;
		bool secondRead = ImGuiExtensionManager.IsImPlotAvailable;
		// Property should return a stable value on repeated reads
		Assert.AreEqual(firstRead, secondRead);
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
		// No contexts should have been created by a standalone BeginFrame call
		Assert.IsFalse(ImGuiExtensionManager.IsImNodesContextCreated);
	}

	[TestMethod]
	public void Cleanup_DoesNotThrow_WhenNotInitialized()
	{
		// Cleanup should be safe to call even if no contexts were created
		ImGuiExtensionManager.Cleanup();
		// After cleanup, context-created flags should remain false
		Assert.IsFalse(ImGuiExtensionManager.IsImNodesContextCreated);
	}

	[TestMethod]
	public void Cleanup_DoesNotThrow_AfterInitialize()
	{
		ImGuiExtensionManager.Initialize();
		ImGuiExtensionManager.Cleanup();
		// After cleanup, context-created flags should be false
		Assert.IsFalse(ImGuiExtensionManager.IsImNodesContextCreated);
	}
}
