// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Examples.App;

using ktsu.ImGui.App;
using ktsu.Semantics.Paths;
using ktsu.Semantics.Strings;

/// <summary>
/// Shared demo image assets, split by the texture-loading path that uploads them so the demo
/// exercises both. <see cref="EagerlyLoaded"/> is uploaded from OnStart — while the renderer
/// backend is still being constructed (OnStart runs inside the ImGuiController constructor,
/// before ImGuiApp's controller field is assigned). <see cref="LazilyLoaded"/> is uploaded on
/// first render in the main loop. Keeping the two sets disjoint matters: textures are cached by
/// path, so a single image would only ever upload through whichever path touched it first.
/// </summary>
internal static class DemoImages
{
	/// <summary>
	/// Images loaded eagerly from OnStart. This exercises — and guards against regression of —
	/// the mid-construction texture-upload path that previously threw
	/// "Renderer backend is not initialized." If that breaks again, the demo crashes on launch.
	/// </summary>
	internal static readonly string[] EagerlyLoaded =
	[
		"trevor-curious.png",
		"trevor-thinking.png",
		"trevor-working.png",
	];

	/// <summary>
	/// Images loaded lazily on first render in the main loop (the normal in-frame upload path).
	/// </summary>
	internal static readonly string[] LazilyLoaded =
	[
		"trevor-exploring.png",
		"trevor-success.png",
		"trevor-failure.png",
	];

	/// <summary>
	/// Resolves a demo image file name to its absolute path in the output directory.
	/// </summary>
	/// <param name="fileName">The image file name (e.g. "trevor-curious.png").</param>
	/// <returns>The absolute path to the image alongside the executable.</returns>
	internal static AbsoluteFilePath Path(string fileName) =>
		AppContext.BaseDirectory.As<AbsoluteDirectoryPath>() / fileName.As<FileName>();

	/// <summary>
	/// Eagerly uploads every image in <see cref="EagerlyLoaded"/>. Called from OnStart.
	/// </summary>
	internal static void LoadEager()
	{
		foreach (string fileName in EagerlyLoaded)
		{
			_ = ImGuiApp.GetOrLoadTexture(Path(fileName));
		}
	}
}
