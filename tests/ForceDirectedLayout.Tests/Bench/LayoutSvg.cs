// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ForceDirectedLayout.Tests.Bench;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using ktsu.ForceDirectedLayout;

/// <summary>
/// Draws a settled layout to SVG, so it can be looked at rather than only measured.
/// </summary>
/// <remarks>
/// Metrics catch what they were written to catch. A picture catches the rest — a graph that settled
/// into two clumps joined by one long link, a node parked inside a fan of edges it has nothing to do
/// with, a chain that folded back on itself — none of which any single number here names. Writing
/// SVG rather than rendering through the editor means no window, no GPU and no ImGui context, so it
/// works from any test on any machine.
/// <para>
/// Links are drawn first and as the cubic the renderer actually draws, with its control points
/// offset horizontally by a quarter of the link's length, and nodes are drawn over them. That is the
/// real z-order, so a link that vanishes behind a node in this picture is a link that vanishes in the
/// editor — which is the defect <see cref="LayoutMetrics.LinksOverBodies"/> counts.
/// </para>
/// </remarks>
public static class LayoutSvg
{
	/// <summary>Blank space left around the graph's bounding box.</summary>
	private const double Margin = 40.0;

	/// <summary>Fraction of a link's length its bezier control points are offset by.</summary>
	private const double ControlPointFraction = 0.25;

	/// <summary>Writes a core's current arrangement to an SVG file.</summary>
	/// <param name="core">The layout to draw.</param>
	/// <param name="path">Where to write it.</param>
	/// <param name="caption">Optional line of text drawn at the top, such as a metrics summary.</param>
	public static void Write(LayoutCore core, string path, string? caption = null)
	{
		ArgumentNullException.ThrowIfNull(core);
		File.WriteAllText(path, Render(core, caption));
	}

	/// <summary>Renders a core's current arrangement as SVG markup.</summary>
	/// <param name="core">The layout to draw.</param>
	/// <param name="caption">Optional line of text drawn at the top, such as a metrics summary.</param>
	public static string Render(LayoutCore core, string? caption = null)
	{
		ArgumentNullException.ThrowIfNull(core);

		(double minX, double minY, double width, double height) = Viewport(core);
		HashSet<int> overlapping = OverlappingBodies(core);

		StringBuilder svg = new();
		svg.Append(Invariant($"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"{minX:F1} {minY:F1} {width:F1} {height:F1}\" width=\"{width:F0}\" height=\"{height:F0}\">"));
		svg.Append(Invariant($"<rect x=\"{minX:F1}\" y=\"{minY:F1}\" width=\"{width:F1}\" height=\"{height:F1}\" fill=\"#1e1e1e\"/>"));

		// Links first: the renderer draws them beneath the node backgrounds, so anything hidden here
		// is hidden there too.
		for (int e = 0; e < core.EdgeCount; e++)
		{
			(Vec2D from, Vec2D to) = Ends(core, e);
			double offset = Math.Sqrt(((to.X - from.X) * (to.X - from.X)) + ((to.Y - from.Y) * (to.Y - from.Y))) * ControlPointFraction;

			svg.Append(Invariant($"<path d=\"M {from.X:F1} {from.Y:F1} C {from.X + offset:F1} {from.Y:F1}, {to.X - offset:F1} {to.Y:F1}, {to.X:F1} {to.Y:F1}\" fill=\"none\" stroke=\"#8ab4f8\" stroke-width=\"2\"/>"));
		}

		for (int i = 0; i < core.BodyCount; i++)
		{
			BodyState body = core.Bodies[i];
			string fill = overlapping.Contains(i) ? "#5a2a2a" : "#2d2d30";
			string stroke = overlapping.Contains(i) ? "#e06c75" : "#5a5a5e";

			svg.Append(Invariant($"<rect x=\"{body.Position.X:F1}\" y=\"{body.Position.Y:F1}\" width=\"{body.Dimensions.X:F1}\" height=\"{body.Dimensions.Y:F1}\" rx=\"4\" fill=\"{fill}\" stroke=\"{stroke}\" stroke-width=\"2\"/>"));
			svg.Append(Invariant(
				$"<text x=\"{body.Position.X + 6:F1}\" y=\"{body.Position.Y + 18:F1}\" font-family=\"monospace\" font-size=\"13\" fill=\"#d4d4d4\">{body.Id}</text>"));
		}

		if (!string.IsNullOrEmpty(caption))
		{
			svg.Append(Invariant(
				$"<text x=\"{minX + 8:F1}\" y=\"{minY + 20:F1}\" font-family=\"monospace\" font-size=\"14\" fill=\"#9cdcfe\">{Escape(caption)}</text>"));
		}

		svg.Append("</svg>");
		return svg.ToString();
	}

