// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

/// <summary>
/// A timecode frame rate: frames per second as numerator/denominator, and whether labels use
/// drop-frame counting.
/// </summary>
/// <remarks>
/// A rational rather than a float, so 23.976, 29.97 and 59.94 are held exactly as 24000/1001,
/// 30000/1001 and 60000/1001, and converting a frame count to seconds is not off by a rounding
/// error that grows with the length of the programme.
/// </remarks>
public readonly record struct TimecodeRate
{
	/// <summary>
	/// Creates a rate. Throws <see cref="ArgumentOutOfRangeException"/> unless both parts are positive,
	/// and <see cref="ArgumentException"/> when <paramref name="dropFrame"/> is set at a nominal rate
	/// other than 30 or 60.
	/// </summary>
	/// <param name="numerator">Frames per second numerator, e.g. 30000.</param>
	/// <param name="denominator">Frames per second denominator, e.g. 1001.</param>
	/// <param name="dropFrame">Whether frame labels skip numbers to track wall-clock time.</param>
	/// <exception cref="ArgumentOutOfRangeException">
	/// Either part is not positive, or the rate is below half a frame per second, which leaves no frame
	/// to number within a labelled second.
	/// </exception>
	/// <exception cref="ArgumentException">Drop-frame was requested at a nominal rate other than 30 or 60.</exception>
	public TimecodeRate(int numerator, int denominator, bool dropFrame = false)
	{
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(numerator);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(denominator);

		int nominal = RoundedRate(numerator, denominator);
		if (nominal <= 0)
		{
			throw new ArgumentOutOfRangeException(
				nameof(numerator),
				$"{numerator}/{denominator} frames per second rounds to no frames in a labelled second.");
		}

		if (dropFrame && nominal != 30 && nominal != 60)
		{
			throw new ArgumentException(
				$"Drop-frame counting exists only at 29.97 and 59.94 (nominal 30 and 60); {numerator}/{denominator} is nominally {nominal}.",
				nameof(dropFrame));
		}

		Numerator = numerator;
		Denominator = denominator;
		DropFrame = dropFrame;
	}

	/// <summary>Gets the frames per second numerator, e.g. 30000.</summary>
	public int Numerator { get; }

	/// <summary>Gets the frames per second denominator, e.g. 1001.</summary>
	public int Denominator { get; }

	/// <summary>Gets a value indicating whether frame labels skip numbers to track wall-clock time (29.97 / 59.94 only).</summary>
	public bool DropFrame { get; }

	/// <summary>Gets the frames in a labelled second: round(Numerator / Denominator), e.g. 30 for 29.97.</summary>
	public int NominalFramesPerSecond => RoundedRate(Numerator, Denominator);

	/// <summary>Gets <see cref="Numerator"/> / <see cref="Denominator"/> as a double.</summary>
	public double FramesPerSecond => Denominator > 0 ? (double)Numerator / Denominator : 0.0;

	/// <summary>Gets 23.976 fps (24000/1001).</summary>
	[SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "The underscore stands for the decimal point, which is how every NLE names this rate.")]
	public static TimecodeRate Fps23_976 { get; } = new(24000, 1001);

	/// <summary>Gets 24 fps.</summary>
	public static TimecodeRate Fps24 { get; } = new(24, 1);

	/// <summary>Gets 25 fps.</summary>
	public static TimecodeRate Fps25 { get; } = new(25, 1);

	/// <summary>Gets 29.97 fps (30000/1001) with drop-frame labels.</summary>
	[SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "The underscore stands for the decimal point, which is how every NLE names this rate.")]
	public static TimecodeRate Fps29_97Drop { get; } = new(30000, 1001, dropFrame: true);

	/// <summary>Gets 29.97 fps (30000/1001) with non-drop labels.</summary>
	[SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "The underscore stands for the decimal point, which is how every NLE names this rate.")]
	public static TimecodeRate Fps29_97NonDrop { get; } = new(30000, 1001);

	/// <summary>Gets 30 fps.</summary>
	public static TimecodeRate Fps30 { get; } = new(30, 1);

	/// <summary>Gets 48 fps.</summary>
	public static TimecodeRate Fps48 { get; } = new(48, 1);

	/// <summary>Gets 50 fps.</summary>
	public static TimecodeRate Fps50 { get; } = new(50, 1);

	/// <summary>Gets 59.94 fps (60000/1001) with drop-frame labels.</summary>
	[SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "The underscore stands for the decimal point, which is how every NLE names this rate.")]
	public static TimecodeRate Fps59_94Drop { get; } = new(60000, 1001, dropFrame: true);

	/// <summary>Gets 59.94 fps (60000/1001) with non-drop labels.</summary>
	[SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "The underscore stands for the decimal point, which is how every NLE names this rate.")]
	public static TimecodeRate Fps59_94NonDrop { get; } = new(60000, 1001);

	/// <summary>Gets 60 fps.</summary>
	public static TimecodeRate Fps60 { get; } = new(60, 1);

	/// <summary>Gets a value indicating whether this rate was built by the constructor rather than defaulted.</summary>
	internal bool IsValid => Numerator > 0 && Denominator > 0;

	/// <summary>
	/// Names the rate: "23.976", "24", "29.97 DF", "29.97", "59.94 DF", … (three decimals, trailing
	/// zeros trimmed, invariant culture).
	/// </summary>
	/// <returns>The rate's name.</returns>
	public override string ToString() =>
		FramesPerSecond.ToString("0.###", CultureInfo.InvariantCulture) + (DropFrame ? " DF" : string.Empty);

	// Round half up, in integers, so a rate a hair either side of a whole number is not decided by
	// floating point.
	private static int RoundedRate(int numerator, int denominator) =>
		denominator <= 0 ? 0 : (int)((((long)numerator * 2) + denominator) / ((long)denominator * 2));
}

