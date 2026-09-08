// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.SyntaxHighlighting;

using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Resolves language names and aliases to definitions. The built-ins from
/// <see cref="BuiltInLanguages"/> are registered on first use; applications can add their own with
/// <see cref="Register(LanguageDefinition)"/>.
/// </summary>
public static class LanguageRegistry
{
	private static readonly Dictionary<string, LanguageDefinition> ByName =
		new(StringComparer.OrdinalIgnoreCase);

	private static readonly List<LanguageDefinition> Registered = [];

#if NET9_0_OR_GREATER
	private static readonly Lock Gate = new();
#else
	private static readonly object Gate = new();
#endif

	static LanguageRegistry()
	{
		foreach (LanguageDefinition language in BuiltInLanguages.All)
		{
			Add(language);
		}
	}

	/// <summary>The registered definitions, built-ins first and custom registrations in order added.</summary>
	public static IReadOnlyList<LanguageDefinition> Languages
	{
		get
		{
			lock (Gate)
			{
				return [.. Registered];
			}
		}
	}

	/// <summary>The canonical name of every registered language.</summary>
	public static IReadOnlyList<string> Names => [.. Languages.Select(language => language.Name)];

	/// <summary>
	/// Registers a definition, replacing any existing one with the same canonical name. Aliases
	/// already claimed by another language are left pointing at that language.
	/// </summary>
	/// <param name="language">The definition to register.</param>
	public static void Register(LanguageDefinition language)
	{
		Ensure.NotNull(language);
		lock (Gate)
		{
			Add(language);
		}
	}

	/// <summary>Looks up a language by canonical name or alias.</summary>
	/// <param name="name">The name or alias to look up; may be <see langword="null"/>.</param>
	/// <param name="language">The matched definition, when found.</param>
	/// <returns><see langword="true"/> when a definition matched.</returns>
	public static bool TryGet(string? name, out LanguageDefinition language)
	{
		if (string.IsNullOrWhiteSpace(name))
		{
			language = BuiltInLanguages.PlainText;
			return false;
		}

		lock (Gate)
		{
			return ByName.TryGetValue(name.Trim(), out language!);
		}
	}

	/// <summary>
	/// Looks up a language, falling back to <see cref="BuiltInLanguages.PlainText"/> when the name is
	/// unknown, empty, or <see langword="null"/>. Unknown languages render as unstyled text rather
	/// than throwing, because the name usually comes from a markdown fence or a user-selected file.
	/// </summary>
	/// <param name="name">The name or alias to resolve.</param>
	/// <returns>The matched definition, or plain text.</returns>
	public static LanguageDefinition Resolve(string? name) =>
		TryGet(name, out LanguageDefinition language) ? language : BuiltInLanguages.PlainText;

	private static void Add(LanguageDefinition language)
	{
		int existing = Registered.FindIndex(candidate =>
			string.Equals(candidate.Name, language.Name, StringComparison.OrdinalIgnoreCase));

		if (existing >= 0)
		{
			Registered[existing] = language;
		}
		else
		{
			Registered.Add(language);
		}

		ByName[language.Name] = language;
		foreach (string alias in language.Aliases)
		{
			// An alias never displaces another language's entry, so registering "cpp" cannot steal "c".
			bool claimedByOther = ByName.TryGetValue(alias, out LanguageDefinition? claimed)
				&& !string.Equals(claimed.Name, language.Name, StringComparison.OrdinalIgnoreCase);

			if (!claimedByOther)
			{
				ByName[alias] = language;
			}
		}
	}
}
