// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using System;
using System.Linq;
using System.Numerics;

using Hexa.NET.ImGui;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Drives the scoped helpers <see cref="ScopedDisable"/> and <see cref="ImGuiWidgets.ScopedId"/> on
/// their own.
/// </summary>
[TestClass]
public sealed class ScopeUtilityTests : WidgetTest
{
	private bool clicked;

	[TestMethod]
	public void ScopedDisable_StopsTheButtonInsideItFromBeingClicked()
	{
		bool disabled = true;

		Start(() =>
		{
			using (new ScopedDisable(disabled))
			{
				clicked |= ImGui.Button("Apply", new Vector2(120f, 32f));
				Mark("Apply");
			}
		});

		Click("Apply");

		Assert.IsFalse(clicked, "A disabled button reported a click.");
	}

	[TestMethod]
	public void ScopedDisable_WhenNotDisabling_LeavesTheButtonWorking()
	{
		Start(() =>
		{
			using (new ScopedDisable(false))
			{
				clicked |= ImGui.Button("Apply", new Vector2(120f, 32f));
				Mark("Apply");
			}
		});

		Click("Apply");

		Assert.IsTrue(clicked, "A button inside a no-op disable scope did not respond to a click.");
	}

	[TestMethod]
	public void ScopedDisable_DrawsTheContentGreyed()
	{
		bool disabled = false;

		Start(() =>
		{
			using (new ScopedDisable(disabled))
			{
				ImGui.Button("Apply", new Vector2(120f, 32f));
				Mark("Apply");
			}
		});

		MoveAway();
		byte[] enabled = Snapshot();

		disabled = true;
		Step(2);
		MoveAway();

		Assert.IsTrue(PixelsChangedSince(enabled) > 0, "A disabled button drew the same as an enabled one.");
	}

	[TestMethod]
	public void ScopedDisable_RestoresTheEnabledStateAfterTheBlock()
	{
		Start(() =>
		{
			using (new ScopedDisable(true))
			{
				ImGui.Button("Inside", new Vector2(100f, 30f));
			}

			clicked |= ImGui.Button("Outside", new Vector2(100f, 30f));
			Mark("Outside");
		});

		Click("Outside");

		Assert.IsTrue(clicked, "A button after a disable scope was still disabled.");
	}

	[TestMethod]
	public void ScopedId_QualifiesProbeNamesWithItsScope()
	{
		Start(() =>
		{
			using (new ImGuiWidgets.ScopedId("left"))
			{
				ImGui.Button("Delete", new Vector2(100f, 30f));
				Mark("Delete");
			}

			using (new ImGuiWidgets.ScopedId("right"))
			{
				ImGui.Button("Delete", new Vector2(100f, 30f));
				Mark("Delete");
			}
		});

		Assert.IsTrue(Harness.Probe.KnownNames.Any(name => name.EndsWith("left/Delete", StringComparison.Ordinal)), "The left scope's button was not qualified by its scope.");
		Assert.IsTrue(Harness.Probe.KnownNames.Any(name => name.EndsWith("right/Delete", StringComparison.Ordinal)), "The right scope's button was not qualified by its scope.");
		Assert.IsTrue(Harness.Probe.IsAmbiguous("Delete"), "Two identically labeled buttons did not read as ambiguous by their bare name.");
	}

	[TestMethod]
	public void ScopedId_KeepsTwoIdenticallyLabeledButtonsApart()
	{
		bool leftClicked = false;
		bool rightClicked = false;

		Start(() =>
		{
			using (new ImGuiWidgets.ScopedId("left"))
			{
				leftClicked |= ImGui.Button("Delete", new Vector2(100f, 30f));
				Mark("Delete");
			}

			using (new ImGuiWidgets.ScopedId("right"))
			{
				rightClicked |= ImGui.Button("Delete", new Vector2(100f, 30f));
				Mark("Delete");
			}
		});

		Click("right/Delete");

		Assert.IsTrue(rightClicked, "Clicking the right-hand button did not activate it.");
		Assert.IsFalse(leftClicked, "Clicking the right-hand button activated the left-hand one too.");
	}

	[TestMethod]
	public void ScopedId_PopsItsScopeOnDispose()
	{
		Start(() =>
		{
			using (new ImGuiWidgets.ScopedId("inside"))
			{
				ImGui.Button("Nested", new Vector2(100f, 30f));
				Mark("Nested");
			}

			ImGui.Button("After", new Vector2(100f, 30f));
			Mark("After");
		});

		Assert.IsTrue(
			Harness.Probe.KnownNames.Any(name => name.EndsWith("##mainWindow/After", StringComparison.Ordinal)),
			$"The scope leaked past its block. Recorded: {string.Join(", ", Harness.Probe.KnownNames)}.");
	}
}
