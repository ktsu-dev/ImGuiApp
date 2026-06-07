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
	// GetVersionS is Hexa's managed-string variant of GetVersion (which returns a raw byte*).
	OnStart = () => Console.WriteLine($"IMGUIAPP_IOS_IMGUI_VERSION={ImGui.GetVersionS()}"),

	OnRender = _ =>
	{
		// Stage tracing: the frame loop SIGSEGVs inside this callback (between the NewFrame and Render
		// markers), so pinpoint which ImGui draw call faults. Crashes on the first frame, so this logs
		// once. Remove once green.
		Console.WriteLine("IMGUIAPP_IOS_OR begin");
		Console.Out.Flush();
		ImGui.Begin("Smoke");
		Console.WriteLine("IMGUIAPP_IOS_OR after-begin");
		Console.Out.Flush();
		// Use TextUnformatted, not Text: ImGui.Text maps to the variadic igText(fmt, ...), and calling
		// a variadic C function through HexaGen's fixed function-pointer signature crashes on the Apple
		// ARM64 ABI (varargs are passed on the stack; the callee walks a bogus va_list).
		// TextUnformatted maps to the non-variadic igTextUnformatted and is safe.
		ImGui.TextUnformatted("iOS Metal renderer smoke test");
		Console.WriteLine("IMGUIAPP_IOS_OR after-text");
		Console.Out.Flush();
		ImGui.Button("Tap");
		Console.WriteLine("IMGUIAPP_IOS_OR after-button");
		Console.Out.Flush();
		ImGui.End();
		Console.WriteLine("IMGUIAPP_IOS_OR after-end");
		Console.Out.Flush();
	},
});
