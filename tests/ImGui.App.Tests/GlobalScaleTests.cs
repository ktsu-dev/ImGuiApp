// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.App.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests for global scale accessibility feature
/// </summary>
[TestClass]
public class GlobalScaleTests
{
	[TestInitialize]
	public void TestInitialize()
	{
		// Reset before each test
		ImGuiApp.Reset();
	}

	[TestCleanup]
	public void TestCleanup()
	{
		// Clean up after each test
		ImGuiApp.Reset();
	}

	[TestMethod]
	public void GlobalScale_DefaultValue_IsOne()
	{
		// Assert
		Assert.AreEqual(1.0f, ImGuiApp.GlobalScale, 0.001f);
	}

	[TestMethod]
	public void SetGlobalScale_ValidValue_UpdatesProperty()
	{
		// Act
		ImGuiApp.SetGlobalScale(1.5f);

		// Assert
		Assert.AreEqual(1.5f, ImGuiApp.GlobalScale, 0.001f);
	}

	[TestMethod]
	public void SetGlobalScale_MinimumValue_Succeeds()
	{
		// Act
		ImGuiApp.SetGlobalScale(0.5f);

		// Assert
		Assert.AreEqual(0.5f, ImGuiApp.GlobalScale, 0.001f);
	}

	[TestMethod]
	public void SetGlobalScale_MaximumValue_Succeeds()
	{
		// Act
		ImGuiApp.SetGlobalScale(3.0f);

		// Assert
		Assert.AreEqual(3.0f, ImGuiApp.GlobalScale, 0.001f);
	}

	[TestMethod]
	public void SetGlobalScale_BelowMinimum_ThrowsArgumentOutOfRangeException()
	{
		// Act & Assert
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => ImGuiApp.SetGlobalScale(0.4f));
	}

	[TestMethod]
	public void SetGlobalScale_AboveMaximum_ThrowsArgumentOutOfRangeException()
	{
		// Act & Assert
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => ImGuiApp.SetGlobalScale(3.1f));
	}

	[TestMethod]
	public void SetGlobalScale_CallsCallback_WhenConfigured()
	{
		// Arrange
		float callbackScale = 0f;
		bool callbackInvoked = false;
		ImGuiApp.Config = new ImGuiAppConfig
		{
			OnGlobalScaleChanged = (scale) =>
			{
				callbackInvoked = true;
				callbackScale = scale;
			}
		};

		// Act
		ImGuiApp.SetGlobalScale(1.25f);

		// Assert
		Assert.IsTrue(callbackInvoked, "Expected OnGlobalScaleChanged callback to be invoked when SetGlobalScale is called");
		Assert.AreEqual(1.25f, callbackScale, 0.001f);
	}

	[TestMethod]
	public void SetGlobalScale_NoCallback_DoesNotThrow()
	{
		// Arrange
		ImGuiApp.Config = new ImGuiAppConfig
		{
			OnGlobalScaleChanged = null!
		};

		// Act & Assert - Should not throw
		ImGuiApp.SetGlobalScale(1.5f);
		Assert.AreEqual(1.5f, ImGuiApp.GlobalScale, 0.001f);
	}

	[TestMethod]
	public void SetGlobalScale_MultipleValues_CallbackInvokedEachTime()
	{
		// Arrange
		int callbackCount = 0;
		float lastScale = 0f;
		ImGuiApp.Config = new ImGuiAppConfig
		{
			OnGlobalScaleChanged = (scale) =>
			{
				callbackCount++;
				lastScale = scale;
			}
		};

		// Act
		ImGuiApp.SetGlobalScale(0.75f);
		ImGuiApp.SetGlobalScale(1.0f);
		ImGuiApp.SetGlobalScale(1.5f);
		ImGuiApp.SetGlobalScale(2.0f);

		// Assert
		Assert.AreEqual(4, callbackCount);
		Assert.AreEqual(2.0f, lastScale, 0.001f);
		Assert.AreEqual(2.0f, ImGuiApp.GlobalScale, 0.001f);
	}

	[TestMethod]
	public void Reset_ResetsGlobalScale_ToDefaultValue()
	{
		// Arrange
		ImGuiApp.SetGlobalScale(2.0f);
		Assert.AreEqual(2.0f, ImGuiApp.GlobalScale, 0.001f);

		// Act
		ImGuiApp.Reset();

		// Assert
		Assert.AreEqual(1.0f, ImGuiApp.GlobalScale, 0.001f);
	}

	[TestMethod]
	public void SetGlobalScale_CommonAccessibilityValues_AllSucceed()
	{
		// Test common accessibility scale values
		float[] commonScales = [0.75f, 1.0f, 1.25f, 1.5f, 1.75f, 2.0f];

		foreach (float scale in commonScales)
		{
			// Act
			ImGuiApp.SetGlobalScale(scale);

			// Assert
			Assert.AreEqual(scale, ImGuiApp.GlobalScale, 0.001f, $"Failed for scale {scale}");
		}
	}
}
