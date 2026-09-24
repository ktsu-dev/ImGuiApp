// Copyright (c) 2023-2026 ktsu-dev contributors

[assembly: DoNotParallelize]

namespace ktsu.ImGui.App.Tests;

using System.Diagnostics;
using System.Numerics;
using ktsu.ImGui.App.Tests.Images;
using ktsu.Semantics.Paths;
using ktsu.Semantics.Strings;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Silk.NET.Core.Contexts;
using Silk.NET.Maths;
using Silk.NET.Windowing;

[TestClass]
public sealed class ImGuiAppTests : IDisposable
{
	public TestContext TestContext { get; set; } = null!;

	private Mock<IWindow>? _mockWindow;
	private Mock<IMonitor>? _mockMonitor;
	private TestGL? _testGL;
	private MockGL? _mockGL;
	private Mock<IGLContext>? _mockContext;
	private TestOpenGLProvider? _glProvider;

	[TestInitialize]
	public void Setup()
	{
		ResetState();
		_mockWindow = new Mock<IWindow>();
		_mockMonitor = new Mock<IMonitor>();
		_testGL = new TestGL();
		_mockGL = new MockGL(_testGL);
		_mockContext = new Mock<IGLContext>();
		_glProvider = new TestOpenGLProvider(_mockGL);

		// Setup default window properties
		_mockWindow.Setup(w => w.Size).Returns(new Vector2D<int>(1280, 720));
		_mockWindow.Setup(w => w.Position).Returns(new Vector2D<int>(0, 0));
		_mockWindow.Setup(w => w.Monitor).Returns(_mockMonitor.Object);
		_mockWindow.Setup(w => w.GLContext).Returns(_mockContext.Object);

		// Setup monitor bounds
		Rectangle<int> bounds = new(0, 0, 1920, 1080);
		_mockMonitor.Setup(m => m.Bounds).Returns(bounds);
	}

	[TestCleanup]
	public void Cleanup()
	{
		ResetState();
		_glProvider?.Dispose();
		_mockGL?.Dispose();
		_testGL?.Dispose();
	}

	public void Dispose()
	{
		Cleanup();
	}

	private static void ResetState()
	{
		ImGuiApp.Reset();

		// These tests assert on application-driven window placement, so pin the geometry mode
		// instead of letting it be decided by whichever session the test host happens to run in
		// (auto-detection hands geometry to the compositor under Wayland and tiling WMs).
		ImGuiApp.Config = new ImGuiAppConfig { WindowGeometry = WindowGeometryMode.Application };
	}

	[TestMethod]
	public void EmsToPx_WithValidInput_ReturnsCorrectPixels()
	{
		const float ems = 1.5f;
		int expected = (int)(ems * FontAppearance.DefaultFontPointSize);
		int actual = ImGuiApp.EmsToPx(ems);
		Assert.AreEqual(expected, actual);
	}

	[TestMethod]
	public void PtsToPx_WithValidInput_ReturnsCorrectPixels()
	{
		const float pts = 12.0f;
		float expected = pts;
		int actual = ImGuiApp.PtsToPx((int)pts);
		Assert.AreEqual(expected, actual);
	}

	[TestMethod]
	public void ImGuiAppWindowState_DefaultValues_AreCorrect()
	{
		ImGuiAppWindowState state = new();
		Assert.AreEqual(new Vector2(1280, 720), state.Size);
		Assert.AreEqual(new Vector2(-short.MinValue, -short.MinValue), state.Pos);
		Assert.AreEqual(WindowState.Normal, state.LayoutState);
	}

	[TestMethod]
	public void ImGuiAppConfig_DefaultValues_AreCorrect()
	{
		ImGuiAppConfig config = new();
		Assert.AreEqual("ImGuiApp", config.Title);
		Assert.AreEqual(string.Empty, config.IconPath);
		Assert.IsNotNull(config.InitialWindowState);
		Assert.IsNotNull(config.OnStart);
		Assert.IsNotNull(config.OnUpdate);
		Assert.IsNotNull(config.OnRender);
		Assert.IsNotNull(config.OnAppMenu);
		Assert.IsNotNull(config.OnMoveOrResize);
		Assert.IsNotNull(config.Fonts);
		Assert.IsNotNull(config.PerformanceSettings);
	}

	[TestMethod]
	public void Start_WithNullConfig_ThrowsArgumentNullException()
	{
		Assert.ThrowsExactly<ArgumentNullException>(() => ImGuiApp.Start(null!));
	}

	[TestMethod]
	public void Start_WithInvalidIconPath_ThrowsFileNotFoundException()
	{
		ImGuiAppConfig config = TestHelpers.CreateTestConfig(iconPath: "nonexistent.png");
		Assert.ThrowsExactly<FileNotFoundException>(() => ImGuiApp.Start(config));
	}

	[TestMethod]
	public void Stop_WhenNotRunning_ThrowsInvalidOperationException()
	{
		Assert.ThrowsExactly<InvalidOperationException>(ImGuiApp.Stop);
	}

