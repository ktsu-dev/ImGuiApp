// Adapted from https://github.com/dotnet/Silk.NET/blob/main/src/OpenGL/Extensions/Silk.NET.OpenGL.Extensions.ImGui/Texture.cs
// License: MIT

namespace ktsu.ImGuiApp.ImGuiController;

using System;

using Silk.NET.OpenGL;

/// <summary>
/// Specifies the texture coordinate axes for texture wrapping.
/// </summary>
public enum TextureCoordinate
{
	/// <summary>
	/// No texture coordinate.
	/// </summary>
	None = 0,

	/// <summary>
	/// The S coordinate (corresponds to the x-axis in texture space).
	/// </summary>
	S = TextureParameterName.TextureWrapS,

	/// <summary>
	/// The T coordinate (corresponds to the y-axis in texture space).
	/// </summary>
	T = TextureParameterName.TextureWrapT,

	/// <summary>
	/// The R coordinate (corresponds to the z-axis in texture space).
	/// </summary>
	R = TextureParameterName.TextureWrapR
}

internal class Texture : IDisposable
{
	public const SizedInternalFormat Srgb8Alpha8 = (SizedInternalFormat)GLEnum.Srgb8Alpha8;
	public const SizedInternalFormat Rgb32F = (SizedInternalFormat)GLEnum.Rgb32f;

	public const GLEnum MaxTextureMaxAnisotropy = (GLEnum)0x84FF;

	public static float? MaxAniso;
	private readonly GL _gl;
	public readonly string? Name;
	public readonly uint GlTexture;
	public readonly uint Width, Height;
	public readonly uint MipmapLevels;
	public readonly SizedInternalFormat InternalFormat;

	public unsafe Texture(GL gl, int width, int height, IntPtr data, bool generateMipmaps = false, bool srgb = false)
	{
		_gl = gl;
		MaxAniso ??= gl.GetFloat(MaxTextureMaxAnisotropy);
		Width = (uint)width;
		Height = (uint)height;
		InternalFormat = srgb ? Srgb8Alpha8 : SizedInternalFormat.Rgba8;
		MipmapLevels = (uint)(!generateMipmaps ? 1 : (int)Math.Floor(Math.Log(Math.Max(Width, Height), 2)));

		GlTexture = _gl.GenTexture();
		Bind();

		var pxFormat = PixelFormat.Bgra;

		_gl.TexStorage2D(GLEnum.Texture2D, MipmapLevels, InternalFormat, Width, Height);
		_gl.TexSubImage2D(GLEnum.Texture2D, 0, 0, 0, Width, Height, pxFormat, PixelType.UnsignedByte, (void*)data);

		if (generateMipmaps)
		{
			_gl.GenerateTextureMipmap(GlTexture);
		}

		SetWrap(TextureCoordinate.S, TextureWrapMode.Repeat);
		SetWrap(TextureCoordinate.T, TextureWrapMode.Repeat);

		uint mip = MipmapLevels - 1;
		_gl.TexParameterI(GLEnum.Texture2D, TextureParameterName.TextureMaxLevel, ref mip);
	}

	public void Bind() => _gl.BindTexture(GLEnum.Texture2D, GlTexture);

	public void SetMinFilter(TextureMinFilter filter)
	{
		int intFilter = (int)filter;
		_gl.TexParameterI(GLEnum.Texture2D, TextureParameterName.TextureMinFilter, ref intFilter);
	}

	public void SetMagFilter(TextureMagFilter filter)
	{
		int intFilter = (int)filter;
		_gl.TexParameterI(GLEnum.Texture2D, TextureParameterName.TextureMagFilter, ref intFilter);
	}

	public void SetAnisotropy(float level)
	{
		const TextureParameterName textureMaxAnisotropy = (TextureParameterName)0x84FE;
		_gl.TexParameter(GLEnum.Texture2D, (GLEnum)textureMaxAnisotropy, Util.Clamp(level, 1, MaxAniso.GetValueOrDefault()));
	}

	public void SetLod(int @base, int min, int max)
	{
		_gl.TexParameterI(GLEnum.Texture2D, TextureParameterName.TextureLodBias, ref @base);
		_gl.TexParameterI(GLEnum.Texture2D, TextureParameterName.TextureMinLod, ref min);
		_gl.TexParameterI(GLEnum.Texture2D, TextureParameterName.TextureMaxLod, ref max);
	}

	public void SetWrap(TextureCoordinate coord, TextureWrapMode mode)
	{
		int intMode = (int)mode;
		_gl.TexParameterI(GLEnum.Texture2D, (TextureParameterName)coord, ref intMode);
	}

	public void Dispose() => _gl.DeleteTexture(GlTexture);
}
