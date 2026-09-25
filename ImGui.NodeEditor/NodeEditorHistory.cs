// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.NodeEditor;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ktsu.UndoRedo;
using ktsu.UndoRedo.Contracts;
using ktsu.UndoRedo.Core.Services;
using ktsu.UndoRedo.Models;

/// <summary>
/// Undo and redo for a <see cref="NodeEditorEngine"/>, recorded onto a <c>ktsu.UndoRedo</c> stack.
/// </summary>
/// <remarks>
/// Every change is stored as the difference it made rather than as a copy of the graph. A change is
/// recorded by running it inside <see cref="Record{T}(string, Func{T})"/>: the graph is captured before
/// and after, and what differs — nodes added or removed with their pins, values and links, links,
/// comment boxes, and any node fields or pin values that changed — becomes one undoable step.
/// Undoing puts back exactly that and nothing else, so a node the layout moved in the meantime is
/// not dragged back to where it stood when some unrelated node was deleted.
/// <para>
/// Three kinds of change are recorded without being asked:
/// </para>
/// <list type="bullet">
/// <item>Pin values written through <see cref="NodeEditorEngine.SetPinValue(int, object?, long?)"/>,
/// which is what the inline editors and <see cref="NodeInspectorPanel"/> do. Writes sharing an edit
/// gesture merge into one step, so a slider dragged across a hundred frames undoes in one.</item>
/// <item>Node drags and comment box gestures, when this history is assigned to
/// <see cref="NodeEditorRenderer.History"/>. A drag is recorded once, when the mouse is released.</item>
/// <item><see cref="NodeEditorEngine.Clear"/> called outside a recording empties the history, since
/// it restarts the id counters and every recorded id would come to name a different node.</item>
/// </list>
/// <para>
/// Everything else — creating, deleting and duplicating nodes, making and breaking links — has to
/// go through this class, either with the convenience methods here or by wrapping the call in
/// <see cref="Record{T}(string, Func{T})"/>. A change made on the engine directly is simply not in
/// the history.
/// </para>
/// <para>
/// Pass the <see cref="AttributeBasedNodeFactory"/> the nodes were made with, if any, so undoing the
/// removal of a factory-made node reattaches its instance: without it the node comes back with its
/// pins and values but <see cref="AttributeBasedNodeFactory.GetBinding"/> no longer knows it.
/// </para>
/// <para>
/// The stack is an ordinary <see cref="IUndoRedoService"/>. A host that already keeps one for the
/// rest of its document can hand it in, and the graph's steps interleave with its own.
/// </para>
/// </remarks>
public sealed class NodeEditorHistory : IDisposable
{
	/// <summary>How many steps a history made with its own stack keeps, when not told.</summary>
	public const int DefaultCapacity = 256;

	private readonly AttributeBasedNodeFactory? factory;

	/// <summary>Above zero while a change is being recorded or a step applied, when the engine's own events are ours.</summary>
	private int suppressDepth;

	/// <summary>Whether the engine was cleared by the change being recorded, which reissues every id.</summary>
	private bool clearedDuringRecord;

	private bool disposed;

	/// <summary>
	/// Create a history with a stack of its own.
	/// </summary>
	/// <param name="engine">The engine to record.</param>
	/// <param name="factory">The factory its nodes are made with, if any.</param>
	/// <param name="capacity">How many steps to keep before the oldest is dropped.</param>
	public NodeEditorHistory(NodeEditorEngine engine, AttributeBasedNodeFactory? factory = null, int capacity = DefaultCapacity)
		: this(engine, CreateService(capacity), factory)
	{
	}

	/// <summary>
	/// Create a history that records onto an existing stack.
	/// </summary>
	/// <param name="engine">The engine to record.</param>
	/// <param name="service">The stack to record onto.</param>
	/// <param name="factory">The factory its nodes are made with, if any.</param>
	public NodeEditorHistory(NodeEditorEngine engine, IUndoRedoService service, AttributeBasedNodeFactory? factory = null)
	{
		Engine = Ensure.NotNull(engine);
		Service = Ensure.NotNull(service);
		this.factory = factory;

		Engine.PinValueChanged += OnPinValueChanged;
		Engine.Cleared += OnCleared;
	}

