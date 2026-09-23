// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.NodeEditor.Tests;

using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.App;
using ktsu.ImGui.App.Testing;
using ktsu.ImGui.NodeEditor;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Drives <see cref="NodeInspectorPanel"/>, which draws one node's parameters as a property grid.
/// It needs a real context because a property grid is a table, and a table that never opened draws
/// no rows.
/// </summary>
[TestClass]
public sealed class NodeInspectorPanelTests
{
	private static readonly HarnessOptions Viewport = new() { Width = 700, Height = 500 };

	private readonly NodeEditorEngine engine = new();

	private ImGuiAppHarness harness = null!;
	private int inspected;

	[TestCleanup]
	public void TearDown() => harness?.Dispose();

	private void Start()
	{
		harness = ImGuiAppHarness.Start(
			new ImGuiAppConfig
			{
				Title = nameof(NodeInspectorPanelTests),
				OnRender = _ => NodeInspectorPanel.Draw(engine, inspected),
				SaveIniSettings = false,
			},
			Viewport);

		harness.Step(3);
	}

	private bool IsVisible(string name) => harness.Probe.WasSeenInFrame(name, harness.FrameCount - 1);

	[TestMethod]
	public void Draw_ShowsARowForEachInputPin()
	{
		Node node = engine.CreateNodeFromSpecs(
			new Vector2(0, 0),
			"Blob Filter",
			[new PinSpec("Threshold", typeof(double), 128.0), new PinSpec("AreaMin", typeof(double), 50.0)],
			[new PinSpec("Count", typeof(int))]);
		inspected = node.Id;
		Start();

		Assert.IsTrue(IsVisible("Threshold"));
		Assert.IsTrue(IsVisible("AreaMin"));
	}

	/// <summary>
	/// There is no evaluator to give an output pin a value, per #440, so an output row would be a
	/// box showing nothing.
	/// </summary>
	[TestMethod]
	public void Draw_ShowsNoRowForAnOutputPin()
	{
		Node node = engine.CreateNodeFromSpecs(
			new Vector2(0, 0),
			"Blob Filter",
			[new PinSpec("Threshold", typeof(double), 128.0)],
			[new PinSpec("Count", typeof(int))]);
		inspected = node.Id;
		Start();

		Assert.IsFalse(IsVisible("Count"));
	}

	[TestMethod]
	public void Draw_ForANodeThatDoesNotExist_DrawsNothingAndDoesNotThrow()
	{
		inspected = 999;
		Start();
		harness.Step(3);

		Assert.AreEqual(0, ImGui.GetCurrentContext().ErrorCountCurrentFrame, "ImGui reported the panel's drawing as misuse.");
	}

	[TestMethod]
	public void Draw_EditingARow_WritesThroughToTheEngine()
	{
		Node node = engine.CreateNodeFromSpecs(
			new Vector2(0, 0),
			"Blob Filter",
			[new PinSpec("Threshold", typeof(double), 128.0)],
			[]);
		inspected = node.Id;
		Start();

		// The grid's double row is an InputDouble, so clicking it places the caret in text that is
		// already there. Without the select-all, typing appends to "128.000000" instead of replacing
		// it.
		harness.Click("Threshold");
		harness.Step(1);
		harness.Keyboard.Press(ImGuiKey.A, ctrl: true);
		harness.Keyboard.Type("200");
		harness.Keyboard.Press(ImGuiKey.Enter);
		harness.Step(3);

		Assert.AreEqual(200.0, engine.GetPinValue(node.InputPins[0].Id));
	}
}
