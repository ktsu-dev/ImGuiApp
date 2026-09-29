// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using System;
using System.Numerics;

using ktsu.ImGui.App.Testing;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Envelope = ImGuiWidgets.Envelope;

/// <summary>Drives <see cref="ImGuiWidgets.EnvelopeEditor"/> on its own.</summary>
/// <remarks>
/// 5 seconds across 500 pixels, so a pixel is a hundredth of a second, and 200 pixels of height, so
/// 40 pixels is a fifth of full level.
/// </remarks>
[TestClass]
public sealed class EnvelopeEditorTests : WidgetTest
{
	private const string Label = "Env";
	private const float Width = 500f;
	private const float Height = 200f;
	private const float TimeSpan = 5f;

	private static readonly Envelope Fixture = new(0.2f, 0.3f, 0.1f, 0.4f, 0.6f, 0.5f);

	private Envelope env = Fixture;
	private bool changed;
	private bool showDelayAndHold = true;

	private void Draw() =>
		changed |= ImGuiWidgets.EnvelopeEditor(Label, ref env, new Vector2(Width, Height), TimeSpan, showDelayAndHold);

	private void DragBy(string name, float dx, float dy)
	{
		Vector2 from = CenterOf(name);
		Harness.Mouse.Drag(from.X, from.Y, from.X + dx, from.Y + dy);
		Step();
	}

	[TestMethod]
	public void EnvelopeEditor_IsDrawnAndMarksItself()
	{
		Start(Draw);

		Assert.IsTrue(IsVisible(Label), "The editor marked no probe item.");
		AssertSomethingWasDrawn("the envelope editor");
		Assert.IsFalse(changed, "Drawing a valid envelope reported a change.");
	}

	[TestMethod]
	public void EnvelopeEditor_ReservesTheSizeItIsGiven()
	{
		Start(Draw);

		Rectangle rect = RectOf(Label);

		Assert.IsTrue(Math.Abs(rect.Width - Width) <= 1, $"The editor claimed {rect.Width}px of width rather than {Width}.");
		Assert.IsTrue(Math.Abs(rect.Height - Height) <= 1, $"The editor claimed {rect.Height}px of height rather than {Height}.");
	}

	[TestMethod]
	public void EnvelopeEditor_MarksEveryVisibleHandle()
	{
		Start(Draw);

		foreach (string name in (string[])["delay", "attack", "hold", "decay", "sustain", "release", "attackTension", "decayTension", "releaseTension"])
		{
			Assert.IsTrue(IsVisible($"{Label}/{name}"), $"The {name} handle was not marked.");
		}
	}

	[TestMethod]
	public void EnvelopeEditor_DraggingTheAttackHandleRightLengthensTheAttack()
	{
		Start(Draw);

		DragBy($"{Label}/attack", 30f, 0f);

		Assert.AreEqual(0.6f, env.Attack, 0.02f);
		Assert.IsTrue(changed);
		Assert.AreEqual(Fixture.Delay, env.Delay, "Dragging the attack moved the delay.");
		Assert.AreEqual(Fixture.Decay, env.Decay, "Dragging the attack moved the decay.");
	}

	[TestMethod]
	public void EnvelopeEditor_DraggingTheSustainHandleDownLowersSustain()
	{
		Start(Draw);

		DragBy($"{Label}/sustain", 0f, 40f);

		Assert.AreEqual(0.4f, env.Sustain, 0.02f);
		Assert.AreEqual(Fixture.Decay, env.Decay);
	}

	[TestMethod]
	public void EnvelopeEditor_DraggingTheDecayHandleMovesDecayAndSustain()
	{
		Start(Draw);

		DragBy($"{Label}/decay", 20f, 40f);

		Assert.AreEqual(0.6f, env.Decay, 0.02f);
		Assert.AreEqual(0.4f, env.Sustain, 0.02f);
	}

	[TestMethod]
	public void EnvelopeEditor_DraggingATensionHandleUpBendsTheAttack()
	{
		Start(Draw);

		DragBy($"{Label}/attackTension", 0f, -50f);

		Assert.AreEqual(0.5f, env.AttackTension, 0.05f);
	}

