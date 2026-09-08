// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ForceDirectedLayout;

using System;

/// <summary>
/// Adapter that resolves an arbitrary edge type to a pair of body ids.
/// The "source" body is biased to the left and the "target" to the right when
/// <see cref="PhysicsSettings.DirectionalBias"/> is positive.
/// </summary>
/// <typeparam name="TEdge">Caller-defined edge type.</typeparam>
/// <param name="GetSourceBodyId">Returns the id of the body the edge originates from.</param>
/// <param name="GetTargetBodyId">Returns the id of the body the edge terminates at.</param>
/// <param name="GetSourcePinOffset">
/// Returns where the edge attaches to its source body, relative to that body's origin. Supply this
/// together with <paramref name="GetTargetPinOffset"/> and the forces that shape a link's length and
/// angle measure between the points a renderer joins, instead of between body centres - which on a
/// body with several rows of pins differ by most of its height. Leave both null and the edge is
/// treated as attaching at the bodies' centres.
/// </param>
/// <param name="GetTargetPinOffset">
/// Returns where the edge attaches to its target body, relative to that body's origin. See
/// <paramref name="GetSourcePinOffset"/>.
/// </param>
public sealed record EdgeAccessor<TEdge>(
	Func<TEdge, int> GetSourceBodyId,
	Func<TEdge, int> GetTargetBodyId,
	Func<TEdge, Vec2D>? GetSourcePinOffset = null,
	Func<TEdge, Vec2D>? GetTargetPinOffset = null
);
