// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.App.Tests;

using System.Numerics;
using ktsu.Semantics.Paths;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Silk.NET.Windowing;

/// <summary>
/// Tests for ImGuiApp window management functionality including initialization, configuration validation, and event handling.
/// </summary>
[TestClass]
public sealed class ImGuiAppWindowManagementTests
{
	[TestInitialize]
	public void Setup()
	{
		ImGuiApp.Reset();
	}

	[TestCleanup]
	public void Cleanup()
	{
		ImGuiApp.Reset();
	}

	#region ValidateConfig Tests

	[TestMethod]
	public void ValidateConfig_WithValidConfig_DoesNotThrow()
	{
		ImGuiAppConfig config = TestHelpers.CreateTestConfig();

		try
		{
			ImGuiApp.ValidateConfig(config);
		}
#pragma warning disable CA1031 // Do not catch general exception types
		catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
		{
			Assert.Fail($"Expected no exception, but got: {ex.Message}");
		}
	}

	[TestMethod]
	public void AdjustConfigForStartup_WithMinimizedState_ConvertsToNormal()
	{
		ImGuiAppConfig config = TestHelpers.CreateTestConfig();
		config.InitialWindowState.LayoutState = WindowState.Minimized;

		ImGuiApp.AdjustConfigForStartup(config);

		Assert.AreEqual(WindowState.Normal, config.InitialWindowState.LayoutState);
	}

	[TestMethod]
	public void ValidateConfig_WithNonExistentIconPath_ThrowsFileNotFoundException()
	{
		ImGuiAppConfig config = TestHelpers.CreateTestConfig(iconPath: "nonexistent_icon.png");

		Assert.ThrowsExactly<FileNotFoundException>(() => ImGuiApp.ValidateConfig(config));
	}

	[TestMethod]
	public void ValidateConfig_WithEmptyIconPath_DoesNotThrow()
	{
		ImGuiAppConfig config = TestHelpers.CreateTestConfig(iconPath: "");

		try
		{
			ImGuiApp.ValidateConfig(config);
		}
#pragma warning disable CA1031 // Do not catch general exception types
		catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
		{
			Assert.Fail($"Expected no exception, but got: {ex.Message}");
		}
	}

	[TestMethod]
	public void ValidateConfig_WithNullFontKey_ThrowsArgumentNullException()
	{
		ImGuiAppConfig config = TestHelpers.CreateTestConfig();

		// This will throw ArgumentNullException when trying to add null key to dictionary
		Assert.ThrowsExactly<ArgumentNullException>(() => config.Fonts[null!] = [1, 2, 3]);
	}

	[TestMethod]
	public void ValidateConfig_WithEmptyFontKey_ThrowsArgumentException()
	{
		ImGuiAppConfig config = TestHelpers.CreateTestConfig();
		config.Fonts[""] = [1, 2, 3];

		Assert.ThrowsExactly<ArgumentException>(() => ImGuiApp.ValidateConfig(config));
	}

	[TestMethod]
	public void ValidateConfig_WithNullFontData_ThrowsArgumentException()
	{
		ImGuiAppConfig config = TestHelpers.CreateTestConfig();
		config.Fonts["TestFont"] = null!;

		Assert.ThrowsExactly<ArgumentException>(() => ImGuiApp.ValidateConfig(config));
	}

	[TestMethod]
	public void ValidateConfig_WithEmptyDefaultFonts_ThrowsArgumentException()
	{
		// Create a config with empty default fonts
		ImGuiAppConfig config = new()
		{
			Title = "Test",
			InitialWindowState = new ImGuiAppWindowState
			{
				Size = new Vector2(800, 600),
				Pos = new Vector2(100, 100),
				LayoutState = WindowState.Normal
			},
			DefaultFonts = [] // Empty default fonts should cause validation error
		};

		Assert.ThrowsExactly<ArgumentException>(() => ImGuiApp.ValidateConfig(config));
	}

	#endregion

	#region InitializeWindow Tests

	[TestMethod]
	public void InitializeWindow_InTestMode_UsesTestWindow()
	{
		Mock<IWindow> mockWindow = TestHelpers.CreateMockWindow();
		ImGuiAppConfig config = new()
		{
			Title = "Test",
			TestMode = true,
			TestWindow = mockWindow.Object,
			InitialWindowState = new ImGuiAppWindowState
			{
				Size = new Vector2(800, 600),
				Pos = new Vector2(100, 100),
				LayoutState = WindowState.Normal
			}
		};

		ImGuiApp.InitializeWindow(config);

		Assert.AreSame(mockWindow.Object, ImGuiApp.window);
	}

