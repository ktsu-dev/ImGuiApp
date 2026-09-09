// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ForceDirectedLayout;

using System;

/// <summary>
/// Non-generic, double-precision force-directed simulation core.
/// Operates on contiguous arrays of <see cref="BodyState"/> and <see cref="EdgeRef"/>.
/// Allocation-free in the steady state - all working buffers are pooled for the lifetime of the instance.
/// </summary>
/// <remarks>
/// This is the AOT-friendly algorithm layer. It contains no reflection, no generics, no managed-only types,
/// and no allocations per step. It is the implementation underneath both the generic managed facade
/// (<see cref="ForceDirectedLayout{TBody, TEdge}"/>) and the C ABI exports.
/// </remarks>
public sealed class LayoutCore
{
	/// <summary>Overlap depth below which <see cref="SeparateOverlaps"/> leaves a pair alone.</summary>
	private const double OverlapEpsilon = 0.01;

	/// <summary>
	/// Horizontal clearance an edge needs per unit of vertical drop, as a fraction of that drop, for its
	/// rendered curve to stay inside the channel between its two endpoint bodies.
	/// </summary>
	/// <remarks>
	/// ImNodes renders a link as a cubic bezier whose inner control points are offset horizontally by
	/// <c>0.25 * length</c> from each pin. Writing <c>gap</c> for the clear horizontal span between the
	/// source's right edge and the target's left edge, the curve's x-coordinate is monotonic - it never
	/// doubles back over either body - exactly when <c>gap >= 0.25 * length</c>. Substituting
	/// <c>length = sqrt(gap^2 + dy^2)</c> and solving gives <c>gap >= |dy| / sqrt(15)</c>, so the ratio
	/// below is <c>1 / sqrt(15)</c>, a cap of about 75.5 degrees off horizontal. Links are drawn beneath
	/// the node backgrounds, so a curve that doubles back is a curve that disappears.
	/// </remarks>
	public const double BezierClearanceRatio = 0.2581988897471611;

	private BodyState[] bodies = [];

	/// <summary>
	/// Per-body flag: this body is mid-untangle along X - an endpoint of a backward edge, which has to
	/// travel horizontally past its partner. Set from scratch every substep.
	/// </summary>
	private bool[] untanglingInX = [];

	/// <summary>
	/// Per-body flag: this body is mid-untangle along Y - the far end of two links crossing each other,
	/// which have to swap vertically. Set from scratch every substep.
	/// </summary>
	private bool[] untanglingInY = [];
	private int bodyCount;

	private EdgeRef[] edges = [];
	private int edgeCount;

	/// <summary>Head of the per-body list of edges arriving at that body; -1 for none.</summary>
	private int[] edgesIntoBody = [];

	/// <summary>Head of the per-body list of edges leaving that body; -1 for none.</summary>
	private int[] edgesOutOfBody = [];

	/// <summary>Next link in the <see cref="edgesIntoBody"/> chain, indexed by edge.</summary>
	private int[] nextEdgeInto = [];

	/// <summary>Next link in the <see cref="edgesOutOfBody"/> chain, indexed by edge.</summary>
	private int[] nextEdgeOutOf = [];

	/// <summary>Simulation settings. Mutate between frames as needed.</summary>
	public LayoutSettings Settings { get; set; } = LayoutSettings.Defaults;

	/// <summary>World origin in body-position space. Gravity blends toward this point per <see cref="LayoutSettings.OriginAnchorWeight"/>.</summary>
	public Vec2D WorldOrigin { get; set; }

	/// <summary>Last computed gravity target (blend of centroid and world origin). Published for debug rendering.</summary>
	public Vec2D GravityCenter { get; private set; }

	/// <summary>Total kinetic energy in the system after the last <see cref="Step"/>.</summary>
	public double TotalSystemEnergy { get; private set; }

	/// <summary>True when <see cref="TotalSystemEnergy"/> is below <see cref="LayoutSettings.StabilityThreshold"/>.</summary>
	public bool IsStable { get; private set; }

	/// <summary>Substep count and per-substep delta time from the last <see cref="Step"/>.</summary>
	public (int SubstepCount, double SubstepDeltaTime) LastStepInfo { get; private set; }

	/// <summary>Mutable view of the current bodies. Length equals <see cref="BodyCount"/>.</summary>
	public Span<BodyState> Bodies => bodies.AsSpan(0, bodyCount);

	/// <summary>Number of active bodies.</summary>
	public int BodyCount => bodyCount;

	/// <summary>Mutable view of the current edges. Length equals <see cref="EdgeCount"/>.</summary>
	public Span<EdgeRef> Edges => edges.AsSpan(0, edgeCount);

	/// <summary>Number of active edges.</summary>
	public int EdgeCount => edgeCount;