	[TestMethod]
	public void EnsureWindowPositionIsValid_WithInvalidPosition_MovesToValidPosition()
	{
		// Reset state to ensure clean test environment
		ResetState();

		// Set up window field using direct access to internal member
		Mock<IWindow> mockWindow = new();
		Mock<IMonitor> mockMonitor = new();

		// Set up monitor bounds
		Rectangle<int> monitorBounds = new(0, 0, 1920, 1080);
		mockMonitor.Setup(m => m.Bounds).Returns(monitorBounds);

		// Set up window in an invalid position (off screen)
		Vector2D<int> windowSize = new(800, 600);
		Vector2D<int> offScreenPosition = new(-1000, -1000);
		mockWindow.Setup(w => w.Size).Returns(windowSize);
		mockWindow.Setup(w => w.Position).Returns(offScreenPosition);
		mockWindow.Setup(w => w.Monitor).Returns(mockMonitor.Object);
		mockWindow.Setup(w => w.WindowState).Returns(WindowState.Normal);

		// Allow position and size to be set
		Vector2D<int> finalPosition = offScreenPosition;
		Vector2D<int> finalSize = windowSize;
		mockWindow.SetupSet(w => w.Position = It.IsAny<Vector2D<int>>())
			.Callback<Vector2D<int>>(pos => finalPosition = pos);
		mockWindow.SetupSet(w => w.Size = It.IsAny<Vector2D<int>>())
			.Callback<Vector2D<int>>(size => finalSize = size);
		mockWindow.SetupSet(w => w.WindowState = It.IsAny<WindowState>());

		// Set the mock window directly using internal field
		ImGuiApp.window = mockWindow.Object;

		// Call EnsureWindowPositionIsValid directly using internal method
		ImGuiApp.EnsureWindowPositionIsValid();

		// Verify the window was moved to a valid position
		Assert.IsTrue(monitorBounds.Contains(finalPosition),
			"Window position should be within monitor bounds");

		// Verify the window size was preserved (improvement from new logic)
		Assert.AreEqual(windowSize, finalSize, "Window size should be preserved when it fits");
	}

	[TestMethod]
	public void EnsureWindowPositionIsValid_WithPerformanceOptimization_SkipsUnnecessaryChecks()
	{
		// Reset state to ensure clean test environment
		ResetState();

		Mock<IWindow> mockWindow = new();
		Mock<IMonitor> mockMonitor = new();

		Rectangle<int> monitorBounds = new(0, 0, 1920, 1080);
		mockMonitor.Setup(m => m.Bounds).Returns(monitorBounds);

		Vector2D<int> validPosition = new(100, 100);
		Vector2D<int> validSize = new(800, 600);
		mockWindow.Setup(w => w.Size).Returns(validSize);
		mockWindow.Setup(w => w.Position).Returns(validPosition);
		mockWindow.Setup(w => w.Monitor).Returns(mockMonitor.Object);
		mockWindow.Setup(w => w.WindowState).Returns(WindowState.Normal);

		ImGuiApp.window = mockWindow.Object;

		// First call should perform validation
		ImGuiApp.EnsureWindowPositionIsValid();
		int boundsCallsAfterFirst = mockMonitor.Invocations.Count;

		// Second call with same position/size should skip validation (performance optimization)
		ImGuiApp.EnsureWindowPositionIsValid();
		int boundsCallsAfterSecond = mockMonitor.Invocations.Count;

		// Should have made no additional calls on second invocation due to caching
		Assert.AreEqual(boundsCallsAfterFirst, boundsCallsAfterSecond,
			"Second call should skip validation due to performance optimization");
	}

	[TestMethod]
	public void EnsureWindowPositionIsValid_WithPartiallyVisibleWindow_RelocatesWhenInsufficientlyVisible()
	{
		// Reset state to ensure clean test environment
		ResetState();

		Mock<IWindow> mockWindow = new();
		Mock<IMonitor> mockMonitor = new();

		Rectangle<int> monitorBounds = new(0, 0, 1920, 1080);
		mockMonitor.Setup(m => m.Bounds).Returns(monitorBounds);

		// Window with only small corner visible (less than 25% visibility requirement)
		Vector2D<int> windowSize = new(800, 600);
		Vector2D<int> barelyVisiblePosition = new(1870, 1030); // Only 50x50 pixels visible
		mockWindow.Setup(w => w.Size).Returns(windowSize);
		mockWindow.Setup(w => w.Position).Returns(barelyVisiblePosition);
		mockWindow.Setup(w => w.Monitor).Returns(mockMonitor.Object);
		mockWindow.Setup(w => w.WindowState).Returns(WindowState.Normal);

		Vector2D<int> finalPosition = barelyVisiblePosition;
		mockWindow.SetupSet(w => w.Position = It.IsAny<Vector2D<int>>())
			.Callback<Vector2D<int>>(pos => finalPosition = pos);
		mockWindow.SetupSet(w => w.Size = It.IsAny<Vector2D<int>>());
		mockWindow.SetupSet(w => w.WindowState = It.IsAny<WindowState>());

		ImGuiApp.window = mockWindow.Object;
		ImGuiApp.EnsureWindowPositionIsValid();

		// Window should be relocated since it's insufficiently visible
		Assert.AreNotEqual(barelyVisiblePosition, finalPosition,
			"Window should be relocated when insufficiently visible");
		Assert.IsTrue(monitorBounds.Contains(finalPosition),
			"Relocated window should be fully within monitor bounds");
	}

