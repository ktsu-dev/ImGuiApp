// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.NodeEditor;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using Hexa.NET.ImGui;
using Hexa.NET.ImNodes;

/// <summary>
/// Pure rendering class - only handles ImNodes display, no business logic
/// </summary>
public class NodeEditorRenderer
{
	/// <summary>The smallest <see cref="Zoom"/> the view allows.</summary>
	public const float MinZoom = 0.25f;

	/// <summary>The largest <see cref="Zoom"/> the view allows.</summary>
	public const float MaxZoom = 2.0f;

	/// <summary>How much of the editor a fitted graph is asked to fill, leaving a margin around it.</summary>
	private const float FitMargin = 0.9f;

	/// <summary>How thick a highlighted node's outline is drawn, at the view's own scale.</summary>
	private const float HighlightedNodeBorderThickness = 3.0f;

	private readonly Dictionary<int, Vector2> lastKnownNodePositions = [];

	/// <summary>Pin rows collected while drawing one node, reused across nodes and frames.</summary>
	private readonly List<(int PinId, float MiddleY, bool IsInput)> pinRows = [];
	private readonly Dictionary<int, Vector2> lastKnownNodeDimensions = [];
	private readonly HashSet<int> currentlyDraggedNodes = [];

	/// <summary>Where each node was drawn this frame, in screen space.</summary>
	private readonly Dictionary<int, ScreenRect> nodeScreenRects = [];

	/// <summary>Where each pin's circle was drawn this frame, in screen space.</summary>
	private readonly Dictionary<int, Vector2> pinScreenPositions = [];

	/// <summary>The links drawn in the highlight colour this frame.</summary>
	private readonly HashSet<int> highlightedLinks = [];

	/// <summary>The nodes outlined in the highlight colour this frame.</summary>
	private readonly HashSet<int> highlightedNodes = [];

	// Cached editor-to-screen transform: derived empirically from ImNodes
	// during Render() so it matches ImNodes' internal coordinate system exactly
	private Vector2 editorToScreenBase;
	private bool hasEditorTransform;

	// The point zoom scales about, cached from the last Render so the position and dimension
	// read-backs can undo the same transform the render applied.
	private Vector2 zoomAnchor;

	/// <summary>
	/// Set of node IDs currently being dragged by the user
	/// </summary>
	public IReadOnlySet<int> CurrentlyDraggedNodes => currentlyDraggedNodes;

	/// <summary>
	/// Whether hovering a node draws the links that meet it in the highlight colour.
	/// </summary>
	/// <remarks>
	/// Which links reach a node is the thing a dense graph hides: the lines cross, pass behind other
	/// nodes, and leave at pins too close together to tell apart. Colouring them while the node is
	/// under the pointer answers it without asking the user to click anything.
	/// </remarks>
	public bool HighlightLinksOnNodeHover { get; set; } = true;

	/// <summary>
	/// Whether hovering a node also highlights everything its value reaches: the nodes downstream of
	/// it and the links leading to them.
	/// </summary>
	/// <remarks>
	/// Off by default. The reach of a node in a large graph is most of the graph, so highlighting it
	/// is a question worth asking rather than a permanent state of the view.
	/// </remarks>
	public bool HighlightDownstreamOnNodeHover { get; set; }

	/// <summary>
	/// Whether the link under the pointer is drawn again over the nodes it passes behind.
	/// </summary>
	/// <remarks>
	/// ImNodes draws every link underneath the nodes, so following a hovered link across a crowded
	/// graph means following a line that keeps disappearing. This redraws that one link on top, where
	/// it can be traced from end to end.
	/// </remarks>
	public bool DrawHoveredLinkOnTop { get; set; } = true;

	/// <summary>
	/// The colour highlighted links and nodes are drawn in, or null to take ImNodes' own hovered-link
	/// colour so the highlight follows the theme.
	/// </summary>
	public Vector4? HighlightColor { get; set; }

	/// <summary>The node the pointer was over as of the last frame drawn, if any.</summary>
	public int? HoveredNodeId { get; private set; }

	/// <summary>The link the pointer was over as of the last frame drawn, if any.</summary>
	public int? HoveredLinkId { get; private set; }

	/// <summary>The links drawn in the highlight colour this frame.</summary>
	public IReadOnlySet<int> HighlightedLinks => highlightedLinks;

	/// <summary>The nodes outlined in the highlight colour this frame.</summary>
	public IReadOnlySet<int> HighlightedNodes => highlightedNodes;

	/// <summary>
	/// How large the graph is drawn, as a multiplier: 1 draws it at the engine's own scale.
	/// </summary>
	/// <remarks>
	/// ImNodes has no zoom of its own, so this is applied here: node positions are scaled on their
	/// way into ImNodes and unscaled on the way back out, and the font is scaled to match so a node's
	/// box — which ImNodes sizes from its content — grows and shrinks with the distances between
	/// nodes. Scaling positions alone would only pack the nodes tighter while they stayed the same
	/// size, which is not what anyone means by zooming out.
	/// <para>
	/// The engine never sees the zoomed values. Its positions and dimensions stay at their own scale,
	/// which is what the force-directed layout runs on: rest length, repulsion distance and overlap
	/// margin are all lengths, and none of them would mean the same thing in a space that changed
	/// whenever the user zoomed.
	/// </para>
	/// </remarks>
	public float Zoom
	{
		get;
		set => field = Math.Clamp(value, MinZoom, MaxZoom);
	} = 1.0f;

