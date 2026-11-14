// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Monsoon.Tests;

using System.Text.RegularExpressions;

using ktsu.ImGui.Monsoon.Effects;

[TestClass]
public sealed partial class TextTests
{
	/// <summary>Voicing marks sit just above the cell's top edge, so a point may stray this far outside it.</summary>
	private const float Margin = 0.5f;

	[TestMethod]
	public void EveryCoveredGlyphHasStrokesInsideItsGrid()
	{
		Assert.IsGreaterThan(40, KatakanaGlyphs.Covered.Count());
		foreach (char c in KatakanaGlyphs.Covered)
		{
			GlyphCell cell = KatakanaGlyphs.Layout(c.ToString()).Single();
			Assert.IsNotNull(cell.Strokes, $"'{c}' is covered but has no strokes.");
			Assert.IsNotEmpty(cell.Strokes);
			foreach (GlyphStroke stroke in cell.Strokes)
			{
				Assert.IsGreaterThanOrEqualTo(2, stroke.Points.Count);
				foreach (System.Numerics.Vector2 p in stroke.Points)
				{
					Assert.IsTrue(p.X is >= -Margin and <= KatakanaGlyphs.GridSize + Margin && p.Y is >= -Margin and <= KatakanaGlyphs.GridSize + Margin, $"'{c}' has a point {p} outside its grid.");
				}
			}
		}
	}

	[TestMethod]
	public void TheDemoTitlesCanBeDrawn()
	{
		foreach (string text in new[] { "モンスーン", "データ", "状態" })
		{
			foreach (char c in text)
			{
				if (c is not ('状' or '態'))
				{
					Assert.IsTrue(KatakanaGlyphs.CanDraw(c), $"'{c}' in {text} cannot be drawn.");
				}
			}
		}
	}

	[TestMethod]
	public void VoicedKanaDecomposeAndSmallKanaAreDrawn()
	{
		Assert.IsTrue(KatakanaGlyphs.CanDraw('ガ'));
		Assert.IsTrue(KatakanaGlyphs.CanDraw('パ'));
		Assert.IsTrue(KatakanaGlyphs.CanDraw('ッ'));
		Assert.IsTrue(KatakanaGlyphs.CanDraw('ャ'));

		IReadOnlyList<GlyphCell> voiced = KatakanaGlyphs.Layout("ガ");
		IReadOnlyList<GlyphCell> plain = KatakanaGlyphs.Layout("カ");
		Assert.HasCount(1, voiced);
		Assert.IsGreaterThan(plain[0].Strokes!.Count, voiced[0].Strokes!.Count);
	}

	[TestMethod]
	public void UnknownCharactersLayOutWithoutStrokes()
	{
		Assert.IsFalse(KatakanaGlyphs.CanDraw('Q'));
		IReadOnlyList<GlyphCell> cells = KatakanaGlyphs.Layout("アQ");
		Assert.HasCount(2, cells);
		Assert.IsNotNull(cells[0].Strokes);
		Assert.IsNull(cells[1].Strokes);
	}

	[TestMethod]
	public void AdvanceLeavesAGap() => Assert.AreEqual(23f, KatakanaGlyphs.Advance(20f), 1e-4f);

	[TestMethod]
	public void HexRow_HasAddressAndFourWords()
	{
		for (long row = 0; row < 50; row++)
		{
			string text = MonsoonWidgets.HexRow(3u, row);
			Assert.IsTrue(HexRowPattern().IsMatch(text), $"'{text}' is not a hex row.");
			Assert.StartsWith(((row * 16) & 0xFFFF).ToString("X4", System.Globalization.CultureInfo.InvariantCulture), text);
			Assert.AreEqual(text, MonsoonWidgets.HexRow(3u, row));
		}
	}

	[TestMethod]
	[DataRow("UPLINK", "UPLINK")]
	[DataRow("UPLINK##a", "UPLINK")]
	[DataRow("##hidden", "")]
	public void VisibleLabel_StripsTheIdSuffix(string label, string expected) =>
		Assert.AreEqual(expected, MonsoonWidgets.VisibleLabel(label));

	[GeneratedRegex("^[0-9A-F]{4} :( [0-9A-F]{4}){4}$")]
	private static partial Regex HexRowPattern();
}