	[TestMethod]
	public void EnsureWindowPositionIsValid_WithSufficientlyVisibleWindow_LeavesWindowAlone()
	{
		// Reset state to ensure clean test environment
		ResetState();

		Mock<IWindow> mockWindow = new();
		Mock<IMonitor> mockMonitor = new();

		Rectangle<int> monitorBounds = new(0, 0, 1920, 1080);
		mockMonitor.Setup(m => m.Bounds).Returns(monitorBounds);

		// Window with significant visibility (more than 25%)
		// Window 800x600 = 480,000 pixels total
		// Visible portion: 400x300 = 120,000 pixels (25% exactly)
		Vector2D<int> windowSize = new(800, 600);
		Vector2D<int> partiallyVisiblePosition = new(1520, 780); // 400x300 pixels visible
		mockWindow.Setup(w => w.Size).Returns(windowSize);
		mockWindow.Setup(w => w.Position).Returns(partiallyVisiblePosition);
		mockWindow.Setup(w => w.Monitor).Returns(mockMonitor.Object);
		mockWindow.Setup(w => w.WindowState).Returns(WindowState.Normal);

		mockWindow.SetupSet(w => w.Position = It.IsAny<Vector2D<int>>());

		ImGuiApp.window = mockWindow.Object;
		ImGuiApp.EnsureWindowPositionIsValid();

		// Window position should not be changed since it has sufficient visibility
		mockWindow.VerifySet(w => w.Position = It.IsAny<Vector2D<int>>(), Times.Never,
			"Window should not be moved when sufficiently visible");
	}

	[TestMethod]
	public void EnsureWindowPositionIsValid_WithOversizedWindow_FitsToMonitor()
	{
		// Reset state to ensure clean test environment
		ResetState();

		Mock<IWindow> mockWindow = new();
		Mock<IMonitor> mockMonitor = new();

		// Small monitor
		Rectangle<int> smallMonitorBounds = new(0, 0, 1024, 768);
		mockMonitor.Setup(m => m.Bounds).Returns(smallMonitorBounds);

		// Oversized window completely off-screen
		Vector2D<int> oversizedSize = new(2000, 1500);
		Vector2D<int> offScreenPosition = new(-2500, -2000); // Completely off-screen
		mockWindow.Setup(w => w.Size).Returns(oversizedSize);
		mockWindow.Setup(w => w.Position).Returns(offScreenPosition);
		mockWindow.Setup(w => w.Monitor).Returns(mockMonitor.Object);
		mockWindow.Setup(w => w.WindowState).Returns(WindowState.Normal);

		Vector2D<int> finalSize = oversizedSize;
		mockWindow.SetupSet(w => w.Position = It.IsAny<Vector2D<int>>());
		mockWindow.SetupSet(w => w.Size = It.IsAny<Vector2D<int>>())
			.Callback<Vector2D<int>>(size => finalSize = size);
		mockWindow.SetupSet(w => w.WindowState = It.IsAny<WindowState>());

		ImGuiApp.window = mockWindow.Object;
		ImGuiApp.EnsureWindowPositionIsValid();

		// Debug: Check if window was relocated at all
		mockWindow.VerifySet(w => w.Position = It.IsAny<Vector2D<int>>(), Times.AtLeastOnce,
			"Window should have been relocated when completely off-screen");

		// Window should be resized to fit monitor (with 100px margin)
		Assert.IsLessThanOrEqualTo(smallMonitorBounds.Size.X - 100,
finalSize.X, $"Window width {finalSize.X} should be <= {smallMonitorBounds.Size.X - 100} to fit monitor");
		Assert.IsLessThanOrEqualTo(smallMonitorBounds.Size.Y - 100,
finalSize.Y, $"Window height {finalSize.Y} should be <= {smallMonitorBounds.Size.Y - 100} to fit monitor");
		Assert.IsTrue(finalSize.X >= 640 && finalSize.Y >= 480,
			$"Window size {finalSize} should maintain minimum size of 640x480");
	}

	[TestMethod]
	public void ForceWindowPositionValidation_ForcesNextValidation()
	{
		// Reset state to ensure clean test environment
		ResetState();

		Mock<IWindow> mockWindow = new();
		Mock<IMonitor> mockMonitor = new();

		Rectangle<int> monitorBounds = new(0, 0, 1920, 1080);
		mockMonitor.Setup(m => m.Bounds).Returns(monitorBounds);

		Vector2D<int> validPosition = new(100, 100);
		Vector2D<int> validSize = new(800, 600);
		mockWindow.Setup(w => w.Size).Returns(validSize);
		mockWindow.Setup(w => w.Position).Returns(validPosition);
		mockWindow.Setup(w => w.Monitor).Returns(mockMonitor.Object);
		mockWindow.Setup(w => w.WindowState).Returns(WindowState.Normal);

		ImGuiApp.window = mockWindow.Object;

		// First validation
		ImGuiApp.EnsureWindowPositionIsValid();
		int callsAfterFirst = mockMonitor.Invocations.Count;

		// Force validation should make next call perform validation even with same position
		ImGuiApp.ForceWindowPositionValidation();
		ImGuiApp.EnsureWindowPositionIsValid();
		int callsAfterForced = mockMonitor.Invocations.Count;

		// Should have made additional calls after forcing validation
		Assert.IsGreaterThan(callsAfterFirst,
callsAfterForced, "Forced validation should cause additional monitor access");
	}

