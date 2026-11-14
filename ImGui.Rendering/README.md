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
- **High-range targets**: `CreateRenderTarget(width, height, depth, RenderTargetFormat.Rgba16F)` makes a
  float target, `BlendMode.Additive` accumulates light into it, and `ToneMap(source, destination,
  ToneMap3D)` maps it into an eight-bit target for display (exposure, then a power, then a `ToneCurve`:
  `Clamp`, `Reinhard` or `Exponential`, per channel or, with `PreserveHue`, on the brightest channel
  so bright colours keep their hue; `ToneMap3D.Apply` is the arithmetic). For thousands of
  overlapping, mostly transparent draws, where eight bits round each faint contribution away and drift
  the hue. Ask `SupportsFormat` first: these members have default implementations that report no
  support, so an implementation written before them keeps compiling.
- **Emissive volumes**: `CreateVolumeTexture(width, height, depth, channels, Half[])` uploads one or
  four floats per voxel, and `DrawVolume(target, volume, VolumeDrawState)` ray-marches it over every
  pixel, front to back, adding each channel's value times its colour (`Color`, `Color1`–`Color3`)
  along each pixel's ray through the box `VolumeToWorld` places the texture in, attenuated by
  `exp(-∫ Extinction · value ds)` in front of it, so a dust channel can darken what lies behind. Samples
  scale with the voxels crossed and start at a per-pixel offset, so there is no step to tune and no
  banding; `FadeInStart`/`FadeInEnd` silence the volume near the eye so geometry drawn there can take
  over. Optional low- and high-frequency noise layers (`LowNoiseAmplitude`/`LowNoiseScale`, `HighNoiseAmplitude`/`HighNoiseScale`) multiply in world-fixed value noise for detail finer than the voxels, faded out below a few pixels (`PixelAngle`) so it does not shimmer, and weighted per channel (`NoiseWeights`) so, say, dust can be clumpier than starlight. `VolumeRay` is the per-pixel arithmetic every renderer matches. Ask `SupportsVolumes` first.

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

For a high-range scene, set the camera state's format and tone map; the widget draws into a float
target and shows it tone-mapped, falling back to eight bits on a renderer without float targets:

```csharp
cameraState.Format = RenderTargetFormat.Rgba16F;
cameraState.ToneMap = new ToneMap3D(Exposure: 1f, Power: 0.6f, Curve: ToneCurve.Exponential);
```
