// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.ImGuiController;

using System.Diagnostics.CodeAnalysis;

using Hexa.NET.ImGui;

using Silk.NET.OpenGL;

/// <summary>
/// Carries out <see cref="TextureReconciler"/>'s decisions against OpenGL.
/// </summary>
/// <remarks>
/// This is deliberately all of the graphics-facing work and nothing else, so the decisions above it
/// stay testable and these calls can be driven through <see cref="IGL"/> without a real context.
/// </remarks>
/// <param name="gl">The OpenGL context to issue calls against.</param>
internal sealed class GLTextureUploader(IGL gl) : ITextureUploader
{
	private readonly IGL gl = gl;

	/// <inheritdoc/>
	[SuppressMessage("Major Code Smell", "S6640:Make sure that using \"unsafe\" is safe here", Justification = "Required for native OpenGL interop; the pixel pointer belongs to ImGui and is read within the upload call only.")]
	public unsafe nint Create(int width, int height, nint pixels)
	{
		uint created = gl.GenTexture();
		gl.BindTexture(GLEnum.Texture2D, created);
		gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMinFilter, (int)GLEnum.Linear);
		gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMagFilter, (int)GLEnum.Linear);
		gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapS, (int)GLEnum.ClampToEdge);
		gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapT, (int)GLEnum.ClampToEdge);
		gl.PixelStore(GLEnum.UnpackRowLength, 0);
		gl.TexImage2D(GLEnum.Texture2D, 0, (int)GLEnum.Rgba8, (uint)width, (uint)height, 0, GLEnum.Rgba, GLEnum.UnsignedByte, (void*)pixels);
		gl.CheckError("Create ImGui texture");
		return (nint)created;
	}

	/// <inheritdoc/>
	[SuppressMessage("Major Code Smell", "S6640:Make sure that using \"unsafe\" is safe here", Justification = "Required for native OpenGL interop; the pixel pointer belongs to ImGui and is read within the upload call only.")]
	public unsafe void Update(nint textureId, int sourceRowPixels, ImTextureRect rect, nint pixels)
	{
		gl.BindTexture(GLEnum.Texture2D, (uint)textureId);

		// The rectangle is read out of the middle of a full-atlas buffer, so the unpack stride has
		// to describe that buffer rather than the rectangle. Leaving it set would misread every
		// later upload, so it goes back to 0 straight after.
		gl.PixelStore(GLEnum.UnpackRowLength, sourceRowPixels);
		gl.TexSubImage2D(GLEnum.Texture2D, 0, rect.X, rect.Y, rect.W, rect.H, GLEnum.Rgba, GLEnum.UnsignedByte, (void*)pixels);
		gl.PixelStore(GLEnum.UnpackRowLength, 0);
		gl.CheckError("Update ImGui texture");
	}

	/// <inheritdoc/>
	public void Destroy(nint textureId) => gl.DeleteTexture((uint)textureId);
}
