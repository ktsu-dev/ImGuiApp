// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Testing;

using System;
using System.Numerics;

using Hexa.NET.ImGui;

/// <summary>
/// Owns an ImGui context with no window, no input backend and no GPU.
/// </summary>
/// <remarks>
/// This duplicates a little of what <c>ImGuiController</c> does, because that class is
/// simultaneously the context owner and the OpenGL renderer and every constructor overload requires
/// Silk's <c>GL</c>, <c>IView</c> and <c>IInputContext</c>. ImGuiApp issue #313 splits those
/// responsibilities, and this class should be deleted when it does.
/// </remarks>
internal sealed class HeadlessImGuiContext : IDisposable
{
	private readonly SoftwareRenderer renderer;
	private ImGuiContextPtr context;
	private nint fontTextureId;
	private bool disposed;

	/// <summary>Initializes the context, display metrics and font atlas.</summary>
	/// <param name="width">Display width in pixels.</param>
	/// <param name="height">Display height in pixels.</param>
	/// <param name="dpiScale">Framebuffer scale applied to the display.</param>
	/// <param name="renderer">The renderer receiving draw data and owning the atlas texture.</param>
	public HeadlessImGuiContext(int width, int height, float dpiScale, SoftwareRenderer renderer)
	{
		Ensure.NotNull(renderer);
		this.renderer = renderer;

		context = ImGui.CreateContext();
		ImGui.SetCurrentContext(context);

		ImGuiIOPtr io = ImGui.GetIO();
		io.DisplaySize = new Vector2(width, height);
		io.DisplayFramebufferScale = new Vector2(dpiScale, dpiScale);
		io.DeltaTime = 1f / 60f;

		// Nothing is read from or written to disk. Layout persisted between runs would make a test
		// depend on whatever ran before it, which is the opposite of what this harness is for.
		unsafe
		{
			io.IniFilename = null;
			io.LogFilename = null;
		}

		BuildFontAtlas(io);
	}

	/// <summary>Gets the ImGui IO block for this context.</summary>
	public static ImGuiIOPtr IO => ImGui.GetIO();

	/// <summary>Begins a frame.</summary>
	/// <param name="delta">Seconds elapsed since the previous frame.</param>
	public void BeginFrame(float delta)
	{
		ImGui.SetCurrentContext(context);
		ImGui.GetIO().DeltaTime = delta;
		ImGui.NewFrame();
	}

	/// <summary>Ends the frame and submits its draw data to the renderer.</summary>
	public void EndFrame()
	{
		ImGui.Render();
		renderer.RenderDrawData(ImGui.GetDrawData());
	}

	/// <inheritdoc/>
	public void Dispose()
	{
		if (disposed)
		{
			return;
		}

		if (fontTextureId != 0)
		{
			renderer.DeleteTexture(fontTextureId);
			fontTextureId = 0;
		}

		unsafe
		{
			if (context.Handle is not null)
			{
				ImGui.DestroyContext(context);
				context = default;
			}
		}

		disposed = true;
	}

	private unsafe void BuildFontAtlas(ImGuiIOPtr io)
	{
		// Matches what ImGuiController.RecreateFontDeviceTexture does for the bound ImGui version,
		// which is the authoritative reference for this atlas API.
		if (!io.Fonts.TexIsBuilt)
		{
			ImGuiP.ImFontAtlasBuildMain(io.Fonts);
		}

		ImTextureDataPtr texData = io.Fonts.TexData;
		if (texData.Handle is null || texData.Pixels is null || texData.Width <= 0 || texData.Height <= 0)
		{
			throw new InvalidOperationException("ImGui produced no font atlas pixels, so no text could be rendered.");
		}

		ReadOnlySpan<byte> pixels = new(texData.Pixels, texData.Width * texData.Height * 4);
		fontTextureId = renderer.CreateTexture(pixels, texData.Width, texData.Height);
		texData.SetTexID(fontTextureId);
	}
}
