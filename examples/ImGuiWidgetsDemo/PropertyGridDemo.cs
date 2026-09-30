// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Examples.Widgets;

using System.IO;
using System.Numerics;
using Hexa.NET.ImGui;
using ktsu.ImGui.App;
using ktsu.ImGui.Styler;
using ktsu.ImGui.Widgets;
using ktsu.Semantics.Color;
using ktsu.Semantics.Paths;

/// <summary>
/// The property grid, with a row of every kind it supports.
/// </summary>
internal static class PropertyGridDemo
{
	/// <summary>Gets the tags held by the list row.</summary>
	internal static IReadOnlyList<string> PropertyTags => propertyTags;

	/// <summary>Gets how many rows have been edited.</summary>
	internal static int PropertyEditCount { get; private set; }

	private static bool propertyVisible = true;
	private static int propertyQuantity = 4;
	private static long propertySerial = 100_200_300L;
	private static float propertyWeight = 2.5f;
	private static double propertyTolerance = 0.0125d;
	private static string propertyItemName = "Widget";
	private static EnumValues propertyMode = EnumValues.Value1;
	private static Vector2 propertyOffset = new(10.0f, 20.0f);
	private static Vector3 propertyScale = Vector3.One;
	private static ImGuiWidgets.DoubleVector2 propertyOrigin = new(0.0d, 0.0d);
	private static ImGuiWidgets.DoubleVector3 propertyExtent = new(1.0d, 1.0d, 1.0d);
	private static Color propertyTint = Color.FromSrgb(0.2f, 0.6f, 1.0f, 1.0f);
	private static string propertyConfigFile = string.Empty;
	private static string propertyOutputFolder = string.Empty;
	private static string propertyIconFile = string.Empty;
	private static bool propertyGridReadOnly;
	private static float propertyLabelWeight = 0.4f;
	private static readonly List<string> propertyTags = ["alpha", "beta"];
	private static readonly List<int> propertyCounts = [1, 2, 3];
	private static readonly List<Color> propertySwatches =
	[
		Color.FromSrgb(0.9f, 0.3f, 0.3f, 1.0f),
		Color.FromSrgb(0.3f, 0.8f, 0.4f, 1.0f),
	];

	/// <summary>
	/// Returns this section's state to its starting values. The demo keeps its state in statics, which
	/// outlive a harness.
	/// </summary>
	internal static void ResetState()
	{
		propertyVisible = true;
		propertyQuantity = 4;
		propertySerial = 100_200_300L;
		propertyWeight = 2.5f;
		propertyTolerance = 0.0125d;
		propertyItemName = "Widget";
		propertyMode = EnumValues.Value1;
		propertyOffset = new Vector2(10.0f, 20.0f);
		propertyScale = Vector3.One;
		propertyOrigin = new ImGuiWidgets.DoubleVector2(0.0d, 0.0d);
		propertyExtent = new ImGuiWidgets.DoubleVector3(1.0d, 1.0d, 1.0d);
		propertyTint = Color.FromSrgb(0.2f, 0.6f, 1.0f, 1.0f);
		propertyConfigFile = string.Empty;
		propertyOutputFolder = string.Empty;
		propertyIconFile = string.Empty;
		propertyGridReadOnly = false;
		propertyLabelWeight = 0.4f;
		PropertyEditCount = 0;
		propertyTags.Clear();
		propertyTags.AddRange(["alpha", "beta"]);
		propertyCounts.Clear();
		propertyCounts.AddRange([1, 2, 3]);
		propertySwatches.Clear();
		propertySwatches.AddRange([Color.FromSrgb(0.9f, 0.3f, 0.3f, 1.0f), Color.FromSrgb(0.3f, 0.8f, 0.4f, 1.0f)]);
	}

