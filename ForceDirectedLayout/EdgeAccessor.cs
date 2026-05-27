// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

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
public sealed record EdgeAccessor<TEdge>(
	Func<TEdge, int> GetSourceBodyId,
	Func<TEdge, int> GetTargetBodyId
);
