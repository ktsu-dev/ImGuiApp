// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Silk.NET.Input;
using Silk.NET.Windowing;

/// <summary>
/// Tests for what happens after the blocking run loop returns. A window that closes cleanly has to
/// leave the static state as it found it, otherwise the process can never start a second window —
/// which is what a restart-to-apply-settings flow, or a harness driving sequential sessions, needs.
/// </summary>
[TestClass]
public sealed class WindowTeardownTests
{
	[TestInitialize]
	public void Setup() => ImGuiApp.Reset();

	[TestCleanup]
	public void Cleanup() => ImGuiApp.Reset();

	[TestMethod]
	public void RunWindowLoopDisposesTheWindowAndClearsIt()
	{
		Mock<IWindow> mockWindow = TestHelpers.CreateMockWindow();
		ImGuiApp.window = mockWindow.Object;

		ImGuiApp.RunWindowLoop();

		// Run() is an extension method over IWindow, so what it did is observable only through the
		// teardown that follows it.
		mockWindow.Verify(w => w.Dispose(), Times.Once);
		Assert.IsNull(ImGuiApp.window,
			"The run loop has returned, so nothing is running and Start's guard must not think otherwise.");
	}

	[TestMethod]
	public void RunWindowLoopReleasesTheResourcesThatHungOffTheWindow()
	{
		Mock<IWindow> mockWindow = TestHelpers.CreateMockWindow();
		Mock<IInputContext> mockInput = new();
		ImGuiApp.window = mockWindow.Object;
		ImGuiApp.inputContext = mockInput.Object;

		ImGuiApp.RunWindowLoop();

		mockInput.Verify(i => i.Dispose(), Times.Once);
		Assert.IsNull(ImGuiApp.inputContext);
		Assert.IsNull(ImGuiApp.controller);
		Assert.IsNull(ImGuiApp.renderer);
		Assert.IsNull(ImGuiApp.gl);
		Assert.IsNull(ImGuiApp.glProvider);
	}

	[TestMethod]
	public void StartIsAllowedAgainOnceTheRunLoopHasReturned()
	{
		ImGuiAppConfig config = TestHelpers.CreateTestConfig();

		// TestMode builds the window and returns without entering the blocking loop, so the loop
		// is driven here instead — the mock's Run() returns immediately, as a closed window does.
		ImGuiApp.Start(config);
		ImGuiApp.RunWindowLoop();

		ImGuiApp.Start(config);

		Assert.IsNotNull(ImGuiApp.window,
			"A second Start after a clean teardown builds a window rather than throwing 'Application is already running.'");
	}
}
