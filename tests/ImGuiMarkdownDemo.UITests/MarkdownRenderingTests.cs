// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.examples.ImGuiMarkdownDemo.UITests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.App;
using ktsu.ImGui.App.Testing;
using ktsu.ImGui.Color;
using ktsu.ImGui.Markdown;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Renders small documents through the real renderer, headlessly, and checks what they draw and how
/// they respond to the pointer: links, linked images, image placeholders, inline code and fonts.
/// </summary>
[TestClass]
public sealed class MarkdownRenderingTests
{
	private const float PlaceholderWidth = 120.0f;

	private const int LogoSize = 64;

	// Links draw in this colour, so a link is found in a frame by its pixels.
	private static readonly ImGuiVector4 LinkMagenta = new(1.0f, 0.0f, 1.0f, 1.0f);

	private readonly List<string> clicked = [];

	private ImGuiAppHarness? harness;

	private Vector2 origin;

	private ImGuiAppTextureInfo? logo;

	private ImGuiAppHarness Harness => harness ?? throw new InvalidOperationException("Start a document first.");

	[TestCleanup]
	public void DisposeHarness()
	{
		harness?.Dispose();
		harness = null;
	}

	[TestMethod]
	public void LinksInSeveralParagraphsAndCells_HaveNoConflictingIds()
	{
		// #684: link IDs restarted from zero in every block, so the first link of each paragraph and
		// each table cell shared one ID, and hovering any of them raised ImGui's ID conflict overlay.
		Show("""
			[first](https://a.example)

			[second](https://b.example)

			| A | B |
			|---|---|
			| [x](https://x.example) | [y](https://y.example) |
			""");

		List<Rectangle> links = FindLinks();
		Assert.HasCount(4, links, "Expected two paragraph links and two linked cells.");

		foreach (Rectangle link in links)
		{
			Harness.Mouse.MoveTo(CenterX(link), CenterY(link));
			Harness.Step(2);
			Assert.AreEqual(ImGuiMouseCursor.Hand, Harness.MouseCursor, "The pointer is not over a link.");
			Assert.AreEqual(0, ImGui.GetCurrentContext().DebugDrawIdConflictsCount, "Hovering a link raised an ID conflict.");
		}

		foreach (Rectangle link in links)
		{
			Harness.Mouse.Click(CenterX(link), CenterY(link));
		}

		Assert.AreEqual(
			"https://a.example https://b.example https://x.example https://y.example",
			string.Join(' ', clicked),
			"Each link should report its own URL, once.");
	}

	[TestMethod]
	public void ALinkedPlaceholderImage_IsClickable()
	{
		// #685: an image inside a link drew and returned before the link's hit-test, so it never clicked.
		Show("[![Build status](https://img.example/badge.svg)](https://example.com/actions) done");

		Harness.Mouse.MoveTo(origin.X + (PlaceholderWidth / 2), origin.Y + 4);
		Harness.Step(2);
		Assert.AreEqual(ImGuiMouseCursor.Hand, Harness.MouseCursor);

		Harness.Mouse.Click(origin.X + (PlaceholderWidth / 2), origin.Y + 4);
		Assert.AreEqual("https://example.com/actions", string.Join(' ', clicked));
	}

	[TestMethod]
	public void ALinkedResolvedImage_IsClickable()
	{
		Show("[![logo](logo.png)](https://example.com/home)");

		// The 64 px logo is the first thing in the document, so it starts at the origin.
		Harness.Mouse.Click(origin.X + 32, origin.Y + 32);
		Assert.AreEqual("https://example.com/home", string.Join(' ', clicked));
	}

	[TestMethod]
	public void AnImageWithNoLink_StaysInert()
	{
		Show("![logo](logo.png)");

		Harness.Mouse.MoveTo(origin.X + 32, origin.Y + 32);
		Harness.Step(2);
		Assert.AreNotEqual(ImGuiMouseCursor.Hand, Harness.MouseCursor);

		Harness.Mouse.Click(origin.X + 32, origin.Y + 32);
		Assert.IsEmpty(clicked);
	}

	[TestMethod]
	public void AnUnresolvedImage_KeepsItsCaptionInsideThePlaceholder()
	{
		// #686: the placeholder drew the image's whole URL, unclipped, over whatever followed it. The
		// caption is linked here only so it draws in the colour this test looks for.
		Show("[![A description much too long to fit inside the box](https://img.example/a/very/long/path/to/a/badge.svg)](https://example.com)");

		Rectangle? caption = Harness.Capture().FindBounds(IsLinkPixel);
		Assert.IsNotNull(caption, "The placeholder drew no caption.");
		Assert.IsLessThanOrEqualTo(origin.X + PlaceholderWidth + 1, caption.Value.MaxX, "The caption ran past the placeholder.");
	}

