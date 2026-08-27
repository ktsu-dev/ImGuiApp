// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using System;
using System.Numerics;

using ktsu.ImGui.App.Testing;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>Drives <see cref="ImGuiWidgets.Text(string)"/> and its centered variants on their own.</summary>
/// <remarks>
/// The centered variants position the cursor and then leave a zero-width spacer as the last
/// submitted item, so their placement is measured from the pixels rather than from the probe: each
/// test renders a frame without the text, then one with it, and compares what changed.
/// </remarks>
[TestClass]
public sealed class TextTests : WidgetTest
{
	private const string Sample = "Widgets";
	private const string Name = "text";

	private bool show;

	[TestMethod]
	public void Text_DrawsTheString()
	{
		Start(() =>
		{
			ImGuiWidgets.Text(Sample);
			Mark(Name);
		});

		Rectangle rect = RectOf(Name);

		Assert.IsTrue(rect.Width > 0 && rect.Height > 0, "The text reserved no space.");
		AssertSomethingWasDrawn("the text");
	}

	[TestMethod]
	public void Text_IsLeftAligned()
	{
		show = false;
		Start(() =>
		{
			if (show)
			{
				ImGuiWidgets.Text(Sample);
			}
		});

		byte[] blank = Snapshot();
		show = true;
		Step(2);

		Rectangle drawn = BoundsOfDifference(blank) ?? throw new InvalidOperationException("The text drew nothing.");

		Assert.IsTrue(drawn.MinX < Harness.Options.Width / 4, $"Left-aligned text started at {drawn.MinX} in a {Harness.Options.Width}px window.");
	}

	[TestMethod]
	public void TextCentered_IsCenteredInTheWindow()
	{
		show = false;
		Start(() =>
		{
			if (show)
			{
				ImGuiWidgets.TextCentered(Sample);
			}
		});

		byte[] blank = Snapshot();
		show = true;
		Step(2);

		Rectangle drawn = BoundsOfDifference(blank) ?? throw new InvalidOperationException("Centered text drew nothing.");
		int textCenter = drawn.MinX + (drawn.Width / 2);
		int windowCenter = Harness.Options.Width / 2;

		Assert.IsTrue(
			Math.Abs(textCenter - windowCenter) <= 20,
			$"Centered text sat at {textCenter}, well off the window's center of {windowCenter}.");
	}

	[TestMethod]
	public void TextCenteredWithin_CentersInsideTheContainerItIsGiven()
	{
		Vector2 container = new(400f, 40f);
		show = false;

		Start(() =>
		{
			if (show)
			{
				ImGuiWidgets.TextCenteredWithin(Sample, container);
			}
		});

		byte[] blank = Snapshot();
		show = true;
		Step(2);

		Rectangle drawn = BoundsOfDifference(blank) ?? throw new InvalidOperationException("Centered-within text drew nothing.");
		int textCenter = drawn.MinX + (drawn.Width / 2);

		// The container starts at the window's content origin, so its center is half its width in.
		Assert.IsTrue(
			Math.Abs(textCenter - (int)(container.X / 2f)) <= 20,
			$"Text centered in a {container.X}px container sat at {textCenter}, not near {container.X / 2f}.");
	}

	[TestMethod]
	public void TextCenteredWithin_ANarrowerContainerMovesTheTextLeft()
	{
		show = false;

		Start(() => ImGuiWidgets.TextCenteredWithin(Sample, show ? new Vector2(120f, 40f) : new Vector2(400f, 40f)));

		byte[] wide = Snapshot();
		show = true;
		Step(2);

		Rectangle changed = BoundsOfDifference(wide) ?? throw new InvalidOperationException("Narrowing the container moved nothing.");

		Assert.IsTrue(changed.Width > 0, "The text did not move when its container narrowed.");
	}
}
