// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.NodeEditor;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Hexa.NET.ImGui;
using Hexa.NET.ImNodes;

/// <summary>
/// Drawing and handling comment boxes. See <see cref="CommentBox"/>.
/// </summary>
/// <remarks>
/// ImNodes has no notion of a group, so comment boxes are this renderer's own. They are drawn into
/// the editor's background channel before any node is submitted, which puts them over the grid and
/// under every node and link, and handled with ordinary ImGui items placed over their title bars,
/// resize handles and close buttons. An ImGui item under the pointer is also what stops ImNodes
/// starting a box selection when a title bar is clicked.
/// </remarks>
public partial class NodeEditorRenderer
{
	/// <summary>How large a comment box's resize handle is, at the view's own scale.</summary>
	private const float CommentResizeHandleSize = 14f;

	/// <summary>How long a title may be typed into the rename box.</summary>
	private const int CommentTitleMaxLength = 256;

	/// <summary>Where each comment box was drawn this frame, in screen space.</summary>
	private readonly Dictionary<int, ScreenRect> commentBoxScreenRects = [];

	private CommentGesture? commentGesture;
	private string renameBuffer = string.Empty;
	private int renameStartFrame;

	/// <summary>Whether comment boxes are drawn and can be handled. On by default.</summary>
	public bool DrawCommentBoxes { get; set; } = true;

	/// <summary>
	/// The fill colour for a comment box that has no <see cref="CommentBox.Color"/> of its own, or
	/// null to take ImNodes' title bar colour at a low opacity so the boxes follow the theme.
	/// </summary>
	/// <remarks>
	/// A box is filled with its colour as given, alpha included; its title bar and border are drawn
	/// from the same colour at a higher opacity, so a faint fill still has a legible edge.
	/// </remarks>
	public Vector4? CommentBoxColor { get; set; }

	/// <summary>The comment box whose title bar the pointer was over in the last frame drawn, if any.</summary>
	public int? HoveredCommentBoxId { get; private set; }

	/// <summary>The comment box whose title is being edited, if any.</summary>
	public int? RenamingCommentBoxId { get; private set; }

	/// <summary>
	/// Start editing a comment box's title in place, as a double-click on its title bar does.
	/// </summary>
	/// <param name="commentBoxId">The comment box.</param>
	public void BeginRenamingCommentBox(int commentBoxId)
	{
		RenamingCommentBoxId = commentBoxId;
		renameBuffer = string.Empty;
		renameStartFrame = -1;
		commentGesture = null;
	}

	/// <summary>
	/// Where a comment box was drawn in the last frame, in screen space.
	/// </summary>
	/// <param name="commentBoxId">The comment box.</param>
	/// <param name="rect">The rectangle it occupied, title bar included.</param>
	/// <returns>True when the box was drawn in the last frame.</returns>
	public bool TryGetCommentBoxScreenRect(int commentBoxId, out ScreenRect rect) =>
		commentBoxScreenRects.TryGetValue(commentBoxId, out rect);

	/// <summary>The nodes a comment box drag in progress is carrying.</summary>
	private IEnumerable<int> CommentBoxCarriedNodes =>
		commentGesture is { Resizing: false } gesture ? gesture.Nodes.Select(n => n.NodeId) : [];

	/// <summary>
	/// Draw every comment box, then let the user handle them.
	/// </summary>
	private void RenderCommentBoxes(NodeEditorEngine engine)
	{
		HoveredCommentBoxId = null;
		commentBoxScreenRects.Clear();

		if (commentGesture is not null && engine.FindCommentBox(commentGesture.BoxId) is null)
		{
			// Removed from under the gesture, by the host or an undo. There is nothing left to move.
			commentGesture = null;
		}

		if (!DrawCommentBoxes || engine.CommentBoxes.Count == 0)
		{
			commentGesture = null;
			RenamingCommentBoxId = null;
			return;
		}

		ImDrawListPtr drawList = ImGui.GetWindowDrawList();
		float titleHeight = ImGui.GetFrameHeight();

		foreach (CommentBox box in engine.CommentBoxes)
		{
			DrawCommentBox(drawList, box, titleHeight);
		}

		// While the pointer is over a node or a link, ImNodes owns the click: a comment title bar a
		// node overlaps must not steal the node's drag. A gesture in progress keeps its items, since
		// a box being dragged carries nodes along under the pointer.
		bool interactive = commentGesture is not null || (HoveredNodeId is null && HoveredLinkId is null);
		if (!interactive)
		{
			return;
		}

		Vector2 cursor = ImGui.GetCursorScreenPos();
		int? closing = null;

		// Topmost first: ImGui gives the pointer to the first item submitted under it, and a box
		// drawn later is drawn on top.
		foreach (CommentBox box in engine.CommentBoxes.Reverse().ToList())
		{
			if (HandleCommentBox(engine, box, titleHeight))
			{
				closing = box.Id;
			}
		}

		ImGui.SetCursorScreenPos(cursor);

		if (closing is int closedId)
		{
			if (History is NodeEditorHistory history)
			{
				history.RemoveCommentBox(closedId);
			}
			else
			{
				engine.RemoveCommentBox(closedId);
			}
		}
	}

