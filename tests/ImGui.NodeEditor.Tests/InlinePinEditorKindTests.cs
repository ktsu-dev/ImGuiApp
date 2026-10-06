// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.NodeEditor.Tests;

using System;
using System.Collections.Generic;
using System.Numerics;

using Hexa.NET.ImGui;
using Hexa.NET.ImNodes;

using ktsu.ImGui.App;
using ktsu.ImGui.App.Testing;
using ktsu.ImGui.NodeEditor;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// One test per <see cref="PinValueKind"/> on the node face, driven through the widget that kind's
/// inline editor actually draws.
/// </summary>
/// <remarks>
/// The companion to <see cref="NodeInspectorPanelKindTests"/>. Together they turn "a type has an
/// editor on both surfaces or on neither" into something the suite checks rather than something
/// <see cref="PinValueKinds.Classify"/> merely asserts about itself.
/// <para>
/// Nothing on a node face is marked for the probe — ImNodes lays these out and the renderer submits
/// raw ImGui — so every click here is aimed by geometry, read live from the style. The row starts
/// flush at the node's left content edge, so an editor is found by walking out past the node
/// padding, the label, and the spacing <c>SameLine()</c> leaves. A checkbox is the one editor that
/// ignores <see cref="NodeEditorRenderer.InlineEditorWidth"/>, being a square of the frame height,
/// so it is aimed at separately.
/// </para>
/// </remarks>
[TestClass]
public sealed class InlinePinEditorKindTests
{
	/// <summary>The enum an enum-typed pin is driven with.</summary>
	private enum Mode
	{
		Fast,
		Slow,
	}

	private static readonly HarnessOptions Viewport = new() { Width = 900, Height = 700 };

	private readonly NodeEditorEngine engine = new();
	private readonly NodeEditorRenderer renderer = new();

	private ImGuiAppHarness harness = null!;
	private Node node = null!;

	[TestCleanup]
	public void TearDown() => harness?.Dispose();

	/// <summary>Creates a one-input node of the given type and starts a harness drawing it.</summary>
	private int StartWith(string label, Type dataType, object? initial)
	{
		node = engine.CreateNodeFromSpecs(
			new Vector2(250, 200),
			"Blob Filter",
			[new PinSpec(label, dataType, initial)],
			[]);

		harness = ImGuiAppHarness.Start(
			new ImGuiAppConfig
			{
				Title = nameof(InlinePinEditorKindTests),
				OnRender = _ => DrawGraph(),
				SaveIniSettings = false,
			},
			Viewport);

		harness.Step(5);
		return node.InputPins[0].Id;
	}

	private void DrawGraph()
	{
		renderer.Render(engine, ImGui.GetContentRegionAvail());

		foreach (KeyValuePair<int, Vector2> update in renderer.GetNodePositionUpdates(engine))
		{
			engine.UpdateNodePosition(update.Key, update.Value);
		}

		foreach (KeyValuePair<int, Vector2> update in renderer.GetNodeDimensionUpdates(engine))
		{
			engine.UpdateNodeDimensions(update.Key, update.Value);
		}
	}

	/// <summary>Where the editor on the node's one input row starts, and how tall that row sits.</summary>
	private (float Left, float Y) EditorOrigin()
	{
		Assert.IsTrue(renderer.TryGetNodeScreenRect(node.Id, out ScreenRect rect));
		Assert.IsTrue(renderer.TryGetPinScreenPosition(node.InputPins[0].Id, out Vector2 pin));

		float labelWidth = ImGui.CalcTextSize(node.InputPins[0].EffectiveDisplayName).X;
		float nodePadding = ImNodes.GetStyle().NodePadding.X;
		float itemSpacing = ImGui.GetStyle().ItemSpacing.X;

		return (rect.Min.X + nodePadding + labelWidth + itemSpacing, pin.Y);
	}

	/// <summary>A point the given fraction of the way across the editor's own width.</summary>
	private Vector2 EditorPoint(float fraction)
	{
		(float left, float y) = EditorOrigin();
		return new Vector2(left + (renderer.InlineEditorWidth * renderer.Zoom * fraction), y);
	}

