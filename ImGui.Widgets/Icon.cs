// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGuiWidgets;

using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGuiStyler;

/// <summary>
/// Provides custom ImGui widgets.
/// </summary>
public static partial class ImGuiWidgets
{
	/// <summary>
	/// Gets or sets a value indicating whether to enable debug drawing for icons.
	/// </summary>
	public static bool EnableIconDebugDraw { get; set; }

	/// <summary>
	/// Specifies the alignment of the icon.
	/// </summary>
	public enum IconAlignment
	{
		/// <summary>
		/// Aligns the icon horizontally.
		/// </summary>
		Horizontal,

		/// <summary>
		/// Aligns the icon vertically.
		/// </summary>
		Vertical,
	}

	/// <summary>
	/// Additional options to modify Icon behavior.
	/// </summary>
	public class IconOptions
	{
		/// <summary>
		/// The color of the icon.
		/// </summary>
		public Vector4 Color { get; init; } = ImGuiStyler.Color.Palette.Neutral.White.Value;

		/// <summary>
		/// The tooltip to display.
		/// </summary>
		public string Tooltip { get; init; } = string.Empty;

		/// <summary>
		/// Gets or sets the action to be performed on click.
		/// </summary>
		public Action? OnClick { get; init; }

		/// <summary>
		/// Gets or sets the action to be performed on double click.
		/// </summary>
		public Action? OnDoubleClick { get; init; }

		/// <summary>
		/// Gets or sets the action to be performed on right click.
		/// </summary>
		public Action? OnRightClick { get; init; }

		/// <summary>
		/// Gets or sets the action to be performed on context menu.
		/// </summary>
		public Action? OnContextMenu { get; init; }
	}

	/// <summary>
	/// Renders an icon with the specified parameters.
	/// </summary>
	/// <param name="label">The label of the icon.</param>
	/// <param name="textureId">The texture ID of the icon.</param>
	/// <param name="imageSize">The size of the image.</param>
	/// <param name="iconAlignment">The alignment of the icon.</param>
	/// <returns>Was the icon bounds clicked</returns>
	public static bool Icon(string label, uint textureId, float imageSize, IconAlignment iconAlignment) =>
		IconImpl.Show(label, textureId, new(imageSize, imageSize), iconAlignment, new());

	/// <summary>
	/// Renders an icon with the specified parameters.
	/// </summary>
	/// <param name="label">The label of the icon.</param>
	/// <param name="textureId">The texture ID of the icon.</param>
	/// <param name="imageSize">The size of the image.</param>
	/// <param name="iconAlignment">The alignment of the icon.</param>
	/// <returns>Was the icon bounds clicked</returns>
	public static bool Icon(string label, uint textureId, Vector2 imageSize, IconAlignment iconAlignment) =>
		IconImpl.Show(label, textureId, imageSize, iconAlignment, new());

	/// <summary>
	/// Renders an icon with the specified parameters.
	/// </summary>
	/// <param name="label">The label of the icon.</param>
	/// <param name="textureId">The texture ID of the icon.</param>
	/// <param name="imageSize">The size of the image.</param>
	/// <param name="iconAlignment">The alignment of the icon.</param>
	/// <param name="options">Additional options</param>
	/// <returns>Was the icon bounds clicked</returns>
	public static bool Icon(string label, uint textureId, float imageSize, IconAlignment iconAlignment, IconOptions options) =>
		IconImpl.Show(label, textureId, new(imageSize, imageSize), iconAlignment, options);

	/// <summary>
	/// Renders an icon with the specified parameters.
	/// </summary>
	/// <param name="label">The label of the icon.</param>
	/// <param name="textureId">The texture ID of the icon.</param>
	/// <param name="imageSize">The size of the image.</param>
	/// <param name="iconAlignment">The alignment of the icon.</param>
	/// <param name="options">Additional options</param>
	/// <returns>Was the icon bounds clicked</returns>
	public static bool Icon(string label, uint textureId, Vector2 imageSize, IconAlignment iconAlignment, IconOptions options) =>
		IconImpl.Show(label, textureId, imageSize, iconAlignment, options);

