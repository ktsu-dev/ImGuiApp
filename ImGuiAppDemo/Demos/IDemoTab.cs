// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGuiApp.Demo.Demos;

/// <summary>
/// Interface for demo tab implementations
/// </summary>
internal interface IDemoTab
{
	/// <summary>
	/// Gets the name of the tab to display in the UI
	/// </summary>
	string TabName { get; }

	/// <summary>
	/// Renders the demo tab content
	/// </summary>
	void Render();

	/// <summary>
	/// Updates the demo state (called each frame)
	/// </summary>
	/// <param name="deltaTime">Time elapsed since last frame</param>
	void Update(float deltaTime);
}