	/// <summary>Where a box's corners are on screen.</summary>
	private (Vector2 Min, Vector2 Max) CommentBoxScreenRect(CommentBox box) =>
		(canvasOrigin + ToView(box.Position), canvasOrigin + ToView(box.Max));

	/// <summary>The fill colour a box is drawn with.</summary>
	private Vector4 CommentBoxFill(CommentBox box)
	{
		if (box.Color is Vector4 own)
		{
			return own;
		}

		if (CommentBoxColor is Vector4 configured)
		{
			return configured;
		}

		Vector4 titleBar = ImGui.ColorConvertU32ToFloat4(ImNodes.GetStyle().Colors[(int)ImNodesCol.TitleBar]);
		return titleBar with { W = 0.25f };
	}

	private void DrawCommentBox(ImDrawListPtr drawList, CommentBox box, float titleHeight)
	{
		(Vector2 min, Vector2 max) = CommentBoxScreenRect(box);
		commentBoxScreenRects[box.Id] = new ScreenRect(min, max);
		Vector4 fill = CommentBoxFill(box);
		Vector4 edge = fill with { W = Math.Min(1f, (fill.W * 2.5f) + 0.2f) };
		uint edgeColor = ImGui.ColorConvertFloat4ToU32(edge);
		float rounding = ImNodes.GetStyle().NodeCornerRounding;
		Vector2 titleMax = new(max.X, Math.Min(max.Y, min.Y + titleHeight));

		drawList.AddRectFilled(min, max, ImGui.ColorConvertFloat4ToU32(fill), rounding);
		drawList.AddRectFilled(min, titleMax, edgeColor, rounding, ImDrawFlags.RoundCornersTop);
		drawList.AddRect(min, max, edgeColor, rounding, ImDrawFlags.None, Math.Max(1f, Zoom));

		uint textColor = ImGui.GetColorU32(ImGuiCol.Text);
		float padding = ImGui.GetStyle().FramePadding.X;

		if (RenamingCommentBoxId != box.Id)
		{
			drawList.PushClipRect(min, new Vector2(titleMax.X - titleHeight, titleMax.Y), true);
			drawList.AddText(min + new Vector2(padding, ImGui.GetStyle().FramePadding.Y), textColor, box.Title);
			drawList.PopClipRect();
		}

		// The close button: a cross in a square at the right of the title bar.
		Vector2 closeCentre = new(max.X - (titleHeight * 0.5f), min.Y + (titleHeight * 0.5f));
		float arm = titleHeight * 0.2f;
		drawList.AddLine(closeCentre - new Vector2(arm, arm), closeCentre + new Vector2(arm, arm), textColor, Math.Max(1f, Zoom));
		drawList.AddLine(closeCentre + new Vector2(-arm, arm), closeCentre + new Vector2(arm, -arm), textColor, Math.Max(1f, Zoom));

		// The resize handle: a triangle in the bottom-right corner.
		float handle = CommentResizeHandleSize * Zoom;
		drawList.AddTriangleFilled(max, max - new Vector2(handle, 0f), max - new Vector2(0f, handle), edgeColor);
	}

