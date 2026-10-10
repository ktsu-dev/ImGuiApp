// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.WidgetGallery;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Resources;
using System.Runtime.CompilerServices;

using Hexa.NET.ImGui;

using ktsu.ImGui.App;

/// <summary>How a gallery run renders: its fonts and output.</summary>
internal sealed record GallerySettings
{
	/// <summary>Gets the directory the tiles and composites are written to.</summary>
	public required string OutputDirectory { get; init; }

	/// <summary>Gets the width of the composite images, in pixels.</summary>
	public int CompositeWidth { get; init; } = 1200;

	/// <summary>
	/// Gets the path to the Material icon font the date picker, the file tree and the file dialogs
	/// draw their glyphs from. Use <c>MaterialSymbolsOutlined[...].ttf</c>: Hexa's constants are
	/// Material Symbols code points, and the older <c>MaterialIcons-Regular.ttf</c> lacks some of
	/// them, the file icon among them. The font is not in the repository; without it those tiles
	/// show placeholder boxes where their icons belong.
	/// </summary>
	public string? MaterialIconsPath { get; init; }

	/// <summary>
	/// Loads the fonts a real <c>ImGuiApp</c> application draws with, so the gallery looks like the
	/// widgets do in an application rather than like the widget suite's pictures.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The headless harness never builds <c>ImGuiApp</c>'s font atlas: it renders in Dear ImGui's
	/// built-in bitmap font and does not call <see cref="ImGuiAppConfig.OnConfigureFonts"/>. That
	/// keeps the widget suites' measurements independent of the application font, but it is not what
	/// anyone sees on screen. This runs from <see cref="ImGuiAppConfig.OnStart"/>, which the harness
	/// calls before its first frame, and adds the same Nerd Font <c>ImGuiApp</c> ships at the same
	/// fourteen pixels, then merges Material Icons over it the way <c>examples/ImGuiAppDemo</c> does.
	/// </para>
	/// <para>
	/// The Nerd Font is read from <c>ImGuiApp</c>'s own resources by name, since it is not public
	/// surface. If that name ever changes the gallery falls back to the built-in font rather than
	/// failing.
	/// </para>
	/// </remarks>
	[SuppressMessage("Major Code Smell", "S6640:Make sure that using \"unsafe\" is safe here", Justification = "The pointers handed to the atlas address FontHelper's cached glyph ranges and arrays on the pinned object heap, all of which outlive the atlas; none is dereferenced here.")]
	public void LoadFonts()
	{
		ImGuiIOPtr io = ImGui.GetIO();

		if (ApplicationFont.Value is not byte[] nerdFont)
		{
			return;
		}

		unsafe
		{
			bool withMaterialIcons = MaterialIconsPath is not null && File.Exists(MaterialIconsPath);

			// Dear ImGui looks a glyph up in a merged font's sources in the order they were added, and
			// the first source that has it wins. The Nerd Font is added first, because it is the text
			// font, and it has glyphs of its own at many Private Use Area code points Material Icons
			// also uses: the file dialogs' generic file icon (Draft, U+E66D) came out as Font Awesome
			// Extension's "J", and the file tree's Computer icon (U+E31E) as a weather glyph. Every
			// Private Use Area glyph the gallery draws is a Material one, so when Material Icons is
			// loaded the Nerd Font is told to stay out of that area altogether.
			ImFontConfigPtr config = ImGui.ImFontConfig();
			config.FontDataOwnedByAtlas = false;
			config.PixelSnapH = true;

			if (withMaterialIcons)
			{
				config.GlyphExcludeRanges = (uint*)Unsafe.AsPointer(ref PrivateUseArea[0]);
			}

			ImFont* font = io.Fonts.AddFontFromMemoryTTF(Unsafe.AsPointer(ref nerdFont[0]), nerdFont.Length, ApplicationFontPixels, config, FontHelper.GetExtendedUnicodeRanges(io.Fonts));

			if (font is null)
			{
				return;
			}

			io.FontDefault = font;

			if (withMaterialIcons)
			{
				_ = FontHelper.AddCustomFont(io, File.ReadAllBytes(MaterialIconsPath!), ApplicationFontPixels, FontHelper.GetMaterialIconRanges(), mergeWithPrevious: true);
			}
		}
	}

	/// <summary>
	/// The Basic Multilingual Plane Private Use Area as a zero-terminated glyph range, on the pinned
	/// object heap because the atlas keeps the pointer.
	/// </summary>
	private static readonly uint[] PrivateUseArea = CreatePrivateUseArea();

	private static uint[] CreatePrivateUseArea()
	{
		uint[] range = GC.AllocateUninitializedArray<uint>(3, pinned: true);
		range[0] = 0xE000;
		range[1] = 0xF8FF;
		range[2] = 0;
		return range;
	}

	/// <summary>The pixel size <c>ImGuiApp</c> loads its default font at, at a display scale of one.</summary>
	private const float ApplicationFontPixels = 14f;

	/// <summary>The Nerd Font embedded in <c>ktsu.ImGui.App</c>, or null if it cannot be found.</summary>
	/// <remarks>
	/// The bytes are copied into an array on the pinned object heap, because the atlas reads the font
	/// data again whenever it rasterizes a glyph it has not drawn before, for the life of the process.
	/// </remarks>
	private static readonly Lazy<byte[]?> ApplicationFont = new(() =>
	{
		try
		{
			ResourceManager resources = new("ktsu.ImGui.App.Resources.Resources", typeof(ImGuiApp).Assembly);

			if (resources.GetObject("NerdFont", CultureInfo.InvariantCulture) is not byte[] data || data.Length == 0)
			{
				return null;
			}

			byte[] pinned = GC.AllocateUninitializedArray<byte>(data.Length, pinned: true);
			data.CopyTo(pinned, 0);
			return pinned;
		}
		catch (MissingManifestResourceException)
		{
			return null;
		}
	});

	/// <summary>Gets the name each category is captioned with.</summary>
	/// <param name="category">The category.</param>
	/// <returns>Its caption.</returns>
	public static string CategoryTitle(GalleryCategory category) => category switch
	{
		GalleryCategory.InputAndControls => "Input and controls",
		GalleryCategory.DisplayAndStatus => "Display and status",
		GalleryCategory.ProgressAndLoading => "Progress and loading",
		GalleryCategory.DataAndSignals => "Data and signals",
		GalleryCategory.LayoutAndContainers => "Layout and containers",
		GalleryCategory.Editors => "Editors",
		GalleryCategory.DialogsAndWindows => "Dialogs and windows",
		_ => throw new ArgumentOutOfRangeException(nameof(category), category, null),
	};
}
