// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.ImGuiController.Hdr;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Silk.NET.OpenGL;

/// <summary>
/// Presents an OpenGL window in HDR on macOS: an extended dynamic range <c>CAMetalLayer</c> over the
/// window's content, fed from OpenGL through a shared IOSurface.
/// </summary>
/// <remarks>
/// <para>
/// macOS shows values above 1 only from a layer that asks for extended dynamic range, and the OpenGL
/// surface SDL makes can't. So OpenGL draws the finished frame into a half-float IOSurface, which a
/// Metal texture over the same memory copies into the layer's drawable. The layer is half-float in
/// extended linear sRGB, where 1 is the screen's SDR white and the screen goes as far above it as its
/// current headroom allows, so nothing is scaled; values are only limited to the headroom.
/// </para>
/// <para>
/// The layer sits over the window's content view as a sublayer, so it covers what OpenGL would have
/// presented and input still reaches the view beneath it. Its size follows the view through its
/// autoresizing mask; the drawable is resized with the frame.
/// </para>
/// <para>
/// Everything goes through the Objective-C runtime by selector, since the desktop build carries no
/// macOS bindings. Any step that fails leaves the window presenting in SDR.
/// </para>
/// </remarks>
[SupportedOSPlatform("macos")]
[ExcludeFromCodeCoverage(Justification = "Drives Metal, Core Animation and IOSurface through the Objective-C runtime, which only a Mac has.")]
internal sealed partial class MetalEdrPresenter : IHdrPresenter
{
	private const string ObjC = "/usr/lib/libobjc.A.dylib";
	private const string MetalFramework = "/System/Library/Frameworks/Metal.framework/Metal";
	private const string CoreGraphics = "/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics";
	private const string CoreFoundation = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";
	private const string IOSurfaceFramework = "/System/Library/Frameworks/IOSurface.framework/IOSurface";
	private const string OpenGLFramework = "/System/Library/Frameworks/OpenGL.framework/OpenGL";

	private const ulong PixelFormatRgba16Float = 115;
	private const ulong AutoresizeWidthAndHeight = 2 | 16;
	private const uint GlTextureRectangle = 0x84F5;
	private const int GlRgba16F = 0x881A;
	private const uint GlRgba = 0x1908;
	private const uint GlHalfFloat = 0x140B;
	private const int HalfFloatRgbaFourCC = 0x52476841;

	private readonly GL gl;
	private readonly nint window;
	private readonly nint device;
	private readonly nint queue;
	private readonly nint layer;
	private readonly uint framebuffer;
	private nint surface;
	private nint metalTexture;
	private uint glTexture;
	private int width;
	private int height;
	private bool disposed;

	private MetalEdrPresenter(GL gl, nint window, nint device, nint queue, nint layer)
	{
		this.gl = gl;
		this.window = window;
		this.device = device;
		this.queue = queue;
		this.layer = layer;
		framebuffer = gl.GenFramebuffer();
	}

	/// <inheritdoc />
	public bool FlipsVertically => true;

