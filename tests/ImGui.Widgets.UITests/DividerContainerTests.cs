// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.App.Testing;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>Drives <see cref="ImGuiWidgets.DividerContainer"/> on its own.</summary>
[TestClass]
public sealed class DividerContainerTests : WidgetTest
{
	private const string ContainerId = "layout";
	private const string LeftZone = "left";
	private const string RightZone = "right";

	// The container marks each divider handle under "{container}/divider/{zone}", so the handle
	// between the two zones is addressed by the zone it follows.
	private const string Handle = "layout/divider/left";

	private ImGuiWidgets.DividerContainer container = null!;
	private readonly List<string> resized = [];

	private ImGuiWidgets.DividerContainer BuildContainer(ImGuiWidgets.DividerLayout layout = ImGuiWidgets.DividerLayout.Columns)
	{
		ImGuiWidgets.DividerContainer built = new(ContainerId, c => resized.Add(string.Join(",", c.GetSizes())), layout);

		built.Add(LeftZone, 0.3f, true, _ =>
		{
			ImGui.TextUnformatted("Left content");
			Mark("left-content");
		});

		built.Add(RightZone, 0.7f, false, _ =>
		{
			ImGui.TextUnformatted("Right content");
			Mark("right-content");
		});

		return built;
	}

	[TestMethod]
	public void DividerContainer_DrawsEveryZone()
	{
		container = BuildContainer();
		Start(() => container.Tick(1f / 60f));

		Assert.IsTrue(IsVisible("left-content"), "The left zone's content was not drawn.");
		Assert.IsTrue(IsVisible("right-content"), "The right zone's content was not drawn.");
	}

	[TestMethod]
	public void DividerContainer_Columns_PlacesZonesSideBySide()
	{
		container = BuildContainer(ImGuiWidgets.DividerLayout.Columns);
		Start(() => container.Tick(1f / 60f));

		Rectangle left = RectOf("left-content");
		Rectangle right = RectOf("right-content");

		Assert.IsTrue(right.MinX > left.MinX, "The right zone was not placed right of the left one.");
	}

	[TestMethod]
	public void DividerContainer_Rows_StacksZones()
	{
		container = BuildContainer(ImGuiWidgets.DividerLayout.Rows);
		Start(() => container.Tick(1f / 60f));

		Rectangle first = RectOf("left-content");
		Rectangle second = RectOf("right-content");

		Assert.IsTrue(second.MinY > first.MinY, "The second zone was not stacked below the first.");
	}

	[TestMethod]
	public void DividerContainer_MarksItsDividerHandle()
	{
		container = BuildContainer();
		Start(() => container.Tick(1f / 60f));

		Assert.IsTrue(IsVisible(Handle), $"No divider handle was marked. Recorded: {string.Join(", ", Harness.Probe.KnownNames)}.");
	}

	[TestMethod]
	public void DividerContainer_DraggingTheHandleResizesTheZones()
	{
		container = BuildContainer();
		Start(() => container.Tick(1f / 60f));

		Vector2 handle = CenterOf(Handle);
		Harness.Mouse.Drag(handle.X, handle.Y, handle.X + 120f, handle.Y);
		Step(2);

		float leftSize = container.GetSizes()[0];

		Assert.IsTrue(leftSize > 0.3f, $"Dragging the handle right left the first zone at {leftSize}.");
	}

	[TestMethod]
	public void DividerContainer_ReportsAResize()
	{
		container = BuildContainer();
		Start(() => container.Tick(1f / 60f));
		resized.Clear();

		Vector2 handle = CenterOf(Handle);
		Harness.Mouse.Drag(handle.X, handle.Y, handle.X + 120f, handle.Y);
		Step(2);

		Assert.IsTrue(resized.Count > 0, "Resizing the container fired no callback.");
	}

	[TestMethod]
	public void DividerContainer_SetSize_MovesTheZone()
	{
		container = BuildContainer();
		Start(() => container.Tick(1f / 60f));
		byte[] before = Snapshot();

		container.SetSize(LeftZone, 0.6f);
		Step(2);

		Assert.AreEqual(0.6f, container.GetSizes()[0], "The zone's size was not set.");
		Assert.IsTrue(PixelsChangedSince(before) > 0, "Resizing a zone changed nothing on screen.");
	}

