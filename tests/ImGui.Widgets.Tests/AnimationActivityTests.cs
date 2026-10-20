// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using ktsu.ImGui.Probes;
using ktsu.ImGui.Widgets.Animation;
using ktsu.ImGui.Widgets.Scroll;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// The animation primitives ask the host for active frames while they move, and stop asking once
/// they settle, so no widget has to remember to (#485).
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class AnimationActivityTests
{
	[TestInitialize]
	public void Setup() => _ = AnimationActivity.TakeRequest();

	[TestCleanup]
	public void Cleanup() => _ = AnimationActivity.TakeRequest();

	[TestMethod]
	public void TakeRequest_ClearsTheRequest()
	{
		AnimationActivity.RequestActiveFrame();

		Assert.IsTrue(AnimationActivity.IsRequested);
		Assert.IsTrue(AnimationActivity.TakeRequest());
		Assert.IsFalse(AnimationActivity.IsRequested);
		Assert.IsFalse(AnimationActivity.TakeRequest());
	}

	[TestMethod]
	public void Tween_RequestsWhileRunning_AndNotOnceComplete()
	{
		Tween tween = new(0.0f, 1.0f, 1.0f);

		_ = tween.Update(0.5f);
		Assert.IsTrue(AnimationActivity.TakeRequest());

		_ = tween.Update(1.0f);
		Assert.IsFalse(tween.IsActive);
		Assert.IsFalse(AnimationActivity.TakeRequest());

		_ = tween.Update(0.1f);
		Assert.IsFalse(AnimationActivity.TakeRequest());
	}

	[TestMethod]
	public void RepeatingTween_KeepsRequesting()
	{
		Tween tween = new(0.0f, 1.0f, 0.2f) { Repeat = true };

		for (int i = 0; i < 10; i++)
		{
			_ = tween.Update(0.15f);
			Assert.IsTrue(AnimationActivity.TakeRequest(), $"Update {i}");
		}
	}

	[TestMethod]
	public void Spring_RequestsWhileSettling_AndNotAtRest()
	{
		Spring spring = new(0.0f);
		spring.SetTarget(100.0f);

		_ = spring.Update(1.0f / 60.0f);
		Assert.IsTrue(AnimationActivity.TakeRequest());

		_ = spring.Update(5.0f);
		Assert.IsFalse(spring.IsActive);
		Assert.IsFalse(AnimationActivity.TakeRequest());
	}

	[TestMethod]
	public void InertialScroll_RequestsWhileCoasting_AndNotAtRest()
	{
		InertialScroll scroll = new();
		scroll.Fling(1000.0f);

		_ = scroll.Update(1.0f / 60.0f);
		Assert.IsTrue(AnimationActivity.TakeRequest());

		_ = scroll.Update(10.0f);
		Assert.IsFalse(scroll.IsActive);
		Assert.IsFalse(AnimationActivity.TakeRequest());
	}
}
