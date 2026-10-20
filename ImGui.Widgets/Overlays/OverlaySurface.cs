// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Overlays;

using System.Numerics;

using Hexa.NET.ImGui;

/// <summary>
/// The ImGui half of the overlays: a window over the whole main viewport, lifted above every other
/// window, and the dimming drawn behind a sheet or a drawer.
/// </summary>
internal static class OverlaySurface
{
	private const ImGuiWindowFlags FullscreenFlags = ImGuiWindowFlags.NoDecoration
		| ImGuiWindowFlags.NoSavedSettings
		| ImGuiWindowFlags.NoMove
		| ImGuiWindowFlags.NoResize
		| ImGuiWindowFlags.NoBackground
		| ImGuiWindowFlags.NoScrollWithMouse
		| ImGuiWindowFlags.NoNav;

	/// <summary>
	/// Begins a borderless window covering the main viewport and lifts it in front of every window
	/// drawn so far. The window blocks the pointer from everything beneath it, which is what a modal
	/// overlay wants. Always pair with <see cref="ImGui.End()"/>.
	/// </summary>
	/// <param name="name">The window's name; start it with <c>##</c> so nothing shows.</param>
	/// <param name="takeFocus">Whether to give the window keyboard focus this frame.</param>
	internal static void BeginFullscreen(string name, bool takeFocus)
	{
		ImGuiViewportPtr viewport = ImGui.GetMainViewport();
		ImGui.SetNextWindowPos(viewport.Pos);
		ImGui.SetNextWindowSize(viewport.Size);
		if (takeFocus)
		{
			ImGui.SetNextWindowFocus();
		}

		ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
		ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0f);
		ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0f);
		ImGui.Begin(name, FullscreenFlags);
		ImGui.PopStyleVar(3);
		BringToFront();
	}

	/// <summary>Lifts the current window in front of every window drawn so far, without focusing it.</summary>
	internal static void BringToFront() => ImGuiP.BringWindowToDisplayFront(ImGuiP.GetCurrentWindow());

	/// <summary>Dims the whole main viewport, in the theme's modal dimming colour.</summary>
	/// <param name="amount">How much of the dimming to draw, from 0 (none) to 1 (all).</param>
	internal static void DrawScrim(float amount)
	{
		if (amount <= 0f)
		{
			return;
		}

		ImGuiViewportPtr viewport = ImGui.GetMainViewport();
		ImGui.GetWindowDrawList().AddRectFilled(viewport.Pos, viewport.Pos + viewport.Size, Color(ImGuiCol.ModalWindowDimBg, amount));
	}

	/// <summary>
	/// Reports a press on the current overlay window that landed outside a panel, which is how a tap
	/// on the dimming reads.
	/// </summary>
	/// <param name="panelMin">The panel's top left corner.</param>
	/// <param name="panelMax">The panel's bottom right corner.</param>
	/// <returns>True on the frame the press lands.</returns>
	internal static bool ScrimPressed(Vector2 panelMin, Vector2 panelMax) =>
		ImGui.IsWindowHovered()
		&& ImGui.IsMouseClicked(ImGuiMouseButton.Left)
		&& !ImGui.IsMouseHoveringRect(panelMin, panelMax, false);

	/// <summary>Gets a style colour with its alpha scaled.</summary>
	/// <param name="color">The style colour.</param>
	/// <param name="alpha">How much of the colour's own alpha to keep.</param>
	/// <returns>The packed colour.</returns>
	internal static uint Color(ImGuiCol color, float alpha) => ImGui.GetColorU32(color, alpha);

	/// <summary>
	/// Gets a style colour made opaque, for a panel drawn over the page: themes give windows a little
	/// transparency, which over a page reads as the page showing through the panel.
	/// </summary>
	/// <param name="color">The style colour.</param>
	/// <param name="alpha">The alpha to draw at, typically how far the panel has faded in.</param>
	/// <returns>The packed colour.</returns>
	internal static uint Opaque(ImGuiCol color, float alpha = 1f)
	{
		Vector4 value = ImGui.GetStyle().Colors[(int)color];
		value.W = alpha * ImGui.GetStyle().Alpha;
		return ImGui.ColorConvertFloat4ToU32(value);
	}
}
