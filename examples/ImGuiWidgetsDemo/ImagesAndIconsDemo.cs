// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Examples.Widgets;

using System.Numerics;
using Hexa.NET.ImGui;
using ktsu.ImGui.App;
using ktsu.ImGui.Color;
using ktsu.ImGui.Popups;
using ktsu.ImGui.Widgets;

/// <summary>
/// Images and icons, with the click, double-click, context-menu and hover events icons raise.
/// </summary>
internal static class ImagesAndIconsDemo
{
	private static ImGuiPopups.MessageOK MessageOK { get; } = new();

	/// <summary>
	/// Returns this section's state to its starting values. The demo keeps its state in statics, which
	/// outlive a harness.
	/// </summary>
	internal static void ResetState()
	{
		// Nothing to reset: the section keeps no state beyond the popup it opens.
	}

	/// <summary>Draws the demo section.</summary>
	internal static void Show()
	{
		if (DemoProbe.Header("Images & Icons"))
		{
			ImGui.TextUnformatted("Interactive images and icons with events:");
			ImGui.Separator();

			// Image demo with color tinting
			ImGui.TextUnformatted("Clickable Image (with alpha-preserved tinting):");
			ImGuiVector4 tintColor = new(1.0f, 0.8f, 0.8f, 1.0f); // Light red tint
			if (ImGuiWidgets.Image(DemoContext.KtsuTexture.TextureId, new Vector2(64, 64), tintColor))
			{
				MessageOK.Open("Image Clicked", "You clicked the tinted image!");
			}

			ImGui.SameLine();
			if (ImGuiWidgets.Image(DemoContext.KtsuTexture.TextureId, new Vector2(64, 64))) // No tint
			{
				MessageOK.Open("Image Clicked", "You clicked the normal image!");
			}

			ImGui.Separator();

			// Icon demos
			ImGui.TextUnformatted("Interactive Icons:");

			float iconSize = ImGuiApp.EmsToPx(4.0f);

			ImGuiWidgets.Icon("Click Me", DemoContext.KtsuTexture.TextureId, iconSize, ImGuiWidgets.IconAlignment.Vertical,
				new ImGuiWidgets.IconOptions()
				{
					OnClick = () => MessageOK.Open("Click", "Single click detected!")
				});

			ImGui.SameLine();
			ImGuiWidgets.Icon("Double Click", DemoContext.KtsuTexture.TextureId, iconSize, ImGuiWidgets.IconAlignment.Vertical,
				new ImGuiWidgets.IconOptions()
				{
					OnDoubleClick = () => MessageOK.Open("Double Click", "Double click detected!")
				});

			ImGui.SameLine();
			ImGuiWidgets.Icon("Right Click", DemoContext.KtsuTexture.TextureId, iconSize, ImGuiWidgets.IconAlignment.Vertical,
				new ImGuiWidgets.IconOptions()
				{
					OnContextMenu = () =>
					{
						if (ImGui.MenuItem("Context Item 1"))
						{
							MessageOK.Open("Menu", "Context Item 1 selected");
						}

						if (ImGui.MenuItem("Context Item 2"))
						{
							MessageOK.Open("Menu", "Context Item 2 selected");
						}

						ImGui.Separator();
						if (ImGui.MenuItem("Context Item 3"))
						{
							MessageOK.Open("Menu", "Context Item 3 selected");
						}
					},
				});

			ImGui.SameLine();
			ImGuiWidgets.Icon("Hover Me", DemoContext.KtsuTexture.TextureId, iconSize, ImGuiWidgets.IconAlignment.Vertical,
				new ImGuiWidgets.IconOptions()
				{
					Tooltip = "This is a tooltip that appears when you hover over the icon!"
				});

			ImGui.Separator();

			ImGui.TextUnformatted("Horizontal Layout Icons:");
			ImGuiWidgets.Icon("Horizontal 1", DemoContext.KtsuTexture.TextureId, iconSize, ImGuiWidgets.IconAlignment.Horizontal);
			ImGuiWidgets.Icon("Horizontal 2", DemoContext.KtsuTexture.TextureId, iconSize, ImGuiWidgets.IconAlignment.Horizontal);
		}
	}

	/// <summary>
	/// Draws the popup this section opens. Called once the whole Advanced tab has drawn, so the popup
	/// is submitted after every section rather than in the middle of them.
	/// </summary>
	internal static void ShowPopups() => MessageOK.ShowIfOpen();
}
