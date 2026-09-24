// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Images;

/// <summary>
/// The in-memory arrangement of 8-bit pixels that a texture can be created or updated from. Textures
/// are always stored as RGBA8; any other layout is converted on the way in.
/// </summary>
/// <remarks>
/// Named for the byte order in memory, not for a packed integer's bit order: <see cref="Bgra8"/> is
/// the bytes B, G, R, A at increasing addresses, which is what SkiaSharp calls <c>Bgra8888</c>,
/// OpenCV's four-channel <c>Mat</c> holds, and Windows DIBs use.
/// </remarks>
public enum PixelLayout
{
	/// <summary>Four bytes per pixel: red, green, blue, alpha.</summary>
	Rgba8,

	/// <summary>Four bytes per pixel: blue, green, red, alpha.</summary>
	Bgra8,

	/// <summary>Three bytes per pixel: red, green, blue. Alpha is taken as opaque.</summary>
	Rgb8,

	/// <summary>Three bytes per pixel: blue, green, red. Alpha is taken as opaque. OpenCV's default three-channel order.</summary>
	Bgr8,

	/// <summary>One byte per pixel of luminance, copied to red, green and blue. Alpha is taken as opaque.</summary>
	Gray8,
}
