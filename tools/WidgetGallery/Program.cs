// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.WidgetGallery;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;

using ktsu.ImGui.WidgetGallery.Catalog;

/// <summary>
/// Renders every widget in <c>ktsu.ImGui.Widgets</c> headlessly, one tile per widget, and composites
/// the tiles into gallery images for the README.
/// </summary>
internal static class Program
{
	private const string Usage = """
		Usage: dotnet run -c Release --project tools/WidgetGallery -- [options]

		  --out <dir>             Where to write images. Default: docs/gallery in the repository.
		  --only <text>           Capture only tiles whose name contains the text.
		  --width <pixels>        Width of the composite images. Default: 1200.
		  --material-icons <ttf>  A Material icon font for the date picker, file tree and file dialogs.
		                          Material Symbols covers every glyph they draw; MaterialIcons-Regular.ttf
		                          lacks the dialogs' generic file icon.
		                          Default: MaterialIcons-Regular.ttf next to the tool, when present.
		  --check                 Only report widgets that have no tile; exit 1 if there are any.
		  --help                  Show this text.
		""";

	private static int Main(string[] args) => Run(args);

	/// <summary>Runs the tool.</summary>
	/// <param name="args">The command line.</param>
	/// <returns>The process exit code: 0 on success, 1 when a tile failed or a widget has none, 2 on a bad command line.</returns>
	internal static int Run(string[] args)
	{
		if (!TryParse(args, out Dictionary<string, string> options, out HashSet<string> flags))
		{
			Console.Error.WriteLine(Usage);
			return 2;
		}

		if (flags.Contains("--help") || flags.Contains("-h"))
		{
			Console.WriteLine(Usage);
			return 0;
		}

		IReadOnlyList<GalleryEntry> catalog = GalleryCatalog.Build();
		IReadOnlyList<string> uncovered = CatalogCoverage.Uncovered(GalleryCatalog.CoveredMembers(catalog));

		if (flags.Contains("--check"))
		{
			return ReportCoverage(uncovered) ? 0 : 1;
		}

		GallerySettings settings = BuildSettings(options);

		if (!File.Exists(settings.MaterialIconsPath))
		{
			Console.WriteLine("Material Icons font not found: the date picker, file tree and file dialogs will show placeholder boxes for their icons.");
		}

		List<GalleryEntry> selected = options.TryGetValue("--only", out string? only)
			? [.. catalog.Where(entry => entry.Name.Contains(only, StringComparison.OrdinalIgnoreCase))]
			: [.. catalog];

		try
		{
			return Render(selected, settings, writeComposites: only is null) ? 0 : 1;
		}
		finally
		{
			SampleFolder.Delete();
			ReportCoverage(uncovered);
		}
	}

	/// <summary>Splits the command line into options that take a value and flags that do not.</summary>
	/// <param name="args">The command line.</param>
	/// <param name="options">Receives each option and its value.</param>
	/// <param name="flags">Receives each flag.</param>
	/// <returns>False, having reported it, when an argument is not recognised.</returns>
	internal static bool TryParse(string[] args, out Dictionary<string, string> options, out HashSet<string> flags)
	{
		options = [];
		flags = [];
		int i = 0;

		while (i < args.Length)
		{
			string arg = args[i];

			if (arg is "--check" or "--help" or "-h")
			{
				flags.Add(arg);
				i++;
			}
			else if (arg.StartsWith("--", StringComparison.Ordinal) && i + 1 < args.Length)
			{
				options[arg] = args[i + 1];
				i += 2;
			}
			else
			{
				Console.Error.WriteLine($"Unrecognised argument '{arg}'.");
				return false;
			}
		}

		return true;
	}

	private static GallerySettings BuildSettings(Dictionary<string, string> options) => new()
	{
		OutputDirectory = options.TryGetValue("--out", out string? output) ? Path.GetFullPath(output) : DefaultOutputDirectory(),
		CompositeWidth = options.TryGetValue("--width", out string? width) ? int.Parse(width, CultureInfo.InvariantCulture) : 1200,
		MaterialIconsPath = options.TryGetValue("--material-icons", out string? icons)
			? Path.GetFullPath(icons)
			: Path.Join(AppContext.BaseDirectory, "MaterialIcons-Regular.ttf"),
	};

