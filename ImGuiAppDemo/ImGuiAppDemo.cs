namespace ktsu.io.ImGuiAppDemo;

using System.Runtime.InteropServices;
using ImGuiNET;
using ktsu.io.ImGuiApp;
using ktsu.io.ImGuiAppDemo.Properties;

internal class ImGuiAppDemo
{
	private static bool showImGuiDemo;
	private static void Main() =>
		ImGuiApp.Start(nameof(ImGuiAppDemo), new ImGuiAppWindowState(), OnStart, OnTick, OnMenu, OnWindowResized);

	private static int[] FontSizes { get; } = [12, 13, 14, 16, 18, 20, 24, 28, 32, 40, 48];
	private static Dictionary<int, ImFontPtr> Fonts { get; } = [];

	internal static void InitFonts()
	{
		byte[] fontBytes = Resources.CARDCHAR;
		var io = ImGui.GetIO();
		var fontAtlasPtr = io.Fonts;
		nint fontBytesPtr = Marshal.AllocHGlobal(fontBytes.Length);
		Marshal.Copy(fontBytes, 0, fontBytesPtr, fontBytes.Length);
		fontAtlasPtr.AddFontDefault();
		foreach (int size in FontSizes)
		{
			unsafe
			{
				var fontConfigNativePtr = ImGuiNative.ImFontConfig_ImFontConfig();
				var fontConfig = new ImFontConfigPtr(fontConfigNativePtr)
				{
					OversampleH = 2,
					OversampleV = 2,
					PixelSnapH = true,
				};
				fontAtlasPtr.AddFontFromMemoryTTF(fontBytesPtr, fontBytes.Length, size, fontConfig, fontAtlasPtr.GetGlyphRangesDefault());
			}
		}

		fontAtlasPtr.Build();

		int numFonts = fontAtlasPtr.Fonts.Size;
		for (int i = 0; i < numFonts; i++)
		{
			var font = fontAtlasPtr.Fonts[i];
			Fonts[(int)font.ConfigData.SizePixels] = font;
		}
	}

	private static void OnStart() => InitFonts();

	private static void OnTick(float dt)
	{
		ImGui.ShowDemoWindow(ref showImGuiDemo);
		ImGui.Begin("Demo");
		ImGui.PushFont(Fonts[24]);
		ImGui.Text("Hello, ImGui.NET!");
		ImGui.PopFont();
		ImGui.Text("This is a demo of ImGui.NET.");
		ImGui.End();
	}

	private static void OnMenu()
	{
	}

	private static void OnWindowResized()
	{
	}
}
