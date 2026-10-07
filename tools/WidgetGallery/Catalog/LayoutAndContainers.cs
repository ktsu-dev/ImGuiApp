// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.WidgetGallery.Catalog;

using System.Collections.Generic;
using System.Globalization;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Widgets;
using ktsu.ImGui.Widgets.Overlays;
using ktsu.Semantics.Color;

/// <summary>The tiles in the LayoutAndContainers group.</summary>
internal static class LayoutAndContainersTiles
{
	/// <summary>Builds the group's entries.</summary>
	/// <returns>The entries, in gallery order.</returns>
	public static IEnumerable<GalleryEntry> Build()
	{
		const GalleryCategory Category = GalleryCategory.LayoutAndContainers;

		yield return new("Card", Category, [nameof(ImGuiWidgets.Card)], _ =>
		{
			using (new ImGuiWidgets.Card(240f))
			{
				ImGui.TextUnformatted("Weekly report");
				ImGui.TextDisabled("Generated 29 September");
				ImGui.Spacing();
				ImGui.TextWrapped("Cards pad and frame whatever is drawn inside them.");
			}
		});

		yield return ToolbarTile(Category);

		string[] cells = ["Alpha", "Bravo", "Charlie", "Delta", "Echo", "Foxtrot", "Golf", "Hotel", "India"];
		yield return new("Grid", Category, [nameof(ImGuiWidgets.RowMajorGrid), nameof(ImGuiWidgets.ColumnMajorGrid)], _ =>
			ImGuiWidgets.RowMajorGrid(
				"##grid",
				cells,
				_ => new Vector2(84f, 28f),
				(item, _, itemSize) => ImGui.Button(item, itemSize),
				new ImGuiWidgets.GridOptions { GridSize = new Vector2(300f, 0f), FitToContents = true }));

		ImGuiWidgets.TabPanel tabs = new("##tabs", closable: true, reorderable: true);
		tabs.AddTab("Scene", () => ImGui.TextUnformatted("Scene hierarchy goes here."));
		tabs.AddTab("Assets", () => ImGui.TextUnformatted("Asset browser"));
		tabs.AddTab("Console", () => ImGui.TextUnformatted("Log output"), isDirty: true);
		yield return new("TabPanel", Category, [nameof(ImGuiWidgets.TabPanel)], _ => tabs.Draw())
		{
			Bounds = new Vector2(320f, 80f),
		};

		ImGuiWidgets.DividerContainer divider = new("##divider");
		divider.Add("left", 0.35f, true, _ => ImGui.TextUnformatted("Outline"));
		divider.Add("right", 0.65f, false, _ => ImGui.TextUnformatted("Editor"));
		yield return new("DividerContainer", Category, [nameof(ImGuiWidgets.DividerContainer)], _ => divider.Tick(1f / 60f))
		{
			Bounds = new Vector2(360f, 120f),
		};

		float paneWidth = 140f;
		float paneHeight = 60f;
		yield return new("Splitters", Category, [nameof(ImGuiWidgets.VerticalSplitter), nameof(ImGuiWidgets.HorizontalSplitter)], _ =>
		{
			ImGui.BeginChild("##left", new Vector2(paneWidth, 130f), ImGuiChildFlags.Borders);
			ImGui.TextUnformatted("Left");
			ImGui.EndChild();
			ImGui.SameLine(0f, 0f);
			ImGuiWidgets.VerticalSplitter("##vsplit", ref paneWidth, 80f, 240f, 130f, 6f);
			ImGui.SameLine(0f, 0f);
			ImGui.BeginGroup();
			ImGui.BeginChild("##top", new Vector2(160f, paneHeight), ImGuiChildFlags.Borders);
			ImGui.TextUnformatted("Top");
			ImGui.EndChild();
			ImGuiWidgets.HorizontalSplitter("##hsplit", ref paneHeight, 30f, 100f, 160f, 6f);
			ImGui.BeginChild("##bottom", new Vector2(160f, 130f - paneHeight - 6f), ImGuiChildFlags.Borders);
			ImGui.TextUnformatted("Bottom");
			ImGui.EndChild();
			ImGui.EndGroup();
		});

		yield return new("Tree", Category, [nameof(ImGuiWidgets.Tree)], _ =>
		{
			ImGuiWidgets.Tree.Branch("Fruit", ImGuiTreeNodeFlags.DefaultOpen, () =>
			{
				ImGuiWidgets.Tree.Branch("Citrus", ImGuiTreeNodeFlags.DefaultOpen, () =>
				{
					ImGuiWidgets.Tree.Leaf(() => ImGui.TextUnformatted("Lemon"));
					ImGuiWidgets.Tree.Leaf(() => ImGui.TextUnformatted("Lime"));
				});
				ImGuiWidgets.Tree.Leaf(() => ImGui.TextUnformatted("Apple"));
			});
		});

		yield return ReorderableTreeTile(Category);

		yield return new("IconTreeNode", Category, [nameof(ImGuiWidgets.IconTreeNode)], _ =>
		{
			if (ImGuiWidgets.IconTreeNode("Project", "\uE2C7", Color.FromHex("#e6b333"), ImGuiTreeNodeFlags.DefaultOpen))
			{
				if (ImGuiWidgets.IconTreeNode("Sources", "\uE2C7", Color.FromHex("#e6b333"), ImGuiTreeNodeFlags.DefaultOpen))
				{
					ImGui.BulletText("Program.cs");
					ImGui.TreePop();
				}

				ImGui.BulletText("README.md");
				ImGui.TreePop();
			}
		});

		ImGuiWidgets.ImageCanvasState canvas = new();
		yield return new("ImageCanvas", Category, [nameof(ImGuiWidgets.ImageCanvas)], context =>
			ImGuiWidgets.ImageCanvas("##canvas", context.SampleTextureId, new Vector2(SampleImage.Size, SampleImage.Size), canvas, new Vector2(260f, 170f)));

		yield return AssetBrowser(Category);

		yield return ImageCompareEntry(Category);

		yield return CropOverlayTile(Category);
		yield return TransformBoxTile(Category);

		yield return PixelLoupeTile(Category);

		bool enabled = true;
		int count = 12;
		float ratio = 0.62f;
		string title = "Main camera";
		Vector3 position = new(1.5f, 0f, -4f);
		Color tint = Color.FromHex("#4fa3e0");
		CatalogHelpers.Difficulty difficulty = CatalogHelpers.Difficulty.Normal;
		List<string> tags = ["player", "physics"];
		yield return new("PropertyGrid", Category, [nameof(ImGuiWidgets.PropertyGrid)], _ =>
		{
			using ImGuiWidgets.PropertyGrid grid = new("##properties");

			using (grid.Section("Object"))
			{
				grid.Value("Name", ref title);
				grid.Value("Enabled", ref enabled);
				grid.Value("Position", ref position);
				grid.Value("Tint", ref tint);
			}

			using (grid.Section("Settings"))
			{
				grid.Value("Count", ref count);
				grid.Value("Ratio", ref ratio, 0f, 1f);
				grid.Enum("Difficulty", ref difficulty);
				grid.List("Tags", tags);
			}
		})
		{
			Bounds = new Vector2(380f, 290f),
			ViewportHeight = 480,
		};

		ImGuiWidgets.VirtualTableColumn[] columns =
		[
			new("Name", ImGuiTableColumnFlags.WidthStretch),
			new("Size", ImGuiTableColumnFlags.WidthFixed, 70f),
		];
		ImGuiWidgets.VirtualTableOptions tableOptions = new() { RowHeight = 20f, OuterSize = new Vector2(300f, 170f) };
		yield return new("VirtualTable", Category, [nameof(ImGuiWidgets.VirtualTable)], _ =>
			ImGuiWidgets.VirtualTable("##table", 100_000, columns, row =>
			{
				ImGui.TextUnformatted(string.Create(CultureInfo.InvariantCulture, $"asset_{row:D5}.png"));
				ImGui.TableNextColumn();
				ImGui.TextUnformatted(string.Create(CultureInfo.InvariantCulture, $"{(row * 37 % 900) + 12} KB"));
			}, tableOptions));

		OverlayHost overlays = new();
		overlays.Show("toast", () =>
		{
			ImGui.SetNextWindowPos(new Vector2(24f, 40f));
			ImGui.SetNextWindowBgAlpha(0.95f);

			if (ImGui.Begin("##toast", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings))
			{
				ImGui.TextUnformatted("Saved 3 files");
				ImGui.TextDisabled("Overlays stack in layers above the app.");
			}

			ImGui.End();
		});
		yield return new("OverlayHost", Category, [], _ => overlays.Render());
	}