	/// <summary>
	/// Calculates the size of the icon with the specified parameters.
	/// </summary>
	/// <param name="label">The label of the icon.</param>
	/// <param name="imageSize">The size of the image.</param>
	/// <param name="iconAlignment">The alignment of the icon.</param>
	/// <returns>The calculated size of the icon.</returns>
	public static Vector2 CalcIconSize(string label, float imageSize, IconAlignment iconAlignment) => CalcIconSize(label, new Vector2(imageSize), iconAlignment);

	/// <summary>
	/// Calculates the size of the icon with the specified parameters.
	/// </summary>
	/// <param name="label">The label of the icon.</param>
	/// <param name="imageSize">The size of the image.</param>
	/// <returns>The calculated size of the icon.</returns>
	public static Vector2 CalcIconSize(string label, Vector2 imageSize) => CalcIconSize(label, imageSize, IconAlignment.Horizontal);

	/// <summary>
	/// Calculates the size of the icon with the specified parameters.
	/// </summary>
	/// <param name="label">The label of the icon.</param>
	/// <param name="imageSize">The size of the image.</param>
	/// <param name="iconAlignment">The alignment of the image and label with respect to each other.</param>
	/// <returns>The calculated size of the widget.</returns>
	public static Vector2 CalcIconSize(string label, Vector2 imageSize, IconAlignment iconAlignment)
	{
		ImGuiStylePtr style = ImGui.GetStyle();
		Vector2 framePadding = style.FramePadding;
		Vector2 itemSpacing = style.ItemSpacing;
		Vector2 labelSize = ImGui.CalcTextSize(label);
		if (iconAlignment == IconAlignment.Horizontal)
		{
			Vector2 boundingBoxSize = imageSize + new Vector2(labelSize.X + itemSpacing.X, 0);
			boundingBoxSize.Y = Math.Max(boundingBoxSize.Y, labelSize.Y);
			return boundingBoxSize + (framePadding * 2);
		}
		else if (iconAlignment == IconAlignment.Vertical)
		{
			Vector2 boundingBoxSize = imageSize + new Vector2(0, labelSize.Y + itemSpacing.Y);
			boundingBoxSize.X = Math.Max(boundingBoxSize.X, labelSize.X);
			return boundingBoxSize + (framePadding * 2);
		}

		return imageSize;
	}

	/// <summary>
	/// Contains the implementation details for rendering icons.
	/// </summary>
	internal static class IconImpl
	{
		internal static bool Show(string label, uint textureId, Vector2 imageSize, IconAlignment iconAlignment, IconOptions options)
		{
			ArgumentNullException.ThrowIfNull(label);
			ArgumentNullException.ThrowIfNull(options);

			bool wasClicked = false;

			ImGuiStylePtr style = ImGui.GetStyle();
			Vector2 framePadding = style.FramePadding;
			Vector2 itemSpacing = style.ItemSpacing;

			ImGui.PushID(label);

			Vector2 cursorStartPos = ImGui.GetCursorScreenPos();
			Vector2 labelSize = ImGui.CalcTextSize(label);// TODO, maybe pass this to an internal overload of CalcIconSize to save recalculating
			Vector2 boundingBoxSize = CalcIconSize(label, imageSize, iconAlignment);

			ImGui.SetCursorScreenPos(cursorStartPos + framePadding);

			switch (iconAlignment)
			{
				case IconAlignment.Horizontal:
					HorizontalLayout(label, textureId, imageSize, labelSize, boundingBoxSize, itemSpacing, options.Color, cursorStartPos);
					break;
				case IconAlignment.Vertical:
					VerticalLayout(label, textureId, imageSize, labelSize, boundingBoxSize, itemSpacing, options.Color, cursorStartPos);
					break;
				default:
					throw new NotImplementedException();
			}

			ImGui.SetCursorScreenPos(cursorStartPos);
			ImGui.Dummy(boundingBoxSize);
			bool isHovered = ImGui.IsItemHovered();
			bool isMouseClicked = ImGui.IsMouseClicked(ImGuiMouseButton.Left);
			bool isMouseDoubleClicked = ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left);
			bool isRightMouseClicked = ImGui.IsMouseClicked(ImGuiMouseButton.Right);
			bool isRightMouseReleased = ImGui.IsMouseReleased(ImGuiMouseButton.Right);

