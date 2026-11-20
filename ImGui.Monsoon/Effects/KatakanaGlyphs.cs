// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Monsoon.Effects;

using System.Collections.Frozen;
using System.Globalization;
using System.Numerics;
using System.Text;

/// <summary>One stroke of a glyph: a polyline on the glyph's 8×8 grid, y pointing down.</summary>
/// <param name="Points">The points, in grid units.</param>
internal readonly record struct GlyphStroke(IReadOnlyList<Vector2> Points);

/// <summary>
/// Katakana drawn as strokes, so a sign can be written without a CJK font.
/// </summary>
/// <remarks>
/// The strokes are hand-authored on an 8×8 grid and are deliberately simple: they are meant to read
/// as neon tubing, not as typography. Voiced kana are drawn as the base glyph plus the dakuten or
/// handakuten mark, and small kana are the full glyph shrunk towards the bottom left. The voiced
/// forms come from a table rather than <see cref="string.Normalize()"/>, which does not decompose
/// kana under invariant globalization; a separately written combining mark is folded into the
/// glyph before it.
/// </remarks>
internal static class KatakanaGlyphs
{
	/// <summary>The width and height of the grid every stroke is drawn on.</summary>
	internal const float GridSize = 8f;

	/// <summary>The gap between glyphs, as a share of the glyph's height.</summary>
	internal const float Gap = 0.15f;

	/// <summary>The combining voiced sound mark (dakuten).</summary>
	internal const char Dakuten = '゙';

	/// <summary>The combining semi-voiced sound mark (handakuten).</summary>
	internal const char Handakuten = '゚';

	private const float SmallScale = 0.7f;

	private static readonly FrozenDictionary<char, string> SmallKana = new Dictionary<char, string>
	{
		['ァ'] = "ア",
		['ィ'] = "イ",
		['ゥ'] = "ウ",
		['ェ'] = "エ",
		['ォ'] = "オ",
		['ッ'] = "ツ",
		['ャ'] = "ヤ",
		['ュ'] = "ユ",
		['ョ'] = "ヨ",
		['ヮ'] = "ワ",
	}.ToFrozenDictionary();

	private static readonly FrozenDictionary<char, GlyphStroke[]> Strokes = BuildStrokes();

	private static readonly FrozenDictionary<char, (char Base, char Mark)> Voiced = BuildVoiced();

	/// <summary>Gets every character with strokes of its own, marks included.</summary>
	internal static IEnumerable<char> Covered => Strokes.Keys;

	/// <summary>Whether <paramref name="c"/> can be drawn, directly, as small kana, or decomposed.</summary>
	/// <param name="c">The character.</param>
	/// <returns>True when <see cref="Layout"/> draws it as strokes.</returns>
	internal static bool CanDraw(char c) =>
		Strokes.ContainsKey(c) || SmallKana.ContainsKey(c) || Voiced.ContainsKey(c);

	/// <summary>
	/// Lays <paramref name="text"/> out as glyph cells: for each character, the strokes to draw (in
	/// grid units, already scaled and placed within the cell) or none when it cannot be drawn.
	/// </summary>
	/// <param name="text">The text.</param>
	/// <returns>One cell per character of the composed text, in order.</returns>
	internal static IReadOnlyList<GlyphCell> Layout(string text)
	{
		Ensure.NotNull(text);
		List<GlyphCell> cells = new(text.Length);
		foreach (char c in text)
		{
			bool isMark = c is Dakuten or Handakuten;
			if (isMark && cells.Count > 0 && cells[^1].Strokes is { } previous)
			{
				cells[^1] = cells[^1] with { Strokes = [.. previous, .. Strokes[c]] };
				continue;
			}

			cells.Add(new GlyphCell(c, StrokesFor(c)));
		}

		return cells;
	}

	/// <summary>Gets the advance from one glyph to the next, for a glyph <paramref name="height"/> tall.</summary>
	/// <param name="height">The glyph height in pixels.</param>
	/// <returns>The advance in pixels.</returns>
	internal static float Advance(float height) => height * (1f + Gap);

	private static GlyphStroke[]? StrokesFor(char c)
	{
		if (Strokes.TryGetValue(c, out GlyphStroke[]? own))
		{
			return own;
		}

		if (SmallKana.TryGetValue(c, out string? big))
		{
			return [.. Strokes[big[0]].Select(Shrink)];
		}

		if (Voiced.TryGetValue(c, out (char Base, char Mark) parts))
		{
			return [.. Strokes[parts.Base], .. Strokes[parts.Mark]];
		}

		return null;
	}