	/// <summary>
	/// Resize the body buffer to hold <paramref name="count"/> entries.
	/// Existing entries (up to the new count) are preserved. Caller is expected to populate <see cref="Bodies"/> after.
	/// </summary>
	public void ResizeBodies(int count)
	{
		if (count < 0)
		{
			throw new ArgumentOutOfRangeException(nameof(count));
		}

		if (bodies.Length < count)
		{
			Array.Resize(ref bodies, count);
			Array.Resize(ref untanglingInX, count);
			Array.Resize(ref untanglingInY, count);
			Array.Resize(ref edgesIntoBody, count);
			Array.Resize(ref edgesOutOfBody, count);
		}
		bodyCount = count;
	}

	/// <summary>
	/// Resize the edge buffer to hold <paramref name="count"/> entries.
	/// Existing entries (up to the new count) are preserved. Caller is expected to populate <see cref="Edges"/> after.
	/// </summary>
	public void ResizeEdges(int count)
	{
		if (count < 0)
		{
			throw new ArgumentOutOfRangeException(nameof(count));
		}

		if (edges.Length < count)
		{
			Array.Resize(ref edges, count);
			Array.Resize(ref nextEdgeInto, count);
			Array.Resize(ref nextEdgeOutOf, count);
		}
		edgeCount = count;
	}

	/// <summary>
	/// Set <see cref="WorldOrigin"/> to the centroid of the current bodies' centers.
	/// </summary>
	public void InitializeWorldOriginToCentroid()
	{
		if (bodyCount == 0)
		{
			WorldOrigin = Vec2D.Zero;
			return;
		}

		Vec2D centroid = Vec2D.Zero;
		for (int i = 0; i < bodyCount; i++)
		{
			centroid += bodies[i].Position + (bodies[i].Dimensions * 0.5);
		}
		WorldOrigin = centroid / bodyCount;
	}

	/// <summary>
	/// Advance the simulation by <paramref name="deltaTime"/> seconds.
	/// </summary>
	public void Step(double deltaTime)
	{
		if (Settings.Enabled == 0 || bodyCount == 0)
		{
			LastStepInfo = (0, 0.0);
			return;
		}

		double targetTimestep = 1.0 / Settings.TargetPhysicsHz;
		int numberOfSubsteps = Math.Max(1, (int)Math.Ceiling(deltaTime / targetTimestep));
		double substepDt = deltaTime / numberOfSubsteps;

		LastStepInfo = (numberOfSubsteps, substepDt);

		for (int substep = 0; substep < numberOfSubsteps; substep++)
		{
			ResetForces();

			CalculateRepulsionForces();
			CalculateLinkForces();
			CalculateLinkFlatteningForces();
			CalculateLinkUntwistForces();
			CalculateDirectionalForces();
			CalculateGravityForces();

			IntegrateMotion(substepDt);

			ApplyDirectionalConstraints();
			SeparateOverlaps();
			RecentreOnOrigin();
		}

		double energy = 0.0;
		for (int i = 0; i < bodyCount; i++)
		{
			energy += bodies[i].Velocity.LengthSquared();
		}
		TotalSystemEnergy = energy;
		IsStable = energy < Settings.StabilityThreshold;
	}

	private void ResetForces()
	{
		for (int i = 0; i < bodyCount; i++)
		{
			bodies[i].Force = Vec2D.Zero;
		}
	}