	[TestMethod]
	public void OpenGLProvider_GetGL_ReturnsSameInstance()
	{
		// Setup test GL provider
		using TestGL testGL = new();
		using MockGL mockGL = new(testGL);
		using TestOpenGLProvider provider = new(mockGL);

		// Get GL instances
		ImGuiController.IGL gl1 = provider.GetGL();
		ImGuiController.IGL gl2 = provider.GetGL();

		// Verify same instance is returned
		Assert.AreSame(gl1, gl2, "OpenGLProvider should return the same GL instance on subsequent calls");
	}

	[TestMethod]
	public void DeleteTexture_WithNullGL_ThrowsInvalidOperationException()
	{
		// Reset state to ensure clean test environment
		ResetState();

		// Set up a basic invoker that executes actions immediately using direct access
		ImGuiApp.Invoker = new Invoker.Invoker();

		// Now test the DeleteTexture method
		Assert.ThrowsExactly<InvalidOperationException>(() => ImGuiApp.DeleteTexture(1));
	}

	[TestMethod]
	public void GetOrLoadTexture_WithInvalidPath_ThrowsArgumentException()
	{
		AbsoluteFilePath invalidPath = new();
		Assert.ThrowsExactly<ArgumentException>(() => ImGuiApp.GetOrLoadTexture(invalidPath));
	}

	[TestMethod]
	public void TextureReloading_AfterDeletion_CreatesNewTexture()
	{
		// Reset state to ensure clean test environment
		ResetState();

		// Set up a path for testing - use a proper absolute path format
		AbsoluteFilePath mockTexturePath = Path.GetFullPath("test_texture.png").As<AbsoluteFilePath>();

		// We need to initialize minimal parts of ImGuiApp for the test
		ImGuiApp.Invoker = new Invoker.Invoker();

		// We'll test using the public API (TryGetTexture) rather than direct access
		// First, verify there's no texture initially
		bool initialTextureExists = ImGuiApp.TryGetTexture(mockTexturePath, out _);
		Assert.IsFalse(initialTextureExists, "Should not have any textures initially");

		// Manually add a texture through direct access to internal property
		System.Collections.Concurrent.ConcurrentDictionary<AbsoluteFilePath, ImGuiAppTextureInfo> texturesDict = ImGuiApp.Textures;
		Assert.IsNotNull(texturesDict, "Textures dictionary should not be null");

		// First texture
		ImGuiAppTextureInfo firstTextureInfo = new()
		{
			Path = mockTexturePath,
			TextureId = 1001,
			Width = 100,
			Height = 100
		};

		// Add the texture directly to the dictionary
		texturesDict.TryAdd(mockTexturePath, firstTextureInfo);

		// Verify it can be accessed via the public API
		bool textureExists = ImGuiApp.TryGetTexture(mockTexturePath, out ImGuiAppTextureInfo? retrievedTexture);
		Assert.IsTrue(textureExists, "Texture should exist after adding");
		Assert.IsNotNull(retrievedTexture, "Retrieved texture should not be null");
		Assert.AreEqual(1001, retrievedTexture!.TextureId, "Texture ID should match");

		// Remove the texture to simulate deletion
		texturesDict.TryRemove(mockTexturePath, out _);

		// Verify it's removed
		textureExists = ImGuiApp.TryGetTexture(mockTexturePath, out _);
		Assert.IsFalse(textureExists, "Texture should be removed after deletion");

		// Create a second texture
		ImGuiAppTextureInfo secondTextureInfo = new()
		{
			Path = mockTexturePath,
			TextureId = 1002,
			Width = 100,
			Height = 100
		};

		// Add the second texture to the dictionary
		texturesDict.TryAdd(mockTexturePath, secondTextureInfo);

		// Verify the newly loaded texture
		textureExists = ImGuiApp.TryGetTexture(mockTexturePath, out ImGuiAppTextureInfo? reloadedTexture);
		Assert.IsTrue(textureExists, "Texture should exist after reloading");
		Assert.IsNotNull(reloadedTexture, "Reloaded texture should not be null");
		Assert.AreEqual(1002, reloadedTexture!.TextureId, "New texture ID should match");
		Assert.AreNotEqual(firstTextureInfo.TextureId, reloadedTexture.TextureId, "Reloaded texture should have a different ID");
	}

	// Minimal IRendererBackend stand-in: records calls without touching a real GL context.
	private sealed class FakeRendererBackend : IRendererBackend
	{
		public int CreateTextureCallCount { get; private set; }
		public int DeleteTextureCallCount { get; private set; }
		public nint NextHandle { get; init; }
		public nint LastUpdatedTextureId { get; private set; }
		public byte[] LastUpdatedPixels { get; private set; } = [];
		public bool UpdateTextureResult { get; init; } = true;
		public int LastUpdateThreadId { get; private set; }

		public nint CreateTexture(ReadOnlySpan<byte> rgba, int width, int height)
		{
			CreateTextureCallCount++;
			return NextHandle;
		}

		public bool UpdateTexture(nint id, ReadOnlySpan<byte> rgba, int width, int height)
		{
			LastUpdatedTextureId = id;
			LastUpdateThreadId = Environment.CurrentManagedThreadId;
			LastUpdatedPixels = rgba.ToArray();
			return UpdateTextureResult;
		}

