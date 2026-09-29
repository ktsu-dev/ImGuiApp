// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.WidgetGallery.Catalog;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Widgets;

/// <summary>The tiles in the InputAndControls group.</summary>
internal static class InputAndControlsTiles
{
	private static readonly string[] Fruit = ["apple", "apricot", "banana", "blueberry", "cherry", "grape", "grapefruit", "lemon"];

	/// <summary>Builds the group's entries.</summary>
	/// <returns>The entries, in gallery order.</returns>
	public static IEnumerable<GalleryEntry> Build()
	{
		const GalleryCategory Category = GalleryCategory.InputAndControls;

		bool wifi = true;
		bool bluetooth = false;
		yield return new("Switch", Category, [nameof(ImGuiWidgets.Switch)], _ =>
		{
			ImGuiWidgets.Switch("Wi-Fi", ref wifi);
			ImGuiWidgets.Switch("Bluetooth", ref bluetooth);
		});

		int view = 1;
		yield return new("SegmentedControl", Category, [nameof(ImGuiWidgets.SegmentedControl)], _ =>
			ImGuiWidgets.SegmentedControl("##view", ref view, "Day", "Week", "Month"));

		int quantity = 3;
		yield return new("Stepper", Category, [nameof(ImGuiWidgets.Stepper)], _ =>
			ImGuiWidgets.Stepper("Quantity", ref quantity, 1, 0, 99));

		// One minute in at 29.97 drop-frame, which is the label the scheme skips to.
		int position = 1800;
		yield return new("TimecodeField", Category, [nameof(ImGuiWidgets.TimecodeField)], _ =>
			ImGuiWidgets.TimecodeField("Position", ref position, TimecodeRate.Fps29_97Drop));

		float lower = 20f;
		float upper = 65f;
		yield return new("RangeSlider", Category, [nameof(ImGuiWidgets.RangeSlider)], _ =>
		{
			ImGui.PushItemWidth(260f);
			ImGuiWidgets.RangeSlider("Price", ref lower, ref upper, 0f, 100f, 5f);
			ImGui.PopItemWidth();
		});

		float padX = 0.35f;
		float padY = 0.7f;
		yield return new("XYPad", Category, [nameof(ImGuiWidgets.XYPad)], _ =>
			ImGuiWidgets.XYPad("Pan", ref padX, ref padY, new Vector2(140f, 140f)));

		ColorWheelValue wheel = new(Hue: 205f, Strength: 0.45f, Master: 0.2f);
		yield return new("ColorWheel", Category, [nameof(ImGuiWidgets.ColorWheel)], _ =>
			ImGuiWidgets.ColorWheel("Tint", ref wheel, 140f));

		// A teal-and-orange grade: cool shadows, a touch of warmth in the mids, warm highlights.
		ColorWheelValue lift = new(Hue: 190f, Strength: 0.35f, Master: -0.1f);
		ColorWheelValue gamma = new(Hue: 35f, Strength: 0.15f);
		ColorWheelValue gain = new(Hue: 30f, Strength: 0.4f, Master: 0.15f);
		yield return new("LiftGammaGain", Category, [nameof(ImGuiWidgets.LiftGammaGain)], _ =>
			ImGuiWidgets.LiftGammaGain("Grade", ref lift, ref gamma, ref gain, 130f));

		ImGuiKnobVariant[] variants = [ImGuiKnobVariant.Tick, ImGuiKnobVariant.Dot, ImGuiKnobVariant.Wiper, ImGuiKnobVariant.WiperDot, ImGuiKnobVariant.Stepped, ImGuiKnobVariant.Space];
		float[] knobValues = [0.25f, 0.5f, 0.8f, 0.4f, 0.6f, 0.7f];
		yield return new("Knob", Category, [nameof(ImGuiWidgets.Knob)], _ =>
		{
			for (int i = 0; i < variants.Length; i++)
			{
				if (i > 0)
				{
					ImGui.SameLine();
				}

				ImGuiWidgets.Knob(variants[i].ToString(), ref knobValues[i], 0f, 1f, variant: variants[i], size: 56f);
			}
		});

		float rating = 3.5f;
		yield return new("Rating", Category, [nameof(ImGuiWidgets.Rating)], _ =>
			ImGuiWidgets.Rating("##rating", ref rating, 5, allowHalf: true));

		yield return new("Chip", Category, [nameof(ImGuiWidgets.Chip)], _ =>
		{
			ImGuiWidgets.Chip("Design");
			ImGui.SameLine();
			ImGuiWidgets.Chip("Engineering", selected: true);
			ImGui.SameLine();
			ImGuiWidgets.Chip("Removable", false, out bool _);
		});

		string[] tags = ["All", "Audio", "Video", "Images", "Documents"];
		int tag = 2;
		yield return new("ChipGroup", Category, [nameof(ImGuiWidgets.ChipGroup)], _ =>
			ImGuiWidgets.ChipGroup("##tags", tags, ref tag));

		string pin = "4071";
		yield return new("PinInput", Category, [nameof(ImGuiWidgets.PinInput)], _ =>
			ImGuiWidgets.PinInput("##pin", ref pin, 6));

		SearchBoxOptions searchOptions = new("Search", Hint: "Filter fruit");
		string searchText = "gr*";
		yield return new("SearchBox", Category, [nameof(ImGuiWidgets.SearchBox)], _ =>
		{
			ImGui.PushItemWidth(240f);
			string[] matches = [.. ImGuiWidgets.SearchBox(ref searchOptions, ref searchText, Fruit, fruit => fruit)];
			ImGui.PopItemWidth();

			foreach (string match in matches)
			{
				ImGui.BulletText(match);
			}
		});

		SearchBoxRankedOptions rankedOptions = new("Fuzzy search");
		string rankedText = "bery";
		yield return new("SearchBoxRanked", Category, [nameof(ImGuiWidgets.SearchBoxRanked)], _ =>
		{
			ImGui.PushItemWidth(240f);
			string[] matches = [.. ImGuiWidgets.SearchBoxRanked(ref rankedOptions, ref rankedText, Fruit, fruit => fruit).Take(3)];
			ImGui.PopItemWidth();

			foreach (string match in matches)
			{
				ImGui.BulletText(match);
			}
		});

		CatalogHelpers.Difficulty difficulty = CatalogHelpers.Difficulty.Normal;
		string flavour = "Vanilla";
		Collection<string> flavours = ["Vanilla", "Chocolate", "Strawberry", "Pistachio"];
		yield return new("Combo", Category, [nameof(ImGuiWidgets.Combo)], _ =>
		{
			ImGui.PushItemWidth(180f);
			ImGuiWidgets.Combo("Difficulty", ref difficulty);
			ImGuiWidgets.Combo("Flavour", ref flavour, flavours);
			ImGui.PopItemWidth();
		})
		{
			ViewportHeight = 300,
			Interact = context => context.Click("Flavour"),
		};

		CatalogHelpers.Difficulty enumDifficulty = CatalogHelpers.Difficulty.Hard;
		yield return new("EnumCombo", Category, [nameof(ImGuiWidgets.EnumCombo)], _ =>
		{
			ImGui.PushItemWidth(180f);
			ImGuiWidgets.EnumCombo("Difficulty", ref enumDifficulty);
			ImGui.PopItemWidth();
		});

		string breadcrumb = "home/projects/imgui/widgets";
		yield return new("Breadcrumb", Category, [nameof(ImGuiWidgets.Breadcrumb)], _ =>
			ImGuiWidgets.Breadcrumb("##breadcrumb", ref breadcrumb));

		DateTime date = new(2026, 9, 29, 0, 0, 0, DateTimeKind.Utc);
		yield return new("DatePicker", Category, [nameof(ImGuiWidgets.DatePicker)], _ =>
			ImGuiWidgets.DatePicker("Due", ref date));

		DateTime year = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
		yield return new("YearPicker", Category, [nameof(ImGuiWidgets.YearPicker)], _ =>
			ImGuiWidgets.YearPicker("Year", ref year));

		bool notifications = true;
		yield return new("ToggleSwitch", Category, [nameof(ImGuiWidgets.ToggleSwitch)], _ =>
			ImGuiWidgets.ToggleSwitch("Notifications", ref notifications));

		bool bold = true;
		bool italic = false;
		yield return new("ToggleButton", Category, [nameof(ImGuiWidgets.ToggleButton)], _ =>
		{
			ImGuiWidgets.ToggleButton("Bold", ref bold, new Vector2(70f, 0f));
			ImGui.SameLine();
			ImGuiWidgets.ToggleButton("Italic", ref italic, new Vector2(70f, 0f));
		});

		yield return new("TransparentButton", Category, [nameof(ImGuiWidgets.TransparentButton)], _ =>
		{
			ImGuiWidgets.TransparentButton("Transparent");
			ImGui.SameLine();
			ImGui.Button("Regular");
		});

		string query = "inline";
		yield return new("InlineButton", Category, [nameof(ImGuiWidgets.InlineButton)], _ =>
		{
			ImGui.PushItemWidth(220f);
			ImGui.InputText("##inline", ref query, 64);
			ImGui.PopItemWidth();
			Vector2 min = ImGui.GetItemRectMin();
			Vector2 max = ImGui.GetItemRectMax();
			ImGuiWidgets.InlineButton("x", min, max, new Vector2(1f, 0.5f));
		});
	}
}