	/// <summary>
	/// Push every pair of bodies apart, inverse-square in the clear space between their rectangles.
	/// </summary>
	/// <remarks>
	/// The distance is the one between the two closest points on the pair's bounding boxes, not the one
	/// between their centres. A centre measurement measures the wrong thing: it counts each body's own
	/// extent as part of the distance between them, so a wide node reads as far from a neighbour pressed
	/// against its side, while two small ones read as crowded with a screen of empty space between them.
	/// The same setting then spaces a graph differently depending only on how big its nodes happen to be,
	/// and the nodes of a node editor are every size from a literal to a class. What a reader sees is the
	/// clear space, and the clear space is what this works on.
	/// <para>
	/// That distance is zero along any axis the two overlap on, so for a pair sharing a row it is their
	/// horizontal gap alone and for a pair sharing a column their vertical gap alone. It is what holds a
	/// tall node's neighbour off by as much room beside its corner as beside its middle.
	/// </para>
	/// <para>
	/// The direction stays along the line between the centres, because it is the room a pair has that the
	/// closest points establish and not the way they should go. Taking the direction from them as well
	/// makes every force between a pair sharing a row exactly horizontal and every force between a pair
	/// sharing a column exactly vertical, which leaves repulsion unable to move a body diagonally out of
	/// another's way: measured over ten starting arrangements of the graph in
	/// <c>ForceLayoutTests.CounterGraph</c>, that draws around half again as many links across bodies
	/// they are not an end of, and leaves the edges several degrees steeper.
	/// </para>
	/// <para>
	/// Since the distance no longer includes the bodies' own extents it is much the smaller number, so
	/// the same spacing needs a smaller <see cref="LayoutSettings.RepulsionStrength"/> than a centre
	/// measurement did. The default was halved to match, and a caller carrying a value tuned against the
	/// old measurement should expect to do the same.
	/// </para>
	/// </remarks>
	private void CalculateRepulsionForces()
	{
		// Floored above zero, not just taken as given. The clamp below is what stops an inverse-square
		// force exploding when two boxes touch, and a caller who sets MinRepulsionDistance to zero
		// removes it: the clear distance between touching boxes is exactly zero, so the division
		// yields infinity, the integrator carries that into a NaN position, and every metric taken
		// afterwards reads NaN rather than "bad". A layout that silently becomes NaN is worse than one
		// that is merely crowded, so zero means "as small as this can safely be" rather than nothing.
		double minDist = Math.Max(Settings.MinRepulsionDistance, MinimumRepulsionClamp);
		double strength = Settings.RepulsionStrength;

		for (int i = 0; i < bodyCount; i++)
		{
			for (int j = i + 1; j < bodyCount; j++)
			{
				Vec2D aCenter = bodies[i].Position + (bodies[i].Dimensions * 0.5);
				Vec2D bCenter = bodies[j].Position + (bodies[j].Dimensions * 0.5);

				Vec2D direction = aCenter - bCenter;
				double dist = direction.Length();

				if (dist < 0.1)
				{
					continue;
				}

				// Inverse-square, clamped at MinRepulsionDistance to prevent explosions when bodies
				// touch - which is where the clear distance reaches zero, rather than where the bodies
				// are coincident.
				double effectiveDist = Math.Max(ClearDistance(i, j, direction), minDist);
				double magnitude = strength / (effectiveDist * effectiveDist);
				Vec2D force = direction * (magnitude / dist);

				bodies[i].Force += force;
				bodies[j].Force -= force;
			}
		}
	}

	/// <summary>
	/// Smallest separation the inverse-square repulsion is ever evaluated at, whatever
	/// <see cref="LayoutSettings.MinRepulsionDistance"/> says.
	/// </summary>
	/// <remarks>
	/// Small enough that it changes nothing for any usable setting, and positive so the division can
	/// never be by zero.
	/// </remarks>
	private const double MinimumRepulsionClamp = 0.001;

	/// <summary>
	/// The distance between the two closest points on two bodies' bounding boxes: their gap along each
	/// axis they are disjoint on, and zero once they touch or overlap on both.
	/// </summary>
	/// <param name="i">Index of the first body.</param>
	/// <param name="j">Index of the second body.</param>
	/// <param name="between">Offset between the two centres, which every caller has already computed.</param>
	private double ClearDistance(int i, int j, Vec2D between)
	{
		Vec2D clearance = (bodies[i].Dimensions + bodies[j].Dimensions) * 0.5;
		double gapX = Math.Max(Math.Abs(between.X) - clearance.X, 0.0);
		double gapY = Math.Max(Math.Abs(between.Y) - clearance.Y, 0.0);

		return Math.Sqrt((gapX * gapX) + (gapY * gapY));
	}

	/// <summary>
	/// The two points an edge actually joins: its pin positions when the caller supplied them, and the
	/// two body centres when it did not.
	/// </summary>
	/// <remarks>
	/// A renderer draws a link between pins, not between centres, and on a node with several rows of
	/// pins those differ by most of the node's height. Every force that reasons about a link's length
	/// or its angle has to use the same two points the link is drawn between, or it is shaping
	/// something the user cannot see.
	/// </remarks>
	private (Vec2D Source, Vec2D Target) EdgeEndpoints(int e)
	{
		int s = edges[e].SourceIndex;
		int t = edges[e].TargetIndex;

		if (edges[e].HasPinOffsets != 0)
		{
			return (bodies[s].Position + edges[e].SourcePinOffset,
				bodies[t].Position + edges[e].TargetPinOffset);
		}

		return (bodies[s].Position + (bodies[s].Dimensions * 0.5),
			bodies[t].Position + (bodies[t].Dimensions * 0.5));
	}

	private void CalculateLinkForces()
	{
		double restLength = Settings.RestLinkLength;
		double springK = Settings.LinkSpringStrength;

		for (int e = 0; e < edgeCount; e++)
		{
			int s = edges[e].SourceIndex;
			int t = edges[e].TargetIndex;
			if ((uint)s >= (uint)bodyCount || (uint)t >= (uint)bodyCount)
			{
				continue;
			}

			(Vec2D sourcePin, Vec2D targetPin) = EdgeEndpoints(e);

			Vec2D direction = targetPin - sourcePin;
			double currentLength = direction.Length();
			if (currentLength <= 0.1)
			{
				continue;
			}

			// Hooke's law: force proportional to displacement from rest length.
			double extension = currentLength - restLength;
			double magnitude = springK * extension;
			Vec2D force = direction * (magnitude / currentLength);

			bodies[s].Force += force;
			bodies[t].Force -= force;
		}
	}

