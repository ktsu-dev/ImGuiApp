// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.SyntaxHighlighting;

using System.Collections.Generic;

using ktsu.SyntaxHighlighting;

/// <summary>
/// Renders syntax-highlighted source code inside Dear ImGui. Tokenizing itself lives in the
/// renderer-agnostic <c>ktsu.SyntaxHighlighting</c>; this is the Dear ImGui drawing layer over it.
/// The static <see cref="Render(string, string, SyntaxHighlightConfig)"/> caches tokenization by
/// source text; for hot paths, construct a <see cref="HighlightedCode"/> once and render it each frame.
/// </summary>
public static class ImGuiSyntaxHighlighting
{
	private static readonly SyntaxHighlightConfig DefaultConfig = new();

	/// <summary>Tokenizes (cached) and renders code at the current cursor position.</summary>
	/// <param name="code">The source text.</param>
	/// <param name="language">The language name or alias; unknown names render as plain text.</param>
	/// <param name="config">Optional rendering config; defaults are used when omitted.</param>
	public static void Render(string code, string language, SyntaxHighlightConfig? config = null)
	{
		if (string.IsNullOrEmpty(code))
		{
			return;
		}

		SyntaxHighlightConfig active = config ?? DefaultConfig;
		CodeRenderer.Render(SyntaxHighlighter.HighlightCached(code, language, active.TabWidth), active);
	}

	/// <summary>Renders pre-tokenized code at the current cursor position.</summary>
	/// <param name="code">The tokenized code.</param>
	/// <param name="config">Optional rendering config; defaults are used when omitted.</param>
	public static void Render(HighlightedCode code, SyntaxHighlightConfig? config = null)
	{
		Ensure.NotNull(code);
		CodeRenderer.Render(code.Lines, config ?? DefaultConfig);
	}

	/// <summary>
	/// Tokenizes code without drawing anything, for callers that want the classified runs — to draw
	/// them their own way, to export them, or to test them.
	/// </summary>
	/// <param name="code">The source text.</param>
	/// <param name="language">The language name or alias; unknown names classify everything as plain.</param>
	/// <param name="tabWidth">The tab stop width used when expanding tabs to spaces.</param>
	/// <returns>The tokenized lines.</returns>
	public static IReadOnlyList<HighlightedLine> Highlight(string code, string language, int tabWidth = SyntaxHighlighter.DefaultTabWidth) =>
		SyntaxHighlighter.Highlight(code, language, tabWidth);
}
