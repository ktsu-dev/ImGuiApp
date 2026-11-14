// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Phosphor;

/// <summary>A blip on a <see cref="ImGuiPhosphor.RadarSweep"/>.</summary>
/// <param name="Bearing">Bearing in radians, clockwise from straight up.</param>
/// <param name="Range">Distance from the centre, from 0 at the centre to 1 at the outer ring.</param>
public readonly record struct RadarContact(float Bearing, float Range);