	/// <summary>
	/// Render the entire node editor
	/// </summary>
	public void Render(NodeEditorEngine engine, Vector2 editorSize)
	{
		Ensure.NotNull(engine);

		// Scaled about the middle of the editor, so zooming keeps whatever is in the middle of the
		// view in the middle of it rather than sending the graph towards a corner. Cached because the
		// read-backs have to undo exactly the transform this render applied, and they are not told
		// how big the editor is.
		zoomAnchor = editorSize * 0.5f;

		// Hover is only answerable once the editor has been drawn, so what is highlighted this frame
		// comes from the hover the last frame ended on. A frame's lag on a pointer that has to rest
		// on a node to mean anything is not a lag anyone sees.
		RefreshHighlights(engine);
		nodeScreenRects.Clear();
		pinScreenPositions.Clear();

		bool scaled = !IsUnzoomed;
		ScaledStyle restore = default;
		if (scaled)
		{
			ImGui.PushFont(ImGui.GetFont(), ImGui.GetFontSize() * Zoom);
			restore = ScaleImNodesStyle(Zoom);
		}

		uint highlightColor = HighlightColorU32();

		ImNodes.BeginNodeEditor();

		// Render all nodes
		foreach (Node node in engine.Nodes)
		{
			RenderNode(engine, node, highlightColor);
		}

		// Render all links
		foreach (Link link in engine.Links)
		{
			RenderLink(link, highlightColor);
		}

		// Cache the editor-to-screen transform while inside the editor context.
		// Derived empirically from a reference node so it matches ImNodes' internal
		// coordinate system exactly, regardless of how panning/origin are composed.
		CacheEditorTransform(engine);

		ImNodes.EndNodeEditor();

		// Asked while the answers are still about the frame just drawn, and before the style is put
		// back, since the overlay is drawn at the sizes that frame used.
		ReadHoverState();
		DrawHoveredLinkOverNodes(engine);

		if (scaled)
		{
			RestoreImNodesStyle(restore);
			ImGui.PopFont();
		}
	}

	/// <summary>
	/// Scale the lengths in the node editor's style, returning the values to put back afterwards.
	/// </summary>
	/// <param name="zoom">The multiplier to apply.</param>
	/// <returns>The style as it was, for <see cref="RestoreImNodesStyle"/>.</returns>
	/// <remarks>
	/// Scaling the font alone leaves a node's padding, its corner rounding and its pin circles the
	/// size they were, so a node zoomed out is not a smaller node — it is the same chrome around
	/// smaller text, and its box does not shrink in proportion. Everything in the style that is a
	/// length is scaled with the text so that it does.
	/// <para>
	/// Grid spacing is scaled too, which is what makes the background move with the graph rather than
	/// staying put underneath it.
	/// </para>
	/// </remarks>
	private static ScaledStyle ScaleImNodesStyle(float zoom)
	{
		ImNodesStylePtr style = ImNodes.GetStyle();
		ScaledStyle previous = new(
			style.GridSpacing,
			style.NodeCornerRounding,
			style.NodePadding,
			style.NodeBorderThickness,
			style.LinkThickness,
			style.LinkHoverDistance,
			style.PinCircleRadius,
			style.PinQuadSideLength,
			style.PinTriangleSideLength,
			style.PinLineThickness,
			style.PinHoverRadius,
			style.PinOffset);

		style.GridSpacing = previous.GridSpacing * zoom;
		style.NodeCornerRounding = previous.NodeCornerRounding * zoom;
		style.NodePadding = previous.NodePadding * zoom;
		style.NodeBorderThickness = previous.NodeBorderThickness * zoom;
		style.LinkThickness = previous.LinkThickness * zoom;
		style.LinkHoverDistance = previous.LinkHoverDistance * zoom;
		style.PinCircleRadius = previous.PinCircleRadius * zoom;
		style.PinQuadSideLength = previous.PinQuadSideLength * zoom;
		style.PinTriangleSideLength = previous.PinTriangleSideLength * zoom;
		style.PinLineThickness = previous.PinLineThickness * zoom;
		style.PinHoverRadius = previous.PinHoverRadius * zoom;
		style.PinOffset = previous.PinOffset * zoom;

		return previous;
	}

	/// <summary>
	/// Put the node editor's style back the way <see cref="ScaleImNodesStyle"/> found it.
	/// </summary>
	/// <param name="previous">The style to restore.</param>
	/// <remarks>
	/// Restored field by field rather than by writing the struct back whole, which would need the
	/// project to allow unsafe code for the sake of one assignment.
	/// </remarks>
	private static void RestoreImNodesStyle(ScaledStyle previous)
	{
		ImNodesStylePtr style = ImNodes.GetStyle();

		style.GridSpacing = previous.GridSpacing;
		style.NodeCornerRounding = previous.NodeCornerRounding;
		style.NodePadding = previous.NodePadding;
		style.NodeBorderThickness = previous.NodeBorderThickness;
		style.LinkThickness = previous.LinkThickness;
		style.LinkHoverDistance = previous.LinkHoverDistance;
		style.PinCircleRadius = previous.PinCircleRadius;
		style.PinQuadSideLength = previous.PinQuadSideLength;
		style.PinTriangleSideLength = previous.PinTriangleSideLength;
		style.PinLineThickness = previous.PinLineThickness;
		style.PinHoverRadius = previous.PinHoverRadius;
		style.PinOffset = previous.PinOffset;
	}

