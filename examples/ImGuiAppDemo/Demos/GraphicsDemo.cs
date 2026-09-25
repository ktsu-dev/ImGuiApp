// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Examples.App.Demos;

using System.Numerics;
using Hexa.NET.ImGui;
using ktsu.ImGui.App;
using ktsu.ImGui.App.Images;
using ktsu.Semantics.Paths;
using ktsu.Semantics.Strings;

/// <summary>
/// Demo for graphics and drawing capabilities
/// </summary>
internal sealed class GraphicsDemo : IDemoTab
{
	private readonly List<Vector2> canvasPoints = [];
	private Vector4 drawColor = new(1.0f, 1.0f, 0.0f, 1.0f);
	private float brushSize = 5.0f;
	private float animationTime;

	// Textures uploaded from raw pixel buffers, one per PixelLayout. The demo tabs outlive an ImGui
	// context (the UI tests start a fresh one per test), so they are recreated whenever the context
	// they were made in is no longer the current one.
	private const int PatternSize = 64;
	private const int RowPadding = 16;
	private static readonly PixelLayout[] ShownLayouts = [PixelLayout.Bgra8, PixelLayout.Rgb8, PixelLayout.Bgr8, PixelLayout.Gray8];
	private readonly ImGuiAppTextureInfo?[] layoutTextures = new ImGuiAppTextureInfo?[ShownLayouts.Length];
	private ImGuiAppTextureInfo? animatedTexture;
	private byte[] animatedPixels = [];
	private nint textureContext;
	private bool animatePixels = true;

	public string TabName => "Graphics & Drawing";

	public void Update(float deltaTime) => animationTime += deltaTime;