	private static FrozenDictionary<char, (char Base, char Mark)> BuildVoiced()
	{
		// In the katakana block each voiced form follows its base: +1 for the dakuten, and +2 for
		// the handakuten in the ha row.
		Dictionary<char, (char Base, char Mark)> voiced = new()
		{
			['ヴ'] = ('ウ', Dakuten),
			['ヷ'] = ('ワ', Dakuten),
			['ヺ'] = ('ヲ', Dakuten),
		};

		foreach (char b in "カキクケコサシスセソタチツテトハヒフヘホ")
		{
			voiced[(char)(b + 1)] = (b, Dakuten);
		}

		foreach (char b in "ハヒフヘホ")
		{
			voiced[(char)(b + 2)] = (b, Handakuten);
		}

		return voiced.ToFrozenDictionary();
	}

	private static GlyphStroke Shrink(GlyphStroke stroke) =>
		new([.. stroke.Points.Select(p => new Vector2(p.X * SmallScale, GridSize - ((GridSize - p.Y) * SmallScale)))]);

	private static FrozenDictionary<char, GlyphStroke[]> BuildStrokes()
	{
		Dictionary<char, string> source = new()
		{
			['ア'] = "1,1 7,1 6,2.5 4.5,3.5|4,2.5 4,5 3,6.5 1.5,7.5",
			['イ'] = "6,.5 4,3 1,5|4.5,2.8 4.5,7.5",
			['ウ'] = "4,.3 4,1.5|1.5,3.5 1.5,1.8 6.5,1.8 6.5,3.5 5,6 2.5,7.5",
			['エ'] = "2,1.5 6,1.5|4,1.5 4,6.5|1,6.5 7,6.5",
			['オ'] = "1,2.5 7,2.5|5,.5 5,7 4,6.5|4.5,3.5 1,6.5",
			['カ'] = "1.5,2.5 6.5,2.5 6.5,6 5.5,7.2 4.5,6.5|3.5,.5 3.2,4 1.5,7.3",
			['キ'] = "1.5,2.5 6.5,2|1,4.5 7,4|3.5,.5 4.5,7.5",
			['ク'] = "3.5,.5 1.5,3.5|3,1.8 6.5,1.8 5.5,4.5 2,7.5",
			['ケ'] = "2.5,.5 1,3.5|2,2.5 7,2.5|5,2.5 4.5,5 2.5,7.5",
			['コ'] = "1.5,1.5 6.5,1.5 6.5,6.5|1.5,6.5 6.5,6.5",
			['サ'] = "1,2.5 7,2.5|2.5,.5 2.5,4.5|5.5,.5 5.5,4.5 4,6.5 2.5,7.5",
			['シ'] = "1.5,1 3,2|1,3.5 2.5,4.5|1.5,7.3 4.5,5.5 7,2",
			['ス'] = "1.5,1.5 6.5,1.5 4.5,4.5 1,7.5|4.5,4.5 7,7",
			['セ'] = "1,3 7,2.2 5.5,4.5|3,.5 3,6.5 3.5,7 6.5,7",
			['ソ'] = "1.5,1.5 2.5,3.5|6.5,1 5.5,4.5 2.5,7.5",
			['タ'] = "3.5,.5 1.5,3.5|3,1.8 6.5,1.8 5.5,4.5 2,7.5|2.5,3.5 5.2,4.8",
			['チ'] = "6,.5 2,1.8|1,3.5 7,3.5|4,1.4 4,5.5 2.5,7.5",
			['ツ'] = "1,1.5 1.8,3|3.2,1 4,2.5|7,1 5.5,4.8 2.5,7.5",
			['テ'] = "2,1 6,1|1,3 7,3|4,3 4,5.5 2.5,7.5",
			['ト'] = "2.5,.5 2.5,7.5|2.5,3.5 6,5",
			['ナ'] = "1,2.5 7,2.5|4.5,.5 4.5,4.5 3.5,6.3 2,7.5",
			['ニ'] = "2,2 6,2|1,6.5 7,6.5",
			['ヌ'] = "1.5,1.5 6.5,1.5 4.5,4.5 1,7.5|2.5,3.8 6,6.5",
			['ネ'] = "4,.3 4,1.5|1.5,1.8 6.5,1.8 1,6|4,4 4,7.5|5,4.5 7,6",
			['ノ'] = "6.5,.8 5,4 1.5,7.5",
			['ハ'] = "3,1.5 1,6.5|5,1.5 7,6.5",
			['ヒ'] = "2,.8 2,6.8 7,6.8|2,3.5 6.5,2.5",
			['フ'] = "1.5,1.5 6.5,1.5 5.5,4.5 2.5,7.5",
			['ヘ'] = ".5,5 3,2.5 7.5,6.5",
			['ホ'] = "1,2.5 7,2.5|4,.5 4,7.5 3.2,7|2.5,4 1,6.5|5.5,4 7,6.5",
			['マ'] = "1,1.5 7,1.5 4,5|3,3.8 5,6.5",
			['ミ'] = "2,1 6,2|2.5,3.5 5.5,4.5|1.5,5.8 6.5,7.3",
			['ム'] = "3.5,.5 1,6.8 6.5,6|5,4 7,7.3",
			['メ'] = "6,.8 4,4.5 1,7.3|2,2.5 6,6",
			['モ'] = "1.5,1.2 6.5,1.2|1,3.8 7,3.8|3.5,1.2 3.5,6.5 4.5,7 7,7",
			['ヤ'] = "1,3 7,2 5.5,4.5|2.5,.5 4.5,7.5",
			['ユ'] = "1.5,2 5.5,2 5.5,6.5|.5,6.5 7.5,6.5",
			['ヨ'] = "1.5,1 6.5,1 6.5,7|1.5,4 6.5,4|1.5,7 6.5,7",
			['ラ'] = "2,.8 6,.8|1.5,3 6.5,3 5.5,5.5 2.5,7.5",
			['リ'] = "2,1 2,5|6,.5 6,4.5 4.5,6.5 2.5,7.5",
			['ル'] = "2.5,1 2.5,4.5 1,7|4.5,.5 4.5,7 7,5",
			['レ'] = "2,.8 2,7 7,4",
			['ロ'] = "1.5,1.5 1.5,6.5|1.5,1.5 6.5,1.5 6.5,6.5|1.5,6.5 6.5,6.5",
			['ワ'] = "1.5,3 1.5,1.5 6.5,1.5 5.5,4.5 2.5,7.5",
			['ヲ'] = "1.5,1.5 6.5,1.5 5.5,4.5 2.5,7.5|1.5,3.5 6,3.5",
			['ン'] = "1.5,1.5 3,2.5|1.5,7.3 4.5,5.5 7,2",
			['ー'] = ".8,4 7.2,4",
			['・'] = "3.6,4 4.4,4",
			[Dakuten] = "6.2,0 6.8,1.2|7.4,-.2 8,1",
			[Handakuten] = Octagon(7f, 0.7f, 0.8f),
		};

		return source.ToFrozenDictionary(entry => entry.Key, entry => Parse(entry.Value));
	}