	/// <summary>
	/// The lengths in the node editor's style that a zoom scales, as they were before it did.
	/// </summary>
	private readonly record struct ScaledStyle(
		float GridSpacing,
		float NodeCornerRounding,
		Vector2 NodePadding,
		float NodeBorderThickness,
		float LinkThickness,
		float LinkHoverDistance,
		float PinCircleRadius,
		float PinQuadSideLength,
		float PinTriangleSideLength,
		float PinLineThickness,
		float PinHoverRadius,
		float PinOffset);

	/// <summary>
	/// Convert a position in the engine's space to the one it is drawn at.
	/// </summary>
	private Vector2 ToView(Vector2 position) => ((position - zoomAnchor) * Zoom) + zoomAnchor;

	/// <summary>
	/// Convert a position read back out of ImNodes to the engine's space.
	/// </summary>
	private Vector2 ToEngine(Vector2 position) => ((position - zoomAnchor) / Zoom) + zoomAnchor;

	/// <summary>
	/// Whether the view is at the engine's own scale, where the transform is the identity.
	/// </summary>
	private bool IsUnzoomed => Math.Abs(Zoom - 1.0f) < 0.0001f;

	/// <summary>
	/// Render a single node
	/// </summary>
	private void RenderNode(NodeEditorEngine engine, Node node, uint highlightColor)
	{
		pinRows.Clear();
		// Apply engine position to ImNodes BEFORE rendering the node
		// This ensures physics-calculated positions are reflected immediately.
		// Held in the space ImNodes works in, so a zoom change moves every node here and the
		// read-back can tell a user's drag apart from what this wrote.
		Vector2 viewPos = ToView(node.Position);

		if (lastKnownNodePositions.TryGetValue(node.Id, out Vector2 lastPos))
		{
			// Check if engine position differs from what we last set in ImNodes
			if (Vector2.Distance(lastPos, viewPos) > 0.1f)
			{
				ImNodes.SetNodeEditorSpacePos(node.Id, viewPos);
				lastKnownNodePositions[node.Id] = viewPos;
			}
		}
		else
		{
			// First render - set initial position
			ImNodes.SetNodeEditorSpacePos(node.Id, viewPos);
			lastKnownNodePositions[node.Id] = viewPos;
		}

		// ImNodes copies the outline colour and border thickness into the node as it begins it, so a
		// highlighted node is styled around its own submission rather than by the style in force when
		// the editor ends.
		bool highlighted = highlightedNodes.Contains(node.Id);
		if (highlighted)
		{
			ImNodes.PushColorStyle(ImNodesCol.NodeOutline, highlightColor);
			ImNodes.PushStyleVar(ImNodesStyleVar.NodeBorderThickness, HighlightedNodeBorderThickness * Zoom);
		}

		ImNodes.BeginNode(node.Id);

		// Node title
		ImNodes.BeginNodeTitleBar();
		ImGui.Text(node.Name);
		ImNodes.EndNodeTitleBar();

		// A node with no pins has nothing to draw under its title, and ImNodes ends the title bar
		// by moving the cursor to where the node's content starts. ImGui reports a cursor left
		// past the content with no item submitted after it ("code uses SetCursorPos() to extend
		// window/parent boundaries"), so an empty node claims its content origin explicitly.
		if (node.InputPins.Count == 0 && node.OutputPins.Count == 0)
		{
			ImGui.Dummy(Vector2.Zero);
		}

		// Input pins
		foreach (Pin pin in node.InputPins)
		{
			ImNodes.BeginInputAttribute(pin.Id);
			ImGui.Text(pin.EffectiveDisplayName);
			ImNodes.EndInputAttribute();
			RecordPinRow(pin.Id, isInput: true);
		}

		// Add some spacing between inputs and outputs
		if (node.InputPins.Count > 0 && node.OutputPins.Count > 0)
		{
			ImGui.Spacing();
		}

		// Output pins
		foreach (Pin pin in node.OutputPins)
		{
			ImNodes.BeginOutputAttribute(pin.Id);

			// Right-align output pin text by calculating node content width
			string pinText = pin.EffectiveDisplayName;
			Vector2 textSize = ImGui.CalcTextSize(pinText);

			// Calculate the node's content width based on the longest text
			float nodeContentWidth = CalculateNodeContentWidth(node);
			float paddingWidth = nodeContentWidth - textSize.X;

			// Add padding to push text to the right
			if (paddingWidth > 0)
			{
				ImGui.Dummy(new Vector2(paddingWidth, 0));
				ImGui.SameLine();
			}

			ImGui.Text(pinText);
			ImNodes.EndOutputAttribute();
			RecordPinRow(pin.Id, isInput: false);
		}

		ImNodes.EndNode();

		if (highlighted)
		{
			ImNodes.PopStyleVar();
			ImNodes.PopColorStyle();
		}

		PublishPinOffsets(engine, node);
	}