	public void Render()
	{
		if (DemoProbe.TabItem(TabName))
		{
			if (ImGui.BeginChild("##content"))
			{
				// Image display
				AbsoluteFilePath iconPath = AppContext.BaseDirectory.As<AbsoluteDirectoryPath>() / "icon.png".As<FileName>();
				ImGuiAppTextureInfo iconTexture = ImGuiApp.GetOrLoadTexture(iconPath);

				ImGui.SeparatorText("Image Display:");
				ImGui.Image(iconTexture.TextureRef, new Vector2(64, 64));
				ImGui.SameLine();
				ImGui.Image(iconTexture.TextureRef, new Vector2(32, 32));
				ImGui.SameLine();
				ImGui.Image(iconTexture.TextureRef, new Vector2(16, 16));

				// Two texture-loading paths, side by side. The eager set was uploaded from OnStart
				// (mid-construction); these GetOrLoadTexture calls are cache hits. The lazy set is
				// uploaded here on first render — the normal in-loop path.
				ImGui.SeparatorText("Texture Loading Paths:");

				ImGui.Text("Eagerly loaded in OnStart (mid-construction):");
				RenderImageRow(DemoImages.EagerlyLoaded);

				ImGui.Text("Lazily loaded on first render:");
				RenderImageRow(DemoImages.LazilyLoaded);

				RenderPixelLayouts();

				// Custom drawing with ImDrawList
				ImGui.SeparatorText("Custom Drawing Canvas:");
				ImGui.ColorEdit4("Draw Color", ref drawColor);
				DemoProbe.SliderFloat("Brush Size", ref brushSize, 1.0f, 20.0f);

				if (DemoProbe.Button("Clear Canvas"))
				{
					canvasPoints.Clear();
				}

				Vector2 canvasPos = ImGui.GetCursorScreenPos();
				Vector2 canvasSize = new(400, 200);

				// Draw canvas background
				ImDrawListPtr drawList = ImGui.GetWindowDrawList();
				drawList.AddRectFilled(canvasPos, canvasPos + canvasSize, ImGui.ColorConvertFloat4ToU32(new Vector4(0.1f, 0.1f, 0.1f, 1.0f)));
				drawList.AddRect(canvasPos, canvasPos + canvasSize, ImGui.ColorConvertFloat4ToU32(new Vector4(0.5f, 0.5f, 0.5f, 1.0f)));

				// Handle mouse input for drawing
				ImGui.InvisibleButton("Canvas", canvasSize);
				if (ImGui.IsItemActive() && ImGui.IsMouseDown(ImGuiMouseButton.Left))
				{
					Vector2 mousePos = ImGui.GetMousePos() - canvasPos;
					if (mousePos.X >= 0 && mousePos.Y >= 0 && mousePos.X <= canvasSize.X && mousePos.Y <= canvasSize.Y)
					{
						canvasPoints.Add(mousePos);
					}
				}

				// Draw points
				uint color = ImGui.ColorConvertFloat4ToU32(drawColor);
				foreach (Vector2 point in canvasPoints)
				{
					drawList.AddCircleFilled(canvasPos + point, brushSize, color);
				}

				// Draw some simple shapes for demonstration
				ImGui.SeparatorText("Shape Examples:");
				Vector2 shapeStart = ImGui.GetCursorScreenPos();

				// Simple animated circle
				float t = animationTime;
				Vector2 center = shapeStart + new Vector2(100, 50);
				float radius = 20 + (MathF.Sin(t * 2) * 5);
				drawList.AddCircle(center, radius, ImGui.ColorConvertFloat4ToU32(new Vector4(1, 0, 0, 1)), 16, 2.0f);

				// Moving rectangle
				Vector2 rectPos = shapeStart + new Vector2(200 + (MathF.Sin(t) * 30), 30);
				drawList.AddRectFilled(rectPos, rectPos + new Vector2(40, 40), ImGui.ColorConvertFloat4ToU32(new Vector4(0, 1, 0, 0.7f)));

				// Blend-mode comparison: the same three overlapping translucent circles drawn with
				// the default alpha-over compositing (left) and with additive blending (right).
				// Additive makes the overlaps accumulate toward white for a neon/glow look.
				uint red = ImGui.ColorConvertFloat4ToU32(new Vector4(1.0f, 0.2f, 0.2f, 0.5f));
				uint green = ImGui.ColorConvertFloat4ToU32(new Vector4(0.2f, 1.0f, 0.2f, 0.5f));
				uint blue = ImGui.ColorConvertFloat4ToU32(new Vector4(0.2f, 0.4f, 1.0f, 0.5f));

				Vector2 alphaCenter = shapeStart + new Vector2(80, 150);
				drawList.AddCircleFilled(alphaCenter + new Vector2(-15, 0), 30, red, 32);
				drawList.AddCircleFilled(alphaCenter + new Vector2(15, 0), 30, green, 32);
				drawList.AddCircleFilled(alphaCenter + new Vector2(0, 22), 30, blue, 32);

				Vector2 additiveCenter = shapeStart + new Vector2(260, 150);
				ImGuiApp.SetDrawBlendMode(drawList, ImGuiAppBlendMode.Additive);
				drawList.AddCircleFilled(additiveCenter + new Vector2(-15, 0), 30, red, 32);
				drawList.AddCircleFilled(additiveCenter + new Vector2(15, 0), 30, green, 32);
				drawList.AddCircleFilled(additiveCenter + new Vector2(0, 22), 30, blue, 32);
				ImGuiApp.SetDrawBlendMode(drawList, ImGuiAppBlendMode.AlphaBlend); // restore for the rest of the frame

				ImGui.Dummy(new Vector2(400, 230)); // Reserve space
			}
			ImGui.EndChild();

			ImGui.EndTabItem();
		}
	}

	/// <summary>
	/// Uploads the same picture from each <see cref="PixelLayout"/>, with row padding on the
	/// four-byte and BGR layouts, and streams an animated BGR buffer through UpdateTexture.
	/// </summary>
	private void RenderPixelLayouts()
	{
		ImGui.SeparatorText("Textures From Raw Pixels:");
		ImGui.TextWrapped("One picture, packed four ways. BGRA and BGR rows carry padding past the last pixel, as camera and OpenCV buffers often do; the row stride tells the upload where each row starts.");

		EnsurePixelTextures();

		for (int i = 0; i < ShownLayouts.Length; i++)
		{
			ImGuiAppTextureInfo texture = layoutTextures[i]!;
			ImGui.BeginGroup();
			ImGui.Image(texture.TextureRef, new Vector2(PatternSize, PatternSize));
			ImGui.TextUnformatted(ShownLayouts[i].ToString());
			ImGui.EndGroup();
			ImGui.SameLine();
		}

		ImGui.BeginGroup();
		ImGui.Image(animatedTexture!.TextureRef, new Vector2(PatternSize, PatternSize));
		ImGui.TextUnformatted("UpdateTexture");
		ImGui.EndGroup();

		DemoProbe.Checkbox("Animate streamed pixels", ref animatePixels);
		if (animatePixels)
		{
			int stride = StrideFor(PixelLayout.Bgr8);
			FillPattern(animatedPixels, PixelLayout.Bgr8, stride, animationTime);
			ImGuiApp.UpdateTexture(animatedTexture, animatedPixels, PatternSize, PatternSize, PixelLayout.Bgr8, stride);
		}
	}

