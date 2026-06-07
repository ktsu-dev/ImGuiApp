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
		// Exercise the merged font atlas draw path: an accented Latin glyph (extended Unicode range)
		// and an emoji glyph (merged NotoEmoji range). Missing glyphs are skipped, not fatal, so this
		// can't fail the smoke on its own — its value is that building/uploading the real atlas didn't.
		ImGui.TextUnformatted("Unicode café é ü ñ  emoji \U0001F600 \U0001F389");
		ImGui.Button("Tap");
		ImGui.End();

		// Exercise the app-menu draw path (BeginMainMenuBar/BeginMenu/MenuItem) so its ImGui calls are
		// CI-checked on the Apple ARM64 ABI. ImGuiApp.RenderAppMenu only renders on iPad, and the smoke
		// sim is an iPhone, so call these directly here rather than relying on that path.
		if (ImGui.BeginMainMenuBar())
		{
			if (ImGui.BeginMenu("Smoke"))
			{
				ImGui.MenuItem("Item", string.Empty, false);
				ImGui.EndMenu();
			}

			ImGui.EndMainMenuBar();
		}

		// CI can't inject real touches/keys into the headless simulator, so exercise the ImGui input
		// IO calls the iOS input bridge makes (pointer, button, key, modifier, text, wheel). This
		// verifies they don't crash on the Apple ARM64 ABI the way the variadic igText did.
		ImGuiIOPtr io = ImGui.GetIO();
		io.AddMousePosEvent(8f, 8f);
		io.AddMouseButtonEvent(0, true);
		io.AddMouseButtonEvent(0, false);
		io.AddMouseWheelEvent(0f, 1f);
		io.AddKeyEvent(ImGuiKey.ModShift, true);
		io.AddKeyEvent(ImGuiKey.A, true);
		io.AddKeyEvent(ImGuiKey.A, false);
		io.AddKeyEvent(ImGuiKey.ModShift, false);
		io.AddInputCharacter((uint)'A');
	},
});