	/// <summary>Builds the asset browser tile, with two tiles selected so the highlight shows.</summary>
	/// <param name="category">The group the tile belongs to.</param>
	/// <returns>The entry.</returns>
	private static GalleryEntry AssetBrowser(GalleryCategory category)
	{
		ImGuiWidgets.AssetBrowserState assets = new();
		_ = assets.Click(1, ctrl: false, shift: false);
		_ = assets.Click(2, ctrl: true, shift: false);
		ImGuiWidgets.AssetBrowserOptions options = new() { TileSize = 64f, Size = new Vector2(300f, 170f) };

		return new("AssetBrowser", category, [nameof(ImGuiWidgets.AssetBrowser)], context =>
			ImGuiWidgets.AssetBrowser(
				"##assets",
				200,
				index => string.Create(CultureInfo.InvariantCulture, $"asset_{index:D3}.png"),
				index => index % 3 == 0 ? 0 : context.SampleTextureId,
				assets,
				options));
	}

	/// <summary>
	/// Builds the ImageCompare tile: the sample image against its inverse, split a little left of
	/// centre so both sides of the divider show the picture.
	/// </summary>
	/// <param name="category">The group the tile belongs to.</param>
	/// <returns>The entry.</returns>
	private static GalleryEntry ImageCompareEntry(GalleryCategory category)
	{
		Vector2 imageSize = new(SampleImage.Size, SampleImage.Size);
		Vector2 canvasSize = new(260f, 170f);
		ImGuiWidgets.ImageCanvasState state = new();
		state.FitToViewport(imageSize, canvasSize);
		float split = 0.45f;
		return new("ImageCompare", category, [nameof(ImGuiWidgets.ImageCompare)], context =>
			ImGuiWidgets.ImageCompare("##compare", context.SampleTextureId, context.InvertedSampleTexture.TextureId, imageSize, state, ref split, canvasSize));
	}

