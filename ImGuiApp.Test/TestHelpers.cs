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

		// Set up window lifecycle events
		var handlers = new WindowEventHandlers();

		// Setup event raising capabilities
		mockWindow.SetupAdd(x => x.Load += It.IsAny<Action>())
			.Callback<Action>(handlers.LoadHandlers.Add);
		mockWindow.SetupAdd(x => x.Update += It.IsAny<Action<double>>())
			.Callback<Action<double>>(handlers.UpdateHandlers.Add);
		mockWindow.SetupAdd(x => x.Render += It.IsAny<Action<double>>())
			.Callback<Action<double>>(handlers.RenderHandlers.Add);
		mockWindow.SetupAdd(x => x.Closing += It.IsAny<Action>())
			.Callback<Action>(handlers.CloseHandlers.Add);

		// Store the handlers in a static dictionary
		WindowHandlers[mockWindow.Object] = handlers;

		// Setup IsClosing property
		mockWindow.SetupProperty(w => w.IsClosing, false);

		return mockWindow;
	}

	// Static dictionary to store handlers for each mock window
	private static readonly Dictionary<IWindow, WindowEventHandlers> WindowHandlers = [];

	/// <summary>
	/// Creates a test configuration with optional customization.
	/// </summary>
	/// <param name="title">The window title.</param>
	/// <param name="iconPath">The path to the window icon.</param>
	/// <returns>An ImGuiAppConfig instance.</returns>
	public static ImGuiAppConfig CreateTestConfig(string title = "Test Window", string iconPath = "")
	{
		var mockWindow = CreateMockWindow();

		return new ImGuiAppConfig
		{
			Title = title,
			IconPath = iconPath,
			InitialWindowState = new ImGuiAppWindowState
			{
				Size = new Vector2(800, 600),
				Position = new Vector2(100, 100),
			}
		};
	}

	/// <summary>
	/// Helper class to store window event handlers for testing.
	/// </summary>
	internal sealed class WindowEventHandlers
	{
		public List<Action> LoadHandlers { get; } = [];
		public List<Action<double>> UpdateHandlers { get; } = [];
		public List<Action<double>> RenderHandlers { get; } = [];
		public List<Action> CloseHandlers { get; } = [];
	}

	/// <summary>
	/// Simulates a window lifecycle for testing.
	/// </summary>
	/// <param name="window">The mock window to simulate.</param>
	/// <exception cref="ArgumentNullException">Thrown when window is null.</exception>
	/// <exception cref="ArgumentException">Thrown when window is not a mock window created by CreateMockWindow.</exception>
	public static void SimulateWindowLifecycle(IWindow window)
	{
		ArgumentNullException.ThrowIfNull(window);

		if (!WindowHandlers.TryGetValue(window, out var handlers))
		{
			throw new ArgumentException("Window is not a mock window created by CreateMockWindow", nameof(window));
		}

		// Trigger Load
		foreach (var handler in handlers.LoadHandlers)
		{
			handler();
		}

		// Simulate a few frames
		for (var i = 0; i < 3; i++)
		{
			foreach (var handler in handlers.UpdateHandlers)
			{
				handler(0.016); // ~60 FPS
			}

			foreach (var handler in handlers.RenderHandlers)
			{
				handler(0.016);
			}
		}

		// Trigger Close
		window.IsClosing = true;
		foreach (var handler in handlers.CloseHandlers)
		{
			handler();
		}

		// Clean up
		WindowHandlers.Remove(window);
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
