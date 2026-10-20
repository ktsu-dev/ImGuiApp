// Copyright (c) 2023-2026 ktsu-dev contributors

#if IOS

namespace ktsu.ImGui.App;

using System.Numerics;

/// <summary>
/// Sends what <see cref="TouchScrollGesture"/> decides the fingers mean to <see cref="ImGuiApp"/>'s
/// iOS input bridge.
/// </summary>
internal sealed class ImGuiAppTouchSink : ITouchPointerSink
{
	/// <inheritdoc/>
	public void MovePointer(Vector2 position) => ImGuiApp.OnPointerMoved(position);

	/// <inheritdoc/>
	public void SetButton(bool down) => ImGuiApp.OnPointerButton(down);

	/// <inheritdoc/>
	public void Scroll(float horizontal, float vertical) => ImGuiApp.OnPointerScroll(horizontal, vertical);
}

#endif
