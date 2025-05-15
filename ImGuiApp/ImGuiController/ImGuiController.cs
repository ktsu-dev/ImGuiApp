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
		Init(gl, view, input);

		var io = ImGui.GetIO();
		if (imGuiFontConfig is not null)
		{
			var glyphRange = imGuiFontConfig.Value.GetGlyphRange?.Invoke(io) ?? default;

			io.Fonts.AddFontFromFileTTF(imGuiFontConfig.Value.FontPath, imGuiFontConfig.Value.FontSize, null, (uint*)glyphRange);
		}

		onConfigureIO?.Invoke();

		io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;

		CreateDeviceResources();

		SetPerFrameImGuiData(1f / 60f);

		BeginFrame();
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
	private static void OnKeyDown(IKeyboard keyboard, Key keycode, int scancode) =>
		OnKeyEvent(keyboard, keycode, scancode, down: true);

	/// <summary>
	/// Delegate to receive keyboard key up events.
	/// </summary>
	/// <param name="keyboard">The keyboard context generating the event.</param>
	/// <param name="keycode">The native keycode of the released key.</param>
	/// <param name="scancode">The native scancode of the released key.</param>
	private static void OnKeyUp(IKeyboard keyboard, Key keycode, int scancode) =>
		OnKeyEvent(keyboard, keycode, scancode, down: false);

	private static void OnMouseScroll(IMouse mouse, ScrollWheel scroll)
	{
		var io = ImGui.GetIO();
		io.AddMouseWheelEvent(scroll.X, scroll.Y);
	}

	private static void OnMouseDown(IMouse mouse, MouseButton button) => OnMouseButton(mouse, button, down: true);

	private static void OnMouseUp(IMouse mouse, MouseButton button) => OnMouseButton(mouse, button, down: false);

	private static void OnMouseButton(IMouse _, MouseButton button, bool down)
	{
		var imguiMouseButton = TranslateMouseButtonToImGuiMouseButton(button);
		if (imguiMouseButton != ImGuiMouseButton.COUNT)
		{
			var io = ImGui.GetIO();
			io.AddMouseButtonEvent((int)imguiMouseButton, down);
		}
	}

	private void OnMouseMove(IMouse _, Vector2 position)
	{
		var io = ImGui.GetIO();
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
		var io = ImGui.GetIO();
		var imGuiKey = TranslateInputKeyToImGuiKey(keycode);
		io.AddKeyEvent(imGuiKey, down);
		io.SetKeyEventNativeData(imGuiKey, (int)keycode, scancode);

		var imguiModKey = TranslateImGuiKeyToImGuiModKey(imGuiKey);
		if (imguiModKey != ImGuiKey.NamedKey_END)
		{
			io.AddKeyEvent(imguiModKey, down);
		}
	}

	private void OnKeyChar(IKeyboard arg1, char arg2) => _pressedChars.Add(arg2);

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
		var io = ImGui.GetIO();
		io.DisplaySize = new Vector2(_windowWidth, _windowHeight);

		if (_windowWidth > 0 && _windowHeight > 0 && _view is not null)
		{
			io.DisplayFramebufferScale = new Vector2(_view.FramebufferSize.X / _windowWidth,
				_view.FramebufferSize.Y / _windowHeight);
		}

		io.DeltaTime = deltaSeconds; // DeltaTime is in seconds.
	}

	private void UpdateImGuiInput()
	{
		var io = ImGui.GetIO();

		if (_input is null || _keyboard is null)
		{
			return;
		}

		foreach (var c in _pressedChars)
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
			Key.Number0 => ImGuiKey._0,
			Key.Number1 => ImGuiKey._1,
			Key.Number2 => ImGuiKey._2,
			Key.Number3 => ImGuiKey._3,
			Key.Number4 => ImGuiKey._4,
			Key.Number5 => ImGuiKey._5,
			Key.Number6 => ImGuiKey._6,
			Key.Number7 => ImGuiKey._7,
			Key.Number8 => ImGuiKey._8,
			Key.Number9 => ImGuiKey._9,
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
			MouseButton.Button4 => ImGuiMouseButton.COUNT,
			MouseButton.Button5 => ImGuiMouseButton.COUNT,
			MouseButton.Button6 => ImGuiMouseButton.COUNT,
			MouseButton.Button7 => ImGuiMouseButton.COUNT,
			MouseButton.Button8 => ImGuiMouseButton.COUNT,
			MouseButton.Button9 => ImGuiMouseButton.COUNT,
			MouseButton.Button10 => ImGuiMouseButton.COUNT,
			MouseButton.Button11 => ImGuiMouseButton.COUNT,
			MouseButton.Button12 => ImGuiMouseButton.COUNT,
			MouseButton.Unknown => ImGuiMouseButton.COUNT,
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
			_ => ImGuiKey.NamedKey_END
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
#endif

		var L = drawDataPtr.DisplayPos.X;
		var R = drawDataPtr.DisplayPos.X + drawDataPtr.DisplaySize.X;
		var T = drawDataPtr.DisplayPos.Y;
		var B = drawDataPtr.DisplayPos.Y + drawDataPtr.DisplaySize.Y;

		Span<float> orthoProjection = [
				2.0f / (R - L), 0.0f, 0.0f, 0.0f,
				0.0f, 2.0f / (T - B), 0.0f, 0.0f,
				0.0f, 0.0f, -1.0f, 0.0f,
				(R + L) / (L - R), (T + B) / (B - T), 0.0f, 1.0f,
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

		var framebufferWidth = (int)(drawDataPtr.DisplaySize.X * drawDataPtr.FramebufferScale.X);
		var framebufferHeight = (int)(drawDataPtr.DisplaySize.Y * drawDataPtr.FramebufferScale.Y);
		if (framebufferWidth <= 0 || framebufferHeight <= 0)
		{
			return;
		}

		// Backup GL state
		_gl.GetInteger(GLEnum.ActiveTexture, out var lastActiveTexture);
		_gl.ActiveTexture(GLEnum.Texture0);

		_gl.GetInteger(GLEnum.CurrentProgram, out var lastProgram);
		_gl.GetInteger(GLEnum.TextureBinding2D, out var lastTexture);

		_gl.GetInteger(GLEnum.SamplerBinding, out var lastSampler);

		_gl.GetInteger(GLEnum.ArrayBufferBinding, out var lastArrayBuffer);
		_gl.GetInteger(GLEnum.VertexArrayBinding, out var lastVertexArrayObject);

#if !GLES
		Span<int> lastPolygonMode = stackalloc int[2];
		_gl.GetInteger(GLEnum.PolygonMode, lastPolygonMode);
#endif

		Span<int> lastScissorBox = stackalloc int[4];
		_gl.GetInteger(GLEnum.ScissorBox, lastScissorBox);

		_gl.GetInteger(GLEnum.BlendSrcRgb, out var lastBlendSrcRgb);
		_gl.GetInteger(GLEnum.BlendDstRgb, out var lastBlendDstRgb);

		_gl.GetInteger(GLEnum.BlendSrcAlpha, out var lastBlendSrcAlpha);
		_gl.GetInteger(GLEnum.BlendDstAlpha, out var lastBlendDstAlpha);

		_gl.GetInteger(GLEnum.BlendEquationRgb, out var lastBlendEquationRgb);
		_gl.GetInteger(GLEnum.BlendEquationAlpha, out var lastBlendEquationAlpha);

		var lastEnableBlend = _gl.IsEnabled(GLEnum.Blend);
		var lastEnableCullFace = _gl.IsEnabled(GLEnum.CullFace);
		var lastEnableDepthTest = _gl.IsEnabled(GLEnum.DepthTest);
		var lastEnableStencilTest = _gl.IsEnabled(GLEnum.StencilTest);
		var lastEnableScissorTest = _gl.IsEnabled(GLEnum.ScissorTest);

#if !GLES && !LEGACY
		var lastEnablePrimitiveRestart = _gl.IsEnabled(GLEnum.PrimitiveRestart);
#endif

		SetupRenderState(drawDataPtr, framebufferWidth, framebufferHeight);

		// Will project scissor/clipping rectangles into framebuffer space
		var clipOff = drawDataPtr.DisplayPos;         // (0,0) unless using multi-viewports
		var clipScale = drawDataPtr.FramebufferScale; // (1,1) unless using retina display which are often (2,2)

		// Render command lists
		for (var n = 0; n < drawDataPtr.CmdListsCount; n++)
		{
			var cmdListPtr = drawDataPtr.CmdLists[n];

			// Upload vertex/index buffers

			_gl.BufferData(GLEnum.ArrayBuffer, (nuint)(cmdListPtr.VtxBuffer.Size * sizeof(ImDrawVert)), cmdListPtr.VtxBuffer.Data, GLEnum.StreamDraw);
			_gl.CheckGlError($"Data Vert {n}");
			_gl.BufferData(GLEnum.ElementArrayBuffer, (nuint)(cmdListPtr.IdxBuffer.Size * sizeof(ushort)), cmdListPtr.IdxBuffer.Data, GLEnum.StreamDraw);
			_gl.CheckGlError($"Data Idx {n}");

			for (var cmd_i = 0; cmd_i < cmdListPtr.CmdBuffer.Size; cmd_i++)
			{
				var cmdPtr = cmdListPtr.CmdBuffer[cmd_i];

				if (cmdPtr.UserCallback != nint.Zero)
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
						// Apply scissor/clipping rectangle
						_gl.Scissor((int)clipRect.X, (int)(framebufferHeight - clipRect.W), (uint)(clipRect.Z - clipRect.X), (uint)(clipRect.W - clipRect.Y));
						_gl.CheckGlError("Scissor");

						// Bind texture, Draw
						_gl.BindTexture(GLEnum.Texture2D, (uint)cmdPtr.TextureId);
						_gl.CheckGlError("Texture");

						_gl.DrawElementsBaseVertex(GLEnum.Triangles, cmdPtr.ElemCount, GLEnum.UnsignedShort, (void*)(cmdPtr.IdxOffset * sizeof(ushort)), (int)cmdPtr.VtxOffset);
						_gl.CheckGlError("Draw");
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

		_gl.GetInteger(GLEnum.TextureBinding2D, out var lastTexture);
		_gl.GetInteger(GLEnum.ArrayBufferBinding, out var lastArrayBuffer);
		_gl.GetInteger(GLEnum.VertexArrayBinding, out var lastVertexArray);

		var vertexSource =
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
				gl_Position = ProjMtx * vec4(Position.xy,0,1);
			}";

		var fragmentSource =
			@"#version 330
			in vec2 Frag_UV;
			in vec4 Frag_Color;
			uniform sampler2D Texture;
			layout (location = 0) out vec4 Out_Color;
			void main()
			{
				Out_Color = Frag_Color * texture(Texture, Frag_UV.st);
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
		if (_gl is null)
		{
			return;
		}

		// Build texture atlas
		var io = ImGui.GetIO();
		io.Fonts.GetTexDataAsRGBA32(out nint pixels, out var width, out var height, out var _);   // Load as RGBA 32-bit (75% of the memory is wasted, but default font is so small) because it is more likely to be compatible with user's existing shaders. If your ImTextureId represent a higher-level concept than just a GL texture id, consider calling GetTexDataAsAlpha8() instead to save on GPU memory.

		// Upload texture to graphics system
		_gl.GetInteger(GLEnum.TextureBinding2D, out var lastTexture);

		_fontTexture = new Texture(_gl, width, height, pixels);
		_fontTexture.Bind();
		_fontTexture.SetMagFilter(TextureMagFilter.Linear);
		_fontTexture.SetMinFilter(TextureMinFilter.Linear);

		// Store our identifier
		io.Fonts.SetTexID((nint)_fontTexture.GlTexture);

		// Restore state
		_gl.BindTexture(GLEnum.Texture2D, (uint)lastTexture);

		io.Fonts.ClearTexData();

		FontsConfigured = true;
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
