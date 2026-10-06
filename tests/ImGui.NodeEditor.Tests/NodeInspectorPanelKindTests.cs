// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.NodeEditor.Tests;

using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.App;
using ktsu.ImGui.App.Testing;
using ktsu.ImGui.NodeEditor;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// One test per <see cref="PinValueKind"/> on the inspector panel, driven through the widget that
/// kind's row actually draws.
/// </summary>
/// <remarks>
/// The design's load-bearing claim is that a type gets an editor on both surfaces or on neither.
/// Asserting that <see cref="PinValueKinds.Classify"/> returns a kind says nothing about whether the
/// row behind it works, so each kind is edited here and in
/// <c>InlinePinEditorTests</c> and the resulting value read back off the engine.
/// <para>
/// Where a click has to land was measured rather than assumed. A grid row marks the widget itself,
/// so the probe rectangle is the input box — except for a multi-component box, where the rectangle
/// spans every component and the one being typed into has to be aimed at by fraction.
/// </para>
/// </remarks>
[TestClass]
public sealed class NodeInspectorPanelKindTests
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

	/// <summary>Creates a one-input node of the given type and starts a harness inspecting it.</summary>
	private int StartWith(string label, Type dataType, object? initial)
	{
		Node node = engine.CreateNodeFromSpecs(
			Vector2.Zero,
			"Blob Filter",
			[new PinSpec(label, dataType, initial)],
			[]);
		inspected = node.Id;

		harness = ImGuiAppHarness.Start(
			new ImGuiAppConfig
			{
				Title = nameof(NodeInspectorPanelKindTests),
				OnRender = _ => NodeInspectorPanel.Draw(engine, inspected),
				SaveIniSettings = false,
			},
			Viewport);

		harness.Step(3);
		return node.InputPins[0].Id;
	}

	/// <summary>Selects a text row's contents, types a replacement, and commits it.</summary>
	private void Retype(string text)
	{
		harness.Keyboard.Press(ImGuiKey.A, ctrl: true);
		harness.Keyboard.Type(text);
		harness.Keyboard.Press(ImGuiKey.Enter);
		harness.Step(3);
	}

	/// <summary>
	/// Clicks into one component of a multi-component row, which the probe records as a single
	/// rectangle spanning them all.
	/// </summary>
	private void ClickComponent(string label, float fraction)
	{
		Rectangle rect = harness.Probe.Rect(label)!.Value;
		harness.Mouse.Click(rect.MinX + (rect.Width * fraction), (rect.MinY + rect.MaxY) * 0.5f);
		harness.Step(1);
	}

	[TestMethod]
	public void Boolean_TogglesOnClick()
	{
		int pinId = StartWith("Invert", typeof(bool), false);

		harness.Click("Invert");
		harness.Step(2);

		Assert.AreEqual(true, engine.GetPinValue(pinId));
	}

	[TestMethod]
	public void Int32_TakesATypedValue()
	{
		int pinId = StartWith("Count", typeof(int), 5);

		harness.Click("Count");
		harness.Step(1);
		Retype("42");

		Assert.AreEqual(42, engine.GetPinValue(pinId));
	}

	[TestMethod]
	public void Single_TakesATypedValue()
	{
		int pinId = StartWith("Sigma", typeof(float), 2f);

		harness.Click("Sigma");
		harness.Step(1);
		Retype("7.5");

		Assert.AreEqual(7.5f, engine.GetPinValue(pinId));
	}

	[TestMethod]
	public void Double_TakesATypedValue()
	{
		int pinId = StartWith("Threshold", typeof(double), 128.0);

		harness.Click("Threshold");
		harness.Step(1);
		Retype("200");

		Assert.AreEqual(200.0, engine.GetPinValue(pinId));
	}

	[TestMethod]
	public void String_TakesATypedValue()
	{
		int pinId = StartWith("Label", typeof(string), "before");

		harness.Click("Label");
		harness.Step(1);
		Retype("after");

		Assert.AreEqual("after", engine.GetPinValue(pinId));
	}

	/// <summary>
	/// An <c>InputFloat2</c> is two boxes inside one item, so typing into the left quarter edits X
	/// and leaves Y alone. Both halves are asserted: a row that wrote a whole new vector would pass
	/// the first and fail the second.
	/// </summary>
	[TestMethod]
	public void Vector2_TakesATypedComponent()
	{
		int pinId = StartWith("Offset", typeof(Vector2), new Vector2(1f, 2f));

		ClickComponent("Offset", 0.25f);
		Retype("9");

		Assert.AreEqual(new Vector2(9f, 2f), engine.GetPinValue(pinId));
	}

	[TestMethod]
	public void Vector3_TakesATypedComponent()
	{
		int pinId = StartWith("Scale", typeof(Vector3), new Vector3(1f, 2f, 3f));

		ClickComponent("Scale", 1f / 6f);
		Retype("9");

		Assert.AreEqual(new Vector3(9f, 2f, 3f), engine.GetPinValue(pinId));
	}

	/// <summary>
	/// The combo's popup sits directly below it, padded by the window padding, with one selectable
	/// per option spaced by the item spacing — the geometry
	/// <c>ktsu.ImGui.Widgets.UITests.ComboTests</c> uses, read live from the style.
	/// </summary>
	[TestMethod]
	public void Enum_TakesAPickedName()
	{
		int pinId = StartWith("Mode", typeof(Mode), Mode.Fast);

		float rowPitch = ImGui.GetTextLineHeightWithSpacing();
		float popupPadding = ImGui.GetStyle().WindowPadding.Y;
		Rectangle rect = harness.Probe.Rect("Mode")!.Value;

		harness.Click("Mode");
		harness.Mouse.Click(rect.MinX + 12f, rect.MaxY + popupPadding + (rowPitch * 1.5f));
		harness.Step(3);

		Assert.AreEqual(Mode.Slow, engine.GetPinValue(pinId));
	}

	/// <summary>
	/// The ninth kind. A pin nothing can edit is still drawn — dropping it would show less than the
	/// node has — but the row is disabled, so the same edit every other test here makes lands
	/// nowhere. <c>long</c> is the documented example: Dear ImGui has no <c>InputLong</c>.
	/// </summary>
	[TestMethod]
	public void Unsupported_ShowsADisabledRow()
	{
		int pinId = StartWith("BigCount", typeof(long), 5L);

		Assert.IsTrue(harness.Probe.WasSeenInFrame("BigCount", harness.FrameCount - 1));

		harness.Click("BigCount");
		harness.Step(1);
		Retype("999");

		Assert.AreEqual(5L, engine.GetPinValue(pinId));
	}
}
