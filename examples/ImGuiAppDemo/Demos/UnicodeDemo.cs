// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Examples.App.Demos;

using Hexa.NET.ImGui;

/// <summary>
/// Demo for Unicode and emoji support
/// </summary>
internal sealed class UnicodeDemo : IDemoTab
{
	public string TabName => "Unicode & Emojis";

	public void Update(float deltaTime)
	{
		// No updates needed for Unicode demo
	}

	public void Render()
	{
		if (ImGui.BeginTabItem(TabName))
		{
			if (ImGui.BeginChild("##content"))
			{
				ImGui.TextWrapped("Unicode and Emoji Support (Enabled by Default)");
				ImGui.TextWrapped("ImGuiApp automatically includes support for Unicode characters and emojis. This feature works with your configured fonts.");

				ImGui.SeparatorText("Basic ASCII:");
				ImGui.Text("Hello World!");
				ImGui.Text("Accented characters: café, naïve, résumé");
				ImGui.Text("Mathematical symbols: ∞ ≠ ≈ ≤ ≥ ± × ÷ ∂ ∑ ∏ √ ∫");
				ImGui.Text("Currency symbols: $ € £ ¥ ₹ ₿");
				ImGui.Text("Arrows: ← → ↑ ↓ ↔ ↕ ⇐ ⇒ ⇑ ⇓");
				ImGui.Text("Geometric shapes: ■ □ ▲ △ ● ○ ◆ ◇ ★ ☆");
				ImGui.Text("Miscellaneous symbols: ♠ ♣ ♥ ♦ ☀ ☁ ☂ ☃ ♪ ♫");

				ImGui.SeparatorText("Full Emoji Range Support (if font supports them)");
				ImGui.Text("Faces: 😀 😃 😄 😁 😆 😅 😂 🤣 😊 😇 😍 😎 🤓 🧐 🤔 😴");
				ImGui.Text("Gestures: 👍 👎 👌 ✌️ 🤞 🤟 🤘 🤙 👈 👉 👆 👇 ☝️ ✋ 🤚 🖐");
				ImGui.Text("Objects: 🚀 💻 📱 🎸 🎨 🏆 🌟 💎 ⚡ 🔥 💡 🔧 ⚙️ 🔑 💰");
				ImGui.Text("Nature: 🌈 🌞 🌙 ⭐ 🌍 🌊 🌳 🌸 🦋 🐝 🐶 🐱 🦊 🐻 🐼");
				ImGui.Text("Food: 🍎 🍌 🍕 🍔 🍟 🍦 🎂 ☕ 🍺 🍷 🍓 🥑 🥨 🧀 🍯");
				ImGui.Text("Transport: 🚗 🚂 ✈️ 🚲 🚢 🚁 🚌 🏍️ 🛸 🚜 🏎️ 🚙 🚕 🚐");
				ImGui.Text("Activities: ⚽ 🏀 🏈 ⚾ 🎾 🏐 🏉 🎱 🏓 🏸 🥊 ⛳ 🎯 🎪");
				ImGui.Text("Weather: ☀️ ⛅ ☁️ 🌤️ ⛈️ 🌧️ ❄️ ☃️ ⛄ 🌬️ 💨 🌊 💧");
				ImGui.Text("Symbols: ❤️ 💚 💙 💜 🖤 💛 💔 ❣️ 💕 💖 💗 💘 💝 ✨");
				ImGui.Text("Arrows: ← → ↑ ↓ ↔ ↕ ↖ ↗ ↘ ↙ ⤴️ ⤵️ 🔀 🔁 🔂 🔄 🔃");
				ImGui.Text("Math: ± × ÷ = ≠ ≈ ≤ ≥ ∞ √ ∑ ∏ ∂ ∫ Ω π α β γ δ");
				ImGui.Text("Geometric: ■ □ ▲ △ ● ○ ◆ ◇ ★ ☆ ♠ ♣ ♥ ♦ ▪ ▫ ◾ ◽");
				ImGui.Text("Currency: $ € £ ¥ ₹ ₿ ¢ ₽ ₩ ₡ ₪ ₫ ₱ ₴ ₦ ₨ ₵");
				ImGui.Text("Dingbats: ✂ ✈ ☎ ⌚ ⏰ ⏳ ⌛ ⚡ ☔ ☂ ☀ ⭐ ☁ ⛅ ❄");
				ImGui.Text("Enclosed: ① ② ③ ④ ⑤ ⑥ ⑦ ⑧ ⑨ ⑩ ⓐ ⓑ ⓒ ⓓ ⓔ ⓕ");

				ImGui.Separator();
				ImGui.TextWrapped("Note: Character display depends on your configured font's Unicode support. " +
								 "If characters show as question marks, your font may not include those glyphs.");

				ImGui.Separator();
				ImGui.TextWrapped("To disable Unicode support (ASCII only), set EnableUnicodeSupport = false in your ImGuiAppConfig.");
			}
			ImGui.EndChild();

			ImGui.EndTabItem();
		}
	}
}
