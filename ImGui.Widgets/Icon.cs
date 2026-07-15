// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Widgets;

using System.Collections.ObjectModel;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Styler;

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
		public Vector4 Color { get; init; } = Styler.Palette.Neutral.White.Value;

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
	/// <param name="textBlock">The text of the icon.</param>
	/// <param name="textureId">The texture ID of the icon.</param>
	/// <param name="imageSize">The size of the image.</param>
	/// <param name="iconAlignment">The alignment of the icon.</param>
	/// <returns>Was the icon bounds clicked</returns>
	public static bool Icon(string textBlock, nint textureId, float imageSize, IconAlignment iconAlignment) =>
		IconImpl.Show(textBlock, textureId, new(imageSize, imageSize), iconAlignment, new());

	/// <summary>
	/// Renders an icon with the specified parameters.
	/// </summary>
	/// <param name="textBlock">The text of the icon.</param>
	/// <param name="textureId">The texture ID of the icon.</param>
	/// <param name="imageSize">The size of the image.</param>
	/// <param name="iconAlignment">The alignment of the icon.</param>
	/// <returns>Was the icon bounds clicked</returns>
	public static bool Icon(string textBlock, nint textureId, Vector2 imageSize, IconAlignment iconAlignment) =>
		IconImpl.Show(textBlock, textureId, imageSize, iconAlignment, new());

	/// <summary>
	/// Renders an icon with the specified parameters.
	/// </summary>
	/// <param name="textBlock">The text of the icon.</param>
	/// <param name="textureId">The texture ID of the icon.</param>
	/// <param name="imageSize">The size of the image.</param>
	/// <param name="iconAlignment">The alignment of the icon.</param>
	/// <param name="options">Additional options</param>
	/// <returns>Was the icon bounds clicked</returns>
	public static bool Icon(string textBlock, nint textureId, float imageSize, IconAlignment iconAlignment, IconOptions options) =>
		IconImpl.Show(textBlock, textureId, new(imageSize, imageSize), iconAlignment, options);

	/// <summary>
	/// Renders an icon with the specified parameters.
	/// </summary>
	/// <param name="textBlock">The text of the icon.</param>
	/// <param name="textureId">The texture ID of the icon.</param>
	/// <param name="imageSize">The size of the image.</param>
	/// <param name="iconAlignment">The alignment of the icon.</param>
	/// <param name="options">Additional options</param>
	/// <returns>Was the icon bounds clicked</returns>
	public static bool Icon(string textBlock, nint textureId, Vector2 imageSize, IconAlignment iconAlignment, IconOptions options) =>
		IconImpl.Show(textBlock, textureId, imageSize, iconAlignment, options);

	/// <summary>
	/// Calculates the size of the icon with the specified parameters.
	/// </summary>
	/// <param name="textBlock">The text of the icon.</param>
	/// <param name="imageSize">The size of the image.</param>
	/// <param name="iconAlignment">The alignment of the icon.</param>
	/// <param name="itemSpacing">The spacing between items.</param>
	/// <param name="framePadding">The padding of the frame.</param>
	/// <returns>The calculated size of the icon.</returns>
	public static Vector2 CalcIconSize(string textBlock, float imageSize, IconAlignment iconAlignment, Vector2 itemSpacing, Vector2 framePadding) =>
		CalcIconSize(CalcTextBlockSize(textBlock, itemSpacing), new Vector2(imageSize), iconAlignment, itemSpacing, framePadding);

	/// <summary>
	/// Calculates the size of the icon with the specified parameters.
	/// </summary>
	/// <param name="textBlockSize">The size of the text block of the icon.</param>
	/// <param name="imageSize">The size of the image.</param>
	/// <param name="iconAlignment">The alignment of the icon.</param>
	/// <param name="itemSpacing">The spacing between items.</param>
	/// <param name="framePadding">The padding of the frame.</param>
	/// <returns>The calculated size of the icon.</returns>
	public static Vector2 CalcIconSize(Vector2 textBlockSize, float imageSize, IconAlignment iconAlignment, Vector2 itemSpacing, Vector2 framePadding) => CalcIconSize(textBlockSize, new Vector2(imageSize), iconAlignment, itemSpacing, framePadding);

	/// <summary>
	/// Calculates the size of the icon with the specified parameters.
	/// </summary>
	/// <param name="textBlockSize">The size of the text block of the icon.</param>
	/// <param name="imageSize">The size of the image.</param>
	/// <param name="itemSpacing">The spacing between items.</param>
	/// <param name="framePadding">The padding of the frame.</param>
	/// <returns>The calculated size of the icon.</returns>
	public static Vector2 CalcIconSize(Vector2 textBlockSize, Vector2 imageSize, Vector2 itemSpacing, Vector2 framePadding) => CalcIconSize(textBlockSize, imageSize, IconAlignment.Horizontal, itemSpacing, framePadding);

	/// <summary>
	/// Calculates the size of the icon with the specified parameters.
	/// </summary>
	/// <param name="textBlockSize">The size of the text block of the icon.</param>
	/// <param name="imageSize">The size of the image.</param>
	/// <param name="iconAlignment">The alignment of the image and text block with respect to each other.</param>
	/// <param name="itemSpacing">The spacing between items.</param>
	/// <param name="framePadding">The padding of the frame.</param>
	/// <returns>The calculated size of the widget.</returns>
	public static Vector2 CalcIconSize(Vector2 textBlockSize, Vector2 imageSize, IconAlignment iconAlignment, Vector2 itemSpacing, Vector2 framePadding)
	{
		switch (iconAlignment)
		{
			case IconAlignment.Horizontal:
			{
				Vector2 boundingBoxSize = imageSize + new Vector2(textBlockSize.X + itemSpacing.X, 0);
				boundingBoxSize.Y = Math.Max(boundingBoxSize.Y, textBlockSize.Y);
				return boundingBoxSize + (framePadding * 2);
			}
			case IconAlignment.Vertical:
			{
				Vector2 boundingBoxSize = imageSize + new Vector2(0, textBlockSize.Y + itemSpacing.Y);
				boundingBoxSize.X = Math.Max(boundingBoxSize.X, textBlockSize.X);
				return boundingBoxSize + (framePadding * 2);
			}
			default:
				throw new NotImplementedException($"CalcIconSize is not implemented for IconAlignment {iconAlignment}");
		}
	}

	internal static IEnumerable<(string, Vector2)> GetLinesWithSizes(string textBlock) =>
		textBlock
		.Trim().Split('\n')
		.Where(line => !string.IsNullOrWhiteSpace(line))
		.Select(line => (line, ImGui.CalcTextSize(line)));

	internal static Vector2 CalcTextBlockSize(string textBlock, Vector2 itemSpacing) =>
		CalcTextBlockSize(GetLinesWithSizes(textBlock), itemSpacing);

	internal static Vector2 CalcTextBlockSize(IEnumerable<(string, Vector2)> linesWithSizes, Vector2 itemSpacing)
	{
		float textBlockWidth = 0.0f;
		float textBlockHeight = 0.0f;
		float postTextYOffset = 0.0f; // Initialized to zero and only populated if there are lines
		foreach ((string line, Vector2 lineSize) in linesWithSizes)
		{
			textBlockWidth = Math.Max(textBlockWidth, lineSize.X);
			textBlockHeight += lineSize.Y;
			postTextYOffset = itemSpacing.Y;
			textBlockHeight += postTextYOffset;
		}

		// if there are lines, remove the last spacing
		// if there are no lines, we never added any spacing so theres nothing to remove
		textBlockHeight -= postTextYOffset;

		return new(textBlockWidth, textBlockHeight);
	}

	/// <summary>
	/// Contains the implementation details for rendering icons.
	/// </summary>
	internal static class IconImpl
	{
		internal static bool Show(string textBlock, nint textureId, Vector2 imageSize, IconAlignment iconAlignment, IconOptions options)
		{
			Ensure.NotNull(textBlock);
			Ensure.NotNull(options);

			IEnumerable<(string, Vector2)> linesWithSizes = GetLinesWithSizes(textBlock);

			bool wasClicked = false;

			ImGuiStylePtr style = ImGui.GetStyle();
			Vector2 framePadding = style.FramePadding;
			Vector2 itemSpacing = style.ItemSpacing;
			Vector2 textBlockSize = CalcTextBlockSize(linesWithSizes, itemSpacing);

			ImGui.PushID(textBlock);

			Vector2 cursorStartPos = ImGui.GetCursorScreenPos();

			Collection<Vector2> labelSizes = [];
			Vector2 boundingBoxSize = CalcIconSize(textBlockSize, imageSize, iconAlignment, itemSpacing, framePadding);

			ImGui.SetCursorScreenPos(cursorStartPos + framePadding);

			switch (iconAlignment)
			{
				case IconAlignment.Horizontal:
					HorizontalLayout(linesWithSizes, textureId, imageSize, boundingBoxSize, itemSpacing, options.Color, cursorStartPos);
					break;
				case IconAlignment.Vertical:
					VerticalLayout(linesWithSizes, textureId, imageSize, boundingBoxSize, options.Color, cursorStartPos);
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
					ImGui.OpenPopup($"{textBlock}_Context");
				}
			}

			if (ImGui.BeginPopup($"{textBlock}_Context"))
			{
				options.OnContextMenu?.Invoke();
				ImGui.EndPopup();
			}

			ImGui.PopID();

			return wasClicked;
		}

		private static void VerticalLayout(IEnumerable<(string, Vector2)> linesWithSizes, nint textureId, Vector2 imageSize, Vector2 boundingBoxSize, Vector4 color = default, Vector2 cursorStartPos = default)
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

			foreach ((string line, Vector2 lineSize) in linesWithSizes)
			{
				Vector2 textCursorPos = new(cursorStartPos.X, ImGui.GetCursorScreenPos().Y);
				ImGui.SetCursorScreenPos(textCursorPos);

				using (new Alignment.CenterWithin(lineSize, new Vector2(boundingBoxSize.X, lineSize.Y)))
				{
					ImGui.TextUnformatted(line);
				}
			}
		}

		private static void HorizontalLayout(IEnumerable<(string, Vector2)> linesWithSizes, nint textureId, Vector2 imageSize, Vector2 boundingBoxSize, Vector2 itemSpacing, Vector4 color = default, Vector2 cursorStartPos = default)
		{
			Vector2 imageTopLeft = cursorStartPos + new Vector2(0, (boundingBoxSize.Y - imageSize.Y) / 2);
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

			Vector2 textBlockSize = CalcTextBlockSize(linesWithSizes, itemSpacing);

			float textStartX = cursorStartPos.X + imageSize.X + itemSpacing.X;
			float textStartY = cursorStartPos.Y + ((boundingBoxSize.Y - textBlockSize.Y) / 2.0f);
			ImGui.SetCursorScreenPos(new Vector2(textStartX, textStartY));

			foreach ((string line, Vector2 lineSize) in linesWithSizes)
			{
				float currentY = ImGui.GetCursorScreenPos().Y;
				ImGui.SetCursorScreenPos(new Vector2(textStartX, currentY));
				ImGui.TextUnformatted(line);
			}
		}
	}
}