	private static UndoRedoService CreateService(int capacity) =>
		new(new StackManager(), new SaveBoundaryManager(), new CommandMerger(), UndoRedoOptions.Create(maxStackSize: capacity));

	/// <summary>The stack the steps are recorded onto.</summary>
	public IUndoRedoService Service { get; }

	/// <summary>The engine being recorded.</summary>
	public NodeEditorEngine Engine { get; }

	/// <summary>Whether there is a step to undo.</summary>
	public bool CanUndo => Service.CanUndo;

	/// <summary>Whether there is a step to redo.</summary>
	public bool CanRedo => Service.CanRedo;

	/// <summary>What <see cref="Undo"/> would undo, for a menu item or a tooltip.</summary>
	public string? NextUndoDescription => CanUndo ? Service.Commands[Service.CurrentPosition].Description : null;

	/// <summary>What <see cref="Redo"/> would redo, for a menu item or a tooltip.</summary>
	public string? NextRedoDescription => CanRedo ? Service.Commands[Service.CurrentPosition + 1].Description : null;

	/// <summary>Whether the history is applying a step or recording a change right now.</summary>
	/// <remarks>
	/// Engine events raised while this is true are part of the step, not new edits. A host listening
	/// to the engine for its own bookkeeping can read this to tell the two apart.
	/// </remarks>
	public bool IsApplying => suppressDepth > 0;

	/// <summary>Undo the most recent step.</summary>
	/// <returns>True if there was one.</returns>
	public bool Undo() => CanUndo && Service.Undo();

	/// <summary>Redo the most recently undone step.</summary>
	/// <returns>True if there was one.</returns>
	public bool Redo() => CanRedo && Service.Redo();

	/// <summary>Forget every step.</summary>
	public void Clear() => Service.Clear();

	/// <summary>
	/// Run a change against the engine and record what it did as one step.
	/// </summary>
	/// <typeparam name="T">What the change returns.</typeparam>
	/// <param name="description">What to call the step in a menu.</param>
	/// <param name="change">The change.</param>
	/// <returns>Whatever the change returned.</returns>
	/// <remarks>
	/// A change that turns out to change nothing — a link the engine refused, a removal of an id that
	/// named nothing — records nothing. A change that throws is recorded as far as it got before
	/// throwing, so what it did can still be undone. A recording nested inside another is part of
	/// the outer one.
	/// </remarks>
	public T Record<T>(string description, Func<T> change)
	{
		Ensure.NotNull(description);
		Ensure.NotNull(change);

		if (suppressDepth > 0)
		{
			return change();
		}

		GraphState before = Capture();
		clearedDuringRecord = false;
		suppressDepth++;
		try
		{
			return change();
		}
		finally
		{
			suppressDepth--;
			GraphChange difference = GraphChange.Between(before, Capture(), clearedDuringRecord);
			if (!difference.IsEmpty)
			{
				Push(new GraphChangeCommand(this, description, difference));
			}
		}
	}

	/// <summary>
	/// Run a change against the engine and record what it did as one step.
	/// </summary>
	/// <param name="description">What to call the step in a menu.</param>
	/// <param name="change">The change.</param>
	public void Record(string description, Action change)
	{
		Ensure.NotNull(change);
		Record(description, () =>
		{
			change();
			return true;
		});
	}

	/// <summary>
	/// Record moves that have already been made, such as a drag the user has just finished.
	/// </summary>
	/// <param name="moves">The moves. Those that go nowhere, or name no node, are skipped.</param>
	/// <param name="description">What to call the step, or null for "Move node" or "Move N nodes".</param>
	/// <returns>True if a step was recorded.</returns>
	/// <remarks>
	/// Nothing is moved: the nodes are expected to be at <see cref="NodeMove.To"/> already, which is
	/// where a drag leaves them. <see cref="NodeEditorRenderer"/> calls this itself when it has a
	/// <see cref="NodeEditorRenderer.History"/>.
	/// </remarks>
	public bool RecordNodeMoves(IEnumerable<NodeMove> moves, string? description = null)
	{
		Ensure.NotNull(moves);
		List<NodeMove> real = [.. moves.Where(m => m.From != m.To && Engine.Nodes.Any(n => n.Id == m.NodeId))];
		if (real.Count == 0)
		{
			return false;
		}

		GraphChange change = new() { Moves = real };
		Push(new GraphChangeCommand(this, description ?? (real.Count == 1 ? "Move node" : $"Move {real.Count} nodes"), change));
		return true;
	}

