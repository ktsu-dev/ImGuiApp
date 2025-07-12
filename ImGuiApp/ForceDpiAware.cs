// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGuiApp;

using System.ComponentModel;
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
					nint display = NativeMethods.XOpenDisplay(null!);
					if (display == IntPtr.Zero)
					{
						throw new InvalidOperationException("Failed to open X11 display connection");
					}

					string? dpiString = Marshal.PtrToStringAnsi(NativeMethods.XGetDefault(display, "Xft", "dpi"));
					if (dpiString == null || !double.TryParse(dpiString, NumberStyles.Any, CultureInfo.InvariantCulture, out userDpiScale))
					{
						userDpiScale = NativeMethods.XDisplayWidth(display, 0) * 25.4 / NativeMethods.XDisplayWidthMM(display, 0);
					}

					if (NativeMethods.XCloseDisplay(display) != 0)
					{
						throw new InvalidOperationException("Failed to close X11 display connection");
					}
				}
				else if (xdgSessionType == "wayland")
				{
					userDpiScale = GetWaylandDpiScale();
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

	/// <summary>
	/// Gets the DPI scale factor for Wayland sessions.
	/// </summary>
	/// <returns>The DPI scale factor in dots per inch.</returns>
	private static double GetWaylandDpiScale()
	{
		// Method 1: Check environment variables (most reliable for WSL)
		if (TryGetEnvironmentScale(out double envScale))
		{
			return envScale * StandardDpiScale;
		}

		// Method 2: Try to get GNOME settings (your Arch setup has gsettings)
		if (TryGetGnomeScale(out double gnomeScale))
		{
			return gnomeScale * StandardDpiScale;
		}

		// Method 3: Try WSLg-specific detection
		if (TryGetWslgScale(out double wslgScale))
		{
			return wslgScale * StandardDpiScale;
		}

		// Fallback: Return standard DPI
		return StandardDpiScale;
	}

	/// <summary>
	/// Tries to get scale factor from environment variables.
	/// </summary>
	/// <param name="scale">The scale factor if found.</param>
	/// <returns>True if a scale factor was found, false otherwise.</returns>
	private static bool TryGetEnvironmentScale(out double scale)
	{
		scale = 1.0;

		// Check GDK_SCALE (used by GTK applications - you have gtk3 installed)
		string? gdkScale = Environment.GetEnvironmentVariable("GDK_SCALE");
		if (!string.IsNullOrEmpty(gdkScale) && double.TryParse(gdkScale, NumberStyles.Any, CultureInfo.InvariantCulture, out scale))
		{
			return true;
		}

		// Check QT_SCALE_FACTOR (for Qt applications)
		string? qtScale = Environment.GetEnvironmentVariable("QT_SCALE_FACTOR");
		if (!string.IsNullOrEmpty(qtScale) && double.TryParse(qtScale, NumberStyles.Any, CultureInfo.InvariantCulture, out scale))
		{
			return true;
		}

		// Check WSL-specific environment variables
		string? wslDistro = Environment.GetEnvironmentVariable("WSL_DISTRO_NAME");
		if (!string.IsNullOrEmpty(wslDistro))
		{
			// In WSL, check for Windows DPI settings that might be forwarded
			string? wslScale = Environment.GetEnvironmentVariable("WSL_DPI_SCALE");
			if (!string.IsNullOrEmpty(wslScale) && double.TryParse(wslScale, NumberStyles.Any, CultureInfo.InvariantCulture, out scale))
			{
				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// Tries to get scale factor from GNOME settings (your setup has gsettings support).
	/// </summary>
	/// <param name="scale">The scale factor if found.</param>
	/// <returns>True if a scale factor was found, false otherwise.</returns>
	private static bool TryGetGnomeScale(out double scale)
	{
		scale = 1.0;

		try
		{
			// Try to get GNOME scaling factor using gsettings (you have gsettings-desktop-schemas)
			using System.Diagnostics.Process process = new();
			process.StartInfo.FileName = "gsettings";
			process.StartInfo.Arguments = "get org.gnome.desktop.interface scaling-factor";
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.CreateNoWindow = true;

			process.Start();
			string output = process.StandardOutput.ReadToEnd().Trim();
			process.WaitForExit();

			// Parse the output (usually in format "uint32 2" for 2x scaling)
			if (output.StartsWith("uint32 ") &&
				int.TryParse(output[7..], out int intScale) &&
				intScale > 0)
			{
				scale = intScale;
				return true;
			}

			// Also try text scaling factor
			process.StartInfo.Arguments = "get org.gnome.desktop.interface text-scaling-factor";
			process.Start();
			output = process.StandardOutput.ReadToEnd().Trim();
			process.WaitForExit();

			if (double.TryParse(output, NumberStyles.Any, CultureInfo.InvariantCulture, out scale) && scale > 0)
			{
				return true;
			}
		}
		catch (Win32Exception)
		{
			// gsettings not found
		}
		catch (IOException)
		{
			// IO error
		}
		catch (InvalidOperationException)
		{
			// Process operation failed
		}

		return false;
	}

	/// <summary>
	/// Tries to get scale factor optimized for WSLg (Windows Subsystem for Linux GUI).
	/// </summary>
	/// <param name="scale">The scale factor if found.</param>
	/// <returns>True if a scale factor was found, false otherwise.</returns>
	private static bool TryGetWslgScale(out double scale)
	{
		scale = 1.0;

		try
		{
			// Check if we're in WSL
			string? wslDistro = Environment.GetEnvironmentVariable("WSL_DISTRO_NAME");
			if (string.IsNullOrEmpty(wslDistro))
			{
				return false;
			}

			// Try to get DPI from X11 resources (WSLg provides X11 compatibility)
			using System.Diagnostics.Process process = new();
			process.StartInfo.FileName = "xrdb";
			process.StartInfo.Arguments = "-query";
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.CreateNoWindow = true;

			process.Start();
			string output = process.StandardOutput.ReadToEnd();
			process.WaitForExit();

			// Look for Xft.dpi setting
			string[] lines = output.Split('\n');
			foreach (string line in lines)
			{
				if (line.Contains("Xft.dpi:") && line.Contains('\t'))
				{
					string dpiValue = line.Split('\t')[1].Trim();
					if (double.TryParse(dpiValue, NumberStyles.Any, CultureInfo.InvariantCulture, out double dpi))
					{
						scale = dpi / StandardDpiScale;
						return true;
					}
				}
			}

			// Fallback: Try to detect Windows host DPI scaling
			// WSLg sometimes forwards Windows display settings
			if (TryGetWindowsHostDpi(out double hostScale))
			{
				scale = hostScale;
				return true;
			}
		}
		catch (Win32Exception)
		{
			// xrdb not found
		}
		catch (IOException)
		{
			// IO error
		}
		catch (InvalidOperationException)
		{
			// Process operation failed
		}

		return false;
	}

	/// <summary>
	/// Attempts to detect Windows host DPI settings that might affect WSL.
	/// </summary>
	/// <param name="scale">The detected scale factor.</param>
	/// <returns>True if a scale factor was detected.</returns>
	private static bool TryGetWindowsHostDpi(out double scale)
	{
		scale = 1.0;

		try
		{
			// Try to read Windows registry via WSL integration
			// This is a heuristic based on common WSL setups
			string? wslInterop = Environment.GetEnvironmentVariable("WSL_INTEROP");
			if (!string.IsNullOrEmpty(wslInterop))
			{
				// Common Windows DPI scale factors that WSL might inherit
				// 100% = 1.0, 125% = 1.25, 150% = 1.5, 175% = 1.75, 200% = 2.0

				// Try to detect from resolution and physical size
				using System.Diagnostics.Process process = new();
				process.StartInfo.FileName = "xrandr";
				process.StartInfo.Arguments = "--verbose";
				process.StartInfo.UseShellExecute = false;
				process.StartInfo.RedirectStandardOutput = true;
				process.StartInfo.CreateNoWindow = true;

				process.Start();
				string output = process.StandardOutput.ReadToEnd();
				process.WaitForExit();

				// Parse xrandr output for DPI calculation
				if (TryParseXrandrDpi(output, out double calculatedDpi))
				{
					scale = calculatedDpi / StandardDpiScale;
					return true;
				}
			}
		}
		catch (Win32Exception)
		{
			// xrandr not available
		}
		catch (IOException)
		{
			// IO error
		}
		catch (InvalidOperationException)
		{
			// Process failed
		}

		return false;
	}

	/// <summary>
	/// Parses xrandr output to calculate DPI.
	/// </summary>
	/// <param name="xrandrOutput">The output from xrandr --verbose.</param>
	/// <param name="dpi">The calculated DPI.</param>
	/// <returns>True if DPI was successfully calculated.</returns>
	private static bool TryParseXrandrDpi(string xrandrOutput, out double dpi)
	{
		dpi = StandardDpiScale;

		try
		{
			string[] lines = xrandrOutput.Split('\n');
			int width = 0, height = 0, mmWidth = 0, mmHeight = 0;

			foreach (string line in lines)
			{
				// Look for resolution line (e.g., "1920x1080")
				if (line.Contains(" connected ") && line.Contains('x'))
				{
					string[] parts = line.Split(' ');
					foreach (string part in parts)
					{
						if (part.Contains('x') && !part.Contains('+'))
						{
							string[] dimensions = part.Split('x');
							if (dimensions.Length == 2 &&
								int.TryParse(dimensions[0], out width) &&
								int.TryParse(dimensions[1], out height))
							{
								break;
							}
						}
					}
				}

				// Look for physical size (e.g., "510mm x 287mm")
				if (line.Contains("mm x ") && line.Contains("mm"))
				{
					string mmPart = line[Math.Max(0, line.IndexOf("mm x ") - 10)..];
					string[] parts = mmPart.Split(' ');
					foreach (string part in parts)
					{
						if (part.EndsWith("mm"))
						{
							string numStr = part.Replace("mm", "");
							if (int.TryParse(numStr, out int mm))
							{
								if (mmWidth == 0)
								{
									mmWidth = mm;
								}
								else if (mmHeight == 0)
								{
									mmHeight = mm;
									break;
								}
							}
						}
					}
				}
			}

			// Calculate DPI if we have all values
			if (width > 0 && mmWidth > 0)
			{
				dpi = width * 25.4 / mmWidth; // Convert mm to inches
				return true;
			}
		}
		catch (ArgumentException)
		{
			// Parsing error
		}

		return false;
	}
}

