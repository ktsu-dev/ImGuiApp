// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGuiApp.Test;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Concurrent;
using System.Reflection;
using ktsu.StrongPaths;
using ktsu.Extensions;

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
		Assert.IsTrue(ImGuiApp.IsFocused);
	}

	[TestMethod]
	public void IsIdle_AfterReset_ReturnsFalse()
	{
		Assert.IsFalse(ImGuiApp.IsIdle);
	}

	[TestMethod]
	public void IsVisible_WithNullWindow_ReturnsFalse()
	{
		bool isVisible = ImGuiApp.IsVisible;
		Assert.IsFalse(isVisible);
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
		Assert.AreEqual(0, textures.Count);
	}

	#endregion

	#region Method Tests

	[TestMethod]
	public void OnUserInput_UpdatesLastInputTime()
	{
		FieldInfo? field = typeof(ImGuiApp).GetField("lastInputTime", BindingFlags.NonPublic | BindingFlags.Static);
		Assert.IsNotNull(field);

		DateTime before = (DateTime)field.GetValue(null)!;
		Thread.Sleep(1);
		ImGuiApp.OnUserInput();
		DateTime after = (DateTime)field.GetValue(null)!;

		Assert.IsTrue(after > before);
	}

	[TestMethod]
	public void OnUserInput_CanBeCalledMultipleTimes()
	{
		ImGuiApp.OnUserInput();
		ImGuiApp.OnUserInput();
		ImGuiApp.OnUserInput();
		Assert.IsTrue(true); // If we get here, no exceptions were thrown
	}

	[TestMethod]
	public void Stop_WithNullWindow_ThrowsInvalidOperationException()
	{
		Assert.ThrowsException<InvalidOperationException>(ImGuiApp.Stop);
	}

	[TestMethod]
	public void Reset_ResetsStateCorrectly()
	{
		ImGuiApp.Reset();

		Assert.IsFalse(ImGuiApp.IsIdle);
		Assert.IsTrue(ImGuiApp.IsFocused);
		Assert.IsNotNull(ImGuiApp.WindowState);
		Assert.AreEqual(0, ImGuiApp.Textures.Count);
	}

	#endregion

	#region Texture Management Tests

	[TestMethod]
	public void TryGetTexture_WithAbsolutePath_NonExistentTexture_ReturnsFalse()
	{
		AbsoluteFilePath testPath = "/test/texture.png".As<AbsoluteFilePath>();
		bool result = ImGuiApp.TryGetTexture(testPath, out ImGuiAppTextureInfo? textureInfo);

		Assert.IsFalse(result);
		Assert.IsNull(textureInfo);
	}

	[TestMethod]
	public void TryGetTexture_WithStringPath_NonExistentTexture_ReturnsFalse()
	{
		const string testPath = "/test/texture.png";
		bool result = ImGuiApp.TryGetTexture(testPath, out ImGuiAppTextureInfo? textureInfo);

		Assert.IsFalse(result);
		Assert.IsNull(textureInfo);
	}

	[TestMethod]
	public void DeleteTexture_WithNullTextureInfo_ThrowsArgumentNullException()
	{
		Assert.ThrowsException<ArgumentNullException>(() => ImGuiApp.DeleteTexture(null!));
	}

	#endregion

	#region Property Accessibility Tests

	[TestMethod]
	public void CoreProperties_AreReadOnly()
	{
		PropertyInfo? isFocused = typeof(ImGuiApp).GetProperty("IsFocused");
		PropertyInfo? isIdle = typeof(ImGuiApp).GetProperty("IsIdle");
		PropertyInfo? scaleFactor = typeof(ImGuiApp).GetProperty("ScaleFactor");

		Assert.IsNotNull(isFocused);
		Assert.IsNotNull(isIdle);
		Assert.IsNotNull(scaleFactor);

		Assert.IsTrue(isFocused.CanRead);
		Assert.IsTrue(isIdle.CanRead);
		Assert.IsTrue(scaleFactor.CanRead);

		// These should not have public setters
		Assert.IsFalse(isFocused.SetMethod?.IsPublic ?? false);
		Assert.IsFalse(isIdle.SetMethod?.IsPublic ?? false);
		Assert.IsFalse(scaleFactor.SetMethod?.IsPublic ?? false);
	}

	#endregion

	#region Internal Structure Tests

	[TestMethod]
	public void CommonFontSizes_ContainsExpectedValues()
	{
		FieldInfo? field = typeof(ImGuiApp).GetField("CommonFontSizes", BindingFlags.NonPublic | BindingFlags.Static);
		Assert.IsNotNull(field);

		int[]? sizes = field.GetValue(null) as int[];
		Assert.IsNotNull(sizes);

		int[] expectedSizes = [10, 12, 14, 16, 18, 20, 24, 32, 48];
		CollectionAssert.AreEqual(expectedSizes, sizes);
	}

	[TestMethod]
	public void DebugLogger_IsInternalStaticClass()
	{
		Type? debugLoggerType = typeof(ImGuiApp).GetNestedType("DebugLogger", BindingFlags.NonPublic);
		Assert.IsNotNull(debugLoggerType);
		Assert.IsTrue(debugLoggerType.IsClass);
		Assert.IsTrue(debugLoggerType.IsAbstract && debugLoggerType.IsSealed);
	}

	[TestMethod]
	public void DebugLogger_LogMethod_CanBeCalled()
	{
		Type? debugLoggerType = typeof(ImGuiApp).GetNestedType("DebugLogger", BindingFlags.NonPublic);
		Assert.IsNotNull(debugLoggerType);

		MethodInfo? logMethod = debugLoggerType.GetMethod("Log", BindingFlags.Public | BindingFlags.Static);
		Assert.IsNotNull(logMethod);

		ParameterInfo[] methodParams = logMethod.GetParameters();
		Assert.AreEqual(1, methodParams.Length);
		Assert.AreEqual(typeof(string), methodParams[0].ParameterType);

		// Should not throw when called
		object[] invokeParams = ["Test message from ImGuiAppCoreTests"];
		logMethod.Invoke(null, invokeParams);
		Assert.IsTrue(true);
	}

	#endregion
}