	/// <summary>Selects a text editor's contents, types a replacement, and commits it.</summary>
	private void Retype(string text)
	{
		harness.Keyboard.Press(ImGuiKey.A, ctrl: true);
		harness.Keyboard.Type(text);
		harness.Keyboard.Press(ImGuiKey.Enter);
		harness.Step(3);
	}

	/// <summary>Clicks into one component of an editor, then retypes it.</summary>
	private void RetypeComponent(float fraction, string text)
	{
		Vector2 point = EditorPoint(fraction);
		harness.Mouse.Click(point.X, point.Y);
		harness.Step(1);
		Retype(text);
	}

	/// <summary>
	/// A checkbox is a square of the frame height rather than the item width every other editor
	/// takes, so it sits at the start of the editor rather than across it.
	/// </summary>
	[TestMethod]
	public void Boolean_TogglesOnClick()
	{
		int pinId = StartWith("Invert", typeof(bool), false);

		(float left, float y) = EditorOrigin();
		harness.Mouse.Click(left + (ImGui.GetFrameHeight() * 0.5f), y);
		harness.Step(3);

		Assert.AreEqual(true, engine.GetPinValue(pinId));
	}

	/// <summary>
	/// An <c>int</c> pin draws a <c>DragInt</c>, whose interaction is scrubbing rather than typing.
	/// Dragging right raises the value; the exact amount is the drag's own business, so only the
	/// direction is asserted.
	/// </summary>
	[TestMethod]
	public void Int32_ScrubsOnDrag()
	{
		int pinId = StartWith("Count", typeof(int), 5);

		Vector2 start = EditorPoint(0.25f);
		harness.Mouse.Drag(start.X, start.Y, start.X + 40f, start.Y);
		harness.Step(3);

		Assert.IsGreaterThan(5, (int)engine.GetPinValue(pinId)!, "Dragging right should have raised the value.");
	}

	[TestMethod]
	public void Single_ScrubsOnDrag()
	{
		int pinId = StartWith("Sigma", typeof(float), 2f);

		Vector2 start = EditorPoint(0.25f);
		harness.Mouse.Drag(start.X, start.Y, start.X + 40f, start.Y);
		harness.Step(3);

		Assert.IsGreaterThan(2f, (float)engine.GetPinValue(pinId)!, "Dragging right should have raised the value.");
	}

	/// <summary>
	/// A <c>double</c> pin draws an <c>InputDouble</c> with a step of zero, which is a plain text
	/// field: dragging across it only extends a selection, so it is committed by typing.
	/// </summary>
	[TestMethod]
	public void Double_TakesATypedValue()
	{
		int pinId = StartWith("Threshold", typeof(double), 128.0);

		RetypeComponent(0.5f, "200");

		Assert.AreEqual(200.0, engine.GetPinValue(pinId));
	}

	[TestMethod]
	public void String_TakesATypedValue()
	{
		int pinId = StartWith("Label", typeof(string), "before");

		RetypeComponent(0.5f, "after");

		Assert.AreEqual("after", engine.GetPinValue(pinId));
	}

	/// <summary>
	/// An <c>InputFloat2</c> is two boxes inside one item, so typing into the left quarter edits X
	/// and leaves Y alone. Both halves are asserted: an editor that wrote a whole new vector would
	/// pass the first and fail the second.
	/// </summary>
	[TestMethod]
	public void Vector2_TakesATypedComponent()
	{
		int pinId = StartWith("Offset", typeof(Vector2), new Vector2(1f, 2f));

		RetypeComponent(0.25f, "9");

		Assert.AreEqual(new Vector2(9f, 2f), engine.GetPinValue(pinId));
	}

	[TestMethod]
	public void Vector3_TakesATypedComponent()
	{
		int pinId = StartWith("Scale", typeof(Vector3), new Vector3(1f, 2f, 3f));

		RetypeComponent(1f / 6f, "9");

		Assert.AreEqual(new Vector3(9f, 2f, 3f), engine.GetPinValue(pinId));
	}

