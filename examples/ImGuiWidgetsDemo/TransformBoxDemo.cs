// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Examples.Widgets;

using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Widgets;

/// <summary>
/// The transform box, arranging a turned "layer" over an image canvas: the box is the layer's own unit
/// square, drawn through the layer's placement, so its handles sit on the layer's turned corners.
/// </summary>
internal static class TransformBoxDemo
{
	private static readonly Vector2 CanvasSize = new(480f, 300f);

	private static readonly ImGuiWidgets.ImageCanvasState Canvas = new();
	private static readonly TransformBoxOptions Options = new() { UniformCorners = true, Skew = true };
	private static bool fitted;
	private static float angle = 15f;
	private static bool edgeHandles = true;
	private static bool uniformCorners = true;
	private static bool skew = true;
	private static bool distort = true;

	// The box the layer rests in between drags: the unit square, or a quadrilateral once a corner has
	// been moved alone, which no Matrix3x2 basis can hold, so it is kept as the box rather than folded.
	private static TransformBoxRect resting = TransformBoxRect.Unit;

	// The layer's unit square on the image before its turn, which a skew makes a parallelogram.
	private static Matrix3x2 basis = Matrix3x2.Identity;

	// While a handle is held the box is drawn in the frame the press began in, and the layer's
	// rectangle in that frame is what the drag changes; on release it is folded into the layer.
	private static TransformBoxRect held = TransformBoxRect.Unit;

	/// <summary>Gets the layer's rectangle in image pixels, before its turn: where its unit square's top-left and bottom-right corners land.</summary>
	internal static TransformBoxRect Layer => new(Vector2.Transform(Vector2.Zero, basis), Vector2.Transform(Vector2.One, basis));

	/// <summary>Gets whether a corner of the layer has been moved on its own.</summary>
	internal static bool IsDistorted => resting.IsDistorted;

	/// <summary>Gets whether the layer has been skewed.</summary>
	internal static bool IsSkewed => MathF.Abs(Vector2.Dot(Vector2.Normalize(new Vector2(basis.M11, basis.M12)), Vector2.Normalize(new Vector2(basis.M21, basis.M22)))) > 1e-4f;

	/// <summary>
	/// Returns this section's state to its starting values. The demo keeps its state in statics, which
	/// outlive a harness.
	/// </summary>
	internal static void ResetState()
	{
		basis = Matrix3x2.Identity;
		held = TransformBoxRect.Unit;
		fitted = false;
		angle = 15f;
		edgeHandles = true;
		uniformCorners = true;
		skew = true;
		distort = true;
		resting = TransformBoxRect.Unit;
	}

	/// <summary>Draws the demo section.</summary>
	internal static void Show()
	{
		if (!DemoProbe.Header("Transform Box"))
		{
			return;
		}

		ImGui.TextUnformatted("Drag the layer to move it, a corner to scale it (Shift for free), or an edge to stretch it (Ctrl to skew).");
		ImGui.TextUnformatted("Ctrl+Shift on a corner moves it alone, distorting the layer.");
		ImGui.TextUnformatted("Dragging outside the layer pans the canvas.");

		DemoProbe.SliderFloat("Turn", ref angle, -45f, 45f, "%.0f°");
		DemoProbe.Checkbox("Edge handles", ref edgeHandles);
		ImGui.SameLine();
		DemoProbe.Checkbox("Uniform corners", ref uniformCorners);
		ImGui.SameLine();
		DemoProbe.Checkbox("Ctrl skews", ref skew);
		ImGui.SameLine();
		DemoProbe.Checkbox("Ctrl+Shift distorts", ref distort);
		Options.EdgeHandles = edgeHandles;
		Options.UniformCorners = uniformCorners;
		Options.Skew = skew;
		Options.Distort = distort;

		Vector2 imageSize = new(DemoContext.KtsuTexture.Width, DemoContext.KtsuTexture.Height);
		if (!fitted)
		{
			Canvas.FitToViewport(imageSize, CanvasSize);
			basis = Matrix3x2.CreateScale(imageSize * 0.4f) * Matrix3x2.CreateTranslation(imageSize * 0.3f);
			fitted = true;
		}

		ImGui.SetNextItemAllowOverlap();
		ImGuiWidgets.ImageCanvas("transform_demo_canvas", DemoContext.KtsuTexture.TextureId, imageSize, Canvas, CanvasSize);
		Vector2 canvasMin = ImGui.GetItemRectMin();

		// The layer's placement: its unit square put on the image, turned about its centre, which the
		// canvas then puts on screen.
		Matrix3x2 placement = basis * Turn(basis, angle);
		(Vector2 imageMin, Vector2 imageMax) = Canvas.ImageRectInViewport(imageSize, CanvasSize);
		Matrix3x2 imageToScreen = Matrix3x2.CreateScale((imageMax - imageMin) / imageSize) * Matrix3x2.CreateTranslation(canvasMin + imageMin);
		Matrix3x2 frameToScreen = placement * imageToScreen;

		ImDrawListPtr drawList = ImGui.GetWindowDrawList();
		Vector2[] corners = [.. resting.Corners().Select(corner => Vector2.Transform(corner, frameToScreen))];
		drawList.AddQuadFilled(corners[0], corners[1], corners[2], corners[3], ImGui.GetColorU32(new Vector4(0.3f, 0.6f, 1f, 0.35f)));

		// The layer itself does not move while a handle is held, so the frame stays the one the press
		// began in; the rectangle in that frame is the live edit, drawn here and folded in on release.
		TransformBoxResult result = ImGuiWidgets.TransformBox("transform_demo", ref held, frameToScreen, canvasMin, canvasMin + CanvasSize, Options);
		if (!result.Held && held != resting)
		{
			if (held.IsDistorted)
			{
				resting = held;
			}
			else
			{
				basis = Fold(basis, held, angle);
				held = TransformBoxRect.Unit;
			}
		}

		TransformBoxRect layer = Layer;
		string shape = IsDistorted ? ", distorted" : IsSkewed ? ", skewed" : string.Empty;
		ImGui.TextUnformatted($"Layer: ({layer.Min.X:0}, {layer.Min.Y:0}) to ({layer.Max.X:0}, {layer.Max.Y:0}){shape}");
	}

	/// <summary>The turn about the centre of a layer's unit square on the image.</summary>
	private static Matrix3x2 Turn(Matrix3x2 layerBasis, float degrees) =>
		Matrix3x2.CreateRotation(degrees * MathF.PI / 180f, Vector2.Transform(new Vector2(0.5f), layerBasis));

	/// <summary>The layer once the box's shape in its own frame is folded into it.</summary>
	/// <remarks>
	/// The box carries the unit square onto its rectangle and then through its skew. Composed with the
	/// old placement that is where the layer now lands; the turn is about the new centre, so the old one
	/// is taken back out about there, which leaves the picture exactly where the drag left it.
	/// </remarks>
	private static Matrix3x2 Fold(Matrix3x2 layerBasis, TransformBoxRect rect, float degrees)
	{
		Matrix3x2 box = Matrix3x2.CreateScale(rect.Size) * Matrix3x2.CreateTranslation(rect.Min) * rect.Shape();
		Matrix3x2 landed = box * layerBasis * Turn(layerBasis, degrees);
		Matrix3x2 newTurn = Matrix3x2.CreateRotation(degrees * MathF.PI / 180f, Vector2.Transform(new Vector2(0.5f), landed));
		return Matrix3x2.Invert(newTurn, out Matrix3x2 unturn) ? landed * unturn : layerBasis;
	}
}