	/// <summary>
	/// Record a finished gesture on comment boxes: the boxes as they were and are, and the nodes they
	/// carried.
	/// </summary>
	internal bool RecordCommentBoxGesture(string description, IReadOnlyList<(CommentBox Before, CommentBox After)> boxes, IReadOnlyList<NodeMove> moves)
	{
		GraphChange change = new()
		{
			ChangedBoxes = [.. boxes.Where(b => b.Before != b.After)],
			Moves = [.. moves.Where(m => m.From != m.To)],
		};

		if (change.IsEmpty)
		{
			return false;
		}

		Push(new GraphChangeCommand(this, description, change));
		return true;
	}

	/// <summary>Create a link as one step. See <see cref="NodeEditorEngine.TryCreateLink"/>.</summary>
	/// <param name="fromPinId">One end.</param>
	/// <param name="toPinId">The other end.</param>
	/// <returns>What the engine said. A refused link records nothing.</returns>
	public LinkCreationResult TryCreateLink(int fromPinId, int toPinId) =>
		Record("Connect pins", () => Engine.TryCreateLink(fromPinId, toPinId));

	/// <summary>Remove a link as one step.</summary>
	/// <param name="linkId">The link.</param>
	/// <returns>True if there was one.</returns>
	public bool RemoveLink(int linkId) => Record("Disconnect pins", () => Engine.RemoveLink(linkId));

	/// <summary>Remove links as one step.</summary>
	/// <param name="linkIds">The links.</param>
	/// <returns>How many there were.</returns>
	public int RemoveLinks(IEnumerable<int> linkIds)
	{
		Ensure.NotNull(linkIds);
		List<int> ids = [.. linkIds];
		return Record(ids.Count == 1 ? "Disconnect pins" : $"Disconnect {ids.Count} links", () => ids.Count(Engine.RemoveLink));
	}

	/// <summary>Remove a node, and the links reaching it, as one step.</summary>
	/// <param name="nodeId">The node.</param>
	/// <returns>True if there was one.</returns>
	public bool RemoveNode(int nodeId) => Record("Delete node", () => Engine.RemoveNode(nodeId));

	/// <summary>Remove nodes, and the links reaching them, as one step.</summary>
	/// <param name="nodeIds">The nodes.</param>
	/// <returns>How many there were.</returns>
	public int RemoveNodes(IEnumerable<int> nodeIds)
	{
		Ensure.NotNull(nodeIds);
		List<int> ids = [.. nodeIds.Distinct()];
		return Record(ids.Count == 1 ? "Delete node" : $"Delete {ids.Count} nodes", () => ids.Count(Engine.RemoveNode));
	}

	/// <summary>Duplicate nodes as one step. See <see cref="NodeEditorEngine.DuplicateNodes"/>.</summary>
	/// <param name="nodeIds">The nodes.</param>
	/// <param name="offset">Where each copy goes, relative to its original.</param>
	/// <returns>The copies.</returns>
	public IReadOnlyList<Node> DuplicateNodes(IEnumerable<int> nodeIds, Vector2 offset) =>
		Record("Duplicate", () => Engine.DuplicateNodes(nodeIds, offset));

	/// <summary>Add a comment box as one step. See <see cref="NodeEditorEngine.CreateCommentBox"/>.</summary>
	/// <param name="position">Its top-left corner.</param>
	/// <param name="size">Its size.</param>
	/// <param name="title">Its title.</param>
	/// <param name="color">Its colour, or null to follow the theme.</param>
	/// <returns>The new comment box.</returns>
	public CommentBox CreateCommentBox(Vector2 position, Vector2 size, string title, Vector4? color = null) =>
		Record("Add comment", () => Engine.CreateCommentBox(position, size, title, color));

