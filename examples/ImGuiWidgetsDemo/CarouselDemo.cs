// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Examples.Widgets;

using System.Numerics;
using Hexa.NET.ImGui;
using ktsu.ImGui.Widgets;

/// <summary>
/// The carousel: pages dragged or flicked sideways, with page dots underneath.
/// </summary>
internal static class CarouselDemo
{
	private static readonly string[] Titles = ["Welcome", "Drag the pages", "Flick to skip ahead", "Tap a dot"];
	private static readonly Vector4[] Tints =
	[
		new(0.20f, 0.45f, 0.75f, 1f),
		new(0.25f, 0.60f, 0.40f, 1f),
		new(0.75f, 0.50f, 0.20f, 1f),
		new(0.55f, 0.30f, 0.65f, 1f),
	];

	private static int page;

	/// <summary>Gets the page the carousel shows.</summary>
	internal static int Page => page;

	/// <summary>
	/// Returns this section's state to its starting values. The demo keeps its state in statics, which
	/// outlive a harness.
	/// </summary>
	internal static void ResetState() => page = 0;

	/// <summary>Draws the demo section.</summary>
	internal static void Show()
	{
		if (DemoProbe.Header("Mobile - Carousel"))
		{
			ImGui.TextWrapped("Drag the pages sideways, or flick them. A quick flick turns the page even when the drag was short.");

			float width = MathF.Min(ImGui.GetContentRegionAvail().X, 420f);
			_ = ImGuiWidgets.Carousel("demoCarousel", ref page, Titles.Length, new Vector2(width, 160f), DrawPage);

			ImGui.TextUnformatted($"Page {page + 1} of {Titles.Length}");
			ImGui.SameLine();
			if (DemoProbe.Button("Next page##carouselNext"))
			{
				page = (page + 1) % Titles.Length;
			}
		}
	}

	private static void DrawPage(int index, Vector2 size)
	{
		Vector2 min = ImGui.GetCursorScreenPos();
		ImGui.GetWindowDrawList().AddRectFilled(min, min + size, ImGui.ColorConvertFloat4ToU32(Tints[index]), ImGui.GetStyle().FrameRounding);

		string title = Titles[index];
		Vector2 textSize = ImGui.CalcTextSize(title);
		ImGui.SetCursorScreenPos(min + ((size - textSize) * 0.5f));
		ImGui.TextUnformatted(title);
	}
}
