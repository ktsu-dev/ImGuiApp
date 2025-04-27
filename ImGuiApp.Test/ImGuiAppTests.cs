// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGuiApp.Test;

using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using SixLabors.ImageSharp.PixelFormats;

[TestClass]
public sealed class ImGuiAppTests
{
	private Mock<IWindow>? _mockWindow;
	private Mock<IMonitor>? _mockMonitor;

	[TestInitialize]
	public void Setup()
	{
		_mockWindow = new Mock<IWindow>();
		_mockMonitor = new Mock<IMonitor>();

		// Setup default window properties
		_mockWindow.Setup(w => w.Size).Returns(new Vector2D<int>(1280, 720));
		_mockWindow.Setup(w => w.Position).Returns(new Vector2D<int>(0, 0));
		_mockWindow.Setup(w => w.Monitor).Returns(_mockMonitor.Object);

		// Setup monitor bounds
		var bounds = new Rectangle<int>(0, 0, 1920, 1080);
		_mockMonitor.Setup(m => m.Bounds).Returns(bounds);
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
	[DataRow(-100, -100)]
	[DataRow(2000, 2000)]
	public void EnsureWindowPositionIsValid_WithInvalidPosition_AdjustsPosition(int x, int y)
	{
		var config = TestHelpers.CreateTestConfig(position: new Vector2(x, y));
		ImGuiApp.Start(config);

		var state = ImGuiApp.WindowState;
		Assert.IsTrue(state.Pos.X >= 0);
		Assert.IsTrue(state.Pos.Y >= 0);
		Assert.IsTrue(state.Pos.X < 1920 - state.Size.X);
		Assert.IsTrue(state.Pos.Y < 1080 - state.Size.Y);
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
}
