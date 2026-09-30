// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using ktsu.Semantics.Color;

/// <summary>One colour stop: a position in 0..1 along the gradient and its colour (linear, with alpha).</summary>
/// <param name="Position">The stop's position along the gradient, from 0 at the left to 1 at the right.</param>
/// <param name="Color">The stop's colour, in linear RGB with straight alpha.</param>
public readonly record struct GradientStop(float Position, Color Color);