	[TestMethod]
	public void InitializeWindow_InTestModeWithNullTestWindow_ThrowsInvalidOperationException()
	{
		ImGuiAppConfig config = new()
		{
			Title = "Test",
			TestMode = true,
			TestWindow = null,
			InitialWindowState = new ImGuiAppWindowState
			{
				Size = new Vector2(800, 600),
				Pos = new Vector2(100, 100),
				LayoutState = WindowState.Normal
			}
		};

		Assert.ThrowsExactly<InvalidOperationException>(() => ImGuiApp.InitializeWindow(config));
	}

	[TestMethod]
	public void InitializeWindow_WithValidConfig_DoesNotThrow()
	{
		ImGuiAppConfig config = TestHelpers.CreateTestConfig();

		try
		{
			ImGuiApp.InitializeWindow(config);
		}
#pragma warning disable CA1031 // Do not catch general exception types
		catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
		{
			Assert.Fail($"Expected no exception, but got: {ex.Message}");
		}
	}

	#endregion

	#region Window State Management Tests

	[TestMethod]
	public void WindowState_WithNullWindow_ReturnsDefaultState()
	{
		// Ensure window is null
		ImGuiApp.window = null;

		ImGuiAppWindowState state = ImGuiApp.WindowState;

		Assert.IsNotNull(state);
		Assert.AreEqual(WindowState.Normal, state.LayoutState);
	}

	[TestMethod]
	public void CaptureWindowNormalState_WithNormalWindow_UpdatesLastNormalWindowState()
	{
		Mock<IWindow> mockWindow = TestHelpers.CreateMockWindow();
		mockWindow.Setup(w => w.WindowState).Returns(WindowState.Normal);
		mockWindow.Setup(w => w.Size).Returns(new Silk.NET.Maths.Vector2D<int>(1024, 768));
		mockWindow.Setup(w => w.Position).Returns(new Silk.NET.Maths.Vector2D<int>(200, 150));

		ImGuiApp.window = mockWindow.Object;

		ImGuiApp.CaptureWindowNormalState();

		Assert.AreEqual(new Vector2(1024, 768), ImGuiApp.LastNormalWindowState.Size);
		Assert.AreEqual(new Vector2(200, 150), ImGuiApp.LastNormalWindowState.Pos);
		Assert.AreEqual(WindowState.Normal, ImGuiApp.LastNormalWindowState.LayoutState);
	}

	[TestMethod]
	public void CaptureWindowNormalState_WithMaximizedWindow_DoesNotUpdateLastNormalWindowState()
	{
		Mock<IWindow> mockWindow = TestHelpers.CreateMockWindow();
		mockWindow.Setup(w => w.WindowState).Returns(WindowState.Maximized);

		ImGuiApp.window = mockWindow.Object;

		Vector2 originalSize = ImGuiApp.LastNormalWindowState.Size;
		Vector2 originalPos = ImGuiApp.LastNormalWindowState.Pos;

		ImGuiApp.CaptureWindowNormalState();

		// Should not change when window is maximized
		Assert.AreEqual(originalSize, ImGuiApp.LastNormalWindowState.Size);
		Assert.AreEqual(originalPos, ImGuiApp.LastNormalWindowState.Pos);
	}

	[TestMethod]
	public void CaptureWindowNormalState_WhileOverlayActive_DoesNotUpdateLastNormalWindowState()
	{
		// Overlay mode makes the window borderless and corner-locked; that geometry must not be
		// recorded as the "normal" state, so it stays available for DisableOverlay to restore.
		Mock<IWindow> mockWindow = TestHelpers.CreateMockWindow();
		mockWindow.Setup(w => w.WindowState).Returns(WindowState.Normal);
		mockWindow.Setup(w => w.Size).Returns(new Silk.NET.Maths.Vector2D<int>(380, 320));
		mockWindow.Setup(w => w.Position).Returns(new Silk.NET.Maths.Vector2D<int>(1500, 24));
		ImGuiApp.window = mockWindow.Object;

		Vector2 originalSize = ImGuiApp.LastNormalWindowState.Size;
		Vector2 originalPos = ImGuiApp.LastNormalWindowState.Pos;

		ImGuiApp.EnableOverlay();
		ImGuiApp.CaptureWindowNormalState();

		Assert.AreEqual(originalSize, ImGuiApp.LastNormalWindowState.Size, "Overlay geometry must not overwrite the saved normal size.");
		Assert.AreEqual(originalPos, ImGuiApp.LastNormalWindowState.Pos, "Overlay geometry must not overwrite the saved normal position.");

		ImGuiApp.DisableOverlay();
	}