	[TestMethod]
	public void InlineCodeWithSpaces_DrawsOneChip()
	{
		// #687: a code span was split at its spaces, so "x   = 1" drew as three shaded chips.
		Show("`x   = 1`");

		CapturedFrame frame = Harness.Capture();
		float textWidth = ImGui.CalcTextSize("x   = 1").X;

		// Glyphs sit on the bluish code background, so a column of the line that is bluer anywhere than
		// the window beside it is shaded. Separate chips leave columns of bare window between them.
		int top = (int)origin.Y;
		int bottom = top + (int)ImGui.GetFontSize();
		int runs = 0;
		int first = -1;
		int last = -1;
		bool inRun = false;
		for (int x = (int)origin.X - 4; x < (int)(origin.X + textWidth + 24); x++)
		{
			bool shaded = false;
			for (int y = top; y <= bottom && !shaded; y++)
			{
				shaded = frame.GetPixel(x, y).B - frame.GetPixel((int)origin.X + 400, y).B > 12;
			}

			if (shaded && !inRun)
			{
				runs++;
				first = first < 0 ? x : first;
			}

			last = shaded ? x : last;
			inRun = shaded;
		}

		Assert.AreEqual(1, runs, "The code span should sit on one continuous background.");

		// Split chips can still touch, since each is padded, but they close up the spaces between them.
		// Kept whole, the span is as wide as its text with every space in it, plus its padding.
		Assert.IsGreaterThanOrEqualTo(textWidth + 2, last - first + 1, "The span lost its interior spaces.");
	}

	[TestMethod]
	public void HeaderCellsAndHeadings_ResolveTheirOwnFonts()
	{
		// #676: every token pushed a font for its own role, plain text took Body, and the header row's
		// Bold and the heading's H1 were replaced token by token.
		HashSet<MarkdownFontRole> requested = [];
		Show(
			"""
			# Title

			| Head |
			|------|
			""",
			(role, _) =>
			{
				requested.Add(role);
				return null;
			});

		Assert.AreEqual("Bold H1", string.Join(' ', requested.Order()));
	}

	private void Show(string markdown, Func<MarkdownFontRole, float, ImFontPtr?>? fontResolver = null)
	{
		MarkdownConfig config = new()
		{
			LinkColor = LinkMagenta,
			FontResolver = fontResolver,
			OnLinkClicked = clicked.Add,
			ImageResolver = src => src == "logo.png" ? new MarkdownImageResult((logo ??= CreateLogo()).TextureId, new Vector2(LogoSize)) : null,
		};

		harness = ImGuiAppHarness.Start(
			new ImGuiAppConfig
			{
				Title = nameof(MarkdownRenderingTests),
				SaveIniSettings = false,
				OnRender = _ =>
				{
					ImGui.SetNextWindowPos(Vector2.Zero);
					ImGui.SetNextWindowSize(new Vector2(800, 600));
					ImGui.Begin("Document", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoSavedSettings);
					origin = ImGui.GetCursorScreenPos();
					ImGuiMarkdown.Render(markdown, config);
					ImGui.End();
				},
			},
			new HarnessOptions());

		harness.Step(3);
	}

	private static ImGuiAppTextureInfo CreateLogo()
	{
		byte[] rgba = new byte[LogoSize * LogoSize * 4];
		Array.Fill<byte>(rgba, 255);
		return ImGuiApp.CreateTexture(rgba, LogoSize, LogoSize);
	}

	private static bool IsLinkPixel(Rgba32 p) => p.R > 160 && p.G < 90 && p.B > 160;

	/// <summary>Finds each link by its colour: rows of link pixels form a line, and gaps in a line separate links.</summary>
	private List<Rectangle> FindLinks()
	{
		CapturedFrame frame = Harness.Capture();
		List<Rectangle> links = [];

		int y = 0;
		while (y < frame.Height)
		{
			if (!RowHasLink(frame, y))
			{
				y++;
				continue;
			}

			// A link's underline can sit a row or two below its glyphs, so a short gap stays in the line.
			int top = y;
			int bottom = y;
			while (y < frame.Height && y - bottom <= 3)
			{
				if (RowHasLink(frame, y))
				{
					bottom = y;
				}

				y++;
			}
			int? start = null;
			int last = 0;
			for (int x = 0; x < frame.Width; x++)
			{
				bool any = false;
				for (int r = top; r <= bottom && !any; r++)
				{
					any = IsLinkPixel(frame.GetPixel(x, r));
				}

				if (!any)
				{
					continue;
				}

				if (start is not null && x - last > 12)
				{
					links.Add(new Rectangle(start.Value, top, last, bottom));
					start = null;
				}

				start ??= x;
				last = x;
			}

			if (start is not null)
			{
				links.Add(new Rectangle(start.Value, top, last, bottom));
			}
		}

		return links;
	}

	private static bool RowHasLink(CapturedFrame frame, int y)
	{
		for (int x = 0; x < frame.Width; x++)
		{
			if (IsLinkPixel(frame.GetPixel(x, y)))
			{
				return true;
			}
		}

		return false;
	}

	private static float CenterX(Rectangle r) => (r.MinX + r.MaxX) / 2f;

	private static float CenterY(Rectangle r) => (r.MinY + r.MaxY) / 2f;
}
