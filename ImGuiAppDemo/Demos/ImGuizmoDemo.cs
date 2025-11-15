// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGuiApp.Demo.Demos;

using System;
using System.Numerics;
using Hexa.NET.ImGui;
using Hexa.NET.ImGuizmo;

/// <summary>
/// Demo for ImGuizmo 3D manipulation gizmos
/// </summary>
internal sealed class ImGuizmoDemo : IDemoTab
{
	private Matrix4x4 gizmoTransform = Matrix4x4.Identity;
	private Matrix4x4 gizmoView = Matrix4x4.CreateLookAt(new Vector3(0, 0, 5), Vector3.Zero, Vector3.UnitY);
	private Matrix4x4 gizmoProjection;
	private ImGuizmoOperation gizmoOperation = ImGuizmoOperation.Translate;
	private ImGuizmoMode gizmoMode = ImGuizmoMode.Local;
	private bool gizmoEnabled = true;
	private float animationTime;

	public string TabName => "ImGuizmo 3D Gizmos";

	public void Update(float deltaTime)
	{
		animationTime += deltaTime;

		// Update gizmo view matrix for rotation demo
		float cameraAngle = animationTime * 0.2f;
		Vector3 cameraPos = new(MathF.Sin(cameraAngle) * 5f, 3f, MathF.Cos(cameraAngle) * 5f);
		gizmoView = Matrix4x4.CreateLookAt(cameraPos, Vector3.Zero, Vector3.UnitY);
	}

	public void Render()
	{
		if (ImGui.BeginTabItem(TabName))
		{
			ImGui.TextWrapped("ImGuizmo provides 3D manipulation gizmos for translate, rotate, and scale operations.");

			// Gizmo controls
			ImGui.SeparatorText("Gizmo Controls:.");
			ImGui.Checkbox("Enable Gizmo", ref gizmoEnabled);

			// Operation selection
			string[] operationNames = Enum.GetNames<ImGuizmoOperation>();
			ImGuizmoOperation[] operations = Enum.GetValues<ImGuizmoOperation>();
			int opIndex = Array.IndexOf(operations, gizmoOperation);
			if (ImGui.Combo("Operation", ref opIndex, operationNames, operationNames.Length))
			{
				gizmoOperation = operations[opIndex];
			}

			// Mode selection
			string[] modeNames = Enum.GetNames<ImGuizmoMode>();
			ImGuizmoMode[] modes = Enum.GetValues<ImGuizmoMode>();
			int modeIndex = Array.IndexOf(modes, gizmoMode);
			if (ImGui.Combo("Mode", ref modeIndex, modeNames, modeNames.Length))
			{
				gizmoMode = modes[modeIndex];
			}

			// Display transform matrix values
			ImGui.SeparatorText("Transform Matrix:");
			ImGui.Text($"[{gizmoTransform.M11:F2}, {gizmoTransform.M12:F2}, {gizmoTransform.M13:F2}, {gizmoTransform.M14:F2}]");
			ImGui.Text($"[{gizmoTransform.M21:F2}, {gizmoTransform.M22:F2}, {gizmoTransform.M23:F2}, {gizmoTransform.M24:F2}]");
			ImGui.Text($"[{gizmoTransform.M31:F2}, {gizmoTransform.M32:F2}, {gizmoTransform.M33:F2}, {gizmoTransform.M34:F2}]");
			ImGui.Text($"[{gizmoTransform.M41:F2}, {gizmoTransform.M42:F2}, {gizmoTransform.M43:F2}, {gizmoTransform.M44:F2}]");

			if (ImGui.Button("Reset Transform"))
			{
				gizmoTransform = Matrix4x4.Identity;
			}

			// Gizmo viewport - use a reasonable size instead of all available space
			Vector2 availableSize = ImGui.GetContentRegionAvail();
			Vector2 gizmoSize = new(availableSize.X, Math.Min(availableSize.Y, availableSize.X * 0.6f)); // Use width-based aspect ratio

			// Get position for gizmo (NO space reservation - see what happens!)
			Vector2 gizmoPos = ImGui.GetCursorScreenPos();

			// Update projection matrix based on gizmo size
			float aspectRatio = gizmoSize.Y > 0 ? gizmoSize.X / gizmoSize.Y : 1.0f;
			gizmoProjection = Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 4f, aspectRatio, 0.1f, 100f);

			// Set up ImGuizmo for this viewport
			if (gizmoEnabled)
			{
				// IMPORTANT: Set the drawlist and enable ImGuizmo and set the rect, before using any ImGuizmo functions
				ImGuizmo.SetDrawlist();
				ImGuizmo.Enable(true);
				ImGuizmo.SetRect(gizmoPos.X, gizmoPos.Y, gizmoSize.X, gizmoSize.Y);

				// Create view and projection matrices for the gizmo
				Matrix4x4 view = gizmoView;
				Matrix4x4 proj = gizmoProjection;

				// Draw grid
				Matrix4x4 identity = Matrix4x4.Identity;
				ImGuizmo.DrawGrid(ref view, ref proj, ref identity, 10.0f);

				// IMPORTANT: Use ID management for proper gizmo isolation
				ImGuizmo.PushID(0);

				// Draw the gizmo
				Matrix4x4 transform = gizmoTransform;
				if (ImGuizmo.Manipulate(ref view, ref proj, gizmoOperation, gizmoMode, ref transform))
				{
					gizmoTransform = transform;
				}

				ImGuizmo.PopID();
			}

			// Display gizmo state below the gizmo area
			if (gizmoEnabled)
			{
				ImGui.Text($"Gizmo Over: {ImGuizmo.IsOver()}");
				ImGui.Text($"Gizmo Using: {ImGuizmo.IsUsing()}");
			}

			ImGui.EndTabItem();
		}
	}
}