	[TestMethod]
	public void DividerContainer_RemovingAZoneDropsItsContent()
	{
		container = BuildContainer();
		Start(() => container.Tick(1f / 60f));

		container.Remove(RightZone);
		Step(2);

		Assert.IsFalse(IsVisible("right-content"), "A removed zone was still drawn.");
		Assert.IsTrue(IsVisible("left-content"), "Removing one zone stopped the other from drawing.");
	}

	[TestMethod]
	public void DividerContainer_AClickOnAPopupOverTheHandle_DoesNotResize()
	{
		bool coverOpen = false;
		bool coverClicked = false;
		container = new ImGuiWidgets.DividerContainer(ContainerId, ImGuiWidgets.DividerLayout.Columns);
		container.Add(LeftZone, 0.3f, true, _ =>
		{
			// A popup opened from inside a zone, as a combo's list is, laid over the divider.
			if (coverOpen && !ImGui.IsPopupOpen("cover"))
			{
				ImGui.OpenPopup("cover");
			}

			Vector2 at = Harness.Probe.Rect(Handle) is Rectangle handle
				? new Vector2(handle.MinX - 40f, handle.MinY + 20f)
				: Vector2.Zero;
			ImGui.SetNextWindowPos(at);
			if (ImGui.BeginPopup("cover"))
			{
				coverClicked |= ImGui.Button("over the handle", new Vector2(120f, 60f));
				Mark("cover-button");
				ImGui.EndPopup();
			}
		});
		container.Add(RightZone, 0.7f, false, _ => ImGui.TextUnformatted("Right content"));

		Start(() => container.Tick(1f / 60f));
		coverOpen = true;
		Step(3);

		Rectangle handleRect = RectOf(Handle);
		Rectangle button = RectOf("cover-button");
		Assert.IsTrue(button.MinX < handleRect.MinX && button.MaxX > handleRect.MaxX, "The popup does not cover the handle, so this tests nothing.");

		Harness.Mouse.Click(handleRect.MinX + (handleRect.Width / 2f), button.MinY + (button.Height / 2f));
		Step(2);

		Assert.AreEqual(0.3f, container.GetSizes()[0], "A click on the popup resized the container underneath.");
		Assert.IsTrue(coverClicked, "The popup's button didn't get the click.");
	}

	[TestMethod]
	public void DividerContainer_MarksItselfAndEveryZone()
	{
		container = BuildContainer();
		Start(() => container.Tick(1f / 60f));

		Assert.IsTrue(IsVisible(ContainerId), "The container did not mark itself.");
		Rectangle whole = RectOf(ContainerId);

		foreach (string zone in new[] { LeftZone, RightZone })
		{
			Rectangle rect = RectOf($"{ContainerId}/zone/{zone}");
			Assert.IsTrue(rect.MinX >= whole.MinX && rect.MaxX <= whole.MaxX + 1, $"Zone '{zone}' at {rect} is outside the container at {whole}.");
		}

		Assert.IsTrue(RectOf($"{ContainerId}/zone/{RightZone}").MinX > RectOf($"{ContainerId}/zone/{LeftZone}").MinX, "The zones were not marked in order.");
	}

	// Issue #724: with three or more zones, or in rows, the gaps between zones, the two outer margins
	// and the dividers' place in their gaps drifted apart.

	[TestMethod]
	[DataRow(ImGuiWidgets.DividerLayout.Columns, DisplayName = "Columns")]
	[DataRow(ImGuiWidgets.DividerLayout.Rows, DisplayName = "Rows")]
	public void DividerContainer_ThreeZones_HaveEqualGapsAndMarginsWithCentredDividers(ImGuiWidgets.DividerLayout layout)
	{
		AssertEvenlySpaced(layout, [("props", 0.24f), ("graph", 0.50f), ("views", 0.26f)]);
	}

