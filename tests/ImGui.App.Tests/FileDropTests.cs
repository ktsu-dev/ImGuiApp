// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Tests;

using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;

/// <summary>
/// Tests for files dropped onto the window from the file manager reaching
/// <see cref="ImGuiAppConfig.OnFilesDropped"/>.
/// </summary>
[TestClass]
public sealed class FileDropTests
{
	private Mock<IWindow> mockWindow = null!;
	private readonly List<(string[] Paths, Vector2 Position)> drops = [];

	[TestInitialize]
	public void Setup()
	{
		ImGuiApp.Reset();
		mockWindow = TestHelpers.CreateMockWindow();
		ImGuiApp.window = mockWindow.Object;
		ImGuiApp.Config = new ImGuiAppConfig { OnFilesDropped = (paths, position) => drops.Add((paths, position)) };
	}

	[TestCleanup]
	public void Cleanup() => ImGuiApp.Reset();

	private static void UseMouseAt(Vector2 position)
	{
		Mock<IMouse> mouse = new();
		mouse.Setup(m => m.Position).Returns(position);
		Mock<IInputContext> input = new();
		input.Setup(i => i.Mice).Returns([mouse.Object]);
		ImGuiApp.inputContext = input.Object;
	}

	[TestMethod]
	public void ADropOnTheWindowReachesTheApplicationWithThePointerPosition()
	{
		ImGuiApp.SetupWindowFileDropHandler();
		UseMouseAt(new Vector2(120, 45));

		string[] paths = ["/photos/a.png", "/photos/b.jpg"];
		mockWindow.Raise(w => w.FileDrop += null, [paths]);

		Assert.HasCount(1, drops);
		CollectionAssert.AreEqual(paths, drops[0].Paths);
		Assert.AreEqual(new Vector2(120, 45), drops[0].Position);
	}

	[TestMethod]
	public void TheDropPositionIsInFramebufferPixelsOnAScaledSurface()
	{
		mockWindow.Setup(w => w.Size).Returns(new Vector2D<int>(800, 600));
		mockWindow.Setup(w => w.FramebufferSize).Returns(new Vector2D<int>(1600, 1200));
		UseMouseAt(new Vector2(100, 50));

		Assert.AreEqual(new Vector2(200, 100), ImGuiApp.DropPosition(),
			"ImGui works in framebuffer pixels, so a window-unit cursor is scaled like a mouse move.");
	}

	[TestMethod]
	public void WithNoMouseTheDropLandsAtTheOrigin()
	{
		ImGuiApp.inputContext = null;

		Assert.AreEqual(Vector2.Zero, ImGuiApp.DropPosition());
	}

	[TestMethod]
	public void AnEmptyDropIsNotDelivered()
	{
		ImGuiApp.DeliverDroppedFiles([], Vector2.One);
		ImGuiApp.DeliverDroppedFiles(null, Vector2.One);

		Assert.IsEmpty(drops);
	}

	[TestMethod]
	public void ADropWithNoHandlerIsIgnored()
	{
		ImGuiApp.Config = new ImGuiAppConfig();

		ImGuiApp.DeliverDroppedFiles(["/a.png"], Vector2.One);

		Assert.IsEmpty(drops);
	}
}
