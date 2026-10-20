// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using Hexa.NET.ImGui;

using ktsu.ImGui.Probes;

using HexaTooltipHelper = Hexa.NET.ImGui.Widgets.TooltipHelper;

public static partial class ImGuiWidgets
{
	/// <summary>
	/// Shows a tooltip for the preceding item while it is hovered.
	/// </summary>
	/// <param name="description">The tooltip text.</param>
	/// <exception cref="ArgumentNullException"><paramref name="description"/> is <see langword="null"/>.</exception>
	/// <remarks>
	/// While the tooltip is shown it is marked for UI probes as <c>tooltip</c>; separate two in one
	/// window with <see cref="ScopedId"/>.
	/// </remarks>
	public static void Tooltip(string description)
	{
		Ensure.NotNull(description);
		if (!ImGuiProbes.IsRecording)
		{
			HexaTooltipHelper.Tooltip(description);
			return;
		}

		int activeBefore = ImGui.GetCurrentContext().WindowsActiveCount;
		HexaTooltipHelper.Tooltip(description);
		MarkTooltipOpenedSince(activeBefore);
	}

	/// <summary>
	/// Marks the tooltip window that began this frame at or after <paramref name="activeBefore"/>,
	/// which is the one the call just opened. Nothing is marked when no tooltip opened.
	/// </summary>
	/// <param name="activeBefore">How many windows had begun this frame before the tooltip call.</param>
	private static void MarkTooltipOpenedSince(int activeBefore)
	{
		ImGuiContextPtr context = ImGui.GetCurrentContext();
		int frame = ImGui.GetFrameCount();
		for (int i = 0; i < context.Windows.Size; i++)
		{
			ImGuiWindowPtr window = context.Windows[i];
			if ((window.Flags & ImGuiWindowFlags.Tooltip) != 0
				&& window.LastFrameActive == frame
				&& window.BeginOrderWithinContext >= activeBefore)
			{
				ImGuiProbes.MarkRegion("tooltip", window.Pos, window.Pos + window.Size);
				return;
			}
		}
	}
}
