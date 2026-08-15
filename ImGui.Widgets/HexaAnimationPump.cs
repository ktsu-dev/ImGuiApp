// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using Hexa.NET.ImGui;

using HexaAnimationManager = Hexa.NET.ImGui.Widgets.AnimationManager;

/// <summary>
/// Advances Hexa's animation clock once per frame.
/// </summary>
/// <remarks>
/// <para>
/// Hexa's animated widgets register state with <c>AnimationManager</c> and read it back through
/// <c>GetAnimationValue</c>, but the clock only advances when something calls
/// <c>AnimationManager.Tick()</c>. Upstream that happens inside <c>WidgetManager.Draw()</c>, a
/// per-frame pump this library does not run.
/// </para>
/// <para>
/// Without the tick the registered state is never removed and its elapsed time stays at zero, so
/// <c>GetAnimationValue</c> keeps returning the eased value for t=0 rather than the -1 sentinel that
/// means "no animation". <see cref="ImGuiWidgets.ToggleSwitch"/> reads that as a completed animation
/// in the opposite direction and renders inverted from the first click onward.
/// </para>
/// <para>
/// This is a stopgap, not the eventual lifecycle contract. When a real per-frame pump exists,
/// delete this type and its call sites: <c>WidgetManager.Draw()</c> ticks the animation clock
/// itself, so leaving both in place would advance animations at double speed.
/// </para>
/// </remarks>
internal static class HexaAnimationPump
{
	/// <summary>
	/// The ImGui frame the clock was last advanced on. -1 means never.
	/// </summary>
	private static int lastTickedFrame = -1;

	/// <summary>
	/// Advances Hexa's animation clock if it has not already been advanced this frame.
	/// </summary>
	/// <remarks>
	/// Call before delegating to an animated Hexa widget. Ticking is idempotent within a frame, so
	/// any number of animated widgets may call this and the clock still advances exactly once.
	/// </remarks>
	internal static void TickOncePerFrame()
	{
		if (ShouldTick(ImGui.GetFrameCount(), ref lastTickedFrame))
		{
			HexaAnimationManager.Tick();
		}
	}

	/// <summary>
	/// Decides whether the animation clock should advance, updating the recorded frame when it should.
	/// </summary>
	/// <param name="currentFrame">The current ImGui frame number.</param>
	/// <param name="lastFrame">The frame the clock was last advanced on; updated when this returns <see langword="true"/>.</param>
	/// <returns><see langword="true"/> if the caller should advance the clock.</returns>
	/// <remarks>
	/// Any change in frame number counts, not just an increase, so the clock keeps advancing if the
	/// frame counter is ever reset — for example when the ImGui context is recreated.
	/// </remarks>
	internal static bool ShouldTick(int currentFrame, ref int lastFrame)
	{
		if (currentFrame == lastFrame)
		{
			return false;
		}

		lastFrame = currentFrame;
		return true;
	}
}
