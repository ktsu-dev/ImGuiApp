// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.WidgetGallery.Catalog;

using System;
using System.Collections.Generic;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.App.Testing;
using ktsu.ImGui.Widgets;

/// <summary>The tiles in the MobileAndNavigation group.</summary>
internal static class MobileAndNavigationTiles
{
	/// <summary>Builds the group's entries.</summary>
	/// <returns>The entries, in gallery order.</returns>
	public static IEnumerable<GalleryEntry> Build()
	{
		const GalleryCategory Category = GalleryCategory.MobileAndNavigation;

		yield return SwipeableListItemTile(Category);
		yield return CarouselTile(Category);
		yield return PickerWheelTile(Category);
		yield return TimePickerTile(Category);
		yield return PullToRefreshTile(Category);
		yield return LongPressMenuTile(Category);
	}

	private static GalleryEntry LongPressMenuTile(GalleryCategory category)
	{
		string[] actions = ["Copy", "Share", "Delete"];

		return new("LongPressMenu", category, [nameof(ImGuiWidgets.LongPressMenu)], tile =>
			_ = ImGuiWidgets.LongPressMenu("photo", actions, () => ImGui.Button("Beach", new Vector2(110f, 80f))))
		{
			// Held until the menu opens, so the tile shows the menu beside its target.
			Interact = context =>
			{
				Rectangle target = context.RectOf("photo");
				context.Harness.Mouse.MoveTo(target.MinX + (target.Width / 2f), target.MinY + (target.Height / 2f));
				context.Harness.Step();
				HarnessMouse.Down(0);
				context.Harness.Step(45);
				HarnessMouse.Up(0);
				context.Harness.Step(30);
			},
			Cleanup = context =>
			{
				context.Harness.Keyboard.Press(ImGuiKey.Escape);
				context.Harness.Step(2);
			},
		};
	}

	private static GalleryEntry PullToRefreshTile(GalleryCategory category) =>
		new("PullToRefresh", category, [nameof(ImGuiWidgets.PullToRefresh)], tile =>
		{
			using ImGuiWidgets.PullToRefresh feed = new("feed", new Vector2(260f, 150f), isRefreshing: false);
			for (int i = 5; i >= 1; i--)
			{
				ImGui.TextUnformatted($"Post {i}");
			}
		})
		{
			// Held part of the way down, so the tile shows the gap and the spinner filling in.
			Interact = context =>
			{
				Rectangle region = context.RectOf("feed");
				float x = region.MinX + (region.Width / 2f);
				float y = region.MinY + 20f;
				context.Harness.Mouse.MoveTo(x, y);
				context.Harness.Step();
				HarnessMouse.Down(0);
				context.Harness.Step();
				for (int i = 1; i <= 30; i++)
				{
					context.Harness.Mouse.MoveTo(x, y + (i * 5f));
					context.Harness.Step();
				}
			},
			Cleanup = context =>
			{
				HarnessMouse.Up(0);
				context.Harness.Step();
			},
		};

	private static GalleryEntry PickerWheelTile(GalleryCategory category)
	{
		string[] sizes = ["Extra small", "Small", "Medium", "Large", "Extra large"];
		int size = 2;
		int quantity = 6;

		return new("PickerWheel", category, [nameof(ImGuiWidgets.PickerWheel)], tile =>
		{
			_ = ImGuiWidgets.PickerWheel("size", ref size, sizes);
			ImGui.SameLine();
			_ = ImGuiWidgets.PickerWheel("quantity", ref quantity, 1, 99, format: "0");
		});
	}

	private static GalleryEntry TimePickerTile(GalleryCategory category)
	{
		TimeOnly time = new(7, 30);

		return new("TimePicker", category, [nameof(ImGuiWidgets.TimePicker)], tile =>
			_ = ImGuiWidgets.TimePicker("alarm", ref time, new TimePickerOptions { TwelveHour = true, MinuteStep = 5 }));
	}

	private static GalleryEntry CarouselTile(GalleryCategory category)
	{
		string[] titles = ["Welcome", "Drag the pages", "Flick to skip ahead"];
		Vector4[] tints = [new(0.20f, 0.45f, 0.75f, 1f), new(0.25f, 0.60f, 0.40f, 1f), new(0.75f, 0.50f, 0.20f, 1f)];
		int page = 0;

		return new("Carousel", category, [nameof(ImGuiWidgets.Carousel)], tile =>
			_ = ImGuiWidgets.Carousel("tour", ref page, titles.Length, new Vector2(280f, 120f), (index, size) =>
			{
				Vector2 min = ImGui.GetCursorScreenPos();
				ImGui.GetWindowDrawList().AddRectFilled(min, min + size, ImGui.ColorConvertFloat4ToU32(tints[index]), 6f);
				Vector2 text = ImGui.CalcTextSize(titles[index]);
				ImGui.SetCursorScreenPos(min + ((size - text) * 0.5f));
				ImGui.TextUnformatted(titles[index]);
			}))
		{
			// Caught mid-slide, a third of the way from the first page to the second, so the tile shows the paging.
			Interact = context =>
			{
				Rectangle pages = context.RectOf("tour");
				float y = pages.MinY + (pages.Height / 2f);
				context.Harness.Mouse.MoveTo(pages.MinX + 200f, y);
				context.Harness.Step();
				HarnessMouse.Down(0);
				context.Harness.Step();
				for (int i = 1; i <= 20; i++)
				{
					context.Harness.Mouse.MoveTo(pages.MinX + 200f - (i * 5f), y);
					context.Harness.Step();
				}
			},
			Cleanup = context =>
			{
				HarnessMouse.Up(0);
				context.Harness.Step();
			},
		};
	}

	private static GalleryEntry SwipeableListItemTile(GalleryCategory category)
	{
		SwipeAction[] leading = [new("Read")];
		SwipeAction[] trailing = [new("Delete", Destructive: true), new("Archive")];
		string[] subjects = ["Lunch on Friday?", "Your order has shipped", "Minutes from Tuesday"];

		return new("SwipeableListItem", category, [nameof(ImGuiWidgets.SwipeableListItem)], tile =>
		{
			ImGui.BeginChild("##inbox", new Vector2(320f, 0f), ImGuiChildFlags.AutoResizeY);
			for (int i = 0; i < subjects.Length; i++)
			{
				string subject = subjects[i];
				_ = ImGuiWidgets.SwipeableListItem($"mail{i}", 40f, size =>
				{
					ImGui.SetCursorScreenPos(ImGui.GetCursorScreenPos() + new Vector2(8f, (size.Y - ImGui.GetTextLineHeight()) / 2f));
					ImGui.TextUnformatted(subject);
				}, leading, trailing);
				ImGui.Separator();
			}

			ImGui.EndChild();
		})
		{
			// The middle row is dragged open, so the tile shows the actions it reveals.
			Interact = context =>
			{
				Rectangle row = context.RectOf("mail1");
				float y = row.MinY + (row.Height / 2f);
				context.Harness.Mouse.Drag(row.MinX + 200f, y, row.MinX + 60f, y, steps: 60);
				context.Harness.Step(40);
			},
		};
	}
}
