// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Imaging;

using System.IO.Compression;

/// <summary>Settings for <see cref="PngEncoder"/>. The output is always eight-bit RGBA.</summary>
public sealed record PngEncoderOptions
{
	/// <summary>Gets the default settings: optimal compression, pixels written as they are.</summary>
	public static PngEncoderOptions Default { get; } = new();

	/// <summary>Gets how hard the image data is compressed. Defaults to <see cref="CompressionLevel.Optimal"/>.</summary>
	public CompressionLevel CompressionLevel { get; init; } = CompressionLevel.Optimal;

	/// <summary>
	/// Gets a value indicating whether the colour of every fully transparent pixel is written as black.
	/// </summary>
	/// <remarks>
	/// The image itself is not changed. A fully transparent pixel shows nothing whatever its colour, so
	/// zeroing it changes no rendering and usually compresses better.
	/// </remarks>
	public bool ClearTransparentPixels { get; init; }
}
