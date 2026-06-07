// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

#if IOS

namespace ktsu.ImGui.App;

using System;
using System.Numerics;
using System.Runtime.InteropServices;

using CoreGraphics;

using Hexa.NET.ImGui;

using HexaGen.Runtime;

/// <summary>
/// iOS renderer orchestration for <see cref="ImGuiApp"/>: stands up the Dear ImGui context and font
/// atlas, owns the Metal <see cref="IRendererBackend"/>, and drives the per-frame begin/submit cycle
/// the <c>Tick</c> loop calls into. This mirrors the role the desktop <c>ImGuiController</c> plays,
/// scoped to what the Metal port needs for v1 (default font atlas; full font parity is a later task).
/// </summary>
public static partial class ImGuiApp
{
	/// <summary>The Metal-backed renderer; null until <see cref="InitializeRenderer"/> runs.</summary>
	internal static IRendererBackend? Renderer { get; private set; }

	/// <summary>The Metal-backed view, used to read the logical display size and pixel scale each frame.</summary>
	internal static MetalView? RenderView { get; private set; }

	/// <summary>Guards one-time installation of the native-library resolver hook.</summary>
	private static bool nativeResolverInstalled;

	/// <summary>
	/// Routes Hexa.NET native library loads to the app's own program image. Hexa.NET.ImGui ships no
	/// native cimgui for iOS, so we statically link it into the app instead (Dear ImGui 1.92.2b, built
	/// by <c>scripts/build-cimgui-ios.sh</c> and linked via the <c>libcimgui-sim.a</c> NativeReference).
	/// HexaGen normally resolves each native symbol from a handle returned by <c>dlopen</c>; on iOS that
	/// fails because there is no on-disk <c>cimgui.dylib</c>. Returning
	/// <see cref="NativeLibrary.GetMainProgramHandle"/> makes HexaGen's function table resolve the
	/// statically-linked exports (<c>igGetVersion</c>, <c>igBegin</c>, …) via <c>TryGetExport</c> — the
	/// managed equivalent of <c>DllImport("__Internal")</c>. Must run before any ImGui call.
	/// </summary>
	private static void EnsureNativeLibraryResolver()
	{
		if (nativeResolverInstalled)
		{
			return;
		}

		nativeResolverInstalled = true;
		LibraryLoader.InterceptLibraryLoad += static (string libraryName, out nint pointer) =>
		{
			pointer = NativeLibrary.GetMainProgramHandle();
			return true;
		};

		// Diagnostic (CI-only signal): confirm the statically-linked cimgui exports are actually
		// reachable from the main program image before the first ImGui call. If these print
		// "NOT FOUND", the symbols were dead-stripped / the NativeReference did not link them (a
		// resolution problem); if they print addresses but ImGui.CreateContext still crashes, the
		// fault is an ABI/struct mismatch instead. Flushed so the markers survive a follow-on crash.
		nint mainHandle = NativeLibrary.GetMainProgramHandle();
		foreach (string symbol in new[] { "igGetVersion", "igCreateContext", "igGetIO", "igGetDrawData" })
		{
			bool found = NativeLibrary.TryGetExport(mainHandle, symbol, out nint address);
			Console.WriteLine($"IMGUIAPP_IOS_SYM {symbol}={(found ? "0x" + address.ToString("x") : "NOT FOUND")}");
		}

		Console.Out.Flush();
	}

	/// <summary>
	/// Creates the ImGui context, configures IO, builds the font atlas, and constructs the Metal
	/// backend for the given view. Called once by the view controller after its Metal view exists and
	/// before <see cref="RaiseStart"/>, so consumer <c>OnStart</c> code can rely on a live renderer.
	/// </summary>
	/// <param name="view">The Metal-backed view to render into.</param>
	internal static void InitializeRenderer(MetalView view)
	{
		Ensure.NotNull(view);
		if (Renderer is not null)
		{
			return;
		}

		RenderView = view;
		ScaleFactor = view.Scale;

		// Point Hexa's native loader at the statically-linked cimgui before the first ImGui call.
		EnsureNativeLibraryResolver();

		ImGui.CreateContext();
		ImGui.StyleColorsDark();

		ImGuiIOPtr io = ImGui.GetIO();
		io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;

		// Note: ini persistence (honouring Config.SaveIniSettings and redirecting imgui.ini away from
		// the read-only app bundle to a writable directory) is handled by the later ini-redirect task.

		Renderer = new MetalRendererBackend(view.MetalLayer);
		BuildFontAtlas(io);
	}

	/// <summary>
	/// Builds the default font atlas and uploads it to the GPU via the backend, mirroring the desktop
	/// legacy-atlas path (no <c>RendererHasTextures</c>): build with <c>ImFontAtlasBuildMain</c>, upload
	/// the RGBA32 pixels, hand the texture id back to ImGui with <c>SetTexID</c>, then free the CPU copy.
	/// </summary>
	/// <param name="io">The ImGui IO whose font atlas to build.</param>
	private static unsafe void BuildFontAtlas(ImGuiIOPtr io)
	{
		// The context is freshly created here, so add the default font unconditionally; richer font
		// configuration (sizes, emoji, Nerd Font ranges) shared with desktop is a later task.
		io.Fonts.AddFontDefault();

		if (!io.Fonts.TexIsBuilt)
		{
			ImGuiP.ImFontAtlasBuildMain(io.Fonts);
		}

		ImTextureDataPtr texData = io.Fonts.TexData;
		if (texData.Pixels is null || texData.Width <= 0 || texData.Height <= 0)
		{
			return;
		}

		ReadOnlySpan<byte> pixels = new(texData.Pixels, texData.Width * texData.Height * 4);
		nint textureId = Renderer!.CreateTexture(pixels, texData.Width, texData.Height);
		texData.SetTexID(textureId);

		// The atlas pixels now live on the GPU; drop the CPU copy to save memory (matches desktop).
		io.Fonts.ClearTexData();
	}

	/// <summary>
	/// Begins an ImGui frame if the renderer is ready: pushes the current display size, framebuffer
	/// scale, and frame delta, keeps the drawable sized to the view, then calls <c>NewFrame</c>.
	/// </summary>
	/// <param name="deltaSeconds">Elapsed wall-clock time since the previous frame, in seconds.</param>
	/// <returns><see langword="true"/> if a frame was begun and must be ended with <see cref="EndImGuiFrameAndRender"/>.</returns>
	internal static bool TryBeginImGuiFrame(float deltaSeconds)
	{
		if (Renderer is null || RenderView is null)
		{
			return false;
		}

		float scale = RenderView.Scale;
		CGRect bounds = RenderView.Bounds;

		ImGuiIOPtr io = ImGui.GetIO();
		io.DisplaySize = new Vector2((float)bounds.Width, (float)bounds.Height);
		io.DisplayFramebufferScale = new Vector2(scale, scale);
		io.DeltaTime = deltaSeconds > 0f ? deltaSeconds : 1f / 60f;

		RenderView.MetalLayer.DrawableSize = new CGSize(bounds.Width * scale, bounds.Height * scale);

		ImGui.NewFrame();
		return true;
	}

	/// <summary>Ends the ImGui frame and submits the built draw data to the Metal backend.</summary>
	internal static void EndImGuiFrameAndRender()
	{
		ImGui.Render();
		Renderer!.RenderDrawData(ImGui.GetDrawData());
	}
}

#endif
