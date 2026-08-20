// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Tests;

using System;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class ExternalFrameSessionTests
{
	[TestCleanup]
	public void Cleanup()
	{
		// These tests mutate process-global state, so it is reset regardless of outcome.
		if (ImGuiApp.Invoker is not null)
		{
			ImGuiApp.EndExternalFrameSession();
		}
	}

	[TestMethod]
	public void BeginExternalFrameSession_AssignsAnInvoker()
	{
		ImGuiApp.BeginExternalFrameSession();

		Assert.IsNotNull(ImGuiApp.Invoker, "A host needs an invoker, or work marshaled from a worker thread is lost.");
	}

	[TestMethod]
	public void EndExternalFrameSession_ClearsTheInvoker()
	{
		ImGuiApp.BeginExternalFrameSession();
		ImGuiApp.EndExternalFrameSession();

		Assert.IsNull(ImGuiApp.Invoker);
	}

	[TestMethod]
	public void BeginExternalFrameSession_Twice_Throws()
	{
		ImGuiApp.BeginExternalFrameSession();

		// Silently replacing the invoker would strand anything already queued on the first one.
		Assert.ThrowsExactly<InvalidOperationException>(ImGuiApp.BeginExternalFrameSession);
	}

	[TestMethod]
	public void Invoker_RunsQueuedWorkWhenPumped()
	{
		ImGuiApp.BeginExternalFrameSession();
		bool ran = false;

		ImGuiApp.Invoker.Invoke(() => ran = true);

		Assert.IsTrue(ran, "Work invoked from the owning thread should run.");
	}

	[TestMethod]
	public void RenderFrameContents_NullConfig_Throws() =>
		Assert.ThrowsExactly<ArgumentNullException>(() => ImGuiApp.RenderFrameContents(null!, 0.016f));
}
