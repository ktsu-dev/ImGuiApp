// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class ToastQueueStateTests
{
	private static ToastRequest Toast(string message, float duration = 1f) => new(message, new ToastOptions { Duration = duration });

	private static void Run(ToastQueueState state, float seconds, bool holding = false)
	{
		for (float t = 0f; t < seconds; t += 0.01f)
		{
			state.Update(0.01f, holding);
		}
	}

	[TestMethod]
	public void Toasts_ShowOneAtATime_InTheOrderAskedFor()
	{
		ToastQueueState state = new();
		state.Enqueue(Toast("first"));
		state.Enqueue(Toast("second"));

		state.Update(0.01f, false);
		Assert.AreEqual("first", state.Current!.Message);
		Assert.AreEqual(2, state.Count);

		Run(state, ToastQueueStateTests.Lifetime(1f));
		state.Update(0.01f, false);

		Assert.AreEqual("second", state.Current!.Message);
		Assert.AreEqual(1, state.Count);
	}

	[TestMethod]
	public void Toast_SlidesIn_Stays_AndSlidesOut()
	{
		ToastQueueState state = new();
		state.Enqueue(Toast("hello", 1f));

		state.Update(ToastQueueState.EnterTime * 0.5f, false);
		Assert.AreEqual(ToastPhase.Entering, state.Phase);
		Assert.AreEqual(0.5f, state.Visibility, 0.01f);

		state.Update(ToastQueueState.EnterTime, false);
		Assert.AreEqual(ToastPhase.Showing, state.Phase);
		Assert.AreEqual(1f, state.Visibility);

		state.Update(1f, false);
		Assert.AreEqual(ToastPhase.Leaving, state.Phase);

		state.Update(ToastQueueState.LeaveTime, false);
		Assert.AreEqual(ToastPhase.Hidden, state.Phase);
		Assert.IsNull(state.Current);
		Assert.AreEqual(0, state.Count);
	}

	[TestMethod]
	public void Holding_PausesTheCountdown()
	{
		ToastQueueState state = new();
		state.Enqueue(Toast("hold me", 0.5f));
		state.Update(ToastQueueState.EnterTime, false);

		Run(state, 2f, holding: true);
		Assert.AreEqual(ToastPhase.Showing, state.Phase);

		Run(state, 0.6f);
		Assert.AreNotEqual(ToastPhase.Showing, state.Phase);
	}

	[TestMethod]
	public void ZeroDuration_StaysUntilDismissed()
	{
		ToastQueueState state = new();
		state.Enqueue(Toast("sticky", 0f));
		Run(state, 10f);
		Assert.AreEqual(ToastPhase.Showing, state.Phase);

		state.Dismiss();
		Assert.AreEqual(ToastPhase.Leaving, state.Phase);
		Run(state, ToastQueueState.LeaveTime + 0.05f);
		Assert.IsNull(state.Current);
	}

	[TestMethod]
	public void Dismiss_WhileEntering_LeavesFromWhereItHadGot()
	{
		ToastQueueState state = new();
		state.Enqueue(Toast("quick"));
		state.Update(ToastQueueState.EnterTime * 0.25f, false);
		float before = state.Visibility;

		state.Dismiss();

		Assert.AreEqual(ToastPhase.Leaving, state.Phase);
		Assert.AreEqual(before, state.Visibility, 1e-4f);
	}

	[TestMethod]
	public void Clear_DropsTheWaitingToasts()
	{
		ToastQueueState state = new();
		state.Enqueue(Toast("a"));
		state.Enqueue(Toast("b"));
		state.Enqueue(Toast("c"));
		state.Update(0.01f, false);

		state.Clear();
		Run(state, ToastQueueState.LeaveTime + 0.05f);

		Assert.AreEqual(0, state.Count);
		Assert.IsNull(state.Current);
	}

	private static float Lifetime(float duration) => ToastQueueState.EnterTime + duration + ToastQueueState.LeaveTime + 0.05f;
}
