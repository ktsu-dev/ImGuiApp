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

	OnRender = _ =>
	{
		ImGui.Begin("Smoke");

		// TextUnformatted, not Text: ImGui.Text maps to the variadic igText(fmt, ...), and calling a
		// variadic C function through HexaGen's fixed function-pointer signature crashes on the Apple
		// ARM64 ABI (varargs are passed on the stack; the callee walks a bogus va_list).
		// TextUnformatted maps to the non-variadic igTextUnformatted and is safe.
		ImGui.TextUnformatted("iOS Metal renderer smoke test");
		ImGui.Button("Tap");
		ImGui.End();
	},
});
