// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Widgets;

using System.Collections.Generic;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Widgets.Gestures;

public static partial class ImGuiWidgets
{
	private static readonly Dictionary<uint, GestureMachine> GestureMachines = [];

	/// <summary>
	/// Claim a rectangular region with an invisible button and report mobile-style gestures
	/// (tap, double-tap, long-press, swipe, pan) over it.
	/// </summary>
	/// <param name="label">Stable label used to push an ImGui ID for per-region state tracking.</param>
	/// <param name="size">Region size in screen pixels.</param>
	/// <param name="settings">Optional threshold overrides; defaults are mobile-friendly.</param>
	/// <returns>Gestures that fired this frame plus continuous pointer state.</returns>
	/// <remarks>
	/// Mouse-only on desktop. The invisible button uses <see cref="ImGuiMouseButton.Left"/>.
	/// State is keyed by <see cref="ImGui.GetID(string)"/> so the same label inside different
	/// parent scopes yields distinct state.
	/// </remarks>
	public static GestureResult GestureDetector(string label, Vector2 size, GestureSettings? settings = null)
	{
		uint id = ImGui.GetID(label);
		if (!GestureMachines.TryGetValue(id, out GestureMachine? machine))
		{
			machine = new GestureMachine(settings);
			GestureMachines[id] = machine;
		}

		ImGui.InvisibleButton(label, size);

		bool isActive = ImGui.IsItemActive();
		Vector2 pos = ImGui.GetMousePos();
		float deltaTime = ImGui.GetIO().DeltaTime;

		return machine.Update(isActive, pos, deltaTime);
	}

	/// <summary>
	/// Reset cached gesture state for a label. Call this when a screen unmounts or focus is lost
	/// and you do not want a stale press to fire a tap on the next interaction.
	/// </summary>
	/// <param name="label">Label previously passed to <see cref="GestureDetector"/>.</param>
	public static void ResetGestureDetector(string label)
	{
		uint id = ImGui.GetID(label);
		if (GestureMachines.TryGetValue(id, out GestureMachine? machine))
		{
			machine.Reset();
		}
	}
}