	/// <summary>
	/// Pull an edge towards horizontal, by two means. A levelling force closes the vertical offset
	/// between its two ends continuously, which is what makes a link lie flat. On top of that, a
	/// horizontal splay opens the clear span between their facing edges whenever it is too narrow for
	/// the rendered curve, per <see cref="BezierClearanceRatio"/> - that one is a floor, switching off
	/// once the curve is safe, so it guarantees a link stays visible without ever levelling it.
	/// Both are soft, balanced against the link spring, so equilibrium settles near the target rather
	/// than exactly on it, and neither can make every edge in a graph horizontal at once.
	/// </summary>
	/// <summary>
	/// The two points this edge's curve is drawn between, for the passes that shape its angle.
	/// </summary>
	/// <remarks>
	/// With pin offsets supplied these are the pins themselves. Without them the fallback is the pair a
	/// node editor implies - the source's right edge and the target's left edge, each at its body's
	/// mid-height - rather than the body centres <see cref="EdgeEndpoints"/> falls back to. Centres
	/// would put both points inside their bodies and overstate the horizontal room a curve has.
	/// </remarks>
	private (Vec2D Source, Vec2D Target) FlattenedEndpoints(int e)
	{
		int s = edges[e].SourceIndex;
		int t = edges[e].TargetIndex;

		if (edges[e].HasPinOffsets != 0)
		{
			return (bodies[s].Position + edges[e].SourcePinOffset,
				bodies[t].Position + edges[e].TargetPinOffset);
		}

		return (new Vec2D(bodies[s].Position.X + bodies[s].Dimensions.X, bodies[s].Position.Y + (bodies[s].Dimensions.Y * 0.5)),
			new Vec2D(bodies[t].Position.X, bodies[t].Position.Y + (bodies[t].Dimensions.Y * 0.5)));
	}

	private void CalculateLinkFlatteningForces()
	{
		double strength = Settings.LinkFlatteningStrength;
		if (strength <= 0)
		{
			return;
		}

		double margin = Settings.LinkFlatteningMargin;

		for (int e = 0; e < edgeCount; e++)
		{
			int s = edges[e].SourceIndex;
			int t = edges[e].TargetIndex;
			if ((uint)s >= (uint)bodyCount || (uint)t >= (uint)bodyCount)
			{
				continue;
			}

			// The angle and the clearance are properties of the drawn curve, so both are measured between
			// the points the curve actually joins.
			(Vec2D sourcePin, Vec2D targetPin) = FlattenedEndpoints(e);
			double gap = targetPin.X - sourcePin.X;
			double verticalDrop = Math.Abs(targetPin.Y - sourcePin.Y);

			// Which way round the two bodies sit is a property of the bodies, not of where a link happens
			// to attach, so the ordering test stays on their centres.
			double sourceCenterX = bodies[s].Position.X + (bodies[s].Dimensions.X * 0.5);
			double targetCenterX = bodies[t].Position.X + (bodies[t].Dimensions.X * 0.5);

			// Prefer horizontal: close the vertical offset between the two ends, always, in proportion to
			// how far apart they sit. The clearance splay below only fires once a curve is at risk of
			// hiding, which keeps a link legal without ever making it flat; this is what lays it flat.
			// A backward edge is exempt - it is still being reordered, and pulling it level would fight
			// the vertical slide that reorder needs.
			if (targetCenterX > sourceCenterX)
			{
				double levelling = strength * (targetPin.Y - sourcePin.Y);
				bodies[s].Force += new Vec2D(0, levelling);
				bodies[t].Force += new Vec2D(0, -levelling);
			}

			double required = (verticalDrop * BezierClearanceRatio) + margin;
			double violation = required - gap;
			if (violation <= 0)
			{
				continue;
			}

			double forceX = strength * violation;
			bodies[s].Force += new Vec2D(-forceX, 0);
			bodies[t].Force += new Vec2D(forceX, 0);
		}
	}

