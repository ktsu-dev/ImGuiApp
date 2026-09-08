// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.SyntaxHighlighting;

using System.Collections.Generic;

/// <summary>
/// The language definitions registered by default. Each is exposed individually so an application
/// can derive a variant from one with a <c>with</c> expression and register that instead.
/// </summary>
public static class BuiltInLanguages
{
	private static readonly LineCommentRule SlashComment = new() { Prefix = "//" };
	private static readonly LineCommentRule SlashDocComment = new() { Prefix = "///", Kind = TokenKind.DocComment };
	private static readonly LineCommentRule HashComment = new() { Prefix = "#" };
	private static readonly LineCommentRule DashComment = new() { Prefix = "--" };
	private static readonly BlockCommentRule SlashStarComment = new() { Open = "/*", Close = "*/" };
	private static readonly BlockCommentRule SlashStarDocComment = new() { Open = "/**", Close = "*/", Kind = TokenKind.DocComment };

	private static readonly StringRule DoubleQuoted = new() { Open = "\"", Close = "\"" };
	private static readonly StringRule SingleQuoted = new() { Open = "'", Close = "'" };
	private static readonly StringRule BackTicked = new() { Open = "`", Close = "`", AllowMultiline = true };

	/// <summary>Plain text: no comments, keywords, or literals, so every character stays unstyled.</summary>
	public static LanguageDefinition PlainText { get; } = new()
	{
		Name = "text",
		Aliases = ["plain", "plaintext", "txt", "none"],
		HighlightFunctionCalls = false,
		OperatorCharacters = string.Empty,
		PunctuationCharacters = string.Empty,
	};

	/// <summary>C#.</summary>
	public static LanguageDefinition CSharp { get; } = new()
	{
		Name = "csharp",
		Aliases = ["cs", "c#"],
		LineComments = [SlashDocComment, SlashComment],
		BlockComments = [SlashStarComment],
		Strings =
		[
			new StringRule { Open = "\"\"\"", Close = "\"\"\"", Escape = null, AllowMultiline = true },
			new StringRule { Open = "@\"", Close = "\"", Escape = null, DoubledCloseEscapes = true, AllowMultiline = true },
			new StringRule { Open = "$@\"", Close = "\"", Escape = null, DoubledCloseEscapes = true, AllowMultiline = true },
			new StringRule { Open = "$\"", Close = "\"" },
			DoubleQuoted,
			SingleQuoted,
		],
		Keywords =
		[
			"abstract", "as", "async", "await", "base", "checked", "class", "const", "delegate", "enum",
			"event", "explicit", "extern", "file", "fixed", "get", "global", "implicit", "in", "init",
			"interface", "internal", "is", "lock", "namespace", "new", "operator", "out", "override",
			"params", "partial", "private", "protected", "public", "readonly", "record", "ref", "required",
			"sealed", "set", "sizeof", "stackalloc", "static", "struct", "this", "typeof", "unchecked",
			"unsafe", "using", "value", "virtual", "volatile", "where", "with",
		],
		ControlKeywords =
		[
			"break", "case", "catch", "continue", "default", "do", "else", "finally", "for", "foreach",
			"goto", "if", "return", "switch", "throw", "try", "while", "yield",
		],
		Types =
		[
			"bool", "byte", "char", "decimal", "double", "dynamic", "float", "int", "long", "nint",
			"nuint", "object", "sbyte", "short", "string", "uint", "ulong", "ushort", "var", "void",
		],
		Constants = ["true", "false", "null", "default"],
		DirectivePrefix = '#',
	};

	/// <summary>C.</summary>
	public static LanguageDefinition C { get; } = new()
	{
		Name = "c",
		Aliases = ["h"],
		LineComments = [SlashComment],
		BlockComments = [SlashStarDocComment, SlashStarComment],
		Strings = [DoubleQuoted, SingleQuoted],
		Keywords =
		[
			"auto", "const", "enum", "extern", "inline", "register", "restrict", "signed", "sizeof",
			"static", "struct", "typedef", "union", "unsigned", "volatile",
		],
		ControlKeywords = ["break", "case", "continue", "default", "do", "else", "for", "goto", "if", "return", "switch", "while"],
		Types = ["bool", "char", "double", "float", "int", "long", "short", "size_t", "void"],
		Constants = ["NULL", "true", "false"],
		DirectivePrefix = '#',
	};

