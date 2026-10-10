// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.ImGuiController;

/// <summary>
/// A platform input method that can be told where the caret is.
/// </summary>
internal interface IImeTarget
{
	/// <summary>
	/// Moves the window's composition and candidate windows to a caret.
	/// </summary>
	/// <param name="windowHandle">The native window handle.</param>
	/// <param name="placement">The caret, in window client pixels.</param>
	public void Place(nint windowHandle, ImePlacement placement);
}
