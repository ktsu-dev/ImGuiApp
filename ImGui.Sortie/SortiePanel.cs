// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Sortie;

using Hexa.NET.ImGui;
using ktsu.ScopedAction;

/// <summary>
/// The scope of a panel opened by <see cref="ImGuiSortie.Panel"/>. Disposing it closes the panel, so use it in a
/// <c>using</c> block and draw the content inside.
/// </summary>
public sealed class SortiePanel : ScopedAction
{
	internal SortiePanel(bool isVisible) : base(onOpen: null, onClose: ImGui.EndChild) => IsVisible = isVisible;

	/// <summary>Gets a value indicating whether the panel's content is visible, so costly content can be skipped.</summary>
	public bool IsVisible { get; }
}
