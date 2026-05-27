// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ForceDirectedLayout;

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

/// <summary>
/// Double-precision 2D vector used by the force-directed layout core.
/// Blittable POD layout so it can cross the C ABI boundary unchanged.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct Vec2D : IEquatable<Vec2D>
{
	/// <summary>X component.</summary>
	public double X;

	/// <summary>Y component.</summary>
	public double Y;

	/// <summary>Construct a vector with the given components.</summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vec2D(double x, double y)
	{
		X = x;
		Y = y;
	}

	/// <summary>Zero vector.</summary>
	public static Vec2D Zero
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => default;
	}

	/// <summary>Squared length. Cheaper than <see cref="Length"/> when only comparing magnitudes.</summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly double LengthSquared() => (X * X) + (Y * Y);

	/// <summary>Euclidean length.</summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly double Length() => Math.Sqrt((X * X) + (Y * Y));

	/// <summary>Linear interpolation from <paramref name="a"/> (t=0) to <paramref name="b"/> (t=1).</summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vec2D Lerp(Vec2D a, Vec2D b, double t) => new(a.X + ((b.X - a.X) * t), a.Y + ((b.Y - a.Y) * t));

	/// <summary>Component-wise addition.</summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vec2D operator +(Vec2D a, Vec2D b) => new(a.X + b.X, a.Y + b.Y);

	/// <summary>Component-wise subtraction.</summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vec2D operator -(Vec2D a, Vec2D b) => new(a.X - b.X, a.Y - b.Y);

	/// <summary>Scalar multiplication.</summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vec2D operator *(Vec2D v, double s) => new(v.X * s, v.Y * s);

	/// <summary>Scalar multiplication.</summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vec2D operator *(double s, Vec2D v) => new(v.X * s, v.Y * s);

	/// <summary>Scalar division.</summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vec2D operator /(Vec2D v, double s) => new(v.X / s, v.Y / s);

	/// <summary>Unary negation.</summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vec2D operator -(Vec2D v) => new(-v.X, -v.Y);

	/// <inheritdoc/>
	public static bool operator ==(Vec2D a, Vec2D b) => a.X == b.X && a.Y == b.Y;

	/// <inheritdoc/>
	public static bool operator !=(Vec2D a, Vec2D b) => !(a == b);

	/// <inheritdoc/>
	public readonly bool Equals(Vec2D other) => X == other.X && Y == other.Y;

	/// <inheritdoc/>
	public override readonly bool Equals(object? obj) => obj is Vec2D v && Equals(v);

	/// <inheritdoc/>
	public override readonly int GetHashCode() => HashCode.Combine(X, Y);

	/// <inheritdoc/>
	public override readonly string ToString() => $"({X}, {Y})";
}
