// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Bulkhead;

using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ScopedAction;

/// <summary>
/// One region of a <see cref="BulkheadPanel"/>, a child window that everything drawn inside its
/// scope lands in. Dispose it to close the child window.
/// </summary>
public sealed class BulkheadPanelRegion : ScopedAction
{
	internal BulkheadPanelRegion(string id, Vector2 min, Vector2 size, ImGuiWindowFlags flags, Vector2? itemSpacing)
	{
		// A panel too small for the region still opens one, a pixel square, so whatever the caller
		// draws inside the scope is clipped away rather than spilling into the window.
		ImGui.SetCursorScreenPos(min);
		Vector2 childSize = new(Math.Max(1f, size.X), Math.Max(1f, size.Y));
		IsVisible = ImGui.BeginChild(id, childSize, ImGuiChildFlags.None, flags);
		if (itemSpacing is Vector2 spacing)
		{
			ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, spacing);
		}

		OnClose = () =>
		{
			if (itemSpacing is not null)
			{
				ImGui.PopStyleVar();
			}

			ImGui.EndChild();
		};
	}

	/// <summary>
	/// Gets a value indicating whether the region is on screen. Drawing is safe either way; this is
	/// for skipping work that costs something to prepare.
	/// </summary>
	public bool IsVisible { get; }
}
