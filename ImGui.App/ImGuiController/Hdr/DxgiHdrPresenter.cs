// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.ImGuiController.Hdr;

using System;
using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using Silk.NET.OpenGL;

/// <summary>
/// Presents an OpenGL window in HDR on Windows: a Direct3D 11 flip swap chain in scRGB on the
/// window, fed from OpenGL through <c>WGL_NV_DX_interop2</c>.
/// </summary>
/// <remarks>
/// <para>
/// OpenGL on Windows can't ask for an HDR surface, so the frame goes to Direct3D: OpenGL draws it
/// into a Direct3D texture it shares through the interop extension, which is copied to the swap
/// chain's back buffer and presented. The swap chain is half-float in scRGB, where 1 is 80 nits and
/// the colours are linear in Rec.709 primaries, so wider ones are negative; that is the one HDR
/// format DWM composes without a conversion of its own. The same arrangement serves mpv's
/// <c>dxinterop</c> backend.
/// </para>
/// <para>
/// NVIDIA, AMD and Intel drivers all expose the extension. Where it is missing, or Direct3D and
/// OpenGL are on different adapters (a laptop drawing OpenGL on its discrete GPU), creating this
/// fails and the window presents in SDR as before.
/// </para>
/// <para>
/// COM is called through each interface's function table by index, which keeps Silk.NET's 12 MB of
/// Direct3D bindings out of every application. The indices were checked against those bindings.
/// </para>
/// </remarks>
[SupportedOSPlatform("windows")]
[ExcludeFromCodeCoverage(Justification = "Drives Direct3D, DXGI and the WGL interop extension, which only a Windows desktop with a GPU has.")]
[SuppressMessage("Major Code Smell", "S6640:Make sure that using \"unsafe\" is safe here", Justification = "COM is called through function tables by index; every pointer is a COM interface this class holds a reference to, or a local it passes for the call's duration.")]
internal sealed unsafe partial class DxgiHdrPresenter : IHdrPresenter
{
	private const int DriverTypeHardware = 1;
	private const uint CreateDeviceBgraSupport = 0x20;
	private const uint SdkVersion = 7;
	private const uint FormatR16G16B16A16Float = 10;
	private const uint UsageRenderTargetOutput = 0x20;
	private const uint SwapEffectFlipDiscard = 4;
	private const uint ColorSpaceScRgb = 1;
	private const uint ColorSpaceHdr10 = 12;
	private const uint ColorSpaceSupportPresent = 1;
	private const uint BindShaderResource = 0x8;
	private const uint BindRenderTarget = 0x20;
	private const uint WindowAssociationNoAltEnter = 2;
	private const uint GlTexture2D = 0x0DE1;
	private const uint AccessWriteDiscard = 2;

	private static readonly Guid DxgiDevice = new("54ec77fa-1377-44e6-8c32-88fd5f44c84c");
	private static readonly Guid DxgiFactory2 = new("50c83a1c-e072-4c48-87b0-3630fa36a6d0");
	private static readonly Guid DxgiSwapChain3 = new("94d99bdb-f1f8-4ab0-b236-7da0170edab1");
	private static readonly Guid DxgiOutput6 = new("068346e8-aaec-4b84-add7-137f513f77a1");
	private static readonly Guid D3D11Texture2D = new("6f15aaf2-d208-4e89-9ab4-489535d34f9c");

	private readonly GL gl;
	private readonly InteropFunctions interop;
	private readonly nint device;
	private readonly nint context;
	private readonly nint swapChain;
	private readonly nint interopDevice;
	private readonly uint framebuffer;
	private nint sharedTexture;
	private nint interopObject;
	private uint glTexture;
	private int width;
	private int height;
	private bool locked;
	private bool disposed;

	private DxgiHdrPresenter(GL gl, InteropFunctions interop, nint device, nint context, nint swapChain, nint interopDevice, int width, int height)
	{
		this.gl = gl;
		this.interop = interop;
		this.device = device;
		this.context = context;
		this.swapChain = swapChain;
		this.interopDevice = interopDevice;
		this.width = width;
		this.height = height;
		framebuffer = gl.GenFramebuffer();
	}

