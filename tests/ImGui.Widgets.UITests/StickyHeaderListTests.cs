// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.App.Testing;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>Drives <see cref="ImGuiWidgets.StickyHeaderList"/> on its own.</summary>
[TestClass]
public sealed class StickyHeaderListTests : WidgetTest
{
	private const string Id = "contacts";

	private static readonly string[] Letters = ["A", "B", "C", "D"];

	private string? pinned;

	private void Draw()
	{
		using ImGuiWidgets.StickyHeaderList list = new(Id, new Vector2(300f, 240f));
		foreach (string letter in Letters)
		{
			list.Section(letter);
			for (int i = 0; i < 12; i++)
			{
				ImGui.TextUnformatted($"{letter} contact {i}");
			}
		}

		list.Dispose();
		pinned = list.PinnedTitle;
	}

	private void ScrollDown(int clicks)
	{
		Rectangle list = RectOf(Id);
		for (int i = 0; i < clicks; i++)
		{
			Harness.Mouse.Wheel(list.MinX + 50, list.MinY + 100, -1);
		}
	}

	[TestMethod]
	public void AtTheTop_NoHeaderIsPinned()
	{
		Start(Draw);

		Assert.IsNull(pinned);
		Assert.IsFalse(IsVisible($"{Id}/pinned"));
	}

	[TestMethod]
	public void ScrolledIntoASection_ItsHeaderStaysAtTheTop()
	{
		Start(Draw);
		ScrollDown(1);

		Assert.AreEqual("A", pinned);
		Assert.IsTrue(IsVisible($"{Id}/pinned"));
		Assert.AreEqual(RectOf(Id).MinY, RectOf($"{Id}/pinned").MinY, 1);
	}

	[TestMethod]
	public void ScrolledFurther_TheNextSectionsHeaderTakesOver()
	{
		Start(Draw);
		ScrollDown(4);

		Assert.IsTrue(pinned is "B" or "C", $"Pinned {pinned}.");
		Assert.IsTrue(RectOf($"{Id}/pinned").MinY <= RectOf(Id).MinY + 1);
	}
}
