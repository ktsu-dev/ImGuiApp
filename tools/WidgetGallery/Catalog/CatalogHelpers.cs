// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.WidgetGallery.Catalog;

using System;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Probes;

/// <summary>Small pieces several tile groups share.</summary>
internal static class CatalogHelpers
{
	/// <summary>Difficulty levels, for the enum combos and the property grid.</summary>
	internal enum Difficulty
	{
		Easy,
		Normal,
		Hard,
		Nightmare,
	}

	/// <summary>Records the item just submitted under a name, so an interaction can find it.</summary>
	/// <param name="name">The name.</param>
	public static void Mark(string name) => ImGuiProbes.MarkItem(name);

	/// <summary>Draws content inside a bordered child region, so alignment helpers have a box to align within.</summary>
	/// <param name="id">The child's id.</param>
	/// <param name="size">The box.</param>
	/// <param name="content">What to draw inside it.</param>
	public static void Framed(string id, Vector2 size, Action content)
	{
		ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);

		if (ImGui.BeginChild(id, size, ImGuiChildFlags.Borders, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
		{
			content();
		}

		ImGui.EndChild();
		ImGui.PopStyleVar();
	}
}