	/// <summary>
	/// A crop over its own canvas: rotated a little so the handles, the dimmed surround and the
	/// rotate handle all show. Its own method to keep <see cref="Build"/> under the coupling limit.
	/// </summary>
	/// <param name="category">The group the tile belongs to.</param>
	/// <returns>The tile.</returns>
	private static GalleryEntry ReorderableTreeTile(GalleryCategory category)
	{
		// A still frame shows the rows and their indent; the drag affordances only exist while a row is
		// held, which a tile cannot show.
		string[] names = ["Background", "Photos", "Beach", "Forest", "Notes"];
		ReorderableTreeRow[] rows = [new(0, false), new(0, true), new(1, false), new(1, false), new(0, false)];
		return new("ReorderableTree", category, [nameof(ImGuiWidgets.ReorderableTree)], _ =>
			ImGuiWidgets.ReorderableTree("##reorderable", rows, i => ImGui.Selectable(names[i], i == 2)));
	}

	private static GalleryEntry CropOverlayTile(GalleryCategory category)
	{
		Vector2 imageSize = new(SampleImage.Size, SampleImage.Size);
		Vector2 canvasSize = new(260f, 170f);
		ImGuiWidgets.ImageCanvasState canvas = new();
		canvas.FitToViewport(imageSize, canvasSize);
		CropRect crop = new(imageSize / 2f, imageSize * new Vector2(0.55f, 0.45f), 8f);
		return new("CropOverlay", category, [nameof(ImGuiWidgets.CropOverlay)], context =>
		{
			ImGuiWidgets.ImageCanvas("##cropCanvas", context.SampleTextureId, imageSize, canvas, canvasSize);
			ImGuiWidgets.CropOverlay("##crop", ref crop, imageSize, canvas, ImGui.GetItemRectMin(), canvasSize);

			// The overlay restores the cursor, so something has to follow it before the window ends.
			ImGui.Dummy(Vector2.Zero);
		});
	}

	/// <summary>
	/// A transform box on a turned layer, its handles on the layer's turned corners. Its
	/// own method to keep <see cref="Build"/> under the coupling limit.
	/// </summary>
	/// <param name="category">The group the tile belongs to.</param>
	/// <returns>The tile.</returns>
	private static GalleryEntry TransformBoxTile(GalleryCategory category)
	{
		Vector2 imageSize = new(SampleImage.Size, SampleImage.Size);
		Vector2 canvasSize = new(260f, 170f);
		ImGuiWidgets.ImageCanvasState canvas = new();
		canvas.FitToViewport(imageSize, canvasSize);
		TransformBoxRect rect = TransformBoxRect.Unit;
		return new("TransformBox", category, [nameof(ImGuiWidgets.TransformBox)], context =>
		{
			ImGui.SetNextItemAllowOverlap();
			ImGuiWidgets.ImageCanvas("##boxCanvas", context.SampleTextureId, imageSize, canvas, canvasSize);
			Vector2 canvasMin = ImGui.GetItemRectMin();
			(Vector2 imageMin, Vector2 imageMax) = canvas.ImageRectInViewport(imageSize, canvasSize);
			Matrix3x2 frameToScreen = Matrix3x2.CreateScale(new Vector2(0.5f, 0.35f) * (imageMax - imageMin))
				* Matrix3x2.CreateRotation(-0.2f)
				* Matrix3x2.CreateTranslation(canvasMin + imageMin + ((imageMax - imageMin) * new Vector2(0.22f, 0.4f)));
			ImGuiWidgets.TransformBox("##box", ref rect, frameToScreen, canvasMin, canvasMin + canvasSize);

			// The box restores the cursor, so something has to follow it before the window ends.
			ImGui.Dummy(Vector2.Zero);
		});
	}

