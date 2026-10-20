// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Probes;
using ktsu.ImGui.Rendering;

public static partial class ImGuiWidgets
{
	/// <summary>
	/// What a <see cref="Viewport3D"/> hands its draw callback for one frame.
	/// </summary>
	/// <param name="Renderer">The renderer to draw with.</param>
	/// <param name="Target">The target to draw into, already cleared to <see cref="Viewport3DState.ClearColor"/> and depth 1.</param>
	/// <param name="ViewProjection">
	/// The camera's view and projection for this viewport. Multiply a model matrix in front of it,
	/// <c>model * ViewProjection</c>, and pass the product as <see cref="DrawState3D.ModelViewProjection"/>.
	/// Use this rather than <see cref="Viewport3DState.ViewProjection"/>: the target can be larger
	/// than the viewport, and this matrix also maps the view onto the part of it that is shown.
	/// </param>
	/// <param name="PixelSize">The viewport's size in pixels, which is its size in ImGui units times the framebuffer scale.</param>
	public readonly record struct Viewport3DFrame(IRenderer3D Renderer, nint Target, Matrix4x4 ViewProjection, Vector2 PixelSize);

	/// <summary>
	/// Draws a 3D view through the host's <see cref="IRenderer3D"/>, with an orbit camera the mouse drives.
	/// </summary>
	/// <param name="label">The widget's id, and its probe name.</param>
	/// <param name="size">The size in ImGui units. A zero or negative axis takes the content region available on that axis.</param>
	/// <param name="state">The camera, which the mouse changes: left-drag orbits, middle-drag or Shift+left-drag pans, the wheel dollies.</param>
	/// <param name="draw">Draws the scene into the target. Called once per frame, and only when there is a 3D renderer.</param>
	/// <returns>
	/// <see langword="true"/> when the scene was drawn; <see langword="false"/> when the host has no
	/// 3D renderer, in which case the viewport is an empty frame of the same size filled with
	/// <see cref="Viewport3DState.ClearColor"/>, and the camera still answers the mouse.
	/// </returns>
	/// <remarks>
	/// <para>
	/// The widget owns the render target: it creates one per viewport, keyed by ImGui id, and
	/// releases it once the viewport has gone <see cref="Viewport3DTargets.ReleaseAfterFrames"/>
	/// frames without being drawn. Immediate mode gives no signal for a widget that stopped being
	/// drawn, so the release happens on a later <see cref="Viewport3D"/> call, for any viewport, or
	/// when the renderer changes.
	/// </para>
	/// <para>
	/// The target's size is rounded up to <see cref="Viewport3DTargets.Granularity"/> pixels and
	/// only reallocated when that rounded size changes. A viewport sized to the content region
	/// changes size every frame while a splitter is dragged, and reallocating per pixel would make
	/// that drag slow. <see cref="Viewport3DFrame.ViewProjection"/> maps the view onto the part of
	/// the target that is shown, so a caller never sees the rounding.
	/// </para>
	/// </remarks>
	[SuppressMessage("Major Code Smell", "S6640:Make sure that using \"unsafe\" is safe here.", Justification = "Required for native ImGui interop; pointer is scoped to the call and not retained.")]
	public static bool Viewport3D(string label, Vector2 size, Viewport3DState state, Action<Viewport3DFrame> draw)
	{
		Ensure.NotNull(label);
		Ensure.NotNull(state);
		Ensure.NotNull(draw);

		Vector2 available = ImGui.GetContentRegionAvail();
		Vector2 frameSize = new(
			size.X > 0f ? size.X : MathF.Max(1f, available.X),
			size.Y > 0f ? size.Y : MathF.Max(1f, available.Y));

		Vector2 origin = ImGui.GetCursorScreenPos();
		ImGui.InvisibleButton(label, frameSize, ImGuiButtonFlags.MouseButtonLeft | ImGuiButtonFlags.MouseButtonMiddle);
		ImGuiProbes.MarkItem(label);
		uint id = ImGui.GetItemID();
		WidgetCursor.OnLastItem(ImGuiMouseCursor.ResizeAll);
		Viewport3DInput.Apply(state, frameSize);

		int frame = ImGui.GetFrameCount();
		ImDrawListPtr drawList = ImGui.GetWindowDrawList();
		bool drew = false;

		if (Renderer3DHost.TryGet(out IRenderer3D? renderer))
		{
			Vector2 framebufferScale = ImGui.GetIO().DisplayFramebufferScale;
			int pixelWidth = Math.Max(1, (int)MathF.Ceiling(frameSize.X * MathF.Max(1f, framebufferScale.X)));
			int pixelHeight = Math.Max(1, (int)MathF.Ceiling(frameSize.Y * MathF.Max(1f, framebufferScale.Y)));

			Viewport3DTargets.Entry entry = Viewport3DTargets.Acquire(id, renderer, pixelWidth, pixelHeight, frame);
			Vector2 shown = new((float)pixelWidth / entry.Width, (float)pixelHeight / entry.Height);

			renderer.Clear(entry.Handle, state.ClearColor, 1f);
			Matrix4x4 viewProjection = state.ViewProjection((float)pixelWidth / pixelHeight) * Viewport3DTargets.ShownPart(shown);
			draw(new Viewport3DFrame(renderer, entry.Handle, viewProjection, new Vector2(pixelWidth, pixelHeight)));

			unsafe
			{
				drawList.AddImage(new ImTextureRef(texId: renderer.GetTargetTexture(entry.Handle)), origin, origin + frameSize, Vector2.Zero, shown, ImGui.GetColorU32(Vector4.One));
			}

			drew = true;
		}
		else
		{
			drawList.AddRectFilled(origin, origin + frameSize, ImGui.GetColorU32(state.ClearColor));
		}

		Viewport3DTargets.Sweep(frame, renderer);
		return drew;
	}

