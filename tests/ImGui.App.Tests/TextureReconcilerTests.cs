// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Tests;

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Hexa.NET.ImGui;

using ktsu.ImGui.App.ImGuiController;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Covers the decisions ImGui's texture protocol asks a backend to make. These run without a
/// graphics context, which is the point of the <see cref="ITextureUploader"/> seam.
/// </summary>
[TestClass]
public sealed unsafe class TextureReconcilerTests
{
	/// <summary>Records what was asked of the renderer instead of touching a GPU.</summary>
	private sealed class FakeUploader : ITextureUploader
	{
		public List<string> Calls { get; } = [];

		public nint NextId { get; set; } = 7;

		public List<(nint TextureId, int SourceRowPixels, ImTextureRect Rect)> Updates { get; } = [];

		public nint Create(int width, int height, nint pixels)
		{
			Calls.Add($"create {width}x{height}");
			return NextId;
		}

		public void Update(nint textureId, int sourceRowPixels, ImTextureRect rect, nint pixels)
		{
			Calls.Add($"update {rect.W}x{rect.H}@{rect.X},{rect.Y}");
			Updates.Add((textureId, sourceRowPixels, rect));
		}

		public void Destroy(nint textureId)
		{
			Calls.Add($"destroy {textureId}");
		}
	}

	/// <summary>
	/// An ImGui texture record owned by the test. ImGui asserts a texture is Destroyed before its
	/// pixels are allocated or freed, so both ends of its life are staged here.
	/// </summary>
	private sealed class TestTexture : IDisposable
	{
		private ImTextureData* raw;

		public TestTexture(int width, int height)
		{
			raw = (ImTextureData*)NativeMemory.AllocZeroed((nuint)sizeof(ImTextureData));
			Ptr = new ImTextureDataPtr(raw);
			Ptr.SetStatus(ImTextureStatus.Destroyed);
			Ptr.Create(ImTextureFormat.Rgba32, width, height);
		}

		public ImTextureDataPtr Ptr { get; }

		public void Dispose()
		{
			if (raw is null)
			{
				return;
			}

			Ptr.SetStatus(ImTextureStatus.Destroyed);
			Ptr.DestroyPixels();
			NativeMemory.Free(raw);
			raw = null;
		}
	}

	[TestMethod]
	public void WantCreate_UploadsAndAdoptsTheRendererId()
	{
		FakeUploader uploader = new() { NextId = 42 };
		TextureReconciler reconciler = new(uploader);
		using TestTexture texture = new(16, 8);

		reconciler.Reconcile(texture.Ptr);

		Assert.AreEqual("create 16x8", uploader.Calls[0]);
		Assert.AreEqual(ImTextureStatus.Ok, texture.Ptr.Status, "A satisfied request must be marked Ok or ImGui asks again every frame.");
		Assert.AreEqual(42, (nint)(nuint)texture.Ptr.GetTexID());
		Assert.AreEqual(1, reconciler.UploadCount);
	}

	[TestMethod]
	public void WantUpdates_UploadsEachDirtyRectangleWithTheAtlasStride()
	{
		FakeUploader uploader = new();
		TextureReconciler reconciler = new(uploader);
		using TestTexture texture = new(64, 32);

		reconciler.Reconcile(texture.Ptr);
		texture.Ptr.SetStatus(ImTextureStatus.WantUpdates);
		texture.Ptr.Updates.PushBack(new ImTextureRect(2, 3, 4, 5));
		texture.Ptr.Updates.PushBack(new ImTextureRect(10, 11, 6, 7));

		reconciler.Reconcile(texture.Ptr);

		Assert.HasCount(2, uploader.Updates);
		Assert.AreEqual(64, uploader.Updates[0].SourceRowPixels, "The stride must describe the whole atlas, not the rectangle, or the rows read at the wrong offset.");
		Assert.AreEqual(4, uploader.Updates[0].Rect.W);
		Assert.AreEqual(11, uploader.Updates[1].Rect.Y);
		Assert.AreEqual(ImTextureStatus.Ok, texture.Ptr.Status);
	}