	private static string Octagon(float cx, float cy, float r)
	{
		StringBuilder builder = new();
		for (int i = 0; i <= 8; i++)
		{
			double angle = i * Math.PI / 4d;
			if (i > 0)
			{
				builder.Append(' ');
			}

			builder.Append(((float)(cx + (r * Math.Cos(angle)))).ToString(CultureInfo.InvariantCulture));
			builder.Append(',');
			builder.Append(((float)(cy + (r * Math.Sin(angle)))).ToString(CultureInfo.InvariantCulture));
		}

		return builder.ToString();
	}

	private static GlyphStroke[] Parse(string strokes) =>
		[.. strokes.Split('|')
			.Select(stroke => new GlyphStroke([.. stroke.Split(' ')
				.Select(point =>
				{
					string[] xy = point.Split(',');
					return new Vector2(
						float.Parse(xy[0], CultureInfo.InvariantCulture),
						float.Parse(xy[1], CultureInfo.InvariantCulture));
				})]))];
}

/// <summary>One character laid out for drawing.</summary>
/// <param name="Character">The character.</param>
/// <param name="Strokes">Its strokes on the 8×8 grid, or null when it has none and is drawn as plain text.</param>
internal readonly record struct GlyphCell(char Character, IReadOnlyList<GlyphStroke>? Strokes);
