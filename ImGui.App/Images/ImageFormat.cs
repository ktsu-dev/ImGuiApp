// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Images;

/// <summary>The image container formats <see cref="ImageDecoder"/> recognises.</summary>
public enum ImageFormat
{
	/// <summary>The data matches none of the supported formats.</summary>
	Unknown,

	/// <summary>Portable Network Graphics.</summary>
	Png,

	/// <summary>JFIF or EXIF JPEG.</summary>
	Jpeg,

	/// <summary>Windows bitmap.</summary>
	Bmp,

	/// <summary>Truevision TGA.</summary>
	Tga,
}
