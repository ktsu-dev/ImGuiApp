// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Testing;

using System;

using Hexa.NET.ImGui;

using ktsu.ImGui.App;
using ktsu.ImGui.Probes;

/// <summary>
/// Runs an ImGui application with no window, no display and no GPU, advancing frames under the
/// caller's control.
/// </summary>
/// <remarks>
/// Input is injected straight into ImGui, so nothing reaches the operating system, no window needs
/// focus, and no other application on the machine is disturbed. That is what lets these tests run
/// on a busy desktop or on a headless runner without either interfering with the other.
/// </remarks>
public sealed class ImGuiAppHarness : IDisposable
{
	// ImGui contexts are process-global. Two live harnesses would corrupt each other's state in
	// ways that surface as unrelated failures elsewhere, so a second one is refused outright.
	private static ImGuiAppHarness? live;

	private readonly ImGuiAppConfig config;
	private readonly HeadlessImGuiContext context;
	private bool disposed;

	private ImGuiAppHarness(ImGuiAppConfig config, HarnessOptions options)
	{
		this.config = config;
		Options = options;
		Renderer = new SoftwareRenderer(options.Width, options.Height);
		context = new HeadlessImGuiContext(options.Width, options.Height, options.DpiScale, Renderer);
		Mouse = new HarnessMouse(this);
		Keyboard = new HarnessKeyboard(this);
	}

	/// <summary>Gets the options this harness was started with.</summary>
	public HarnessOptions Options { get; }

	/// <summary>Gets the number of frames advanced so far.</summary>
	public int FrameCount { get; private set; }

	/// <summary>
	/// Gets the mouse cursor the most recent frame asked for, as <c>ImGui.GetMouseCursor()</c>
	/// reported it once the frame ended. This is what the desktop backend shows on the window's
	/// pointer, so a test asserts a widget's cursor here.
	/// </summary>
	public ImGuiMouseCursor MouseCursor { get; private set; }

	/// <summary>
	/// Gets where the most recent frame asked the operating system's input method to open, in
	/// ImGui's screen coordinates, or null when nothing was being typed into. This is what the
	/// desktop backend carries to the window, so a test asserts a text widget's caret here.
	/// </summary>
	public ImePlacement? ImePlacement { get; private set; }

	/// <summary>Gets the render target holding the most recently rendered frame.</summary>
	/// <remarks>
	/// A frame is rasterized when its pixels are first read, here or through <see cref="Capture"/>,
	/// rather than when it is stepped, so read this again after each step instead of holding the
	/// bitmap across one. After the harness is disposed it no longer reflects a frame that was
	/// stepped but never read.
	/// </remarks>
	public Bitmap32 Target => Renderer.Target;

	/// <summary>Gets the rasterizer drawing this harness's frames.</summary>
	internal SoftwareRenderer Renderer { get; }

	/// <summary>Gets the mouse input injector.</summary>
	public HarnessMouse Mouse { get; }

	/// <summary>Gets the keyboard input injector.</summary>
	public HarnessKeyboard Keyboard { get; }

	/// <summary>Gets the record of where named items were drawn.</summary>
	public ItemProbe Probe { get; } = new();

