// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

[assembly: DoNotParallelize]

namespace ktsu.ImGuiApp.Test;

using System.Numerics;
using ktsu.Extensions;
using ktsu.StrongPaths;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Silk.NET.Core.Contexts;
using Silk.NET.Maths;
using Silk.NET.Windowing;

[TestClass]
public sealed class ImGuiAppTests : IDisposable
{
	private Mock<IWindow>? _mockWindow;
	private Mock<IMonitor>? _mockMonitor;
	private TestGL? _testGL;
	private MockGL? _mockGL;
	private Mock<IGLContext>? _mockContext;

	[TestInitialize]
	public void Setup()
	{
		ResetState();
		_mockWindow = new Mock<IWindow>();
		_mockMonitor = new Mock<IMonitor>();
		_testGL = new TestGL();
		_mockGL = new MockGL(_testGL);
		_mockContext = new Mock<IGLContext>();

		// Setup default window properties
		_mockWindow.Setup(w => w.Size).Returns(new Vector2D<int>(1280, 720));
		_mockWindow.Setup(w => w.Position).Returns(new Vector2D<int>(0, 0));
		_mockWindow.Setup(w => w.Monitor).Returns(_mockMonitor.Object);
		_mockWindow.Setup(w => w.GLContext).Returns(_mockContext.Object);

		// Setup monitor bounds
		var bounds = new Rectangle<int>(0, 0, 1920, 1080);
		_mockMonitor.Setup(m => m.Bounds).Returns(bounds);
	}

	[TestCleanup]
	public void Cleanup()
	{
		ResetState();
		_mockGL?.Dispose();
		_testGL?.Dispose();
	}

	public void Dispose()
	{
		Cleanup();
	}

	private static void ResetState()
	{
		ImGuiApp.Reset();
	}

	[TestMethod]
	public void EmsToPx_WithValidInput_ReturnsCorrectPixels()
	{
		const float ems = 1.5f;
		var expected = (int)(ems * FontAppearance.DefaultFontPointSize);
		var actual = UIScaler.EmsToPx(ems);
		Assert.AreEqual(expected, actual);
	}

	[TestMethod]
	public void PtsToPx_WithValidInput_ReturnsCorrectPixels()
	{
		const float pts = 12.0f;
		var expected = pts;
		var actual = UIScaler.PtsToPx((int)pts);
		Assert.AreEqual(expected, actual);
	}

	[TestMethod]
	public void ImGuiAppWindowState_DefaultValues_AreCorrect()
	{
		var state = new ImGuiAppWindowState();
		Assert.AreEqual(new Vector2(1280, 720), state.Size);
		Assert.AreEqual(new Vector2(-short.MinValue, -short.MinValue), state.Position);
	}

	[TestMethod]
	public void ImGuiAppConfig_DefaultValues_AreCorrect()
	{
		var config = new ImGuiAppConfig();
		Assert.AreEqual("ImGuiApp", config.Title);
		Assert.AreEqual(string.Empty, config.IconPath);
		Assert.IsNotNull(config.InitialWindowState);
		Assert.IsNotNull(config.OnStart);
		Assert.IsNotNull(config.OnUpdate);
		Assert.IsNotNull(config.OnRender);
		Assert.IsNotNull(config.OnAppMenu);
		Assert.IsNotNull(config.OnMove);
		Assert.IsNotNull(config.OnResize);
		Assert.IsNotNull(config.Fonts);
	}

	[TestMethod]
	[ExpectedException(typeof(ArgumentNullException))]
	public void Start_WithNullConfig_ThrowsArgumentNullException()
	{
		ImGuiApp.Start(null!);
	}

	[TestMethod]
	[ExpectedException(typeof(FileNotFoundException))]
	public void Start_WithInvalidIconPath_ThrowsFileNotFoundException()
	{
		var config = TestHelpers.CreateTestConfig(iconPath: "nonexistent.png");
		ImGuiApp.Start(config);
	}

