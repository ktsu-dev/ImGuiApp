// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using System;
using System.Numerics;
using System.Runtime.CompilerServices;

using Hexa.NET.ImGui;

using ktsu.ImGui.App;
using ktsu.ImGui.App.Testing;
using ktsu.ImGui.Rendering;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>Drives <see cref="ImGuiWidgets.Viewport3D"/> on its own, through the software renderer.</summary>
[TestClass]
public sealed class Viewport3DTests : WidgetTest
{
	private const string Label = "viewport";
	private static readonly Vector2 ViewportSize = new(240f, 180f);

	private ImGuiWidgets.Viewport3DState state = new();
	private Vector2 size = ViewportSize;
	private bool drawCube = true;
	private bool showViewport = true;
	private bool returned;
	private int callbacks;
	private ImGuiWidgets.Viewport3DFrame lastFrame;

	[TestInitialize]
	public void ResetFields()
	{
		state = new ImGuiWidgets.Viewport3DState { Distance = 5f, Yaw = 0.5f, Pitch = 0.4f };
		size = ViewportSize;
		drawCube = true;
		showViewport = true;
		returned = false;
		callbacks = 0;
	}

	private void Draw()
	{
		if (showViewport)
		{
			returned = ImGuiWidgets.Viewport3D(Label, size, state, DrawScene);
		}
	}

	private void DrawScene(ImGuiWidgets.Viewport3DFrame frame)
	{
		callbacks++;
		lastFrame = frame;
		if (drawCube)
		{
			DrawCube(frame);
		}
	}

	/// <summary>A unit cube, each face its own flat colour, at the origin.</summary>
	internal static void DrawCube(ImGuiWidgets.Viewport3DFrame frame)
	{
		Vector3[] corners =
		[
			new(-1, -1, -1), new(1, -1, -1), new(1, 1, -1), new(-1, 1, -1),
			new(-1, -1, 1), new(1, -1, 1), new(1, 1, 1), new(-1, 1, 1),
		];
		int[] faces = [4, 5, 6, 7, 1, 0, 3, 2, 5, 1, 2, 6, 0, 4, 7, 3, 7, 6, 2, 3, 0, 1, 5, 4];
		uint[] colors = [0xFF4040E0u, 0xFF40E040u, 0xFFE07040u, 0xFF40D0E0u, 0xFFE040D0u, 0xFFE0E040u];

		Vertex3D[] vertices = new Vertex3D[24];
		uint[] indices = new uint[36];
		for (int f = 0; f < 6; f++)
		{
			for (int c = 0; c < 4; c++)
			{
				vertices[(f * 4) + c] = new Vertex3D(corners[faces[(f * 4) + c]], Vector2.Zero, colors[f]);
			}

			uint v = (uint)(f * 4);
			int i = f * 6;
			indices[i] = v;
			indices[i + 1] = v + 1;
			indices[i + 2] = v + 2;
			indices[i + 3] = v;
			indices[i + 4] = v + 2;
			indices[i + 5] = v + 3;
		}

		frame.Renderer.Draw(frame.Target, vertices, indices, new DrawState3D
		{
			ModelViewProjection = frame.ViewProjection,
			Depth = DepthMode.TestAndWrite,
			Cull = CullMode.None,
		});
	}

	private static IRenderer3D Renderer => Renderer3DHost.TryGet(out IRenderer3D? renderer)
		? renderer
		: throw new AssertFailedException("The harness offered no 3D renderer.");

	private Rectangle? CubeBounds()
	{
		drawCube = false;
		Step(2);
		byte[] empty = Snapshot();

		drawCube = true;
		Step(2);
		return BoundsOfDifference(empty);
	}

	[TestMethod]
	public void Viewport3D_DrawsThroughTheRendererAndMarksItself()
	{
		Start(Draw);

		Assert.IsTrue(IsVisible(Label), "The viewport marked no probe item.");
		Assert.IsTrue(returned, "The viewport reported that it did not draw, with a 3D renderer installed.");
		Assert.IsGreaterThan(0, callbacks, "The draw callback was never called.");
		Assert.AreEqual(new Vector2(ViewportSize.X, ViewportSize.Y), lastFrame.PixelSize);

		Rectangle rect = RectOf(Label);
		Assert.IsTrue(Math.Abs(rect.Width - ViewportSize.X) <= 1 && Math.Abs(rect.Height - ViewportSize.Y) <= 1, $"The viewport claimed {rect.Width}x{rect.Height} rather than {ViewportSize}.");
	}

