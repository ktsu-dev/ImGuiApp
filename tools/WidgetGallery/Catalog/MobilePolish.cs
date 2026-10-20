// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.WidgetGallery.Catalog;

using System.Collections.Generic;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.App.Testing;
using ktsu.ImGui.Widgets;
using ktsu.ImGui.Widgets.Overlays;

/// <summary>
/// The list and onboarding tiles of the MobileAndNavigation group: expandable rows, a list that
/// loads more as it is scrolled, a list whose section headers pin, and a coach mark tour.
/// </summary>
internal static class MobilePolishTiles
{
	/// <summary>Builds the entries.</summary>
	/// <returns>The entries, in gallery order.</returns>
	public static IEnumerable<GalleryEntry> Build()
	{
		const GalleryCategory Category = GalleryCategory.MobileAndNavigation;

		yield return ExpandableListItemTile(Category);
		yield return AccordionTile(Category);
		yield return InfiniteListTile(Category);
		yield return StickyHeaderListTile(Category);
		yield return TutorialTile(Category);
	}

	private static GalleryEntry ExpandableListItemTile(GalleryCategory category)
	{
		bool shipping = true;
		bool returns = false;

		return new("ExpandableListItem", category, [nameof(ImGuiWidgets.ExpandableListItem)], tile =>
		{
			ImGui.BeginChild("##order", new Vector2(280f, 0f), ImGuiChildFlags.AutoResizeY);
			_ = ImGuiWidgets.ExpandableListItem("Shipping", ref shipping, () =>
			{
				ImGui.TextUnformatted("Standard, 3 to 5 days");
				ImGui.TextUnformatted("Arrives by Friday");
			});
			_ = ImGuiWidgets.ExpandableListItem("Returns", ref returns, () => ImGui.TextUnformatted("Free within 30 days"));
			ImGui.EndChild();
		});
	}

	private static GalleryEntry AccordionTile(GalleryCategory category)
	{
		ImGuiWidgets.Accordion faq = new();
		faq.Open("How do I reset my password?");

		return new("Accordion", category, [nameof(ImGuiWidgets.Accordion)], tile =>
		{
			ImGui.BeginChild("##faq", new Vector2(300f, 0f), ImGuiChildFlags.AutoResizeY);
			_ = faq.Item("How do I reset my password?", () => ImGui.TextWrapped("Open Settings, then Account, then Reset password."));
			_ = faq.Item("Can I change my email?", () => ImGui.TextUnformatted("Yes, under Account."));
			_ = faq.Item("Where are my receipts?", () => ImGui.TextUnformatted("Under Orders."));
			ImGui.EndChild();
		});
	}

	private static GalleryEntry InfiniteListTile(GalleryCategory category) =>
		new("InfiniteList", category, [nameof(ImGuiWidgets.InfiniteList)], tile =>
			ImGuiWidgets.InfiniteList("feed", new Vector2(260f, 180f), 20, i => ImGui.TextUnformatted($"Post {i + 1}"), () => { }, isLoading: true))
		{
			// Scrolled to the end, so the tile shows the loading row under the last post.
			Interact = context =>
			{
				Rectangle list = context.RectOf("feed");
				context.Harness.Mouse.Wheel(list.MinX + (list.Width / 2f), list.MinY + (list.Height / 2f), -40);
				context.Harness.Step(4);
			},
		};

	private static GalleryEntry StickyHeaderListTile(GalleryCategory category)
	{
		string[][] sections =
		[
			["Ada", "Alan", "Alice", "Andrew", "Anita", "Arthur"],
			["Barbara", "Bjarne", "Brian", "Butler"],
			["Claude", "Dennis", "Donald"],
		];
		string[] titles = ["A", "B", "C-D"];

		return new("StickyHeaderList", category, [nameof(ImGuiWidgets.StickyHeaderList)], tile =>
		{
			using ImGuiWidgets.StickyHeaderList list = new("contacts", new Vector2(240f, 170f));
			for (int s = 0; s < sections.Length; s++)
			{
				list.Section(titles[s]);
				foreach (string name in sections[s])
				{
					ImGui.TextUnformatted(name);
				}
			}
		})
		{
			// Scrolled part way into the first section, so its header is pinned over the rows.
			Interact = context =>
			{
				Rectangle list = context.RectOf("contacts");
				context.Harness.Mouse.Wheel(list.MinX + (list.Width / 2f), list.MinY + (list.Height / 2f), -1);
				context.Harness.Step(4);
			},
		};
	}

	private static GalleryEntry TutorialTile(GalleryCategory category)
	{
		OverlayHost host = new();
		CoachMarkStep[] steps =
		[
			new("new", "Start here", "New makes a blank document."),
			new("open", "Pick up where you left off", "Open lists your recent files."),
		];
		ImGuiWidgets.Tutorial tour = new(host, "tour", steps);

		return new("Tutorial", category, [nameof(ImGuiWidgets.Tutorial)], tile =>
		{
			_ = ImGui.Button("New");
			tour.Target("new");
			ImGui.SameLine();
			_ = ImGui.Button("Open");
			tour.Target("open");
			for (int i = 1; i <= 8; i++)
			{
				ImGui.TextUnformatted($"Recent file {i}");
			}

			host.Render();
		})
		{
			ViewportWidth = 360,
			ViewportHeight = 320,
			Interact = context =>
			{
				tour.Start();
				context.Harness.Step(30);
			},
			Cleanup = context =>
			{
				tour.Skip();
				context.Harness.Step(30);
			},
		};
	}
}