	/// <summary>Turns the mouse over a viewport into camera moves.</summary>
	internal static class Viewport3DInput
	{
		/// <summary>Orbit per pixel dragged, in radians: a drag across a 300-pixel viewport turns it about 170 degrees.</summary>
		internal const float OrbitRadiansPerPixel = 0.01f;

		/// <summary>Dolly per wheel notch, as the exponent <see cref="Viewport3DState.Dolly"/> takes: about 10% closer per notch.</summary>
		internal const float DollyPerNotch = 0.1f;

		/// <summary>Applies this frame's drag and wheel to the camera. Call right after the viewport's item is submitted.</summary>
		/// <param name="state">The camera.</param>
		/// <param name="frameSize">The viewport's size in ImGui units.</param>
		internal static void Apply(Viewport3DState state, Vector2 frameSize)
		{
			ImGuiIOPtr io = ImGui.GetIO();

			if (ImGui.IsItemActive())
			{
				Vector2 delta = io.MouseDelta;
				bool pan = ImGui.IsMouseDown(ImGuiMouseButton.Middle) || io.KeyShift;
				if (pan)
				{
					// The scene follows the pointer at the target's depth: the view there is
					// 2 tan(fov/2) distances high, and Pan already multiplies by the distance.
					float perPixel = 2f * MathF.Tan(state.FieldOfView / 2f) / frameSize.Y;
					state.Pan(-delta.X * perPixel, delta.Y * perPixel);
				}
				else
				{
					state.Orbit(-delta.X * OrbitRadiansPerPixel, delta.Y * OrbitRadiansPerPixel);
				}
			}

			if (ImGui.IsItemHovered())
			{
				// Owning the wheel keeps a scrollable parent window from scrolling as the view dollies.
				ImGui.SetItemKeyOwner(ImGuiKey.MouseWheelY);
				if (io.MouseWheel != 0f)
				{
					state.Dolly(io.MouseWheel * DollyPerNotch);
				}
			}
		}
	}

	/// <summary>
	/// The render targets every <see cref="Viewport3D"/> draws into, one per viewport id.
	/// </summary>
	internal static class Viewport3DTargets
	{
		/// <summary>Target sizes are rounded up to a multiple of this many pixels.</summary>
		internal const int Granularity = 64;

		/// <summary>A target not drawn for this many frames is released.</summary>
		internal const int ReleaseAfterFrames = 60;

		private static readonly Dictionary<uint, Entry> Entries = [];

		/// <summary>Gets how many targets are held, for tests.</summary>
		internal static int Count => Entries.Count;

		/// <summary>Gets the handle of the target held for <paramref name="id"/>, or zero, for tests.</summary>
		/// <param name="id">The viewport's ImGui id.</param>
		/// <returns>The target handle, or zero when there is none.</returns>
		internal static nint HandleFor(uint id) => Entries.TryGetValue(id, out Entry? entry) ? entry.Handle : 0;

		/// <summary>Gets the ids of every target held, for tests.</summary>
		internal static IReadOnlyCollection<uint> Ids => Entries.Keys;

