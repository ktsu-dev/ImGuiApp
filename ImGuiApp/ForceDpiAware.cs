// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

#pragma warning disable IDE0078 // Use pattern matching

namespace ktsu.ImGuiApp;

using System.ComponentModel;
using System.Globalization;
using System.Linq;
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
			else
			{
				string? xdgSessionType = Environment.GetEnvironmentVariable("XDG_SESSION_TYPE")?.ToLower();

				if (xdgSessionType is null or "x11")
				{
					nint display = NativeMethods.XOpenDisplay(null!);
					if (display == IntPtr.Zero)
					{
						userDpiScale = GetWaylandDpiScale();
					}
					else
					{
						string? dpiString = Marshal.PtrToStringAnsi(NativeMethods.XGetDefault(display, "Xft", "dpi"));

						if (dpiString == null || !double.TryParse(dpiString, NumberStyles.Any, CultureInfo.InvariantCulture, out userDpiScale))
						{
							int width = NativeMethods.XDisplayWidth(display, 0);
							int widthMM = NativeMethods.XDisplayWidthMM(display, 0);
							userDpiScale = width * 25.4 / widthMM;
						}

						if (NativeMethods.XCloseDisplay(display) != 0)
						{
							throw new InvalidOperationException("Failed to close X11 display connection");
						}

						// If X11 gives us standard DPI, try WSL-specific detection
						if (Math.Abs(userDpiScale - StandardDpiScale) < 1.0)
						{
							double wslScale = GetWaylandDpiScale(); // This includes WSL host detection
							if (Math.Abs(wslScale - StandardDpiScale) > 1.0)
							{
								userDpiScale = wslScale;
							}
						}
					}
				}
				else if (xdgSessionType == "wayland")
				{
					userDpiScale = GetWaylandDpiScale();
				}
				else
				{
					userDpiScale = GetWaylandDpiScale();
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
		if (TryGetGnomeScale(out double gnomeScale) && gnomeScale > 1.0)
		{
			return gnomeScale * StandardDpiScale;
		}

		// Method 3: Try WSLg-specific detection
		if (TryGetWslgScale(out double wslgScale))
		{
			return wslgScale * StandardDpiScale;
		}

		// Method 4: WSL-specific fallback - try to detect Windows host DPI
		if (TryGetWslHostDpiScale(out double hostScale))
		{
			return hostScale * StandardDpiScale;
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

		// Check for manual override first (for WSL users to match Windows host DPI)
		string? dpiOverride = Environment.GetEnvironmentVariable("IMGUI_DPI_SCALE");
		if (!string.IsNullOrEmpty(dpiOverride) && double.TryParse(dpiOverride, NumberStyles.Any, CultureInfo.InvariantCulture, out scale))
		{
			return true;
		}

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
			if (wslDistro is null or "")
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
			if (TryGetWslHostDpiScale(out double hostScale))
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
	/// Tries to get the DPI scale factor from the Windows host when running in WSL.
	/// </summary>
	/// <param name="scale">The scale factor if detected.</param>
	/// <returns>True if a scale factor was detected.</returns>
	private static bool TryGetWslHostDpiScale(out double scale)
	{
		scale = 1.0;

		try
		{
			// Check if we're running in WSL
			string? wslDistro = Environment.GetEnvironmentVariable("WSL_DISTRO_NAME");
			if (string.IsNullOrEmpty(wslDistro))
			{
				return false;
			}

			// Try Windows host DPI detection methods
			if (TryGetWindowsRegistryDpi(out scale))
			{
				return true;
			}

			// Try WSLg-specific Windows DPI detection
			if (TryGetWslgWindowsDpi(out scale))
			{
				return true;
			}

			// Try heuristic detection for high-DPI displays
			if (TryDetectHighDpiDisplay(out scale))
			{
				return true;
			}
		}
		catch (Win32Exception)
		{
			// Process execution failed
		}
		catch (IOException)
		{
			// File system access failed
		}
		catch (InvalidOperationException)
		{
			// Process operation failed
		}

		return false;
	}

	/// <summary>
	/// Tries to get DPI from Windows registry via WSL interop.
	/// </summary>
	/// <param name="scale">The detected scale factor.</param>
	/// <returns>True if DPI was detected.</returns>
	private static bool TryGetWindowsRegistryDpi(out double scale)
	{
		scale = 1.0;

		// Try multiple registry approaches
		if (TryGetSystemDpiFromPowerShell(out scale))
		{
			return true;
		}

		if (TryGetLogPixelsFromRegistry(out scale))
		{
			return true;
		}

		if (TryGetDpiFromSystemMetrics(out scale))
		{
			return true;
		}

		return false;
	}

	/// <summary>
	/// Tries to get system DPI directly via PowerShell.
	/// </summary>
	/// <param name="scale">The detected scale factor.</param>
	/// <returns>True if DPI was detected.</returns>
	private static bool TryGetSystemDpiFromPowerShell(out double scale)
	{
		scale = 1.0;

		try
		{
			// Use PowerShell to get system DPI directly
			using System.Diagnostics.Process process = new();
			process.StartInfo.FileName = "powershell.exe";
			process.StartInfo.Arguments = "-Command \"Add-Type -AssemblyName System.Windows.Forms; [System.Windows.Forms.Screen]::PrimaryScreen.WorkingArea.Width; [System.Windows.Forms.SystemInformation]::WorkingArea.Width; [System.Drawing.Graphics]::FromHwnd([System.IntPtr]::Zero).DpiX\"";
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.RedirectStandardError = true;
			process.StartInfo.CreateNoWindow = true;

			process.Start();
			string output = process.StandardOutput.ReadToEnd().Trim();
			string errorOutput = process.StandardError.ReadToEnd().Trim();
			process.WaitForExit();

			if (process.ExitCode == 0 && !string.IsNullOrEmpty(output))
			{
				string[] lines = [.. output.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l))];
				if (lines.Length >= 1)
				{
					string dpiLine = lines[^1].Trim();
					if (double.TryParse(dpiLine, NumberStyles.Any, CultureInfo.InvariantCulture, out double dpi) && dpi > 0)
					{
						scale = dpi / 96.0;
						bool isValid = scale > 1.0 && scale <= MaxScaleFactor;
						return isValid;
					}
				}
			}
		}
		catch (Win32Exception)
		{
			// Logger.Warning?.Print(LogClass.Application, $"Win32Exception in system DPI detection: {ex.Message}");
		}
		catch (IOException)
		{
			// Logger.Warning?.Print(LogClass.Application, $"IOException in system DPI detection: {ex.Message}");
		}
		catch (InvalidOperationException)
		{
			// Logger.Warning?.Print(LogClass.Application, $"InvalidOperationException in system DPI detection: {ex.Message}");
		}

		return false;
	}

	/// <summary>
	/// Tries to get LogPixels from Windows registry.
	/// </summary>
	/// <param name="scale">The detected scale factor.</param>
	/// <returns>True if LogPixels was found.</returns>
	private static bool TryGetLogPixelsFromRegistry(out double scale)
	{
		scale = 1.0;

		try
		{
			// Try multiple registry locations for LogPixels
			string[] registryCommands = [
				"Get-ItemProperty -Path 'HKCU:\\Control Panel\\Desktop' -Name LogPixels -ErrorAction SilentlyContinue | Select-Object -ExpandProperty LogPixels",
				"Get-ItemProperty -Path 'HKCU:\\Control Panel\\Desktop\\WindowMetrics' -Name AppliedDPI -ErrorAction SilentlyContinue | Select-Object -ExpandProperty AppliedDPI",
				"Get-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\FontDPI' -Name LogPixels -ErrorAction SilentlyContinue | Select-Object -ExpandProperty LogPixels"
			];

			foreach (string command in registryCommands)
			{
				using System.Diagnostics.Process process = new();
				process.StartInfo.FileName = "powershell.exe";
				process.StartInfo.Arguments = $"-Command \"{command}\"";
				process.StartInfo.UseShellExecute = false;
				process.StartInfo.RedirectStandardOutput = true;
				process.StartInfo.CreateNoWindow = true;

				process.Start();
				string output = process.StandardOutput.ReadToEnd().Trim();
				process.WaitForExit();

				if (process.ExitCode == 0 && !string.IsNullOrEmpty(output) && int.TryParse(output, out int logPixels))
				{
					scale = logPixels / 96.0;
					bool isValid = scale > 1.0 && scale <= MaxScaleFactor;
					return isValid;
				}
			}
		}
		catch (Win32Exception)
		{
			// Logger.Warning?.Print(LogClass.Application, $"Win32Exception in registry LogPixels detection: {ex.Message}");
		}
		catch (IOException)
		{
			// Logger.Warning?.Print(LogClass.Application, $"IOException in registry LogPixels detection: {ex.Message}");
		}
		catch (InvalidOperationException)
		{
			// Logger.Warning?.Print(LogClass.Application, $"InvalidOperationException in registry LogPixels detection: {ex.Message}");
		}

		return false;
	}

	/// <summary>
	/// Tries to get DPI from Windows system metrics.
	/// </summary>
	/// <param name="scale">The detected scale factor.</param>
	/// <returns>True if DPI was detected.</returns>
	private static bool TryGetDpiFromSystemMetrics(out double scale)
	{
		scale = 1.0;

		try
		{
			// Try to get system DPI using GetDeviceCaps equivalent
			using System.Diagnostics.Process process = new();
			process.StartInfo.FileName = "powershell.exe";
			process.StartInfo.Arguments = "-Command \"Add-Type -Name Win32 -Namespace System -MemberDefinition '[DllImport(\\\"user32.dll\\\")] public static extern IntPtr GetDC(IntPtr hwnd); [DllImport(\\\"gdi32.dll\\\")] public static extern int GetDeviceCaps(IntPtr hdc, int nIndex); [DllImport(\\\"user32.dll\\\")] public static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);'; $hdc = [System.Win32]::GetDC([System.IntPtr]::Zero); $dpi = [System.Win32]::GetDeviceCaps($hdc, 88); [System.Win32]::ReleaseDC([System.IntPtr]::Zero, $hdc); $dpi\"";
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.RedirectStandardError = true;
			process.StartInfo.CreateNoWindow = true;

			process.Start();
			string output = process.StandardOutput.ReadToEnd().Trim();
			string errorOutput = process.StandardError.ReadToEnd().Trim();
			process.WaitForExit();

			if (process.ExitCode == 0 && int.TryParse(output, out int dpi) && dpi > 0)
			{
				scale = dpi / 96.0;
				bool isValid = scale > 1.0 && scale <= MaxScaleFactor;
				return isValid;
			}
		}
		catch (Win32Exception)
		{
			// Logger.Warning?.Print(LogClass.Application, $"Win32Exception in system metrics detection: {ex.Message}");
		}
		catch (IOException)
		{
			// Logger.Warning?.Print(LogClass.Application, $"IOException in system metrics detection: {ex.Message}");
		}
		catch (InvalidOperationException)
		{
			// Logger.Warning?.Print(LogClass.Application, $"InvalidOperationException in system metrics detection: {ex.Message}");
		}

		return false;
	}

	/// <summary>
	/// Tries to get DPI from WSLg Windows host.
	/// </summary>
	/// <param name="scale">The detected scale factor.</param>
	/// <returns>True if DPI was detected.</returns>
	private static bool TryGetWslgWindowsDpi(out double scale)
	{
		scale = 1.0;

		try
		{
			// Try to get system DPI via Windows interop using PowerShell (wmic is deprecated)
			using System.Diagnostics.Process process = new();
			process.StartInfo.FileName = "powershell.exe";
			process.StartInfo.Arguments = "-Command \"Get-WmiObject -Class Win32_DesktopMonitor | Select-Object PixelsPerXLogicalInch | Format-List\"";
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.RedirectStandardError = true;
			process.StartInfo.CreateNoWindow = true;

			process.Start();
			string output = process.StandardOutput.ReadToEnd();
			string errorOutput = process.StandardError.ReadToEnd().Trim();
			process.WaitForExit();

			if (process.ExitCode == 0)
			{
				foreach (string line in output.Split('\n'))
				{
					if (line.Contains("PixelsPerXLogicalInch") && line.Contains(':'))
					{
						string[] parts = line.Split(':', 2);
						if (parts.Length > 1)
						{
							string value = parts[1].Trim();
							if (int.TryParse(value, out int dpi) && dpi > 0)
							{
								scale = dpi / 96.0;
								bool isValid = scale > 1.0 && scale <= MaxScaleFactor;
								return isValid;
							}
						}
					}
				}
			}

			// Alternative approach using CIM (newer than WMI)
			process.StartInfo.Arguments = "-Command \"Get-CimInstance -Class Win32_DesktopMonitor | Select-Object PixelsPerXLogicalInch | Format-List\"";
			process.Start();
			output = process.StandardOutput.ReadToEnd();
			errorOutput = process.StandardError.ReadToEnd().Trim();
			process.WaitForExit();

			if (process.ExitCode == 0)
			{
				foreach (string line in output.Split('\n'))
				{
					if (line.Contains("PixelsPerXLogicalInch") && line.Contains(':'))
					{
						string[] parts = line.Split(':', 2);
						if (parts.Length > 1)
						{
							string value = parts[1].Trim();
							if (int.TryParse(value, out int dpi) && dpi > 0)
							{
								scale = dpi / 96.0;
								bool isValid = scale > 1.0 && scale <= MaxScaleFactor;
								return isValid;
							}
						}
					}
				}
			}
		}
		catch (Win32Exception)
		{
			// Logger.Warning?.Print(LogClass.Application, $"Win32Exception in WSLg detection: {ex.Message}");
		}
		catch (IOException)
		{
			// Logger.Warning?.Print(LogClass.Application, $"IOException in WSLg detection: {ex.Message}");
		}
		catch (InvalidOperationException)
		{
			// Logger.Warning?.Print(LogClass.Application, $"InvalidOperationException in WSLg detection: {ex.Message}");
		}

		return false;
	}

	/// <summary>
	/// Tries to detect high-DPI displays using heuristics.
	/// </summary>
	/// <param name="scale">The detected scale factor.</param>
	/// <returns>True if a high-DPI display was detected.</returns>
	private static bool TryDetectHighDpiDisplay(out double scale)
	{
		scale = 1.0;

		try
		{
			// Get display information from xrandr
			using System.Diagnostics.Process process = new();
			process.StartInfo.FileName = "xrandr";
			process.StartInfo.Arguments = "--current";
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.CreateNoWindow = true;

			process.Start();
			string output = process.StandardOutput.ReadToEnd();
			process.WaitForExit();

			if (process.ExitCode == 0)
			{
				// Look for high-resolution displays that typically use scaling
				foreach (string line in output.Split('\n'))
				{
					if (line.Contains(" connected ") && line.Contains('x'))
					{
						// Extract resolution (e.g., "2560x1440")
						string[] parts = line.Split(' ');
						foreach (string part in parts)
						{
							if (part.Contains('x') && !part.Contains('+'))
							{
								string[] dimensions = part.Split('x');
								if (dimensions.Length == 2 &&
									int.TryParse(dimensions[0], out int width) &&
									int.TryParse(dimensions[1], out int height))
								{
									// Common high-DPI resolutions that typically use 125% scaling
									if ((width >= 2560 && height >= 1440) ||  // 1440p+
										(width >= 1920 && height >= 1080 && (width > 1920 || height > 1080))) // >1080p
									{
										scale = 1.25; // 125% scaling is common for these resolutions
										return true;
									}
								}
							}
						}
					}
				}
			}
		}
		catch (Win32Exception)
		{
			// Process execution failed
		}
		catch (IOException)
		{
			// File system access failed
		}
		catch (InvalidOperationException)
		{
			// Process operation failed
		}

		return false;
	}
}

