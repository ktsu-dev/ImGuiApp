// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Widgets.Overlays;

/// <summary>
/// Named z-order bands for overlays hosted by an <see cref="OverlayHost"/>. Lower values are
/// drawn first (further back); higher values are drawn last (closer to the user). Values are
/// spaced out so callers can slot custom layers between the presets by casting an intermediate
/// integer to <see cref="OverlayLayer"/>.
/// </summary>
public enum OverlayLayer
{
	/// <summary>Full-screen scrims and background dimmers that sit behind every other overlay.</summary>
	Background = 0,

	/// <summary>Edge-anchored navigation drawers.</summary>
	Drawer = 1000,

	/// <summary>Slide-up bottom sheets with snap points.</summary>
	BottomSheet = 2000,

	/// <summary>Bottom-anchored action sheets / button stacks.</summary>
	ActionSheet = 3000,

	/// <summary>Modal dialogs and alerts.</summary>
	Dialog = 4000,

	/// <summary>Transient toasts and snackbars.</summary>
	Toast = 5000,

	/// <summary>Tooltips and coach marks that must sit above everything else.</summary>
	Tooltip = 6000,
}
