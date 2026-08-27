// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using System;

using Hexa.NET.ImGui;

using ktsu.ImGui.App.Testing;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>Drives <see cref="ImGuiWidgets.Card"/> on its own.</summary>
[TestClass]
public sealed class CardTests : WidgetTest
{
	private const string Body = "Card body";

	private float width;

	private void DrawCard()
	{
		using (new ImGuiWidgets.Card(width))
		{
			ImGui.TextUnformatted(Body);
			Mark(Body);
		}
	}

	[TestMethod]
	public void Card_DrawsAPanelBehindItsContent()
	{
		bool show = false;

		Start(() =>
		{
			if (show)
			{
				DrawCard();
			}
			else
			{
				ImGui.TextUnformatted(Body);
				Mark(Body);
			}
		});

		byte[] plainText = Snapshot();
		show = true;
		Step(2);

		Assert.IsNotNull(BoundsOfDifference(plainText), "The card drew no panel behind its content.");
	}

	[TestMethod]
	public void Card_PadsItsContentInFromTheEdge()
	{
		Start(DrawCard);

		Rectangle text = RectOf(Body);

		// The window's own content starts a few pixels in; the card adds its padding on top of
		// that, so its content has to start further right than bare text would.
		Assert.IsTrue(text.MinX > 12, $"Card content started at {text.MinX}, no further in than unpadded text.");
	}

	[TestMethod]
	public void Card_FixedWidth_DrawsThatWide()
	{
		width = 260f;
		bool show = false;

		Start(() =>
		{
			if (show)
			{
				DrawCard();
			}
		});

		byte[] blank = Snapshot();
		show = true;
		Step(2);

		Rectangle drawn = BoundsOfDifference(blank) ?? throw new InvalidOperationException("The card drew nothing.");

		// The card paints a soft shadow outside its own rectangle, so the painted region runs a
		// few pixels wider than the width requested.
		Assert.IsTrue(
			drawn.Width >= (int)width && drawn.Width <= (int)width + 16,
			$"A {width}px card drew {drawn.Width}px wide, more than its shadow accounts for.");
	}

	[TestMethod]
	public void Card_ShrinksToFitItsContentWhenGivenNoWidth()
	{
		width = 0f;
		bool show = false;

		Start(() =>
		{
			if (show)
			{
				DrawCard();
			}
		});

		byte[] blank = Snapshot();
		show = true;
		Step(2);

		Rectangle drawn = BoundsOfDifference(blank) ?? throw new InvalidOperationException("The card drew nothing.");

		Assert.IsTrue(
			drawn.Width < Harness.Options.Width / 2,
			$"A content-sized card drew {drawn.Width}px wide in a {Harness.Options.Width}px window, so it did not shrink to fit.");
	}

	[TestMethod]
	public void Card_WithoutABorder_DrawsDifferently()
	{
		bool border = true;

		Start(() =>
		{
			using (new ImGuiWidgets.Card(240f, border: border))
			{
				ImGui.TextUnformatted(Body);
			}
		});

		byte[] bordered = Snapshot();
		border = false;
		Step(2);

		Assert.IsTrue(PixelsChangedSince(bordered) > 0, "A borderless card drew the same as a bordered one.");
	}
}
