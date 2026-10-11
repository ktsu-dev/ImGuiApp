// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Testing.Tests;

using System;
using System.Numerics;

using Hexa.NET.ImGui;
using Hexa.NET.ImGuizmo;
using Hexa.NET.ImNodes;
using Hexa.NET.ImPlot;

using ktsu.ImGui.App;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// <see cref="ImGuiApp.EnableExtensions(ImGuiAppExtensions)"/> under the harness: an application
/// that launches with no extension turns one on the first time it needs it, and draws with it.
/// </summary>
[TestClass]
public sealed class EnableExtensionsTests
{
	private static HarnessOptions Window() => new() { Width = 400, Height = 300 };

	/// <summary>Draws a fixed window holding a node editor with one node.</summary>
	private static void DrawNodeEditor()
	{
		ImGui.SetNextWindowPos(Vector2.Zero);
		ImGui.SetNextWindowSize(new Vector2(380, 280));
		ImGui.Begin("nodes", ImGuiWindowFlags.NoSavedSettings);
		ImNodes.BeginNodeEditor();
		ImNodes.BeginNode(1);
		ImGui.TextUnformatted("node");
		ImNodes.EndNode();
		ImNodes.EndNodeEditor();
		ImGui.End();
	}

	private static void AssertNoImGuiErrors(string what) =>
		Assert.AreEqual(0, ImGui.GetCurrentContext().ErrorCountCurrentFrame, $"ImGui reported {what} as misuse.");

	[TestMethod]
	public void EnableExtensions_BetweenFrames_LetsAnAppLaunchedWithNoneDrawWithImNodes()
	{
		bool editorOpen = false;
		using ImGuiAppHarness harness = ImGuiAppHarness.Start(
			new ImGuiAppConfig
			{
				Extensions = ImGuiAppExtensions.None,
				OnRender = _ =>
				{
					if (editorOpen)
					{
						DrawNodeEditor();
					}
				},
			},
			Window());

		harness.Step();
		Assert.IsFalse(ImGuiExtensionManager.IsImNodesContextCreated, "An application launched with None has no ImNodes context.");

		ImGuiAppExtensions ready = ImGuiApp.EnableExtensions(ImGuiAppExtensions.ImNodes);
		editorOpen = true;
		harness.Step(2);

		Assert.AreEqual(ImGuiAppExtensions.ImNodes, ready);
		Assert.AreEqual(ImGuiAppExtensions.ImNodes, ImGuiExtensionManager.Enabled);
		Assert.IsTrue(ImGuiExtensionManager.IsImNodesContextCreated);
		AssertNoImGuiErrors("the node editor");
	}

	[TestMethod]
	public void EnableExtensions_InsideOnRender_BeforeTheFirstNodeEditorDraws()
	{
		// The case the API exists for: a node editor window opening for the first time turns ImNodes
		// on in the same frame it draws with it.
		ImGuiAppExtensions ready = ImGuiAppExtensions.None;
		using ImGuiAppHarness harness = ImGuiAppHarness.Start(
			new ImGuiAppConfig
			{
				Extensions = ImGuiAppExtensions.None,
				OnRender = _ =>
				{
					ready = ImGuiApp.EnableExtensions(ImGuiAppExtensions.ImNodes);
					DrawNodeEditor();
				},
			},
			Window());

		harness.Step(3);

		Assert.AreEqual(ImGuiAppExtensions.ImNodes, ready);
		Assert.IsTrue(ImGuiExtensionManager.IsImNodesContextCreated);
		AssertNoImGuiErrors("the node editor");
	}

	[TestMethod]
	public void EnableExtensions_Twice_IsHarmless()
	{
		using ImGuiAppHarness harness = ImGuiAppHarness.Start(
			new ImGuiAppConfig { Extensions = ImGuiAppExtensions.None, OnRender = _ => DrawNodeEditorIfEnabled() },
			Window());

		ImGuiAppExtensions first = ImGuiApp.EnableExtensions(ImGuiAppExtensions.ImNodes);
		harness.Step();
		ImNodesContextPtr context = ImNodes.GetCurrentContext();

		ImGuiAppExtensions second = ImGuiApp.EnableExtensions(ImGuiAppExtensions.ImNodes);
		harness.Step();

		Assert.AreEqual(first, second);
		Assert.AreEqual(ImGuiAppExtensions.ImNodes, ImGuiExtensionManager.Enabled);
		Assert.AreEqual(context, ImNodes.GetCurrentContext(), "Enabling again must not replace the ImNodes context.");
		AssertNoImGuiErrors("the node editor");
	}

