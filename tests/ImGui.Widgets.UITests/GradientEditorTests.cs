// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.App.Testing;
using ktsu.Semantics.Color;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>Drives <see cref="ImGuiWidgets.GradientEditor(string, IList{GradientStop}, Vector2)"/> on its own.</summary>
[TestClass]
public sealed class GradientEditorTests : WidgetTest
{
	private static readonly Color Red = Color.FromBytes(255, 0, 0, 255);
	private static readonly Color Green = Color.FromBytes(0, 255, 0, 255);
	private static readonly Color Blue = Color.FromBytes(0, 0, 255, 255);

	private List<GradientStop> stops = [new(0f, Red), new(0.5f, Green), new(1f, Blue)];
	private bool changed;

	private void Draw() => changed |= ImGuiWidgets.GradientEditor("Gradient", stops, new Vector2(300f, 24f));

	[TestMethod]
	public void GradientEditor_MarksTheBarAndEachStop()
	{
		Start(Draw);

		Assert.IsTrue(IsVisible("Gradient/bar"), "The bar was not marked.");
		Assert.IsTrue(IsVisible("Gradient/stop0"), "The first stop was not marked.");
		Assert.IsTrue(IsVisible("Gradient/stop1"), "The middle stop was not marked.");
		Assert.IsTrue(IsVisible("Gradient/stop2"), "The last stop was not marked.");
		Assert.IsFalse(IsVisible("Gradient/colour"), "The colour editor showed with nothing selected.");
	}

	[TestMethod]
	public void GradientEditor_ClickingEmptySpaceAddsAStop()
	{
		Start(Draw);

		ClickFraction("Gradient/bar", 0.25f, 0.3f);

		Assert.HasCount(4, stops);
		Assert.IsTrue(stops[1].Position is >= 0.23f and <= 0.27f, $"The new stop landed at {stops[1].Position}.");
		Assert.IsTrue(changed, "Adding a stop reported no change.");
	}

	[TestMethod]
	public void GradientEditor_ClickingAStopSelectsIt()
	{
		Start(Draw);

		Click("Gradient/stop1");

		Assert.IsTrue(IsVisible("Gradient/colour"), "Selecting a stop did not show the colour editor.");
		Assert.HasCount(3, stops);
	}

	[TestMethod]
	public void GradientEditor_DraggingAStopMovesIt()
	{
		Start(Draw);

		DragAcross("Gradient/bar", 0.5f, 0.7f, 0.3f);

		Assert.IsTrue(stops[1].Position is >= 0.68f and <= 0.72f, $"The stop landed at {stops[1].Position}.");
		Assert.AreEqual(Green, stops[1].Color, "Dragging the stop changed its colour.");
	}

	[TestMethod]
	public void GradientEditor_StopsNeverCross()
	{
		Start(Draw);

		DragAcross("Gradient/bar", 0.5f, 0.99f, 0.3f);

		for (int i = 1; i < stops.Count; i++)
		{
			Assert.IsTrue(stops[i - 1].Position <= stops[i].Position, $"Stop {i} is out of order.");
		}

		Assert.AreSequenceEqual([Red, Green, Blue], stops.Select(stop => stop.Color));
	}

	[TestMethod]
	public void GradientEditor_DraggingAStopAwayRemovesIt()
	{
		Start(Draw);

		Vector2 center = CenterOf("Gradient/stop1");
		Harness.Mouse.Drag(center.X, center.Y, center.X, center.Y + 80f);
		Step();

		Assert.HasCount(2, stops);
		Assert.IsFalse(stops.Any(stop => stop.Color == Green), "The dragged-away stop is still there.");
	}

	[TestMethod]
	public void GradientEditor_NeverRemovesBelowTwoStops()
	{
		stops = [new(0f, Red), new(1f, Blue)];
		Start(Draw);

		Vector2 center = CenterOf("Gradient/stop0");
		Harness.Mouse.Drag(center.X, center.Y, center.X, center.Y + 80f);
		Step();

		Assert.HasCount(2, stops);
	}

	[TestMethod]
	public void GradientEditor_DeleteRemovesTheSelectedStop()
	{
		Start(Draw);

		Click("Gradient/stop1");
		Harness.Keyboard.Press(ImGuiKey.Delete);
		Step();

		Assert.HasCount(2, stops);
	}

	[TestMethod]
	public void GradientEditor_DrawsTheSampledGradient()
	{
		Start(Draw);

		Rectangle bar = RectOf("Gradient/bar");
		int y = bar.MinY + (bar.Height / 4);
		Rgba32 left = Harness.Target.GetPixel(bar.MinX + (bar.Width * 5 / 100), y);
		Rgba32 right = Harness.Target.GetPixel(bar.MinX + (bar.Width * 95 / 100), y);

		Assert.IsTrue(left.R > left.B, $"The left end is not red: R {left.R}, B {left.B}.");
		Assert.IsTrue(right.B > right.R, $"The right end is not blue: R {right.R}, B {right.B}.");
	}

	[TestMethod]
	public void GradientEditor_SeedsAnEmptyList()
	{
		stops = [];
		Start(Draw);

		Assert.HasCount(2, stops);
		Assert.AreEqual(0f, stops[0].Position);
		Assert.AreEqual(1f, stops[1].Position);
	}

	[TestMethod]
	public void GradientEditor_ACallerColourEditorRecoloursTheSelectedStop()
	{
		string? editorId = null;
		Start(() => changed |= ImGuiWidgets.GradientEditor("Gradient", stops, (id, ref colour) =>
		{
			editorId = id;
			bool pressed = ImGui.Button("Make white");
			Mark("Make white");
			if (pressed)
			{
				colour = Color.FromBytes(255, 255, 255, 255);
			}

			return pressed;
		}, new Vector2(300f, 24f)));

		Assert.IsNull(editorId, "The caller's editor drew with nothing selected.");

		Click("Gradient/stop1");
		Assert.AreEqual("colour", editorId, "The caller's editor was not given the colour id.");
		Assert.IsFalse(IsVisible("Gradient/colour"), "The built-in colour editor drew beside the caller's.");

		changed = false;
		Click("Make white");

		Assert.AreEqual(Color.FromBytes(255, 255, 255, 255), stops[1].Color, "The caller's colour did not reach the stop.");
		Assert.IsTrue(changed, "Recolouring a stop reported no change.");
	}
}
