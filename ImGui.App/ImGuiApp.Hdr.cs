// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.InteropServices;
using Hexa.NET.ImGui;
using ktsu.ImGui.App.ImGuiController.Hdr;

/// <summary>
/// HDR presentation and half-float textures for <see cref="ImGuiApp"/>.
/// </summary>
public static partial class ImGuiApp
{
	/// <summary>The colour every frame starts from, encoded.</summary>
	private static readonly Vector4 FrameClearColor = new(.45f, .55f, .60f, 1f);

	/// <summary>The window's HDR presentation, or null while it presents in SDR.</summary>
	internal static HdrPresentation? hdrPresentation;

	/// <summary>
	/// Gets what the screen the window is on can show. An SDR screen, or a window that doesn't present
	/// in HDR, reports <see cref="HdrDisplayInfo.Sdr"/>.
	/// </summary>
	/// <remarks>
	/// It is asked again every second and whenever the window moves, so it follows the window to
	/// another screen and follows HDR being turned on or off in the system's settings. A texture that
	/// depends on it should be remade when it changes.
	/// </remarks>
	public static HdrDisplayInfo HdrDisplay { get; internal set; } = HdrDisplayInfo.Sdr;

	/// <summary>Gets whether the window presents through the platform's HDR presentation, whatever the screen is showing now.</summary>
	public static bool IsHdrOutputActive => hdrPresentation is not null;

	/// <summary>
	/// Creates a GPU texture from half-float pixels encoded by <see cref="HdrEncoding"/>, which can hold
	/// values above SDR white.
	/// </summary>
	/// <param name="rgba">Tightly packed pixels, four values each, <paramref name="width"/> * <paramref name="height"/> * 4 in all.</param>
	/// <param name="width">Texture width in pixels.</param>
	/// <param name="height">Texture height in pixels.</param>
	/// <returns>Texture info owned by the caller, with the same lifetime rules as <see cref="CreateTexture(ReadOnlySpan{byte}, int, int)"/>.</returns>
	/// <remarks>
	/// Values above 1 are shown brighter than white while <see cref="HdrDisplay"/> reports an HDR screen,
	/// up to its <see cref="HdrDisplayInfo.Headroom"/>, and as white otherwise. A renderer without
	/// half-float textures gets the pixels converted to RGBA8, which shows them the same way an SDR
	/// screen does.
	/// </remarks>
	/// <exception cref="ArgumentException">The span length does not equal width * height * 4.</exception>
	public static ImGuiAppTextureInfo CreateTexture(ReadOnlySpan<Half> rgba, int width, int height)
	{
		ValidateHalfPixelSpan(rgba, width, height);
		Half[] pixels = rgba.ToArray();
		return Invoker.Invoke(() =>
		{
			if (renderer is IHalfFloatTextures halfFloat)
			{
				ImGuiAppTextureInfo info = new() { Width = width, Height = height };
				SetTexture(info, halfFloat.CreateTexture(pixels, width, height));
				return info;
			}

			return CreateTextureFromBuffer(ToRgba8(pixels), width, height);
		});
	}

	/// <summary>
	/// Replaces the contents of a texture made by <see cref="CreateTexture(ReadOnlySpan{Half}, int, int)"/>.
	/// </summary>
	/// <param name="textureInfo">The texture to update.</param>
	/// <param name="rgba">Tightly packed pixels, four values each, <paramref name="width"/> * <paramref name="height"/> * 4 in all.</param>
	/// <param name="width">Texture width in pixels.</param>
	/// <param name="height">Texture height in pixels.</param>
	/// <remarks>
	/// Safe to call from any thread, like <see cref="UpdateTexture(ImGuiAppTextureInfo, ReadOnlySpan{byte}, int, int)"/>.
	/// A size change remakes the texture and gives <paramref name="textureInfo"/> the new handle.
	/// </remarks>
	/// <exception cref="ArgumentNullException"><paramref name="textureInfo"/> is null.</exception>
	/// <exception cref="ArgumentException">The span length does not equal width * height * 4.</exception>
	public static void UpdateTexture(ImGuiAppTextureInfo textureInfo, ReadOnlySpan<Half> rgba, int width, int height)
	{
		Ensure.NotNull(textureInfo);
		ValidateHalfPixelSpan(rgba, width, height);
		Half[] pixels = rgba.ToArray();
		Invoker.Invoke(() =>
		{
			bool sameSize = textureInfo.Width == width && textureInfo.Height == height;
			if (renderer is IHalfFloatTextures halfFloat)
			{
				if (sameSize && halfFloat.UpdateTexture(textureInfo.TextureId, pixels, width, height))
				{
					return;
				}

				renderer.DeleteTexture(textureInfo.TextureId);
				SetTexture(textureInfo, halfFloat.CreateTexture(pixels, width, height));
				textureInfo.Width = width;
				textureInfo.Height = height;
				return;
			}

			UpdateTextureFromBuffer(textureInfo, ToRgba8(pixels), width, height, tryInPlace: true);
		});
	}

