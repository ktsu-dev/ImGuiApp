// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using System;
using System.Numerics;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests the orbit camera behind Viewport3D. All pure — no ImGui context required.
/// </summary>
/// <remarks>
/// Every expected number here is derived from the camera's own definition rather than read off a
/// run, because a camera that is wrong in a self-consistent way still produces a picture and the
/// only thing that catches it is arithmetic done independently.
/// </remarks>
[TestClass]
public class Viewport3DStateTests
{
	private const float Tolerance = 1e-5f;

	[TestMethod]
	public void DefaultCamera_SitsOnThePositiveZSideLookingTowardsTheOrigin()
	{
		ImGuiWidgets.Viewport3DState camera = new();

		Assert.AreEqual(0f, camera.Eye.X, Tolerance);
		Assert.AreEqual(0f, camera.Eye.Y, Tolerance);
		Assert.AreEqual(5f, camera.Eye.Z, Tolerance);
		Assert.AreEqual(Vector3.Zero, camera.Target);
	}

	[TestMethod]
	public void AQuarterTurnOfYaw_SwingsTheEyeOntoThePositiveXAxis()
	{
		// sin(pi/2) = 1 and cos(pi/2) = 0, so the eye lands at (distance, 0, 0).
		ImGuiWidgets.Viewport3DState camera = new();

		camera.Orbit((float)(Math.PI / 2.0), 0f);

		Assert.AreEqual(5f, camera.Eye.X, Tolerance);
		Assert.AreEqual(0f, camera.Eye.Y, Tolerance);
		Assert.AreEqual(0f, camera.Eye.Z, Tolerance);
	}

	[TestMethod]
	public void OrbitingKeepsTheEyeExactlyItsDistanceFromTheTarget()
	{
		ImGuiWidgets.Viewport3DState camera = new() { Distance = 17.5f };

		camera.Orbit(2.3f, 0.7f);

		Assert.AreEqual(17.5f, Vector3.Distance(camera.Eye, camera.Target), 1e-4f);
	}

	[TestMethod]
	public void YawIsNotWrapped_AndTenTurnsLandBackWhereItStarted()
	{
		ImGuiWidgets.Viewport3DState camera = new();
		Vector3 before = camera.Eye;

		for (int i = 0; i < 10; i++)
		{
			camera.Orbit((float)(Math.PI * 2.0), 0f);
		}

		Assert.AreEqual((float)(Math.PI * 20.0), camera.Yaw, 1e-3f);
		Assert.AreEqual(before.X, camera.Eye.X, 1e-3f);
		Assert.AreEqual(before.Z, camera.Eye.Z, 1e-3f);
	}

	[TestMethod]
	public void PitchClampsShortOfThePoles_WhicheverWayItIsDriven()
	{
		ImGuiWidgets.Viewport3DState camera = new();

		camera.Orbit(0f, 100f);
		Assert.AreEqual(ImGuiWidgets.Viewport3DState.PitchLimitRadians, camera.Pitch, Tolerance);

		camera.Orbit(0f, -200f);
		Assert.AreEqual(-ImGuiWidgets.Viewport3DState.PitchLimitRadians, camera.Pitch, Tolerance);
	}

	[TestMethod]
	public void AtThePitchLimit_TheViewIsStillTheRightWayRound()
	{
		// What the margin in PitchLimitRadians prevents, measured rather than assumed, and it is
		// not the NaN it looks like: the float nearest pi/2 is slightly ABOVE pi/2, so widening the
		// limit to exactly (float)(Math.PI / 2.0) puts the eye a fraction past the pole, the cosine
		// lands at -4.4e-8, and the whole view mirrors — a point on the camera's right renders on
		// its left with every element of the matrix still perfectly finite. A finiteness assertion
		// passes straight through that mutation; this one does not.
		ImGuiWidgets.Viewport3DState camera = new();
		camera.Orbit(0f, 100f);

		Vector4 clip = Vector4.Transform(new Vector4(1f, 0f, 0f, 1f), camera.ViewProjection(1f));

		Assert.AreEqual(0.482843f, clip.X / clip.W, 1e-3f);
	}

	[TestMethod]
	public void APointOffToTheRight_LandsWhereTheFieldOfViewSaysItShould()
	{
		// Default camera: eye (0, 0, 5), target the origin, 45 degree vertical field of view.
		// A world point at (1, 0, 0) is 1 unit lateral at a view depth of 5, so
		//   ndc.x = (1 / 5) / tan(fov / 2) = 0.2 / tan(pi / 8) = 0.482843.
		// Pinning the number rather than only its sign is what makes this catch a projection built
		// with the wrong field of view, or the two matrices multiplied the other way round.
		ImGuiWidgets.Viewport3DState camera = new();

		Vector4 clip = Vector4.Transform(new Vector4(1f, 0f, 0f, 1f), camera.ViewProjection(1f));

		Assert.AreEqual(5f, clip.W, 1e-4f);
		Assert.AreEqual(0.482843f, clip.X / clip.W, 1e-4f);
		Assert.AreEqual(0f, clip.Y / clip.W, 1e-4f);
	}

