// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Examples.Widgets;

using Hexa.NET.ImGui;
using ktsu.ImGui.App;
using ktsu.ImGui.Widgets;
using ktsu.Semantics.Paths;
using ktsu.Semantics.Strings;

/// <summary>
/// The mobile decorators: avatar, badge, rating and page indicator.
/// </summary>
internal static class MobileDecoratorsDemo
{
	/// <summary>Gets the rating currently shown.</summary>
	internal static float RatingValue => ratingValue;

	/// <summary>Gets or sets the count shown on the notification badge. The containers section's card button raises it.</summary>
	internal static int NotificationCount
	{
		get => notificationCount;
		set => notificationCount = value;
	}

	private static float ratingValue = 3.0f;
	private static float halfRatingValue = 3.5f;
	private static int notificationCount = 5;
	private static int carouselPage;

	/// <summary>
	/// Returns this section's state to its starting values. The demo keeps its state in statics, which
	/// outlive a harness.
	/// </summary>
	internal static void ResetState()
	{
		ratingValue = 3.0f;
		halfRatingValue = 3.5f;
		notificationCount = 5;
		carouselPage = 0;
	}

	/// <summary>Draws the demo section.</summary>
	internal static void Show()
	{
		if (DemoProbe.Header("Mobile - Decorators"))
		{
			AbsoluteFilePath ktsuIconPath = AppContext.BaseDirectory.As<AbsoluteDirectoryPath>() / "ktsu.png".As<FileName>();
			ImGuiAppTextureInfo ktsuTexture = ImGuiApp.GetOrLoadTexture(ktsuIconPath);

			ImGui.TextUnformatted("Avatars (circular image, initials fallback, presence dot):");
			ImGui.Separator();
			ImGuiWidgets.Avatar("AvatarImage", ktsuTexture.TextureId, status: AvatarStatus.Online);
			ImGui.SameLine();
			ImGuiWidgets.Avatar("AvatarJD", "John Doe", status: AvatarStatus.Away);
			ImGui.SameLine();
			ImGuiWidgets.Avatar("AvatarAB", "Ada Byron", status: AvatarStatus.Busy);
			ImGui.SameLine();
			ImGuiWidgets.Avatar("AvatarGrace", "Grace Hopper", status: AvatarStatus.Offline);

			ImGui.Separator();
			ImGui.TextUnformatted("Badges (overlay the previously drawn item):");
			DemoProbe.Button("Inbox");
			ImGuiWidgets.Badge(notificationCount);
			ImGui.SameLine(0, 30);
			DemoProbe.Button("Messages");
			ImGuiWidgets.Badge(150);
			ImGui.SameLine(0, 30);
			DemoProbe.Button("Updates");
			ImGuiWidgets.BadgeDot();
			DemoProbe.SliderInt("Count", ref notificationCount, 0, 200);

			ImGui.Separator();
			ImGui.TextUnformatted("Rating (click to set):");
			ImGuiWidgets.Rating("WholeRating", ref ratingValue);
			ImGui.SameLine();
			ImGui.TextUnformatted($"{ratingValue:0.#}");
			ImGui.TextUnformatted("Half-step, read-only:");
			ImGuiWidgets.Rating("HalfRating", ref halfRatingValue, allowHalf: true);
			ImGui.SameLine();
			ImGuiWidgets.Rating("ReadOnlyRating", ref halfRatingValue, allowHalf: true, readOnly: true);

			ImGui.Separator();
			ImGui.TextUnformatted("Page indicator (click a dot):");
			carouselPage = ImGuiWidgets.PageIndicator("Carousel", carouselPage, 5, interactive: true);
			ImGui.TextUnformatted($"Page {carouselPage + 1} of 5");
		}
	}
}
