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
}