	/// <summary>
	/// Puts two links that share a node into the same vertical order as the pins they attach to.
	/// </summary>
	/// <remarks>
	/// Two links arriving at one node from two different bodies cross whenever the bodies sit in the
	/// opposite vertical order to the pins they arrive at: the upper body's link has to dive under the
	/// lower body's to reach the lower pin. Nothing else in the simulation can see this. Every other force
	/// acts on one link or one pair of bodies at a time, and each of these two links is individually
	/// short, level and well spaced - they are only wrong about each other.
	/// <para>
	/// Measured over forty starting arrangements of a graph the size of a small class, this is what a
	/// settled tangle is made of: 5.83 twisted pairs against 5.85 crossings between links sharing a node,
	/// a one-to-one match, and nearly three times the 2.15 crossings between links sharing nothing.
	/// </para>
	/// <para>
	/// The correction is vertical and equal and opposite, so it swaps the far ends without moving the
	/// pair's centre or disturbing the left-to-right ordering. Unlike a clearance force it does not decay
	/// as it succeeds: it holds full strength right up to the moment the two ends draw level and switches
	/// off only once they have passed, which is what carries the swap through instead of stalling
	/// half-done against the springs.
	/// </para>
	/// <para>
	/// Links whose pin offsets are unknown fall back to their bodies' mid-heights, which gives both links
	/// at a shared node the same pin height and no order to preserve, so this does nothing - correctly,
	/// since without pin data there is no pin order to be wrong about.
	/// </para>
	/// </remarks>
	private void CalculateLinkUntwistForces()
	{
		// Cleared here rather than with the forces, so the passes that run before this one read the flag
		// as it stood a substep ago. A body moves at most MaxVelocity times the substep in between.
		Array.Clear(untanglingInY, 0, bodyCount);

		double strength = Settings.LinkUntwistStrength;
		if (strength <= 0)
		{
			return;
		}

		BuildEdgeBuckets();

		for (int b = 0; b < bodyCount; b++)
		{
			UntwistSharedEnds(edgesIntoBody[b], nextEdgeInto, sharedAtTarget: true, strength);
			UntwistSharedEnds(edgesOutOfBody[b], nextEdgeOutOf, sharedAtTarget: false, strength);
		}
	}

	/// <summary>
	/// Untwists every pair of edges in one body's list against each other.
	/// </summary>
	/// <param name="first">Head of the list, or -1 when the body has no edges on this side.</param>
	/// <param name="next">The chain to follow, indexed by edge.</param>
	/// <param name="sharedAtTarget">True when the list is of edges arriving, false when leaving.</param>
	/// <param name="strength">Force per unit of vertical swap still to be made.</param>
	private void UntwistSharedEnds(int first, int[] next, bool sharedAtTarget, double strength)
	{
		for (int a = first; a >= 0; a = next[a])
		{
			(Vec2D aSource, Vec2D aTarget) = FlattenedEndpoints(a);
			Vec2D aNear = sharedAtTarget ? aTarget : aSource;
			Vec2D aFar = sharedAtTarget ? aSource : aTarget;
			int aFarBody = sharedAtTarget ? edges[a].SourceIndex : edges[a].TargetIndex;

			for (int b = next[a]; b >= 0; b = next[b])
			{
				int bFarBody = sharedAtTarget ? edges[b].SourceIndex : edges[b].TargetIndex;

				// Two links from one body to another cannot be untwisted by moving bodies.
				if (bFarBody == aFarBody)
				{
					continue;
				}

				(Vec2D bSource, Vec2D bTarget) = FlattenedEndpoints(b);
				Vec2D bNear = sharedAtTarget ? bTarget : bSource;
				Vec2D bFar = sharedAtTarget ? bSource : bTarget;

				double atPins = aNear.Y - bNear.Y;
				double atFarEnds = aFar.Y - bFar.Y;

				// Same order at both ends, or level at one of them, and the two do not cross.
				if (atPins * atFarEnds >= 0)
				{
					continue;
				}

				// Full strength until the far ends draw level, and past it: the swap is only over once
				// they have changed places, not once they have stopped being far apart.
				double swap = strength * (atPins - atFarEnds);
				bodies[aFarBody].Force += new Vec2D(0, swap);
				bodies[bFarBody].Force -= new Vec2D(0, swap);

				untanglingInY[aFarBody] = true;
				untanglingInY[bFarBody] = true;
			}
		}
	}

	/// <summary>
	/// Groups edges by the body at each end, so untwisting compares only the pairs that share one.
	/// </summary>
	/// <remarks>
	/// Comparing every edge with every other would be quadratic in the edge count, which for a graph of
	/// any size is far more work than the quadratic-in-bodies repulsion. Only edges meeting at a body can
	/// be twisted about each other, and bucketing makes the pass cost the sum of the squared degrees.
	/// </remarks>
	private void BuildEdgeBuckets()
	{
		for (int b = 0; b < bodyCount; b++)
		{
			edgesIntoBody[b] = -1;
			edgesOutOfBody[b] = -1;
		}

		for (int e = 0; e < edgeCount; e++)
		{
			int s = edges[e].SourceIndex;
			int t = edges[e].TargetIndex;
			nextEdgeInto[e] = -1;
			nextEdgeOutOf[e] = -1;

			if ((uint)s >= (uint)bodyCount || (uint)t >= (uint)bodyCount)
			{
				continue;
			}

			nextEdgeInto[e] = edgesIntoBody[t];
			edgesIntoBody[t] = e;
			nextEdgeOutOf[e] = edgesOutOfBody[s];
			edgesOutOfBody[s] = e;
		}
	}

