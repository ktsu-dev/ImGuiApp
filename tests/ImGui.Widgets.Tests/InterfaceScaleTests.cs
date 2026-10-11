// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>The interface scale outside a frame, where there is no context to read a font from.</summary>
[TestClass]
public sealed class InterfaceScaleTests
{
	[TestMethod]
	public void WithoutAContext_TheScaleIsOne()
	{
		Assert.AreEqual(1f, InterfaceScale.Current);
		Assert.AreEqual(12f, InterfaceScale.Pixels(12f));
	}
}
