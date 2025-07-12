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
			else if (OperatingSystem.IsLinux())
			{
				string? xdgSessionType = Environment.GetEnvironmentVariable("XDG_SESSION_TYPE")?.ToLower();
				Console.WriteLine($"[DPI Debug] XDG_SESSION_TYPE: '{xdgSessionType}'");

				if (xdgSessionType is null or "x11")
				{
					Console.WriteLine("[DPI Debug] Using X11 DPI detection path");
					nint display = NativeMethods.XOpenDisplay(null!);
					if (display == IntPtr.Zero)
					{
						Console.WriteLine("[DPI Debug] Failed to open X11 display, trying Wayland fallback");
						userDpiScale = GetWaylandDpiScale();
					}
					else
					{
						string? dpiString = Marshal.PtrToStringAnsi(NativeMethods.XGetDefault(display, "Xft", "dpi"));
						Console.WriteLine($"[DPI Debug] X11 Xft.dpi: '{dpiString}'");

						if (dpiString == null || !double.TryParse(dpiString, NumberStyles.Any, CultureInfo.InvariantCulture, out userDpiScale))
						{
							int width = NativeMethods.XDisplayWidth(display, 0);
							int widthMM = NativeMethods.XDisplayWidthMM(display, 0);
							userDpiScale = width * 25.4 / widthMM;
							Console.WriteLine($"[DPI Debug] X11 calculated DPI: {userDpiScale:F1} (width: {width}px, widthMM: {widthMM}mm)");
						}
						else
						{
							Console.WriteLine($"[DPI Debug] X11 Xft.dpi parsed: {userDpiScale:F1}");
						}

						if (NativeMethods.XCloseDisplay(display) != 0)
						{
							throw new InvalidOperationException("Failed to close X11 display connection");
						}

						// If X11 gives us standard DPI, try WSL-specific detection
						if (Math.Abs(userDpiScale - StandardDpiScale) < 1.0)
						{
							Console.WriteLine("[DPI Debug] X11 returned standard DPI, trying WSL host detection");
							double wslScale = GetWaylandDpiScale(); // This includes WSL host detection
							if (Math.Abs(wslScale - StandardDpiScale) > 1.0)
							{
								Console.WriteLine($"[DPI Debug] WSL host detection found better DPI: {wslScale:F1}");
								userDpiScale = wslScale;
							}
						}
					}
				}
				else if (xdgSessionType == "wayland")
				{
					Console.WriteLine("[DPI Debug] Using Wayland DPI detection path");
					userDpiScale = GetWaylandDpiScale();
				}
				else
				{
					Console.WriteLine($"[DPI Debug] Unknown session type '{xdgSessionType}', trying Wayland fallback");
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
		Console.WriteLine("[DPI Debug] Trying Wayland DPI detection methods...");

		// Method 1: Check environment variables (most reliable for WSL)
		if (TryGetEnvironmentScale(out double envScale))
		{
			Console.WriteLine($"[DPI Debug] Environment scale detected: {envScale:F3}");
			return envScale * StandardDpiScale;
		}
		Console.WriteLine("[DPI Debug] Environment variables not found");

		// Method 2: Try to get GNOME settings (your Arch setup has gsettings)
		if (TryGetGnomeScale(out double gnomeScale) && gnomeScale > 1.0)
		{
			Console.WriteLine($"[DPI Debug] GNOME scale detected: {gnomeScale:F3}");
			return gnomeScale * StandardDpiScale;
		}
		Console.WriteLine("[DPI Debug] GNOME settings returned default scale (1.0), trying other methods");

		// Method 3: Try WSLg-specific detection
		if (TryGetWslgScale(out double wslgScale))
		{
			Console.WriteLine($"[DPI Debug] WSLg scale detected: {wslgScale:F3}");
			return wslgScale * StandardDpiScale;
		}
		Console.WriteLine("[DPI Debug] WSLg detection failed");

		// Method 4: WSL-specific fallback - try to detect Windows host DPI
		if (TryGetWslHostDpiScale(out double hostScale))
		{
			Console.WriteLine($"[DPI Debug] WSL host scale detected: {hostScale:F3}");
			return hostScale * StandardDpiScale;
		}
		Console.WriteLine("[DPI Debug] WSL host detection failed");

		// Fallback: Return standard DPI
		Console.WriteLine("[DPI Debug] Falling back to standard DPI (96)");
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
			Console.WriteLine($"[DPI Debug] Manual DPI override found: {scale:F3}");
			return true;
		}

		// Check GDK_SCALE (used by GTK applications - you have gtk3 installed)
		string? gdkScale = Environment.GetEnvironmentVariable("GDK_SCALE");
		if (!string.IsNullOrEmpty(gdkScale) && double.TryParse(gdkScale, NumberStyles.Any, CultureInfo.InvariantCulture, out scale))
		{
			Console.WriteLine($"[DPI Debug] GDK_SCALE found: {scale:F3}");
			return true;
		}

		// Check QT_SCALE_FACTOR (for Qt applications)
		string? qtScale = Environment.GetEnvironmentVariable("QT_SCALE_FACTOR");
		if (!string.IsNullOrEmpty(qtScale) && double.TryParse(qtScale, NumberStyles.Any, CultureInfo.InvariantCulture, out scale))
		{
			Console.WriteLine($"[DPI Debug] QT_SCALE_FACTOR found: {scale:F3}");
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
				Console.WriteLine($"[DPI Debug] WSL_DPI_SCALE found: {scale:F3}");
				return true;
			}
		}

		Console.WriteLine("[DPI Debug] No environment scale variables found");
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
			// Check if we're in WSL
			string? wslDistro = Environment.GetEnvironmentVariable("WSL_DISTRO_NAME");
			Console.WriteLine($"[DPI Debug] WSL_DISTRO_NAME: '{wslDistro}'");
			if (wslDistro is null or "")
			{
				Console.WriteLine("[DPI Debug] Not in WSL, skipping WSL host detection");
				return false;
			}

			// Try to get the Windows host DPI using common WSL scale factors
			// Many WSL setups inherit the Windows display scaling
			Console.WriteLine("[DPI Debug] In WSL, trying Windows host DPI detection methods...");

			// Method 1: Try to read from Windows registry via WSL interop
			Console.WriteLine("[DPI Debug] Trying Windows registry access...");
			if (TryGetWindowsRegistryDpi(out double registryScale))
			{
				Console.WriteLine($"[DPI Debug] Windows registry DPI detected: {registryScale:F3}");
				scale = registryScale;
				return true;
			}

			// Method 2: Try to detect from common WSL environment patterns
			// Look for indicators of high-DPI displays
			string? display = Environment.GetEnvironmentVariable("DISPLAY");
			Console.WriteLine($"[DPI Debug] DISPLAY: '{display}'");
			if (display is ":0" or ":0.0")
			{
				// This is likely WSLg running on a Windows host
				// Try to get DPI from Windows system
				Console.WriteLine("[DPI Debug] Trying WSLg Windows DPI detection...");
				if (TryGetWslgWindowsDpi(out double wslgScale))
				{
					Console.WriteLine($"[DPI Debug] WSLg Windows DPI detected: {wslgScale:F3}");
					scale = wslgScale;
					return true;
				}
			}

			// Method 3: Use heuristics based on common high-DPI setups
			// If we detect a high-resolution display, assume 125% scaling
			Console.WriteLine("[DPI Debug] Trying heuristic high-DPI detection...");
			if (TryDetectHighDpiDisplay(out double heuristicScale))
			{
				Console.WriteLine($"[DPI Debug] Heuristic high-DPI detected: {heuristicScale:F3}");
				scale = heuristicScale;
				return true;
			}
		}
		catch (Win32Exception ex)
		{
			Console.WriteLine($"[DPI Debug] Win32Exception in WSL host detection: {ex.Message}");
		}
		catch (IOException ex)
		{
			Console.WriteLine($"[DPI Debug] IOException in WSL host detection: {ex.Message}");
		}
		catch (InvalidOperationException ex)
		{
			Console.WriteLine($"[DPI Debug] InvalidOperationException in WSL host detection: {ex.Message}");
		}

		Console.WriteLine("[DPI Debug] All WSL host detection methods failed");
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

			Console.WriteLine("[DPI Debug] Trying PowerShell system DPI detection...");
			process.Start();
			string output = process.StandardOutput.ReadToEnd().Trim();
			string errorOutput = process.StandardError.ReadToEnd().Trim();
			process.WaitForExit();

			Console.WriteLine($"[DPI Debug] PowerShell DPI exit code: {process.ExitCode}");
			Console.WriteLine($"[DPI Debug] PowerShell DPI output: '{output}'");
			if (!string.IsNullOrEmpty(errorOutput))
			{
				Console.WriteLine($"[DPI Debug] PowerShell DPI error: '{errorOutput}'");
			}

			if (process.ExitCode == 0 && !string.IsNullOrEmpty(output))
			{
				string[] lines = [.. output.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l))];
				if (lines.Length >= 1)
				{
					string dpiLine = lines[^1].Trim();
					if (double.TryParse(dpiLine, NumberStyles.Any, CultureInfo.InvariantCulture, out double dpi) && dpi > 0)
					{
						scale = dpi / 96.0;
						Console.WriteLine($"[DPI Debug] Parsed system DPI: {dpi}, calculated scale: {scale:F3}");
						bool isValid = scale > 1.0 && scale <= MaxScaleFactor;
						Console.WriteLine($"[DPI Debug] System DPI scale factor valid: {isValid}");
						return isValid;
					}
				}
			}
		}
		catch (Win32Exception ex)
		{
			Console.WriteLine($"[DPI Debug] Win32Exception in system DPI detection: {ex.Message}");
		}
		catch (IOException ex)
		{
			Console.WriteLine($"[DPI Debug] IOException in system DPI detection: {ex.Message}");
		}
		catch (InvalidOperationException ex)
		{
			Console.WriteLine($"[DPI Debug] InvalidOperationException in system DPI detection: {ex.Message}");
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

				Console.WriteLine($"[DPI Debug] Trying registry command: {command}");
				process.Start();
				string output = process.StandardOutput.ReadToEnd().Trim();
				process.WaitForExit();

				Console.WriteLine($"[DPI Debug] Registry command exit code: {process.ExitCode}");
				Console.WriteLine($"[DPI Debug] Registry command output: '{output}'");

				if (process.ExitCode == 0 && !string.IsNullOrEmpty(output) && int.TryParse(output, out int logPixels))
				{
					scale = logPixels / 96.0;
					Console.WriteLine($"[DPI Debug] Parsed LogPixels: {logPixels}, calculated scale: {scale:F3}");
					bool isValid = scale > 1.0 && scale <= MaxScaleFactor;
					Console.WriteLine($"[DPI Debug] Registry scale factor valid: {isValid}");
					return isValid;
				}
			}
		}
		catch (Win32Exception ex)
		{
			Console.WriteLine($"[DPI Debug] Win32Exception in registry LogPixels detection: {ex.Message}");
		}
		catch (IOException ex)
		{
			Console.WriteLine($"[DPI Debug] IOException in registry LogPixels detection: {ex.Message}");
		}
		catch (InvalidOperationException ex)
		{
			Console.WriteLine($"[DPI Debug] InvalidOperationException in registry LogPixels detection: {ex.Message}");
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

			Console.WriteLine("[DPI Debug] Trying PowerShell system metrics DPI detection...");
			process.Start();
			string output = process.StandardOutput.ReadToEnd().Trim();
			string errorOutput = process.StandardError.ReadToEnd().Trim();
			process.WaitForExit();

			Console.WriteLine($"[DPI Debug] System metrics exit code: {process.ExitCode}");
			Console.WriteLine($"[DPI Debug] System metrics output: '{output}'");
			if (!string.IsNullOrEmpty(errorOutput))
			{
				Console.WriteLine($"[DPI Debug] System metrics error: '{errorOutput}'");
			}

			if (process.ExitCode == 0 && int.TryParse(output, out int dpi) && dpi > 0)
			{
				scale = dpi / 96.0;
				Console.WriteLine($"[DPI Debug] Parsed system metrics DPI: {dpi}, calculated scale: {scale:F3}");
				bool isValid = scale > 1.0 && scale <= MaxScaleFactor;
				Console.WriteLine($"[DPI Debug] System metrics scale factor valid: {isValid}");
				return isValid;
			}
		}
		catch (Win32Exception ex)
		{
			Console.WriteLine($"[DPI Debug] Win32Exception in system metrics detection: {ex.Message}");
		}
		catch (IOException ex)
		{
			Console.WriteLine($"[DPI Debug] IOException in system metrics detection: {ex.Message}");
		}
		catch (InvalidOperationException ex)
		{
			Console.WriteLine($"[DPI Debug] InvalidOperationException in system metrics detection: {ex.Message}");
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

			Console.WriteLine("[DPI Debug] Trying WSLg Windows DPI detection with PowerShell...");
			process.Start();
			string output = process.StandardOutput.ReadToEnd();
			string errorOutput = process.StandardError.ReadToEnd().Trim();
			process.WaitForExit();

			Console.WriteLine($"[DPI Debug] WSLg PowerShell exit code: {process.ExitCode}");
			Console.WriteLine($"[DPI Debug] WSLg PowerShell output: '{output}'");
			if (!string.IsNullOrEmpty(errorOutput))
			{
				Console.WriteLine($"[DPI Debug] WSLg PowerShell error: '{errorOutput}'");
			}

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
								Console.WriteLine($"[DPI Debug] WSLg detected DPI: {dpi}, scale: {scale:F3}");
								bool isValid = scale > 1.0 && scale <= MaxScaleFactor;
								Console.WriteLine($"[DPI Debug] WSLg scale factor valid: {isValid}");
								return isValid;
							}
						}
					}
				}
			}

			// Alternative approach using CIM (newer than WMI)
			Console.WriteLine("[DPI Debug] Trying WSLg Windows DPI detection with Get-CimInstance...");
			process.StartInfo.Arguments = "-Command \"Get-CimInstance -Class Win32_DesktopMonitor | Select-Object PixelsPerXLogicalInch | Format-List\"";
			process.Start();
			output = process.StandardOutput.ReadToEnd();
			errorOutput = process.StandardError.ReadToEnd().Trim();
			process.WaitForExit();

			Console.WriteLine($"[DPI Debug] WSLg CIM exit code: {process.ExitCode}");
			Console.WriteLine($"[DPI Debug] WSLg CIM output: '{output}'");
			if (!string.IsNullOrEmpty(errorOutput))
			{
				Console.WriteLine($"[DPI Debug] WSLg CIM error: '{errorOutput}'");
			}

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
								Console.WriteLine($"[DPI Debug] WSLg CIM detected DPI: {dpi}, scale: {scale:F3}");
								bool isValid = scale > 1.0 && scale <= MaxScaleFactor;
								Console.WriteLine($"[DPI Debug] WSLg CIM scale factor valid: {isValid}");
								return isValid;
							}
						}
					}
				}
			}
		}
		catch (Win32Exception ex)
		{
			Console.WriteLine($"[DPI Debug] Win32Exception in WSLg detection: {ex.Message}");
		}
		catch (IOException ex)
		{
			Console.WriteLine($"[DPI Debug] IOException in WSLg detection: {ex.Message}");
		}
		catch (InvalidOperationException ex)
		{
			Console.WriteLine($"[DPI Debug] InvalidOperationException in WSLg detection: {ex.Message}");
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

