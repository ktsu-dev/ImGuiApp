// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Images;

using System;

/// <summary>
/// Thrown when image data is malformed, truncated, or uses a feature <see cref="ImageDecoder"/>
/// does not implement.
/// </summary>
public sealed class InvalidImageDataException : Exception
{
	/// <summary>Initializes a new instance of the <see cref="InvalidImageDataException"/> class.</summary>
	public InvalidImageDataException()
	{
	}

	/// <summary>Initializes a new instance of the <see cref="InvalidImageDataException"/> class.</summary>
	/// <param name="message">A description of what was wrong with the data.</param>
	public InvalidImageDataException(string message)
		: base(message)
	{
	}

	/// <summary>Initializes a new instance of the <see cref="InvalidImageDataException"/> class.</summary>
	/// <param name="message">A description of what was wrong with the data.</param>
	/// <param name="innerException">The underlying failure.</param>
	public InvalidImageDataException(string message, Exception innerException)
		: base(message, innerException)
	{
	}
}
