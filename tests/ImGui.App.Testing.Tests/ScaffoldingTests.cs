// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Testing.Tests;

using ktsu.ImGui.App;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class ScaffoldingTests
{
	[TestMethod]
	public void TestingAssembly_ReferencesImGuiApp() =>
		// The harness is a plain consumer of ktsu.ImGui.App's public surface rather than a friend
		// assembly, so this only needs to prove the reference resolves at runtime.
		Assert.AreEqual("ktsu.ImGui.App", typeof(ImGuiApp).Assembly.GetName().Name);

	[TestMethod]
	public void Invoker_IsNullBeforeAnySessionStarts() =>
		// Documents a constraint the harness has to work around rather than a behavior to rely on.
		// ImGuiApp.Invoker is assigned during Start and its setter is internal, so a harness that
		// never calls Start has to be given a way to install one. See the external frame session
		// API added alongside RenderFrameContents.
		Assert.IsNull(ImGuiApp.Invoker, "Invoker is only assigned once a session starts.");
}