	/// <summary>
	/// A combo is the one editor whose target is not on the node at all: the popup is its own window,
	/// laid out with padding a caller cannot read back off the style. So it is found rather than
	/// computed — the pixels that changed below the combo when it opened are the popup, and the
	/// second of two options sits three quarters of the way down it.
	/// </summary>
	/// <remarks>
	/// The same tactic the widget suites use for a vendor widget that marks nothing, and it is honest
	/// about what it measures: if the popup ever stops being drawn, the bounds come back empty and
	/// the test says so rather than failing on a coordinate that happened to miss.
	/// </remarks>
	[TestMethod]
	public void Enum_TakesAPickedName()
	{
		int pinId = StartWith("Mode", typeof(Mode), Mode.Fast);

		Vector2 combo = EditorPoint(0.5f);
		float comboBottom = combo.Y + (ImGui.GetFrameHeight() * 0.5f);

		Rgba32[] closed = Snapshot();
		harness.Mouse.Click(combo.X, combo.Y);
		harness.Step(2);
		Rectangle popup = ChangedBoundsBelow(closed, Snapshot(), comboBottom);

		Assert.IsGreaterThan(0, popup.Height, "The combo did not open, so there is no popup to pick from.");

		harness.Mouse.Click(popup.MinX + 8f, popup.MinY + (popup.Height * 0.75f));
		harness.Step(3);

		Assert.AreEqual(Mode.Slow, engine.GetPinValue(pinId));
	}

	/// <summary>Every pixel of the current frame, for comparing against a later one.</summary>
	private Rgba32[] Snapshot()
	{
		Bitmap32 target = harness.Target;
		Rgba32[] copy = new Rgba32[target.Width * target.Height];

		for (int y = 0; y < target.Height; y++)
		{
			for (int x = 0; x < target.Width; x++)
			{
				copy[(y * target.Width) + x] = target.GetPixel(x, y);
			}
		}

		return copy;
	}

	/// <summary>
	/// The rectangle covering everything that changed between two frames below a given line.
	/// </summary>
	/// <param name="before">The earlier frame.</param>
	/// <param name="after">The later one.</param>
	/// <param name="top">Rows above this are ignored, which is what keeps the combo's own highlight
	/// out of the answer.</param>
	/// <returns>The changed rectangle, or a degenerate one when nothing changed.</returns>
	private Rectangle ChangedBoundsBelow(Rgba32[] before, Rgba32[] after, float top)
	{
		int width = harness.Target.Width;
		int firstRow = (int)MathF.Ceiling(top);
		int minX = int.MaxValue;
		int minY = int.MaxValue;
		int maxX = -1;
		int maxY = -1;

		for (int y = firstRow; y < harness.Target.Height; y++)
		{
			for (int x = 0; x < width; x++)
			{
				if (before[(y * width) + x].Equals(after[(y * width) + x]))
				{
					continue;
				}

				minX = Math.Min(minX, x);
				minY = Math.Min(minY, y);
				maxX = Math.Max(maxX, x);
				maxY = Math.Max(maxY, y);
			}
		}

		return maxX < 0 ? default : new Rectangle(minX, minY, maxX, maxY);
	}

	/// <summary>
	/// The ninth kind. Nothing here can edit a <c>long</c> — Dear ImGui has no <c>InputLong</c> — so
	/// no editor is drawn at all, which shows up as the node being no wider than its label alone.
	/// </summary>
	[TestMethod]
	public void Unsupported_GetsNoEditor()
	{
		renderer.DrawInlinePinEditors = false;
		StartWith("BigCount", typeof(long), 5L);
		float withoutEditors = engine.Nodes[0].Dimensions.X;

		renderer.DrawInlinePinEditors = true;
		harness.Step(5);

		Assert.AreEqual(withoutEditors, engine.Nodes[0].Dimensions.X, "Nothing here can edit a long, so nothing should have been drawn.");
	}
}
