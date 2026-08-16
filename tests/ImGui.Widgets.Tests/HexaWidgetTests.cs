// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using System.Linq;

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

	private enum PlainEnum
	{
		First,
		Second,
		Third,
	}

	private enum ExplicitValueEnum
	{
		Ten = 10,
		Twenty = 20,
	}

	[Flags]
	private enum FlagsEnum
	{
		None = 0,
		Alpha = 1 << 0,
		Beta = 1 << 1,
	}

	[TestMethod]
	public void EnumComboNames_PlainEnum_ReturnsAllMembersInDeclarationOrder()
	{
		IReadOnlyList<string> names = ImGuiWidgets.EnumComboNames<PlainEnum>();

		// CA1861: hoist the expected literal array out of the call so it isn't flagged as a
		// constant-array argument.
		string[] expected = ["First", "Second", "Third"];
		CollectionAssert.AreEqual(expected, names.ToArray());
	}

	[TestMethod]
	public void EnumComboNames_ExplicitValueEnum_ReturnsAllMembers()
	{
		IReadOnlyList<string> names = ImGuiWidgets.EnumComboNames<ExplicitValueEnum>();

		// CA1861: hoist the expected literal array out of the call so it isn't flagged as a
		// constant-array argument.
		string[] expected = ["Ten", "Twenty"];
		CollectionAssert.AreEqual(expected, names.ToArray());
	}

	[TestMethod]
	public void EnumComboNames_FlagsEnum_ReturnsDeclaredMembersNotCombinations()
	{
		IReadOnlyList<string> names = ImGuiWidgets.EnumComboNames<FlagsEnum>();

		// CA1861: hoist the expected literal array out of the call so it isn't flagged as a
		// constant-array argument. Enum.GetValues<T>() orders by underlying value, which happens
		// to coincide with declaration order for this enum; that ordering is a runtime guarantee,
		// not one this code makes.
		string[] expected = ["None", "Alpha", "Beta"];
		CollectionAssert.AreEqual(expected, names.ToArray());
	}

	[TestMethod]
	public void EnumCombo_NullLabel_ThrowsBeforeTouchingImGui()
	{
		Assert.ThrowsExactly<ArgumentNullException>(() =>
		{
			PlainEnum local = PlainEnum.First;
			ImGuiWidgets.EnumCombo(null!, ref local);
		});
	}

	[TestMethod]
	public void FlattenCaptions_EmptyList_ReturnsEmpty()
	{
		(byte[] bytes, int[] offsets) = ImGuiWidgets.FlattenCaptions([]);

		Assert.AreEqual(0, bytes.Length);
		Assert.AreEqual(0, offsets.Length);
	}

	[TestMethod]
	public void FlattenCaptions_SingleAsciiCaption_WritesNullTerminatedBytes()
	{
		(byte[] bytes, int[] offsets) = ImGuiWidgets.FlattenCaptions(
			[new FlameGraphSample(0f, 1f, 0, "ab")]);

		// CA1861: hoist the expected literal array out of the call so it isn't flagged as a
		// constant-array argument.
		byte[] expectedBytes = [0x61, 0x62, 0x00];
		int[] expectedOffsets = [0];
		CollectionAssert.AreEqual(expectedBytes, bytes);
		CollectionAssert.AreEqual(expectedOffsets, offsets);
	}

	[TestMethod]
	public void FlattenCaptions_MultipleCaptions_OffsetsPointPastEachTerminator()
	{
		(byte[] bytes, int[] offsets) = ImGuiWidgets.FlattenCaptions(
		[
			new FlameGraphSample(0f, 1f, 0, "ab"),
			new FlameGraphSample(1f, 2f, 1, "c"),
		]);

		// "ab\0c\0"
		// CA1861: hoist the expected literal array out of the call so it isn't flagged as a
		// constant-array argument.
		byte[] expectedBytes = [0x61, 0x62, 0x00, 0x63, 0x00];
		int[] expectedOffsets = [0, 3];
		CollectionAssert.AreEqual(expectedBytes, bytes);
		CollectionAssert.AreEqual(expectedOffsets, offsets);
	}

	[TestMethod]
	public void FlattenCaptions_MultiByteUtf8Caption_UsesByteLengthNotCharLength()
	{
		// U+00E9 encodes as two bytes (0xC3 0xA9), so the next offset must be 3, not 2.
		(byte[] bytes, int[] offsets) = ImGuiWidgets.FlattenCaptions(
		[
			new FlameGraphSample(0f, 1f, 0, "é"),
			new FlameGraphSample(1f, 2f, 1, "x"),
		]);

		// CA1861: hoist the expected literal array out of the call so it isn't flagged as a
		// constant-array argument.
		byte[] expectedBytes = [0xC3, 0xA9, 0x00, 0x78, 0x00];
		int[] expectedOffsets = [0, 3];
		CollectionAssert.AreEqual(expectedBytes, bytes);
		CollectionAssert.AreEqual(expectedOffsets, offsets);
	}

	[TestMethod]
	public void FlattenCaptions_EmptyStringCaption_WritesOnlyTerminator()
	{
		(byte[] bytes, int[] offsets) = ImGuiWidgets.FlattenCaptions(
		[
			new FlameGraphSample(0f, 1f, 0, ""),
			new FlameGraphSample(1f, 2f, 1, "y"),
		]);

		// CA1861: hoist the expected literal array out of the call so it isn't flagged as a
		// constant-array argument.
		byte[] expectedBytes = [0x00, 0x79, 0x00];
		int[] expectedOffsets = [0, 1];
		CollectionAssert.AreEqual(expectedBytes, bytes);
		CollectionAssert.AreEqual(expectedOffsets, offsets);
	}

	[TestMethod]
	public void ResolveFlameGraphSelection_InRange_PassesThrough()
	{
		Assert.AreEqual(2, ImGuiWidgets.ResolveFlameGraphSelection(2, 5));
	}

	[TestMethod]
	public void ResolveFlameGraphSelection_Negative_ClampsToMinusOne()
	{
		Assert.AreEqual(-1, ImGuiWidgets.ResolveFlameGraphSelection(-7, 5));
	}

	[TestMethod]
	public void ResolveFlameGraphSelection_PastEnd_ClampsToMinusOne()
	{
		Assert.AreEqual(-1, ImGuiWidgets.ResolveFlameGraphSelection(5, 5));
	}

	[TestMethod]
	public void ResolveFlameGraphSelection_EmptySampleList_ClampsToMinusOne()
	{
		Assert.AreEqual(-1, ImGuiWidgets.ResolveFlameGraphSelection(0, 0));
	}

	[TestMethod]
	public void FlameGraph_NullSamples_ThrowsBeforeTouchingImGui()
	{
		Assert.ThrowsExactly<ArgumentNullException>(() =>
		{
			int selected = -1;
			ImGuiWidgets.FlameGraph("label", null!, ref selected);
		});
	}
}