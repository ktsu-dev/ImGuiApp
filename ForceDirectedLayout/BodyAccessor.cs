// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ForceDirectedLayout;

using System;

/// <summary>
/// Adapter that lets the layout engine read and write the physics state of an arbitrary body type.
/// All vector quantities use double precision (<see cref="Vec2D"/>); callers using
/// <see cref="System.Numerics.Vector2"/> at the consumer level should convert at the accessor boundary.
/// </summary>
/// <typeparam name="TBody">Caller-defined body type.</typeparam>
/// <param name="GetId">Returns a stable integer id used to identify the body across frames and edges.</param>
/// <param name="GetPosition">Returns the body's top-left position in layout space.</param>
/// <param name="GetDimensions">Returns the body's width/height. Used for centering and ordering.</param>
/// <param name="GetVelocity">Returns the body's current velocity.</param>
/// <param name="GetForce">Returns the body's last-applied net force. Used so callers can render force debug overlays.</param>
/// <param name="GetIsPinned">Returns true when the body should not move during simulation but still exerts forces on others.</param>
/// <param name="WithPhysicsState">Returns a new (or mutated) body with the given position, velocity, and force applied.</param>
public sealed record BodyAccessor<TBody>(
	Func<TBody, int> GetId,
	Func<TBody, Vec2D> GetPosition,
	Func<TBody, Vec2D> GetDimensions,
	Func<TBody, Vec2D> GetVelocity,
	Func<TBody, Vec2D> GetForce,
	Func<TBody, bool> GetIsPinned,
	Func<TBody, Vec2D, Vec2D, Vec2D, TBody> WithPhysicsState
);