	private static void DrawNodeEditorIfEnabled()
	{
		if (ImGuiExtensionManager.IsImNodesContextCreated)
		{
			DrawNodeEditor();
		}
	}

	[TestMethod]
	public void EnableExtensions_ImNodes_KeepsAnAlreadyEnabledImPlot()
	{
		bool drawNodes = false;
		using ImGuiAppHarness harness = ImGuiAppHarness.Start(
			new ImGuiAppConfig
			{
				Extensions = ImGuiAppExtensions.ImPlot,
				OnRender = _ =>
				{
					ImGui.SetNextWindowPos(Vector2.Zero);
					ImGui.SetNextWindowSize(new Vector2(380, 140));
					ImGui.Begin("plot", ImGuiWindowFlags.NoSavedSettings);
					if (ImPlot.BeginPlot("p", new Vector2(300, 100)))
					{
						ImPlot.EndPlot();
					}

					ImGui.End();

					if (drawNodes)
					{
						DrawNodeEditor();
					}
				},
			},
			Window());

		harness.Step();
		Assert.IsTrue(ImGuiExtensionManager.IsImPlotContextCreated);

		ImGuiAppExtensions ready = ImGuiApp.EnableExtensions(ImGuiAppExtensions.ImNodes);
		drawNodes = true;
		harness.Step(2);

		Assert.AreEqual(ImGuiAppExtensions.ImNodes, ready);
		Assert.AreEqual(ImGuiAppExtensions.ImNodes | ImGuiAppExtensions.ImPlot, ImGuiExtensionManager.Enabled);
		Assert.IsTrue(ImGuiExtensionManager.IsImPlotContextCreated, "Enabling ImNodes must not drop ImPlot.");
		Assert.IsTrue(ImGuiExtensionManager.IsImNodesContextCreated);
		AssertNoImGuiErrors("the plot and the node editor");
	}

	[TestMethod]
	public void EnableExtensions_ImGuizmo_InsideOnRender_CanManipulateInTheSameFrame()
	{
		// ImGuizmo draws into the window its BeginFrame opens, and the per-frame BeginFrame has
		// already run by the time OnRender does. Without the catch-up BeginFrame, Manipulate would
		// draw through a null draw list in native code.
		float[] view = ToArray(Matrix4x4.CreateLookAt(new Vector3(0, 0, 5), Vector3.Zero, Vector3.UnitY));
		float[] projection = ToArray(Matrix4x4.CreatePerspectiveFieldOfView(1f, 4f / 3f, 0.1f, 100f));
		float[] model = ToArray(Matrix4x4.Identity);
		ImGuiAppExtensions ready = ImGuiAppExtensions.None;

		using ImGuiAppHarness harness = ImGuiAppHarness.Start(
			new ImGuiAppConfig
			{
				Extensions = ImGuiAppExtensions.None,
				OnRender = _ =>
				{
					ready = ImGuiApp.EnableExtensions(ImGuiAppExtensions.ImGuizmo);
					ImGuizmo.SetRect(0, 0, 400, 300);
					ImGuizmo.Manipulate(ref view[0], ref projection[0], ImGuizmoOperation.Translate, ImGuizmoMode.World, ref model[0]);
				},
			},
			Window());

		harness.Step(2);

		Assert.AreEqual(ImGuiAppExtensions.ImGuizmo, ready);
		Assert.IsTrue(ImGuiExtensionManager.IsImGuizmoAvailable);
		Assert.AreEqual(ImGuiAppExtensions.ImGuizmo, ImGuiExtensionManager.Enabled);
		AssertNoImGuiErrors("the gizmo");
	}

	[TestMethod]
	public void EnableExtensions_BeforeLaunch_Throws()
	{
		// A harness that has been disposed has torn its extensions down, which is the state of an
		// application that has not launched.
		ImGuiAppHarness.Start(new ImGuiAppConfig(), Window()).Dispose();

		Assert.ThrowsExactly<InvalidOperationException>(() => ImGuiApp.EnableExtensions(ImGuiAppExtensions.ImNodes));
	}

	private static float[] ToArray(Matrix4x4 m) =>
	[
		m.M11, m.M12, m.M13, m.M14,
		m.M21, m.M22, m.M23, m.M24,
		m.M31, m.M32, m.M33, m.M34,
		m.M41, m.M42, m.M43, m.M44,
	];
}
