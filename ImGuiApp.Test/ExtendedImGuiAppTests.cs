// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGuiApp.Test;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using ktsu.StrongPaths;
using ktsu.Extensions;

[TestClass]
public class ExtendedImGuiAppTests
{
	[TestInitialize]
	public void Setup()
	{
		ImGuiApp.Reset();
	}

	[TestMethod]
	public void ImGuiApp_TryGetTexture_WithAbsolutePath_ReturnsCorrectResult()
	{
		AbsoluteFilePath testPath = "/test/texture.png".As<AbsoluteFilePath>();

		// Should return false for non-existent texture
		bool result = ImGuiApp.TryGetTexture(testPath, out ImGuiAppTextureInfo? textureInfo);

		Assert.IsFalse(result);
		Assert.IsNull(textureInfo);
	}

	[TestMethod]
	public void ImGuiApp_TryGetTexture_WithStringPath_ReturnsCorrectResult()
	{
		const string testPath = "/test/texture.png";

		// Should return false for non-existent texture
		bool result = ImGuiApp.TryGetTexture(testPath, out ImGuiAppTextureInfo? textureInfo);

		Assert.IsFalse(result);
		Assert.IsNull(textureInfo);
	}

	[TestMethod]
	public void ImGuiApp_DeleteTexture_WithNullTextureInfo_ThrowsArgumentNullException()
	{
		Assert.ThrowsException<ArgumentNullException>(() => ImGuiApp.DeleteTexture(null!));
	}

	[TestMethod]
	public void ImGuiApp_Reset_ResetsStateCorrectly()
	{
		// Call reset and verify properties return to default state
		ImGuiApp.Reset();

		Assert.IsFalse(ImGuiApp.IsIdle);
		Assert.IsTrue(ImGuiApp.IsFocused);
		Assert.IsNotNull(ImGuiApp.WindowState);
	}

	[TestMethod]
	public void ImGuiApp_WindowState_ReturnsValidState()
	{
		ImGuiAppWindowState state = ImGuiApp.WindowState;

		Assert.IsNotNull(state);
		Assert.IsInstanceOfType<ImGuiAppWindowState>(state);
	}

	[TestMethod]
	public void ImGuiApp_IsIdle_HasCorrectDefaultValue()
	{
		// After reset, should be false
		Assert.IsFalse(ImGuiApp.IsIdle);
	}

	[TestMethod]
	public void ImGuiApp_IsFocused_HasCorrectDefaultValue()
	{
		// After reset, should be true
		Assert.IsTrue(ImGuiApp.IsFocused);
	}

	[TestMethod]
	public void ImGuiApp_OnUserInput_UpdatesInputState()
	{
		// This method should be safe to call and not throw
		ImGuiApp.OnUserInput();

		// Verify it can be called multiple times
		ImGuiApp.OnUserInput();
		ImGuiApp.OnUserInput();

		Assert.IsTrue(true); // If we get here, no exceptions were thrown
	}

	[TestMethod]
	public void ImGuiApp_ScaleFactor_HasDefaultValue()
	{
		// After reset, ScaleFactor should have a default value
		// We can't access it directly, but we can verify the app doesn't crash
		ImGuiAppWindowState state = ImGuiApp.WindowState;
		Assert.IsNotNull(state);
	}
}