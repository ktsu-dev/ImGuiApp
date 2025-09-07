// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Styler;

using System.Numerics;

using Hexa.NET.ImGui;

/// <summary>
/// Provides functionality for creating and aligning buttons in ImGui.
/// </summary>
public static class Button
{
	/// <summary>
	/// Contains methods for aligning buttons.
	/// </summary>
	public static class Alignment
	{
		/// <summary>
		/// Aligns the button text to the left.
		/// </summary>
		/// <returns>A scoped button alignment with left alignment.</returns>
		public static ScopedButtonAlignment Left() => new(new(0f, 0.5f));

		/// <summary>
		/// Aligns the button text to the center.
		/// </summary>
		/// <returns>A scoped button alignment with center alignment.</returns>
		public static ScopedButtonAlignment Center() => new(new(0.5f, 0.5f));

		/// <summary>
		/// Represents a scoped button alignment.
		/// </summary>
		/// <param name="vector">The alignment vector.</param>
		public class ScopedButtonAlignment(Vector2 vector) : ScopedStyleVar(ImGuiStyleVar.ButtonTextAlign, vector)
		{
		}
	}
}
