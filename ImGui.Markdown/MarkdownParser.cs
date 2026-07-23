// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Markdown;

using System.Collections.Generic;

using Markdig;

using MarkdigAst = Markdig.Syntax.MarkdownDocument;

/// <summary>
/// Parses markdown into a Markdig AST and caches parses keyed by source string so the
/// immediate-mode render loop does not re-parse unchanged text every frame.
/// </summary>
internal static class MarkdownParser
{
	private const int MaxCacheEntries = 32;

	private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
		.UsePipeTables()
		.UseTaskLists()
		.UseAutoLinks()
		.Build();

	// Insertion-ordered cache; oldest entry is evicted when the cap is exceeded.
	private static readonly Dictionary<string, MarkdigAst> Cache = [];
	private static readonly Queue<string> InsertionOrder = new();
#if NET10_0_OR_GREATER
	private static readonly System.Threading.Lock Gate = new();
#else
	private static readonly object Gate = new();
#endif

	/// <summary>Parses markdown into a fresh AST using the configured pipeline.</summary>
	/// <param name="markdown">The markdown source.</param>
	/// <returns>The parsed document.</returns>
	public static MarkdigAst Parse(string markdown) => Markdown.Parse(markdown ?? string.Empty, Pipeline);

	/// <summary>Returns a cached AST for the source, parsing and caching it on first use.</summary>
	/// <param name="markdown">The markdown source.</param>
	/// <returns>The cached parsed document.</returns>
	public static MarkdigAst GetOrParse(string markdown)
	{
		string key = markdown ?? string.Empty;
		lock (Gate)
		{
			if (Cache.TryGetValue(key, out MarkdigAst? cached))
			{
				return cached;
			}

			MarkdigAst parsed = Parse(key);
			Cache[key] = parsed;
			InsertionOrder.Enqueue(key);

			while (InsertionOrder.Count > MaxCacheEntries)
			{
				string evict = InsertionOrder.Dequeue();
				Cache.Remove(evict);
			}

			return parsed;
		}
	}
}