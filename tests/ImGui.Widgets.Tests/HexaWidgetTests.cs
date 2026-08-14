// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests for the pure helpers backing the Hexa-delegated widgets. The draw paths need a live
/// ImGui context and are verified visually in ImGuiWidgetsDemo; these cover the argument
/// resolution and marshalling logic that can run without one.
/// </summary>
[TestClass]
public sealed class HexaWidgetTests
{
	[TestMethod]
	public void ResolveSplitterMetrics_ZeroThickness_DerivesFromGrabMinSize()
	{
		(float thickness, float tolerance) = ImGuiWidgets.ResolveSplitterMetrics(0f, 4f, 8f);

		Assert.AreEqual(2f, thickness);
		Assert.AreEqual(4f, tolerance);
	}

	[TestMethod]
	public void ResolveSplitterMetrics_ZeroTolerance_DerivesFromGrabMinSize()
	{
		(float thickness, float tolerance) = ImGuiWidgets.ResolveSplitterMetrics(3f, 0f, 8f);

		Assert.AreEqual(3f, thickness);
		Assert.AreEqual(8f, tolerance);
	}

	[TestMethod]
	public void ResolveSplitterMetrics_BothZero_DerivesBoth()
	{
		(float thickness, float tolerance) = ImGuiWidgets.ResolveSplitterMetrics(0f, 0f, 16f);

		Assert.AreEqual(4f, thickness);
		Assert.AreEqual(16f, tolerance);
	}

	[TestMethod]
	public void ResolveSplitterMetrics_BothSupplied_PassesThroughUntouched()
	{
		(float thickness, float tolerance) = ImGuiWidgets.ResolveSplitterMetrics(1.5f, 12f, 8f);

		Assert.AreEqual(1.5f, thickness);
		Assert.AreEqual(12f, tolerance);
	}

	[TestMethod]
	public void VerticalSplitter_NullId_ThrowsBeforeTouchingImGui()
	{
		float width = 100f;

		// Ensure.NotNull must run before any ImGui call, or this would crash with no context
		// rather than throwing.
		Assert.ThrowsExactly<ArgumentNullException>(() =>
		{
			float local = width;
			ImGuiWidgets.VerticalSplitter(null!, ref local);
		});
	}

	[TestMethod]
	public void VerticalSplitter_MinGreaterThanMax_ThrowsBeforeTouchingImGui()
	{
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
		{
			float local = 100f;
			ImGuiWidgets.VerticalSplitter("id", ref local, minWidth: 50f, maxWidth: 10f);
		});
	}

	[TestMethod]
	public void HorizontalSplitter_MinGreaterThanMax_ThrowsBeforeTouchingImGui()
	{
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
		{
			float local = 100f;
			ImGuiWidgets.HorizontalSplitter("id", ref local, minHeight: 50f, maxHeight: 10f);
		});
	}
}
