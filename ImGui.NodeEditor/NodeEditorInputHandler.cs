// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.NodeEditor;

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Hexa.NET.ImGui;
using Hexa.NET.ImNodes;

/// <summary>
/// Pure input handling class - only handles ImNodes input events, no business logic
/// </summary>
public class NodeEditorInputHandler
{
	/// <summary>
	/// Process all input events and return the actions that should be taken
	/// </summary>
	[SuppressMessage("Major Code Smell", "S2325:Make 'ProcessInput' a static method.", Justification = "Public instance method; making it static would be a breaking API change.")]
	public InputEvents ProcessInput()
	{
		InputEvents events = new();

		// Check for new link creation
		ProcessLinkCreation(events);

		// Check for link deletion
		ProcessLinkDeletion(events);

		// One press clears everything the user selected, links and nodes alike, so the key is read
		// once and both selections are drained from it. Reading it per selection kind would work
		// today but invites the two to drift apart, which is how "Delete removed my links but left
		// the node" happens.
		if (IsDeleteSelectionPressed())
		{
			ProcessSelectedLinkDeletion(events);
			ProcessSelectedNodeDeletion(events);
		}

		// Check for nodes the user selected and asked to duplicate
		ProcessSelectedNodeDuplication(events);

		return events;
	}

	/// <summary>
	/// Whether this frame carries the "remove what I have selected" gesture.
	/// </summary>
	private static bool IsDeleteSelectionPressed()
	{
		// A Delete aimed at a text field is not aimed at the graph. Without this, typing in any
		// input box on the same frame would silently drop the user's selection.
		if (ImGui.GetIO().WantTextInput)
		{
			return false;
		}

		// Backspace is included because on a Mac keyboard it is the key labelled Delete; the
		// forward-delete key is a chord most users never reach for.
		// repeat: false, so holding the key down deletes the selection once rather than firing
		// again every repeat interval.
		return ImGui.IsKeyPressed(ImGuiKey.Delete, repeat: false) || ImGui.IsKeyPressed(ImGuiKey.Backspace, repeat: false);
	}

	/// <summary>
	/// Turns "select a node, then press Ctrl+D" into duplication requests.
	/// </summary>
	/// <remarks>
	/// ImNodes tracks which nodes are selected but offers no notion of duplicating one, so the whole
	/// gesture is the application's to make. The handler only reports what was asked for;
	/// <see cref="NodeEditorEngine.DuplicateNodes"/> is what answers it, and the offset the copies
	/// land at is the application's choice rather than this method's.
	/// </remarks>
	[SuppressMessage("Major Code Smell", "S6640:Make sure that using \"unsafe\" is safe here.", Justification = "Required for native ImNodes interop; the buffer is pinned for the call and not retained.")]
	private static void ProcessSelectedNodeDuplication(InputEvents events)
	{
		// A Ctrl+D aimed at a text field is not aimed at the graph.
		if (ImGui.GetIO().WantTextInput)
		{
			return;
		}

		// As a chord rather than a key plus a modifier test: a chord matches the modifiers exactly,
		// so Ctrl+Shift+D stays available to whatever else wants it, and ImGuiKey.ModCtrl is the
		// Command key on a Mac when the host sets ConfigMacOSXBehaviors. No repeat, so holding the
		// keys down duplicates once rather than filling the graph.
		if (!ImGui.IsKeyChordPressed((int)(ImGuiKey.ModCtrl | ImGuiKey.D)))
		{
			return;
		}

		int selectedCount = ImNodes.NumSelectedNodes();
		if (selectedCount <= 0)
		{
			return;
		}

		int[] selected = new int[selectedCount];
		unsafe
		{
			fixed (int* buffer = selected)
			{
				ImNodes.GetSelectedNodes(buffer);
			}
		}

		// Distinct covers a selection that names an id more than once; duplicating it twice would
		// put two copies in the same place.
		events.NodeDuplicationRequests.AddRange(selected.Distinct());

		// The selection is deliberately left alone. Unlike a delete it names nodes that are still
		// there afterwards, and a second Ctrl+D on the same selection is a reasonable thing to ask
		// for.
	}

	[SuppressMessage("Major Code Smell", "S6640:Make sure that using \"unsafe\" is safe here.", Justification = "Required for native ImNodes interop; pointers are scoped to the call and not retained.")]
	private static void ProcessLinkCreation(InputEvents events)
	{
		int startPin = 0;
		int endPin = 0;
		bool isLinkCreated;

		unsafe
		{
			isLinkCreated = ImNodes.IsLinkCreated(&startPin, &endPin);
		}

		if (isLinkCreated)
		{
			events.LinkCreationRequests.Add(new LinkCreationRequest(startPin, endPin));
		}
	}

	[SuppressMessage("Major Code Smell", "S6640:Make sure that using \"unsafe\" is safe here.", Justification = "Required for native ImNodes interop; pointers are scoped to the call and not retained.")]
	private static void ProcessLinkDeletion(InputEvents events)
	{
		int linkId = 0;
		bool isLinkDestroyed;

		unsafe
		{
			isLinkDestroyed = ImNodes.IsLinkDestroyed(&linkId);
		}

		if (isLinkDestroyed)
		{
			events.LinkDeletionRequests.Add(linkId);
		}
	}