	/// <summary>
	/// Submit one link, in the highlight colour if it is one of the highlighted ones.
	/// </summary>
	/// <remarks>
	/// ImNodes captures a link's three colours — its own, its hovered one and its selected one — as
	/// the link is submitted, so pushing them around this call colours this link and no other. All
	/// three are pushed because a highlighted link that is also hovered or selected should stay the
	/// highlight colour rather than reverting under the pointer.
	/// </remarks>
	private void RenderLink(Link link, uint highlightColor)
	{
		bool highlighted = highlightedLinks.Contains(link.Id);
		if (highlighted)
		{
			ImNodes.PushColorStyle(ImNodesCol.Link, highlightColor);
			ImNodes.PushColorStyle(ImNodesCol.LinkHovered, highlightColor);
			ImNodes.PushColorStyle(ImNodesCol.LinkSelected, highlightColor);
		}

		ImNodes.Link(link.Id, link.OutputPinId, link.InputPinId);

		if (highlighted)
		{
			ImNodes.PopColorStyle();
			ImNodes.PopColorStyle();
			ImNodes.PopColorStyle();
		}
	}

	/// <summary>
	/// Work out what this frame draws in the highlight colour, from the hover the last frame ended on.
	/// </summary>
	private void RefreshHighlights(NodeEditorEngine engine)
	{
		highlightedLinks.Clear();
		highlightedNodes.Clear();

		if (HoveredNodeId is not int hovered)
		{
			return;
		}

		if (HighlightLinksOnNodeHover)
		{
			foreach (Link link in engine.GetNodeLinks(hovered))
			{
				highlightedLinks.Add(link.Id);
			}
		}

		if (HighlightDownstreamOnNodeHover)
		{
			GraphReach reach = engine.GetDownstream(hovered);

			foreach (int linkId in reach.LinkIds)
			{
				highlightedLinks.Add(linkId);
			}

			foreach (int nodeId in reach.NodeIds)
			{
				highlightedNodes.Add(nodeId);
			}
		}
	}

	/// <summary>
	/// The colour a highlight is drawn in: whatever was asked for, or ImNodes' hovered-link colour.
	/// </summary>
	private uint HighlightColorU32() =>
		HighlightColor is Vector4 color
			? ImGui.ColorConvertFloat4ToU32(color)
			: ImNodes.GetStyle().Colors[(int)ImNodesCol.LinkHovered];

	/// <summary>
	/// Note what the pointer was over in the frame just drawn.
	/// </summary>
	/// <remarks>
	/// ImNodes only answers these once the editor has ended, which is why they describe the frame
	/// that just finished rather than the one about to be built.
	/// </remarks>
	private void ReadHoverState()
	{
		int nodeId = 0;
		HoveredNodeId = ImNodes.IsNodeHovered(ref nodeId) ? nodeId : null;

		int linkId = 0;
		HoveredLinkId = ImNodes.IsLinkHovered(ref linkId) ? linkId : null;
	}

	/// <summary>
	/// Draw the hovered link again, over the nodes ImNodes drew on top of it.
	/// </summary>
	/// <remarks>
	/// ImNodes puts every link in a channel below the nodes, and there is no way to lift one out of
	/// it, so the link is drawn a second time on the foreground list — the only list that is
	/// guaranteed to be composited after the editor's own. It is the same curve ImNodes draws: the
	/// cubic bezier between the two pins, with control points a quarter of the straight-line distance
	/// to either side, so the copy lies exactly over the original rather than beside it.
	/// <para>
	/// Clipped to the editor, so a link whose ends are outside the visible area does not draw over
	/// whatever sits next to it.
	/// </para>
	/// </remarks>
	private void DrawHoveredLinkOverNodes(NodeEditorEngine engine)
	{
		if (!DrawHoveredLinkOnTop || HoveredLinkId is not int hoveredLink)
		{
			return;
		}

		Link? link = engine.Links.FirstOrDefault(l => l.Id == hoveredLink);
		if (link is null ||
			!pinScreenPositions.TryGetValue(link.OutputPinId, out Vector2 start) ||
			!pinScreenPositions.TryGetValue(link.InputPinId, out Vector2 end))
		{
			return;
		}

		ImNodesStylePtr style = ImNodes.GetStyle();
		uint color = style.Colors[(int)ImNodesCol.LinkHovered];
		float thickness = style.LinkThickness;
		float segmentsPerLength = style.LinkLineSegmentsPerLength;

		float length = (end - start).Length();
		Vector2 control = new(0.25f * length, 0.0f);
		int segments = Math.Max((int)(length * segmentsPerLength), 1);

		// The editor is the item the node editor just submitted, so its rectangle is the area to
		// keep the overlay inside without the caller having to say where the editor is.
		Vector2 clipMin = ImGui.GetItemRectMin();
		Vector2 clipMax = ImGui.GetItemRectMax();
		bool clip = clipMax.X > clipMin.X && clipMax.Y > clipMin.Y;

		ImDrawListPtr drawList = ImGui.GetForegroundDrawList();
		if (clip)
		{
			drawList.PushClipRect(clipMin, clipMax, true);
		}

		drawList.AddBezierCubic(start, start + control, end - control, end, color, thickness, segments);

		if (clip)
		{
			drawList.PopClipRect();
		}
	}

