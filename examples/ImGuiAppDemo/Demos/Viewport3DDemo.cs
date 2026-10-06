// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Examples.App.Demos;

using System.Numerics;
using Hexa.NET.ImGui;
using ktsu.ImGui.App;
using ktsu.ImGui.Probes;
using ktsu.ImGui.Widgets;

/// <summary>
/// Demo for the backend-agnostic 3D path: an orbit camera from <see cref="ImGuiWidgets.Viewport3DState"/>
/// driving a shaded cube drawn through <see cref="IRenderer3D"/> into an offscreen target, shown as
/// an ImGui image.
/// </summary>
/// <remarks>
/// <see cref="IRenderer3D"/> is an optional extension a backend may implement, so this demo asks
/// for it every frame with <see cref="ImGuiApp.TryGetRenderer3D"/> and, where the backend offers
/// none, falls back to projecting the cube's edges on the CPU through the same camera matrix and
/// drawing them as a wireframe. Either way the camera, and everything done to it by the mouse, is
/// the same.
/// </remarks>
internal sealed class Viewport3DDemo : IDemoTab
{
	private static readonly Vector2 ViewportSize = new(480, 320);

	// A unit cube's eight corners and, per face, its four corners in order and its colour.
	private static readonly Vector3[] Corners =
	[
		new(-1, -1, -1), new(1, -1, -1), new(1, 1, -1), new(-1, 1, -1),
		new(-1, -1, 1), new(1, -1, 1), new(1, 1, 1), new(-1, 1, 1),
	];

	private static readonly (int A, int B, int C, int D, Vector3 Normal, Vector3 Color)[] Faces =
	[
		(4, 5, 6, 7, Vector3.UnitZ, new(0.90f, 0.30f, 0.30f)),
		(1, 0, 3, 2, -Vector3.UnitZ, new(0.30f, 0.90f, 0.30f)),
		(5, 1, 2, 6, Vector3.UnitX, new(0.30f, 0.45f, 0.95f)),
		(0, 4, 7, 3, -Vector3.UnitX, new(0.95f, 0.85f, 0.30f)),
		(7, 6, 2, 3, Vector3.UnitY, new(0.85f, 0.35f, 0.90f)),
		(0, 1, 5, 4, -Vector3.UnitY, new(0.30f, 0.90f, 0.90f)),
	];

	private static readonly (int A, int B)[] Edges =
	[
		(0, 1), (1, 2), (2, 3), (3, 0),
		(4, 5), (5, 6), (6, 7), (7, 4),
		(0, 4), (1, 5), (2, 6), (3, 7),
	];

	private static readonly Vector3 LightDirection = Vector3.Normalize(new Vector3(0.4f, 0.8f, 0.6f));

	private float spinAngle;
	private bool spin = true;
	private bool preferCpuWireframe;

	// The target belongs to the renderer that created it. The demo tabs outlive a renderer (the UI
	// tests start a fresh one per test), so the target is recreated when the renderer changes.
	private IRenderer3D? targetOwner;
	private nint target;

	public string TabName => "3D Viewport";

	/// <summary>Gets the orbit camera, which a test reads to see what the mouse did to it.</summary>
	internal ImGuiWidgets.Viewport3DState Camera { get; } = new() { Distance = 6f, Yaw = 0.6f, Pitch = 0.4f };

	/// <summary>Gets a value indicating whether the last frame drew through <see cref="IRenderer3D"/>.</summary>
	internal bool LastFrameUsedRenderer3D { get; private set; }

	/// <summary>Puts the demo back as it starts, since demo tabs outlive a harness.</summary>
	internal void ResetState()
	{
		ResetCamera();
		spinAngle = 0f;
		spin = true;
		preferCpuWireframe = false;
	}

	private void ResetCamera()
	{
		Camera.Target = Vector3.Zero;
		Camera.Distance = 6f;
		Camera.Yaw = 0.6f;
		Camera.Pitch = 0.4f;
	}

