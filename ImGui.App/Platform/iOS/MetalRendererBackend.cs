// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

#if IOS

namespace ktsu.ImGui.App;

using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Reflection;

using CoreAnimation;

using Foundation;

using Hexa.NET.ImGui;

using Metal;

/// <summary>
/// Metal implementation of <see cref="IRendererBackend"/>, ported from the upstream
/// <c>imgui_impl_metal.mm</c> reference. Owns the command queue, render pipeline, sampler, and the
/// set of GPU textures (font atlas + any user textures), and submits a built ImGui draw-data tree
/// to a <see cref="CAMetalLayer"/> each frame. Texture handles are opaque, monotonically increasing
/// <see cref="nint"/> ids (not native pointers) used purely as dictionary keys, mirroring how the
/// desktop OpenGL backend hands GL texture names back to ImGui via <c>SetTexID</c> / <c>GetTexID</c>.
/// </summary>
internal sealed unsafe class MetalRendererBackend : IRendererBackend
{
	private const string VertexFunctionName = "imgui_vertex";
	private const string FragmentFunctionName = "imgui_fragment";

	// Vertex data is bound at buffer(0) (described by the vertex descriptor); the projection matrix
	// is pushed inline at buffer(1) via SetVertexBytes. These match the indices in ImGui.metal.
	private const uint VertexBufferIndex = 0;
	private const uint UniformBufferIndex = 1;

	private readonly CAMetalLayer layer;
	private readonly IMTLDevice device;
	private readonly IMTLCommandQueue commandQueue;
	private readonly IMTLRenderPipelineState pipelineState;
	private readonly IMTLSamplerState samplerState;
	private readonly Dictionary<nint, IMTLTexture> textures = [];

	// Clear colour matches the desktop renderer's default (RGB 115,140,153).
	private readonly MTLClearColor clearColor = new(115.0 / 255.0, 140.0 / 255.0, 153.0 / 255.0, 1.0);

	private nint nextTextureId = 1;
	private bool disposed;

	/// <summary>
	/// Initialises the Metal backend against a layer. Compiles the embedded ImGui shader at runtime,
	/// builds the render pipeline (matching the layer's pixel format) and the linear sampler.
	/// </summary>
	/// <param name="layer">The Metal layer whose drawables this backend renders into.</param>
	public MetalRendererBackend(CAMetalLayer layer)
	{
		Ensure.NotNull(layer);
		this.layer = layer;

		device = layer.Device
			?? MTLDevice.SystemDefault
			?? throw new PlatformNotSupportedException("No Metal device is available on this system.");

		commandQueue = device.CreateCommandQueue()
			?? throw new InvalidOperationException("Failed to create a Metal command queue.");

		IMTLLibrary library = CompileShaderLibrary(device);
		IMTLFunction vertexFunction = library.CreateFunction(VertexFunctionName);
		IMTLFunction fragmentFunction = library.CreateFunction(FragmentFunctionName);

		pipelineState = CreatePipelineState(device, layer.PixelFormat, vertexFunction, fragmentFunction);
		samplerState = CreateSamplerState(device);
	}

	/// <inheritdoc />
	public nint CreateTexture(ReadOnlySpan<byte> rgba, int width, int height)
	{
		using MTLTextureDescriptor descriptor = MTLTextureDescriptor.CreateTexture2DDescriptor(
			MTLPixelFormat.RGBA8Unorm, (nuint)width, (nuint)height, mipmapped: false);

		IMTLTexture texture = device.CreateTexture(descriptor)
			?? throw new InvalidOperationException("Failed to create a Metal texture.");

		MTLRegion region = MTLRegion.Create2D(0, 0, (nuint)width, (nuint)height);
		fixed (byte* pixels = rgba)
		{
			texture.ReplaceRegion(region, 0, (nint)pixels, (nuint)(width * 4));
		}

		nint id = nextTextureId++;
		textures[id] = texture;
		return id;
	}

