// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGuiApp;
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
	[return: MarshalAs(UnmanagedType.Bool)]
	internal static partial bool SetProcessDPIAware();

	private const string X11LibraryName = "libX11.so.6";

	/// <summary>
	/// Opens a connection to the X server that controls a display.
	/// </summary>
	/// <param name="display">The name of the display.</param>
	/// <returns>A handle to the display structure.</returns>
	[LibraryImport(X11LibraryName)]
	internal static partial IntPtr XOpenDisplay([MarshalAs(UnmanagedType.LPStr)] string display);

	/// <summary>
	/// Returns the value of a default setting.
	/// </summary>
	/// <param name="display">A handle to the display structure.</param>
	/// <param name="program">The program name.</param>
	/// <param name="option">The option name.</param>
	/// <returns>The value of the default setting.</returns>
	[LibraryImport(X11LibraryName)]
	internal static partial IntPtr XGetDefault(IntPtr display, [MarshalAs(UnmanagedType.LPStr)] string program, [MarshalAs(UnmanagedType.LPStr)] string option);

	/// <summary>
	/// Returns the width of the screen in pixels.
	/// </summary>
	/// <param name="display">A handle to the display structure.</param>
	/// <param name="screenNumber">The screen number.</param>
	/// <returns>The width of the screen in pixels.</returns>
	[LibraryImport(X11LibraryName)]
	internal static partial int XDisplayWidth(IntPtr display, int screenNumber);

	/// <summary>
	/// Returns the width of the screen in millimeters.
	/// </summary>
	/// <param name="display">A handle to the display structure.</param>
	/// <param name="screenNumber">The screen number.</param>
	/// <returns>The width of the screen in millimeters.</returns>
	[LibraryImport(X11LibraryName)]
	internal static partial int XDisplayWidthMM(IntPtr display, int screenNumber);

	/// <summary>
	/// Closes the connection to the X server.
	/// </summary>
	/// <param name="display">A handle to the display structure.</param>
	/// <returns>Zero if the operation was successful; otherwise, a non-zero value.</returns>
	[LibraryImport(X11LibraryName)]
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
	internal static partial int GdiplusStartup(out IntPtr token, in StartupInputEx input, out StartupOutput output);

	[LibraryImport(GDILibraryName)]
	internal static partial int GdipCreateFromHWND(IntPtr hwnd, out IntPtr graphics);

	[LibraryImport(GDILibraryName)]
	internal static partial int GdipDeleteGraphics(IntPtr graphics);

	[LibraryImport(GDILibraryName)]
	internal static partial int GdipGetDpiX(IntPtr graphics, out float dpi);
}
