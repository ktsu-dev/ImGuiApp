// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Markdown.Tests;

using ktsu.ImGui.Markdown;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class ListMarkerTests
{
	[TestMethod]
	public void For_Unordered_ReturnsBullet()
	{
		Assert.AreEqual("-", ListMarker.For(ordered: false, itemIndex: 0, startNumber: 1, taskChecked: null));
	}

	[TestMethod]
	public void For_Ordered_CountsFromStart()
	{
		Assert.AreEqual("1.", ListMarker.For(ordered: true, itemIndex: 0, startNumber: 1, taskChecked: null));
		Assert.AreEqual("4.", ListMarker.For(ordered: true, itemIndex: 2, startNumber: 2, taskChecked: null));
	}

	[TestMethod]
	public void For_Task_ReturnsCheckbox()
	{
		Assert.AreEqual("[ ]", ListMarker.For(ordered: false, itemIndex: 0, startNumber: 1, taskChecked: false));
		Assert.AreEqual("[x]", ListMarker.For(ordered: false, itemIndex: 0, startNumber: 1, taskChecked: true));
	}
}
