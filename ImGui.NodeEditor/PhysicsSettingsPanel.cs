// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.NodeEditor;

using System.Globalization;

using Hexa.NET.ImGui;

using ktsu.ForceDirectedLayout;
using ktsu.ImGui.Probes;

/// <summary>
/// Draws every knob the layout simulation has, so a graph can be tuned while it is on screen.
/// </summary>
/// <remarks>
/// The forces interact, and none of them can be judged on its own: raising repulsion changes what
/// the spring's rest length means, and levelling links only works in the room repulsion made. So the
/// panel exposes the whole of <see cref="PhysicsSettings"/> rather than a chosen subset, grouped by
/// the force each setting belongs to and captioned with what moving it does. A setting that is not
/// here is a setting nobody can reach without recompiling.
/// <para>
/// Every control marks itself with <see cref="ImGuiProbes"/>, so a UI test can address it by the
/// label the user sees.
/// </para>
/// </remarks>
public static class PhysicsSettingsPanel
{
	/// <summary>The value every "Reset" restores, and what the sliders are ranged around.</summary>
	private static readonly PhysicsSettings Defaults = new();

	/// <summary>
	/// Draws the whole panel.
	/// </summary>
	/// <param name="settings">The settings to edit; replaced when the user changes anything.</param>
	/// <returns>True when <paramref name="settings"/> was changed by this frame's input.</returns>
	public static bool Draw(ref PhysicsSettings settings)
	{
		Ensure.NotNull(settings);

		bool changed = false;
		PhysicsSettings working = settings;

		bool enabled = working.Enabled;
		if (Checkbox("Run simulation", ref enabled))
		{
			working = working with { Enabled = enabled };
			changed = true;
		}

		ImGui.SameLine();
		if (Button("Reset all"))
		{
			// The running flag is the user's, not the layout's, so a reset of the tuning leaves it be.
			working = Defaults with { Enabled = working.Enabled };
			changed = true;
		}

		changed |= DrawRepulsion(ref working);
		changed |= DrawSprings(ref working);
		changed |= DrawLinkShaping(ref working);
		changed |= DrawGravity(ref working);
		changed |= DrawOverlap(ref working);
		changed |= DrawIntegration(ref working);

		if (changed)
		{
			settings = working;
		}

		return changed;
	}

	/// <summary>
	/// Draws what the simulation is doing right now, which is what says whether a setting helped.
	/// </summary>
	/// <param name="engine">The engine being tuned.</param>
	/// <remarks>
	/// Energy is the honest read on a tuning change: a graph that will not settle shows it here long
	/// before the eye catches the drift, and a graph that settles instantly was probably damped into
	/// stillness before it finished arranging itself.
	/// </remarks>
	public static void DrawDiagnostics(NodeEditorEngine engine)
	{
		Ensure.NotNull(engine);

		(int substeps, float substepDelta) = engine.LastPhysicsStepInfo;

		Text("Energy", engine.TotalSystemEnergy.ToString("F1", CultureInfo.CurrentCulture));
		Text("Settled", engine.IsStable ? "yes" : "no");
		Text("Substeps", substeps.ToString(CultureInfo.CurrentCulture));
		Text("Substep", $"{substepDelta * 1000f:F2} ms");
		Text("Bodies", $"{engine.Nodes.Count} nodes, {engine.Links.Count} links");
	}

	private static bool DrawRepulsion(ref PhysicsSettings settings)
	{
		if (!Header("Repulsion"))
		{
			return false;
		}

		bool changed = false;
		double repulsion = settings.RepulsionStrength;
		if (Slider("Repulsion strength", ref repulsion, 0.0, 50_000_000.0, "%.0f",
			"Pushes every pair of nodes apart, by the inverse square of the clear space between their boxes — the gap you can see, not the distance between their centres, so a big node holds its neighbours off no harder than a small one does. This is what makes the room the other forces arrange things in; with none, a graph collapses onto itself."))
		{
			settings = settings with { RepulsionStrength = repulsion };
			changed = true;
		}

		double minDistance = settings.MinRepulsionDistance;
		if (Slider("Minimum distance", ref minDistance, 1.0, 200.0, "%.0f px",
			"The clear space repulsion stops getting stronger below. Without a floor, two nodes that touch would be flung apart."))
		{
			settings = settings with { MinRepulsionDistance = minDistance };
			changed = true;
		}

		return changed;
	}

