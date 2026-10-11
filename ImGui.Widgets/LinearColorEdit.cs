// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Color;
using ktsu.ImGui.Probes;
using ktsu.Semantics.Color;

public static partial class ImGuiWidgets
{
	/// <summary>Edits a colour held in linear light, showing it the way a person reads colour: sRGB-encoded.</summary>
	/// <param name="label">The label, and the probe name. Text after <c>##</c> is hidden but used for the id.</param>
	/// <param name="linear">The colour in linear light, alpha straight. Written only when the edit changes it.</param>
	/// <param name="flags">ImGui's colour edit flags, passed through unchanged.</param>
	/// <returns>True if the colour changed this frame.</returns>
	/// <remarks>
	/// Dear ImGui's own colour controls edit whatever numbers they are handed, so a linear colour handed
	/// to them shows its hex digits, its sliders and its swatch in the wrong place: mid grey reads #363636
	/// and the picker's halfway point is far too bright. This converts at the seam, through
	/// <c>ktsu.Semantics.Color</c>, so the stored value stays linear and everything shown is sRGB. Alpha
	/// is not a colour channel and passes through untouched.
	/// </remarks>
	public static bool ColorEdit(string label, ref Vector4 linear, ImGuiColorEditFlags flags = ImGuiColorEditFlags.None)
	{
		Ensure.NotNull(label);
		Vector4 shown = LinearColor.ToSrgb(linear);
		bool changed = ImGui.ColorEdit4(label, ref shown, flags);
		ImGuiProbes.MarkItem(label);
		return LinearColor.Adopt(changed, shown, ref linear);
	}

	/// <summary>Edits an opaque colour held in linear light, showing it sRGB-encoded.</summary>
	/// <param name="label">The label, and the probe name. Text after <c>##</c> is hidden but used for the id.</param>
	/// <param name="linear">The colour in linear light. Written only when the edit changes it.</param>
	/// <param name="flags">ImGui's colour edit flags, passed through unchanged.</param>
	/// <returns>True if the colour changed this frame.</returns>
	/// <remarks>See <see cref="ColorEdit(string, ref Vector4, ImGuiColorEditFlags)"/>.</remarks>
	public static bool ColorEdit(string label, ref Vector3 linear, ImGuiColorEditFlags flags = ImGuiColorEditFlags.None)
	{
		Ensure.NotNull(label);
		Vector3 shown = LinearColor.ToSrgb(linear);
		bool changed = ImGui.ColorEdit3(label, ref shown, flags);
		ImGuiProbes.MarkItem(label);
		return LinearColor.Adopt(changed, shown, ref linear);
	}

	/// <summary>Draws a colour picker over a colour held in linear light, showing it sRGB-encoded.</summary>
	/// <param name="label">The label, and the probe name. Text after <c>##</c> is hidden but used for the id.</param>
	/// <param name="linear">The colour in linear light, alpha straight. Written only when the pick changes it.</param>
	/// <param name="flags">ImGui's colour edit flags, passed through unchanged.</param>
	/// <returns>True if the colour changed this frame.</returns>
	/// <remarks>See <see cref="ColorEdit(string, ref Vector4, ImGuiColorEditFlags)"/>.</remarks>
	public static bool ColorPicker(string label, ref Vector4 linear, ImGuiColorEditFlags flags = ImGuiColorEditFlags.None)
	{
		Ensure.NotNull(label);
		Vector4 shown = LinearColor.ToSrgb(linear);
		bool changed = ImGui.ColorPicker4(label, ref shown, flags);
		ImGuiProbes.MarkItem(label);
		return LinearColor.Adopt(changed, shown, ref linear);
	}

