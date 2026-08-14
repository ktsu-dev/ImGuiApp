// Copyright (c) 2023-2026 ktsu.dev contributors

namespace ktsu.ImGui.App;

/// <summary>
/// Detects whether the host window manager owns window geometry, so the application can stop
/// placing, sizing, and tracking its own window where doing so is meaningless or actively harmful.
/// </summary>
/// <remarks>
/// Two environments take geometry away from the client:
/// <list type="bullet">
/// <item><description>
/// Wayland. A Wayland client has no way to read or set its own surface position — the backend
/// reports a placeholder (usually 0,0) and position requests are dropped on the floor.
/// </description></item>
/// <item><description>
/// Tiling window managers (i3, sway, niri, Hyprland, dwm, …). The window manager assigns every
/// window a slot; a client that resizes or moves itself is either ignored or fights the tiler,
/// which shows up as flicker and repeated resize storms.
/// </description></item>
/// </list>
/// Detection is cached for the process lifetime because the answer cannot change without a new
/// session.
/// </remarks>
public static class WindowingEnvironment
{
	/// <summary>
	/// Window manager and desktop names that tile. Matched case-insensitively against the
	/// XDG desktop environment variables, which may hold a colon-separated list.
	/// </summary>
	private static readonly HashSet<string> TilingDesktopNames = new(StringComparer.OrdinalIgnoreCase)
	{
		"awesome", "bspwm", "cwm", "dk", "dwm", "herbstluftwm", "hyprland", "i3", "leftwm",
		"niri", "notion", "qtile", "ratpoison", "river", "spectrwm", "stumpwm", "sway",
		"wayfire", "wmii", "xmonad",
	};

	/// <summary>
	/// Environment variables whose mere presence identifies a running tiling window manager,
	/// used as a backstop for sessions that do not advertise themselves via the XDG variables.
	/// </summary>
	private static readonly string[] TilingSessionMarkers =
	[
		"SWAYSOCK",
		"NIRI_SOCKET",
		"HYPRLAND_INSTANCE_SIGNATURE",
		"I3SOCK",
		"RIVER_SOCKET",
	];

	/// <summary>
	/// Environment variables that name the running desktop or window manager. The value may be a
	/// colon-separated list, such as <c>wlroots:sway</c>.
	/// </summary>
	private static readonly string[] DesktopNameVariables =
	[
		"XDG_CURRENT_DESKTOP",
		"XDG_SESSION_DESKTOP",
		"DESKTOP_SESSION",
	];

	private static Lazy<bool> detection = CreateDetection();

	/// <summary>
	/// Gets a value indicating whether the window manager — rather than the application — owns
	/// window position and size.
	/// </summary>
	public static bool CompositorOwnsGeometry => detection.Value;

	/// <summary>
	/// Determines whether the described session hands window geometry to the window manager.
	/// </summary>
	/// <param name="readEnvironmentVariable">Reads an environment variable by name; injected for testing.</param>
	/// <returns>True when the window manager owns geometry.</returns>
	internal static bool Detect(Func<string, string?> readEnvironmentVariable)
	{
		Ensure.NotNull(readEnvironmentVariable);

		// Windows and macOS place windows on the client's behalf but honour client-requested
		// geometry, so the application stays in charge there.
		if (!OperatingSystem.IsLinux() && !OperatingSystem.IsFreeBSD())
		{
			return false;
		}

		// Any Wayland session, tiling or not: the protocol has no concept of a client-visible
		// window position at all.
		if (string.Equals(readEnvironmentVariable("XDG_SESSION_TYPE"), "wayland", StringComparison.OrdinalIgnoreCase) ||
			!string.IsNullOrEmpty(readEnvironmentVariable("WAYLAND_DISPLAY")))
		{
			return true;
		}

		foreach (string marker in TilingSessionMarkers)
		{
			if (!string.IsNullOrEmpty(readEnvironmentVariable(marker)))
			{
				return true;
			}
		}

		foreach (string variable in DesktopNameVariables)
		{
			string? value = readEnvironmentVariable(variable);
			if (string.IsNullOrEmpty(value))
			{
				continue;
			}

			foreach (string name in value.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
			{
				if (TilingDesktopNames.Contains(name))
				{
					return true;
				}
			}
		}

		return false;
	}

	/// <summary>
	/// Clears the cached detection result so the next query re-reads the environment.
	/// </summary>
	internal static void ResetCache() => detection = CreateDetection();

	private static Lazy<bool> CreateDetection() => new(() => Detect(Environment.GetEnvironmentVariable));
}
