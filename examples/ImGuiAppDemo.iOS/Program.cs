// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.App;
using ktsu.Semantics.Paths;
using ktsu.Semantics.Strings;

// A curated iOS showcase for ImGui.App. Unlike the full desktop ImGuiAppDemo (whose ImGuizmo/ImNodes/
// ImPlot tabs and overlay window are desktop-only, and whose ~200 ImGui.Text/BulletText/TextColored
// calls map to variadic cimgui entry points that crash on the Apple ARM64 ABI), this app sticks to the
// iOS-safe subset and demonstrates what does work on-device: widgets, Unicode/emoji glyphs, GPU textures
// (validating ImGuiApp.GetOrLoadTexture on Metal), animation, and the input/IO surface.
//
// THE GOLDEN RULE for iOS text: never call the variadic formatters (ImGui.Text, TextColored, TextWrapped,
// BulletText, SetTooltip, ...). Format with C# string interpolation and pass the result to the
// non-variadic ImGui.TextUnformatted. For color/wrapping, push the relevant state and TextUnformatted.

// --- Captured demo state (top-level locals are hoisted into the OnRender closure) ---
float elapsed = 0f;
int clickCount = 0;
bool checkboxValue = true;
float sliderValue = 0.4f;
Vector3 tint = new(0.26f, 0.59f, 0.98f);
int comboIndex = 1;
int radioIndex = 0;
long frameCount = 0;

// The bundled logo texture, loaded lazily on the first frame (guarded so a missing asset degrades
// gracefully instead of crashing the showcase).
ImGuiAppTextureInfo? logo = null;
bool logoTried = false;