	/// <inheritdoc />
	public void DeleteTexture(nint id)
	{
		if (textures.Remove(id, out IMTLTexture? texture))
		{
			texture.Dispose();
		}
	}

	/// <inheritdoc />
	public void RenderDrawData(ImDrawDataPtr drawData)
	{
		int framebufferWidth = (int)(drawData.DisplaySize.X * drawData.FramebufferScale.X);
		int framebufferHeight = (int)(drawData.DisplaySize.Y * drawData.FramebufferScale.Y);
		if (framebufferWidth <= 0 || framebufferHeight <= 0 || drawData.CmdListsCount == 0)
		{
			return;
		}

		ICAMetalDrawable? drawable = layer.NextDrawable();
		if (drawable is null)
		{
			return;
		}

		IMTLCommandBuffer? commandBuffer = commandQueue.CommandBuffer();
		if (commandBuffer is null)
		{
			drawable.Dispose();
			return;
		}

		using MTLRenderPassDescriptor passDescriptor = new();
		MTLRenderPassColorAttachmentDescriptor colorAttachment = passDescriptor.ColorAttachments[0];
		colorAttachment.Texture = drawable.Texture;
		colorAttachment.LoadAction = MTLLoadAction.Clear;
		colorAttachment.StoreAction = MTLStoreAction.Store;
		colorAttachment.ClearColor = clearColor;

		IMTLRenderCommandEncoder? encoder = commandBuffer.CreateRenderCommandEncoder(passDescriptor);
		if (encoder is null)
		{
			commandBuffer.Commit();
			drawable.Dispose();
			return;
		}

		// Orthographic projection in ImGui's logical (point) space; the viewport maps it to pixels.
		float left = drawData.DisplayPos.X;
		float right = drawData.DisplayPos.X + drawData.DisplaySize.X;
		float top = drawData.DisplayPos.Y;
		float bottom = drawData.DisplayPos.Y + drawData.DisplaySize.Y;
		Span<float> projection =
		[
			2f / (right - left), 0f, 0f, 0f,
			0f, 2f / (top - bottom), 0f, 0f,
			0f, 0f, 1f, 0f,
			(right + left) / (left - right), (top + bottom) / (bottom - top), 0f, 1f,
		];

		SetupRenderState(encoder, framebufferWidth, framebufferHeight, projection);

		Vector2 clipOff = drawData.DisplayPos;
		Vector2 clipScale = drawData.FramebufferScale;

		// Per-frame vertex/index buffers; released once the GPU has consumed them.
		List<IMTLBuffer> transientBuffers = [];

		for (int n = 0; n < drawData.CmdListsCount; n++)
		{
			ImDrawListPtr cmdList = drawData.CmdLists[n];

			int vertexBytes = cmdList.VtxBuffer.Size * sizeof(ImDrawVert);
			int indexBytes = cmdList.IdxBuffer.Size * sizeof(ushort);
			if (vertexBytes == 0 || indexBytes == 0)
			{
				continue;
			}

			IMTLBuffer vertexBuffer = device.CreateBuffer((nint)cmdList.VtxBuffer.Data, (nuint)vertexBytes, MTLResourceOptions.StorageModeShared)!;
			IMTLBuffer indexBuffer = device.CreateBuffer((nint)cmdList.IdxBuffer.Data, (nuint)indexBytes, MTLResourceOptions.StorageModeShared)!;
			transientBuffers.Add(vertexBuffer);
			transientBuffers.Add(indexBuffer);

			encoder.SetVertexBuffer(vertexBuffer, 0, VertexBufferIndex);

			for (int cmdIndex = 0; cmdIndex < cmdList.CmdBuffer.Size; cmdIndex++)
			{
				ImDrawCmd cmd = cmdList.CmdBuffer[cmdIndex];

				if (cmd.UserCallback != null)
				{
					// Mirror imgui_impl_*: the reset sentinel asks the backend to restore its pipeline
					// state; any other value is a real user callback invoked with this list + command.
					if ((nint)cmd.UserCallback == ImGui.ImDrawCallbackResetRenderState)
					{
						SetupRenderState(encoder, framebufferWidth, framebufferHeight, projection);
						encoder.SetVertexBuffer(vertexBuffer, 0, VertexBufferIndex);
					}
					else
					{
						ImDrawCmd localCmd = cmd;
						((delegate* unmanaged[Cdecl]<ImDrawList*, ImDrawCmd*, void>)cmd.UserCallback)(cmdList.Handle, &localCmd);
					}

					continue;
				}

				// Project the clip rectangle into framebuffer pixels and clamp to the render target.
				double clipMinX = Math.Max((cmd.ClipRect.X - clipOff.X) * clipScale.X, 0.0);
				double clipMinY = Math.Max((cmd.ClipRect.Y - clipOff.Y) * clipScale.Y, 0.0);
				double clipMaxX = Math.Min((cmd.ClipRect.Z - clipOff.X) * clipScale.X, framebufferWidth);
				double clipMaxY = Math.Min((cmd.ClipRect.W - clipOff.Y) * clipScale.Y, framebufferHeight);
				if (clipMaxX <= clipMinX || clipMaxY <= clipMinY)
				{
					continue;
				}

				encoder.SetScissorRect(new MTLScissorRect(
					(nuint)clipMinX, (nuint)clipMinY,
					(nuint)(clipMaxX - clipMinX), (nuint)(clipMaxY - clipMinY)));

				nint textureId = (nint)(nuint)cmd.GetTexID();
				if (textures.TryGetValue(textureId, out IMTLTexture? texture))
				{
					encoder.SetFragmentTexture(texture, 0);
				}

				encoder.DrawIndexedPrimitives(
					MTLPrimitiveType.Triangle,
					cmd.ElemCount,
					MTLIndexType.UInt16,
					indexBuffer,
					(nuint)(cmd.IdxOffset * sizeof(ushort)),
					instanceCount: 1,
					baseVertex: (nint)cmd.VtxOffset,
					baseInstance: 0);
			}
		}

		encoder.EndEncoding();
		commandBuffer.PresentDrawable(drawable);
		commandBuffer.AddCompletedHandler(_ =>
		{
			foreach (IMTLBuffer buffer in transientBuffers)
			{
				buffer.Dispose();
			}

			drawable.Dispose();
		});
		commandBuffer.Commit();
	}

