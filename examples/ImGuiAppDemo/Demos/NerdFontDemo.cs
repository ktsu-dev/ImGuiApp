// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Examples.App.Demos;

using Hexa.NET.ImGui;

/// <summary>
/// Demo for Nerd Font icon support
/// </summary>
internal sealed class NerdFontDemo : IDemoTab
{
	public string TabName => "Nerd Fonts";

	public void Update(float deltaTime)
	{
		// No updates needed for Nerd Font demo
	}

	public void Render()
	{
		if (ImGui.BeginTabItem(TabName))
		{
			ImGui.TextWrapped("Nerd Font Icons (Patched Fonts)");
			ImGui.TextWrapped("This tab demonstrates Nerd Font icons if you're using a Nerd Font (like JetBrains Mono Nerd Font, Fira Code Nerd Font, etc.).");

			// Powerline symbols
			ImGui.SeparatorText("Powerline Symbols:");
			ImGui.Text("Basic: \uE0A0 \uE0A1 \uE0A2 \uE0B0 \uE0B1 \uE0B2 \uE0B3");
			ImGui.Text("Extra: \uE0A3 \uE0B4 \uE0B5 \uE0B6 \uE0B7 \uE0B8 \uE0CA \uE0CC \uE0CD \uE0D0 \uE0D1 \uE0D4");

			// Font Awesome icons
			ImGui.SeparatorText("Font Awesome Icons");
			ImGui.Text("Files & Folders: \uF07B \uF07C \uF15B \uF15C \uF016 \uF017 \uF019 \uF01A \uF093 \uF095");
			ImGui.Text("Git & Version Control: \uF1D3 \uF1D2 \uF126 \uF127 \uF128 \uF129 \uF12A \uF12B");
			ImGui.Text("Media & UI: \uF04B \uF04C \uF04D \uF050 \uF051 \uF048 \uF049 \uF067 \uF068 \uF00C \uF00D");

			// Material Design icons
			ImGui.SeparatorText("Material Design Icons");
			ImGui.Text("Navigation: \uF52A \uF52B \uF544 \uF53F \uF540 \uF541 \uF542 \uF543");
			ImGui.Text("Actions: \uF8D5 \uF8D6 \uF8D7 \uF8D8 \uF8D9 \uF8DA \uF8DB \uF8DC");
			ImGui.Text("Content: \uF1C1 \uF1C2 \uF1C3 \uF1C4 \uF1C5 \uF1C6 \uF1C7 \uF1C8");

			// Weather icons
			ImGui.SeparatorText("Weather Icons");
			ImGui.Text("Basic Weather: \uE30D \uE30E \uE30F \uE310 \uE311 \uE312 \uE313 \uE314");
			ImGui.Text("Temperature: \uE315 \uE316 \uE317 \uE318 \uE319 \uE31A \uE31B \uE31C");
			ImGui.Text("Wind & Pressure: \uE31D \uE31E \uE31F \uE320 \uE321 \uE322 \uE323 \uE324");

			// Devicons
			ImGui.SeparatorText("Developer Icons (Devicons)");
			ImGui.Text("Languages: \uE73C \uE73D \uE73E \uE73F \uE740 \uE741 \uE742 \uE743"); // Various programming languages
			ImGui.Text("Frameworks: \uE744 \uE745 \uE746 \uE747 \uE748 \uE749 \uE74A \uE74B");
			ImGui.Text("Tools: \uE74C \uE74D \uE74E \uE74F \uE750 \uE751 \uE752 \uE753");

			// Octicons
			ImGui.SeparatorText("Octicons (GitHub Icons)");
			ImGui.Text("Version Control: \uF418 \uF419 \uF41A \uF41B \uF41C \uF41D \uF41E \uF41F");
			ImGui.Text("Issues & PRs: \uF420 \uF421 \uF422 \uF423 \uF424 \uF425 \uF426 \uF427");
			ImGui.Text("Social: \u2665 \u26A1 \uF428 \uF429 \uF42A \uF42B \uF42C \uF42D");

			// Font Logos
			ImGui.SeparatorText("Brand Logos (Font Logos)");
			ImGui.Text("Tech Brands: \uF300 \uF301 \uF302 \uF303 \uF304 \uF305 \uF306 \uF307");
			ImGui.Text("More Logos: \uF308 \uF309 \uF30A \uF30B \uF30C \uF30D \uF30E \uF30F");

			// Pomicons
			ImGui.SeparatorText("Pomicons");
			ImGui.Text("Small Icons: \uE000 \uE001 \uE002 \uE003 \uE004 \uE005 \uE006 \uE007");
			ImGui.Text("More Icons: \uE008 \uE009 \uE00A \uE00B \uE00C \uE00D");

			ImGui.Separator();
			ImGui.TextWrapped("Note: These icons will only display correctly if you're using a Nerd Font. " +
							 "If you see question marks or boxes, switch to a Nerd Font like 'JetBrains Mono Nerd Font' or 'Fira Code Nerd Font'.");

			ImGui.Separator();
			ImGui.TextWrapped("Popular Nerd Fonts: JetBrains Mono Nerd Font, Fira Code Nerd Font, Hack Nerd Font, " +
							 "Source Code Pro Nerd Font, DejaVu Sans Mono Nerd Font, and many more at nerdfonts.com");

			ImGui.EndTabItem();
		}
	}
}
