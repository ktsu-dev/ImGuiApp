// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.SyntaxHighlighting;

/// <summary>
/// The syntactic role of a token, which selects its color from the active <see cref="SyntaxTheme"/>.
/// </summary>
public enum TokenKind
{
	/// <summary>Text with no special meaning: identifiers, whitespace, and unrecognized characters.</summary>
	Plain,

	/// <summary>A line or block comment.</summary>
	Comment,

	/// <summary>A documentation comment (for example <c>///</c> in C# or <c>/** */</c> in Java-like languages).</summary>
	DocComment,

	/// <summary>A declaration or modifier keyword (<c>class</c>, <c>public</c>, <c>const</c>).</summary>
	Keyword,

	/// <summary>A control-flow keyword (<c>if</c>, <c>for</c>, <c>return</c>).</summary>
	ControlKeyword,

	/// <summary>A built-in or well-known type name (<c>int</c>, <c>string</c>, <c>bool</c>).</summary>
	Type,

	/// <summary>An identifier used as a function or method call.</summary>
	Function,

	/// <summary>A numeric literal.</summary>
	Number,

	/// <summary>A string or character literal.</summary>
	StringLiteral,

	/// <summary>A language constant such as <c>true</c>, <c>false</c>, or <c>null</c>.</summary>
	Constant,

	/// <summary>An operator such as <c>+</c>, <c>=&gt;</c>, or <c>==</c>.</summary>
	Operator,

	/// <summary>Structural punctuation such as braces, brackets, semicolons, and commas.</summary>
	Punctuation,

	/// <summary>A preprocessor or directive line (<c>#include</c>, <c>#if</c>, <c>@media</c>).</summary>
	Preprocessor,

	/// <summary>A markup element name.</summary>
	Tag,

	/// <summary>A markup attribute name.</summary>
	AttributeName,

	/// <summary>An object key or property name (a JSON/YAML key, a CSS property).</summary>
	Property,
}