	#endregion

	#region Window Position Validation Tests

	[TestMethod]
	public void EnsureWindowPositionIsValid_WithNullWindow_DoesNotThrow()
	{
		ImGuiApp.window = null;

		try
		{
			ImGuiApp.EnsureWindowPositionIsValid();
		}
#pragma warning disable CA1031 // Do not catch general exception types
		catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
		{
			Assert.Fail($"Expected no exception, but got: {ex.Message}");
		}
	}

	[TestMethod]
	public void EnsureWindowPositionIsValid_WithNullMonitor_DoesNotThrow()
	{
		Mock<IWindow> mockWindow = TestHelpers.CreateMockWindow();
		mockWindow.Setup(w => w.Monitor).Returns((IMonitor?)null);
		mockWindow.Setup(w => w.WindowState).Returns(WindowState.Normal);

		ImGuiApp.window = mockWindow.Object;

		try
		{
			ImGuiApp.EnsureWindowPositionIsValid();
		}
#pragma warning disable CA1031 // Do not catch general exception types
		catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
		{
			Assert.Fail($"Expected no exception, but got: {ex.Message}");
		}
	}

	[TestMethod]
	public void EnsureWindowPositionIsValid_WithMinimizedWindow_DoesNotThrow()
	{
		Mock<IWindow> mockWindow = TestHelpers.CreateMockWindow();
		Mock<IMonitor> mockMonitor = new();
		mockWindow.Setup(w => w.Monitor).Returns(mockMonitor.Object);
		mockWindow.Setup(w => w.WindowState).Returns(WindowState.Minimized);

		ImGuiApp.window = mockWindow.Object;

		try
		{
			ImGuiApp.EnsureWindowPositionIsValid();
		}
#pragma warning disable CA1031 // Do not catch general exception types
		catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
		{
			Assert.Fail($"Expected no exception, but got: {ex.Message}");
		}
	}

	#endregion

	#region Performance Settings Tests

	[TestMethod]
	public void UpdateWindowPerformance_WithThrottlingDisabled_DoesNotUpdateTargetFrameTime()
	{
		ImGuiAppConfig config = new()
		{
			Title = "Test",
			InitialWindowState = new ImGuiAppWindowState
			{
				Size = new Vector2(800, 600),
				Pos = new Vector2(100, 100),
				LayoutState = WindowState.Normal
			},
			PerformanceSettings = new ImGuiAppPerformanceSettings
			{
				EnableThrottledRendering = false
			}
		};
		ImGuiApp.Config = config;

		double originalTargetFrameTime = ImGuiApp.targetFrameTimeMs;

		ImGuiApp.UpdateWindowPerformance();

		Assert.AreEqual(originalTargetFrameTime, ImGuiApp.targetFrameTimeMs);
	}

	[TestMethod]
	public void UpdateWindowPerformance_WithIdleDetectionDisabled_SetsIdleToFalse()
	{
		ImGuiAppConfig config = new()
		{
			Title = "Test",
			InitialWindowState = new ImGuiAppWindowState
			{
				Size = new Vector2(800, 600),
				Pos = new Vector2(100, 100),
				LayoutState = WindowState.Normal
			},
			PerformanceSettings = new ImGuiAppPerformanceSettings
			{
				EnableIdleDetection = false
			}
		};
		ImGuiApp.Config = config;

		ImGuiApp.UpdateWindowPerformance();

		Assert.IsFalse(ImGuiApp.IsIdle, "IsIdle should be false when idle detection is disabled");
	}