		public void DeleteTexture(nint id) => DeleteTextureCallCount++;
		public void RenderDrawData(Hexa.NET.ImGui.ImDrawDataPtr drawData) { }
		public void Dispose() { }
	}

	[TestMethod]
	public void UpdateTexture_ThroughTheBackendInterface_ReceivesTheHandleAndPixels()
	{
		// The local is typed as IRendererBackend deliberately. Calling through the interface is what
		// makes this fail to compile until the interface declares UpdateTexture. Calling it on the
		// concrete fake would compile and pass the moment the fake gained the method, proving nothing
		// about the contract this task exists to add.
		using FakeRendererBackend fake = new() { NextHandle = 11 };
		IRendererBackend backend = fake;
		nint id = backend.CreateTexture(new byte[4], 1, 1);
		byte[] pixels = [1, 2, 3, 4];

		bool updated = backend.UpdateTexture(id, pixels, 1, 1);

		Assert.IsTrue(updated, "A backend that supports in-place update should report success");
		Assert.AreEqual(id, fake.LastUpdatedTextureId, "The handle should reach the backend unchanged");
		CollectionAssert.AreEqual(pixels, fake.LastUpdatedPixels, "The pixel payload should reach the backend unchanged");
	}

	[TestMethod]
	public void UploadTextureRGBA_WithBackendRegisteredButControllerUnset_UploadsViaBackend()
	{
		// Reproduces the OnStart-during-construction case (regression from d2b7e72):
		// the renderer backend is registered early in the ImGuiController constructor,
		// but ImGuiApp.controller has not been assigned yet because we are still inside
		// `controller = new(...)`. Texture upload must succeed via the backend regardless.
		ResetState();
		ImGuiApp.Invoker = new Invoker.Invoker();
		FakeRendererBackend backend = new() { NextHandle = 7 };
		ImGuiApp.renderer = backend;
		ImGuiApp.controller = null;

		nint id = ImGuiApp.UploadTextureRGBA(new byte[2 * 2 * 4], 2, 2);

		Assert.AreEqual(1, backend.CreateTextureCallCount, "Upload should route through the registered backend");
		Assert.AreEqual(7, id, "Returned texture id should come from the backend");
	}

	[TestMethod]
	public void DeleteTexture_WithBackendRegisteredButControllerUnset_DeletesViaBackend()
	{
		ResetState();
		ImGuiApp.Invoker = new Invoker.Invoker();
		FakeRendererBackend backend = new();
		ImGuiApp.renderer = backend;
		ImGuiApp.controller = null;

		ImGuiApp.DeleteTexture(123);

		Assert.AreEqual(1, backend.DeleteTextureCallCount, "Delete should route through the registered backend");
	}

	[TestMethod]
	public void GetOrLoadTexture_WithConcurrentFirstAccessToOnePath_UploadsExactlyOneTexture()
	{
		// Loading assets off the UI thread is a supported pattern - the invoker exists precisely to
		// marshal those calls onto the render thread - so two threads can reach GetOrLoadTexture for
		// the same uncached path at once. While the cache miss was checked outside the Invoke, both
		// threads uploaded a separate GPU texture and both assigned Textures[path]. Only the last
		// assignment was reachable, so the other handle could not be reached by DeleteTexture,
		// CleanupAllTextures or ReloadAllTextures and leaked for the life of the GL context.
		ResetState();

		// This thread owns the invoker, so it stands in for the render thread: the callers' Invoke
		// bodies run only while it pumps DoInvokes below.
		ImGuiApp.Invoker = new Invoker.Invoker();
		FakeRendererBackend backend = new() { NextHandle = 4242 };
		ImGuiApp.renderer = backend;
		ImGuiApp.controller = null;

		byte[] png = TestImageBuilder.Png(1, 1, colorType: 6, bitDepth: 8, [0x20, 0x40, 0x60, 0xFF]);
		string file = Path.Join(Path.GetTempPath(), $"{Guid.NewGuid():N}.png");
		File.WriteAllBytes(file, png);

		try
		{
			AbsoluteFilePath path = file.As<AbsoluteFilePath>();
			const int callerCount = 4;
			using CountdownEvent arrived = new(callerCount);
			using ManualResetEventSlim release = new(false);

			Task<ImGuiAppTextureInfo>[] callers = new Task<ImGuiAppTextureInfo>[callerCount];
			for (int i = 0; i < callerCount; i++)
			{
				callers[i] = Task.Run(() =>
				{
					arrived.Signal();
					release.Wait();
					return ImGuiApp.GetOrLoadTexture(path);
				});
			}

			Assert.IsTrue(arrived.Wait(TimeSpan.FromSeconds(30)), "Every caller should reach the start gate");
			release.Set();

			// Nothing can publish a texture until this thread pumps, so holding the pump back keeps
			// all four callers on the uncached side of the cache check. Give them a moment to get
			// there and queue their upload. Pumping too early would only let one caller win by
			// itself, which weakens this assertion rather than failing it spuriously.
			Thread.Sleep(250);

			Stopwatch pump = Stopwatch.StartNew();
			while (Array.Exists(callers, caller => !caller.IsCompleted) && pump.Elapsed < TimeSpan.FromSeconds(30))
			{
				ImGuiApp.Invoker.DoInvokes();
				Thread.Sleep(1);
			}

			foreach (Task<ImGuiAppTextureInfo> caller in callers)
			{
				Assert.IsTrue(caller.IsCompletedSuccessfully, $"Every concurrent caller should complete: {caller.Status} {caller.Exception?.Message}");
			}

			Assert.AreEqual(1, backend.CreateTextureCallCount, "Concurrent first access to one path should upload exactly one GPU texture");

			ImGuiAppTextureInfo published = callers[0].Result;
			foreach (Task<ImGuiAppTextureInfo> caller in callers)
			{
				Assert.AreSame(published, caller.Result, "Every caller should receive the single published texture");
			}

			Assert.IsTrue(ImGuiApp.TryGetTexture(path, out ImGuiAppTextureInfo? cached), "The texture should be published to the cache");
			Assert.AreSame(published, cached, "The cached texture should be the one handed to the callers");
		}
		finally
		{
			File.Delete(file);
		}
	}