	[TestMethod]
	public void AWiderViewportSpreadsTheSameScene_OverProportionallyLessOfIt()
	{
		ImGuiWidgets.Viewport3DState camera = new();
		Vector4 point = new(1f, 0f, 0f, 1f);

		Vector4 square = Vector4.Transform(point, camera.ViewProjection(1f));
		Vector4 wide = Vector4.Transform(point, camera.ViewProjection(2f));

		Assert.AreEqual(square.X / square.W / 2f, wide.X / wide.W, 1e-4f);
	}

	[TestMethod]
	public void PanningRight_MovesAlongTheCameraAxisRatherThanTheWorldOne()
	{
		// Yawed a quarter turn the eye is on +X looking back towards -X, so "right" from there is
		// -Z. A pan implemented against the world axes would move along +X and this would fail.
		ImGuiWidgets.Viewport3DState camera = new();
		camera.Orbit((float)(Math.PI / 2.0), 0f);

		camera.Pan(1f, 0f);

		Assert.AreEqual(0f, camera.Target.X, 1e-4f);
		Assert.AreEqual(0f, camera.Target.Y, 1e-4f);
		Assert.AreEqual(-5f, camera.Target.Z, 1e-4f);
	}

	[TestMethod]
	public void PanningScalesWithDistance_SoADragCoversTheSameFractionOfTheViewAtEveryZoom()
	{
		ImGuiWidgets.Viewport3DState near = new() { Distance = 1f };
		ImGuiWidgets.Viewport3DState far = new() { Distance = 10f };

		near.Pan(0.1f, 0f);
		far.Pan(0.1f, 0f);

		Assert.AreEqual(0.1f, near.Target.X, 1e-4f);
		Assert.AreEqual(1f, far.Target.X, 1e-4f);
	}

	[TestMethod]
	public void PanningCarriesTheEyeWithTheTarget_LeavingTheDistanceAlone()
	{
		ImGuiWidgets.Viewport3DState camera = new();
		Vector3 eyeBefore = camera.Eye;

		camera.Pan(0.3f, -0.2f);

		Assert.AreEqual(camera.Target - Vector3.Zero, camera.Eye - eyeBefore);
		Assert.AreEqual(5f, Vector3.Distance(camera.Eye, camera.Target), 1e-4f);
	}

	[TestMethod]
	public void DollyingIsMultiplicative_SoOppositeAmountsCancel()
	{
		ImGuiWidgets.Viewport3DState camera = new() { Distance = 7f };

		camera.Dolly(1.3f);
		float closer = camera.Distance;
		camera.Dolly(-1.3f);

		Assert.IsLessThan(7f, closer, $"A positive dolly should move closer, but distance became {closer}.");
		Assert.AreEqual(7f, camera.Distance, 1e-4f);
	}

	[TestMethod]
	public void DollyingAllTheWayIn_StopsShortOfTheTargetRatherThanThroughIt()
	{
		ImGuiWidgets.Viewport3DState camera = new();

		camera.Dolly(100f);

		Assert.AreEqual(ImGuiWidgets.Viewport3DState.MinimumDistance, camera.Distance, 0f);
		Assert.IsTrue(float.IsFinite(camera.ViewProjection(1f).M11));
	}

	[TestMethod]
	public void ANegativeDistance_IsClampedRatherThanTurningTheCameraInsideOut()
	{
		ImGuiWidgets.Viewport3DState camera = new() { Distance = -4f };

		Assert.AreEqual(ImGuiWidgets.Viewport3DState.MinimumDistance, camera.Distance, 0f);
	}

	[TestMethod]
	public void ADegenerateFieldOfView_IsRefusedAtTheSetterRatherThanInTheMatrix()
	{
		ImGuiWidgets.Viewport3DState camera = new();

		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => camera.FieldOfView = 0f);
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => camera.FieldOfView = (float)Math.PI);
		Assert.AreEqual((float)(Math.PI / 4.0), camera.FieldOfView, Tolerance);
	}

	[TestMethod]
	public void NonPositiveClipPlanes_AreRefused()
	{
		ImGuiWidgets.Viewport3DState camera = new();

		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => camera.NearPlane = 0f);
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => camera.FarPlane = -1f);
	}

	[TestMethod]
	public void ANonPositiveAspectRatio_IsRefusedRatherThanYieldingAnInfiniteMatrix()
	{
		ImGuiWidgets.Viewport3DState camera = new();

		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => camera.ViewProjection(0f));
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => camera.ViewProjection(-1.5f));
	}
}
