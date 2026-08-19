// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Testing.Tests;

using System;
using System.Collections.Generic;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.App;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class ImGuiAppHarnessTests
{
	private static HarnessOptions Window() => new() { Width = 300, Height = 200 };

	/// <summary>Draws a fixed window so geometry lands at predictable coordinates.</summary>
	private static void Probe(Action body)
	{
		ImGui.SetNextWindowPos(Vector2.Zero);
		ImGui.SetNextWindowSize(new Vector2(240, 150));
		ImGui.Begin("probe", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoSavedSettings);
		body();
		ImGui.End();
	}

	[TestMethod]
	public void Step_InvokesTheApplicationRenderCallback()
	{
		int calls = 0;
		using ImGuiAppHarness harness = ImGuiAppHarness.Start(new ImGuiAppConfig { OnRender = _ => calls++ }, Window());

		harness.Step();

		Assert.AreEqual(1, calls, "One step should invoke OnRender exactly once.");
	}

	[TestMethod]
	public void Step_AdvancesTheFrameCount()
	{
		using ImGuiAppHarness harness = ImGuiAppHarness.Start(new ImGuiAppConfig(), Window());

		harness.Step(frames: 3);

		Assert.AreEqual(3, harness.FrameCount);
	}

	[TestMethod]
	public void Step_RunsWorkQueuedOnTheInvoker()
	{
		// An application that uploads a texture from a worker marshals it through the invoker, so a
		// harness that never pumped it would leave every asynchronous result invisible.
		bool ran = false;
		using ImGuiAppHarness harness = ImGuiAppHarness.Start(new ImGuiAppConfig(), Window());

		ImGuiApp.Invoker.Invoke(() => ran = true);
		harness.Step();

		Assert.IsTrue(ran, "Queued work should run during a step.");
	}

	[TestMethod]
	public void Step_RendersIntoTheTarget()
	{
		ImGuiAppConfig config = new() { OnRender = _ => Probe(() => ImGui.TextUnformatted("hello")) };
		using ImGuiAppHarness harness = ImGuiAppHarness.Start(config, Window());

		harness.Step(frames: 2);

		int changed = harness.Capture().CountPixels(p => p != harness.Options.ClearColor);

		Assert.IsTrue(changed > 100, $"Rendering a window should change many pixels, but only {changed} changed.");
	}

	[TestMethod]
	public void Step_ExceptionInRenderCallback_PropagatesWithFrameNumber()
	{
		ImGuiAppConfig config = new() { OnRender = _ => throw new InvalidOperationException("boom") };
		using ImGuiAppHarness harness = ImGuiAppHarness.Start(config, Window());

		HarnessFrameException error = Assert.ThrowsExactly<HarnessFrameException>(harness.Step);

		Assert.AreEqual(0, error.FrameNumber, "The first frame is frame zero.");
		Assert.IsInstanceOfType<InvalidOperationException>(error.InnerException);
	}

	[TestMethod]
	public void Start_WhileAnotherHarnessIsLive_Throws()
	{
		using ImGuiAppHarness first = ImGuiAppHarness.Start(new ImGuiAppConfig(), Window());

		Assert.ThrowsExactly<InvalidOperationException>(() => ImGuiAppHarness.Start(new ImGuiAppConfig(), Window()));
	}

	[TestMethod]
	public void Capture_BeforeAnyFrame_Throws()
	{
		using ImGuiAppHarness harness = ImGuiAppHarness.Start(new ImGuiAppConfig(), Window());

		Assert.ThrowsExactly<InvalidOperationException>(harness.Capture);
	}

	[TestMethod]
	public void Capture_IsACopyNotAView()
	{
		using ImGuiAppHarness harness = ImGuiAppHarness.Start(
			new ImGuiAppConfig(),
			new HarnessOptions { Width = 32, Height = 32, ClearColor = new Rgba32(1, 1, 1, 255) });

		harness.Step();
		CapturedFrame first = harness.Capture();
		Rgba32 asCaptured = first.GetPixel(0, 0);

		harness.Target.Clear(new Rgba32(9, 9, 9, 255));

		Assert.AreEqual(asCaptured, first.GetPixel(0, 0), "A capture must not change when later frames render.");
		Assert.AreNotEqual(new Rgba32(9, 9, 9, 255), first.GetPixel(0, 0), "The capture must not follow the live target.");
	}

	[TestMethod]
	public void StepUntil_PredicateBecomesTrue_StopsEarly()
	{
		int frames = 0;
		using ImGuiAppHarness harness = ImGuiAppHarness.Start(new ImGuiAppConfig { OnRender = _ => frames++ }, Window());

		bool reached = harness.StepUntil(() => frames >= 3, maxFrames: 100);

		Assert.IsTrue(reached, "The predicate became true, so StepUntil should report success.");
		Assert.AreEqual(3, frames, "It should stop as soon as the predicate holds, not spend the whole budget.");
	}

	[TestMethod]
	public void StepUntil_PredicateNeverTrue_ReturnsFalseAtTheBudget()
	{
		using ImGuiAppHarness harness = ImGuiAppHarness.Start(new ImGuiAppConfig(), Window());

		bool reached = harness.StepUntil(() => false, maxFrames: 5);

		Assert.IsFalse(reached, "An exhausted budget reports false rather than throwing.");
		Assert.AreEqual(5, harness.FrameCount, "It should spend exactly the budget.");
	}

	[TestMethod]
	public void StepUntil_PredicateAlreadyTrue_DoesNotStep()
	{
		using ImGuiAppHarness harness = ImGuiAppHarness.Start(new ImGuiAppConfig(), Window());

		bool reached = harness.StepUntil(() => true, maxFrames: 10);

		Assert.IsTrue(reached);
		Assert.AreEqual(0, harness.FrameCount, "An already-satisfied predicate should advance nothing.");
	}

	[TestMethod]
	public void Mouse_MoveTo_IsVisibleToTheApplication()
	{
		Vector2 seen = Vector2.Zero;
		using ImGuiAppHarness harness = ImGuiAppHarness.Start(
			new ImGuiAppConfig { OnRender = _ => seen = ImGui.GetIO().MousePos },
			Window());

		harness.Mouse.MoveTo(42, 24);
		harness.Step();

		Assert.AreEqual(42f, seen.X, "ImGui should report the injected mouse position.");
		Assert.AreEqual(24f, seen.Y);
	}

	[TestMethod]
	public void Mouse_Click_ActivatesAButton()
	{
		bool pressed = false;
		ImGuiAppConfig config = new()
		{
			OnRender = _ => Probe(() =>
			{
				if (ImGui.Button("press me", new Vector2(120, 30)))
				{
					pressed = true;
				}
			}),
		};

		using ImGuiAppHarness harness = ImGuiAppHarness.Start(config, Window());

		// One frame first so the button exists and ImGui knows its rectangle.
		harness.Step();
		harness.Mouse.Click(60, 25);

		Assert.IsTrue(pressed, "A click inside the button rectangle should activate it.");
	}

	[TestMethod]
	public void Mouse_Wheel_IsVisibleToTheApplication()
	{
		float wheel = 0;
		using ImGuiAppHarness harness = ImGuiAppHarness.Start(
			new ImGuiAppConfig { OnRender = _ => wheel = ImGui.GetIO().MouseWheel },
			Window());

		harness.Mouse.Wheel(10, 10, clicks: 3);

		Assert.AreEqual(3f, wheel, "Three wheel clicks should reach ImGui as a wheel delta of three.");
	}

	[TestMethod]
	public void Mouse_Drag_VisitsIntermediatePositionsAndFinishesAtTheDestination()
	{
		List<Vector2> positions = [];
		using ImGuiAppHarness harness = ImGuiAppHarness.Start(
			new ImGuiAppConfig { OnRender = _ => positions.Add(ImGui.GetIO().MousePos) },
			Window());

		harness.Mouse.Drag(10, 10, 90, 10, steps: 8);

		Assert.IsTrue(positions.Count >= 8, "A drag should render several intermediate frames.");
		Assert.AreEqual(90f, positions[^1].X, "A drag should finish at its destination.");
	}

	[TestMethod]
	public void Keyboard_Press_IsVisibleToTheApplication()
	{
		bool sawKey = false;
		using ImGuiAppHarness harness = ImGuiAppHarness.Start(
			new ImGuiAppConfig { OnRender = _ => sawKey |= ImGui.IsKeyPressed(ImGuiKey.Z) },
			Window());

		harness.Keyboard.Press(ImGuiKey.Z);

		Assert.IsTrue(sawKey, "The application should observe the injected key press.");
	}

	[TestMethod]
	public void Keyboard_PressWithCtrl_ReportsTheModifier()
	{
		bool sawCtrlZ = false;
		using ImGuiAppHarness harness = ImGuiAppHarness.Start(
			new ImGuiAppConfig { OnRender = _ => sawCtrlZ |= ImGui.GetIO().KeyCtrl && ImGui.IsKeyPressed(ImGuiKey.Z) },
			Window());

		harness.Keyboard.Press(ImGuiKey.Z, ctrl: true);

		Assert.IsTrue(sawCtrlZ, "Ctrl and Z should arrive together, which is what a shortcut needs.");
	}

	[TestMethod]
	public void Keyboard_Type_DeliversEveryCharacterInOrder()
	{
		string typed = string.Empty;
		ImGuiAppConfig config = new()
		{
			OnRender = _ =>
			{
				ImGuiIOPtr io = ImGui.GetIO();
				for (int i = 0; i < io.InputQueueCharacters.Size; i++)
				{
					typed += (char)io.InputQueueCharacters[i];
				}
			},
		};

		using ImGuiAppHarness harness = ImGuiAppHarness.Start(config, Window());
		harness.Keyboard.Type("ab.png");

		Assert.AreEqual("ab.png", typed, "Every character including the period should arrive in order.");
	}
}
