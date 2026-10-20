// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.App.Testing;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>Drives <c>ImGuiWidgets.LogView</c> on its own.</summary>
[TestClass]
public sealed class LogViewTests : WidgetTest
{
	private static readonly DateTime Origin = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);

	private readonly List<LogEntry> entries = [];
	private readonly ImGuiWidgets.LogViewState state = new();
	private ImGuiWidgets.LogViewOptions? options;
	private string? copied;
	private bool changed;

	private void DrawLog()
	{
		options ??= new() { Size = new Vector2(600f, 300f), OnCopy = text => copied = text };
		changed |= ImGuiWidgets.LogView("log", entries, state, options);
	}

	private void Add(LogViewLevel level, string message, string? category = null) =>
		entries.Add(new LogEntry(level, message, Origin.AddSeconds(entries.Count), category));

	/// <summary>Adds <paramref name="count"/> lines cycling through the levels, every fifth one a "load" line.</summary>
	private void AddMixed(int count)
	{
		for (int i = 0; i < count; i++)
		{
			LogViewLevel level = (LogViewLevel)(i % 6);
			string message = i % 5 == 0
				? string.Create(CultureInfo.InvariantCulture, $"load asset {i}")
				: string.Create(CultureInfo.InvariantCulture, $"line {i}");
			Add(level, message, "io");
		}
	}

	/// <summary>
	/// Clicks a row near its right end. The middle of a row is where a column border falls at this size,
	/// and a press on a resizable table's border grabs the border rather than the row.
	/// </summary>
	private void ClickRow(int row) => ClickFraction(RowProbe(row), 0.9f);

	private static string RowProbe(int row) =>
		string.Create(CultureInfo.InvariantCulture, $"log/rows/[{row}]");

	[TestMethod]
	public void LogView_DrawsRowsAndMarksItsParts()
	{
		AddMixed(20);
		Start(DrawLog);

		Assert.IsTrue(IsVisible("log"), "The widget itself is not marked.");
		Assert.IsTrue(IsVisible("log/filter"), "The filter is not marked.");
		Assert.IsTrue(IsVisible("log/rows/[0]"), "The first row is not marked.");
		Assert.IsTrue(IsVisible("log/Warn"), "The Warn toggle is not marked.");
	}

	[TestMethod]
	public void LogView_TypingInTheFilterHidesRows()
	{
		AddMixed(20);
		Start(DrawLog);

		Click("log/filter");
		Harness.Keyboard.Type("*load*");
		Step(2);

		Assert.AreEqual(4, state.VisibleCount);
		Assert.IsFalse(IsVisible(RowProbe(5)));
	}

	[TestMethod]
	public void LogView_LevelCheckboxHidesALevel()
	{
		AddMixed(20);
		Start(DrawLog);
		int debugCount = entries.Count(entry => entry.Level == LogViewLevel.Debug);

		Click("log/Debug");
		Step();

		Assert.IsFalse(state.IsLevelEnabled(LogViewLevel.Debug));
		Assert.AreEqual(entries.Count - debugCount, state.VisibleCount);
	}

	[TestMethod]
	public void LogView_ClickSelectsAndShiftClickExtends()
	{
		AddMixed(8);
		Start(DrawLog);

		ClickRow(1);

		Assert.AreEqual("1", string.Join(",", state.SelectedSourceIndices));
		Assert.IsTrue(changed);

		// The modifier gets a frame of its own: ImGui trickles input, and a key change queued in the same
		// frame as the pointer's move defers the move, and the press with it.
		HarnessKeyboard.KeyDown(ImGuiKey.ModShift);
		Step();
		ClickRow(3);
		HarnessKeyboard.KeyUp(ImGuiKey.ModShift);
		Step();

		Assert.AreEqual("1,2,3", string.Join(",", state.SelectedSourceIndices));
	}

	[TestMethod]
	public void LogView_CtrlCCopiesTheSelection()
	{
		AddMixed(8);
		Start(DrawLog);

		ClickRow(0);
		Hover(RowProbe(0));
		Harness.Keyboard.Press(ImGuiKey.C, ctrl: true);

		Assert.IsNotNull(copied, "Ctrl+C copied nothing.");
		Assert.AreEqual(state.BuildCopyText(entries, selectionOnly: true), copied);
	}

	[TestMethod]
	public void LogView_RightClickCopiesEveryVisibleRow()
	{
		AddMixed(5);
		Start(DrawLog);

		Vector2 row = CenterOf(RowProbe(2));
		Harness.Mouse.Click(row.X, row.Y, button: 1);
		Step();
		Click("Copy all visible");

		Assert.AreEqual(state.BuildCopyText(entries, selectionOnly: false), copied);
	}

	[TestMethod]
	public void LogView_FollowsNewLines()
	{
		AddMixed(200);
		Start(DrawLog);
		Step(3);

		AddMixed(50);
		Step(3);

		Assert.IsTrue(IsVisible(RowProbe(249)), "The newest row is not in view while following.");
	}

	[TestMethod]
	public void LogView_ScrollingUpStopsFollowing()
	{
		AddMixed(200);
		Start(DrawLog);
		Step(3);
		Assert.IsTrue(IsVisible(RowProbe(199)), "The list did not start at the bottom.");

		Vector2 list = CenterOf(RowProbe(199));
		Harness.Mouse.Wheel(list.X, list.Y - 60f, 5);
		Step(2);

		Assert.IsFalse(state.IsFollowing);

		AddMixed(10);
		Step(3);

		Assert.IsFalse(IsVisible(RowProbe(209)), "The list followed new lines after being scrolled up.");
	}

	[TestMethod]
	public void LogView_CollapsedRowShowsTheCount()
	{
		for (int i = 0; i < 5; i++)
		{
			Add(LogViewLevel.Warning, "frame hitch");
		}

		Start(DrawLog);

		Click("log/Collapse repeats");
		Step();

		Assert.AreEqual(1, state.VisibleCount);
		Assert.AreEqual(5, state.RepeatCountOf(0));
	}

	[TestMethod]
	public void LogView_EmptyLogDrawsWithoutRows()
	{
		Start(DrawLog);

		Assert.AreEqual(0, state.VisibleCount);
		Assert.IsTrue(state.IsFollowing);
		Assert.IsTrue(IsVisible("log"));
	}
}
