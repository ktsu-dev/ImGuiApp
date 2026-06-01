// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Widgets.Gestures;

using System;
using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Discrete gesture events that may fire on a given frame.
/// Multiple flags can be set on the same frame (e.g. <see cref="Tap"/> + <see cref="PanEnd"/>
/// is suppressed by the detector, but <see cref="PanEnd"/> + <see cref="SwipeRight"/> can co-occur).
/// </summary>
[Flags]
[SuppressMessage(
	"Naming",
	"CA1711:Identifiers should not have incorrect suffix",
	Justification = "The 'Flags' suffix is intentional and accurately describes a [Flags] enum used as a result bitmask; the alternatives (GestureKind/GestureType) would imply a single-value classification.")]
[SuppressMessage(
	"Minor Code Smell",
	"S2344:Enumeration type names should not have 'Flags' or 'Enum' suffixes",
	Justification = "Public API: renaming would be a breaking change. The 'Flags' suffix accurately describes this [Flags] bitmask enum.")]
public enum GestureFlags
{
	/// <summary>No gesture fired this frame.</summary>
	None = 0,

	/// <summary>Short press-and-release within tap distance and duration.</summary>
	Tap = 1 << 0,

	/// <summary>Second tap within the configured double-tap interval and radius.</summary>
	DoubleTap = 1 << 1,

	/// <summary>Pointer held in place past the long-press duration. Fires once per press.</summary>
	LongPress = 1 << 2,

	/// <summary>Release with a leftward velocity past the swipe threshold.</summary>
	SwipeLeft = 1 << 3,

	/// <summary>Release with a rightward velocity past the swipe threshold.</summary>
	SwipeRight = 1 << 4,

	/// <summary>Release with an upward velocity past the swipe threshold.</summary>
	SwipeUp = 1 << 5,

	/// <summary>Release with a downward velocity past the swipe threshold.</summary>
	SwipeDown = 1 << 6,

	/// <summary>First frame where movement exceeded the pan threshold.</summary>
	PanStart = 1 << 7,

	/// <summary>Pointer is being dragged. Set every frame from <see cref="PanStart"/> until release.</summary>
	Pan = 1 << 8,

	/// <summary>Pointer released after a pan. Set on the release frame.</summary>
	PanEnd = 1 << 9,
}
