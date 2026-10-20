// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Examples.Widgets;

using System;
using System.Numerics;
using Hexa.NET.ImGui;
using ktsu.ImGui.Rendering;
using ktsu.ImGui.Widgets;

/// <summary>
/// A 3D viewport: a shaded cube under an orbit camera, drawn through the host's 3D renderer.
/// </summary>
internal static class Viewport3DDemo
{
	private static readonly Vector3[] Corners =
	[
		new(-1, -1, -1), new(1, -1, -1), new(1, 1, -1), new(-1, 1, -1),
		new(-1, -1, 1), new(1, -1, 1), new(1, 1, 1), new(-1, 1, 1),
	];

	// Each face's four corners counter-clockwise from outside, its outward normal, and its colour.
	private static readonly (int A, int B, int C, int D, Vector3 Normal, Vector3 Color)[] Faces =
	[
		(4, 5, 6, 7, Vector3.UnitZ, new(0.90f, 0.30f, 0.30f)),
		(1, 0, 3, 2, -Vector3.UnitZ, new(0.30f, 0.90f, 0.30f)),
		(5, 1, 2, 6, Vector3.UnitX, new(0.30f, 0.45f, 0.95f)),
		(0, 4, 7, 3, -Vector3.UnitX, new(0.95f, 0.85f, 0.30f)),
		(7, 6, 2, 3, Vector3.UnitY, new(0.85f, 0.35f, 0.90f)),
		(0, 1, 5, 4, -Vector3.UnitY, new(0.30f, 0.90f, 0.90f)),
	];

	private static readonly Vector3 LightDirection = Vector3.Normalize(new Vector3(0.4f, 0.8f, 0.6f));

	private static float spinAngle;

	/// <summary>Gets the demo's camera, which a test reads to see what the mouse did.</summary>
	internal static ImGuiWidgets.Viewport3DState Camera { get; } = new() { Distance = 6f, Yaw = 0.6f, Pitch = 0.4f };

	/// <summary>Gets or sets a value indicating whether the cube spins.</summary>
	internal static bool Spin { get; set; } = true;

	/// <summary>Gets a value indicating whether the last frame drew through a 3D renderer.</summary>
	internal static bool LastFrameDrew { get; private set; }

	/// <summary>
	/// Returns this section's state to its starting values. The demo keeps its state in statics, which
	/// outlive a harness.
	/// </summary>
	internal static void ResetState()
	{
		Camera.Target = Vector3.Zero;
		Camera.Distance = 6f;
		Camera.Yaw = 0.6f;
		Camera.Pitch = 0.4f;
		spinAngle = 0f;
		Spin = true;
		LastFrameDrew = false;
	}

	/// <summary>Draws the demo section.</summary>
	internal static void Show()
	{
		if (DemoProbe.Header("Viewport3D"))
		{
			ImGui.TextUnformatted("A 3D view drawn through the host's renderer, with an orbit camera:");
			ImGui.BulletText("Drag to orbit");
			ImGui.BulletText("Middle-drag or Shift+drag to pan");
			ImGui.BulletText("Scroll to move closer or further away");
			ImGui.Separator();

			bool spin = Spin;
			if (DemoProbe.Checkbox("Spin the cube", ref spin))
			{
				Spin = spin;
			}

			ImGui.SameLine();
			if (DemoProbe.Button("Reset camera"))
			{
				Camera.Target = Vector3.Zero;
				Camera.Distance = 6f;
				Camera.Yaw = 0.6f;
				Camera.Pitch = 0.4f;
			}

			if (Spin)
			{
				spinAngle += ImGui.GetIO().DeltaTime * 0.6f;
			}

			LastFrameDrew = ImGuiWidgets.Viewport3D("viewport_3d_demo", new Vector2(ImGui.GetContentRegionAvail().X, 300f), Camera, DrawCube);
			if (!LastFrameDrew)
			{
				ImGui.TextDisabled("This backend has no 3D renderer, so the viewport is empty.");
			}
		}
	}

	private static void DrawCube(ImGuiWidgets.Viewport3DFrame frame)
	{
		Matrix4x4 model = Matrix4x4.CreateRotationY(spinAngle);
		Vertex3D[] vertices = new Vertex3D[Faces.Length * 4];
		uint[] indices = new uint[Faces.Length * 6];

		for (int f = 0; f < Faces.Length; f++)
		{
			(int a, int b, int c, int d, Vector3 normal, Vector3 color) = Faces[f];

			// The renderer has no lighting, by design: the shading is baked into the vertex colour.
			float light = 0.35f + (0.65f * MathF.Max(0f, Vector3.Dot(Vector3.TransformNormal(normal, model), LightDirection)));
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

		frame.Renderer.Draw(frame.Target, vertices, indices, new DrawState3D
		{
			ModelViewProjection = model * frame.ViewProjection,
			Depth = DepthMode.TestAndWrite,
			Cull = CullMode.None,
		});
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