	[TestMethod]
	public void Viewport3D_ACubeRendersInsideTheViewport()
	{
		Start(Draw);

		Rectangle? cube = CubeBounds();
		Rectangle rect = RectOf(Label);

		Assert.IsNotNull(cube, "Drawing a cube changed no pixels.");
		Assert.IsTrue(cube.Value.MinX >= rect.MinX && cube.Value.MaxX <= rect.MaxX && cube.Value.MinY >= rect.MinY && cube.Value.MaxY <= rect.MaxY, $"The cube spilled out of the viewport: {cube} outside {rect}.");
		Assert.IsGreaterThan(rect.Width / 4, cube.Value.Width, $"The cube is implausibly small: {cube}.");
	}

	[TestMethod]
	public void Viewport3D_TheCubeIsCentredWhenTheTargetIsRoundedUpPastTheViewport()
	{
		// 240x180 allocates 256x192, so the shown part is a fraction of the target. A cube at the
		// origin looking straight on is centred only if the view is mapped onto that fraction.
		state.Yaw = 0f;
		state.Pitch = 0f;
		Start(Draw);

		Rectangle? cube = CubeBounds();
		Rectangle rect = RectOf(Label);

		Assert.IsNotNull(cube);
		float cubeCentreX = (cube.Value.MinX + cube.Value.MaxX) / 2f;
		float cubeCentreY = (cube.Value.MinY + cube.Value.MaxY) / 2f;
		float viewCentreX = (rect.MinX + rect.MaxX) / 2f;
		float viewCentreY = (rect.MinY + rect.MaxY) / 2f;
		Assert.IsTrue(Math.Abs(cubeCentreX - viewCentreX) <= 2f && Math.Abs(cubeCentreY - viewCentreY) <= 2f, $"The cube's centre ({cubeCentreX}, {cubeCentreY}) is off the viewport's ({viewCentreX}, {viewCentreY}).");
		Assert.IsTrue(Math.Abs(cube.Value.Width - cube.Value.Height) <= 2, $"A face seen straight on came out {cube.Value.Width}x{cube.Value.Height}, so the aspect is wrong.");
	}

	[TestMethod]
	public void Viewport3D_AnOrbitDragTurnsTheCameraAndChangesThePixels()
	{
		Start(Draw);
		float yaw = state.Yaw;
		float pitch = state.Pitch;
		Step();
		byte[] before = Snapshot();

		Rectangle rect = RectOf(Label);
		Harness.Mouse.Drag(rect.MinX + (rect.Width * 0.3f), rect.MinY + (rect.Height * 0.4f), rect.MinX + (rect.Width * 0.7f), rect.MinY + (rect.Height * 0.6f));
		Step();

		Assert.AreNotEqual(yaw, state.Yaw, "A left drag across the viewport did not turn the camera.");
		Assert.AreNotEqual(pitch, state.Pitch, "A downward drag did not raise or lower the camera.");
		Assert.AreEqual(Vector3.Zero, state.Target, "An orbit moved the target.");
		Assert.IsGreaterThan(0, PixelsChangedSince(before), "Orbiting changed no pixels.");
	}

	[TestMethod]
	public void Viewport3D_AMiddleDragPansTheTarget()
	{
		Start(Draw);
		float yaw = state.Yaw;

		Rectangle rect = RectOf(Label);
		Harness.Mouse.Drag(rect.MinX + (rect.Width * 0.3f), rect.MinY + (rect.Height * 0.5f), rect.MinX + (rect.Width * 0.7f), rect.MinY + (rect.Height * 0.5f), button: 2);

		Assert.AreNotEqual(Vector3.Zero, state.Target, "A middle drag did not pan.");
		Assert.AreEqual(yaw, state.Yaw, "A middle drag turned the camera.");
	}

