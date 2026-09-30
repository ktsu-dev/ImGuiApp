// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using System.Collections.Generic;

using ktsu.Semantics.Color;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests the evaluator and interaction behind GradientEditor: sampling, seeding and sorting,
/// pressing, dragging and removing stops. All pure — no ImGui context required.
/// </summary>
[TestClass]
public class GradientEditorStateTests
{
	private const float Grab = 0.02f;

	private static readonly Color Black = new(0, 0, 0, 1);
	private static readonly Color White = new(1, 1, 1, 1);
	private static readonly Color Red = new(1, 0, 0, 1);
	private static readonly Color Green = new(0, 1, 0, 1);
	private static readonly Color Blue = new(0, 0, 1, 1);
	private static readonly Color Yellow = new(1, 1, 0, 1);

	private static List<GradientStop> ThreeStops() => [new(0f, Red), new(0.5f, Green), new(1f, Blue)];

	private static ImGuiWidgets.GradientEditorState PressMiddle(List<GradientStop> stops)
	{
		ImGuiWidgets.GradientEditorState state = new();
		state.Press(stops, 0.5f, Grab);
		return state;
	}

	[TestMethod]
	public void SampleGradient_EmptyIsTransparent() =>
		Assert.AreEqual(new Color(0, 0, 0, 0), ImGuiWidgets.SampleGradient([], 0.5f));

	[TestMethod]
	public void SampleGradient_SingleStopIsConstant()
	{
		List<GradientStop> stops = [new(0.3f, Red)];

		Assert.AreEqual(Red, ImGuiWidgets.SampleGradient(stops, 0f));
		Assert.AreEqual(Red, ImGuiWidgets.SampleGradient(stops, 0.3f));
		Assert.AreEqual(Red, ImGuiWidgets.SampleGradient(stops, 1f));
	}

	[TestMethod]
	public void SampleGradient_MidpointLerpsInLinearRgb()
	{
		Color mid = ImGuiWidgets.SampleGradient([new(0f, Black), new(1f, White)], 0.5f);

		Assert.AreEqual(0.5, mid.R, 1e-9);
		Assert.AreEqual(0.5, mid.G, 1e-9);
		Assert.AreEqual(0.5, mid.B, 1e-9);
	}

	[TestMethod]
	public void SampleGradient_InterpolatesAlpha() =>
		Assert.AreEqual(0.5, ImGuiWidgets.SampleGradient([new(0f, new Color(0, 0, 0, 1)), new(1f, new Color(0, 0, 0, 0))], 0.5f).A, 1e-9);

	[TestMethod]
	public void SampleGradient_ClampsOutsideTheStops()
	{
		List<GradientStop> stops = [new(0.2f, Red), new(0.8f, Blue)];

		Assert.AreEqual(Red, ImGuiWidgets.SampleGradient(stops, 0f));
		Assert.AreEqual(Blue, ImGuiWidgets.SampleGradient(stops, 1f));
	}

	[TestMethod]
	public void SampleGradient_CoincidentStopsMakeAHardEdge()
	{
		List<GradientStop> stops = [new(0f, Red), new(0.5f, Red), new(0.5f, Blue), new(1f, Blue)];

		Assert.AreEqual(Red, ImGuiWidgets.SampleGradient(stops, 0.4999f));
		Assert.AreEqual(Blue, ImGuiWidgets.SampleGradient(stops, 0.5f));
	}

	[TestMethod]
	public void SampleGradient_NaNIsTreatedAsZero() =>
		Assert.AreEqual(Red, ImGuiWidgets.SampleGradient([new(0f, Red), new(1f, Blue)], float.NaN));

	[TestMethod]
	public void SampleGradient_DoesNotRequireSortedInput() =>
		Assert.AreEqual(Red.Lerp(Blue, 0.25), ImGuiWidgets.SampleGradient([new(1f, Blue), new(0f, Red)], 0.25f));

	[TestMethod]
	public void EnsureValid_SeedsAnEmptyList()
	{
		List<GradientStop> stops = [];

		Assert.IsTrue(new ImGuiWidgets.GradientEditorState().EnsureValid(stops));
		Assert.AreSequenceEqual([new GradientStop(0f, Black), new GradientStop(1f, White)], stops);
	}

	[TestMethod]
	public void EnsureValid_MirrorsASingleStop()
	{
		List<GradientStop> stops = [new(0.3f, Red)];

		new ImGuiWidgets.GradientEditorState().EnsureValid(stops);

		Assert.AreSequenceEqual([new GradientStop(0.3f, Red), new GradientStop(1f, Red)], stops);
	}

	[TestMethod]
	public void EnsureValid_SortsAndKeepsColoursWithTheirStops()
	{
		List<GradientStop> stops = [new(1f, Blue), new(0f, Red)];

		new ImGuiWidgets.GradientEditorState().EnsureValid(stops);

		Assert.AreSequenceEqual([new GradientStop(0f, Red), new GradientStop(1f, Blue)], stops);
	}

	[TestMethod]
	public void EnsureValid_IsStableForEqualPositions()
	{
		List<GradientStop> stops = [new(0.5f, Red), new(0.5f, Blue), new(0f, Green)];

		new ImGuiWidgets.GradientEditorState().EnsureValid(stops);

		Assert.AreSequenceEqual([new GradientStop(0f, Green), new GradientStop(0.5f, Red), new GradientStop(0.5f, Blue)], stops);
	}

