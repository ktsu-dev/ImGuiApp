// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.NodeEditor;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

/// <summary>
/// Comment boxes: labelled regions drawn behind the nodes. See <see cref="CommentBox"/>.
/// </summary>
public partial class NodeEditorEngine
{
	/// <summary>The smallest a comment box may be made, so it always has a title bar to hold.</summary>
	public static Vector2 MinimumCommentBoxSize => new(80f, 48f);

	/// <summary>How much room <see cref="CreateCommentBoxAround"/> leaves between the nodes and the edge, when not told.</summary>
	public const float DefaultCommentBoxPadding = 24f;

	/// <summary>
	/// How much taller than its padding <see cref="CreateCommentBoxAround"/> makes a box's top edge,
	/// so the title bar sits above the nodes rather than over them.
	/// </summary>
	public const float CommentBoxTitleAllowance = 28f;

	private readonly List<CommentBox> commentBoxes = [];
	private int nextCommentBoxId = 1;

	/// <summary>The comment boxes, in the order they are drawn: later ones over earlier ones.</summary>
	public IReadOnlyList<CommentBox> CommentBoxes => commentBoxes.AsReadOnly();

	/// <summary>
	/// Add a comment box.
	/// </summary>
	/// <param name="position">Its top-left corner.</param>
	/// <param name="size">Its size, which is raised to <see cref="MinimumCommentBoxSize"/> if smaller.</param>
	/// <param name="title">The label for its title bar.</param>
	/// <param name="color">Its fill colour, or null to follow the theme.</param>
	/// <returns>The new comment box.</returns>
	public CommentBox CreateCommentBox(Vector2 position, Vector2 size, string title, Vector4? color = null)
	{
		Ensure.NotNull(title);
		CommentBox box = new(nextCommentBoxId++, title, position, Vector2.Max(size, MinimumCommentBoxSize), color);
		commentBoxes.Add(box);
		return box;
	}

	/// <summary>
	/// Add a comment box sized to enclose a set of nodes.
	/// </summary>
	/// <param name="nodeIds">The nodes to enclose. Ids naming no node are skipped.</param>
	/// <param name="title">The label for its title bar.</param>
	/// <param name="padding">The room to leave between the nodes and the box's edge.</param>
	/// <param name="color">Its fill colour, or null to follow the theme.</param>
	/// <returns>The new comment box, or null if none of the ids named a node.</returns>
	/// <remarks>
	/// The top edge is given <see cref="CommentBoxTitleAllowance"/> on top of the padding, so the
	/// title bar does not cover the topmost node. A node's size is measured the first time it is
	/// drawn, so a box made around nodes that have never been drawn is made around their corners.
	/// </remarks>
	public CommentBox? CreateCommentBoxAround(IEnumerable<int> nodeIds, string title, float padding = DefaultCommentBoxPadding, Vector4? color = null)
	{
		Ensure.NotNull(nodeIds);
		HashSet<int> wanted = [.. nodeIds];
		List<Node> enclosed = [.. nodes.Where(n => wanted.Contains(n.Id))];
		if (enclosed.Count == 0)
		{
			return null;
		}

		Vector2 lowest = new(float.MaxValue, float.MaxValue);
		Vector2 highest = new(float.MinValue, float.MinValue);
		foreach (Node node in enclosed)
		{
			lowest = Vector2.Min(lowest, node.Position);
			highest = Vector2.Max(highest, node.Position + node.Dimensions);
		}

		Vector2 topLeft = lowest - new Vector2(padding, padding + CommentBoxTitleAllowance);
		Vector2 bottomRight = highest + new Vector2(padding, padding);
		return CreateCommentBox(topLeft, bottomRight - topLeft, title, color);
	}

	/// <summary>Find a comment box by id.</summary>
	/// <param name="commentBoxId">The comment box.</param>
	/// <returns>It, or null if there is none with that id.</returns>
	public CommentBox? FindCommentBox(int commentBoxId) => commentBoxes.Find(b => b.Id == commentBoxId);

	/// <summary>
	/// Remove a comment box. The nodes inside it are left where they are.
	/// </summary>
	/// <param name="commentBoxId">The comment box.</param>
	/// <returns>True if there was one to remove.</returns>
	public bool RemoveCommentBox(int commentBoxId) => commentBoxes.RemoveAll(b => b.Id == commentBoxId) > 0;

	/// <summary>Change a comment box's title.</summary>
	/// <param name="commentBoxId">The comment box.</param>
	/// <param name="title">Its new title.</param>
	/// <returns>True if the comment box exists.</returns>
	public bool RenameCommentBox(int commentBoxId, string title)
	{
		Ensure.NotNull(title);
		return ReplaceCommentBox(commentBoxId, box => box with { Title = title });
	}

	/// <summary>Change a comment box's fill colour.</summary>
	/// <param name="commentBoxId">The comment box.</param>
	/// <param name="color">Its new colour, or null to follow the theme.</param>
	/// <returns>True if the comment box exists.</returns>
	public bool SetCommentBoxColor(int commentBoxId, Vector4? color) =>
		ReplaceCommentBox(commentBoxId, box => box with { Color = color });

