// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.ImGuiController;

using Hexa.NET.ImGui;

/// <summary>
/// Satisfies the texture requests ImGui raises when a backend advertises
/// <see cref="ImGuiBackendFlags.RendererHasTextures"/>.
/// </summary>
/// <remarks>
/// ImGui owns the pixels and states what it wants done; this decides what that means and hands the
/// work to an <see cref="ITextureUploader"/>. Keeping the two apart is what allows the font atlas to
/// grow and repack at runtime -- and so glyphs to be rasterized at any size a caller pushes --
/// without the decisions being welded to a particular graphics API.
/// </remarks>
internal sealed class TextureReconciler
{
	private readonly ITextureUploader uploader;

	/// <summary>Initializes the reconciler with the renderer that performs the uploads.</summary>
	/// <param name="uploader">The renderer that performs the uploads.</param>
	public TextureReconciler(ITextureUploader uploader)
	{
		Ensure.NotNull(uploader);
		this.uploader = uploader;
	}

	/// <summary>
	/// Gets the number of textures created or uploaded so far.
	/// </summary>
	/// <remarks>
	/// This rises whenever ImGui has rasterized something it did not have before, which is the only
	/// outward sign that a glyph was baked on demand rather than taken from a size registered up front.
	/// </remarks>
	public int UploadCount { get; private set; }

	/// <summary>Brings every texture in a frame's list up to date, skipping those already settled.</summary>
	/// <param name="textures">The frame's texture list, from <c>ImDrawData.Textures</c>.</param>
	public unsafe void ReconcileFrame(ImVector<ImTextureDataPtr> textures)
	{
		if (textures.Data is null)
		{
			return;
		}

		for (int i = 0; i < textures.Size; i++)
		{
			ImTextureDataPtr tex = textures[i];
			if (tex.Status != ImTextureStatus.Ok)
			{
				Reconcile(tex);
			}
		}
	}

	/// <summary>Acts on one texture's outstanding request.</summary>
	/// <param name="tex">The texture ImGui wants brought up to date.</param>
	/// <exception cref="InvalidOperationException">The texture is not RGBA32.</exception>
	public unsafe void Reconcile(ImTextureDataPtr tex)
	{
		// Uploaders write four channels per pixel, so an Alpha8 texture would go up as garbage
		// rather than fail. ImGui asks for RGBA32 by default; anything else is a configuration
		// mistake worth surfacing where it happens.
		if (tex.Format != ImTextureFormat.Rgba32)
		{
			throw new InvalidOperationException($"ImGui requested a {tex.Format} texture, but only {ImTextureFormat.Rgba32} can be uploaded.");
		}

		switch (tex.Status)
		{
			case ImTextureStatus.WantCreate:
				tex.SetTexID(uploader.Create(tex.Width, tex.Height, (nint)tex.GetPixels()));
				tex.SetStatus(ImTextureStatus.Ok);
				UploadCount++;
				break;

			case ImTextureStatus.WantUpdates:
				for (int i = 0; i < tex.Updates.Size; i++)
				{
					ImTextureRect rect = tex.Updates[i];
					uploader.Update((nint)(nuint)tex.GetTexID(), tex.Width, rect, (nint)tex.GetPixelsAt(rect.X, rect.Y));
				}

				tex.SetStatus(ImTextureStatus.Ok);
				UploadCount++;
				break;

			// UnusedFrames guards against releasing a texture the renderer may still be reading:
			// ImGui asks for destruction as soon as a texture falls out of use, which can be the
			// same frame it was last drawn with.
			case ImTextureStatus.WantDestroy when tex.UnusedFrames > 0:
				Destroy(tex);
				break;

			default:
				break;
		}
	}

	/// <summary>Releases every texture the uploader still solely owns.</summary>
	/// <remarks>
	/// A RefCount of 1 means this reconciler holds the only remaining reference, so the texture is
	/// ours to release. Anything still referenced elsewhere is left to its owner.
	/// </remarks>
	/// <param name="textures">The platform texture list, from <c>ImGuiPlatformIO.Textures</c>.</param>
	public unsafe void DestroyAll(ImVector<ImTextureDataPtr> textures)
	{
		if (textures.Data is null)
		{
			return;
		}

		for (int i = 0; i < textures.Size; i++)
		{
			ImTextureDataPtr tex = textures[i];
			if (tex.RefCount == 1)
			{
				Destroy(tex);
			}
		}
	}

	private void Destroy(ImTextureDataPtr tex)
	{
		uploader.Destroy((nint)(nuint)tex.GetTexID());
		tex.SetTexID((nint)0);
		tex.SetStatus(ImTextureStatus.Destroyed);
	}
}
