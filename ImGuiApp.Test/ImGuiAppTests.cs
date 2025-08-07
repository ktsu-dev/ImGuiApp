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
	public void Start_WithNullConfig_ThrowsArgumentNullException()
	{
		Assert.ThrowsExactly<ArgumentNullException>(() => ImGuiApp.Start(null!));
	}

	[TestMethod]
	public void Start_WithInvalidIconPath_ThrowsFileNotFoundException()
	{
		ImGuiAppConfig config = TestHelpers.CreateTestConfig(iconPath: "nonexistent.png");
		Assert.ThrowsExactly<FileNotFoundException>(() => ImGuiApp.Start(config));
	}

	[TestMethod]
	public void Stop_WhenNotRunning_ThrowsInvalidOperationException()
	{
		Assert.ThrowsExactly<InvalidOperationException>(ImGuiApp.Stop);
	}

	[TestMethod]
	public void EnsureWindowPositionIsValid_WithInvalidPosition_MovesToValidPosition()
	{
		// Reset state to ensure clean test environment
		ResetState();

		// Set up window field using direct access to internal member
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
		Vector2D<int> finalSize = windowSize;
		mockWindow.SetupSet(w => w.Position = It.IsAny<Vector2D<int>>())
			.Callback<Vector2D<int>>(pos => finalPosition = pos);
		mockWindow.SetupSet(w => w.Size = It.IsAny<Vector2D<int>>())
			.Callback<Vector2D<int>>(size => finalSize = size);
		mockWindow.SetupSet(w => w.WindowState = It.IsAny<WindowState>());

		// Set the mock window directly using internal field
		ImGuiApp.window = mockWindow.Object;

		// Call EnsureWindowPositionIsValid directly using internal method
		ImGuiApp.EnsureWindowPositionIsValid();

		// Verify the window was moved to a valid position
		Assert.IsTrue(monitorBounds.Contains(finalPosition),
			"Window position should be within monitor bounds");

		// Verify the window size was preserved (improvement from new logic)
		Assert.AreEqual(windowSize, finalSize, "Window size should be preserved when it fits");
	}

	[TestMethod]
	public void EnsureWindowPositionIsValid_WithPerformanceOptimization_SkipsUnnecessaryChecks()
	{
		// Reset state to ensure clean test environment
		ResetState();

		Mock<IWindow> mockWindow = new();
		Mock<IMonitor> mockMonitor = new();

		Rectangle<int> monitorBounds = new(0, 0, 1920, 1080);
		mockMonitor.Setup(m => m.Bounds).Returns(monitorBounds);

		Vector2D<int> validPosition = new(100, 100);
		Vector2D<int> validSize = new(800, 600);
		mockWindow.Setup(w => w.Size).Returns(validSize);
		mockWindow.Setup(w => w.Position).Returns(validPosition);
		mockWindow.Setup(w => w.Monitor).Returns(mockMonitor.Object);
		mockWindow.Setup(w => w.WindowState).Returns(WindowState.Normal);

		ImGuiApp.window = mockWindow.Object;

		// First call should perform validation
		ImGuiApp.EnsureWindowPositionIsValid();
		int boundsCallsAfterFirst = mockMonitor.Invocations.Count;

		// Second call with same position/size should skip validation (performance optimization)
		ImGuiApp.EnsureWindowPositionIsValid();
		int boundsCallsAfterSecond = mockMonitor.Invocations.Count;

		// Should have made no additional calls on second invocation due to caching
		Assert.AreEqual(boundsCallsAfterFirst, boundsCallsAfterSecond,
			"Second call should skip validation due to performance optimization");
	}

	[TestMethod]
	public void EnsureWindowPositionIsValid_WithPartiallyVisibleWindow_RelocatesWhenInsufficientlyVisible()
	{
		// Reset state to ensure clean test environment
		ResetState();

		Mock<IWindow> mockWindow = new();
		Mock<IMonitor> mockMonitor = new();

		Rectangle<int> monitorBounds = new(0, 0, 1920, 1080);
		mockMonitor.Setup(m => m.Bounds).Returns(monitorBounds);

		// Window with only small corner visible (less than 25% visibility requirement)
		Vector2D<int> windowSize = new(800, 600);
		Vector2D<int> barelyVisiblePosition = new(1870, 1030); // Only 50x50 pixels visible
		mockWindow.Setup(w => w.Size).Returns(windowSize);
		mockWindow.Setup(w => w.Position).Returns(barelyVisiblePosition);
		mockWindow.Setup(w => w.Monitor).Returns(mockMonitor.Object);
		mockWindow.Setup(w => w.WindowState).Returns(WindowState.Normal);

		Vector2D<int> finalPosition = barelyVisiblePosition;
		mockWindow.SetupSet(w => w.Position = It.IsAny<Vector2D<int>>())
			.Callback<Vector2D<int>>(pos => finalPosition = pos);
		mockWindow.SetupSet(w => w.Size = It.IsAny<Vector2D<int>>());
		mockWindow.SetupSet(w => w.WindowState = It.IsAny<WindowState>());

		ImGuiApp.window = mockWindow.Object;
		ImGuiApp.EnsureWindowPositionIsValid();

		// Window should be relocated since it's insufficiently visible
		Assert.AreNotEqual(barelyVisiblePosition, finalPosition,
			"Window should be relocated when insufficiently visible");
		Assert.IsTrue(monitorBounds.Contains(finalPosition),
			"Relocated window should be fully within monitor bounds");
	}

	[TestMethod]
	public void EnsureWindowPositionIsValid_WithSufficientlyVisibleWindow_LeavesWindowAlone()
	{
		// Reset state to ensure clean test environment
		ResetState();

		Mock<IWindow> mockWindow = new();
		Mock<IMonitor> mockMonitor = new();

		Rectangle<int> monitorBounds = new(0, 0, 1920, 1080);
		mockMonitor.Setup(m => m.Bounds).Returns(monitorBounds);

		// Window with significant visibility (more than 25%)
		// Window 800x600 = 480,000 pixels total
		// Visible portion: 400x300 = 120,000 pixels (25% exactly)
		Vector2D<int> windowSize = new(800, 600);
		Vector2D<int> partiallyVisiblePosition = new(1520, 780); // 400x300 pixels visible
		mockWindow.Setup(w => w.Size).Returns(windowSize);
		mockWindow.Setup(w => w.Position).Returns(partiallyVisiblePosition);
		mockWindow.Setup(w => w.Monitor).Returns(mockMonitor.Object);
		mockWindow.Setup(w => w.WindowState).Returns(WindowState.Normal);

		mockWindow.SetupSet(w => w.Position = It.IsAny<Vector2D<int>>());

		ImGuiApp.window = mockWindow.Object;
		ImGuiApp.EnsureWindowPositionIsValid();

		// Window position should not be changed since it has sufficient visibility
		mockWindow.VerifySet(w => w.Position = It.IsAny<Vector2D<int>>(), Times.Never,
			"Window should not be moved when sufficiently visible");
	}

	[TestMethod]
	public void EnsureWindowPositionIsValid_WithOversizedWindow_FitsToMonitor()
	{
		// Reset state to ensure clean test environment
		ResetState();

		Mock<IWindow> mockWindow = new();
		Mock<IMonitor> mockMonitor = new();

		// Small monitor
		Rectangle<int> smallMonitorBounds = new(0, 0, 1024, 768);
		mockMonitor.Setup(m => m.Bounds).Returns(smallMonitorBounds);

		// Oversized window completely off-screen
		Vector2D<int> oversizedSize = new(2000, 1500);
		Vector2D<int> offScreenPosition = new(-2500, -2000); // Completely off-screen
		mockWindow.Setup(w => w.Size).Returns(oversizedSize);
		mockWindow.Setup(w => w.Position).Returns(offScreenPosition);
		mockWindow.Setup(w => w.Monitor).Returns(mockMonitor.Object);
		mockWindow.Setup(w => w.WindowState).Returns(WindowState.Normal);

		Vector2D<int> finalSize = oversizedSize;
		mockWindow.SetupSet(w => w.Position = It.IsAny<Vector2D<int>>());
		mockWindow.SetupSet(w => w.Size = It.IsAny<Vector2D<int>>())
			.Callback<Vector2D<int>>(size => finalSize = size);
		mockWindow.SetupSet(w => w.WindowState = It.IsAny<WindowState>());

		ImGuiApp.window = mockWindow.Object;
		ImGuiApp.EnsureWindowPositionIsValid();

		// Debug: Check if window was relocated at all
		mockWindow.VerifySet(w => w.Position = It.IsAny<Vector2D<int>>(), Times.AtLeastOnce,
			"Window should have been relocated when completely off-screen");

		// Window should be resized to fit monitor (with 100px margin)
		Assert.IsTrue(finalSize.X <= smallMonitorBounds.Size.X - 100,
			$"Window width {finalSize.X} should be <= {smallMonitorBounds.Size.X - 100} to fit monitor");
		Assert.IsTrue(finalSize.Y <= smallMonitorBounds.Size.Y - 100,
			$"Window height {finalSize.Y} should be <= {smallMonitorBounds.Size.Y - 100} to fit monitor");
		Assert.IsTrue(finalSize.X >= 640 && finalSize.Y >= 480,
			$"Window size {finalSize} should maintain minimum size of 640x480");
	}

	[TestMethod]
	public void ForceWindowPositionValidation_ForcesNextValidation()
	{
		// Reset state to ensure clean test environment
		ResetState();

		Mock<IWindow> mockWindow = new();
		Mock<IMonitor> mockMonitor = new();

		Rectangle<int> monitorBounds = new(0, 0, 1920, 1080);
		mockMonitor.Setup(m => m.Bounds).Returns(monitorBounds);

		Vector2D<int> validPosition = new(100, 100);
		Vector2D<int> validSize = new(800, 600);
		mockWindow.Setup(w => w.Size).Returns(validSize);
		mockWindow.Setup(w => w.Position).Returns(validPosition);
		mockWindow.Setup(w => w.Monitor).Returns(mockMonitor.Object);
		mockWindow.Setup(w => w.WindowState).Returns(WindowState.Normal);

		ImGuiApp.window = mockWindow.Object;

		// First validation
		ImGuiApp.EnsureWindowPositionIsValid();
		int callsAfterFirst = mockMonitor.Invocations.Count;

		// Force validation should make next call perform validation even with same position
		ImGuiApp.ForceWindowPositionValidation();
		ImGuiApp.EnsureWindowPositionIsValid();
		int callsAfterForced = mockMonitor.Invocations.Count;

		// Should have made additional calls after forcing validation
		Assert.IsTrue(callsAfterForced > callsAfterFirst,
			"Forced validation should cause additional monitor access");
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

		// Set up a basic invoker that executes actions immediately using direct access
		ImGuiApp.Invoker = new Invoker.Invoker();

		// Now test the DeleteTexture method
		Assert.ThrowsExactly<InvalidOperationException>(() => ImGuiApp.DeleteTexture(1));
	}

	[TestMethod]
	public void GetOrLoadTexture_WithInvalidPath_ThrowsArgumentException()
	{
		AbsoluteFilePath invalidPath = new();
		Assert.ThrowsExactly<ArgumentException>(() => ImGuiApp.GetOrLoadTexture(invalidPath));
	}

	[TestMethod]
	public void TextureReloading_AfterDeletion_CreatesNewTexture()
	{
		// Reset state to ensure clean test environment
		ResetState();

		// Set up a path for testing - use a proper absolute path format
		AbsoluteFilePath mockTexturePath = Path.GetFullPath("test_texture.png").As<AbsoluteFilePath>();

		// We need to initialize minimal parts of ImGuiApp for the test
		ImGuiApp.Invoker = new Invoker.Invoker();

		// We'll test using the public API (TryGetTexture) rather than direct access
		// First, verify there's no texture initially
		bool initialTextureExists = ImGuiApp.TryGetTexture(mockTexturePath, out _);
		Assert.IsFalse(initialTextureExists, "Should not have any textures initially");

		// Manually add a texture through direct access to internal property
		System.Collections.Concurrent.ConcurrentDictionary<AbsoluteFilePath, ImGuiAppTextureInfo> texturesDict = ImGuiApp.Textures;
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
		Assert.AreEqual(2.0, settings.NotVisibleFps);
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