	[TestMethod]
	public void UpdateWindowPerformance_WithFocusedWindow_DoesNotThrow()
	{
		ImGuiAppConfig config = new()
		{
			Title = "Test",
			InitialWindowState = new ImGuiAppWindowState
			{
				Size = new Vector2(800, 600),
				Pos = new Vector2(100, 100),
				LayoutState = WindowState.Normal
			},
			PerformanceSettings = new ImGuiAppPerformanceSettings
			{
				FocusedFps = 60.0
			}
		};
		ImGuiApp.Config = config;

		try
		{
			ImGuiApp.UpdateWindowPerformance();
		}
#pragma warning disable CA1031 // Do not catch general exception types
		catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
		{
			Assert.Fail($"Expected no exception, but got: {ex.Message}");
		}
	}

	[TestMethod]
	public void UpdateWindowPerformance_WithUnfocusedWindow_DoesNotThrow()
	{
		ImGuiAppConfig config = new()
		{
			Title = "Test",
			InitialWindowState = new ImGuiAppWindowState
			{
				Size = new Vector2(800, 600),
				Pos = new Vector2(100, 100),
				LayoutState = WindowState.Normal
			},
			PerformanceSettings = new ImGuiAppPerformanceSettings
			{
				UnfocusedFps = 10.0
			}
		};
		ImGuiApp.Config = config;

		try
		{
			ImGuiApp.UpdateWindowPerformance();
		}
#pragma warning disable CA1031 // Do not catch general exception types
		catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
		{
			Assert.Fail($"Expected no exception, but got: {ex.Message}");
		}
	}

	[TestMethod]
	public void UpdateWindowPerformance_WhenOverlayActive_UsesOverlayFps()
	{
		ImGuiAppConfig config = new()
		{
			Title = "Test",
			InitialWindowState = new ImGuiAppWindowState
			{
				Size = new Vector2(800, 600),
				Pos = new Vector2(100, 100),
				LayoutState = WindowState.Normal
			},
			PerformanceSettings = new ImGuiAppPerformanceSettings
			{
				FocusedFps = 30.0,
				OverlayFps = 60.0
			}
		};
		ImGuiApp.Config = config;

		// No native window in tests, but overlay mode is logically active regardless of platform.
		ImGuiApp.EnableOverlay(opacity: 0.85f);
		Assert.IsTrue(ImGuiApp.IsOverlayActive, "Overlay mode should be active after EnableOverlay.");

		ImGuiApp.UpdateWindowPerformance();

		Assert.AreEqual(1000.0 / 60.0, ImGuiApp.targetFrameTimeMs, 0.0001,
			"Overlay mode should drive the frame rate from OverlayFps, bypassing focus/idle/visibility throttling.");

		ImGuiApp.DisableOverlay();
		Assert.IsFalse(ImGuiApp.IsOverlayActive, "Overlay mode should be inactive after DisableOverlay.");
	}

	[TestMethod]
	public void UpdateWindowPerformance_WhenOverlayActive_ClearsIdleState()
	{
		ImGuiAppConfig config = new()
		{
			Title = "Test",
			PerformanceSettings = new ImGuiAppPerformanceSettings
			{
				OverlayFps = 30.0,
				EnableIdleDetection = true,
				IdleTimeoutSeconds = 0.0
			}
		};
		ImGuiApp.Config = config;
		ImGuiApp.EnableOverlay();

		ImGuiApp.UpdateWindowPerformance();

		Assert.IsFalse(ImGuiApp.IsIdle, "Overlay mode shows live data, so it should not be throttled to the idle rate.");

		ImGuiApp.DisableOverlay();
	}

	[TestMethod]
	public void EnableOverlay_ThenDisableOverlay_TogglesIsOverlayActive()
	{
		Assert.IsFalse(ImGuiApp.IsOverlayActive, "Overlay mode should be inactive by default.");

		ImGuiApp.EnableOverlay(opacity: 0.5f, clickThrough: true);
		Assert.IsTrue(ImGuiApp.IsOverlayActive);

		// Idempotent: re-enabling (e.g. every frame) keeps it active.
		ImGuiApp.EnableOverlay(opacity: 0.9f, clickThrough: false);
		Assert.IsTrue(ImGuiApp.IsOverlayActive);

		ImGuiApp.DisableOverlay();
		Assert.IsFalse(ImGuiApp.IsOverlayActive);

		// Idempotent: disabling again is safe.
		ImGuiApp.DisableOverlay();
		Assert.IsFalse(ImGuiApp.IsOverlayActive);
	}