	[TestMethod]
	public void Viewport3D_PanningKeepsTheSceneUnderThePointer()
	{
		// Looking straight down -Z at a target on the origin, a drag of a quarter of the height
		// should move the target a quarter of the visible height the other way.
		state.Yaw = 0f;
		state.Pitch = 0f;
		Start(Draw);

		Rectangle rect = RectOf(Label);
		float dragPixels = rect.Height / 4f;
		Harness.Mouse.Drag(rect.MinX + (rect.Width * 0.5f), rect.MinY + (rect.Height * 0.5f), rect.MinX + (rect.Width * 0.5f) + dragPixels, rect.MinY + (rect.Height * 0.5f), button: 2);

		float visibleHeight = 2f * state.Distance * MathF.Tan(state.FieldOfView / 2f);
		float expected = -visibleHeight / 4f;
		Assert.AreEqual(expected, state.Target.X, Math.Abs(expected) * 0.05f, "The scene did not follow the pointer.");
	}

	[TestMethod]
	public void Viewport3D_AShiftDragPansRatherThanOrbits()
	{
		Start(Draw);
		float yaw = state.Yaw;

		Rectangle rect = RectOf(Label);
		HarnessKeyboard.KeyDown(ImGuiKey.ModShift);
		Step();
		Harness.Mouse.Drag(rect.MinX + (rect.Width * 0.3f), rect.MinY + (rect.Height * 0.5f), rect.MinX + (rect.Width * 0.7f), rect.MinY + (rect.Height * 0.5f));
		HarnessKeyboard.KeyUp(ImGuiKey.ModShift);
		Step();

		Assert.AreNotEqual(Vector3.Zero, state.Target, "A Shift+left drag did not pan.");
		Assert.AreEqual(yaw, state.Yaw, "A Shift+left drag turned the camera.");
	}

	[TestMethod]
	public void Viewport3D_TheWheelDolliesTowardsTheTarget()
	{
		Start(Draw);
		float distance = state.Distance;

		Vector2 centre = CenterOf(Label);
		Harness.Mouse.Wheel(centre.X, centre.Y, 2);

		Assert.IsLessThan(distance, state.Distance, "Scrolling up over the viewport did not move the camera closer.");
	}

	[TestMethod]
	public void Viewport3D_ShowsTheFreeDragCursor()
	{
		Start(Draw);

		ImGuiMouseCursor cursor = CursorWhileDragging(Label, 0.5f, 0.5f, new Vector2(30f, 0f));

		Assert.AreEqual(ImGuiMouseCursor.ResizeAll, cursor);
	}

	[TestMethod]
	public void Viewport3D_WithNoRendererDrawsAnEmptyFrameOfTheRightSize()
	{
		// ktsu.ImGui.App installs its resolver when the module loads, so load it before saving the
		// resolver, or Start would put the real one back over the null this installs.
		RuntimeHelpers.RunModuleConstructor(typeof(ImGuiApp).Module.ModuleHandle);
		Func<IRenderer3D?>? resolver = Renderer3DHost.Resolver;
		Renderer3DHost.Resolver = static () => null;
		try
		{
			Start(Draw);

			Assert.IsFalse(returned, "The viewport reported drawing with no renderer.");
			Assert.AreEqual(0, callbacks, "The draw callback ran with no renderer to draw with.");
			Rectangle rect = RectOf(Label);
			Assert.IsTrue(Math.Abs(rect.Width - ViewportSize.X) <= 1 && Math.Abs(rect.Height - ViewportSize.Y) <= 1, $"The empty frame is {rect.Width}x{rect.Height} rather than {ViewportSize}.");
			AssertSomethingWasDrawn("the empty viewport");

			Rectangle before = rect;
			DragAcross(Label, 0.3f, 0.7f);
			Assert.AreEqual(before.Width, RectOf(Label).Width);
			Assert.AreNotEqual(0.5f, state.Yaw, "The camera stopped answering the mouse without a renderer.");
		}
		finally
		{
			Renderer3DHost.Resolver = resolver;
		}
	}

	[TestMethod]
	public void Viewport3D_ATargetIsReleasedAfterTheViewportStopsBeingDrawn()
	{
		ImGuiWidgets.Viewport3DState other = new();
		Start(() =>
		{
			Draw();
			ImGuiWidgets.Viewport3D("other", new Vector2(64f, 64f), other, static _ => { });
		});

		Assert.AreEqual(2, ImGuiWidgets.Viewport3DTargets.Count);
		nint handle = lastFrame.Target;
		Assert.IsTrue(Renderer.GetTargetTexture(handle) != 0);

		showViewport = false;
		Step(ImGuiWidgets.Viewport3DTargets.ReleaseAfterFrames + 2);

		Assert.AreEqual(1, ImGuiWidgets.Viewport3DTargets.Count, "A viewport no longer drawn kept its target.");
		Assert.ThrowsExactly<ArgumentException>(() => Renderer.GetTargetTexture(handle), "The target was forgotten but not deleted.");
	}