	/// <summary>
	/// Change a comment box's size, keeping its top-left corner where it is. Its contents do not move.
	/// </summary>
	/// <param name="commentBoxId">The comment box.</param>
	/// <param name="size">Its new size, raised to <see cref="MinimumCommentBoxSize"/> if smaller.</param>
	/// <returns>True if the comment box exists.</returns>
	/// <remarks>
	/// Resizing is how a box takes in a node or lets one go: containment is geometric, so a node
	/// is in the box as soon as the box grows to cover it.
	/// </remarks>
	public bool ResizeCommentBox(int commentBoxId, Vector2 size) =>
		ReplaceCommentBox(commentBoxId, box => box with { Size = Vector2.Max(size, MinimumCommentBoxSize) });

	/// <summary>
	/// The nodes lying wholly inside a comment box.
	/// </summary>
	/// <param name="commentBoxId">The comment box.</param>
	/// <returns>Their ids, or nothing if there is no such box.</returns>
	/// <remarks>
	/// A node that has never been drawn has no measured size yet, so it counts as inside when its
	/// top-left corner is.
	/// </remarks>
	public IReadOnlyList<int> GetNodesInCommentBox(int commentBoxId)
	{
		CommentBox? box = FindCommentBox(commentBoxId);
		return box is null
			? []
			: [.. nodes.Where(n => box.Contains(n.Position, n.Dimensions)).Select(n => n.Id)];
	}

	/// <summary>
	/// The other comment boxes lying wholly inside a comment box.
	/// </summary>
	/// <param name="commentBoxId">The comment box.</param>
	/// <returns>Their ids, or nothing if there is no such box.</returns>
	public IReadOnlyList<int> GetCommentBoxesInCommentBox(int commentBoxId)
	{
		CommentBox? box = FindCommentBox(commentBoxId);
		return box is null
			? []
			: [.. commentBoxes.Where(b => b.Id != commentBoxId && box.Contains(b.Position, b.Size)).Select(b => b.Id)];
	}

	/// <summary>
	/// Move a comment box, carrying along everything inside it.
	/// </summary>
	/// <param name="commentBoxId">The comment box.</param>
	/// <param name="delta">How far to move it.</param>
	/// <returns>The moves made to the nodes it carried, or nothing if there is no such box.</returns>
	/// <remarks>
	/// What is inside is decided before anything moves, so a node the box passes over on its way is
	/// not picked up. Comment boxes nested inside this one move with it, and so do their nodes; a
	/// node inside two nested boxes is moved once.
	/// <para>
	/// The nodes are moved with <see cref="UpdateNodePosition"/>, the same as a drag, so a physics
	/// simulation left running is free to move them again afterwards.
	/// </para>
	/// </remarks>
	public IReadOnlyList<NodeMove> MoveCommentBox(int commentBoxId, Vector2 delta)
	{
		CommentBox? box = FindCommentBox(commentBoxId);
		if (box is null)
		{
			return [];
		}

		IReadOnlyList<int> carriedNodes = GetNodesInCommentBox(commentBoxId);
		IReadOnlyList<int> carriedBoxes = GetCommentBoxesInCommentBox(commentBoxId);

		ReplaceCommentBox(commentBoxId, b => b with { Position = b.Position + delta });
		foreach (int nestedId in carriedBoxes)
		{
			ReplaceCommentBox(nestedId, b => b with { Position = b.Position + delta });
		}

		List<NodeMove> moves = [];
		foreach (int nodeId in carriedNodes)
		{
			Node node = nodes.Find(n => n.Id == nodeId)!;
			Vector2 to = node.Position + delta;
			UpdateNodePosition(nodeId, to);
			moves.Add(new NodeMove(nodeId, node.Position, to));
		}

		return moves;
	}

	private bool ReplaceCommentBox(int commentBoxId, Func<CommentBox, CommentBox> change)
	{
		int index = commentBoxes.FindIndex(b => b.Id == commentBoxId);
		if (index < 0)
		{
			return false;
		}

		commentBoxes[index] = change(commentBoxes[index]);
		return true;
	}

	/// <summary>
	/// Put a comment box back exactly as it was, id and all, or overwrite the one holding its id.
	/// </summary>
	/// <param name="box">The comment box as it should be.</param>
	/// <param name="index">Where in the drawing order it goes if it is not already present.</param>
	internal void RestoreCommentBox(CommentBox box, int index)
	{
		int existing = commentBoxes.FindIndex(b => b.Id == box.Id);
		if (existing >= 0)
		{
			commentBoxes[existing] = box;
		}
		else
		{
			commentBoxes.Insert(Math.Clamp(index, 0, commentBoxes.Count), box);
		}

		nextCommentBoxId = Math.Max(nextCommentBoxId, box.Id + 1);
	}
}
