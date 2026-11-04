// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Tests;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.App.ImGuiController;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Silk.NET.OpenGL;

/// <summary>
/// Drives <see cref="OpenGLRenderer"/> over a stand-in for OpenGL, with real draw data from an
/// <see cref="ImGuiContextHost"/>: what it saves and puts back, what it draws, and which textures it
/// will and will not touch.
/// </summary>
[TestClass]
public sealed class OpenGLRendererTests
{
	private const int Width = 200;
	private const int Height = 120;

	[SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "The renderer owns the 3D renderer and disposes it with itself.")]
	private static OpenGLRenderer RendererOver(FakeGLRenderer gl) => new(gl, new NullTextureUploader(), new GLRenderer3D(new GLRenderer3DTests.FakeGL3D()));

	[TestMethod]
	public void Constructor_BuildsTheProgram_AndPutsTheBindingsBack()
	{
		FakeGLRenderer gl = new();
		gl.Integers[GLEnum.TextureBinding2D] = 42;
		gl.Integers[GLEnum.ArrayBufferBinding] = 43;
		gl.Integers[GLEnum.VertexArrayBinding] = 44;

		using OpenGLRenderer renderer = RendererOver(gl);

		CollectionAssert.Contains(gl.Calls, "CreateProgram");
		Assert.AreEqual(42, gl.GetInteger(GLEnum.TextureBinding2D));
		Assert.AreEqual(43, gl.GetInteger(GLEnum.ArrayBufferBinding));
		Assert.AreEqual(44, gl.GetInteger(GLEnum.VertexArrayBinding));
	}

	[TestMethod]
	public void Constructor_NullArguments_Throw()
	{
		using GLRenderer3D renderer3D = new(new GLRenderer3DTests.FakeGL3D());
		Assert.ThrowsExactly<ArgumentNullException>(() => new OpenGLRenderer(null!, new NullTextureUploader(), renderer3D));
		Assert.ThrowsExactly<ArgumentNullException>(() => new OpenGLRenderer(new FakeGLRenderer(), null!, renderer3D));
		Assert.ThrowsExactly<ArgumentNullException>(() => new OpenGLRenderer(new FakeGLRenderer(), new NullTextureUploader(), null!));
	}

	[TestMethod]
	public void CreateTexture_UploadsThePixels_AndKeepsTheCallersBinding()
	{
		FakeGLRenderer gl = new();
		using OpenGLRenderer renderer = RendererOver(gl);
		gl.Integers[GLEnum.TextureBinding2D] = 9;

		nint id = renderer.CreateTexture(new byte[4 * 3 * 2], 3, 2);

		Assert.AreEqual((uint)id, gl.TextureWrites.Single().Texture);
		Assert.AreEqual((3, 2, 24), (gl.TextureWrites[0].Width, gl.TextureWrites[0].Height, gl.TextureWrites[0].Bytes));
		Assert.AreEqual(9, gl.GetInteger(GLEnum.TextureBinding2D), "The caller may be mid-frame, so its binding must survive.");
	}

	[TestMethod]
	public void UpdateTexture_WritesTheNamedTexture_AndKeepsTheCallersBinding()
	{
		FakeGLRenderer gl = new();
		using OpenGLRenderer renderer = RendererOver(gl);
		gl.Integers[GLEnum.TextureBinding2D] = 9;

		Assert.IsTrue(renderer.UpdateTexture(55, new byte[16], 2, 2));

		Assert.AreEqual((55u, 2, 2, 16), gl.TextureWrites.Single());
		Assert.AreEqual(9, gl.GetInteger(GLEnum.TextureBinding2D));
	}

	[TestMethod]
	public void DeleteTexture_DeletesAPlainTexture()
	{
		FakeGLRenderer gl = new();
		using OpenGLRenderer renderer = RendererOver(gl);

		renderer.DeleteTexture(5);

		CollectionAssert.Contains(gl.Calls, "DeleteTexture(5)");
	}

	[TestMethod]
	public void RenderDrawData_DrawsTheFrame_AndPutsEveryStateBack()
	{
		FakeGLRenderer gl = new();
		using OpenGLRenderer renderer = RendererOver(gl);
		using ImGuiContextHost host = new(new NullTextureUploader(), ImGuiAppExtensions.None);
		ImDrawDataPtr drawData = Frame(host, _ => { });

		// Deliberately unusual state, so a default left behind would show.
		gl.Integers[GLEnum.ActiveTexture] = (int)GLEnum.Texture3;
		gl.Integers[GLEnum.CurrentProgram] = 3;
		gl.Integers[GLEnum.TextureBinding2D] = 9;
		gl.Integers[GLEnum.SamplerBinding] = 4;
		gl.Integers[GLEnum.ArrayBufferBinding] = 5;
		gl.Integers[GLEnum.VertexArrayBinding] = 6;
		gl.BlendEquationSeparate(GLEnum.FuncSubtract, GLEnum.Max);
		gl.BlendFuncSeparate(GLEnum.One, GLEnum.Zero, GLEnum.DstColor, GLEnum.SrcColor);
		gl.Enable(GLEnum.CullFace);
		gl.Enable(GLEnum.DepthTest);
		gl.Enable(GLEnum.StencilTest);
		gl.Enable(GLEnum.PrimitiveRestart);
		gl.Scissor(1, 2, 3, 4);
		gl.PolygonMode(GLEnum.Line);
		string before = State(gl);

		renderer.RenderDrawData(drawData);

		Assert.IsNotEmpty(gl.Draws, "A window with text should have been drawn.");
		Assert.IsTrue(gl.Draws.All(d => d.Texture == 1), "Every command samples the atlas, which the uploader named 1.");
		Assert.AreEqual(0, gl.Draws[0].IndexOffset);
		CollectionAssert.Contains(gl.Calls, $"Viewport(0, 0, {Width}, {Height})");
		Assert.AreEqual(State(gl), before, "Everything the frame changed should be put back.");
	}

	[TestMethod]
	public void RenderDrawData_UploadsEachListsVerticesAndIndices()
	{
		FakeGLRenderer gl = new();
		using OpenGLRenderer renderer = RendererOver(gl);
		using ImGuiContextHost host = new(new NullTextureUploader(), ImGuiAppExtensions.None);
		ImDrawDataPtr drawData = Frame(host, _ => { });

		renderer.RenderDrawData(drawData);

		ImDrawListPtr list = drawData.CmdLists[0];
		Assert.AreEqual((GLEnum.ArrayBuffer, list.VtxBuffer.Size * 20), gl.Uploads[0], "An ImDrawVert is 20 bytes.");
		Assert.AreEqual((GLEnum.ElementArrayBuffer, list.IdxBuffer.Size * sizeof(ushort)), gl.Uploads[1]);
		CollectionAssert.Contains(gl.Calls, "VertexAttribPointer(2, 4, UnsignedByte, True, 20, 16)");
	}

	[TestMethod]
	public void RenderDrawData_SkipsACommandClippedOffTheFramebuffer()
	{
		FakeGLRenderer gl = new();
		using OpenGLRenderer renderer = RendererOver(gl);
		using ImGuiContextHost host = new(new NullTextureUploader(), ImGuiAppExtensions.None);
		ImDrawDataPtr onScreen = Frame(host, _ => { });
		renderer.RenderDrawData(onScreen);
		int visible = gl.Draws.Count;
		gl.Draws.Clear();

		ImDrawDataPtr withOffscreen = Frame(host, list =>
		{
			list.PushClipRect(new Vector2(-50, -50), new Vector2(-10, -10));
			list.AddRectFilled(new Vector2(-40, -40), new Vector2(-20, -20), 0xFFFFFFFF);
			list.PopClipRect();
		});
		renderer.RenderDrawData(withOffscreen);

		Assert.AreEqual(visible, gl.Draws.Count, "A command whose clip rectangle is off the framebuffer should not be drawn.");
	}

	[TestMethod]
	public void RenderDrawData_ScissorsInFramebufferPixels_FromTheBottom()
	{
		FakeGLRenderer gl = new();
		using OpenGLRenderer renderer = RendererOver(gl);
		using ImGuiContextHost host = new(new NullTextureUploader(), ImGuiAppExtensions.None);
		ImDrawDataPtr drawData = Frame(host, list =>
		{
			list.PushClipRect(new Vector2(10, 20), new Vector2(50, 40));
			list.AddRectFilled(new Vector2(10, 20), new Vector2(50, 40), 0xFFFFFFFF);
			list.PopClipRect();
		});

		renderer.RenderDrawData(drawData);

		Assert.IsTrue(
			gl.Draws.Any(d => (d.X, d.Y, d.Width, d.Height) == (10, Height - 40, 40u, 20u)),
			"OpenGL's scissor origin is the bottom-left, so y is measured up from the framebuffer's bottom edge.");
	}

	[TestMethod]
	public void RenderDrawData_RunsCallbacks_AndResetsStateOnTheSentinel()
	{
		FakeGLRenderer gl = new();
		using OpenGLRenderer renderer = RendererOver(gl);
		using ImGuiContextHost host = new(new NullTextureUploader(), ImGuiAppExtensions.None);
		ImDrawDataPtr drawData = Frame(host, list =>
		{
			// The first is run as a callback. With no application running it changes nothing.
			ImGuiApp.SetDrawBlendMode(list, ImGuiAppBlendMode.Additive);
			list.AddRectFilled(new Vector2(0, 0), new Vector2(5, 5), 0xFFFFFFFF);
			ImGuiApp.SetDrawBlendMode(list, ImGuiAppBlendMode.AlphaBlend);
			list.AddRectFilled(new Vector2(5, 5), new Vector2(9, 9), 0xFFFFFFFF);
		});
		MarkLastCallbackAsResetRenderState(drawData);

		renderer.RenderDrawData(drawData);

		Assert.AreEqual(2, gl.Calls.Count(c => c == $"Viewport(0, 0, {Width}, {Height})"), "The reset sentinel should set the render state up again.");
		Assert.IsNotEmpty(gl.Draws);
	}

	[TestMethod]
	public void RenderDrawData_WithNoFramebuffer_DoesNothing()
	{
		FakeGLRenderer gl = new();
		using OpenGLRenderer renderer = RendererOver(gl);
		using ImGuiContextHost host = new(new NullTextureUploader(), ImGuiAppExtensions.None);
		host.NewFrame(Vector2.Zero, Vector2.One, 1f / 60f);
		ImDrawDataPtr drawData = host.EndFrame();
		gl.Calls.Clear();

		renderer.RenderDrawData(drawData);

		Assert.IsEmpty(gl.Calls, "A minimised window has nothing to draw into.");
	}

	[TestMethod]
	public void SetBlendMode_SwitchesBetweenAdditiveAndAlpha()
	{
		FakeGLRenderer gl = new();
		using OpenGLRenderer renderer = RendererOver(gl);
		gl.Calls.Clear();

		renderer.SetBlendMode(ImGuiAppBlendMode.Additive);
		renderer.SetBlendMode(ImGuiAppBlendMode.AlphaBlend);

		// Formatted the way the stand-in formats them, since several GLEnum names share a value.
		CollectionAssert.AreEqual(
			new List<string>
			{
				$"BlendFuncSeparate({GLEnum.SrcAlpha}, {GLEnum.One}, {GLEnum.One}, {GLEnum.One})",
				$"BlendFuncSeparate({GLEnum.SrcAlpha}, {GLEnum.OneMinusSrcAlpha}, {GLEnum.One}, {GLEnum.OneMinusSrcAlpha})",
			},
			gl.Calls);
	}

	[TestMethod]
	public void OrthographicProjection_MapsTheDisplayCornersOntoClipSpace()
	{
		float[] m = OpenGLRenderer.OrthographicProjection(new Vector2(10, 20), new Vector2(200, 100));
		Matrix4x4 projection = new(
			m[0], m[1], m[2], m[3],
			m[4], m[5], m[6], m[7],
			m[8], m[9], m[10], m[11],
			m[12], m[13], m[14], m[15]);

		AssertNear(new Vector2(-1, 1), Project(new Vector2(10, 20)), "The top-left of the display is the top-left of clip space.");
		AssertNear(new Vector2(1, -1), Project(new Vector2(210, 120)), "The bottom-right of the display is the bottom-right of clip space.");

		static void AssertNear(Vector2 expected, Vector2 actual, string message)
		{
			Assert.AreEqual(expected.X, actual.X, 1e-6f, message);
			Assert.AreEqual(expected.Y, actual.Y, 1e-6f, message);
		}

		Vector2 Project(Vector2 point)
		{
			Vector4 clip = Vector4.Transform(new Vector4(point, 0, 1), projection);
			return new Vector2(clip.X, clip.Y);
		}
	}

	[TestMethod]
	public void Dispose_FreesTheBuffersAndProgram_Once()
	{
		FakeGLRenderer gl = new();
		OpenGLRenderer renderer = RendererOver(gl);
		gl.Calls.Clear();

		renderer.Dispose();
		renderer.Dispose();

		Assert.AreEqual(2, gl.Calls.Count(c => c.StartsWith("DeleteBuffer", StringComparison.Ordinal)));
		Assert.AreEqual(1, gl.Calls.Count(c => c == $"DeleteProgram({FakeGLRenderer.Program})"));
	}

	private static ImDrawDataPtr Frame(ImGuiContextHost host, Action<ImDrawListPtr> drawExtra)
	{
		host.NewFrame(new Vector2(Width, Height), Vector2.One, 1f / 60f);
		ImGui.SetNextWindowPos(Vector2.Zero);
		ImGui.SetNextWindowSize(new Vector2(150, 80));
		ImGui.Begin("probe", ImGuiWindowFlags.NoSavedSettings);
		ImGui.TextUnformatted("hello");
		drawExtra(ImGui.GetWindowDrawList());
		ImGui.End();
		return host.EndFrame();
	}

	/// <summary>
	/// Turns the last callback in the frame into ImGui's reset-render-state sentinel, which can only
	/// be recorded through a native call that takes the sentinel as a function pointer.
	/// </summary>
	private static unsafe void MarkLastCallbackAsResetRenderState(ImDrawDataPtr drawData)
	{
		for (int n = drawData.CmdListsCount - 1; n >= 0; n--)
		{
			ImDrawListPtr list = drawData.CmdLists[n];
			for (int i = list.CmdBuffer.Size - 1; i >= 0; i--)
			{
				if (list.CmdBuffer.Data[i].UserCallback != null)
				{
					list.CmdBuffer.Data[i].UserCallback = (void*)ImGui.ImDrawCallbackResetRenderState;
					return;
				}
			}
		}

		Assert.Fail("The frame recorded no callback to mark.");
	}

	private static string State(FakeGLRenderer gl) =>
		string.Join(", ", gl.Integers.OrderBy(p => p.Key).Select(p => $"{p.Key}={p.Value}")) +
		" | " + string.Join(", ", gl.Enabled.Order()) +
		" | " + string.Join(", ", gl.ScissorBox) +
		" | " + string.Join(", ", gl.PolygonModes);
}