	/// <summary>
	/// Where a node was drawn in the last frame, in screen space.
	/// </summary>
	/// <param name="nodeId">The node.</param>
	/// <param name="rect">The rectangle it occupied.</param>
	/// <returns>True when the node has been drawn at least once.</returns>
	public bool TryGetNodeScreenRect(int nodeId, out ScreenRect rect) =>
		nodeScreenRects.TryGetValue(nodeId, out rect);

	/// <summary>
	/// Where a pin's circle was drawn in the last frame, in screen space.
	/// </summary>
	/// <param name="pinId">The pin.</param>
	/// <param name="position">The point links to it meet.</param>
	/// <returns>True when the pin has been drawn at least once.</returns>
	public bool TryGetPinScreenPosition(int pinId, out Vector2 position) =>
		pinScreenPositions.TryGetValue(pinId, out position);

	/// <summary>
	/// Notes the vertical middle of the pin row just submitted, in screen space.
	/// </summary>
	/// <remarks>
	/// ImNodes draws a pin's circle on the node's edge, level with the middle of its attribute's row,
	/// so the row's rectangle is what says where the pin is. The node's own box is not final until
	/// <c>EndNode</c>, which is why the rows are only turned into offsets afterwards.
	/// </remarks>
	private void RecordPinRow(int pinId, bool isInput) =>
		pinRows.Add((pinId, (ImGui.GetItemRectMin().Y + ImGui.GetItemRectMax().Y) * 0.5f, isInput));

	/// <summary>
	/// Turns this node's recorded pin rows into offsets from its origin and hands them to the engine,
	/// so the layout can measure a link between the points it is drawn between.
	/// </summary>
	private void PublishPinOffsets(NodeEditorEngine engine, Node node)
	{
		if (pinRows.Count == 0)
		{
			return;
		}

		Vector2 nodeScreenPos = ImNodes.GetNodeScreenSpacePos(node.Id);
		Vector2 nodeDimensions = ImNodes.GetNodeDimensions(node.Id);
		nodeScreenRects[node.Id] = new ScreenRect(nodeScreenPos, nodeScreenPos + nodeDimensions);

		// Where ImNodes puts the pin circle itself: just outside the node's edge, level with the
		// middle of its row. An overlay drawn between anything else would not meet the link it is
		// covering.
		float pinOffset = ImNodes.GetStyle().PinOffset;

		foreach ((int pinId, float middleY, bool isInput) in pinRows)
		{
			pinScreenPositions[pinId] = new Vector2(
				isInput ? nodeScreenPos.X - pinOffset : nodeScreenPos.X + nodeDimensions.X + pinOffset,
				middleY);

			// Inputs sit on the left edge and outputs on the right. Everything here is in the zoomed
			// space the view draws in, and the engine's lengths are not, so the offset is scaled back
			// the same way node dimensions are.
			float x = isInput ? 0f : nodeDimensions.X;
			engine.UpdatePinOffset(pinId, new Vector2(x, middleY - nodeScreenPos.Y) / Zoom);
		}
	}

	/// <summary>
	/// Bring the whole graph into view: centred in the editor, and zoomed out far enough to fit.
	/// </summary>
	/// <param name="engine">The engine holding the nodes.</param>
	/// <param name="editorSize">The area the graph is drawn in.</param>
	/// <returns>True if there was anything to bring into view.</returns>
	/// <remarks>
	/// Centring moves the nodes rather than panning the editor. It has to: <see cref="Render"/> writes
	/// every node's position into ImNodes on the frame it is drawn, so a pan is undone as soon as it
	/// is read back — the positions are the only thing that decides where a node appears. The whole
	/// arrangement is translated by one offset, so the shape a layout settled into is preserved rather
	/// than being disturbed by the act of looking at it.
	/// <para>
	/// The zoom is then whatever makes the arrangement fit, with a margin so nothing sits against an
	/// edge, and never above 1: a graph small enough to be magnified is shown at its own size, since
	/// magnifying it is not what "fit" means to someone who asked to see all of it. A graph too big
	/// even at <see cref="MinZoom"/> is shown as small as the view goes, which is the most of it there
	/// is to be had.
	/// </para>
	/// <para>
	/// A node's size is measured when it is drawn, so a graph fitted before its first frame is fitted
	/// against sizes that are still zero. Callers that fit on opening should fit again once the
	/// dimensions have arrived.
	/// </para>
	/// </remarks>
	public bool FitToView(NodeEditorEngine engine, Vector2 editorSize)
	{
		Ensure.NotNull(engine);

		if (engine.Nodes.Count == 0)
		{
			return false;
		}

		// Measured across each node's whole extent rather than its top-left corner, so a wide node on
		// one edge does not pull the arrangement off centre by half its width.
		Vector2 lowest = new(float.MaxValue, float.MaxValue);
		Vector2 highest = new(float.MinValue, float.MinValue);

		foreach (Node node in engine.Nodes)
		{
			lowest = Vector2.Min(lowest, node.Position);
			highest = Vector2.Max(highest, node.Position + node.Dimensions);
		}

		Vector2 centre = editorSize * 0.5f;
		Vector2 offset = centre - ((lowest + highest) * 0.5f);

		foreach (Node node in engine.Nodes.ToArray())
		{
			engine.UpdateNodePosition(node.Id, node.Position + offset);
		}

		Zoom = FittingZoom(highest - lowest, editorSize);
		return true;
	}