	private void CalculateDirectionalForces()
	{
		// Cleared here for the same reason the untwist flag is: the passes ahead of this one read it as
		// it stood a substep ago.
		Array.Clear(untanglingInX, 0, bodyCount);

		double bias = Settings.DirectionalBias;
		if (bias <= 0)
		{
			return;
		}

		for (int e = 0; e < edgeCount; e++)
		{
			int s = edges[e].SourceIndex;
			int t = edges[e].TargetIndex;
			if ((uint)s >= (uint)bodyCount || (uint)t >= (uint)bodyCount)
			{
				continue;
			}

			double sourceCenterX = bodies[s].Position.X + (bodies[s].Dimensions.X * 0.5);
			double targetCenterX = bodies[t].Position.X + (bodies[t].Dimensions.X * 0.5);

			double minGap = ((bodies[s].Dimensions.X + bodies[t].Dimensions.X) * 0.5) + 20.0;
			double currentGap = targetCenterX - sourceCenterX;

			// A backward edge has to get its endpoints past one another. SeparateOverlaps reads this to
			// let them go around, rather than holding them apart on the very axis the swap travels.
			// Recorded here because this is the pass that already knows which way each edge runs; it is
			// read a substep later, by which time a body has moved at most MaxVelocity * dt.
			if (currentGap < 0)
			{
				untanglingInX[s] = true;
				untanglingInX[t] = true;
			}

			double violation = minGap - currentGap;

			if (violation > 0)
			{
				double forceX = bias * violation;
				if (currentGap < 0)
				{
					forceX *= 1.0 + (Math.Abs(currentGap) / minGap);
				}

				bodies[s].Force += new Vec2D(-forceX, 0);
				bodies[t].Force += new Vec2D(forceX, 0);
			}
		}
	}

	private void ApplyDirectionalConstraints()
	{
		double bias = Settings.DirectionalBias;
		if (bias <= 0)
		{
			return;
		}

		for (int e = 0; e < edgeCount; e++)
		{
			int s = edges[e].SourceIndex;
			int t = edges[e].TargetIndex;
			if ((uint)s >= (uint)bodyCount || (uint)t >= (uint)bodyCount)
			{
				continue;
			}

			double sourceCenterX = bodies[s].Position.X + (bodies[s].Dimensions.X * 0.5);
			double targetCenterX = bodies[t].Position.X + (bodies[t].Dimensions.X * 0.5);

			if (sourceCenterX > targetCenterX)
			{
				double overlap = sourceCenterX - targetCenterX;
				double correction = overlap * bias * 0.05;

				bool sourceMovable = bodies[s].IsPinned == 0 && bodies[s].IsFrozen == 0;
				bool targetMovable = bodies[t].IsPinned == 0 && bodies[t].IsFrozen == 0;

				if (sourceMovable && targetMovable)
				{
					bodies[s].Position += new Vec2D(-correction, 0);
					bodies[t].Position += new Vec2D(correction, 0);
				}
				else if (sourceMovable)
				{
					bodies[s].Position += new Vec2D(-correction * 2.0, 0);
				}
				else if (targetMovable)
				{
					bodies[t].Position += new Vec2D(correction * 2.0, 0);
				}
			}
		}
	}

