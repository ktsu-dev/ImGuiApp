// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Phosphor;

/// <summary>
/// Widgets for a phosphor terminal: glowing text, boxed readouts, vector frames, segment bars, status
/// lights, oscilloscope and sweep displays, and the scanline overlay that sits over all of them.
/// </summary>
/// <remarks>
/// Every widget draws in <see cref="PhosphorStyle.Palette"/>, so a <see cref="PhosphorStyle.Scope"/> or a
/// call to <see cref="PhosphorStyle.Apply"/> retints all of them at once.
/// </remarks>
public static partial class ImGuiPhosphor
{
}