	/// <summary>
	/// Work out the largest zoom an arrangement of the given extent still fits at.
	/// </summary>
	/// <param name="extent">How much room the arrangement takes at the engine's scale.</param>
	/// <param name="editorSize">The room there is to show it in.</param>
	/// <returns>The zoom to use, within the range the view allows.</returns>
	private static float FittingZoom(Vector2 extent, Vector2 editorSize)
	{
		if (extent.X <= 0 || extent.Y <= 0 || editorSize.X <= 0 || editorSize.Y <= 0)
		{
			return 1.0f;
		}

		float fitting = Math.Min(editorSize.X / extent.X, editorSize.Y / extent.Y) * FitMargin;
		return Math.Clamp(Math.Min(fitting, 1.0f), MinZoom, MaxZoom);
	}

	/// <summary>
	/// Check for nodes that have moved and return their new positions
	/// </summary>
	public Dictionary<int, Vector2> GetNodePositionUpdates(NodeEditorEngine engine)
	{
		Dictionary<int, Vector2> updates = [];
		currentlyDraggedNodes.Clear();

		foreach (Node node in engine.Nodes)
		{
			// Only query positions for nodes that have been rendered at least once
			if (!lastKnownNodePositions.ContainsKey(node.Id))
			{
				continue; // Skip nodes that haven't been rendered yet
			}

			Vector2 currentImNodesPos = ImNodes.GetNodeEditorSpacePos(node.Id);

			// Only report a change if the ImNodes position differs from where this renderer drew the
			// node. That means the user dragged it — ImNodes changed independently of us — and the
			// position is reported back in the engine's space, not the one it was drawn in.
			if (Vector2.Distance(ToView(node.Position), currentImNodesPos) > 0.1f)
			{
				updates[node.Id] = ToEngine(currentImNodesPos);
				lastKnownNodePositions[node.Id] = currentImNodesPos;
				currentlyDraggedNodes.Add(node.Id);
			}
		}

		return updates;
	}

	/// <summary>
	/// Check for nodes that have been resized and return their new dimensions
	/// </summary>
	/// <remarks>
	/// A size measured while the view is zoomed is not the node's size: the text, the padding and the
	/// pin circles scale with the zoom, but ImGui's own spacing inside the node does not, and the
	/// font size is rounded to whole pixels. Dividing such a measurement by the zoom gives a value
	/// that depends on how far the user happened to be zoomed out, and handing that to a layout that
	/// keeps node boxes apart would re-space the graph every time the view changed.
	/// <para>
	/// So a node that has already been measured keeps the size it was measured at, and only a node
	/// that has never been measured takes a zoomed measurement — an approximate size being better
	/// than none for a node created while zoomed out. It is corrected the next time the view is at
	/// its own scale.
	/// </para>
	/// </remarks>
	[SuppressMessage("Major Code Smell", "S3267:Loops should be simplified with \"LINQ\" expressions.", Justification = "Explicit loop is clearer; the loop contains a continue and dictionary mutation that would not simplify cleanly.")]
	public Dictionary<int, Vector2> GetNodeDimensionUpdates(NodeEditorEngine engine)
	{
		Dictionary<int, Vector2> updates = [];

		foreach (Node node in engine.Nodes)
		{
			// Only query dimensions for nodes that have been rendered at least once
			if (!lastKnownNodePositions.ContainsKey(node.Id))
			{
				continue; // Skip nodes that haven't been rendered yet
			}

			Vector2 currentImNodesDims = ImNodes.GetNodeDimensions(node.Id);

			// Check if this is a new node or if dimensions changed
			if (!lastKnownNodeDimensions.TryGetValue(node.Id, out Vector2 lastDims))
			{
				// Initialize with current dimensions for new nodes
				lastKnownNodeDimensions[node.Id] = currentImNodesDims;
				updates[node.Id] = currentImNodesDims / Zoom;
			}
			else if (Vector2.Distance(lastDims, currentImNodesDims) > 0.1f && IsUnzoomed)
			{
				updates[node.Id] = currentImNodesDims;
				lastKnownNodeDimensions[node.Id] = currentImNodesDims;
			}
		}

		return updates;
	}

	/// <summary>
	/// Cache the editor-to-screen transform by deriving it from a reference node's
	/// known editor-space and screen-space positions within the active editor context
	/// </summary>
	private void CacheEditorTransform(NodeEditorEngine engine)
	{
		if (engine.Nodes.Count > 0)
		{
			int refNodeId = engine.Nodes[0].Id;
			Vector2 refScreenPos = ImNodes.GetNodeScreenSpacePos(refNodeId);
			Vector2 refEditorPos = ImNodes.GetNodeEditorSpacePos(refNodeId);
			editorToScreenBase = refScreenPos - refEditorPos;
			hasEditorTransform = true;
		}
		else
		{
			hasEditorTransform = false;
		}
	}