	/// <inheritdoc />
	public bool FlipsVertically => true;

	/// <summary>Sets up HDR presentation on a window, or says why it can't.</summary>
	/// <param name="gl">The window's OpenGL context, current on this thread.</param>
	/// <param name="window">The window's handle.</param>
	/// <param name="width">The window's framebuffer width.</param>
	/// <param name="height">The window's framebuffer height.</param>
	/// <param name="reason">Why it couldn't be set up, when it couldn't.</param>
	/// <returns>The presenter, or null.</returns>
	public static DxgiHdrPresenter? TryCreate(GL gl, nint window, int width, int height, out string reason)
	{
		if (!InteropFunctions.TryLoad(out InteropFunctions interop))
		{
			reason = "The OpenGL driver doesn't offer WGL_NV_DX_interop2.";
			return null;
		}

		nint device = 0;
		nint context = 0;
		nint swapChain = 0;
		try
		{
			if (D3D11CreateDevice(0, DriverTypeHardware, 0, CreateDeviceBgraSupport, 0, 0, SdkVersion, out device, out _, out context) < 0)
			{
				reason = "Direct3D 11 couldn't create a device.";
				return null;
			}

			swapChain = CreateSwapChain(device, window, width, height);
			if (swapChain == 0)
			{
				reason = "DXGI couldn't make a half-float swap chain for the window.";
				return null;
			}

			uint support = 0;
			if (Call<uint, nint, int>(swapChain, 37, ColorSpaceScRgb, (nint)(&support)) < 0 || (support & ColorSpaceSupportPresent) == 0
				|| Call<uint, int>(swapChain, 38, ColorSpaceScRgb) < 0)
			{
				reason = "The swap chain can't present scRGB.";
				return null;
			}

			nint interopDevice = interop.OpenDevice(device);
			if (interopDevice == 0)
			{
				reason = "OpenGL couldn't open the Direct3D device; they may be on different adapters.";
				return null;
			}

			DxgiHdrPresenter presenter = new(gl, interop, device, context, swapChain, interopDevice, width, height);
			device = context = swapChain = 0;
			reason = string.Empty;
			return presenter;
		}
		finally
		{
			Release(swapChain);
			Release(context);
			Release(device);
		}
	}

	/// <inheritdoc />
	public HdrDisplayInfo QueryDisplay()
	{
		nint output = 0;
		nint output6 = 0;
		try
		{
			if (Call<nint, int>(swapChain, 15, (nint)(&output)) < 0 || QueryInterface(output, DxgiOutput6, out output6) < 0)
			{
				return HdrDisplayInfo.Sdr;
			}

			byte* description = stackalloc byte[160];
			if (Call<nint, int>(output6, 27, (nint)description) < 0)
			{
				return HdrDisplayInfo.Sdr;
			}

			ReadOnlySpan<byte> desc = new(description, 152);
			if (BinaryPrimitives.ReadUInt32LittleEndian(desc[100..]) != ColorSpaceHdr10)
			{
				return HdrDisplayInfo.Sdr;
			}

			string deviceName = Encoding.Unicode.GetString(desc[..64]).TrimEnd('\0');
			double peak = BinaryPrimitives.ReadSingleLittleEndian(desc[140..]);
			double white = WindowsDisplayConfig.SdrWhiteNits(deviceName) ?? HdrDisplayInfo.ReferenceWhiteNits;
			return HdrDisplayInfo.FromLuminance(white, peak);
		}
		finally
		{
			Release(output6);
			Release(output);
		}
	}

	/// <inheritdoc />
	public (float Scale, float Limit) OutputRange(HdrDisplayInfo display)
	{
		Ensure.NotNull(display);
		return display.IsHdr
			? ((float)(display.SdrWhiteNits / HdrDisplayInfo.ReferenceWhiteNits), (float)(display.PeakNits / HdrDisplayInfo.ReferenceWhiteNits))
			: (1f, 1f);
	}