	[TestMethod]
	public void EnsureValid_FixesNonFinitePositions()
	{
		List<GradientStop> stops = [new(float.NaN, Red), new(3f, Blue)];

		Assert.IsTrue(new ImGuiWidgets.GradientEditorState().EnsureValid(stops));
		Assert.AreEqual(0f, stops[0].Position);
		Assert.AreEqual(1f, stops[1].Position);
	}

	[TestMethod]
	public void EnsureValid_UnchangedReturnsFalse() =>
		Assert.IsFalse(new ImGuiWidgets.GradientEditorState().EnsureValid(ThreeStops()));

	[TestMethod]
	public void Press_OnAStopSelectsItAndDoesNotInsert()
	{
		List<GradientStop> stops = ThreeStops();
		ImGuiWidgets.GradientEditorState state = new();

		Assert.IsFalse(state.Press(stops, 0.51f, Grab));
		Assert.AreEqual(1, state.SelectedIndex);
		Assert.HasCount(3, stops);
	}

	[TestMethod]
	public void Press_OnEmptySpaceInsertsASampledStop()
	{
		List<GradientStop> stops = [new(0f, Black), new(1f, White)];
		List<GradientStop> original = [.. stops];
		ImGuiWidgets.GradientEditorState state = new();

		Assert.IsTrue(state.Press(stops, 0.25f, Grab));
		Assert.HasCount(3, stops);
		Assert.AreEqual(1, state.SelectedIndex);
		Assert.AreEqual(new GradientStop(0.25f, ImGuiWidgets.SampleGradient(original, 0.25f)), stops[1]);
	}

	[TestMethod]
	public void Drag_MovesTheSelectedStopAndKeepsItsColour()
	{
		List<GradientStop> stops = ThreeStops();
		ImGuiWidgets.GradientEditorState state = PressMiddle(stops);

		Assert.IsTrue(state.Drag(stops, 0.7f, 0f));
		Assert.AreEqual(0.7f, stops[1].Position, 1e-6f);
		Assert.AreEqual(Green, stops[1].Color);
	}

	[TestMethod]
	public void Drag_NeverCrossesANeighbour()
	{
		List<GradientStop> stops = ThreeStops();
		ImGuiWidgets.GradientEditorState state = PressMiddle(stops);

		state.Drag(stops, 0.99f, 0f);
		state.Drag(stops, 1.5f, 0f);

		Assert.IsTrue(stops[1].Position <= stops[2].Position, $"The middle stop crossed to {stops[1].Position}.");
		Assert.AreSequenceEqual([Red, Green, Blue], stops.ConvertAll(stop => stop.Color));
	}

	[TestMethod]
	public void Drag_FarOutsideMarksPendingRemoval()
	{
		List<GradientStop> stops = ThreeStops();
		ImGuiWidgets.GradientEditorState state = PressMiddle(stops);

		state.Drag(stops, 0.5f, 30f);

		Assert.IsTrue(state.PendingRemoval);
	}

	[TestMethod]
	public void Drag_FarOutsideWithTwoStopsIsNotPending()
	{
		List<GradientStop> stops = [new(0f, Red), new(1f, Blue)];
		ImGuiWidgets.GradientEditorState state = new();
		state.Press(stops, 0f, Grab);

		state.Drag(stops, 0f, 30f);

		Assert.IsFalse(state.PendingRemoval);
	}

	[TestMethod]
	public void Release_RemovesAPendingStop()
	{
		List<GradientStop> stops = ThreeStops();
		ImGuiWidgets.GradientEditorState state = PressMiddle(stops);
		state.Drag(stops, 0.5f, 30f);

		Assert.IsTrue(state.Release(stops));
		Assert.HasCount(2, stops);
		Assert.AreEqual(-1, state.SelectedIndex);
		Assert.IsFalse(state.PendingRemoval);
	}

	[TestMethod]
	public void DeleteSelected_RemovesAboveTheMinimum()
	{
		List<GradientStop> stops = ThreeStops();
		ImGuiWidgets.GradientEditorState state = PressMiddle(stops);
		state.Release(stops);

		Assert.IsTrue(state.DeleteSelected(stops));
		Assert.HasCount(2, stops);

		state.Press(stops, 0f, Grab);
		state.Release(stops);

		Assert.IsFalse(state.DeleteSelected(stops));
		Assert.HasCount(2, stops);
	}

	[TestMethod]
	public void SetSelectedColor_ReplacesOnlyTheSelectedColour()
	{
		List<GradientStop> stops = ThreeStops();
		ImGuiWidgets.GradientEditorState state = PressMiddle(stops);
		state.Release(stops);

		Assert.IsTrue(state.SetSelectedColor(stops, Yellow));
		Assert.AreSequenceEqual([Red, Yellow, Blue], stops.ConvertAll(stop => stop.Color));
		Assert.AreSequenceEqual([0f, 0.5f, 1f], stops.ConvertAll(stop => stop.Position));
		Assert.IsFalse(state.SetSelectedColor(stops, Yellow));

		Assert.IsFalse(new ImGuiWidgets.GradientEditorState().SetSelectedColor(stops, Red));
	}
}