	/// <summary>Draws a colour picker over an opaque colour held in linear light, showing it sRGB-encoded.</summary>
	/// <param name="label">The label, and the probe name. Text after <c>##</c> is hidden but used for the id.</param>
	/// <param name="linear">The colour in linear light. Written only when the pick changes it.</param>
	/// <param name="flags">ImGui's colour edit flags, passed through unchanged.</param>
	/// <returns>True if the colour changed this frame.</returns>
	/// <remarks>See <see cref="ColorEdit(string, ref Vector4, ImGuiColorEditFlags)"/>.</remarks>
	public static bool ColorPicker(string label, ref Vector3 linear, ImGuiColorEditFlags flags = ImGuiColorEditFlags.None)
	{
		Ensure.NotNull(label);
		Vector3 shown = LinearColor.ToSrgb(linear);
		bool changed = ImGui.ColorPicker3(label, ref shown, flags);
		ImGuiProbes.MarkItem(label);
		return LinearColor.Adopt(changed, shown, ref linear);
	}

	/// <summary>Draws a colour swatch button for a colour held in linear light, filled sRGB-encoded.</summary>
	/// <param name="id">The id, and the probe name. Text after <c>##</c> is hidden but used for the id.</param>
	/// <param name="linear">The colour in linear light, alpha straight.</param>
	/// <param name="flags">ImGui's colour edit flags, passed through unchanged.</param>
	/// <param name="size">The button's size, or zero for ImGui's default square.</param>
	/// <returns>True if the button was clicked this frame.</returns>
	public static bool ColorButton(string id, Vector4 linear, ImGuiColorEditFlags flags = ImGuiColorEditFlags.None, Vector2 size = default)
	{
		Ensure.NotNull(id);
		bool clicked = ImGui.ColorButton(id, LinearColor.ToSrgb(linear), flags, size);
		ImGuiProbes.MarkItem(id);
		return clicked;
	}

	/// <summary>The conversions behind the linear colour controls, with no ImGui dependency.</summary>
	internal static class LinearColor
	{
		/// <summary>Encodes a linear colour for display; alpha passes through.</summary>
		/// <param name="linear">The colour in linear light.</param>
		/// <returns>The sRGB-encoded colour.</returns>
		public static Vector4 ToSrgb(Vector4 linear) => new Color(linear.X, linear.Y, linear.Z, linear.W).ToSrgbVector4();

		/// <summary>Encodes an opaque linear colour for display.</summary>
		/// <param name="linear">The colour in linear light.</param>
		/// <returns>The sRGB-encoded colour.</returns>
		public static Vector3 ToSrgb(Vector3 linear) => new Color(linear.X, linear.Y, linear.Z, 1d).ToSrgbVector3();

		/// <summary>Decodes a displayed colour back to linear light; alpha passes through.</summary>
		/// <param name="srgb">The sRGB-encoded colour.</param>
		/// <returns>The colour in linear light.</returns>
		public static Vector4 ToLinear(Vector4 srgb)
		{
			Color color = ColorImGuiExtensions.FromImGuiVector4(srgb);
			return new Vector4((float)color.R, (float)color.G, (float)color.B, srgb.W);
		}

		/// <summary>Decodes an opaque displayed colour back to linear light.</summary>
		/// <param name="srgb">The sRGB-encoded colour.</param>
		/// <returns>The colour in linear light.</returns>
		public static Vector3 ToLinear(Vector3 srgb)
		{
			Vector4 linear = ToLinear(new Vector4(srgb, 1f));
			return new Vector3(linear.X, linear.Y, linear.Z);
		}

		/// <summary>Writes an edit back, only when ImGui reported one.</summary>
		/// <param name="changed">Whether ImGui reported an edit.</param>
		/// <param name="shown">The edited, displayed colour.</param>
		/// <param name="linear">The stored colour.</param>
		/// <returns><paramref name="changed"/>.</returns>
		/// <remarks>
		/// Writing only on an edit is what keeps a colour that is merely displayed bit-for-bit what the
		/// caller stored, rather than whatever a round trip through the encoding rounds it to.
		/// </remarks>
		public static bool Adopt(bool changed, Vector4 shown, ref Vector4 linear)
		{
			if (changed)
			{
				linear = ToLinear(shown);
			}

			return changed;
		}

		/// <inheritdoc cref="Adopt(bool, Vector4, ref Vector4)"/>
		public static bool Adopt(bool changed, Vector3 shown, ref Vector3 linear)
		{
			if (changed)
			{
				linear = ToLinear(shown);
			}

			return changed;
		}
	}
}
