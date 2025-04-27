// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGuiApp;

using System.Globalization;
using System.Runtime.InteropServices;

/// <summary>
/// Contains methods to set the application as DPI-Aware and to get the actual and window scale factors.
/// </summary>
public static partial class ForceDpiAware
{
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
			NativeMethods.SetProcessDPIAware();
		}
	}

	/// <summary>
	/// Gets the actual scale factor based on the current operating system and display settings.
	/// </summary>
	/// <returns>The actual scale factor.</returns>
	public static double GetActualScaleFactor()
	{
		var userDpiScale = 96.0;

		try
		{
			if (OperatingSystem.IsWindows())
			{
				userDpiScale = GdiPlusHelper.GetDpiX(IntPtr.Zero);
			}
			else if (OperatingSystem.IsLinux())
			{
				var xdgSessionType = Environment.GetEnvironmentVariable("XDG_SESSION_TYPE")?.ToLower();

				if (xdgSessionType is null or "x11")
				{
					var display = NativeMethods.XOpenDisplay(null!);
					var dpiString = Marshal.PtrToStringAnsi(NativeMethods.XGetDefault(display, "Xft", "dpi"));
					if (dpiString == null || !double.TryParse(dpiString, NumberStyles.Any, CultureInfo.InvariantCulture, out userDpiScale))
					{
						userDpiScale = NativeMethods.XDisplayWidth(display, 0) * 25.4 / NativeMethods.XDisplayWidthMM(display, 0);
					}

					if (NativeMethods.XCloseDisplay(display) == 0)
					{
						throw new InvalidOperationException("Failed to close X11 display connection");
					}
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
		var userDpiScale = GetActualScaleFactor();

		return Math.Min(userDpiScale / StandardDpiScale, MaxScaleFactor);
	}
}
