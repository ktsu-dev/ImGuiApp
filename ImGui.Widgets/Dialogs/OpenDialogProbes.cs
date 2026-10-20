// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System.Collections.Generic;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Probes;

/// <summary>
/// Marks the windows of open Hexa dialogs for UI probes.
/// </summary>
/// <remarks>
/// Hexa's dialogs are sealed and drawn by Hexa's own managers, so nothing inside them can be
/// hooked. Each dialog registers here while it is shown, and the deferred-drawing pump marks every
/// registered window it finds drawn this frame, which is the one place that runs after they draw.
/// </remarks>
internal static class OpenDialogProbes
{
	private static readonly List<Entry> Entries = [];

	/// <summary>Registers an open dialog until <see cref="Remove"/> is called with the same owner.</summary>
	/// <param name="owner">The dialog instance, which identifies the entry.</param>
	/// <param name="probeName">The name the dialog is marked under.</param>
	/// <param name="windowName">Exactly the string Hexa passes to <c>ImGui.Begin</c> for it.</param>
	public static void Add(object owner, string probeName, string windowName) =>
		Entries.Add(new Entry(owner, probeName, windowName));

	/// <summary>Unregisters a dialog when it closes.</summary>
	/// <param name="owner">The dialog instance passed to <see cref="Add"/>.</param>
	public static void Remove(object owner) => Entries.RemoveAll(entry => ReferenceEquals(entry.Owner, owner));

	/// <summary>
	/// Marks every registered dialog window that was drawn this frame. Does nothing unless probes
	/// are recording.
	/// </summary>
	public static void MarkAll()
	{
		if (!ImGuiProbes.IsRecording)
		{
			return;
		}

		int frame = ImGui.GetFrameCount();
		foreach (Entry entry in Entries)
		{
			ImGuiWindowPtr window = ImGuiP.FindWindowByName(entry.WindowName);
			if (!window.IsNull && window.LastFrameActive == frame)
			{
				Vector2 position = window.Pos;
				ImGuiProbes.MarkRegion(entry.ProbeName, position, position + window.Size);
			}
		}
	}

	private sealed record Entry(object Owner, string ProbeName, string WindowName);
}
