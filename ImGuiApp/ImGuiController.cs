using ImGuiNET;
using System.Numerics;
using System.Runtime.CompilerServices;
using Veldrid;
using ResourceSet = Veldrid.ResourceSet;

namespace ktsu.io.ImGuiApp;

/// <summary>
/// A modified version of Veldrid.ImGui's ImGuiRenderer.
/// Manages input for ImGui and handles rendering ImGui's DrawLists with Veldrid.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
internal sealed class ImGuiController : IDisposable
{
	private GraphicsDevice _gd;
	private bool _frameBegun;

	// Veldrid objects
	private DeviceBuffer? _vertexBuffer;
	private DeviceBuffer? _indexBuffer;
	private DeviceBuffer? _projMatrixBuffer;
	private Texture? _fontTexture;
	private TextureView? _fontTextureView;
	private Shader? _vertexShader;
	private Shader? _fragmentShader;
	private ResourceLayout? _layout;
	private ResourceLayout? _textureLayout;
	private Pipeline? _pipeline;
	private ResourceSet? _mainResourceSet;
	private ResourceSet? _fontTextureResourceSet;

	private readonly IntPtr _fontAtlasID = 1;
	private bool _controlDown;
	private bool _shiftDown;
	private bool _altDown;
	private bool _winKeyDown;

	private int _windowWidth;
	private int _windowHeight;
	private Vector2 _scaleFactor = Vector2.One;

	// Image trackers
	private readonly Dictionary<TextureView, ResourceSetInfo> _setsByView = new();
	private readonly Dictionary<Texture, TextureView> _autoViewsByTexture = new();
	private readonly Dictionary<IntPtr, ResourceSetInfo> _viewsById = new();
	private readonly List<IDisposable> _ownedResources = new();
	private int _lastAssignedID = 100;

	/// <summary>
	/// Constructs a new ImGuiController.
	/// </summary>
	internal ImGuiController(GraphicsDevice gd, OutputDescription outputDescription, int width, int height)
	{
		_gd = gd;
		_windowWidth = width;
		_windowHeight = height;

		nint context = ImGui.CreateContext();
		ImGui.SetCurrentContext(context);

		var io = ImGui.GetIO();
		io.Fonts.AddFontDefault();
		io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;

		CreateDeviceResources(gd, outputDescription);
		SetKeyMappings();

		SetPerFrameImGuiData(1f / 60f);

		ImGui.NewFrame();
		_frameBegun = true;
	}

