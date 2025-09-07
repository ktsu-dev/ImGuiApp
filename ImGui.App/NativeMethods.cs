// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.App;
using System.Runtime.InteropServices;

internal static partial class NativeMethods
{
	[LibraryImport("kernel32.dll")]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	internal static partial nint GetConsoleWindow();

	[LibraryImport("user32.dll")]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[return: MarshalAs(UnmanagedType.Bool)]
	internal static partial bool ShowWindow(nint hWnd, int nCmdShow);

	/// <summary>
	/// Sets the process as DPI-Aware on Windows.
	/// </summary>
	/// <returns>True if the operation was successful; otherwise, false.</returns>
	[LibraryImport("user32.dll")]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[return: MarshalAs(UnmanagedType.Bool)]
	internal static partial bool SetProcessDPIAware();

	/// <summary>
	/// Sets the DPI awareness context for the process (Windows 10 version 1607 and later).
	/// </summary>
	/// <param name="dpiContext">The DPI awareness context to set.</param>
	/// <returns>The previous DPI awareness context, or null if the function failed.</returns>
	[LibraryImport("user32.dll")]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	internal static partial nint SetProcessDpiAwarenessContext(nint dpiContext);

	/// <summary>
	/// Sets the DPI awareness for the process (Windows 8.1 and later).
	/// </summary>
	/// <param name="value">The DPI awareness value to set.</param>
	/// <returns>HRESULT indicating success or failure.</returns>
	[LibraryImport("Shcore.dll")]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	internal static partial int SetProcessDpiAwareness(ProcessDpiAwareness value);

	/// <summary>
	/// DPI awareness values for SetProcessDpiAwareness.
	/// </summary>
	internal enum ProcessDpiAwareness
	{
		/// <summary>
		/// DPI unaware. This app does not scale for DPI changes and is always assumed to have a scale factor of 100% (96 DPI).
		/// </summary>
		ProcessDpiUnaware = 0,

		/// <summary>
		/// System DPI aware. This app does not scale for DPI changes.
		/// </summary>
		ProcessSystemDpiAware = 1,

		/// <summary>
		/// Per monitor DPI aware. This app checks for the DPI when it is created and adjusts the scale factor whenever the DPI changes.
		/// </summary>
		ProcessPerMonitorDpiAware = 2
	}

	// DPI Awareness Context constants
	internal static readonly nint DPI_AWARENESS_CONTEXT_UNAWARE = new(-1);
	internal static readonly nint DPI_AWARENESS_CONTEXT_SYSTEM_AWARE = new(-2);
	internal static readonly nint DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE = new(-3);
	internal static readonly nint DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = new(-4);
	internal static readonly nint DPI_AWARENESS_CONTEXT_UNAWARE_GDISCALED = new(-5);

	internal const string X11LibraryName = "libX11.so.6";

	/// <summary>
	/// Opens a connection to the X server that controls a display.
	/// </summary>
	/// <param name="display">The name of the display.</param>
	/// <returns>A handle to the display structure.</returns>
	[LibraryImport(X11LibraryName)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	internal static partial IntPtr XOpenDisplay([MarshalAs(UnmanagedType.LPStr)] string display);

	/// <summary>
	/// Returns the value of a default setting.
	/// </summary>
	/// <param name="display">A handle to the display structure.</param>
	/// <param name="program">The program name.</param>
	/// <param name="option">The option name.</param>
	/// <returns>The value of the default setting.</returns>
	[LibraryImport(X11LibraryName)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	internal static partial IntPtr XGetDefault(IntPtr display, [MarshalAs(UnmanagedType.LPStr)] string program, [MarshalAs(UnmanagedType.LPStr)] string option);

	/// <summary>
	/// Returns the width of the screen in pixels.
	/// </summary>
	/// <param name="display">A handle to the display structure.</param>
	/// <param name="screenNumber">The screen number.</param>
	/// <returns>The width of the screen in pixels.</returns>
	[LibraryImport(X11LibraryName)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	internal static partial int XDisplayWidth(IntPtr display, int screenNumber);

	/// <summary>
	/// Returns the width of the screen in millimeters.
	/// </summary>
	/// <param name="display">A handle to the display structure.</param>
	/// <param name="screenNumber">The screen number.</param>
	/// <returns>The width of the screen in millimeters.</returns>
	[LibraryImport(X11LibraryName)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	internal static partial int XDisplayWidthMM(IntPtr display, int screenNumber);

	/// <summary>
	/// Closes the connection to the X server.
	/// </summary>
	/// <param name="display">A handle to the display structure.</param>
	/// <returns>Zero if the operation was successful; otherwise, a non-zero value.</returns>
	[LibraryImport(X11LibraryName)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	internal static partial int XCloseDisplay(IntPtr display);

	/// <summary>
	/// Structure containing startup input parameters for GDI+.
	/// </summary>
	internal struct StartupInputEx
	{
		public int GdiplusVersion;

		public IntPtr DebugEventCallback;
		public int SuppressBackgroundThread;
		public int SuppressExternalCodecs;
		public int StartupParameters;

		/// <summary>
		/// Gets the default startup input parameters.
		/// </summary>
		public static StartupInputEx Default => new()
		{
			// We assume Windows 8 and upper
			GdiplusVersion = 2,
			DebugEventCallback = IntPtr.Zero,
			SuppressBackgroundThread = 0,
			SuppressExternalCodecs = 0,
			StartupParameters = 0,
		};
	}

	/// <summary>
	/// Structure containing startup output parameters for GDI+.
	/// </summary>
	internal struct StartupOutput
	{
		public IntPtr NotificationHook;
		public IntPtr NotificationUnhook;
	}

	private const string GDILibraryName = "gdiplus.dll";

	[LibraryImport(GDILibraryName)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	internal static partial int GdiplusStartup(out IntPtr token, in StartupInputEx input, out StartupOutput output);

	[LibraryImport(GDILibraryName)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	internal static partial int GdipCreateFromHWND(IntPtr hwnd, out IntPtr graphics);

	[LibraryImport(GDILibraryName)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	internal static partial int GdipDeleteGraphics(IntPtr graphics);

	[LibraryImport(GDILibraryName)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	internal static partial int GdipGetDpiX(IntPtr graphics, out float dpi);
}
