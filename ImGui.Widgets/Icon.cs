// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Widgets;

using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
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
		public Vector4 Color { get; init; } = Styler.Color.Palette.Neutral.White.Value;

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
	public static bool Icon(string label, nint textureId, float imageSize, IconAlignment iconAlignment) =>
		IconImpl.Show(label, textureId, new(imageSize, imageSize), iconAlignment, new());

	/// <summary>
	/// Renders an icon with the specified parameters.
	/// </summary>
	/// <param name="label">The label of the icon.</param>
	/// <param name="textureId">The texture ID of the icon.</param>
	/// <param name="imageSize">The size of the image.</param>
	/// <param name="iconAlignment">The alignment of the icon.</param>
	/// <returns>Was the icon bounds clicked</returns>
	public static bool Icon(string label, nint textureId, Vector2 imageSize, IconAlignment iconAlignment) =>
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
	public static bool Icon(string label, nint textureId, float imageSize, IconAlignment iconAlignment, IconOptions options) =>
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
	public static bool Icon(string label, nint textureId, Vector2 imageSize, IconAlignment iconAlignment, IconOptions options) =>
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
		Ensure.NotNull(label);

#pragma warning disable IDE0305
		string[] lines = label.Trim().Split('\n').Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();
#pragma warning restore IDE0305

		ImGuiStylePtr style = ImGui.GetStyle();
		Vector2 framePadding = style.FramePadding;
		Vector2 itemSpacing = style.ItemSpacing;

		Vector2 totalLabelSize = Vector2.Zero;
		for (int labelIndex = 0; labelIndex < lines.Length; labelIndex++)
		{
			string line = lines[labelIndex];
			Vector2 thisLabelSize = ImGui.CalcTextSize(line);

			totalLabelSize.X = Math.Max(totalLabelSize.X, thisLabelSize.X);
			totalLabelSize.Y += thisLabelSize.Y;

			bool isLastIndex = labelIndex == lines.Length - 1;
			if (!isLastIndex)
			{
				totalLabelSize.Y += itemSpacing.Y;
			}
		}

		switch (iconAlignment)
		{
			case IconAlignment.Horizontal:
			{
				Vector2 boundingBoxSize = imageSize + new Vector2(totalLabelSize.X + itemSpacing.X, 0);
				boundingBoxSize.Y = Math.Max(boundingBoxSize.Y, totalLabelSize.Y);
				return boundingBoxSize + (framePadding * 2);
			}
			case IconAlignment.Vertical:
			{
				Vector2 boundingBoxSize = imageSize + new Vector2(0, totalLabelSize.Y + itemSpacing.Y);
				boundingBoxSize.X = Math.Max(boundingBoxSize.X, totalLabelSize.X);
				return boundingBoxSize + (framePadding * 2);
			}
			default:
				throw new NotImplementedException($"CalcIconSize is not implemented for IconAlignment {iconAlignment}");
		}
	}

	/// <summary>
	/// Contains the implementation details for rendering icons.
	/// </summary>
	internal static class IconImpl
	{
		internal static bool Show(string text, nint textureId, Vector2 imageSize, IconAlignment iconAlignment, IconOptions options)
		{
			Ensure.NotNull(text);
			Ensure.NotNull(options);

#pragma warning disable IDE0305
			string[] lines = text.Trim().Split('\n').Where(line => !string.IsNullOrWhiteSpace(line)).ToArray();
#pragma warning restore IDE0305

			bool wasClicked = false;

			ImGuiStylePtr style = ImGui.GetStyle();
			Vector2 framePadding = style.FramePadding;
			Vector2 itemSpacing = style.ItemSpacing;

			ImGui.PushID(text);

			Vector2 cursorStartPos = ImGui.GetCursorScreenPos();

			Collection<Vector2> labelSizes = [];
			Vector2 boundingBoxSize = CalcIconSize(text, imageSize, iconAlignment);
			foreach (string line in lines)
			{
				labelSizes.Add(ImGui.CalcTextSize(line));// TODO, maybe pass this to an internal overload of CalcIconSize to save recalculating
			}

			ImGui.SetCursorScreenPos(cursorStartPos + framePadding);

			switch (iconAlignment)
			{
				case IconAlignment.Horizontal:
					HorizontalLayout(lines, textureId, imageSize, labelSizes, boundingBoxSize, itemSpacing, options.Color, cursorStartPos);
					break;
				case IconAlignment.Vertical:
					VerticalLayout(lines, textureId, imageSize, labelSizes, boundingBoxSize, itemSpacing, options.Color, cursorStartPos);
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
					ImGui.OpenPopup($"{text}_Context");
				}
			}

			if (ImGui.BeginPopup($"{text}_Context"))
			{
				options.OnContextMenu?.Invoke();
				ImGui.EndPopup();
			}

			ImGui.PopID();

			return wasClicked;
		}

		[SuppressMessage("Major Code Smell", "S6640:Make sure that using \"unsafe\" is safe here.", Justification = "Required for native ImGui interop; pointer is scoped to the call and not retained.")]
#pragma warning disable IDE0060
		private static void VerticalLayout(string[] lines, nint textureId, Vector2 imageSize, Collection<Vector2> labelSizes, Vector2 boundingBoxSize, Vector2 itemSpacing, Vector4 color = default, Vector2 cursorStartPos = default)
#pragma warning restore IDE0060
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

			for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
			{
				string label = lines[lineIndex];
				Vector2 labelSize = labelSizes[lineIndex];

				Vector2 textCursorPos = new(cursorStartPos.X, ImGui.GetCursorScreenPos().Y);
				ImGui.SetCursorScreenPos(textCursorPos);

				using (new Alignment.CenterWithin(labelSize, new Vector2(boundingBoxSize.X, labelSize.Y)))
				{
					ImGui.TextUnformatted(label);
				}
			}
		}

		[SuppressMessage("Major Code Smell", "S6640:Make sure that using \"unsafe\" is safe here.", Justification = "Required for native ImGui interop; pointer is scoped to the call and not retained.")]
#pragma warning disable IDE0060
		private static void HorizontalLayout(string[] labels, nint textureId, Vector2 imageSize, Collection<Vector2> labelSizes, Vector2 boundingBoxSize, Vector2 itemSpacing, Vector4 color = default, Vector2 cursorStartPos = default)
#pragma warning restore IDE0060
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

			float textBlockHeight = 0.0f;
			for (int labelIndex = 0; labelIndex < labels.Length; labelIndex++)
			{
				textBlockHeight += labelSizes[labelIndex].Y;
				if (labelIndex < labels.Length - 1)
				{
					textBlockHeight += itemSpacing.Y;
				}
			}

			float textStartX = cursorStartPos.X + imageSize.X + itemSpacing.X;
			float textStartY = cursorStartPos.Y + ((boundingBoxSize.Y - textBlockHeight) / 2.0f);
			ImGui.SetCursorScreenPos(new Vector2(textStartX, textStartY));

			for (int labelIndex = 0; labelIndex < labels.Length; labelIndex++)
			{
				float currentY = ImGui.GetCursorScreenPos().Y;
				ImGui.SetCursorScreenPos(new Vector2(textStartX, currentY));
				ImGui.TextUnformatted(labels[labelIndex]);
			}
		}
	}
}
