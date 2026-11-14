// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Phosphor;

using System.Numerics;
using Hexa.NET.ImGui;
using ktsu.ScopedAction;

/// <summary>
/// A panel opened by <see cref="ImGuiPhosphor.Frame"/>. Disposing it closes the panel, so a
/// <c>using</c> block keeps the open and the close paired however the content returns.
/// </summary>
public sealed class PhosphorFrame : ScopedAction
{
	internal PhosphorFrame(string title, Vector2 size)
	{
		IsVisible = ImGui.BeginChild(title, size, ImGuiChildFlags.AlwaysUseWindowPadding, ImGuiWindowFlags.NoBackground);
		OnClose = ImGui.EndChild;
		ImGuiPhosphor.DrawFrameChrome(title);
	}

	/// <summary>Gets a value indicating whether the panel's content is visible and should be drawn.</summary>
	public bool IsVisible { get; }
}
