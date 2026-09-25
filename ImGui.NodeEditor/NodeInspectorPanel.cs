// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.NodeEditor;

using System.Globalization;
using System.Numerics;
using System.Reflection;

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
					DrawEnum(grid, engine, pin, label, current, editable);
					break;

				case PinValueKind.Unsupported:
				default:
					// Shown, not omitted: a pin with no editor is still part of the node.
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

	/// <summary>The open generic <c>PropertyGrid.Enum&lt;TEnum&gt;</c> method, resolved once.</summary>
	private static readonly MethodInfo EnumRowMethod = typeof(ImGuiWidgets.PropertyGrid).GetMethod(nameof(ImGuiWidgets.PropertyGrid.Enum))
		?? throw new MissingMethodException(nameof(ImGuiWidgets.PropertyGrid), nameof(ImGuiWidgets.PropertyGrid.Enum));

	/// <summary>
	/// <see cref="EnumRowMethod"/> closed over one enum type, cached so a row drawn every frame does
	/// not call <see cref="MethodInfo.MakeGenericMethod"/> every frame.
	/// </summary>
	/// <remarks>
	/// Not thread-safe, and does not need to be: like the rest of this library it relies on ImGui's
	/// own single-threaded model, where drawing happens on one thread and never concurrently with
	/// itself.
	/// </remarks>
	private static readonly Dictionary<Type, MethodInfo> EnumRowMethodsByType = [];

	/// <summary>
	/// Draws an enum pin through the grid's own <c>Enum&lt;TEnum&gt;</c> row, reached via reflection
	/// because the pin's enum type is only known here as a runtime <see cref="Type"/>.
	/// </summary>
	/// <remarks>
	/// Going through <see cref="ImGuiWidgets.PropertyGrid.Enum{TEnum}"/> rather than drawing a combo
	/// by hand keeps this row inside the grid's own <c>BeginRow</c>/<c>EndRow</c> plumbing, so it
	/// gets the same column layout, probe mark and disabled handling every other row gets for free.
	/// A null pin (a <c>Polarity?</c> that was never set, say) is seeded through
	/// <see cref="UndefinedSentinel"/> rather than <c>Activator.CreateInstance</c>: the latter
	/// produces the enum's zero value, which — whenever a member happens to be defined at zero,
	/// the common case — renders as though the user had already picked that name.
	/// </remarks>
	private static void DrawEnum(ImGuiWidgets.PropertyGrid grid, NodeEditorEngine engine, Pin pin, string label, object? current, bool editable)
	{
		Type enumType = Nullable.GetUnderlyingType(pin.DataType!) ?? pin.DataType!;

		if (!EnumRowMethodsByType.TryGetValue(enumType, out MethodInfo? method))
		{
			method = EnumRowMethod.MakeGenericMethod(enumType);
			EnumRowMethodsByType[enumType] = method;
		}

		object value = current ?? UndefinedSentinel(enumType);
		object?[] arguments = [label, value];

		bool changed = (bool)method.Invoke(grid, arguments)!;
		if (changed && editable)
		{
			engine.SetPinValue(pin.Id, arguments[1]);
		}
	}

	/// <summary>
	/// A value of <paramref name="enumType"/> that matches none of its defined members.
	/// </summary>
	/// <remarks>
	/// <see cref="ImGuiWidgets.PropertyGrid.Enum{TEnum}"/> looks up the row's current value with
	/// <c>Array.IndexOf</c> and hands the result straight to <c>ImGui.Combo</c>, which itself renders
	/// a blank preview for an index outside the item list. Handing it a value no member owns gets
	/// that blank preview for free, rather than misrepresenting an unset pin as a chosen one.
	/// </remarks>
	private static object UndefinedSentinel(Type enumType)
	{
		Array definedValues = Enum.GetValues(enumType);
		HashSet<long> defined = new(definedValues.Length);

		foreach (object definedValue in definedValues)
		{
			try
			{
				defined.Add(Convert.ToInt64(definedValue, CultureInfo.InvariantCulture));
			}
			catch (OverflowException)
			{
				// An underlying value outside long's range can never collide with the small
				// non-negative candidates tried below.
			}
		}

		// Among any n+1 candidates there is one no set of n defined values can occupy.
		for (long candidate = 0; candidate <= defined.Count; candidate++)
		{
			if (!defined.Contains(candidate))
			{
				return Enum.ToObject(enumType, candidate);
			}
		}

		return Enum.ToObject(enumType, 0);
	}
}
