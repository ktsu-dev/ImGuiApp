// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Tests;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AppResources = ktsu.ImGui.App.Resources.Resources;

/// <summary>
/// Guards every <c>.resx</c> in the repository against manifest-name drift.
/// </summary>
/// <remarks>
/// A <c>.resx</c>'s manifest name is derived from <c>RootNamespace</c>, which <c>ktsu.Sdk</c>
/// computes from <c>AuthorsNamespace</c> and <c>ProjectNamespace</c>, while the generated
/// <c>Resources.Designer.cs</c> carries the name it was generated with baked in as a string
/// literal. Those two can drift apart silently, and nothing notices until the first lookup throws
/// <see cref="MissingManifestResourceException"/> at run time — which is exactly what happened to
/// <c>examples/ImGuiAppDemo</c>. Pinning <c>LogicalName</c> on the <c>EmbeddedResource</c> item
/// takes the derivation out of the picture; these tests make an unpinned or mismatched entry fail
/// here instead of in a consuming application.
/// </remarks>
[TestClass]
public class EmbeddedResourceManifestNameTests
{
	private const string ResourceManagerConstructorPrefix = "new global::System.Resources.ResourceManager(\"";

	[TestMethod]
	public void EveryResxInTheRepository_PinsItsLogicalName()
	{
		List<string> unpinned = [];

		foreach (string resxPath in EnumerateResxFiles())
		{
			string projectPath = FindOwningProject(resxPath);
			XElement? item = FindEmbeddedResourceItem(projectPath, resxPath);

			if (item is null)
			{
				unpinned.Add($"{Describe(resxPath)} has no explicit EmbeddedResource item in {Path.GetFileName(projectPath)}");
				continue;
			}

			if (string.IsNullOrWhiteSpace(ReadLogicalName(item)))
			{
				unpinned.Add($"{Describe(resxPath)} has no LogicalName in {Path.GetFileName(projectPath)}");
			}
		}

		Assert.IsEmpty(
			unpinned,
			$"Every .resx needs a <LogicalName> so its manifest name cannot drift with RootNamespace:{Environment.NewLine}{string.Join(Environment.NewLine, unpinned)}");
	}

	[TestMethod]
	public void EveryPinnedLogicalName_MatchesTheNameItsDesignerLooksUp()
	{
		List<string> mismatches = [];

		foreach (string resxPath in EnumerateResxFiles())
		{
			string projectPath = FindOwningProject(resxPath);
			XElement? item = FindEmbeddedResourceItem(projectPath, resxPath);
			string? logicalName = item is null ? null : ReadLogicalName(item);
			string? designerName = ReadDesignerResourceName(resxPath);

			if (logicalName is null || designerName is null)
			{
				continue;
			}

			string expected = $"{designerName}.resources";
			if (!string.Equals(logicalName, expected, StringComparison.Ordinal))
			{
				mismatches.Add($"{Describe(resxPath)} pins '{logicalName}' but its designer looks up '{expected}'");
			}
		}

		Assert.IsEmpty(
			mismatches,
			$"A LogicalName that does not match the designer's baked-in name throws MissingManifestResourceException on first lookup:{Environment.NewLine}{string.Join(Environment.NewLine, mismatches)}");
	}

	[TestMethod]
	public void ImGuiAppResourceManager_ResolvesAgainstTheShippedAssemblyManifest()
	{
		ResourceManager manager = AppResources.ResourceManager;
		string expected = $"{manager.BaseName}.resources";
		string[] manifestNames = typeof(ImGuiApp).Assembly.GetManifestResourceNames();

		Assert.IsTrue(
			manifestNames.Contains(expected, StringComparer.Ordinal),
			$"ktsu.ImGui.App must embed '{expected}' for font and emoji loading to work; it embeds: {string.Join(", ", manifestNames)}");
	}

	[TestMethod]
	public void ImGuiAppResources_LoadTheFontsTheyDeclare()
	{
		Assert.IsNotEmpty(AppResources.NerdFont, "The Nerd Font resource should decode to a non-empty font.");
		Assert.IsNotEmpty(AppResources.NotoEmoji, "The Noto Emoji resource should decode to a non-empty font.");
	}

