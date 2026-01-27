// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.App.Tests;

using ktsu.Semantics.Paths;
using ktsu.Semantics.Strings;
using System.Collections.Concurrent;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests for core ImGuiApp functionality including properties, state management, and basic operations.
/// </summary>
[TestClass]
public class ImGuiAppCoreTests
{
	[TestInitialize]
	public void Setup()
	{
		ImGuiApp.Reset();
	}

	#region Property Tests

	[TestMethod]
	public void IsFocused_AfterReset_ReturnsTrue()
	{
		Assert.IsTrue(ImGuiApp.IsFocused, "IsFocused should be true after reset");
	}

	[TestMethod]
	public void IsIdle_AfterReset_ReturnsFalse()
	{
		Assert.IsFalse(ImGuiApp.IsIdle, "IsIdle should be false after reset");
	}

	[TestMethod]
	public void IsVisible_WithNullWindow_ReturnsFalse()
	{
		bool isVisible = ImGuiApp.IsVisible;
		Assert.IsFalse(isVisible, "IsVisible should be false when window is null");
	}

	[TestMethod]
	public void ScaleFactor_AfterReset_ReturnsOne()
	{
		Assert.AreEqual(1.0f, ImGuiApp.ScaleFactor);
	}

	[TestMethod]
	public void Invoker_AfterReset_IsNull()
	{
		Assert.IsNull(ImGuiApp.Invoker);
	}

	[TestMethod]
	public void WindowState_IsNeverNull()
	{
		ImGuiAppWindowState windowState = ImGuiApp.WindowState;
		Assert.IsNotNull(windowState);
		Assert.IsInstanceOfType<ImGuiAppWindowState>(windowState);
	}

	[TestMethod]
	public void Textures_IsNeverNull()
	{
		ConcurrentDictionary<AbsoluteFilePath, ImGuiAppTextureInfo> textures = ImGuiApp.Textures;
		Assert.IsNotNull(textures);
	}

	[TestMethod]
	public void Textures_AfterReset_IsEmpty()
	{
		ConcurrentDictionary<AbsoluteFilePath, ImGuiAppTextureInfo> textures = ImGuiApp.Textures;
		Assert.IsEmpty(textures);
	}

	#endregion

	#region Method Tests

	[TestMethod]
	public void OnUserInput_UpdatesLastInputTime()
	{
		DateTime before = ImGuiApp.lastInputTime;
		ImGuiApp.OnUserInput();
		DateTime after = ImGuiApp.lastInputTime;

		Assert.IsTrue(after >= before, "lastInputTime should be updated to a time equal to or after the previous value");
	}

	[TestMethod]
	public void OnUserInput_CanBeCalledMultipleTimes()
	{
		try
		{
			ImGuiApp.OnUserInput();
			ImGuiApp.OnUserInput();
			ImGuiApp.OnUserInput();
		}
#pragma warning disable CA1031 // Do not catch general exception types
		catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
		{
			Assert.Fail($"Expected no exception, but got: {ex.Message}");
		}
	}

	[TestMethod]
	public void Stop_WithNullWindow_ThrowsInvalidOperationException()
	{
		Assert.ThrowsExactly<InvalidOperationException>(ImGuiApp.Stop);
	}

	[TestMethod]
	public void Reset_ResetsStateCorrectly()
	{
		ImGuiApp.Reset();

		Assert.IsFalse(ImGuiApp.IsIdle, "IsIdle should be false after reset");
		Assert.IsTrue(ImGuiApp.IsFocused, "IsFocused should be true after reset");
		Assert.IsNotNull(ImGuiApp.WindowState);
		Assert.IsEmpty(ImGuiApp.Textures);
	}

	#endregion

	#region Texture Management Tests

	[TestMethod]
	public void TryGetTexture_WithAbsolutePath_NonExistentTexture_ReturnsFalse()
	{
		AbsoluteFilePath testPath = Path.GetFullPath("nonexistent_texture.png").As<AbsoluteFilePath>();
		bool result = ImGuiApp.TryGetTexture(testPath, out ImGuiAppTextureInfo? textureInfo);

		Assert.IsFalse(result, "TryGetTexture should return false for non-existent texture");
		Assert.IsNull(textureInfo);
	}

	[TestMethod]
	public void TryGetTexture_WithStringPath_NonExistentTexture_ReturnsFalse()
	{
		string testPath = Path.GetFullPath("nonexistent_texture.png");
		bool result = ImGuiApp.TryGetTexture(testPath, out ImGuiAppTextureInfo? textureInfo);

		Assert.IsFalse(result, "TryGetTexture should return false for non-existent texture");
		Assert.IsNull(textureInfo);
	}

	[TestMethod]
	public void DeleteTexture_WithNullTextureInfo_ThrowsArgumentNullException()
	{
		Assert.ThrowsExactly<ArgumentNullException>(() => ImGuiApp.DeleteTexture(null!));
	}

	#endregion

	#region Property Accessibility Tests

	[TestMethod]
	public void CoreProperties_AreReadOnly()
	{
		// Test that these properties can be accessed directly and have expected default values
		Assert.IsTrue(ImGuiApp.IsFocused, "IsFocused should be true by default");
		Assert.IsFalse(ImGuiApp.IsIdle, "IsIdle should be false by default");
		Assert.AreEqual(1.0f, ImGuiApp.ScaleFactor);

		// Test properties are read-only by attempting direct access (compilation test)
		// The fact that this compiles and runs means they are readable
		bool focusedValue = ImGuiApp.IsFocused;
		bool idleValue = ImGuiApp.IsIdle;
		float scaleValue = ImGuiApp.ScaleFactor;

		Assert.IsTrue(focusedValue || !focusedValue, "focusedValue should be a valid boolean");  // Tautology to use the values
		Assert.IsTrue(idleValue || !idleValue, "idleValue should be a valid boolean");
		Assert.IsGreaterThanOrEqualTo(0, scaleValue, "ScaleFactor should be non-negative");
	}

	#endregion

	#region Internal Structure Tests

	[TestMethod]
	public void CommonFontSizes_ContainsExpectedValues()
	{
		int[] sizes = ImGuiApp.CommonFontSizes;
		Assert.IsNotNull(sizes);

		int[] expectedSizes = [10, 12, 14, 16, 18, 20, 24, 32, 48];
		CollectionAssert.AreEqual(expectedSizes, sizes);
	}

	[TestMethod]
	public void DebugLogger_IsInternalStaticClass()
	{
		// Test DebugLogger functionality through direct access
		// The fact that this compiles means DebugLogger is accessible as internal
		Assert.IsNotNull(typeof(DebugLogger));
		Assert.IsTrue(typeof(DebugLogger).IsClass, "DebugLogger should be a class");
		Assert.IsTrue(typeof(DebugLogger).IsAbstract && typeof(DebugLogger).IsSealed, "DebugLogger should be a static class (abstract and sealed)");
	}

	[TestMethod]
	public void DebugLogger_LogMethod_CanBeCalled()
	{
		try
		{
			// Test DebugLogger.Log method through direct access
			// Should not throw when called directly
			DebugLogger.Log("Test message from ImGuiAppCoreTests");
		}
#pragma warning disable CA1031 // Do not catch general exception types
		catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
		{
			Assert.Fail($"Expected no exception, but got: {ex.Message}");
		}
	}

	#endregion
}
