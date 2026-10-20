// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.WidgetGallery.Catalog;

using System.Collections.Generic;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Widgets;
using ktsu.ImGui.Widgets.Overlays;

/// <summary>
/// The overlay and navigation tiles of the MobileAndNavigation group: the ones that draw over the
/// window, through an <see cref="OverlayHost"/>, and the bars and buttons that sit at its edges.
/// </summary>
internal static class MobileOverlayTiles
{
	// A phone-shaped viewport, so the overlays read at the size they are designed for.
	private const int PhoneWidth = 360;
	private const int PhoneHeight = 480;

	/// <summary>Builds the entries.</summary>
	/// <returns>The entries, in gallery order.</returns>
	public static IEnumerable<GalleryEntry> Build()
	{
		const GalleryCategory Category = GalleryCategory.MobileAndNavigation;

		yield return ToasterTile(Category);
		yield return ActionSheetTile(Category);
		yield return BottomSheetTile(Category);
		yield return NavigationDrawerTile(Category);
		yield return SearchOverlayTile(Category);
		yield return FloatingActionButtonTile(Category);
		yield return SpeedDialTile(Category);
		yield return BottomNavigationBarTile(Category);
		yield return AppBarTile(Category);
	}

	// Something to cover, so the dimming has a page to dim.
	private static void Page(string title)
	{
		ImGui.TextUnformatted(title);
		ImGui.Separator();
		for (int i = 1; i <= 12; i++)
		{
			ImGui.TextUnformatted($"Item {i}");
		}
	}

	private static GalleryEntry ToasterTile(GalleryCategory category)
	{
		OverlayHost host = new();
		ImGuiWidgets.Toaster toaster = new(host, "toasts");

		return new("Toaster", category, [nameof(ImGuiWidgets.Toaster)], tile =>
		{
			Page("Inbox");
			host.Render();
		})
		{
			ViewportWidth = PhoneWidth,
			ViewportHeight = PhoneHeight,
			Interact = context =>
			{
				toaster.Show("Message deleted", new ToastOptions { Duration = 0f, ActionLabel = "Undo" });
				context.Harness.Step(30);
			},
		};
	}

	private static GalleryEntry ActionSheetTile(GalleryCategory category)
	{
		OverlayHost host = new();
		ImGuiWidgets.ActionSheet sheet = new(host, "sheet");
		ActionSheetButton[] buttons = [new("Share"), new("Duplicate"), new("Delete photo", IsDestructive: true)];

		return new("ActionSheet", category, [nameof(ImGuiWidgets.ActionSheet)], tile =>
		{
			Page("Photos");
			host.Render();
		})
		{
			ViewportWidth = PhoneWidth,
			ViewportHeight = PhoneHeight,
			Interact = context =>
			{
				sheet.Open(buttons, _ => { }, "Beach.jpg");
				context.Harness.Step(30);
			},
			Cleanup = context =>
			{
				sheet.Cancel();
				context.Harness.Step(30);
			},
		};
	}

	private static GalleryEntry BottomSheetTile(GalleryCategory category)
	{
		OverlayHost host = new();
		string[] places = ["Harbour Bridge", "Opera House", "Botanic Garden", "Bondi Beach", "Manly Ferry"];
		ImGuiWidgets.BottomSheet sheet = new(host, "sheet", size =>
		{
			ImGui.TextUnformatted("Nearby");
			ImGui.Separator();
			foreach (string place in places)
			{
				ImGui.Selectable(place, false, ImGuiSelectableFlags.None, new Vector2(size.X, 0f));
			}
		});

		return new("BottomSheet", category, [nameof(ImGuiWidgets.BottomSheet)], tile =>
		{
			Page("Map");
			host.Render();
		})
		{
			ViewportWidth = PhoneWidth,
			ViewportHeight = PhoneHeight,
			Interact = context =>
			{
				sheet.Open(BottomSheetDetent.Half);
				context.Harness.Step(60);
			},
		};
	}

