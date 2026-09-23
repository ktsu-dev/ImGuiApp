// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.NodeEditor;

using System.Linq;
using System.Numerics;

using ktsu.ImGui.Widgets;

/// <summary>
/// Draws one node's parameters as a two-column property grid.
/// </summary>
/// <remarks>
/// The panel is told which node to inspect rather than asking what is selected, so it holds no
/// ImNodes state and can be drawn without a renderer. <see cref="NodeEditorRenderer.SelectedNodeIds"/>
/// is where a host gets the id from.
/// <para>
/// Which pins are editable is decided by <see cref="PinValueKinds"/>, the same classifier the
/// renderer's inline editors use, so a type has an editor on both surfaces or on neither.
/// </para>
/// </remarks>
public static class NodeInspectorPanel
{
	/// <summary>
	/// Draw the parameters of one node.
	/// </summary>
	/// <param name="engine">The engine holding the graph and its values.</param>
	/// <param name="nodeId">The node to inspect.</param>
	/// <returns>True if any row changed its value this frame.</returns>
	/// <remarks>
	/// Only input pins are drawn. An output pin's value would come from evaluating the graph, which
	/// this library does not do.
	/// <para>
	/// A connected pin's row is drawn disabled, because its value arrives along the link and editing
	/// the stored literal would change nothing anyone reads. A pin whose type has no editor is drawn
	/// disabled too, rather than omitted, so the panel does not quietly show less than the node has.
	/// </para>
	/// </remarks>
	public static bool Draw(NodeEditorEngine engine, int nodeId)
	{
		Ensure.NotNull(engine);

		Node? node = engine.Nodes.FirstOrDefault(n => n.Id == nodeId);
		if (node is null)
		{
			return false;
		}

		using ImGuiWidgets.PropertyGrid grid = new($"NodeInspector{nodeId}");

		foreach (Pin pin in node.InputPins)
		{
			DrawRow(grid, engine, pin);
		}

		return grid.Changed;
	}

	private static void DrawRow(ImGuiWidgets.PropertyGrid grid, NodeEditorEngine engine, Pin pin)
	{
		PinValueKind kind = PinValueKinds.Classify(pin.DataType);
		bool editable = kind != PinValueKind.Unsupported && !engine.IsPinConnected(pin.Id);

		using (new ScopedDisable(!editable))
		{
			string label = pin.EffectiveDisplayName;
			object? current = engine.GetPinValue(pin.Id);

			switch (kind)
			{
				case PinValueKind.Boolean:
					DrawBoolean(grid, engine, pin, label, current, editable);
					break;

				case PinValueKind.Int32:
					DrawInt32(grid, engine, pin, label, current, editable);
					break;

				case PinValueKind.Single:
					DrawSingle(grid, engine, pin, label, current, editable);
					break;

				case PinValueKind.Double:
					DrawDouble(grid, engine, pin, label, current, editable);
					break;

				case PinValueKind.String:
					DrawString(grid, engine, pin, label, current, editable);
					break;

				case PinValueKind.Vector2:
					DrawVector2(grid, engine, pin, label, current, editable);
					break;

				case PinValueKind.Vector3:
					DrawVector3(grid, engine, pin, label, current, editable);
					break;

				case PinValueKind.Enum:
				case PinValueKind.Unsupported:
				default:
					// Shown, not omitted: a pin with no editor is still part of the node. The enum
					// case has no editor either. The grid's Enum row is generic over the enum type,
					// which is only known here as a Type, so it is shown as text rather than reflected
					// into that overload.
					DrawUnsupported(grid, label, current);
					break;
			}
		}
	}

	private static void DrawBoolean(ImGuiWidgets.PropertyGrid grid, NodeEditorEngine engine, Pin pin, string label, object? current, bool editable)
	{
		bool value = current as bool? ?? false;
		if (grid.Value(label, ref value) && editable)
		{
			engine.SetPinValue(pin.Id, value);
		}
	}

	private static void DrawInt32(ImGuiWidgets.PropertyGrid grid, NodeEditorEngine engine, Pin pin, string label, object? current, bool editable)
	{
		int value = current as int? ?? 0;
		if (grid.Value(label, ref value) && editable)
		{
			engine.SetPinValue(pin.Id, value);
		}
	}

	private static void DrawSingle(ImGuiWidgets.PropertyGrid grid, NodeEditorEngine engine, Pin pin, string label, object? current, bool editable)
	{
		float value = current as float? ?? 0f;
		if (grid.Value(label, ref value) && editable)
		{
			engine.SetPinValue(pin.Id, value);
		}
	}

	private static void DrawDouble(ImGuiWidgets.PropertyGrid grid, NodeEditorEngine engine, Pin pin, string label, object? current, bool editable)
	{
		double value = current as double? ?? 0.0;
		if (grid.Value(label, ref value) && editable)
		{
			engine.SetPinValue(pin.Id, value);
		}
	}

	private static void DrawString(ImGuiWidgets.PropertyGrid grid, NodeEditorEngine engine, Pin pin, string label, object? current, bool editable)
	{
		string value = current as string ?? string.Empty;
		if (grid.Value(label, ref value) && editable)
		{
			engine.SetPinValue(pin.Id, value);
		}
	}

	private static void DrawVector2(ImGuiWidgets.PropertyGrid grid, NodeEditorEngine engine, Pin pin, string label, object? current, bool editable)
	{
		Vector2 value = current as Vector2? ?? Vector2.Zero;
		if (grid.Value(label, ref value) && editable)
		{
			engine.SetPinValue(pin.Id, value);
		}
	}

	private static void DrawVector3(ImGuiWidgets.PropertyGrid grid, NodeEditorEngine engine, Pin pin, string label, object? current, bool editable)
	{
		Vector3 value = current as Vector3? ?? Vector3.Zero;
		if (grid.Value(label, ref value) && editable)
		{
			engine.SetPinValue(pin.Id, value);
		}
	}

	private static void DrawUnsupported(ImGuiWidgets.PropertyGrid grid, string label, object? current)
	{
		string value = current?.ToString() ?? string.Empty;
		grid.Value(label, ref value);
	}
}
