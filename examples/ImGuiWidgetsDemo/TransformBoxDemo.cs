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
	private static readonly TransformBoxOptions Options = new() { UniformCorners = true };
	private static bool fitted;
	private static float angle = 15f;
	private static bool edgeHandles = true;
	private static bool uniformCorners = true;

	// While a handle is held the box is drawn in the frame the press began in, and the layer's
	// rectangle in that frame is what the drag changes; on release it is folded into the layer.
	private static TransformBoxRect held = TransformBoxRect.Unit;

	/// <summary>Gets the layer's rectangle in image pixels, before its turn.</summary>
	internal static TransformBoxRect Layer { get; private set; }

	/// <summary>
	/// Returns this section's state to its starting values. The demo keeps its state in statics, which
	/// outlive a harness.
	/// </summary>
	internal static void ResetState()
	{
		Layer = default;
		held = TransformBoxRect.Unit;
		fitted = false;
		angle = 15f;
		edgeHandles = true;
		uniformCorners = true;
	}

	/// <summary>Draws the demo section.</summary>
	internal static void Show()
	{
		if (!DemoProbe.Header("Transform Box"))
		{
			return;
		}

		ImGui.TextUnformatted("Drag the layer to move it, a corner to scale it (Shift for free), or an edge to stretch it.");
		ImGui.TextUnformatted("Dragging outside the layer pans the canvas.");

		DemoProbe.SliderFloat("Turn", ref angle, -45f, 45f, "%.0f°");
		DemoProbe.Checkbox("Edge handles", ref edgeHandles);
		ImGui.SameLine();
		DemoProbe.Checkbox("Uniform corners", ref uniformCorners);
		Options.EdgeHandles = edgeHandles;
		Options.UniformCorners = uniformCorners;

		Vector2 imageSize = new(DemoContext.KtsuTexture.Width, DemoContext.KtsuTexture.Height);
		if (!fitted)
		{
			Canvas.FitToViewport(imageSize, CanvasSize);
			Layer = new TransformBoxRect(imageSize * 0.3f, imageSize * 0.7f);
			fitted = true;
		}

		ImGui.SetNextItemAllowOverlap();
		ImGuiWidgets.ImageCanvas("transform_demo_canvas", DemoContext.KtsuTexture.TextureId, imageSize, Canvas, CanvasSize);
		Vector2 canvasMin = ImGui.GetItemRectMin();

		// The layer's placement: its unit square scaled to its size, turned about its centre, then put
		// on the image, which the canvas puts on screen.
		Vector2 size = Layer.Size;
		Vector2 centre = (Layer.Min + Layer.Max) / 2f;
		Matrix3x2 placement = Matrix3x2.CreateScale(size)
			* Matrix3x2.CreateTranslation(-size / 2f)
			* Matrix3x2.CreateRotation(angle * MathF.PI / 180f)
			* Matrix3x2.CreateTranslation(centre);
		(Vector2 imageMin, Vector2 imageMax) = Canvas.ImageRectInViewport(imageSize, CanvasSize);
		Matrix3x2 imageToScreen = Matrix3x2.CreateScale((imageMax - imageMin) / imageSize) * Matrix3x2.CreateTranslation(canvasMin + imageMin);
		Matrix3x2 frameToScreen = placement * imageToScreen;

		ImDrawListPtr drawList = ImGui.GetWindowDrawList();
		Vector2[] corners = [.. TransformBoxRect.Unit.Corners().Select(corner => Vector2.Transform(corner, frameToScreen))];
		drawList.AddQuadFilled(corners[0], corners[1], corners[2], corners[3], ImGui.GetColorU32(new Vector4(0.3f, 0.6f, 1f, 0.35f)));

		// The layer itself does not move while a handle is held, so the frame stays the one the press
		// began in; the rectangle in that frame is the live edit, drawn here and folded in on release.
		TransformBoxResult result = ImGuiWidgets.TransformBox("transform_demo", ref held, frameToScreen, canvasMin, canvasMin + CanvasSize, Options);
		if (!result.Held && held != TransformBoxRect.Unit)
		{
			Layer = Fold(Layer, held, angle);
			held = TransformBoxRect.Unit;
		}

		ImGui.TextUnformatted($"Layer: ({Layer.Min.X:0}, {Layer.Min.Y:0}) to ({Layer.Max.X:0}, {Layer.Max.Y:0})");
	}

	/// <summary>The layer once a rectangle in its own frame becomes its new extent.</summary>
	/// <remarks>
	/// The new rectangle's centre is carried through the old placement to find where it sits on the image,
	/// and its size is the old size scaled by the rectangle's, so the turn is kept about the new centre.
	/// </remarks>
	private static TransformBoxRect Fold(TransformBoxRect layer, TransformBoxRect rect, float degrees)
	{
		Vector2 size = layer.Size;
		Vector2 centre = (layer.Min + layer.Max) / 2f;
		Vector2 local = (((rect.Min + rect.Max) / 2f) - new Vector2(0.5f)) * size;
		Vector2 newCentre = centre + Vector2.Transform(local, Matrix3x2.CreateRotation(degrees * MathF.PI / 180f));
		Vector2 newSize = size * rect.Size;
		return new TransformBoxRect(newCentre - (newSize / 2f), newCentre + (newSize / 2f));
	}
}