	internal void WindowResized(int width, int height)
	{
		_windowWidth = width;
		_windowHeight = height;
	}

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0002:Simplify Member Access", Justification = "<Pending>")]
	internal void CreateDeviceResources(GraphicsDevice gd, OutputDescription outputDescription)
	{
		_gd = gd;
		var factory = gd.ResourceFactory;
		_vertexBuffer = factory.CreateBuffer(new BufferDescription(10000, BufferUsage.VertexBuffer | BufferUsage.Dynamic));
		_vertexBuffer.Name = "ImGui.NET Vertex Buffer";
		_indexBuffer = factory.CreateBuffer(new BufferDescription(2000, BufferUsage.IndexBuffer | BufferUsage.Dynamic));
		_indexBuffer.Name = "ImGui.NET Index Buffer";
		RecreateFontDeviceTexture(gd);

		_projMatrixBuffer = factory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer | BufferUsage.Dynamic));
		_projMatrixBuffer.Name = "ImGui.NET Projection Buffer";

		string prefix = typeof(ImGuiApp).Assembly.GetName().Name!;
		byte[] vertexShaderBytes = LoadEmbeddedShaderCode(gd.ResourceFactory, $"{prefix}.Shaders.imgui-vertex", ShaderStages.Vertex);
		byte[] fragmentShaderBytes = LoadEmbeddedShaderCode(gd.ResourceFactory, $"{prefix}.Shaders.imgui-frag", ShaderStages.Fragment);
		_vertexShader = factory.CreateShader(new ShaderDescription(ShaderStages.Vertex, vertexShaderBytes, "VS"));
		_fragmentShader = factory.CreateShader(new ShaderDescription(ShaderStages.Fragment, fragmentShaderBytes, "FS"));

		var vertexLayouts = new VertexLayoutDescription[]
		{
			new(
				new VertexElementDescription("in_position", VertexElementSemantic.Position, VertexElementFormat.Float2),
				new VertexElementDescription("in_texCoord", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
				new VertexElementDescription("in_color", VertexElementSemantic.Color, VertexElementFormat.Byte4_Norm)
			)
		};

		_layout = factory.CreateResourceLayout(new ResourceLayoutDescription(
			new ResourceLayoutElementDescription("ProjectionMatrixBuffer", ResourceKind.UniformBuffer, ShaderStages.Vertex),
			new ResourceLayoutElementDescription("MainSampler", ResourceKind.Sampler, ShaderStages.Fragment)));
		_textureLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(
			new ResourceLayoutElementDescription("MainTexture", ResourceKind.TextureReadOnly, ShaderStages.Fragment)));

		var pd = new GraphicsPipelineDescription(
			BlendStateDescription.SingleAlphaBlend,
			new DepthStencilStateDescription(false, false, ComparisonKind.Always),
			new RasterizerStateDescription(FaceCullMode.None, PolygonFillMode.Solid, FrontFace.Clockwise, false, true),
			PrimitiveTopology.TriangleList,
			new ShaderSetDescription(vertexLayouts, new[] { _vertexShader, _fragmentShader }),
			new ResourceLayout[] { _layout, _textureLayout },
			outputDescription);
		_pipeline = factory.CreateGraphicsPipeline(ref pd);

		_mainResourceSet = factory.CreateResourceSet(new ResourceSetDescription(_layout,
			_projMatrixBuffer,
			gd.PointSampler));

		_fontTextureResourceSet = factory.CreateResourceSet(new ResourceSetDescription(_textureLayout, _fontTextureView));
	}

	/// <summary>
	/// Gets or creates a handle for a texture to be drawn with ImGui.
	/// Pass the returned handle to Image() or ImageButton().
	/// </summary>
	internal IntPtr GetOrCreateImGuiBinding(ResourceFactory factory, TextureView textureView)
	{
		if (!_setsByView.TryGetValue(textureView, out var rsi))
		{
			var resourceSet = factory.CreateResourceSet(new ResourceSetDescription(_textureLayout, textureView));
			rsi = new ResourceSetInfo(GetNextImGuiBindingID(), resourceSet);

			_setsByView.Add(textureView, rsi);
			_viewsById.Add(rsi.ImGuiBinding, rsi);
			_ownedResources.Add(resourceSet);
		}

		return rsi.ImGuiBinding;
	}

	private IntPtr GetNextImGuiBindingID()
	{
		int newID = _lastAssignedID++;
		return newID;
	}

	/// <summary>
	/// Gets or creates a handle for a texture to be drawn with ImGui.
	/// Pass the returned handle to Image() or ImageButton().
	/// </summary>
	internal IntPtr GetOrCreateImGuiBinding(ResourceFactory factory, Texture texture)
	{
		if (!_autoViewsByTexture.TryGetValue(texture, out var textureView))
		{
			textureView = factory.CreateTextureView(texture);
			_autoViewsByTexture.Add(texture, textureView);
			_ownedResources.Add(textureView);
		}

		return GetOrCreateImGuiBinding(factory, textureView);
	}

	/// <summary>
	/// Retrieves the shader texture binding for the given helper handle.
	/// </summary>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1305:Specify IFormatProvider", Justification = "<Pending>")]
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "<Pending>")]
	internal ResourceSet GetImageResourceSet(IntPtr imGuiBinding)
	{
		if (!_viewsById.TryGetValue(imGuiBinding, out var tvi))
		{
			throw new InvalidOperationException("No registered ImGui binding with id " + imGuiBinding.ToString());
		}

		return tvi.ResourceSet;
	}

	internal void ClearCachedImageResources()
	{
		foreach (var resource in _ownedResources)
		{
			resource.Dispose();
		}

		_ownedResources.Clear();
		_setsByView.Clear();
		_viewsById.Clear();
		_autoViewsByTexture.Clear();
		_lastAssignedID = 100;
	}

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0010:Add missing cases", Justification = "<Pending>")]
	private static byte[] LoadEmbeddedShaderCode(ResourceFactory factory, string name, ShaderStages _)
	{
		switch (factory.BackendType)
		{
			case GraphicsBackend.Direct3D11:
			{
				string resourceName = name + ".hlsl.bytes";
				return GetEmbeddedResourceBytes(resourceName);
			}
			case GraphicsBackend.OpenGL:
			{
				string resourceName = name + ".glsl";
				return GetEmbeddedResourceBytes(resourceName);
			}
			case GraphicsBackend.Vulkan:
			{
				string resourceName = name + ".spv";
				return GetEmbeddedResourceBytes(resourceName);
			}
			case GraphicsBackend.Metal:
			{
				string resourceName = name + ".metallib";
				return GetEmbeddedResourceBytes(resourceName);
			}
			default:
				throw new NotImplementedException();
		}
	}

	private static byte[] GetEmbeddedResourceBytes(string resourceName)
	{
		var assembly = typeof(ImGuiController).Assembly;
		string[] names = assembly.GetManifestResourceNames();
		if (!names.Contains(resourceName))
		{
			throw new ArgumentException($"Resource {resourceName} was not found in the assembly", nameof(resourceName));
		}

		using var s = assembly.GetManifestResourceStream(resourceName);
		long length = s?.Length ?? 0;
		byte[] ret = new byte[length];
		s?.Read(ret, 0, (int)length);
		return ret;
	}

	/// <summary>
	/// Recreates the device texture used to render text.
	/// </summary>
	internal unsafe void RecreateFontDeviceTexture(GraphicsDevice gd)
	{
		var io = ImGui.GetIO();
		io.Fonts.GetTexDataAsRGBA32(out byte* pixels, out int width, out int height, out int bytesPerPixel);
		// Store our identifier
		io.Fonts.SetTexID(_fontAtlasID);

		_fontTexture = gd.ResourceFactory.CreateTexture(TextureDescription.Texture2D(
			(uint)width,
			(uint)height,
			1,
			1,
			PixelFormat.R8_G8_B8_A8_UNorm,
			TextureUsage.Sampled));
		_fontTexture.Name = "ImGui.NET Font Texture";
		gd.UpdateTexture(
			_fontTexture,
			(IntPtr)pixels,
			(uint)(bytesPerPixel * width * height),
			0,
			0,
			0,
			(uint)width,
			(uint)height,
			1,
			0,
			0);
		_fontTextureView = gd.ResourceFactory.CreateTextureView(_fontTexture);

		io.Fonts.ClearTexData();
	}

	/// <summary>
	/// Renders the ImGui draw list data.
	/// This method requires a <see cref="GraphicsDevice"/> because it may create new DeviceBuffers if the size of vertex
	/// or index data has increased beyond the capacity of the existing buffers.
	/// A <see cref="CommandList"/> is needed to submit drawing and resource update commands.
	/// </summary>
	internal void Render(GraphicsDevice gd, CommandList cl)
	{
		if (_frameBegun)
		{
			_frameBegun = false;
			ImGui.Render();
			RenderImDrawData(ImGui.GetDrawData(), gd, cl);
		}
	}

	/// <summary>
	/// Updates ImGui input and IO configuration state.
	/// </summary>
	internal void Update(float deltaSeconds, InputSnapshot snapshot)
	{
		if (_frameBegun)
		{
			ImGui.Render();
		}

		SetPerFrameImGuiData(deltaSeconds);
		UpdateImGuiInput(snapshot);

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
		io.DisplaySize = new Vector2(
			_windowWidth / _scaleFactor.X,
			_windowHeight / _scaleFactor.Y);
		io.DisplayFramebufferScale = _scaleFactor;
		io.DeltaTime = deltaSeconds; // DeltaTime is in seconds.
	}

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0010:Add missing cases", Justification = "<Pending>")]
	private void UpdateImGuiInput(InputSnapshot snapshot)
	{
		var io = ImGui.GetIO();

		var mousePosition = snapshot.MousePosition;

		// Determine if any of the mouse buttons were pressed during this snapshot period, even if they are no longer held.
		bool leftPressed = false;
		bool middlePressed = false;
		bool rightPressed = false;
		foreach (var me in snapshot.MouseEvents)
		{
			if (me.Down)
			{
				switch (me.MouseButton)
				{
					case MouseButton.Left:
						leftPressed = true;
						break;
					case MouseButton.Middle:
						middlePressed = true;
						break;
					case MouseButton.Right:
						rightPressed = true;
						break;
				}
			}
		}

		io.MouseDown[0] = leftPressed || snapshot.IsMouseDown(MouseButton.Left);
		io.MouseDown[1] = rightPressed || snapshot.IsMouseDown(MouseButton.Right);
		io.MouseDown[2] = middlePressed || snapshot.IsMouseDown(MouseButton.Middle);
		io.MousePos = mousePosition;
		io.MouseWheel = snapshot.WheelDelta;

		var keyCharPresses = snapshot.KeyCharPresses;
		for (int i = 0; i < keyCharPresses.Count; i++)
		{
			char c = keyCharPresses[i];
			io.AddInputCharacter(c);
		}

		var keyEvents = snapshot.KeyEvents;
		for (int i = 0; i < keyEvents.Count; i++)
		{
			var keyEvent = keyEvents[i];
			io.KeysDown[(int)keyEvent.Key] = keyEvent.Down;
			if (keyEvent.Key == Key.ControlLeft)
			{
				_controlDown = keyEvent.Down;
			}

			if (keyEvent.Key == Key.ShiftLeft)
			{
				_shiftDown = keyEvent.Down;
			}

			if (keyEvent.Key == Key.AltLeft)
			{
				_altDown = keyEvent.Down;
			}

			if (keyEvent.Key == Key.WinLeft)
			{
				_winKeyDown = keyEvent.Down;
			}
		}

		io.KeyCtrl = _controlDown;
		io.KeyAlt = _altDown;
		io.KeyShift = _shiftDown;
		io.KeySuper = _winKeyDown;
	}

	private static void SetKeyMappings()
	{
		var io = ImGui.GetIO();
		io.KeyMap[(int)ImGuiKey.Tab] = (int)Key.Tab;
		io.KeyMap[(int)ImGuiKey.LeftArrow] = (int)Key.Left;
		io.KeyMap[(int)ImGuiKey.RightArrow] = (int)Key.Right;
		io.KeyMap[(int)ImGuiKey.UpArrow] = (int)Key.Up;
		io.KeyMap[(int)ImGuiKey.DownArrow] = (int)Key.Down;
		io.KeyMap[(int)ImGuiKey.PageUp] = (int)Key.PageUp;
		io.KeyMap[(int)ImGuiKey.PageDown] = (int)Key.PageDown;
		io.KeyMap[(int)ImGuiKey.Home] = (int)Key.Home;
		io.KeyMap[(int)ImGuiKey.End] = (int)Key.End;
		io.KeyMap[(int)ImGuiKey.Delete] = (int)Key.Delete;
		io.KeyMap[(int)ImGuiKey.Backspace] = (int)Key.BackSpace;
		io.KeyMap[(int)ImGuiKey.Enter] = (int)Key.Enter;
		io.KeyMap[(int)ImGuiKey.Escape] = (int)Key.Escape;
		io.KeyMap[(int)ImGuiKey.A] = (int)Key.A;
		io.KeyMap[(int)ImGuiKey.C] = (int)Key.C;
		io.KeyMap[(int)ImGuiKey.V] = (int)Key.V;
		io.KeyMap[(int)ImGuiKey.X] = (int)Key.X;
		io.KeyMap[(int)ImGuiKey.Y] = (int)Key.Y;
		io.KeyMap[(int)ImGuiKey.Z] = (int)Key.Z;
	}

	private void RenderImDrawData(ImDrawDataPtr draw_data, GraphicsDevice gd, CommandList cl)
	{
		uint vertexOffsetInVertices = 0;
		uint indexOffsetInElements = 0;

		if (draw_data.CmdListsCount == 0)
		{
			return;
		}

		uint totalVBSize = (uint)(draw_data.TotalVtxCount * Unsafe.SizeOf<ImDrawVert>());
		if (totalVBSize > _vertexBuffer?.SizeInBytes)
		{
			gd.DisposeWhenIdle(_vertexBuffer);
			_vertexBuffer = gd.ResourceFactory.CreateBuffer(new BufferDescription((uint)(totalVBSize * 1.5f), BufferUsage.VertexBuffer | BufferUsage.Dynamic));
		}

		uint totalIBSize = (uint)(draw_data.TotalIdxCount * sizeof(ushort));
		if (totalIBSize > _indexBuffer?.SizeInBytes)
		{
			gd.DisposeWhenIdle(_indexBuffer);
			_indexBuffer = gd.ResourceFactory.CreateBuffer(new BufferDescription((uint)(totalIBSize * 1.5f), BufferUsage.IndexBuffer | BufferUsage.Dynamic));
		}

		for (int i = 0; i < draw_data.CmdListsCount; i++)
		{
			var cmd_list = draw_data.CmdListsRange[i];

			cl.UpdateBuffer(
				_vertexBuffer,
				vertexOffsetInVertices * (uint)Unsafe.SizeOf<ImDrawVert>(),
				cmd_list.VtxBuffer.Data,
				(uint)(cmd_list.VtxBuffer.Size * Unsafe.SizeOf<ImDrawVert>()));

			cl.UpdateBuffer(
				_indexBuffer,
				indexOffsetInElements * sizeof(ushort),
				cmd_list.IdxBuffer.Data,
				(uint)(cmd_list.IdxBuffer.Size * sizeof(ushort)));

			vertexOffsetInVertices += (uint)cmd_list.VtxBuffer.Size;
			indexOffsetInElements += (uint)cmd_list.IdxBuffer.Size;
		}

		// Setup orthographic projection matrix into our constant buffer
		var io = ImGui.GetIO();
		var mvp = Matrix4x4.CreateOrthographicOffCenter(
			0f,
			io.DisplaySize.X,
			io.DisplaySize.Y,
			0.0f,
			-1.0f,
			1.0f);

		_gd.UpdateBuffer(_projMatrixBuffer, 0, ref mvp);

		cl.SetVertexBuffer(0, _vertexBuffer);
		cl.SetIndexBuffer(_indexBuffer, IndexFormat.UInt16);
		cl.SetPipeline(_pipeline);
		cl.SetGraphicsResourceSet(0, _mainResourceSet);

		draw_data.ScaleClipRects(io.DisplayFramebufferScale);

		// Render command lists
		int vtx_offset = 0;
		int idx_offset = 0;
		for (int n = 0; n < draw_data.CmdListsCount; n++)
		{
			var cmd_list = draw_data.CmdListsRange[n];
			for (int cmd_i = 0; cmd_i < cmd_list.CmdBuffer.Size; cmd_i++)
			{
				var pcmd = cmd_list.CmdBuffer[cmd_i];
				if (pcmd.UserCallback != IntPtr.Zero)
				{
					throw new NotImplementedException();
				}
				else
				{
					if (pcmd.TextureId != IntPtr.Zero)
					{
						if (pcmd.TextureId == _fontAtlasID)
						{
							cl.SetGraphicsResourceSet(1, _fontTextureResourceSet);
						}
						else
						{
							cl.SetGraphicsResourceSet(1, GetImageResourceSet(pcmd.TextureId));
						}
					}

					cl.SetScissorRect(
						0,
						(uint)pcmd.ClipRect.X,
						(uint)pcmd.ClipRect.Y,
						(uint)(pcmd.ClipRect.Z - pcmd.ClipRect.X),
						(uint)(pcmd.ClipRect.W - pcmd.ClipRect.Y));

					cl.DrawIndexed(pcmd.ElemCount, 1, (uint)idx_offset, vtx_offset, 0);
				}

				idx_offset += (int)pcmd.ElemCount;
			}

			vtx_offset += cmd_list.VtxBuffer.Size;
		}
	}

	/// <summary>
	/// Frees all graphics resources used by the renderer.
	/// </summary>
	void IDisposable.Dispose()
	{
		_vertexBuffer?.Dispose();
		_indexBuffer?.Dispose();
		_projMatrixBuffer?.Dispose();
		_fontTexture?.Dispose();
		_fontTextureView?.Dispose();
		_vertexShader?.Dispose();
		_fragmentShader?.Dispose();
		_layout?.Dispose();
		_textureLayout?.Dispose();
		_pipeline?.Dispose();
		_mainResourceSet?.Dispose();
		_fontTextureResourceSet?.Dispose();

		foreach (var resource in _ownedResources)
		{
			resource?.Dispose();
		}
	}

	private readonly struct ResourceSetInfo
	{
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
		public readonly IntPtr ImGuiBinding;
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
		public readonly ResourceSet ResourceSet;

		public ResourceSetInfo(IntPtr imGuiBinding, ResourceSet resourceSet)
		{
			ImGuiBinding = imGuiBinding;
			ResourceSet = resourceSet;
		}
	}
}
