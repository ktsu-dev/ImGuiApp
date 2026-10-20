// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Testing.Tests;

using System;
using System.Collections.Generic;

using ktsu.ImGui.App;
using ktsu.ScopedAction;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// <see cref="ImGuiApp.RenderFrameContents"/> is the frame body the application and the harness
/// share, so the order it calls an application's callbacks in is what every theme installed through
/// <see cref="ImGuiAppConfig.FrameWrapperFactory"/> sees.
/// </summary>
[TestClass]
public sealed class FrameContentsTests
{
	private static readonly string[] ExpectedOrder = ["open", "menu", "render", "close"];

	/// <summary>A frame wrapper that records when it opens and closes.</summary>
	private sealed class RecordingScope(Action onOpen, Action onClose) : ScopedAction(onOpen, onClose);

	[TestMethod]
	public void RenderFrameContents_InvokesTheFrameWrapperAroundTheCallbacks()
	{
		List<string> calls = [];
		ImGuiAppConfig config = new()
		{
			FrameWrapperFactory = () => new RecordingScope(() => calls.Add("open"), () => calls.Add("close")),
			OnAppMenu = () => calls.Add("menu"),
			OnRender = _ => calls.Add("render"),
		};

		using ImGuiAppHarness harness = ImGuiAppHarness.Start(config, new HarnessOptions { Width = 320, Height = 240 });
		harness.Step();

		CollectionAssert.AreEqual(
			ExpectedOrder,
			calls,
			"The wrapper must open before the menu and the window contents, and close after both.");
	}
}
