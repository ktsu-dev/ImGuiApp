// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGuiApp.Test;

using System.Numerics;
using Moq;
using Silk.NET.Maths;
using Silk.NET.Windowing;

/// <summary>
/// Provides helper methods for testing ImGuiApp components.
/// </summary>
public static class TestHelpers
{
	/// <summary>
	/// Creates a mock window with default settings.
	/// </summary>
	/// <returns>A mock IWindow instance.</returns>
	public static Mock<IWindow> CreateMockWindow()
	{
		var mockWindow = new Mock<IWindow>();
		mockWindow.Setup(w => w.Size).Returns(new Vector2D<int>(1280, 720));
		mockWindow.Setup(w => w.Position).Returns(new Vector2D<int>(0, 0));
		mockWindow.Setup(w => w.WindowState).Returns(WindowState.Normal);
		return mockWindow;
	}

	/// <summary>
	/// Creates a test configuration with optional customization.
	/// </summary>
	/// <param name="title">The window title.</param>
	/// <param name="iconPath">The path to the window icon.</param>
	/// <returns>An ImGuiAppConfig instance.</returns>
	public static ImGuiAppConfig CreateTestConfig(string title = "Test Window", string iconPath = "")
	{
		return new ImGuiAppConfig
		{
			Title = title,
			IconPath = iconPath,
			InitialWindowState = new ImGuiAppWindowState
			{
				Size = new Vector2(800, 600),
				Pos = new Vector2(100, 100),
				LayoutState = WindowState.Normal
			}
		};
	}

	/// <summary>
	/// Verifies that a window's properties match the expected values.
	/// </summary>
	/// <param name="window">The window to verify.</param>
	/// <param name="expectedSize">The expected window size.</param>
	/// <param name="expectedPosition">The expected window position.</param>
	/// <param name="expectedState">The expected window state.</param>
	/// <exception cref="ArgumentNullException">Thrown when window is null.</exception>
	public static void VerifyWindowProperties(
		IWindow window,
		Vector2D<int> expectedSize,
		Vector2D<int> expectedPosition,
		WindowState expectedState)
	{
		ArgumentNullException.ThrowIfNull(window);

		Assert.AreEqual(expectedSize, window.Size);
		Assert.AreEqual(expectedPosition, window.Position);
		Assert.AreEqual(expectedState, window.WindowState);
	}
}
