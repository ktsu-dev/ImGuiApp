// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App;

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
	internal static void CheckStatus(int gdiStatus) => GdiPlusDpi.CheckStatus(gdiStatus);

	/// <summary>
	/// Gets the DPI (dots per inch) along the X axis for a given window handle.
	/// </summary>
	/// <param name="hwnd">The handle to the window.</param>
	/// <returns>The DPI along the X axis.</returns>
	/// <exception cref="InvalidOperationException">Thrown when a GDI+ operation fails.</exception>
	public static float GetDpiX(IntPtr hwnd) => GdiPlusDpi.GetDpiX(NativeGdiPlusGraphics.Instance, hwnd);

	/// <summary>
	/// The GDI+ operations <see cref="GdiPlusHelper.GetDpiX(IntPtr)"/> runs, as the platform actually
	/// provides them.
	/// </summary>
	private sealed class NativeGdiPlusGraphics : IGdiPlusGraphics
	{
		/// <summary>Gets the single instance; the type holds no state of its own.</summary>
		internal static NativeGdiPlusGraphics Instance { get; } = new();

		/// <inheritdoc/>
		public int CreateFromHwnd(IntPtr hwnd, out IntPtr graphics) => NativeMethods.GdipCreateFromHWND(hwnd, out graphics);

		/// <inheritdoc/>
		public int GetDpiX(IntPtr graphics, out float dpi) => NativeMethods.GdipGetDpiX(graphics, out dpi);

		/// <inheritdoc/>
		public int DeleteGraphics(IntPtr graphics) => NativeMethods.GdipDeleteGraphics(graphics);
	}
}
