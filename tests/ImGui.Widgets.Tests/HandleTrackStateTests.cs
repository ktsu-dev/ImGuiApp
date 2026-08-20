// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests the interaction behind HandleTrack and RangeSlider. All pure — no ImGui context required.
/// </summary>
[TestClass]
public class HandleTrackStateTests
{
	[TestMethod]
	public void Normalize_ClampsHandlesIntoTheBounds()
	{
		float[] handles = [-3f, 0.5f, 9f];

		ImGuiWidgets.HandleTrackState.Normalize(handles, 0f, 1f, 0f);

		Assert.AreEqual(0f, handles[0], 1e-6f);
		Assert.AreEqual(0.5f, handles[1], 1e-6f);
		Assert.AreEqual(1f, handles[2], 1e-6f);
	}

	[TestMethod]
	public void Normalize_SortsHandlesAscending()
	{
		float[] handles = [0.9f, 0.1f, 0.5f];

		ImGuiWidgets.HandleTrackState.Normalize(handles, 0f, 1f, 0f);

		Assert.AreEqual(0.1f, handles[0], 1e-6f);
		Assert.AreEqual(0.5f, handles[1], 1e-6f);
		Assert.AreEqual(0.9f, handles[2], 1e-6f);
	}

	[TestMethod]
	public void Normalize_OpensCollapsedHandlesToTheMinimumGap()
	{
		float[] handles = [0.5f, 0.5f, 0.5f];

		ImGuiWidgets.HandleTrackState.Normalize(handles, 0f, 1f, 0.1f);

		// Tolerance because the gap is opened by repeated float addition; asserting the exact
		// bound would be testing rounding, not Normalize.
		Assert.IsGreaterThanOrEqualTo(0.1f - 1e-6f, handles[1] - handles[0], "The first pair is closer than minGap.");
		Assert.IsGreaterThanOrEqualTo(0.1f - 1e-6f, handles[2] - handles[1], "The second pair is closer than minGap.");
	}

	[TestMethod]
	public void Normalize_SettlesDownWhenHandlesAreClusteredAtTheTop()
	{
		// Three handles piled against the top with a minGap that fits comfortably. The upward
		// pass separates them; the downward settle ensures none escape upperBound.
		float[] handles = [1f, 1f, 1f];

		ImGuiWidgets.HandleTrackState.Normalize(handles, 0f, 1f, 0.25f);

		Assert.IsGreaterThanOrEqualTo(0f, handles[0], "A handle was pushed below lowerBound.");
		Assert.IsLessThanOrEqualTo(1f, handles[2], "A handle was pushed above upperBound.");
	}

	[TestMethod]
	public void Normalize_NarrowsMinGapWhenItCannotFit()
	{
		// Three handles in a span of 1 with minGap = 0.6 require spread of 1.2, which exceeds
		// the available range. The gap is narrowed to spread evenly (0.5 each) with all handles
		// kept strictly inside bounds.
		float[] handles = [1f, 1f, 1f];

		ImGuiWidgets.HandleTrackState.Normalize(handles, 0f, 1f, 0.6f);

		// Verify all handles are strictly inside bounds
		Assert.IsGreaterThanOrEqualTo(0f, handles[0], "First handle pushed below lowerBound.");
		Assert.IsLessThanOrEqualTo(1f, handles[2], "Last handle pushed above upperBound.");

		// Verify handles are evenly spread
		float gap1 = handles[1] - handles[0];
		float gap2 = handles[2] - handles[1];
		Assert.AreEqual(gap1, gap2, 1e-6f, "Gaps should be equal when minGap is narrowed.");
	}
}
