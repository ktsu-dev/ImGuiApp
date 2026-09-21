// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App;

/// <summary>
/// The GDI+ calls <see cref="GdiPlusHelper.GetDpiX(IntPtr)"/> makes, behind a seam.
/// </summary>
internal interface IGdiPlusGraphics
{
	/// <summary>Creates a graphics context for a window handle.</summary>
	/// <param name="hwnd">The handle to the window.</param>
	/// <param name="graphics">The created graphics context.</param>
	/// <returns>The GDI+ status code.</returns>
	public int CreateFromHwnd(IntPtr hwnd, out IntPtr graphics);

	/// <summary>Reads the DPI along the X axis of a graphics context.</summary>
	/// <param name="graphics">The graphics context to read.</param>
	/// <param name="dpi">The DPI along the X axis.</param>
	/// <returns>The GDI+ status code.</returns>
	public int ReadDpiX(IntPtr graphics, out float dpi);

	/// <summary>Releases a graphics context.</summary>
	/// <param name="graphics">The graphics context to release.</param>
	/// <returns>The GDI+ status code.</returns>
	public int DeleteGraphics(IntPtr graphics);
}

/// <summary>
/// The create-read-delete sequence behind <see cref="GdiPlusHelper.GetDpiX(IntPtr)"/>, separated
/// from the P/Invokes that carry it out.
/// </summary>
/// <remarks>
/// The sequence is what has to be got right — the graphics context is released whichever way
/// reading the DPI ends — and with the P/Invokes inlined there was no way to observe that without a
/// real window and a way to make GDI+ fail on demand. Holding them behind
/// <see cref="IGdiPlusGraphics"/> lets a test say "this call fails" and watch what happens to the
/// handle. It also keeps the sequence off the Windows-only surface, so that test runs on any host.
/// </remarks>
internal static class GdiPlusDpi
{
	/// <summary>
	/// Checks the status of a GDI+ operation and throws an exception if it failed.
	/// </summary>
	/// <param name="gdiStatus">The status code returned by a GDI+ operation.</param>
	/// <exception cref="InvalidOperationException">Thrown when the GDI+ operation fails.</exception>
	internal static void CheckStatus(int gdiStatus)
	{
		if (gdiStatus != 0)
		{
			throw new InvalidOperationException($"GDI Status Error: {gdiStatus}");
		}
	}

	/// <summary>
	/// Gets the DPI along the X axis for a window handle, releasing the graphics context it creates
	/// whether or not reading the DPI succeeds.
	/// </summary>
	/// <param name="graphics">The GDI+ operations to carry the sequence out with.</param>
	/// <param name="hwnd">The handle to the window.</param>
	/// <returns>The DPI along the X axis.</returns>
	/// <exception cref="InvalidOperationException">Thrown when a GDI+ operation fails.</exception>
	internal static float GetDpiX(IGdiPlusGraphics graphics, IntPtr hwnd)
	{
		Ensure.NotNull(graphics);

		CheckStatus(graphics.CreateFromHwnd(hwnd, out IntPtr graphicsHandle));

		float result;
		int deleteStatus;

		try
		{
			CheckStatus(graphics.ReadDpiX(graphicsHandle, out result));
		}
		finally
		{
			// Deliberately unchecked here: a failure reading the DPI is the one worth reporting, and
			// throwing out of a finally block would replace it with this one.
			deleteStatus = graphics.DeleteGraphics(graphicsHandle);
		}

		CheckStatus(deleteStatus);

		return result;
	}
}