	public void Update(float deltaTime)
	{
		if (spin)
		{
			spinAngle += deltaTime * 0.6f;
		}
	}

	public void Render()
	{
		if (DemoProbe.TabItem(TabName))
		{
			if (ImGui.BeginChild("##content"))
			{
				ImGui.TextWrapped("An orbit camera (Viewport3DState) over a cube drawn through IRenderer3D into an offscreen target. Drag with the left button to orbit, the right button to pan, and use the wheel to dolly.");

				DemoProbe.Checkbox("Spin the cube", ref spin);
				ImGui.SameLine();
				DemoProbe.Checkbox("Force CPU wireframe", ref preferCpuWireframe);
				ImGui.SameLine();
				if (DemoProbe.Button("Reset Camera"))
				{
					ResetCamera();
				}

				Matrix4x4 model = Matrix4x4.CreateRotationY(spinAngle);
				Matrix4x4 viewProjection = Camera.ViewProjection(ViewportSize.X / ViewportSize.Y);

				Vector2 origin = ImGui.GetCursorScreenPos();
				if (!preferCpuWireframe && ImGuiApp.TryGetRenderer3D(out IRenderer3D? renderer3D))
				{
					DrawThroughRenderer3D(renderer3D, model * viewProjection);
					LastFrameUsedRenderer3D = true;
				}
				else
				{
					DrawCpuWireframe(origin, model * viewProjection);
					LastFrameUsedRenderer3D = false;
				}

				HandleCameraInput(origin);

				ImGui.Text(LastFrameUsedRenderer3D
					? "Backend: IRenderer3D, drawn into an offscreen render target with depth."
					: "Backend: CPU wireframe (this backend has no IRenderer3D, or it was switched off).");
				ImGui.Text($"Yaw {Camera.Yaw:0.00}  Pitch {Camera.Pitch:0.00}  Distance {Camera.Distance:0.00}  Target {Camera.Target:0.00}");
			}
			ImGui.EndChild();

			ImGui.EndTabItem();
		}
	}

	private void DrawThroughRenderer3D(IRenderer3D renderer3D, Matrix4x4 modelViewProjection)
	{
		if (!ReferenceEquals(targetOwner, renderer3D))
		{
			targetOwner = renderer3D;
			target = renderer3D.CreateRenderTarget((int)ViewportSize.X, (int)ViewportSize.Y, depth: true);
		}

		renderer3D.Clear(target, Camera.ClearColor, 1f);

		// Shading is baked into the vertex colour: IRenderer3D has no lighting, by design.
		Vertex3D[] vertices = new Vertex3D[Faces.Length * 4];
		uint[] indices = new uint[Faces.Length * 6];
		Matrix4x4 rotation = Matrix4x4.CreateRotationY(spinAngle);
		for (int f = 0; f < Faces.Length; f++)
		{
			(int a, int b, int c, int d, Vector3 normal, Vector3 color) = Faces[f];
			float light = 0.35f + (0.65f * MathF.Max(0f, Vector3.Dot(Vector3.TransformNormal(normal, rotation), LightDirection)));
			uint packed = Pack(color * light);

			int v = f * 4;
			vertices[v] = new Vertex3D(Corners[a], Vector2.Zero, packed);
			vertices[v + 1] = new Vertex3D(Corners[b], Vector2.Zero, packed);
			vertices[v + 2] = new Vertex3D(Corners[c], Vector2.Zero, packed);
			vertices[v + 3] = new Vertex3D(Corners[d], Vector2.Zero, packed);

			int i = f * 6;
			indices[i] = (uint)v;
			indices[i + 1] = (uint)(v + 1);
			indices[i + 2] = (uint)(v + 2);
			indices[i + 3] = (uint)v;
			indices[i + 4] = (uint)(v + 2);
			indices[i + 5] = (uint)(v + 3);
		}

		renderer3D.Draw(target, vertices, indices, new DrawState3D
		{
			ModelViewProjection = modelViewProjection,
			Depth = DepthMode.TestAndWrite,
			Cull = CullMode.None,
			Blend = BlendMode.Opaque,
			Topology = PrimitiveTopology.TriangleList,
		});

		ImGuiWidgets.Image(renderer3D.GetTargetTexture(target), ViewportSize);
	}

