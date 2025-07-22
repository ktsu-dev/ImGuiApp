// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGuiApp;

using System;
using System.IO;
using System.Runtime.InteropServices;
using Hexa.NET.ImGui;
using ktsu.ImGuiApp.ImGuiController;

/// <summary>
/// Helper class for configuring ImGui fonts with Unicode and emoji support.
/// </summary>
public static class FontHelper
{
	/// <summary>
	/// Gets the path to the Noto Sans font with Unicode support.
	/// </summary>
	/// <returns>The path to Noto Sans font, or null if not found.</returns>
	public static string? GetNotoSansFontPath()
	{
		var possiblePaths = new[]
		{
			"/usr/share/fonts/truetype/noto/NotoSans-Regular.ttf",
			"/usr/share/fonts/opentype/noto/NotoSans-Regular.otf",
			"/System/Library/Fonts/NotoSans.ttf", // macOS
			"C:\\Windows\\Fonts\\NotoSans-Regular.ttf", // Windows
		};

		foreach (var path in possiblePaths)
		{
			if (File.Exists(path))
			{
				return path;
			}
		}

		return null;
	}

	/// <summary>
	/// Gets the path to the Noto Color Emoji font.
	/// </summary>
	/// <returns>The path to Noto Color Emoji font, or null if not found.</returns>
	public static string? GetNotoColorEmojiFontPath()
	{
		var possiblePaths = new[]
		{
			"/usr/share/fonts/truetype/noto/NotoColorEmoji.ttf",
			"/System/Library/Fonts/Apple Color Emoji.ttc", // macOS
			"C:\\Windows\\Fonts\\seguiemj.ttf", // Windows emoji font
		};

		foreach (var path in possiblePaths)
		{
			if (File.Exists(path))
			{
				return path;
			}
		}

		return null;
	}

	/// <summary>
	/// Gets the path to DejaVu Sans font as a fallback.
	/// </summary>
	/// <returns>The path to DejaVu Sans font, or null if not found.</returns>
	public static string? GetDejaVuSansFontPath()
	{
		var possiblePaths = new[]
		{
			"/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf",
			"/System/Library/Fonts/DejaVuSans.ttf", // macOS
			"C:\\Windows\\Fonts\\DejaVuSans.ttf", // Windows
		};

		foreach (var path in possiblePaths)
		{
			if (File.Exists(path))
			{
				return path;
			}
		}

		return null;
	}

	/// <summary>
	/// Creates a font configuration with the best available Unicode font.
	/// </summary>
	/// <param name="fontSize">The font size.</param>
	/// <returns>A font configuration with Unicode support, or null if no suitable font is found.</returns>
	public static ImGuiFontConfig? CreateUnicodeFontConfig(int fontSize)
	{
		string? fontPath = GetNotoSansFontPath() ?? GetDejaVuSansFontPath();
		
		if (fontPath is null)
		{
			return null;
		}

		return ImGuiFontConfig.WithUnicodeSupport(fontPath, fontSize);
	}

	/// <summary>
	/// Creates a font configuration with emoji support using the best available emoji font.
	/// </summary>
	/// <param name="fontSize">The font size.</param>
	/// <returns>A font configuration with emoji support, or null if no suitable font is found.</returns>
	public static ImGuiFontConfig? CreateEmojiFontConfig(int fontSize)
	{
		string? fontPath = GetNotoColorEmojiFontPath();
		
		if (fontPath is null)
		{
			return null;
		}

		return ImGuiFontConfig.WithEmojiSupport(fontPath, fontSize);
	}

	/// <summary>
	/// Configures ImGui IO with Unicode and emoji support by merging fonts.
	/// </summary>
	/// <param name="io">The ImGui IO pointer.</param>
	/// <param name="baseFontSize">The base font size for regular text.</param>
	/// <param name="emojiFontSize">The font size for emojis (optional, defaults to base font size).</param>
	/// <returns>True if fonts were successfully configured, false otherwise.</returns>
	public static unsafe bool ConfigureUnicodeAndEmojiSupport(ImGuiIOPtr io, int baseFontSize, int? emojiFontSize = null)
	{
		emojiFontSize ??= baseFontSize;
		bool success = false;

		// Try to add Unicode font
		var unicodeFontConfig = CreateUnicodeFontConfig(baseFontSize);
		if (unicodeFontConfig.HasValue)
		{
			var config = unicodeFontConfig.Value;
			fixed (byte* fontPathPtr = System.Text.Encoding.UTF8.GetBytes(config.FontPath + "\0"))
			{
				nint glyphRange = config.GetGlyphRange?.Invoke(io) ?? default;
				io.Fonts.AddFontFromFileTTF(fontPathPtr, config.FontSize, null, (uint*)glyphRange);
				success = true;
			}
		}

		// Try to merge emoji font
		var emojiFontConfig = CreateEmojiFontConfig(emojiFontSize.Value);
		if (emojiFontConfig.HasValue)
		{
			var config = emojiFontConfig.Value;
			fixed (byte* fontPathPtr = System.Text.Encoding.UTF8.GetBytes(config.FontPath + "\0"))
			{
				// Create font config for merging
				ImFontConfigPtr fontConfig = ImGui.ImFontConfig();
				fontConfig.MergeMode = true; // Merge with previous font
				fontConfig.PixelSnapH = true;
				
				nint glyphRange = config.GetGlyphRange?.Invoke(io) ?? default;
				io.Fonts.AddFontFromFileTTF(fontPathPtr, config.FontSize, fontConfig, (uint*)glyphRange);
				success = true;
			}
		}

		return success;
	}

	/// <summary>
	/// Creates a simple example showing how to test Unicode and emoji rendering.
	/// </summary>
	/// <param name="windowTitle">The title for the test window.</param>
	public static void ShowUnicodeTestWindow(string windowTitle = "Unicode & Emoji Test")
	{
		if (ImGui.Begin(windowTitle))
		{
			ImGui.Text("Basic ASCII: Hello World!");
			ImGui.Text("Accented characters: café, naïve, résumé");
			ImGui.Text("Mathematical symbols: ∞ ≠ ≈ ≤ ≥ ± × ÷ ∂ ∑ ∏ √ ∫");
			ImGui.Text("Currency symbols: $ € £ ¥ ₹ ₿");
			ImGui.Text("Arrows: ← → ↑ ↓ ↔ ↕ ⇐ ⇒ ⇑ ⇓");
			ImGui.Text("Geometric shapes: ■ □ ▲ △ ● ○ ◆ ◇ ★ ☆");
			ImGui.Text("Miscellaneous symbols: ♠ ♣ ♥ ♦ ☀ ☁ ☂ ☃ ♪ ♫");
			ImGui.Separator();
			ImGui.Text("Emojis (if supported):");
			ImGui.Text("Faces: 😀 😃 😄 😁 😆 😅 😂 🤣 😊 😇");
			ImGui.Text("Objects: 🚀 💻 📱 🎸 🎨 🏆 🌟 💎 ⚡ 🔥");
			ImGui.Text("Nature: 🌈 🌞 🌙 ⭐ 🌍 🌊 🌳 🌸 🦋 🐝");
			ImGui.Text("Food: 🍎 🍌 🍕 🍔 🍟 🍦 🎂 ☕ 🍺 🍷");
		}
		ImGui.End();
	}
}