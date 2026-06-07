// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

#if IOS

namespace ktsu.ImGui.App;

using System;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;

using CoreGraphics;

using Foundation;

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

	/// <summary>Frame counter used to gate the per-frame diagnostic logging to the first few frames.</summary>
	internal static int DiagFrame { get; set; }

	/// <summary>
	/// CI-only stage tracer for the first few frames: the simulator smoke run crashes with an
	/// unsymbolicated SIGSEGV somewhere in the frame loop, so these markers localise the exact native
	/// call that faults (ImGui NewFrame/Render vs. the Metal draw submission). Removed once green.
	/// </summary>
	/// <param name="stage">A short label identifying the point reached in the frame.</param>
	internal static void DiagLog(string stage)
	{
		if (DiagFrame < 3)
		{
			Console.WriteLine($"IMGUIAPP_IOS_FRAME {stage}");
			Console.Out.Flush();
		}
	}

	/// <summary>Guards one-time installation of the native-library resolver hook.</summary>
	private static bool nativeResolverInstalled;

	/// <summary>
	/// Points HexaGen's native loader at the embedded cimgui dynamic library. Hexa.NET.ImGui ships no
	/// native cimgui for iOS, so we build our own (Dear ImGui 1.92.3 docking, via
	/// <c>scripts/build-cimgui-ios.sh</c>) and embed it as <c>cimgui.dylib</c> through a
	/// <c>Kind=Dynamic</c> NativeReference. HexaGen resolves each native symbol through a function
	/// table built from a library handle; its default <c>dlopen("cimgui")</c> fails on iOS, so we
	/// <c>dlopen</c> the embedded dylib ourselves and feed that handle to every Hexa library load.
	/// A dynamic library (vs. a static archive) is a real load dependency whose exports are
	/// dlsym-visible — static linking left the symbols absent from the executable. Must run before
	/// any ImGui call.
	/// </summary>
	private static void EnsureNativeLibraryResolver()
	{
		if (nativeResolverInstalled)
		{
			return;
		}

		nativeResolverInstalled = true;

		// The dylib is embedded and loaded as an app dependency; dlopen it (via its @rpath install
		// name, leaf, or bundle path) to obtain a handle for the function table. Log which path wins
		// so the candidate list can be trimmed later.
		string bundle = NSBundle.MainBundle.BundlePath;
		string[] candidates =
		[
			"@rpath/cimgui.dylib",
			"cimgui.dylib",
			Path.Combine(bundle, "cimgui.dylib"),
			Path.Combine(bundle, "Frameworks", "cimgui.dylib"),
		];

		nint cimguiHandle = 0;
		foreach (string candidate in candidates)
		{
			if (NativeLibrary.TryLoad(candidate, out nint handle))
			{
				cimguiHandle = handle;
				Console.WriteLine($"IMGUIAPP_IOS_DLOPEN ok={candidate}");
				break;
			}

			Console.WriteLine($"IMGUIAPP_IOS_DLOPEN fail={candidate}");
		}

		nint resolved = cimguiHandle != 0 ? cimguiHandle : NativeLibrary.GetMainProgramHandle();
		LibraryLoader.InterceptLibraryLoad += (string libraryName, out nint pointer) =>
		{
			pointer = resolved;
			return true;
		};

		// Diagnostic (CI-only signal): confirm the cimgui exports resolve from the chosen handle
		// before the first ImGui call. "NOT FOUND" means the dylib didn't load / isn't exporting;
		// addresses mean resolution works and any later crash is an ABI matter. Flushed so the
		// markers survive a follow-on crash.
		foreach (string symbol in new[] { "igGetVersion", "igCreateContext", "igGetIO", "igGetDrawData" })
		{
			bool found = NativeLibrary.TryGetExport(resolved, symbol, out nint address);
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

		// Diagnostic: sane width/height/font-count confirm the 1.92.3 native font structs line up with
		// the managed 1.92.2b bindings; garbage values would point at a font-struct ABI mismatch.
		Console.WriteLine($"IMGUIAPP_IOS_ATLAS w={texData.Width} h={texData.Height} fonts={io.Fonts.Fonts.Size} pixels={(texData.Pixels is null ? "null" : "ok")}");
		Console.Out.Flush();

		if (texData.Pixels is null || texData.Width <= 0 || texData.Height <= 0)
		{
			return;
		}

		ReadOnlySpan<byte> pixels = new(texData.Pixels, texData.Width * texData.Height * 4);
		nint textureId = Renderer!.CreateTexture(pixels, texData.Width, texData.Height);
		texData.SetTexID(textureId);
		Console.WriteLine($"IMGUIAPP_IOS_ATLAS texid=0x{textureId:x}");
		Console.Out.Flush();

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

		DiagLog("newframe");
		ImGui.NewFrame();
		DiagLog("newframe-ok");
		return true;
	}

	/// <summary>Ends the ImGui frame and submits the built draw data to the Metal backend.</summary>
	internal static void EndImGuiFrameAndRender()
	{
		DiagLog("render");
		ImGui.Render();
		DiagLog("render-ok");
		ImDrawDataPtr drawData = ImGui.GetDrawData();
		DiagLog("rdd");
		Renderer!.RenderDrawData(drawData);
		DiagLog("rdd-ok");
		DiagFrame++;
	}
}

#endif
