// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using System.Numerics;

using Hexa.NET.ImGui;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>Drives <see cref="ImGuiWidgets.Tooltip"/> on its own.</summary>
/// <remarks>
/// The tooltip appears only after the pointer has rested on the item for a moment, so a test that
/// hovers and looks immediately sees the item's own hover highlight and nothing else. These tests
/// hold the hover for a second of frames before asking what is on screen.
/// </remarks>
[TestClass]
public sealed class TooltipTests : WidgetTest
{
	private const string Host = "Save";
	private const int HoverDelayFrames = 60;

	private string description = "Writes the document to disk";

	private void Draw()
	{
		ImGui.Button(Host, new Vector2(120f, 32f));
		Mark(Host);
		ImGuiWidgets.Tooltip(description);
	}

	private void HoverAndWait()
	{
		Hover(Host);
		Step(HoverDelayFrames);
	}

	[TestMethod]
	public void Tooltip_DrawsNothingWhileTheItemIsNotHovered()
	{
		Start(Draw);
		MoveAway();
		byte[] idle = Snapshot();

		Step(HoverDelayFrames);
		MoveAway();

		Assert.AreEqual(0, PixelsChangedSince(idle), "Something appeared without the item being hovered.");
	}

	[TestMethod]
	public void Tooltip_AppearsOnceTheHoverIsHeld()
	{
		Start(Draw);
		MoveAway();
		byte[] idle = Snapshot();

		Hover(Host);
		int justHovered = PixelsChangedSince(idle);

		Step(HoverDelayFrames);
		int held = PixelsChangedSince(idle);

		Assert.IsTrue(
			held > justHovered,
			$"Holding the hover drew no more than the moment it started ({held} pixels against {justHovered}), so no tooltip appeared.");
	}

	[TestMethod]
	public void Tooltip_DisappearsWhenThePointerLeaves()
	{
		Start(Draw);
		MoveAway();
		byte[] idle = Snapshot();

		HoverAndWait();
		MoveAway();
		Step(2);

		Assert.AreEqual(0, PixelsChangedSince(idle), "The tooltip stayed on screen after the pointer left.");
	}

	[TestMethod]
	public void Tooltip_ShowsTheTextItIsGiven()
	{
		description = "Short";
		Start(Draw);
		HoverAndWait();
		byte[] shortTooltip = Snapshot();

		description = "A considerably longer description that runs well past the short one";
		Step(HoverDelayFrames);

		Assert.IsTrue(PixelsChangedSince(shortTooltip) > 0, "A long tooltip drew the same as a short one.");
	}
}