	private void DrawCpuWireframe(Vector2 origin, Matrix4x4 modelViewProjection)
	{
		ImDrawListPtr drawList = ImGui.GetWindowDrawList();
		Vector4 clear = Camera.ClearColor;
		drawList.AddRectFilled(origin, origin + ViewportSize, ImGui.ColorConvertFloat4ToU32(clear));

		uint lineColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.9f, 0.9f, 0.9f, 1f));
		foreach ((int a, int b) in Edges)
		{
			if (Project(Corners[a], modelViewProjection, origin, out Vector2 from)
				&& Project(Corners[b], modelViewProjection, origin, out Vector2 to))
			{
				drawList.AddLine(from, to, lineColor, 2f);
			}
		}

		ImGui.Dummy(ViewportSize);
	}

	/// <summary>
	/// Takes a model-space point to the screen, or reports false for a point behind the eye, whose
	/// divide would send it to the wrong side of the view.
	/// </summary>
	private static bool Project(Vector3 point, Matrix4x4 modelViewProjection, Vector2 origin, out Vector2 screen)
	{
		Vector4 clip = Vector4.Transform(new Vector4(point, 1f), modelViewProjection);
		if (clip.W <= 1e-5f)
		{
			screen = default;
			return false;
		}

		Vector2 ndc = new(clip.X / clip.W, clip.Y / clip.W);
		screen = origin + new Vector2((ndc.X + 1f) * 0.5f * ViewportSize.X, (1f - ndc.Y) * 0.5f * ViewportSize.Y);
		return true;
	}

	/// <summary>
	/// Converts the mouse over the viewport into camera moves. The image was the last item
	/// submitted, so an invisible button laid over the same rectangle captures the drags.
	/// </summary>
	private void HandleCameraInput(Vector2 origin)
	{
		Vector2 after = ImGui.GetCursorScreenPos();
		ImGui.SetCursorScreenPos(origin);
		ImGui.InvisibleButton("Viewport", ViewportSize, ImGuiButtonFlags.MouseButtonLeft | ImGuiButtonFlags.MouseButtonRight);
		ImGuiProbes.MarkItem("Viewport");

		ImGuiIOPtr io = ImGui.GetIO();
		if (ImGui.IsItemActive())
		{
			Vector2 delta = io.MouseDelta;
			if (ImGui.IsMouseDown(ImGuiMouseButton.Left))
			{
				Camera.Orbit(-delta.X * 0.01f, delta.Y * 0.01f);
			}
			else if (ImGui.IsMouseDown(ImGuiMouseButton.Right))
			{
				Camera.Pan(-delta.X / ViewportSize.Y, delta.Y / ViewportSize.Y);
			}
		}

		// Dolly is multiplicative, so a frame with no wheel input scales the distance by exactly one.
		if (ImGui.IsItemHovered())
		{
			Camera.Dolly(io.MouseWheel * 0.1f);
		}

		ImGui.SetCursorScreenPos(after);
	}

	/// <summary>Packs a colour the way ImGui packs one: red in the low byte, opaque.</summary>
	private static uint Pack(Vector3 color)
	{
		uint r = (uint)(Math.Clamp(color.X, 0f, 1f) * 255f);
		uint g = (uint)(Math.Clamp(color.Y, 0f, 1f) * 255f);
		uint b = (uint)(Math.Clamp(color.Z, 0f, 1f) * 255f);
		return r | (g << 8) | (b << 16) | (0xFFu << 24);
	}
}
