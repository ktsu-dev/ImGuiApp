// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Examples.Widgets;

using ktsu.ImGui.App;
using ktsu.Semantics.Paths;
using ktsu.Semantics.Strings;

/// <summary>
/// What more than one section needs from the frame being drawn. Set by the demo's frame and tab
/// callbacks, read by the sections, so no section has to be handed it as an argument.
/// </summary>
internal static class DemoContext
{
	/// <summary>
	/// Gets or sets the frame's delta time. <c>DividerContainer.Tick</c>
	/// needs one, and a section runs from a collapsing header with none threaded through to it, so
	/// the frame callback stashes it here.
	/// </summary>
	internal static float DeltaTime { get; set; }

	/// <summary>Gets the path of the ktsu logo shipped beside the demo.</summary>
	internal static AbsoluteFilePath KtsuIconPath =>
		AppContext.BaseDirectory.As<AbsoluteDirectoryPath>() / "ktsu.png".As<FileName>();

	/// <summary>
	/// Gets or sets the ktsu logo texture, loaded once per frame by the Advanced tab before any of its
	/// sections draws.
	/// </summary>
	internal static ImGuiAppTextureInfo KtsuTexture { get; set; } = new();
}