	/// <summary>Add a comment box around nodes as one step. See <see cref="NodeEditorEngine.CreateCommentBoxAround"/>.</summary>
	/// <param name="nodeIds">The nodes to enclose.</param>
	/// <param name="title">Its title.</param>
	/// <param name="padding">The room to leave around the nodes.</param>
	/// <param name="color">Its colour, or null to follow the theme.</param>
	/// <returns>The new comment box, or null if none of the ids named a node.</returns>
	public CommentBox? CreateCommentBoxAround(IEnumerable<int> nodeIds, string title, float padding = NodeEditorEngine.DefaultCommentBoxPadding, Vector4? color = null) =>
		Record("Add comment", () => Engine.CreateCommentBoxAround(nodeIds, title, padding, color));

	/// <summary>Remove a comment box as one step. The nodes inside it stay.</summary>
	/// <param name="commentBoxId">The comment box.</param>
	/// <returns>True if there was one.</returns>
	public bool RemoveCommentBox(int commentBoxId) => Record("Delete comment", () => Engine.RemoveCommentBox(commentBoxId));

	/// <summary>Rename a comment box as one step.</summary>
	/// <param name="commentBoxId">The comment box.</param>
	/// <param name="title">Its new title.</param>
	/// <returns>True if there was one.</returns>
	public bool RenameCommentBox(int commentBoxId, string title) => Record("Rename comment", () => Engine.RenameCommentBox(commentBoxId, title));

	/// <summary>Move a comment box and its contents as one step.</summary>
	/// <param name="commentBoxId">The comment box.</param>
	/// <param name="delta">How far to move it.</param>
	/// <returns>The moves made to the nodes it carried.</returns>
	public IReadOnlyList<NodeMove> MoveCommentBox(int commentBoxId, Vector2 delta) =>
		Record("Move comment", () => Engine.MoveCommentBox(commentBoxId, delta));

	/// <inheritdoc/>
	public void Dispose()
	{
		if (disposed)
		{
			return;
		}

		Engine.PinValueChanged -= OnPinValueChanged;
		Engine.Cleared -= OnCleared;
		disposed = true;
	}

	private void OnPinValueChanged(object? sender, PinValueChangedEventArgs e)
	{
		if (suppressDepth > 0 || Equals(e.OldValue, e.NewValue))
		{
			return;
		}

		Push(new PinValueCommand(this, e.PinId, e.OldValue, e.NewValue, e.EditGesture, $"Edit {PinName(e.PinId)}", alreadyApplied: true));
	}

	private void OnCleared(object? sender, EventArgs e)
	{
		if (suppressDepth > 0)
		{
			clearedDuringRecord = true;
			return;
		}

		Service.Clear();
	}

	private string PinName(int pinId)
	{
		foreach (Node node in Engine.Nodes)
		{
			Pin? pin = node.InputPins.Find(p => p.Id == pinId) ?? node.OutputPins.Find(p => p.Id == pinId);
			if (pin is not null)
			{
				return pin.EffectiveDisplayName;
			}
		}

		return "value";
	}

	/// <summary>
	/// Hand a step to the stack. The step describes a change already made, so the stack's own call
	/// to execute it is the one it skips.
	/// </summary>
	private void Push(ICommand command) => Applying(() => Service.Execute(command));

	/// <summary>Run something with the engine's events treated as part of a step rather than as edits.</summary>
	internal void Applying(Action apply)
	{
		suppressDepth++;
		try
		{
			apply();
		}
		finally
		{
			suppressDepth--;
		}
	}

	private GraphState Capture()
	{
		Dictionary<int, NodeState> capturedNodes = [];
		foreach (Node node in Engine.Nodes)
		{
			NodeSnapshot snapshot = Engine.CaptureNode(node.Id)!;
			capturedNodes[node.Id] = new NodeState(snapshot, factory?.GetBinding(node.Id));
		}

		List<IndexedLink> capturedLinks = [.. Engine.Links.Select((link, index) => new IndexedLink(link, index))];
		List<IndexedCommentBox> capturedBoxes = [.. Engine.CommentBoxes.Select((box, index) => new IndexedCommentBox(box, index))];

		return new GraphState(capturedNodes, capturedLinks, capturedBoxes, Engine.WorldOrigin);
	}

