namespace ktsu.ImGuiApp.Demo;

using System.Numerics;

using ImGuiNET;

using ktsu.Extensions;
using ktsu.ImGuiApp;
using ktsu.ImGuiAppDemo.Properties;
using ktsu.StrongPaths;

internal static class ImGuiAppDemo
{
	private static bool showImGuiDemo;
	private static void Main() => ImGuiApp.Start(new()
	{
		Title = "ImGuiApp Demo",
		IconPath = AppContext.BaseDirectory.As<AbsoluteDirectoryPath>() / "icon.png".As<FileName>(),
		OnRender = OnRender,
		OnAppMenu = OnAppMenu,
		Fonts = new Dictionary<string, byte[]>
		{
			{ nameof(Resources.CARDCHAR), Resources.CARDCHAR }
		},
	});

	private static void OnRender(float dt)
	{
		ImGui.ShowDemoWindow(ref showImGuiDemo);
		if (ImGui.BeginChild("Demo"))
		{
			using (new FontAppearance(nameof(Resources.CARDCHAR), 24))
			{
				ImGui.Text("Hello, ImGui.NET!");
			}

			ImGui.Text("This is a demo of ImGui.NET.");

			using (new FontAppearance(nameof(Resources.CARDCHAR)))
			{
				ImGui.Text("Fancy!");
			}

			var iconPath = AppContext.BaseDirectory.As<AbsoluteDirectoryPath>() / "icon.png".As<FileName>();
			var iconTexture = ImGuiApp.GetOrLoadTexture(iconPath);
			ImGui.Image((nint)iconTexture.TextureId, new Vector2(128, 128));
		}

		ImGui.EndChild();
	}

	private static void OnAppMenu()
	{
		if (ImGui.BeginMenu("View"))
		{
			_ = ImGui.MenuItem("ImGui Demo", string.Empty, ref showImGuiDemo);
			ImGui.EndMenu();
		}
	}
}
