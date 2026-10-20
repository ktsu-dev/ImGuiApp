// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.SyntaxHighlighting.Tests;

using System.Collections.Generic;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class CodeTokenizerTests
{
	private static IReadOnlyList<HighlightedLine> Highlight(string code, string language) =>
		SyntaxHighlighter.Highlight(code, language);

	[TestMethod]
	public void CSharp_ClassifiesKeywordsTypesAndLiterals()
	{
		IReadOnlyList<HighlightedLine> lines = Highlight("public static int Count = 42;", "csharp");

		TokenAssert.HasToken(lines, "public", TokenKind.Keyword);
		TokenAssert.HasToken(lines, "static", TokenKind.Keyword);
		TokenAssert.HasToken(lines, "int", TokenKind.Type);
		TokenAssert.HasToken(lines, "Count", TokenKind.Plain);
		TokenAssert.HasToken(lines, "42", TokenKind.Number);
		TokenAssert.HasToken(lines, ";", TokenKind.Punctuation);
	}

	[TestMethod]
	public void CSharp_SeparatesControlKeywordsFromDeclarations()
	{
		IReadOnlyList<HighlightedLine> lines = Highlight("if (ready) { return; }", "cs");

		TokenAssert.HasToken(lines, "if", TokenKind.ControlKeyword);
		TokenAssert.HasToken(lines, "return", TokenKind.ControlKeyword);
	}

	[TestMethod]
	public void CSharp_ClassifiesCallsAsFunctions()
	{
		IReadOnlyList<HighlightedLine> lines = Highlight("Console.WriteLine(message);", "csharp");

		TokenAssert.HasToken(lines, "WriteLine", TokenKind.Function);
		TokenAssert.HasToken(lines, "Console", TokenKind.Plain);
		TokenAssert.HasToken(lines, "message", TokenKind.Plain);
	}

	[TestMethod]
	public void CSharp_DocCommentBeatsLineComment()
	{
		// The doc comment's tags are XML (see EmbeddedLanguageTests), so the prefix and the prose
		// between them are what carries the DocComment kind.
		IReadOnlyList<HighlightedLine> lines = Highlight("/// <summary>Doc.</summary>\n// plain\n", "csharp");

		TokenAssert.HasToken(lines, "/// ", TokenKind.DocComment);
		TokenAssert.HasToken(lines, "Doc.", TokenKind.DocComment);
		TokenAssert.HasToken(lines, "// plain", TokenKind.Comment);
	}

	[TestMethod]
	public void CSharp_VerbatimStringKeepsDoubledQuotes()
	{
		// A doubled quote inside @"..." escapes the quote, so the literal runs to the final one.
		IReadOnlyList<HighlightedLine> lines = Highlight("x = @\"a\"\"b\"; y = 1;", "csharp");

		TokenAssert.HasToken(lines, "@\"a\"\"b\"", TokenKind.StringLiteral);
		TokenAssert.HasToken(lines, "1", TokenKind.Number);
	}

	[TestMethod]
	public void CSharp_RawStringSpansLines()
	{
		string code = "var text = \"\"\"\nline one\nline two\n\"\"\";";
		IReadOnlyList<HighlightedLine> lines = Highlight(code, "csharp");

		Assert.AreEqual(4, lines.Count);
		Assert.IsTrue(
			lines[1].Tokens.All(token => token.Kind == TokenKind.StringLiteral),
			$"The raw string body was not held together: {TokenAssert.Describe(lines)}");
		TokenAssert.HasToken(lines, ";", TokenKind.Punctuation);
	}

	[TestMethod]
	public void CSharp_EscapedQuoteDoesNotEndTheString()
	{
		IReadOnlyList<HighlightedLine> lines = Highlight("""s = "a\"b"; n = 7;""", "csharp");

		TokenAssert.HasToken(lines, "7", TokenKind.Number);
		Assert.AreEqual(
			1,
			TokenAssert.Flatten(lines).Count(token => token.Kind == TokenKind.StringLiteral),
			$"The escaped quote split the literal: {TokenAssert.Describe(lines)}");
	}

	[TestMethod]
	public void UnterminatedStringStopsAtTheNewline()
	{
		// One stray quote must not recolor the rest of the document.
		IReadOnlyList<HighlightedLine> lines = Highlight("s = \"oops\nint value = 3;", "csharp");

		TokenAssert.HasToken(lines, "int", TokenKind.Type);
		TokenAssert.HasToken(lines, "3", TokenKind.Number);
	}

	[TestMethod]
	public void CSharp_DirectiveRunsToEndOfLine()
	{
		IReadOnlyList<HighlightedLine> lines = Highlight("#if DEBUG\nint x;\n#endif\n", "csharp");

		TokenAssert.HasToken(lines, "#if DEBUG", TokenKind.Preprocessor);
		TokenAssert.HasToken(lines, "#endif", TokenKind.Preprocessor);
		TokenAssert.HasToken(lines, "int", TokenKind.Type);
	}

	[TestMethod]
	public void HashIsOnlyADirectiveAtTheStartOfALine()
	{
		IReadOnlyList<HighlightedLine> lines = Highlight("a = b # c;", "csharp");

		Assert.IsEmpty(
			TokenAssert.Flatten(lines).Where(token => token.Kind == TokenKind.Preprocessor),
			$"A mid-line '#' was treated as a directive: {TokenAssert.Describe(lines)}");
	}

	[TestMethod]
	public void BlockCommentSpansLinesAndClosesCorrectly()
	{
		IReadOnlyList<HighlightedLine> lines = Highlight("/* one\ntwo */ int x;", "csharp");

		Assert.AreEqual(2, lines.Count);
		Assert.AreEqual(TokenKind.Comment, lines[0].Tokens[0].Kind);
		TokenAssert.HasToken(lines, "int", TokenKind.Type);
	}

	[TestMethod]
	public void Numbers_CoverHexFloatAndExponent()
	{
		IReadOnlyList<HighlightedLine> lines = Highlight("a = 0xFF; b = 1.5f; c = 1e-9; d = 1_000;", "csharp");

		TokenAssert.HasToken(lines, "0xFF", TokenKind.Number);
		TokenAssert.HasToken(lines, "1.5f", TokenKind.Number);
		TokenAssert.HasToken(lines, "1e-9", TokenKind.Number);
		TokenAssert.HasToken(lines, "1_000", TokenKind.Number);
	}

	[TestMethod]
	public void MemberAccessAfterANumberIsNotPartOfIt()
	{
		IReadOnlyList<HighlightedLine> lines = Highlight("x = 3.ToString();", "csharp");

		TokenAssert.HasToken(lines, "3", TokenKind.Number);
		TokenAssert.HasToken(lines, "ToString", TokenKind.Function);
	}

	[TestMethod]
	public void Python_TripleQuotedStringAndKeywords()
	{
		IReadOnlyList<HighlightedLine> lines = Highlight("def run():\n    \"\"\"Doc.\"\"\"\n    return True\n", "py");

		TokenAssert.HasToken(lines, "def", TokenKind.Keyword);
		TokenAssert.HasToken(lines, "run", TokenKind.Function);
		TokenAssert.HasToken(lines, "\"\"\"Doc.\"\"\"", TokenKind.StringLiteral);
		TokenAssert.HasToken(lines, "return", TokenKind.ControlKeyword);
		TokenAssert.HasToken(lines, "True", TokenKind.Constant);
	}

	[TestMethod]
	public void Json_ClassifiesKeysSeparatelyFromStringValues()
	{
		IReadOnlyList<HighlightedLine> lines = Highlight("""{ "name": "value", "count": 3, "ok": true }""", "json");

		TokenAssert.HasToken(lines, "\"name\"", TokenKind.Property);
		TokenAssert.HasToken(lines, "\"value\"", TokenKind.StringLiteral);
		TokenAssert.HasToken(lines, "3", TokenKind.Number);
		TokenAssert.HasToken(lines, "true", TokenKind.Constant);
	}

	[TestMethod]
	public void Sql_MatchesKeywordsRegardlessOfCase()
	{
		IReadOnlyList<HighlightedLine> lines = Highlight("SELECT id FROM users where id = 1;", "sql");

		TokenAssert.HasToken(lines, "SELECT", TokenKind.Keyword);
		TokenAssert.HasToken(lines, "where", TokenKind.Keyword);
		TokenAssert.HasToken(lines, "1", TokenKind.Number);
	}

	[TestMethod]
	public void Lua_LongCommentBeatsTheLineComment()
	{
		IReadOnlyList<HighlightedLine> lines = Highlight("--[[ block\nstill comment ]] local x = 1", "lua");

		Assert.AreEqual(TokenKind.Comment, lines[1].Tokens[0].Kind);
		TokenAssert.HasToken(lines, "local", TokenKind.Keyword);
	}

	[TestMethod]
	public void Shell_SingleQuotedStringIgnoresBackslashEscapes()
	{
		IReadOnlyList<HighlightedLine> lines = Highlight("""echo 'a\' b""", "bash");

		Assert.AreEqual(
			1,
			TokenAssert.Flatten(lines).Count(token => token.Kind == TokenKind.StringLiteral),
			$"The backslash was treated as an escape: {TokenAssert.Describe(lines)}");
	}

	[TestMethod]
	public void UnknownLanguageLeavesEverythingPlain()
	{
		IReadOnlyList<HighlightedLine> lines = Highlight("class Thing { }", "brainfuck");

		Assert.IsTrue(
			TokenAssert.Flatten(lines).All(token => token.Kind == TokenKind.Plain),
			$"An unknown language classified tokens: {TokenAssert.Describe(lines)}");
	}

	// Asserts that the literal, written after "x = " and followed by ";\ny = 1;", is a string from
	// its first character to its last, and that the code after it is code again.
	private static void AssertLiteralIsHeldTogether(string literal, string language)
	{
		const string Before = "x = ";
		string code = $"{Before}{literal};\ny = 1;";
		IReadOnlyList<HighlightedLine> lines = Highlight(code, language);

		TokenAssert.Reconstructs(lines, code);
		int offset = 0;
		foreach (HighlightedLine line in lines)
		{
			foreach (HighlightedToken token in line.Tokens)
			{
				int end = offset + token.Text.Length;
				bool insideLiteral = offset < Before.Length + literal.Length && end > Before.Length;
				if (insideLiteral)
				{
					Assert.AreEqual(
						TokenKind.StringLiteral,
						token.Kind,
						$"'{token.Text}' inside {literal} was not part of the string: {TokenAssert.Describe(lines)}");
					Assert.IsTrue(
						offset >= Before.Length && end <= Before.Length + literal.Length,
						$"'{token.Text}' runs past the edges of {literal}: {TokenAssert.Describe(lines)}");
				}

				offset = end;
			}

			offset++;
		}

		TokenAssert.HasToken(lines, "1", TokenKind.Number);
	}

	[TestMethod]
	[DataRow("$\"\"\"\n    Hello {name}\n    \"\"\"")]
	[DataRow("$$\"\"\"\n    {\"a\": {{n}}}\n    \"\"\"")]
	[DataRow("$$$\"\"\"\n    {{{n}}}\n    \"\"\"")]
	[DataRow("$\"\"\"Hello {name}\"\"\"")]
	[DataRow("\"\"\"\"\n    A \"\"\" inside\n    \"\"\"\"")]
	[DataRow("$\"\"\"\"\" five \"\"\"\" quotes \"\"\"\"\"")]
	[DataRow("@$\"C:\\dir\\\"")]
	[DataRow("$@\"say \"\"hi\"\"\"")]
	public void CSharp_RawAndVerbatimInterpolatedStringsHoldTogether(string literal) =>
		AssertLiteralIsHeldTogether(literal, "csharp");

	[TestMethod]
	[DataRow("f\"\"\"\n    Hello {name}\n    \"\"\"")]
	[DataRow("f'''\n    Hello {name}\n    '''")]
	[DataRow("r\"\"\"\n    C:\\dir\\\n    \"\"\"")]
	[DataRow("r'''\n    \\d+\n    '''")]
	[DataRow("rf\"\"\"\n    {path}\\n\n    \"\"\"")]
	[DataRow("Fr'''\n    {path}\n    '''")]
	[DataRow("b\"\"\"\n    bytes\n    \"\"\"")]
	[DataRow("RB\"C:\\dir\\\"")]
	[DataRow("F\"{name}\"")]
	public void Python_PrefixedStringsHoldTogether(string literal) =>
		AssertLiteralIsHeldTogether(literal, "python");

	[TestMethod]
	public void Python_RawStringDoesNotTreatBackslashAsAnEscape()
	{
		IReadOnlyList<HighlightedLine> lines = Highlight("p = r'C:\\dir\\'\nn = 2", "python");

		TokenAssert.HasToken(lines, "r'C:\\dir\\'", TokenKind.StringLiteral);
		TokenAssert.HasToken(lines, "2", TokenKind.Number);
	}

	[TestMethod]
	[DataRow("c")]
	[DataRow("cpp")]
	[DataRow("javascript")]
	[DataRow("typescript")]
	public void EmptyBlockCommentClosesItself(string language)
	{
		IReadOnlyList<HighlightedLine> lines = Highlight("f(/**/);\nx = 1;\n/* x */ y = 2;", language);

		TokenAssert.HasToken(lines, "/**/", TokenKind.Comment);
		TokenAssert.HasToken(lines, "1", TokenKind.Number);
		TokenAssert.HasToken(lines, "/* x */", TokenKind.Comment);
		TokenAssert.HasToken(lines, "2", TokenKind.Number);
	}

	[TestMethod]
	public void LongerBlockCommentsKeepTheirKinds()
	{
		IReadOnlyList<HighlightedLine> lines = Highlight("/***/ a; /** doc */ b; /* x */ c = 1;", "c");

		TokenAssert.HasToken(lines, "/***/", TokenKind.DocComment);
		TokenAssert.HasToken(lines, "/** doc */", TokenKind.DocComment);
		TokenAssert.HasToken(lines, "/* x */", TokenKind.Comment);
		TokenAssert.HasToken(lines, "1", TokenKind.Number);
	}

	[TestMethod]
	public void SlashStarSlashStillOpensAComment()
	{
		// C reads /*/ as an opener whose star is not the close's, so the comment runs to the next */.
		IReadOnlyList<HighlightedLine> lines = Highlight("/*/ still */ x = 1;", "c");

		TokenAssert.HasToken(lines, "/*/ still */", TokenKind.Comment);
		TokenAssert.HasToken(lines, "1", TokenKind.Number);
	}

	[TestMethod]
	[DataRow("echo ${#arr[@]} done")]
	[DataRow("if [ $# -eq 0 ]; then exit; fi")]
	[DataRow("echo ${x#y}")]
	[DataRow("echo ${x##*/}")]
	[DataRow("echo a#b")]
	public void Shell_HashInsideAWordIsNotAComment(string code)
	{
		IReadOnlyList<HighlightedLine> lines = Highlight(code, "bash");

		Assert.IsFalse(
			TokenAssert.Flatten(lines).Any(token => token.Kind == TokenKind.Comment),
			$"A # inside a word started a comment: {TokenAssert.Describe(lines)}");
	}

	[TestMethod]
	public void Shell_HashAfterWhitespaceOrAtLineStartIsAComment()
	{
		IReadOnlyList<HighlightedLine> lines = Highlight("# heading\necho hi # note\n  # indented", "bash");

		TokenAssert.HasToken(lines, "# heading", TokenKind.Comment);
		TokenAssert.HasToken(lines, "# note", TokenKind.Comment);
		TokenAssert.HasToken(lines, "# indented", TokenKind.Comment);
	}

	[TestMethod]
	public void Yaml_HashInsideAValueIsNotAComment()
	{
		IReadOnlyList<HighlightedLine> lines = Highlight("url: http://example.com/#frag\n# top\nkey: value # c", "yaml");

		Assert.IsFalse(
			lines[0].Tokens.Any(token => token.Kind == TokenKind.Comment),
			$"The fragment started a comment: {TokenAssert.Describe(lines)}");
		TokenAssert.HasToken(lines, "# top", TokenKind.Comment);
		TokenAssert.HasToken(lines, "# c", TokenKind.Comment);
	}

	[TestMethod]
	public void Python_HashAnywhereIsStillAComment()
	{
		IReadOnlyList<HighlightedLine> lines = Highlight("x = 1#note", "python");

		TokenAssert.HasToken(lines, "#note", TokenKind.Comment);
	}

	[TestMethod]
	public void TokensAlwaysReconstructTheSource()
	{
		string code = "public void Run() // go\n{\n\tvar s = \"x\";\n}\n";
		IReadOnlyList<HighlightedLine> lines = Highlight(code, "csharp");

		TokenAssert.Reconstructs(lines, "public void Run() // go\n{\n    var s = \"x\";\n}");
	}
}
