// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System.Globalization;

using Hexa.NET.ImGui;

public static partial class ImGuiWidgets
{
	/// <summary>
	/// The clipper setup and row naming both virtualized tables share, so their rows scroll and are
	/// addressed the same way.
	/// </summary>
	internal static class TableClipping
	{
		/// <summary>Begins a clipper over a table's rows.</summary>
		/// <param name="clipper">The clipper to begin.</param>
		/// <param name="rowCount">How many rows there are in all.</param>
		/// <param name="rowHeight">The height of every row, or zero to let the clipper measure the first.</param>
		/// <param name="forcedRow">A row to draw whether or not it's visible, or -1 for none.</param>
		internal static void Begin(ref ImGuiListClipper clipper, int rowCount, float rowHeight, int forcedRow)
		{
			// An explicit height lets the clipper skip its measuring pass. Left to ImGui when there isn't
			// one, rather than guessed at from the font: a guess that disagreed with the real row pitch
			// would put every skipped row at the wrong offset.
			if (rowHeight > 0f)
			{
				clipper.Begin(rowCount, rowHeight);
			}
			else
			{
				clipper.Begin(rowCount);
			}

			// A row far outside the visible range isn't drawn, and a scroll anchored on a row can only
			// happen if the row is drawn. This is what makes a scroll land on the first frame.
			if (forcedRow >= 0)
			{
				clipper.IncludeItemByIndex(forcedRow);
			}
		}

		/// <summary>
		/// Builds a row's probe name, in the <c>Tags/[0]</c> shape the property grid's lists already use,
		/// so a bracketed index means the same thing across the library.
		/// </summary>
		internal static string RowName(string label, int row) =>
			string.Create(CultureInfo.InvariantCulture, $"{label}/[{row}]");
	}
}
