// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

/// <summary>Which modifiers a chord holds, as <see cref="KeyChordMatcher"/> reads and builds them.</summary>
/// <param name="Ctrl">Whether Ctrl is held.</param>
/// <param name="Alt">Whether Alt is held.</param>
/// <param name="Shift">Whether Shift is held.</param>
/// <param name="Meta">Whether the Super, Windows or Command key is held.</param>
public readonly record struct KeyChordModifiers(bool Ctrl, bool Alt, bool Shift, bool Meta);
