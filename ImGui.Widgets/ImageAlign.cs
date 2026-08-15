// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System.Diagnostics.CodeAnalysis;
using System.Numerics;

using Hexa.NET.ImGui;

using HexaImageHelper = Hexa.NET.ImGui.Widgets.ImageHelper;

public static partial class ImGuiWidgets
{
	/// <summary>
	/// Draws an image centred vertically within the current line.
	/// </summary>
	/// <param name="textureId">Native texture handle to draw.</param>
	/// <param name="size">Size to draw the image at, in pixels.</param>
	[SuppressMessage("Major Code Smell", "S6640:Make sure that using \"unsafe\" is safe here", Justification = "Required for native Hexa.NET.ImGui image interop (ImTextureRef); the block is scoped to the draw call and retains no pointers.")]
	public static void ImageCenteredV(nint textureId, Vector2 size)
	{
		unsafe
		{
			HexaImageHelper.ImageCenteredV(new ImTextureRef(texId: textureId), size);
		}
	}

	/// <summary>
	/// Draws an image centred horizontally within the available content region.
	/// </summary>
	/// <param name="textureId">Native texture handle to draw.</param>
	/// <param name="size">Size to draw the image at, in pixels.</param>
	[SuppressMessage("Major Code Smell", "S6640:Make sure that using \"unsafe\" is safe here", Justification = "Required for native Hexa.NET.ImGui image interop (ImTextureRef); the block is scoped to the draw call and retains no pointers.")]
	public static void ImageCenteredH(nint textureId, Vector2 size)
	{
		unsafe
		{
			HexaImageHelper.ImageCenteredH(new ImTextureRef(texId: textureId), size);
		}
	}

	/// <summary>
	/// Draws an image centred both vertically and horizontally within the available content region.
	/// </summary>
	/// <param name="textureId">Native texture handle to draw.</param>
	/// <param name="size">Size to draw the image at, in pixels.</param>
	[SuppressMessage("Major Code Smell", "S6640:Make sure that using \"unsafe\" is safe here", Justification = "Required for native Hexa.NET.ImGui image interop (ImTextureRef); the block is scoped to the draw call and retains no pointers.")]
	public static void ImageCenteredVH(nint textureId, Vector2 size)
	{
		unsafe
		{
			HexaImageHelper.ImageCenteredVH(new ImTextureRef(texId: textureId), size);
		}
	}

	/// <summary>
	/// Draws an image scaled to fit inside a destination box while preserving its aspect ratio.
	/// </summary>
	/// <param name="textureId">Native texture handle to draw.</param>
	/// <param name="imageSize">The image's natural size, in pixels.</param>
	/// <param name="destinationSize">The box to fit the image inside, in pixels.</param>
	[SuppressMessage("Major Code Smell", "S6640:Make sure that using \"unsafe\" is safe here", Justification = "Required for native Hexa.NET.ImGui image interop (ImTextureRef); the block is scoped to the draw call and retains no pointers.")]
	public static void ImageScaleTo(nint textureId, Vector2 imageSize, Vector2 destinationSize)
	{
		unsafe
		{
			HexaImageHelper.ImageScaleTo(new ImTextureRef(texId: textureId), imageSize, destinationSize);
		}
	}
}
