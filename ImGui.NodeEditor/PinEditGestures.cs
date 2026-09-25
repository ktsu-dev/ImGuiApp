// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.NodeEditor;

using System.Collections.Generic;
using System.Threading;
using Hexa.NET.ImGui;

/// <summary>
/// Tells one continuous edit of a pin's value from the next, for
/// <see cref="NodeEditorEngine.SetPinValue(int, object?, long?)"/>.
/// </summary>
/// <remarks>
/// An edit gesture starts when the widget editing the pin is activated — the press on a checkbox or
/// a drag box, the click into a text field — and lasts until the next activation. Every write in
/// between shares one token, which is what lets <see cref="NodeEditorHistory"/> fold a drag across
/// a hundred frames into one step while keeping two separate drags of the same slider apart.
/// <para>
/// <see cref="Track"/> has to be called on every frame the widget is drawn, straight after it, not
/// only on the frames it reports a change: a checkbox is activated on the frame it is pressed and
/// changes on the frame it is released, and a drag box can be activated a frame before it moves.
/// </para>
/// <para>
/// Tokens come from one counter shared by every instance, so the inline editors and the inspector
/// cannot issue the same token for the same pin and have their edits merged into each other.
/// </para>
/// </remarks>
internal sealed class PinEditGestures
{
	private static long lastIssued;

	private readonly Dictionary<int, long> current = [];

	/// <summary>
	/// The gesture the last-submitted widget's edit of a pin belongs to.
	/// </summary>
	/// <param name="pinId">The pin the widget edits.</param>
	/// <returns>The token for the gesture in progress, which is a new one if the widget was just activated.</returns>
	public long Track(int pinId)
	{
		if (ImGui.IsItemActivated() || !current.TryGetValue(pinId, out long gesture))
		{
			gesture = IssueGesture();
			current[pinId] = gesture;
		}

		return gesture;
	}

	/// <summary>A token no instance has issued before.</summary>
	private static long IssueGesture() => Interlocked.Increment(ref lastIssued);

	/// <summary>Forget a pin that is gone.</summary>
	/// <param name="pinId">The pin.</param>
	public void Forget(int pinId) => current.Remove(pinId);
}
