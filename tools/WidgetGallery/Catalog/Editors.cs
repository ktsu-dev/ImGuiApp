// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.WidgetGallery.Catalog;

using System;
using System.Collections.Generic;
using System.Numerics;

using ktsu.ImGui.Widgets;
using ktsu.Semantics.Color;

/// <summary>The tiles in the Editors group.</summary>
internal static class EditorTiles
{
	/// <summary>Builds the group's entries.</summary>
	/// <returns>The entries, in gallery order.</returns>
	public static IEnumerable<GalleryEntry> Build()
	{
		const GalleryCategory Category = GalleryCategory.Editors;

		CurveData falloff = new(
		[
			new CurveKnot(new Vector2(0f, 0f), CurvePointKind.Smooth),
			new CurveKnot(new Vector2(0.35f, 0.75f), CurvePointKind.Smooth),
			new CurveKnot(new Vector2(0.7f, 0.55f), CurvePointKind.Corner),
			new CurveKnot(new Vector2(1f, 1f), CurvePointKind.Smooth),
		]);
		int selection = -1;
		yield return new("CurveEditor", Category, [nameof(ImGuiWidgets.CurveEditor)], _ =>
			ImGuiWidgets.CurveEditor(falloff, new Vector2(300f, 170f), Vector2.Zero, Vector2.One, ref selection, "##falloff"));

		GalleryCurves channels = new();
		yield return new("CurveEditor (multi-curve)", Category, [nameof(ImGuiWidgets.CurveEditor)], _ =>
			ImGuiWidgets.CurveEditor(channels, new Vector2(300f, 170f), "##channels"));

		BezierControlPoints ease = new(new Vector2(0.25f, 0.1f), new Vector2(0.25f, 1f));
		yield return new("BezierEditor", Category, [nameof(ImGuiWidgets.BezierEditor)], _ =>
			ImGuiWidgets.BezierEditor("Ease", ref ease, 150f));

		GallerySequence sequence = new();
		int currentFrame = 42;
		bool expanded = true;
		int selectedEntry = 1;
		int firstFrame = 0;
		yield return new("Sequencer", Category, [nameof(ImGuiWidgets.Sequencer)], _ =>
			ImGuiWidgets.Sequencer(sequence, ref currentFrame, ref expanded, ref selectedEntry, ref firstFrame))
		{
			ViewportWidth = 820,
			Bounds = new Vector2(760f, 170f),
		};

		IReadOnlyList<ImGuiWidgets.DiffHunk> hunks = BuildDiff();
		HashSet<int> diffSelection = [];
		yield return new("DiffView", Category, [nameof(ImGuiWidgets.DiffView)], _ =>
			ImGuiWidgets.DiffView("##diff", hunks, diffSelection))
		{
			Bounds = new Vector2(440f, 132f),
		};

		yield return new("DiffView (side by side)", Category, [nameof(ImGuiWidgets.DiffView)], _ =>
			ImGuiWidgets.DiffView("##diff-side", hunks, new ImGuiWidgets.DiffViewOptions { Mode = ImGuiWidgets.DiffViewMode.SideBySide }))
		{
			ViewportWidth = 820,
			Bounds = new Vector2(740f, 112f),
		};
	}

	private static List<ImGuiWidgets.DiffHunk> BuildDiff()
	{
		static ImGuiWidgets.DiffLine Line(ImGuiWidgets.DiffLineKind kind, string text, int? oldNumber, int? newNumber) =>
			new() { Kind = kind, Text = text, OldNumber = oldNumber, NewNumber = newNumber };

		return
		[
			new ImGuiWidgets.DiffHunk
			{
				Heading = "@@ -12,6 +12,7 @@ void Update(float dt)",
				Lines =
				[
					Line(ImGuiWidgets.DiffLineKind.Context, "\tvelocity += gravity * dt;", 12, 12),
					Line(ImGuiWidgets.DiffLineKind.Removed, "\tposition += velocity;", 13, null),
					Line(ImGuiWidgets.DiffLineKind.Added, "\tposition += velocity * dt;", null, 13),
					Line(ImGuiWidgets.DiffLineKind.Added, "\tClampToWorld(ref position);", null, 14),
					Line(ImGuiWidgets.DiffLineKind.Context, "\tUpdateBounds();", 14, 15),
				],
			},
		];
	}

	/// <summary>Three colour channels, as a grading or animation tool would show them.</summary>
	private sealed class GalleryCurves : ImGuiWidgets.CurveSource
	{
		private readonly Vector2[][] curves =
		[
			[new(0f, 0.05f), new(0.3f, 0.4f), new(0.7f, 0.8f), new(1f, 0.95f)],
			[new(0f, 0f), new(0.5f, 0.6f), new(1f, 1f)],
			[new(0f, 0.2f), new(0.4f, 0.25f), new(1f, 0.85f)],
		];

		private readonly Srgb[] colors = [new(0.95f, 0.35f, 0.3f), new(0.35f, 0.85f, 0.4f), new(0.35f, 0.55f, 0.95f)];

		public override int CurveCount => curves.Length;

		public override Vector2 ViewMin => Vector2.Zero;

		public override Vector2 ViewMax => Vector2.One;

		public override int GetPointCount(int curveIndex) => curves[curveIndex].Length;

		public override Span<Vector2> GetPoints(int curveIndex) => curves[curveIndex];

		public override Srgb GetCurveColor(int curveIndex) => colors[curveIndex];

		public override int EditPoint(int curveIndex, int pointIndex, Vector2 value)
		{
			curves[curveIndex][pointIndex] = value;
			return pointIndex;
		}

		public override void AddPoint(int curveIndex, Vector2 value)
		{
			// The gallery only draws the curves.
		}
	}

	/// <summary>A short edit with a few clips on video and audio tracks.</summary>
	private sealed class GallerySequence : ImGuiWidgets.SequenceSource
	{
		private readonly SequenceItem[] items =
		[
			new(0, 30, 0, new Srgb(0.85f, 0.45f, 0.25f)),
			new(34, 80, 0, new Srgb(0.3f, 0.6f, 0.9f)),
			new(10, 70, 1, new Srgb(0.45f, 0.75f, 0.4f)),
			new(60, 95, 2, new Srgb(0.7f, 0.45f, 0.85f)),
		];

		private readonly string[] labels = ["Intro", "Interview", "Music bed", "Titles"];

		public override int FrameMin => 0;

		public override int FrameMax => 100;

		public override int ItemCount => items.Length;

		public override IReadOnlyList<string> ItemTypeNames => ["Video", "Audio", "Text"];

		public override SequenceItem GetItem(int index) => items[index];

		public override string GetItemLabel(int index) => labels[index];

		public override void SetItemRange(int index, int start, int endFrame) =>
			items[index] = items[index] with { Start = start, End = endFrame };
	}
}
