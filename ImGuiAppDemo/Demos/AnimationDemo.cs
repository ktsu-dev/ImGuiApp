// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGuiApp.Demo.Demos;

using System.Numerics;
using Hexa.NET.ImGui;

/// <summary>
/// Demo for animations and effects
/// </summary>
internal sealed class AnimationDemo : IDemoTab
{
	private float animationTime;
	private float bounceOffset;
	private float pulseScale = 1.0f;
	private float textSpeed = 50.0f;

	public string TabName => "Animation & Effects";

	public void Update(float deltaTime)
	{
		animationTime += deltaTime;

		// Bouncing animation
		bounceOffset = MathF.Abs(MathF.Sin(animationTime * 3)) * 50;

		// Pulse animation
		pulseScale = 0.8f + (0.4f * MathF.Sin(animationTime * 4));
	}

	public void Render()
	{
		if (ImGui.BeginTabItem(TabName))
		{
			ImGui.SeparatorText("Animation Examples:");

			// Simple animations
			ImGui.Text("Bouncing Animation:");
			Vector2 ballPos = ImGui.GetCursorScreenPos();
			ballPos.Y += bounceOffset;
			ImDrawListPtr drawList = ImGui.GetWindowDrawList();
			drawList.AddCircleFilled(ballPos + new Vector2(50, 50), 20, ImGui.ColorConvertFloat4ToU32(new Vector4(1, 0.5f, 0, 1)));
			ImGui.Dummy(new Vector2(100, 100));

			// Pulsing element
			ImGui.Text("Pulse Animation:");
			Vector2 pulsePos = ImGui.GetCursorScreenPos();
			float pulseSize = 20 * pulseScale;
			drawList.AddCircleFilled(pulsePos + new Vector2(50, 50), pulseSize,
				ImGui.ColorConvertFloat4ToU32(new Vector4(0.5f, 0, 1, 0.7f)));
			ImGui.Dummy(new Vector2(100, 100));

			ImGui.SeparatorText("Animated Text:");
			ImGui.SliderFloat("Text Speed", ref textSpeed, 10.0f, 200.0f);

			for (int i = 0; i < 20; i++)
			{
				float wave = (MathF.Sin((animationTime * 3.0f) + (i * 0.5f)) * 0.5f) + 0.5f;
				ImGui.TextColored(new Vector4(wave, 1.0f - wave, 0.5f, 1.0f), i % 5 == 4 ? " " : "▓");
				if (i % 5 != 4)
				{
					ImGui.SameLine();
				}
			}

			ImGui.EndTabItem();
		}
	}
}
