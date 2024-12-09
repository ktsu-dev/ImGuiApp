// Adapted From: https://github.com/Ryujinx/Ryujinx/blob/master/src/Ryujinx.Common/SystemInterop/GdiPlusHelper.cs
// License: MIT

namespace ktsu.ImGuiApp;

using System.Runtime.InteropServices;
using System.Runtime.Versioning;

/// <summary>
/// Helper class for GDI+ operations.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
[SupportedOSPlatform("windows")]
public static partial class GdiPlusHelper
{
	private const string LibraryName = "gdiplus.dll";

	/// <summary>
	/// Static constructor to initialize GDI+.
	/// </summary>
	static GdiPlusHelper() => CheckStatus(GdiplusStartup(out _, StartupInputEx.Default, out _));

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
	/// Structure containing startup input parameters for GDI+.
	/// </summary>
	private struct StartupInputEx
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
	private struct StartupOutput
	{
		public IntPtr NotificationHook;
		public IntPtr NotificationUnhook;
	}

	[LibraryImport(LibraryName)]
	private static partial int GdiplusStartup(out IntPtr token, in StartupInputEx input, out StartupOutput output);

	[LibraryImport(LibraryName)]
	private static partial int GdipCreateFromHWND(IntPtr hwnd, out IntPtr graphics);

	[LibraryImport(LibraryName)]
	private static partial int GdipDeleteGraphics(IntPtr graphics);

	[LibraryImport(LibraryName)]
	private static partial int GdipGetDpiX(IntPtr graphics, out float dpi);

	/// <summary>
	/// Gets the DPI (dots per inch) along the X axis for a given window handle.
	/// </summary>
	/// <param name="hwnd">The handle to the window.</param>
	/// <returns>The DPI along the X axis.</returns>
	/// <exception cref="InvalidOperationException">Thrown when a GDI+ operation fails.</exception>
	public static float GetDpiX(IntPtr hwnd)
	{
		CheckStatus(GdipCreateFromHWND(hwnd, out IntPtr graphicsHandle));
		CheckStatus(GdipGetDpiX(graphicsHandle, out float result));
		CheckStatus(GdipDeleteGraphics(graphicsHandle));

		return result;
	}
}