	[TestMethod]
	public void SetOverlayGeometry_WhenOverlayInactive_DoesNotThrow()
	{
		try
		{
			// No overlay active and no native window: should be a safe no-op.
			ImGuiApp.SetOverlayGeometry(OverlayCorner.TopRight, 24, 24, 460, 320);
		}
#pragma warning disable CA1031 // Do not catch general exception types
		catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
		{
			Assert.Fail($"Expected no exception, but got: {ex.Message}");
		}
	}

	[TestMethod]
	public void EnableOverlay_OnTransition_CapturesPreOverlayWindowState()
	{
		Mock<IWindow> mockWindow = CreateGeometryMockWindow(new(1024, 768), new(50, 60), WindowState.Normal);
		ImGuiApp.window = mockWindow.Object;
		ImGuiApp.Config = new() { Title = "Test" };

		Assert.IsNull(ImGuiApp.preOverlayWindowState, "No snapshot should exist before overlay mode is entered.");

		ImGuiApp.EnableOverlay();

		Assert.IsNotNull(ImGuiApp.preOverlayWindowState, "Entering overlay mode should snapshot the window.");
		Assert.AreEqual(new Vector2(1024, 768), ImGuiApp.preOverlayWindowState!.Size);
		Assert.AreEqual(new Vector2(50, 60), ImGuiApp.preOverlayWindowState.Pos);
		Assert.AreEqual(WindowState.Normal, ImGuiApp.preOverlayWindowState.LayoutState);

		ImGuiApp.DisableOverlay();

		Assert.IsNull(ImGuiApp.preOverlayWindowState, "Leaving overlay mode should clear the snapshot.");
	}

	[TestMethod]
	public void EnableOverlay_CalledEveryFrame_SnapshotsOnlyTheInitialTransition()
	{
		Mock<IWindow> mockWindow = CreateGeometryMockWindow(new(800, 600), new(10, 20), WindowState.Normal);
		ImGuiApp.window = mockWindow.Object;
		ImGuiApp.Config = new() { Title = "Test" };

		ImGuiApp.EnableOverlay();

		// A later frame relocates/resizes the overlay (as SetOverlayGeometry does), then the app
		// calls EnableOverlay again — as the demo does every frame. The original snapshot must stand.
		mockWindow.Object.Size = new Silk.NET.Maths.Vector2D<int>(380, 320);
		mockWindow.Object.Position = new Silk.NET.Maths.Vector2D<int>(1500, 24);
		ImGuiApp.EnableOverlay();

		Assert.AreEqual(new Vector2(800, 600), ImGuiApp.preOverlayWindowState!.Size, "Re-enabling overlay must not overwrite the original snapshot.");
		Assert.AreEqual(new Vector2(10, 20), ImGuiApp.preOverlayWindowState.Pos);

		ImGuiApp.DisableOverlay();
	}

	[TestMethod]
	public void DisableOverlay_AfterEnable_RestoresWindowSizePositionAndState()
	{
		Mock<IWindow> mockWindow = CreateGeometryMockWindow(new(1280, 720), new(100, 100), WindowState.Normal);
		ImGuiApp.window = mockWindow.Object;
		ImGuiApp.Config = new() { Title = "Test" };

		ImGuiApp.EnableOverlay();

		// Simulate the overlay relocating/resizing the window, as SetOverlayGeometry does natively.
		mockWindow.Object.Size = new Silk.NET.Maths.Vector2D<int>(380, 320);
		mockWindow.Object.Position = new Silk.NET.Maths.Vector2D<int>(1500, 24);

		ImGuiApp.DisableOverlay();

		Assert.AreEqual(new Silk.NET.Maths.Vector2D<int>(1280, 720), mockWindow.Object.Size, "Size should be restored to its pre-overlay value.");
		Assert.AreEqual(new Silk.NET.Maths.Vector2D<int>(100, 100), mockWindow.Object.Position, "Position should be restored to its pre-overlay value.");
		Assert.AreEqual(WindowState.Normal, mockWindow.Object.WindowState, "Layout state should be restored to its pre-overlay value.");
	}

