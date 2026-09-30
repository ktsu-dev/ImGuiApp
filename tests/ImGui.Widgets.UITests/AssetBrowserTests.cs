// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.App.Testing;
using ktsu.ImGui.Probes;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>Drives <c>ImGuiWidgets.AssetBrowser</c> on its own.</summary>
[TestClass]
public sealed class AssetBrowserTests : WidgetTest
{
	private const string Label = "assets";
	private const string DropTarget = "drop";
	private const float TileSize = 64f;

	private readonly ImGuiWidgets.AssetBrowserState state = new();
	private readonly List<int> activated = [];
	private readonly List<int> dropped = [];

	private ImGuiWidgets.AssetBrowserOptions options = null!;
	private int count = 24;
	private int labelCalls;
	private nint texture;
	private bool changed;

	[TestInitialize]
	public void SetUpOptions() =>
		options = new()
		{
			Size = new Vector2(400f, 300f),
			TileSize = TileSize,
			OnActivate = activated.Add,
		};

	private void Draw()
	{
		changed |= ImGuiWidgets.AssetBrowser(
			Label,
			count,
			index =>
			{
				labelCalls++;
				return string.Create(CultureInfo.InvariantCulture, $"Item {index}");
			},
			_ => texture,
			state,
			options);

		_ = ImGui.Button(DropTarget, new Vector2(200f, 40f));
		ImGuiProbes.MarkItem(DropTarget);
		if (ImGui.BeginDragDropTarget())
		{
			if (ImGuiWidgets.TryAcceptAssetPayload(options.DragDropPayloadType, out IReadOnlyList<int> indices))
			{
				dropped.AddRange(indices);
			}

			ImGui.EndDragDropTarget();
		}
	}

	private void StartBrowser()
	{
		Start(Draw);
		texture = CreateTestTexture(16).TextureId;
		Step(2);
	}

	private static string Tile(int index) =>
		string.Create(CultureInfo.InvariantCulture, $"{Label}/[{index}]");

	private void AssertSelected(params int[] expected) =>
		Assert.AreSequenceEqual(expected, state.SelectedIndices);

	private void ClickWithModifier(ImGuiKey modifier, int index)
	{
		HarnessKeyboard.KeyDown(modifier);
		Step();
		Click(Tile(index));
		HarnessKeyboard.KeyUp(modifier);
		Step();
	}

	/// <summary>Counts the tiles sharing the first tile's row, which is the column count the widget chose.</summary>
	private int VisibleColumns()
	{
		float top = RectOf(Tile(0)).MinY;
		int columns = 0;
		while (columns < count && IsVisible(Tile(columns)) && MathF.Abs(RectOf(Tile(columns)).MinY - top) < 0.5f)
		{
			columns++;
		}

		return columns;
	}

	private void DragTileToDropTarget(int index)
	{
		Vector2 from = CenterOf(Tile(index));
		Vector2 to = CenterOf(DropTarget);
		Harness.Mouse.Drag(from.X, from.Y, to.X, to.Y);
		Step();
	}

	[TestMethod]
	public void AssetBrowser_DrawsTilesAndMarksThem()
	{
		StartBrowser();

		Assert.IsTrue(IsVisible(Label), "The browser as a whole is marked under its label.");
		Assert.IsTrue(IsVisible(Tile(0)));
		Assert.AreEqual(TileSize, RectOf(Tile(0)).Width, 0.5f);
		Assert.IsGreaterThan(1, VisibleColumns(), "Tiles flow across the row before wrapping.");
		Assert.IsFalse(changed, "Drawing alone selects nothing.");
	}

	[TestMethod]
	public void AssetBrowser_ClickSelects()
	{
		StartBrowser();

		Click(Tile(3));

		AssertSelected(3);
		Assert.AreEqual(3, state.FocusIndex);
		Assert.IsTrue(changed);
	}

	[TestMethod]
	public void AssetBrowser_CtrlClickAddsAndShiftClickExtends()
	{
		StartBrowser();

		Click(Tile(1));
		ClickWithModifier(ImGuiKey.ModCtrl, 4);
		AssertSelected(1, 4);

		ClickWithModifier(ImGuiKey.ModShift, 6);
		AssertSelected(4, 5, 6);
	}

	/// <summary>
	/// A plain press on a tile that is already part of a multi-selection has to wait for the release,
	/// because it may be the start of a drag. Released without moving, it is an ordinary click.
	/// </summary>
	[TestMethod]
	public void AssetBrowser_PlainClickOnASelectedTile_CollapsesOnRelease()
	{
		StartBrowser();
		Click(Tile(1));
		ClickWithModifier(ImGuiKey.ModCtrl, 2);

		Click(Tile(2));

		AssertSelected(2);
	}

