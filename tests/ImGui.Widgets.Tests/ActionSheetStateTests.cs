// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class ActionSheetStateTests
{
	[TestMethod]
	public void Answer_TakesAButton_AndClosesTheSheet()
	{
		ActionSheetState state = new();
		state.Open(3);

		Assert.IsTrue(state.TryAnswer(2));
		Assert.IsFalse(state.IsOpen);
	}

	[TestMethod]
	public void Answer_IsTakenOnce()
	{
		ActionSheetState state = new();
		state.Open(3);
		state.TryAnswer(0);

		Assert.IsFalse(state.TryAnswer(1));
		Assert.IsFalse(state.TryAnswer(ActionSheetState.Cancelled));
	}

	[TestMethod]
	public void Answer_RejectsAnIndexThatIsNotAButton()
	{
		ActionSheetState state = new();
		state.Open(2);

		Assert.IsFalse(state.TryAnswer(2));
		Assert.IsFalse(state.TryAnswer(-2));
		Assert.IsTrue(state.IsOpen);
	}

	[TestMethod]
	public void Cancel_IsAnAnswer()
	{
		ActionSheetState state = new();
		state.Open(0);

		Assert.IsTrue(state.TryAnswer(ActionSheetState.Cancelled));
	}

	[TestMethod]
	public void Update_ReportsWhenTheSheetHasSlidAway()
	{
		ActionSheetState state = new();
		state.Open(1);
		state.Update(1f);
		state.TryAnswer(0);

		Assert.IsTrue(state.Update(1f));
		Assert.IsFalse(state.Transition.IsVisible);
	}
}
