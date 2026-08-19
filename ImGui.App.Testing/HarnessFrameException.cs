// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Testing;

using System;

/// <summary>
/// Thrown when an application callback fails during a harness frame. Carries the frame number,
/// because a failure on frame two hundred of a drag is a different problem from one on frame zero.
/// </summary>
public sealed class HarnessFrameException : Exception
{
	/// <summary>Initializes a new instance of the <see cref="HarnessFrameException"/> class.</summary>
	public HarnessFrameException() => FrameNumber = -1;

	/// <summary>Initializes a new instance of the <see cref="HarnessFrameException"/> class.</summary>
	/// <param name="message">The error message.</param>
	public HarnessFrameException(string message)
		: base(message) => FrameNumber = -1;

	/// <summary>Initializes a new instance of the <see cref="HarnessFrameException"/> class.</summary>
	/// <param name="message">The error message.</param>
	/// <param name="innerException">The failure that occurred inside the frame.</param>
	public HarnessFrameException(string message, Exception innerException)
		: base(message, innerException) => FrameNumber = -1;

	/// <summary>Initializes a new instance of the <see cref="HarnessFrameException"/> class.</summary>
	/// <param name="frameNumber">The zero-based frame that failed.</param>
	/// <param name="innerException">The failure that occurred inside the frame.</param>
	public HarnessFrameException(int frameNumber, Exception innerException)
		: base($"The application threw during harness frame {frameNumber}.", innerException) =>
		FrameNumber = frameNumber;

	/// <summary>Gets the zero-based frame that failed, or minus one when it is not known.</summary>
	public int FrameNumber { get; }
}
