// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Testing;

using System;

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
	private readonly SoftwareRenderer renderer;
	private readonly HeadlessImGuiContext context;
	private bool disposed;

	private ImGuiAppHarness(ImGuiAppConfig config, HarnessOptions options)
	{
		this.config = config;
		Options = options;
		renderer = new SoftwareRenderer(options.Width, options.Height);
		context = new HeadlessImGuiContext(options.Width, options.Height, options.DpiScale, renderer);
		Mouse = new HarnessMouse(this);
		Keyboard = new HarnessKeyboard(this);
	}

	/// <summary>Gets the options this harness was started with.</summary>
	public HarnessOptions Options { get; }

	/// <summary>Gets the number of frames advanced so far.</summary>
	public int FrameCount { get; private set; }

	/// <summary>Gets the render target holding the most recently rendered frame.</summary>
	public Bitmap32 Target => renderer.Target;

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

		ImGuiApp.BeginExternalFrameSession();

		ImGuiAppHarness harness = new(config, options);
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
				context.BeginFrame(Options.FrameDelta);

				// Anything marshalled onto the UI thread has to run, or work queued from a worker
				// never lands. An application that uploads a texture through the invoker would
				// otherwise have every asynchronous result stay invisible to the test.
				config.OnUpdate?.Invoke(Options.FrameDelta);
				ImGuiApp.Invoker.DoInvokes();

				renderer.Clear(Options.ClearColor);
				ImGuiApp.RenderFrameContents(config, Options.FrameDelta);

				context.EndFrame();
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
	/// <param name="name">A name the application passed to <see cref="ImGuiApp.MarkItem"/>.</param>
	/// <exception cref="ArgumentException">The name was never recorded.</exception>
	/// <exception cref="InvalidOperationException">The item was not drawn in the most recent frame.</exception>
	public void Click(string name)
	{
		ObjectDisposedException.ThrowIf(disposed, this);
		Ensure.NotNull(name);

		Rectangle rect = Probe.Rect(name)
			?? throw new ArgumentException(
				$"No item named '{name}' has been marked. Marked so far: {string.Join(", ", Probe.KnownNames)}.",
				nameof(name));

		if (Probe.IsAmbiguous(name))
		{
			throw new InvalidOperationException(
				$"Item '{name}' was marked more than once in the same frame, so it does not identify one item. Give the widgets distinct labels, or address this one by position.");
		}

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
			: new CapturedFrame(renderer.Target);
	}

	/// <inheritdoc/>
	public void Dispose()
	{
		if (disposed)
		{
			return;
		}

		context.Dispose();
		renderer.Dispose();

		if (ReferenceEquals(live, this))
		{
			live = null;
			ImGuiProbes.SetProbe(null);
			ImGuiApp.EndExternalFrameSession();
		}

		disposed = true;
	}
}
