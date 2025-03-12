namespace ktsu.ImGuiApp.Demo;

using ImGuiNET;

using ktsu.Extensions;
using ktsu.ImGuiApp;
using ktsu.StrongPaths;
using ktsu.ImGuiAppDemo.Properties;

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