	/// <summary>Draws the demo section.</summary>
	internal static void Show()
	{
		if (!DemoProbe.Header("Property Grid"))
		{
			return;
		}

		using (Indent.ByDefault())
		{
			ImGui.TextUnformatted("A two-column grid of labelled editors, one row per property.");

			DemoProbe.Checkbox("Read only##propertyGridReadOnly", ref propertyGridReadOnly);
			DemoProbe.SliderFloat("Label column##propertyGridLabelWeight", ref propertyLabelWeight, 0.15f, 0.75f, "%.2f");

			ImGuiWidgets.PropertyGridOptions options = new()
			{
				ReadOnly = propertyGridReadOnly,
				LabelColumnWeight = propertyLabelWeight,
				OnBrowse = BrowseForProperty,
				ThumbnailResolver = ResolvePropertyThumbnail,
			};

			using (ImGuiWidgets.PropertyGrid grid = new("DemoProperties", options))
			{
				using (grid.Section("Basics"))
				{
					grid.Value("Visible", ref propertyVisible);
					grid.Value("Quantity", ref propertyQuantity, 0, 100);
					grid.Value("Serial", ref propertySerial);
					grid.Value("Weight", ref propertyWeight);
					grid.Value("Tolerance", ref propertyTolerance);
					grid.Value("Item Name", ref propertyItemName);
					grid.Enum("Mode", ref propertyMode);
				}

				using (grid.Section("Geometry"))
				{
					grid.Value("Offset", ref propertyOffset);
					grid.Value("Scale", ref propertyScale);
					grid.Value("Origin", ref propertyOrigin);
					grid.Value("Extent", ref propertyExtent);
				}

				using (grid.Section("Appearance"))
				{
					grid.Value("Tint", ref propertyTint);
				}

				using (grid.Section("Paths"))
				{
					grid.FilePath("Config File", ref propertyConfigFile);
					grid.DirectoryPath("Output Folder", ref propertyOutputFolder);
					grid.ImagePath("Icon", ref propertyIconFile);
				}

				using (grid.Section("Lists"))
				{
					grid.List("Tags", propertyTags);
					grid.List("Counts", propertyCounts);
					grid.List("Swatches", propertySwatches);
				}

				if (grid.Changed)
				{
					PropertyEditCount++;
				}
			}

			ImGui.TextUnformatted($"Rows edited: {PropertyEditCount}");

			if (DemoProbe.Button("Use the demo icon##propertyGridIcon"))
			{
				propertyIconFile = DemoContext.KtsuIconPath.ToString();
			}
		}
	}

	/// <summary>
	/// Answers a property grid path row's browse button with the matching Hexa dialog. The dialogs
	/// are asynchronous, so the request is completed from their close callback rather than here.
	/// </summary>
	/// <param name="request">What the row asked for.</param>
	private static void BrowseForProperty(ImGuiWidgets.PropertyPathRequest request)
	{
		if (request.Kind == ImGuiWidgets.PropertyPathKind.Directory)
		{
			ImGuiWidgets.OpenFolderDialog folderDialog = new();
			folderDialog.Show(outcome => request.Complete(outcome.Path?.ToString()));
			return;
		}

		ImGuiWidgets.OpenFileDialog fileDialog = new();
		fileDialog.Show(outcome => request.Complete(outcome.Path?.ToString()));
	}

	/// <summary>
	/// Turns an image row's path into a texture for its thumbnail.
	/// </summary>
	/// <remarks>
	/// A property grid shows whatever is typed into the row, including half-finished and misspelled
	/// paths, so the file is checked before the texture cache is asked to load it.
	/// </remarks>
	/// <param name="path">The path the row holds.</param>
	/// <returns>The texture id, or zero when there is nothing to show.</returns>
	private static nint ResolvePropertyThumbnail(string path)
	{
		// TryCreate promises nothing about the out value to the null analysis, hence the third test.
		if (!File.Exists(path) || !AbsoluteFilePath.TryCreate(path, out AbsoluteFilePath? file) || file is null)
		{
			return 0;
		}

		return ImGuiApp.GetOrLoadTexture(file).TextureId;
	}
}
