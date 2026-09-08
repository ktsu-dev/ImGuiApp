// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.SyntaxHighlighting.Tests;

using System;
using System.Collections.Generic;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class LanguageRegistryTests
{
	[TestMethod]
	public void AliasesResolveToTheirLanguage()
	{
		Assert.AreEqual("csharp", LanguageRegistry.Resolve("cs").Name);
		Assert.AreEqual("csharp", LanguageRegistry.Resolve("C#").Name);
		Assert.AreEqual("javascript", LanguageRegistry.Resolve("JS").Name);
		Assert.AreEqual("shell", LanguageRegistry.Resolve("bash").Name);
	}

	[TestMethod]
	public void NamesAreMatchedIgnoringCaseAndSurroundingSpace()
	{
		Assert.AreEqual("python", LanguageRegistry.Resolve(" Python ").Name);
	}

	[TestMethod]
	public void UnknownAndMissingNamesFallBackToPlainText()
	{
		Assert.AreEqual(BuiltInLanguages.PlainText, LanguageRegistry.Resolve("klingon"));
		Assert.AreEqual(BuiltInLanguages.PlainText, LanguageRegistry.Resolve(null));
		Assert.AreEqual(BuiltInLanguages.PlainText, LanguageRegistry.Resolve(string.Empty));
	}

	[TestMethod]
	public void TryGetReportsWhetherTheNameWasKnown()
	{
		Assert.IsTrue(LanguageRegistry.TryGet("json", out LanguageDefinition json));
		Assert.AreEqual("json", json.Name);
		Assert.IsFalse(LanguageRegistry.TryGet("klingon", out _));
	}

	[TestMethod]
	public void EveryBuiltInIsRegistered()
	{
		IReadOnlyList<string> names = LanguageRegistry.Names;
		foreach (LanguageDefinition language in BuiltInLanguages.All)
		{
			Assert.Contains(language.Name, names);
		}
	}

	[TestMethod]
	public void ACustomLanguageCanBeRegisteredAndUsed()
	{
		LanguageDefinition ini = new()
		{
			Name = "test-ini",
			Aliases = ["test-conf"],
			LineComments = [new LineCommentRule { Prefix = ";" }],
			HighlightPropertyNames = false,
			Constants = ["yes", "no"],
		};

		LanguageRegistry.Register(ini);

		Assert.AreEqual("test-ini", LanguageRegistry.Resolve("test-conf").Name);
		TokenAssert.HasToken(ImGuiSyntaxHighlighting.Highlight("; note\nflag = yes", "test-ini"), "; note", TokenKind.Comment);
		TokenAssert.HasToken(ImGuiSyntaxHighlighting.Highlight("; note\nflag = yes", "test-ini"), "yes", TokenKind.Constant);
	}

	[TestMethod]
	public void AnAliasNeverStealsAnotherLanguagesName()
	{
		LanguageDefinition impostor = new()
		{
			Name = "test-impostor",
			Aliases = ["csharp"],
		};

		LanguageRegistry.Register(impostor);

		Assert.AreEqual("csharp", LanguageRegistry.Resolve("csharp").Name);
	}

	[TestMethod]
	public void RegisteringNullThrows() =>
		Assert.ThrowsExactly<ArgumentNullException>(() => LanguageRegistry.Register(null!));
}
