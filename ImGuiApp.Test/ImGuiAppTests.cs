// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

[assembly: DoNotParallelize]

namespace ktsu.ImGuiApp.Test;

using System.Numerics;
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
		var bounds = new Rectangle<int>(0, 0, 1920, 1080);
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
		var expected = (int)(ems * FontAppearance.DefaultFontPointSize);
		var actual = ImGuiApp.EmsToPx(ems);
		Assert.AreEqual(expected, actual);
	}

	[TestMethod]
	public void PtsToPx_WithValidInput_ReturnsCorrectPixels()
	{
		const float pts = 12.0f;
		var expected = pts;
		var actual = ImGuiApp.PtsToPx((int)pts);
		Assert.AreEqual(expected, actual);
	}

	[TestMethod]
	public void ImGuiAppWindowState_DefaultValues_AreCorrect()
	{
		var state = new ImGuiAppWindowState();
		Assert.AreEqual(new Vector2(1280, 720), state.Size);
		Assert.AreEqual(new Vector2(-short.MinValue, -short.MinValue), state.Pos);
		Assert.AreEqual(WindowState.Normal, state.LayoutState);
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
		Assert.IsNotNull(config.OnMoveOrResize);
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
	[ExpectedException(typeof(InvalidOperationException))]
	public void Start_WhenAlreadyRunning_ThrowsInvalidOperationException()
	{
		var config = TestHelpers.CreateTestConfig();
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
	public void OpenGLProvider_GetGL_ReturnsSameInstance()
	{
		// Setup test GL provider
		using var testGL = new TestGL();
		using var mockGL = new MockGL(testGL);
		using var provider = new TestOpenGLProvider(mockGL);

		// Get GL instances
		var gl1 = provider.GetGL();
		var gl2 = provider.GetGL();

		// Verify same instance is returned
		Assert.AreSame(gl1, gl2, "OpenGLProvider should return the same GL instance on subsequent calls");
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
		var invalidPath = new StrongPaths.AbsoluteFilePath();
		Assert.ThrowsException<ArgumentException>(() => ImGuiApp.GetOrLoadTexture(invalidPath));
	}
}