	[TestMethod]
	public void CreateTexture_WithWrongPixelCount_Throws()
	{
		byte[] tooFew = new byte[8];

		Assert.ThrowsExactly<ArgumentException>(() => ImGuiApp.CreateTexture(tooFew, 2, 2));
	}

	[TestMethod]
	public void UpdateTexture_WithNullInfo_Throws()
	{
		Assert.ThrowsExactly<ArgumentNullException>(() => ImGuiApp.UpdateTexture(null!, new byte[4], 1, 1));
	}

	[TestMethod]
	public void CreateTexture_WithBackendRegistered_ReturnsUntrackedTextureInfo()
	{
		ResetState();
		ImGuiApp.Invoker = new Invoker.Invoker();
		FakeRendererBackend backend = new() { NextHandle = 42 };
		ImGuiApp.renderer = backend;
		ImGuiApp.controller = null;

		ImGuiAppTextureInfo info = ImGuiApp.CreateTexture(new byte[2 * 2 * 4], 2, 2);

		Assert.AreEqual(42, info.TextureId, "Handle should come from the backend");
		Assert.AreEqual(2, info.Width);
		Assert.AreEqual(2, info.Height);
		Assert.AreEqual(0, ImGuiApp.Textures.Count, "Memory textures must stay out of the path-keyed cache");
	}

	[TestMethod]
	public void UpdateTexture_WithSameSize_UpdatesInPlaceWithoutRecreating()
	{
		ResetState();
		ImGuiApp.Invoker = new Invoker.Invoker();
		FakeRendererBackend backend = new() { NextHandle = 42 };
		ImGuiApp.renderer = backend;
		ImGuiApp.controller = null;
		ImGuiAppTextureInfo info = ImGuiApp.CreateTexture(new byte[1 * 1 * 4], 1, 1);
		byte[] pixels = [1, 2, 3, 4];

		ImGuiApp.UpdateTexture(info, pixels, 1, 1);

		Assert.AreEqual(info.TextureId, backend.LastUpdatedTextureId, "In-place update should target the existing handle");
		CollectionAssert.AreEqual(pixels, backend.LastUpdatedPixels, "The pixel payload should reach the backend unchanged");
		Assert.AreEqual(0, backend.DeleteTextureCallCount, "A successful in-place update must not delete the texture");
		Assert.AreEqual(1, backend.CreateTextureCallCount, "A successful in-place update must not recreate the texture");
	}

	// Runs work on a thread-pool thread while this thread plays the window thread, pumping the
	// invoker until the work finishes, so anything the work marshals through the invoker runs here.
	private Task RunOnWorkerWhilePumping(Action work)
	{
		Task worker = Task.Run(work, TestContext.CancellationToken);
		Stopwatch pump = Stopwatch.StartNew();
		while (!worker.IsCompleted && pump.Elapsed < TimeSpan.FromSeconds(30))
		{
			ImGuiApp.Invoker.DoInvokes();
			SpinWait.SpinUntil(() => worker.IsCompleted, TimeSpan.FromMilliseconds(1));
		}

		return worker;
	}

	[TestMethod]
	public void UpdateTexture_FromAWorkerThread_UpdatesOnTheInvokerThread()
	{
		// The in-place path used to call the backend on whatever thread called UpdateTexture. On
		// OpenGL that is a GL call with no current context, which is exactly what an application
		// producing frames on a worker thread does. The update has to reach the backend on the
		// thread that owns the invoker, and the caller has to wait for it.
		ResetState();
		ImGuiApp.Invoker = new Invoker.Invoker();
		FakeRendererBackend backend = new() { NextHandle = 42 };
		ImGuiApp.renderer = backend;
		ImGuiApp.controller = null;
		ImGuiAppTextureInfo info = ImGuiApp.CreateTexture(new byte[1 * 1 * 4], 1, 1);
		byte[] pixels = [5, 6, 7, 8];

		Task worker = RunOnWorkerWhilePumping(() => ImGuiApp.UpdateTexture(info, pixels, 1, 1));

		Assert.IsTrue(worker.IsCompletedSuccessfully, $"The worker's update should complete: {worker.Status} {worker.Exception?.Message}");
		Assert.AreEqual(Environment.CurrentManagedThreadId, backend.LastUpdateThreadId, "The backend must be called on the invoker's thread, not the worker's");
		Assert.AreEqual(info.TextureId, backend.LastUpdatedTextureId, "In-place update should target the existing handle");
		Assert.AreSequenceEqual(pixels, backend.LastUpdatedPixels, "The pixel payload should reach the backend unchanged");
		Assert.AreEqual(1, backend.CreateTextureCallCount, "A successful in-place update must not recreate the texture");
	}

