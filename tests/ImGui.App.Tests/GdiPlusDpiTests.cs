// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Tests;

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// The create-read-delete sequence behind <c>GdiPlusHelper.GetDpiX</c>. It runs on every window
/// move and resize, so a graphics context it fails to release is a handle leak that compounds for
/// as long as the drag lasts.
/// </summary>
[TestClass]
public class GdiPlusDpiTests
{
	private static readonly IntPtr GraphicsHandle = new(0x1234);
	private static readonly IntPtr WindowHandle = new(0x5678);

	[TestMethod]
	public void GetDpiX_EveryCallSucceeds_ReturnsDpiAndReleasesTheHandle()
	{
		// Arrange
		FakeGdiPlusGraphics gdiPlus = new() { Dpi = 144f };

		// Act
		float dpi = GdiPlusDpi.GetDpiX(gdiPlus, WindowHandle);

		// Assert
		Assert.AreEqual(144f, dpi, "The DPI read from the graphics context should be returned");
		CollectionAssert.AreEqual(new[] { GraphicsHandle }, gdiPlus.Deleted, "The graphics context should be released exactly once");
	}

	[TestMethod]
	public void GetDpiX_ReadingTheDpiFails_StillReleasesTheHandle()
	{
		// Arrange: GdipGetDpiX fails after the graphics context has been created.
		FakeGdiPlusGraphics gdiPlus = new() { DpiStatus = 2 };

		// Act
		InvalidOperationException exception = Assert.ThrowsExactly<InvalidOperationException>(
			() => GdiPlusDpi.GetDpiX(gdiPlus, WindowHandle),
			"A failing GDI+ call should surface as an InvalidOperationException");

		// Assert
		Assert.Contains("2", exception.Message, "The failure reported should be the one from reading the DPI");
		CollectionAssert.AreEqual(new[] { GraphicsHandle }, gdiPlus.Deleted, "The graphics context should be released even though reading the DPI threw");
	}

	[TestMethod]
	public void GetDpiX_CreatingTheGraphicsContextFails_ReleasesNothing()
	{
		// Arrange: there is no handle to release when GdipCreateFromHWND itself fails.
		FakeGdiPlusGraphics gdiPlus = new() { CreateStatus = 1 };

		// Act
		InvalidOperationException exception = Assert.ThrowsExactly<InvalidOperationException>(
			() => GdiPlusDpi.GetDpiX(gdiPlus, WindowHandle),
			"A failing GDI+ call should surface as an InvalidOperationException");

		// Assert
		Assert.Contains("1", exception.Message, "The failure reported should be the one from creating the graphics context");
		Assert.IsEmpty(gdiPlus.Deleted, "Nothing was created, so nothing should be released");
		Assert.IsFalse(gdiPlus.DpiWasRead, "The DPI should not be read from a graphics context that was never created");
	}

	[TestMethod]
	public void GetDpiX_ReleasingTheHandleFails_Throws()
	{
		// Arrange: the DPI reads fine and GdipDeleteGraphics is the call that fails.
		FakeGdiPlusGraphics gdiPlus = new() { DeleteStatus = 3 };

		// Act
		InvalidOperationException exception = Assert.ThrowsExactly<InvalidOperationException>(
			() => GdiPlusDpi.GetDpiX(gdiPlus, WindowHandle),
			"A failing release should not be swallowed");

		// Assert
		Assert.Contains("3", exception.Message, "The failure reported should be the one from releasing the graphics context");
	}

	/// <summary>
	/// Stands in for GDI+, handing out a known handle and failing whichever call the test asks it to.
	/// </summary>
	private sealed class FakeGdiPlusGraphics : IGdiPlusGraphics
	{
		/// <summary>Gets or sets the status <see cref="CreateFromHwnd"/> returns.</summary>
		public int CreateStatus { get; set; }

		/// <summary>Gets or sets the status <see cref="GetDpiX"/> returns.</summary>
		public int DpiStatus { get; set; }

		/// <summary>Gets or sets the status <see cref="DeleteGraphics"/> returns.</summary>
		public int DeleteStatus { get; set; }

		/// <summary>Gets or sets the DPI a successful read reports.</summary>
		public float Dpi { get; set; } = 96f;

		/// <summary>Gets a value indicating whether the DPI was read.</summary>
		public bool DpiWasRead { get; private set; }

		/// <summary>Gets the handles released, in the order they were released.</summary>
		public List<IntPtr> Deleted { get; } = [];

		/// <inheritdoc/>
		public int CreateFromHwnd(IntPtr hwnd, out IntPtr graphics)
		{
			graphics = CreateStatus == 0 ? GraphicsHandle : IntPtr.Zero;
			return CreateStatus;
		}

		/// <inheritdoc/>
		public int GetDpiX(IntPtr graphics, out float dpi)
		{
			DpiWasRead = true;
			dpi = DpiStatus == 0 ? Dpi : 0f;
			return DpiStatus;
		}

		/// <inheritdoc/>
		public int DeleteGraphics(IntPtr graphics)
		{
			Deleted.Add(graphics);
			return DeleteStatus;
		}
	}
}
