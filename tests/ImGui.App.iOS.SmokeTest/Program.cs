// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

using Hexa.NET.ImGui;

using ktsu.ImGui.App;

// Headless smoke test for the iOS lifecycle + Metal renderer. On iOS, ImGuiApp.Start hands control
// to UIApplication.Main; each frame now builds a real ImGui frame and submits it through the Metal
// backend. The render callback draws a small window so the simulator CI job exercises the full
// textured path (font-atlas glyphs + filled shapes), not just an empty frame. The view controller's
// IMGUIAPP_IOS_SMOKE_FRAMES hook runs a few frames, prints a success marker, and exits cleanly so CI
// can assert the app launched, rendered, and ticked without crashing in the Metal pipeline.
ImGuiApp.Start(new ImGuiAppConfig
{
	Title = "ImGuiApp iOS Smoke Test",

	// Logs the native Dear ImGui version once the renderer (and statically-linked cimgui) is up. CI
	// greps this to confirm the native ABI matches the managed Hexa.NET.ImGui bindings (1.92.2); a
	// mismatch would mean the cimgui build drifted from the version Hexa expects.
	OnStart = () => Console.WriteLine($"IMGUIAPP_IOS_IMGUI_VERSION={ImGui.GetVersion()}"),

	OnRender = _ =>
	{
		ImGui.Begin("Smoke");
		ImGui.Text("iOS Metal renderer smoke test");
		ImGui.Button("Tap");
		ImGui.End();
	},
});