	/// <summary>Remove what a step takes away, then put back what it brings, in that order.</summary>
	internal void Apply(GraphChange change, bool forward) =>
		Applying(() =>
		{
			RemoveTakenAway(change, forward);
			PutBack(change, forward);
			RestoreEdits(change, forward);
		});

	private void RemoveTakenAway(GraphChange change, bool forward)
	{
		foreach (IndexedLink link in forward ? change.RemovedLinks : change.AddedLinks)
		{
			Engine.RemoveLink(link.Link.Id);
		}

		foreach (IndexedCommentBox box in forward ? change.RemovedBoxes : change.AddedBoxes)
		{
			Engine.RemoveCommentBox(box.Box.Id);
		}

		foreach (NodeState node in forward ? change.RemovedNodes : change.AddedNodes)
		{
			Engine.RemoveNode(node.Snapshot.Node.Id);
		}
	}

	private void PutBack(GraphChange change, bool forward)
	{
		// In their old order, so each lands back at the index it was captured at.
		foreach (NodeState node in (forward ? change.AddedNodes : change.RemovedNodes).OrderBy(n => n.Snapshot.Index))
		{
			bool restored = Engine.RestoreNode(node.Snapshot);
			if (restored && node.Binding is NodeBinding binding)
			{
				factory?.RestoreBinding(binding);
			}
		}

		foreach (IndexedLink link in (forward ? change.AddedLinks : change.RemovedLinks).OrderBy(l => l.Index))
		{
			Engine.RestoreLink(link.Link);
		}

		foreach (IndexedCommentBox box in (forward ? change.AddedBoxes : change.RemovedBoxes).OrderBy(b => b.Index))
		{
			Engine.RestoreCommentBox(box.Box, box.Index);
		}
	}

	private void RestoreEdits(GraphChange change, bool forward)
	{
		foreach ((Node before, Node after) in change.ChangedNodes)
		{
			Engine.RestoreNodeFields(forward ? after : before);
		}

		foreach (NodeMove move in change.Moves)
		{
			Engine.UpdateNodePosition(move.NodeId, forward ? move.To : move.From);
		}

		foreach ((int pinId, object? before, object? after) in change.ChangedValues)
		{
			Engine.RestorePinValue(pinId, forward ? after : before);
		}

		foreach ((CommentBox before, CommentBox after) in change.ChangedBoxes)
		{
			CommentBox target = forward ? after : before;
			int index = Engine.CommentBoxes.ToList().FindIndex(b => b.Id == target.Id);
			Engine.RestoreCommentBox(target, index < 0 ? Engine.CommentBoxes.Count : index);
		}

		if (change.WorldOrigin is (Vector2 originBefore, Vector2 originAfter))
		{
			Engine.WorldOrigin = forward ? originAfter : originBefore;
		}
	}

	/// <summary>Write a pin's value as part of a step.</summary>
	internal void ApplyPinValue(int pinId, object? value) => Applying(() => Engine.RestorePinValue(pinId, value));
}

/// <summary>A node as captured, with whatever the factory had bound to it.</summary>
internal sealed record NodeState(NodeSnapshot Snapshot, NodeBinding? Binding);

/// <summary>A link and where it stood in the engine's list.</summary>
internal readonly record struct IndexedLink(Link Link, int Index);

/// <summary>A comment box and where it stood in the drawing order.</summary>
internal readonly record struct IndexedCommentBox(CommentBox Box, int Index);

/// <summary>Everything a step can change, captured at one moment.</summary>
internal sealed record GraphState(
	IReadOnlyDictionary<int, NodeState> Nodes,
	IReadOnlyList<IndexedLink> Links,
	IReadOnlyList<IndexedCommentBox> Boxes,
	Vector2 WorldOrigin);

