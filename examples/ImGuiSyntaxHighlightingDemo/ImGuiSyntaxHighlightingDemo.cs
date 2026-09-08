// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Examples.SyntaxHighlighting;

using System.Collections.Generic;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.App;
using ktsu.ImGui.Markdown;
using ktsu.ImGui.Probes;
using ktsu.ImGui.SyntaxHighlighting;
using ktsu.SyntaxHighlighting;

internal static class ImGuiSyntaxHighlightingDemo
{
	/// <summary>One entry in the language picker: the label shown, the language name, and its sample.</summary>
	/// <param name="Label">The name shown in the picker.</param>
	/// <param name="Language">The language name passed to the highlighter.</param>
	/// <param name="Sample">The code rendered for this entry.</param>
	internal sealed record Snippet(string Label, string Language, string Sample);

	/// <summary>The name of the ImGui window the demo renders into.</summary>
	internal const string WindowTitle = "Syntax Highlighting";

	/// <summary>The snippets the demo can show. Internal so a UI test asserts against these, not a copy.</summary>
	internal static IReadOnlyList<Snippet> Snippets { get; } =
	[
		new Snippet("C#", "csharp", """
			// Renders a highlighted snippet.
			public sealed record Snippet(string Language, string Code)
			{
				/// <summary>Draws the snippet.</summary>
				public void Render(SyntaxHighlightConfig config)
				{
					ImGuiSyntaxHighlighting.Render(Code, Language, config);
				}
			}
			"""),
		new Snippet("Python", "python", """"
			import math

			def circle_area(radius: float) -> float:
			    """Area of a circle, or zero for a negative radius."""
			    if radius < 0:
			        return 0.0
			    return math.pi * radius ** 2

			print(circle_area(2.5))
			""""),
		new Snippet("JSON", "json", """
			{
			  "name": "ktsu.ImGui.SyntaxHighlighting",
			  "version": "1.0.0",
			  "languages": ["csharp", "python", "json", "xml", "sql"],
			  "lineNumbers": true,
			  "theme": null
			}
			"""),
		new Snippet("XML", "xml", """
			<?xml version="1.0" encoding="utf-8"?>
			<!-- The project file of this very library. -->
			<Project>
			  <Sdk Name="ktsu.Sdk" />
			  <PropertyGroup>
			    <TargetFrameworks>net10.0;net9.0;net8.0</TargetFrameworks>
			  </PropertyGroup>
			</Project>
			"""),
		new Snippet("SQL", "sql", """
			-- Busiest languages in the sample corpus.
			SELECT language, COUNT(*) AS uses
			FROM snippets
			WHERE length > 0
			GROUP BY language
			ORDER BY uses DESC
			LIMIT 10;
			"""),
		new Snippet("Shell", "shell", """
			#!/usr/bin/env bash
			set -euo pipefail

			for project in ImGui.*/; do
				echo "building ${project}"
				dotnet build "${project}" --configuration Release
			done
			"""),
	];

	/// <summary>
	/// The snippets on the embedded-language tab. Each holds another language inside a comment or a
	/// string, which the highlighter finds without being told where to look.
	/// </summary>
	internal static IReadOnlyList<Snippet> EmbeddedSnippets { get; } =
	[
		new Snippet("C# host", "csharp", """"
			/// <summary>Posts an <see cref="Order"/> and returns the receipt.</summary>
			/// <param name="order">The order to post.</param>
			public Receipt Post(Order order)
			{
				// Doc comment tags are XML; the prose between them stays comment-colored.
				string body = """{"id": 7, "items": ["pen", "ink"], "paid": true}""";
				string query = "SELECT id, total FROM receipts WHERE order_id = @id";

				// lang=sql
				string tail = "ORDER BY total DESC";

				return Send(body, query + tail);
			}
			""""),
		new Snippet("JSON host", "json", """
			{
			  "name": "embedded",
			  "payload": "{\"retries\": 3, \"verbose\": false}",
			  "template": "<row id='1'><cell>ok</cell></row>"
			}
			"""),
	];

	/// <summary>The markdown rendered on the markdown tab, whose fences route through the highlighter.</summary>
	internal const string MarkdownSample = """
		# Highlighted markdown

		`ImGui.Markdown` draws fenced code blocks through its `CodeBlockRenderer` hook, so a
		markdown document picks up highlighting without either library depending on the other.

		```csharp
		public static void Render(string code, string language) =>
			ImGuiSyntaxHighlighting.Render(code, language);
		```

		A fence with no language falls back to plain, unstyled text:

		```
		just some text
		```
		""";

	/// <summary>The index of the snippet currently shown.</summary>
	internal static int SelectedSnippet { get; set; }

	/// <summary>Whether the line-number gutter is shown.</summary>
	internal static bool ShowLineNumbers { get; set; } = true;

	/// <summary>The palette: 0 follows the ImGui theme, 1 forces dark, 2 forces light.</summary>
	internal static int SelectedPalette { get; set; }

	/// <summary>
	/// Returns every piece of demo state to its starting value. The demo keeps its state in statics,
	/// which outlive a harness, so a test that ran before this one would otherwise decide what this
	/// one starts from.
	/// </summary>
	internal static void ResetState()
	{
		SelectedSnippet = 0;
		ShowLineNumbers = true;
		SelectedPalette = 0;
	}

	/// <summary>
	/// Builds the configuration the demo runs on. Extracted from <c>Main</c> so a UI test drives the
	/// real configuration rather than a parallel one written for testing.
	/// </summary>
	/// <returns>The application configuration.</returns>
	internal static ImGuiAppConfig BuildConfig() => new()
	{
		Title = "ImGui.SyntaxHighlighting - Demo",
		OnRender = _ => RenderWindow(),
		SaveIniSettings = false,
	};

	/// <summary>The highlighting configuration built from the demo's current toggles.</summary>
	/// <returns>The configuration passed to the highlighter.</returns>
	internal static SyntaxHighlightConfig BuildHighlightConfig() => new()
	{
		ShowLineNumbers = ShowLineNumbers,
		Theme = SelectedPalette switch
		{
			1 => SyntaxTheme.Dark,
			2 => SyntaxTheme.Light,
			_ => null,
		},
	};

	/// <summary>The markdown configuration that routes fenced code blocks through the highlighter.</summary>
	/// <returns>The configuration the markdown tab renders with.</returns>
	internal static MarkdownConfig BuildMarkdownConfig() => new()
	{
		CodeBlockRenderer = (language, code) =>
			ImGuiSyntaxHighlighting.Render(code, language ?? "text", BuildHighlightConfig()),
	};

	private static void Main() => ImGuiApp.Start(BuildConfig());

	private static void RenderWindow()
	{
		// Without a starting size the window opens at ImGui's 32px default, which leaves almost no
		// content width for a code block that does not wrap.
		ImGui.SetNextWindowSize(new Vector2(820, 640), ImGuiCond.FirstUseEver);
		ImGui.Begin(WindowTitle);

		RenderControls();
		ImGui.Separator();

		if (ImGui.BeginTabBar("##tabs"))
		{
			if (DemoTab("Snippets"))
			{
				RenderSnippetTab();
				ImGui.EndTabItem();
			}

			if (DemoTab("Embedded"))
			{
				RenderEmbeddedTab();
				ImGui.EndTabItem();
			}

			if (DemoTab("In markdown"))
			{
				ImGuiMarkdown.Render(MarkdownSample, BuildMarkdownConfig());
				ImGui.EndTabItem();
			}

			ImGui.EndTabBar();
		}

		ImGui.End();
	}

	private static void RenderControls()
	{
		bool lineNumbers = ShowLineNumbers;
		if (DemoCheckbox("Line numbers", ref lineNumbers))
		{
			ShowLineNumbers = lineNumbers;
		}

		ImGui.SameLine();
		ImGui.TextUnformatted("Palette:");
		foreach ((string label, int index) in new[] { ("Follow theme", 0), ("Dark", 1), ("Light", 2) })
		{
			ImGui.SameLine();
			if (DemoRadio(label, SelectedPalette == index))
			{
				SelectedPalette = index;
			}
		}
	}

	private static void RenderEmbeddedTab()
	{
		ImGui.TextWrapped(
			"XML, JSON and SQL are recognized inside a host language's comments and strings. A "
			+ "'// lang=<name>' comment names the language of the next string outright.");
		ImGui.Spacing();

		foreach (Snippet snippet in EmbeddedSnippets)
		{
			ImGui.TextUnformatted(snippet.Label);

			Vector2 contentMin = ImGui.GetCursorScreenPos();
			float contentWidth = ImGui.GetContentRegionAvail().X;
			ImGuiSyntaxHighlighting.Render(snippet.Sample, snippet.Language, BuildHighlightConfig());
			Vector2 contentMax = new(contentMin.X + contentWidth, ImGui.GetCursorScreenPos().Y);
			ImGuiProbes.MarkRegion($"embedded/{snippet.Label}", contentMin, contentMax);

			ImGui.Spacing();
		}
	}

	private static void RenderSnippetTab()
	{
		for (int index = 0; index < Snippets.Count; index++)
		{
			if (index > 0)
			{
				ImGui.SameLine();
			}

			if (DemoRadio(Snippets[index].Label, SelectedSnippet == index))
			{
				SelectedSnippet = index;
			}
		}

		ImGui.Spacing();

		Snippet snippet = Snippets[SelectedSnippet];

		// The highlighter paints into the draw list rather than submitting named items, so recording
		// the span it occupied gives a test a stable handle on "the rendered code".
		Vector2 contentMin = ImGui.GetCursorScreenPos();
		float contentWidth = ImGui.GetContentRegionAvail().X;
		ImGuiSyntaxHighlighting.Render(snippet.Sample, snippet.Language, BuildHighlightConfig());
		Vector2 contentMax = new(contentMin.X + contentWidth, ImGui.GetCursorScreenPos().Y);
		ImGuiProbes.MarkRegion("code", contentMin, contentMax);
	}

	/// <summary>Submits a tab and records it for probing, so a test can switch tabs by label.</summary>
	private static bool DemoTab(string label)
	{
		bool open = ImGui.BeginTabItem(label);
		ImGuiProbes.MarkItem(label);
		return open;
	}

	/// <summary>Submits a checkbox and records it for probing.</summary>
	private static bool DemoCheckbox(string label, ref bool value)
	{
		bool changed = ImGui.Checkbox(label, ref value);
		ImGuiProbes.MarkItem(label);
		return changed;
	}

	/// <summary>Submits a radio button and records it for probing.</summary>
	private static bool DemoRadio(string label, bool active)
	{
		bool clicked = ImGui.RadioButton(label, active);
		ImGuiProbes.MarkItem(label);
		return clicked;
	}
}
