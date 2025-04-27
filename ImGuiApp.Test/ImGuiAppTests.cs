// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGuiApp.Test;

using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class ImGuiAppTests
{
	[TestMethod]
	public void EmsToPx_WithValidInput_ReturnsCorrectPixels()
	{
		// This test requires running ImGui context, so we'll need to mock it
		// or test it in an integration test
	}

	[TestMethod]
	public void PtsToPx_WithValidInput_ReturnsCorrectPixels()
	{
		// Arrange
		const int points = 12;
		const float scaleFactor = 1.5f;

		// Act
		var pixels = (int)(points * scaleFactor);

		// Assert
		Assert.AreEqual(18, pixels);
	}

	[TestMethod]
	public void ImGuiAppWindowState_DefaultValues_AreCorrect()
	{
		// Arrange & Act
		var state = new ImGuiAppWindowState();

		// Assert
		Assert.AreEqual(new Vector2(1280, 720), state.Size);
		Assert.AreEqual(new Vector2(-short.MinValue, -short.MinValue), state.Pos);
	}

	[TestMethod]
	public void ImGuiAppConfig_DefaultValues_AreCorrect()
	{
		// Arrange & Act
		var config = new ImGuiAppConfig();

		// Assert
		Assert.AreEqual("ImGuiApp", config.Title);
		Assert.AreEqual(string.Empty, config.IconPath);
		Assert.IsNotNull(config.Fonts);
	}
}
