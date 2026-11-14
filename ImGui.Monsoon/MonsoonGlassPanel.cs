// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Monsoon;

using Hexa.NET.ImGui;
using ktsu.ImGui.Probes;
using ktsu.ScopedAction;

/// <summary>
/// The scope a <see cref="MonsoonWidgets.GlassPanel"/> is drawn in: the panel's child window was begun when the
/// scope was made, and disposing it ends the child window and marks the panel for probes.
/// </summary>
public sealed class MonsoonGlassPanel : ScopedAction
{
	internal MonsoonGlassPanel(string id, bool isVisible)
		: base(onOpen: null, onClose: () =>
		{
			ImGui.EndChild();
			ImGuiProbes.MarkItem(id);
		}) => IsVisible = isVisible;

	/// <summary>Gets a value indicating whether the panel's contents are visible, so work for them is worth doing.</summary>
	public bool IsVisible { get; }
}
