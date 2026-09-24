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
	/// Dear ImGui has no InputLong, so a 64-bit integer needs an unsafe DragScalar. It is left out of
	/// v1 rather than carried in on one unverified interop call.
	/// </summary>
	[TestMethod]
	public void Classify_Int64_IsUnsupportedForNow()
	{
		Assert.AreEqual(PinValueKind.Unsupported, PinValueKinds.Classify(typeof(long)));
	}

	[TestMethod]
	public void Classify_ATypeWithNoEditor_IsUnsupported()
	{
		Assert.AreEqual(PinValueKind.Unsupported, PinValueKinds.Classify(typeof(NodeEditorEngine)));
	}
}
