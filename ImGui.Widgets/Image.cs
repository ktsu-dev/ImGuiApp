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
	/// Displays an image with the specified texture ID and size.
	/// </summary>
	/// <param name="textureId">The ID of the texture to display.</param>
	/// <param name="size">The size of the image.</param>
	/// <returns>True if the image is clicked; otherwise, false.</returns>
	public static bool Image(uint textureId, Vector2 size) => ImageImpl.Show(textureId, size, Vector4.One);

	/// <summary>
	/// Displays an image with the specified texture ID, size, and color.
	/// </summary>
	/// <param name="textureId">The ID of the texture to display.</param>
	/// <param name="size">The size of the image.</param>
	/// <param name="color">The color to apply to the image.</param>
	/// <returns>True if the image is clicked; otherwise, false.</returns>
	public static bool Image(uint textureId, Vector2 size, Vector4 color) => ImageImpl.Show(textureId, size, color);

	/// <summary>
	/// Displays a centered image with the specified texture ID and size.
	/// </summary>
	/// <param name="textureId">The ID of the texture to display.</param>
	/// <param name="size">The size of the image.</param>
	/// <returns>True if the image is clicked; otherwise, false.</returns>
	public static bool ImageCentered(uint textureId, Vector2 size) => ImageImpl.Centered(textureId, size, Vector4.One);

	/// <summary>
	/// Displays a centered image with the specified texture ID, size, and color.
	/// </summary>
	/// <param name="textureId">The ID of the texture to display.</param>
	/// <param name="size">The size of the image.</param>
	/// <param name="color">The color to apply to the image.</param>
	/// <returns>True if the image is clicked; otherwise, false.</returns>
	public static bool ImageCentered(uint textureId, Vector2 size, Vector4 color) => ImageImpl.Centered(textureId, size, color);

	/// <summary>
	/// Displays a centered image within a container with the specified texture ID, size, and container size.
	/// </summary>
	/// <param name="textureId">The ID of the texture to display.</param>
	/// <param name="size">The size of the image.</param>
	/// <param name="containerSize">The size of the container.</param>
	/// <returns>True if the image is clicked; otherwise, false.</returns>
	public static bool ImageCenteredWithin(uint textureId, Vector2 size, Vector2 containerSize) => ImageImpl.CenteredWithin(textureId, size, containerSize, Vector4.One);

	/// <summary>
	/// Displays a centered image within a container with the specified texture ID, size, container size, and color.
	/// </summary>
	/// <param name="textureId">The ID of the texture to display.</param>
	/// <param name="size">The size of the image.</param>
	/// <param name="containerSize">The size of the container.</param>
	/// <param name="color">The color to apply to the image.</param>
	/// <returns>True if the image is clicked; otherwise, false.</returns>
	public static bool ImageCenteredWithin(uint textureId, Vector2 size, Vector2 containerSize, Vector4 color) => ImageImpl.CenteredWithin(textureId, size, containerSize, color);

	internal static class ImageImpl
	{
		/// <summary>
		/// Displays an image with the specified texture ID and size.
		/// </summary>
		/// <param name="textureId">The ID of the texture to display.</param>
		/// <param name="size">The size of the image.</param>
		/// <returns>True if the image is clicked; otherwise, false.</returns>
		internal static bool Show(uint textureId, Vector2 size) => Show(textureId, size, Vector4.One);

		/// <summary>
		/// Displays an image with the specified texture ID, size, and color.
		/// </summary>
		/// <param name="textureId">The ID of the texture to display.</param>
		/// <param name="size">The size of the image.</param>
		/// <param name="color">The color to apply to the image.</param>
		/// <returns>True if the image is clicked; otherwise, false.</returns>
		internal static bool Show(uint textureId, Vector2 size, Vector4 color = default)
		{
			unsafe
			{
				if (color != default)
				{
					// Use transparent background with color as tint to preserve alpha
					ImGui.ImageWithBg(new ImTextureRef(texId: textureId), size, Vector4.Zero, color);
				}
				else
				{
					ImGui.Image(new ImTextureRef(texId: textureId), size);
				}
			}
			return ImGui.IsItemClicked();
		}

		/// <summary>
		/// Displays a centered image with the specified texture ID and size.
		/// </summary>
		/// <param name="textureId">The ID of the texture to display.</param>
		/// <param name="size">The size of the image.</param>
		/// <returns>True if the image is clicked; otherwise, false.</returns>
		internal static bool Centered(uint textureId, Vector2 size) => Centered(textureId, size, Vector4.One);

		/// <summary>
		/// Displays a centered image with the specified texture ID, size, and color.
		/// </summary>
		/// <param name="textureId">The ID of the texture to display.</param>
		/// <param name="size">The size of the image.</param>
		/// <param name="color">The color to apply to the image.</param>
		/// <returns>True if the image is clicked; otherwise, false.</returns>
		internal static bool Centered(uint textureId, Vector2 size, Vector4 color)
		{
			bool clicked = false;
			using (new Alignment.Center(size))
			{
				clicked = Show(textureId, size, color);
			}

			return clicked;
		}

		/// <summary>
		/// Displays a centered image within a container with the specified texture ID, size, and container size.
		/// </summary>
		/// <param name="textureId">The ID of the texture to display.</param>
		/// <param name="size">The size of the image.</param>
		/// <param name="containerSize">The size of the container.</param>
		/// <returns>True if the image is clicked; otherwise, false.</returns>
		internal static bool CenteredWithin(uint textureId, Vector2 size, Vector2 containerSize) => CenteredWithin(textureId, size, containerSize, Vector4.One);

		/// <summary>
		/// Displays a centered image within a container with the specified texture ID, size, container size, and color.
		/// </summary>
		/// <param name="textureId">The ID of the texture to display.</param>
		/// <param name="imageSize">The size of the image.</param>
		/// <param name="containerSize">The size of the container.</param>
		/// <param name="color">The color to apply to the image.</param>
		/// <returns>True if the image is clicked; otherwise, false.</returns>
		internal static bool CenteredWithin(uint textureId, Vector2 imageSize, Vector2 containerSize, Vector4 color)
		{
			bool clicked = false;
			using (new Alignment.CenterWithin(imageSize, containerSize))
			{
				clicked = Show(textureId, imageSize, color);
			}

			return clicked;
		}
	}
}
