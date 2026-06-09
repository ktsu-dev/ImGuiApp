// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.App;

using System.Threading;
using Silk.NET.Windowing;

/// <summary>
/// Embedded / non-blocking hosting support for <see cref="ImGuiApp"/>.
/// </summary>
public static partial class ImGuiApp
{
	/// <summary>
	/// Starts the ImGui application without blocking the calling thread, returning a session the host can
	/// drive.
	/// </summary>
	/// <param name="config">The configuration settings for the ImGui application.</param>
	/// <returns>An <see cref="IImGuiAppSession"/> controlling the running session.</returns>
	/// <remarks>
	/// The render loop runs on a dedicated UI thread owned by the session rather than on the caller, which
	/// is what allows a plugin host to keep control of its own thread. With
	/// <see cref="ImGuiAppConfig.WindowHost"/> set to <see cref="ImGuiAppWindowHost.Standalone"/> this is a
	/// non-blocking floating window; with <see cref="ImGuiAppWindowHost.EmbeddedChild"/> the window is
	/// reparented under <see cref="ImGuiAppConfig.ParentWindowHandle"/> so it renders as a docked child.
	/// Embedded hosting is currently implemented on Windows; the existing <see cref="Start(ImGuiAppConfig)"/>
	/// behaviour is unchanged.
	/// </remarks>
	/// <exception cref="ArgumentException">Thrown when <see cref="ImGuiAppConfig.WindowHost"/> is <see cref="ImGuiAppWindowHost.EmbeddedChild"/> but no <see cref="ImGuiAppConfig.ParentWindowHandle"/> was supplied.</exception>
	/// <exception cref="InvalidOperationException">Thrown when an application is already running.</exception>
	/// <exception cref="PlatformNotSupportedException">Thrown when embedded hosting is requested on a platform where it is not yet implemented.</exception>
	public static IImGuiAppSession StartEmbedded(ImGuiAppConfig config)
	{
		Ensure.NotNull(config);

		if (window != null)
		{
			throw new InvalidOperationException("Application is already running.");
		}

		if (config.WindowHost == ImGuiAppWindowHost.EmbeddedChild)
		{
			if (config.ParentWindowHandle == 0)
			{
				throw new ArgumentException("ParentWindowHandle must be set when WindowHost is EmbeddedChild.", nameof(config));
			}

			if (!OperatingSystem.IsWindows())
			{
				throw new PlatformNotSupportedException("Embedded window hosting is currently implemented on Windows only.");
			}
		}

		EmbeddedSession session = new(config);
		session.Start();
		return session;
	}

	/// <summary>
	/// Reparents the freshly created window under the host's parent window and strips its decorations.
	/// </summary>
	/// <param name="config">The configuration whose <see cref="ImGuiAppConfig.ParentWindowHandle"/> to embed under.</param>
	private static void ReparentToHost(ImGuiAppConfig config)
	{
		if (config.WindowHost != ImGuiAppWindowHost.EmbeddedChild || !OperatingSystem.IsWindows())
		{
			return;
		}

		nint child = TryGetWindowHandle();
		if (child == 0 || window == null)
		{
			return;
		}

		// Convert the top-level window into a borderless child of the host window.
		long style = NativeMethods.GetWindowLongPtr(child, NativeMethods.GWL_STYLE);
		style &= ~(NativeMethods.WS_POPUP | NativeMethods.WS_CAPTION | NativeMethods.WS_THICKFRAME | NativeMethods.WS_SYSMENU);
		style |= NativeMethods.WS_CHILD;
		_ = NativeMethods.SetWindowLongPtr(child, NativeMethods.GWL_STYLE, (nint)style);

		_ = NativeMethods.SetParent(child, config.ParentWindowHandle);

		_ = NativeMethods.SetWindowPos(
			child,
			0,
			0,
			0,
			window.Size.X,
			window.Size.Y,
			NativeMethods.SWP_FRAMECHANGED | NativeMethods.SWP_NOZORDER | NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_SHOWWINDOW);
	}

	/// <summary>
	/// A non-blocking <see cref="ImGuiApp"/> session that owns its render loop on a dedicated UI thread.
	/// </summary>
	private sealed class EmbeddedSession : IImGuiAppSession
	{
		private readonly ImGuiAppConfig sessionConfig;
		private readonly Thread uiThread;
		private readonly ManualResetEventSlim windowReady = new(false);
		private volatile bool running;
		private bool disposed;

		internal EmbeddedSession(ImGuiAppConfig config)
		{
			sessionConfig = config;
			uiThread = new Thread(RunLoop)
			{
				IsBackground = true,
				Name = "ImGuiApp Embedded UI",
			};
		}

		/// <inheritdoc/>
		public bool IsRunning => running;

		/// <inheritdoc/>
		public nint NativeHandle => TryGetWindowHandle();

		/// <summary>
		/// Starts the UI thread and waits until the window has been created (or creation has failed).
		/// </summary>
		internal void Start()
		{
			uiThread.Start();
			// Wait, with a generous timeout, so the caller sees a valid NativeHandle on return without
			// hanging forever if window creation fails.
			windowReady.Wait(TimeSpan.FromSeconds(10));
		}

		/// <inheritdoc/>
		public void Resize(int width, int height)
		{
			if (!running)
			{
				return;
			}

			_ = Invoker?.InvokeAsync(() =>
			{
				IWindow? target = window;
				if (target is null)
				{
					return;
				}

				target.Size = new(width, height);
			});
		}

		/// <inheritdoc/>
		public void Focus(bool focused)
		{
			if (!running)
			{
				return;
			}

			_ = Invoker?.InvokeAsync(() => IsFocused = focused);
		}

		/// <inheritdoc/>
		public void Dispose()
		{
			if (disposed)
			{
				return;
			}

			disposed = true;

			if (running)
			{
				_ = Invoker?.InvokeAsync(() => window?.Close());
				_ = uiThread.Join(TimeSpan.FromSeconds(5));
			}

			windowReady.Dispose();
			GC.SuppressFinalize(this);
		}

		private void RunLoop()
		{
			try
			{
				Invoker = new();
				Config = sessionConfig;
				AdjustConfigForStartup(sessionConfig);
				ValidateConfig(sessionConfig);
				ForceDpiAware.Windows();
				InitializeWindow(sessionConfig);
				SetupWindowLoadHandler(sessionConfig);
				SetupWindowResizeHandler(sessionConfig);
				SetupWindowMoveHandler(sessionConfig);
				SetupWindowUpdateHandler(sessionConfig);
				SetupWindowRenderHandler(sessionConfig);
				SetupWindowClosingHandler();

				window!.FocusChanged += focused => IsFocused = focused;
				window.Load += OnWindowLoaded;

				running = true;
				window.Run();
			}
			finally
			{
				running = false;
				windowReady.Set();
				window?.Dispose();
				window = null;
			}
		}

		private void OnWindowLoaded()
		{
			ReparentToHost(sessionConfig);
			windowReady.Set();
		}
	}
}
