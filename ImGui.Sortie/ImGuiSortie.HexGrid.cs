// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Sortie;

using System.Collections.Generic;
using System.Numerics;
using Hexa.NET.ImGui;
using ktsu.ImGui.Probes;

public static partial class ImGuiSortie
{
	/// <summary>
	/// Draws a field of flat-topped hexagons, odd columns half a cell lower, as many as fit the size. Cells in
	/// <paramref name="lit"/> are filled with their lamp colour and the hovered cell is outlined.
	/// </summary>
	/// <param name="label">Id of the grid, also its probe name.</param>
	/// <param name="size">Grid size; zero width fills the available width.</param>
	/// <param name="radius">Distance from a cell's centre to its corners.</param>
	/// <param name="lit">Which cells are lit, and in what colour. May be null.</param>
	/// <param name="clicked">The cell clicked this frame, if any.</param>
	/// <returns>True on the frame a cell is clicked.</returns>
	public static bool HexGrid(string label, Vector2 size, float radius, IReadOnlyDictionary<HexCell, SortieLamp>? lit, out HexCell clicked)
	{
		Ensure.NotNull(label);
		clicked = default;
		SortiePalette palette = SortieStyle.Palette;
		float r = MathF.Max(radius, 3f);
		Vector2 area = SortieDraw.Resolve(size, r * 8f);
		Vector2 min = ImGui.GetCursorScreenPos();
		ImGui.InvisibleButton(label, area);
		bool hovered = ImGui.IsItemHovered();
		bool pressed = ImGui.IsItemClicked();
		ImGuiProbes.MarkItem(label);

		float stepX = r * 1.5f;
		float cellHeight = r * SortieDraw.Sqrt3;
		int columns = Math.Max(1, (int)((area.X - (r * 0.5f)) / stepX));
		int rows = Math.Max(1, (int)((area.Y - (cellHeight * 0.5f)) / cellHeight));
		Vector2 mouse = ImGui.GetMousePos();
		HexCell? hoveredCell = hovered ? CellAt(mouse - min, r, columns, rows) : null;

		ImDrawListPtr drawList = ImGui.GetWindowDrawList();
		drawList.PushClipRect(min, min + area, true);
		for (int column = 0; column < columns; column++)
		{
			for (int row = 0; row < rows; row++)
			{
				HexCell cell = new(column, row);
				Vector2 center = min + CellCenter(cell, r);
				Vector2[] outer = SortieDraw.Hexagon(center, r - 1f);
				if (lit is not null && lit.TryGetValue(cell, out SortieLamp lamp) && lamp != SortieLamp.Off)
				{
					SortieDraw.FillConvex(drawList, SortieDraw.Hexagon(center, r - 2.5f), palette.LampColor(lamp));
				}

				foreach (Vector2 corner in outer)
				{
					drawList.PathLineTo(corner);
				}

				drawList.PathStroke(SortieDraw.U32(palette.LeadDim), ImDrawFlags.Closed, 1f);
			}
		}

		if (hoveredCell is HexCell target)
		{
			foreach (Vector2 corner in SortieDraw.Hexagon(min + CellCenter(target, r), r - 1f))
			{
				drawList.PathLineTo(corner);
			}

			drawList.PathStroke(SortieDraw.U32(palette.LeadBright), ImDrawFlags.Closed, 2f);
			ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
			if (pressed)
			{
				clicked = target;
			}
		}

		drawList.PopClipRect();
		return pressed && hoveredCell.HasValue;
	}

	/// <summary>Gives a cell's centre relative to the grid's top left.</summary>
	/// <param name="cell">The cell.</param>
	/// <param name="radius">Cell radius.</param>
	/// <returns>Its centre.</returns>
	internal static Vector2 CellCenter(HexCell cell, float radius)
	{
		float height = radius * SortieDraw.Sqrt3;
		float x = radius + (cell.Column * radius * 1.5f);
		float y = (height / 2f) + (cell.Row * height) + ((cell.Column & 1) == 1 ? height / 2f : 0f);
		return new Vector2(x, y);
	}

	/// <summary>Finds the cell containing a point, by taking the nearest cell centre among the candidates around it.</summary>
	/// <param name="point">The point, relative to the grid's top left.</param>
	/// <param name="radius">Cell radius.</param>
	/// <param name="columns">Columns in the grid.</param>
	/// <param name="rows">Rows in the grid.</param>
	/// <returns>The cell, or null when the point falls outside every cell.</returns>
	internal static HexCell? CellAt(Vector2 point, float radius, int columns, int rows)
	{
		int guess = (int)MathF.Floor((point.X - radius) / (radius * 1.5f));
		HexCell? best = null;
		float bestDistance = float.MaxValue;
		for (int column = guess - 1; column <= guess + 2; column++)
		{
			if (column < 0 || column >= columns)
			{
				continue;
			}

			float height = radius * SortieDraw.Sqrt3;
			float shift = (column & 1) == 1 ? height / 2f : 0f;
			int rowGuess = (int)MathF.Floor((point.Y - shift) / height);
			for (int row = rowGuess - 1; row <= rowGuess + 1; row++)
			{
				if (row < 0 || row >= rows)
				{
					continue;
				}

				HexCell cell = new(column, row);
				float distance = Vector2.DistanceSquared(point, CellCenter(cell, radius));
				if (distance < bestDistance)
				{
					bestDistance = distance;
					best = cell;
				}
			}
		}

		// Inside the inscribed circle is always inside; between it and the corners is close enough for a pointer.
		return bestDistance <= radius * radius ? best : null;
	}
}