	private static bool DrawSprings(ref PhysicsSettings settings)
	{
		if (!Header("Link springs"))
		{
			return false;
		}

		bool changed = false;
		double spring = settings.LinkSpringStrength;
		if (Slider("Spring strength", ref spring, 0.0, 3.0, "%.2f",
			"How hard a link pulls its two ends toward the rest length. Raise it to tighten clusters, lower it to let repulsion spread them."))
		{
			settings = settings with { LinkSpringStrength = spring };
			changed = true;
		}

		double rest = settings.RestLinkLength;
		if (Slider("Rest length", ref rest, 50.0, 600.0, "%.0f px",
			"The distance a link is happy at, measured between the pins it actually joins. This sets the horizontal step between one level of the graph and the next."))
		{
			settings = settings with { RestLinkLength = rest };
			changed = true;
		}

		double bias = settings.DirectionalBias;
		if (Slider("Left-to-right bias", ref bias, 0.0, 3.0, "%.2f",
			"Pushes each link's source left of its target. A link running backwards is reordered, and the pair is allowed past each other vertically while it happens."))
		{
			settings = settings with { DirectionalBias = bias };
			changed = true;
		}

		return changed;
	}

	private static bool DrawLinkShaping(ref PhysicsSettings settings)
	{
		if (!Header("Link shaping"))
		{
			return false;
		}

		bool changed = false;
		double flattening = settings.LinkFlatteningStrength;
		if (Slider("Flattening", ref flattening, 0.0, 3.0, "%.2f",
			"Levels each link's two ends, and splays them apart horizontally when the drawn curve would otherwise double back behind its own nodes and disappear."))
		{
			settings = settings with { LinkFlatteningStrength = flattening };
			changed = true;
		}

		double margin = settings.LinkFlatteningMargin;
		if (Slider("Flattening margin", ref margin, 0.0, 200.0, "%.0f px",
			"Extra horizontal room demanded on top of the clearance the curve geometry needs. Raise it if links still graze the nodes they leave."))
		{
			settings = settings with { LinkFlatteningMargin = margin };
			changed = true;
		}

		double untwist = settings.LinkUntwistStrength;
		if (Slider("Untwisting", ref untwist, 0.0, 1.0, "%.3f",
			"Swaps two links that meet at one node into the order of the pins they arrive at, so they stop crossing. Stronger settings untangle more and drive more nodes across links they are not part of."))
		{
			settings = settings with { LinkUntwistStrength = untwist };
			changed = true;
		}

		return changed;
	}

	private static bool DrawGravity(ref PhysicsSettings settings)
	{
		if (!Header("Gravity"))
		{
			return false;
		}

		bool changed = false;
		double gravity = settings.GravityStrength;
		if (Slider("Gravity strength", ref gravity, 0.0, 400.0, "%.0f",
			"Pulls every node toward the gravity target, which is what stops repulsion pushing a graph apart for ever."))
		{
			settings = settings with { GravityStrength = gravity };
			changed = true;
		}

		double anchor = settings.OriginAnchorWeight;
		if (Slider("Origin anchor", ref anchor, 0.0, 1.0, "%.2f",
			"Where that target sits: 0 is the graph's own centre, so it may drift; 1 is the fixed world origin, so it stays put."))
		{
			settings = settings with { OriginAnchorWeight = anchor };
			changed = true;
		}

		return changed;
	}

	private static bool DrawOverlap(ref PhysicsSettings settings)
	{
		if (!Header("Overlap"))
		{
			return false;
		}

		bool changed = false;
		double margin = settings.OverlapMargin;
		if (Slider("Clearance", ref margin, 0.0, 200.0, "%.0f px",
			"The gap kept between two node rectangles. At zero the pass is off and the forces alone decide, which leaves nodes drawn over each other."))
		{
			settings = settings with { OverlapMargin = margin };
			changed = true;
		}

		double correction = settings.MaxOverlapCorrection;
		if (Slider("Maximum correction", ref correction, 1.0, 200.0, "%.0f px",
			"How far a pair may be pushed apart in one substep. Lower values make a deep overlap slide apart over several frames rather than snap."))
		{
			settings = settings with { MaxOverlapCorrection = correction };
			changed = true;
		}

		return changed;
	}

