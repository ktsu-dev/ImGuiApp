// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGuiApp.ImGuiController;

using System;
using System.Collections.Generic;
using System.Numerics;

using Hexa.NET.ImGui;

using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

internal class ImGuiController : IDisposable
{

	private GL? _gl;
	private IView? _view;
	private IInputContext? _input;
	private bool _frameBegun;
	private readonly List<char> _pressedChars = [];
	private IKeyboard? _keyboard;
	private IMouse? _mouse;

	private int _attribLocationTex;
	private int _attribLocationProjMtx;
	private int _attribLocationVtxPos;
	private int _attribLocationVtxUV;
	private int _attribLocationVtxColor;
	private uint _vboHandle;
	private uint _elementsHandle;
	private uint _vertexArrayObject;

	private Texture? _fontTexture;
	private Shader? _shader;

	private int _windowWidth;
	private int _windowHeight;

	internal bool FontsConfigured { get; private set; }

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
	public ImGuiController(GL gl, IView view, IInputContext input, ImGuiFontConfig? imGuiFontConfig = null, Action? onConfigureIO = null)
	{
		DebugLogger.Log("ImGuiController: Starting initialization");
		Init(gl, view, input);
		DebugLogger.Log("ImGuiController: Init completed");

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

		io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;

		DebugLogger.Log("ImGuiController: Creating device resources");
		CreateDeviceResources();
		DebugLogger.Log("ImGuiController: Device resources created");

		SetPerFrameImGuiData(1f / 60f);

		DebugLogger.Log("ImGuiController: Beginning frame");
		BeginFrame();
		DebugLogger.Log("ImGuiController: Initialization completed");
	}

	private void Init(GL gl, IView view, IInputContext input)
	{
		_gl = gl;
		_view = view;
		_input = input;
		_windowWidth = view.Size.X;
		_windowHeight = view.Size.Y;

		ImGui.CreateContext();

		ImGui.StyleColorsDark();
	}

