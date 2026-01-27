// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.App.Tests;

using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Silk.NET.Windowing;
using ktsu.Semantics.Paths;
using ktsu.Semantics.Strings;

/// <summary>
/// Tests for ImGuiApp data structures including texture info, window state, configuration, and performance settings.
/// </summary>
[TestClass]
public class ImGuiAppDataStructureTests
{
	[TestInitialize]
	public void Setup()
	{
		ImGuiApp.Reset();
	}

	#region ImGuiAppTextureInfo Tests

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
		AbsoluteFilePath testPath = Path.GetFullPath("test_texture.png").As<AbsoluteFilePath>();

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
	public void ImGuiAppTextureInfo_IsPublicClass()
	{
		Type textureInfoType = typeof(ImGuiAppTextureInfo);
		Assert.IsTrue(textureInfoType.IsClass, "ImGuiAppTextureInfo should be a class type");
		Assert.IsTrue(textureInfoType.IsPublic, "ImGuiAppTextureInfo should be publicly accessible");
	}

	#endregion

	#region ImGuiAppWindowState Tests

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
	public void ImGuiAppWindowState_IsPublicClass()
	{
		Type windowStateType = typeof(ImGuiAppWindowState);
		Assert.IsTrue(windowStateType.IsClass, "ImGuiAppWindowState should be a class type");
		Assert.IsTrue(windowStateType.IsPublic, "ImGuiAppWindowState should be publicly accessible");
	}

	#endregion

	#region ImGuiAppConfig Tests

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
		Assert.IsTrue(config.TestMode, "TestMode should be true when explicitly set to true");
		Assert.IsFalse(config.EnableUnicodeSupport, "EnableUnicodeSupport should be false when explicitly set to false");
		Assert.IsFalse(config.SaveIniSettings, "SaveIniSettings should be false when explicitly set to false");
	}

	[TestMethod]
	public void ImGuiAppConfig_Fonts_CanBeModified()
	{
		ImGuiAppConfig config = new();
		byte[] testFontData = [0x01, 0x02, 0x03, 0x04];

		config.Fonts.Add("TestFont", testFontData);

		Assert.HasCount(1, config.Fonts);
		Assert.IsTrue(config.Fonts.ContainsKey("TestFont"), "Fonts dictionary should contain the added 'TestFont' key");
		Assert.AreSame(testFontData, config.Fonts["TestFont"]);
	}

	[TestMethod]
	public void ImGuiAppConfig_EmojiFont_ReturnsValue()
	{
		byte[]? emojiFont = ImGuiAppConfig.EmojiFont;

		// The font may or may not be available depending on the build configuration
		Assert.IsTrue(emojiFont is null || emojiFont.Length > 0, "EmojiFont should either be null or contain font data with non-zero length");
	}

	[TestMethod]
	public void ImGuiAppConfig_IsPublicClass()
	{
		Type configType = typeof(ImGuiAppConfig);
		Assert.IsTrue(configType.IsClass, "ImGuiAppConfig should be a class type");
		Assert.IsTrue(configType.IsPublic, "ImGuiAppConfig should be publicly accessible");
	}

	#endregion

	#region ImGuiAppPerformanceSettings Tests

	[TestMethod]
	public void ImGuiAppPerformanceSettings_DefaultValues_AreCorrect()
	{
		ImGuiAppPerformanceSettings settings = new();

		Assert.IsTrue(settings.EnableThrottledRendering, "EnableThrottledRendering should default to true");
		Assert.AreEqual(30.0, settings.FocusedFps);
		Assert.AreEqual(5.0, settings.UnfocusedFps);
		Assert.AreEqual(10.0, settings.IdleFps);
		Assert.AreEqual(2.0, settings.NotVisibleFps);
		Assert.IsTrue(settings.EnableIdleDetection, "EnableIdleDetection should default to true");
		Assert.AreEqual(30.0, settings.IdleTimeoutSeconds);
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
			NotVisibleFps = 1.0,
			EnableIdleDetection = false,
			IdleTimeoutSeconds = 60.0
		};

		Assert.IsFalse(settings.EnableThrottledRendering, "EnableThrottledRendering should be false when explicitly set to false");
		Assert.AreEqual(60.0, settings.FocusedFps);
		Assert.AreEqual(15.0, settings.UnfocusedFps);
		Assert.AreEqual(5.0, settings.IdleFps);
		Assert.AreEqual(1.0, settings.NotVisibleFps);
		Assert.IsFalse(settings.EnableIdleDetection, "EnableIdleDetection should be false when explicitly set to false");
		Assert.AreEqual(60.0, settings.IdleTimeoutSeconds);
	}

	[TestMethod]
	public void ImGuiAppPerformanceSettings_IsPublicClass()
	{
		Type settingsType = typeof(ImGuiAppPerformanceSettings);
		Assert.IsTrue(settingsType.IsClass, "ImGuiAppPerformanceSettings should be a class type");
		Assert.IsTrue(settingsType.IsPublic, "ImGuiAppPerformanceSettings should be publicly accessible");
	}

	#endregion
}
