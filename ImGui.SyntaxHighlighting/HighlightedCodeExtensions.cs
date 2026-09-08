// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.SyntaxHighlighting;

using ktsu.SyntaxHighlighting;

/// <summary>
/// Draws code that was tokenized by the renderer-agnostic <c>ktsu.SyntaxHighlighting</c>. The
/// tokenized type itself knows nothing about Dear ImGui, so drawing it is an extension here rather
/// than a member there.
/// </summary>
public static class HighlightedCodeExtensions
{
	/// <summary>Renders pre-tokenized code at the current cursor position.</summary>
	/// <param name="code">The tokenized code.</param>
	/// <param name="config">Optional rendering config; defaults are used when omitted.</param>
	public static void Render(this HighlightedCode code, SyntaxHighlightConfig? config = null) =>
		ImGuiSyntaxHighlighting.Render(code, config);
}
