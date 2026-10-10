// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App;

using System.Numerics;

/// <summary>
/// Where the operating system's input method should put its composition and candidate windows:
/// at a text caret, below the line it sits on.
/// </summary>
/// <param name="Position">The caret's top-left corner, in ImGui's screen coordinates.</param>
/// <param name="LineHeight">The height of the line the caret is on, in the same units.</param>
public readonly record struct ImePlacement(Vector2 Position, float LineHeight);