	[TestMethod]
	public void UpdateTexture_FromAWorkerThread_WhenBackendDeclines_FallsBackToRecreate()
	{
		ResetState();
		ImGuiApp.Invoker = new Invoker.Invoker();
		FakeRendererBackend backend = new() { NextHandle = 42, UpdateTextureResult = false };
		ImGuiApp.renderer = backend;
		ImGuiApp.controller = null;
		ImGuiAppTextureInfo info = ImGuiApp.CreateTexture(new byte[1 * 1 * 4], 1, 1);

		Task worker = RunOnWorkerWhilePumping(() => ImGuiApp.UpdateTexture(info, new byte[1 * 1 * 4], 1, 1));

		Assert.IsTrue(worker.IsCompletedSuccessfully, $"The worker's update should complete: {worker.Status} {worker.Exception?.Message}");
		Assert.AreEqual(1, backend.DeleteTextureCallCount, "A declined update must delete the old texture");
		Assert.AreEqual(2, backend.CreateTextureCallCount, "A declined update must recreate the texture");
	}

	[TestMethod]
	public void UpdateTexture_WithDifferentSize_RecreatesTexture()
	{
		ResetState();
		ImGuiApp.Invoker = new Invoker.Invoker();
		FakeRendererBackend backend = new() { NextHandle = 42 };
		ImGuiApp.renderer = backend;
		ImGuiApp.controller = null;
		ImGuiAppTextureInfo info = ImGuiApp.CreateTexture(new byte[2 * 2 * 4], 2, 2);

		ImGuiApp.UpdateTexture(info, new byte[1 * 1 * 4], 1, 1);

		Assert.AreEqual(1, backend.DeleteTextureCallCount, "A size change must delete the old texture");
		Assert.AreEqual(2, backend.CreateTextureCallCount, "A size change must recreate the texture");
		Assert.AreEqual(1, info.Width, "The info must carry the new width");
		Assert.AreEqual(1, info.Height, "The info must carry the new height");
	}

	[TestMethod]
	public void UpdateTexture_WhenBackendDeclines_FallsBackToRecreate()
	{
		ResetState();
		ImGuiApp.Invoker = new Invoker.Invoker();
		FakeRendererBackend backend = new() { NextHandle = 42, UpdateTextureResult = false };
		ImGuiApp.renderer = backend;
		ImGuiApp.controller = null;
		ImGuiAppTextureInfo info = ImGuiApp.CreateTexture(new byte[1 * 1 * 4], 1, 1);

		ImGuiApp.UpdateTexture(info, new byte[1 * 1 * 4], 1, 1);

		Assert.AreEqual(1, backend.DeleteTextureCallCount, "A declined update must delete the old texture");
		Assert.AreEqual(2, backend.CreateTextureCallCount, "A declined update must recreate the texture");
	}

	// Writes a decodable PNG to a temp file and returns its path. The caller deletes it.
	private static string WriteTempPng(int width, int height)
	{
		byte[] pixels = new byte[width * height * 4];
		Array.Fill(pixels, (byte)0xFF);
		byte[] png = TestImageBuilder.Png(width, height, colorType: 6, bitDepth: 8, pixels);
		string file = Path.Join(Path.GetTempPath(), $"{Guid.NewGuid():N}.png");
		File.WriteAllBytes(file, png);
		return file;
	}

	[TestMethod]
	public void UpdateTexture_OnAPathTrackedTexture_KeepsItPublishedInTheCache()
	{
		// UpdateTexture's recreate fallback went through DeleteTexture, which drops every cache entry
		// pointing at the deleted handle - including the entry GetOrLoadTexture published for this
		// path - and then never restored it. The texture stayed live on the GPU but became invisible
		// to TryGetTexture, so callers were told a loaded texture was missing.
		ResetState();
		ImGuiApp.Invoker = new Invoker.Invoker();
		FakeRendererBackend backend = new() { NextHandle = 4242 };
		ImGuiApp.renderer = backend;
		ImGuiApp.controller = null;

		string file = WriteTempPng(2, 2);
		try
		{
			AbsoluteFilePath path = file.As<AbsoluteFilePath>();
			ImGuiAppTextureInfo info = ImGuiApp.GetOrLoadTexture(path);

			// A size change forces the delete/recreate fallback regardless of what the backend can do
			// in place, which is the path the issue reports.
			ImGuiApp.UpdateTexture(info, new byte[1 * 1 * 4], 1, 1);

			Assert.IsTrue(ImGuiApp.TryGetTexture(path, out ImGuiAppTextureInfo? cached), "A path-tracked texture must stay published after a resize");
			Assert.AreSame(info, cached, "The cache should still hold the instance the caller is using");
			Assert.AreEqual(1, cached!.Width, "The published entry must carry the new size");
			Assert.AreEqual(1, cached.Height, "The published entry must carry the new size");
			Assert.AreEqual(backend.NextHandle, cached.TextureId, "The published entry must carry the recreated handle");
		}
		finally
		{
			File.Delete(file);
		}
	}

