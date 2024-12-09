// Adapted From: https://github.com/Ryujinx/Ryujinx/blob/master/src/Ryujinx.Common/SystemInterop/ForceDpiAware.cs
// License: MIT

namespace ktsu.ImGuiApp;

using System.Globalization;
using System.Runtime.InteropServices;

/// <summary>
/// Contains methods to set the application as DPI-Aware and to get the actual and window scale factors.
/// </summary>
public static partial class ForceDpiAware
{
	/// <summary>
	/// Sets the process as DPI-Aware on Windows.
	/// </summary>
	/// <returns>True if the operation was successful; otherwise, false.</returns>
	[LibraryImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static partial bool SetProcessDPIAware();

	private const string X11LibraryName = "libX11.so.6";

	/// <summary>
	/// Opens a connection to the X server that controls a display.
	/// </summary>
	/// <param name="display">The name of the display.</param>
	/// <returns>A handle to the display structure.</returns>
	[LibraryImport(X11LibraryName)]
	private static partial IntPtr XOpenDisplay([MarshalAs(UnmanagedType.LPStr)] string display);

	/// <summary>
	/// Returns the value of a default setting.
	/// </summary>
	/// <param name="display">A handle to the display structure.</param>
	/// <param name="program">The program name.</param>
	/// <param name="option">The option name.</param>
	/// <returns>The value of the default setting.</returns>
	[LibraryImport(X11LibraryName)]
	private static partial IntPtr XGetDefault(IntPtr display, [MarshalAs(UnmanagedType.LPStr)] string program, [MarshalAs(UnmanagedType.LPStr)] string option);

	/// <summary>
	/// Returns the width of the screen in pixels.
	/// </summary>
	/// <param name="display">A handle to the display structure.</param>
	/// <param name="screenNumber">The screen number.</param>
	/// <returns>The width of the screen in pixels.</returns>
	[LibraryImport(X11LibraryName)]
	private static partial int XDisplayWidth(IntPtr display, int screenNumber);

	/// <summary>
	/// Returns the width of the screen in millimeters.
	/// </summary>
	/// <param name="display">A handle to the display structure.</param>
	/// <param name="screenNumber">The screen number.</param>
	/// <returns>The width of the screen in millimeters.</returns>
	[LibraryImport(X11LibraryName)]
	private static partial int XDisplayWidthMM(IntPtr display, int screenNumber);

	/// <summary>
	/// Closes the connection to the X server.
	/// </summary>
	/// <param name="display">A handle to the display structure.</param>
	/// <returns>Zero if the operation was successful; otherwise, a non-zero value.</returns>
	[LibraryImport(X11LibraryName)]
	private static partial int XCloseDisplay(IntPtr display);

	private const double StandardDpiScale = 96.0;
	private const double MaxScaleFactor = 10.25;

	/// <summary>
	/// Marks the application as DPI-Aware when running on the Windows operating system.
	/// </summary>
	public static void Windows()
	{
		// Make process DPI aware for proper window sizing on high-res screens.
		if (OperatingSystem.IsWindowsVersionAtLeast(6))
		{
			SetProcessDPIAware();
		}
	}

	/// <summary>
	/// Gets the actual scale factor based on the current operating system and display settings.
	/// </summary>
	/// <returns>The actual scale factor.</returns>
	public static double GetActualScaleFactor()
	{
		double userDpiScale = 96.0;

		try
		{
			if (OperatingSystem.IsWindows())
			{
				userDpiScale = GdiPlusHelper.GetDpiX(IntPtr.Zero);
			}
			else if (OperatingSystem.IsLinux())
			{
				string? xdgSessionType = Environment.GetEnvironmentVariable("XDG_SESSION_TYPE")?.ToLower();

				if (xdgSessionType is null or "x11")
				{
					IntPtr display = XOpenDisplay(null!);
					string? dpiString = Marshal.PtrToStringAnsi(XGetDefault(display, "Xft", "dpi"));
					if (dpiString == null || !double.TryParse(dpiString, NumberStyles.Any, CultureInfo.InvariantCulture, out userDpiScale))
					{
						userDpiScale = XDisplayWidth(display, 0) * 25.4 / XDisplayWidthMM(display, 0);
					}
					_ = XCloseDisplay(display);
				}
				else if (xdgSessionType == "wayland")
				{
					// TODO
					//Logger.Warning?.Print(LogClass.Application, "Couldn't determine monitor DPI: Wayland not yet supported");
				}
				else
				{
					//Logger.Warning?.Print(LogClass.Application, $"Couldn't determine monitor DPI: Unrecognised XDG_SESSION_TYPE: {xdgSessionType}");
				}
			}
		}
		catch (Exception)
		{
			//Logger.Warning?.Print(LogClass.Application, $"Couldn't determine monitor DPI: {e.Message}");
			throw;
		}

		return userDpiScale;
	}

	/// <summary>
	/// Gets the window scale factor based on the actual scale factor and standard DPI scale.
	/// </summary>
	/// <returns>The window scale factor.</returns>
	public static double GetWindowScaleFactor()
	{
		double userDpiScale = GetActualScaleFactor();

		return Math.Min(userDpiScale / StandardDpiScale, MaxScaleFactor);
	}
}
