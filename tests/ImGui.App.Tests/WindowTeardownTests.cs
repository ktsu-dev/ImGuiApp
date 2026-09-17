// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Tests;

using System.Runtime.InteropServices;
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

	[TestMethod]
	public void RunWindowLoopTearsDownWhenTheLoopThrows()
	{
		Mock<IWindow> mockWindow = TestHelpers.CreateMockWindow();
		Mock<IInputContext> mockInput = new();

		// A consumer's OnRender/OnUpdate callback runs inside the frame action the window drives, so
		// a throw from there surfaces as a throw out of Run() — nothing raises Closing on the way.
		InvalidOperationException fromTheRenderCallback = new("OnRender threw");
		mockWindow.Setup(w => w.Run(It.IsAny<Action>())).Throws(fromTheRenderCallback);

		ImGuiApp.window = mockWindow.Object;
		ImGuiApp.inputContext = mockInput.Object;

		InvalidOperationException caught = Assert.ThrowsExactly<InvalidOperationException>(ImGuiApp.RunWindowLoop);

		Assert.AreSame(fromTheRenderCallback, caught,
			"Teardown runs on the way out but must not swallow or replace the consumer's exception.");
		mockWindow.Verify(w => w.Dispose(), Times.Once);
		mockInput.Verify(i => i.Dispose(), Times.Once);
		Assert.IsNull(ImGuiApp.window,
			"A host that catches the exception and keeps running must be able to Start again, which reads this field.");
		Assert.IsNull(ImGuiApp.inputContext);
		Assert.IsNull(ImGuiApp.controller);
		Assert.IsNull(ImGuiApp.renderer);
		Assert.IsNull(ImGuiApp.gl);
		Assert.IsNull(ImGuiApp.glProvider);
	}

	[TestMethod]
	public void TeardownWindowReleasesPinnedFontMemory()
	{
		// Both the blocking loop and the embedded session end in TeardownWindow, and on a faulted
		// loop it is the only thing that runs — the Closing handler that usually frees these never
		// fires.
		GCHandle pinnedFontData = GCHandle.Alloc(new byte[64], GCHandleType.Pinned);
		nint fontMemory = Marshal.AllocHGlobal(64);
		ImGuiApp.currentPinnedFontData.Add(pinnedFontData);
		ImGuiApp.currentFontMemoryHandles.Add(fontMemory);

		ImGuiApp.TeardownWindow();

		// CleanupPinnedFontData frees each handle and then drains the lists, so drained lists are
		// the observable signature of it having run. The handle itself cannot be asserted on:
		// GCHandle is a struct, so freeing the copy held in the list leaves this copy still
		// reading IsAllocated == true.
		Assert.IsEmpty(ImGuiApp.currentPinnedFontData,
			"Teardown has to free the pinned font handles, not just drop the window that owned them.");
		Assert.IsEmpty(ImGuiApp.currentFontMemoryHandles,
			"The unmanaged font buffers are freed by the same sequence, and nothing else will free them.");
	}
}
