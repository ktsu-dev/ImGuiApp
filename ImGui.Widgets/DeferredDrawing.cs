// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System.Diagnostics;

using Hexa.NET.ImGui;

using HexaAnimationManager = Hexa.NET.ImGui.Widgets.AnimationManager;
using HexaDialogManager = Hexa.NET.ImGui.Widgets.Dialogs.DialogManager;
using HexaMessageBoxes = Hexa.NET.ImGui.Widgets.MessageBoxes;
using HexaPopupManager = Hexa.NET.ImGui.Widgets.Dialogs.PopupManager;
using HexaWidgetManager = Hexa.NET.ImGui.Widgets.WidgetManager;

public static partial class ImGuiWidgets
{
	/// <summary>
	/// Tracks which frame a pump last ran on.
	/// </summary>
	internal struct PumpTracker
	{
		/// <summary>
		/// Gets or sets the frame a pump last ran on.
		/// </summary>
		public int LastFrame { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether a pump has ever run.
		/// </summary>
		public bool HasEverPumped { get; set; }
	}

	/// <summary>
	/// The result of evaluating a pump call.
	/// </summary>
	internal enum PumpState
	{
		/// <summary>
		/// The first pump of this frame.
		/// </summary>
		Ok,

		/// <summary>
		/// A pump already ran this frame; running another draws everything twice.
		/// </summary>
		DoublePumped,
	}

	private static PumpTracker pumpTracker;

	/// <summary>
	/// Draws every dialog, message box and popup that is currently open, and advances the
	/// animation clock. Call once per frame, at the end of your render callback.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Hexa's dialogs are stateful: showing one registers it with a static manager, and it is only
	/// drawn when a pump runs. Without this call no dialog ever appears and the manager's internal
	/// collections grow for the life of the process.
	/// </para>
	/// <para>
	/// Mutually exclusive with <see cref="DrawDeferredDocked"/>, which already does everything this
	/// does. Calling both in one frame draws every dialog twice.
	/// </para>
	/// </remarks>
	public static void DrawDeferred()
	{
		ReportPumpState();

		HexaDialogManager.Draw();
		HexaMessageBoxes.Draw();
		HexaPopupManager.Draw();
		HexaAnimationManager.Tick();
	}

	/// <summary>
	/// Draws a dockspace over the main viewport, every registered <see cref="DockedWindow"/>, and
	/// everything <see cref="DrawDeferred"/> draws. Call once per frame, at the end of your render
	/// callback.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Only needed if the application uses <see cref="DockedWindow"/>. This creates a dockspace
	/// over the main viewport, which is a layout decision — prefer <see cref="DrawDeferred"/>
	/// unless you want that.
	/// </para>
	/// <para>
	/// Mutually exclusive with <see cref="DrawDeferred"/>. Calling both in one frame draws every
	/// dialog twice.
	/// </para>
	/// <para>
	/// Requires <see cref="ImGuiConfigFlags.DockingEnable"/>, which is set by
	/// <c>ImGuiAppConfig.EnableDocking</c>. Hexa's <c>WidgetManager.Draw()</c> calls
	/// <c>DockSpaceOverViewport</c> without checking the flag, so without it the dockspace is a
	/// no-op. The flag cannot be set from here: ImGui only accepts it before the first frame, and
	/// changing it mid-frame aborts the process on the next one. Docking stays opt-in through the
	/// config rather than being enabled globally, so applications that never dock are unaffected.
	/// </para>
	/// </remarks>
	public static void DrawDeferredDocked()
	{
		ReportPumpState();
		RequireDocking();

		// WidgetManager.Draw internally calls DialogManager.Draw, MessageBoxes.Draw,
		// PopupManager.Draw and AnimationManager.Tick, so this must not also call them.
		HexaWidgetManager.Draw();
	}

	/// <summary>
	/// Verifies that ImGui's docking config flag is set.
	/// </summary>
	/// <remarks>
	/// This used to set the flag itself, which cannot work: ImGui requires it before the first
	/// NewFrame, and this only ever runs inside a frame. Setting it here left the flag disagreeing
	/// with the previous frame's, and ImGui aborted the process on the very next frame. Reporting
	/// the misconfiguration is the most this can do from where it runs.
	/// </remarks>
	/// <exception cref="InvalidOperationException">Docking is not enabled.</exception>
	private static void RequireDocking()
	{
		ImGuiIOPtr io = ImGui.GetIO();
		if ((io.ConfigFlags & ImGuiConfigFlags.DockingEnable) == 0)
		{
			throw new InvalidOperationException(
				"DrawDeferredDocked() requires docking, which is off. Set ImGuiAppConfig.EnableDocking = true. " +
				"ImGui only accepts the docking flag before the first frame, so it cannot be turned on from here.");
		}
	}

