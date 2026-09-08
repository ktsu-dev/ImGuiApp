// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.SyntaxHighlighting;

using ktsu.Semantics.Color;

/// <summary>
/// Maps every <see cref="TokenKind"/> to a color. Colors are held as the semantic linear
/// <see cref="Color"/> and converted only when drawing, so a theme can be manipulated with the
/// operations in <c>ktsu.Semantics.Color</c> before it is used, whatever the host renderer.
/// </summary>
/// <remarks>
/// <see cref="Background"/>, <see cref="Plain"/> and <see cref="LineNumber"/> are nullable: leaving
/// them unset makes the renderer take them from the host UI's own theme (in
/// <c>ktsu.ImGui.SyntaxHighlighting</c>, from <c>FrameBg</c>, <c>Text</c> and <c>TextDisabled</c>),
/// so a code block keeps matching the surrounding UI when the application changes theme.
/// </remarks>
public sealed record SyntaxTheme
{
	/// <summary>A human-readable name for the theme.</summary>
	public string Name { get; init; } = "Custom";

	/// <summary>The code background; when <see langword="null"/>, the theme's frame background is used.</summary>
	public Color? Background { get; init; }

	/// <summary>Unclassified text; when <see langword="null"/>, the theme's text color is used.</summary>
	public Color? Plain { get; init; }

	/// <summary>The line-number gutter; when <see langword="null"/>, the theme's disabled text color is used.</summary>
	public Color? LineNumber { get; init; }

	/// <summary>The color for <see cref="TokenKind.Comment"/>.</summary>
	public Color Comment { get; init; } = Color.FromHex("#6a9955");

	/// <summary>The color for <see cref="TokenKind.DocComment"/>.</summary>
	public Color DocComment { get; init; } = Color.FromHex("#57a64a");

	/// <summary>The color for <see cref="TokenKind.Keyword"/>.</summary>
	public Color Keyword { get; init; } = Color.FromHex("#569cd6");

	/// <summary>The color for <see cref="TokenKind.ControlKeyword"/>.</summary>
	public Color ControlKeyword { get; init; } = Color.FromHex("#c586c0");

	/// <summary>The color for <see cref="TokenKind.Type"/>.</summary>
	public Color Type { get; init; } = Color.FromHex("#4ec9b0");

	/// <summary>The color for <see cref="TokenKind.Function"/>.</summary>
	public Color Function { get; init; } = Color.FromHex("#dcdcaa");

	/// <summary>The color for <see cref="TokenKind.Number"/>.</summary>
	public Color Number { get; init; } = Color.FromHex("#b5cea8");

	/// <summary>The color for <see cref="TokenKind.StringLiteral"/>.</summary>
	public Color StringLiteral { get; init; } = Color.FromHex("#ce9178");

	/// <summary>The color for <see cref="TokenKind.Constant"/>.</summary>
	public Color Constant { get; init; } = Color.FromHex("#569cd6");

	/// <summary>The color for <see cref="TokenKind.Operator"/>.</summary>
	public Color Operator { get; init; } = Color.FromHex("#d4d4d4");

	/// <summary>The color for <see cref="TokenKind.Punctuation"/>.</summary>
	public Color Punctuation { get; init; } = Color.FromHex("#d4d4d4");

	/// <summary>The color for <see cref="TokenKind.Preprocessor"/>.</summary>
	public Color Preprocessor { get; init; } = Color.FromHex("#9b9b9b");

	/// <summary>The color for <see cref="TokenKind.Tag"/>.</summary>
	public Color Tag { get; init; } = Color.FromHex("#569cd6");

	/// <summary>The color for <see cref="TokenKind.AttributeName"/>.</summary>
	public Color AttributeName { get; init; } = Color.FromHex("#9cdcfe");

	/// <summary>The color for <see cref="TokenKind.Property"/>.</summary>
	public Color Property { get; init; } = Color.FromHex("#9cdcfe");

	/// <summary>
	/// A palette tuned for dark backgrounds. Background, plain text and gutter follow the host UI's
	/// own theme.
	/// </summary>
	public static SyntaxTheme Dark { get; } = new() { Name = "Dark" };

	/// <summary>
	/// A palette tuned for light backgrounds. Background, plain text and gutter follow the host UI's
	/// own theme.
	/// </summary>
	public static SyntaxTheme Light { get; } = new()
	{
		Name = "Light",
		Comment = Color.FromHex("#008000"),
		DocComment = Color.FromHex("#008000"),
		Keyword = Color.FromHex("#0000ff"),
		ControlKeyword = Color.FromHex("#af00db"),
		Type = Color.FromHex("#267f99"),
		Function = Color.FromHex("#795e26"),
		Number = Color.FromHex("#098658"),
		StringLiteral = Color.FromHex("#a31515"),
		Constant = Color.FromHex("#0000ff"),
		Operator = Color.FromHex("#3b3b3b"),
		Punctuation = Color.FromHex("#3b3b3b"),
		Preprocessor = Color.FromHex("#6f6f6f"),
		Tag = Color.FromHex("#800000"),
		AttributeName = Color.FromHex("#e50000"),
		Property = Color.FromHex("#0451a5"),
	};

	/// <summary>
	/// Picks the built-in palette that reads correctly against a background of the given relative
	/// luminance, so the default rendering follows a light or dark application theme.
	/// </summary>
	/// <param name="backgroundLuminance">The background's WCAG relative luminance (0 is black, 1 is white).</param>
	/// <returns><see cref="Light"/> for light backgrounds, otherwise <see cref="Dark"/>.</returns>
	public static SyntaxTheme ForBackgroundLuminance(double backgroundLuminance) =>
		backgroundLuminance >= 0.5 ? Light : Dark;

	/// <summary>Gets the color for a token kind.</summary>
	/// <param name="kind">The token kind.</param>
	/// <returns>
	/// The configured color, or <see langword="null"/> for <see cref="TokenKind.Plain"/> when
	/// <see cref="Plain"/> is unset and the caller should use the host UI's own text color.
	/// </returns>
	public Color? ColorFor(TokenKind kind) => kind switch
	{
		TokenKind.Comment => Comment,
		TokenKind.DocComment => DocComment,
		TokenKind.Keyword => Keyword,
		TokenKind.ControlKeyword => ControlKeyword,
		TokenKind.Type => Type,
		TokenKind.Function => Function,
		TokenKind.Number => Number,
		TokenKind.StringLiteral => StringLiteral,
		TokenKind.Constant => Constant,
		TokenKind.Operator => Operator,
		TokenKind.Punctuation => Punctuation,
		TokenKind.Preprocessor => Preprocessor,
		TokenKind.Tag => Tag,
		TokenKind.AttributeName => AttributeName,
		TokenKind.Property => Property,
		_ => Plain,
	};
}
