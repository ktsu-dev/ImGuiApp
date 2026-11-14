// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Tests;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using ktsu.ImGui.App.ImGuiController;

using ktsu.ImGui.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Silk.NET.OpenGL;

/// <summary>
/// Covers the OpenGL <see cref="IRenderer3D"/> against a recording, stateful stand-in for the
/// context, so the objects it allocates, the guards it enforces and the state it puts back are
/// checked without a GPU. Whether the pixels are right is the software renderer's suite's job.
/// </summary>
[TestClass]
public sealed class GLRenderer3DTests
{
	private static readonly Vertex3D[] Triangle =
	[
		new(new Vector3(-1f, -1f, 0.5f), Vector2.Zero, 0xFF0000FF),
		new(new Vector3(1f, -1f, 0.5f), Vector2.UnitX, 0xFF00FF00),
		new(new Vector3(0f, 1f, 0.5f), Vector2.One, 0xFFFF0000),
	];

	private static readonly uint[] TriangleIndices = [0, 1, 2];

	[TestMethod]
	public void CreateRenderTarget_WithDepth_AttachesAColourTextureAndADepthRenderbuffer()
	{
		FakeGL3D gl = new();
		using GLRenderer3D renderer = new(gl);

		nint target = renderer.CreateRenderTarget(64, 32, depth: true);

		Assert.AreEqual((nint)FakeGL3D.FirstTexture, target, "The handle is the colour texture's name.");
		Assert.AreEqual(target, renderer.GetTargetTexture(target));
		CollectionAssert.Contains(gl.Calls, "TexImage2D(Rgba8,64x32)");
		CollectionAssert.Contains(gl.Calls, "RenderbufferStorage(DepthComponent24,64x32)");
		CollectionAssert.Contains(gl.Calls, $"FramebufferTexture2D(ColorAttachment0,{FakeGL3D.FirstTexture})");
		CollectionAssert.Contains(gl.Calls, $"FramebufferRenderbuffer(DepthAttachment,{FakeGL3D.FirstRenderbuffer})");
	}

	[TestMethod]
	public void CreateRenderTarget_WithoutDepth_AllocatesNoRenderbuffer()
	{
		FakeGL3D gl = new();
		using GLRenderer3D renderer = new(gl);

		renderer.CreateRenderTarget(16, 16, depth: false);

		Assert.IsFalse(gl.Calls.Any(c => c.StartsWith("GenRenderbuffer", StringComparison.Ordinal)));
		Assert.IsFalse(gl.Calls.Any(c => c.StartsWith("FramebufferRenderbuffer", StringComparison.Ordinal)));
	}

	[TestMethod]
	public void CreateRenderTarget_LeavesTheBindingsItFoundInPlace()
	{
		FakeGL3D gl = FakeGL3D.WithUnusualState();
		using GLRenderer3D renderer = new(gl);
		string before = gl.Snapshot();

		renderer.CreateRenderTarget(16, 16, depth: true);

		Assert.AreEqual(before, gl.Snapshot());
	}

	[TestMethod]
	public void CreateRenderTarget_WhenTheFramebufferIsIncomplete_ReleasesWhatItMadeAndThrows()
	{
		FakeGL3D gl = new() { FramebufferStatus = GLEnum.FramebufferUnsupported };
		using GLRenderer3D renderer = new(gl);

		Assert.ThrowsExactly<InvalidOperationException>(() => renderer.CreateRenderTarget(16, 16, depth: true));

		CollectionAssert.Contains(gl.Calls, $"DeleteTexture({FakeGL3D.FirstTexture})");
		CollectionAssert.Contains(gl.Calls, $"DeleteRenderbuffer({FakeGL3D.FirstRenderbuffer})");
		CollectionAssert.Contains(gl.Calls, $"DeleteFramebuffer({FakeGL3D.FirstFramebuffer})");
		Assert.IsFalse(renderer.IsRenderTarget((nint)FakeGL3D.FirstTexture));
	}

