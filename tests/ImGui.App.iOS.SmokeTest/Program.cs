// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

using ktsu.ImGui.App;

// Minimal headless smoke test for the iOS lifecycle. On iOS, ImGuiApp.Start hands control to
// UIApplication.Main; the view controller's IMGUIAPP_IOS_SMOKE_FRAMES hook runs a few frames,
// prints a success marker, and exits cleanly so the simulator CI job can assert the app launched
// and ticked. The render callback is intentionally empty — no ImGui drawing exists until the
// Metal backend lands (this verifies lifecycle plumbing only).
ImGuiApp.Start(new ImGuiAppConfig
{
	Title = "ImGuiApp iOS Smoke Test",
	OnRender = _ => { },
});