	/// <summary>
	/// Starts a harness around an application configuration. Pass the same configuration the
	/// application gives <see cref="ImGuiApp.Start"/>, so the test exercises the real setup rather
	/// than a parallel one written for testing.
	/// </summary>
	/// <param name="config">The application configuration.</param>
	/// <param name="options">Determinism settings.</param>
	/// <returns>A live harness. Dispose it to release the ImGui context.</returns>
	/// <exception cref="InvalidOperationException">Another harness is already running.</exception>
	public static ImGuiAppHarness Start(ImGuiAppConfig config, HarnessOptions options)
	{
		Ensure.NotNull(config);
		Ensure.NotNull(options);

		if (live is not null)
		{
			throw new InvalidOperationException(
				"An ImGuiAppHarness is already running in this process. ImGui contexts are global, so harnesses cannot overlap. Dispose the previous one first.");
		}

		ImGuiAppHarness harness = new(config, options);

		// The rasterizer becomes ImGuiApp's renderer backend for the session, so an application
		// that uploads a texture through ImGuiApp reaches the renderer that is drawing its frames.
		// Without this, anything showing an image it generated rather than loaded fails on a
		// backend that was never installed.
		try
		{
			ImGuiApp.BeginExternalFrameSession(harness.Renderer);
		}
		catch
		{
			// Not yet the live harness, so this releases the context and the rasterizer without
			// ending a session that was never begun.
			harness.Dispose();
			throw;
		}

		// ImGui only accepts the docking flag before the first NewFrame, and Start does not render a
		// frame -- Step does -- so this is the right side of that boundary.
		if (config.EnableDocking)
		{
			Hexa.NET.ImGui.ImGui.GetIO().ConfigFlags |= Hexa.NET.ImGui.ImGuiConfigFlags.DockingEnable;
		}

		// Dear ImGui defaults ConfigMacOSXBehaviors to true on Apple platforms, and one of those
		// behaviours swaps Ctrl and Super so that Cmd drives shortcuts. An injected ImGuiKey.ModCtrl
		// therefore reaches the application as KeySuper on macOS and as KeyCtrl everywhere else, so
		// the same test asserts different things depending on the host. The harness exists to inject
		// input deterministically, so it pins the behaviour off and Ctrl means Ctrl on every
		// platform. A test that wants the macOS mapping can set the flag itself; it is read afresh
		// each frame.
		Hexa.NET.ImGui.ImGui.GetIO().ConfigMacOSXBehaviors = false;

		// ImGuiController does this for a windowed application, and the harness replaces the
		// controller. Without it ImGuizmo, ImNodes and ImPlot never learn the ImGui context and an
		// application that draws with any of them faults inside native code rather than failing as
		// a test. This is reflection-based detection, so it costs nothing when none are referenced.
		ImGuiExtensionManager.Initialize();
		ImGuiExtensionManager.SetImGuiContext();
		ImGuiExtensionManager.CreateExtensionContexts();

		live = harness;

		// Items are recorded against the frame being rendered, which is FrameCount until the step
		// completes and increments it.
		ImGuiProbes.SetProbe((name, min, max) => harness.Probe.Record(name, min, max, harness.FrameCount));

		config.OnStart?.Invoke();

		return harness;
	}

	/// <summary>Advances exactly one frame.</summary>
	public void Step() => Step(1);

	/// <summary>Advances a number of frames.</summary>
	/// <param name="frames">How many frames to advance. Must be positive.</param>
	public void Step(int frames)
	{
		ObjectDisposedException.ThrowIf(disposed, this);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(frames);

		for (int i = 0; i < frames; i++)
		{
			int frameNumber = FrameCount;

			try
			{
				// The previous frame is discarded unread here unless something read its pixels,
				// which is the point: rasterizing is nearly all a headless frame costs, and most
				// frames a test steps through are never looked at. Every frame clears the whole
				// target, so the last frame alone decides what a read sees. This has to precede
				// NewFrame, which reuses the draw data the discarded frame points at.
				Renderer.BeginDeferredFrame(Options.ClearColor);
				context.BeginFrame(Options.FrameDelta);

				// Must follow NewFrame, as it does in ImGuiController.
				ImGuiExtensionManager.BeginFrame();

				// Anything marshaled onto the UI thread has to run, or work queued from a worker
				// never lands. An application that uploads a texture through the invoker would
				// otherwise have every asynchronous result stay invisible to the test.
				config.OnUpdate?.Invoke(Options.FrameDelta);
				ImGuiApp.Invoker.DoInvokes();

				ImGuiApp.RenderFrameContents(config, Options.FrameDelta);

				context.EndFrameDeferred();
				MouseCursor = ImGui.GetMouseCursor();
				ImePlacement = ImeCaret.Requested;
			}
			catch (Exception error) when (error is not HarnessFrameException)
			{
				throw new HarnessFrameException(frameNumber, error);
			}

			FrameCount++;
		}
	}

