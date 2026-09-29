// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.WidgetGallery;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Resources;

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
	/// Gets the path to <c>MaterialIcons-Regular.ttf</c>, which the date picker, the file tree and the
	/// file dialogs draw their glyphs from. The font is not in the repository; without it those
	/// tiles show placeholder boxes where their icons belong.
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
	[SuppressMessage("Major Code Smell", "S6640:Make sure that using \"unsafe\" is safe here", Justification = "FontHelper's glyph range helpers return uint* that ImGui owns for the lifetime of the atlas; they are passed straight through and never dereferenced here.")]
	public void LoadFonts()
	{
		ImGuiIOPtr io = ImGui.GetIO();

		if (ApplicationFont.Value is not byte[] nerdFont)
		{
			return;
		}

		unsafe
		{
			ImFontPtr? font = FontHelper.AddCustomFont(io, nerdFont, ApplicationFontPixels, FontHelper.GetExtendedUnicodeRanges(io.Fonts));

			if (font is not ImFontPtr loaded)
			{
				return;
			}

			io.FontDefault = loaded;

			if (MaterialIconsPath is not null && File.Exists(MaterialIconsPath))
			{
				_ = FontHelper.AddCustomFont(io, File.ReadAllBytes(MaterialIconsPath), ApplicationFontPixels, FontHelper.GetMaterialIconRanges(), mergeWithPrevious: true);
			}
		}
	}

	/// <summary>The pixel size <c>ImGuiApp</c> loads its default font at, at a display scale of one.</summary>
	private const float ApplicationFontPixels = 14f;

	/// <summary>The Nerd Font embedded in <c>ktsu.ImGui.App</c>, or null if it cannot be found.</summary>
	private static readonly Lazy<byte[]?> ApplicationFont = new(() =>
	{
		try
		{
			ResourceManager resources = new("ktsu.ImGui.App.Resources.Resources", typeof(ImGuiApp).Assembly);
			return resources.GetObject("NerdFont", CultureInfo.InvariantCulture) as byte[];
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
