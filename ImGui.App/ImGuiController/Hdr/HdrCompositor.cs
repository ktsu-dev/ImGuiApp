// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.ImGuiController.Hdr;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Silk.NET.OpenGL;

/// <summary>
/// Draws a frame into a half-float target and hands it to an <see cref="IHdrPresenter"/>, decoded
/// to the linear values the platform's HDR presentation takes.
/// </summary>
/// <remarks>
/// <para>
/// The frame is drawn exactly as an SDR frame is, in <see cref="HdrEncoding"/>'s encoded space, so
/// blending and filtering are unchanged and the interface looks as it does in an SDR window. The
/// target is half-float only so a texture's values above 1 survive to the last pass, which decodes
/// each pixel, scales SDR white to the screen's SDR white, and limits it to the screen's peak.
/// </para>
/// <para>
/// The presenter's target is a texture another API shows, and both Direct3D and Metal put a texture's
/// first row at the top where OpenGL puts it at the bottom, so the last pass flips rows when the
/// presenter says so.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage(Justification = "Issues OpenGL calls to a live context, which unit tests do not have; the arithmetic it performs on the GPU is HdrEncoding's, which is tested.")]
internal sealed class HdrCompositor : IDisposable
{
	private const string VertexSource =
		@"#version 330
		uniform int FlipRows;
		out vec2 Frag_UV;
		void main()
		{
			vec2 corner = vec2(float((gl_VertexID << 1) & 2), float(gl_VertexID & 2));
			Frag_UV = vec2(corner.x, FlipRows != 0 ? 1.0 - corner.y : corner.y);
			gl_Position = vec4((corner * 2.0) - 1.0, 0.0, 1.0);
		}";

	private const string FragmentSource =
		@"#version 330
		in vec2 Frag_UV;
		uniform sampler2D Frame;
		uniform float Scale;
		uniform float Limit;
		layout (location = 0) out vec4 Out_Color;
		vec3 Decode(vec3 encoded)
		{
			vec3 magnitude = abs(encoded);
			vec3 linear = mix(magnitude / 12.92, pow((magnitude + 0.055) / 1.055, vec3(2.4)), step(vec3(0.04045), magnitude));
			return sign(encoded) * linear;
		}
		void main()
		{
			vec3 encoded = texture(Frame, Frag_UV).rgb;
			Out_Color = vec4(clamp(Decode(encoded) * Scale, vec3(-Limit), vec3(Limit)), 1.0);
		}";

	private readonly GL gl;
	private readonly uint program;
	private readonly int frameLocation;
	private readonly int flipLocation;
	private readonly int scaleLocation;
	private readonly int limitLocation;
	private readonly uint vertexArray;
	private uint framebuffer;
	private uint frame;
	private int width;
	private int height;
	private bool disposed;

	/// <summary>Builds the last pass's program over a context.</summary>
	/// <param name="gl">The window's context, current on this thread.</param>
	/// <param name="presenter">The platform presentation the frame goes to. It is disposed with this.</param>
	public HdrCompositor(GL gl, IHdrPresenter presenter)
	{
		this.gl = gl;
		Presenter = presenter;
		program = Compile(gl);
		frameLocation = gl.GetUniformLocation(program, "Frame");
		flipLocation = gl.GetUniformLocation(program, "FlipRows");
		scaleLocation = gl.GetUniformLocation(program, "Scale");
		limitLocation = gl.GetUniformLocation(program, "Limit");
		vertexArray = gl.GenVertexArray();
	}

	/// <summary>Gets or sets what the screen can show, which the last pass scales to.</summary>
	public HdrDisplayInfo Display { get; set; } = HdrDisplayInfo.Sdr;

	/// <summary>Gets the presenter the frame goes to.</summary>
	public IHdrPresenter Presenter { get; }

	/// <summary>Binds the half-float target for a frame of the given size and clears it.</summary>
	/// <param name="frameWidth">The frame's width in pixels.</param>
	/// <param name="frameHeight">The frame's height in pixels.</param>
	/// <param name="clear">The colour to clear to, encoded.</param>
	public void BeginFrame(int frameWidth, int frameHeight, Vector4 clear)
	{
		EnsureTarget(Math.Max(1, frameWidth), Math.Max(1, frameHeight));
		gl.BindFramebuffer(FramebufferTarget.Framebuffer, framebuffer);
		gl.Viewport(0, 0, (uint)width, (uint)height);
		gl.ClearColor(clear.X, clear.Y, clear.Z, clear.W);
		gl.Clear(ClearBufferMask.ColorBufferBit);
	}

