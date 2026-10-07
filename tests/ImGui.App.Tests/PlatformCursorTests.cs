// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Tests;

using System;

using Hexa.NET.ImGui;

using ktsu.ImGui.App.ImGuiController;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using Silk.NET.Input;

/// <summary>
/// Covers how the cursor a frame decided on reaches the window's pointer, against a platform
/// cursor that records what it was told rather than changing anything on screen.
/// </summary>
[TestClass]
public sealed class PlatformCursorTests
{
	private static Mock<ICursor> CreateCursor(Func<StandardCursor, bool>? isSupported = null)
	{
		Mock<ICursor> cursor = new();
		cursor.SetupAllProperties();
		cursor.Object.CursorMode = CursorMode.Normal;
		cursor.Setup(c => c.IsSupported(It.IsAny<StandardCursor>())).Returns(isSupported ?? (_ => true));
		cursor.Invocations.Clear();
		return cursor;
	}

	/// <summary>Every cursor ImGui can ask for, with the standard cursor that should show.</summary>
	[TestMethod]
	[DataRow(ImGuiMouseCursor.Arrow, StandardCursor.Arrow)]
	[DataRow(ImGuiMouseCursor.TextInput, StandardCursor.IBeam)]
	[DataRow(ImGuiMouseCursor.ResizeAll, StandardCursor.ResizeAll)]
	[DataRow(ImGuiMouseCursor.ResizeNs, StandardCursor.VResize)]
	[DataRow(ImGuiMouseCursor.ResizeEw, StandardCursor.HResize)]
	[DataRow(ImGuiMouseCursor.ResizeNesw, StandardCursor.NeswResize)]
	[DataRow(ImGuiMouseCursor.ResizeNwse, StandardCursor.NwseResize)]
	[DataRow(ImGuiMouseCursor.Hand, StandardCursor.Hand)]
	[DataRow(ImGuiMouseCursor.NotAllowed, StandardCursor.NotAllowed)]
	[DataRow(ImGuiMouseCursor.Wait, StandardCursor.Wait)]
	[DataRow(ImGuiMouseCursor.Progress, StandardCursor.WaitArrow)]
	public void EachImGuiCursor_ShowsTheMatchingStandardCursor(ImGuiMouseCursor requested, StandardCursor expected)
	{
		Mock<ICursor> cursor = CreateCursor();

		new PlatformCursor().Apply(cursor.Object, requested, imGuiDrawsCursor: false, changesDisabled: false);

		Assert.AreEqual(CursorType.Standard, cursor.Object.Type);
		Assert.AreEqual(expected, cursor.Object.StandardCursor);
		Assert.AreEqual(CursorMode.Normal, cursor.Object.CursorMode);
	}

	/// <summary>No ImGui cursor is left mapping to nothing by accident; only None hides the pointer.</summary>
	[TestMethod]
	public void EveryImGuiCursorButNone_HasAStandardCursor()
	{
		foreach (ImGuiMouseCursor value in Enum.GetValues<ImGuiMouseCursor>())
		{
			if (value is ImGuiMouseCursor.None or ImGuiMouseCursor.Count)
			{
				continue;
			}

			Assert.IsNotNull(PlatformCursor.ToStandardCursor(value), $"{value} has no standard cursor");
		}

		Assert.IsNull(PlatformCursor.ToStandardCursor(ImGuiMouseCursor.None));
	}

	/// <summary>ImGui asking for no cursor hides the system pointer, and asking again brings it back.</summary>
	[TestMethod]
	public void NoneHidesThePointer_AndALaterCursorShowsItAgain()
	{
		Mock<ICursor> cursor = CreateCursor();
		PlatformCursor platform = new();

		platform.Apply(cursor.Object, ImGuiMouseCursor.None, imGuiDrawsCursor: false, changesDisabled: false);
		Assert.AreEqual(CursorMode.Hidden, cursor.Object.CursorMode);

		platform.Apply(cursor.Object, ImGuiMouseCursor.Hand, imGuiDrawsCursor: false, changesDisabled: false);
		Assert.AreEqual(CursorMode.Normal, cursor.Object.CursorMode);
		Assert.AreEqual(StandardCursor.Hand, cursor.Object.StandardCursor);
	}

