// Copyright (c) 2023-2026 ktsu-dev contributors

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

	[TestMethod]
	public void Initialize_None_MakesNoExtensionAvailable()
	{
		ImGuiExtensionManager.Initialize(ImGuiAppExtensions.None);
		try
		{
			Assert.AreEqual(ImGuiAppExtensions.None, ImGuiExtensionManager.Enabled);
			Assert.IsFalse(ImGuiExtensionManager.IsImGuizmoAvailable);
			Assert.IsFalse(ImGuiExtensionManager.IsImNodesAvailable);
			Assert.IsFalse(ImGuiExtensionManager.IsImPlotAvailable);
		}
		finally
		{
			ImGuiExtensionManager.Initialize();
		}
	}

	[TestMethod]
	public void Initialize_Subset_MakesOnlyThatExtensionAvailable()
	{
		ImGuiExtensionManager.Initialize(ImGuiAppExtensions.ImPlot);
		try
		{
			Assert.AreEqual(ImGuiAppExtensions.ImPlot, ImGuiExtensionManager.Enabled);
			Assert.IsTrue(ImGuiExtensionManager.IsImPlotAvailable, "ImGui.App references ImPlot, so asking for it must find it.");
			Assert.IsFalse(ImGuiExtensionManager.IsImGuizmoAvailable);
			Assert.IsFalse(ImGuiExtensionManager.IsImNodesAvailable);
		}
		finally
		{
			ImGuiExtensionManager.Initialize();
		}
	}

	[TestMethod]
	public void Initialize_All_AfterNone_FindsEveryReferencedExtension()
	{
		// Detection is cached per extension, so turning one back on must still find it.
		ImGuiExtensionManager.Initialize(ImGuiAppExtensions.None);
		ImGuiExtensionManager.Initialize(ImGuiAppExtensions.All);

		Assert.AreEqual(ImGuiAppExtensions.All, ImGuiExtensionManager.Enabled);
		Assert.IsTrue(ImGuiExtensionManager.IsImGuizmoAvailable);
		Assert.IsTrue(ImGuiExtensionManager.IsImNodesAvailable);
		Assert.IsTrue(ImGuiExtensionManager.IsImPlotAvailable);
	}

	[TestMethod]
	public void CreateExtensionContexts_CreatesOnlyTheContextsAskedFor()
	{
		Hexa.NET.ImGui.ImGuiContextPtr context = Hexa.NET.ImGui.ImGui.CreateContext();
		try
		{
			ImGuiExtensionManager.Initialize(ImGuiAppExtensions.ImPlot);
			ImGuiExtensionManager.SetImGuiContext();
			ImGuiExtensionManager.CreateExtensionContexts();

			Assert.IsTrue(ImGuiExtensionManager.IsImPlotContextCreated);
			Assert.IsFalse(ImGuiExtensionManager.IsImNodesContextCreated);
		}
		finally
		{
			ImGuiExtensionManager.Cleanup();
			ImGuiExtensionManager.Initialize();
			Hexa.NET.ImGui.ImGui.DestroyContext(context);
		}

		Assert.IsFalse(ImGuiExtensionManager.IsImPlotContextCreated);
	}

	[TestMethod]
	public void CreateExtensionContexts_None_CreatesNoContext()
	{
		Hexa.NET.ImGui.ImGuiContextPtr context = Hexa.NET.ImGui.ImGui.CreateContext();
		try
		{
			ImGuiExtensionManager.Initialize(ImGuiAppExtensions.None);
			ImGuiExtensionManager.SetImGuiContext();
			ImGuiExtensionManager.CreateExtensionContexts();

			Assert.IsFalse(ImGuiExtensionManager.IsImPlotContextCreated);
			Assert.IsFalse(ImGuiExtensionManager.IsImNodesContextCreated);
		}
		finally
		{
			ImGuiExtensionManager.Cleanup();
			ImGuiExtensionManager.Initialize();
			Hexa.NET.ImGui.ImGui.DestroyContext(context);
		}
	}

	[TestMethod]
	public void Config_Extensions_DefaultsToAll()
	{
		ImGuiAppConfig config = new();

		Assert.AreEqual(ImGuiAppExtensions.All, config.Extensions);
		Assert.AreEqual(ImGuiAppExtensions.All, config.EffectiveExtensions);
	}

	[TestMethod]
	public void Config_EffectiveExtensions_FollowsExtensions()
	{
		ImGuiAppConfig config = new() { Extensions = ImGuiAppExtensions.ImNodes | ImGuiAppExtensions.ImPlot };

		Assert.AreEqual(ImGuiAppExtensions.ImNodes | ImGuiAppExtensions.ImPlot, config.EffectiveExtensions);
	}

	[TestMethod]
	public void Config_AutoDiscoverExtensionsOff_OverridesExtensions()
	{
		ImGuiAppConfig config = new() { AutoDiscoverExtensions = false, Extensions = ImGuiAppExtensions.All };

		Assert.AreEqual(ImGuiAppExtensions.None, config.EffectiveExtensions);
	}
}
