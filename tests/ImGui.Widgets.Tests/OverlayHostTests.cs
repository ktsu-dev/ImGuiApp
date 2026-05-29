// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Widgets.Tests;

using System;
using System.Collections.Generic;

using ktsu.ImGui.Widgets.Overlays;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class OverlayHostTests
{
	[TestMethod]
	public void New_IsEmpty()
	{
		OverlayHost host = new();

		Assert.AreEqual(0, host.Count);
		Assert.IsFalse(host.HasOverlays);
	}

	[TestMethod]
	public void Show_RegistersOverlay()
	{
		OverlayHost host = new();

		host.Show("toast", () => { });

		Assert.AreEqual(1, host.Count);
		Assert.IsTrue(host.HasOverlays);
		Assert.IsTrue(host.IsShown("toast"));
	}

	[TestMethod]
	public void Show_SameKeyTwice_DoesNotDuplicate()
	{
		OverlayHost host = new();

		host.Show("sheet", () => { });
		host.Show("sheet", () => { });

		Assert.AreEqual(1, host.Count);
	}

	[TestMethod]
	public void Show_SameKey_UpdatesDrawCallback()
	{
		OverlayHost host = new();
		List<string> drawn = [];

		host.Show("k", () => drawn.Add("first"));
		host.Show("k", () => drawn.Add("second"));
		host.Render();

		string[] expected = ["second"];
		CollectionAssert.AreEqual(expected, drawn);
	}

	[TestMethod]
	public void Dismiss_RemovesOverlay_AndReportsTrue()
	{
		OverlayHost host = new();
		host.Show("toast", () => { });

		bool removed = host.Dismiss("toast");

		Assert.IsTrue(removed);
		Assert.IsFalse(host.IsShown("toast"));
		Assert.AreEqual(0, host.Count);
	}

	[TestMethod]
	public void Dismiss_UnknownKey_ReportsFalse()
	{
		OverlayHost host = new();

		Assert.IsFalse(host.Dismiss("nope"));
	}

	[TestMethod]
	public void Clear_RemovesEverything()
	{
		OverlayHost host = new();
		host.Show("a", () => { });
		host.Show("b", () => { });

		host.Clear();

		Assert.AreEqual(0, host.Count);
		Assert.IsFalse(host.HasOverlays);
	}

	[TestMethod]
	public void Render_Empty_DoesNothing()
	{
		OverlayHost host = new();

		// Must not throw with nothing registered.
		host.Render();

		Assert.AreEqual(0, host.Count);
	}

	[TestMethod]
	public void Render_DrawsInAscendingLayerOrder()
	{
		OverlayHost host = new();
		List<string> drawn = [];

		// Register out of z-order on purpose.
		host.Show("toast", () => drawn.Add("toast"), OverlayLayer.Toast);
		host.Show("bg", () => drawn.Add("bg"), OverlayLayer.Background);
		host.Show("dialog", () => drawn.Add("dialog"), OverlayLayer.Dialog);

		host.Render();

		string[] expected = ["bg", "dialog", "toast"];
		CollectionAssert.AreEqual(expected, drawn);
	}

	[TestMethod]
	public void Render_SameLayer_DrawsInRegistrationOrder()
	{
		OverlayHost host = new();
		List<string> drawn = [];

		host.Show("first", () => drawn.Add("first"), OverlayLayer.Toast);
		host.Show("second", () => drawn.Add("second"), OverlayLayer.Toast);
		host.Show("third", () => drawn.Add("third"), OverlayLayer.Toast);

		host.Render();

		string[] expected = ["first", "second", "third"];
		CollectionAssert.AreEqual(expected, drawn);
	}

	[TestMethod]
	public void Render_ReShowingKey_PreservesRegistrationOrder()
	{
		OverlayHost host = new();
		List<string> drawn = [];

		host.Show("a", () => { }, OverlayLayer.Toast);
		host.Show("b", () => { }, OverlayLayer.Toast);

		// Re-show "a" — it should keep its original (earlier) slot, not jump to the front.
		host.Show("a", () => drawn.Add("a"), OverlayLayer.Toast);
		host.Show("b", () => drawn.Add("b"), OverlayLayer.Toast);

		host.Render();

		string[] expected = ["a", "b"];
		CollectionAssert.AreEqual(expected, drawn);
	}

	[TestMethod]
	public void Render_DismissDuringDraw_IsSafe_AndAlreadySnapshottedStillDraw()
	{
		OverlayHost host = new();
		List<string> drawn = [];

		host.Show("a", () =>
		{
			drawn.Add("a");
			// Dismiss a later overlay mid-render; it was already snapshotted so it still draws.
			host.Dismiss("b");
		}, OverlayLayer.Background);
		host.Show("b", () => drawn.Add("b"), OverlayLayer.Toast);

		host.Render();

		string[] expected = ["a", "b"];
		CollectionAssert.AreEqual(expected, drawn);
		// The dismissal takes effect for the next frame.
		Assert.IsFalse(host.IsShown("b"));
	}

	[TestMethod]
	public void Render_ShowDuringDraw_DefersToNextFrame()
	{
		OverlayHost host = new();
		List<string> drawn = [];

		host.Show("a", () =>
		{
			drawn.Add("a");
			host.Show("b", () => drawn.Add("b"), OverlayLayer.Toast);
		}, OverlayLayer.Background);

		host.Render();
		string[] firstFrame = ["a"];
		CollectionAssert.AreEqual(firstFrame, drawn);

		drawn.Clear();
		host.Render();
		string[] secondFrame = ["a", "b"];
		CollectionAssert.AreEqual(secondFrame, drawn);
	}

	[TestMethod]
	public void Render_CanBeCalledRepeatedly_WithStableOrder()
	{
		OverlayHost host = new();
		List<string> drawn = [];

		host.Show("bg", () => drawn.Add("bg"), OverlayLayer.Background);
		host.Show("toast", () => drawn.Add("toast"), OverlayLayer.Toast);

		host.Render();
		host.Render();

		string[] expected = ["bg", "toast", "bg", "toast"];
		CollectionAssert.AreEqual(expected, drawn);
	}

	[TestMethod]
	public void Show_NullKey_Throws() =>
		Assert.ThrowsExactly<ArgumentNullException>(() => new OverlayHost().Show(null!, () => { }));

	[TestMethod]
	public void Show_NullDraw_Throws() =>
		Assert.ThrowsExactly<ArgumentNullException>(() => new OverlayHost().Show("k", null!));

	[TestMethod]
	public void Dismiss_NullKey_Throws() =>
		Assert.ThrowsExactly<ArgumentNullException>(() => new OverlayHost().Dismiss(null!));

	[TestMethod]
	public void IsShown_NullKey_Throws() =>
		Assert.ThrowsExactly<ArgumentNullException>(() => new OverlayHost().IsShown(null!));
}