	[TestMethod]
	public void AssetBrowser_ClickOnEmptySpaceClears()
	{
		count = 3;
		StartBrowser();
		Click(Tile(1));

		ClickWithin(Label, 200f, 250f);

		Assert.IsEmpty(state.SelectedIndices);
	}

	[TestMethod]
	public void AssetBrowser_ArrowKeysMoveByColumns()
	{
		StartBrowser();
		int columns = VisibleColumns();
		Click(Tile(0));

		Harness.Keyboard.Press(ImGuiKey.RightArrow);
		AssertSelected(1);

		Harness.Keyboard.Press(ImGuiKey.DownArrow);
		AssertSelected(1 + columns);

		Harness.Keyboard.Press(ImGuiKey.LeftArrow, shift: true);
		AssertSelected(columns, 1 + columns);
	}

	[TestMethod]
	public void AssetBrowser_CtrlASelectsAllAndEscapeClears()
	{
		StartBrowser();
		Click(Tile(0));

		Harness.Keyboard.Press(ImGuiKey.A, ctrl: true);
		Assert.HasCount(count, state.SelectedIndices);

		Harness.Keyboard.Press(ImGuiKey.Escape);
		Assert.IsEmpty(state.SelectedIndices);
	}

	[TestMethod]
	public void AssetBrowser_DoubleClickActivates()
	{
		StartBrowser();
		Vector2 center = CenterOf(Tile(5));

		Harness.Mouse.Click(center.X, center.Y);
		Harness.Mouse.Click(center.X, center.Y);
		Step();

		Assert.AreSequenceEqual([5], activated);
	}

	[TestMethod]
	public void AssetBrowser_EnterActivatesTheFocus()
	{
		StartBrowser();
		Click(Tile(2));

		Harness.Keyboard.Press(ImGuiKey.RightArrow);
		Harness.Keyboard.Press(ImGuiKey.Enter);

		Assert.AreSequenceEqual([3], activated);
	}

	[TestMethod]
	public void AssetBrowser_CtrlWheelResizesTiles()
	{
		StartBrowser();
		Vector2 center = CenterOf(Tile(0));

		HarnessKeyboard.KeyDown(ImGuiKey.ModCtrl);
		Step();
		Harness.Mouse.Wheel(center.X, center.Y, 2);
		HarnessKeyboard.KeyUp(ImGuiKey.ModCtrl);
		Step(2);

		Assert.AreEqual(TileSize * 1.21f, options.TileSize, 0.01f);
		Assert.AreEqual(TileSize * 1.21f, RectOf(Tile(0)).Width, 0.5f);
	}

	[TestMethod]
	public void AssetBrowser_WheelWithoutCtrlDoesNotResize()
	{
		count = 500;
		StartBrowser();
		Vector2 center = CenterOf(Tile(0));

		Harness.Mouse.Wheel(center.X, center.Y, -2);
		Step();

		Assert.AreEqual(TileSize, options.TileSize);
	}

	/// <summary>
	/// Fifty thousand assets have to cost a screenful of labels, or the widget is no use for the
	/// catalogues it exists for.
	/// </summary>
	[TestMethod]
	public void AssetBrowser_ScrollsOnlyTheVisibleRows()
	{
		count = 50000;
		StartBrowser();
		int columns = VisibleColumns();

		labelCalls = 0;
		Step();

		// A 300-pixel view holds at most five rows of 64-pixel tiles with their labels; allow two more
		// for the partial rows the clipper draws at either edge.
		Assert.IsLessThanOrEqualTo(columns * 7, labelCalls);
		Assert.IsGreaterThan(0, labelCalls);
		Assert.IsFalse(IsVisible(Tile(40000)));
	}

	[TestMethod]
	public void AssetBrowser_DragDeliversTheSelection()
	{
		StartBrowser();
		Click(Tile(1));
		ClickWithModifier(ImGuiKey.ModCtrl, 2);

		DragTileToDropTarget(2);

		Assert.AreSequenceEqual([1, 2], dropped);
		AssertSelected(1, 2);
	}

	[TestMethod]
	public void AssetBrowser_DraggingAnUnselectedTileSelectsIt()
	{
		StartBrowser();
		Click(Tile(1));

		DragTileToDropTarget(3);

		Assert.AreSequenceEqual([3], dropped);
		AssertSelected(3);
	}

	[TestMethod]
	public void AssetBrowser_ZeroTextureDrawsAFrame()
	{
		Start(Draw);
		Step(2);

		Assert.IsTrue(IsVisible(Tile(0)));
		AssertSomethingWasDrawn("a tile with no thumbnail still draws its frame and label");
	}

	[TestMethod]
	public void AssetBrowser_ShrinkingTheCountDropsSelection()
	{
		StartBrowser();
		Click(Tile(5));

		count = 3;
		Step();

		Assert.IsEmpty(state.SelectedIndices);
		Assert.AreEqual(2, state.FocusIndex);
		Assert.IsFalse(IsVisible(Tile(5)));
	}
}
