// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Examples.App.Demos;

using System.Numerics;
using Hexa.NET.ImGui;

/// <summary>
/// Demo for advanced ImGui widgets
/// </summary>
internal sealed class AdvancedWidgetsDemo : IDemoTab
{
	private Vector3 colorPickerValue = new(0.4f, 0.7f, 0.2f);
	private Vector4 color4Value = new(1.0f, 0.5f, 0.2f, 1.0f);
	private float animationTime;

	public string TabName => "Advanced Widgets";

	public void Update(float deltaTime) => animationTime += deltaTime;

	public void Render()
	{
		if (ImGui.BeginTabItem(TabName))
		{
			// Color controls
			ImGui.SeparatorText("Color Controls:");
			ImGui.ColorEdit3("Color RGB", ref colorPickerValue);
			ImGui.ColorEdit4("Color RGBA", ref color4Value);
			ImGui.SetNextItemWidth(200.0f);
			ImGui.ColorPicker3("Color Picker", ref colorPickerValue);

			// Tree view
			ImGui.SeparatorText("Tree View:");
			if (ImGui.TreeNode("Root Node"))
			{
				for (int i = 0; i < 5; i++)
				{
					string nodeName = $"Child Node {i}";
					bool nodeOpen = ImGui.TreeNode(nodeName);

					if (i == 2 && nodeOpen)
					{
						for (int j = 0; j < 3; j++)
						{
							if (ImGui.TreeNode($"Grandchild {j}"))
							{
								ImGui.Text($"Leaf item {j}");
								ImGui.TreePop();
							}
						}
					}
					else if (nodeOpen)
					{
						ImGui.Text($"Content of {nodeName}");
					}

					if (nodeOpen)
					{
						ImGui.TreePop();
					}
				}
				ImGui.TreePop();
			}

			// Progress bars and loading indicators
			ImGui.SeparatorText("Progress Indicators:");
			float progress = ((float)Math.Sin(animationTime * 2.0) * 0.5f) + 0.5f;
			ImGui.ProgressBar(progress, new Vector2(-1, 0), $"{progress * 100:F1}%");

			// Spinner-like effect
			ImGui.Text("Loading...");
			ImGui.SameLine();
			for (int i = 0; i < 8; i++)
			{
				float rotation = (animationTime * 5.0f) + (i * MathF.PI / 4.0f);
				float alpha = (MathF.Sin(rotation) + 1.0f) * 0.5f;
				ImGui.TextColored(new Vector4(1, 1, 1, alpha), "●");
				if (i < 7)
				{
					ImGui.SameLine();
				}
			}

			ImGui.EndTabItem();
		}
	}
}