	/// <summary>C++.</summary>
	public static LanguageDefinition Cpp { get; } = new()
	{
		Name = "cpp",
		Aliases = ["c++", "cc", "cxx", "hpp"],
		LineComments = [SlashComment],
		BlockComments = [SlashStarDocComment, SlashStarComment],
		Strings = [DoubleQuoted, SingleQuoted],
		Keywords =
		[
			"auto", "class", "concept", "const", "consteval", "constexpr", "constinit", "decltype",
			"delete", "enum", "explicit", "export", "extern", "friend", "inline", "mutable", "namespace",
			"new", "noexcept", "operator", "override", "private", "protected", "public", "register",
			"requires", "sizeof", "static", "struct", "template", "this", "typedef", "typename", "union",
			"unsigned", "using", "virtual", "volatile",
		],
		ControlKeywords =
		[
			"break", "case", "catch", "continue", "co_await", "co_return", "co_yield", "default", "do",
			"else", "for", "goto", "if", "return", "switch", "throw", "try", "while",
		],
		Types = ["bool", "char", "double", "float", "int", "long", "short", "signed", "size_t", "void", "wchar_t"],
		Constants = ["true", "false", "nullptr", "NULL"],
		DirectivePrefix = '#',
	};

	/// <summary>JavaScript.</summary>
	public static LanguageDefinition JavaScript { get; } = new()
	{
		Name = "javascript",
		Aliases = ["js", "jsx", "mjs", "cjs"],
		LineComments = [SlashComment],
		BlockComments = [SlashStarDocComment, SlashStarComment],
		Strings = [DoubleQuoted, SingleQuoted, BackTicked],
		Keywords =
		[
			"async", "await", "class", "const", "delete", "export", "extends", "from", "function", "get",
			"import", "in", "instanceof", "let", "new", "of", "set", "static", "super", "this", "typeof",
			"var", "void", "with",
		],
		ControlKeywords = ["break", "case", "catch", "continue", "default", "do", "else", "finally", "for", "if", "return", "switch", "throw", "try", "while", "yield"],
		Constants = ["true", "false", "null", "undefined", "NaN", "Infinity"],
		IdentifierStartCharacters = "_$",
		IdentifierCharacters = "_$",
	};

	/// <summary>TypeScript.</summary>
	public static LanguageDefinition TypeScript { get; } = JavaScript with
	{
		Name = "typescript",
		Aliases = ["ts", "tsx"],
		Keywords =
		[
			"abstract", "as", "async", "await", "class", "const", "declare", "delete", "enum", "export",
			"extends", "from", "function", "get", "implements", "import", "in", "infer", "instanceof",
			"interface", "keyof", "let", "namespace", "new", "of", "private", "protected", "public",
			"readonly", "satisfies", "set", "static", "super", "this", "type", "typeof", "var", "void",
		],
		Types = ["any", "bigint", "boolean", "never", "number", "object", "string", "symbol", "unknown"],
	};

	/// <summary>Python.</summary>
	public static LanguageDefinition Python { get; } = new()
	{
		Name = "python",
		Aliases = ["py"],
		LineComments = [HashComment],
		Strings =
		[
			new StringRule { Open = "\"\"\"", Close = "\"\"\"", AllowMultiline = true },
			new StringRule { Open = "'''", Close = "'''", AllowMultiline = true },
			new StringRule { Open = "f\"", Close = "\"" },
			new StringRule { Open = "r\"", Close = "\"", Escape = null },
			DoubleQuoted,
			SingleQuoted,
		],
		Keywords =
		[
			"and", "as", "assert", "async", "await", "class", "def", "del", "from", "global", "import",
			"in", "is", "lambda", "match", "nonlocal", "not", "or", "pass", "with",
		],
		ControlKeywords = ["break", "case", "continue", "elif", "else", "except", "finally", "for", "if", "raise", "return", "try", "while", "yield"],
		Types = ["bool", "bytes", "dict", "float", "frozenset", "int", "list", "set", "str", "tuple"],
		Constants = ["True", "False", "None", "self", "cls"],
	};

	/// <summary>JSON.</summary>
	public static LanguageDefinition Json { get; } = new()
	{
		Name = "json",
		Aliases = ["jsonc", "json5"],
		LineComments = [SlashComment],
		BlockComments = [SlashStarComment],
		Strings = [DoubleQuoted],
		Constants = ["true", "false", "null"],
		HighlightFunctionCalls = false,
		HighlightPropertyNames = true,
		OperatorCharacters = string.Empty,
		PunctuationCharacters = "{}[],:",
	};