	/// <inheritdoc />
	public bool BeginOutput(int width, int height, out uint framebuffer)
	{
		framebuffer = this.framebuffer;
		if (!EnsureTarget(width, height))
		{
			return false;
		}

		nint lockedObject = interopObject;
		locked = interop.LockObjects(interopDevice, 1, &lockedObject) != 0;
		return locked;
	}

	/// <inheritdoc />
	public void EndOutput()
	{
		if (!locked)
		{
			return;
		}

		gl.Flush();
		nint lockedObject = interopObject;
		interop.UnlockObjects(interopDevice, 1, &lockedObject);
		locked = false;

		nint backBuffer = 0;
		Guid texture = D3D11Texture2D;
		if (Call<uint, nint, nint, int>(swapChain, 9, 0, (nint)(&texture), (nint)(&backBuffer)) >= 0)
		{
			Call<nint, nint>(context, 47, backBuffer, sharedTexture);
			Release(backBuffer);
			Call<uint, uint, int>(swapChain, 8, 0, 0);
		}
	}

	/// <inheritdoc />
	public void Dispose()
	{
		if (disposed)
		{
			return;
		}

		disposed = true;
		ReleaseTarget();
		gl.DeleteFramebuffer(framebuffer);
		interop.CloseDevice(interopDevice);
		Release(swapChain);
		Release(context);
		Release(device);
	}

	private static nint CreateSwapChain(nint device, nint window, int width, int height)
	{
		nint dxgiDevice = 0;
		nint adapter = 0;
		nint factory = 0;
		nint swapChain1 = 0;
		try
		{
			Guid factoryId = DxgiFactory2;
			if (QueryInterface(device, DxgiDevice, out dxgiDevice) < 0
				|| Call<nint, int>(dxgiDevice, 7, (nint)(&adapter)) < 0
				|| Call<nint, nint, int>(adapter, 6, (nint)(&factoryId), (nint)(&factory)) < 0)
			{
				return 0;
			}

			SwapChainDescription description = new()
			{
				Width = (uint)Math.Max(1, width),
				Height = (uint)Math.Max(1, height),
				Format = FormatR16G16B16A16Float,
				SampleCount = 1,
				BufferUsage = UsageRenderTargetOutput,
				BufferCount = 2,
				SwapEffect = SwapEffectFlipDiscard,
			};

			if (((delegate* unmanaged[Stdcall]<nint, nint, nint, SwapChainDescription*, nint, nint, nint*, int>)Table(factory)[15])(factory, device, window, &description, 0, 0, &swapChain1) < 0)
			{
				return 0;
			}

			Call<nint, uint, int>(factory, 8, window, WindowAssociationNoAltEnter);
			return QueryInterface(swapChain1, DxgiSwapChain3, out nint swapChain3) < 0 ? 0 : swapChain3;
		}
		finally
		{
			Release(swapChain1);
			Release(factory);
			Release(adapter);
			Release(dxgiDevice);
		}
	}