	/// <summary>
	/// Records that a dialog was shown, so a missing pump can be reported at the point it matters.
	/// </summary>
	/// <exception cref="InvalidOperationException">No pump has ever run.</exception>
	internal static void NotifyDialogShown()
	{
		if (!pumpTracker.HasEverPumped)
		{
			throw new InvalidOperationException(
				"A dialog was shown but neither ImGuiWidgets.DrawDeferred() nor " +
				"ImGuiWidgets.DrawDeferredDocked() has ever run. Hexa's dialogs are only drawn by " +
				"a per-frame pump; call ImGuiWidgets.DrawDeferred() at the end of your render " +
				"callback. Without it the dialog never appears and the manager's internal " +
				"collections grow for the life of the process.");
		}
	}

	/// <summary>
	/// Updates the pump tracker and reports whether this call is the first of the frame.
	/// </summary>
	/// <param name="currentFrame">The current ImGui frame number.</param>
	/// <param name="tracker">The tracker to update.</param>
	/// <returns>The state of this pump call.</returns>
	/// <remarks>
	/// Any change in frame number counts, not just an increase, so a reset frame counter (a
	/// recreated ImGui context) does not stall the pump.
	/// </remarks>
	internal static PumpState EvaluatePump(int currentFrame, ref PumpTracker tracker)
	{
		if (tracker.HasEverPumped && tracker.LastFrame == currentFrame)
		{
			return PumpState.DoublePumped;
		}

		tracker.LastFrame = currentFrame;
		tracker.HasEverPumped = true;
		return PumpState.Ok;
	}

	/// <summary>
	/// Evaluates the pump for this frame and traces a warning if it was already pumped.
	/// </summary>
	private static void ReportPumpState()
	{
		if (EvaluatePump(ImGui.GetFrameCount(), ref pumpTracker) == PumpState.DoublePumped)
		{
			Trace.TraceWarning(
				"ImGuiWidgets: a deferred-drawing pump already ran this frame. DrawDeferred() and " +
				"DrawDeferredDocked() are mutually exclusive - DrawDeferredDocked() already draws " +
				"everything DrawDeferred() does. Every dialog is being drawn twice this frame.");
		}
	}

	/// <summary>
	/// Tracks the fallback animation tick separately from <see cref="pumpTracker"/>, so the two
	/// never interfere with each other.
	/// </summary>
	private static PumpTracker fallbackAnimationTracker;

	/// <summary>
	/// Advances Hexa's animation clock if no per-frame pump has ever run, so animated Hexa-backed
	/// widgets (for example <see cref="ToggleSwitch"/>) stay correct even when the application
	/// never calls <see cref="DrawDeferred"/> or <see cref="DrawDeferredDocked"/>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Once a pump has run, this does nothing: the pump owns the clock from then on, and <see
	/// cref="DrawDeferredDocked"/> ticks it from inside Hexa's <c>WidgetManager.Draw()</c>, where
	/// this method's once-per-frame guard cannot suppress it. Ticking here too in that case would
	/// advance animations at double speed - the exact failure mode the deleted
	/// <c>HexaAnimationPump</c> stopgap's own doc comment warned about.
	/// </para>
	/// <para>
	/// Before the first pump call, this ticks the clock at most once per frame using its own
	/// tracker, so any number of animated widgets calling it in the same frame still advance the
	/// clock exactly once.
	/// </para>
	/// </remarks>
	internal static void TickAnimationClockIfUnpumped()
	{
		if (EvaluateFallbackTick(ImGui.GetFrameCount(), pumpTracker.HasEverPumped, ref fallbackAnimationTracker))
		{
			HexaAnimationManager.Tick();
		}
	}

	/// <summary>
	/// Decides whether the animation-clock fallback should tick this frame.
	/// </summary>
	/// <param name="currentFrame">The current ImGui frame number.</param>
	/// <param name="pumpHasEverRun">
	/// Whether <see cref="DrawDeferred"/> or <see cref="DrawDeferredDocked"/> has ever run. When
	/// <see langword="true"/>, the fallback never ticks - the pump owns the clock.
	/// </param>
	/// <param name="tracker">
	/// The fallback's own tracker, separate from the pump's <see cref="PumpTracker"/>, so the two
	/// never interfere.
	/// </param>
	/// <returns><see langword="true"/> if the caller should advance the clock.</returns>
	internal static bool EvaluateFallbackTick(int currentFrame, bool pumpHasEverRun, ref PumpTracker tracker)
	{
		if (pumpHasEverRun)
		{
			return false;
		}

		if (tracker.HasEverPumped && tracker.LastFrame == currentFrame)
		{
			return false;
		}

		tracker.LastFrame = currentFrame;
		tracker.HasEverPumped = true;
		return true;
	}
}
