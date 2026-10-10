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

	/// <summary>The space between a <see cref="Framed"/> box's border and what is drawn inside it.</summary>
	public const float FramePadding = 6f;

	/// <summary>
	/// Draws content inside a bordered child region, so alignment helpers have a box to align within,
	/// with a caption centred underneath saying which helper it is.
	/// </summary>
	/// <remarks>
	/// The box is padded by <see cref="FramePadding"/>, so text an alignment helper leaves at an edge
	/// does not sit on the border. A helper that takes the box size explicitly should be given
	/// <see cref="Inner"/> of it, the region the padding leaves.
	/// </remarks>
	/// <param name="id">The child's id.</param>
	/// <param name="caption">What is drawn under the box.</param>
	/// <param name="size">The box.</param>
	/// <param name="content">What to draw inside it.</param>
	public static void Framed(string id, string caption, Vector2 size, Action content)
	{
		ImGui.BeginGroup();
		ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(FramePadding));

		if (ImGui.BeginChild(id, size, ImGuiChildFlags.Borders, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
		{
			content();
		}

		ImGui.EndChild();
		ImGui.PopStyleVar();

		float captionWidth = ImGui.CalcTextSize(caption).X;
		ImGui.SetCursorPosX(ImGui.GetCursorPosX() + MathF.Max(0f, (size.X - captionWidth) / 2f));
		ImGui.TextDisabled(caption);
		ImGui.EndGroup();
	}

	/// <summary>Gets the region inside a <see cref="Framed"/> box once its padding is taken off.</summary>
	/// <param name="size">The box.</param>
	/// <returns>The padded region.</returns>
	public static Vector2 Inner(Vector2 size) => size - new Vector2(FramePadding * 2f);
}