	[TestMethod]
	public void Viewport3D_TargetsMadeByAnEarlierRendererAreForgotten()
	{
		Start(Draw);
		Assert.AreEqual(1, ImGuiWidgets.Viewport3DTargets.Count);
		DisposeHarness();

		Start(Draw);

		Assert.AreEqual(1, ImGuiWidgets.Viewport3DTargets.Count, "A target from the previous harness's renderer is still held.");
		Assert.AreNotEqual(0, lastFrame.Target);
	}

	[TestMethod]
	public void Viewport3D_ResizingWithinABucketKeepsTheAllocation()
	{
		size = new Vector2(100f, 100f);
		Start(Draw);
		uint id = Single(ImGuiWidgets.Viewport3DTargets.Ids);
		nint handle = ImGuiWidgets.Viewport3DTargets.HandleFor(id);

		size = new Vector2(120f, 110f);
		Step(2);
		Assert.AreEqual(handle, ImGuiWidgets.Viewport3DTargets.HandleFor(id));
		Assert.AreEqual(new Vector2(120f, 110f), lastFrame.PixelSize);

		size = new Vector2(200f, 100f);
		Step(2);
		Assert.AreEqual(handle, ImGuiWidgets.Viewport3DTargets.HandleFor(id), "The software renderer keeps a target's id when it resizes.");
		Assert.AreEqual(1, ImGuiWidgets.Viewport3DTargets.Count);
		Assert.AreEqual(new Vector2(200f, 100f), lastFrame.PixelSize);
	}

	[TestMethod]
	public void Viewport3D_AZeroSizeFillsTheContentRegion()
	{
		size = Vector2.Zero;
		Start(Draw);

		Rectangle rect = RectOf(Label);

		Assert.IsGreaterThan(DefaultViewport.Width / 2, rect.Width, $"A zero width claimed only {rect.Width}px.");
		Assert.IsGreaterThan(DefaultViewport.Height / 2, rect.Height, $"A zero height claimed only {rect.Height}px.");
	}

	[TestMethod]
	public void Bucket_RoundsUpToTheGranularity()
	{
		Assert.AreEqual(64, ImGuiWidgets.Viewport3DTargets.Bucket(1));
		Assert.AreEqual(64, ImGuiWidgets.Viewport3DTargets.Bucket(64));
		Assert.AreEqual(128, ImGuiWidgets.Viewport3DTargets.Bucket(65));
	}

	[TestMethod]
	public void ShownPart_MapsTheWholeViewOntoTheTopLeftOfTheTarget()
	{
		Matrix4x4 shown = ImGuiWidgets.Viewport3DTargets.ShownPart(new Vector2(0.5f, 0.25f));

		// Clip-space corners, with a w other than one so the translation is checked as w-scaled.
		Vector4 topLeft = Vector4.Transform(new Vector4(-2f, 2f, 0.5f, 2f), shown);
		Vector4 bottomRight = Vector4.Transform(new Vector4(2f, -2f, 0.5f, 2f), shown);

		Assert.AreEqual(-1f, topLeft.X / topLeft.W, 1e-6f);
		Assert.AreEqual(1f, topLeft.Y / topLeft.W, 1e-6f);
		Assert.AreEqual(0f, bottomRight.X / bottomRight.W, 1e-6f, "The right edge should land halfway across the target.");
		Assert.AreEqual(0.5f, bottomRight.Y / bottomRight.W, 1e-6f, "The bottom edge should land a quarter of the way down.");
		Assert.AreEqual(0.25f, topLeft.Z / topLeft.W, 1e-6f, "Depth must pass through untouched.");
	}

	private static uint Single(System.Collections.Generic.IReadOnlyCollection<uint> ids)
	{
		Assert.HasCount(1, ids);
		foreach (uint id in ids)
		{
			return id;
		}

		return 0;
	}
}
