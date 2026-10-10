// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.WidgetGallery.Catalog;

using System.Collections.Generic;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Color;
using ktsu.ImGui.Widgets;
using ktsu.Semantics.Color;

/// <summary>The tiles in the DisplayAndStatus group.</summary>
internal static class DisplayAndStatusTiles
{
	/// <summary>Builds the group's entries.</summary>
	/// <returns>The entries, in gallery order.</returns>
	public static IEnumerable<GalleryEntry> Build()
	{
		const GalleryCategory Category = GalleryCategory.DisplayAndStatus;

		yield return new("Avatar", Category, [nameof(ImGuiWidgets.Avatar)], context =>
		{
			ImGuiWidgets.Avatar("##image", context.SampleTextureId, 48f, AvatarStatus.Online);
			ImGui.SameLine();
			ImGuiWidgets.Avatar("##ada", "Ada Lovelace", 48f, AvatarStatus.Away);
			ImGui.SameLine();
			ImGuiWidgets.Avatar("##grace", "Grace Hopper", 48f, AvatarStatus.Busy);
			ImGui.SameLine();
			ImGuiWidgets.Avatar("##alan", "Alan Turing", 48f, AvatarStatus.Offline);
		});

		yield return new("Badge", Category, [nameof(ImGuiWidgets.Badge), nameof(ImGuiWidgets.BadgeDot)], _ =>
		{
			ImGui.Button("Inbox", new Vector2(90f, 30f));
			ImGuiWidgets.Badge(7);
			ImGui.SameLine(0f, 24f);
			ImGui.Button("Alerts", new Vector2(90f, 30f));
			ImGuiWidgets.Badge(150);
			ImGui.SameLine(0f, 24f);
			ImGui.Button("Status", new Vector2(90f, 30f));
			ImGuiWidgets.Badge("new");
			ImGui.SameLine(0f, 24f);
			ImGui.Button("Chat", new Vector2(90f, 30f));
			ImGuiWidgets.BadgeDot();
		});

		string[] swatches = ["#e05252", "#e0a052", "#52b06a", "#5287e0", "#9a52e0"];
		yield return new("ColorIndicator", Category, [nameof(ImGuiWidgets.ColorIndicator)], _ =>
		{
			for (int i = 0; i < swatches.Length; i++)
			{
				if (i > 0)
				{
					ImGui.SameLine();
				}

				ImGuiWidgets.ColorIndicator(Color.FromHex(swatches[i]).ToImColor(), enabled: i != swatches.Length - 1);
			}
		});

		yield return new("Icon", Category, [nameof(ImGuiWidgets.Icon)], context =>
		{
			ImGuiWidgets.Icon("Sunset", context.SampleTextureId, 64f, ImGuiWidgets.IconAlignment.Vertical);
			ImGui.SameLine();
			ImGuiWidgets.Icon("Horizontal", context.SampleTextureId, 32f, ImGuiWidgets.IconAlignment.Horizontal);
		});

		yield return new("Image", Category, [nameof(ImGuiWidgets.Image), nameof(ImGuiWidgets.ImageScaleTo)], context =>
		{
			ImGuiWidgets.Image(context.SampleTextureId, new Vector2(96f, 96f));
			ImGui.SameLine();
			ImGuiWidgets.Image(context.SampleTextureId, new Vector2(96f, 96f), new ImGuiVector4(0.6f, 0.8f, 1f, 1f));
			ImGui.SameLine();
			ImGuiWidgets.ImageScaleTo(context.SampleTextureId, new Vector2(128f, 128f), new Vector2(160f, 96f));
		});

		yield return new(
			"Image alignment",
			Category,
			[
				nameof(ImGuiWidgets.ImageCentered),
				nameof(ImGuiWidgets.ImageCenteredWithin),
				nameof(ImGuiWidgets.ImageCenteredH),
				nameof(ImGuiWidgets.ImageCenteredV),
				nameof(ImGuiWidgets.ImageCenteredVH),
			],
			context =>
			{
				Vector2 box = new(130f, 90f);
				Vector2 image = new(40f, 40f);

				CatalogHelpers.Framed("##centered", "ImageCentered", box, () => ImGuiWidgets.ImageCentered(context.SampleTextureId, image));
				ImGui.SameLine();
				CatalogHelpers.Framed("##within", "ImageCenteredWithin", box, () => ImGuiWidgets.ImageCenteredWithin(context.SampleTextureId, image, CatalogHelpers.Inner(box)));
				ImGui.SameLine();
				CatalogHelpers.Framed("##h", "ImageCenteredH", box, () => ImGuiWidgets.ImageCenteredH(context.SampleTextureId, image));
				ImGui.SameLine();
				CatalogHelpers.Framed("##v", "ImageCenteredV", box, () => ImGuiWidgets.ImageCenteredV(context.SampleTextureId, image));
				ImGui.SameLine();
				CatalogHelpers.Framed("##vh", "ImageCenteredVH", box, () => ImGuiWidgets.ImageCenteredVH(context.SampleTextureId, image));
			})
		{
			ViewportWidth = 900,
		};

		yield return new(
			"Text alignment",
			Category,
			[
				nameof(ImGuiWidgets.Text),
				nameof(ImGuiWidgets.TextCentered),
				nameof(ImGuiWidgets.TextCenteredWithin),
				nameof(ImGuiWidgets.TextCenteredH),
				nameof(ImGuiWidgets.TextCenteredV),
				nameof(ImGuiWidgets.TextCenteredVH),
			],
			_ =>
			{
				Vector2 box = new(130f, 60f);

				CatalogHelpers.Framed("##text", "Text", box, () => ImGuiWidgets.Text("Text"));
				ImGui.SameLine();
				CatalogHelpers.Framed("##centered", "TextCentered", box, () => ImGuiWidgets.TextCentered("Centered"));
				ImGui.SameLine();
				CatalogHelpers.Framed("##within", "TextCenteredWithin", box, () => ImGuiWidgets.TextCenteredWithin("Within a box", CatalogHelpers.Inner(box)));
				ImGui.SameLine();
				CatalogHelpers.Framed("##h", "TextCenteredH", box, () => ImGuiWidgets.TextCenteredH("H"));
				ImGui.SameLine();
				CatalogHelpers.Framed("##v", "TextCenteredV", box, () => ImGuiWidgets.TextCenteredV("V"));
				ImGui.SameLine();
				CatalogHelpers.Framed("##vh", "TextCenteredVH", box, () => ImGuiWidgets.TextCenteredVH("VH"));
			})
		{
			ViewportWidth = 900,
		};

		int page = 2;
		yield return new("PageIndicator", Category, [nameof(ImGuiWidgets.PageIndicator)], _ =>
			page = ImGuiWidgets.PageIndicator("##pages", page, 6));

		yield return new("Tooltip", Category, [nameof(ImGuiWidgets.Tooltip)], _ =>
		{
			ImGui.Button("Hover me", new Vector2(110f, 30f));
			CatalogHelpers.Mark("tooltip-host");
			ImGuiWidgets.Tooltip("Tooltips wrap long descriptions and follow the pointer.");
		})
		{
			Interact = context =>
			{
				// Tooltips wait for the pointer to rest before they appear.
				context.Hover("tooltip-host");
				context.Harness.Step(60);
			},
		};
	}
}
