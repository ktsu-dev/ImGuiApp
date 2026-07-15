// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Widgets;

using System;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Color;
using ktsu.Semantics.Color;

/// <summary>
/// Provides custom ImGui widgets.
/// </summary>
public static partial class ImGuiWidgets
{
	/// <summary>
	/// A scoped, shadowed container that draws an elevated rounded panel behind whatever is rendered
	/// inside the <c>using</c> block. Because Dear ImGui is immediate-mode and the card's size is not
	/// known until its content has been laid out, the body is drawn onto a foreground draw-list channel
	/// first and the background (shadow + fill + border) is painted behind it on disposal.
	/// </summary>
	/// <remarks>
	/// Usage:
	/// <code>
	/// using (new ImGuiWidgets.Card())
	/// {
	///     ImGui.TextUnformatted("Title");
	///     ImGui.TextWrapped("Body copy that flows inside the padded card.");
	/// }
	/// </code>
	/// Nested child windows / scrolling regions are not supported inside a card because they render to
	/// their own draw lists and will not participate in the channel split.
	/// </remarks>
	public sealed class Card : IDisposable
	{
		private readonly ImDrawListPtr drawList;
		private readonly Vector2 origin;
		private readonly Vector2 padding;
		private readonly float rounding;
		private readonly float width;
		private readonly Vector4? background;
		private readonly bool border;
		private readonly bool wrapPushed;
		private bool disposed;

		/// <summary>
		/// Begins a card. Call inside a <c>using</c> block; the panel is painted when the block exits.
		/// </summary>
		/// <param name="width">Outer card width in pixels. When <c>0</c> the card shrinks to fit its content.</param>
		/// <param name="padding">Inner padding in pixels on every edge. When negative the style's <see cref="ImGuiStyle.WindowPadding"/> is used.</param>
		/// <param name="rounding">Corner radius in pixels. When negative a value derived from the style's frame rounding is used.</param>
		/// <param name="background">Explicit fill colour. When <see langword="null"/> an elevated surface colour is resolved from the active theme.</param>
		/// <param name="border">Whether to stroke a one-pixel border in the theme's border colour.</param>
		public Card(float width = 0f, float padding = -1f, float rounding = -1f, Vector4? background = null, bool border = true)
		{
			ImGuiStylePtr style = ImGui.GetStyle();
			this.padding = padding >= 0f ? new Vector2(padding, padding) : style.WindowPadding;
			this.rounding = rounding >= 0f ? rounding : MathF.Max(style.FrameRounding, 4.0f);
			this.width = width;
			this.background = background;
			this.border = border;

			drawList = ImGui.GetWindowDrawList();
			origin = ImGui.GetCursorScreenPos();

			// Content paints on the foreground channel so the background can be drawn behind it later.
			drawList.ChannelsSplit(2);
			drawList.ChannelsSetCurrent(1);

			ImGui.SetCursorScreenPos(origin + this.padding);
			ImGui.BeginGroup();

			if (width > 0f)
			{
				float inner = MathF.Max(width - (this.padding.X * 2.0f), 1.0f);
				ImGui.PushTextWrapPos(ImGui.GetCursorPosX() + inner);
				wrapPushed = true;
			}
		}

		/// <summary>
		/// Closes the content group and paints the card's shadow, fill, and border behind it.
		/// </summary>
		public void Dispose()
		{
			if (disposed)
			{
				return;
			}

			disposed = true;

			if (wrapPushed)
			{
				ImGui.PopTextWrapPos();
			}

			ImGui.EndGroup();

			Vector2 contentMax = ImGui.GetItemRectMax();
			Vector2 cardMin = origin;
			Vector2 cardMax = contentMax + padding;
			if (width > 0f)
			{
				cardMax.X = origin.X + width;
			}

			drawList.ChannelsSetCurrent(0);

			Span<Vector4> colors = ImGui.GetStyle().Colors;
			DrawShadow(drawList, cardMin, cardMax, rounding, colors[(int)ImGuiCol.BorderShadow]);

			Vector4 fill = background ?? ResolveSurface(colors);
			drawList.AddRectFilled(cardMin, cardMax, ImGui.GetColorU32(fill), rounding);

			if (border)
			{
				drawList.AddRect(cardMin, cardMax, ImGui.GetColorU32(colors[(int)ImGuiCol.Border]), rounding);
			}

			drawList.ChannelsMerge();

			// Reserve the full card footprint in the layout so following widgets flow below it.
			ImGui.SetCursorScreenPos(origin);
			ImGui.Dummy(cardMax - cardMin);
		}

		// Resolve an "elevated surface" colour: prefer an opaque child/popup background, else fall back to the window background.
		private static Vector4 ResolveSurface(Span<Vector4> colors)
		{
			Vector4 child = colors[(int)ImGuiCol.ChildBg];
			if (child.W > 0.01f)
			{
				return child;
			}

			Vector4 popup = colors[(int)ImGuiCol.PopupBg];
			return popup.W > 0.01f ? popup : colors[(int)ImGuiCol.WindowBg];
		}

		// Soft drop shadow: a stack of expanding rounded rects, faintest on the outside, offset slightly downward.
		private static void DrawShadow(ImDrawListPtr drawList, Vector2 min, Vector2 max, float rounding, Vector4 shadowColor)
		{
			// Fall back to a soft black shadow when the theme supplies a fully-transparent shadow colour.
			ImColor baseColor = shadowColor.W > 0.01f ? new ImColor { Value = shadowColor } : new Srgb(0f, 0f, 0f).ToImColor(0.25f);
			float baseAlpha = baseColor.Value.W;

			const int layers = 4;
			float maxGrow = MathF.Max(rounding, 6.0f);
			Vector2 offset = new(0.0f, MathF.Max(rounding * 0.25f, 2.0f));

			// Largest (faintest) first so smaller, stronger layers stack on top.
			for (int i = layers; i >= 1; i--)
			{
				float f = (float)i / layers;
				float grow = maxGrow * f;
				float alpha = baseAlpha * (1.0f - f);
				if (alpha <= 0.0f)
				{
					continue;
				}

				Vector2 g = new(grow, grow);
				drawList.AddRectFilled(min - g + offset, max + g + offset, baseColor.WithAlpha(alpha).ToImGuiU32(), rounding + grow);
			}
		}
	}
}