	/// <summary>
	/// Advances frames until a condition holds or a frame budget runs out.
	/// </summary>
	/// <remarks>
	/// The budget counts frames rather than milliseconds, so a loaded machine takes longer in real
	/// time without changing the outcome. That is the difference between a suite that is slow on a
	/// busy runner and one that fails on it.
	/// </remarks>
	/// <param name="predicate">Checked before the first frame and after every frame.</param>
	/// <param name="maxFrames">The most frames to advance. Must be positive.</param>
	/// <returns>True if the condition held, false if the budget ran out.</returns>
	public bool StepUntil(Func<bool> predicate, int maxFrames)
	{
		ObjectDisposedException.ThrowIf(disposed, this);
		Ensure.NotNull(predicate);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxFrames);

		if (predicate())
		{
			return true;
		}

		for (int i = 0; i < maxFrames; i++)
		{
			Step();

			if (predicate())
			{
				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// Clicks a named item at the center of the rectangle ImGui reported for it, so the test states
	/// no coordinate of its own.
	/// </summary>
	/// <param name="name">A name the application passed to <see cref="ktsu.ImGui.Probes.ImGuiProbes.MarkItem(string)"/>.</param>
	/// <exception cref="ArgumentException">The name was never recorded.</exception>
	/// <exception cref="InvalidOperationException">The item was not drawn in the most recent frame.</exception>
	public void Click(string name)
	{
		ObjectDisposedException.ThrowIf(disposed, this);
		Ensure.NotNull(name);

		if (Probe.IsAmbiguous(name))
		{
			throw new InvalidOperationException(
				$"'{name}' does not identify one item. Candidates: {string.Join(", ", Probe.Matches(name))}. Qualify it further, for example by including the window name.");
		}

		Rectangle rect = Probe.Rect(name)
			?? throw new ArgumentException(
				$"No item matching '{name}' has been marked. Marked so far: {string.Join(", ", Probe.KnownNames)}.",
				nameof(name));

		if (!Probe.WasSeenInFrame(name, FrameCount - 1))
		{
			throw new InvalidOperationException(
				$"Item '{name}' was not drawn in the most recent frame, so its recorded position is stale. Clicking it would hit whatever has since moved there.");
		}

		Mouse.Click(rect.MinX + (rect.Width / 2f), rect.MinY + (rect.Height / 2f));
	}

	/// <summary>
	/// Takes an immutable snapshot of the most recently rendered frame. The snapshot copies the
	/// pixels, so later frames do not alter it.
	/// </summary>
	/// <returns>A snapshot suitable for measuring and for writing to disk.</returns>
	/// <exception cref="InvalidOperationException">No frame has been rendered yet.</exception>
	public CapturedFrame Capture()
	{
		ObjectDisposedException.ThrowIf(disposed, this);

		return FrameCount == 0
			? throw new InvalidOperationException("No frame has been rendered yet, so there is nothing to capture. Call Step first.")
			: new CapturedFrame(Renderer.Target);
	}

	/// <inheritdoc/>
	public void Dispose()
	{
		if (disposed)
		{
			return;
		}

		// A deferred frame points into ImGui's draw data, which is about to be destroyed with the
		// context, and textures are about to be released underneath it.
		Renderer.DiscardDeferredFrame();

		// Extension teardown reaches into ImGui's context, so it has to happen while that context
		// is still alive. Destroying the ImPlot context after ImGui's has gone faults in native code.
		if (ReferenceEquals(live, this))
		{
			ImGuiExtensionManager.Cleanup();
		}

		context.Dispose();
		Renderer.Dispose();

		if (ReferenceEquals(live, this))
		{
			live = null;
			ImGuiProbes.SetProbe(null);
			ImGuiApp.EndExternalFrameSession();
		}

		disposed = true;
	}
}
