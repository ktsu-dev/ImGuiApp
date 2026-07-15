// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Widgets;

using System.Diagnostics.CodeAnalysis;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Color;
using ktsu.ImGui.Styler;

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
	public static bool Image(nint textureId, Vector2 size) => ImageImpl.Show(textureId, size, ImGuiVector4.One);

	/// <summary>
	/// Displays an image with the specified texture ID, size, and color.
	/// </summary>
	/// <param name="textureId">The ID of the texture to display.</param>
	/// <param name="size">The size of the image.</param>
	/// <param name="color">The color to apply to the image.</param>
	/// <returns>True if the image is clicked; otherwise, false.</returns>
	public static bool Image(nint textureId, Vector2 size, ImGuiVector4 color) => ImageImpl.Show(textureId, size, color);

	/// <summary>
	/// Displays a centered image with the specified texture ID and size.
	/// </summary>
	/// <param name="textureId">The ID of the texture to display.</param>
	/// <param name="size">The size of the image.</param>
	/// <returns>True if the image is clicked; otherwise, false.</returns>
	public static bool ImageCentered(nint textureId, Vector2 size) => ImageImpl.Centered(textureId, size, ImGuiVector4.One);

	/// <summary>
	/// Displays a centered image with the specified texture ID, size, and color.
	/// </summary>
	/// <param name="textureId">The ID of the texture to display.</param>
	/// <param name="size">The size of the image.</param>
	/// <param name="color">The color to apply to the image.</param>
	/// <returns>True if the image is clicked; otherwise, false.</returns>
	public static bool ImageCentered(nint textureId, Vector2 size, ImGuiVector4 color) => ImageImpl.Centered(textureId, size, color);

	/// <summary>
	/// Displays a centered image within a container with the specified texture ID, size, and container size.
	/// </summary>
	/// <param name="textureId">The ID of the texture to display.</param>
	/// <param name="size">The size of the image.</param>
	/// <param name="containerSize">The size of the container.</param>
	/// <returns>True if the image is clicked; otherwise, false.</returns>
	public static bool ImageCenteredWithin(nint textureId, Vector2 size, Vector2 containerSize) => ImageImpl.CenteredWithin(textureId, size, containerSize, ImGuiVector4.One);

	/// <summary>
	/// Displays a centered image within a container with the specified texture ID, size, container size, and color.
	/// </summary>
	/// <param name="textureId">The ID of the texture to display.</param>
	/// <param name="size">The size of the image.</param>
	/// <param name="containerSize">The size of the container.</param>
	/// <param name="color">The color to apply to the image.</param>
	/// <returns>True if the image is clicked; otherwise, false.</returns>
	public static bool ImageCenteredWithin(nint textureId, Vector2 size, Vector2 containerSize, ImGuiVector4 color) => ImageImpl.CenteredWithin(textureId, size, containerSize, color);

	internal static class ImageImpl
	{
		/// <summary>
		/// Displays an image with the specified texture ID and size.
		/// </summary>
		/// <param name="textureId">The ID of the texture to display.</param>
		/// <param name="size">The size of the image.</param>
		/// <returns>True if the image is clicked; otherwise, false.</returns>
		internal static bool Show(nint textureId, Vector2 size) => Show(textureId, size, ImGuiVector4.One);

		/// <summary>
		/// Displays an image with the specified texture ID, size, and color.
		/// </summary>
		/// <param name="textureId">The ID of the texture to display.</param>
		/// <param name="size">The size of the image.</param>
		/// <param name="color">The color to apply to the image.</param>
		/// <returns>True if the image is clicked; otherwise, false.</returns>
		[SuppressMessage("Major Code Smell", "S3427:Method overloads with default parameter values should not overlap", Justification = "The no-arg overload intentionally uses ImGuiVector4.One as default, distinct from the default(ImGuiVector4) sentinel; both are needed for correct behavior.")]
		[SuppressMessage("Major Code Smell", "S6640:Make sure that using \"unsafe\" is safe here", Justification = "Required for native ImGui/OpenGL interop; pointers are scoped to the call and not retained.")]
		internal static bool Show(nint textureId, Vector2 size, ImGuiVector4 color = default)
		{
			unsafe
			{
				if (color != default)
				{
					// Use transparent background with color as tint to preserve alpha
					ImGui.ImageWithBg(new ImTextureRef(texId: textureId), size, ImGuiVector4.Zero, color);
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
		internal static bool Centered(nint textureId, Vector2 size) => Centered(textureId, size, ImGuiVector4.One);

		/// <summary>
		/// Displays a centered image with the specified texture ID, size, and color.
		/// </summary>
		/// <param name="textureId">The ID of the texture to display.</param>
		/// <param name="size">The size of the image.</param>
		/// <param name="color">The color to apply to the image.</param>
		/// <returns>True if the image is clicked; otherwise, false.</returns>
		internal static bool Centered(nint textureId, Vector2 size, ImGuiVector4 color)
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
		internal static bool CenteredWithin(nint textureId, Vector2 size, Vector2 containerSize) => CenteredWithin(textureId, size, containerSize, ImGuiVector4.One);

		/// <summary>
		/// Displays a centered image within a container with the specified texture ID, size, container size, and color.
		/// </summary>
		/// <param name="textureId">The ID of the texture to display.</param>
		/// <param name="imageSize">The size of the image.</param>
		/// <param name="containerSize">The size of the container.</param>
		/// <param name="color">The color to apply to the image.</param>
		/// <returns>True if the image is clicked; otherwise, false.</returns>
		internal static bool CenteredWithin(nint textureId, Vector2 imageSize, Vector2 containerSize, ImGuiVector4 color)
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