	private void BeginFrame()
	{
		ImGui.NewFrame();
		_frameBegun = true;
		_keyboard = _input?.Keyboards[0];
		_mouse = _input?.Mice[0];
		if (_view is not null)
		{
			_view.Resize += WindowResized;
		}

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
	private static void OnKeyDown(IKeyboard keyboard, Key keycode, int scancode)
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
	private static void OnKeyUp(IKeyboard keyboard, Key keycode, int scancode)
	{
		ImGuiApp.OnUserInput();
		OnKeyEvent(keyboard, keycode, scancode, down: false);
	}

	private static void OnMouseScroll(IMouse mouse, ScrollWheel scroll)
	{
		ImGuiApp.OnUserInput();
		ImGuiIOPtr io = ImGui.GetIO();
		io.AddMouseWheelEvent(scroll.X, scroll.Y);
	}

	private static void OnMouseDown(IMouse mouse, MouseButton button)
	{
		ImGuiApp.OnUserInput();
		OnMouseButton(mouse, button, down: true);
	}

	private static void OnMouseUp(IMouse mouse, MouseButton button)
	{
		ImGuiApp.OnUserInput();
		OnMouseButton(mouse, button, down: false);
	}

	private static void OnMouseButton(IMouse _, MouseButton button, bool down)
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

	private void OnMouseMove(IMouse _, Vector2 position)
	{
		ImGuiApp.OnUserInput();
		ImGuiIOPtr io = ImGui.GetIO();
		io.AddMousePosEvent(position.X, position.Y);
	}

	/// <summary>
	/// Delegate to receive keyboard key events.
	/// </summary>
	/// <param name="_">The keyboard context generating the event.</param>
	/// <param name="keycode">The native keycode of the key generating the event.</param>
	/// <param name="scancode">The native scancode of the key generating the event.</param>
	/// <param name="down">True if the event is a key down event, otherwise False</param>
	private static void OnKeyEvent(IKeyboard _, Key keycode, int scancode, bool down)
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

	private void OnKeyChar(IKeyboard arg1, char arg2)
	{
		ImGuiApp.OnUserInput();
		_pressedChars.Add(arg2);
	}

	private void WindowResized(Vector2D<int> size)
	{
		_windowWidth = size.X;
		_windowHeight = size.Y;
	}

	public void Render()
	{
		if (_frameBegun)
		{
			_frameBegun = false;
			ImGui.Render();
			RenderImDrawData(ImGui.GetDrawData());
		}
	}

	/// <summary>
	/// Updates ImGui input and IO configuration state.
	/// </summary>
	public void Update(float deltaSeconds)
	{
		if (_frameBegun)
		{
			ImGui.Render();
		}

		SetPerFrameImGuiData(deltaSeconds);
		UpdateImGuiInput();

		_frameBegun = true;
		ImGui.NewFrame();
	}

	/// <summary>
	/// Sets per-frame data based on the associated window.
	/// This is called by Update(float).
	/// </summary>
	private void SetPerFrameImGuiData(float deltaSeconds)
	{
		ImGuiIOPtr io = ImGui.GetIO();
		io.DisplaySize = new Vector2(_windowWidth, _windowHeight);

		if (_windowWidth > 0 && _windowHeight > 0 && _view is not null)
		{
			// Force framebuffer scale to 1.0 on Linux to prevent blurry text rendering
			// WSL and Linux often have framebuffer scaling issues that cause blur
			if (OperatingSystem.IsLinux())
			{
				io.DisplayFramebufferScale = Vector2.One;
			}
			else
			{
				io.DisplayFramebufferScale = new Vector2(_view.FramebufferSize.X / _windowWidth,
					_view.FramebufferSize.Y / _windowHeight);
			}
		}

		io.DeltaTime = deltaSeconds; // DeltaTime is in seconds.
	}

	private void UpdateImGuiInput()
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
	private static ImGuiKey TranslateInputKeyToImGuiKey(Key key)
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
			Key.Unknown => throw new NotImplementedException(),
			Key.World1 => throw new NotImplementedException(),
			Key.World2 => throw new NotImplementedException(),
			Key.F25 => throw new NotImplementedException(),
			_ => throw new NotImplementedException($"Key '{key}' hasn't been implemented in TranslateInputKeyToImGuiKey"),
		};
	}

	private static ImGuiMouseButton TranslateMouseButtonToImGuiMouseButton(MouseButton mouseButton)
	{
		return mouseButton switch
		{
			MouseButton.Left => ImGuiMouseButton.Left,
			MouseButton.Right => ImGuiMouseButton.Right,
			MouseButton.Middle => ImGuiMouseButton.Middle,
			_ => throw new NotImplementedException($"MouseButton {mouseButton} hasn't been implemented in TranslateMouseButtonToImGuiMouseButton")
		};
	}

	/// <summary>
	/// Translate an ImGuiKey to the matching ImGuiKey.Mod*.
	/// </summary>
	/// <param name="key">The ImGuiKey to translate.</param>
	/// <returns>The matching ImGuiKey.Mod*.</returns>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0072:Add missing cases", Justification = "<Pending>")]
	private static ImGuiKey TranslateImGuiKeyToImGuiModKey(ImGuiKey key)
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

	private unsafe void SetupRenderState(ImDrawDataPtr drawDataPtr, int framebufferWidth, int framebufferHeight)
	{
		if (_gl is null || _shader is null)
		{
			return;
		}

		// Setup render state: alpha-blending enabled, no face culling, no depth testing, scissor enabled, polygon fill
		_gl.Enable(GLEnum.Blend);
		_gl.BlendEquation(GLEnum.FuncAdd);
		_gl.BlendFuncSeparate(GLEnum.SrcAlpha, GLEnum.OneMinusSrcAlpha, GLEnum.One, GLEnum.OneMinusSrcAlpha);
		_gl.Disable(GLEnum.CullFace);
		_gl.Disable(GLEnum.DepthTest);
		_gl.Disable(GLEnum.StencilTest);
		_gl.Enable(GLEnum.ScissorTest);
#if !GLES && !LEGACY
		_gl.Disable(GLEnum.PrimitiveRestart);
		_gl.PolygonMode(GLEnum.FrontAndBack, GLEnum.Fill);
		// Disable multisampling for pixel-perfect rendering
		_gl.Disable(GLEnum.Multisample);
#endif

		float L = drawDataPtr.DisplayPos.X;
		float R = drawDataPtr.DisplayPos.X + drawDataPtr.DisplaySize.X;
		float T = drawDataPtr.DisplayPos.Y;
		float B = drawDataPtr.DisplayPos.Y + drawDataPtr.DisplaySize.Y;

		// Use double precision for more accurate orthographic projection
		double dL = L, dR = R, dT = T, dB = B;

		Span<float> orthoProjection = [
				(float)(2.0 / (dR - dL)), 0.0f, 0.0f, 0.0f,
				0.0f, (float)(2.0 / (dT - dB)), 0.0f, 0.0f,
				0.0f, 0.0f, -1.0f, 0.0f,
				(float)((dR + dL) / (dL - dR)), (float)((dT + dB) / (dB - dT)), 0.0f, 1.0f,
			];

		_shader.UseShader();
		_gl.Uniform1(_attribLocationTex, 0);
		_gl.UniformMatrix4(_attribLocationProjMtx, 1, false, orthoProjection);
		_gl.CheckGlError("Projection");

		_gl.BindSampler(0, 0);

		// Setup desired GL state
		// Recreate the VAO every time (this is to easily allow multiple GL contexts to be rendered to. VAO are not shared among GL contexts)
		// The renderer would actually work without any VAO bound, but then our VertexAttrib calls would overwrite the default one currently bound.
		_vertexArrayObject = _gl.GenVertexArray();
		_gl.BindVertexArray(_vertexArrayObject);
		_gl.CheckGlError("VAO");

		// Bind vertex/index buffers and setup attributes for ImDrawVert
		_gl.BindBuffer(GLEnum.ArrayBuffer, _vboHandle);
		_gl.BindBuffer(GLEnum.ElementArrayBuffer, _elementsHandle);
		_gl.EnableVertexAttribArray((uint)_attribLocationVtxPos);
		_gl.EnableVertexAttribArray((uint)_attribLocationVtxUV);
		_gl.EnableVertexAttribArray((uint)_attribLocationVtxColor);
		_gl.VertexAttribPointer((uint)_attribLocationVtxPos, 2, GLEnum.Float, false, (uint)sizeof(ImDrawVert), (void*)0);
		_gl.VertexAttribPointer((uint)_attribLocationVtxUV, 2, GLEnum.Float, false, (uint)sizeof(ImDrawVert), (void*)8);
		_gl.VertexAttribPointer((uint)_attribLocationVtxColor, 4, GLEnum.UnsignedByte, true, (uint)sizeof(ImDrawVert), (void*)16);
	}

	private unsafe void RenderImDrawData(ImDrawDataPtr drawDataPtr)
	{
		if (_gl is null)
		{
			return;
		}

		int framebufferWidth = (int)(drawDataPtr.DisplaySize.X * drawDataPtr.FramebufferScale.X);
		int framebufferHeight = (int)(drawDataPtr.DisplaySize.Y * drawDataPtr.FramebufferScale.Y);
		if (framebufferWidth <= 0 || framebufferHeight <= 0)
		{
			return;
		}

		// Backup GL state
		_gl.GetInteger(GLEnum.ActiveTexture, out int lastActiveTexture);
		_gl.ActiveTexture(GLEnum.Texture0);

		_gl.GetInteger(GLEnum.CurrentProgram, out int lastProgram);
		_gl.GetInteger(GLEnum.TextureBinding2D, out int lastTexture);

		_gl.GetInteger(GLEnum.SamplerBinding, out int lastSampler);

		_gl.GetInteger(GLEnum.ArrayBufferBinding, out int lastArrayBuffer);
		_gl.GetInteger(GLEnum.VertexArrayBinding, out int lastVertexArrayObject);

#if !GLES
		Span<int> lastPolygonMode = stackalloc int[2];
		_gl.GetInteger(GLEnum.PolygonMode, lastPolygonMode);
#endif

		Span<int> lastScissorBox = stackalloc int[4];
		_gl.GetInteger(GLEnum.ScissorBox, lastScissorBox);

		_gl.GetInteger(GLEnum.BlendSrcRgb, out int lastBlendSrcRgb);
		_gl.GetInteger(GLEnum.BlendDstRgb, out int lastBlendDstRgb);

		_gl.GetInteger(GLEnum.BlendSrcAlpha, out int lastBlendSrcAlpha);
		_gl.GetInteger(GLEnum.BlendDstAlpha, out int lastBlendDstAlpha);

		_gl.GetInteger(GLEnum.BlendEquationRgb, out int lastBlendEquationRgb);
		_gl.GetInteger(GLEnum.BlendEquationAlpha, out int lastBlendEquationAlpha);

		bool lastEnableBlend = _gl.IsEnabled(GLEnum.Blend);
		bool lastEnableCullFace = _gl.IsEnabled(GLEnum.CullFace);
		bool lastEnableDepthTest = _gl.IsEnabled(GLEnum.DepthTest);
		bool lastEnableStencilTest = _gl.IsEnabled(GLEnum.StencilTest);
		bool lastEnableScissorTest = _gl.IsEnabled(GLEnum.ScissorTest);

#if !GLES && !LEGACY
		bool lastEnablePrimitiveRestart = _gl.IsEnabled(GLEnum.PrimitiveRestart);
#endif

		SetupRenderState(drawDataPtr, framebufferWidth, framebufferHeight);

		// Will project scissor/clipping rectangles into framebuffer space
		Vector2 clipOff = drawDataPtr.DisplayPos;         // (0,0) unless using multi-viewports
		Vector2 clipScale = drawDataPtr.FramebufferScale; // (1,1) unless using retina display which are often (2,2)

		// Render command lists
		for (int n = 0; n < drawDataPtr.CmdListsCount; n++)
		{
			ImDrawListPtr cmdListPtr = drawDataPtr.CmdLists[n];

			// Upload vertex/index buffers

			_gl.BufferData(GLEnum.ArrayBuffer, (nuint)(cmdListPtr.VtxBuffer.Size * sizeof(ImDrawVert)), cmdListPtr.VtxBuffer.Data, GLEnum.StreamDraw);
			_gl.CheckGlError($"Data Vert {n}");
			_gl.BufferData(GLEnum.ElementArrayBuffer, (nuint)(cmdListPtr.IdxBuffer.Size * sizeof(ushort)), cmdListPtr.IdxBuffer.Data, GLEnum.StreamDraw);
			_gl.CheckGlError($"Data Idx {n}");

			for (int cmd_i = 0; cmd_i < cmdListPtr.CmdBuffer.Size; cmd_i++)
			{
				ImDrawCmd cmdPtr = cmdListPtr.CmdBuffer[cmd_i];

				unsafe
				{
					if (cmdPtr.UserCallback != null)
					{
						throw new NotImplementedException();
					}
					else
					{
						Vector4 clipRect;
						clipRect.X = (cmdPtr.ClipRect.X - clipOff.X) * clipScale.X;
						clipRect.Y = (cmdPtr.ClipRect.Y - clipOff.Y) * clipScale.Y;
						clipRect.Z = (cmdPtr.ClipRect.Z - clipOff.X) * clipScale.X;
						clipRect.W = (cmdPtr.ClipRect.W - clipOff.Y) * clipScale.Y;

						if (clipRect.X < framebufferWidth && clipRect.Y < framebufferHeight && clipRect.Z >= 0.0f && clipRect.W >= 0.0f)
						{
							// Round scissor rectangle to pixel boundaries to avoid sub-pixel rendering issues
							int scissorX = (int)Math.Floor(clipRect.X);
							int scissorY = (int)Math.Floor(framebufferHeight - clipRect.W);
							uint scissorWidth = (uint)Math.Max(0, Math.Ceiling(clipRect.Z - clipRect.X));
							uint scissorHeight = (uint)Math.Max(0, Math.Ceiling(clipRect.W - clipRect.Y));

							// Apply scissor/clipping rectangle
							_gl.Scissor(scissorX, scissorY, scissorWidth, scissorHeight);
							_gl.CheckGlError("Scissor");

							// Bind texture, Draw
							// In ImGui 1.92.0+, use GetTexID() method to get texture ID
							// This method returns the texture ID compatible with OpenGL
							uint textureId = (uint)cmdPtr.GetTexID();
							_gl.BindTexture(GLEnum.Texture2D, textureId);
							_gl.CheckGlError("Texture");

							_gl.DrawElementsBaseVertex(GLEnum.Triangles, cmdPtr.ElemCount, GLEnum.UnsignedShort, (void*)(cmdPtr.IdxOffset * sizeof(ushort)), (int)cmdPtr.VtxOffset);
							_gl.CheckGlError("Draw");
						}
					}
				}
			}
		}

		// Destroy the temporary VAO
		_gl.DeleteVertexArray(_vertexArrayObject);
		_vertexArrayObject = 0;

		// Restore modified GL state
		_gl.UseProgram((uint)lastProgram);
		_gl.BindTexture(GLEnum.Texture2D, (uint)lastTexture);

		_gl.BindSampler(0, (uint)lastSampler);

		_gl.ActiveTexture((GLEnum)lastActiveTexture);

		_gl.BindVertexArray((uint)lastVertexArrayObject);

		_gl.BindBuffer(GLEnum.ArrayBuffer, (uint)lastArrayBuffer);
		_gl.BlendEquationSeparate((GLEnum)lastBlendEquationRgb, (GLEnum)lastBlendEquationAlpha);
		_gl.BlendFuncSeparate((GLEnum)lastBlendSrcRgb, (GLEnum)lastBlendDstRgb, (GLEnum)lastBlendSrcAlpha, (GLEnum)lastBlendDstAlpha);

		if (lastEnableBlend)
		{
			_gl.Enable(GLEnum.Blend);
		}
		else
		{
			_gl.Disable(GLEnum.Blend);
		}

		if (lastEnableCullFace)
		{
			_gl.Enable(GLEnum.CullFace);
		}
		else
		{
			_gl.Disable(GLEnum.CullFace);
		}

		if (lastEnableDepthTest)
		{
			_gl.Enable(GLEnum.DepthTest);
		}
		else
		{
			_gl.Disable(GLEnum.DepthTest);
		}

		if (lastEnableStencilTest)
		{
			_gl.Enable(GLEnum.StencilTest);
		}
		else
		{
			_gl.Disable(GLEnum.StencilTest);
		}

		if (lastEnableScissorTest)
		{
			_gl.Enable(GLEnum.ScissorTest);
		}
		else
		{
			_gl.Disable(GLEnum.ScissorTest);
		}

#if !GLES && !LEGACY
		if (lastEnablePrimitiveRestart)
		{
			_gl.Enable(GLEnum.PrimitiveRestart);
		}
		else
		{
			_gl.Disable(GLEnum.PrimitiveRestart);
		}

		_gl.PolygonMode(GLEnum.FrontAndBack, (GLEnum)lastPolygonMode[0]);
#endif

		_gl.Scissor(lastScissorBox[0], lastScissorBox[1], (uint)lastScissorBox[2], (uint)lastScissorBox[3]);
	}

	private void CreateDeviceResources()
	{
		if (_gl is null)
		{
			return;
		}

		// Backup GL state

		_gl.GetInteger(GLEnum.TextureBinding2D, out int lastTexture);
		_gl.GetInteger(GLEnum.ArrayBufferBinding, out int lastArrayBuffer);
		_gl.GetInteger(GLEnum.VertexArrayBinding, out int lastVertexArray);

		string vertexSource =
			@"#version 330
			layout (location = 0) in vec2 Position;
			layout (location = 1) in vec2 UV;
			layout (location = 2) in vec4 Color;
			uniform mat4 ProjMtx;
			out vec2 Frag_UV;
			out vec4 Frag_Color;
			void main()
			{
				Frag_UV = UV;
				Frag_Color = Color;
				// Round position to nearest pixel to improve line alignment
				vec2 roundedPos = floor(Position.xy + 0.5);
				gl_Position = ProjMtx * vec4(roundedPos, 0, 1);
			}";

		string fragmentSource =
			@"#version 330
			in vec2 Frag_UV;
			in vec4 Frag_Color;
			uniform sampler2D Texture;
			layout (location = 0) out vec4 Out_Color;
			void main()
			{
				// Use precise texture sampling to avoid floating point errors
				vec2 texelSize = 1.0 / textureSize(Texture, 0);
				vec2 adjustedUV = clamp(Frag_UV, texelSize * 0.5, 1.0 - texelSize * 0.5);
				Out_Color = Frag_Color * texture(Texture, adjustedUV);
			}";

		_shader = new Shader(_gl, vertexSource, fragmentSource);

		_attribLocationTex = _shader.GetUniformLocation("Texture");
		_attribLocationProjMtx = _shader.GetUniformLocation("ProjMtx");
		_attribLocationVtxPos = _shader.GetAttribLocation("Position");
		_attribLocationVtxUV = _shader.GetAttribLocation("UV");
		_attribLocationVtxColor = _shader.GetAttribLocation("Color");

		_vboHandle = _gl.GenBuffer();
		_elementsHandle = _gl.GenBuffer();

		RecreateFontDeviceTexture();

		// Restore modified GL state
		_gl.BindTexture(GLEnum.Texture2D, (uint)lastTexture);
		_gl.BindBuffer(GLEnum.ArrayBuffer, (uint)lastArrayBuffer);

		_gl.BindVertexArray((uint)lastVertexArray);

		_gl.CheckGlError("End of ImGui setup");
	}

	/// <summary>
	/// Creates the texture used to render text.
	/// </summary>
	private unsafe void RecreateFontDeviceTexture()
	{
		DebugLogger.Log("RecreateFontDeviceTexture: Starting");
		if (_gl is null)
		{
			DebugLogger.Log("RecreateFontDeviceTexture: OpenGL is null, returning");
			return;
		}

		// Build texture atlas
		ImGuiIOPtr io = ImGui.GetIO();
		DebugLogger.Log("RecreateFontDeviceTexture: Got ImGui IO");
		unsafe
		{
			// Build font atlas if it's not already built
			if (!io.Fonts.TexIsBuilt)
			{
				ImGuiApp.DebugLogger.Log("RecreateFontDeviceTexture: Font atlas not built yet, building now");

				// Build the font atlas using ImFontAtlasBuildMain
				// This is required when the backend doesn't support ImGuiBackendFlags_RendererHasTextures
				ImGuiP.ImFontAtlasBuildMain(io.Fonts);

				ImGuiApp.DebugLogger.Log("RecreateFontDeviceTexture: Font atlas built successfully");
			}
			else
			{
				ImGuiApp.DebugLogger.Log("RecreateFontDeviceTexture: Font atlas already built");
			}

			// Get texture data using the correct API for Hexa.NET.ImGui 2.2.8
			ImTextureDataPtr texData = io.Fonts.TexData;
			DebugLogger.Log($"RecreateFontDeviceTexture: Got texture data - Width: {texData.Width}, Height: {texData.Height}");

			// Only proceed if we have valid texture data
			if (texData.Pixels != null && texData.Width > 0 && texData.Height > 0)
			{
				DebugLogger.Log("RecreateFontDeviceTexture: Texture data is valid, creating OpenGL texture");

				// Create OpenGL texture from font atlas data
				_gl.GetInteger(GLEnum.TextureBinding2D, out int lastTexture);
				DebugLogger.Log("RecreateFontDeviceTexture: Got last texture binding");

				// Create texture with the font atlas data
				DebugLogger.Log("RecreateFontDeviceTexture: Creating Texture object");
				_fontTexture = new Texture(_gl, texData.Width, texData.Height,
					(nint)texData.Pixels, false, false, PixelFormat.Rgba);
				DebugLogger.Log("RecreateFontDeviceTexture: Texture object created");

				// Store texture ID in ImGui's font atlas
				DebugLogger.Log("RecreateFontDeviceTexture: Setting texture ID");
				texData.SetTexID((nint)_fontTexture.GlTexture);
				DebugLogger.Log("RecreateFontDeviceTexture: Texture ID set");

				// Set texture filtering
				DebugLogger.Log("RecreateFontDeviceTexture: Setting texture filtering");
				_fontTexture.Bind();
				_fontTexture.SetMagFilter(TextureMagFilter.Nearest);
				_fontTexture.SetMinFilter(TextureMinFilter.Nearest);
				_fontTexture.SetWrap(TextureCoordinate.S, TextureWrapMode.ClampToEdge);
				_fontTexture.SetWrap(TextureCoordinate.T, TextureWrapMode.ClampToEdge);
				DebugLogger.Log("RecreateFontDeviceTexture: Texture filtering set");

				// Restore previous texture binding
				_gl.BindTexture(GLEnum.Texture2D, (uint)lastTexture);
				DebugLogger.Log("RecreateFontDeviceTexture: Restored previous texture binding");

				// Clear font atlas texture data to save memory
				io.Fonts.ClearTexData();
				DebugLogger.Log("RecreateFontDeviceTexture: Cleared font atlas texture data");

				// Mark fonts as configured
				FontsConfigured = true;
				DebugLogger.Log("RecreateFontDeviceTexture: Marked fonts as configured");
			}
			else
			{
				DebugLogger.Log("RecreateFontDeviceTexture: Invalid texture data - skipping");
			}
		}
		DebugLogger.Log("RecreateFontDeviceTexture: Completed");
	}

	/// <summary>
	/// Frees all graphics resources used by the renderer.
	/// </summary>
	public void Dispose()
	{
		if (_gl is null || _view is null || _keyboard is null || _mouse is null || _fontTexture is null || _shader is null)
		{
			return;
		}

		_view.Resize -= WindowResized;
		_keyboard.KeyDown -= OnKeyDown;
		_keyboard.KeyUp -= OnKeyUp;
		_keyboard.KeyChar -= OnKeyChar;
		_mouse.MouseDown -= OnMouseDown;
		_mouse.MouseUp -= OnMouseUp;
		_mouse.MouseMove -= OnMouseMove;
		_mouse.Scroll -= OnMouseScroll;

		_gl.DeleteBuffer(_vboHandle);
		_gl.DeleteBuffer(_elementsHandle);
		_gl.DeleteVertexArray(_vertexArrayObject);

		_fontTexture.Dispose();
		_shader.Dispose();

		ImGui.DestroyContext();
	}
}
