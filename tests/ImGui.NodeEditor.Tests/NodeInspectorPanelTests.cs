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
	/// <summary>The enum an enum-typed pin is driven with.</summary>
	private enum Mode
	{
		Fast,
		Slow,
	}

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

	/// <summary>
	/// A connected pin's value arrives along its link, so editing the stored literal would change
	/// nothing anyone reads. The row is still drawn (dropping it would show less than the node has),
	/// just disabled.
	/// </summary>
	[TestMethod]
	public void Draw_ForAConnectedPin_ShowsARowButDisablesIt()
	{
		Node source = engine.CreateNodeFromSpecs(
			new Vector2(0, 0),
			"Source",
			[],
			[new PinSpec("Value", typeof(double), 1.0)]);
		Node target = engine.CreateNodeFromSpecs(
			new Vector2(200, 0),
			"Blob Filter",
			[new PinSpec("Threshold", typeof(double), 128.0)],
			[]);
		LinkCreationResult link = engine.TryCreateLink(source.OutputPins[0].Id, target.InputPins[0].Id);
		Assert.IsTrue(link.Success, link.Message);

		inspected = target.Id;
		Start();

		Assert.IsTrue(IsVisible("Threshold"));

		// Disabled items do not respond to input, so the honest check is driving the same edit the
		// unconnected-pin test performs and confirming nothing landed, rather than asserting on
		// ImGui's disabled visual state.
		harness.Click("Threshold");
		harness.Step(1);
		harness.Keyboard.Press(ImGuiKey.A, ctrl: true);
		harness.Keyboard.Type("999");
		harness.Keyboard.Press(ImGuiKey.Enter);
		harness.Step(3);

		Assert.AreEqual(128.0, engine.GetPinValue(target.InputPins[0].Id));
	}

	/// <summary>
	/// A pin whose type <see cref="PinValueKinds.Classify"/> does not cover is shown, not omitted,
	/// but has no working editor. <c>long</c> is the documented example: Dear ImGui has no
	/// <c>InputLong</c>.
	/// </summary>
	[TestMethod]
	public void Draw_ForAnUnsupportedTypePin_ShowsARowButDisablesIt()
	{
		Node node = engine.CreateNodeFromSpecs(
			new Vector2(0, 0),
			"Blob Filter",
			[new PinSpec("BigCount", typeof(long), 5L)],
			[]);
		inspected = node.Id;
		Start();

		Assert.IsTrue(IsVisible("BigCount"));

		harness.Click("BigCount");
		harness.Step(1);
		harness.Keyboard.Press(ImGuiKey.A, ctrl: true);
		harness.Keyboard.Type("999");
		harness.Keyboard.Press(ImGuiKey.Enter);
		harness.Step(3);

		Assert.AreEqual(5L, engine.GetPinValue(node.InputPins[0].Id));
	}

	/// <summary>
	/// An enum pin gets a genuinely working editor here, the same as the renderer's inline one, so
	/// the two surfaces cannot disagree about a type <see cref="PinValueKinds.Classify"/> says is
	/// editable.
	/// </summary>
	[TestMethod]
	public void Draw_EditingAnEnumRow_WritesThroughToTheEngine()
	{
		Node node = engine.CreateNodeFromSpecs(
			new Vector2(0, 0),
			"Blob Filter",
			[new PinSpec("Mode", typeof(Mode), Mode.Fast)],
			[]);
		inspected = node.Id;
		Start();

		Assert.IsTrue(IsVisible("Mode"));

		// The combo's popup sits directly below the combo, padded by the window padding, with one
		// selectable per option spaced by the item spacing. This is the same geometry
		// ktsu.ImGui.Widgets.UITests.ComboTests uses, read live from the style so aiming at an option
		// needs no hard-coded coordinate.
		float rowPitch = ImGui.GetTextLineHeightWithSpacing();
		float popupPadding = ImGui.GetStyle().WindowPadding.Y;
		Rectangle rect = harness.Probe.Rect("Mode")!.Value;

		harness.Click("Mode");
		harness.Mouse.Click(rect.MinX + 12f, rect.MaxY + popupPadding + (rowPitch * 1.5f));
		harness.Step(3);

		Assert.AreEqual(Mode.Slow, engine.GetPinValue(node.InputPins[0].Id));
	}
}
