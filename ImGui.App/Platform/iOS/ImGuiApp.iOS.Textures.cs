// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

#if IOS

namespace ktsu.ImGui.App;

using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

using Hexa.NET.ImGui;

using ktsu.Semantics.Paths;
using ktsu.Semantics.Strings;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

/// <summary>
/// iOS texture surface for <see cref="ImGuiApp"/>: loads images from disk via ImageSharp and uploads
/// them to the GPU through the Metal <see cref="IRendererBackend"/>, mirroring the desktop public API
/// (which lives in the iOS-excluded <c>ImGuiApp.cs</c>). The font atlas already exercises the same
/// <c>Renderer.CreateTexture</c> path, so this adds the decode + cache + user-texture handles on top.
/// </summary>
public static partial class ImGuiApp
{
	/// <summary>Cache of loaded textures keyed by source path, so repeat loads are free (mirrors desktop).</summary>
	internal static ConcurrentDictionary<AbsoluteFilePath, ImGuiAppTextureInfo> Textures { get; } = [];

	private static readonly ArrayPool<byte> bytePool = ArrayPool<byte>.Shared;

	/// <summary>
	/// Gets a previously loaded texture, or loads it from <paramref name="path"/> (decoding with
	/// ImageSharp and uploading to the GPU) and caches it.
	/// </summary>
	/// <param name="path">Absolute path to the image file.</param>
	/// <returns>The texture info, including the GPU handle and an <see cref="ImTextureRef"/> for drawing.</returns>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S6640:Make sure that using \"unsafe\" is safe here", Justification = "Required for native ImGui texture interop; the pointer is scoped to the call and not retained.")]
	public static ImGuiAppTextureInfo GetOrLoadTexture(AbsoluteFilePath path)
	{
		if (Textures.TryGetValue(path, out ImGuiAppTextureInfo? existingTexture))
		{
			return existingTexture;
		}

		using Image<Rgba32> image = Image.Load<Rgba32>(path);

		ImGuiAppTextureInfo textureInfo = new()
		{
			Path = path,
			Width = image.Width,
			Height = image.Height,
		};

		UseImageBytes(image, bytes =>
		{
			textureInfo.TextureId = UploadTextureRGBA(bytes, image.Width, image.Height);
			unsafe
			{
				textureInfo.TextureRef = new ImTextureRef(default, textureInfo.TextureId);
			}
		});

		Textures[path] = textureInfo;
		return textureInfo;
	}

	/// <summary>Tries to get an already-loaded texture without loading it.</summary>
	/// <param name="path">The image file path.</param>
	/// <param name="textureInfo">The texture info if cached; otherwise null.</param>
	/// <returns><see langword="true"/> if the texture was cached.</returns>
	public static bool TryGetTexture(AbsoluteFilePath path, out ImGuiAppTextureInfo? textureInfo) =>
		Textures.TryGetValue(path, out textureInfo);

	/// <summary>Tries to get an already-loaded texture without loading it.</summary>
	/// <param name="path">The image file path as a string.</param>
	/// <param name="textureInfo">The texture info if cached; otherwise null.</param>
	/// <returns><see langword="true"/> if the texture was cached.</returns>
	public static bool TryGetTexture(string path, out ImGuiAppTextureInfo? textureInfo) =>
		TryGetTexture(path.As<AbsoluteFilePath>(), out textureInfo);

	/// <summary>
	/// Executes an action with temporary access to the image's RGBA bytes, using a pooled buffer that is
	/// returned afterwards. The buffer may be larger than the image; only the first width*height*4 bytes
	/// are pixel data.
	/// </summary>
	/// <param name="image">The image to read.</param>
	/// <param name="action">The action to run with the pooled byte buffer.</param>
	public static void UseImageBytes(Image<Rgba32> image, Action<byte[]> action)
	{
		Ensure.NotNull(image);
		Ensure.NotNull(action);

		int bufferSize = image.Width * image.Height * Unsafe.SizeOf<Rgba32>();
		byte[] pooledBuffer = bytePool.Rent(bufferSize);
		try
		{
			image.CopyPixelDataTo(pooledBuffer.AsSpan(0, bufferSize));
			action(pooledBuffer);
		}
		finally
		{
			bytePool.Return(pooledBuffer);
		}
	}

	/// <summary>Uploads RGBA pixel data to the GPU via the Metal backend and returns the texture handle.</summary>
	/// <param name="bytes">The RGBA pixel buffer (may be over-sized; only width*height*4 bytes are read).</param>
	/// <param name="width">Texture width in pixels.</param>
	/// <param name="height">Texture height in pixels.</param>
	/// <returns>The GPU texture handle.</returns>
	internal static nint UploadTextureRGBA(byte[] bytes, int width, int height) =>
		Invoker.Invoke(() =>
		{
			if (Renderer is null)
			{
				throw new InvalidOperationException("Renderer backend is not initialized.");
			}

			int pixelByteCount = width * height * 4;
			return Renderer.CreateTexture(bytes.AsSpan(0, pixelByteCount), width, height);
		});

	/// <summary>Deletes a texture from the GPU and drops it from the cache.</summary>
	/// <param name="textureId">The GPU handle from <see cref="ImGuiAppTextureInfo.TextureId"/>.</param>
	/// <exception cref="InvalidOperationException">Thrown if the renderer backend is not initialized.</exception>
	public static void DeleteTexture(nint textureId) =>
		Invoker.Invoke(() =>
		{
			if (Renderer is null)
			{
				throw new InvalidOperationException("Renderer backend is not initialized.");
			}

			Renderer.DeleteTexture(textureId);
			foreach (KeyValuePair<AbsoluteFilePath, ImGuiAppTextureInfo> entry in Textures.Where(x => x.Value.TextureId == textureId).ToList())
			{
				Textures.Remove(entry.Key, out _);
			}
		});

	/// <summary>Deletes a texture from the GPU and drops it from the cache.</summary>
	/// <param name="textureInfo">The texture to delete.</param>
	public static void DeleteTexture(ImGuiAppTextureInfo textureInfo) =>
		DeleteTexture(Ensure.NotNull(textureInfo).TextureId);

	/// <summary>Deletes every cached texture from the GPU and clears the cache.</summary>
	public static void CleanupAllTextures()
	{
		if (Renderer is null)
		{
			return;
		}

		foreach (AbsoluteFilePath path in Textures.Keys.ToList())
		{
			if (Textures.TryGetValue(path, out ImGuiAppTextureInfo? info))
			{
				DeleteTexture(info.TextureId);
			}
		}

		Textures.Clear();
	}
}

#endif