	/// <summary>Sets up HDR presentation on a window, or says why it can't.</summary>
	/// <param name="gl">The window's OpenGL context, current on this thread.</param>
	/// <param name="nsWindow">The window's <c>NSWindow</c>.</param>
	/// <param name="reason">Why it couldn't be set up, when it couldn't.</param>
	/// <returns>The presenter, or null.</returns>
	public static MetalEdrPresenter? TryCreate(GL gl, nint nsWindow, out string reason)
	{
		nint device = NativeMethods.MTLCreateSystemDefaultDevice();
		if (device == 0)
		{
			reason = "Metal has no device.";
			return null;
		}

		nint queue = Send(device, "newCommandQueue");
		nint layer = Send(Send(Class("CAMetalLayer"), "layer"), "retain");
		nint view = Send(nsWindow, "contentView");
		if (queue == 0 || layer == 0 || view == 0)
		{
			Release(layer);
			Release(queue);
			Release(device);
			reason = "Core Animation couldn't make a Metal layer for the window.";
			return null;
		}

		SendPointer(layer, "setDevice:", device);
		SendUnsigned(layer, "setPixelFormat:", PixelFormatRgba16Float);
		SendBool(layer, "setWantsExtendedDynamicRangeContent:", true);
		SendBool(layer, "setFramebufferOnly:", false);
		SendBool(layer, "setOpaque:", true);
		nint colorSpace = NativeMethods.CGColorSpaceCreateWithName(CoreGraphicsConstant("kCGColorSpaceExtendedLinearSRGB"));
		SendPointer(layer, "setColorspace:", colorSpace);
		NativeMethods.CGColorSpaceRelease(colorSpace);
		SendUnsigned(layer, "setAutoresizingMask:", AutoresizeWidthAndHeight);

		SendBool(view, "setWantsLayer:", true);
		nint root = Send(view, "layer");
		if (root == 0)
		{
			Release(layer);
			Release(queue);
			Release(device);
			reason = "The window's content view has no layer.";
			return null;
		}

		SendPointer(root, "addSublayer:", layer);
		reason = string.Empty;
		return new MetalEdrPresenter(gl, nsWindow, device, queue, layer);
	}

	/// <inheritdoc />
	public HdrDisplayInfo QueryDisplay()
	{
		nint screen = Send(window, "screen");
		if (screen == 0)
		{
			return HdrDisplayInfo.Sdr;
		}

		double potential = SendDouble(screen, "maximumPotentialExtendedDynamicRangeColorComponentValue");
		return potential > 1 ? HdrDisplayInfo.FromHeadroom(potential, 100) : HdrDisplayInfo.Sdr;
	}

	/// <inheritdoc />
	public (float Scale, float Limit) OutputRange(HdrDisplayInfo display)
	{
		Ensure.NotNull(display);
		return (1f, (float)display.Headroom);
	}

	/// <inheritdoc />
	public bool BeginOutput(int frameWidth, int frameHeight, out uint framebuffer)
	{
		framebuffer = this.framebuffer;
		return EnsureTarget(Math.Max(1, frameWidth), Math.Max(1, frameHeight));
	}