/// <summary>
/// Conversions between frame counts, timecode text and seconds. Pure; no ImGui.
/// </summary>
/// <remarks>
/// Drop-frame is only a labelling scheme: at 29.97 it skips the labels <c>;00</c> and <c>;01</c> at
/// the start of every minute except each tenth, so an hour of labels lasts an hour of wall-clock
/// time. No frames are dropped. Everything here therefore works on frame counts, and the scheme
/// exists only in <see cref="FromFrame"/> and <see cref="ToFrame"/>.
/// </remarks>
public static class Timecode
{
	/// <summary>
	/// Formats a frame count as <c>HH:MM:SS:FF</c> (<c>HH:MM:SS;FF</c> for drop-frame). Negative counts
	/// get a leading '-'. Hours are not wrapped at 24.
	/// </summary>
	/// <param name="frame">The frame count.</param>
	/// <param name="rate">The frame rate.</param>
	/// <returns>The timecode text.</returns>
	/// <exception cref="ArgumentException"><paramref name="rate"/> is <see langword="default"/>.</exception>
	public static string Format(int frame, TimecodeRate rate)
	{
		EnsureValid(rate, nameof(rate));

		// Through long, so int.MinValue has a magnitude to format.
		long magnitude = Math.Abs((long)frame);
		(long hours, int minutes, int seconds, int frames) = Label(magnitude, rate);

		int frameDigits = Math.Max(2, (rate.NominalFramesPerSecond - 1).ToString(CultureInfo.InvariantCulture).Length);

		return string.Concat(
			frame < 0 ? "-" : string.Empty,
			hours.ToString("00", CultureInfo.InvariantCulture),
			":",
			minutes.ToString("00", CultureInfo.InvariantCulture),
			":",
			seconds.ToString("00", CultureInfo.InvariantCulture),
			rate.DropFrame ? ";" : ":",
			frames.ToString(CultureInfo.InvariantCulture).PadLeft(frameDigits, '0'));
	}

	/// <summary>
	/// Parses typed timecode into a frame count. Returns <see langword="false"/> for text that names no
	/// frame at this rate.
	/// </summary>
	/// <remarks>
	/// Whitespace is trimmed and an optional leading '-' negates the result. <c>:</c>, <c>;</c> and
	/// <c>.</c> are interchangeable separators, and one to four fields are read right-aligned, so
	/// <c>"1:00"</c> is one second. Bare digits, up to eight, are split into pairs from the right, so
	/// <c>"10000"</c> is one minute. A label drop-frame skips is rejected.
	/// </remarks>
	/// <param name="text">The typed text.</param>
	/// <param name="rate">The frame rate.</param>
	/// <param name="frame">The frame count, or 0 when parsing fails.</param>
	/// <returns><see langword="true"/> when <paramref name="text"/> names a frame.</returns>
	/// <exception cref="ArgumentException"><paramref name="rate"/> is <see langword="default"/>.</exception>
	public static bool TryParse(string? text, TimecodeRate rate, out int frame)
	{
		EnsureValid(rate, nameof(rate));
		frame = 0;

		if (text is null)
		{
			return false;
		}

		ReadOnlySpan<char> body = text.AsSpan().Trim();
		bool negative = body.Length > 0 && body[0] == '-';
		if (negative)
		{
			body = body[1..];
		}

		if (body.IsEmpty || !TryReadFields(body, out int[] fields))
		{
			return false;
		}

		// Right-aligned: the last field is always the frames.
		int count = fields.Length;
		int frames = fields[count - 1];
		int seconds = count > 1 ? fields[count - 2] : 0;
		int minutes = count > 2 ? fields[count - 3] : 0;
		int hours = count > 3 ? fields[count - 4] : 0;

		if (!TryToFrame(hours, minutes, seconds, frames, rate, out long result) || result > int.MaxValue)
		{
			return false;
		}

		frame = negative ? (int)-result : (int)result;
		return true;
	}

