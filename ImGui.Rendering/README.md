# ktsu.ImGui.Rendering

[![NuGet](https://img.shields.io/nuget/v/ktsu.ImGui.Rendering?logo=nuget)](https://nuget.org/packages/ktsu.ImGui.Rendering)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](https://github.com/ktsu-dev/ImGuiApp/blob/main/LICENSE.md)

The backend-agnostic 3D contract for the ktsu ImGui suite, in a package of its own so a widget library
can draw 3D without depending on the application host.

- **`IRenderer3D`** draws indexed triangles or lines into an offscreen render target with a depth
  buffer, which is then shown as an ImGui image. It is not a rendering engine: no shaders, materials
  or lighting, just the textured-triangle path every backend already has, with a matrix and a depth
  buffer. `ktsu.ImGui.App`'s OpenGL backend and `ktsu.ImGui.App.Testing`'s software renderer
  implement it.
- **`Vertex3D`** and **`DrawState3D`** describe what is drawn. A vertex has no normal; bake shading
  into its colour.
- **`Renderer3DHost`** is where a library asks for the host's renderer. `ktsu.ImGui.App` installs its
  resolver automatically.

Most applications never use this package directly: `ImGuiWidgets.Viewport3D` in `ktsu.ImGui.Widgets`
owns the target and the camera, and hands your callback the renderer to draw with.

```csharp
ImGuiWidgets.Viewport3D("scene", Vector2.Zero, cameraState, frame =>
	frame.Renderer.Draw(frame.Target, vertices, indices, new DrawState3D
	{
		ModelViewProjection = model * frame.ViewProjection,
		Depth = DepthMode.TestAndWrite,
	}));
```