	/// <summary>
	/// Sets up HDR presentation when the configuration asks for it and the platform can give it, and
	/// stops the window swapping its own OpenGL buffers, since the presenter shows each frame instead.
	/// </summary>
	/// <param name="config">The configuration.</param>
	[ExcludeFromCodeCoverage(Justification = "Needs a live window and OpenGL context; what it sets up is HdrPresentation, which is tested.")]
	[SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "The compositor is owned by the presentation it is handed to, which CleanupHdrOutput disposes.")]
	internal static void SetupHdrOutput(ImGuiAppConfig config)
	{
		if (!config.HdrOutput || gl is null || window is null)
		{
			return;
		}

		IHdrPresenter? presenter = null;
		try
		{
			presenter = CreateHdrPresenter(gl, window, out string reason);
			if (presenter is null)
			{
				DebugLogger.Log($"HDR output: presenting in SDR. {reason}");
				return;
			}

			string presenterName = presenter.GetType().Name;
			HdrCompositor compositor = new(gl, presenter);
			presenter = null;
			hdrPresentation = new HdrPresentation(compositor, TimeProvider.System);
			window.ShouldSwapAutomatically = false;
			RefreshHdrDisplay();
			DebugLogger.Log($"HDR output: presenting through {presenterName}, screen {HdrDisplay}");
		}
		catch (Exception exception) when (exception is DllNotFoundException or EntryPointNotFoundException or InvalidOperationException or MarshalDirectiveException)
		{
			DebugLogger.Log($"HDR output: presenting in SDR. {exception.Message}");
		}
		finally
		{
			presenter?.Dispose();
		}
	}

	[ExcludeFromCodeCoverage(Justification = "Creates a platform presenter over a live window.")]
	private static IHdrPresenter? CreateHdrPresenter(Silk.NET.OpenGL.GL context, Silk.NET.Windowing.IWindow host, out string reason)
	{
		Silk.NET.Maths.Vector2D<int> size = host.FramebufferSize;
		if (OperatingSystem.IsWindows() && host.Native?.Win32 is { } win32)
		{
			return DxgiHdrPresenter.TryCreate(context, win32.Hwnd, size.X, size.Y, out reason);
		}

		if (OperatingSystem.IsMacOS() && host.Native?.Cocoa is nint nsWindow && nsWindow != 0)
		{
			return MetalEdrPresenter.TryCreate(context, nsWindow, out reason);
		}

		reason = "HDR presentation isn't available on this platform.";
		return null;
	}

	/// <summary>Asks the screen again what it shows, at most once a second unless forced.</summary>
	/// <param name="force">Whether to ask now whatever the interval, as when the window moves.</param>
	internal static void RefreshHdrDisplay(bool force = true)
	{
		if (hdrPresentation is not null)
		{
			HdrDisplay = hdrPresentation.Refresh(force);
		}
	}

	/// <summary>Points drawing at the half-float frame when presenting in HDR, and clears whichever target a frame draws into.</summary>
	[ExcludeFromCodeCoverage(Justification = "Draws into a live window's OpenGL context; the HDR side is HdrPresentation.BeginFrame, which is tested.")]
	internal static void BeginPresentedFrame()
	{
		if (hdrPresentation is not null && window is not null)
		{
			Silk.NET.Maths.Vector2D<int> size = window.FramebufferSize;
			hdrPresentation.BeginFrame(size.X, size.Y, FrameClearColor);
			HdrDisplay = hdrPresentation.Display;
			return;
		}

		gl?.ClearColor(FrameClearColor.X, FrameClearColor.Y, FrameClearColor.Z, FrameClearColor.W);
		gl?.Clear(Silk.NET.OpenGL.ClearBufferMask.ColorBufferBit);
	}

	/// <summary>
	/// Presents the frame through the HDR presenter, when there is one; otherwise the window swaps its
	/// own buffers. A presenter that keeps failing is given up, and the window goes back to SDR.
	/// </summary>
	internal static void EndPresentedFrame()
	{
		if (hdrPresentation is null || hdrPresentation.EndFrame())
		{
			return;
		}

		DebugLogger.Log("HDR output: the presenter stopped taking frames; presenting in SDR.");
		CleanupHdrOutput();
		window?.ShouldSwapAutomatically = true;
	}

	/// <summary>Releases the HDR presentation, before the OpenGL context it draws with.</summary>
	internal static void CleanupHdrOutput()
	{
		hdrPresentation?.Dispose();
		hdrPresentation = null;
		HdrDisplay = HdrDisplayInfo.Sdr;
	}

	[SuppressMessage("Major Code Smell", "S6640:Make sure that using \"unsafe\" is safe here", Justification = "ImTextureRef is built from the handle alone; no pointer is dereferenced or retained.")]
	private static unsafe void SetTexture(ImGuiAppTextureInfo info, nint textureId)
	{
		info.TextureId = textureId;
		info.TextureRef = new ImTextureRef(default, textureId);
	}

	private static byte[] ToRgba8(Half[] pixels)
	{
		byte[] rgba = new byte[pixels.Length];
		HdrEncoding.ToRgba8(pixels, rgba);
		return rgba;
	}

	private static void ValidateHalfPixelSpan(ReadOnlySpan<Half> rgba, int width, int height)
	{
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
		if (rgba.Length != (long)width * height * 4)
		{
			throw new ArgumentException($"Expected {(long)width * height * 4} values for a {width}x{height} texture, got {rgba.Length}.", nameof(rgba));
		}
	}
}