	/// <summary>
	/// Push apart any pair of bodies whose rectangles are on top of one another.
	/// </summary>
	/// <remarks>
	/// Repulsion does know how big a body is - it is measured across the clear space between the two
	/// rectangles - but it is a soft force with a ceiling on it: it stops getting stronger below
	/// <see cref="LayoutSettings.MinRepulsionDistance"/>, so a link spring pulling to a fixed rest
	/// length can hold a pair overlapping in spite of it, and two bodies at the same point have no
	/// direction to be pushed along at all. Either way the rectangles end up squarely on top of one
	/// another, which is what a consumer drawing them sees, and no amount of force tuning reaches it.
	/// <para>
	/// So it is resolved positionally, after integration, the same way <see cref="ApplyDirectionalConstraints"/>
	/// is: for each overlapping pair, along the axis they overlap least on — the shorter push, and the
	/// one that leaves the arrangement the forces worked out most nearly as it was — and shared
	/// equally between the two so the arrangement's centroid does not drift. The correction is capped
	/// per substep, so a deep overlap slides apart over a few frames rather than snapping.
	/// </para>
	/// <para>
	/// The whole overlap is resolved rather than a fraction of it. A fraction loses: between two
	/// linked bodies the spring pulls back harder each substep than a fraction of the overlap pushes,
	/// and the pair comes to rest still overlapping, just less.
	/// </para>
	/// </remarks>
	private void SeparateOverlaps()
	{
		double margin = Settings.OverlapMargin;
		if (margin <= 0)
		{
			return;
		}

		double maxCorrection = Settings.MaxOverlapCorrection;

		for (int i = 0; i < bodyCount; i++)
		{
			for (int j = i + 1; j < bodyCount; j++)
			{
				bool sourceMovable = bodies[i].IsPinned == 0 && bodies[i].IsFrozen == 0;
				bool targetMovable = bodies[j].IsPinned == 0 && bodies[j].IsFrozen == 0;
				if (!sourceMovable && !targetMovable)
				{
					continue;
				}

				Vec2D clearance = ((bodies[i].Dimensions + bodies[j].Dimensions) * 0.5) + new Vec2D(margin, margin);
				Vec2D aCenter = bodies[i].Position + (bodies[i].Dimensions * 0.5);
				Vec2D bCenter = bodies[j].Position + (bodies[j].Dimensions * 0.5);
				Vec2D between = bCenter - aCenter;

				double overlapX = clearance.X - Math.Abs(between.X);
				double overlapY = clearance.Y - Math.Abs(between.Y);

				// An overlap this shallow is not worth a write, and stopping short of exactly zero keeps
				// rounding from nudging a resolved pair for ever.
				if (overlapX <= OverlapEpsilon || overlapY <= OverlapEpsilon)
				{
					continue;
				}

				// Separating a pair along the very axis it is trying to move on holds it exactly one
				// clearance apart on the wrong side of itself, and the untangling force and this pass
				// fight to a standstill. So whichever axis an untangle needs is left free and the
				// separation goes on the other one: a backward edge reorders along X, so it is pushed
				// apart on Y; a twisted pair swaps along Y, so it is pushed apart on X. A body doing both
				// at once has no axis left and is allowed to overlap until one of them is done.
				bool needsFreeX = untanglingInX[i] || untanglingInX[j];
				bool needsFreeY = untanglingInY[i] || untanglingInY[j];
				if (needsFreeX && needsFreeY)
				{
					continue;
				}

				bool separateOnY;
				if (needsFreeX)
				{
					separateOnY = true;
				}
				else if (needsFreeY)
				{
					// Two bodies in the same column have nothing to gain from separating on X: it would
					// have to move them a whole body width to achieve nothing the swap needs. They pass
					// through each other instead, and are back under the pass as soon as the swap is done.
					if (overlapX >= Math.Min(bodies[i].Dimensions.X, bodies[j].Dimensions.X))
					{
						continue;
					}

					separateOnY = false;
				}
				else
				{
					separateOnY = overlapX >= overlapY;
				}

				double depth = separateOnY ? overlapY : overlapX;
				double correction = Math.Min(depth, maxCorrection);

				// A zero component has no side to be on, so the later body goes the positive way:
				// arbitrary, but consistent, which is what stops a coincident pair from jittering.
				double along = separateOnY ? between.Y : between.X;
				double sign = along < 0 ? -1.0 : 1.0;

				Vec2D push = separateOnY
					? new Vec2D(0, correction * sign)
					: new Vec2D(correction * sign, 0);

				if (sourceMovable && targetMovable)
				{
					bodies[i].Position -= push * 0.5;
					bodies[j].Position += push * 0.5;
				}
				else if (sourceMovable)
				{
					bodies[i].Position -= push;
				}
				else
				{
					bodies[j].Position += push;
				}
			}
		}
	}

	/// <summary>
	/// The fraction of its remaining offset from <see cref="WorldOrigin"/> the arrangement is slid back
	/// each substep, before <see cref="LayoutSettings.OriginAnchorWeight"/> scales it.
	/// </summary>
	/// <remarks>
	/// Not a setting, because it does not change where a graph comes to rest — only how quickly it gets
	/// there. The resting place is the origin either way. Clamped to one so the graph can close the
	/// offset but never travel past it, whatever weight is asked for.
	/// </remarks>
	private const double RecentringRate = 0.25;

	private void CalculateGravityForces()
	{
		if (bodyCount == 0)
		{
			return;
		}

		Vec2D centroid = Vec2D.Zero;
		for (int i = 0; i < bodyCount; i++)
		{
			centroid += bodies[i].Position + (bodies[i].Dimensions * 0.5);
		}
		centroid /= bodyCount;

		Vec2D gravityTarget = Vec2D.Lerp(centroid, WorldOrigin, Settings.OriginAnchorWeight);
		GravityCenter = gravityTarget;

		double magnitude = Settings.GravityStrength;

		for (int i = 0; i < bodyCount; i++)
		{
			Vec2D nodeCenter = bodies[i].Position + (bodies[i].Dimensions * 0.5);
			Vec2D toCenter = gravityTarget - nodeCenter;
			double distance = toCenter.Length();
			if (distance > 0.1)
			{
				bodies[i].Force += toCenter * (magnitude / distance);
			}
		}
	}