/// <summary>What one step changed, in both directions.</summary>
internal sealed class GraphChange
{
	public IReadOnlyList<NodeState> AddedNodes { get; init; } = [];
	public IReadOnlyList<NodeState> RemovedNodes { get; init; } = [];
	public IReadOnlyList<IndexedLink> AddedLinks { get; init; } = [];
	public IReadOnlyList<IndexedLink> RemovedLinks { get; init; } = [];
	public IReadOnlyList<IndexedCommentBox> AddedBoxes { get; init; } = [];
	public IReadOnlyList<IndexedCommentBox> RemovedBoxes { get; init; } = [];
	public IReadOnlyList<(Node Before, Node After)> ChangedNodes { get; init; } = [];
	public IReadOnlyList<NodeMove> Moves { get; init; } = [];
	public IReadOnlyList<(int PinId, object? Before, object? After)> ChangedValues { get; init; } = [];
	public IReadOnlyList<(CommentBox Before, CommentBox After)> ChangedBoxes { get; init; } = [];
	public (Vector2 Before, Vector2 After)? WorldOrigin { get; init; }

	public bool IsEmpty =>
		AddedNodes.Count == 0 && RemovedNodes.Count == 0 &&
		AddedLinks.Count == 0 && RemovedLinks.Count == 0 &&
		AddedBoxes.Count == 0 && RemovedBoxes.Count == 0 &&
		ChangedNodes.Count == 0 && Moves.Count == 0 &&
		ChangedValues.Count == 0 && ChangedBoxes.Count == 0 &&
		WorldOrigin is null;

	/// <summary>The first node a step touches, for a host that navigates to changes.</summary>
	public int? FirstNodeId =>
		AddedNodes.Concat(RemovedNodes).Select(n => (int?)n.Snapshot.Node.Id).FirstOrDefault()
		?? ChangedNodes.Select(c => (int?)c.After.Id).FirstOrDefault()
		?? Moves.Select(m => (int?)m.NodeId).FirstOrDefault();

	/// <summary>
	/// Work out what changed between two captures.
	/// </summary>
	/// <param name="before">The graph before.</param>
	/// <param name="after">The graph after.</param>
	/// <param name="replacedEverything">
	/// Whether the engine was cleared in between. Clearing restarts the id counters, so an id present
	/// on both sides may name two unrelated things, and the only safe reading is that everything
	/// before was removed and everything after was added.
	/// </param>
	public static GraphChange Between(GraphState before, GraphState after, bool replacedEverything)
	{
		if (replacedEverything)
		{
			return new GraphChange
			{
				RemovedNodes = [.. before.Nodes.Values],
				AddedNodes = [.. after.Nodes.Values],
				RemovedLinks = before.Links,
				AddedLinks = after.Links,
				RemovedBoxes = before.Boxes,
				AddedBoxes = after.Boxes,
				WorldOrigin = before.WorldOrigin == after.WorldOrigin ? null : (before.WorldOrigin, after.WorldOrigin),
			};
		}

		List<(Node, Node)> changedNodes = [];
		List<(int, object?, object?)> changedValues = [];

		foreach ((int id, NodeState was) in before.Nodes)
		{
			if (!after.Nodes.TryGetValue(id, out NodeState? now))
			{
				continue;
			}

			if (FieldsDiffer(was.Snapshot.Node, now.Snapshot.Node))
			{
				changedNodes.Add((was.Snapshot.Node, now.Snapshot.Node));
			}

			Dictionary<int, object?> nowValues = now.Snapshot.Pins.ToDictionary(p => p.PinId, p => p.Value);
			changedValues.AddRange(was.Snapshot.Pins
				.Where(pin => nowValues.TryGetValue(pin.PinId, out object? value) && !Equals(pin.Value, value))
				.Select(pin => (pin.PinId, pin.Value, nowValues[pin.PinId])));
		}

		HashSet<int> linksBefore = [.. before.Links.Select(l => l.Link.Id)];
		HashSet<int> linksAfter = [.. after.Links.Select(l => l.Link.Id)];

		Dictionary<int, CommentBox> boxesBefore = before.Boxes.ToDictionary(b => b.Box.Id, b => b.Box);
		Dictionary<int, CommentBox> boxesAfter = after.Boxes.ToDictionary(b => b.Box.Id, b => b.Box);

		return new GraphChange
		{
			RemovedNodes = [.. before.Nodes.Where(n => !after.Nodes.ContainsKey(n.Key)).Select(n => n.Value)],
			AddedNodes = [.. after.Nodes.Where(n => !before.Nodes.ContainsKey(n.Key)).Select(n => n.Value)],
			RemovedLinks = [.. before.Links.Where(l => !linksAfter.Contains(l.Link.Id))],
			AddedLinks = [.. after.Links.Where(l => !linksBefore.Contains(l.Link.Id))],
			RemovedBoxes = [.. before.Boxes.Where(b => !boxesAfter.ContainsKey(b.Box.Id))],
			AddedBoxes = [.. after.Boxes.Where(b => !boxesBefore.ContainsKey(b.Box.Id))],
			ChangedBoxes = [.. boxesBefore
				.Where(b => boxesAfter.TryGetValue(b.Key, out CommentBox? now) && now != b.Value)
				.Select(b => (b.Value, boxesAfter[b.Key]))],
			ChangedNodes = changedNodes,
			ChangedValues = changedValues,
			WorldOrigin = before.WorldOrigin == after.WorldOrigin ? null : (before.WorldOrigin, after.WorldOrigin),
		};
	}

