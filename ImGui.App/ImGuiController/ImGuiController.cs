// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.ImGuiController;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Hexa.NET.ImGui;
using ktsu.ImGui.App;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

/// <summary>
/// Runs ImGui in a Silk.NET window: carries the window's keyboard, mouse, size, cursor and input
/// method to and from an <see cref="ImGuiContextHost"/>, and draws the frames it produces with an
/// <see cref="OpenGLRenderer"/>.
/// </summary>
/// <remarks>
/// This used to be all three at once, which is why the headless harness kept a copy of the context
/// setup it could not reach without a GL context and a window. The context and the frame cycle now
/// live in <see cref="ImGuiContextHost"/>, which the harness drives too, and the drawing in
/// <see cref="OpenGLRenderer"/>; what remains here is the window (#313).
/// </remarks>
internal sealed class ImGuiController : IDisposable
{
	internal IView? _view;
	internal IInputContext? _input;
	internal bool _frameBegun;
	internal readonly List<char> _pressedChars = [];
	internal IKeyboard? _keyboard;
	internal IMouse? _mouse;
	private readonly PlatformCursor _platformCursor = new();
	private readonly PlatformIme _platformIme = PlatformIme.ForCurrentPlatform();

	// The context and frame cycle, with no window or graphics API behind it.
	internal ImGuiContextHost? _host;

	// Draws the frames, holds the textures, and is the application's renderer backend.
	internal OpenGLRenderer? _renderer;

	internal int _windowWidth;
	internal int _windowHeight;

	internal bool FontsConfigured { get; private set; }

	internal bool _disposed;

	/// <summary>
	/// Constructs a new ImGuiController.
	/// </summary>
	public ImGuiController(GL gl, IView view, IInputContext input) : this(gl, view, input, null, null)
	{
	}

	/// <summary>
	/// Constructs a new ImGuiController with font configuration.
	/// </summary>
	public ImGuiController(GL gl, IView view, IInputContext input, ImGuiFontConfig imGuiFontConfig) : this(gl, view, input, imGuiFontConfig, null)
	{
	}

	/// <summary>
	/// Constructs a new ImGuiController with an onConfigureIO Action.
	/// </summary>
	public ImGuiController(GL gl, IView view, IInputContext input, Action onConfigureIO) : this(gl, view, input, null, onConfigureIO)
	{
	}

	/// <summary>
	/// Constructs a new ImGuiController with font configuration and onConfigure Action.
	/// </summary>
	[SuppressMessage("Minor Code Smell", "S3427:Method overloads with default parameter values should not overlap", Justification = "Overlapping overloads are by design to support multiple calling conventions; the explicit 3-param overload delegates here.")]
	[SuppressMessage("Major Code Smell", "S6640:Make sure that using \"unsafe\" is safe here", Justification = "Required for native ImGui font-loading interop; unsafe pointer is pinned within a fixed block and not retained.")]
	[SuppressMessage("Major Code Smell", "S3010:Static fields should not be updated in constructors", Justification = "Registering the renderer as the global ImGuiApp.renderer must happen here, before onConfigureIO runs, because user OnStart code resolves the backend via ImGuiApp.renderer and the ImGuiApp.controller field is not yet assigned. See the inline comment.")]
	public ImGuiController(GL gl, IView view, IInputContext input, ImGuiFontConfig? imGuiFontConfig = null, Action? onConfigureIO = null)
	{
		DebugLogger.Log("ImGuiController: Starting initialization");
		Init(gl, view, input);
		DebugLogger.Log("ImGuiController: Init completed");

		// Register the renderer as the backend now that the GL context is live. This must happen
		// before onConfigureIO runs: user OnStart code (invoked from there) may load textures,
		// and that path resolves the backend via ImGuiApp.renderer. The ImGuiApp.controller
		// field is not assigned until this constructor returns, so it cannot be relied on here.
		ImGuiApp.renderer = _renderer;

		ImGuiIOPtr io = ImGui.GetIO();
		if (imGuiFontConfig is not null)
		{
			DebugLogger.Log("ImGuiController: Adding font from config");
			nint glyphRange = imGuiFontConfig.Value.GetGlyphRange?.Invoke(io) ?? default;

			unsafe
			{
				fixed (byte* fontPathPtr = System.Text.Encoding.UTF8.GetBytes(imGuiFontConfig.Value.FontPath + "\0"))
				{
					io.Fonts.AddFontFromFileTTF(fontPathPtr, imGuiFontConfig.Value.FontSize, null, (uint*)glyphRange);
				}
			}
		}

		DebugLogger.Log("ImGuiController: Calling onConfigureIO");
		onConfigureIO?.Invoke();
		DebugLogger.Log("ImGuiController: onConfigureIO completed");

		// The font atlas is not uploaded here. Under RendererHasTextures ImGui creates it on the
		// first frame that draws text, so what start-up readies is the pipeline, and fonts count as
		// configured once the pipeline can carry them.
		FontsConfigured = true;

		DebugLogger.Log("ImGuiController: Beginning frame");
		BeginFrame();
		DebugLogger.Log("ImGuiController: Initialization completed");
	}