	/// <summary>Applies the frame-invariant pipeline state: pipeline, sampler, viewport, projection.</summary>
	private void SetupRenderState(IMTLRenderCommandEncoder encoder, int framebufferWidth, int framebufferHeight, ReadOnlySpan<float> projection)
	{
		encoder.SetRenderPipelineState(pipelineState);
		encoder.SetFragmentSamplerState(samplerState, 0);
		encoder.SetViewport(new MTLViewport(0.0, 0.0, framebufferWidth, framebufferHeight, 0.0, 1.0));

		fixed (float* projectionPtr = projection)
		{
			encoder.SetVertexBytes((nint)projectionPtr, (nuint)(projection.Length * sizeof(float)), UniformBufferIndex);
		}
	}

	private static IMTLLibrary CompileShaderLibrary(IMTLDevice device)
	{
		string source = LoadShaderSource();
		using MTLCompileOptions options = new();
		IMTLLibrary library = device.CreateLibrary(source, options, out NSError error);
		using (error)
		{
			return error is not null || library is null
				? throw new InvalidOperationException($"Failed to compile the ImGui Metal shader: {error?.LocalizedDescription ?? "unknown error"}.")
				: library;
		}
	}

	private static string LoadShaderSource()
	{
		Assembly assembly = typeof(MetalRendererBackend).Assembly;
		string resourceName = Array.Find(assembly.GetManifestResourceNames(), name => name.EndsWith("ImGui.metal", StringComparison.Ordinal))
			?? throw new InvalidOperationException("Embedded Metal shader 'ImGui.metal' was not found in the assembly.");

		using Stream stream = assembly.GetManifestResourceStream(resourceName)
			?? throw new InvalidOperationException($"Failed to open the embedded Metal shader stream '{resourceName}'.");
		using StreamReader reader = new(stream);
		return reader.ReadToEnd();
	}

