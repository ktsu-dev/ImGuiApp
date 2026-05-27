// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ForceDirectedLayout;

using System;
using System.Numerics;

/// <summary>
/// Adapter that lets the layout engine read and write the physics state of an arbitrary body type.
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
	Func<TBody, Vector2> GetPosition,
	Func<TBody, Vector2> GetDimensions,
	Func<TBody, Vector2> GetVelocity,
	Func<TBody, Vector2> GetForce,
	Func<TBody, bool> GetIsPinned,
	Func<TBody, Vector2, Vector2, Vector2, TBody> WithPhysicsState
);