	private static bool Render(List<GalleryEntry> entries, GallerySettings settings, bool writeComposites)
	{
		string tileDirectory = Path.Join(settings.OutputDirectory, "widgets");
		Directory.CreateDirectory(tileDirectory);

		List<CapturedTile> tiles = [];
		List<string> failures = [];
		Stopwatch clock = Stopwatch.StartNew();

		foreach (GalleryEntry entry in entries)
		{
			Console.Write($"  {entry.Name,-28}");

			try
			{
				CapturedTile tile = Quietly(() => TileCapture.Capture(entry, settings));
				tile.Pixels.SavePng(Path.Join(tileDirectory, entry.Slug + ".png"));
				tiles.Add(tile);
				Console.WriteLine($"{tile.Pixels.Width}x{tile.Pixels.Height}");
			}
			catch (Exception exception) when (exception is InvalidOperationException or ktsu.ImGui.App.Testing.HarnessFrameException)
			{
				failures.Add(entry.Name);
				Console.WriteLine($"FAILED: {exception.Message}");
			}
		}

		Console.WriteLine($"Captured {tiles.Count} of {entries.Count} tiles in {clock.Elapsed.TotalSeconds:F1}s into {tileDirectory}");

		if (writeComposites && tiles.Count > 0)
		{
			tiles = [.. tiles.Where(tile => tile.Entry.InComposite)];
			WriteComposite(Path.Join(settings.OutputDirectory, "widgets.png"), "ktsu.ImGui.Widgets", tiles, settings);

			foreach (IGrouping<GalleryCategory, CapturedTile> group in tiles.GroupBy(tile => tile.Entry.Category))
			{
				string title = GallerySettings.CategoryTitle(group.Key);
				WriteComposite(
					Path.Join(settings.OutputDirectory, "widgets-" + GalleryEntry.MakeSlug(title) + ".png"),
					"ktsu.ImGui.Widgets",
					[.. group],
					settings);
			}
		}

		foreach (string failure in failures)
		{
			Console.Error.WriteLine($"Failed to capture '{failure}'.");
		}

		return failures.Count == 0;
	}

	private static void WriteComposite(string path, string title, IReadOnlyList<CapturedTile> tiles, GallerySettings settings)
	{
		Quietly(() => GalleryCompositor.Compose(title, tiles, settings)).SavePng(path);
		Console.WriteLine($"Wrote {path}");
	}

	/// <summary>
	/// Runs a harness with standard output silenced. <c>ImGuiApp</c> logs every extension context it
	/// creates and destroys to the console, which buries the tool's own progress under six lines a tile.
	/// </summary>
	private static T Quietly<T>(Func<T> action)
	{
		TextWriter output = Console.Out;
		Console.SetOut(TextWriter.Null);

		try
		{
			return action();
		}
		finally
		{
			Console.SetOut(output);
		}
	}

	private static bool ReportCoverage(IReadOnlyList<string> uncovered)
	{
		if (uncovered.Count == 0)
		{
			Console.WriteLine("Every widget in ImGuiWidgets has a gallery tile.");
			return true;
		}

		Console.WriteLine($"{uncovered.Count} widget(s) in ImGuiWidgets have no gallery tile: {string.Join(", ", uncovered)}.");
		Console.WriteLine("Add an entry to the matching file in tools/WidgetGallery/Catalog.");
		return false;
	}

	/// <summary>Finds <c>docs/gallery</c> in the repository the tool is being run from.</summary>
	private static string DefaultOutputDirectory()
	{
		for (DirectoryInfo? directory = new(Environment.CurrentDirectory); directory is not null; directory = directory.Parent)
		{
			if (File.Exists(Path.Join(directory.FullName, "ImGui.sln")))
			{
				return Path.Join(directory.FullName, "docs", "gallery");
			}
		}

		return Path.GetFullPath("gallery");
	}
}
