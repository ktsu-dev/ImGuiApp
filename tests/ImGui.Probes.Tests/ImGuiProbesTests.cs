// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Probes.Tests;

using System.Collections.Generic;
using System.Numerics;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Covers the parts of the registry that need no ImGui context. Marking itself is exercised against a
/// live context by the harness tests, since a mark reads the current item's rectangle.
/// </summary>
[TestClass]
public sealed class ImGuiProbesTests
{
	[TestCleanup]
	public void Cleanup()
	{
		// The registry is process-global, so each test leaves it as it found it.
		ImGuiProbes.SetProbe(null);
		ImGuiProbes.Enabled = true;
	}

	[TestMethod]
	public void IsRecording_WithNoProbeInstalled_IsFalse()
	{
		ImGuiProbes.SetProbe(null);

		Assert.IsFalse(ImGuiProbes.IsRecording, "Nothing should be recording in a production application.");
	}

	[TestMethod]
	public void IsRecording_WithProbeInstalled_IsTrue()
	{
		ImGuiProbes.SetProbe((_, _, _) => { });

		Assert.IsTrue(ImGuiProbes.IsRecording);
	}

	[TestMethod]
	public void IsRecording_WhenDisabled_IsFalseEvenWithAProbeInstalled()
	{
		ImGuiProbes.SetProbe((_, _, _) => { });
		ImGuiProbes.Enabled = false;

		Assert.IsFalse(ImGuiProbes.IsRecording, "The flag is a master switch, independent of the callback.");
	}

	[TestMethod]
	public void MarkRegion_WhenNotRecording_DoesNothing()
	{
		ImGuiProbes.SetProbe(null);

		// A library marks unconditionally, so this must be inert rather than throwing.
		ImGuiProbes.MarkRegion("ignored", Vector2.Zero, Vector2.One);
	}

	[TestMethod]
	public void MarkRegion_WhenRecording_ReportsTheRectangleVerbatim()
	{
		List<(string Name, Vector2 Min, Vector2 Max)> recorded = [];
		ImGuiProbes.SetProbe((name, min, max) => recorded.Add((name, min, max)));

		ImGuiProbes.MarkRegion("divider", new Vector2(10, 20), new Vector2(30, 40));

		Assert.AreEqual(1, recorded.Count);
		Assert.AreEqual(new Vector2(10, 20), recorded[0].Min);
		Assert.AreEqual(new Vector2(30, 40), recorded[0].Max);
	}

	[TestMethod]
	public void MarkRegion_WhenDisabled_RecordsNothing()
	{
		List<string> recorded = [];
		ImGuiProbes.SetProbe((name, _, _) => recorded.Add(name));
		ImGuiProbes.Enabled = false;

		ImGuiProbes.MarkRegion("suppressed", Vector2.Zero, Vector2.One);

		Assert.AreEqual(0, recorded.Count, "The flag should suppress marking without removing the callback.");
	}

	[TestMethod]
	public void Qualify_WithPushedScopes_JoinsThemInOrder()
	{
		ImGuiProbes.PushScope("outer");
		ImGuiProbes.PushScope("inner");

		try
		{
			// No ImGui context here, so the window portion is empty and only the scopes appear.
			Assert.AreEqual("outer/inner/item", ImGuiProbes.Qualify("item"));
		}
		finally
		{
			ImGuiProbes.PopScope();
			ImGuiProbes.PopScope();
		}
	}

	[TestMethod]
	public void Qualify_WithNoScopes_ReturnsTheNameUnchanged() =>
		Assert.AreEqual("item", ImGuiProbes.Qualify("item"));

	[TestMethod]
	public void PopScope_RemovesOnlyTheMostRecentScope()
	{
		ImGuiProbes.PushScope("outer");
		ImGuiProbes.PushScope("inner");
		ImGuiProbes.PopScope();

		try
		{
			Assert.AreEqual("outer/item", ImGuiProbes.Qualify("item"));
		}
		finally
		{
			ImGuiProbes.PopScope();
		}
	}

	[TestMethod]
	public void PopScope_WithNothingPushed_DoesNotThrow()
	{
		// An unbalanced pop should not corrupt state for whatever runs next.
		ImGuiProbes.PopScope();

		Assert.AreEqual("item", ImGuiProbes.Qualify("item"));
	}
}
