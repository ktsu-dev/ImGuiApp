// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Examples.App.Demos;

using System.Numerics;
using Hexa.NET.ImGui;
using ktsu.ImGui.App;
using ktsu.Semantics.Paths;
using ktsu.Semantics.Strings;

/// <summary>
/// Demo for graphics and drawing capabilities
/// </summary>
internal sealed class GraphicsDemo : IDemoTab
{
	private readonly List<Vector2> canvasPoints = [];
	private Vector4 drawColor = new(1.0f, 1.0f, 0.0f, 1.0f);
	private float brushSize = 5.0f;
	private float animationTime;

	public string TabName => "Graphics & Drawing";

	public void Update(float deltaTime) => animationTime += deltaTime;

	public void Render()
	{
		if (ImGui.BeginTabItem(TabName))
		{
			// Image display
			AbsoluteFilePath iconPath = AppContext.BaseDirectory.As<AbsoluteDirectoryPath>() / "icon.png".As<FileName>();
			ImGuiAppTextureInfo iconTexture = ImGuiApp.GetOrLoadTexture(iconPath);

			ImGui.SeparatorText("Image Display:");
			ImGui.Image(iconTexture.TextureRef, new Vector2(64, 64));
			ImGui.SameLine();
			ImGui.Image(iconTexture.TextureRef, new Vector2(32, 32));
			ImGui.SameLine();
			ImGui.Image(iconTexture.TextureRef, new Vector2(16, 16));

			// Custom drawing with ImDrawList
			ImGui.SeparatorText("Custom Drawing Canvas:");
			ImGui.ColorEdit4("Draw Color", ref drawColor);
			ImGui.SliderFloat("Brush Size", ref brushSize, 1.0f, 20.0f);

			if (ImGui.Button("Clear Canvas"))
			{
				canvasPoints.Clear();
			}

			Vector2 canvasPos = ImGui.GetCursorScreenPos();
			Vector2 canvasSize = new(400, 200);

			// Draw canvas background
			ImDrawListPtr drawList = ImGui.GetWindowDrawList();
			drawList.AddRectFilled(canvasPos, canvasPos + canvasSize, ImGui.ColorConvertFloat4ToU32(new Vector4(0.1f, 0.1f, 0.1f, 1.0f)));
			drawList.AddRect(canvasPos, canvasPos + canvasSize, ImGui.ColorConvertFloat4ToU32(new Vector4(0.5f, 0.5f, 0.5f, 1.0f)));

			// Handle mouse input for drawing
			ImGui.InvisibleButton("Canvas", canvasSize);
			if (ImGui.IsItemActive() && ImGui.IsMouseDown(ImGuiMouseButton.Left))
			{
				Vector2 mousePos = ImGui.GetMousePos() - canvasPos;
				if (mousePos.X >= 0 && mousePos.Y >= 0 && mousePos.X <= canvasSize.X && mousePos.Y <= canvasSize.Y)
				{
					canvasPoints.Add(mousePos);
				}
			}

			// Draw points
			uint color = ImGui.ColorConvertFloat4ToU32(drawColor);
			foreach (Vector2 point in canvasPoints)
			{
				drawList.AddCircleFilled(canvasPos + point, brushSize, color);
			}

			// Draw some simple shapes for demonstration
			ImGui.SeparatorText("Shape Examples:");
			Vector2 shapeStart = ImGui.GetCursorScreenPos();

			// Simple animated circle
			float t = animationTime;
			Vector2 center = shapeStart + new Vector2(100, 50);
			float radius = 20 + (MathF.Sin(t * 2) * 5);
			drawList.AddCircle(center, radius, ImGui.ColorConvertFloat4ToU32(new Vector4(1, 0, 0, 1)), 16, 2.0f);

			// Moving rectangle
			Vector2 rectPos = shapeStart + new Vector2(200 + (MathF.Sin(t) * 30), 30);
			drawList.AddRectFilled(rectPos, rectPos + new Vector2(40, 40), ImGui.ColorConvertFloat4ToU32(new Vector4(0, 1, 0, 0.7f)));

			ImGui.Dummy(new Vector2(400, 100)); // Reserve space

			ImGui.EndTabItem();
		}
	}
}
