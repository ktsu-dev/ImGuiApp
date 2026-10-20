// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class StickyHeaderGeometryTests
{
	private const float Header = 20f;
	private static readonly float[] Tops = [0f, 200f, 500f];

	[TestMethod]
	public void AtTheTop_NothingIsPinned()
	{
		(int index, _) = StickyHeaderGeometry.Pin(Tops, Header, 0f);

		Assert.AreEqual(-1, index);
	}

	[TestMethod]
	public void InsideASection_ItsHeaderSitsAtTheTopOfTheView()
	{
		(int index, float top) = StickyHeaderGeometry.Pin(Tops, Header, 90f);

		Assert.AreEqual(0, index);
		Assert.AreEqual(90f, top);
	}

	[TestMethod]
	public void TheNextHeader_PushesThePinnedOneUp()
	{
		(int index, float top) = StickyHeaderGeometry.Pin(Tops, Header, 190f);

		Assert.AreEqual(0, index);
		Assert.AreEqual(180f, top);
	}

	[TestMethod]
	public void PastTheNextHeader_ThatOneIsPinned()
	{
		(int index, float top) = StickyHeaderGeometry.Pin(Tops, Header, 260f);

		Assert.AreEqual(1, index);
		Assert.AreEqual(260f, top);
	}

	[TestMethod]
	public void InTheLastSection_NothingPushes()
	{
		(int index, float top) = StickyHeaderGeometry.Pin(Tops, Header, 900f);

		Assert.AreEqual(2, index);
		Assert.AreEqual(900f, top);
	}

	[TestMethod]
	public void WithNoSections_NothingIsPinned()
	{
		(int index, _) = StickyHeaderGeometry.Pin([], Header, 100f);

		Assert.AreEqual(-1, index);
	}
}
