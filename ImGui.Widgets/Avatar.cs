// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Widgets;

using System;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Styler;

/// <summary>
/// Presence/status indicator drawn as a small dot on an <see cref="ImGuiWidgets.Avatar(string, string, float, AvatarStatus)"/>.
/// </summary>
public enum AvatarStatus
{
	/// <summary>No status dot is drawn.</summary>
	None,
	/// <summary>Available (green).</summary>
	Online,
	/// <summary>Idle / away (amber).</summary>
	Away,
	/// <summary>Do-not-disturb / busy (red).</summary>
	Busy,
	/// <summary>Signed out (grey).</summary>
	Offline,
}

/// <summary>
/// Provides custom ImGui widgets.
/// </summary>
public static partial class ImGuiWidgets
{
	/// <summary>
	/// Draws a circular avatar from a texture, with an optional presence dot.
	/// </summary>
	/// <param name="id">Unique widget identifier.</param>
	/// <param name="textureId">The texture to clip into a circle.</param>
	/// <param name="diameter">Avatar diameter in pixels. When 0, defaults to twice the frame height so it scales with DPI.</param>
	/// <param name="status">Presence status drawn as a dot at the bottom-right.</param>
	/// <returns>True if the avatar was clicked.</returns>
	public static bool Avatar(string id, uint textureId, float diameter = 0f, AvatarStatus status = AvatarStatus.None) =>
		AvatarImpl.Draw(id, textureId, null, diameter, status);

	/// <summary>
	/// Draws a circular avatar showing the initials of <paramref name="displayName"/> on a colour derived
	/// deterministically from the name, with an optional presence dot.
	/// </summary>
	/// <param name="id">Unique widget identifier.</param>
	/// <param name="displayName">Name used to compute the initials and background colour.</param>
	/// <param name="diameter">Avatar diameter in pixels. When 0, defaults to twice the frame height so it scales with DPI.</param>
	/// <param name="status">Presence status drawn as a dot at the bottom-right.</param>
	/// <returns>True if the avatar was clicked.</returns>
	public static bool Avatar(string id, string displayName, float diameter = 0f, AvatarStatus status = AvatarStatus.None) =>
		AvatarImpl.Draw(id, 0, displayName ?? string.Empty, diameter, status);

	/// <summary>
	/// Computes the up-to-two-character initials for a display name. The first letters of the first and last
	/// whitespace-separated tokens are used; a single token yields its first character. Returns "?" when no
	/// usable characters are present.
	/// </summary>
	/// <param name="displayName">The display name to abbreviate.</param>
	/// <returns>An uppercase initials string of one or two characters.</returns>
	public static string Initials(string displayName)
	{
		if (string.IsNullOrWhiteSpace(displayName))
		{
			return "?";
		}

		string[] tokens = displayName.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
		if (tokens.Length == 0)
		{
			return "?";
		}

		if (tokens.Length == 1)
		{
			return char.ToUpperInvariant(tokens[0][0]).ToString();
		}

		char first = char.ToUpperInvariant(tokens[0][0]);
		char last = char.ToUpperInvariant(tokens[^1][0]);
		return $"{first}{last}";
	}

	internal static class AvatarImpl
	{
		internal static bool Draw(string id, uint textureId, string? displayName, float diameter, AvatarStatus status)
		{
			float resolvedDiameter = diameter > 0f ? diameter : ImGui.GetFrameHeight() * 2.0f;
			float radius = resolvedDiameter * 0.5f;

			Vector2 origin = ImGui.GetCursorScreenPos();
			bool clicked = ImGui.InvisibleButton(id, new Vector2(resolvedDiameter, resolvedDiameter));
			Vector2 center = new(origin.X + radius, origin.Y + radius);

			ImDrawListPtr drawList = ImGui.GetWindowDrawList();

			if (displayName is null)
			{
				unsafe
				{
					drawList.AddImageRounded(new ImTextureRef(texId: textureId), origin, origin + new Vector2(resolvedDiameter, resolvedDiameter), Vector2.Zero, Vector2.One, ImGui.GetColorU32(Vector4.One), radius);
				}
			}
			else
			{
				Vector4 background = BackgroundFor(displayName);
				drawList.AddCircleFilled(center, radius, ImGui.GetColorU32(background), 0);

				string initials = Initials(displayName);
				Vector4 textColor = Luminance(background) > 0.55f ? new Vector4(0f, 0f, 0f, 1f) : Vector4.One;
				Vector2 textSize = ImGui.CalcTextSize(initials);
				Vector2 textPos = new(center.X - (textSize.X * 0.5f), center.Y - (textSize.Y * 0.5f));
				drawList.AddText(textPos, ImGui.GetColorU32(textColor), initials);
			}

			if (status != AvatarStatus.None)
			{
				DrawStatusDot(drawList, center, radius, status);
			}

			return clicked;
		}

		private static void DrawStatusDot(ImDrawListPtr drawList, Vector2 center, float radius, AvatarStatus status)
		{
			// Anchor the dot on the circle edge at the 45° bottom-right position.
			float diagonal = radius * 0.70710678f; // radius * sin(45°)
			Vector2 dotCenter = new(center.X + diagonal, center.Y + diagonal);
			float dotRadius = MathF.Max(radius * 0.28f, 3.0f);

			// A window-coloured ring separates the dot from the avatar fill.
			uint ringColor = ImGui.GetColorU32(ImGuiCol.WindowBg);
			drawList.AddCircleFilled(dotCenter, dotRadius + MathF.Max(dotRadius * 0.25f, 1.5f), ringColor, 0);

			Vector4 statusColor = status switch
			{
				AvatarStatus.Online => Color.Palette.Semantic.Success.Value,
				AvatarStatus.Away => Color.Palette.Semantic.Warning.Value,
				AvatarStatus.Busy => Color.Palette.Semantic.Error.Value,
				AvatarStatus.Offline => Color.Palette.Neutral.Gray.Value,
				_ => Color.Palette.Neutral.Gray.Value,
			};
			drawList.AddCircleFilled(dotCenter, dotRadius, ImGui.GetColorU32(statusColor), 0);
		}

		private static Vector4 BackgroundFor(string displayName)
		{
			// Stable (non-randomized) FNV-1a hash so the same name always maps to the same hue.
			uint hash = 2166136261u;
			foreach (char c in displayName)
			{
				hash = (hash ^ c) * 16777619u;
			}

			float hue = (hash % 360u) / 360.0f;
			return Color.FromHSL(hue, 0.55f, 0.55f).Value;
		}

		private static float Luminance(Vector4 color) => (0.2126f * color.X) + (0.7152f * color.Y) + (0.0722f * color.Z);
	}
}
