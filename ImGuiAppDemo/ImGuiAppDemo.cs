namespace ktsu.ImGuiApp.Demo;

using ImGuiNET;

using ktsu.ImGuiApp;

internal static class ImGuiAppDemo
{
	private static bool showImGuiDemo;
	private static void Main() =>
		ImGuiApp.Start(nameof(ImGuiAppDemo), new ImGuiAppWindowState(), OnStart, OnTick, OnMenu, OnWindowResized);

	private static void OnStart()
	{
	}

	private static void OnTick(float dt)
	{
		ImGui.ShowDemoWindow(ref showImGuiDemo);
		if (ImGui.BeginChild("Demo"))
		{
			ImGui.Text("Hello, ImGui.NET!");
			ImGui.Text("This is a demo of ImGui.NET.");
		}

		ImGui.EndChild();
	}

	private static void OnMenu()
	{
		if (ImGui.BeginMenu("View"))
		{
			_ = ImGui.MenuItem("ImGui Demo", string.Empty, ref showImGuiDemo);
			ImGui.EndMenu();
		}
	}

	private static void OnWindowResized()
	{
	}
}