	[TestMethod]
	public void CreateRenderTarget_RejectsAnEmptySize()
	{
		using GLRenderer3D renderer = new(new FakeGL3D());

		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => renderer.CreateRenderTarget(0, 16, depth: false));
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => renderer.CreateRenderTarget(16, -1, depth: false));
	}

	[TestMethod]
	public void DeleteRenderTarget_ReleasesTheFramebufferAndBothAttachments()
	{
		FakeGL3D gl = new();
		using GLRenderer3D renderer = new(gl);
		nint target = renderer.CreateRenderTarget(16, 16, depth: true);
		gl.Calls.Clear();

		renderer.DeleteRenderTarget(target);

		CollectionAssert.AreEquivalent(
			new[]
			{
				$"DeleteFramebuffer({FakeGL3D.FirstFramebuffer})",
				$"DeleteRenderbuffer({FakeGL3D.FirstRenderbuffer})",
				$"DeleteTexture({FakeGL3D.FirstTexture})",
			},
			gl.Calls.ToArray());
		Assert.ThrowsExactly<ArgumentException>(() => renderer.GetTargetTexture(target), "A deleted target is no longer a target.");
	}

	[TestMethod]
	public void DeleteRenderTarget_RejectsAPlainTextureAndDeletesNothing()
	{
		FakeGL3D gl = new();
		using GLRenderer3D renderer = new(gl);
		renderer.CreateRenderTarget(16, 16, depth: true);
		gl.Calls.Clear();

		// 5 is what CreateTexture might have handed out; on OpenGL it is the same kind of name.
		Assert.ThrowsExactly<ArgumentException>(() => renderer.DeleteRenderTarget(5));

		Assert.IsEmpty(gl.Calls, "Refusing must not free anything on the way out.");
	}

	[TestMethod]
	public void DeleteTexture_OnTheOpenGLRenderer_RejectsARenderTarget()
	{
		FakeGL3D gl = new();
		using OpenGLRenderer controller = ControllerOver(gl);
		nint target = controller.CreateRenderTarget(16, 16, depth: true);
		gl.Calls.Clear();

		Assert.ThrowsExactly<ArgumentException>(() => controller.DeleteTexture(controller.GetTargetTexture(target)));

		Assert.IsEmpty(gl.Calls, "A live target's colour attachment must survive the attempt.");
		Assert.AreEqual(target, controller.GetTargetTexture(target), "The target should still be live.");
	}

	[TestMethod]
	public void DeleteTexture_OnTheOpenGLRenderer_StillAcceptsAPlainTexture()
	{
		using OpenGLRenderer controller = ControllerOver(new FakeGL3D());
		controller.CreateRenderTarget(16, 16, depth: false);

		// What matters is that the guard does not mistake an ordinary texture for a target.
		controller.DeleteTexture(5);
	}

	[TestMethod]
	public void OpenGLRenderer_ForwardsEveryRenderer3DCallToGLRenderer3D()
	{
		FakeGL3D gl = new();
		using OpenGLRenderer renderer = ControllerOver(gl);
		IRenderer3D controller = renderer;

		nint target = controller.CreateRenderTarget(16, 16, depth: true);
		Assert.IsTrue(controller.ResizeRenderTarget(target, 32, 32));
		controller.Clear(target, Vector4.One, 1f);
		controller.Draw(target, Triangle, TriangleIndices, default);
		Assert.AreEqual(target, controller.GetTargetTexture(target));
		controller.DeleteRenderTarget(target);

		Assert.HasCount(1, gl.Clears);
		Assert.HasCount(1, gl.Draws);
		CollectionAssert.Contains(gl.Calls, $"DeleteFramebuffer({FakeGL3D.FirstFramebuffer})");
	}

	[TestMethod]
	public void OpenGLRenderer_ForwardsTheHighRangeMembersRatherThanLeavingThemToTheDefaults()
	{
		// IRenderer3D's defaults for these refuse, so a missing forward would silently report no
		// float targets on the one backend that has them.
		FakeGL3D gl = new();
		using OpenGLRenderer renderer = ControllerOver(gl);
		IRenderer3D controller = renderer;

		Assert.IsTrue(controller.SupportsFormat(RenderTargetFormat.Rgba16F));
		nint hdr = controller.CreateRenderTarget(16, 16, depth: true, RenderTargetFormat.Rgba16F);
		nint shown = controller.CreateRenderTarget(16, 16, depth: false);
		controller.ToneMap(hdr, shown, ToneMap3D.Default);

		CollectionAssert.Contains(gl.Calls, "TexImage2D(Rgba16f,16x16)");
		Assert.HasCount(1, gl.Draws);
	}

	[TestMethod]
	public void CreateRenderTarget_Rgba16F_AllocatesAFloatColourTexture()
	{
		FakeGL3D gl = new();
		using GLRenderer3D renderer = new(gl);

		Assert.IsTrue(renderer.SupportsFormat(RenderTargetFormat.Rgba16F));
		renderer.CreateRenderTarget(64, 32, depth: true, RenderTargetFormat.Rgba16F);

		CollectionAssert.Contains(gl.Calls, "TexImage2D(Rgba16f,64x32)");
		CollectionAssert.Contains(gl.Calls, "RenderbufferStorage(DepthComponent24,64x32)");
	}

	[TestMethod]
	public void ResizeRenderTarget_KeepsAFloatTargetsFormat()
	{
		FakeGL3D gl = new();
		using GLRenderer3D renderer = new(gl);
		nint target = renderer.CreateRenderTarget(16, 16, depth: false, RenderTargetFormat.Rgba16F);
		gl.Calls.Clear();

		Assert.IsTrue(renderer.ResizeRenderTarget(target, 40, 20));

		CollectionAssert.Contains(gl.Calls, "TexImage2D(Rgba16f,40x20)");
	}

	[TestMethod]
	public void Draw_Additive_AddsTheSourceScaledByItsAlpha()
	{
		FakeGL3D gl = new();
		using GLRenderer3D renderer = new(gl);
		nint target = renderer.CreateRenderTarget(8, 8, depth: false, RenderTargetFormat.Rgba16F);

		renderer.Draw(target, Triangle, TriangleIndices, new DrawState3D { Blend = BlendMode.Additive });

		FakeGL3D.DrawRecord draw = gl.Draws.Single();
		Assert.IsTrue(draw.Enabled.Contains(GLEnum.Blend));
		Assert.AreEqual(GLEnum.SrcAlpha, draw.BlendSrcRgb);
		Assert.AreEqual(GLEnum.One, draw.BlendDstRgb, "Additive keeps the whole destination.");
	}

	[TestMethod]
	public void ToneMap_DrawsOneTriangleIntoTheDestinationReadingTheSource()
	{
		FakeGL3D gl = FakeGL3D.WithUnusualState();
		using GLRenderer3D renderer = new(gl);
		nint hdr = renderer.CreateRenderTarget(32, 24, depth: true, RenderTargetFormat.Rgba16F);
		nint shown = renderer.CreateRenderTarget(32, 24, depth: false);

		renderer.ToneMap(hdr, shown, new ToneMap3D(0.5f, 0.8f, ToneCurve.Exponential));

		FakeGL3D.DrawRecord draw = gl.Draws.Single();
		Assert.AreEqual(GLEnum.Triangles, draw.Mode);
		Assert.AreEqual(3, draw.Count, "One triangle covers the viewport.");
		Assert.AreEqual((int)FakeGL3D.FirstFramebuffer + 1, draw.Framebuffer, "It draws into the destination's framebuffer.");
		Assert.AreEqual((int)hdr, draw.Texture, "It samples the source's colour texture.");
		Assert.AreEqual("0,0,32,24", draw.Viewport);
		Assert.IsFalse(draw.Enabled.Contains(GLEnum.Blend), "The destination is replaced, not blended into.");
		Assert.IsFalse(draw.Enabled.Contains(GLEnum.DepthTest));
		Assert.IsFalse(draw.Enabled.Contains(GLEnum.ScissorTest));
		CollectionAssert.Contains(gl.Uniforms.Values.ToArray(), 0.5f);
		CollectionAssert.Contains(gl.Uniforms.Values.ToArray(), 0.8f);
		CollectionAssert.Contains(gl.Uniforms.Values.ToArray(), (int)ToneCurve.Exponential);
	}

	[TestMethod]
	public void ToneMap_PassesPreserveHueToTheShader()
	{
		FakeGL3D gl = new();
		using GLRenderer3D renderer = new(gl);
		nint hdr = renderer.CreateRenderTarget(8, 8, depth: false, RenderTargetFormat.Rgba16F);
		nint shown = renderer.CreateRenderTarget(8, 8, depth: false);

		renderer.ToneMap(hdr, shown, new ToneMap3D(1f, 1f, ToneCurve.Clamp, PreserveHue: true));
		int location = gl.GetUniformLocation(0, "PreserveHue");
		Assert.AreEqual(1, gl.Uniforms[location]);

		renderer.ToneMap(hdr, shown, ToneMap3D.Default);
		Assert.AreEqual(0, gl.Uniforms[location]);
	}

	[TestMethod]
	public void ToneMap_PutsBackEveryPieceOfStateItTouched()
	{
		FakeGL3D gl = FakeGL3D.WithUnusualState();
		using GLRenderer3D renderer = new(gl);
		nint hdr = renderer.CreateRenderTarget(16, 16, depth: false, RenderTargetFormat.Rgba16F);
		nint shown = renderer.CreateRenderTarget(16, 16, depth: false);
		string before = gl.Snapshot();

		renderer.ToneMap(hdr, shown, ToneMap3D.Default);

		Assert.AreEqual(before, gl.Snapshot());
	}

	[TestMethod]
	public void ToneMap_CompilesItsProgramOnceAndOnlyWhenFirstNeeded()
	{
		FakeGL3D gl = new();
		using GLRenderer3D renderer = new(gl);
		nint hdr = renderer.CreateRenderTarget(16, 16, depth: false, RenderTargetFormat.Rgba16F);
		nint shown = renderer.CreateRenderTarget(16, 16, depth: false);
		Assert.AreEqual(0, gl.ProgramsCreated);

		renderer.ToneMap(hdr, shown, ToneMap3D.Default);
		renderer.ToneMap(hdr, shown, ToneMap3D.Default);

		Assert.AreEqual(1, gl.ProgramsCreated);
	}

	[TestMethod]
	public void ToneMap_IntoItselfOrAnUnknownTarget_IsRejected()
	{
		FakeGL3D gl = new();
		using GLRenderer3D renderer = new(gl);
		nint hdr = renderer.CreateRenderTarget(16, 16, depth: false, RenderTargetFormat.Rgba16F);

		Assert.ThrowsExactly<ArgumentException>(() => renderer.ToneMap(hdr, hdr, ToneMap3D.Default));
		Assert.ThrowsExactly<ArgumentException>(() => renderer.ToneMap(hdr, 5, ToneMap3D.Default));
		Assert.IsEmpty(gl.Draws);
	}

	[TestMethod]
	public void CreateVolumeTexture_UploadsHalfFloatsUnpaddedAndPutsTheBindingsBack()
	{
		FakeGL3D gl = FakeGL3D.WithUnusualState();
		using GLRenderer3D renderer = new(gl);
		string before = gl.Snapshot();

		Assert.IsTrue(renderer.SupportsVolumes);
		nint volume = renderer.CreateVolumeTexture(3, 2, 5, 1, new Half[30]);

		Assert.IsTrue(renderer.IsVolume(volume));
		CollectionAssert.Contains(gl.Calls, "TexImage3D(R16f,3x2x5,Red,HalfFloat,60,align=1)", "Odd-width rows of two-byte texels need an alignment of one.");
		Assert.AreEqual(before, gl.Snapshot(), "The 3D binding and the unpack alignment are put back.");
	}

	[TestMethod]
	public void CreateVolumeTexture_WithFourChannels_UploadsRgbaHalfFloats()
	{
		FakeGL3D gl = new();
		using GLRenderer3D renderer = new(gl);

		renderer.CreateVolumeTexture(2, 2, 2, 4, new Half[32]);

		CollectionAssert.Contains(gl.Calls, "TexImage3D(Rgba16f,2x2x2,Rgba,HalfFloat,64,align=1)");
	}

	[TestMethod]
	public void DrawVolume_PassesTheChannelColoursAndExtinction()
	{
		FakeGL3D gl = new();
		using GLRenderer3D renderer = new(gl);
		nint target = renderer.CreateRenderTarget(8, 8, depth: false, RenderTargetFormat.Rgba16F);
		nint volume = renderer.CreateVolumeTexture(1, 1, 1, 4, new Half[4]);

		renderer.DrawVolume(target, volume, new VolumeDrawState
		{
			ViewProjection = Matrix4x4.Identity,
			VolumeToWorld = Matrix4x4.Identity,
			Color1 = new Vector4(0.1f, 0.2f, 0.3f, 1f),
			Color3 = new Vector4(0.7f, 0.8f, 0.9f, 1f),
			Extinction = new Vector4(0f, 0f, 0f, 2.5f),
		});

		Assert.AreEqual(new Vector4(0.1f, 0.2f, 0.3f, 1f), gl.Uniforms[gl.GetUniformLocation(0, "Color1")]);
		Assert.AreEqual(new Vector4(0.7f, 0.8f, 0.9f, 1f), gl.Uniforms[gl.GetUniformLocation(0, "Color3")]);
		Assert.AreEqual(new Vector4(0f, 0f, 0f, 2.5f), gl.Uniforms[gl.GetUniformLocation(0, "Extinction")]);
		Assert.AreEqual(4, gl.Uniforms[gl.GetUniformLocation(0, "Channels")]);
		Assert.AreEqual(Vector4.One, gl.Uniforms[gl.GetUniformLocation(0, "NoiseWeights")], "Unset weights mean full noise in every channel.");
		Assert.AreEqual(Vector3.One, gl.Uniforms[gl.GetUniformLocation(0, "NoiseStretch")], "Unset stretch means round cells.");
	}

	[TestMethod]
	public void DrawVolume_WithADetailMap_BindsItOnUnitOneAndPutsTheBindingsBack()
	{
		FakeGL3D gl = FakeGL3D.WithUnusualState();
		using GLRenderer3D renderer = new(gl);
		nint target = renderer.CreateRenderTarget(8, 8, depth: false, RenderTargetFormat.Rgba16F);
		nint volume = renderer.CreateVolumeTexture(2, 2, 2, 4, new Half[32]);
		nint detail = renderer.CreateVolumeTexture(4, 4, 1, 4, new Half[64]);
		string before = gl.Snapshot();

		renderer.DrawVolume(target, volume, new VolumeDrawState
		{
			ViewProjection = Matrix4x4.Identity,
			VolumeToWorld = Matrix4x4.CreateScale(1f, 1f, 300f),
			DetailMap = detail,
			DetailFadeHeight = 50f,
		});

		Assert.AreEqual(1, gl.Uniforms[gl.GetUniformLocation(0, "HasDetail")]);
		Assert.AreEqual(1, gl.Uniforms[gl.GetUniformLocation(0, "DetailMap")], "The detail map samples unit 1.");
		Assert.AreEqual(50f, gl.Uniforms[gl.GetUniformLocation(0, "DetailFadeHeight")]);
		Assert.AreEqual(300f, gl.Uniforms[gl.GetUniformLocation(0, "PlaneScale")], "The plane's thickness in world units comes from the box.");
		Assert.AreEqual(before, gl.Snapshot(), "Unit 1's binding is put back too.");
	}

	[TestMethod]
	public void DrawVolume_WithAnUnknownDetailMap_IsRejected()
	{
		using GLRenderer3D renderer = new(new FakeGL3D());
		nint target = renderer.CreateRenderTarget(8, 8, depth: false, RenderTargetFormat.Rgba16F);
		nint volume = renderer.CreateVolumeTexture(1, 1, 1, 1, new Half[1]);
		Assert.ThrowsExactly<ArgumentException>(() => renderer.DrawVolume(target, volume, new VolumeDrawState { DetailMap = 999 }));
	}

	[TestMethod]
	public void CreateVolumeTexture_RejectsAWrongSizedSpan()
	{
		using GLRenderer3D renderer = new(new FakeGL3D());
		Assert.ThrowsExactly<ArgumentException>(() => renderer.CreateVolumeTexture(2, 2, 2, 1, new Half[7]));
	}

	[TestMethod]
	public void DrawVolume_DrawsOneTriangleIntoTheTargetReadingTheVolume()
	{
		FakeGL3D gl = FakeGL3D.WithUnusualState();
		using GLRenderer3D renderer = new(gl);
		nint target = renderer.CreateRenderTarget(32, 24, depth: true, RenderTargetFormat.Rgba16F);
		nint volume = renderer.CreateVolumeTexture(4, 4, 4, 1, new Half[64]);

		renderer.DrawVolume(target, volume, new VolumeDrawState
		{
			ViewProjection = Matrix4x4.Identity,
			VolumeToWorld = Matrix4x4.Identity,
			Color = new Vector4(0.5f, 0.25f, 0.125f, 1f),
			Blend = BlendMode.Additive,
		});

		FakeGL3D.DrawRecord draw = gl.Draws.Single();
		Assert.AreEqual(3, draw.Count);
		Assert.AreEqual((int)FakeGL3D.FirstFramebuffer, draw.Framebuffer);
		Assert.AreEqual((int)volume, draw.Texture3D, "The volume is bound as a 3D texture.");
		Assert.AreEqual("0,0,32,24", draw.Viewport);
		Assert.AreEqual(GLEnum.One, draw.BlendDstRgb, "Additive keeps the whole destination.");
		Assert.IsFalse(draw.Enabled.Contains(GLEnum.DepthTest));
		CollectionAssert.Contains(gl.Uniforms.Values.ToArray(), new Vector4(0.5f, 0.25f, 0.125f, 1f));
		CollectionAssert.Contains(gl.Uniforms.Values.ToArray(), new Vector3(4, 4, 4));
	}

	[TestMethod]
	public void DrawVolume_PassesTheNoiseToTheShader()
	{
		FakeGL3D gl = new();
		using GLRenderer3D renderer = new(gl);
		nint target = renderer.CreateRenderTarget(8, 8, depth: false, RenderTargetFormat.Rgba16F);
		nint volume = renderer.CreateVolumeTexture(1, 1, 1, 1, new Half[1]);

		renderer.DrawVolume(target, volume, new VolumeDrawState
		{
			ViewProjection = Matrix4x4.Identity,
			VolumeToWorld = Matrix4x4.Identity,
			LowNoiseAmplitude = 0.375f,
			LowNoiseScale = 1200f,
			HighNoiseAmplitude = 0.25f,
			HighNoiseScale = 120f,
			NoiseOffset = new Vector3(1f, 2f, 3f),
			PixelAngle = 0.0025f,
		});

		Assert.AreEqual(0.375f, gl.Uniforms[gl.GetUniformLocation(0, "LowNoiseAmplitude")]);
		Assert.AreEqual(1200f, gl.Uniforms[gl.GetUniformLocation(0, "LowNoiseScale")]);
		Assert.AreEqual(0.25f, gl.Uniforms[gl.GetUniformLocation(0, "HighNoiseAmplitude")]);
		Assert.AreEqual(120f, gl.Uniforms[gl.GetUniformLocation(0, "HighNoiseScale")]);
		Assert.AreEqual(new Vector3(1f, 2f, 3f), gl.Uniforms[gl.GetUniformLocation(0, "NoiseOffset")]);
		Assert.AreEqual(0.0025f, gl.Uniforms[gl.GetUniformLocation(0, "PixelAngle")]);
	}

	[TestMethod]
	public void DrawVolume_PutsBackEveryPieceOfStateItTouched()
	{
		FakeGL3D gl = FakeGL3D.WithUnusualState();
		using GLRenderer3D renderer = new(gl);
		nint target = renderer.CreateRenderTarget(16, 16, depth: false, RenderTargetFormat.Rgba16F);
		nint volume = renderer.CreateVolumeTexture(2, 2, 2, 1, new Half[8]);
		string before = gl.Snapshot();

		renderer.DrawVolume(target, volume, new VolumeDrawState { ViewProjection = Matrix4x4.Identity, VolumeToWorld = Matrix4x4.Identity, Blend = BlendMode.Additive });

		Assert.AreEqual(before, gl.Snapshot());
	}

	[TestMethod]
	public void DrawVolume_WithASingularMatrix_DrawsNothing()
	{
		FakeGL3D gl = new();
		using GLRenderer3D renderer = new(gl);
		nint target = renderer.CreateRenderTarget(8, 8, depth: false);
		nint volume = renderer.CreateVolumeTexture(1, 1, 1, 1, new Half[1]);

		renderer.DrawVolume(target, volume, new VolumeDrawState { ViewProjection = default, VolumeToWorld = Matrix4x4.Identity });

		Assert.IsEmpty(gl.Draws);
	}

	[TestMethod]
	public void DeleteVolumeTexture_DeletesItAndRejectsAnythingElse()
	{
		FakeGL3D gl = new();
		using GLRenderer3D renderer = new(gl);
		nint volume = renderer.CreateVolumeTexture(1, 1, 1, 1, new Half[1]);
		gl.Calls.Clear();

		renderer.DeleteVolumeTexture(volume);

		CollectionAssert.Contains(gl.Calls, $"DeleteTexture({volume})");
		Assert.ThrowsExactly<ArgumentException>(() => renderer.DeleteVolumeTexture(volume));
	}

	[TestMethod]
	public void OpenGLRenderer_ForwardsVolumesAndRefusesToDeleteOneAsATexture()
	{
		FakeGL3D gl = new();
		using OpenGLRenderer renderer = ControllerOver(gl);
		IRenderer3D controller = renderer;

		Assert.IsTrue(controller.SupportsVolumes);
		nint volume = controller.CreateVolumeTexture(1, 1, 1, 1, new Half[1]);
		nint target = controller.CreateRenderTarget(8, 8, depth: false, RenderTargetFormat.Rgba16F);
		controller.DrawVolume(target, volume, new VolumeDrawState { ViewProjection = Matrix4x4.Identity, VolumeToWorld = Matrix4x4.Identity });

		Assert.HasCount(1, gl.Draws);
		Assert.ThrowsExactly<ArgumentException>(() => renderer.DeleteTexture(volume));
		controller.DeleteVolumeTexture(volume);
	}

	[TestMethod]
	public void Dispose_ReleasesTheToneMapProgramToo()
	{
		FakeGL3D gl = new();
		GLRenderer3D renderer = new(gl);
		nint hdr = renderer.CreateRenderTarget(16, 16, depth: false, RenderTargetFormat.Rgba16F);
		nint shown = renderer.CreateRenderTarget(16, 16, depth: false);
		renderer.ToneMap(hdr, shown, ToneMap3D.Default);

		renderer.Dispose();

		CollectionAssert.Contains(gl.Calls, $"DeleteProgram({FakeGL3D.FirstProgram})");
	}

	[TestMethod]
	public void ResizeRenderTarget_KeepsTheHandleAndRespecifiesBothAttachments()
	{
		FakeGL3D gl = new();
		using GLRenderer3D renderer = new(gl);
		nint target = renderer.CreateRenderTarget(16, 16, depth: true);
		gl.Calls.Clear();

		Assert.IsTrue(renderer.ResizeRenderTarget(target, 40, 20));

		Assert.AreEqual(target, renderer.GetTargetTexture(target), "A caller showing the target need not rebind.");
		CollectionAssert.Contains(gl.Calls, "TexImage2D(Rgba8,40x20)");
		CollectionAssert.Contains(gl.Calls, "RenderbufferStorage(DepthComponent24,40x20)");
		Assert.IsFalse(gl.Calls.Any(c => c.StartsWith("Gen", StringComparison.Ordinal)), "Resizing reuses every object.");
	}

	[TestMethod]
	public void ResizeRenderTarget_AnswersFalseForAHandleItDidNotIssue()
	{
		using GLRenderer3D renderer = new(new FakeGL3D());

		Assert.IsFalse(renderer.ResizeRenderTarget(5, 16, 16));
	}

	[TestMethod]
	public void Draw_PutsBackEveryPieceOfStateItTouched()
	{
		FakeGL3D gl = FakeGL3D.WithUnusualState();
		using GLRenderer3D renderer = new(gl);
		nint target = renderer.CreateRenderTarget(32, 32, depth: true);
		string before = gl.Snapshot();

		renderer.Draw(target, Triangle, TriangleIndices, new DrawState3D
		{
			ModelViewProjection = Matrix4x4.Identity,
			Depth = DepthMode.TestAndWrite,
			Cull = CullMode.Back,
			Blend = BlendMode.Alpha,
		});

		Assert.AreEqual(before, gl.Snapshot());
	}

	[TestMethod]
	public void Draw_DrawsIntoTheTargetWithTheStateItAskedFor()
	{
		FakeGL3D gl = FakeGL3D.WithUnusualState();
		using GLRenderer3D renderer = new(gl);
		nint target = renderer.CreateRenderTarget(32, 24, depth: true);

		renderer.Draw(target, Triangle, TriangleIndices, new DrawState3D
		{
			Depth = DepthMode.Test,
			Cull = CullMode.Back,
			Blend = BlendMode.Opaque,
		});

		FakeGL3D.DrawRecord draw = gl.Draws.Single();
		Assert.AreEqual(GLEnum.Triangles, draw.Mode);
		Assert.AreEqual(3, draw.Count);
		Assert.AreEqual((int)FakeGL3D.FirstFramebuffer, draw.Framebuffer);
		Assert.AreEqual("0,0,32,24", draw.Viewport, "The viewport has to be the target's, not the window's.");
		Assert.IsTrue(draw.Enabled.Contains(GLEnum.DepthTest));
		Assert.IsFalse(draw.DepthWrite, "DepthMode.Test tests without writing.");
		Assert.IsTrue(draw.Enabled.Contains(GLEnum.CullFace));
		Assert.AreEqual(GLEnum.CW, draw.FrontFace, "The shader flips y, which flips the winding that counts as front.");
		Assert.AreEqual(GLEnum.Back, draw.CullFaceMode);
		Assert.IsFalse(draw.Enabled.Contains(GLEnum.Blend));
		Assert.IsFalse(draw.Enabled.Contains(GLEnum.ScissorTest), "The frame's scissor rectangle has nothing to do with the target.");
	}

	[TestMethod]
	public void Draw_IntoATargetWithoutDepth_LeavesTheDepthTestOff()
	{
		FakeGL3D gl = new();
		using GLRenderer3D renderer = new(gl);
		nint target = renderer.CreateRenderTarget(8, 8, depth: false);

		renderer.Draw(target, Triangle, TriangleIndices, new DrawState3D { Depth = DepthMode.TestAndWrite });

		Assert.IsFalse(gl.Draws.Single().Enabled.Contains(GLEnum.DepthTest));
	}

	[TestMethod]
	public void Draw_LineList_DrawsLinesAndDropsATrailingPartialPrimitive()
	{
		FakeGL3D gl = new();
		using GLRenderer3D renderer = new(gl);
		nint target = renderer.CreateRenderTarget(8, 8, depth: false);

		renderer.Draw(target, Triangle, [0, 1, 1, 2, 2], new DrawState3D { Topology = PrimitiveTopology.LineList });

		FakeGL3D.DrawRecord draw = gl.Draws.Single();
		Assert.AreEqual(GLEnum.Lines, draw.Mode);
		Assert.AreEqual(4, draw.Count);
	}

	[TestMethod]
	public void Draw_UploadsTheMatrixInMemoryOrderWithoutTransposing()
	{
		FakeGL3D gl = new();
		using GLRenderer3D renderer = new(gl);
		nint target = renderer.CreateRenderTarget(8, 8, depth: false);
		Matrix4x4 matrix = new(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16);

		renderer.Draw(target, Triangle, TriangleIndices, new DrawState3D { ModelViewProjection = matrix });

		// Row-vector, row-major in memory; OpenGL reads that as column-major, which is the transpose,
		// and the transpose times a column vector is the same product. Transposing would be the bug.
		CollectionAssert.AreEqual(Enumerable.Range(1, 16).Select(i => (float)i).ToArray(), gl.LastMatrix);
	}

	[TestMethod]
	public void Draw_WithAnIndexPastTheEnd_ThrowsAndDrawsNothing()
	{
		FakeGL3D gl = new();
		using GLRenderer3D renderer = new(gl);
		nint target = renderer.CreateRenderTarget(8, 8, depth: false);

		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => renderer.Draw(target, Triangle, [0, 1, 3], default));

		Assert.IsEmpty(gl.Draws);
	}

	[TestMethod]
	public void Draw_WithNoIndices_IsANoOp()
	{
		FakeGL3D gl = new();
		using GLRenderer3D renderer = new(gl);
		nint target = renderer.CreateRenderTarget(8, 8, depth: false);
		gl.Calls.Clear();

		renderer.Draw(target, Triangle, [], default);

		Assert.IsEmpty(gl.Calls, "Nothing to draw means nothing compiled, bound or uploaded.");
	}

	[TestMethod]
	public void Draw_SamplingTheTargetItDrawsInto_IsRejected()
	{
		FakeGL3D gl = new();
		using GLRenderer3D renderer = new(gl);
		nint target = renderer.CreateRenderTarget(8, 8, depth: false);

		Assert.ThrowsExactly<ArgumentException>(() =>
			renderer.Draw(target, Triangle, TriangleIndices, new DrawState3D { TextureId = renderer.GetTargetTexture(target) }));
	}

	[TestMethod]
	public void Draw_IntoAnUnknownTarget_Throws()
	{
		using GLRenderer3D renderer = new(new FakeGL3D());

		Assert.ThrowsExactly<ArgumentException>(() => renderer.Draw(5, Triangle, TriangleIndices, default));
	}

	[TestMethod]
	public void Draw_CompilesTheProgramOnceAndOnlyWhenFirstNeeded()
	{
		FakeGL3D gl = new();
		using GLRenderer3D renderer = new(gl);
		nint target = renderer.CreateRenderTarget(8, 8, depth: false);
		Assert.AreEqual(0, gl.ProgramsCreated, "Creating a target needs no program.");

		renderer.Draw(target, Triangle, TriangleIndices, default);
		renderer.Draw(target, Triangle, TriangleIndices, default);

		Assert.AreEqual(1, gl.ProgramsCreated);
	}

	[TestMethod]
	public void Clear_PutsBackEveryPieceOfStateItTouched()
	{
		FakeGL3D gl = FakeGL3D.WithUnusualState();
		using GLRenderer3D renderer = new(gl);
		nint target = renderer.CreateRenderTarget(32, 32, depth: true);
		string before = gl.Snapshot();

		renderer.Clear(target, new Vector4(0.25f, 0.5f, 0.75f, 1f), 1f);

		Assert.AreEqual(before, gl.Snapshot());
	}

	[TestMethod]
	public void Clear_ClearsBothAttachmentsWhateverMasksTheFrameLeftSet()
	{
		FakeGL3D gl = FakeGL3D.WithUnusualState();
		using GLRenderer3D renderer = new(gl);
		nint target = renderer.CreateRenderTarget(32, 32, depth: true);

		renderer.Clear(target, new Vector4(0.25f, 0.5f, 0.75f, 1f), 1f);

		FakeGL3D.ClearRecord clear = gl.Clears.Single();
		Assert.AreEqual(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit, clear.Mask);
		Assert.AreEqual((int)FakeGL3D.FirstFramebuffer, clear.Framebuffer);
		Assert.IsTrue(clear.DepthWrite, "A depth clear with depth writes off clears nothing.");
		Assert.IsFalse(clear.ScissorTest, "A scissored clear clears part of the target.");
		Assert.AreEqual("0.25,0.5,0.75,1", clear.Color);
	}

	[TestMethod]
	public void Dispose_ReleasesLiveTargetsAndTheProgram()
	{
		FakeGL3D gl = new();
		GLRenderer3D renderer = new(gl);
		nint target = renderer.CreateRenderTarget(8, 8, depth: true);
		renderer.Draw(target, Triangle, TriangleIndices, default);
		gl.Calls.Clear();

		renderer.Dispose();

		CollectionAssert.Contains(gl.Calls, $"DeleteTexture({FakeGL3D.FirstTexture})");
		CollectionAssert.Contains(gl.Calls, $"DeleteFramebuffer({FakeGL3D.FirstFramebuffer})");
		CollectionAssert.Contains(gl.Calls, $"DeleteProgram({FakeGL3D.FirstProgram})");
	}

	[TestMethod]
	public void Dispose_Twice_ReleasesNothingTheSecondTime()
	{
		FakeGL3D gl = new();
		GLRenderer3D renderer = new(gl);
		renderer.CreateRenderTarget(8, 8, depth: false);
		renderer.Dispose();
		gl.Calls.Clear();

		renderer.Dispose();

		Assert.IsEmpty(gl.Calls);
	}

	[TestMethod]
	public void Vertex3D_HasTheLayoutTheVertexArrayDescribes()
	{
		Vertex3D[] vertex = [new(new Vector3(1f, 2f, 3f), new Vector2(4f, 5f), 0xAABBCCDD)];
		ReadOnlySpan<byte> bytes = MemoryMarshal.AsBytes<Vertex3D>(vertex);

		Assert.AreEqual(24, Unsafe.SizeOf<Vertex3D>());
		Assert.AreEqual(1f, BitConverter.ToSingle(bytes[GLRenderer3D.PositionOffset..]));
		Assert.AreEqual(3f, BitConverter.ToSingle(bytes[(GLRenderer3D.PositionOffset + 8)..]));
		Assert.AreEqual(4f, BitConverter.ToSingle(bytes[GLRenderer3D.UvOffset..]));
		Assert.AreEqual(0xAABBCCDDu, BitConverter.ToUInt32(bytes[GLRenderer3D.ColorOffset..]));
		Assert.AreEqual((byte)0xDD, bytes[GLRenderer3D.ColorOffset], "Red is the low byte, which is the first one OpenGL reads.");
	}

	[TestMethod]
	public void TryGetRenderer3D_AnswersTrueForTheOpenGLBackend()
	{
		IRendererBackend? previous = ImGuiApp.renderer;
		try
		{
			using OpenGLRenderer renderer = ControllerOver(new FakeGL3D());
			ImGuiApp.renderer = renderer;

			Assert.IsTrue(ImGuiApp.TryGetRenderer3D(out IRenderer3D? renderer3D));
			Assert.IsInstanceOfType<OpenGLRenderer>(renderer3D);
		}
		finally
		{
			ImGuiApp.renderer = previous;
		}
	}

	/// <summary>An OpenGL renderer over stand-ins, carrying a 3D renderer over <paramref name="gl"/>.</summary>
	[SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "The renderer owns the 3D renderer and disposes it with itself.")]
	private static OpenGLRenderer ControllerOver(FakeGL3D gl) => new(new FakeGLRenderer(), new NullTextureUploader(), new GLRenderer3D(gl));

	/// <summary>
	/// Records calls and models the state <see cref="GLRenderer3D"/> reads and writes, so a test
	/// can compare the context before and after a call.
	/// </summary>
	internal sealed class FakeGL3D : IGL3D
	{
		public const uint FirstTexture = 100;
		public const uint FirstFramebuffer = 200;
		public const uint FirstRenderbuffer = 300;
		public const uint FirstProgram = 600;

		private readonly Dictionary<GLEnum, int> integers = [];
		private readonly Dictionary<string, int> uniformLocations = [];
		private readonly Dictionary<(int Unit, GLEnum Target), int> textureByUnit = [];
		private readonly Dictionary<int, int> samplerByUnit = [];
		private readonly HashSet<GLEnum> enabled = [];
		private readonly int[] viewport = new int[4];
		private readonly float[] clearColor = new float[4];
		private float clearDepth;
		private uint nextTexture = FirstTexture;
		private uint nextFramebuffer = FirstFramebuffer;
		private uint nextRenderbuffer = FirstRenderbuffer;
		private uint nextBuffer = 400;
		private uint nextVertexArray = 500;
		private uint nextProgram = FirstProgram;

		public FakeGL3D() => integers[GLEnum.ActiveTexture] = (int)GLEnum.Texture0;

		public List<string> Calls { get; } = [];

		public List<DrawRecord> Draws { get; } = [];

		public List<ClearRecord> Clears { get; } = [];

		public float[] LastMatrix { get; private set; } = [];

		public int ProgramsCreated { get; private set; }

		public GLEnum FramebufferStatus { get; set; } = GLEnum.FramebufferComplete;

		private int ActiveUnit => integers[GLEnum.ActiveTexture] - (int)GLEnum.Texture0;

		/// <summary>A context left in a state nobody would pick, so a restore to defaults shows up.</summary>
		public static FakeGL3D WithUnusualState()
		{
			FakeGL3D gl = new();
			gl.integers[GLEnum.DrawFramebufferBinding] = 7;
			gl.integers[GLEnum.ReadFramebufferBinding] = 8;
			gl.integers[GLEnum.CurrentProgram] = 9;
			gl.integers[GLEnum.ActiveTexture] = (int)GLEnum.Texture3;
			gl.textureByUnit[(0, GLEnum.Texture2D)] = 11;
			gl.textureByUnit[(3, GLEnum.Texture2D)] = 33;
			gl.textureByUnit[(0, GLEnum.Texture3D)] = 44;
			gl.integers[GLEnum.UnpackAlignment] = 8;
			gl.samplerByUnit[0] = 12;
			gl.integers[GLEnum.RenderbufferBinding] = 15;
			gl.integers[GLEnum.VertexArrayBinding] = 13;
			gl.integers[GLEnum.ArrayBufferBinding] = 14;
			gl.integers[GLEnum.DepthWritemask] = 0;
			gl.integers[GLEnum.DepthFunc] = (int)GLEnum.Gequal;
			gl.integers[GLEnum.CullFaceMode] = (int)GLEnum.FrontAndBack;
			gl.integers[GLEnum.FrontFace] = (int)GLEnum.Ccw;
			gl.integers[GLEnum.BlendEquationRgb] = (int)GLEnum.FuncSubtract;
			gl.integers[GLEnum.BlendEquationAlpha] = (int)GLEnum.Max;
			gl.integers[GLEnum.BlendSrcRgb] = (int)GLEnum.DstColor;
			gl.integers[GLEnum.BlendDstRgb] = (int)GLEnum.Zero;
			gl.integers[GLEnum.BlendSrcAlpha] = (int)GLEnum.One;
			gl.integers[GLEnum.BlendDstAlpha] = (int)GLEnum.SrcAlpha;
			gl.enabled.Add(GLEnum.ScissorTest);
			gl.enabled.Add(GLEnum.Blend);
			gl.enabled.Add(GLEnum.StencilTest);
			gl.viewport[0] = 1;
			gl.viewport[1] = 2;
			gl.viewport[2] = 300;
			gl.viewport[3] = 400;
			gl.clearColor[0] = 0.1f;
			gl.clearColor[3] = 0.9f;
			gl.clearDepth = 0.3f;
			return gl;
		}

		/// <summary>Everything modelled, as one comparable string.</summary>
		public string Snapshot() => string.Join(
			"; ",
			integers.OrderBy(p => p.Key).Select(p => $"{p.Key}={p.Value}")
				.Concat(textureByUnit.Where(p => p.Value != 0).OrderBy(p => p.Key.Unit).ThenBy(p => p.Key.Target).Select(p => $"Texture[{p.Key.Unit},{p.Key.Target}]={p.Value}"))
				.Concat(samplerByUnit.Where(p => p.Value != 0).OrderBy(p => p.Key).Select(p => $"Sampler[{p.Key}]={p.Value}"))
				.Append("Enabled=" + string.Join(",", enabled.OrderBy(e => e)))
				.Append("Viewport=" + string.Join(",", viewport))
				.Append("ClearColor=" + string.Join(",", clearColor))
				.Append($"ClearDepth={clearDepth}"));

		public int GetInteger(GLEnum pname) => pname switch
		{
			GLEnum.TextureBinding2D => textureByUnit.GetValueOrDefault((ActiveUnit, GLEnum.Texture2D)),
			GLEnum.TextureBinding3D => textureByUnit.GetValueOrDefault((ActiveUnit, GLEnum.Texture3D)),
			GLEnum.SamplerBinding => samplerByUnit.GetValueOrDefault(ActiveUnit),
			_ => integers.GetValueOrDefault(pname),
		};

		public void GetInteger(GLEnum pname, Span<int> data)
		{
			Assert.AreEqual(GLEnum.Viewport, pname);
			viewport.CopyTo(data);
		}

		public void GetFloat(GLEnum pname, Span<float> data)
		{
			if (pname == GLEnum.ColorClearValue)
			{
				clearColor.CopyTo(data);
				return;
			}

			Assert.AreEqual(GLEnum.DepthClearValue, pname);
			data[0] = clearDepth;
		}

		public bool IsEnabled(GLEnum cap) => enabled.Contains(cap);

		public void Enable(GLEnum cap) => enabled.Add(cap);

		public void Disable(GLEnum cap) => enabled.Remove(cap);

		public uint GenTexture() => nextTexture++;

		public void BindTexture(GLEnum target, uint texture) => textureByUnit[(ActiveUnit, target)] = (int)texture;

		public void TexParameter(GLEnum target, GLEnum pname, int param)
		{
		}

		public void TexImage2DEmpty(GLEnum target, GLEnum internalFormat, int width, int height) =>
			Calls.Add($"TexImage2D({internalFormat},{width}x{height})");

		public void DeleteTexture(uint texture) => Calls.Add($"DeleteTexture({texture})");

		public void ActiveTexture(GLEnum unit) => integers[GLEnum.ActiveTexture] = (int)unit;

		public void BindSampler(uint unit, uint sampler) => samplerByUnit[(int)unit] = (int)sampler;

		public uint GenFramebuffer() => nextFramebuffer++;

		public void BindFramebuffer(GLEnum target, uint framebuffer)
		{
			if (target is GLEnum.Framebuffer or GLEnum.DrawFramebuffer)
			{
				integers[GLEnum.DrawFramebufferBinding] = (int)framebuffer;
			}

			if (target is GLEnum.Framebuffer or GLEnum.ReadFramebuffer)
			{
				integers[GLEnum.ReadFramebufferBinding] = (int)framebuffer;
			}
		}

		public void FramebufferTexture2D(GLEnum target, GLEnum attachment, GLEnum textureTarget, uint texture) =>
			Calls.Add($"FramebufferTexture2D({attachment},{texture})");

		public void FramebufferRenderbuffer(GLEnum target, GLEnum attachment, uint renderbuffer) =>
			Calls.Add($"FramebufferRenderbuffer({attachment},{renderbuffer})");

		public GLEnum CheckFramebufferStatus(GLEnum target) => FramebufferStatus;

		public void DeleteFramebuffer(uint framebuffer) => Calls.Add($"DeleteFramebuffer({framebuffer})");

		public uint GenRenderbuffer()
		{
			Calls.Add("GenRenderbuffer");
			return nextRenderbuffer++;
		}

		public void BindRenderbuffer(uint renderbuffer) => integers[GLEnum.RenderbufferBinding] = (int)renderbuffer;

		public void RenderbufferStorage(GLEnum internalFormat, int width, int height) =>
			Calls.Add($"RenderbufferStorage({internalFormat},{width}x{height})");

		public void DeleteRenderbuffer(uint renderbuffer) => Calls.Add($"DeleteRenderbuffer({renderbuffer})");

		public void Viewport(int x, int y, int width, int height)
		{
			viewport[0] = x;
			viewport[1] = y;
			viewport[2] = width;
			viewport[3] = height;
		}

		public void ClearColor(float red, float green, float blue, float alpha)
		{
			clearColor[0] = red;
			clearColor[1] = green;
			clearColor[2] = blue;
			clearColor[3] = alpha;
		}

		public void ClearDepth(float depth) => clearDepth = depth;

		public void Clear(ClearBufferMask mask)
		{
			Calls.Add($"Clear({mask})");
			Clears.Add(new ClearRecord(
				mask,
				integers.GetValueOrDefault(GLEnum.DrawFramebufferBinding),
				integers.GetValueOrDefault(GLEnum.DepthWritemask) != 0,
				enabled.Contains(GLEnum.ScissorTest),
				string.Join(",", clearColor)));
		}

		public void DepthMask(bool write) => integers[GLEnum.DepthWritemask] = write ? 1 : 0;

		public void DepthFunc(GLEnum func) => integers[GLEnum.DepthFunc] = (int)func;

		public void CullFace(GLEnum mode) => integers[GLEnum.CullFaceMode] = (int)mode;

		public void FrontFace(GLEnum mode) => integers[GLEnum.FrontFace] = (int)mode;

		public void BlendEquationSeparate(GLEnum modeRgb, GLEnum modeAlpha)
		{
			integers[GLEnum.BlendEquationRgb] = (int)modeRgb;
			integers[GLEnum.BlendEquationAlpha] = (int)modeAlpha;
		}

		public void BlendFuncSeparate(GLEnum srcRgb, GLEnum dstRgb, GLEnum srcAlpha, GLEnum dstAlpha)
		{
			integers[GLEnum.BlendSrcRgb] = (int)srcRgb;
			integers[GLEnum.BlendDstRgb] = (int)dstRgb;
			integers[GLEnum.BlendSrcAlpha] = (int)srcAlpha;
			integers[GLEnum.BlendDstAlpha] = (int)dstAlpha;
		}

		public uint CreateProgram(string vertexSource, string fragmentSource)
		{
			Calls.Add("CreateProgram");
			ProgramsCreated++;
			return nextProgram++;
		}

		/// <summary>A distinct location per uniform name, as a real program gives, so no two uniforms share a slot.</summary>
		public int GetUniformLocation(uint program, string name)
		{
			if (!uniformLocations.TryGetValue(name, out int location))
			{
				location = uniformLocations.Count + 1;
				uniformLocations[name] = location;
			}

			return location;
		}

		public void UseProgram(uint program) => integers[GLEnum.CurrentProgram] = (int)program;

		public void DeleteProgram(uint program) => Calls.Add($"DeleteProgram({program})");

		public void Uniform1(int location, int value) => Uniforms[location] = value;

		public void Uniform1(int location, float value) => Uniforms[location] = value;

		public void UniformMatrix4(int location, ReadOnlySpan<float> value) => LastMatrix = value.ToArray();

		public uint GenBuffer() => nextBuffer++;

		public void BindBuffer(GLEnum target, uint buffer)
		{
			Calls.Add($"BindBuffer({target},{buffer})");
			if (target == GLEnum.ArrayBuffer)
			{
				integers[GLEnum.ArrayBufferBinding] = (int)buffer;
			}
		}

		public void BufferData(GLEnum target, ReadOnlySpan<byte> data) => Calls.Add($"BufferData({target},{data.Length})");

		public void DeleteBuffer(uint buffer) => Calls.Add($"DeleteBuffer({buffer})");

		public uint GenVertexArray() => nextVertexArray++;

		public void BindVertexArray(uint vertexArray) => integers[GLEnum.VertexArrayBinding] = (int)vertexArray;

		public void DeleteVertexArray(uint vertexArray) => Calls.Add($"DeleteVertexArray({vertexArray})");

		public void EnableVertexAttribArray(uint index)
		{
		}

		public void VertexAttribPointer(uint index, int size, GLEnum type, bool normalized, int stride, int offset)
		{
		}

		public void DrawElements(GLEnum mode, int count)
		{
			Calls.Add($"DrawElements({mode},{count})");
			Draws.Add(new DrawRecord(
				mode,
				count,
				integers.GetValueOrDefault(GLEnum.DrawFramebufferBinding),
				string.Join(",", viewport),
				[.. enabled],
				integers.GetValueOrDefault(GLEnum.DepthWritemask) != 0,
				(GLEnum)integers.GetValueOrDefault(GLEnum.FrontFace),
				(GLEnum)integers.GetValueOrDefault(GLEnum.CullFaceMode),
				(GLEnum)integers.GetValueOrDefault(GLEnum.BlendSrcRgb),
				(GLEnum)integers.GetValueOrDefault(GLEnum.BlendDstRgb),
				integers.GetValueOrDefault(GLEnum.CurrentProgram),
				textureByUnit.GetValueOrDefault((0, GLEnum.Texture2D)),
				textureByUnit.GetValueOrDefault((0, GLEnum.Texture3D))));
		}

		public void DrawArrays(GLEnum mode, int first, int count)
		{
			Calls.Add($"DrawArrays({mode},{first},{count})");
			Draws.Add(new DrawRecord(
				mode,
				count,
				integers.GetValueOrDefault(GLEnum.DrawFramebufferBinding),
				string.Join(",", viewport),
				[.. enabled],
				integers.GetValueOrDefault(GLEnum.DepthWritemask) != 0,
				(GLEnum)integers.GetValueOrDefault(GLEnum.FrontFace),
				(GLEnum)integers.GetValueOrDefault(GLEnum.CullFaceMode),
				(GLEnum)integers.GetValueOrDefault(GLEnum.BlendSrcRgb),
				(GLEnum)integers.GetValueOrDefault(GLEnum.BlendDstRgb),
				integers.GetValueOrDefault(GLEnum.CurrentProgram),
				textureByUnit.GetValueOrDefault((0, GLEnum.Texture2D)),
				textureByUnit.GetValueOrDefault((0, GLEnum.Texture3D))));
		}

		public Dictionary<int, object> Uniforms { get; } = [];

		public void Uniform3(int location, float x, float y, float z) => Uniforms[location] = new Vector3(x, y, z);

		public void Uniform4(int location, float x, float y, float z, float w) => Uniforms[location] = new Vector4(x, y, z, w);

		public void TexImage3D(GLEnum target, GLEnum internalFormat, int width, int height, int depth, GLEnum format, GLEnum type, ReadOnlySpan<byte> data) =>
			Calls.Add($"TexImage3D({internalFormat},{width}x{height}x{depth},{format},{type},{data.Length},align={integers.GetValueOrDefault(GLEnum.UnpackAlignment)})");

		public void PixelStore(GLEnum pname, int param) => integers[pname] = param;

		public sealed record DrawRecord(
			GLEnum Mode,
			int Count,
			int Framebuffer,
			string Viewport,
			HashSet<GLEnum> Enabled,
			bool DepthWrite,
			GLEnum FrontFace,
			GLEnum CullFaceMode,
			GLEnum BlendSrcRgb,
			GLEnum BlendDstRgb,
			int Program,
			int Texture,
			int Texture3D);

		public sealed record ClearRecord(ClearBufferMask Mask, int Framebuffer, bool DepthWrite, bool ScissorTest, string Color);
	}
}