	[TestMethod]
	public void DisableOverlay_AfterEnableWhileMaximized_RestoresMaximizedStateWithoutForcingGeometry()
	{
		Mock<IWindow> mockWindow = CreateGeometryMockWindow(new(1920, 1040), new(0, 0), WindowState.Maximized);
		ImGuiApp.window = mockWindow.Object;
		ImGuiApp.Config = new() { Title = "Test" };

		ImGuiApp.EnableOverlay();
		ImGuiApp.DisableOverlay();

		// A maximized layout state dictates its own geometry, so the restore sets only the state —
		// never the size or position (which would otherwise un-maximize the window).
		mockWindow.VerifySet(w => w.WindowState = WindowState.Maximized, Times.AtLeastOnce,
			"Leaving overlay should restore the maximized layout state.");
		mockWindow.VerifySet(w => w.Size = It.IsAny<Silk.NET.Maths.Vector2D<int>>(), Times.Never,
			"Size should not be forced when restoring a maximized window.");
		mockWindow.VerifySet(w => w.Position = It.IsAny<Silk.NET.Maths.Vector2D<int>>(), Times.Never,
			"Position should not be forced when restoring a maximized window.");
	}

	[TestMethod]
	public void EnableAndDisableOverlay_RemapCanvasOncePerTransition()
	{
		int remapCount = 0;
		Mock<IWindow> mockWindow = CreateGeometryMockWindow(new(800, 600), new(0, 0), WindowState.Normal);
		ImGuiApp.window = mockWindow.Object;
		ImGuiApp.Config = new() { Title = "Test", OnMoveOrResize = () => remapCount++ };

		ImGuiApp.EnableOverlay();
		Assert.AreEqual(1, remapCount, "Entering overlay mode should remap the canvas exactly once.");

		// Per-frame re-enable must not remap again.
		ImGuiApp.EnableOverlay();
		Assert.AreEqual(1, remapCount, "Re-enabling an already-active overlay must not remap the canvas again.");

		ImGuiApp.DisableOverlay();
		Assert.AreEqual(2, remapCount, "Leaving overlay mode should remap the canvas exactly once.");

		// Per-frame re-disable must not remap again.
		ImGuiApp.DisableOverlay();
		Assert.AreEqual(2, remapCount, "Re-disabling an inactive overlay must not remap the canvas again.");
	}

	[TestMethod]
	public void DisableOverlay_WhenNeverEnabled_DoesNotRemapOrTouchWindow()
	{
		int remapCount = 0;
		Mock<IWindow> mockWindow = CreateGeometryMockWindow(new(800, 600), new(0, 0), WindowState.Normal);
		ImGuiApp.window = mockWindow.Object;
		ImGuiApp.Config = new() { Title = "Test", OnMoveOrResize = () => remapCount++ };

		ImGuiApp.DisableOverlay();

		Assert.AreEqual(0, remapCount, "Disabling an overlay that was never enabled must not remap the canvas.");
		mockWindow.VerifySet(w => w.Size = It.IsAny<Silk.NET.Maths.Vector2D<int>>(), Times.Never);
		mockWindow.VerifySet(w => w.Position = It.IsAny<Silk.NET.Maths.Vector2D<int>>(), Times.Never);
		mockWindow.VerifySet(w => w.WindowState = It.IsAny<WindowState>(), Times.Never);
	}

	/// <summary>
	/// Builds a mock window whose size, position, and layout state are read/write with backing
	/// storage, so overlay enter/exit geometry changes can be simulated and asserted.
	/// </summary>
	private static Mock<IWindow> CreateGeometryMockWindow(Silk.NET.Maths.Vector2D<int> size, Silk.NET.Maths.Vector2D<int> position, WindowState state)
	{
		Mock<IWindow> mockWindow = new();
		mockWindow.SetupProperty(w => w.Size, size);
		mockWindow.SetupProperty(w => w.Position, position);
		mockWindow.SetupProperty(w => w.WindowState, state);
		return mockWindow;
	}

	#endregion

	#region Window Icon Tests

	[TestMethod]
	public void SetWindowIcon_WithNonExistentFile_ThrowsFileNotFoundException()
	{
		Assert.ThrowsExactly<FileNotFoundException>(() => ImGuiApp.SetWindowIcon("nonexistent_icon.png"));
	}

	[TestMethod]
	public void SetWindowIcon_WithEmptyPath_ThrowsArgumentException()
	{
		Assert.ThrowsExactly<ArgumentException>(() => ImGuiApp.SetWindowIcon(""));
	}

	#endregion

	#region Texture Management Integration Tests

