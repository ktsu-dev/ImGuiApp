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
	private string? clicked;

	// Sections of full-width buttons, so a row hidden under the pinned header can report a click.
	private void DrawButtons()
	{
		using ImGuiWidgets.StickyHeaderList list = new(Id, new Vector2(300f, 240f));
		foreach (string letter in new[] { "A", "B" })
		{
			list.Section(letter);
			for (int i = 0; i < 12; i++)
			{
				string name = $"{letter}{i}";
				if (ImGui.Button($"{letter} contact {i}", new Vector2(-1f, 0f)))
				{
					clicked = name;
				}

				Mark(name);
			}
		}

		list.Dispose();
		pinned = list.PinnedTitle;
	}

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

	// Issue #712: the pinned header is painted over the rows, and has to take the pointer from them too.
	[TestMethod]
	public void ClickingThePinnedHeader_DoesNotClickTheRowBeneathIt()
	{
		Start(DrawButtons);
		ScrollDown(1);
		Step(2);
		Assert.AreEqual("A", pinned);

		Click($"{Id}/pinned");

		Assert.IsNull(clicked, $"The row '{clicked}' hidden under the pinned header was clicked through it.");
	}

	[TestMethod]
	public void WithAHeaderPinned_ARowBelowItStillTakesClicks()
	{
		Start(DrawButtons);
		ScrollDown(1);
		Step(2);

		Click("A6");

		Assert.AreEqual("A6", clicked);
	}
}