	/// <inheritdoc />
	public void EndOutput()
	{
		// IOSurface sharing across APIs has no fence here, so OpenGL finishes before Metal reads.
		gl.Finish();
		nint pool = NativeMethods.objc_autoreleasePoolPush();
		try
		{
			nint drawable = Send(layer, "nextDrawable");
			if (drawable == 0)
			{
				return;
			}

			nint commands = Send(queue, "commandBuffer");
			nint blit = Send(commands, "blitCommandEncoder");
			SendPointers(blit, "copyFromTexture:toTexture:", metalTexture, Send(drawable, "texture"));
			Send(blit, "endEncoding");
			SendPointer(commands, "presentDrawable:", drawable);
			Send(commands, "commit");
		}
		finally
		{
			NativeMethods.objc_autoreleasePoolPop(pool);
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
		Send(layer, "removeFromSuperlayer");
		Release(layer);
		Release(queue);
		Release(device);
	}

	private bool EnsureTarget(int frameWidth, int frameHeight)
	{
		if (metalTexture != 0 && frameWidth == width && frameHeight == height)
		{
			return true;
		}

		ReleaseTarget();
		width = frameWidth;
		height = frameHeight;
		SendSize(layer, "setDrawableSize:", new CGSize(width, height));
		double scale = SendDouble(window, "backingScaleFactor");
		SendDouble(layer, "setContentsScale:", scale > 0 ? scale : 1);

		surface = CreateSurface(width, height);
		if (surface == 0)
		{
			return false;
		}

		nint descriptor = SendDescriptor(Class("MTLTextureDescriptor"), "texture2DDescriptorWithPixelFormat:width:height:mipmapped:", PixelFormatRgba16Float, (ulong)width, (ulong)height, 0);
		metalTexture = SendTexture(device, "newTextureWithDescriptor:iosurface:plane:", descriptor, surface, 0);
		if (metalTexture == 0)
		{
			ReleaseTarget();
			return false;
		}

		glTexture = gl.GenTexture();
		int previous = gl.GetInteger(GetPName.TextureBindingRectangle);
		gl.BindTexture(TextureTarget.TextureRectangle, glTexture);
		int error = NativeMethods.CGLTexImageIOSurface2D(NativeMethods.CGLGetCurrentContext(), GlTextureRectangle, GlRgba16F, width, height, GlRgba, GlHalfFloat, surface, 0);
		gl.BindTexture(TextureTarget.TextureRectangle, (uint)previous);
		if (error != 0)
		{
			ReleaseTarget();
			return false;
		}

		gl.BindFramebuffer(FramebufferTarget.Framebuffer, framebuffer);
		gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.TextureRectangle, glTexture, 0);
		bool complete = gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer) == GLEnum.FramebufferComplete;
		gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
		return complete;
	}

	private void ReleaseTarget()
	{
		if (glTexture != 0)
		{
			gl.DeleteTexture(glTexture);
			glTexture = 0;
		}

		Release(metalTexture);
		metalTexture = 0;
		if (surface != 0)
		{
			NativeMethods.CFRelease(surface);
			surface = 0;
		}
	}

	private static nint CreateSurface(int width, int height)
	{
		nint properties = Send(Class("NSMutableDictionary"), "dictionary");
		SetNumber(properties, "kIOSurfaceWidth", width);
		SetNumber(properties, "kIOSurfaceHeight", height);
		SetNumber(properties, "kIOSurfaceBytesPerElement", 8);
		SetNumber(properties, "kIOSurfacePixelFormat", HalfFloatRgbaFourCC);
		return NativeMethods.IOSurfaceCreate(properties);
	}

	private static void SetNumber(nint dictionary, string key, int value) =>
		SendPointers(dictionary, "setObject:forKey:", SendInt(Class("NSNumber"), "numberWithInt:", value), Constant(IOSurfaceFramework, key));

	private static nint CoreGraphicsConstant(string name) => Constant(CoreGraphics, name);

	private static nint Constant(string library, string name) =>
		Marshal.ReadIntPtr(NativeLibrary.GetExport(NativeLibrary.Load(library), name));

	private static nint Class(string name) => NativeMethods.objc_getClass(name);

	private static void Release(nint instance)
	{
		if (instance != 0)
		{
			Send(instance, "release");
		}
	}

	private static nint Send(nint receiver, string selector) => NativeMethods.MessageSend(receiver, NativeMethods.sel_registerName(selector));

	private static void SendPointer(nint receiver, string selector, nint argument) => NativeMethods.MessageSend(receiver, NativeMethods.sel_registerName(selector), argument);

	private static void SendPointers(nint receiver, string selector, nint first, nint second) => NativeMethods.MessageSend(receiver, NativeMethods.sel_registerName(selector), first, second);

	private static void SendUnsigned(nint receiver, string selector, ulong argument) => NativeMethods.MessageSendUnsigned(receiver, NativeMethods.sel_registerName(selector), argument);

	private static void SendBool(nint receiver, string selector, bool argument) => NativeMethods.MessageSendByte(receiver, NativeMethods.sel_registerName(selector), argument ? (byte)1 : (byte)0);

	private static nint SendInt(nint receiver, string selector, int argument) => NativeMethods.MessageSendInt(receiver, NativeMethods.sel_registerName(selector), argument);

	private static void SendSize(nint receiver, string selector, CGSize size) => NativeMethods.MessageSendSize(receiver, NativeMethods.sel_registerName(selector), size);

	private static double SendDouble(nint receiver, string selector) => NativeMethods.MessageSendDouble(receiver, NativeMethods.sel_registerName(selector));

	private static void SendDouble(nint receiver, string selector, double argument) => NativeMethods.MessageSendDoubleArgument(receiver, NativeMethods.sel_registerName(selector), argument);

	private static nint SendDescriptor(nint receiver, string selector, ulong format, ulong width, ulong height, byte mipmapped) =>
		NativeMethods.MessageSendDescriptor(receiver, NativeMethods.sel_registerName(selector), format, width, height, mipmapped);

	private static nint SendTexture(nint receiver, string selector, nint descriptor, nint ioSurface, ulong plane) =>
		NativeMethods.MessageSendTexture(receiver, NativeMethods.sel_registerName(selector), descriptor, ioSurface, plane);

	/// <summary>The Objective-C runtime and the frameworks' C entry points.</summary>
	private static partial class NativeMethods
	{
		[LibraryImport(ObjC, StringMarshalling = StringMarshalling.Utf8)]
		internal static partial nint objc_getClass(string name);

		[LibraryImport(ObjC, StringMarshalling = StringMarshalling.Utf8)]
		internal static partial nint sel_registerName(string name);

		[LibraryImport(ObjC)]
		internal static partial nint objc_autoreleasePoolPush();

		[LibraryImport(ObjC)]
		internal static partial void objc_autoreleasePoolPop(nint pool);

		[LibraryImport(ObjC, EntryPoint = "objc_msgSend")]
		internal static partial nint MessageSend(nint receiver, nint selector);

		[LibraryImport(ObjC, EntryPoint = "objc_msgSend")]
		internal static partial nint MessageSend(nint receiver, nint selector, nint argument);

		[LibraryImport(ObjC, EntryPoint = "objc_msgSend")]
		internal static partial nint MessageSend(nint receiver, nint selector, nint first, nint second);

		[LibraryImport(ObjC, EntryPoint = "objc_msgSend")]
		internal static partial void MessageSendUnsigned(nint receiver, nint selector, ulong argument);

		[LibraryImport(ObjC, EntryPoint = "objc_msgSend")]
		internal static partial void MessageSendByte(nint receiver, nint selector, byte argument);

		[LibraryImport(ObjC, EntryPoint = "objc_msgSend")]
		internal static partial nint MessageSendInt(nint receiver, nint selector, int argument);

		[LibraryImport(ObjC, EntryPoint = "objc_msgSend")]
		internal static partial void MessageSendSize(nint receiver, nint selector, CGSize size);

		[LibraryImport(ObjC, EntryPoint = "objc_msgSend")]
		internal static partial double MessageSendDouble(nint receiver, nint selector);

		[LibraryImport(ObjC, EntryPoint = "objc_msgSend")]
		internal static partial void MessageSendDoubleArgument(nint receiver, nint selector, double argument);

		[LibraryImport(ObjC, EntryPoint = "objc_msgSend")]
		internal static partial nint MessageSendDescriptor(nint receiver, nint selector, ulong format, ulong width, ulong height, byte mipmapped);

		[LibraryImport(ObjC, EntryPoint = "objc_msgSend")]
		internal static partial nint MessageSendTexture(nint receiver, nint selector, nint descriptor, nint ioSurface, ulong plane);

		[LibraryImport(MetalFramework)]
		internal static partial nint MTLCreateSystemDefaultDevice();

		[LibraryImport(CoreGraphics)]
		internal static partial nint CGColorSpaceCreateWithName(nint name);

		[LibraryImport(CoreGraphics)]
		internal static partial void CGColorSpaceRelease(nint colorSpace);

		[LibraryImport(CoreFoundation)]
		internal static partial void CFRelease(nint instance);

		[LibraryImport(IOSurfaceFramework)]
		internal static partial nint IOSurfaceCreate(nint properties);

		[LibraryImport(OpenGLFramework)]
		internal static partial nint CGLGetCurrentContext();

		[LibraryImport(OpenGLFramework)]
		internal static partial int CGLTexImageIOSurface2D(nint context, uint target, int internalFormat, int width, int height, uint format, uint type, nint ioSurface, uint plane);
	}

	/// <summary>CGSize.</summary>
	[StructLayout(LayoutKind.Sequential)]
	private readonly struct CGSize(double width, double height)
	{
		public readonly double Width = width;
		public readonly double Height = height;
	}
}