	[TestMethod]
	public void EnsureWindowPositionIsValid_WithInvalidPosition_MovesToValidPosition()
	{
		// Reset state to ensure clean test environment
		ResetState();

		// Set up window field using reflection
		var windowField = typeof(ImGuiApp).GetField("window", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
		var mockWindow = new Mock<IWindow>();
		var mockMonitor = new Mock<IMonitor>();

		// Set up monitor bounds
		var monitorBounds = new Rectangle<int>(0, 0, 1920, 1080);
		mockMonitor.Setup(m => m.Bounds).Returns(monitorBounds);

		// Set up window in an invalid position (off screen)
		var windowSize = new Vector2D<int>(800, 600);
		var offScreenPosition = new Vector2D<int>(-1000, -1000);
		mockWindow.Setup(w => w.Size).Returns(windowSize);
		mockWindow.Setup(w => w.Position).Returns(offScreenPosition);
		mockWindow.Setup(w => w.Monitor).Returns(mockMonitor.Object);
		mockWindow.Setup(w => w.WindowState).Returns(WindowState.Normal);

		// Allow position and size to be set
		var finalPosition = offScreenPosition;
		mockWindow.SetupSet(w => w.Position = It.IsAny<Vector2D<int>>())
			.Callback<Vector2D<int>>(pos => finalPosition = pos);
		mockWindow.SetupSet(w => w.Size = It.IsAny<Vector2D<int>>());
		mockWindow.SetupSet(w => w.WindowState = It.IsAny<WindowState>());

		// Set the mock window
		windowField?.SetValue(null, mockWindow.Object);

		// Call EnsureWindowPositionIsValid through reflection
		var method = typeof(ImGuiApp).GetMethod("EnsureWindowPositionIsValid",
			System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
		method?.Invoke(null, null);

		// Verify the window was moved to a valid position
		mockWindow.VerifySet(w => w.Position = It.Is<Vector2D<int>>(pos =>
			monitorBounds.Contains(pos) ||
			monitorBounds.Contains(pos + windowSize)));

		// Additional verification that the window is now visible
		Assert.IsTrue(monitorBounds.Contains(finalPosition) ||
			monitorBounds.Contains(finalPosition + windowSize),
			"Window should be moved to a visible position on the monitor");
	}

	[TestMethod]
	public void DeleteTexture_WithNullGL_ThrowsInvalidOperationException()
	{
		// Reset state to ensure clean test environment
		ResetState();

		// Set up a basic invoker that executes actions immediately
		var invokerField = typeof(ImGuiApp).GetProperty("Invoker", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
		invokerField?.SetValue(null, new Invoker.Invoker());

		// Now test the DeleteTexture method
		Assert.ThrowsException<InvalidOperationException>(() => ImGuiApp.DeleteTexture(1));
	}

	[TestMethod]
	public void GetOrLoadTexture_WithInvalidPath_ThrowsArgumentException()
	{
		var invalidPath = new AbsoluteFilePath();
		Assert.ThrowsException<ArgumentException>(() => ImGuiApp.GetOrLoadTexture(invalidPath));
	}

	[TestMethod]
	public void TextureReloading_AfterDeletion_CreatesNewTexture()
	{
		// Reset state to ensure clean test environment
		ResetState();

		// Set up a path for testing
		var mockTexturePath = "C:/test/texture.png".As<AbsoluteFilePath>();

		// We need to initialize minimal parts of ImGuiApp for the test
		var invokerField = typeof(ImGuiApp).GetProperty("Invoker", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
		invokerField?.SetValue(null, new Invoker.Invoker());

		// We'll test using the public API (TryGetTexture) rather than direct reflection access
		// First, verify there's no texture initially
		var initialTextureExists = ImGuiApp.TryGetTexture(mockTexturePath, out _);
		Assert.IsFalse(initialTextureExists, "Should not have any textures initially");

		// Manually add a texture through the internal field
		// Get the Textures property through reflection - it's a property, not a field
		var texturesProperty = typeof(ImGuiApp).GetProperty("Textures",
			System.Reflection.BindingFlags.Public |
			System.Reflection.BindingFlags.NonPublic |
			System.Reflection.BindingFlags.Static);

		Assert.IsNotNull(texturesProperty, "Textures property should exist");

		// Get the dictionary
		var texturesDict = texturesProperty?.GetValue(null) as System.Collections.Concurrent.ConcurrentDictionary<AbsoluteFilePath, ImGuiAppTextureInfo>;
		Assert.IsNotNull(texturesDict, "Textures dictionary should not be null");

		// First texture
		var firstTextureInfo = new ImGuiAppTextureInfo
		{
			Path = mockTexturePath,
			TextureId = 1001,
			Width = 100,
			Height = 100
		};

		// Add the texture directly to the dictionary
		texturesDict.TryAdd(mockTexturePath, firstTextureInfo);

		// Verify it can be accessed via the public API
		var textureExists = ImGuiApp.TryGetTexture(mockTexturePath, out var retrievedTexture);
		Assert.IsTrue(textureExists, "Texture should exist after adding");
		Assert.IsNotNull(retrievedTexture, "Retrieved texture should not be null");
		Assert.AreEqual(1001u, retrievedTexture!.TextureId, "Texture ID should match");

		// Remove the texture to simulate deletion
		texturesDict.TryRemove(mockTexturePath, out _);

		// Verify it's removed
		textureExists = ImGuiApp.TryGetTexture(mockTexturePath, out _);
		Assert.IsFalse(textureExists, "Texture should be removed after deletion");

		// Create a second texture
		var secondTextureInfo = new ImGuiAppTextureInfo
		{
			Path = mockTexturePath,
			TextureId = 1002,
			Width = 100,
			Height = 100
		};

		// Add the second texture to the dictionary
		texturesDict.TryAdd(mockTexturePath, secondTextureInfo);

		// Verify the newly loaded texture
		textureExists = ImGuiApp.TryGetTexture(mockTexturePath, out var reloadedTexture);
		Assert.IsTrue(textureExists, "Texture should exist after reloading");
		Assert.IsNotNull(reloadedTexture, "Reloaded texture should not be null");
		Assert.AreEqual(1002u, reloadedTexture!.TextureId, "New texture ID should match");
		Assert.AreNotEqual(firstTextureInfo.TextureId, reloadedTexture.TextureId, "Reloaded texture should have a different ID");
	}
}
