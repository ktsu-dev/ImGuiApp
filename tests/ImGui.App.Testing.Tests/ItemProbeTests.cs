// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Testing.Tests;

using System;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.App;
using ktsu.ImGui.Probes;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class ItemProbeTests
{
	private static HarnessOptions Window() => new() { Width = 300, Height = 200 };

	private static ImGuiAppConfig ConfigWithButton(Action onPressed) => new()
	{
		OnRender = _ =>
		{
			ImGui.SetNextWindowPos(Vector2.Zero);
			ImGui.SetNextWindowSize(new Vector2(240, 150));
			ImGui.Begin("probe", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoSavedSettings);

			if (ImGui.Button("press me", new Vector2(120, 30)))
			{
				onPressed();
			}

			ImGuiApp.MarkItem("the.button");

			ImGui.End();
		},
	};

	[TestMethod]
	public void Rect_AfterMarking_ReportsTheItemRectangle()
	{
		using ImGuiAppHarness harness = ImGuiAppHarness.Start(ConfigWithButton(() => { }), Window());
		harness.Step();

		Rectangle? rect = harness.Probe.Rect("the.button");

		Assert.IsNotNull(rect, "A marked item should be resolvable by name.");
		Assert.IsTrue(rect.Value.Width is >= 118 and <= 122, $"The button was 120 wide, but the probe reported {rect.Value.Width}.");
		Assert.IsTrue(rect.Value.Height is >= 28 and <= 32, $"The button was 30 tall, but the probe reported {rect.Value.Height}.");
	}

	[TestMethod]
	public void Click_ByName_ActivatesTheItem()
	{
		bool pressed = false;
		using ImGuiAppHarness harness = ImGuiAppHarness.Start(ConfigWithButton(() => pressed = true), Window());
		harness.Step();

		harness.Click("the.button");

		Assert.IsTrue(pressed, "Clicking by name should activate the item without the test naming a coordinate.");
	}

	[TestMethod]
	public void Click_UnknownName_ThrowsListingKnownNames()
	{
		using ImGuiAppHarness harness = ImGuiAppHarness.Start(ConfigWithButton(() => { }), Window());
		harness.Step();

		ArgumentException error = Assert.ThrowsExactly<ArgumentException>(() => harness.Click("no.such.item"));

		Assert.IsTrue(
			error.Message.Contains("the.button", StringComparison.Ordinal),
			"The failure should list what was seen, since a typo is the usual cause.");
	}

	[TestMethod]
	public void Click_ItemNotDrawnThisFrame_ThrowsRatherThanClickingStalePosition()
	{
		bool visible = true;
		bool pressed = false;
		ImGuiAppConfig config = new()
		{
			OnRender = _ =>
			{
				ImGui.SetNextWindowPos(Vector2.Zero);
				ImGui.SetNextWindowSize(new Vector2(240, 150));
				ImGui.Begin("probe", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoSavedSettings);

				if (visible)
				{
					if (ImGui.Button("press me", new Vector2(120, 30)))
					{
						pressed = true;
					}

					ImGuiApp.MarkItem("the.button");
				}

				ImGui.End();
			},
		};

		using ImGuiAppHarness harness = ImGuiAppHarness.Start(config, Window());
		harness.Step();

		visible = false;
		harness.Step();

		// Clicking a stale rectangle would hit whatever has since moved there, and could pass while
		// testing nothing at all, which is worse than failing.
		Assert.ThrowsExactly<InvalidOperationException>(() => harness.Click("the.button"));
		Assert.IsFalse(pressed);
	}

	[TestMethod]
	public void MarkItem_WithNoProbeInstalled_DoesNothing()
	{
		// Production applications call MarkItem with no harness present. It must be inert, not throw.
		ImGuiProbes.SetProbe(null);

		ImGuiApp.MarkItem("ignored");
	}

	[TestMethod]
	public void Enabled_False_SuppressesMarkingWithoutRemovingTheProbe()
	{
		using ImGuiAppHarness harness = ImGuiAppHarness.Start(ConfigWithButton(() => { }), Window());

		try
		{
			ImGuiProbes.Enabled = false;
			harness.Step();

			Assert.IsNull(harness.Probe.Rect("the.button"), "The flag should suppress marking while the probe stays installed.");

			ImGuiProbes.Enabled = true;
			harness.Step();

			Assert.IsNotNull(harness.Probe.Rect("the.button"), "Re-enabling should resume marking.");
		}
		finally
		{
			ImGuiProbes.Enabled = true;
		}
	}

	[TestMethod]
	public void Click_AmbiguousName_ThrowsRatherThanPickingOne()
	{
		// Two distinct items sharing a name, which is easy to hit once libraries mark automatically.
		ImGuiAppConfig config = new()
		{
			OnRender = _ =>
			{
				ImGui.SetNextWindowPos(Vector2.Zero);
				ImGui.SetNextWindowSize(new Vector2(240, 150));
				ImGui.Begin("probe", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoSavedSettings);

				ImGui.Button("first", new Vector2(80, 20));
				ImGuiApp.MarkItem("duplicate");

				ImGui.Button("second", new Vector2(80, 20));
				ImGuiApp.MarkItem("duplicate");

				ImGui.End();
			},
		};

		using ImGuiAppHarness harness = ImGuiAppHarness.Start(config, Window());
		harness.Step();

		Assert.IsTrue(harness.Probe.IsAmbiguous("duplicate"), "Two items marked under one name in a frame is ambiguous.");
		Assert.ThrowsExactly<InvalidOperationException>(() => harness.Click("duplicate"));
	}
}