	/// <summary>
	/// Frame count of a labelled time. Throws <see cref="ArgumentOutOfRangeException"/> for an invalid
	/// or dropped label.
	/// </summary>
	/// <param name="hours">Hours, zero or more.</param>
	/// <param name="minutes">Minutes, 0 to 59.</param>
	/// <param name="seconds">Seconds, 0 to 59.</param>
	/// <param name="frames">Frames, 0 to <see cref="TimecodeRate.NominalFramesPerSecond"/> - 1.</param>
	/// <param name="rate">The frame rate.</param>
	/// <returns>The frame count.</returns>
	/// <exception cref="ArgumentException"><paramref name="rate"/> is <see langword="default"/>.</exception>
	/// <exception cref="ArgumentOutOfRangeException">
	/// A field is out of range, the label is one drop-frame skips, or the frame count is beyond the int range.
	/// </exception>
	public static int ToFrame(int hours, int minutes, int seconds, int frames, TimecodeRate rate)
	{
		EnsureValid(rate, nameof(rate));

		return TryToFrame(hours, minutes, seconds, frames, rate, out long result) && result <= int.MaxValue
			? (int)result
			: throw new ArgumentOutOfRangeException(
				nameof(frames),
				$"{hours}:{minutes}:{seconds}:{frames} is not a frame label at {rate}.");
	}

	/// <summary>The label of a non-negative frame count.</summary>
	/// <param name="frame">The frame count, zero or more.</param>
	/// <param name="rate">The frame rate.</param>
	/// <returns>The hours, minutes, seconds and frames of the label.</returns>
	/// <exception cref="ArgumentException"><paramref name="rate"/> is <see langword="default"/>.</exception>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="frame"/> is negative.</exception>
	public static (int Hours, int Minutes, int Seconds, int Frames) FromFrame(int frame, TimecodeRate rate)
	{
		EnsureValid(rate, nameof(rate));
		ArgumentOutOfRangeException.ThrowIfNegative(frame);

		(long hours, int minutes, int seconds, int frames) = Label(frame, rate);
		return ((int)hours, minutes, seconds, frames);
	}

	/// <summary>Wall-clock seconds at the start of a frame: frame * Denominator / Numerator.</summary>
	/// <param name="frame">The frame count.</param>
	/// <param name="rate">The frame rate.</param>
	/// <returns>The time in seconds.</returns>
	/// <exception cref="ArgumentException"><paramref name="rate"/> is <see langword="default"/>.</exception>
	public static double ToSeconds(int frame, TimecodeRate rate)
	{
		EnsureValid(rate, nameof(rate));
		return (double)frame * rate.Denominator / rate.Numerator;
	}

	/// <summary>
	/// The nearest frame to a time, rounding halves away from zero, saturating at the int range.
	/// Throws <see cref="ArgumentException"/> for a non-finite time.
	/// </summary>
	/// <param name="seconds">The time in seconds.</param>
	/// <param name="rate">The frame rate.</param>
	/// <returns>The frame count.</returns>
	/// <exception cref="ArgumentException"><paramref name="rate"/> is <see langword="default"/>, or <paramref name="seconds"/> is not finite.</exception>
	public static int FromSeconds(double seconds, TimecodeRate rate)
	{
		EnsureValid(rate, nameof(rate));

		if (!double.IsFinite(seconds))
		{
			throw new ArgumentException($"A time of {seconds} seconds names no frame.", nameof(seconds));
		}

		double frames = Math.Round(seconds * rate.Numerator / rate.Denominator, MidpointRounding.AwayFromZero);

		return frames >= int.MaxValue ? int.MaxValue
			: frames <= int.MinValue ? int.MinValue
			: (int)frames;
	}

