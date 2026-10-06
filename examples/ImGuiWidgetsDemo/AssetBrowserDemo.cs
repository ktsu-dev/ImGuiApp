// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Examples.Widgets;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Probes;
using ktsu.ImGui.Widgets;

/// <summary>
/// Demonstrates <see cref="ImGuiWidgets.AssetBrowser(string, int, Func{int, string}, Func{int, nint}, ImGuiWidgets.AssetBrowserState, ImGuiWidgets.AssetBrowserOptions?)"/>
/// over a catalogue large enough that only the visible rows are ever drawn.
/// </summary>
/// <remarks>
/// One class per section, registered in <see cref="DemoSections"/>; see "Adding Components" in
/// CLAUDE.md for the rule.
/// </remarks>
internal static class AssetBrowserDemo
{
	private const int AssetCount = 5_000;
	private const string PayloadType = "DEMO_ASSETS";
	private const float StartingTileSize = 96f;

	private static readonly string[] Kinds = ["mesh", "texture", "sound", "script"];
	private static readonly string[] Extensions = [".fbx", ".png", ".wav", ".cs"];

	private static readonly List<int> Visible = [];
	private static readonly List<string> Dropped = [];

	private static readonly ImGuiWidgets.AssetBrowserOptions Options = new()
	{
		Size = new Vector2(0f, 300f),
		TileSize = StartingTileSize,
		DragDropPayloadType = PayloadType,
		Tooltip = index => $"{NameOf(Visible[index])}\n{Kinds[Visible[index] % Kinds.Length]}",
		OnActivate = index => lastActivated = NameOf(Visible[index]),
	};

	private static string filter = string.Empty;
	private static string appliedFilter = "\0";
	private static string lastActivated = "nothing";

	/// <summary>Gets the browser's selection, so a test can read what a click did.</summary>
	internal static ImGuiWidgets.AssetBrowserState State { get; private set; } = new();

	/// <summary>
	/// Returns this section's state to its starting values. The demo keeps its state in statics, which
	/// outlive a harness.
	/// </summary>
	internal static void ResetState()
	{
		State = new();
		filter = string.Empty;
		appliedFilter = "\0";
		lastActivated = "nothing";
		Dropped.Clear();
		Options.TileSize = StartingTileSize;
	}

	/// <summary>Draws the demo section.</summary>
	internal static void Show()
	{
		if (!DemoProbe.Header("Asset Browser"))
		{
			return;
		}

		ImGui.TextUnformatted("A virtualized thumbnail grid with multi-select and drag-out:");
		ImGui.BulletText("Click, Ctrl+click and Shift+click to select; arrows, Home and End move the focus");
		ImGui.BulletText("Double-click or Enter to activate; Ctrl+wheel resizes the tiles");
		ImGui.BulletText("Drag the selection onto the box below");

		ImGui.SetNextItemWidth(220f);
		_ = ImGui.InputTextWithHint("##asset_filter", "Filter by name", ref filter, 64);
		ImGuiProbes.MarkItem("Asset filter");
		ImGui.SameLine();
		float tileSize = Options.TileSize;
		ImGui.SetNextItemWidth(160f);
		if (DemoProbe.SliderFloat("Tile size", ref tileSize, Options.MinTileSize, Options.MaxTileSize, "%.0f"))
		{
			Options.TileSize = tileSize;
		}

		ApplyFilter();

		_ = ImGuiWidgets.AssetBrowser(
			"asset_browser_demo",
			Visible.Count,
			index => NameOf(Visible[index]),
			index => ThumbnailOf(Visible[index]),
			State,
			Options);

		ImGui.TextUnformatted(string.Create(
			CultureInfo.InvariantCulture,
			$"{State.SelectedIndices.Count} selected · {Visible.Count:N0} shown · last activated: {lastActivated}"));

		DrawDropTarget();
	}

	private static void ApplyFilter()
	{
		if (filter == appliedFilter)
		{
			return;
		}

		// Selection indices name positions in the filtered view, so a new view starts unselected.
		appliedFilter = filter;
		Visible.Clear();
		for (int asset = 0; asset < AssetCount; asset++)
		{
			if (filter.Length == 0 || NameOf(asset).Contains(filter, StringComparison.OrdinalIgnoreCase))
			{
				Visible.Add(asset);
			}
		}

		_ = State.Clear();
	}

	private static void DrawDropTarget()
	{
		_ = ImGui.BeginChild("asset_drop_target", new Vector2(0f, 80f), ImGuiChildFlags.Borders);
		ImGui.TextDisabled(Dropped.Count == 0 ? "Drop here" : "Dropped:");
		foreach (string name in Dropped)
		{
			ImGui.BulletText(name);
		}

		ImGui.EndChild();
		ImGuiProbes.MarkItem("Asset drop target");

		if (ImGui.BeginDragDropTarget())
		{
			if (ImGuiWidgets.TryAcceptAssetPayload(PayloadType, out IReadOnlyList<int> indices))
			{
				Dropped.Clear();
				foreach (int index in indices)
				{
					Dropped.Add(NameOf(Visible[index]));
				}
			}

			ImGui.EndDragDropTarget();
		}
	}

	private static string NameOf(int asset) =>
		string.Create(CultureInfo.InvariantCulture, $"{Kinds[asset % Kinds.Length]}_{asset:D4}{Extensions[asset % Extensions.Length]}");

	/// <summary>
	/// Textures get the ktsu logo as their thumbnail; everything else has none, which the browser
	/// draws as an empty frame. Both paths show.
	/// </summary>
	private static nint ThumbnailOf(int asset) =>
		asset % Kinds.Length == 1 ? DemoContext.KtsuTexture.TextureId : 0;
}