	[TestMethod]
	public void UpdateTexture_OnAPathTrackedTexture_DoesNotUploadADuplicateOnTheNextLoad()
	{
		// The downstream cost of the desync, which the cache-presence assertion above does not by
		// itself pin: with the path evicted, the next GetOrLoadTexture missed, re-decoded the file and
		// uploaded a second GPU texture, leaving the first one reachable only through the caller's own
		// ImGuiAppTextureInfo. CleanupAllTextures walks Textures.Keys, so whichever handle was not
		// published could never be freed - the leak the issue reports.
		ResetState();
		ImGuiApp.Invoker = new Invoker.Invoker();
		FakeRendererBackend backend = new() { NextHandle = 4242 };
		ImGuiApp.renderer = backend;
		ImGuiApp.controller = null;

		string file = WriteTempPng(2, 2);
		try
		{
			AbsoluteFilePath path = file.As<AbsoluteFilePath>();
			ImGuiAppTextureInfo info = ImGuiApp.GetOrLoadTexture(path);
			ImGuiApp.UpdateTexture(info, new byte[1 * 1 * 4], 1, 1);

			int uploadsAfterUpdate = backend.CreateTextureCallCount;
			ImGuiAppTextureInfo reloaded = ImGuiApp.GetOrLoadTexture(path);

			Assert.AreSame(info, reloaded, "Re-loading the path should hand back the texture already in use");
			Assert.AreEqual(uploadsAfterUpdate, backend.CreateTextureCallCount, "Re-loading a still-cached path must not upload a duplicate GPU texture");
			CollectionAssert.Contains(ImGuiApp.Textures.Keys.ToList(), path, "CleanupAllTextures walks Textures.Keys, so the path must still be among them for the handle to be freeable");
		}
		finally
		{
			File.Delete(file);
		}
	}

	[TestMethod]
	public void UpdateTexture_OnAnUntrackedTexture_StaysOutOfTheCache()
	{
		// The republish must restore only entries this instance was already published under. A texture
		// from CreateTexture has no path and was never in the cache, so a resize must not add one.
		ResetState();
		ImGuiApp.Invoker = new Invoker.Invoker();
		FakeRendererBackend backend = new() { NextHandle = 42 };
		ImGuiApp.renderer = backend;
		ImGuiApp.controller = null;
		ImGuiAppTextureInfo info = ImGuiApp.CreateTexture(new byte[2 * 2 * 4], 2, 2);

		ImGuiApp.UpdateTexture(info, new byte[1 * 1 * 4], 1, 1);

		Assert.AreEqual(0, ImGuiApp.Textures.Count, "A memory texture must stay out of the path-keyed cache across a resize");
	}

	[TestMethod]
	public void PerformanceSettings_DefaultValues_AreCorrect()
	{
		// Arrange & Act
		ImGuiAppPerformanceSettings settings = new();

		// Assert
		Assert.IsTrue(settings.EnableThrottledRendering, "EnableThrottledRendering should default to true");
		Assert.AreEqual(30.0, settings.FocusedFps);
		Assert.AreEqual(5.0, settings.UnfocusedFps);
		Assert.AreEqual(10.0, settings.IdleFps);
		Assert.AreEqual(2.0, settings.NotVisibleFps);
		Assert.AreEqual(30.0, settings.OverlayFps);
		Assert.IsTrue(settings.EnableIdleDetection, "EnableIdleDetection should default to true");
		Assert.AreEqual(30.0, settings.IdleTimeoutSeconds);
	}

	[TestMethod]
	public void Reset_ResetsPerformanceFields_Correctly()
	{
		// Arrange - Simulate some state changes that would occur during normal operation
		// Note: We can't directly set IsIdle since it has a private setter, but we can verify it's reset
		// We also can't directly access lastInputTime, but we can verify the Reset behavior

		// Act - Reset the application state
		ImGuiApp.Reset();

		// Assert - Verify that performance-related fields are reset to their default values
		Assert.IsFalse(ImGuiApp.IsIdle, "IsIdle should be reset to false after Reset");
		Assert.IsTrue(ImGuiApp.IsFocused, "IsFocused should be reset to true after Reset");

		// Note: lastInputTime is private so we can't directly test it, but the Reset() method
		// should set it to DateTime.UtcNow, which will be tested implicitly through the idle detection logic
	}

	[TestMethod]
	public void Reset_PreventesStatePollutionBetweenTests()
	{
		// Arrange - First call Reset to ensure clean state
		ImGuiApp.Reset();

		// Verify initial clean state
		Assert.IsTrue(ImGuiApp.IsFocused, "IsFocused should start as true after Reset");
		Assert.IsFalse(ImGuiApp.IsIdle, "IsIdle should start as false after Reset");

		// Act - Simulate some state changes that could happen during a test
		// We can call OnUserInput to update lastInputTime
		ImGuiApp.OnUserInput();

		// Call Reset again to simulate what happens between test runs
		ImGuiApp.Reset();

		// Assert - Verify that Reset properly restored the default state
		Assert.IsTrue(ImGuiApp.IsFocused, "IsFocused should be reset to true after second Reset");
		Assert.IsFalse(ImGuiApp.IsIdle, "IsIdle should be reset to false after second Reset");

		// The lastInputTime should be reset to DateTime.UtcNow (current time)
		// We can't test this directly, but the behavior should be consistent
	}
}
