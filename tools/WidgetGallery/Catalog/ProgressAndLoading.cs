// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.WidgetGallery.Catalog;

using System.Collections.Generic;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Widgets;
using ktsu.Semantics.Color;

/// <summary>The tiles in the ProgressAndLoading group.</summary>
internal static class ProgressAndLoadingTiles
{
	/// <summary>Builds the group's entries.</summary>
	/// <returns>The entries, in gallery order.</returns>
	public static IEnumerable<GalleryEntry> Build()
	{
		const GalleryCategory Category = GalleryCategory.ProgressAndLoading;

		yield return new("RadialProgressBar", Category, [nameof(ImGuiWidgets.RadialProgressBar)], _ =>
		{
			ImGuiWidgets.RadialProgressBar(0.72f, 36f, 8f);
			ImGui.SameLine();
			ImGuiWidgets.RadialProgressBar(0.35f, 36f, 4f, textMode: ImGuiRadialProgressBarTextMode.Custom, customText: "35");
		});

		yield return new("RadialCountdown", Category, [nameof(ImGuiWidgets.RadialCountdown), nameof(ImGuiWidgets.RadialCountUp)], _ =>
		{
			ImGuiWidgets.RadialCountdown(42f, 60f, 36f, 6f);
			ImGui.SameLine();
			ImGuiWidgets.RadialCountUp(95f, 120f, 36f, 6f);
		});

		yield return new("BufferingBar", Category, [nameof(ImGuiWidgets.BufferingBar)], _ =>
		{
			ImGuiWidgets.BufferingBar(0.6f, new Vector2(220f, 10f), new Srgb(0.2f, 0.2f, 0.22f), new Srgb(0.26f, 0.59f, 0.98f));
			ImGui.Dummy(new Vector2(0f, 4f));
		});

		yield return new("Spinner", Category, [nameof(ImGuiWidgets.Spinner)], _ =>
		{
			ImGuiWidgets.Spinner(14f, 4f, new Srgb(0.26f, 0.59f, 0.98f));
			ImGui.SameLine(0f, 16f);
			ImGuiWidgets.Spinner(10f, 3f, new Srgb(0.9f, 0.6f, 0.2f));
		})
		{
			// The spinner reserves the whole available width, so it is given a box to fill.
			Bounds = new Vector2(80f, 32f),
		};

		yield return new(
			"Skeleton loaders",
			Category,
			[nameof(ImGuiWidgets.SkeletonCircle), nameof(ImGuiWidgets.SkeletonLine), nameof(ImGuiWidgets.SkeletonRect)],
			_ =>
			{
				ImGuiWidgets.SkeletonCircle("##avatar", 44f);
				ImGui.SameLine();
				ImGui.BeginGroup();
				ImGuiWidgets.SkeletonLine("##title", 180f);
				ImGuiWidgets.SkeletonLine("##subtitle", 120f);
				ImGui.EndGroup();
				ImGuiWidgets.SkeletonRect("##body", new Vector2(240f, 70f));
			});
	}
}
