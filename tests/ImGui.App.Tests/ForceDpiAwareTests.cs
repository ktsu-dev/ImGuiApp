// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.App.Test;

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
		Assert.IsTrue(scaleFactor > 0);
		Assert.IsTrue(scaleFactor <= 10.25); // MaxScaleFactor
	}

	[TestMethod]
	public void GetActualScaleFactor_ReturnsValidValue()
	{
		// Act
		double actualScale = ForceDpiAware.GetActualScaleFactor();

		// Assert
		Assert.IsTrue(actualScale > 0);
	}
}