	private static string SourceFilePath([CallerFilePath] string path = "") => path;

	private static string RepositoryRoot()
	{
		DirectoryInfo? directory = new FileInfo(SourceFilePath()).Directory;

		while (directory is not null && !File.Exists(Path.Join(directory.FullName, "ImGui.sln")))
		{
			directory = directory.Parent;
		}

		Assert.IsNotNull(directory, "Could not locate the repository root (the directory holding ImGui.sln) from the test source path.");
		return directory.FullName;
	}

	private static List<string> EnumerateResxFiles()
	{
		string root = RepositoryRoot();

		List<string> resxFiles = [.. Directory
			.EnumerateFiles(root, "*.resx", SearchOption.AllDirectories)
			.Where(static path => !IsBuildOutput(path))
			.OrderBy(static path => path, StringComparer.Ordinal)];

		Assert.IsNotEmpty(resxFiles, "Expected at least one .resx in the repository; the search is probably looking in the wrong place.");
		return resxFiles;
	}

	private static bool IsBuildOutput(string path)
	{
		string normalized = path.Replace('\\', '/');
		return normalized.Contains("/bin/", StringComparison.Ordinal)
			|| normalized.Contains("/obj/", StringComparison.Ordinal);
	}

	private static string FindOwningProject(string resxPath)
	{
		DirectoryInfo? directory = new FileInfo(resxPath).Directory;

		while (directory is not null)
		{
			string[] projects = Directory.GetFiles(directory.FullName, "*.csproj");
			if (projects.Length > 0)
			{
				return projects[0];
			}

			directory = directory.Parent;
		}

		Assert.Fail($"{Describe(resxPath)} does not sit under any .csproj.");
		return string.Empty;
	}

	private static XElement? FindEmbeddedResourceItem(string projectPath, string resxPath)
	{
		XDocument document = XDocument.Load(projectPath);
		string projectDirectory = Path.GetDirectoryName(projectPath)!;
		string target = Path.GetFullPath(resxPath);

		foreach (XElement element in document.Descendants().Where(static element => element.Name.LocalName == "EmbeddedResource"))
		{
			string? spec = (string?)element.Attribute("Update") ?? (string?)element.Attribute("Include");
			if (string.IsNullOrWhiteSpace(spec))
			{
				continue;
			}

			// MSBuild reads an item spec relative to the project unless it is already rooted, so say
			// which of the two this is rather than leaning on Path.Combine silently dropping the
			// project directory for a rooted spec.
			string normalizedSpec = spec.Replace('\\', Path.DirectorySeparatorChar);
			string candidate = Path.IsPathRooted(normalizedSpec)
				? Path.GetFullPath(normalizedSpec)
				: Path.GetFullPath(Path.Join(projectDirectory, normalizedSpec));

			if (string.Equals(candidate, target, StringComparison.Ordinal))
			{
				return element;
			}
		}

		return null;
	}

	private static string? ReadLogicalName(XElement item)
	{
		XElement? child = item.Elements().FirstOrDefault(static element => element.Name.LocalName == "LogicalName");
		return child?.Value.Trim() ?? (string?)item.Attribute("LogicalName");
	}

	private static string? ReadDesignerResourceName(string resxPath)
	{
		string designerPath = Path.ChangeExtension(resxPath, ".Designer.cs");
		if (!File.Exists(designerPath))
		{
			return null;
		}

		string designer = File.ReadAllText(designerPath);
		int start = designer.IndexOf(ResourceManagerConstructorPrefix, StringComparison.Ordinal);
		if (start < 0)
		{
			return null;
		}

		start += ResourceManagerConstructorPrefix.Length;
		int end = designer.IndexOf('"', start);
		return end < 0 ? null : designer[start..end];
	}

	private static string Describe(string path) => Path.GetRelativePath(RepositoryRoot(), path).Replace('\\', '/');
}