	/// <summary>
	/// Render debug overlays on top of the editor
	/// </summary>
	public void RenderDebugOverlays(NodeEditorEngine engine, Vector2 editorAreaPos, Vector2 editorAreaSize, bool showDebug)
	{
		if (!showDebug)
		{
			return;
		}

		if (engine.Nodes.Count == 0 || !hasEditorTransform)
		{
			return;
		}

		// The editor is a child window, and a child window's drawing is composited over its parent's,
		// so an overlay on the parent's list would be underneath the very nodes it is describing —
		// including the force and velocity vectors, which start at node centres. The foreground list
		// is the one drawn after everything.
		ImDrawListPtr drawList = ImGui.GetForegroundDrawList();

		// Clip debug overlays to the editor area so they don't spill into adjacent panels
		drawList.PushClipRect(editorAreaPos, editorAreaPos + editorAreaSize, true);

		// Render canvas origin
		RenderOrigin(drawList, engine);

		// Render node debug info
		RenderNodeDebugInfo(drawList, engine);

		// Render link debug info
		RenderLinkDebugInfo(drawList, engine);

		// Render physics debug info
		if (engine.PhysicsSettings.Enabled)
		{
			RenderPhysicsDebugInfo(drawList, engine);
		}

		drawList.PopClipRect();
	}

	/// <summary>
	/// Convert an editor-space position to screen-space using the cached transform
	/// </summary>
	private Vector2 EditorToScreen(Vector2 editorPos) =>
		editorToScreenBase + ToView(editorPos);

