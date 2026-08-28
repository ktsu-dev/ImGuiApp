// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Tests;

using System.Runtime.InteropServices;
using ktsu.Invoker;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Silk.NET.Windowing;

/// <summary>
/// Tests for changing the window title while the application runs, and for cancelling a close.
/// Together these are what an application with unsaved work needs: a title that can show the open
/// document and its dirty state, and a chance to prompt before the window goes away.
/// </summary>
[TestClass]
public sealed class WindowTitleAndClosingTests
{
	private Mock<IWindow> mockWindow = null!;

	[TestInitialize]
	public void Setup()
	{
		ImGuiApp.Reset();
		mockWindow = TestHelpers.CreateMockWindow();
		ImGuiApp.window = mockWindow.Object;

		// SetWindowTitle marshals to the window thread; run queued work inline so a test does not
		// need a live render loop to observe the effect.
		ImGuiApp.Invoker = new Invoker();
	}

	[TestCleanup]
	public void Cleanup() => ImGuiApp.Reset();

	private static void PumpInvoker() => ImGuiApp.Invoker.DoInvokes();

	[TestMethod]
	public void SetWindowTitleChangesTheWindowTitle()
	{
		ImGuiApp.SetWindowTitle("schema.json - SchemaEditor");
		PumpInvoker();

		Assert.AreEqual("schema.json - SchemaEditor", mockWindow.Object.Title);
		Assert.AreEqual("schema.json - SchemaEditor", ImGuiApp.WindowTitle);
	}

	[TestMethod]
	public void SetWindowTitleWithTheSameTitleDoesNotTouchTheWindow()
	{
		ImGuiApp.SetWindowTitle("Same");
		PumpInvoker();
		mockWindow.Invocations.Clear();

		ImGuiApp.SetWindowTitle("Same");
		PumpInvoker();

		Assert.AreEqual(0, mockWindow.Invocations.Count(i => i.Method.Name == "set_Title"),
			"An unchanged title is not written through, so it is safe to call every frame.");
	}

	[TestMethod]
	public void SetWindowTitleRejectsNull() =>
		Assert.ThrowsExactly<ArgumentNullException>(() => ImGuiApp.SetWindowTitle(null!));

	[TestMethod]
	public void SetWindowTitleBeforeTheWindowExistsIsIgnored()
	{
		ImGuiApp.window = null;

		ImGuiApp.SetWindowTitle("Ignored");

		Assert.AreEqual(ImGuiApp.Config.Title, ImGuiApp.WindowTitle,
			"With no window the configured title is reported.");
	}

	[TestMethod]
	public void WindowTitleFallsBackToTheConfiguredTitle()
	{
		ImGuiApp.window = null;
		ImGuiApp.Config = new ImGuiAppConfig { Title = "Configured" };

		Assert.AreEqual("Configured", ImGuiApp.WindowTitle);
	}

	[TestMethod]
	public void ClosingProceedsWhenNoHandlerIsConfigured()
	{
		ImGuiApp.Config = new ImGuiAppConfig();

		Assert.IsTrue(ImGuiApp.ShouldClose());
		Assert.IsFalse(mockWindow.Object.IsClosing, "Nothing cancels the close.");
	}

	[TestMethod]
	public void ClosingProceedsWhenTheHandlerAllowsIt()
	{
		ImGuiApp.Config = new ImGuiAppConfig { OnClosing = () => true };

		Assert.IsTrue(ImGuiApp.ShouldClose());
	}

	[TestMethod]
	public void ClosingIsCancelledWhenTheHandlerDeclines()
	{
		mockWindow.Object.IsClosing = true;
		ImGuiApp.Config = new ImGuiAppConfig { OnClosing = () => false };

		Assert.IsFalse(ImGuiApp.ShouldClose(), "The caller is told not to tear anything down.");
		Assert.IsFalse(mockWindow.Object.IsClosing,
			"IsClosing is cleared, which is what keeps the run loop going.");
	}

	[TestMethod]
	public void TheClosingHandlerIsConsultedExactlyOncePerClose()
	{
		int calls = 0;
		ImGuiApp.Config = new ImGuiAppConfig
		{
			OnClosing = () =>
			{
				calls++;
				return false;
			},
		};

		ImGuiApp.ShouldClose();

		Assert.AreEqual(1, calls);
	}

	/// <summary>
	/// SetWindowTitle is safe from any thread, so off the window thread the write is queued rather
	/// than run inline, and the window can be torn down before the queued work runs. The write must
	/// drop rather than fault.
	/// </summary>
	/// <remarks>
	/// Exercised directly instead of by staging the thread race: the Invoker runs work inline for
	/// its own thread, so reaching the queued path means owning the Invoker from another thread,
	/// and Invoke then blocks the caller until that thread pumps — leaving no window in which a
	/// test can null the field. Calling the applier is the same code under the same precondition.
	/// </remarks>
	[TestMethod]
	public void ApplyingAQueuedTitleAfterTheWindowIsGoneDoesNothing()
	{
		ImGuiApp.window = null;

		ImGuiApp.ApplyWindowTitle("Queued");

		Assert.AreEqual(ImGuiApp.Config.Title, ImGuiApp.WindowTitle);
	}

	/// <summary>
	/// A cancelled close must not tear anything down: the Closing handler frees the pinned font
	/// data, the controller, the input context and the GL context, none of which is reversible, so
	/// a cancelled close would afterwards be rendering into a released context.
	/// </summary>
	[TestMethod]
	public void ACancelledCloseSkipsTeardown()
	{
		ImGuiApp.Config = new ImGuiAppConfig { OnClosing = () => false };
		ImGuiApp.SetupWindowClosingHandler();

		GCHandle pinned = GCHandle.Alloc(new byte[] { 1, 2, 3 }, GCHandleType.Pinned);
		ImGuiApp.currentPinnedFontData.Add(pinned);
		try
		{
			TestHelpers.SimulateClosing(mockWindow.Object);

			Assert.AreEqual(1, ImGuiApp.currentPinnedFontData.Count,
				"The pinned font data is still held, so teardown did not run.");
			Assert.IsFalse(mockWindow.Object.IsClosing, "The close was cancelled.");
		}
		finally
		{
			if (ImGuiApp.currentPinnedFontData.Remove(pinned) && pinned.IsAllocated)
			{
				pinned.Free();
			}
		}
	}

	/// <summary>
	/// An allowed close runs the teardown the cancelled one skips.
	/// </summary>
	[TestMethod]
	public void AnAllowedCloseRunsTeardown()
	{
		ImGuiApp.Config = new ImGuiAppConfig { OnClosing = () => true };
		ImGuiApp.SetupWindowClosingHandler();

		// Freed by the teardown this test is asserting runs.
		ImGuiApp.currentPinnedFontData.Add(GCHandle.Alloc(new byte[] { 1, 2, 3 }, GCHandleType.Pinned));

		TestHelpers.SimulateClosing(mockWindow.Object);

		Assert.AreEqual(0, ImGuiApp.currentPinnedFontData.Count,
			"Teardown ran and released the pinned font data.");
		Assert.IsTrue(mockWindow.Object.IsClosing, "Nothing cancelled the close.");
	}

	[TestMethod]
	public void ADeclinedCloseCanBeFollowedByAnAcceptedOne()
	{
		bool allow = false;
		ImGuiApp.Config = new ImGuiAppConfig { OnClosing = () => allow };

		Assert.IsFalse(ImGuiApp.ShouldClose(), "First close is vetoed, as if prompting the user.");

		allow = true;
		Assert.IsTrue(ImGuiApp.ShouldClose(), "Once the user confirms, the close goes through.");
	}
}