	private static IMTLRenderPipelineState CreatePipelineState(IMTLDevice device, MTLPixelFormat pixelFormat, IMTLFunction vertexFunction, IMTLFunction fragmentFunction)
	{
		MTLVertexDescriptor vertexDescriptor = new();

		// ImDrawVert: pos float2 @0, uv float2 @8, col uchar4 @16, stride 20.
		vertexDescriptor.Attributes[0].Format = MTLVertexFormat.Float2;
		vertexDescriptor.Attributes[0].Offset = 0;
		vertexDescriptor.Attributes[0].BufferIndex = VertexBufferIndex;
		vertexDescriptor.Attributes[1].Format = MTLVertexFormat.Float2;
		vertexDescriptor.Attributes[1].Offset = 8;
		vertexDescriptor.Attributes[1].BufferIndex = VertexBufferIndex;
		// Non-normalized UChar4: the shader reads uchar4 and divides by 255 itself (matching upstream
		// imgui_impl_metal). A Normalized format is rejected because the shader attribute is uchar4,
		// not float4.
		vertexDescriptor.Attributes[2].Format = MTLVertexFormat.UChar4;
		vertexDescriptor.Attributes[2].Offset = 16;
		vertexDescriptor.Attributes[2].BufferIndex = VertexBufferIndex;
		vertexDescriptor.Layouts[0].Stride = (nuint)sizeof(ImDrawVert);
		vertexDescriptor.Layouts[0].StepFunction = MTLVertexStepFunction.PerVertex;

		using MTLRenderPipelineDescriptor descriptor = new()
		{
			VertexFunction = vertexFunction,
			FragmentFunction = fragmentFunction,
			VertexDescriptor = vertexDescriptor,
		};

		MTLRenderPipelineColorAttachmentDescriptor colorAttachment = descriptor.ColorAttachments[0];
		colorAttachment.PixelFormat = pixelFormat;
		colorAttachment.BlendingEnabled = true;
		colorAttachment.RgbBlendOperation = MTLBlendOperation.Add;
		colorAttachment.AlphaBlendOperation = MTLBlendOperation.Add;
		colorAttachment.SourceRgbBlendFactor = MTLBlendFactor.SourceAlpha;
		colorAttachment.DestinationRgbBlendFactor = MTLBlendFactor.OneMinusSourceAlpha;
		colorAttachment.SourceAlphaBlendFactor = MTLBlendFactor.One;
		colorAttachment.DestinationAlphaBlendFactor = MTLBlendFactor.OneMinusSourceAlpha;

		IMTLRenderPipelineState state = device.CreateRenderPipelineState(descriptor, out NSError error);
		using (error)
		{
			return error is not null || state is null
				? throw new InvalidOperationException($"Failed to create the ImGui Metal pipeline state: {error?.LocalizedDescription ?? "unknown error"}.")
				: state;
		}
	}

	private static IMTLSamplerState CreateSamplerState(IMTLDevice device)
	{
		using MTLSamplerDescriptor descriptor = new()
		{
			MinFilter = MTLSamplerMinMagFilter.Linear,
			MagFilter = MTLSamplerMinMagFilter.Linear,
			SAddressMode = MTLSamplerAddressMode.ClampToEdge,
			TAddressMode = MTLSamplerAddressMode.ClampToEdge,
		};

		return device.CreateSamplerState(descriptor)
			?? throw new InvalidOperationException("Failed to create the Metal sampler state.");
	}

	/// <inheritdoc />
	public void Dispose()
	{
		if (disposed)
		{
			return;
		}

		foreach (IMTLTexture texture in textures.Values)
		{
			texture.Dispose();
		}

		textures.Clear();

		samplerState.Dispose();
		pipelineState.Dispose();
		commandQueue.Dispose();

		disposed = true;
	}
}

#endif
