// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGuiApp.Test;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;

[TestClass]
public class FinalCoverageTests
{
	[TestInitialize]
	public void Setup()
	{
		ImGuiApp.Reset();
	}

	[TestMethod]
	public void ImGuiApp_IsVisible_ReturnsFalse_WhenWindowIsNull()
	{
		bool isVisible = ImGuiApp.IsVisible;
		Assert.IsFalse(isVisible);
	}

	[TestMethod]
	public void ImGuiApp_ScaleFactor_DefaultValue_IsOne()
	{
		float scaleFactor = ImGuiApp.ScaleFactor;
		Assert.AreEqual(1.0f, scaleFactor);
	}

	[TestMethod]
	public void ImGuiApp_Invoker_IsNull_AfterReset()
	{
		// After reset, Invoker should be null
		Assert.IsNull(ImGuiApp.Invoker);
	}

	[TestMethod]
	public void ImGuiApp_CommonFontSizes_ContainsExpectedValues()
	{
		// Use reflection to access the private CommonFontSizes field
		FieldInfo? field = typeof(ImGuiApp).GetField("CommonFontSizes", BindingFlags.NonPublic | BindingFlags.Static);
		Assert.IsNotNull(field);

		int[]? sizes = field.GetValue(null) as int[];
		Assert.IsNotNull(sizes);
		Assert.IsTrue(sizes.Contains(14)); // Should contain default font size
		Assert.IsTrue(sizes.Contains(16)); // Should contain common size
		Assert.IsTrue(sizes.Length > 5); // Should have multiple sizes
	}

	[TestMethod]
	public void ImGuiApp_DebugLogger_LogMethod_CanBeCalled()
	{
		// Access the DebugLogger nested class and its Log method
		Type? debugLoggerType = typeof(ImGuiApp).GetNestedType("DebugLogger", BindingFlags.NonPublic);
		Assert.IsNotNull(debugLoggerType);

		MethodInfo? logMethod = debugLoggerType.GetMethod("Log", BindingFlags.Public | BindingFlags.Static);
		Assert.IsNotNull(logMethod);

		// Call the Log method - it should not throw
		logMethod.Invoke(null, ["Test message from unit test"]);
		Assert.IsTrue(true); // Test passes if no exception is thrown
	}

	[TestMethod]
	public void ImGuiApp_LastInputTime_UpdatedByOnUserInput()
	{
		// Access the private lastInputTime field
		FieldInfo? field = typeof(ImGuiApp).GetField("lastInputTime", BindingFlags.NonPublic | BindingFlags.Static);
		Assert.IsNotNull(field);

		DateTime before = (DateTime)field.GetValue(null)!;
		Thread.Sleep(1); // Ensure time difference
		ImGuiApp.OnUserInput();
		DateTime after = (DateTime)field.GetValue(null)!;

		Assert.IsTrue(after > before);
	}

	[TestMethod]
	public void ImGuiApp_Properties_HaveCorrectAccessibility()
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

	[TestMethod]
	public void ImGuiApp_TexturesProperty_NeverNull()
	{
		System.Collections.Concurrent.ConcurrentDictionary<ktsu.StrongPaths.AbsoluteFilePath, ImGuiAppTextureInfo> textures = ImGuiApp.Textures;
		Assert.IsNotNull(textures);

		// After reset, should be empty
		Assert.AreEqual(0, textures.Count);
	}

	[TestMethod]
	public void ImGuiApp_WindowStateProperty_NeverNull()
	{
		ImGuiAppWindowState windowState = ImGuiApp.WindowState;
		Assert.IsNotNull(windowState);
		Assert.IsInstanceOfType<ImGuiAppWindowState>(windowState);
	}

	[TestMethod]
	public void FontHelper_CleanupMethods_DoNotThrow()
	{
		// These should be safe to call multiple times
		FontHelper.CleanupCustomFonts();
		FontHelper.CleanupGlyphRanges();
		FontHelper.CleanupCustomFonts();
		FontHelper.CleanupGlyphRanges();
		Assert.IsTrue(true);
	}
}