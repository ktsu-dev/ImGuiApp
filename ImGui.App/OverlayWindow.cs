// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.App;

using System.Runtime.Versioning;

/// <summary>
/// Identifies which corner of the active monitor's work area an overlay window locks to
/// when its geometry is managed via <see cref="ImGuiApp.SetOverlayGeometry"/>.
/// </summary>
public enum OverlayCorner
{
	/// <summary>Top-left corner of the work area.</summary>
	TopLeft,

	/// <summary>Top-right corner of the work area.</summary>
	TopRight,

	/// <summary>Bottom-left corner of the work area.</summary>
	BottomLeft,

	/// <summary>Bottom-right corner of the work area.</summary>
	BottomRight,
}

/// <summary>
/// Turns the application's native window into an "overlay": borderless, always-on-top, and
/// whole-window translucent, with optional click-through and corner-locked geometry. The
/// original window styles are cached so the decorated window can be restored exactly.
///
/// Native styling is implemented on Windows; on other platforms the logical
/// <see cref="IsActive"/> state is still tracked (so frame-rate throttling and consumer
/// logic behave consistently) but no window styles are changed. All calls are best-effort
/// and no-op when the native handle is unavailable (e.g. before the window is created) or
/// when the platform does not export the required APIs.
///
/// Per-frame calls are cheap: styles are only re-applied when something actually changed.
/// Must be driven from the thread that owns the render window.
/// </summary>
internal sealed class OverlayChrome
{
	private const int GWL_STYLE = -16;
	private const int GWL_EXSTYLE = -20;

	private const int WS_CAPTION = 0x00C00000;
	private const int WS_THICKFRAME = 0x00040000;
	private const int WS_MINIMIZEBOX = 0x00020000;
	private const int WS_MAXIMIZEBOX = 0x00010000;
	private const int WS_SYSMENU = 0x00080000;

	private const int WS_EX_LAYERED = 0x00080000;
	private const int WS_EX_TRANSPARENT = 0x00000020;

	private const uint LWA_ALPHA = 0x2;

	private static readonly nint HWND_TOPMOST = new(-1);
	private static readonly nint HWND_NOTOPMOST = new(-2);

	private const uint SWP_NOSIZE = 0x0001;
	private const uint SWP_NOMOVE = 0x0002;
	private const uint SWP_NOACTIVATE = 0x0010;
	private const uint SWP_FRAMECHANGED = 0x0020;

	private const uint MONITOR_DEFAULTTONEAREST = 2;

	private nint hwnd;
	private bool styled;
	private nint originalStyle;
	private nint originalExStyle;

	// Set once if the platform doesn't export the window-style APIs (e.g. 32-bit Windows),
	// so we stop retrying the native calls every frame.
	private bool nativeUnavailable;

	// Last-applied values, so per-frame calls only touch Win32 on a real change.
	private bool lastClickThrough;
	private byte lastAlpha;
	private (OverlayCorner Corner, int OffsetX, int OffsetY, int Width, int Height) lastGeometry;
	private bool geometryApplied;

	/// <summary>
	/// Gets a value indicating whether overlay mode is logically active (i.e. <see cref="Enable"/>
	/// has been called more recently than <see cref="Disable"/>). Tracked on all platforms,
	/// independently of whether native styling could be applied.
	/// </summary>
	public bool IsActive { get; private set; }

	/// <summary>
	/// Switches the window into overlay mode (borderless, topmost, layered) and keeps its
	/// click-through and opacity in sync. Safe to call every frame.
	/// </summary>
	/// <param name="windowHandle">The native window handle, or zero if unavailable.</param>
	/// <param name="opacity">Whole-window opacity in the range 0.2–1.0.</param>
	/// <param name="clickThrough">When true, mouse input passes through to whatever is behind the overlay.</param>
	public void Enable(nint windowHandle, float opacity, bool clickThrough)
	{
		IsActive = true;

		if (!OperatingSystem.IsWindows() || windowHandle == 0 || nativeUnavailable)
		{
			return;
		}

		ApplyWindowsStyles(windowHandle, opacity, clickThrough);
	}

	/// <summary>
	/// Locks the overlay to the given corner of its monitor's work area at the given offset
	/// and size. Re-applies only when something changed (or on the first call after entering
	/// overlay mode). No-op unless overlay mode is active and native styling has been applied.
	/// </summary>
	public void SetGeometry(nint windowHandle, OverlayCorner corner, int offsetX, int offsetY, int width, int height)
	{
		if (!IsActive || !styled || !OperatingSystem.IsWindows() || windowHandle == 0)
		{
			return;
		}

		ApplyWindowsGeometry(windowHandle, corner, offsetX, offsetY, width, height);
	}

	/// <summary>Restores the original decorated, non-topmost, opaque window. Safe to call repeatedly.</summary>
	public void Disable()
	{
		if (styled && OperatingSystem.IsWindows())
		{
			RestoreWindowsStyles();
		}

		IsActive = false;
	}

