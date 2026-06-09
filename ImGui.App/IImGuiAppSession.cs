// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.App;

/// <summary>
/// A handle to a non-blocking <see cref="ImGuiApp"/> session started with
/// <see cref="ImGuiApp.StartEmbedded(ImGuiAppConfig)"/>.
/// </summary>
/// <remarks>
/// Unlike <see cref="ImGuiApp.Start(ImGuiAppConfig)"/>, which runs a blocking render loop on the calling
/// thread, an embedded session runs its loop on a dedicated UI thread that it owns. The host drives the
/// session through this interface — forwarding resize and focus changes and disposing it to tear the
/// window down — while requests are marshalled onto the UI thread. Disposing the session stops the loop,
/// detaches the window from its host, and releases the render resources.
/// </remarks>
public interface IImGuiAppSession : IDisposable
{
	/// <summary>
	/// Forwards a new size from the host so the embedded window and its viewport can follow.
	/// </summary>
	/// <param name="width">The new width, in pixels.</param>
	/// <param name="height">The new height, in pixels.</param>
	void Resize(int width, int height);

	/// <summary>
	/// Forwards a focus change from the host.
	/// </summary>
	/// <param name="focused"><see langword="true"/> if the host gave the editor focus; otherwise <see langword="false"/>.</param>
	void Focus(bool focused);

	/// <summary>
	/// Gets the native handle of the session's own window (the child reparented under the host), or
	/// <c>0</c> if it has not been created yet.
	/// </summary>
	nint NativeHandle { get; }

	/// <summary>
	/// Gets a value indicating whether the session's render loop is currently running.
	/// </summary>
	bool IsRunning { get; }
}
