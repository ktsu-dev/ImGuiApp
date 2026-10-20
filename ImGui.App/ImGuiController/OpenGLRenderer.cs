// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.ImGuiController;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Hexa.NET.ImGui;
using ktsu.ImGui.App;
using ktsu.ImGui.Rendering;
using Silk.NET.OpenGL;

/// <summary>
/// Draws ImGui's frames with OpenGL: the shader and buffers, the textures behind
/// <see cref="IRendererBackend"/>, and the 3D path behind <see cref="IRenderer3D"/>.
/// </summary>
/// <remarks>
/// It knows nothing about ImGui's context or the window. <see cref="ImGuiContextHost"/> owns the
/// context and hands this the finished draw data, and <see cref="ImGuiController"/> carries the
/// window's input in (#313).
/// </remarks>
internal sealed class OpenGLRenderer : IRendererBackend, IRenderer3D
{
	internal GL? _gl;

	internal int _attribLocationTex;
	internal int _attribLocationProjMtx;
	internal int _attribLocationVtxPos;
	internal int _attribLocationVtxUV;
	internal int _attribLocationVtxColor;
	internal uint _vboHandle;
	internal uint _elementsHandle;
	internal uint _vertexArrayObject;

	internal Shader? _shader;

	// The 3D path: offscreen framebuffers and a second program. Holds nothing on the GPU until a
	// target is created or something is drawn.
	internal GLRenderer3D? _renderer3D;

	internal bool _disposed;