	/// <summary>
	/// Whether a node's own fields differ: the ones a user or a host sets, not the ones the layout and
	/// the renderer keep writing.
	/// </summary>
	private static bool FieldsDiffer(Node was, Node now) =>
		was.Position != now.Position ||
		was.Name != now.Name ||
		was.IsPinned != now.IsPinned ||
		!was.InputPins.SequenceEqual(now.InputPins) ||
		!was.OutputPins.SequenceEqual(now.OutputPins);
}

/// <summary>One step made of a <see cref="GraphChange"/>.</summary>
internal sealed class GraphChangeCommand(NodeEditorHistory history, string description, GraphChange change)
	: BaseCommand(
		ChangeType.Composite,
		[],
		change.FirstNodeId is int nodeId ? $"node:{nodeId}" : null)
{
	/// <summary>The change was made before the step was pushed, so the stack's first execute is skipped.</summary>
	private bool alreadyApplied = true;

	public override string Description => description;

	public override void Execute()
	{
		if (alreadyApplied)
		{
			alreadyApplied = false;
			return;
		}

		history.Apply(change, forward: true);
	}

	public override void Undo()
	{
		alreadyApplied = false;
		history.Apply(change, forward: false);
	}
}

/// <summary>One step made of a single pin's value, which merges with the next write in the same gesture.</summary>
internal sealed class PinValueCommand(NodeEditorHistory history, int pinId, object? before, object? after, long? gesture, string description, bool alreadyApplied)
	: BaseCommand(ChangeType.Modify, [$"pin:{pinId}"])
{
	private bool alreadyApplied = alreadyApplied;

	public int PinId => pinId;

	public long? Gesture => gesture;

	public object? After => after;

	public override string Description => description;

	public override void Execute()
	{
		if (alreadyApplied)
		{
			alreadyApplied = false;
			return;
		}

		history.ApplyPinValue(pinId, after);
	}

	public override void Undo()
	{
		alreadyApplied = false;
		history.ApplyPinValue(pinId, before);
	}

	/// <summary>Only writes that share a gesture merge: two separate drags of one slider are two steps.</summary>
	public override bool CanMergeWith(ICommand other) =>
		gesture is not null && other is PinValueCommand next && next.PinId == pinId && next.Gesture == gesture;

	/// <summary>
	/// The merged step goes from where this one started to where the next one ended. The stack undoes
	/// this step and then executes the merged one, so the merged one must really execute.
	/// </summary>
	public override ICommand MergeWith(ICommand other) =>
		new PinValueCommand(history, pinId, before, ((PinValueCommand)other).After, gesture, description, alreadyApplied: false);
}