	private unsafe void EnsurePixelTextures()
	{
		nint context = (nint)ImGui.GetCurrentContext().Handle;
		if (context == textureContext && animatedTexture is not null)
		{
			return;
		}

		textureContext = context;
		for (int i = 0; i < ShownLayouts.Length; i++)
		{
			PixelLayout layout = ShownLayouts[i];
			int stride = StrideFor(layout);
			byte[] pixels = new byte[stride * PatternSize];
			FillPattern(pixels, layout, stride, 0f);
			layoutTextures[i] = ImGuiApp.CreateTexture(pixels, PatternSize, PatternSize, layout, stride);
		}

		int animatedStride = StrideFor(PixelLayout.Bgr8);
		animatedPixels = new byte[animatedStride * PatternSize];
		FillPattern(animatedPixels, PixelLayout.Bgr8, animatedStride, animationTime);
		animatedTexture = ImGuiApp.CreateTexture(animatedPixels, PatternSize, PatternSize, PixelLayout.Bgr8, animatedStride);
	}

	private static int BytesPerPixel(PixelLayout layout) => layout switch
	{
		PixelLayout.Rgba8 or PixelLayout.Bgra8 => 4,
		PixelLayout.Rgb8 or PixelLayout.Bgr8 => 3,
		_ => 1,
	};

	// Padded where a real source commonly pads, tight elsewhere, so both stride paths are shown.
	private static int StrideFor(PixelLayout layout) =>
		(PatternSize * BytesPerPixel(layout)) + (layout is PixelLayout.Bgra8 or PixelLayout.Bgr8 ? RowPadding : 0);

	/// <summary>
	/// Writes a gradient with a moving ring into <paramref name="pixels"/> in the given layout.
	/// </summary>
	private static void FillPattern(byte[] pixels, PixelLayout layout, int stride, float time)
	{
		int bpp = BytesPerPixel(layout);
		for (int y = 0; y < PatternSize; y++)
		{
			for (int x = 0; x < PatternSize; x++)
			{
				float dx = x - (PatternSize / 2f);
				float dy = y - (PatternSize / 2f);
				float ring = 0.5f + (0.5f * MathF.Sin((MathF.Sqrt((dx * dx) + (dy * dy)) * 0.4f) - (time * 4f)));
				byte r = (byte)(x * 255 / (PatternSize - 1));
				byte g = (byte)(y * 255 / (PatternSize - 1));
				byte b = (byte)(ring * 255f);
				int offset = (y * stride) + (x * bpp);
				switch (layout)
				{
					case PixelLayout.Bgra8:
						pixels[offset] = b;
						pixels[offset + 1] = g;
						pixels[offset + 2] = r;
						pixels[offset + 3] = 255;
						break;
					case PixelLayout.Rgb8:
						pixels[offset] = r;
						pixels[offset + 1] = g;
						pixels[offset + 2] = b;
						break;
					case PixelLayout.Bgr8:
						pixels[offset] = b;
						pixels[offset + 1] = g;
						pixels[offset + 2] = r;
						break;
					default:
						pixels[offset] = (byte)((r + g + b) / 3);
						break;
				}
			}
		}
	}

	// Renders a horizontal row of demo images, loading (or returning cached) textures by path.
	private static void RenderImageRow(string[] fileNames)
	{
		for (int i = 0; i < fileNames.Length; i++)
		{
			ImGuiAppTextureInfo texture = ImGuiApp.GetOrLoadTexture(DemoImages.Path(fileNames[i]));
			ImGui.Image(texture.TextureRef, new Vector2(64, 64));
			if (i < fileNames.Length - 1)
			{
				ImGui.SameLine();
			}
		}
	}
}
