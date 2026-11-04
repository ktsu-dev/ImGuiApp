// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.ImGuiController;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
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
/// window's input in (#313). Every OpenGL call goes through <see cref="IGLRenderer"/>, so what this
/// decides is tested without a graphics context.
/// </remarks>
internal sealed class OpenGLRenderer : IRendererBackend, IRenderer3D, IHalfFloatTextures
{
	private static readonly int VertexStride = Unsafe.SizeOf<ImDrawVert>();

	private readonly IGLRenderer _gl;

	// The 3D path: offscreen framebuffers and a second program. Holds nothing on the GPU until a
	// target is created or something is drawn.
	private readonly GLRenderer3D _renderer3D;

	private uint _program;
	private int _attribLocationTex;
	private int _attribLocationProjMtx;
	private int _attribLocationVtxPos;
	private int _attribLocationVtxUV;
	private int _attribLocationVtxColor;
	private uint _vboHandle;
	private uint _elementsHandle;
	private uint _vertexArrayObject;
	private bool _disposed;

	/// <summary>Builds the shader program and buffers ImGui's frames are drawn with.</summary>
	/// <param name="gl">The OpenGL context to draw into. It belongs to the caller and is not disposed here.</param>
	[SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "The wrappers adapt a context this class does not own -- ImGuiApp creates and disposes it. Disposing a wrapper would tear down the caller's GL context, so they deliberately live as long as this renderer.")]
	[ExcludeFromCodeCoverage(Justification = "Wraps a live OpenGL context, which unit tests do not have; the constructor it chains to is covered against stand-ins.")]
	public OpenGLRenderer(GL gl)
		: this(new GLRendererWrapper(gl), new GLTextureUploader(new GLWrapper(gl)), new GLRenderer3D(new GL3DWrapper(gl)))
	{
	}

	/// <summary>Builds the renderer over the given OpenGL seams.</summary>
	/// <param name="gl">The calls the renderer itself issues.</param>
	/// <param name="textureUploader">Carries ImGui's own textures to the same context.</param>
	/// <param name="renderer3D">The 3D path over the same context.</param>
	internal OpenGLRenderer(IGLRenderer gl, ITextureUploader textureUploader, GLRenderer3D renderer3D)
	{
		Ensure.NotNull(gl);
		Ensure.NotNull(textureUploader);
		Ensure.NotNull(renderer3D);
		_gl = gl;
		TextureUploader = textureUploader;
		_renderer3D = renderer3D;
		CreateDeviceResources();
	}

	/// <summary>Gets the uploader carrying ImGui's own textures (the font atlas) to this renderer.</summary>
	internal ITextureUploader TextureUploader { get; }

	/// <inheritdoc />
	public nint CreateTexture(ReadOnlySpan<byte> rgba, int width, int height)
	{
		// Preserve whatever was bound — the caller may be mid-frame.
		int previousTextureId = _gl.GetInteger(GLEnum.TextureBinding2D);
		uint glTexture = _gl.CreateTexture(width, height, rgba);
		_gl.BindTexture(GLEnum.Texture2D, (uint)previousTextureId);
		return (nint)glTexture;
	}

	/// <inheritdoc />
	public bool UpdateTexture(nint id, ReadOnlySpan<byte> rgba, int width, int height)
	{
		// Preserve whatever was bound — the caller may be mid-frame.
		int previousTextureId = _gl.GetInteger(GLEnum.TextureBinding2D);
		_gl.BindTexture(GLEnum.Texture2D, (uint)id);
		_gl.ReplaceBoundTexture(width, height, rgba);
		_gl.BindTexture(GLEnum.Texture2D, (uint)previousTextureId);
		return true;
	}

	/// <inheritdoc />
	public nint CreateTexture(ReadOnlySpan<Half> rgba, int width, int height)
	{
		int previousTextureId = _gl.GetInteger(GLEnum.TextureBinding2D);
		uint glTexture = _gl.CreateHalfTexture(width, height, rgba);
		_gl.BindTexture(GLEnum.Texture2D, (uint)previousTextureId);
		return (nint)glTexture;
	}

	/// <inheritdoc />
	public bool UpdateTexture(nint id, ReadOnlySpan<Half> rgba, int width, int height)
	{
		int previousTextureId = _gl.GetInteger(GLEnum.TextureBinding2D);
		_gl.BindTexture(GLEnum.Texture2D, (uint)id);
		_gl.ReplaceBoundHalfTexture(width, height, rgba);
		_gl.BindTexture(GLEnum.Texture2D, (uint)previousTextureId);
		return true;
	}

	/// <inheritdoc />
	/// <exception cref="ArgumentException">
	/// <paramref name="id"/> is a render target. Its texture name comes from the same namespace as
	/// <see cref="CreateTexture(ReadOnlySpan{byte}, int, int)"/>'s, so deleting it here would free a live target's colour attachment;
	/// a target is released through <see cref="DeleteRenderTarget"/> only.
	/// </exception>
	public void DeleteTexture(nint id)
	{
		if (_renderer3D.IsRenderTarget(id))
		{
			throw new ArgumentException($"Texture {id} is a render target's colour attachment. Release it with DeleteRenderTarget.", nameof(id));
		}

		_gl.DeleteTexture((uint)id);
	}

	/// <inheritdoc />
	public nint CreateRenderTarget(int width, int height, bool depth) => _renderer3D.CreateRenderTarget(width, height, depth);

	/// <inheritdoc />
	public bool ResizeRenderTarget(nint target, int width, int height) => _renderer3D.ResizeRenderTarget(target, width, height);

	/// <inheritdoc />
	public void DeleteRenderTarget(nint target) => _renderer3D.DeleteRenderTarget(target);

	/// <inheritdoc />
	public nint GetTargetTexture(nint target) => _renderer3D.GetTargetTexture(target);

	/// <inheritdoc />
	public void Clear(nint target, Vector4 color, float depth) => _renderer3D.Clear(target, color, depth);

	/// <inheritdoc />
	public void Draw(nint target, ReadOnlySpan<Vertex3D> vertices, ReadOnlySpan<uint> indices, in DrawState3D state) =>
		_renderer3D.Draw(target, vertices, indices, state);

	internal void SetupRenderState(ImDrawDataPtr drawDataPtr, int framebufferWidth, int framebufferHeight)
	{
		// Set the viewport from the draw data every frame rather than relying on a resize event to
		// have set it. Some backends never raise one — a Wayland surface can be sized by the
		// compositor before the handler is attached — which would leave the viewport at whatever
		// size the drawable had when the context was created, and the UI drawn into a corner.
		_gl.Viewport(0, 0, framebufferWidth, framebufferHeight);

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
		_gl.PolygonMode(GLEnum.Fill);
		// Disable multisampling for pixel-perfect rendering
		_gl.Disable(GLEnum.Multisample);
#endif

		_gl.UseProgram(_program);
		_gl.Uniform1(_attribLocationTex, 0);
		_gl.UniformMatrix4(_attribLocationProjMtx, OrthographicProjection(drawDataPtr.DisplayPos, drawDataPtr.DisplaySize));
		_gl.CheckError("Projection");

		_gl.BindSampler(0, 0);

		// Recreate the VAO every time (this is to easily allow multiple GL contexts to be rendered to. VAO are not shared among GL contexts)
		// The renderer would actually work without any VAO bound, but then our VertexAttrib calls would overwrite the default one currently bound.
		_vertexArrayObject = _gl.GenVertexArray();
		_gl.BindVertexArray(_vertexArrayObject);
		_gl.CheckError("VAO");

		// Bind vertex/index buffers and setup attributes for ImDrawVert
		_gl.BindBuffer(GLEnum.ArrayBuffer, _vboHandle);
		_gl.BindBuffer(GLEnum.ElementArrayBuffer, _elementsHandle);
		_gl.EnableVertexAttribArray((uint)_attribLocationVtxPos);
		_gl.EnableVertexAttribArray((uint)_attribLocationVtxUV);
		_gl.EnableVertexAttribArray((uint)_attribLocationVtxColor);
		_gl.VertexAttribPointer((uint)_attribLocationVtxPos, 2, GLEnum.Float, false, (uint)VertexStride, 0);
		_gl.VertexAttribPointer((uint)_attribLocationVtxUV, 2, GLEnum.Float, false, (uint)VertexStride, 8);
		_gl.VertexAttribPointer((uint)_attribLocationVtxColor, 4, GLEnum.UnsignedByte, true, (uint)VertexStride, 16);
	}

	/// <summary>
	/// The column-major orthographic projection mapping ImGui's display rectangle onto clip space,
	/// with the first row of the display at the top.
	/// </summary>
	internal static float[] OrthographicProjection(Vector2 displayPos, Vector2 displaySize)
	{
		// Double precision for the divisions, so the projection is exact at large display sizes.
		double left = displayPos.X;
		double right = displayPos.X + displaySize.X;
		double top = displayPos.Y;
		double bottom = displayPos.Y + displaySize.Y;

		return [
			(float)(2.0 / (right - left)), 0.0f, 0.0f, 0.0f,
			0.0f, (float)(2.0 / (top - bottom)), 0.0f, 0.0f,
			0.0f, 0.0f, -1.0f, 0.0f,
			(float)((right + left) / (left - right)), (float)((top + bottom) / (bottom - top)), 0.0f, 1.0f,
		];
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
		int framebufferWidth = (int)(drawDataPtr.DisplaySize.X * drawDataPtr.FramebufferScale.X);
		int framebufferHeight = (int)(drawDataPtr.DisplaySize.Y * drawDataPtr.FramebufferScale.Y);
		if (framebufferWidth <= 0 || framebufferHeight <= 0)
		{
			return;
		}

		// Backup GL state
		int lastActiveTexture = _gl.GetInteger(GLEnum.ActiveTexture);
		_gl.ActiveTexture(GLEnum.Texture0);

		int lastProgram = _gl.GetInteger(GLEnum.CurrentProgram);
		int lastTexture = _gl.GetInteger(GLEnum.TextureBinding2D);
		int lastSampler = _gl.GetInteger(GLEnum.SamplerBinding);
		int lastArrayBuffer = _gl.GetInteger(GLEnum.ArrayBufferBinding);
		int lastVertexArrayObject = _gl.GetInteger(GLEnum.VertexArrayBinding);

#if !GLES
		Span<int> lastPolygonMode = stackalloc int[2];
		_gl.GetInteger(GLEnum.PolygonMode, lastPolygonMode);
#endif

		Span<int> lastScissorBox = stackalloc int[4];
		_gl.GetInteger(GLEnum.ScissorBox, lastScissorBox);

		int lastBlendSrcRgb = _gl.GetInteger(GLEnum.BlendSrcRgb);
		int lastBlendDstRgb = _gl.GetInteger(GLEnum.BlendDstRgb);
		int lastBlendSrcAlpha = _gl.GetInteger(GLEnum.BlendSrcAlpha);
		int lastBlendDstAlpha = _gl.GetInteger(GLEnum.BlendDstAlpha);
		int lastBlendEquationRgb = _gl.GetInteger(GLEnum.BlendEquationRgb);
		int lastBlendEquationAlpha = _gl.GetInteger(GLEnum.BlendEquationAlpha);

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
		_gl.PolygonMode((GLEnum)lastPolygonMode[0]);
#endif

		_gl.Scissor(lastScissorBox[0], lastScissorBox[1], (uint)lastScissorBox[2], (uint)lastScissorBox[3]);
	}

	/// <summary>
	/// Renders all ImGui draw command lists to the current framebuffer.
	/// </summary>
	[SuppressMessage("Major Code Smell", "S6640:Make sure that using \"unsafe\" is safe here", Justification = "ImGui exposes its vertex and index buffers as raw pointers; they are passed to OpenGL for the upload only.")]
	private unsafe void RenderCommandLists(ImDrawDataPtr drawDataPtr, int framebufferWidth, int framebufferHeight, Vector2 clipOff, Vector2 clipScale)
	{
		for (int n = 0; n < drawDataPtr.CmdListsCount; n++)
		{
			ImDrawListPtr cmdListPtr = drawDataPtr.CmdLists[n];

			// Upload vertex/index buffers
			_gl.BufferData(GLEnum.ArrayBuffer, cmdListPtr.VtxBuffer.Size * VertexStride, (nint)cmdListPtr.VtxBuffer.Data);
			_gl.CheckError($"Data Vert {n}");
			_gl.BufferData(GLEnum.ElementArrayBuffer, cmdListPtr.IdxBuffer.Size * sizeof(ushort), (nint)cmdListPtr.IdxBuffer.Data);
			_gl.CheckError($"Data Idx {n}");

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
	[SuppressMessage("Major Code Smell", "S6640:Make sure that using \"unsafe\" is safe here", Justification = "A draw-command callback is an unmanaged function pointer ImGui hands over; it is called with this command's own draw list and command, and nothing is retained.")]
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

			_gl.Scissor(scissorX, scissorY, scissorWidth, scissorHeight);
			_gl.CheckError("Scissor");

			// GetTexID returns the texture name ImGui was given for this command's texture.
			uint textureId = (uint)(nuint)cmdPtr.GetTexID();
			_gl.BindTexture(GLEnum.Texture2D, textureId);
			_gl.CheckError("Texture");

			_gl.DrawElementsBaseVertex(cmdPtr.ElemCount, (nint)(cmdPtr.IdxOffset * sizeof(ushort)), (int)cmdPtr.VtxOffset);
			_gl.CheckError("Draw");
		}
	}

	/// <summary>
	/// Enables or disables an OpenGL capability based on the previously captured state.
	/// </summary>
	private void SetGlCapability(GLEnum capability, bool enabled)
	{
		if (enabled)
		{
			_gl.Enable(capability);
		}
		else
		{
			_gl.Disable(capability);
		}
	}

	private void CreateDeviceResources()
	{
		// Backup GL state
		int lastTexture = _gl.GetInteger(GLEnum.TextureBinding2D);
		int lastArrayBuffer = _gl.GetInteger(GLEnum.ArrayBufferBinding);
		int lastVertexArray = _gl.GetInteger(GLEnum.VertexArrayBinding);

		const string VertexSource =
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

		const string FragmentSource =
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

		_program = _gl.CreateProgram(VertexSource, FragmentSource);

		_attribLocationTex = _gl.GetUniformLocation(_program, "Texture");
		_attribLocationProjMtx = _gl.GetUniformLocation(_program, "ProjMtx");
		_attribLocationVtxPos = _gl.GetAttribLocation(_program, "Position");
		_attribLocationVtxUV = _gl.GetAttribLocation(_program, "UV");
		_attribLocationVtxColor = _gl.GetAttribLocation(_program, "Color");

		_vboHandle = _gl.GenBuffer();
		_elementsHandle = _gl.GenBuffer();

		// Restore modified GL state
		_gl.BindTexture(GLEnum.Texture2D, (uint)lastTexture);
		_gl.BindBuffer(GLEnum.ArrayBuffer, (uint)lastArrayBuffer);
		_gl.BindVertexArray((uint)lastVertexArray);

		_gl.CheckError("End of ImGui setup");
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

		_gl.DeleteBuffer(_vboHandle);
		_gl.DeleteBuffer(_elementsHandle);
		_gl.DeleteVertexArray(_vertexArrayObject);
		_renderer3D.Dispose();
		_gl.DeleteProgram(_program);

		_disposed = true;
	}
}
