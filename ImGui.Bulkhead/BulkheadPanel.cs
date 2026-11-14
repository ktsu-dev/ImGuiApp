// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Bulkhead;

using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Probes;
using ktsu.ScopedAction;

/// <summary>
/// A framed panel opened by <see cref="ImGuiBulkhead.Panel"/>. Its frame is drawn when it opens;
/// <see cref="Sidebar"/> and <see cref="Content"/> open the two regions inside it, and disposing it
/// closes the panel and reserves its space in the window.
/// </summary>
/// <remarks>
/// A panel whose <see cref="Sidebar"/> is never opened draws a plain block in the column instead.
/// The two regions can be opened in either order, each at most once, and both have to be closed
/// before the panel is.
/// </remarks>
public sealed class BulkheadPanel : ScopedAction
{
	private readonly Vector2 origin;
	private readonly Vector2 columnMin;
	private readonly Vector2 columnSize;
	private readonly Vector2 contentMin;
	private readonly Vector2 contentSize;
	private readonly float gap;
	private readonly uint columnColor;
	private bool sidebarOpened;
	private bool contentOpened;
	private bool closed;

	internal BulkheadPanel(string title, Vector2 size, BulkheadPanelOptions options)
	{
		float frame = ImGui.GetFrameHeight();
		Vector2 avail = ImGui.GetContentRegionAvail();
		Vector2 resolved = new(
			size.X > 0f ? size.X : Math.Max(1f, avail.X + size.X),
			size.Y > 0f ? size.Y : Math.Max(1f, avail.Y + size.Y));
		PanelLayout layout = PanelLayout.For(resolved, frame, options);

		Vector4 color = options.Color ?? BulkheadPalette.Amber;
		columnColor = ImGui.GetColorU32(options.ColumnColor ?? BulkheadPalette.Shade(color, -0.25f));
		origin = ImGui.GetCursorScreenPos();
		gap = options.Gap;
		columnMin = origin + new Vector2(0f, layout.TopElbow.Y + options.Gap);
		columnSize = new Vector2(layout.ColumnWidth, layout.ColumnHeight);
		contentMin = origin + layout.ContentOffset;
		contentSize = layout.ContentSize;

		ImDrawListPtr drawList = ImGui.GetWindowDrawList();
		uint background = ImGui.GetColorU32(BulkheadPalette.Background);

		// Top elbow, with the title cut into its bar on a black ground.
		ImGuiBulkhead.DrawElbow(drawList, origin, layout.TopElbow, BulkheadCorner.TopLeft, layout.ColumnWidth, layout.BarThickness, color, null);
		string text = BulkheadDraw.DisplayText(title);
		Vector2 textSize = ImGui.CalcTextSize(text);
		float textRight = origin.X + resolved.X - (layout.BarThickness * 1.5f);
		Vector2 boxMin = new(textRight - textSize.X - (frame * 0.5f), origin.Y);
		Vector2 boxMax = new(textRight + (frame * 0.25f), origin.Y + layout.BarThickness);
		if (boxMin.X > origin.X + layout.ColumnWidth + layout.BarThickness)
		{
			drawList.AddRectFilled(boxMin, boxMax, background);
			drawList.AddText(new Vector2(boxMin.X + (frame * 0.25f), origin.Y + ((layout.BarThickness - textSize.Y) * 0.5f)), ImGui.GetColorU32(color), text);
		}

		// Bottom elbow.
		Vector2 bottomOrigin = origin + new Vector2(0f, resolved.Y - layout.BottomElbow.Y);
		ImGuiBulkhead.DrawElbow(drawList, bottomOrigin, layout.BottomElbow, BulkheadCorner.BottomLeft, layout.ColumnWidth, layout.BarThickness, color, null);

		ImGui.PushID(title);

		OnClose = () =>
		{
			closed = true;

			// With no sidebar, the column is one plain block.
			if (!sidebarOpened && columnSize.Y > 0f)
			{
				drawList.AddRectFilled(columnMin, columnMin + columnSize, columnColor);
			}

			ImGui.PopID();
			ImGui.SetCursorScreenPos(origin);
			ImGui.Dummy(resolved);
			ImGuiProbes.MarkItem(title);
		};
	}

	/// <summary>
	/// Opens the column down the left of the panel for the caller's own controls, usually
	/// <see cref="ImGuiBulkhead.Button"/>s as wide as it is, stacked one gap apart.
	/// </summary>
	/// <returns>The region scope; dispose it before the panel.</returns>
	/// <exception cref="InvalidOperationException">The sidebar was already opened, or the panel is closed.</exception>
	public BulkheadPanelRegion Sidebar()
	{
		EnsureOpen(sidebarOpened, nameof(Sidebar));
		sidebarOpened = true;
		return new BulkheadPanelRegion("##sidebar", columnMin, columnSize, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoBackground, new Vector2(0f, gap));
	}

	/// <summary>Opens the panel's content region, right of the column and between the bars.</summary>
	/// <returns>The region scope; dispose it before the panel.</returns>
	/// <exception cref="InvalidOperationException">The content was already opened, or the panel is closed.</exception>
	public BulkheadPanelRegion Content()
	{
		EnsureOpen(contentOpened, nameof(Content));
		contentOpened = true;
		return new BulkheadPanelRegion("##content", contentMin, contentSize, ImGuiWindowFlags.NoBackground, null);
	}

	private void EnsureOpen(bool alreadyOpened, string region)
	{
		if (closed)
		{
			throw new InvalidOperationException($"The panel is closed; open its {region} inside its using block.");
		}

		if (alreadyOpened)
		{
			throw new InvalidOperationException($"The panel's {region} is already open; a panel has one.");
		}
	}
}
