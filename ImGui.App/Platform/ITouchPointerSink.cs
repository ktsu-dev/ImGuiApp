// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App;

using System.Numerics;

/// <summary>
/// Where <see cref="TouchScrollGesture"/> sends what the fingers mean, as ImGui mouse input.
/// </summary>
internal interface ITouchPointerSink
{
	/// <summary>Moves the ImGui mouse.</summary>
	/// <param name="position">The new position, or <see cref="TouchScrollGesture.Offscreen"/> for none.</param>
	public void MovePointer(Vector2 position);

	/// <summary>Presses or releases the left ImGui mouse button.</summary>
	/// <param name="down">Whether the button is down.</param>
	public void SetButton(bool down);

	/// <summary>Scrolls the wheel.</summary>
	/// <param name="horizontal">Wheel steps across. Positive scrolls left, as ImGui reads it.</param>
	/// <param name="vertical">Wheel steps down the page. Positive scrolls up, as ImGui reads it.</param>
	public void Scroll(float horizontal, float vertical);
}
