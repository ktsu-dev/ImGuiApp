// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Tests;

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
