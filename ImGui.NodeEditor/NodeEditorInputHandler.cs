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

		// Check for links the user selected and asked to delete
		ProcessSelectedLinkDeletion(events);

		return events;
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
		// A Delete aimed at a text field is not aimed at the graph. Without this, typing in any
		// input box on the same frame would silently drop the user's selected links.
		if (ImGui.GetIO().WantTextInput)
		{
			return;
		}

		// Backspace is included because on a Mac keyboard it is the key labelled Delete; the
		// forward-delete key is a chord most users never reach for.
		// repeat: false, so holding the key down deletes the selection once rather than firing
		// again every repeat interval.
		if (!ImGui.IsKeyPressed(ImGuiKey.Delete, repeat: false) && !ImGui.IsKeyPressed(ImGuiKey.Backspace, repeat: false))
		{
			return;
		}

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
}

/// <summary>
/// Request to create a link between two pins
/// </summary>
public record LinkCreationRequest(int FromPinId, int ToPinId);
