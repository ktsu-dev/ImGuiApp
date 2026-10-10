// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.ImGuiController;

using System;
using System.Numerics;

/// <summary>
/// Carries the frame's input method request to the window, so a candidate list opens at the
/// caret.
/// </summary>
/// <remarks>
/// Dear ImGui records the request in its context and offers a <c>PlatformSetImeDataFn</c> callback
/// for a backend to act on. Setting that callback means handing ImGui a native function pointer,
/// so this reads the recorded request once the frame has ended instead, which is the same data at
/// the same moment, and writes to the platform only when it changed.
/// </remarks>
/// <param name="target">Where a placement goes; null records it and goes no further.</param>
internal sealed class PlatformIme(IImeTarget? target)
{
	/// <summary>Gets the placement last carried to the platform, in window client pixels.</summary>
	internal ImePlacement? Applied { get; private set; }

	/// <summary>
	/// Creates the input method bridge for the platform this process runs on.
	/// </summary>
	/// <returns>A bridge to the Win32 input method manager on Windows, otherwise one that records.</returns>
	internal static PlatformIme ForCurrentPlatform() =>
		new(OperatingSystem.IsWindows() ? new Win32ImeTarget() : null);

	/// <summary>
	/// Converts a request in ImGui's coordinates to window client pixels.
	/// </summary>
	/// <param name="requested">The frame's request, in framebuffer pixels.</param>
	/// <param name="viewportPosition">The main viewport's position, which ImGui's coordinates are offset by.</param>
	/// <param name="windowToFramebufferScale">Framebuffer pixels per window unit.</param>
	/// <returns>The placement relative to the window's client area.</returns>
	internal static ImePlacement ToClient(ImePlacement requested, Vector2 viewportPosition, Vector2 windowToFramebufferScale)
	{
		Vector2 scale = new(
			windowToFramebufferScale.X > 0 ? windowToFramebufferScale.X : 1,
			windowToFramebufferScale.Y > 0 ? windowToFramebufferScale.Y : 1);
		return new((requested.Position - viewportPosition) / scale, requested.LineHeight / scale.Y);
	}

	/// <summary>
	/// Applies the frame's request, writing to the platform only when it changed.
	/// </summary>
	/// <param name="windowHandle">The native window handle, or zero when there is none.</param>
	/// <param name="requested">The frame's request, or null when nothing is being typed into.</param>
	/// <param name="viewportPosition">The main viewport's position.</param>
	/// <param name="windowToFramebufferScale">Framebuffer pixels per window unit.</param>
	internal void Apply(nint windowHandle, ImePlacement? requested, Vector2 viewportPosition, Vector2 windowToFramebufferScale)
	{
		// The input method keeps its last position while nothing is typed into, which is what
		// ImGui's own backends do too: there is nothing to move it to.
		if (requested is not { } placement)
		{
			Applied = null;
			return;
		}

		ImePlacement client = ToClient(placement, viewportPosition, windowToFramebufferScale);
		if (Applied == client)
		{
			return;
		}

		Applied = client;
		if (windowHandle != 0)
		{
			target?.Place(windowHandle, client);
		}
	}
}