	private bool EnsureTarget(int frameWidth, int frameHeight)
	{
		frameWidth = Math.Max(1, frameWidth);
		frameHeight = Math.Max(1, frameHeight);
		if (sharedTexture != 0 && frameWidth == width && frameHeight == height)
		{
			return true;
		}

		ReleaseTarget();
		if ((frameWidth != width || frameHeight != height)
			&& Call<uint, uint, uint, uint, uint, int>(swapChain, 13, 0, (uint)frameWidth, (uint)frameHeight, 0, 0) < 0)
		{
			return false;
		}

		width = frameWidth;
		height = frameHeight;
		TextureDescription description = new()
		{
			Width = (uint)width,
			Height = (uint)height,
			MipLevels = 1,
			ArraySize = 1,
			Format = FormatR16G16B16A16Float,
			SampleCount = 1,
			BindFlags = BindRenderTarget | BindShaderResource,
		};

		nint texture = 0;
		if (((delegate* unmanaged[Stdcall]<nint, TextureDescription*, nint, nint*, int>)Table(device)[5])(device, &description, 0, &texture) < 0)
		{
			return false;
		}

		sharedTexture = texture;
		glTexture = gl.GenTexture();
		interopObject = interop.RegisterObject(interopDevice, sharedTexture, glTexture, GlTexture2D, AccessWriteDiscard);
		if (interopObject == 0)
		{
			ReleaseTarget();
			return false;
		}

		gl.BindFramebuffer(FramebufferTarget.Framebuffer, framebuffer);
		gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, glTexture, 0);
		gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
		return true;
	}

	private void ReleaseTarget()
	{
		if (interopObject != 0)
		{
			interop.UnregisterObject(interopDevice, interopObject);
			interopObject = 0;
		}

		if (glTexture != 0)
		{
			gl.DeleteTexture(glTexture);
			glTexture = 0;
		}

		Release(sharedTexture);
		sharedTexture = 0;
	}

	private static nint* Table(nint instance) => *(nint**)instance;

	private static int QueryInterface(nint instance, Guid id, out nint result)
	{
		nint found = 0;
		int status = instance == 0 ? -1 : ((delegate* unmanaged[Stdcall]<nint, Guid*, nint*, int>)Table(instance)[0])(instance, &id, &found);
		result = found;
		return status;
	}

	private static void Release(nint instance)
	{
		if (instance != 0)
		{
			((delegate* unmanaged[Stdcall]<nint, uint>)Table(instance)[2])(instance);
		}
	}

	private static TResult Call<TArg, TResult>(nint instance, int slot, TArg arg)
		where TArg : unmanaged
		where TResult : unmanaged =>
		((delegate* unmanaged[Stdcall]<nint, TArg, TResult>)Table(instance)[slot])(instance, arg);

	private static void Call<TArg1, TArg2>(nint instance, int slot, TArg1 arg1, TArg2 arg2)
		where TArg1 : unmanaged
		where TArg2 : unmanaged =>
		((delegate* unmanaged[Stdcall]<nint, TArg1, TArg2, void>)Table(instance)[slot])(instance, arg1, arg2);

	private static TResult Call<TArg1, TArg2, TResult>(nint instance, int slot, TArg1 arg1, TArg2 arg2)
		where TArg1 : unmanaged
		where TArg2 : unmanaged
		where TResult : unmanaged =>
		((delegate* unmanaged[Stdcall]<nint, TArg1, TArg2, TResult>)Table(instance)[slot])(instance, arg1, arg2);

	private static TResult Call<TArg1, TArg2, TArg3, TResult>(nint instance, int slot, TArg1 arg1, TArg2 arg2, TArg3 arg3)
		where TArg1 : unmanaged
		where TArg2 : unmanaged
		where TArg3 : unmanaged
		where TResult : unmanaged =>
		((delegate* unmanaged[Stdcall]<nint, TArg1, TArg2, TArg3, TResult>)Table(instance)[slot])(instance, arg1, arg2, arg3);

	private static TResult Call<TArg1, TArg2, TArg3, TArg4, TArg5, TResult>(nint instance, int slot, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5)
		where TArg1 : unmanaged
		where TArg2 : unmanaged
		where TArg3 : unmanaged
		where TArg4 : unmanaged
		where TArg5 : unmanaged
		where TResult : unmanaged =>
		((delegate* unmanaged[Stdcall]<nint, TArg1, TArg2, TArg3, TArg4, TArg5, TResult>)Table(instance)[slot])(instance, arg1, arg2, arg3, arg4, arg5);

	[LibraryImport("d3d11.dll")]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	private static partial int D3D11CreateDevice(nint adapter, int driverType, nint software, uint flags, nint featureLevels, uint featureLevelCount, uint sdkVersion, out nint device, out int featureLevel, out nint immediateContext);

	[LibraryImport("opengl32.dll", StringMarshalling = StringMarshalling.Utf8)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	private static partial nint wglGetProcAddress(string name);

	/// <summary>DXGI_SWAP_CHAIN_DESC1.</summary>
	[StructLayout(LayoutKind.Sequential)]
	private struct SwapChainDescription
	{
		public uint Width;
		public uint Height;
		public uint Format;
		public int Stereo;
		public uint SampleCount;
		public uint SampleQuality;
		public uint BufferUsage;
		public uint BufferCount;
		public uint Scaling;
		public uint SwapEffect;
		public uint AlphaMode;
		public uint Flags;
	}

	/// <summary>D3D11_TEXTURE2D_DESC.</summary>
	[StructLayout(LayoutKind.Sequential)]
	private struct TextureDescription
	{
		public uint Width;
		public uint Height;
		public uint MipLevels;
		public uint ArraySize;
		public uint Format;
		public uint SampleCount;
		public uint SampleQuality;
		public uint Usage;
		public uint BindFlags;
		public uint CpuAccessFlags;
		public uint MiscFlags;
	}

	/// <summary>The <c>WGL_NV_DX_interop</c> entry points, which the driver hands out only while its context is current.</summary>
	private readonly struct InteropFunctions
	{
		private readonly delegate* unmanaged[Stdcall]<nint, nint> openDevice;
		private readonly delegate* unmanaged[Stdcall]<nint, int> closeDevice;
		private readonly delegate* unmanaged[Stdcall]<nint, nint, uint, uint, uint, nint> registerObject;
		private readonly delegate* unmanaged[Stdcall]<nint, nint, int> unregisterObject;
		private readonly delegate* unmanaged[Stdcall]<nint, int, nint*, int> lockObjects;
		private readonly delegate* unmanaged[Stdcall]<nint, int, nint*, int> unlockObjects;

		private InteropFunctions(nint open, nint close, nint register, nint unregister, nint lockAll, nint unlockAll)
		{
			openDevice = (delegate* unmanaged[Stdcall]<nint, nint>)open;
			closeDevice = (delegate* unmanaged[Stdcall]<nint, int>)close;
			registerObject = (delegate* unmanaged[Stdcall]<nint, nint, uint, uint, uint, nint>)register;
			unregisterObject = (delegate* unmanaged[Stdcall]<nint, nint, int>)unregister;
			lockObjects = (delegate* unmanaged[Stdcall]<nint, int, nint*, int>)lockAll;
			unlockObjects = (delegate* unmanaged[Stdcall]<nint, int, nint*, int>)unlockAll;
		}

		public static bool TryLoad(out InteropFunctions functions)
		{
			nint open = wglGetProcAddress("wglDXOpenDeviceNV");
			nint close = wglGetProcAddress("wglDXCloseDeviceNV");
			nint register = wglGetProcAddress("wglDXRegisterObjectNV");
			nint unregister = wglGetProcAddress("wglDXUnregisterObjectNV");
			nint lockAll = wglGetProcAddress("wglDXLockObjectsNV");
			nint unlockAll = wglGetProcAddress("wglDXUnlockObjectsNV");
			functions = new(open, close, register, unregister, lockAll, unlockAll);
			return open > 3 && close > 3 && register > 3 && unregister > 3 && lockAll > 3 && unlockAll > 3;
		}

		public nint OpenDevice(nint direct3DDevice) => openDevice(direct3DDevice);

		public void CloseDevice(nint interopDevice) => closeDevice(interopDevice);

		public nint RegisterObject(nint interopDevice, nint resource, uint name, uint type, uint access) =>
			registerObject(interopDevice, resource, name, type, access);

		public void UnregisterObject(nint interopDevice, nint registered) => unregisterObject(interopDevice, registered);

		public int LockObjects(nint interopDevice, int count, nint* objects) => lockObjects(interopDevice, count, objects);

		public int UnlockObjects(nint interopDevice, int count, nint* objects) => unlockObjects(interopDevice, count, objects);
	}
}
