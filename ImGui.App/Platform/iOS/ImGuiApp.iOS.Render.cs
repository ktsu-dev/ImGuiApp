// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

#if IOS

namespace ktsu.ImGui.App;

using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

using CoreGraphics;

using Foundation;

using Hexa.NET.ImGui;

using HexaGen.Runtime;

/// <summary>
/// iOS renderer orchestration for <see cref="ImGuiApp"/>: stands up the Dear ImGui context and font
/// atlas, owns the Metal <see cref="IRendererBackend"/>, and drives the per-frame begin/submit cycle
/// the <c>Tick</c> loop calls into. This mirrors the role the desktop <c>ImGuiController</c> plays,
/// scoped to what the Metal port needs for v1 (Nerd Font + emoji atlas, DPI-aware crisp text).
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
	/// Points HexaGen's native loader at the embedded cimgui dynamic library. Hexa.NET.ImGui ships no
	/// native cimgui for iOS, so we build our own (Dear ImGui 1.92.3 docking, via
	/// <c>scripts/build-cimgui-ios.sh</c>) and embed <c>cimgui.dylib</c> in the app bundle. HexaGen
	/// resolves each native symbol through a function table built from a library handle; its default
	/// <c>dlopen("cimgui")</c> fails on iOS, so we <c>dlopen</c> the embedded dylib ourselves and feed
	/// that handle to every Hexa library load. A dynamic library (vs. a static archive) is a real load
	/// dependency whose exports are dlsym-visible. Must run before any ImGui call.
	/// </summary>
	private static void EnsureNativeLibraryResolver()
	{
		if (nativeResolverInstalled)
		{
			return;
		}

		nativeResolverInstalled = true;

		// The dylib is embedded as an app load dependency; dlopen it (via its @rpath install name, the
		// leaf, or the absolute bundle path) to obtain a handle for the function table.
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
				break;
			}
		}

		nint resolved = cimguiHandle != 0 ? cimguiHandle : NativeLibrary.GetMainProgramHandle();
		LibraryLoader.InterceptLibraryLoad += (string libraryName, out nint pointer) =>
		{
			pointer = resolved;
			return true;
		};
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

		ConfigureIniPersistence(io);

		Renderer = new MetalRendererBackend(view.MetalLayer);
		BuildFontAtlas(io);
	}

	/// <summary>Holds the native UTF-8 ini path for the process lifetime; ImGui reads the pointer lazily.</summary>
	private static nint iniFilenameHandle;

	/// <summary>
	/// Points <c>io.IniFilename</c> at a sandbox-writable location, or disables persistence. The CWD on
	/// iOS is the read-only app bundle, so the desktop default (<c>imgui.ini</c> in CWD) can't be written;
	/// redirect to <c>Library/Application Support/&lt;bundleId&gt;/imgui.ini</c> (per §2.6 of the port plan).
	/// When <see cref="ImGuiAppConfig.SaveIniSettings"/> is false, clear the filename so ImGui never reads
	/// or writes it.
	/// </summary>
	/// <param name="io">The ImGui IO to configure.</param>
	private static unsafe void ConfigureIniPersistence(ImGuiIOPtr io)
	{
		if (!Config.SaveIniSettings)
		{
			io.IniFilename = null;
			return;
		}

		// LocalApplicationData == ~/Library/Application Support inside the app sandbox. Scope by bundle id
		// to keep the file tidy, and create the directory since ImGui only writes the file, not the path.
		string baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
		string? bundleId = NSBundle.MainBundle.BundleIdentifier;
		string dir = string.IsNullOrEmpty(bundleId) ? baseDir : Path.Combine(baseDir, bundleId);
		Directory.CreateDirectory(dir);
		string iniPath = Path.Combine(dir, "imgui.ini");

		// ImGui stores the char* and dereferences it lazily (NewFrame load, periodic auto-save,
		// DestroyContext), so the UTF-8 buffer must outlive this call — allocate it for the process.
		byte[] utf8 = Encoding.UTF8.GetBytes(iniPath);
		iniFilenameHandle = Marshal.AllocHGlobal(utf8.Length + 1);
		Marshal.Copy(utf8, 0, iniFilenameHandle, utf8.Length);
		Marshal.WriteByte(iniFilenameHandle, utf8.Length, 0); // NUL terminator
		io.IniFilename = (byte*)iniFilenameHandle;
	}

	/// <summary>
	/// Builds the font atlas with parity to the desktop (Nerd Font default, merged emoji, extended
	/// Unicode ranges) and uploads it to the GPU via the backend. Mirrors the desktop legacy-atlas path
	/// (no <c>RendererHasTextures</c>): add fonts, build with <c>ImFontAtlasBuildMain</c>, upload the
	/// RGBA32 pixels, hand the texture id back to ImGui with <c>SetTexID</c>, then free the CPU copy.
	/// Glyphs are rasterized at the physical pixel size for the display scale and rendered back down via
	/// <c>style.FontScaleMain</c>, so text is crisp on a 2x/3x screen rather than an upscaled low-res atlas.
	/// </summary>
	/// <param name="io">The ImGui IO whose font atlas to build.</param>
	private static unsafe void BuildFontAtlas(ImGuiIOPtr io)
	{
		// Rasterize at pointSize * scale (physical pixels); FontGlobalScale below brings rendering back
		// to the logical point size so glyphs map 1:1 onto the high-DPI framebuffer.
		float pixelSize = FontAppearance.DefaultFontPointSize * ScaleFactor;

		uint* unicodeRanges = Config.EnableUnicodeSupport
			? FontHelper.GetExtendedUnicodeRanges(io.Fonts)
			: null;
		byte[]? emojiBytes = ImGuiAppConfig.EmojiFont;
		uint* emojiRanges = emojiBytes is not null ? FontHelper.GetEmojiRanges() : null;

		// DefaultFonts (the bundled Nerd Font) first, then any user-supplied Fonts; merge the emoji
		// glyphs onto each so coloured emoji render inline with text.
		ImFontPtr? defaultFont = null;
		foreach ((_, byte[] fontBytes) in Config.DefaultFonts.Concat(Config.Fonts))
		{
			ImFontPtr? font = FontHelper.AddCustomFont(io, fontBytes, pixelSize, unicodeRanges, mergeWithPrevious: false);
			defaultFont ??= font;

			if (emojiBytes is not null && emojiRanges is not null)
			{
				FontHelper.AddCustomFont(io, emojiBytes, pixelSize, emojiRanges, mergeWithPrevious: true);
			}
		}

		if (defaultFont is { } resolved)
		{
			io.FontDefault = resolved;
		}
		else
		{
			// No configured font loaded (e.g. resources unavailable); fall back to ImGui's built-in font.
			io.Fonts.AddFontDefault();
		}

		// FontScaleMain is the 1.92 replacement for the removed io.FontGlobalScale: a global multiplier
		// on rendered font size. The atlas is rasterised at pointSize * scale, so scaling by 1/scale lays
		// text out at the logical point size while keeping it pixel-crisp on the high-DPI framebuffer.
		ImGui.GetStyle().FontScaleMain = 1f / ScaleFactor;

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
