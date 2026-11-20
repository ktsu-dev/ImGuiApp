// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Bulkhead;

using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Styler;

/// <summary>
/// Applies the Bulkhead look to ImGui as a whole: the <see cref="BulkheadTheme"/> colours plus the
/// shapes (pill-rounded frames, square windows, no borders) that let standard ImGui controls sit
/// beside the Bulkhead widgets.
/// </summary>
public static class BulkheadStyle
{
	/// <summary>Applies the look for the rest of the session, as <c>Theme.Apply</c> does.</summary>
	public static void Apply()
	{
		Theme.Apply(BulkheadTheme.Instance);
		ImGuiStylePtr style = ImGui.GetStyle();
		foreach ((ImGuiStyleVar variable, float value) in FloatVariables)
		{
			SetFloat(style, variable, value);
		}

		style.ItemSpacing = ItemSpacing;
		style.FramePadding = FramePadding;
		style.Colors[(int)ImGuiCol.WindowBg] = BulkheadPalette.Background;
		style.Colors[(int)ImGuiCol.ChildBg] = Vector4.Zero;
	}

	/// <summary>
	/// Applies the look until the returned scope is disposed, for one window or panel inside an
	/// application that otherwise looks different.
	/// </summary>
	/// <returns>The scope; dispose it to restore the previous look.</returns>
	public static BulkheadStyleScope Scope() => new();

	internal static readonly Vector2 ItemSpacing = new(6f, 6f);
	internal static readonly Vector2 FramePadding = new(10f, 4f);

	internal static readonly (ImGuiStyleVar Variable, float Value)[] FloatVariables =
	[
		(ImGuiStyleVar.WindowRounding, 0f),
		(ImGuiStyleVar.ChildRounding, 0f),
		(ImGuiStyleVar.FrameRounding, 12f),
		(ImGuiStyleVar.GrabRounding, 12f),
		(ImGuiStyleVar.PopupRounding, 8f),
		(ImGuiStyleVar.ScrollbarRounding, 12f),
		(ImGuiStyleVar.TabRounding, 10f),
		(ImGuiStyleVar.WindowBorderSize, 0f),
		(ImGuiStyleVar.FrameBorderSize, 0f),
		(ImGuiStyleVar.ChildBorderSize, 0f),
	];

	private static void SetFloat(ImGuiStylePtr style, ImGuiStyleVar variable, float value)
	{
		switch (variable)
		{
			case ImGuiStyleVar.WindowRounding:
				style.WindowRounding = value;
				break;
			case ImGuiStyleVar.ChildRounding:
				style.ChildRounding = value;
				break;
			case ImGuiStyleVar.FrameRounding:
				style.FrameRounding = value;
				break;
			case ImGuiStyleVar.GrabRounding:
				style.GrabRounding = value;
				break;
			case ImGuiStyleVar.PopupRounding:
				style.PopupRounding = value;
				break;
			case ImGuiStyleVar.ScrollbarRounding:
				style.ScrollbarRounding = value;
				break;
			case ImGuiStyleVar.TabRounding:
				style.TabRounding = value;
				break;
			case ImGuiStyleVar.WindowBorderSize:
				style.WindowBorderSize = value;
				break;
			case ImGuiStyleVar.FrameBorderSize:
				style.FrameBorderSize = value;
				break;
			case ImGuiStyleVar.ChildBorderSize:
				style.ChildBorderSize = value;
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(variable), variable, "Not a Bulkhead style variable.");
		}
	}
}