	/// <summary>Builds the shader program and buffers ImGui's frames are drawn with.</summary>
	/// <param name="gl">The OpenGL context to draw into. It belongs to the caller and is not disposed here.</param>
	[SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "The GLWrapper adapts a context this class does not own -- ImGuiApp creates and disposes it, and _gl is likewise never disposed here. Disposing the wrapper would tear down the caller's GL context, so it deliberately lives as long as this renderer.")]
	public OpenGLRenderer(GL gl)
	{
		Ensure.NotNull(gl);
		_gl = gl;

		// Wrapped rather than used directly so the uploader depends on IGL, which is what lets the
		// texture calls be exercised without a graphics context.
		TextureUploader = new GLTextureUploader(new GLWrapper(gl));
		_renderer3D = new GLRenderer3D(new GL3DWrapper(gl));
		CreateDeviceResources();
	}

	/// <summary>Gets the uploader carrying ImGui's own textures (the font atlas) to this renderer.</summary>
	internal ITextureUploader TextureUploader { get; }

	/// <inheritdoc />
	[SuppressMessage(
		"Reliability",
		"CA2000:Dispose objects before losing scope",
		Justification = "The Texture wrapper's resource is the GL texture; we hand its name back to ImGui, " +
			"so disposing here would delete the very texture we're returning. Lifetime is owned by the caller.")]
	[SuppressMessage("Major Code Smell", "S6640:Make sure that using \"unsafe\" is safe here", Justification = "Required for native ImGui/OpenGL interop; pointer is pinned within a fixed block and not retained.")]
	public unsafe nint CreateTexture(ReadOnlySpan<byte> rgba, int width, int height)
	{
		if (_gl is null)
		{
			throw new InvalidOperationException("OpenGL context is not initialized.");
		}

		// Preserve whatever was bound — the caller may be mid-frame.
		_gl.GetInteger(GLEnum.TextureBinding2D, out int previousTextureId);

		uint glTexture;
		fixed (byte* pixelPtr = rgba)
		{
			Texture texture = new(_gl, width, height, (IntPtr)pixelPtr, pxFormat: PixelFormat.Rgba);
			texture.Bind();
			texture.SetMagFilter(TextureMagFilter.Linear);
			texture.SetMinFilter(TextureMinFilter.Linear);
			glTexture = texture.GlTexture;
		}

		_gl.BindTexture(GLEnum.Texture2D, (uint)previousTextureId);
		return (nint)glTexture;
	}

	/// <inheritdoc />
	[SuppressMessage("Major Code Smell", "S6640:Make sure that using \"unsafe\" is safe here", Justification = "Required for native ImGui/OpenGL interop; pointer is pinned within a fixed block and not retained.")]
	public unsafe bool UpdateTexture(nint id, ReadOnlySpan<byte> rgba, int width, int height)
	{
		// Matches CreateTexture rather than DeleteTexture's null-conditional no-op: a torn-down context
		// is a fault here, not a declined capability. Returning false would send the caller into the
		// recreate path, where CreateTexture would throw this same exception one step further from the
		// cause.
		if (_gl is null)
		{
			throw new InvalidOperationException("OpenGL context is not initialized.");
		}

		// Preserve whatever was bound — the caller may be mid-frame.
		_gl.GetInteger(GLEnum.TextureBinding2D, out int previousTextureId);
		_gl.BindTexture(GLEnum.Texture2D, (uint)id);

		fixed (byte* pixelPtr = rgba)
		{
			_gl.TexSubImage2D(GLEnum.Texture2D, 0, 0, 0, (uint)width, (uint)height, GLEnum.Rgba, GLEnum.UnsignedByte, pixelPtr);
		}

		_gl.BindTexture(GLEnum.Texture2D, (uint)previousTextureId);
		return true;
	}

	/// <inheritdoc />
	/// <exception cref="ArgumentException">
	/// <paramref name="id"/> is a render target. Its texture name comes from the same namespace as
	/// <see cref="CreateTexture"/>'s, so deleting it here would free a live target's colour attachment;
	/// a target is released through <see cref="DeleteRenderTarget"/> only.
	/// </exception>
	public void DeleteTexture(nint id)
	{
		if (_renderer3D?.IsRenderTarget(id) == true)
		{
			throw new ArgumentException($"Texture {id} is a render target's colour attachment. Release it with DeleteRenderTarget.", nameof(id));
		}

		// _gl can be null if the context has already been torn down; callers expect a no-op then.
		_gl?.DeleteTexture((uint)id);
	}

	/// <inheritdoc />
	public nint CreateRenderTarget(int width, int height, bool depth) => Renderer3D.CreateRenderTarget(width, height, depth);

	/// <inheritdoc />
	public bool ResizeRenderTarget(nint target, int width, int height) => Renderer3D.ResizeRenderTarget(target, width, height);

	/// <inheritdoc />
	public void DeleteRenderTarget(nint target) => Renderer3D.DeleteRenderTarget(target);

	/// <inheritdoc />
	public nint GetTargetTexture(nint target) => Renderer3D.GetTargetTexture(target);

	/// <inheritdoc />
	public void Clear(nint target, Vector4 color, float depth) => Renderer3D.Clear(target, color, depth);

	/// <inheritdoc />
	public void Draw(nint target, ReadOnlySpan<Vertex3D> vertices, ReadOnlySpan<uint> indices, in DrawState3D state) =>
		Renderer3D.Draw(target, vertices, indices, state);

	private GLRenderer3D Renderer3D => _renderer3D ?? throw new InvalidOperationException("OpenGL context is not initialized.");

	[SuppressMessage("Major Code Smell", "S6640:Make sure that using \"unsafe\" is safe here", Justification = "Required for OpenGL vertex attribute pointer setup using ImGui draw vertex layout; pointers are local and not retained.")]
	internal unsafe void SetupRenderState(ImDrawDataPtr drawDataPtr, int framebufferWidth, int framebufferHeight)
	{
		if (_gl is null || _shader is null)
		{
			return;
		}

		// Set the viewport from the draw data every frame rather than relying on a resize event to
		// have set it. Some backends never raise one — a Wayland surface can be sized by the
		// compositor before the handler is attached — which would leave the viewport at whatever
		// size the drawable had when the context was created, and the UI drawn into a corner.
		_gl.Viewport(0, 0, (uint)framebufferWidth, (uint)framebufferHeight);

		// Setup render state: alpha-blending enabled, no face culling, no depth testing, scissor enabled, polygon fill
		_gl.Enable(GLEnum.Blend);
		_gl.BlendEquation(GLEnum.FuncAdd);
		_gl.BlendFuncSeparate(GLEnum.SrcAlpha, GLEnum.OneMinusSrcAlpha, GLEnum.One, GLEnum.OneMinusSrcAlpha);
		_gl.Disable(GLEnum.CullFace);
		_gl.Disable(GLEnum.DepthTest);
		_gl.Disable(GLEnum.StencilTest);
		_gl.Enable(GLEnum.ScissorTest);
#if !GLES && !LEGACY
		_gl.Disable(GLEnum.PrimitiveRestart);
		_gl.PolygonMode(GLEnum.FrontAndBack, GLEnum.Fill);
		// Disable multisampling for pixel-perfect rendering
		_gl.Disable(GLEnum.Multisample);
#endif

		float L = drawDataPtr.DisplayPos.X;
		float R = drawDataPtr.DisplayPos.X + drawDataPtr.DisplaySize.X;
		float T = drawDataPtr.DisplayPos.Y;
		float B = drawDataPtr.DisplayPos.Y + drawDataPtr.DisplaySize.Y;

		// Use double precision for more accurate orthographic projection
		double dL = L, dR = R, dT = T, dB = B;

		Span<float> orthoProjection = [
				(float)(2.0 / (dR - dL)), 0.0f, 0.0f, 0.0f,
				0.0f, (float)(2.0 / (dT - dB)), 0.0f, 0.0f,
				0.0f, 0.0f, -1.0f, 0.0f,
				(float)((dR + dL) / (dL - dR)), (float)((dT + dB) / (dB - dT)), 0.0f, 1.0f,
			];

		_shader.UseShader();
		_gl.Uniform1(_attribLocationTex, 0);
		_gl.UniformMatrix4(_attribLocationProjMtx, 1, false, orthoProjection);
		_gl.CheckGlError("Projection");

		_gl.BindSampler(0, 0);

		// Setup desired GL state
		// Recreate the VAO every time (this is to easily allow multiple GL contexts to be rendered to. VAO are not shared among GL contexts)
		// The renderer would actually work without any VAO bound, but then our VertexAttrib calls would overwrite the default one currently bound.
		_vertexArrayObject = _gl.GenVertexArray();
		_gl.BindVertexArray(_vertexArrayObject);
		_gl.CheckGlError("VAO");

		// Bind vertex/index buffers and setup attributes for ImDrawVert
		_gl.BindBuffer(GLEnum.ArrayBuffer, _vboHandle);
		_gl.BindBuffer(GLEnum.ElementArrayBuffer, _elementsHandle);
		_gl.EnableVertexAttribArray((uint)_attribLocationVtxPos);
		_gl.EnableVertexAttribArray((uint)_attribLocationVtxUV);
		_gl.EnableVertexAttribArray((uint)_attribLocationVtxColor);
		_gl.VertexAttribPointer((uint)_attribLocationVtxPos, 2, GLEnum.Float, false, (uint)sizeof(ImDrawVert), (void*)0);
		_gl.VertexAttribPointer((uint)_attribLocationVtxUV, 2, GLEnum.Float, false, (uint)sizeof(ImDrawVert), (void*)8);
		_gl.VertexAttribPointer((uint)_attribLocationVtxColor, 4, GLEnum.UnsignedByte, true, (uint)sizeof(ImDrawVert), (void*)16);
	}

	/// <summary>
	/// Switches the OpenGL blend function for the remainder of the current draw pass.
	/// Invoked from a draw-command callback (see <see cref="ImGuiApp.SetDrawBlendMode"/>) so the
	/// change applies only to the primitives recorded after it, until another blend-mode callback
	/// or the <c>ImDrawCallback_ResetRenderState</c> sentinel restores the default state.
	/// </summary>
	/// <param name="mode">The blend mode to activate.</param>
	internal void SetBlendMode(ImGuiAppBlendMode mode)
	{
		if (_gl is null)
		{
			return;
		}

		_gl.BlendEquation(GLEnum.FuncAdd);
		switch (mode)
		{
			case ImGuiAppBlendMode.Additive:
				// src*srcAlpha + dst*1: overlapping translucent shapes accumulate toward white
				// instead of compositing alpha-over, giving a neon/glow look.
				_gl.BlendFuncSeparate(GLEnum.SrcAlpha, GLEnum.One, GLEnum.One, GLEnum.One);
				break;

			default:
				// AlphaBlend is the default; matches the default established in SetupRenderState.
				_gl.BlendFuncSeparate(GLEnum.SrcAlpha, GLEnum.OneMinusSrcAlpha, GLEnum.One, GLEnum.OneMinusSrcAlpha);
				break;
		}
	}

	/// <inheritdoc />
	[SuppressMessage("Major Code Smell", "S927:Parameter names should match base declaration and other partial definitions", Justification = "Parameter name 'drawDataPtr' is used consistently in this implementation; renaming could break existing callers using named arguments.")]
	public void RenderDrawData(ImDrawDataPtr drawDataPtr)
	{
		if (_gl is null)
		{
			return;
		}

		int framebufferWidth = (int)(drawDataPtr.DisplaySize.X * drawDataPtr.FramebufferScale.X);
		int framebufferHeight = (int)(drawDataPtr.DisplaySize.Y * drawDataPtr.FramebufferScale.Y);
		if (framebufferWidth <= 0 || framebufferHeight <= 0)
		{
			return;
		}

		// Backup GL state
		_gl.GetInteger(GLEnum.ActiveTexture, out int lastActiveTexture);
		_gl.ActiveTexture(GLEnum.Texture0);

		_gl.GetInteger(GLEnum.CurrentProgram, out int lastProgram);
		_gl.GetInteger(GLEnum.TextureBinding2D, out int lastTexture);

		_gl.GetInteger(GLEnum.SamplerBinding, out int lastSampler);

		_gl.GetInteger(GLEnum.ArrayBufferBinding, out int lastArrayBuffer);
		_gl.GetInteger(GLEnum.VertexArrayBinding, out int lastVertexArrayObject);

#if !GLES
		Span<int> lastPolygonMode = stackalloc int[2];
		_gl.GetInteger(GLEnum.PolygonMode, lastPolygonMode);
#endif

		Span<int> lastScissorBox = stackalloc int[4];
		_gl.GetInteger(GLEnum.ScissorBox, lastScissorBox);

		_gl.GetInteger(GLEnum.BlendSrcRgb, out int lastBlendSrcRgb);
		_gl.GetInteger(GLEnum.BlendDstRgb, out int lastBlendDstRgb);

		_gl.GetInteger(GLEnum.BlendSrcAlpha, out int lastBlendSrcAlpha);
		_gl.GetInteger(GLEnum.BlendDstAlpha, out int lastBlendDstAlpha);

		_gl.GetInteger(GLEnum.BlendEquationRgb, out int lastBlendEquationRgb);
		_gl.GetInteger(GLEnum.BlendEquationAlpha, out int lastBlendEquationAlpha);

		bool lastEnableBlend = _gl.IsEnabled(GLEnum.Blend);
		bool lastEnableCullFace = _gl.IsEnabled(GLEnum.CullFace);
		bool lastEnableDepthTest = _gl.IsEnabled(GLEnum.DepthTest);
		bool lastEnableStencilTest = _gl.IsEnabled(GLEnum.StencilTest);
		bool lastEnableScissorTest = _gl.IsEnabled(GLEnum.ScissorTest);

#if !GLES && !LEGACY
		bool lastEnablePrimitiveRestart = _gl.IsEnabled(GLEnum.PrimitiveRestart);
#endif

		SetupRenderState(drawDataPtr, framebufferWidth, framebufferHeight);

		// Will project scissor/clipping rectangles into framebuffer space
		Vector2 clipOff = drawDataPtr.DisplayPos;         // (0,0) unless using multi-viewports
		Vector2 clipScale = drawDataPtr.FramebufferScale; // (1,1) unless using retina display which are often (2,2)

		// Render command lists
		RenderCommandLists(drawDataPtr, framebufferWidth, framebufferHeight, clipOff, clipScale);

		// Destroy the temporary VAO
		_gl.DeleteVertexArray(_vertexArrayObject);
		_vertexArrayObject = 0;

		// Restore modified GL state
		_gl.UseProgram((uint)lastProgram);
		_gl.BindTexture(GLEnum.Texture2D, (uint)lastTexture);

		_gl.BindSampler(0, (uint)lastSampler);

		_gl.ActiveTexture((GLEnum)lastActiveTexture);

		_gl.BindVertexArray((uint)lastVertexArrayObject);

		_gl.BindBuffer(GLEnum.ArrayBuffer, (uint)lastArrayBuffer);
		_gl.BlendEquationSeparate((GLEnum)lastBlendEquationRgb, (GLEnum)lastBlendEquationAlpha);
		_gl.BlendFuncSeparate((GLEnum)lastBlendSrcRgb, (GLEnum)lastBlendDstRgb, (GLEnum)lastBlendSrcAlpha, (GLEnum)lastBlendDstAlpha);

		SetGlCapability(GLEnum.Blend, lastEnableBlend);
		SetGlCapability(GLEnum.CullFace, lastEnableCullFace);
		SetGlCapability(GLEnum.DepthTest, lastEnableDepthTest);
		SetGlCapability(GLEnum.StencilTest, lastEnableStencilTest);
		SetGlCapability(GLEnum.ScissorTest, lastEnableScissorTest);

#if !GLES && !LEGACY
		SetGlCapability(GLEnum.PrimitiveRestart, lastEnablePrimitiveRestart);

		_gl.PolygonMode(GLEnum.FrontAndBack, (GLEnum)lastPolygonMode[0]);
#endif

		_gl.Scissor(lastScissorBox[0], lastScissorBox[1], (uint)lastScissorBox[2], (uint)lastScissorBox[3]);
	}

	/// <summary>
	/// Renders all ImGui draw command lists to the current framebuffer.
	/// </summary>
	[SuppressMessage("Major Code Smell", "S6640:Make sure that using \"unsafe\" is safe here", Justification = "Required for native ImGui/OpenGL render loop; unsafe pointers are used only for GPU buffer uploads and draw calls, scoped to the call.")]
	private unsafe void RenderCommandLists(ImDrawDataPtr drawDataPtr, int framebufferWidth, int framebufferHeight, Vector2 clipOff, Vector2 clipScale)
	{
		for (int n = 0; n < drawDataPtr.CmdListsCount; n++)
		{
			ImDrawListPtr cmdListPtr = drawDataPtr.CmdLists[n];

			// Upload vertex/index buffers

			_gl!.BufferData(GLEnum.ArrayBuffer, (nuint)(cmdListPtr.VtxBuffer.Size * sizeof(ImDrawVert)), cmdListPtr.VtxBuffer.Data, GLEnum.StreamDraw);
			_gl.CheckGlError($"Data Vert {n}");
			_gl.BufferData(GLEnum.ElementArrayBuffer, (nuint)(cmdListPtr.IdxBuffer.Size * sizeof(ushort)), cmdListPtr.IdxBuffer.Data, GLEnum.StreamDraw);
			_gl.CheckGlError($"Data Idx {n}");

			for (int cmd_i = 0; cmd_i < cmdListPtr.CmdBuffer.Size; cmd_i++)
			{
				ImDrawCmd cmdPtr = cmdListPtr.CmdBuffer[cmd_i];
				RenderDrawCommand(drawDataPtr, cmdListPtr, cmdPtr, framebufferWidth, framebufferHeight, clipOff, clipScale);
			}
		}
	}

	/// <summary>
	/// Executes a single ImGui draw command, honoring user callbacks or issuing the scissored draw call.
	/// </summary>
	[SuppressMessage("Major Code Smell", "S6640:Make sure that using \"unsafe\" is safe here", Justification = "Required for native ImGui/OpenGL render loop; unsafe pointers are used only for GPU buffer uploads and draw calls, scoped to the call.")]
	private unsafe void RenderDrawCommand(ImDrawDataPtr drawDataPtr, ImDrawListPtr cmdListPtr, ImDrawCmd cmdPtr, int framebufferWidth, int framebufferHeight, Vector2 clipOff, Vector2 clipScale)
	{
		if (cmdPtr.UserCallback != null)
		{
			// Honor draw-command callbacks the way the stock imgui_impl_opengl3 backend
			// does. The ImDrawCallback_ResetRenderState sentinel asks the backend to
			// restore its own pipeline state (used after a callback changes GL state, e.g.
			// a per-region blend-mode switch). Any other value is a real user callback —
			// invoke it with this command's draw list and command rather than crashing.
			if ((nint)cmdPtr.UserCallback == ImGui.ImDrawCallbackResetRenderState)
			{
				SetupRenderState(drawDataPtr, framebufferWidth, framebufferHeight);
			}
			else
			{
				ImDrawCmd localCmd = cmdPtr;
				((delegate* unmanaged[Cdecl]<ImDrawList*, ImDrawCmd*, void>)cmdPtr.UserCallback)(cmdListPtr.Handle, &localCmd);
			}

			return;
		}

		Vector4 clipRect;
		clipRect.X = (cmdPtr.ClipRect.X - clipOff.X) * clipScale.X;
		clipRect.Y = (cmdPtr.ClipRect.Y - clipOff.Y) * clipScale.Y;
		clipRect.Z = (cmdPtr.ClipRect.Z - clipOff.X) * clipScale.X;
		clipRect.W = (cmdPtr.ClipRect.W - clipOff.Y) * clipScale.Y;

		if (clipRect.X < framebufferWidth && clipRect.Y < framebufferHeight && clipRect.Z >= 0.0f && clipRect.W >= 0.0f)
		{
			// Round scissor rectangle to pixel boundaries to avoid sub-pixel rendering issues
			int scissorX = (int)Math.Floor(clipRect.X);
			int scissorY = (int)Math.Floor(framebufferHeight - clipRect.W);
			uint scissorWidth = (uint)Math.Max(0, Math.Ceiling(clipRect.Z - clipRect.X));
			uint scissorHeight = (uint)Math.Max(0, Math.Ceiling(clipRect.W - clipRect.Y));

			// Apply scissor/clipping rectangle
			_gl!.Scissor(scissorX, scissorY, scissorWidth, scissorHeight);
			_gl.CheckGlError("Scissor");

			// Bind texture, Draw
			// In ImGui 1.92.0+, use GetTexID() method to get texture ID
			// This method returns the texture ID compatible with OpenGL
			uint textureId = (uint)(nuint)cmdPtr.GetTexID();
			_gl.BindTexture(GLEnum.Texture2D, textureId);
			_gl.CheckGlError("Texture");

			_gl.DrawElementsBaseVertex(GLEnum.Triangles, cmdPtr.ElemCount, GLEnum.UnsignedShort, (void*)(cmdPtr.IdxOffset * sizeof(ushort)), (int)cmdPtr.VtxOffset);
			_gl.CheckGlError("Draw");
		}
	}

	/// <summary>
	/// Enables or disables an OpenGL capability based on the previously captured state.
	/// </summary>
	private void SetGlCapability(GLEnum capability, bool enabled)
	{
		if (enabled)
		{
			_gl!.Enable(capability);
		}
		else
		{
			_gl!.Disable(capability);
		}
	}

	internal void CreateDeviceResources()
	{
		if (_gl is null)
		{
			return;
		}

		// Backup GL state

		_gl.GetInteger(GLEnum.TextureBinding2D, out int lastTexture);
		_gl.GetInteger(GLEnum.ArrayBufferBinding, out int lastArrayBuffer);
		_gl.GetInteger(GLEnum.VertexArrayBinding, out int lastVertexArray);

		string vertexSource =
			@"#version 330
			layout (location = 0) in vec2 Position;
			layout (location = 1) in vec2 UV;
			layout (location = 2) in vec4 Color;
			uniform mat4 ProjMtx;
			out vec2 Frag_UV;
			out vec4 Frag_Color;
			void main()
			{
				Frag_UV = UV;
				Frag_Color = Color;
				// Round position to nearest pixel to improve line alignment
				vec2 roundedPos = floor(Position.xy + 0.5);
				gl_Position = ProjMtx * vec4(roundedPos, 0, 1);
			}";

		string fragmentSource =
			@"#version 330
			in vec2 Frag_UV;
			in vec4 Frag_Color;
			uniform sampler2D Texture;
			layout (location = 0) out vec4 Out_Color;
			void main()
			{
				// Use precise texture sampling to avoid floating point errors
				vec2 texelSize = 1.0 / textureSize(Texture, 0);
				vec2 adjustedUV = clamp(Frag_UV, texelSize * 0.5, 1.0 - texelSize * 0.5);
				Out_Color = Frag_Color * texture(Texture, adjustedUV);
			}";

		_shader = new Shader(_gl, vertexSource, fragmentSource);

		_attribLocationTex = _shader.GetUniformLocation("Texture");
		_attribLocationProjMtx = _shader.GetUniformLocation("ProjMtx");
		_attribLocationVtxPos = _shader.GetAttribLocation("Position");
		_attribLocationVtxUV = _shader.GetAttribLocation("UV");
		_attribLocationVtxColor = _shader.GetAttribLocation("Color");

		_vboHandle = _gl.GenBuffer();
		_elementsHandle = _gl.GenBuffer();

		// Restore modified GL state
		_gl.BindTexture(GLEnum.Texture2D, (uint)lastTexture);
		_gl.BindBuffer(GLEnum.ArrayBuffer, (uint)lastArrayBuffer);

		_gl.BindVertexArray((uint)lastVertexArray);

		_gl.CheckGlError("End of ImGui setup");
	}

	/// <summary>
	/// Frees the shader, buffers and 3D resources. ImGui's own textures are released by
	/// <see cref="ImGuiContextHost"/>, which owns them, and the GL context by its owner.
	/// </summary>
	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}

		if (_gl is not null && _shader is not null)
		{
			_gl.DeleteBuffer(_vboHandle);
			_gl.DeleteBuffer(_elementsHandle);
			_gl.DeleteVertexArray(_vertexArrayObject);

			_renderer3D?.Dispose();
			_shader.Dispose();
		}

		_disposed = true;
	}
}