	/// <summary>Throws when a rate was defaulted rather than constructed.</summary>
	/// <param name="rate">The rate to check.</param>
	/// <param name="paramName">The caller's parameter name.</param>
	/// <exception cref="ArgumentException"><paramref name="rate"/> is not a valid rate.</exception>
	internal static void EnsureValid(TimecodeRate rate, string paramName)
	{
		if (!rate.IsValid)
		{
			throw new ArgumentException(
				$"The timecode rate {rate.Numerator}/{rate.Denominator} is not a frame rate; construct one or use a preset such as TimecodeRate.Fps25.",
				paramName);
		}
	}

	/// <summary>The label of a non-negative frame count, with the hours kept wide.</summary>
	private static (long Hours, int Minutes, int Seconds, int Frames) Label(long frame, TimecodeRate rate)
	{
		long n = rate.NominalFramesPerSecond;
		long drop = DroppedPerMinute(rate);
		long labelFrames = frame;

		if (drop > 0)
		{
			long framesPerMinute = (60 * n) - drop;
			long framesPer10Minutes = (600 * n) - (9 * drop);
			long tens = frame / framesPer10Minutes;
			long rest = frame % framesPer10Minutes;

			labelFrames = frame + (9 * drop * tens) + (rest < drop ? 0 : drop * ((rest - drop) / framesPerMinute));
		}

		return (
			labelFrames / (3600 * n),
			(int)(labelFrames / (60 * n) % 60),
			(int)(labelFrames / n % 60),
			(int)(labelFrames % n));
	}

	/// <summary>The frame count of a label, or <see langword="false"/> when the label is not one.</summary>
	private static bool TryToFrame(int hours, int minutes, int seconds, int frames, TimecodeRate rate, out long result)
	{
		result = 0;
		long n = rate.NominalFramesPerSecond;
		long drop = DroppedPerMinute(rate);

		// Every rate holds at least 3600 frames of labels an hour, so more hours than this is beyond
		// the int range whatever the rate, and refusing them first keeps the arithmetic inside long.
		if (hours < 0 || hours > int.MaxValue / 3600
			|| minutes is < 0 or >= 60
			|| seconds is < 0 or >= 60
			|| frames < 0 || frames >= n)
		{
			return false;
		}

		if (seconds == 0 && frames < drop && minutes % 10 != 0)
		{
			return false;
		}

		long totalMinutes = (60L * hours) + minutes;
		result = (n * ((3600L * hours) + (60L * minutes) + seconds)) + frames - (drop * (totalMinutes - (totalMinutes / 10)));
		return true;
	}

	/// <summary>Labels skipped each minute: 2 at 29.97 DF, 4 at 59.94 DF, 0 otherwise.</summary>
	private static long DroppedPerMinute(TimecodeRate rate) => rate.DropFrame ? rate.NominalFramesPerSecond / 15 : 0;

	/// <summary>Splits the text into all-digit fields, right-aligned, or fails.</summary>
	private static bool TryReadFields(ReadOnlySpan<char> body, out int[] fields)
	{
		fields = [];
		bool separated = body.IndexOfAny(":;.") >= 0;

		if (!separated)
		{
			if (body.Length > 8 || !IsAllDigits(body))
			{
				return false;
			}

			// Pairs from the right: "10000" is 1, 00, 00.
			int count = (body.Length + 1) / 2;
			fields = new int[count];
			int end = body.Length;
			for (int i = count - 1; i >= 0; i--)
			{
				int start = Math.Max(0, end - 2);
				fields[i] = int.Parse(body[start..end], NumberStyles.None, CultureInfo.InvariantCulture);
				end = start;
			}

			return true;
		}

		string[] parts = body.ToString().Split(':', ';', '.');
		if (parts.Length > 4)
		{
			return false;
		}

		fields = new int[parts.Length];
		for (int i = 0; i < parts.Length; i++)
		{
			if (parts[i].Length == 0
				|| !IsAllDigits(parts[i])
				|| !int.TryParse(parts[i], NumberStyles.None, CultureInfo.InvariantCulture, out fields[i]))
			{
				return false;
			}
		}

		return true;
	}

	private static bool IsAllDigits(ReadOnlySpan<char> text)
	{
		foreach (char character in text)
		{
			if (!char.IsAsciiDigit(character))
			{
				return false;
			}
		}

		return true;
	}
}
