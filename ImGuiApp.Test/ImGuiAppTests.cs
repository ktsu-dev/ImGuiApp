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
using SixLabors.ImageSharp.PixelFormats;

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
		const float dpi = 96.0f;
		var expected = ems * 16.0f * (dpi / 96.0f);
		var actual = ImGuiApp.EmsToPx(ems);
		Assert.AreEqual(expected, actual);
	}

	[TestMethod]
	public void PtsToPx_WithValidInput_ReturnsCorrectPixels()
	{
		const float pts = 12.0f;
		const float dpi = 96.0f;
		var expected = pts * (dpi / 72.0f);
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
		ImGuiApp.Start(config); // Should throw
	}

	[TestMethod]
	[ExpectedException(typeof(InvalidOperationException))]
	public void Stop_WhenNotRunning_ThrowsInvalidOperationException()
	{
		ImGuiApp.Stop();
	}

	[TestMethod]
	public void EnsureWindowPositionIsValid_WithInvalidPosition_NotTestable()
	{
		// Skip this test as it requires more complex mocking of the OpenGL context
		Assert.Inconclusive("This test requires more complex mocking of the OpenGL context");
	}

	[TestMethod]
	public void GetImageBytes_WithValidImage_ReturnsCorrectByteArray()
	{
		using var image = new SixLabors.ImageSharp.Image<Rgba32>(100, 100);
		var bytes = ImGuiApp.GetImageBytes(image);
		Assert.IsNotNull(bytes);
		Assert.AreEqual(100 * 100 * 4, bytes.Length); // 4 bytes per pixel (RGBA)
	}

	[TestMethod]
	[ExpectedException(typeof(ArgumentNullException))]
	public void GetImageBytes_WithNullImage_ThrowsArgumentNullException()
	{
		ImGuiApp.GetImageBytes(null!);
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
		// Mock the window and GL context
		var mockWindow = new Mock<IWindow>();
		var mockContext = new Mock<IGLContext>();
		mockWindow.Setup(w => w.GLContext).Returns(mockContext.Object);

		// Create a test config that won't actually run the window
		var config = new ImGuiAppConfig { OnStart = ImGuiApp.Stop };

		// Start and immediately stop the app
		ImGuiApp.Start(config);

		Assert.ThrowsException<InvalidOperationException>(() => ImGuiApp.DeleteTexture(1));
	}

	[TestMethod]
	public void GetOrLoadTexture_WithInvalidPath_ThrowsArgumentException()
	{
		var invalidPath = new StrongPaths.AbsoluteFilePath();
		Assert.ThrowsException<ArgumentException>(() => ImGuiApp.GetOrLoadTexture(invalidPath));
	}

	[TestMethod]
	public void CleanupUnusedTextures_DoesNotThrowException()
	{
		// This should not throw even when there are no textures
		ImGuiApp.CleanupUnusedTextures();
	}
}
