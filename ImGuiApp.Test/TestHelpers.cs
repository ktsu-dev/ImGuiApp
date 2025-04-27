// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGuiApp.Test;

using System.Numerics;
using Moq;
using Silk.NET.Maths;
using Silk.NET.Windowing;

internal static class TestHelpers
{
	internal static Mock<IWindow> CreateMockWindow(
		Vector2D<int>? size = null,
		Vector2D<int>? position = null,
		bool isVisible = true)
	{
		var mock = new Mock<IWindow>();

		mock.Setup(w => w.Size).Returns(size ?? new Vector2D<int>(1280, 720));
		mock.Setup(w => w.Position).Returns(position ?? new Vector2D<int>(0, 0));
		mock.Setup(w => w.IsVisible).Returns(isVisible);

		return mock;
	}

	internal static ImGuiAppConfig CreateTestConfig(
		string title = "Test Window",
		string iconPath = "",
		Vector2? size = null,
		Vector2? position = null)
	{
		return new ImGuiAppConfig
		{
			Title = title,
			IconPath = iconPath,
			InitialWindowState = new ImGuiAppWindowState
			{
				Size = size ?? new Vector2(800, 600),
				Pos = position ?? new Vector2(100, 100)
			}
		};
	}

	internal static void VerifyWindowProperties(
		Mock<IWindow> mockWindow,
		Vector2D<int> expectedSize,
		Vector2D<int> expectedPosition)
	{
		mockWindow.VerifySet(w => w.Size = expectedSize, Times.Once);
		mockWindow.VerifySet(w => w.Position = expectedPosition, Times.Once);
	}
}
