// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Testing;

using System;
using System.Diagnostics.CodeAnalysis;
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
	private bool disposed;

	/// <summary>Initializes the context, display metrics and font atlas.</summary>
	/// <param name="width">Display width in pixels.</param>
	/// <param name="height">Display height in pixels.</param>
	/// <param name="dpiScale">Framebuffer scale applied to the display.</param>
	/// <param name="renderer">The renderer receiving draw data and owning the atlas texture.</param>
	[SuppressMessage("Major Code Smell", "S6640:Make sure that using \"unsafe\" is safe here", Justification = "ImGui exposes the ini and log filenames as raw pointers, and clearing them is the only way to stop layout being persisted between test runs.")]
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

		// Same contract as the OpenGL backend: ImGui owns the atlas and asks for texture work
		// through the frame's texture list, which is what lets it rasterize new glyph sizes on
		// demand. A harness that baked one atlas up front could not exercise that.
		io.BackendFlags |= ImGuiBackendFlags.RendererHasTextures;
	}

	/// <summary>Gets the ImGui IO block for this context.</summary>
	public static ImGuiIOPtr IO => ImGui.GetIO();

	/// <summary>
	/// Gets the number of texture creations and uploads the renderer has been asked to perform.
	/// </summary>
	/// <remarks>
	/// This rises whenever ImGui rasterizes something it did not have before, which is the only
	/// outward sign that a glyph was baked on demand rather than taken from a size registered up
	/// front.
	/// </remarks>
	public int TextureUploadCount { get; private set; }

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
		ImDrawDataPtr drawData = ImGui.GetDrawData();
		ProcessTextureUpdates(drawData);
		renderer.RenderDrawData(drawData);
	}

	/// <inheritdoc/>
	[SuppressMessage("Major Code Smell", "S6640:Make sure that using \"unsafe\" is safe here", Justification = "Required to test a native context handle before destroying it; the pointer is compared and never dereferenced.")]
	public void Dispose()
	{
		if (disposed)
		{
			return;
		}

		ImVector<ImTextureDataPtr> textures = ImGui.GetPlatformIO().Textures;
		for (int i = 0; i < textures.Size; i++)
		{
			ImTextureDataPtr tex = textures[i];
			if (tex.RefCount == 1)
			{
				DestroyTexture(tex);
			}
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

	/// <summary>
	/// Reconciles the frame's ImGui-owned textures with the software renderer.
	/// </summary>
	/// <remarks>
	/// The renderer stores whole images, so a partial update is answered by re-uploading the
	/// texture. That is wasteful and entirely fine here: correctness is what a test harness owes,
	/// and it keeps this free of the dirty-rectangle bookkeeping the GPU backend needs.
	/// </remarks>
	[SuppressMessage("Major Code Smell", "S6640:Make sure that using \"unsafe\" is safe here", Justification = "ImGui exposes texture pixels as a raw pointer; the span wrapping it is scoped to this call.")]
	private unsafe void ProcessTextureUpdates(ImDrawDataPtr drawData)
	{
		ImVector<ImTextureDataPtr> textures = drawData.Textures;
		if (textures.Data is null)
		{
			return;
		}

		for (int i = 0; i < textures.Size; i++)
		{
			ImTextureDataPtr tex = textures[i];
			switch (tex.Status)
			{
				case ImTextureStatus.WantCreate:
					tex.SetTexID(renderer.CreateTexture(PixelsOf(tex), tex.Width, tex.Height));
					tex.SetStatus(ImTextureStatus.Ok);
					TextureUploadCount++;
					break;

				case ImTextureStatus.WantUpdates:
					renderer.UpdateTexture((nint)(nuint)tex.GetTexID(), PixelsOf(tex), tex.Width, tex.Height);
					tex.SetStatus(ImTextureStatus.Ok);
					TextureUploadCount++;
					break;

				case ImTextureStatus.WantDestroy when tex.UnusedFrames > 0:
					DestroyTexture(tex);
					break;

				default:
					break;
			}
		}
	}

	/// <summary>Releases the renderer's copy of an ImGui-owned texture.</summary>
	private void DestroyTexture(ImTextureDataPtr tex)
	{
		renderer.DeleteTexture((nint)(nuint)tex.GetTexID());
		tex.SetTexID((nint)0);
		tex.SetStatus(ImTextureStatus.Destroyed);
	}

	/// <summary>Wraps an ImGui texture's pixel buffer without copying it.</summary>
	[SuppressMessage("Major Code Smell", "S6640:Make sure that using \"unsafe\" is safe here", Justification = "ImGui exposes texture pixels as a raw pointer; the span wrapping it is scoped to the caller.")]
	private static unsafe ReadOnlySpan<byte> PixelsOf(ImTextureDataPtr tex) =>
		new(tex.GetPixels(), tex.GetSizeInBytes());
}