	private static bool DrawIntegration(ref PhysicsSettings settings)
	{
		if (!Header("Motion and limits"))
		{
			return false;
		}

		bool changed = false;
		double damping = settings.DampingFactor;
		if (Slider("Damping", ref damping, 0.01, 0.99, "%.2f",
			"The fraction of a node's speed kept each second. Low values settle quickly but can stop a graph before it has finished arranging itself."))
		{
			settings = settings with { DampingFactor = damping };
			changed = true;
		}

		double maxForce = settings.MaxForce;
		if (Slider("Maximum force", ref maxForce, 100.0, 50_000.0, "%.0f",
			"The cap on the total force one node may feel, which is what keeps two nearly coincident nodes from exploding."))
		{
			settings = settings with { MaxForce = maxForce };
			changed = true;
		}

		double maxVelocity = settings.MaxVelocity;
		if (Slider("Maximum speed", ref maxVelocity, 5.0, 1000.0, "%.0f px/s",
			"The cap on how fast a node may travel. It bounds how long a graph takes to settle, since a node cannot reach its place faster than this."))
		{
			settings = settings with { MaxVelocity = maxVelocity };
			changed = true;
		}

		double hz = settings.TargetPhysicsHz;
		if (Slider("Substep rate", ref hz, 30.0, 480.0, "%.0f Hz",
			"How finely each frame is subdivided, independent of the frame rate. Higher is steadier and costs more."))
		{
			settings = settings with { TargetPhysicsHz = hz };
			changed = true;
		}

		double stability = settings.StabilityThreshold;
		if (Slider("Settled below", ref stability, 0.0, 100.0, "%.1f",
			"The total energy under which the graph is reported settled. It changes what is reported, not how anything moves."))
		{
			settings = settings with { StabilityThreshold = stability };
			changed = true;
		}

		return changed;
	}

	/// <summary>
	/// Submits one labelled slider over a double, with its explanation on hover.
	/// </summary>
	/// <remarks>
	/// ImGui edits floats, and every one of these settings is a double, so each is narrowed for the
	/// drag and widened back. The write only happens when the slider reports a change, so a setting
	/// the user never touches keeps its full precision rather than being rounded by being looked at.
	/// </remarks>
	private static bool Slider(string label, ref double value, double min, double max, string format, string help)
	{
		float editing = (float)value;
		bool changed = ImGui.SliderFloat(label, ref editing, (float)min, (float)max, format);
		ImGuiProbes.MarkItem(label);
		Explain(help);

		if (!changed)
		{
			return false;
		}

		value = editing;
		return true;
	}

	private static bool Checkbox(string label, ref bool value)
	{
		bool changed = ImGui.Checkbox(label, ref value);
		ImGuiProbes.MarkItem(label);
		return changed;
	}

	private static bool Button(string label)
	{
		bool clicked = ImGui.Button(label);
		ImGuiProbes.MarkItem(label);
		return clicked;
	}

	private static bool Header(string label)
	{
		bool open = ImGui.CollapsingHeader(label);
		ImGuiProbes.MarkItem(label);
		return open;
	}

	private static void Text(string label, string value)
	{
		ImGui.TextUnformatted($"{label}: {value}");
		ImGuiProbes.MarkItem(label);
	}

	/// <summary>Attaches the previous item's explanation, wrapped to a readable column.</summary>
	private static void Explain(string help)
	{
		if (!ImGui.IsItemHovered())
		{
			return;
		}

		ImGui.BeginTooltip();
		ImGui.PushTextWrapPos(ImGui.GetFontSize() * 28f);
		ImGui.TextUnformatted(help);
		ImGui.PopTextWrapPos();
		ImGui.EndTooltip();
	}
}