	/// <summary>YAML. Block scalars and anchors are highlighted only as far as line structure allows.</summary>
	public static LanguageDefinition Yaml { get; } = new()
	{
		Name = "yaml",
		Aliases = ["yml"],
		LineComments = [HashComment],
		Strings = [DoubleQuoted, SingleQuoted],
		Constants = ["true", "false", "null", "yes", "no", "on", "off", "~"],
		CaseSensitive = false,
		HighlightFunctionCalls = false,
		HighlightPropertyNames = true,
		IdentifierCharacters = "_-.",
		IdentifierStartCharacters = "_-.",
		OperatorCharacters = "-",
		PunctuationCharacters = "{}[],:",
	};

	/// <summary>XML.</summary>
	public static LanguageDefinition Xml { get; } = new()
	{
		Name = "xml",
		Aliases = ["xaml", "csproj", "svg", "xsd"],
		IsMarkup = true,
	};

	/// <summary>HTML.</summary>
	public static LanguageDefinition Html { get; } = new()
	{
		Name = "html",
		Aliases = ["htm", "xhtml"],
		IsMarkup = true,
	};

	/// <summary>CSS.</summary>
	public static LanguageDefinition Css { get; } = new()
	{
		Name = "css",
		Aliases = ["scss", "less"],
		LineComments = [SlashComment],
		BlockComments = [SlashStarComment],
		Strings = [DoubleQuoted, SingleQuoted],
		Constants = ["inherit", "initial", "none", "unset", "auto"],
		CaseSensitive = false,
		DirectivePrefix = '@',
		HighlightPropertyNames = true,
		IdentifierCharacters = "-_",
		IdentifierStartCharacters = "-_",
		OperatorCharacters = "*>+~=",
		PunctuationCharacters = "{}();,:.#",
	};

	/// <summary>SQL.</summary>
	public static LanguageDefinition Sql { get; } = new()
	{
		Name = "sql",
		Aliases = ["mysql", "pgsql", "sqlite", "tsql"],
		CaseSensitive = false,
		LineComments = [DashComment],
		BlockComments = [SlashStarComment],
		Strings = [SingleQuoted with { Escape = null, DoubledCloseEscapes = true }, DoubleQuoted],
		Keywords =
		[
			"add", "all", "alter", "and", "as", "asc", "between", "by", "column", "constraint", "create",
			"cross", "delete", "desc", "distinct", "drop", "exists", "foreign", "from", "full", "group",
			"having", "in", "index", "inner", "insert", "into", "join", "key", "left", "like", "limit",
			"not", "offset", "on", "or", "order", "outer", "primary", "references", "right", "select",
			"set", "table", "top", "union", "unique", "update", "values", "view", "where", "with",
		],
		ControlKeywords = ["begin", "case", "commit", "else", "end", "if", "return", "rollback", "then", "when", "while"],
		Types = ["bigint", "bit", "blob", "boolean", "char", "date", "datetime", "decimal", "float", "int", "integer", "numeric", "real", "text", "time", "timestamp", "uuid", "varchar"],
		Constants = ["null", "true", "false"],
	};

	/// <summary>Shell scripts (bash, sh, zsh).</summary>
	public static LanguageDefinition Shell { get; } = new()
	{
		Name = "shell",
		Aliases = ["bash", "sh", "zsh", "console"],
		LineComments = [HashComment],
		Strings = [DoubleQuoted, new StringRule { Open = "'", Close = "'", Escape = null }, BackTicked],
		Keywords = ["declare", "export", "function", "local", "readonly", "source", "unset"],
		ControlKeywords = ["case", "do", "done", "elif", "else", "esac", "fi", "for", "if", "in", "return", "then", "until", "while"],
		Constants = ["true", "false"],
		IdentifierCharacters = "_-",
		IdentifierStartCharacters = "_$",
		OperatorCharacters = "|&<>=!*",
		PunctuationCharacters = "(){};",
	};

	/// <summary>Lua.</summary>
	public static LanguageDefinition Lua { get; } = new()
	{
		Name = "lua",
		LineComments = [DashComment],
		BlockComments = [new BlockCommentRule { Open = "--[[", Close = "]]" }],
		Strings = [DoubleQuoted, SingleQuoted, new StringRule { Open = "[[", Close = "]]", Escape = null, AllowMultiline = true }],
		Keywords = ["and", "function", "local", "not", "or", "self"],
		ControlKeywords = ["break", "do", "else", "elseif", "end", "for", "goto", "if", "in", "repeat", "return", "then", "until", "while"],
		Constants = ["true", "false", "nil"],
	};

	/// <summary>Every built-in definition, in registration order.</summary>
	public static IReadOnlyList<LanguageDefinition> All { get; } =
	[
		PlainText, CSharp, C, Cpp, JavaScript, TypeScript, Python, Json, Yaml, Xml, Html, Css, Sql, Shell, Lua,
	];
}