	[TestMethod]
	public void EnvelopeEditor_RightClickResetsATension()
	{
		env = env with { DecayTension = 0.8f };
		Start(Draw);

		Vector2 handle = CenterOf($"{Label}/decayTension");
		Harness.Mouse.Click(handle.X, handle.Y, button: 1);
		Step();

		Assert.AreEqual(0f, env.DecayTension);
		Assert.IsTrue(changed);
	}

	[TestMethod]
	public void EnvelopeEditor_RightClickAwayFromATensionDoesNothing()
	{
		env = env with { DecayTension = 0.8f };
		Start(Draw);

		Vector2 sustain = CenterOf($"{Label}/sustain");
		Harness.Mouse.Click(sustain.X, sustain.Y, button: 1);
		Step();

		Assert.AreEqual(0.8f, env.DecayTension);
		Assert.IsFalse(changed);
	}

	[TestMethod]
	public void EnvelopeEditor_PressingWithoutMovingChangesNothing()
	{
		Start(Draw);

		Click($"{Label}/release");

		Assert.AreEqual(Fixture, env);
		Assert.IsFalse(changed);
	}

	[TestMethod]
	public void EnvelopeEditor_DraggingTheReleaseHandleLeftShortensTheRelease()
	{
		Start(Draw);

		DragBy($"{Label}/release", -20f, 0f);

		Assert.AreEqual(0.3f, env.Release, 0.02f);
		Assert.IsTrue(changed);
	}

	[TestMethod]
	public void EnvelopeEditor_PressingAHandleWithoutMovingChangesNothing()
	{
		Start(Draw);

		Click($"{Label}/decay");

		Assert.AreEqual(Fixture, env);
		Assert.IsFalse(changed);
	}

	[TestMethod]
	public void EnvelopeEditor_AdsrModeHidesDelayAndHold()
	{
		showDelayAndHold = false;
		Start(Draw);

		Assert.IsFalse(IsVisible($"{Label}/delay"));
		Assert.IsFalse(IsVisible($"{Label}/hold"));
		Assert.IsTrue(IsVisible($"{Label}/attack"));
	}

	[TestMethod]
	public void EnvelopeEditor_DrawsTheEvaluatorsCurve()
	{
		Start(Draw);
		byte[] before = Snapshot();

		env = env with { Sustain = 0.1f };
		Step();

		Rectangle difference = BoundsOfDifference(before) ?? throw new AssertFailedException("Changing the sustain level changed nothing on screen.");
		Rectangle rect = RectOf(Label);

		Assert.IsTrue(
			difference.MinX >= rect.MinX && difference.MinY >= rect.MinY
			&& difference.MaxX <= rect.MaxX && difference.MaxY <= rect.MaxY,
			$"The change {difference} spilled outside the editor {rect}.");
	}

	[TestMethod]
	public void EnvelopeEditor_InvalidValuesAreRepairedAndReported()
	{
		env = env with { Attack = float.NaN, Sustain = 3f };
		Start(Draw);

		Assert.IsTrue(changed);
		Assert.AreEqual(0f, env.Attack);
		Assert.AreEqual(1f, env.Sustain);
	}

	[TestMethod]
	public void EnvelopeEditor_BadTimeSpanDrawsOnlyTheFrame()
	{
		Envelope invalid = env with { Attack = float.NaN };
		bool reported = false;
		Start(() => reported |= ImGuiWidgets.EnvelopeEditor(Label, ref invalid, new Vector2(Width, Height), 0f));

		Assert.IsTrue(IsVisible(Label));
		Assert.IsFalse(reported);
		Assert.IsTrue(float.IsNaN(invalid.Attack), "A bad time span still normalised the envelope.");
		Assert.IsFalse(IsVisible($"{Label}/attack"), "A bad time span still placed handles.");
	}

	[TestMethod]
	public void EnvelopeEditor_TooSmallReservesAPixel()
	{
		bool reported = false;
		Start(() => reported |= ImGuiWidgets.EnvelopeEditor(Label, ref env, new Vector2(0.5f, 0.5f), TimeSpan));

		Assert.IsTrue(IsVisible(Label));
		Assert.IsFalse(reported);
	}
}
