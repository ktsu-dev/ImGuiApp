// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Bulkhead;

using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Styler;
using ktsu.ScopedAction;

/// <summary>
/// The Bulkhead look, applied until the scope is disposed. Returned by <see cref="BulkheadStyle.Scope"/>.
/// </summary>
public sealed class BulkheadStyleScope : ScopedAction
{
	/// <summary>Initializes a new instance of the <see cref="BulkheadStyleScope"/> class, pushing the look.</summary>
	public BulkheadStyleScope()
	{
		IReadOnlyDictionary<ImGuiCol, Vector4> themeColors = Theme.GetColorMapping(BulkheadTheme.Instance);
		foreach ((ImGuiCol column, Vector4 color) in themeColors)
		{
			ImGui.PushStyleColor(column, color);
		}

		foreach ((ImGuiStyleVar variable, float value) in BulkheadStyle.FloatVariables)
		{
			ImGui.PushStyleVar(variable, value);
		}

		ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, BulkheadStyle.ItemSpacing);
		ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, BulkheadStyle.FramePadding);
		ImGui.PushStyleColor(ImGuiCol.WindowBg, BulkheadPalette.Background);
		ImGui.PushStyleColor(ImGuiCol.ChildBg, Vector4.Zero);

		OnClose = () =>
		{
			ImGui.PopStyleColor(themeColors.Count + 2);
			ImGui.PopStyleVar(BulkheadStyle.FloatVariables.Length + 2);
		};
	}
}
