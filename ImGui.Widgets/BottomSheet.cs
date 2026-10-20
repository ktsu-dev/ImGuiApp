// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Probes;
using ktsu.ImGui.Widgets.Overlays;

/// <summary>
/// Provides custom ImGui widgets.
/// </summary>
public static partial class ImGuiWidgets
{
	/// <summary>
	/// A panel that slides up from the bottom of the window and rests at a peek, half or full height,
	/// with a handle to drag it between them.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The sheet is drawn through an <see cref="OverlayHost"/>, on <see cref="OverlayLayer.BottomSheet"/>,
	/// so call the host's <see cref="OverlayHost.Render"/> once a frame. While it is open everything behind
	/// it is dimmed and cannot be clicked. Dragging the handle moves the sheet, and letting go springs it
	/// to the nearest height, taking a flick's speed into account. Dragging it below half the peek height,
	/// a tap on the dimming or Escape closes it.
	/// </para>
	/// <para>
	/// The content is drawn in a child window below the handle, given the size it has, which shrinks as
	/// the sheet goes down. The sheet is probe-marked under its id and the handle under <c>id/handle</c>.
	/// </para>
	/// </remarks>
	/// <param name="host">The overlay host that draws the sheet.</param>
	/// <param name="id">A unique id for the sheet.</param>
	/// <param name="content">Draws the sheet's content, given the size of the area it fills.</param>
	public sealed class BottomSheet(OverlayHost host, string id, Action<Vector2> content)
	{
		private readonly BottomSheetState state = new();
		private readonly string key = $"{nameof(BottomSheet)}/{id}";
		private float dragVelocity;
		private bool opening;

		/// <summary>Gets or sets the peek height, in pixels at 100% interface scale.</summary>
		public float PeekHeight { get; set; } = 120f;

		/// <summary>Gets or sets the half height, as a fraction of the window's height.</summary>
		public float HalfFraction { get; set; } = 0.5f;

		/// <summary>Gets or sets the full height, as a fraction of the window's height.</summary>
		public float FullFraction { get; set; } = 0.9f;

		/// <summary>Gets or sets the widest the sheet grows, in pixels at 100% interface scale.</summary>
		public float MaxWidth { get; set; } = 640f;

		/// <summary>Gets a value indicating whether the sheet is open.</summary>
		public bool IsOpen => state.IsOpen;

		/// <summary>Gets the detent the sheet is resting at or heading for, or null when it is closed.</summary>
		public BottomSheetDetent? Detent => state.Detent;

		/// <summary>Opens the sheet, or moves an open one, to a detent.</summary>
		/// <param name="detent">Where it rests.</param>
		public void Open(BottomSheetDetent detent = BottomSheetDetent.Half)
		{
			opening |= !state.IsVisible;
			state.Open(detent);
			host.Show(key, Draw, OverlayLayer.BottomSheet);
		}

		/// <summary>Sends the sheet down and away.</summary>
		public void Close() => state.Close();

		private BottomSheetHeights Heights()
		{
			float height = ImGui.GetMainViewport().Size.Y;
			float full = height * Math.Clamp(FullFraction, 0f, 1f);
			float half = MathF.Min(full, height * Math.Clamp(HalfFraction, 0f, 1f));
			return new BottomSheetHeights(MathF.Min(half, InterfaceScale.Pixels(PeekHeight)), half, full);
		}

		private void Draw()
		{
			BottomSheetHeights heights = Heights();
			if (state.Update(ImGui.GetIO().DeltaTime, heights) || !state.IsVisible)
			{
				host.Dismiss(key);
				return;
			}

			OverlaySurface.BeginFullscreen($"##{key}", opening);
			opening = false;
			OverlaySurface.DrawScrim(heights.Half > 0f ? Math.Clamp(state.Height / heights.Half, 0f, 1f) : 1f);

			ImGuiViewportPtr viewport = ImGui.GetMainViewport();
			float width = MathF.Min(viewport.Size.X, InterfaceScale.Pixels(MaxWidth));
			Vector2 sheetMin = new(viewport.Pos.X + ((viewport.Size.X - width) * 0.5f), viewport.Pos.Y + viewport.Size.Y - state.Height);
			Vector2 sheetMax = new(sheetMin.X + width, viewport.Pos.Y + viewport.Size.Y);
			ImGuiProbes.MarkRegion(id, sheetMin, sheetMax);

			if (state.IsOpen && !state.IsDragging && (OverlaySurface.ScrimPressed(sheetMin, sheetMax) || ImGui.IsKeyPressed(ImGuiKey.Escape)))
			{
				state.Close();
			}

			ImDrawListPtr drawList = ImGui.GetWindowDrawList();
			drawList.AddRectFilled(sheetMin, sheetMax, OverlaySurface.Opaque(ImGuiCol.WindowBg), InterfaceScale.Pixels(16f), ImDrawFlags.RoundCornersTop);

			float handleHeight = InterfaceScale.Pixels(24f);
			DrawHandle(drawList, sheetMin, width, handleHeight, heights);

			float contentHeight = state.Height - handleHeight;
			if (contentHeight >= 1f)
			{
				ImGui.SetCursorScreenPos(sheetMin + new Vector2(0f, handleHeight));
				Vector2 contentSize = new(width, contentHeight);
				ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, ImGui.GetStyle().WindowPadding);
				if (ImGui.BeginChild("##content", contentSize, ImGuiChildFlags.AlwaysUseWindowPadding, ImGuiWindowFlags.NoBackground))
				{
					content(ImGui.GetContentRegionAvail());
				}

				ImGui.EndChild();
				ImGui.PopStyleVar();
			}

			ImGui.End();
		}

		private void DrawHandle(ImDrawListPtr drawList, Vector2 sheetMin, float width, float handleHeight, BottomSheetHeights heights)
		{
			ImGui.SetCursorScreenPos(sheetMin);
			ImGui.InvisibleButton("##handle", new Vector2(width, handleHeight));
			ImGuiProbes.MarkItem(id, "handle");
			WidgetCursor.OnLastItem(ImGuiMouseCursor.ResizeNs);

			if (ImGui.IsItemActivated())
			{
				dragVelocity = 0f;
				state.BeginDrag();
			}

			if (ImGui.IsItemActive())
			{
				ImGuiIOPtr io = ImGui.GetIO();
				state.Drag(io.MouseDelta.Y, heights);
				if (io.DeltaTime > 0f)
				{
					dragVelocity = (dragVelocity * 0.6f) + (io.MouseDelta.Y / io.DeltaTime * 0.4f);
				}
			}

			if (ImGui.IsItemDeactivated())
			{
				state.Release(dragVelocity, heights);
			}

			Vector2 pillSize = new(InterfaceScale.Pixels(36f), InterfaceScale.Pixels(5f));
			Vector2 pillMin = sheetMin + new Vector2((width - pillSize.X) * 0.5f, (handleHeight - pillSize.Y) * 0.5f);
			ImGuiCol pill = ImGui.IsItemActive() || ImGui.IsItemHovered() ? ImGuiCol.Text : ImGuiCol.TextDisabled;
			drawList.AddRectFilled(pillMin, pillMin + pillSize, ImGui.GetColorU32(pill), pillSize.Y * 0.5f);
		}
	}
}
