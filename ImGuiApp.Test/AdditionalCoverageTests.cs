// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGuiApp.Test;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Numerics;
using Silk.NET.Windowing;
using ktsu.StrongPaths;
using ktsu.Extensions;
using System.Reflection;
using ktsu.ImGuiApp.ImGuiController;

[TestClass]
public class AdditionalCoverageTests
{
	[TestInitialize]
	public void Setup()
	{
		ImGuiApp.Reset();
	}

	[TestMethod]
	public void ImGuiAppTextureInfo_DefaultConstructor_InitializesCorrectly()
	{
		ImGuiAppTextureInfo textureInfo = new();

		Assert.AreEqual(new AbsoluteFilePath(), textureInfo.Path);
		Assert.AreEqual(0u, textureInfo.TextureId);
		Assert.AreEqual(0, textureInfo.Width);
		Assert.AreEqual(0, textureInfo.Height);
	}

	[TestMethod]
	public void ImGuiAppTextureInfo_Properties_CanBeSet()
	{
		ImGuiAppTextureInfo textureInfo = new();
		AbsoluteFilePath testPath = "/test/path.png".As<AbsoluteFilePath>();

		textureInfo.Path = testPath;
		textureInfo.TextureId = 123u;
		textureInfo.Width = 256;
		textureInfo.Height = 512;

		Assert.AreEqual(testPath, textureInfo.Path);
		Assert.AreEqual(123u, textureInfo.TextureId);
		Assert.AreEqual(256, textureInfo.Width);
		Assert.AreEqual(512, textureInfo.Height);
	}

	[TestMethod]
	public void ImGuiAppWindowState_DefaultConstructor_HasCorrectDefaults()
	{
		ImGuiAppWindowState windowState = new();

		Assert.AreEqual(new Vector2(1280, 720), windowState.Size);
		Assert.AreEqual(new Vector2(-short.MinValue, -short.MinValue), windowState.Pos);
		Assert.AreEqual(WindowState.Normal, windowState.LayoutState);
	}

	[TestMethod]
	public void ImGuiAppWindowState_Properties_CanBeSet()
	{
		ImGuiAppWindowState windowState = new();
		Vector2 testSize = new(1920, 1080);
		Vector2 testPos = new(100, 200);

		windowState.Size = testSize;
		windowState.Pos = testPos;
		windowState.LayoutState = WindowState.Maximized;

		Assert.AreEqual(testSize, windowState.Size);
		Assert.AreEqual(testPos, windowState.Pos);
		Assert.AreEqual(WindowState.Maximized, windowState.LayoutState);
	}

	[TestMethod]
	public void ImGuiAppConfig_DefaultConstructor_InitializesAllProperties()
	{
		ImGuiAppConfig config = new();

		Assert.IsNotNull(config.Title);
		Assert.IsNotNull(config.IconPath);
		Assert.IsNotNull(config.InitialWindowState);
		Assert.IsNotNull(config.OnStart);
		Assert.IsNotNull(config.OnUpdate);
		Assert.IsNotNull(config.OnRender);
		Assert.IsNotNull(config.OnAppMenu);
		Assert.IsNotNull(config.OnMoveOrResize);
		Assert.IsNotNull(config.Fonts);
		Assert.IsNotNull(config.PerformanceSettings);
	}

	[TestMethod]
	public void ImGuiAppConfig_InitializerSyntax_WorksCorrectly()
	{
		ImGuiAppConfig config = new()
		{
			Title = "Test App",
			IconPath = "test.ico",
			TestMode = true,
			EnableUnicodeSupport = false,
			SaveIniSettings = false
		};

		Assert.AreEqual("Test App", config.Title);
		Assert.AreEqual("test.ico", config.IconPath);
		Assert.IsTrue(config.TestMode);
		Assert.IsFalse(config.EnableUnicodeSupport);
		Assert.IsFalse(config.SaveIniSettings);
	}

	[TestMethod]
	public void ImGuiAppConfig_Fonts_CanBeModified()
	{
		ImGuiAppConfig config = new();
		byte[] testFontData = [0x01, 0x02, 0x03, 0x04];

		config.Fonts.Add("TestFont", testFontData);

		Assert.AreEqual(1, config.Fonts.Count);
		Assert.IsTrue(config.Fonts.ContainsKey("TestFont"));
		Assert.AreSame(testFontData, config.Fonts["TestFont"]);
	}

	[TestMethod]
	public void ImGuiAppConfig_EmojiFont_ReturnsValue()
	{
		// This tests the static EmojiFont property
		byte[]? emojiFont = ImGuiAppConfig.EmojiFont;

		// The font may or may not be available depending on the build configuration
		// We just test that the property can be accessed without throwing
		Assert.IsTrue(emojiFont is null || emojiFont.Length > 0);
	}

