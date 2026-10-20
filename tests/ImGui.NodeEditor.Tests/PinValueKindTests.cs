// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.NodeEditor.Tests;

using System.Numerics;

using ktsu.ImGui.NodeEditor;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// The one place that decides which pin types can be edited. Both the inline editors and the
/// inspector ask it, so that a type either has an editor on both surfaces or on neither.
/// </summary>
[TestClass]
public sealed class PinValueKindTests
{
	private enum Polarity
	{
		Rising,
		Falling,
	}

	[TestMethod]
	public void Classify_RecognizesEverySupportedType()
	{
		Assert.AreEqual(PinValueKind.Boolean, PinValueKinds.Classify(typeof(bool)));
		Assert.AreEqual(PinValueKind.Int32, PinValueKinds.Classify(typeof(int)));
		Assert.AreEqual(PinValueKind.Single, PinValueKinds.Classify(typeof(float)));
		Assert.AreEqual(PinValueKind.Double, PinValueKinds.Classify(typeof(double)));
		Assert.AreEqual(PinValueKind.String, PinValueKinds.Classify(typeof(string)));
		Assert.AreEqual(PinValueKind.Vector2, PinValueKinds.Classify(typeof(Vector2)));
		Assert.AreEqual(PinValueKind.Vector3, PinValueKinds.Classify(typeof(Vector3)));
		Assert.AreEqual(PinValueKind.Enum, PinValueKinds.Classify(typeof(Polarity)));
	}

	[TestMethod]
	public void Classify_LooksThroughNullable()
	{
		Assert.AreEqual(PinValueKind.Double, PinValueKinds.Classify(typeof(double?)));
		Assert.AreEqual(PinValueKind.Enum, PinValueKinds.Classify(typeof(Polarity?)));
	}

	[TestMethod]
	public void Classify_AnUntypedPin_IsUnsupported()
	{
		Assert.AreEqual(PinValueKind.Unsupported, PinValueKinds.Classify(null));
	}

	/// <summary>
	/// Dear ImGui has no InputLong, so a 64-bit integer is edited through DragScalar on a node and
	/// InputScalar in the inspector, both with <c>ImGuiDataType.S64</c> (#566).
	/// </summary>
	[TestMethod]
	public void Classify_Int64_IsInt64()
	{
		Assert.AreEqual(PinValueKind.Int64, PinValueKinds.Classify(typeof(long)));
		Assert.AreEqual(PinValueKind.Int64, PinValueKinds.Classify(typeof(long?)));
	}

	/// <summary>
	/// Int64 was added after the other kinds, so it comes last and every kind before it keeps the
	/// value it shipped with.
	/// </summary>
	[TestMethod]
	public void TheShippedKinds_KeepTheirValues()
	{
		Assert.AreEqual(8, (int)PinValueKind.Enum);
		Assert.AreEqual(9, (int)PinValueKind.Int64);
	}

	[TestMethod]
	public void Classify_Decimal_IsUnsupported()
	{
		Assert.AreEqual(PinValueKind.Unsupported, PinValueKinds.Classify(typeof(decimal)));
	}

	[TestMethod]
	public void Classify_ATypeWithNoEditor_IsUnsupported()
	{
		Assert.AreEqual(PinValueKind.Unsupported, PinValueKinds.Classify(typeof(NodeEditorEngine)));
	}
}