	/// <summary>Clears all logical and cached state without touching the native window (test/reset hook).</summary>
	internal void ResetState()
	{
		IsActive = false;
		styled = false;
		geometryApplied = false;
		nativeUnavailable = false;
		hwnd = 0;
		originalStyle = 0;
		originalExStyle = 0;
		lastAlpha = 0;
		lastClickThrough = false;
		lastGeometry = default;
	}

	[SupportedOSPlatform("windows")]
	private void ApplyWindowsStyles(nint windowHandle, float opacity, bool clickThrough)
	{
		byte alpha = (byte)Math.Clamp(opacity * 255f, 51f, 255f);

		try
		{
			if (!styled || hwnd != windowHandle)
			{
				hwnd = windowHandle;
				originalStyle = NativeMethods.GetWindowLongPtr(windowHandle, GWL_STYLE);
				originalExStyle = NativeMethods.GetWindowLongPtr(windowHandle, GWL_EXSTYLE);

				nint style = originalStyle & ~(nint)(WS_CAPTION | WS_THICKFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX | WS_SYSMENU);
				_ = NativeMethods.SetWindowLongPtr(windowHandle, GWL_STYLE, style);
				_ = NativeMethods.SetWindowLongPtr(windowHandle, GWL_EXSTYLE, originalExStyle | WS_EX_LAYERED);
				_ = NativeMethods.SetWindowPos(windowHandle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_FRAMECHANGED);

				styled = true;
				// Force the click-through and alpha branches below to run for the fresh window.
				lastClickThrough = !clickThrough;
				lastAlpha = 0;
			}

			if (clickThrough != lastClickThrough)
			{
				nint ex = NativeMethods.GetWindowLongPtr(windowHandle, GWL_EXSTYLE) | WS_EX_LAYERED;
				ex = clickThrough ? (ex | WS_EX_TRANSPARENT) : (ex & ~(nint)WS_EX_TRANSPARENT);
				_ = NativeMethods.SetWindowLongPtr(windowHandle, GWL_EXSTYLE, ex);
				lastClickThrough = clickThrough;
			}

			if (alpha != lastAlpha)
			{
				_ = NativeMethods.SetLayeredWindowAttributes(windowHandle, 0, alpha, LWA_ALPHA);
				lastAlpha = alpha;
			}
		}
		catch (Exception ex) when (ex is EntryPointNotFoundException or DllNotFoundException)
		{
			// Older/32-bit Windows may not export the *Ptr window-style APIs; degrade to a
			// normal decorated window rather than throwing every frame.
			nativeUnavailable = true;
			styled = false;
			DebugLogger.Log($"OverlayChrome: native styling unavailable ({ex.Message}); overlay will render as a normal window.");
		}
	}

	[SupportedOSPlatform("windows")]
	private void ApplyWindowsGeometry(nint windowHandle, OverlayCorner corner, int offsetX, int offsetY, int width, int height)
	{
		width = Math.Max(200, width);
		height = Math.Max(140, height);

		(OverlayCorner corner, int offsetX, int offsetY, int width, int height) geometry = (corner, offsetX, offsetY, width, height);
		if (geometryApplied && geometry == lastGeometry)
		{
			return;
		}

		if (!TryGetWorkArea(windowHandle, out NativeMethods.RECT work))
		{
			return;
		}

		bool right = corner is OverlayCorner.TopRight or OverlayCorner.BottomRight;
		bool bottom = corner is OverlayCorner.BottomLeft or OverlayCorner.BottomRight;

		int x = right ? work.Right - width - offsetX : work.Left + offsetX;
		int y = bottom ? work.Bottom - height - offsetY : work.Top + offsetY;

		// The explicit resize also forces the renderer to refresh its framebuffer after the
		// title bar was removed — without it the top of the content can be clipped.
		_ = NativeMethods.SetWindowPos(windowHandle, HWND_TOPMOST, x, y, width, height, SWP_NOACTIVATE);
		lastGeometry = geometry;
		geometryApplied = true;
	}

	[SupportedOSPlatform("windows")]
	private void RestoreWindowsStyles()
	{
		if (hwnd == 0)
		{
			return;
		}

		_ = NativeMethods.SetWindowLongPtr(hwnd, GWL_STYLE, originalStyle);
		_ = NativeMethods.SetWindowLongPtr(hwnd, GWL_EXSTYLE, originalExStyle);
		_ = NativeMethods.SetWindowPos(hwnd, HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_FRAMECHANGED);

		styled = false;
		geometryApplied = false;
		lastAlpha = 0;
		lastClickThrough = false;
	}

	[SupportedOSPlatform("windows")]
	private static bool TryGetWorkArea(nint windowHandle, out NativeMethods.RECT work)
	{
		nint monitor = NativeMethods.MonitorFromWindow(windowHandle, MONITOR_DEFAULTTONEAREST);
		NativeMethods.MONITORINFO info = new() { cbSize = System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.MONITORINFO>() };
		if (monitor != 0 && NativeMethods.GetMonitorInfo(monitor, ref info))
		{
			work = info.rcWork;
			return true;
		}

		work = default;
		return false;
	}
}
