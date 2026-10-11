// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Tests;

using System.Runtime.InteropServices;

using Hexa.NET.ImGui;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests for <see cref="ImGuiAppConfig.InterfaceFont"/>: drawing the interface in another face with
/// the bundled Nerd Font and emoji merged beneath it.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class InterfaceFontTests
{
	private const uint Snowman = 0x2603;
	private const uint Diamond = 0x25C6;

	private ImGuiContextPtr context;

	[TestInitialize]
	public void Setup()
	{
		ImGuiApp.Reset();
		FontHelper.CleanupGlyphRanges();
		context = ImGui.CreateContext();
	}

	[TestCleanup]
	public void Cleanup()
	{
		ImGui.DestroyContext(context);
		FontHelper.CleanupCustomFonts();
		FontHelper.CleanupGlyphRanges();
		ImGuiApp.FontIndices.Clear();
		ImGuiApp.Config = new ImGuiAppConfig();
	}

	[TestMethod]
	public unsafe void EmojiRanges_IncludeTheSymbolBlocks()
	{
		// ☃ and ❄ are in Miscellaneous Symbols and Dingbats, which no bundled text face draws, so
		// the emoji font has to be allowed to answer for them.
		uint* ranges = FontHelper.GetEmojiRanges();

		Assert.IsTrue(RangesContain(ranges, Snowman), "U+2603 SNOWMAN is not in the emoji ranges.");
		Assert.IsTrue(RangesContain(ranges, 0x2744), "U+2744 SNOWFLAKE is not in the emoji ranges.");
		Assert.IsTrue(RangesContain(ranges, 0x1F600), "The emoticons are no longer in the emoji ranges.");
	}

	[TestMethod]
	public void WithoutAnInterfaceFont_TheDefaultIsTheNerdFontAlone()
	{
		ImGuiApp.Config = new ImGuiAppConfig();

		int index = LoadFonts();

		ImFontPtr font = ImGui.GetIO().Fonts.Fonts[index];
		Assert.AreEqual(2, font.Sources.Size, "The default should be the Nerd Font with emoji merged in.");
		Assert.IsFalse(ImGuiApp.FontIndices.ContainsKey(FontAppearance.MonospaceFontName), "Without an interface face there is no separate monospace font.");
	}

	[TestMethod]
	public void WithAnInterfaceFont_TheDefaultLayersTheFaceTheNerdFontAndEmoji()
	{
		ImGuiApp.Config = new ImGuiAppConfig { InterfaceFont = Resources.Resources.NotoEmoji };

		int index = LoadFonts();

		ImFontPtr font = ImGui.GetIO().Fonts.Fonts[index];
		Assert.AreEqual(3, font.Sources.Size, "The default should be the face, then the Nerd Font, then emoji.");
		Assert.AreEqual(index, ImGuiApp.FontIndices[FontAppearance.DefaultFontName]);
	}

	[TestMethod]
	public void WithAnInterfaceFont_TheNerdFontIsAlsoRegisteredAlone()
	{
		ImGuiApp.Config = new ImGuiAppConfig { InterfaceFont = Resources.Resources.NotoEmoji };

		int index = LoadFonts();

		Assert.IsTrue(ImGuiApp.FontIndices.TryGetValue(FontAppearance.MonospaceFontName, out int mono), "The Nerd Font is not registered on its own.");
		Assert.AreNotEqual(index, mono);
		Assert.AreEqual(2, ImGui.GetIO().Fonts.Fonts[mono].Sources.Size, "The monospace font should be the Nerd Font with emoji merged in.");
	}

	[TestMethod]
	public void AddInterfaceFont_DrawsSymbolsAndEmojiTheFaceLacks()
	{
		ImGuiIOPtr io = ImGui.GetIO();
		ImFontPtr? added = FontHelper.AddInterfaceFont(io, Resources.Resources.NerdFont, 14f);

		Assert.IsNotNull(added);
		ImFontPtr font = added.Value;
		Assert.AreEqual(3, font.Sources.Size);
		Assert.IsTrue(font.IsGlyphInFont(Diamond), "The diamond the face has should draw.");
		Assert.IsTrue(font.IsGlyphInFont(Snowman), "The snowman only the emoji font has should draw.");
	}

	private static int LoadFonts()
	{
		byte[] emoji = Resources.Resources.NotoEmoji;
		nint emojiHandle = Marshal.AllocHGlobal(emoji.Length);
		ImGuiApp.currentFontMemoryHandles.Add(emojiHandle);
		Marshal.Copy(emoji, 0, emojiHandle, emoji.Length);

		ImGuiApp.Config.DefaultFonts.TryGetValue(FontAppearance.DefaultFontName, out byte[]? nerd);
		Assert.IsNotNull(nerd);
		return ImGuiApp.LoadConfiguredFonts([new(FontAppearance.DefaultFontName, nerd)], ImGui.GetIO().Fonts, false, true, emojiHandle, emoji.Length);
	}

	private static unsafe bool RangesContain(uint* ranges, uint codePoint)
	{
		for (uint* pair = ranges; pair[0] != 0; pair += 2)
		{
			if (codePoint >= pair[0] && codePoint <= pair[1])
			{
				return true;
			}
		}

		return false;
	}
}
