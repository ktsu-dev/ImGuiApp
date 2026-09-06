// Copyright (c) 2023-2026 ktsu-dev contributors

// ImGui contexts are global and the harness refuses to start while another is live, so every test
// in this assembly must have the process to itself.
[assembly: Microsoft.VisualStudio.TestTools.UnitTesting.DoNotParallelize]

namespace ktsu.ImGui.Popups.Tests;

using System;
using System.Collections.Generic;
using System.Numerics;

using ktsu.ImGui.App;
using ktsu.ImGui.App.Testing;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Drives <see cref="ImGuiPopups.SearchableList{TItem}"/> on its own: what a click on a row does,
/// which rows are listed, and which one is drawn as the current choice.
/// </summary>
[TestClass]
public sealed class SearchableListTests
{
	/// <summary>
	/// An item whose <see cref="object.ToString"/> names its type rather than its value, the way a
	/// polymorphic model's usually does, and which compares by value rather than by reference.
	/// </summary>
	/// <remarks>
	/// Both traits are what the list has to cope with. Several distinct choices share one
	/// ToString, so only the display text tells them apart, and a caller whose list is rebuilt
	/// every frame hands the list an equal but different object each time.
	/// </remarks>
	private sealed class Choice(string label)
	{
		internal string Label { get; } = label;

		public override string ToString() => nameof(Choice);

		public override bool Equals(object? obj) =>
			obj is Choice other && string.Equals(other.Label, Label, StringComparison.Ordinal);

		public override int GetHashCode() => Label.GetHashCode(StringComparison.Ordinal);
	}

	private static readonly HarnessOptions Viewport = new() { Width = 900, Height = 700 };

	private ImGuiAppHarness harness = null!;
	private ImGuiPopups.SearchableList<Choice> list = null!;
	private Choice? confirmed;

	/// <summary>Fresh instances every call, as a caller building its list per frame produces.</summary>
	private static List<Choice> Choices() => [new("Apple"), new("Banana"), new("Cherry")];

	[TestCleanup]
	public void TearDown() => harness?.Dispose();

	/// <summary>
	/// Starts a harness whose whole content is the list, opened on the supplied current choice.
	/// </summary>
	/// <param name="current">The choice the caller already holds, or null for none.</param>
	/// <param name="rebuildEveryFrame">
	/// Whether the items are built again for each frame. The list is redrawn from whatever it was
	/// handed, so a caller with a lazily built sequence gives it a new set of equal objects every
	/// frame, and the list has to recognise the choice across them.
	/// </param>
	private void Open(Choice? current = null, bool rebuildEveryFrame = true)
	{
		List<Choice> fixedItems = Choices();
		list = new ImGuiPopups.SearchableList<Choice>();

		harness = ImGuiAppHarness.Start(
			new ImGuiAppConfig
			{
				Title = nameof(SearchableListTests),
				OnRender = _ => list.ShowIfOpen(),
				SaveIniSettings = false,
			},
			Viewport);

		list.Open(
			"Pick one",
			"Choice",
			rebuildEveryFrame ? new RebuiltEachTime() : fixedItems,
			current,
			choice => choice.Label,
			choice => confirmed = choice);

		// One frame to submit the popup, another for it to be sized and centred.
		harness.Step(3);
	}

	/// <summary>A sequence that yields new instances on every enumeration.</summary>
	private sealed class RebuiltEachTime : IEnumerable<Choice>
	{
		public IEnumerator<Choice> GetEnumerator() => Choices().GetEnumerator();

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
	}

	private bool IsVisible(string name) => harness.Probe.WasSeenInFrame(name, harness.FrameCount - 1);

	private Vector2 CentreOf(string name)
	{
		Rectangle rect = harness.Probe.Rect(name)
			?? throw new AssertFailedException($"'{name}' was never drawn. Drawn so far: {string.Join(", ", harness.Probe.KnownNames)}.");

		return new Vector2(rect.MinX + (rect.Width / 2f), rect.MinY + (rect.Height / 2f));
	}

	[TestMethod]
	public void PickingARowConfirmsItAndClosesTheList()
	{
		Open();

		harness.Click("searchable-list/Banana");
		harness.Step(2);

		Assert.IsNotNull(confirmed, "Picking a row is the choice, so it confirms on its own.");
		Assert.AreEqual("Banana", confirmed.Label);
		Assert.IsFalse(IsVisible("searchable-list/search"), "Confirming should close the list.");
	}

	/// <summary>
	/// The list is keyed on what a row displays, not on what the item's ToString says, or choices
	/// that share a ToString would collapse into whichever one came first.
	/// </summary>
	[TestMethod]
	public void ChoicesThatShareAToStringAreAllListed()
	{
		Open();

		Assert.IsTrue(IsVisible("searchable-list/Apple"), "Apple was not listed.");
		Assert.IsTrue(IsVisible("searchable-list/Banana"), "Banana was not listed.");
		Assert.IsTrue(IsVisible("searchable-list/Cherry"), "Cherry was not listed.");
	}

	/// <summary>
	/// The row holding the current choice is drawn as selected, so the list shows what it is about
	/// to confirm before it is confirmed.
	/// </summary>
	/// <remarks>
	/// Measured on screen because that is what the question is: a selected row carries the header
	/// colour behind it, and an unselected one carries the window's background. Comparing the two
	/// asks whether anything is marked at all without pinning the theme's exact colours.
	/// </remarks>
	[TestMethod]
	public void TheRowHoldingTheCurrentChoiceIsMarked()
	{
		Open(current: new Choice("Banana"));

		Vector2 chosen = CentreOf("searchable-list/Banana");
		Vector2 other = CentreOf("searchable-list/Cherry");

		CapturedFrame frame = harness.Capture();
		Rgba32 onChosenRow = frame.GetPixel((int)chosen.X, (int)chosen.Y + 8);
		Rgba32 onOtherRow = frame.GetPixel((int)other.X, (int)other.Y + 8);

		Assert.AreNotEqual(
			onOtherRow,
			onChosenRow,
			"The row holding the current choice was drawn the same as the rest, so nothing showed as chosen.");
	}

	[TestMethod]
	public void OkConfirmsTheCurrentChoiceWithoutPickingARow()
	{
		Open(current: new Choice("Cherry"));

		harness.Click("searchable-list/ok");
		harness.Step(2);

		Assert.IsNotNull(confirmed);
		Assert.AreEqual("Cherry", confirmed.Label);
	}

	[TestMethod]
	public void CancelLeavesTheChoiceAlone()
	{
		Open(current: new Choice("Apple"));

		harness.Click("searchable-list/cancel");
		harness.Step(2);

		Assert.IsNull(confirmed, "Cancelling should confirm nothing.");
		Assert.IsFalse(IsVisible("searchable-list/search"), "Cancelling should close the list.");
	}
}