		/// <summary>Rounds a pixel length up to <see cref="Granularity"/>.</summary>
		/// <param name="pixels">The length the viewport needs.</param>
		/// <returns>The length to allocate.</returns>
		internal static int Bucket(int pixels) => (pixels + Granularity - 1) / Granularity * Granularity;

		/// <summary>
		/// The clip-space transform squeezing the view onto the top-left part of a larger target.
		/// </summary>
		/// <param name="shown">The fraction of the target's width and height the viewport shows.</param>
		/// <returns>A matrix to multiply after the view and projection.</returns>
		/// <remarks>
		/// Row-vector convention, as <see cref="Vector4.Transform(Vector4, Matrix4x4)"/> uses. The
		/// target's first row is its top, so the shown rows are the ones nearest NDC y = +1.
		/// </remarks>
		internal static Matrix4x4 ShownPart(Vector2 shown) => new(
			shown.X, 0f, 0f, 0f,
			0f, shown.Y, 0f, 0f,
			0f, 0f, 1f, 0f,
			shown.X - 1f, 1f - shown.Y, 0f, 1f);

		/// <summary>Finds or makes the target for a viewport, sized for this frame.</summary>
		/// <param name="id">The viewport's ImGui id.</param>
		/// <param name="renderer">The renderer drawing this frame.</param>
		/// <param name="pixelWidth">The width the viewport needs, in pixels.</param>
		/// <param name="pixelHeight">The height it needs.</param>
		/// <param name="frame">The ImGui frame count.</param>
		/// <returns>The target, at least as large as asked.</returns>
		internal static Entry Acquire(uint id, IRenderer3D renderer, int pixelWidth, int pixelHeight, int frame)
		{
			int width = Bucket(pixelWidth);
			int height = Bucket(pixelHeight);

			if (Entries.TryGetValue(id, out Entry? entry) && ReferenceEquals(entry.Owner, renderer))
			{
				if (entry.Width != width || entry.Height != height)
				{
					if (!renderer.ResizeRenderTarget(entry.Handle, width, height))
					{
						entry.Handle = renderer.CreateRenderTarget(width, height, depth: true);
					}

					entry.Width = width;
					entry.Height = height;
				}
			}
			else
			{
				// A target another renderer made belongs to that renderer, which is gone or is not
				// the one drawing now, so it is forgotten rather than deleted through the wrong one.
				entry = new Entry(renderer, renderer.CreateRenderTarget(width, height, depth: true), width, height);
				Entries[id] = entry;
			}

			entry.LastFrame = frame;
			return entry;
		}

		/// <summary>Releases targets whose viewports have stopped being drawn.</summary>
		/// <param name="frame">The ImGui frame count.</param>
		/// <param name="renderer">The renderer drawing this frame, or null when there is none.</param>
		internal static void Sweep(int frame, IRenderer3D? renderer)
		{
			if (Entries.Count == 0)
			{
				return;
			}

			List<uint>? stale = null;
			foreach ((uint id, Entry entry) in Entries)
			{
				bool otherRenderer = !ReferenceEquals(entry.Owner, renderer);
				if (otherRenderer || frame - entry.LastFrame > ReleaseAfterFrames || frame < entry.LastFrame)
				{
					(stale ??= []).Add(id);
				}
			}

			if (stale is null)
			{
				return;
			}

			foreach (uint id in stale)
			{
				Entry entry = Entries[id];
				Entries.Remove(id);
				if (ReferenceEquals(entry.Owner, renderer))
				{
					renderer!.DeleteRenderTarget(entry.Handle);
				}
			}
		}

		/// <summary>One viewport's target.</summary>
		/// <param name="owner">The renderer that made it.</param>
		/// <param name="handle">Its handle.</param>
		/// <param name="width">Its allocated width.</param>
		/// <param name="height">Its allocated height.</param>
		internal sealed class Entry(IRenderer3D owner, nint handle, int width, int height)
		{
			/// <summary>Gets the renderer that made the target.</summary>
			internal IRenderer3D Owner { get; } = owner;

			/// <summary>Gets or sets the target's handle.</summary>
			internal nint Handle { get; set; } = handle;

			/// <summary>Gets or sets the allocated width in pixels.</summary>
			internal int Width { get; set; } = width;

			/// <summary>Gets or sets the allocated height in pixels.</summary>
			internal int Height { get; set; } = height;

			/// <summary>Gets or sets the last frame the viewport was drawn on.</summary>
			internal int LastFrame { get; set; }
		}
	}
}