	[TestMethod]
	public void FontAppearance_Constants_HaveExpectedValues()
	{
		// Test internal constants via reflection
		FieldInfo? defaultFontName = typeof(FontAppearance)
			.GetField("DefaultFontName", BindingFlags.NonPublic | BindingFlags.Static);
		FieldInfo? defaultFontPointSize = typeof(FontAppearance)
			.GetField("DefaultFontPointSize", BindingFlags.NonPublic | BindingFlags.Static);

		Assert.IsNotNull(defaultFontName);
		Assert.IsNotNull(defaultFontPointSize);
		Assert.AreEqual("default", defaultFontName.GetValue(null));
		Assert.AreEqual(14, defaultFontPointSize.GetValue(null));
	}

	[TestMethod]
	public void GdiPlusHelper_IsStaticClass()
	{
		Type gdiPlusHelperType = typeof(GdiPlusHelper);

		// Verify it's a static class (abstract and sealed)
		Assert.IsTrue(gdiPlusHelperType.IsAbstract && gdiPlusHelperType.IsSealed);
	}

	[TestMethod]
	public void NativeMethods_IsStaticClass()
	{
		Type nativeMethodsType = typeof(NativeMethods);

		// Verify it's a static class (abstract and sealed)
		Assert.IsTrue(nativeMethodsType.IsAbstract && nativeMethodsType.IsSealed);
	}

	[TestMethod]
	public void UIScaler_InheritsScopedAction()
	{
		// Verify UIScaler inherits from ScopedAction
		Assert.IsTrue(typeof(UIScaler).IsSubclassOf(typeof(ktsu.ScopedAction.ScopedAction)));
	}

	[TestMethod]
	public void FontAppearance_InheritsScopedAction()
	{
		// Verify FontAppearance inherits from ScopedAction
		Assert.IsTrue(typeof(FontAppearance).IsSubclassOf(typeof(ktsu.ScopedAction.ScopedAction)));
	}

	[TestMethod]
	public void ImGuiAppPerformanceSettings_DefaultValues_AreCorrect()
	{
		ImGuiAppPerformanceSettings settings = new();

		Assert.IsTrue(settings.EnableThrottledRendering);
		Assert.AreEqual(30.0, settings.FocusedFps);
		Assert.AreEqual(30.0, settings.FocusedUps);
		Assert.AreEqual(5.0, settings.UnfocusedFps);
		Assert.AreEqual(5.0, settings.UnfocusedUps);
		Assert.AreEqual(10.0, settings.IdleFps);
		Assert.AreEqual(10.0, settings.IdleUps);
		Assert.IsTrue(settings.EnableIdleDetection);
		Assert.AreEqual(30.0, settings.IdleTimeoutSeconds);
		Assert.IsTrue(settings.DisableVSyncWhenThrottling);
	}

	[TestMethod]
	public void ImGuiAppPerformanceSettings_Properties_CanBeSet()
	{
		ImGuiAppPerformanceSettings settings = new()
		{
			EnableThrottledRendering = false,
			FocusedFps = 60.0,
			UnfocusedFps = 15.0,
			IdleFps = 5.0,
			EnableIdleDetection = false,
			IdleTimeoutSeconds = 60.0,
			DisableVSyncWhenThrottling = false
		};

		Assert.IsFalse(settings.EnableThrottledRendering);
		Assert.AreEqual(60.0, settings.FocusedFps);
		Assert.AreEqual(15.0, settings.UnfocusedFps);
		Assert.AreEqual(5.0, settings.IdleFps);
		Assert.IsFalse(settings.EnableIdleDetection);
		Assert.AreEqual(60.0, settings.IdleTimeoutSeconds);
		Assert.IsFalse(settings.DisableVSyncWhenThrottling);
	}

	[TestMethod]
	public void ImGuiApp_OnUserInput_DoesNotThrow()
	{
		// This method should be safe to call
		ImGuiApp.OnUserInput();
		Assert.IsTrue(true); // If we get here, it didn't throw
	}

	[TestMethod]
	public void ImGuiApp_IsIdleProperty_HasDefaultValue()
	{
		// After reset, IsIdle should be false
		Assert.IsFalse(ImGuiApp.IsIdle);
	}

	[TestMethod]
	public void ImGuiApp_IsFocusedProperty_HasDefaultValue()
	{
		// After reset, IsFocused should be true
		Assert.IsTrue(ImGuiApp.IsFocused);
	}

	[TestMethod]
	public void Util_Clamp_WorksCorrectly()
	{
		// Test clamping at minimum
		Assert.AreEqual(5.0f, Util.Clamp(3.0f, 5.0f, 10.0f));

		// Test clamping at maximum
		Assert.AreEqual(10.0f, Util.Clamp(15.0f, 5.0f, 10.0f));

		// Test value in range
		Assert.AreEqual(7.0f, Util.Clamp(7.0f, 5.0f, 10.0f));

		// Test edge cases
		Assert.AreEqual(5.0f, Util.Clamp(5.0f, 5.0f, 10.0f));
		Assert.AreEqual(10.0f, Util.Clamp(10.0f, 5.0f, 10.0f));
	}

	[TestMethod]
	public void ImGuiApp_WindowState_ReturnsCorrectState()
	{
		// Test that WindowState property can be accessed
		ImGuiAppWindowState state = ImGuiApp.WindowState;
		Assert.IsNotNull(state);
		Assert.IsInstanceOfType<ImGuiAppWindowState>(state);
	}
}