	/// <summary>Decodes the frame into the presenter's target and presents it.</summary>
	/// <returns>Whether the frame was presented; false when the presenter couldn't take it.</returns>
	public bool EndFrame()
	{
		gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
		if (!Presenter.BeginOutput(width, height, out uint output))
		{
			return false;
		}

		(float scale, float limit) = Presenter.OutputRange(Display);
		gl.BindFramebuffer(FramebufferTarget.Framebuffer, output);
		gl.Viewport(0, 0, (uint)width, (uint)height);
		gl.Disable(EnableCap.Blend);
		gl.Disable(EnableCap.ScissorTest);
		gl.Disable(EnableCap.DepthTest);
		gl.Disable(EnableCap.CullFace);
		gl.UseProgram(program);
		gl.ActiveTexture(TextureUnit.Texture0);
		gl.BindTexture(TextureTarget.Texture2D, frame);
		gl.BindSampler(0, 0);
		gl.Uniform1(frameLocation, 0);
		gl.Uniform1(flipLocation, Presenter.FlipsVertically ? 1 : 0);
		gl.Uniform1(scaleLocation, scale);
		gl.Uniform1(limitLocation, limit);
		gl.BindVertexArray(vertexArray);
		gl.DrawArrays(PrimitiveType.Triangles, 0, 3);
		gl.BindVertexArray(0);
		gl.UseProgram(0);
		gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
		Presenter.EndOutput();
		return true;
	}

	/// <inheritdoc />
	public void Dispose()
	{
		if (disposed)
		{
			return;
		}

		disposed = true;
		gl.DeleteFramebuffer(framebuffer);
		gl.DeleteTexture(frame);
		gl.DeleteVertexArray(vertexArray);
		gl.DeleteProgram(program);
		Presenter.Dispose();
	}

	[SuppressMessage("Major Code Smell", "S6640:Make sure that using \"unsafe\" is safe here", Justification = "Allocates the target with no pixels: the pointer passed is null and nothing is read through it.")]
	private unsafe void EnsureTarget(int frameWidth, int frameHeight)
	{
		if (framebuffer != 0 && frameWidth == width && frameHeight == height)
		{
			return;
		}

		width = frameWidth;
		height = frameHeight;
		if (frame == 0)
		{
			frame = gl.GenTexture();
			framebuffer = gl.GenFramebuffer();
		}

		int previousTexture = gl.GetInteger(GetPName.TextureBinding2D);
		gl.BindTexture(TextureTarget.Texture2D, frame);
		gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba16f, (uint)width, (uint)height, 0, PixelFormat.Rgba, PixelType.HalfFloat, null);
		gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
		gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
		gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
		gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
		gl.BindTexture(TextureTarget.Texture2D, (uint)previousTexture);
		gl.BindFramebuffer(FramebufferTarget.Framebuffer, framebuffer);
		gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, frame, 0);
		gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
	}

	private static uint Compile(GL gl)
	{
		uint vertex = CompileStage(gl, ShaderType.VertexShader, VertexSource);
		uint fragment = CompileStage(gl, ShaderType.FragmentShader, FragmentSource);
		uint linked = gl.CreateProgram();
		gl.AttachShader(linked, vertex);
		gl.AttachShader(linked, fragment);
		gl.LinkProgram(linked);
		gl.DetachShader(linked, vertex);
		gl.DetachShader(linked, fragment);
		gl.DeleteShader(vertex);
		gl.DeleteShader(fragment);
		gl.GetProgram(linked, ProgramPropertyARB.LinkStatus, out int status);
		if (status == 0)
		{
			string log = gl.GetProgramInfoLog(linked);
			gl.DeleteProgram(linked);
			throw new InvalidOperationException($"The HDR pass didn't link: {log}");
		}

		return linked;
	}

	private static uint CompileStage(GL gl, ShaderType type, string source)
	{
		uint shader = gl.CreateShader(type);
		gl.ShaderSource(shader, source);
		gl.CompileShader(shader);
		gl.GetShader(shader, ShaderParameterName.CompileStatus, out int status);
		if (status == 0)
		{
			string log = gl.GetShaderInfoLog(shader);
			gl.DeleteShader(shader);
			throw new InvalidOperationException($"The HDR pass's {type} didn't compile: {log}");
		}

		return shader;
	}
}