	/// <summary>When ImGui draws the pointer itself, the system one is hidden rather than doubled.</summary>
	[TestMethod]
	public void ImGuiDrawingTheCursor_HidesTheSystemPointer()
	{
		Mock<ICursor> cursor = CreateCursor();

		new PlatformCursor().Apply(cursor.Object, ImGuiMouseCursor.Hand, imGuiDrawsCursor: true, changesDisabled: false);

		Assert.AreEqual(CursorMode.Hidden, cursor.Object.CursorMode);
	}

	/// <summary>An application that opted out with NoMouseCursorChange keeps its pointer untouched.</summary>
	[TestMethod]
	public void NoMouseCursorChange_LeavesThePointerAlone()
	{
		Mock<ICursor> cursor = CreateCursor();

		new PlatformCursor().Apply(cursor.Object, ImGuiMouseCursor.Hand, imGuiDrawsCursor: false, changesDisabled: true);

		cursor.VerifySet(c => c.StandardCursor = It.IsAny<StandardCursor>(), Times.Never());
		cursor.VerifySet(c => c.CursorMode = It.IsAny<CursorMode>(), Times.Never());
	}

	/// <summary>A pointer the application captured (hidden and locked for a camera) is not taken back.</summary>
	[TestMethod]
	[DataRow(CursorMode.Disabled)]
	[DataRow(CursorMode.Raw)]
	public void ACapturedPointer_IsNotReleased(CursorMode captured)
	{
		Mock<ICursor> cursor = CreateCursor();
		cursor.Object.CursorMode = captured;
		cursor.Invocations.Clear();

		new PlatformCursor().Apply(cursor.Object, ImGuiMouseCursor.Hand, imGuiDrawsCursor: false, changesDisabled: false);

		Assert.AreEqual(captured, cursor.Object.CursorMode);
		cursor.VerifySet(c => c.StandardCursor = It.IsAny<StandardCursor>(), Times.Never());
	}

	/// <summary>The platform is only written to when the cursor changes, not on every frame.</summary>
	[TestMethod]
	public void AnUnchangedCursor_IsNotWrittenAgain()
	{
		Mock<ICursor> cursor = CreateCursor();
		PlatformCursor platform = new();

		platform.Apply(cursor.Object, ImGuiMouseCursor.ResizeEw, imGuiDrawsCursor: false, changesDisabled: false);
		platform.Apply(cursor.Object, ImGuiMouseCursor.ResizeEw, imGuiDrawsCursor: false, changesDisabled: false);
		platform.Apply(cursor.Object, ImGuiMouseCursor.ResizeEw, imGuiDrawsCursor: false, changesDisabled: false);

		cursor.VerifySet(c => c.StandardCursor = It.IsAny<StandardCursor>(), Times.Once());
	}

	/// <summary>Releasing a capture shows the requested cursor again, even if it never changed.</summary>
	[TestMethod]
	public void ReleasingACapture_ReappliesTheCursor()
	{
		Mock<ICursor> cursor = CreateCursor();
		PlatformCursor platform = new();

		platform.Apply(cursor.Object, ImGuiMouseCursor.Hand, imGuiDrawsCursor: false, changesDisabled: false);
		cursor.Object.CursorMode = CursorMode.Disabled;
		platform.Apply(cursor.Object, ImGuiMouseCursor.Hand, imGuiDrawsCursor: false, changesDisabled: false);
		cursor.Object.CursorMode = CursorMode.Normal;
		cursor.Object.StandardCursor = StandardCursor.Default;
		platform.Apply(cursor.Object, ImGuiMouseCursor.Hand, imGuiDrawsCursor: false, changesDisabled: false);

		Assert.AreEqual(StandardCursor.Hand, cursor.Object.StandardCursor);
	}

	/// <summary>A cursor the platform cannot show falls back to its default rather than failing.</summary>
	[TestMethod]
	public void AnUnsupportedCursor_FallsBackToTheDefault()
	{
		Mock<ICursor> cursor = CreateCursor(c => c != StandardCursor.NotAllowed);

		new PlatformCursor().Apply(cursor.Object, ImGuiMouseCursor.NotAllowed, imGuiDrawsCursor: false, changesDisabled: false);

		Assert.AreEqual(StandardCursor.Default, cursor.Object.StandardCursor);
	}
}