ImGuiApp.Start(new ImGuiAppConfig
{
	Title = "ImGuiApp iOS Demo",
	// Auto-discovery reflects over assemblies for ImGuizmo/ImNodes/ImPlot — there are no native
	// extensions on iOS, so opt out (it is also a no-op there, but this documents intent).
	AutoDiscoverExtensions = false,

	OnRender = dt =>
	{
		elapsed += dt;
		frameCount++;

		// Load the logo on the first frame. On iOS the BundleResource lands in the .app root, which is
		// AppContext.BaseDirectory, so the desktop demo's path resolution works unchanged.
		if (!logoTried)
		{
			logoTried = true;
			AbsoluteFilePath logoPath = AppContext.BaseDirectory.As<AbsoluteDirectoryPath>() / "icon.png".As<FileName>();
			if (File.Exists(logoPath))
			{
				// ImageSharp decode + Metal upload. If iOS trimming/AOT breaks the decode path this throws,
				// which surfaces the full stacktrace in the CI launch log and fails the "logo loaded"
				// assert below — the signal we want, rather than silently shipping a broken texture path.
				logo = ImGuiApp.GetOrLoadTexture(logoPath);
				Console.WriteLine($"IMGUIAPP_DEMO logo loaded {logo.Width}x{logo.Height}");
			}
			else
			{
				Console.WriteLine("IMGUIAPP_DEMO logo asset not found in bundle");
			}
		}

		// Fill the viewport so it reads like a real app rather than a floating window.
		ImGuiViewportPtr viewport = ImGui.GetMainViewport();
		ImGui.SetNextWindowPos(viewport.WorkPos);
		ImGui.SetNextWindowSize(viewport.WorkSize);
		ImGui.Begin("ImGuiApp iOS Demo",
			ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoBringToFrontOnFocus);

		// Draw the logo as an always-visible header thumbnail (not hidden behind an inactive tab) so the
		// ImGui.Image user-texture draw path is exercised every frame, including in the headless CI run.
		if (logo is not null)
		{
			ImGui.Image(logo.TextureRef, new Vector2(24f, 24f));
			ImGui.SameLine();
		}

		ImGui.TextUnformatted("Dear ImGui on iOS \U0001F4F1  —  Metal renderer + ktsu.ImGui.App");
		ImGui.Separator();

		if (ImGui.BeginTabBar("DemoTabs"))
		{
			if (ImGui.BeginTabItem("Widgets"))
			{
				if (ImGui.Button("Tap me"))
				{
					clickCount++;
				}

				ImGui.SameLine();
				ImGui.TextUnformatted($"clicked {clickCount}x");

				_ = ImGui.Checkbox("Enable feature", ref checkboxValue);
				_ = ImGui.SliderFloat("Amount", ref sliderValue, 0f, 1f, "%.2f");
				_ = ImGui.ColorEdit3("Tint", ref tint);
				_ = ImGui.Combo("Mode", ref comboIndex, "Off\0Balanced\0Performance\0");

				ImGui.TextUnformatted("Quality:");
				ImGui.SameLine();
				if (ImGui.RadioButton("Low", radioIndex == 0)) { radioIndex = 0; }
				ImGui.SameLine();
				if (ImGui.RadioButton("Medium", radioIndex == 1)) { radioIndex = 1; }
				ImGui.SameLine();
				if (ImGui.RadioButton("High", radioIndex == 2)) { radioIndex = 2; }

				ImGui.EndTabItem();
			}

			if (ImGui.BeginTabItem("Text"))
			{
				ImGui.SeparatorText("Unicode & emoji");
				ImGui.TextUnformatted("Accented Latin: café  naïve  jalapeño  Zürich");
				ImGui.TextUnformatted("Symbols: → ★ ± ∞ € £ ¥");
				ImGui.TextUnformatted("Emoji: \U0001F600 \U0001F389 \U0001F680 \U0001F4A1 \U0001F525");

				ImGui.SeparatorText("Wrapped & colored (the iOS-safe way)");
				// TextWrapped is variadic; push a wrap position and use TextUnformatted instead.
				ImGui.PushTextWrapPos(ImGui.GetCursorPos().X + ImGui.GetContentRegionAvail().X);
				ImGui.TextUnformatted(
					"This paragraph wraps without ImGui.TextWrapped. The same trick applies to color: " +
					"push ImGuiCol.Text, draw TextUnformatted, then pop — avoiding the variadic TextColored.");
				ImGui.PopTextWrapPos();

				// TextColored is variadic; emulate it with a pushed text color + TextUnformatted.
				ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(tint.X, tint.Y, tint.Z, 1f));
				ImGui.TextUnformatted("This line is tinted by the Widgets-tab color picker.");
				ImGui.PopStyleColor();

				ImGui.EndTabItem();
			}

			if (ImGui.BeginTabItem("Image"))
			{
				if (logo is not null)
				{
					ImGui.TextUnformatted($"GPU texture {logo.Width}x{logo.Height} uploaded via the Metal backend:");
					float side = MathF.Min(192f, ImGui.GetContentRegionAvail().X);
					ImGui.Image(logo.TextureRef, new Vector2(side, side));
				}
				else
				{
					ImGui.TextUnformatted("Logo texture is not available (asset missing from bundle).");
				}

				ImGui.EndTabItem();
			}

			if (ImGui.BeginTabItem("Animation"))
			{
				float pulse = (MathF.Sin(elapsed * 2f) + 1f) * 0.5f;
				ImGui.TextUnformatted($"t = {elapsed:F1}s");
				ImGui.ProgressBar(pulse, new Vector2(-1f, 0f), $"{pulse * 100f:F0}%");

				// A pulsing button: hue cycles with time, all via non-variadic style pushes.
				Vector4 hue = HsvToRgb(elapsed * 0.15f % 1f, 0.6f, 0.9f);
				ImGui.PushStyleColor(ImGuiCol.Button, hue);
				ImGui.Button("Animated", new Vector2(-1f, 40f));
				ImGui.PopStyleColor();

				ImGui.EndTabItem();
			}

			if (ImGui.BeginTabItem("Input / IO"))
			{
				ImGuiIOPtr io = ImGui.GetIO();
				ImGui.TextUnformatted($"Display: {io.DisplaySize.X:F0} x {io.DisplaySize.Y:F0}");
				ImGui.TextUnformatted($"Mouse / touch: {io.MousePos.X:F0}, {io.MousePos.Y:F0}");
				ImGui.TextUnformatted($"Frame: {frameCount}");
				ImGui.TextUnformatted($"Framerate: {io.Framerate:F1} FPS");
				ImGui.TextUnformatted("Touch and drag anywhere to drive the mouse IO above.");

				ImGui.EndTabItem();
			}

			ImGui.EndTabBar();
		}

		ImGui.End();
	},
});

// Minimal HSV->RGB for the animated button tint, avoiding any extra dependency.
static Vector4 HsvToRgb(float h, float s, float v)
{
	float i = MathF.Floor(h * 6f);
	float f = (h * 6f) - i;
	float p = v * (1f - s);
	float q = v * (1f - (f * s));
	float t = v * (1f - ((1f - f) * s));
	return ((int)i % 6) switch
	{
		0 => new Vector4(v, t, p, 1f),
		1 => new Vector4(q, v, p, 1f),
		2 => new Vector4(p, v, t, 1f),
		3 => new Vector4(p, q, v, 1f),
		4 => new Vector4(t, p, v, 1f),
		_ => new Vector4(v, p, q, 1f),
	};
}
