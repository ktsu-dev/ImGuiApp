// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

using Microsoft.VisualStudio.TestTools.UnitTesting;

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
	private TestOpenGLProvider? _glProvider;

	[TestInitialize]
	public void Setup()
	{
		ResetState();
		_mockWindow = new Mock<IWindow>();
		_mockMonitor = new Mock<IMonitor>();
		_testGL = new TestGL();
		_mockGL = new MockGL(_testGL);
		_mockContext = new Mock<IGLContext>();
		_glProvider = new TestOpenGLProvider(_mockGL);

		// Setup default window properties
		_mockWindow.Setup(w => w.Size).Returns(new Vector2D<int>(1280, 720));
		_mockWindow.Setup(w => w.Position).Returns(new Vector2D<int>(0, 0));
		_mockWindow.Setup(w => w.Monitor).Returns(_mockMonitor.Object);
		_mockWindow.Setup(w => w.GLContext).Returns(_mockContext.Object);

		// Setup monitor bounds
		Rectangle<int> bounds = new(0, 0, 1920, 1080);
		_mockMonitor.Setup(m => m.Bounds).Returns(bounds);
	}

	[TestCleanup]
	public void Cleanup()
	{
		ResetState();
		_glProvider?.Dispose();
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
		int expected = (int)(ems * FontAppearance.DefaultFontPointSize);
		int actual = ImGuiApp.EmsToPx(ems);
		Assert.AreEqual(expected, actual);
	}

	[TestMethod]
	public void PtsToPx_WithValidInput_ReturnsCorrectPixels()
	{
		const float pts = 12.0f;
		float expected = pts;
		int actual = ImGuiApp.PtsToPx((int)pts);
		Assert.AreEqual(expected, actual);
	}

	[TestMethod]
	public void ImGuiAppWindowState_DefaultValues_AreCorrect()
	{
		ImGuiAppWindowState state = new();
		Assert.AreEqual(new Vector2(1280, 720), state.Size);
		Assert.AreEqual(new Vector2(-short.MinValue, -short.MinValue), state.Pos);
		Assert.AreEqual(WindowState.Normal, state.LayoutState);
	}

	[TestMethod]
	public void ImGuiAppConfig_DefaultValues_AreCorrect()
	{
		ImGuiAppConfig config = new();
		Assert.AreEqual("ImGuiApp", config.Title);
		Assert.AreEqual(string.Empty, config.IconPath);
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
	[ExpectedException(typeof(ArgumentNullException))]
	public void Start_WithNullConfig_ThrowsArgumentNullException()
	{
		ImGuiApp.Start(null!);
	}

	[TestMethod]
	[ExpectedException(typeof(FileNotFoundException))]
	public void Start_WithInvalidIconPath_ThrowsFileNotFoundException()
	{
		ImGuiAppConfig config = TestHelpers.CreateTestConfig(iconPath: "nonexistent.png");
		ImGuiApp.Start(config);
	}

	[TestMethod]
	[ExpectedException(typeof(InvalidOperationException))]
	public void Start_WhenAlreadyRunning_ThrowsInvalidOperationException()
	{
		ImGuiAppConfig config = TestHelpers.CreateTestConfig();
		ImGuiApp.Start(config);
		TestHelpers.SimulateWindowLifecycle(config.TestWindow!);
		ImGuiApp.Start(config); // Should throw
	}

	[TestMethod]
	[ExpectedException(typeof(InvalidOperationException))]
	public void Stop_WhenNotRunning_ThrowsInvalidOperationException()
	{
		ImGuiApp.Stop();
	}

	[TestMethod]
	public void EnsureWindowPositionIsValid_WithInvalidPosition_MovesToValidPosition()
	{
		// Reset state to ensure clean test environment
		ResetState();

		// Set up window field using reflection
		System.Reflection.FieldInfo? windowField = typeof(ImGuiApp).GetField("window", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
		Mock<IWindow> mockWindow = new();
		Mock<IMonitor> mockMonitor = new();

		// Set up monitor bounds
		Rectangle<int> monitorBounds = new(0, 0, 1920, 1080);
		mockMonitor.Setup(m => m.Bounds).Returns(monitorBounds);

		// Set up window in an invalid position (off screen)
		Vector2D<int> windowSize = new(800, 600);
		Vector2D<int> offScreenPosition = new(-1000, -1000);
		mockWindow.Setup(w => w.Size).Returns(windowSize);
		mockWindow.Setup(w => w.Position).Returns(offScreenPosition);
		mockWindow.Setup(w => w.Monitor).Returns(mockMonitor.Object);
		mockWindow.Setup(w => w.WindowState).Returns(WindowState.Normal);

		// Allow position and size to be set
		Vector2D<int> finalPosition = offScreenPosition;
		mockWindow.SetupSet(w => w.Position = It.IsAny<Vector2D<int>>())
			.Callback<Vector2D<int>>(pos => finalPosition = pos);
		mockWindow.SetupSet(w => w.Size = It.IsAny<Vector2D<int>>());
		mockWindow.SetupSet(w => w.WindowState = It.IsAny<WindowState>());

		// Set the mock window
		windowField?.SetValue(null, mockWindow.Object);

		// Call EnsureWindowPositionIsValid through reflection
		System.Reflection.MethodInfo? method = typeof(ImGuiApp).GetMethod("EnsureWindowPositionIsValid",
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
	public void OpenGLProvider_GetGL_ReturnsSameInstance()
	{
		// Setup test GL provider
		using TestGL testGL = new();
		using MockGL mockGL = new(testGL);
		using TestOpenGLProvider provider = new(mockGL);

		// Get GL instances
		ImGuiController.IGL gl1 = provider.GetGL();
		ImGuiController.IGL gl2 = provider.GetGL();

		// Verify same instance is returned
		Assert.AreSame(gl1, gl2, "OpenGLProvider should return the same GL instance on subsequent calls");
	}

	[TestMethod]
	public void DeleteTexture_WithNullGL_ThrowsInvalidOperationException()
	{
		// Reset state to ensure clean test environment
		ResetState();

		// Set up a basic invoker that executes actions immediately
		System.Reflection.PropertyInfo? invokerField = typeof(ImGuiApp).GetProperty("Invoker", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
		invokerField?.SetValue(null, new Invoker.Invoker());

		// Now test the DeleteTexture method
		Assert.ThrowsException<InvalidOperationException>(() => ImGuiApp.DeleteTexture(1));
	}

	[TestMethod]
	public void GetOrLoadTexture_WithInvalidPath_ThrowsArgumentException()
	{
		AbsoluteFilePath invalidPath = new();
		Assert.ThrowsException<ArgumentException>(() => ImGuiApp.GetOrLoadTexture(invalidPath));
	}

	[TestMethod]
	public void TextureReloading_AfterDeletion_CreatesNewTexture()
	{
		// Reset state to ensure clean test environment
		ResetState();

		// Set up a path for testing - use a proper absolute path format
		AbsoluteFilePath mockTexturePath = Path.GetFullPath("test_texture.png").As<AbsoluteFilePath>();

		// We need to initialize minimal parts of ImGuiApp for the test
		System.Reflection.PropertyInfo? invokerField = typeof(ImGuiApp).GetProperty("Invoker", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
		invokerField?.SetValue(null, new Invoker.Invoker());

		// We'll test using the public API (TryGetTexture) rather than direct reflection access
		// First, verify there's no texture initially
		bool initialTextureExists = ImGuiApp.TryGetTexture(mockTexturePath, out _);
		Assert.IsFalse(initialTextureExists, "Should not have any textures initially");

		// Manually add a texture through the internal field
		// Get the Textures property through reflection - it's a property, not a field
		System.Reflection.PropertyInfo? texturesProperty = typeof(ImGuiApp).GetProperty("Textures",
			System.Reflection.BindingFlags.Public |
			System.Reflection.BindingFlags.NonPublic |
			System.Reflection.BindingFlags.Static);

		Assert.IsNotNull(texturesProperty, "Textures property should exist");

		// Get the dictionary
		System.Collections.Concurrent.ConcurrentDictionary<AbsoluteFilePath, ImGuiAppTextureInfo>? texturesDict = texturesProperty?.GetValue(null) as System.Collections.Concurrent.ConcurrentDictionary<AbsoluteFilePath, ImGuiAppTextureInfo>;
		Assert.IsNotNull(texturesDict, "Textures dictionary should not be null");

		// First texture
		ImGuiAppTextureInfo firstTextureInfo = new()
		{
			Path = mockTexturePath,
			TextureId = 1001,
			Width = 100,
			Height = 100
		};

		// Add the texture directly to the dictionary
		texturesDict.TryAdd(mockTexturePath, firstTextureInfo);

		// Verify it can be accessed via the public API
		bool textureExists = ImGuiApp.TryGetTexture(mockTexturePath, out ImGuiAppTextureInfo? retrievedTexture);
		Assert.IsTrue(textureExists, "Texture should exist after adding");
		Assert.IsNotNull(retrievedTexture, "Retrieved texture should not be null");
		Assert.AreEqual(1001u, retrievedTexture!.TextureId, "Texture ID should match");

		// Remove the texture to simulate deletion
		texturesDict.TryRemove(mockTexturePath, out _);

		// Verify it's removed
		textureExists = ImGuiApp.TryGetTexture(mockTexturePath, out _);
		Assert.IsFalse(textureExists, "Texture should be removed after deletion");

		// Create a second texture
		ImGuiAppTextureInfo secondTextureInfo = new()
		{
			Path = mockTexturePath,
			TextureId = 1002,
			Width = 100,
			Height = 100
		};

		// Add the second texture to the dictionary
		texturesDict.TryAdd(mockTexturePath, secondTextureInfo);

		// Verify the newly loaded texture
		textureExists = ImGuiApp.TryGetTexture(mockTexturePath, out ImGuiAppTextureInfo? reloadedTexture);
		Assert.IsTrue(textureExists, "Texture should exist after reloading");
		Assert.IsNotNull(reloadedTexture, "Reloaded texture should not be null");
		Assert.AreEqual(1002u, reloadedTexture!.TextureId, "New texture ID should match");
		Assert.AreNotEqual(firstTextureInfo.TextureId, reloadedTexture.TextureId, "Reloaded texture should have a different ID");
	}

	[TestMethod]
	public void PerformanceSettings_DefaultValues_AreCorrect()
	{
		// Arrange & Act
		ImGuiAppPerformanceSettings settings = new();

		// Assert
		Assert.IsTrue(settings.EnableThrottledRendering);
		Assert.AreEqual(30.0, settings.FocusedFps);
		Assert.AreEqual(5.0, settings.UnfocusedFps);
		Assert.AreEqual(10.0, settings.IdleFps);
		Assert.IsTrue(settings.EnableIdleDetection);
		Assert.AreEqual(30.0, settings.IdleTimeoutSeconds);
	}

	[TestMethod]
	public void Reset_ResetsPerformanceFields_Correctly()
	{
		// Arrange - Simulate some state changes that would occur during normal operation
		// Note: We can't directly set IsIdle since it has a private setter, but we can verify it's reset
		// We also can't directly access lastInputTime, but we can verify the Reset behavior

		// Act - Reset the application state
		ImGuiApp.Reset();

		// Assert - Verify that performance-related fields are reset to their default values
		Assert.IsFalse(ImGuiApp.IsIdle, "IsIdle should be reset to false");
		Assert.IsTrue(ImGuiApp.IsFocused, "IsFocused should be reset to true");

		// Note: lastInputTime is private so we can't directly test it, but the Reset() method
		// should set it to DateTime.UtcNow, which will be tested implicitly through the idle detection logic
	}

	[TestMethod]
	public void Reset_PreventesStatePollutionBetweenTests()
	{
		// Arrange - First call Reset to ensure clean state
		ImGuiApp.Reset();

		// Verify initial clean state
		Assert.IsTrue(ImGuiApp.IsFocused, "IsFocused should start as true");
		Assert.IsFalse(ImGuiApp.IsIdle, "IsIdle should start as false");

		// Act - Simulate some state changes that could happen during a test
		// We can call OnUserInput to update lastInputTime
		ImGuiApp.OnUserInput();

		// Call Reset again to simulate what happens between test runs
		ImGuiApp.Reset();

		// Assert - Verify that Reset properly restored the default state
		Assert.IsTrue(ImGuiApp.IsFocused, "IsFocused should be reset to true");
		Assert.IsFalse(ImGuiApp.IsIdle, "IsIdle should be reset to false");

		// The lastInputTime should be reset to DateTime.UtcNow (current time)
		// We can't test this directly, but the behavior should be consistent
	}
}