	[TestMethod]
	public void UseImageBytes_WithNullImage_ThrowsArgumentNullException()
	{
		Assert.ThrowsExactly<ArgumentNullException>(() => ImGuiApp.UseImageBytes(null!, _ => { }));
	}

	[TestMethod]
	public void UseImageBytes_WithNullAction_ThrowsArgumentNullException()
	{
		using SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32> image = new(100, 100);
		Assert.ThrowsExactly<ArgumentNullException>(() => ImGuiApp.UseImageBytes(image, null!));
	}

	[TestMethod]
	public void GetOrLoadTexture_WithInvalidPath_ThrowsArgumentException()
	{
		AbsoluteFilePath invalidPath = new();
		Assert.ThrowsExactly<ArgumentException>(() => ImGuiApp.GetOrLoadTexture(invalidPath));
	}

	#endregion

	#region Cleanup Tests

	[TestMethod]
	public void CleanupPinnedFontData_DoesNotThrow()
	{
		try
		{
			ImGuiApp.CleanupPinnedFontData();
		}
#pragma warning disable CA1031 // Do not catch general exception types
		catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
		{
			Assert.Fail($"Expected no exception, but got: {ex.Message}");
		}
	}

	[TestMethod]
	public void CleanupController_DoesNotThrow()
	{
		try
		{
			ImGuiApp.CleanupController();
		}
#pragma warning disable CA1031 // Do not catch general exception types
		catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
		{
			Assert.Fail($"Expected no exception, but got: {ex.Message}");
		}
	}

	[TestMethod]
	public void CleanupInputContext_DoesNotThrow()
	{
		try
		{
			ImGuiApp.CleanupInputContext();
		}
#pragma warning disable CA1031 // Do not catch general exception types
		catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
		{
			Assert.Fail($"Expected no exception, but got: {ex.Message}");
		}
	}

	[TestMethod]
	public void CleanupOpenGL_DoesNotThrow()
	{
		try
		{
			ImGuiApp.CleanupOpenGL();
		}
#pragma warning disable CA1031 // Do not catch general exception types
		catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
		{
			Assert.Fail($"Expected no exception, but got: {ex.Message}");
		}
	}

	#endregion

	#region Utility Method Tests

	[TestMethod]
	public void EmsToPx_WithNullController_UsesDefaultFontSize()
	{
		ImGuiApp.controller = null;

		int result = ImGuiApp.EmsToPx(2.0f);
		int expected = (int)(2.0f * FontAppearance.DefaultFontPointSize);

		Assert.AreEqual(expected, result);
	}

	[TestMethod]
	public void PtsToPx_WithScaleFactor_ReturnsScaledValue()
	{
		// Use reflection to set ScaleFactor
		typeof(ImGuiApp).GetProperty("ScaleFactor")?.SetValue(null, 2.0f);

		int result = ImGuiApp.PtsToPx(12);

		Assert.AreEqual(24, result);
	}

	[TestMethod]
	public void CommonFontSizes_ContainsExpectedSizes()
	{
		int[] expectedSizes = [10, 12, 14, 16, 18, 20, 24, 32, 48];

		CollectionAssert.AreEqual(expectedSizes, ImGuiApp.CommonFontSizes);
	}

	#endregion

	#region Context Change Tests

	[TestMethod]
	public void CheckAndHandleContextChange_WithNullGL_DoesNotThrow()
	{
		ImGuiApp.gl = null;

		try
		{
			ImGuiApp.CheckAndHandleContextChange();
		}
#pragma warning disable CA1031 // Do not catch general exception types
		catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
		{
			Assert.Fail($"Expected no exception, but got: {ex.Message}");
		}
	}

	[TestMethod]
	public void ReloadAllTextures_WithNullGL_DoesNotThrow()
	{
		ImGuiApp.gl = null;

		try
		{
			ImGuiApp.ReloadAllTextures();
		}
#pragma warning disable CA1031 // Do not catch general exception types
		catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
		{
			Assert.Fail($"Expected no exception, but got: {ex.Message}");
		}
	}

	[TestMethod]
	public void CleanupAllTextures_WithNullGL_DoesNotThrow()
	{
		ImGuiApp.gl = null;

		try
		{
			ImGuiApp.CleanupAllTextures();
		}
#pragma warning disable CA1031 // Do not catch general exception types
		catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
		{
			Assert.Fail($"Expected no exception, but got: {ex.Message}");
		}
	}

	#endregion
}
