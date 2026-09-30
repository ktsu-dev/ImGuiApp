// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;

/// <summary>
/// Asks whether a float a widget holds was rewritten, as opposed to whether two measurements agree.
/// </summary>
/// <remarks>
/// A widget that reports "changed" is saying it replaced the caller's value, which is a question about
/// identity rather than closeness. A tolerance is the wrong answer to it: it would swallow a one-ulp drag
/// and report nothing changed when something did. Comparing bit patterns keeps the meaning exactly,
/// including a NaN the caller passed in counting as replaced by whatever finite value it became.
/// </remarks>
internal static class FloatBits
{
	/// <summary>Whether <paramref name="a"/> and <paramref name="b"/> are different values, bit for bit.</summary>
	/// <param name="a">One value.</param>
	/// <param name="b">The other value.</param>
	/// <returns><see langword="true"/> unless the two have identical bit patterns.</returns>
	public static bool Differ(float a, float b) => BitConverter.SingleToInt32Bits(a) != BitConverter.SingleToInt32Bits(b);
}