			if (!string.IsNullOrEmpty(options.Tooltip))
			{
				ImGui.SetItemTooltip(options.Tooltip);
			}

			if (isHovered || EnableIconDebugDraw)
			{
				uint borderColor = ImGui.GetColorU32(ImGui.GetStyle().Colors[(int)ImGuiCol.Border]);
				ImDrawListPtr drawList = ImGui.GetWindowDrawList();
				drawList.AddRect(cursorStartPos, cursorStartPos + boundingBoxSize, ImGui.GetColorU32(borderColor));
			}

			if (isHovered)
			{
				if (isMouseClicked)
				{
					options.OnClick?.Invoke();
					wasClicked = true;
				}

				if (isMouseDoubleClicked)
				{
					options.OnDoubleClick?.Invoke();
				}

				if (isRightMouseClicked)
				{
					options.OnRightClick?.Invoke();
				}

				if (isRightMouseReleased && options.OnContextMenu is not null)
				{
					ImGui.OpenPopup($"{label}_Context");
				}
			}

			if (ImGui.BeginPopup($"{label}_Context"))
			{
				options.OnContextMenu?.Invoke();
				ImGui.EndPopup();
			}

			ImGui.PopID();

			return wasClicked;
		}

		private static void VerticalLayout(string label, uint textureId, Vector2 imageSize, Vector2 labelSize, Vector2 boundingBoxSize, Vector2 itemSpacing, Vector4 color = default, Vector2 cursorStartPos = default)
		{
			Vector2 imageTopLeft = cursorStartPos + new Vector2((boundingBoxSize.X - imageSize.X) / 2, 0);
			ImGui.SetCursorScreenPos(imageTopLeft);
			unsafe
			{
				if (color != default)
				{
					// Use transparent background with color as tint to preserve alpha
					ImGui.ImageWithBg(new ImTextureRef(texId: textureId), imageSize, Vector4.Zero, color);
				}
				else
				{
					ImGui.Image(new ImTextureRef(texId: textureId), imageSize);
				}
			}

			Vector2 labelTopLeft = cursorStartPos + new Vector2((boundingBoxSize.X - labelSize.X) / 2, imageSize.Y + itemSpacing.Y);
			ImGui.SetCursorScreenPos(labelTopLeft);
			ImGui.TextUnformatted(label);
		}

		private static void HorizontalLayout(string label, uint textureId, Vector2 imageSize, Vector2 labelSize, Vector2 boundingBoxSize, Vector2 itemSpacing, Vector4 color = default, Vector2 cursorStartPos = default)
		{
			unsafe
			{
				if (color != default)
				{
					// Use transparent background with color as tint to preserve alpha
					ImGui.ImageWithBg(new ImTextureRef(texId: textureId), imageSize, Vector4.Zero, color);
				}
				else
				{
					ImGui.Image(new ImTextureRef(texId: textureId), imageSize);
				}
			}
			Vector2 leftAlign = new(labelSize.X, boundingBoxSize.Y);
			ImGui.SetCursorScreenPos(cursorStartPos + new Vector2(imageSize.X + itemSpacing.X, 0));
			using (new Alignment.CenterWithin(labelSize, leftAlign))
			{
				ImGui.TextUnformatted(label);
			}
		}
	}
}
