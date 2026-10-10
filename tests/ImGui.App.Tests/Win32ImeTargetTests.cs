// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Tests;

using System.Collections.Generic;
using System.Numerics;

using ktsu.ImGui.App;
using ktsu.ImGui.App.ImGuiController;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Covers what the Win32 input method target sends, against a stand-in for <c>imm32.dll</c> that
/// records the calls.
/// </summary>
[TestClass]
public sealed class Win32ImeTargetTests
{
	private const nint Window = 42;
	private const nint Context = 7;
	private static readonly string[] PlacedCalls = ["get 42", "composition 7", "candidate 7", "release 42 7"];
	private static readonly string[] NoContextCalls = ["get 42"];

	private sealed class RecordingImm(nint context) : IImmApi
	{
		public List<string> Calls { get; } = [];

		public NativeMethods.COMPOSITIONFORM? Composition { get; private set; }

		public NativeMethods.CANDIDATEFORM? Candidate { get; private set; }

		public nint GetContext(nint windowHandle)
		{
			Calls.Add($"get {windowHandle}");
			return context;
		}

		public void ReleaseContext(nint windowHandle, nint context)
		{
			Calls.Add($"release {windowHandle} {context}");
		}

		public void SetCompositionWindow(nint context, NativeMethods.COMPOSITIONFORM form)
		{
			Calls.Add($"composition {context}");
			Composition = form;
		}

		public void SetCandidateWindow(nint context, NativeMethods.CANDIDATEFORM form)
		{
			Calls.Add($"candidate {context}");
			Candidate = form;
		}
	}

	[TestMethod]
	public void Place_SetsBothWindowsAndReleasesTheContext()
	{
		RecordingImm imm = new(Context);

		new Win32ImeTarget(imm).Place(Window, new ImePlacement(new Vector2(30.4f, 40.6f), 17.2f));

		CollectionAssert.AreEqual(PlacedCalls, imm.Calls);
		Assert.AreEqual(30, imm.Composition!.Value.ptCurrentPos.X);
		Assert.AreEqual(41, imm.Composition!.Value.ptCurrentPos.Y);
		Assert.AreEqual(NativeMethods.CFS_FORCE_POSITION, imm.Composition!.Value.dwStyle);
		Assert.AreEqual(NativeMethods.CFS_EXCLUDE, imm.Candidate!.Value.dwStyle);
	}

	[TestMethod]
	public void Place_WithoutAContext_DoesNothingElse()
	{
		RecordingImm imm = new(0);

		new Win32ImeTarget(imm).Place(Window, new ImePlacement(new Vector2(30, 40), 16));

		CollectionAssert.AreEqual(NoContextCalls, imm.Calls);
	}

	[TestMethod]
	public void TheCandidateList_IsKeptOffTheCaretsLine()
	{
		NativeMethods.CANDIDATEFORM form = Win32ImeTarget.CandidateAt(new ImePlacement(new Vector2(30, 40), 16.2f));

		Assert.AreEqual(30, form.ptCurrentPos.X);
		Assert.AreEqual(40, form.ptCurrentPos.Y);
		Assert.AreEqual(30, form.rcArea.Left);
		Assert.AreEqual(31, form.rcArea.Right);
		Assert.AreEqual(40, form.rcArea.Top);
		Assert.AreEqual(57, form.rcArea.Bottom, "The excluded line should cover the caret's whole height, rounded up.");
	}

	[TestMethod]
	public void AZeroHeightLine_StillExcludesOnePixel()
	{
		NativeMethods.CANDIDATEFORM form = Win32ImeTarget.CandidateAt(new ImePlacement(new Vector2(30, 40), 0));

		Assert.AreEqual(41, form.rcArea.Bottom);
	}

	[TestMethod]
	public void ThePlatformBridge_IsChosenForThisPlatform()
	{
		// Constructing it touches no native code on any platform; only placing does.
		Assert.IsNotNull(PlatformIme.ForCurrentPlatform());
		Assert.IsNotNull(new Win32ImeTarget());
	}
}
