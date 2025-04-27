// Adapted From: https://github.com/Ryujinx/Ryujinx/blob/master/src/Ryujinx.Common/SystemInterop/GdiPlusHelper.cs
// License: MIT

namespace ktsu.ImGuiApp;

using System.Runtime.Versioning;

/// <summary>
/// Helper class for GDI+ operations.
/// </summary>
[SupportedOSPlatform("windows")]
public static partial class GdiPlusHelper
{
	/// <summary>
	/// Static constructor to initialize GDI+.
	/// </summary>
	static GdiPlusHelper() => CheckStatus(NativeMethods.GdiplusStartup(out _, NativeMethods.StartupInputEx.Default, out _));

	/// <summary>
	/// Checks the status of a GDI+ operation and throws an exception if it failed.
	/// </summary>
	/// <param name="gdiStatus">The status code returned by a GDI+ operation.</param>
	/// <exception cref="InvalidOperationException">Thrown when the GDI+ operation fails.</exception>
	private static void CheckStatus(int gdiStatus)
	{
		if (gdiStatus != 0)
		{
			throw new InvalidOperationException($"GDI Status Error: {gdiStatus}");
		}
	}

	/// <summary>
	/// Gets the DPI (dots per inch) along the X axis for a given window handle.
	/// </summary>
	/// <param name="hwnd">The handle to the window.</param>
	/// <returns>The DPI along the X axis.</returns>
	/// <exception cref="InvalidOperationException">Thrown when a GDI+ operation fails.</exception>
	public static float GetDpiX(IntPtr hwnd)
	{
		CheckStatus(NativeMethods.GdipCreateFromHWND(hwnd, out var graphicsHandle));
		CheckStatus(NativeMethods.GdipGetDpiX(graphicsHandle, out var result));
		CheckStatus(NativeMethods.GdipDeleteGraphics(graphicsHandle));

		return result;
	}
}
