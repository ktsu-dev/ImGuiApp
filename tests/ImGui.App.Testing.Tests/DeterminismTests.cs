// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Testing.Tests;

using System.IO;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.App;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class DeterminismTests
{
	private static ImGuiAppConfig Scenario() => new()
	{
		OnRender = _ =>
		{
			ImGui.SetNextWindowPos(new Vector2(10, 10));
			ImGui.SetNextWindowSize(new Vector2(160, 90));
			ImGui.Begin("determinism", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoSavedSettings);
			ImGui.TextUnformatted("stable output");
			ImGui.Button("a button");
			ImGui.End();
		},
	};

	private static byte[] RunOnce()
	{
		using ImGuiAppHarness harness = ImGuiAppHarness.Start(Scenario(), new HarnessOptions { Width = 200, Height = 120 });
		harness.Step(frames: 3);
		return harness.Target.Pixels.ToArray();
	}

	[TestMethod]
	public void TwoRunsOfTheSameScenario_ProduceIdenticalPixels()
	{
		byte[] first = RunOnce();
		byte[] second = RunOnce();

		// This is the claim the whole approach rests on. Without it, every pixel measurement in
		// every downstream test is unreliable, and the suite would fail intermittently for reasons
		// nobody could reproduce.
		CollectionAssert.AreEqual(first, second, "The software renderer must produce identical output run to run.");
	}

	[TestMethod]
	public void RenderedFrame_IsNotBlank()
	{
		// A determinism test comparing two blank frames would pass while proving nothing at all.
		byte[] frame = RunOnce();

		bool anythingDrawn = false;
		for (int i = 0; i < frame.Length; i += 4)
		{
			if (frame[i] != 0 || frame[i + 1] != 0 || frame[i + 2] != 0)
			{
				anythingDrawn = true;
				break;
			}
		}

		Assert.IsTrue(anythingDrawn, "The scenario should actually draw something.");
	}

	[TestMethod]
	public void Harness_DoesNotWriteAnIniFile()
	{
		string ini = Path.Combine(Directory.GetCurrentDirectory(), "imgui.ini");
		bool existedBefore = File.Exists(ini);

		RunOnce();

		if (!existedBefore)
		{
			Assert.IsFalse(File.Exists(ini), "Persisting layout would let one test inherit another's state.");
		}
	}

	[TestMethod]
	public void FrameDelta_IsFixedRegardlessOfRealElapsedTime()
	{
		float seen = 0;
		ImGuiAppConfig config = new() { OnRender = delta => seen = delta };

		using ImGuiAppHarness harness = ImGuiAppHarness.Start(
			config,
			new HarnessOptions { Width = 64, Height = 64, FrameDelta = 1f / 30f });

		harness.Step();

		Assert.AreEqual(1f / 30f, seen, "The application must see the configured delta, not wall-clock time.");
	}
}
