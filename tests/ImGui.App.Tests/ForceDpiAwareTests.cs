// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.App.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class ForceDpiAwareTests
{
	[TestMethod]
	public void GetWindowScaleFactor_ReturnsValidValue()
	{
		// Act
		double scaleFactor = ForceDpiAware.GetWindowScaleFactor();

		// Assert
		Assert.IsGreaterThan(0, scaleFactor, "Scale factor should be greater than 0");
		Assert.IsLessThanOrEqualTo(10.25, scaleFactor, "Scale factor should not exceed MaxScaleFactor (10.25)"); // MaxScaleFactor
	}

	[TestMethod]
	public void GetActualScaleFactor_ReturnsValidValue()
	{
		// Act
		double actualScale = ForceDpiAware.GetActualScaleFactor();

		// Assert
		Assert.IsGreaterThan(0, actualScale, "Actual scale factor should be greater than 0");
	}

	[TestMethod]
	public void MacOSBackingScaleToDpi_StandardDisplay_ReturnsStandardDpi()
	{
		// Arrange: pixel width equals point width on a non-Retina display (1x backing scale).
		double dpi = ForceDpiAware.MacOSBackingScaleToDpi(1920, 1920);

		// Assert
		Assert.AreEqual(ForceDpiAware.StandardDpiScale, dpi, "A 1x display should map to the standard DPI scale");
	}

	[TestMethod]
	public void MacOSBackingScaleToDpi_RetinaDisplay_ReturnsDoubleStandardDpi()
	{
		// Arrange: pixels are twice the points on a Retina display (2x backing scale).
		double dpi = ForceDpiAware.MacOSBackingScaleToDpi(3840, 1920);

		// Assert
		Assert.AreEqual(ForceDpiAware.StandardDpiScale * 2.0, dpi, "A 2x Retina display should map to twice the standard DPI scale");
	}

	[TestMethod]
	public void MacOSBackingScaleToDpi_FractionalScale_ScalesProportionally()
	{
		// Arrange: a 1.5x backing scale (e.g. a scaled Retina mode).
		double dpi = ForceDpiAware.MacOSBackingScaleToDpi(2880, 1920);

		// Assert
		Assert.AreEqual(ForceDpiAware.StandardDpiScale * 1.5, dpi, "A 1.5x display should scale the standard DPI proportionally");
	}

	[TestMethod]
	public void MacOSBackingScaleToDpi_ZeroPointWidth_FallsBackToStandardDpi()
	{
		// Arrange: a zero point width (unreadable mode) must not divide by zero.
		double dpi = ForceDpiAware.MacOSBackingScaleToDpi(1920, 0);

		// Assert
		Assert.AreEqual(ForceDpiAware.StandardDpiScale, dpi, "A zero point width should fall back to the standard DPI scale");
	}
}
