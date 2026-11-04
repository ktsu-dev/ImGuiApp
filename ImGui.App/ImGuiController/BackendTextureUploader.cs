// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.ImGuiController;

using System;
using System.Diagnostics.CodeAnalysis;

using Hexa.NET.ImGui;

/// <summary>
/// Carries ImGui's texture requests to any <see cref="IRendererBackend"/>, through the whole-image
/// calls every backend has.
/// </summary>
/// <remarks>
/// A changed rectangle is answered by uploading the whole texture again. That costs a backend
/// that could have written the rectangle alone, which is why the OpenGL path has
/// <see cref="GLTextureUploader"/> instead, and costs nothing that matters to one that stores whole
/// images, such as the software renderer the headless harness draws with.
/// </remarks>
internal sealed class BackendTextureUploader : ITextureUploader
{
	private const int BytesPerPixel = 4;

	private readonly IRendererBackend backend;

	/// <summary>Initializes the uploader over a renderer.</summary>
	/// <param name="backend">The renderer that stores the textures.</param>
	public BackendTextureUploader(IRendererBackend backend)
	{
		Ensure.NotNull(backend);
		this.backend = backend;
	}

	/// <inheritdoc/>
	public nint Create(int width, int height, nint pixels) => backend.CreateTexture(PixelsOf(pixels, width, height), width, height);

	/// <inheritdoc/>
	public void Update(nint textureId, int width, int height, ImTextureRect rect, nint pixels) =>
		backend.UpdateTexture(textureId, PixelsOf(pixels, width, height), width, height);

	/// <inheritdoc/>
	public void Destroy(nint textureId) => backend.DeleteTexture(textureId);

	/// <summary>Wraps ImGui's pixel buffer without copying it.</summary>
	[SuppressMessage("Major Code Smell", "S6640:Make sure that using \"unsafe\" is safe here", Justification = "ImGui exposes texture pixels as a raw pointer; the span wrapping it is scoped to the upload call.")]
	private static unsafe ReadOnlySpan<byte> PixelsOf(nint pixels, int width, int height) =>
		new((void*)pixels, width * height * BytesPerPixel);
}
