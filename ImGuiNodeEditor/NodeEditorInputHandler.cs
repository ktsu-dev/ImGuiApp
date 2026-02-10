// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGuiNodeEditor;

using System.Collections.Generic;
using Hexa.NET.ImNodes;

/// <summary>
/// Pure input handling class - only handles ImNodes input events, no business logic
/// </summary>
public class NodeEditorInputHandler
{
	/// <summary>
	/// Process all input events and return the actions that should be taken
	/// </summary>
	public InputEvents ProcessInput()
	{
		InputEvents events = new();

		// Check for new link creation
		ProcessLinkCreation(events);

		// Check for link deletion
		ProcessLinkDeletion(events);

		return events;
	}

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
