// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Probes;

using System.Threading;

/// <summary>
/// Lets a widget that is animating tell the application host to keep drawing at its active frame
/// rate, so the animation does not drop to the idle rate halfway through.
/// </summary>
/// <remarks>
/// <para>
/// This lives beside <see cref="ImGuiProbes"/> for the same reason that does: it is a cross-cutting
/// seam between packages that should not depend on each other. <c>ktsu.ImGui.Widgets</c> animates and
/// <c>ktsu.ImGui.App</c> throttles, and neither references the other, but both reference this package.
/// </para>
/// <para>
/// A widget calls <see cref="RequestActiveFrame"/> on every frame it is still moving. The host calls
/// <see cref="TakeRequest"/> once per frame when it chooses its frame rate, and treats a request like
/// recent input: the application is not idle while something on screen is animating. Once a frame
/// passes with no request, the host goes back to its usual rules, so a widget never has to release
/// anything. The animation primitives (<c>Tween</c>, <c>Spring</c> and <c>InertialScroll</c>) request
/// automatically while they are active.
/// </para>
/// </remarks>
public static class AnimationActivity
{
	private static int requested;

	/// <summary>Gets a value indicating whether a frame has been requested since the host last took the request.</summary>
	public static bool IsRequested => Volatile.Read(ref requested) != 0;

	/// <summary>
	/// Asks the host to draw the next frame at its active rate. Call it on every frame an animation is
	/// still running. It costs one write, so calling it from several widgets in one frame is fine.
	/// </summary>
	public static void RequestActiveFrame() => Volatile.Write(ref requested, 1);

	/// <summary>
	/// Reports whether a frame was requested since the last call, and clears the request. Called by the
	/// application host when it picks the frame rate.
	/// </summary>
	/// <returns>True when something asked for an active frame since the previous call.</returns>
	public static bool TakeRequest() => Interlocked.Exchange(ref requested, 0) != 0;
}