	/// <summary>
	/// Slide the whole arrangement, as one piece, towards having its drawn bounding box centred on
	/// <see cref="WorldOrigin"/>.
	/// </summary>
	/// <remarks>
	/// Gravity cannot do this job, and it is worth being precise about why, because the obvious repair
	/// makes things worse.
	/// <para>
	/// Gravity pulls each body the same amount whichever side of the target it sits and however far out
	/// it is. Summed over the graph that is a step function of position: it counts bodies rather than
	/// measuring them, so anywhere the counts happen to balance it is exactly zero and nothing holds
	/// the graph anywhere at all. A twelve-node chain settles 79 units to one side and stays; pushed
	/// 600 the other way it comes to rest 79 units to the *other* side, the same distance out, because
	/// both are edges of the same dead band. And where the counts do balance is the median of the body
	/// centres, which for a document with a dense cluster of literals on one side and a few large
	/// functions on the other is nowhere near the middle of what is drawn — 200 units apart on the
	/// corpus's Counter graph.
	/// </para>
	/// <para>
	/// Making gravity proportional to distance fixes both of those and costs something worse: a body
	/// further from the target is then pulled harder, so a pair of wide nodes is squeezed closer
	/// together than a pair of narrow ones, and settled spacing depends on node size again — which is
	/// the whole thing measuring repulsion across clear space rather than between centres was for.
	/// Measured, a 400-wide pair settled 160 apart against a 60-wide pair's 224.
	/// </para>
	/// <para>
	/// So placement is separated from cohesion instead. This force is identical on every body, which
	/// means it cannot change any distance between them: it can only slide the whole arrangement, and
	/// it slides it until the box a reader sees is centred where it should be. Gravity is left to do
	/// the one thing it is good at, which is holding the graph together.
	/// </para>
	/// </remarks>
	private void RecentreOnOrigin()
	{
		double weight = Settings.OriginAnchorWeight;
		if (bodyCount == 0 || weight <= 0)
		{
			return;
		}

		double minX = double.MaxValue;
		double minY = double.MaxValue;
		double maxX = double.MinValue;
		double maxY = double.MinValue;

		for (int i = 0; i < bodyCount; i++)
		{
			// A pinned or frozen body is placed by whoever pinned it, and sliding the graph would move
			// it. One is enough to say where the graph goes, so the whole pass stands down.
			if (bodies[i].IsPinned != 0 || bodies[i].IsFrozen != 0)
			{
				return;
			}

			minX = Math.Min(minX, bodies[i].Position.X);
			minY = Math.Min(minY, bodies[i].Position.Y);
			maxX = Math.Max(maxX, bodies[i].Position.X + bodies[i].Dimensions.X);
			maxY = Math.Max(maxY, bodies[i].Position.Y + bodies[i].Dimensions.Y);
		}

		Vec2D drawnCentre = new((minX + maxX) * 0.5, (minY + maxY) * 0.5);
		Vec2D shift = (WorldOrigin - drawnCentre) * Math.Min(weight * RecentringRate, 1.0);

		for (int i = 0; i < bodyCount; i++)
		{
			bodies[i].Position += shift;
		}
	}

	private void IntegrateMotion(double dt)
	{
		double maxForce = Settings.MaxForce;
		double maxVelocity = Settings.MaxVelocity;
		double dampingPerSubstep = Math.Pow(Settings.DampingFactor, dt);

		for (int i = 0; i < bodyCount; i++)
		{
			ref BodyState body = ref bodies[i];

			if (body.IsPinned != 0 || body.IsFrozen != 0)
			{
				body.Velocity = Vec2D.Zero;
				body.Force = Vec2D.Zero;
				continue;
			}

			Vec2D clampedForce = body.Force;
			double forceLen = clampedForce.Length();
			if (forceLen > maxForce && forceLen > 0)
			{
				clampedForce *= maxForce / forceLen;
			}

			Vec2D newVelocity = body.Velocity + (clampedForce * dt);
			newVelocity *= dampingPerSubstep;

			double velLen = newVelocity.Length();
			if (velLen > maxVelocity && velLen > 0)
			{
				newVelocity *= maxVelocity / velLen;
			}

			body.Position += newVelocity * dt;
			body.Velocity = newVelocity;
			body.Force = clampedForce;
		}
	}

	/// <summary>
	/// Solve to convergence: repeatedly call <see cref="Step"/> with a substep-sized delta time until
	/// the system reports <see cref="IsStable"/> or <paramref name="maxIterations"/> is reached.
	/// </summary>
	/// <param name="maxIterations">Cap on integration ticks. Each tick is a full <see cref="Step"/> call.</param>
	/// <param name="tolerance">If non-zero, overrides <see cref="LayoutSettings.StabilityThreshold"/> for the duration of the call.</param>
	/// <returns>Number of iterations actually executed.</returns>
	public int Solve(int maxIterations, double tolerance)
	{
		double originalThreshold = Settings.StabilityThreshold;
		if (tolerance > 0)
		{
			LayoutSettings s = Settings;
			s.StabilityThreshold = tolerance;
			Settings = s;
		}

		try
		{
			double dt = 1.0 / Settings.TargetPhysicsHz;
			int i;
			for (i = 0; i < maxIterations; i++)
			{
				Step(dt);
				if (IsStable)
				{
					return i + 1;
				}
			}
			return i;
		}
		finally
		{
			if (tolerance > 0)
			{
				LayoutSettings s = Settings;
				s.StabilityThreshold = originalThreshold;
				Settings = s;
			}
		}
	}
}
