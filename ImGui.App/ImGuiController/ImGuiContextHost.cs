// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.ImGuiController;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

using Hexa.NET.ImGui;

/// <summary>
/// Owns an ImGui context and its frame cycle, with no window, no input and no graphics API.
/// </summary>
/// <remarks>
/// <para>
/// This is the half of running ImGui that does not depend on where the frames are shown: creating
/// the context, declaring what the backend supports, setting up the extensions, beginning and
/// ending frames, and keeping ImGui's textures in step with whatever stores them. The desktop
/// application drives it from <see cref="ImGuiController"/> with OpenGL behind it, and the headless
/// test harness drives the same class with the software renderer behind it, so a test runs the
/// setup the application does rather than a copy of it (#313).
/// </para>
/// <para>
/// Texture work goes through an <see cref="ITextureUploader"/>, which is the only thing the host
/// knows about the renderer. Since <see cref="ImGuiBackendFlags.RendererHasTextures"/> ImGui owns
/// the pixels and the renderer only owns texture handles, so that seam is all this needs.
/// </para>
/// </remarks>
internal sealed class ImGuiContextHost : IDisposable
{
	private readonly TextureReconciler textureReconciler;
	private ImGuiContextPtr context;
	private bool disposed;

	/// <summary>Creates the context and sets up the extensions asked for.</summary>
	/// <param name="uploader">Carries ImGui's texture requests to the renderer.</param>
	/// <param name="extensions">The ImGui extensions to set up for this context.</param>
	public ImGuiContextHost(ITextureUploader uploader, ImGuiAppExtensions extensions)
	{
		Ensure.NotNull(uploader);
		textureReconciler = new TextureReconciler(uploader);

		context = ImGui.CreateContext();
		ImGui.SetCurrentContext(context);
		ImGui.StyleColorsDark();

		ImGuiIOPtr io = ImGui.GetIO();

		// RendererHasTextures hands texture lifetime to ImGui: instead of one atlas baked at
		// startup, it asks the backend to create, update, and destroy textures as the frame's needs
		// change. That is what lets the font atlas grow and repack at runtime, so glyphs can be
		// rasterized at whatever size a caller pushes rather than only at sizes registered up front.
		io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset | ImGuiBackendFlags.RendererHasTextures;

		// HasMouseCursors tells ImGui its cursor requests reach the window; without it ImGui also
		// turns off resizing windows by their edges, since nothing would show the user an edge is
		// there. The desktop shows them through PlatformCursor, and the harness reports them.
		io.BackendFlags |= ImGuiBackendFlags.HasMouseCursors;

		// Only the extensions the application asked for are loaded and given contexts; the per-frame
		// BeginFrame and the Cleanup in Dispose skip the rest.
		ImGuiExtensionManager.Initialize(extensions);
		ImGuiExtensionManager.SetImGuiContext();
		ImGuiExtensionManager.CreateExtensionContexts();
	}

	/// <summary>
	/// Gets the number of texture creations and uploads carried out so far.
	/// </summary>
	/// <remarks>
	/// This rises whenever ImGui rasterizes something it did not have before, which is the only
	/// outward sign that a glyph was baked on demand rather than taken from a size registered up
	/// front.
	/// </remarks>
	public int TextureUploadCount => textureReconciler.UploadCount;

	/// <summary>Begins a frame.</summary>
	/// <param name="displaySize">The size of the area being drawn, in ImGui's units.</param>
	/// <param name="framebufferScale">Framebuffer pixels per ImGui unit.</param>
	/// <param name="deltaSeconds">Seconds elapsed since the previous frame.</param>
	public void NewFrame(Vector2 displaySize, Vector2 framebufferScale, float deltaSeconds)
	{
		ObjectDisposedException.ThrowIf(disposed, this);

		ImGui.SetCurrentContext(context);
		ImGuiIOPtr io = ImGui.GetIO();
		io.DisplaySize = displaySize;
		io.DisplayFramebufferScale = framebufferScale;
		io.DeltaTime = deltaSeconds;

		ImGui.NewFrame();

		// Must follow NewFrame: ImGuizmo draws into the window its BeginFrame opens.
		ImGuiExtensionManager.BeginFrame();
	}

	/// <summary>
	/// Ends the frame and brings the renderer's textures up to date with it.
	/// </summary>
	/// <returns>The frame's draw data, ready for the renderer.</returns>
	public ImDrawDataPtr EndFrame()
	{
		ObjectDisposedException.ThrowIf(disposed, this);

		ImGui.Render();
		ImDrawDataPtr drawData = ImGui.GetDrawData();

		// Catch up with texture creations, atlas repacks, and destructions requested since the last
		// frame. Usually there is nothing to do; when a new glyph size is first drawn, this is where
		// the grown atlas reaches the renderer.
		textureReconciler.ReconcileFrame(drawData.Textures);
		return drawData;
	}

	/// <summary>
	/// Releases every texture ImGui still owns, tears down the extensions and destroys the context.
	/// </summary>
	[SuppressMessage("Major Code Smell", "S6640:Make sure that using \"unsafe\" is safe here", Justification = "Required to test a native context handle before destroying it; the pointer is compared and never dereferenced.")]
	public void Dispose()
	{
		if (disposed)
		{
			return;
		}

		unsafe
		{
			if (context.Handle is not null)
			{
				ImGui.SetCurrentContext(context);
				textureReconciler.DestroyAll(ImGui.GetPlatformIO().Textures);

				// Extension teardown reaches into ImGui's context, so it has to happen while that
				// context is still alive. Destroying the ImPlot context after ImGui's has gone
				// faults in native code.
				ImGuiExtensionManager.Cleanup();
				ImGui.DestroyContext(context);
				context = default;
			}
		}

		disposed = true;
	}
}