	[TestMethod]
	public void WantDestroy_WaitsUntilTheTextureHasGoneUnusedForAFrame()
	{
		FakeUploader uploader = new();
		TextureReconciler reconciler = new(uploader);
		using TestTexture texture = new(8, 8);

		reconciler.Reconcile(texture.Ptr);
		uploader.Calls.Clear();
		texture.Ptr.SetStatus(ImTextureStatus.WantDestroy);
		texture.Ptr.UnusedFrames = 0;

		reconciler.Reconcile(texture.Ptr);

		Assert.IsEmpty(uploader.Calls, "Releasing a texture the renderer may still be reading from would be a use-after-free.");
		Assert.AreEqual(ImTextureStatus.WantDestroy, texture.Ptr.Status);

		texture.Ptr.UnusedFrames = 1;
		reconciler.Reconcile(texture.Ptr);

		Assert.AreEqual("destroy 7", uploader.Calls[0]);
		Assert.AreEqual(ImTextureStatus.Destroyed, texture.Ptr.Status);
		Assert.AreEqual(0, (nint)(nuint)texture.Ptr.GetTexID(), "A destroyed texture must not keep a stale id.");
	}

	[TestMethod]
	public void Ok_IsLeftAlone()
	{
		FakeUploader uploader = new();
		TextureReconciler reconciler = new(uploader);
		using TestTexture texture = new(8, 8);
		texture.Ptr.SetStatus(ImTextureStatus.Ok);

		reconciler.Reconcile(texture.Ptr);

		Assert.IsEmpty(uploader.Calls);
		Assert.AreEqual(0, reconciler.UploadCount);
	}

	[TestMethod]
	public void NonRgbaFormat_IsRefusedRatherThanUploadedAsGarbage()
	{
		FakeUploader uploader = new();
		TextureReconciler reconciler = new(uploader);
		using TestTexture texture = new(8, 8);
		texture.Ptr.Format = ImTextureFormat.Alpha8;

		InvalidOperationException ex = Assert.ThrowsExactly<InvalidOperationException>(() => reconciler.Reconcile(texture.Ptr));

		Assert.Contains("Alpha8", ex.Message, StringComparison.Ordinal);
		Assert.IsEmpty(uploader.Calls);
	}

	[TestMethod]
	public void ReconcileFrame_TouchesOnlyTexturesThatAskForSomething()
	{
		FakeUploader uploader = new();
		TextureReconciler reconciler = new(uploader);
		using TestTexture pending = new(4, 4);
		using TestTexture settled = new(4, 4);
		settled.Ptr.SetStatus(ImTextureStatus.Ok);

		ImVector<ImTextureDataPtr> textures = default;
		textures.PushBack(pending.Ptr);
		textures.PushBack(settled.Ptr);
		try
		{
			reconciler.ReconcileFrame(textures);
		}
		finally
		{
			textures.Free();
		}

		Assert.HasCount(1, uploader.Calls);
		Assert.AreEqual("create 4x4", uploader.Calls[0]);
	}

	[TestMethod]
	public void ReconcileFrame_ToleratesAnAbsentTextureList()
	{
		FakeUploader uploader = new();
		TextureReconciler reconciler = new(uploader);

		reconciler.ReconcileFrame(default);

		Assert.IsEmpty(uploader.Calls);
	}

	[TestMethod]
	public void DestroyAll_ToleratesAnAbsentTextureList()
	{
		FakeUploader uploader = new();
		TextureReconciler reconciler = new(uploader);

		// Shutdown can come before ImGui has ever produced a texture list, so this must not
		// dereference it.
		reconciler.DestroyAll(default);

		Assert.IsEmpty(uploader.Calls);
	}

	[TestMethod]
	public void DestroyAll_ReleasesOnlyTexturesNothingElseHolds()
	{
		FakeUploader uploader = new();
		TextureReconciler reconciler = new(uploader);
		using TestTexture ours = new(4, 4);
		using TestTexture shared = new(4, 4);
		reconciler.Reconcile(ours.Ptr);
		reconciler.Reconcile(shared.Ptr);
		uploader.Calls.Clear();
		ours.Ptr.RefCount = 1;
		shared.Ptr.RefCount = 2;

		ImVector<ImTextureDataPtr> textures = default;
		textures.PushBack(ours.Ptr);
		textures.PushBack(shared.Ptr);
		try
		{
			reconciler.DestroyAll(textures);
		}
		finally
		{
			textures.Free();
		}

		Assert.HasCount(1, uploader.Calls);
		Assert.AreEqual(ImTextureStatus.Destroyed, ours.Ptr.Status);
		Assert.AreEqual(ImTextureStatus.Ok, shared.Ptr.Status, "A texture someone else still references is not ours to release.");
	}

	[TestMethod]
	public void Constructor_RejectsAMissingUploader()
	{
		Assert.ThrowsExactly<ArgumentNullException>(() => new TextureReconciler(null!));
	}
}