	/// <summary>
	/// A loupe beside its own canvas, hovered on the sun's edge so the grid shows a gradient and a
	/// readout. Its own method to keep <see cref="Build"/> under the coupling limit.
	/// </summary>
	/// <param name="category">The group the tile belongs to.</param>
	/// <returns>The tile.</returns>
	private static GalleryEntry PixelLoupeTile(GalleryCategory category)
	{
		Vector2 imageSize = new(SampleImage.Size, SampleImage.Size);
		Vector2 canvasSize = new(170f, 170f);
		ImGuiWidgets.ImageCanvasState canvas = new();
		canvas.FitToViewport(imageSize, canvasSize);
		return new("PixelLoupe", category, [nameof(ImGuiWidgets.PixelLoupe)], context =>
		{
			ImGuiWidgets.ImageCanvas("##loupeCanvas", context.SampleTextureId, imageSize, canvas, canvasSize);
			Vector2 canvasMin = ImGui.GetItemRectMin();
			ImGui.SameLine();
			ImGuiWidgets.PixelLoupe("##loupe", canvas, imageSize, canvasMin, canvasSize, ReadSamplePixel, out _, out _, radius: 5, cellSize: 11f);
		})
		{
			Interact = context =>
			{
				// On the sun's rim, where the loupe has a gradient to show rather than a flat colour.
				Vector2 canvasMin = context.CenterOf("##loupeCanvas") - (canvasSize / 2f);
				Vector2 imagePoint = new(SampleImage.Size * 0.38f, SampleImage.Size * 0.44f);
				context.HoverAt(canvasMin + canvas.ImageToViewport(imagePoint, imageSize, canvasSize));
			},
		};
	}

	private static Color ReadSamplePixel(int x, int y)
	{
		(byte r, byte g, byte b) = SampleImage.PixelAt(x, y);
		return Color.FromBytes(r, g, b);
	}

	// Its own method because the toolbar's option types push Build past the analyzers' class-coupling limit.
	private static GalleryEntry ToolbarTile(GalleryCategory category)
	{
		bool preview = true;
		return new("Toolbar", category,
			[nameof(ImGuiWidgets.Toolbar), nameof(ImGuiWidgets.ToolbarSeparator), nameof(ImGuiWidgets.ToolbarButton), nameof(ImGuiWidgets.ToolbarToggleButton)], _ =>
		{
			// Material Icons code points, which the gallery merges into its font when it has them. Merged
			// fonts keep the first glyph for a code point, so these avoid the Nerd Font's ranges: U+E238,
			// Material's format_bold, falls in its Font Awesome Extension block and U+E8B8, settings, in its
			// Devicons block, and each draws as that instead.
			using (ImGuiWidgets.Toolbar("##toolbar", new ToolbarOptions { Width = 340f }))
			{
				ImGuiWidgets.ToolbarButton("Open", "\uE2C7");
				ImGuiWidgets.ToolbarButton("Save", "\uE161");
				ImGuiWidgets.ToolbarSeparator();
				ImGuiWidgets.ToolbarToggleButton("Preview", "\uE8F4", ref preview, new ToolbarButtonOptions { Layout = ToolbarButtonLayout.GlyphOnly });
				ImGuiWidgets.ToolbarButton("Redo", "\uE15A", new ToolbarButtonOptions { Layout = ToolbarButtonLayout.GlyphOnly, Enabled = false });
			}

			ImGui.Spacing();
			using (ImGuiWidgets.Toolbar("##toolbarAbove", new ToolbarOptions { Width = 340f, Layout = ToolbarButtonLayout.GlyphAbove, GlyphSize = 24f }))
			{
				ImGuiWidgets.ToolbarButton("Play", "\uE037", new ToolbarButtonOptions { MinWidth = 56f });
				ImGuiWidgets.ToolbarButton("Stop", "\uE047", new ToolbarButtonOptions { MinWidth = 56f });
				ImGuiWidgets.ToolbarSeparator();
				ImGuiWidgets.ToolbarButton("Tune", "\uE429", new ToolbarButtonOptions { MinWidth = 56f });
			}
		});
	}
}
