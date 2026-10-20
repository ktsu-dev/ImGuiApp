// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class LongPressMenuStateTests
{
	private const float Frame = 1f / 60f;
	private const float LongPress = 0.5f;

	[TestMethod]
	public void Holding_ShrinksSteadily_ToThePressedScale()
	{
		LongPressMenuState state = new();

		state.Update(Frame, pressed: true, pressDuration: LongPress * 0.5f, LongPress, opened: false);
		Assert.AreEqual(1f - ((1f - LongPressMenuState.PressedScale) * 0.5f), state.Scale, 1e-5f);

		state.Update(Frame, pressed: true, pressDuration: LongPress * 2f, LongPress, opened: false);
		Assert.AreEqual(LongPressMenuState.PressedScale, state.Scale, 1e-5f);
	}

	[TestMethod]
	public void Opening_BumpsPastFullSize_ThenSettles()
	{
		LongPressMenuState state = new();
		state.Update(Frame, pressed: true, pressDuration: LongPress, LongPress, opened: false);

		state.Update(Frame, pressed: true, pressDuration: LongPress, LongPress, opened: true);
		float largest = state.Scale;
		for (int i = 0; i < 120; i++)
		{
			state.Update(Frame, pressed: true, pressDuration: LongPress + (i * Frame), LongPress, opened: false);
			largest = System.MathF.Max(largest, state.Scale);
		}

		Assert.IsTrue(largest > 1f, "Opening the menu did not bump the target past its size.");
		Assert.AreEqual(1f, state.Scale, 1e-3f, "Holding on after the menu opened shrank the target again.");
	}

	[TestMethod]
	public void LettingGoTooSoon_SpringsBackWithoutABump()
	{
		LongPressMenuState state = new();
		state.Update(Frame, pressed: true, pressDuration: LongPress * 0.5f, LongPress, opened: false);

		float largest = 0f;
		for (int i = 0; i < 120; i++)
		{
			state.Update(Frame, pressed: false, pressDuration: 0f, LongPress, opened: false);
			largest = System.MathF.Max(largest, state.Scale);
		}

		Assert.IsTrue(largest <= 1f + 1e-4f, "A released press overshot.");
		Assert.AreEqual(1f, state.Scale, 1e-3f);
	}

	[TestMethod]
	public void ANewPress_AfterTheMenu_ShrinksAgain()
	{
		LongPressMenuState state = new();
		state.Update(Frame, pressed: true, pressDuration: LongPress, LongPress, opened: true);
		state.Update(Frame, pressed: false, pressDuration: 0f, LongPress, opened: false);

		state.Update(Frame, pressed: true, pressDuration: LongPress, LongPress, opened: false);

		Assert.AreEqual(LongPressMenuState.PressedScale, state.Scale, 1e-5f);
	}
}
