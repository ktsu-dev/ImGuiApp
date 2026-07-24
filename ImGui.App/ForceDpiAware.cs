// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

#pragma warning disable IDE0078 // Use pattern matching

namespace ktsu.ImGui.App;

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;

/// <summary>
/// Contains methods to set the application as DPI-Aware and to get the actual and window scale factors.
/// </summary>
public static partial class ForceDpiAware
{
	internal const double StandardDpiScale = 96.0;
	internal const double MaxScaleFactor = 10.25;
	private const string PowerShellExecutable = "powershell.exe";

	/// <summary>
	/// Marks the application as DPI-Aware when running on the Windows operating system.
	/// Uses modern DPI awareness APIs for better compatibility with windowing libraries.
	/// </summary>
	public static void Windows()
	{
		// Make process DPI aware for proper window sizing on high-res screens.
		// Use the most modern API available for better compatibility with GLFW and other windowing libraries.

		if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 14393)) // Windows 10 Version 1607
		{
			// Try the modern DPI awareness context API first (recommended)
			try
			{
				nint result = NativeMethods.SetProcessDpiAwarenessContext(NativeMethods.DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);
				if (result != IntPtr.Zero)
				{
					return; // Success
				}
			}
			catch (EntryPointNotFoundException)
			{
				// SetProcessDpiAwarenessContext not available, fall back to older API
			}
		}

		if (OperatingSystem.IsWindowsVersionAtLeast(6, 3)) // Windows 8.1
		{
			// Fall back to SetProcessDpiAwareness
			try
			{
				int result = NativeMethods.SetProcessDpiAwareness(NativeMethods.ProcessDpiAwareness.ProcessPerMonitorDpiAware);
				if (result == 0) // S_OK
				{
					return; // Success
				}
			}
			catch (EntryPointNotFoundException)
			{
				// SetProcessDpiAwareness not available, fall back to oldest API
			}
		}

		if (OperatingSystem.IsWindowsVersionAtLeast(6)) // Windows Vista
		{
			// Fall back to the legacy API as last resort
			NativeMethods.SetProcessDPIAware();
		}
	}

	/// <summary>
	/// Gets the actual scale factor based on the current operating system and display settings.
	/// </summary>
	/// <returns>The actual scale factor.</returns>
	public static double GetActualScaleFactor()
	{
		if (OperatingSystem.IsWindows())
		{
			return GdiPlusHelper.GetDpiX(IntPtr.Zero);
		}

		// Modern macOS has no X11; interrogate the display's backing scale via CoreGraphics.
		if (OperatingSystem.IsMacOS())
		{
			return GetMacOSDpiScale();
		}

		string? xdgSessionType = Environment.GetEnvironmentVariable("XDG_SESSION_TYPE")?.ToLower();

		// X11 (and unspecified session type) uses X11 detection; everything else falls back to Wayland.
		return xdgSessionType is null or "x11"
			? GetX11DpiScale()
			: GetWaylandDpiScale();
	}

	/// <summary>
	/// Gets the DPI scale factor for X11 sessions, falling back to Wayland/WSL detection where needed.
	/// </summary>
	/// <returns>The actual scale factor.</returns>
	private static double GetX11DpiScale()
	{
		nint display = NativeMethods.XOpenDisplay(null!);
		if (display == IntPtr.Zero)
		{
			return GetWaylandDpiScale();
		}

		string? dpiString = Marshal.PtrToStringAnsi(NativeMethods.XGetDefault(display, "Xft", "dpi"));

		if (dpiString == null || !double.TryParse(dpiString, NumberStyles.Any, CultureInfo.InvariantCulture, out double userDpiScale))
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

		return userDpiScale;
	}

	/// <summary>
	/// Gets the DPI scale factor on macOS from the main display's backing scale factor.
	/// </summary>
	/// <returns>
	/// The actual scale factor: <see cref="StandardDpiScale"/> on a standard display,
	/// twice that on a Retina display. Falls back to <see cref="StandardDpiScale"/> when the
	/// display mode cannot be read or CoreGraphics is unavailable (for example, a headless host).
	/// </returns>
	// This method is pure native orchestration over the CoreGraphics P/Invokes; every line only
	// runs on a macOS host with a display, so it cannot be exercised by the coverage runner (which
	// runs on Windows). The testable scale arithmetic lives in MacOSBackingScaleToDpi, which is
	// covered by unit tests. Excluded from coverage for that reason, matching the repository's
	// treatment of other native C ABI boundary code.
	[ExcludeFromCodeCoverage(Justification = "Native CoreGraphics orchestration; only reachable on a macOS host with a display. Testable arithmetic is factored into MacOSBackingScaleToDpi.")]
	private static double GetMacOSDpiScale()
	{
		try
		{
			uint displayId = NativeMethods.CGMainDisplayID();
			nint mode = NativeMethods.CGDisplayCopyDisplayMode(displayId);
			if (mode == IntPtr.Zero)
			{
				return StandardDpiScale;
			}

			try
			{
				// The pixel-width to point-width ratio is the display's backing scale factor
				// (1.0 on a standard display, 2.0 on Retina).
				nuint pixelWidth = NativeMethods.CGDisplayModeGetPixelWidth(mode);
				nuint pointWidth = NativeMethods.CGDisplayModeGetWidth(mode);
				return MacOSBackingScaleToDpi(pixelWidth, pointWidth);
			}
			finally
			{
				// CGDisplayCopyDisplayMode follows the Core Foundation "Copy" ownership rule.
				NativeMethods.CGDisplayModeRelease(mode);
			}
		}
		catch (DllNotFoundException)
		{
			return StandardDpiScale;
		}
		catch (EntryPointNotFoundException)
		{
			return StandardDpiScale;
		}
	}

	/// <summary>
	/// Converts a macOS display mode's pixel and point widths into an actual DPI scale factor.
	/// </summary>
	/// <param name="pixelWidth">The display mode width in physical pixels.</param>
	/// <param name="pointWidth">The display mode width in points.</param>
	/// <returns>
	/// <see cref="StandardDpiScale"/> scaled by the pixel-to-point ratio (the display's backing
	/// scale factor: <c>1.0</c> on a standard display, <c>2.0</c> on Retina), or
	/// <see cref="StandardDpiScale"/> when <paramref name="pointWidth"/> is zero.
	/// </returns>
	internal static double MacOSBackingScaleToDpi(nuint pixelWidth, nuint pointWidth)
	{
		if (pointWidth == 0)
		{
			return StandardDpiScale;
		}

		double scale = (double)pixelWidth / pointWidth;
		return scale * StandardDpiScale;
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
	internal static double GetWaylandDpiScale()
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
	internal static bool TryGetEnvironmentScale(out double scale)
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
	[SuppressMessage("Security Hotspot", "S4036:Make sure the \"PATH\" variable only contains fixed, unwriteable directories", Justification = "PATH is read to locate system tools (gsettings) for DPI detection, not used as a security boundary; this is a diagnostic tool call only.")]
	internal static bool TryGetGnomeScale(out double scale)
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
	[SuppressMessage("Security Hotspot", "S4036:Make sure the \"PATH\" variable only contains fixed, unwriteable directories", Justification = "PATH is read to locate system tools (xrdb) for DPI detection, not used as a security boundary; this is a diagnostic tool call only.")]
	[SuppressMessage("Major Code Smell", "S3267:Loops should be simplified using the \"Where\" LINQ method", Justification = "Explicit loop with early return is clearer and avoids unnecessary LINQ allocations in this diagnostic path.")]
	internal static bool TryGetWslgScale(out double scale)
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
	internal static bool TryGetWslHostDpiScale(out double scale)
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
	internal static bool TryGetWindowsRegistryDpi(out double scale)
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
	[SuppressMessage("Security Hotspot", "S4036:Make sure the \"PATH\" variable only contains fixed, unwriteable directories", Justification = "PATH is read to locate PowerShell for DPI detection, not used as a security boundary; this is a diagnostic tool call only.")]
	internal static bool TryGetSystemDpiFromPowerShell(out double scale)
	{
		scale = 1.0;

		try
		{
			// Use PowerShell to get system DPI directly
			using System.Diagnostics.Process process = new();
			process.StartInfo.FileName = PowerShellExecutable;
			process.StartInfo.Arguments = "-Command \"Add-Type -AssemblyName System.Windows.Forms; [System.Windows.Forms.Screen]::PrimaryScreen.WorkingArea.Width; [System.Windows.Forms.SystemInformation]::WorkingArea.Width; [System.Drawing.Graphics]::FromHwnd([System.IntPtr]::Zero).DpiX\"";
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.RedirectStandardError = true;
			process.StartInfo.CreateNoWindow = true;

			process.Start();
			string output = process.StandardOutput.ReadToEnd().Trim();
			_ = process.StandardError.ReadToEnd();
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
			// Process execution failed
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
	/// Tries to get LogPixels from Windows registry.
	/// </summary>
	/// <param name="scale">The detected scale factor.</param>
	/// <returns>True if LogPixels was found.</returns>
	[SuppressMessage("Security Hotspot", "S4036:Make sure the \"PATH\" variable only contains fixed, unwriteable directories", Justification = "PATH is read to locate PowerShell for reading Windows registry DPI settings, not used as a security boundary; this is a diagnostic tool call only.")]
	internal static bool TryGetLogPixelsFromRegistry(out double scale)
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
				process.StartInfo.FileName = PowerShellExecutable;
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
			// Process execution failed
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
	/// Tries to get DPI from Windows system metrics.
	/// </summary>
	/// <param name="scale">The detected scale factor.</param>
	/// <returns>True if DPI was detected.</returns>
	[SuppressMessage("Security Hotspot", "S4036:Make sure the \"PATH\" variable only contains fixed, unwriteable directories", Justification = "PATH is read to locate PowerShell for DPI detection via system metrics, not used as a security boundary; this is a diagnostic tool call only.")]
	internal static bool TryGetDpiFromSystemMetrics(out double scale)
	{
		scale = 1.0;

		try
		{
			// Try to get system DPI using GetDeviceCaps equivalent
			using System.Diagnostics.Process process = new();
			process.StartInfo.FileName = PowerShellExecutable;
			process.StartInfo.Arguments = "-Command \"Add-Type -Name Win32 -Namespace System -MemberDefinition '[DllImport(\\\"user32.dll\\\")] public static extern IntPtr GetDC(IntPtr hwnd); [DllImport(\\\"gdi32.dll\\\")] public static extern int GetDeviceCaps(IntPtr hdc, int nIndex); [DllImport(\\\"user32.dll\\\")] public static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);'; $hdc = [System.Win32]::GetDC([System.IntPtr]::Zero); $dpi = [System.Win32]::GetDeviceCaps($hdc, 88); [System.Win32]::ReleaseDC([System.IntPtr]::Zero, $hdc); $dpi\"";
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.RedirectStandardError = true;
			process.StartInfo.CreateNoWindow = true;

			process.Start();
			string output = process.StandardOutput.ReadToEnd().Trim();
			_ = process.StandardError.ReadToEnd();
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
			// Process execution failed
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
	/// Tries to get DPI from WSLg Windows host.
	/// </summary>
	/// <param name="scale">The detected scale factor.</param>
	/// <returns>True if DPI was detected.</returns>
	[SuppressMessage("Security Hotspot", "S4036:Make sure the \"PATH\" variable only contains fixed, unwriteable directories", Justification = "PATH is read to locate PowerShell for WSLg DPI detection, not used as a security boundary; this is a diagnostic tool call only.")]
	[SuppressMessage("Major Code Smell", "S3267:Loops should be simplified using the \"Where\" LINQ method", Justification = "Explicit loop with early return is clearer and avoids unnecessary LINQ allocations in this diagnostic path.")]
	internal static bool TryGetWslgWindowsDpi(out double scale)
	{
		scale = 1.0;

		try
		{
			// Try to get system DPI via Windows interop using PowerShell (wmic is deprecated)
			using System.Diagnostics.Process process = new();
			process.StartInfo.FileName = PowerShellExecutable;
			process.StartInfo.Arguments = "-Command \"Get-WmiObject -Class Win32_DesktopMonitor | Select-Object PixelsPerXLogicalInch | Format-List\"";
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.RedirectStandardError = true;
			process.StartInfo.CreateNoWindow = true;

			process.Start();
			string output = process.StandardOutput.ReadToEnd();
			_ = process.StandardError.ReadToEnd();
			process.WaitForExit();

			if (process.ExitCode == 0)
			{
				bool? wmiResult = TryParsePixelsPerLogicalInch(output, out scale);
				if (wmiResult.HasValue)
				{
					return wmiResult.Value;
				}
			}

			// Alternative approach using CIM (newer than WMI)
			process.StartInfo.Arguments = "-Command \"Get-CimInstance -Class Win32_DesktopMonitor | Select-Object PixelsPerXLogicalInch | Format-List\"";
			process.Start();
			output = process.StandardOutput.ReadToEnd();
			_ = process.StandardError.ReadToEnd();
			process.WaitForExit();

			if (process.ExitCode == 0)
			{
				bool? cimResult = TryParsePixelsPerLogicalInch(output, out scale);
				if (cimResult.HasValue)
				{
					return cimResult.Value;
				}
			}
		}
		catch (Win32Exception)
		{
			// Process execution failed
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
	/// Parses the "PixelsPerXLogicalInch" value from PowerShell WMI/CIM output.
	/// </summary>
	/// <param name="output">The command output to parse.</param>
	/// <param name="scale">The detected scale factor for the first parseable positive DPI line.</param>
	/// <returns>
	/// The validity of the first parseable positive DPI line found, or <see langword="null"/>
	/// if no such line was present.
	/// </returns>
	[SuppressMessage("Major Code Smell", "S3267:Loops should be simplified using the \"Where\" LINQ method", Justification = "Explicit loop with early return is clearer and avoids unnecessary LINQ allocations in this diagnostic path.")]
	private static bool? TryParsePixelsPerLogicalInch(string output, out double scale)
	{
		scale = 1.0;

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
						return scale > 1.0 && scale <= MaxScaleFactor;
					}
				}
			}
		}

		return null;
	}

	/// <summary>
	/// Tries to detect high-DPI displays using heuristics.
	/// </summary>
	/// <param name="scale">The detected scale factor.</param>
	/// <returns>True if a high-DPI display was detected.</returns>
	[SuppressMessage("Security Hotspot", "S4036:Make sure the \"PATH\" variable only contains fixed, unwriteable directories", Justification = "PATH is read to locate xrandr for display heuristics, not used as a security boundary; this is a diagnostic tool call only.")]
	[SuppressMessage("Major Code Smell", "S3267:Loops should be simplified using the \"Where\" LINQ method", Justification = "Explicit nested loops with early return are clearer and avoid unnecessary LINQ allocations in this diagnostic path.")]
	internal static bool TryDetectHighDpiDisplay(out double scale)
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
					if (LineIndicatesHighDpiDisplay(line))
					{
						// Common high-DPI resolutions that typically use 125% scaling
						scale = 1.25; // 125% scaling is common for these resolutions
						return true;
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

	/// <summary>
	/// Determines whether an xrandr output line describes a connected high-DPI display.
	/// </summary>
	/// <param name="line">A single line of xrandr output.</param>
	/// <returns>True if the line describes a connected display with a high-DPI resolution.</returns>
	[SuppressMessage("Major Code Smell", "S3267:Loops should be simplified using the \"Where\" LINQ method", Justification = "Explicit loop with early return is clearer and avoids unnecessary LINQ allocations in this diagnostic path.")]
	private static bool LineIndicatesHighDpiDisplay(string line)
	{
		if (!line.Contains(" connected ") || !line.Contains('x'))
		{
			return false;
		}

		// Extract resolution (e.g., "2560x1440")
		string[] parts = line.Split(' ');
		foreach (string part in parts)
		{
			if (IsHighDpiResolution(part))
			{
				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// Determines whether an xrandr resolution token (e.g. "2560x1440") is a high-DPI resolution.
	/// </summary>
	/// <param name="part">A whitespace-delimited token from an xrandr output line.</param>
	/// <returns>True if the token is a resolution that typically uses display scaling.</returns>
	private static bool IsHighDpiResolution(string part)
	{
		if (!part.Contains('x') || part.Contains('+'))
		{
			return false;
		}

		string[] dimensions = part.Split('x');
		return dimensions.Length == 2 &&
			int.TryParse(dimensions[0], out int width) &&
			int.TryParse(dimensions[1], out int height) &&
			((width >= 2560 && height >= 1440) ||  // 1440p+
			(width >= 1920 && height >= 1080 && (width > 1920 || height > 1080))); // >1080p
	}
}