	private void RenderOrigin(ImDrawListPtr drawList, NodeEditorEngine engine)
	{
		if (!hasEditorTransform)
		{
			return;
		}

		// Use WorldOrigin (which tracks with panning) rather than a fixed Vector2.Zero
		Vector2 originScreen = EditorToScreen(engine.WorldOrigin);

		uint originColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.0f, 0.8f, 1.0f, 1.0f));

		// Draw crosshair
		drawList.AddLine(originScreen + new Vector2(-15, 0), originScreen + new Vector2(15, 0), originColor, 2.0f);
		drawList.AddLine(originScreen + new Vector2(0, -15), originScreen + new Vector2(0, 15), originColor, 2.0f);
		drawList.AddCircle(originScreen, 20.0f, originColor, 16, 2.0f);
		drawList.AddText(originScreen + new Vector2(25, -10), originColor, "ORIGIN (0,0)");
	}

	private void RenderNodeDebugInfo(ImDrawListPtr drawList, NodeEditorEngine engine)
	{
		if (!hasEditorTransform)
		{
			return;
		}

		// Calculate bounding box
		Vector2 minPos = new(float.MaxValue);
		Vector2 maxPos = new(float.MinValue);

		Vector2 weightedCenterSum = Vector2.Zero;
		float totalArea = 0.0f;

		foreach (Node node in engine.Nodes)
		{
			Vector2 nodeCenter = node.Position + (node.Dimensions * 0.5f);
			float nodeArea = node.Dimensions.X * node.Dimensions.Y;

			minPos = Vector2.Min(minPos, node.Position);
			maxPos = Vector2.Max(maxPos, node.Position + node.Dimensions);

			weightedCenterSum += nodeCenter * nodeArea;
			totalArea += nodeArea;
		}

		// Bounding box (using cached reference data)
		uint boundingBoxColor = ImGui.ColorConvertFloat4ToU32(new Vector4(1.0f, 1.0f, 0.0f, 0.6f));
		Vector2 minPosScreen = EditorToScreen(minPos);
		Vector2 maxPosScreen = EditorToScreen(maxPos);
		drawList.AddRect(minPosScreen, maxPosScreen, boundingBoxColor, 0.0f, ImDrawFlags.None, 2.0f);

		// Center of mass
		if (totalArea > 0)
		{
			Vector2 centerOfMass = weightedCenterSum / totalArea;
			Vector2 centerOfMassScreen = EditorToScreen(centerOfMass);
			uint centerOfMassColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.0f, 1.0f, 1.0f, 0.9f));
			drawList.AddCircleFilled(centerOfMassScreen, 6.0f, centerOfMassColor);
			drawList.AddCircle(centerOfMassScreen, 12.0f, centerOfMassColor, 16, 2.0f);
		}
	}

	private void RenderLinkDebugInfo(ImDrawListPtr drawList, NodeEditorEngine engine)
	{
		if (!hasEditorTransform)
		{
			return;
		}

		foreach (Link link in engine.Links)
		{
			float? distance = engine.GetLinkDistance(link.Id);
			float? stress = engine.GetLinkStress(link.Id);
			if (!distance.HasValue)
			{
				continue;
			}

			// Find the nodes for this link
			Node? outputNode = engine.Nodes.FirstOrDefault(n => n.OutputPins.Any(p => p.Id == link.OutputPinId));
			Node? inputNode = engine.Nodes.FirstOrDefault(n => n.InputPins.Any(p => p.Id == link.InputPinId));

			if (outputNode == null || inputNode == null)
			{
				continue;
			}

			// Get screen positions using cached reference data (use node centers, not top-left)
			Vector2 outputCenter = outputNode.Position + (outputNode.Dimensions * 0.5f);
			Vector2 inputCenter = inputNode.Position + (inputNode.Dimensions * 0.5f);
			Vector2 startScreen = EditorToScreen(outputCenter);
			Vector2 endScreen = EditorToScreen(inputCenter);

			// Color based on stress: blue = compression, green = rest, red = tension
			Vector4 stressColor = GetStressColor(stress ?? 0.0f);
			uint linkColor = ImGui.ColorConvertFloat4ToU32(stressColor);

			// Draw stress-colored line between node centers
			drawList.AddLine(startScreen, endScreen, linkColor, 2.0f);

			// Draw distance text at midpoint
			Vector2 midpointScreen = (startScreen + endScreen) * 0.5f;
			drawList.AddText(midpointScreen, linkColor, $"{distance.Value:F0}px");
		}
	}

	/// <summary>
	/// Map link stress to a color: blue (compression) → green (rest) → red (tension)
	/// </summary>
	private static Vector4 GetStressColor(float stress)
	{
		// Clamp stress to [-1, 1] for color mapping
		float t = Math.Clamp(stress, -1.0f, 1.0f);

		if (t < 0)
		{
			// Compression: blue to green (t: -1 → 0)
			float blend = 1.0f + t; // 0 → 1
			return new Vector4(0.0f, blend, 1.0f - blend, 0.9f);
		}
		else
		{
			// Tension: green to red (t: 0 → 1)
			return new Vector4(t, 1.0f - t, 0.0f, 0.9f);
		}
	}

	private void RenderPhysicsDebugInfo(ImDrawListPtr drawList, NodeEditorEngine engine)
	{
		if (!hasEditorTransform)
		{
			return;
		}

		foreach (Node node in engine.Nodes)
		{
			Vector2 nodeCenter = node.Position + (node.Dimensions * 0.5f);
			Vector2 nodeCenterScreen = EditorToScreen(nodeCenter);

			// Render force vector
			if (node.Force.Length() > 1.0f)
			{
				Vector2 forceEnd = nodeCenterScreen + (node.Force * 0.1f); // Scale for visibility
				uint forceColor = ImGui.ColorConvertFloat4ToU32(new Vector4(1.0f, 0.0f, 0.0f, 0.8f)); // Red
				drawList.AddLine(nodeCenterScreen, forceEnd, forceColor, 2.0f);
				drawList.AddCircleFilled(forceEnd, 3.0f, forceColor);
			}

			// Render velocity vector
			if (node.Velocity.Length() > 1.0f)
			{
				Vector2 velocityEnd = nodeCenterScreen + (node.Velocity * 0.5f); // Scale for visibility
				uint velocityColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.0f, 1.0f, 0.0f, 0.8f)); // Green
				drawList.AddLine(nodeCenterScreen, velocityEnd, velocityColor, 2.0f);
				drawList.AddCircleFilled(velocityEnd, 3.0f, velocityColor);
			}

			// Render the repulsion floor: this node's own box grown by the minimum distance, which is
			// where another body's nearest point has repulsion at its hardest. Repulsion is measured
			// across the clear space between two boxes, so the floor is a box around this one and not
			// a circle around its centre.
			float repulsionFloor = (float)engine.PhysicsSettings.MinRepulsionDistance;
			Vector2 floorMinScreen = EditorToScreen(node.Position - new Vector2(repulsionFloor, repulsionFloor));
			Vector2 floorMaxScreen = EditorToScreen(node.Position + node.Dimensions + new Vector2(repulsionFloor, repulsionFloor));
			uint repulsionZoneColor = ImGui.ColorConvertFloat4ToU32(new Vector4(1.0f, 0.5f, 0.0f, 0.2f)); // Orange, transparent
			drawList.AddRect(floorMinScreen, floorMaxScreen, repulsionZoneColor, 0.0f, ImDrawFlags.None, 1.0f);
		}

		// Render gravity center (fixed point, in editor/position space)
		Vector2 centroidScreen = EditorToScreen(engine.GravityCenter);
		uint physicsCenterColor = ImGui.ColorConvertFloat4ToU32(new Vector4(1.0f, 0.0f, 1.0f, 0.9f)); // Magenta
		drawList.AddCircleFilled(centroidScreen, 8.0f, physicsCenterColor);
		drawList.AddCircle(centroidScreen, 15.0f, physicsCenterColor, 16, 2.0f);
		drawList.AddText(centroidScreen + new Vector2(20, -10), physicsCenterColor, "GRAVITY CENTER");
	}

	/// <summary>
	/// Calculate the content width of a node based on its longest text element
	/// </summary>
	private static float CalculateNodeContentWidth(Node node)
	{
		float maxWidth = 0;

		// Check node title
		Vector2 titleSize = ImGui.CalcTextSize(node.Name);
		maxWidth = Math.Max(maxWidth, titleSize.X);

		// Check all input pin names
		foreach (Pin pin in node.InputPins)
		{
			Vector2 pinSize = ImGui.CalcTextSize(pin.EffectiveDisplayName);
			maxWidth = Math.Max(maxWidth, pinSize.X);
		}

		// Check all output pin names
		foreach (Pin pin in node.OutputPins)
		{
			Vector2 pinSize = ImGui.CalcTextSize(pin.EffectiveDisplayName);
			maxWidth = Math.Max(maxWidth, pinSize.X);
		}

		// Add some padding to account for node styling
		return maxWidth + 20.0f; // 20px padding
	}
}

/// <summary>
/// A rectangle something was drawn in, in screen space.
/// </summary>
/// <param name="Min">The top-left corner.</param>
/// <param name="Max">The bottom-right corner.</param>
public readonly record struct ScreenRect(Vector2 Min, Vector2 Max)
{
	/// <summary>The rectangle's middle.</summary>
	public Vector2 Centre => (Min + Max) * 0.5f;

	/// <summary>How much room the rectangle takes.</summary>
	public Vector2 Size => Max - Min;
}