	[TestMethod]
	[DataRow(ImGuiWidgets.DividerLayout.Columns, DisplayName = "Columns")]
	[DataRow(ImGuiWidgets.DividerLayout.Rows, DisplayName = "Rows")]
	public void DividerContainer_TwoEvenZones_CentreTheirDivider(ImGuiWidgets.DividerLayout layout)
	{
		AssertEvenlySpaced(layout, [("v1", 0.5f), ("v2", 0.5f)]);
	}

	[TestMethod]
	[DataRow(ImGuiWidgets.DividerLayout.Columns, DisplayName = "Columns")]
	[DataRow(ImGuiWidgets.DividerLayout.Rows, DisplayName = "Rows")]
	public void DividerContainer_FiveZones_HaveEqualGapsAndMarginsWithCentredDividers(ImGuiWidgets.DividerLayout layout)
	{
		AssertEvenlySpaced(layout, [("a", 0.1f), ("b", 0.3f), ("c", 0.15f), ("d", 0.25f), ("e", 0.2f)]);
	}

	[TestMethod]
	public void DividerContainer_ThreeZones_DraggingTheSecondDividerFollowsThePointer()
	{
		container = new ImGuiWidgets.DividerContainer(ContainerId, ImGuiWidgets.DividerLayout.Columns);
		container.Add("props", 0.24f, _ => { });
		container.Add("graph", 0.50f, _ => { });
		container.Add("views", 0.26f, _ => { });
		Start(() => container.Tick(1f / 60f));

		Vector2 handle = CenterOf($"{ContainerId}/divider/graph");
		Harness.Mouse.Drag(handle.X, handle.Y, handle.X - 60f, handle.Y);
		Step(2);

		Assert.AreEqual(handle.X - 60f, CenterOf($"{ContainerId}/divider/graph").X, 1.5f, "The divider did not end up under the pointer.");
		Assert.AreEqual(0.24f, container.GetSizes()[0], 0.0001f, "Dragging the second divider resized the first zone.");
	}

	private void AssertEvenlySpaced(ImGuiWidgets.DividerLayout layout, (string Id, float Size)[] zones)
	{
		container = new ImGuiWidgets.DividerContainer(ContainerId, layout);
		foreach ((string id, float size) in zones)
		{
			container.Add(id, size, _ => { });
		}

		Start(() => container.Tick(1f / 60f));

		bool columns = layout == ImGuiWidgets.DividerLayout.Columns;
		float Lead(Rectangle r) => columns ? r.MinX : r.MinY;
		float Trail(Rectangle r) => columns ? r.MaxX : r.MaxY;
		float Middle(Rectangle r) => columns ? r.MinX + (r.Width / 2f) : r.MinY + (r.Height / 2f);

		Rectangle whole = RectOf(ContainerId);
		Rectangle[] rects = [.. zones.Select(z => RectOf($"{ContainerId}/zone/{z.Id}"))];

		float leading = Lead(rects[0]) - Lead(whole);
		float trailing = Trail(whole) - Trail(rects[^1]);
		Assert.AreEqual(leading, trailing, 1f, $"The outer margins differ: {leading} before the first zone, {trailing} after the last.");
		Assert.IsGreaterThan(0f, leading, "The first zone touches the container's edge.");

		float firstGap = Lead(rects[1]) - Trail(rects[0]);
		for (int i = 0; i < rects.Length - 1; i++)
		{
			float gap = Lead(rects[i + 1]) - Trail(rects[i]);
			Assert.AreEqual(firstGap, gap, 0.01f, $"The gap after zone {i} is {gap}, the first is {firstGap}.");
			Assert.AreEqual(leading * 2f, gap, 1f, $"The gap after zone {i} is not twice the outer margin.");

			float divider = Middle(RectOf($"{ContainerId}/divider/{zones[i].Id}"));
			float gapMiddle = (Trail(rects[i]) + Lead(rects[i + 1])) / 2f;
			Assert.AreEqual(gapMiddle, divider, 0.01f, $"The divider after zone {i} is not centred in its gap.");
		}
	}
}
