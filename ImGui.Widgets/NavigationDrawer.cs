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
	/// A panel that slides in from the left edge of the window over everything else, opened from code
	/// or by dragging in from the edge, as a phone's navigation menu is.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The drawer is drawn through an <see cref="OverlayHost"/>, on <see cref="OverlayLayer.Drawer"/>, so
	/// call the host's <see cref="OverlayHost.Render"/> once a frame. To let a drag from the window's left
	/// edge pull it out, call <see cref="TrackEdgeSwipe"/> once a frame as well; a press within
	/// <see cref="EdgeWidth"/> of the edge that then moves right picks the drawer up, and it follows the
	/// pointer. Letting go springs it open or shut by how far out it is and how fast it was moving.
	/// </para>
	/// <para>
	/// While it is open everything behind it is dimmed and cannot be clicked. A tap on the dimming,
	/// dragging the dimming left, or Escape closes it. The panel is probe-marked under the drawer's id.
	/// </para>
	/// </remarks>
	/// <param name="host">The overlay host that draws the drawer.</param>
	/// <param name="id">A unique id for the drawer.</param>
	/// <param name="content">Draws the drawer's content, in a child window the drawer's size.</param>
	public sealed class NavigationDrawer(OverlayHost host, string id, Action content)
	{
		private readonly NavigationDrawerState state = new();
		private readonly string key = $"{nameof(NavigationDrawer)}/{id}";
		private Vector2 pressAt;
		private bool edgePressed;
		private bool scrimPressed;
		private float dragVelocity;
		private bool opening;

		/// <summary>Gets or sets the drawer's width, in pixels at 100% interface scale. It never takes more than 85% of the window.</summary>
		public float Width { get; set; } = 280f;

		/// <summary>Gets or sets how wide the strip along the edge that a drag can start in is, in pixels at 100% interface scale.</summary>
		public float EdgeWidth { get; set; } = 20f;

		/// <summary>Gets a value indicating whether the drawer is open, or opening.</summary>
		public bool IsOpen => state.IsOpen;

		/// <summary>Opens the drawer.</summary>
		public void Open()
		{
			opening |= !state.IsVisible;
			state.Open();
			host.Show(key, Draw, OverlayLayer.Drawer);
		}

		/// <summary>Closes the drawer.</summary>
		public void Close() => state.Close();

		/// <summary>Opens the drawer when it is shut, and closes it when it is open.</summary>
		public void Toggle()
		{
			if (state.IsOpen)
			{
				Close();
			}
			else
			{
				Open();
			}
		}

		/// <summary>
		/// Watches for a drag in from the window's left edge, and picks the drawer up when one starts.
		/// Call once a frame, outside the drawer's content and before the host's <see cref="OverlayHost.Render"/>.
		/// </summary>
		/// <returns>True on the frame a drag picks the drawer up.</returns>
		public bool TrackEdgeSwipe()
		{
			if (state.IsVisible)
			{
				edgePressed = false;
				return false;
			}

			Vector2 mouse = ImGui.GetMousePos();
			if (ImGui.IsMouseClicked(ImGuiMouseButton.Left)
				&& NavigationDrawerState.InEdgeZone(mouse.X, ImGui.GetMainViewport().Pos.X, InterfaceScale.Pixels(EdgeWidth)))
			{
				edgePressed = true;
				pressAt = mouse;
			}

			if (!edgePressed)
			{
				return false;
			}

			Vector2 moved = mouse - pressAt;
			if (!ImGui.IsMouseDown(ImGuiMouseButton.Left) || MathF.Abs(moved.Y) > Slop())
			{
				edgePressed = false;
				return false;
			}

			if (moved.X <= Slop())
			{
				return false;
			}

			edgePressed = false;
			dragVelocity = 0f;
			state.BeginDrag();

			// The drawer follows this frame's movement itself when it draws, later in the frame.
			state.Drag(moved.X - ImGui.GetIO().MouseDelta.X, PanelWidth());
			host.Show(key, Draw, OverlayLayer.Drawer);
			return true;
		}

		private static float Slop() => InterfaceScale.Pixels(8f);

		private float PanelWidth() => MathF.Min(InterfaceScale.Pixels(Width), ImGui.GetMainViewport().Size.X * 0.85f);

		private void Draw()
		{
			float width = PanelWidth();
			FollowPointer(width);
			if (state.Update(ImGui.GetIO().DeltaTime) || !state.IsVisible)
			{
				host.Dismiss(key);
				return;
			}

			OverlaySurface.BeginFullscreen($"##{key}", opening);
			opening = false;
			OverlaySurface.DrawScrim(state.Openness);

			ImGuiViewportPtr viewport = ImGui.GetMainViewport();
			Vector2 panelMin = new(viewport.Pos.X - (width * (1f - state.Openness)), viewport.Pos.Y);
			Vector2 panelMax = new(panelMin.X + width, viewport.Pos.Y + viewport.Size.Y);
			ImGuiProbes.MarkRegion(id, panelMin, panelMax);

			if (state.IsOpen && !state.IsDragging && OverlaySurface.ScrimPressed(panelMin, panelMax))
			{
				scrimPressed = true;
				pressAt = ImGui.GetMousePos();
			}

			if (state.IsOpen && ImGui.IsKeyPressed(ImGuiKey.Escape))
			{
				state.Close();
			}

			ImGui.GetWindowDrawList().AddRectFilled(panelMin, panelMax, OverlaySurface.Opaque(ImGuiCol.WindowBg));
			ImGui.GetWindowDrawList().AddLine(new Vector2(panelMax.X, panelMin.Y), panelMax, ImGui.GetColorU32(ImGuiCol.Border));

			ImGui.SetCursorScreenPos(panelMin);
			ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, ImGui.GetStyle().WindowPadding);
			if (ImGui.BeginChild("##drawer", panelMax - panelMin, ImGuiChildFlags.AlwaysUseWindowPadding, ImGuiWindowFlags.NoBackground))
			{
				content();
			}

			ImGui.EndChild();
			ImGui.PopStyleVar();
			ImGui.End();
		}

		// A press on the dimming either closes the drawer, when it is let go where it was pressed, or
		// picks the drawer up, when it moves sideways first. A drag from the edge carries on here once
		// the drawer is showing.
		private void FollowPointer(float width)
		{
			ImGuiIOPtr io = ImGui.GetIO();
			bool down = ImGui.IsMouseDown(ImGuiMouseButton.Left);

			if (scrimPressed && !state.IsDragging)
			{
				if (!down)
				{
					scrimPressed = false;
					state.Close();
				}
				else if (MathF.Abs(ImGui.GetMousePos().X - pressAt.X) > Slop())
				{
					scrimPressed = false;
					dragVelocity = 0f;
					state.BeginDrag();
					state.Drag(ImGui.GetMousePos().X - pressAt.X - io.MouseDelta.X, width);
				}
			}

			if (!state.IsDragging)
			{
				return;
			}

			if (down)
			{
				state.Drag(io.MouseDelta.X, width);
				if (io.DeltaTime > 0f)
				{
					dragVelocity = (dragVelocity * 0.6f) + (io.MouseDelta.X / io.DeltaTime * 0.4f);
				}
			}
			else
			{
				state.Release(dragVelocity, width);
			}
		}
	}
}