	[SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Both are owned by this controller and released in Dispose.")]
	internal void Init(GL gl, IView view, IInputContext input)
	{
		_renderer = new OpenGLRenderer(gl);
		_view = view;
		_input = input;
		_windowWidth = view.Size.X;
		_windowHeight = view.Size.Y;

		// AutoDiscoverExtensions = false asks for none.
		_host = new ImGuiContextHost(_renderer.TextureUploader, ImGuiApp.Config.EffectiveExtensions);
	}

	internal void BeginFrame()
	{
		_host?.NewFrame(DisplaySize(), Vector2.One, 1f / 60f);
		_frameBegun = true;
		_keyboard = _input?.Keyboards[0];
		_mouse = _input?.Mice[0];
		_view?.Resize += WindowResized;

		if (_keyboard is not null)
		{
			_keyboard.KeyDown += OnKeyDown;
			_keyboard.KeyUp += OnKeyUp;
			_keyboard.KeyChar += OnKeyChar;
		}

		if (_mouse is not null)
		{
			_mouse.MouseDown += OnMouseDown;
			_mouse.MouseUp += OnMouseUp;
			_mouse.MouseMove += OnMouseMove;
			_mouse.Scroll += OnMouseScroll;
		}
	}

	/// <summary>
	/// Delegate to receive keyboard key down events.
	/// </summary>
	/// <param name="keyboard">The keyboard context generating the event.</param>
	/// <param name="keycode">The native keycode of the pressed key.</param>
	/// <param name="scancode">The native scancode of the pressed key.</param>
	internal static void OnKeyDown(IKeyboard keyboard, Key keycode, int scancode)
	{
		ImGuiApp.OnUserInput();
		OnKeyEvent(keyboard, keycode, scancode, down: true);
	}

	/// <summary>
	/// Delegate to receive keyboard key up events.
	/// </summary>
	/// <param name="keyboard">The keyboard context generating the event.</param>
	/// <param name="keycode">The native keycode of the released key.</param>
	/// <param name="scancode">The native scancode of the released key.</param>
	internal static void OnKeyUp(IKeyboard keyboard, Key keycode, int scancode)
	{
		ImGuiApp.OnUserInput();
		OnKeyEvent(keyboard, keycode, scancode, down: false);
	}

	internal static void OnMouseScroll(IMouse mouse, ScrollWheel scroll)
	{
		ImGuiApp.OnUserInput();
		ImGuiIOPtr io = ImGui.GetIO();
		io.AddMouseWheelEvent(scroll.X, scroll.Y);
	}

	internal static void OnMouseDown(IMouse mouse, MouseButton button)
	{
		ImGuiApp.OnUserInput();
		OnMouseButton(mouse, button, down: true);
	}

	internal static void OnMouseUp(IMouse mouse, MouseButton button)
	{
		ImGuiApp.OnUserInput();
		OnMouseButton(mouse, button, down: false);
	}

	internal static void OnMouseButton(IMouse _, MouseButton button, bool down)
	{
		// Only process supported mouse buttons (Left, Right, Middle)
		if (button is MouseButton.Left or MouseButton.Right or MouseButton.Middle)
		{
			ImGuiMouseButton imguiMouseButton = TranslateMouseButtonToImGuiMouseButton(button);
			ImGuiIOPtr io = ImGui.GetIO();
			io.AddMouseButtonEvent((int)imguiMouseButton, down);
		}
		// Auxiliary buttons (Button4-Button12, Unknown) are ignored
	}

	internal static void OnMouseMove(IMouse _, Vector2 position)
	{
		ImGuiApp.OnUserInput();
		ImGuiIOPtr io = ImGui.GetIO();

		// The backend reports the cursor in window units; ImGui works in framebuffer pixels, which
		// differ on a scaled surface (Wayland fractional scaling, Retina).
		Vector2 scale = ImGuiApp.WindowToFramebufferScale;
		io.AddMousePosEvent(position.X * scale.X, position.Y * scale.Y);
	}

	/// <summary>
	/// Delegate to receive keyboard key events.
	/// </summary>
	/// <param name="_">The keyboard context generating the event.</param>
	/// <param name="keycode">The native keycode of the key generating the event.</param>
	/// <param name="scancode">The native scancode of the key generating the event.</param>
	/// <param name="down">True if the event is a key down event, otherwise False</param>
	internal static void OnKeyEvent(IKeyboard _, Key keycode, int scancode, bool down)
	{
		ImGuiIOPtr io = ImGui.GetIO();
		ImGuiKey imGuiKey = TranslateInputKeyToImGuiKey(keycode);
		io.AddKeyEvent(imGuiKey, down);
		io.SetKeyEventNativeData(imGuiKey, (int)keycode, scancode);

		ImGuiKey imguiModKey = TranslateImGuiKeyToImGuiModKey(imGuiKey);
		if (imguiModKey != ImGuiKey.None)
		{
			io.AddKeyEvent(imguiModKey, down);
		}
	}

	internal void OnKeyChar(IKeyboard arg1, char arg2)
	{
		ImGuiApp.OnUserInput();
		_pressedChars.Add(arg2);
	}

	internal void WindowResized(Vector2D<int> size)
	{
		_windowWidth = size.X;
		_windowHeight = size.Y;
	}

	internal void Render()
	{
		if (_frameBegun)
		{
			_frameBegun = false;
			ImDrawDataPtr drawData = _host!.EndFrame();
			ApplyMouseCursor();
			ApplyIme();
			_renderer!.RenderDrawData(drawData);
		}
	}

	/// <summary>
	/// Carries the frame's input method request (a focused text field, or <see cref="ImeCaret.Set"/>)
	/// to the window, so a candidate list opens at the caret.
	/// </summary>
	private void ApplyIme()
	{
		nint windowHandle = OperatingSystem.IsWindows() && _view?.Native?.Win32 is { } win32 ? win32.Hwnd : 0;
		_platformIme.Apply(windowHandle, ImeCaret.Requested, ImGui.GetMainViewport().Pos, ImGuiApp.WindowToFramebufferScale);
	}

	/// <summary>
	/// Carries the cursor the frame just decided on to the window's pointer.
	/// </summary>
	private void ApplyMouseCursor()
	{
		if (_mouse is null)
		{
			return;
		}

		ImGuiIOPtr io = ImGui.GetIO();
		_platformCursor.Apply(
			_mouse.Cursor,
			ImGui.GetMouseCursor(),
			io.MouseDrawCursor,
			(io.ConfigFlags & ImGuiConfigFlags.NoMouseCursorChange) != 0);
	}

	/// <summary>
	/// Updates ImGui input and IO configuration state.
	/// </summary>
	internal void Update(float deltaSeconds)
	{
		if (_frameBegun)
		{
			ImGui.Render();
		}

		UpdateImGuiInput();

		_frameBegun = true;
		_host?.NewFrame(DisplaySize(), Vector2.One, deltaSeconds);
	}

	/// <summary>
	/// The size ImGui lays the frame out in, which is the window's framebuffer.
	/// </summary>
	/// <remarks>
	/// ImGui's coordinate space is physical framebuffer pixels, so the display size is the
	/// framebuffer and the framebuffer scale is always 1. On a platform that reports the window
	/// in logical units — a scaled Wayland surface, a Retina display — the two differ, and
	/// ImGuiApp.ScaleFactor carries that ratio so the UI is laid out at the right physical size
	/// with glyphs rasterized to match. Anything arriving in logical units (mouse position) is
	/// converted on the way in; see ImGuiApp.WindowToFramebufferScale.
	/// </remarks>
	internal Vector2 DisplaySize()
	{
		Vector2D<int> framebufferSize = _view?.FramebufferSize ?? new Vector2D<int>(_windowWidth, _windowHeight);
		return framebufferSize.X > 0 && framebufferSize.Y > 0
			? new Vector2(framebufferSize.X, framebufferSize.Y)
			: new Vector2(_windowWidth, _windowHeight);
	}

	internal void UpdateImGuiInput()
	{
		ImGuiIOPtr io = ImGui.GetIO();

		if (_input is null || _keyboard is null)
		{
			return;
		}

		foreach (char c in _pressedChars)
		{
			io.AddInputCharacter(c);
		}

		_pressedChars.Clear();
	}

	internal void PressChar(char keyChar) => _pressedChars.Add(keyChar);

	/// <summary>
	/// Translates a Silk.NET.Input.Key to an ImGuiKey.
	/// </summary>
	/// <param name="key">The Silk.NET.Input.Key to translate.</param>
	/// <returns>The corresponding ImGuiKey.</returns>
	/// <exception cref="NotImplementedException">When the key has not been implemented yet.</exception>
	internal static ImGuiKey TranslateInputKeyToImGuiKey(Key key)
	{
		return key switch
		{
			Key.Tab => ImGuiKey.Tab,
			Key.Left => ImGuiKey.LeftArrow,
			Key.Right => ImGuiKey.RightArrow,
			Key.Up => ImGuiKey.UpArrow,
			Key.Down => ImGuiKey.DownArrow,
			Key.PageUp => ImGuiKey.PageUp,
			Key.PageDown => ImGuiKey.PageDown,
			Key.Home => ImGuiKey.Home,
			Key.End => ImGuiKey.End,
			Key.Insert => ImGuiKey.Insert,
			Key.Delete => ImGuiKey.Delete,
			Key.Backspace => ImGuiKey.Backspace,
			Key.Space => ImGuiKey.Space,
			Key.Enter => ImGuiKey.Enter,
			Key.Escape => ImGuiKey.Escape,
			Key.Apostrophe => ImGuiKey.Apostrophe,
			Key.Comma => ImGuiKey.Comma,
			Key.Minus => ImGuiKey.Minus,
			Key.Period => ImGuiKey.Period,
			Key.Slash => ImGuiKey.Slash,
			Key.Semicolon => ImGuiKey.Semicolon,
			Key.Equal => ImGuiKey.Equal,
			Key.LeftBracket => ImGuiKey.LeftBracket,
			Key.BackSlash => ImGuiKey.Backslash,
			Key.RightBracket => ImGuiKey.RightBracket,
			Key.GraveAccent => ImGuiKey.GraveAccent,
			Key.CapsLock => ImGuiKey.CapsLock,
			Key.ScrollLock => ImGuiKey.ScrollLock,
			Key.NumLock => ImGuiKey.NumLock,
			Key.PrintScreen => ImGuiKey.PrintScreen,
			Key.Pause => ImGuiKey.Pause,
			Key.Keypad0 => ImGuiKey.Keypad0,
			Key.Keypad1 => ImGuiKey.Keypad1,
			Key.Keypad2 => ImGuiKey.Keypad2,
			Key.Keypad3 => ImGuiKey.Keypad3,
			Key.Keypad4 => ImGuiKey.Keypad4,
			Key.Keypad5 => ImGuiKey.Keypad5,
			Key.Keypad6 => ImGuiKey.Keypad6,
			Key.Keypad7 => ImGuiKey.Keypad7,
			Key.Keypad8 => ImGuiKey.Keypad8,
			Key.Keypad9 => ImGuiKey.Keypad9,
			Key.KeypadDecimal => ImGuiKey.KeypadDecimal,
			Key.KeypadDivide => ImGuiKey.KeypadDivide,
			Key.KeypadMultiply => ImGuiKey.KeypadMultiply,
			Key.KeypadSubtract => ImGuiKey.KeypadSubtract,
			Key.KeypadAdd => ImGuiKey.KeypadAdd,
			Key.KeypadEnter => ImGuiKey.KeypadEnter,
			Key.KeypadEqual => ImGuiKey.KeypadEqual,
			Key.ShiftLeft => ImGuiKey.LeftShift,
			Key.ControlLeft => ImGuiKey.LeftCtrl,
			Key.AltLeft => ImGuiKey.LeftAlt,
			Key.SuperLeft => ImGuiKey.LeftSuper,
			Key.ShiftRight => ImGuiKey.RightShift,
			Key.ControlRight => ImGuiKey.RightCtrl,
			Key.AltRight => ImGuiKey.RightAlt,
			Key.SuperRight => ImGuiKey.RightSuper,
			Key.Menu => ImGuiKey.Menu,
			Key.Number0 => ImGuiKey.Key0,
			Key.Number1 => ImGuiKey.Key1,
			Key.Number2 => ImGuiKey.Key2,
			Key.Number3 => ImGuiKey.Key3,
			Key.Number4 => ImGuiKey.Key4,
			Key.Number5 => ImGuiKey.Key5,
			Key.Number6 => ImGuiKey.Key6,
			Key.Number7 => ImGuiKey.Key7,
			Key.Number8 => ImGuiKey.Key8,
			Key.Number9 => ImGuiKey.Key9,
			Key.A => ImGuiKey.A,
			Key.B => ImGuiKey.B,
			Key.C => ImGuiKey.C,
			Key.D => ImGuiKey.D,
			Key.E => ImGuiKey.E,
			Key.F => ImGuiKey.F,
			Key.G => ImGuiKey.G,
			Key.H => ImGuiKey.H,
			Key.I => ImGuiKey.I,
			Key.J => ImGuiKey.J,
			Key.K => ImGuiKey.K,
			Key.L => ImGuiKey.L,
			Key.M => ImGuiKey.M,
			Key.N => ImGuiKey.N,
			Key.O => ImGuiKey.O,
			Key.P => ImGuiKey.P,
			Key.Q => ImGuiKey.Q,
			Key.R => ImGuiKey.R,
			Key.S => ImGuiKey.S,
			Key.T => ImGuiKey.T,
			Key.U => ImGuiKey.U,
			Key.V => ImGuiKey.V,
			Key.W => ImGuiKey.W,
			Key.X => ImGuiKey.X,
			Key.Y => ImGuiKey.Y,
			Key.Z => ImGuiKey.Z,
			Key.F1 => ImGuiKey.F1,
			Key.F2 => ImGuiKey.F2,
			Key.F3 => ImGuiKey.F3,
			Key.F4 => ImGuiKey.F4,
			Key.F5 => ImGuiKey.F5,
			Key.F6 => ImGuiKey.F6,
			Key.F7 => ImGuiKey.F7,
			Key.F8 => ImGuiKey.F8,
			Key.F9 => ImGuiKey.F9,
			Key.F10 => ImGuiKey.F10,
			Key.F11 => ImGuiKey.F11,
			Key.F12 => ImGuiKey.F12,
			Key.F13 => ImGuiKey.F13,
			Key.F14 => ImGuiKey.F14,
			Key.F15 => ImGuiKey.F15,
			Key.F16 => ImGuiKey.F16,
			Key.F17 => ImGuiKey.F17,
			Key.F18 => ImGuiKey.F18,
			Key.F19 => ImGuiKey.F19,
			Key.F20 => ImGuiKey.F20,
			Key.F21 => ImGuiKey.F21,
			Key.F22 => ImGuiKey.F22,
			Key.F23 => ImGuiKey.F23,
			Key.F24 => ImGuiKey.F24,
			Key.F25 => ImGuiKey.None,
			Key.Unknown => ImGuiKey.None,
			Key.World1 => ImGuiKey.None,
			Key.World2 => ImGuiKey.None,
			_ => ImGuiKey.None,
		};
	}

	internal static ImGuiMouseButton TranslateMouseButtonToImGuiMouseButton(MouseButton mouseButton)
	{
		return mouseButton switch
		{
			MouseButton.Left => ImGuiMouseButton.Left,
			MouseButton.Right => ImGuiMouseButton.Right,
			MouseButton.Middle => ImGuiMouseButton.Middle,
			_ => ImGuiMouseButton.Middle,
		};
	}

	/// <summary>
	/// Translate an ImGuiKey to the matching ImGuiKey.Mod*.
	/// </summary>
	/// <param name="key">The ImGuiKey to translate.</param>
	/// <returns>The matching ImGuiKey.Mod*.</returns>
	[SuppressMessage("Style", "IDE0072:Add missing cases", Justification = "We're only translating mod keys here.")]
	internal static ImGuiKey TranslateImGuiKeyToImGuiModKey(ImGuiKey key)
	{
		return key switch
		{
			ImGuiKey.LeftShift => ImGuiKey.ModShift,
			ImGuiKey.RightShift => ImGuiKey.ModShift,
			ImGuiKey.LeftCtrl => ImGuiKey.ModCtrl,
			ImGuiKey.RightCtrl => ImGuiKey.ModCtrl,
			ImGuiKey.LeftAlt => ImGuiKey.ModAlt,
			ImGuiKey.RightAlt => ImGuiKey.ModAlt,
			ImGuiKey.LeftSuper => ImGuiKey.ModSuper,
			ImGuiKey.RightSuper => ImGuiKey.ModSuper,
			_ => ImGuiKey.None
		};
	}

	/// <summary>
	/// Switches the OpenGL blend function for the remainder of the current draw pass. See
	/// <see cref="OpenGLRenderer.SetBlendMode"/>.
	/// </summary>
	/// <param name="mode">The blend mode to activate.</param>
	internal void SetBlendMode(ImGuiAppBlendMode mode) => _renderer?.SetBlendMode(mode);

	/// <summary>
	/// Unhooks the window's input and releases the context and the renderer.
	/// </summary>
	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}

		if (_view is not null && _keyboard is not null && _mouse is not null)
		{
			_view.Resize -= WindowResized;
			_keyboard.KeyDown -= OnKeyDown;
			_keyboard.KeyUp -= OnKeyUp;
			_keyboard.KeyChar -= OnKeyChar;
			_mouse.MouseDown -= OnMouseDown;
			_mouse.MouseUp -= OnMouseUp;
			_mouse.MouseMove -= OnMouseMove;
			_mouse.Scroll -= OnMouseScroll;
		}

		// The context first: it releases ImGui's textures through the renderer, which has to still
		// be alive to delete them.
		_host?.Dispose();
		_renderer?.Dispose();

		_disposed = true;
	}
}
