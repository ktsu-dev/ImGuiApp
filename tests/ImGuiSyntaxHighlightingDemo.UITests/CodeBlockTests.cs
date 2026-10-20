// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.examples.ImGuiSyntaxHighlightingDemo.UITests;

using System;
using System.Linq;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.App;
using ktsu.ImGui.App.Testing;
using ktsu.ImGui.SyntaxHighlighting;
using ktsu.SyntaxHighlighting;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Draws a code block on its own inside a scrolling child, for the two things the demo cannot show:
/// that a long line can be scrolled to, and that lines out of view are not drawn.
/// </summary>
[TestClass]
public sealed class CodeBlockTests
{
	private static readonly HarnessOptions Viewport = new() { Width = 640, Height = 480 };

	private ImGuiAppHarness? harness;

	[TestCleanup]
	public void TearDown() => harness?.Dispose();

	private void Start(Action draw)
	{
		harness = ImGuiAppHarness.Start(
			new ImGuiAppConfig
			{
				Title = nameof(CodeBlockTests),
				OnRender = _ => draw(),
				SaveIniSettings = false,
			},
			Viewport);

		harness.Step(3);
	}

	[TestMethod]
	public void ALongLine_InAHorizontallyScrollingChild_CanBeScrolledTo()
	{
		float scrollMaxX = 0.0f;
		float scrollX = 0.0f;
		bool scrollToEnd = false;
		string code = new('x', 400);

		Start(() =>
		{
			ImGui.BeginChild("code", new Vector2(300, 200), ImGuiChildFlags.None, ImGuiWindowFlags.HorizontalScrollbar);
			ImGuiSyntaxHighlighting.Render(code, "csharp");

			if (scrollToEnd)
			{
				ImGui.SetScrollX(ImGui.GetScrollMaxX());
			}

			scrollMaxX = ImGui.GetScrollMaxX();
			scrollX = ImGui.GetScrollX();
			ImGui.EndChild();
		});

		Assert.IsGreaterThan(0.0f, scrollMaxX, "A line wider than the child gave it nothing to scroll sideways to.");

		scrollToEnd = true;
		harness!.Step(3);

		Assert.IsGreaterThan(0.0f, scrollX, "The child could not be scrolled towards the end of the line.");
		Assert.AreEqual(scrollMaxX, scrollX, 1.0f);
	}

	[TestMethod]
	public void AShortBlock_FillsTheWidthItWasGiven_AndNoMore()
	{
		float scrollMaxX = -1.0f;
		float blockWidth = 0.0f;

		Start(() =>
		{
			ImGui.BeginChild("code", new Vector2(300, 200), ImGuiChildFlags.None, ImGuiWindowFlags.HorizontalScrollbar);
			float available = ImGui.GetContentRegionAvail().X;
			ImGuiSyntaxHighlighting.Render("int x = 1;", "csharp");
			blockWidth = ImGui.GetItemRectSize().X - available;
			scrollMaxX = ImGui.GetScrollMaxX();
			ImGui.EndChild();
		});

		Assert.AreEqual(0.0f, blockWidth, 0.5f, "A short block should be exactly as wide as the space it was given.");
		Assert.AreEqual(0.0f, scrollMaxX, "A short block made its child scroll sideways.");
	}

	[TestMethod]
	public void LinesFarOutOfView_AreNotDrawn()
	{
		// Dear ImGui culls glyphs outside the clip rectangle itself, so what reached the draw list
		// cannot tell the two apart. What the renderer does per line before that point can: with line
		// numbers on, every line it lays out formats its number, so the bytes one frame allocates
		// follow how many lines it touched.
		HighlightedCode shortBlock = new(Lines(40), "csharp");
		HighlightedCode longBlock = new(Lines(5000), "csharp");
		SyntaxHighlightConfig config = new() { ShowLineNumbers = true };
		HighlightedCode current = shortBlock;
		long allocated = 0;

		Start(() =>
		{
			ImGui.BeginChild("code", new Vector2(400, 200), ImGuiChildFlags.None, ImGuiWindowFlags.None);

			if (current == longBlock)
			{
				ImGui.SetScrollY(ImGui.GetScrollMaxY() / 2.0f);
			}

			long before = GC.GetAllocatedBytesForCurrentThread();
			ImGuiSyntaxHighlighting.Render(current, config);
			allocated = GC.GetAllocatedBytesForCurrentThread() - before;
			ImGui.EndChild();
		});

		long shortAllocated = allocated;

		current = longBlock;
		harness!.Step(3);

		Assert.IsLessThan(
			Math.Max(shortAllocated * 4, 4096),
			allocated,
			$"A block 125 times longer, scrolled to its middle, allocated {allocated} bytes a frame against {shortAllocated}, so it laid out lines nobody can see.");
	}

	private static string Lines(int count) =>
		string.Join('\n', Enumerable.Range(0, count).Select(i => $"int value{i} = {i} + other; // line {i}"));
}