	/// <summary>
	/// Submit the items that handle one box, and act on what they report.
	/// </summary>
	/// <returns>True if the box's close button was clicked.</returns>
	private bool HandleCommentBox(NodeEditorEngine engine, CommentBox box, float titleHeight)
	{
		(Vector2 min, Vector2 max) = CommentBoxScreenRect(box);
		Vector2 size = max - min;
		float titleBarHeight = Math.Min(titleHeight, size.Y);

		ImGui.PushID($"comment{box.Id}");
		try
		{
			ImGui.SetCursorScreenPos(new Vector2(max.X - titleHeight, min.Y));
			bool closed = ImGui.InvisibleButton("close", new Vector2(titleHeight, titleBarHeight));
			if (ImGui.IsItemHovered())
			{
				HoveredCommentBoxId = box.Id;
				ImGui.SetTooltip("Remove this comment. The nodes inside it stay.");
			}

			float handle = CommentResizeHandleSize * Zoom;
			ImGui.SetCursorScreenPos(max - new Vector2(handle, handle));
			ImGui.InvisibleButton("resize", new Vector2(handle, handle));
			TrackCommentGesture(engine, box, resizing: true);

			if (RenamingCommentBoxId == box.Id)
			{
				HandleRename(engine, box, min, max.X - titleHeight, titleBarHeight);
			}
			else
			{
				ImGui.SetCursorScreenPos(min);
				ImGui.InvisibleButton("title", new Vector2(Math.Max(1f, size.X - titleHeight), Math.Max(1f, titleBarHeight)));
				if (ImGui.IsItemHovered())
				{
					HoveredCommentBoxId = box.Id;
				}

				if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
				{
					BeginRenamingCommentBox(box.Id);
				}
				else
				{
					TrackCommentGesture(engine, box, resizing: false);
				}
			}

			return closed;
		}
		finally
		{
			ImGui.PopID();
		}
	}

	/// <summary>
	/// Start, continue or finish a drag of the item just submitted for a box.
	/// </summary>
	private void TrackCommentGesture(NodeEditorEngine engine, CommentBox box, bool resizing)
	{
		if (resizing && (ImGui.IsItemHovered() || ImGui.IsItemActive()))
		{
			ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeNwse);
		}

		if (ImGui.IsItemActivated())
		{
			commentGesture = BeginCommentGesture(engine, box, resizing);
			return;
		}

		if (commentGesture is not CommentGesture gesture || gesture.BoxId != box.Id || gesture.Resizing != resizing)
		{
			return;
		}

