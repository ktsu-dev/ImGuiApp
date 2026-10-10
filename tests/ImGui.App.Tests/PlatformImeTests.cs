// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Tests;

using System.Collections.Generic;
using System.Numerics;

using ktsu.ImGui.App.ImGuiController;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Covers how a frame's input method request reaches the window, against a target that records
/// what it was told rather than moving anything on screen.
/// </summary>
[TestClass]
public sealed class PlatformImeTests
{
	private const nint Window = 42;

	private sealed class RecordingTarget : IImeTarget
	{
		public List<(nint Window, ImePlacement Placement)> Placed { get; } = [];

		public void Place(nint windowHandle, ImePlacement placement) => Placed.Add((windowHandle, placement));
	}

	[TestMethod]
	public void ARequest_IsPlacedOnTheWindow()
	{
		RecordingTarget target = new();
		PlatformIme ime = new(target);

		ime.Apply(Window, new ImePlacement(new Vector2(30, 40), 16), Vector2.Zero, Vector2.One);

		Assert.HasCount(1, target.Placed);
		Assert.AreEqual((Window, new ImePlacement(new Vector2(30, 40), 16)), target.Placed[0]);
	}

	[TestMethod]
	public void AnUnchangedRequest_IsNotPlacedAgain()
	{
		RecordingTarget target = new();
		PlatformIme ime = new(target);
		ImePlacement caret = new(new Vector2(30, 40), 16);

		ime.Apply(Window, caret, Vector2.Zero, Vector2.One);
		ime.Apply(Window, caret, Vector2.Zero, Vector2.One);

		Assert.HasCount(1, target.Placed);
	}

	[TestMethod]
	public void AMovedCaret_IsPlacedAgain()
	{
		RecordingTarget target = new();
		PlatformIme ime = new(target);

		ime.Apply(Window, new ImePlacement(new Vector2(30, 40), 16), Vector2.Zero, Vector2.One);
		ime.Apply(Window, new ImePlacement(new Vector2(38, 40), 16), Vector2.Zero, Vector2.One);

		Assert.HasCount(2, target.Placed);
		Assert.AreEqual(new Vector2(38, 40), target.Placed[1].Placement.Position);
	}

	[TestMethod]
	public void NoRequest_PlacesNothing_AndTheNextRequestIsPlacedEvenIfUnchanged()
	{
		// Leaving a field and coming back to the same caret has to place it again: another
		// window's input method may have moved it in between.
		RecordingTarget target = new();
		PlatformIme ime = new(target);
		ImePlacement caret = new(new Vector2(30, 40), 16);

		ime.Apply(Window, caret, Vector2.Zero, Vector2.One);
		ime.Apply(Window, null, Vector2.Zero, Vector2.One);
		Assert.IsNull(ime.Applied);
		ime.Apply(Window, caret, Vector2.Zero, Vector2.One);

		Assert.HasCount(2, target.Placed);
	}

	[TestMethod]
	public void ARequest_IsConvertedFromFramebufferPixelsToTheClientArea()
	{
		// ImGui works in framebuffer pixels, offset by the viewport; the platform wants client
		// coordinates in window units.
		ImePlacement client = PlatformIme.ToClient(new ImePlacement(new Vector2(210, 120), 32), new Vector2(10, 20), new Vector2(2, 2));

		Assert.AreEqual(new ImePlacement(new Vector2(100, 50), 16), client);
	}

	[TestMethod]
	public void ADegenerateScale_IsTreatedAsOne()
	{
		ImePlacement client = PlatformIme.ToClient(new ImePlacement(new Vector2(30, 40), 16), Vector2.Zero, Vector2.Zero);

		Assert.AreEqual(new ImePlacement(new Vector2(30, 40), 16), client);
	}

	[TestMethod]
	public void NoWindowHandle_RecordsTheRequestWithoutPlacingIt()
	{
		RecordingTarget target = new();
		PlatformIme ime = new(target);

		ime.Apply(0, new ImePlacement(new Vector2(30, 40), 16), Vector2.Zero, Vector2.One);

		Assert.IsEmpty(target.Placed);
		Assert.IsNotNull(ime.Applied);
	}

	[TestMethod]
	public void NoTarget_RecordsTheRequest()
	{
		// Linux and macOS: GLFW has no input method API, so the request stops here.
		PlatformIme ime = new(null);

		ime.Apply(Window, new ImePlacement(new Vector2(30, 40), 16), Vector2.Zero, Vector2.One);

		Assert.AreEqual(new ImePlacement(new Vector2(30, 40), 16), ime.Applied);
	}
}
