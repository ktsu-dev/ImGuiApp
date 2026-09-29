// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.WidgetGallery;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.App;
using ktsu.ImGui.App.Testing;
using ktsu.ImGui.Widgets;

/// <summary>
/// Lays captured tiles out in captioned rows under category headings and renders the result,
/// through ImGui itself, into one image.
/// </summary>
/// <remarks>
/// The composite is drawn by the same headless renderer the tiles were, in the same font and
/// style, so captions and headings match the widgets they label and no text rendering has to be
/// reimplemented here. The layout is computed up front from sizes measured during capture, which
/// lets the harness be started at exactly the height the gallery needs.
/// </remarks>
internal static class GalleryCompositor
{
	private const float OuterPadding = 24f;
	private const float TileGap = 18f;
	private const float CaptionGap = 4f;
	private const float SectionGap = 22f;
	private const float HeadingGap = 10f;

	/// <summary>Renders a gallery of tiles and returns the image.</summary>
	/// <param name="title">The heading drawn across the top.</param>
	/// <param name="tiles">The tiles, in the order they are to appear.</param>
	/// <param name="settings">How the gallery is being rendered.</param>
	/// <returns>The composite image.</returns>
	public static Bitmap32 Compose(string title, IReadOnlyList<CapturedTile> tiles, GallerySettings settings)
	{
		if (tiles.Count == 0)
		{
			throw new ArgumentException("A gallery needs at least one tile.", nameof(tiles));
		}

		int width = settings.CompositeWidth;
		float lineHeight = tiles[0].LineHeight;
		GalleryLayout layout = GalleryLayout.Compute(tiles, width, lineHeight);

		List<ImGuiAppTextureInfo>? textures = null;

		void Render()
		{
			textures ??= [.. tiles.Select(tile => ImGuiApp.CreateTexture(tile.Pixels.Pixels, tile.Pixels.Width, tile.Pixels.Height))];
			Draw(title, tiles, textures, layout, width);
		}

		using ImGuiAppHarness harness = ImGuiAppHarness.Start(
			new ImGuiAppConfig
			{
				Title = title,
				OnRender = _ => Render(),
				OnStart = settings.LoadFonts,
				SaveIniSettings = false,
			},
			new HarnessOptions { Width = width, Height = layout.Height });

		harness.Mouse.MoveTo(-100f, -100f);
		harness.Step(3);

		return TileCapture.Crop(harness.Target, new Rectangle(0, 0, width, layout.Height), 0);
	}

	private static void Draw(string title, IReadOnlyList<CapturedTile> tiles, List<ImGuiAppTextureInfo> textures, GalleryLayout layout, int width)
	{
		ImGuiViewportPtr viewport = ImGui.GetMainViewport();
		ImGui.SetNextWindowPos(viewport.Pos);
		ImGui.SetNextWindowSize(viewport.Size);
		ImGui.SetNextWindowBgAlpha(1f);
		ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
		ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0f);

		const ImGuiWindowFlags Flags = ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoSavedSettings
			| ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoFocusOnAppearing;

		if (ImGui.Begin("##gallery", Flags))
		{
			ImDrawListPtr drawList = ImGui.GetWindowDrawList();
			uint accent = ImGui.GetColorU32(ImGuiCol.CheckMark);
			uint rule = ImGui.GetColorU32(ImGuiCol.Separator);

			ImGui.SetCursorScreenPos(layout.TitlePosition);
			ImGui.TextUnformatted(title);

			foreach (GalleryLayout.Heading heading in layout.Headings)
			{
				ImGui.SetCursorScreenPos(heading.Position);
				ImGui.PushStyleColor(ImGuiCol.Text, accent);
				ImGui.TextUnformatted(heading.Text);
				ImGui.PopStyleColor();

				float ruleY = heading.Position.Y + layout.LineHeight + (HeadingGap * 0.5f);
				drawList.AddLine(new Vector2(heading.Position.X, ruleY), new Vector2(width - OuterPadding, ruleY), rule);
			}

			for (int i = 0; i < tiles.Count; i++)
			{
				GalleryLayout.Placement placement = layout.Placements[i];
				CapturedTile tile = tiles[i];

				ImGui.SetCursorScreenPos(placement.Image);
				ImGuiWidgets.Image(textures[i].TextureId, new Vector2(tile.Pixels.Width, tile.Pixels.Height));

				ImGui.SetCursorScreenPos(placement.Caption);
				ImGui.TextDisabled(tile.Entry.Name);
			}
		}

		ImGui.End();
		ImGui.PopStyleVar(2);
	}

	/// <summary>Where everything in a composite goes, worked out before anything is drawn.</summary>
	private sealed class GalleryLayout
	{
		/// <summary>A category heading.</summary>
		/// <param name="Text">The heading's text.</param>
		/// <param name="Position">Its top-left corner.</param>
		public readonly record struct Heading(string Text, Vector2 Position);

		/// <summary>Where one tile's image and caption go.</summary>
		/// <param name="Image">The image's top-left corner.</param>
		/// <param name="Caption">The caption's top-left corner.</param>
		public readonly record struct Placement(Vector2 Image, Vector2 Caption);

		public Vector2 TitlePosition { get; private init; }

		public List<Heading> Headings { get; } = [];

		public List<Placement> Placements { get; } = [];

		public int Height { get; private set; }

		public float LineHeight { get; private init; }

		/// <summary>
		/// Flows tiles left to right in rows, starting a new row when the next tile would cross the
		/// right-hand margin and a new section whenever the category changes.
		/// </summary>
		public static GalleryLayout Compute(IReadOnlyList<CapturedTile> tiles, int width, float lineHeight)
		{
			const float padding = OuterPadding;
			const float gap = TileGap;
			const float captionGap = CaptionGap;
			float right = width - padding;

			GalleryLayout layout = new()
			{
				TitlePosition = new Vector2(padding, padding),
				LineHeight = lineHeight,
			};

			float y = padding + lineHeight;
			float x = padding;
			float rowHeight = 0f;
			GalleryCategory? category = null;

			foreach (CapturedTile tile in tiles)
			{
				if (tile.Entry.Category != category)
				{
					category = tile.Entry.Category;
					y += rowHeight + SectionGap;
					layout.Headings.Add(new Heading(GallerySettings.CategoryTitle(category.Value), new Vector2(padding, y)));
					y += lineHeight + HeadingGap;
					x = padding;
					rowHeight = 0f;
				}

				float tileWidth = MathF.Max(tile.Pixels.Width, tile.CaptionSize.X);
				float tileHeight = tile.Pixels.Height + captionGap + lineHeight;

				if (x > padding && x + tileWidth > right)
				{
					y += rowHeight + gap;
					x = padding;
					rowHeight = 0f;
				}

				Vector2 image = new(x + ((tileWidth - tile.Pixels.Width) / 2f), y);
				Vector2 caption = new(x + ((tileWidth - tile.CaptionSize.X) / 2f), y + tile.Pixels.Height + captionGap);
				layout.Placements.Add(new Placement(image, caption));

				x += tileWidth + gap;
				rowHeight = MathF.Max(rowHeight, tileHeight);
			}

			layout.Height = (int)MathF.Ceiling(y + rowHeight + padding);
			return layout;
		}
	}
}
