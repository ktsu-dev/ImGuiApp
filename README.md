# ImGuiApp

A bootstrap library to give you an environment to build an ImGUI.NET application with.

## Minimal Example
```csharp
namespace ktsu.ImGuiAppDemo;

using ImGuiNET;
using ktsu.ImGuiApp;

internal class ImGuiAppDemo
{
	private static bool showImGuiDemo;

	private static void Main() =>
		ImGuiApp.Start(nameof(ImGuiAppDemo), new(), OnStart, OnTick, OnMenu, OnWindowResized);

	private static void OnStart() => {}

	private static void OnTick(float dt)
	{
		ImGui.ShowDemoWindow(ref showImGuiDemo);
		ImGui.Begin("Demo");
		ImGui.Text("Hello, ImGui.NET!");
		ImGui.Text("This is a demo of ImGui.NET.");
		ImGui.End();
	}

	private static void OnMenu() => {}

	private static void OnWindowResized() => {}
}

```