	private static GalleryEntry NavigationDrawerTile(GalleryCategory category)
	{
		OverlayHost host = new();
		string[] destinations = ["Inbox", "Starred", "Sent", "Drafts", "Trash"];
		ImGuiWidgets.NavigationDrawer drawer = new(host, "drawer", () =>
		{
			ImGui.TextUnformatted("Mail");
			ImGui.Separator();
			foreach (string destination in destinations)
			{
				ImGui.Selectable(destination, destination == "Inbox");
			}
		});

		return new("NavigationDrawer", category, [nameof(ImGuiWidgets.NavigationDrawer)], tile =>
		{
			Page("Inbox");
			host.Render();
		})
		{
			ViewportWidth = PhoneWidth,
			ViewportHeight = PhoneHeight,
			Interact = context =>
			{
				drawer.Open();
				context.Harness.Step(60);
			},
		};
	}

	private static GalleryEntry SearchOverlayTile(GalleryCategory category)
	{
		OverlayHost host = new();
		ImGuiWidgets.SearchOverlay search = new(host, "search");
		string[] fruit = ["Apple", "Apricot", "Avocado", "Banana", "Blueberry", "Cherry", "Grape", "Papaya", "Pear"];

		return new("SearchOverlay", category, [nameof(ImGuiWidgets.SearchOverlay)], tile =>
		{
			Page("Fruit");
			host.Render();
		})
		{
			ViewportWidth = PhoneWidth,
			ViewportHeight = PhoneHeight,
			Interact = context =>
			{
				search.Open(fruit, _ => { }, "Search fruit");
				context.Harness.Step(30);
				context.Harness.Keyboard.Type("ap");
				context.Harness.Step(4);
			},
		};
	}

	private static GalleryEntry FloatingActionButtonTile(GalleryCategory category) =>
		new("FloatingActionButton", category, [nameof(ImGuiWidgets.FloatingActionButton)], tile =>
		{
			ImGui.BeginChild("##notes", new Vector2(260f, 180f), ImGuiChildFlags.Borders);
			Page("Notes");
			_ = ImGuiWidgets.FloatingActionButton("add", "+");
			ImGui.EndChild();
		});

	private static GalleryEntry SpeedDialTile(GalleryCategory category)
	{
		SpeedDialAction[] actions = [new("P", "Photo"), new("N", "Note"), new("R", "Reminder")];

		return new("SpeedDial", category, [nameof(ImGuiWidgets.SpeedDial)], tile =>
		{
			ImGui.BeginChild("##create", new Vector2(260f, 300f), ImGuiChildFlags.Borders);
			Page("Today");
			_ = ImGuiWidgets.SpeedDial("create", "+", actions);
			ImGui.EndChild();
		})
		{
			ViewportHeight = PhoneHeight,
			Interact = context =>
			{
				context.Harness.Click("create");
				context.Harness.Step(30);
			},
		};
	}

	private static GalleryEntry BottomNavigationBarTile(GalleryCategory category)
	{
		BottomNavigationItem[] items = [new("H", "Home"), new("S", "Search"), new("L", "Library"), new("P", "Profile")];
		int selected = 1;

		return new("BottomNavigationBar", category, [nameof(ImGuiWidgets.BottomNavigationBar)], tile =>
		{
			ImGui.BeginChild("##phone", new Vector2(320f, 150f), ImGuiChildFlags.Borders);
			ImGui.BeginChild("##page", new Vector2(0f, -ImGuiWidgets.BottomNavigationBarHeight));
			ImGui.TextWrapped("Search everything you have saved.");
			ImGui.EndChild();
			_ = ImGuiWidgets.BottomNavigationBar("nav", ref selected, items);
			ImGui.EndChild();
		});
	}

	private static GalleryEntry AppBarTile(GalleryCategory category)
	{
		AppBarOptions options = new()
		{
			Leading = new AppBarAction("<", "Back"),
			Actions = [new AppBarAction("?", "Search"), new AppBarAction("...", "More")],
		};

		return new("AppBar", category, [nameof(ImGuiWidgets.AppBar)], tile =>
		{
			ImGui.BeginChild("##phone", new Vector2(320f, 130f), ImGuiChildFlags.Borders);
			_ = ImGuiWidgets.AppBar("bar", "Inbox", options);
			ImGui.TextUnformatted("Lunch on Friday?");
			ImGui.TextUnformatted("Your order has shipped");
			ImGui.EndChild();
		});
	}
}