	/// <summary>The area to draw, being the graph's bounding box plus a margin.</summary>
	/// <param name="core">The layout to measure.</param>
	private static (double MinX, double MinY, double Width, double Height) Viewport(LayoutCore core)
	{
		if (core.BodyCount == 0)
		{
			return (0.0, 0.0, 1.0, 1.0);
		}

		double minX = double.MaxValue;
		double minY = double.MaxValue;
		double maxX = double.MinValue;
		double maxY = double.MinValue;

		for (int i = 0; i < core.BodyCount; i++)
		{
			BodyState body = core.Bodies[i];
			minX = Math.Min(minX, body.Position.X);
			minY = Math.Min(minY, body.Position.Y);
			maxX = Math.Max(maxX, body.Position.X + body.Dimensions.X);
			maxY = Math.Max(maxY, body.Position.Y + body.Dimensions.Y);
		}

		return (minX - Margin, minY - Margin, maxX - minX + (Margin * 2), maxY - minY + (Margin * 2));
	}

	/// <summary>Indices of every body drawn over another, so the picture can call them out.</summary>
	/// <param name="core">The layout to inspect.</param>
	private static HashSet<int> OverlappingBodies(LayoutCore core)
	{
		HashSet<int> overlapping = [];

		for (int i = 0; i < core.BodyCount; i++)
		{
			for (int j = i + 1; j < core.BodyCount; j++)
			{
				BodyState a = core.Bodies[i];
				BodyState b = core.Bodies[j];

				bool apart =
					a.Position.X + a.Dimensions.X <= b.Position.X ||
					b.Position.X + b.Dimensions.X <= a.Position.X ||
					a.Position.Y + a.Dimensions.Y <= b.Position.Y ||
					b.Position.Y + b.Dimensions.Y <= a.Position.Y;

				if (!apart)
				{
					overlapping.Add(i);
					overlapping.Add(j);
				}
			}
		}

		return overlapping;
	}

	/// <summary>Where an edge's two ends are drawn, in world space.</summary>
	/// <param name="core">The core holding the edge.</param>
	/// <param name="e">Index of the edge.</param>
	private static (Vec2D From, Vec2D To) Ends(LayoutCore core, int e)
	{
		EdgeRef edge = core.Edges[e];
		BodyState source = core.Bodies[edge.SourceIndex];
		BodyState target = core.Bodies[edge.TargetIndex];

		return edge.HasPinOffsets != 0
			? (source.Position + edge.SourcePinOffset, target.Position + edge.TargetPinOffset)
			: (source.Position + (source.Dimensions * 0.5), target.Position + (target.Dimensions * 0.5));
	}

	/// <summary>Formats with the invariant culture, so a comma decimal separator cannot corrupt the markup.</summary>
	/// <param name="text">An interpolated string to format.</param>
	private static string Invariant(FormattableString text) => text.ToString(CultureInfo.InvariantCulture);

	/// <summary>Escapes the few characters that would otherwise end a text element early.</summary>
	/// <param name="text">Text to escape.</param>
	private static string Escape(string text) =>
		text.Replace("&", "&amp;", StringComparison.Ordinal)
			.Replace("<", "&lt;", StringComparison.Ordinal)
			.Replace(">", "&gt;", StringComparison.Ordinal);
}
