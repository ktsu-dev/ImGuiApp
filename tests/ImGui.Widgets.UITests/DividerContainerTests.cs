// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using System.Collections.Generic;
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
}
