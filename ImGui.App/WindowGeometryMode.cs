// Copyright (c) 2023-2026 ktsu.dev contributors

namespace ktsu.ImGui.App;

/// <summary>
/// Controls who owns the application window's position and size.
/// </summary>
public enum WindowGeometryMode
{
	/// <summary>
	/// Detect at startup: a Wayland session or a known tiling window manager is treated as
	/// <see cref="Compositor"/>, everything else as <see cref="Application"/>.
	/// See <see cref="WindowingEnvironment.CompositorOwnsGeometry"/>.
	/// </summary>
	Auto,

	/// <summary>
	/// The application places and sizes its own window: the requested position is honoured at
	/// startup, the window is relocated when it lands off-screen, and the live position is
	/// persisted as part of <see cref="ImGuiApp.WindowState"/>.
	/// </summary>
	Application,

	/// <summary>
	/// The window manager owns geometry. The application never moves or resizes its own window
	/// and never records the reported position, because that position is either meaningless
	/// (Wayland clients cannot query where they are) or immediately overridden by the tiler.
	/// </summary>
	Compositor,
}
