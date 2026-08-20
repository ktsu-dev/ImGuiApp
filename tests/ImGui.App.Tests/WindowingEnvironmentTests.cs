// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests for tiling / Wayland window manager detection and the geometry-ownership decision it feeds.
/// </summary>
[TestClass]
public sealed class WindowingEnvironmentTests
{
	/// <summary>Builds an environment reader over a fixed set of variables.</summary>
	private static Func<string, string?> EnvironmentReader(params (string Name, string Value)[] variables) =>
		name => variables.FirstOrDefault(v => v.Name == name).Value;

	[TestMethod]
	public void Detect_WithWaylandSessionType_ReportsCompositorOwnership()
	{
		if (!OperatingSystem.IsLinux() && !OperatingSystem.IsFreeBSD())
		{
			Assert.Inconclusive("Detection only inspects the environment on Linux and FreeBSD.");
		}

		bool result = WindowingEnvironment.Detect(EnvironmentReader(("XDG_SESSION_TYPE", "wayland")));

		Assert.IsTrue(result, "A Wayland client cannot query or set its own position, so the compositor owns geometry.");
	}

	[TestMethod]
	public void Detect_WithWaylandDisplayOnly_ReportsCompositorOwnership()
	{
		if (!OperatingSystem.IsLinux() && !OperatingSystem.IsFreeBSD())
		{
			Assert.Inconclusive("Detection only inspects the environment on Linux and FreeBSD.");
		}

		bool result = WindowingEnvironment.Detect(EnvironmentReader(("WAYLAND_DISPLAY", "wayland-1")));

		Assert.IsTrue(result, "WAYLAND_DISPLAY alone identifies a Wayland session.");
	}

	[TestMethod]
	public void Detect_WithTilingDesktopName_ReportsCompositorOwnership()
	{
		if (!OperatingSystem.IsLinux() && !OperatingSystem.IsFreeBSD())
		{
			Assert.Inconclusive("Detection only inspects the environment on Linux and FreeBSD.");
		}

		bool result = WindowingEnvironment.Detect(EnvironmentReader(
			("XDG_SESSION_TYPE", "x11"),
			("XDG_CURRENT_DESKTOP", "i3")));

		Assert.IsTrue(result, "A tiling window manager on X11 owns geometry even though X11 clients can request positions.");
	}

	[TestMethod]
	public void Detect_WithColonSeparatedDesktopList_MatchesAnyEntry()
	{
		if (!OperatingSystem.IsLinux() && !OperatingSystem.IsFreeBSD())
		{
			Assert.Inconclusive("Detection only inspects the environment on Linux and FreeBSD.");
		}

		bool result = WindowingEnvironment.Detect(EnvironmentReader(
			("XDG_SESSION_TYPE", "x11"),
			("XDG_CURRENT_DESKTOP", "wlroots:sway")));

		Assert.IsTrue(result, "XDG_CURRENT_DESKTOP may hold a colon-separated list; any tiling entry counts.");
	}

	[TestMethod]
	public void Detect_WithTilingSessionMarker_ReportsCompositorOwnership()
	{
		if (!OperatingSystem.IsLinux() && !OperatingSystem.IsFreeBSD())
		{
			Assert.Inconclusive("Detection only inspects the environment on Linux and FreeBSD.");
		}

		bool result = WindowingEnvironment.Detect(EnvironmentReader(
			("XDG_SESSION_TYPE", "x11"),
			("I3SOCK", "/run/user/1000/i3/ipc.sock")));

		Assert.IsTrue(result, "A window manager's IPC socket identifies it even when the XDG variables are unset.");
	}

	[TestMethod]
	public void Detect_WithFloatingX11Desktop_ReportsApplicationOwnership()
	{
		bool result = WindowingEnvironment.Detect(EnvironmentReader(
			("XDG_SESSION_TYPE", "x11"),
			("XDG_CURRENT_DESKTOP", "GNOME")));

		Assert.IsFalse(result, "A floating X11 desktop honours client-requested geometry.");
	}

	[TestMethod]
	public void Detect_WithEmptyEnvironment_ReportsApplicationOwnership()
	{
		bool result = WindowingEnvironment.Detect(EnvironmentReader());

		Assert.IsFalse(result, "With nothing to go on, the application keeps its existing placement behavior.");
	}

	[TestMethod]
	public void Detect_WithNullReader_Throws() =>
		Assert.ThrowsExactly<ArgumentNullException>(() => WindowingEnvironment.Detect(null!));

	[TestMethod]
	public void CompositorOwnsWindowGeometry_WithApplicationMode_IgnoresDetection()
	{
		ImGuiApp.Reset();
		ImGuiApp.Config = new ImGuiAppConfig { WindowGeometry = WindowGeometryMode.Application };

		Assert.IsFalse(ImGuiApp.CompositorOwnsWindowGeometry, "An explicit mode must override auto-detection.");

		ImGuiApp.Reset();
	}

	[TestMethod]
	public void CompositorOwnsWindowGeometry_WithCompositorMode_IgnoresDetection()
	{
		ImGuiApp.Reset();
		ImGuiApp.Config = new ImGuiAppConfig { WindowGeometry = WindowGeometryMode.Compositor };

		Assert.IsTrue(ImGuiApp.CompositorOwnsWindowGeometry, "An explicit mode must override auto-detection.");

		ImGuiApp.Reset();
	}

	[TestMethod]
	public void CompositorOwnsGeometry_IsStableAcrossQueriesAndCacheResets()
	{
		bool first = WindowingEnvironment.CompositorOwnsGeometry;

		Assert.AreEqual(first, WindowingEnvironment.CompositorOwnsGeometry, "The cached result must not change between queries.");

		WindowingEnvironment.ResetCache();

		Assert.AreEqual(first, WindowingEnvironment.CompositorOwnsGeometry, "Re-probing the same environment must reach the same conclusion.");
	}

	[TestMethod]
	public void CompositorOwnsWindowGeometry_WithAutoMode_FollowsDetection()
	{
		ImGuiApp.Reset();

		Assert.AreEqual(WindowGeometryMode.Auto, ImGuiApp.Config.WindowGeometry);
		Assert.AreEqual(WindowingEnvironment.CompositorOwnsGeometry, ImGuiApp.CompositorOwnsWindowGeometry);
	}
}