	/// <summary>
	/// Turns "select a link, then press Delete" into deletion requests.
	/// </summary>
	/// <remarks>
	/// ImNodes does not do this for us. <c>ImNodes.IsLinkDestroyed</c> reports only a
	/// link pulled off its pin by a drag, and that gesture is inert unless the editor opts into
	/// <c>ImNodesAttributeFlags.EnableLinkDetachWithDragClick</c> or
	/// <c>ImNodesIO.LinkDetachWithModifierClick</c>. Neither is configured here, so before this
	/// method no gesture could put anything in <see cref="InputEvents.LinkDeletionRequests"/> at
	/// all. ImNodes does track which links are selected, but acting on that selection is left to
	/// the application.
	/// </remarks>
	[SuppressMessage("Major Code Smell", "S6640:Make sure that using \"unsafe\" is safe here.", Justification = "Required for native ImNodes interop; the buffer is pinned for the call and not retained.")]
	private static void ProcessSelectedLinkDeletion(InputEvents events)
	{
		int selectedCount = ImNodes.NumSelectedLinks();
		if (selectedCount <= 0)
		{
			return;
		}

		int[] selected = new int[selectedCount];
		unsafe
		{
			fixed (int* buffer = selected)
			{
				ImNodes.GetSelectedLinks(buffer);
			}
		}

		// A link detached by a drag can also be selected, and ProcessLinkDeletion has already
		// reported it this frame. Asking the application to remove the same id twice would have the
		// second call fail for a link that no longer exists.
		//
		// The filter is materialised before anything is added rather than iterated lazily, so it
		// reads the request list as it stood on entry rather than one it is in the middle of
		// growing. Distinct covers a selection that names an id more than once.
		List<int> newRequests = [.. selected.Where(linkId => !events.LinkDeletionRequests.Contains(linkId)).Distinct()];
		events.LinkDeletionRequests.AddRange(newRequests);

		// The selection named links that are on their way out, so it must not outlive them. Left in
		// place it would name the same ids on the next Delete, and ImNodes would be holding a
		// selection of links the application has already removed.
		ImNodes.ClearLinkSelection();
	}

	/// <summary>
	/// Turns "select a node, then press Delete" into deletion requests.
	/// </summary>
	/// <remarks>
	/// The node half of <see cref="ProcessSelectedLinkDeletion"/>, and for the same reason: ImNodes
	/// tracks which nodes are selected but never acts on that selection, and it has no node
	/// equivalent of <c>IsLinkDestroyed</c> at all. Removing a node also removes the links that
	/// reach it, which is <see cref="NodeEditorEngine.RemoveNode"/>'s job rather than this one's —
	/// a selected link on a doomed node may therefore arrive in
	/// <see cref="InputEvents.LinkDeletionRequests"/> as well, and whichever list the application
	/// drains second finds that id already gone.
	/// </remarks>
	[SuppressMessage("Major Code Smell", "S6640:Make sure that using \"unsafe\" is safe here.", Justification = "Required for native ImNodes interop; the buffer is pinned for the call and not retained.")]
	private static void ProcessSelectedNodeDeletion(InputEvents events)
	{
		int selectedCount = ImNodes.NumSelectedNodes();
		if (selectedCount <= 0)
		{
			return;
		}

		int[] selected = new int[selectedCount];
		unsafe
		{
			fixed (int* buffer = selected)
			{
				ImNodes.GetSelectedNodes(buffer);
			}
		}

		// Distinct covers a selection that names an id more than once. Unlike the link case there is
		// no second source of node deletions to de-duplicate against.
		events.NodeDeletionRequests.AddRange(selected.Distinct());

		// The selection names nodes that are on their way out, so it must not outlive them. Left in
		// place it would name the same ids on the next Delete, and ImNodes would be holding a
		// selection of nodes the application has already removed.
		ImNodes.ClearNodeSelection();
	}
}

/// <summary>
/// Container for all input events that occurred this frame
/// </summary>
public class InputEvents
{
	/// <inheritdoc/>
	public List<LinkCreationRequest> LinkCreationRequests { get; } = [];
	/// <inheritdoc/>
	public List<int> LinkDeletionRequests { get; } = [];
	/// <summary>
	/// Nodes the user selected and asked to remove. Removing a node removes the links that reach it
	/// too, so an application that drains <see cref="LinkDeletionRequests"/> as well may find an id
	/// there that is already gone.
	/// </summary>
	public List<int> NodeDeletionRequests { get; } = [];
	/// <summary>
	/// Nodes the user selected and asked to duplicate. The originals stay where they are; it is the
	/// application that decides where the copies land.
	/// </summary>
	public List<int> NodeDuplicationRequests { get; } = [];
}

/// <summary>
/// Request to create a link between two pins
/// </summary>
public record LinkCreationRequest(int FromPinId, int ToPinId);