		if (ImGui.IsItemActive())
		{
			ApplyCommentGesture(engine, gesture, ImGui.GetMouseDragDelta(ImGuiMouseButton.Left, 0f) / Zoom);
		}
		else
		{
			FinishCommentGesture(engine, gesture);
			commentGesture = null;
		}
	}

	private static CommentGesture BeginCommentGesture(NodeEditorEngine engine, CommentBox box, bool resizing)
	{
		if (resizing)
		{
			return new CommentGesture(box.Id, Resizing: true, [box], []);
		}

		// What the box carries is decided once, as the drag starts, so it does not pick up whatever it
		// is dragged across.
		List<CommentBox> boxes = [box, .. engine.GetCommentBoxesInCommentBox(box.Id).Select(id => engine.FindCommentBox(id)!)];
		List<(int, Vector2)> carried = [.. engine.GetNodesInCommentBox(box.Id)
			.Select(id => (id, engine.Nodes.First(n => n.Id == id).Position))];

		return new CommentGesture(box.Id, Resizing: false, boxes, carried);
	}

	/// <summary>Put everything the gesture moves where the pointer has taken it.</summary>
	/// <param name="engine">The engine.</param>
	/// <param name="gesture">The gesture.</param>
	/// <param name="delta">How far the pointer has moved since the gesture began, in engine space.</param>
	private static void ApplyCommentGesture(NodeEditorEngine engine, CommentGesture gesture, Vector2 delta)
	{
		if (gesture.Resizing)
		{
			CommentBox before = gesture.Boxes[0];
			engine.ResizeCommentBox(before.Id, before.Size + delta);
			return;
		}

		foreach (CommentBox before in gesture.Boxes)
		{
			Reposition(engine, before, before.Position + delta);
		}

		foreach ((int nodeId, Vector2 from) in gesture.Nodes)
		{
			engine.UpdateNodePosition(nodeId, from + delta);
		}
	}

	private static void Reposition(NodeEditorEngine engine, CommentBox before, Vector2 position)
	{
		int index = engine.CommentBoxes.ToList().FindIndex(b => b.Id == before.Id);
		if (index >= 0)
		{
			engine.RestoreCommentBox(engine.CommentBoxes[index] with { Position = position }, index);
		}
	}

	/// <summary>Snap what the gesture moved, if snapping is on, and record the gesture.</summary>
	private void FinishCommentGesture(NodeEditorEngine engine, CommentGesture gesture)
	{
		CommentBox? primary = engine.FindCommentBox(gesture.BoxId);
		if (primary is null)
		{
			return;
		}

		if (SnapToGrid && gesture.Resizing)
		{
			engine.ResizeCommentBox(primary.Id, SnapPositionToGrid(primary.Max) - primary.Position);
		}
		else if (SnapToGrid)
		{
			Vector2 correction = SnapPositionToGrid(primary.Position) - primary.Position;
			Vector2 total = primary.Position + correction - gesture.Boxes[0].Position;
			ApplyCommentGesture(engine, gesture, total);
		}

		List<(CommentBox Before, CommentBox After)> boxes = [.. gesture.Boxes
			.Select(before => (before, engine.FindCommentBox(before.Id)))
			.Where(pair => pair.Item2 is not null)
			.Select(pair => (pair.before, pair.Item2!))];

		List<NodeMove> moves = [.. gesture.Nodes
			.Select(n => (n, engine.Nodes.FirstOrDefault(node => node.Id == n.NodeId)))
			.Where(pair => pair.Item2 is not null)
			.Select(pair => new NodeMove(pair.n.NodeId, pair.n.From, pair.Item2!.Position))];

		History?.RecordCommentBoxGesture(gesture.Resizing ? "Resize comment" : "Move comment", boxes, moves);
	}

	/// <summary>
	/// Draw the title bar's rename box, and commit or cancel when the user is done with it.
	/// </summary>
	private void HandleRename(NodeEditorEngine engine, CommentBox box, Vector2 titleMin, float titleRight, float titleBarHeight)
	{
		int frame = ImGui.GetFrameCount();
		if (renameStartFrame < 0)
		{
			// The first frame the rename box is drawn, which is when it asks for the keyboard.
			renameStartFrame = frame;
			renameBuffer = box.Title;
		}

		float padding = ImGui.GetStyle().FramePadding.X;
		ImGui.SetCursorScreenPos(titleMin + new Vector2(padding * 0.5f, Math.Max(0f, (titleBarHeight - ImGui.GetFrameHeight()) * 0.5f)));
		ImGui.SetNextItemWidth(Math.Max(20f, titleRight - titleMin.X - padding));

		bool justStarted = frame - renameStartFrame < 2;
		if (frame == renameStartFrame)
		{
			ImGui.SetKeyboardFocusHere();
		}

		bool entered = ImGui.InputText("rename", ref renameBuffer, CommentTitleMaxLength, ImGuiInputTextFlags.EnterReturnsTrue | ImGuiInputTextFlags.AutoSelectAll);
		bool cancelled = ImGui.IsKeyPressed(ImGuiKey.Escape, repeat: false);
		bool finished = entered || cancelled || ImGui.IsItemDeactivated() || (!justStarted && !ImGui.IsItemActive());

		if (!finished)
		{
			return;
		}

		RenamingCommentBoxId = null;
		string title = renameBuffer.Trim();
		if (cancelled || title.Length == 0 || title == box.Title)
		{
			return;
		}

		if (History is NodeEditorHistory history)
		{
			history.RenameCommentBox(box.Id, title);
		}
		else
		{
			engine.RenameCommentBox(box.Id, title);
		}
	}

	/// <summary>A comment box drag or resize in progress.</summary>
	/// <param name="BoxId">The box the gesture started on.</param>
	/// <param name="Resizing">Whether it is resizing the box rather than moving it.</param>
	/// <param name="Boxes">The boxes it moves as they were when it started, its own first.</param>
	/// <param name="Nodes">The nodes it carries and where each was when it started.</param>
	private sealed record CommentGesture(int BoxId, bool Resizing, List<CommentBox> Boxes, List<(int NodeId, Vector2 From)> Nodes);
}
