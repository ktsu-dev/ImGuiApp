// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using System.Numerics;

using ktsu.Semantics.Color;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests the defaults a CurveSource subclass inherits. The editor itself needs a live ImGui
/// context and is verified visually in ImGuiWidgetsDemo.
/// </summary>
[TestClass]
public sealed class CurveSourceTests
{
	[TestMethod]
	public void Defaults_AreUsableWithoutOverriding()
	{
		StubCurveSource source = new();

		Assert.IsTrue(source.IsVisible(0));
		Assert.AreEqual(CurveInterpolation.Linear, source.GetInterpolation(0));
	}

	[TestMethod]
	public void GetPoints_ReturnsTheSourcePoints()
	{
		StubCurveSource source = new();

		Span<Vector2> points = source.GetPoints(0);

		Assert.AreEqual(2, points.Length);
		Assert.AreEqual(new Vector2(1f, 1f), points[1]);
	}

	private sealed class StubCurveSource : ImGuiWidgets.CurveSource
	{
		private readonly Vector2[] points = [new Vector2(0f, 0f), new Vector2(1f, 1f)];

		public override int CurveCount => 1;

		public override Vector2 ViewMin => Vector2.Zero;

		public override Vector2 ViewMax => Vector2.One;

		public override int GetPointCount(int curveIndex) => points.Length;

		public override Span<Vector2> GetPoints(int curveIndex) => points;

		public override Srgb GetCurveColor(int curveIndex) => new(1f, 0f, 0f);

		public override int EditPoint(int curveIndex, int pointIndex, Vector2 value)
		{
			points[pointIndex] = value;
			return pointIndex;
		}

		public override void AddPoint(int curveIndex, Vector2 value)
		{
		}
	}
}
