using System.Numerics;
using System.Runtime.CompilerServices;
using ImGuiNET;
using Veldrid;
using ResourceSet = Veldrid.ResourceSet;

namespace ktsu.io
{
	/// <summary>
	/// A modified version of Veldrid.ImGui's ImGuiRenderer.
	/// Manages input for ImGui and handles rendering ImGui's DrawLists with Veldrid.
	/// </summary>
	internal sealed class ImGuiController : IDisposable
	{
		private GraphicsDevice GraphicsDevice { get; set; }
		private DeviceBuffer VertexBuffer { get; set; }
		private DeviceBuffer IndexBuffer { get; set; }
		private DeviceBuffer ProjMatrixBuffer { get; set; }
		private Texture FontTexture { get; set; }
		private TextureView FontTextureView { get; set; }
		private Shader VertexShader { get; set; }
		private Shader FragmentShader { get; set; }
		private ResourceLayout MainResourceLayout { get; set; }
		private ResourceLayout TextureResourceLayout { get; set; }
		private Pipeline Pipeline { get; set; }
		private ResourceSet MainResourceSet { get; set; }
		private ResourceSet FontTextureResourceSet { get; set; }

		private nint FontAtlasID { get; init; } = 1;
		private bool FrameBegun { get; set; }
		private bool ControlDown { get; set; }
		private bool ShiftDown { get; set; }
		private bool AltDown { get; set; }
		private bool WinKeyDown { get; set; }
		private int WindowWidth { get; set; }
		private int WindowHeight { get; set; }
		private static Vector2 ScaleFactor => Vector2.One;

		// Image trackers
		private Dictionary<TextureView, ResourceSetInfo> SetsByView { get; } = new();
		private Dictionary<Texture, TextureView> ViewsByTexture { get; } = new();
		private Dictionary<nint, ResourceSetInfo> ViewsById { get; } = new();
		private List<IDisposable> OwnedResources { get; } = new();
		private int LastAssignedID { get; set; } = 100;

		/// <summary>
		/// Constructs a new ImGuiController.
		/// </summary>
		internal ImGuiController(GraphicsDevice graphicsDevice, OutputDescription outputDescription, int width, int height)
		{
			GraphicsDevice = graphicsDevice;
			WindowWidth = width;
			WindowHeight = height;

			nint context = ImGui.CreateContext();
			ImGui.SetCurrentContext(context);

			var io = ImGui.GetIO();
			io.Fonts.AddFontDefault();
			io.Fonts.SetTexID(FontAtlasID);
			io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;

			// create device resources
			var factory = graphicsDevice.ResourceFactory;
			VertexBuffer = factory.CreateBuffer(new(sizeInBytes: 10000, usage: BufferUsage.VertexBuffer | BufferUsage.Dynamic));
			VertexBuffer.Name = "ImGui.NET Vertex Buffer";
			IndexBuffer = factory.CreateBuffer(new(sizeInBytes: 2000, usage: BufferUsage.IndexBuffer | BufferUsage.Dynamic));
			IndexBuffer.Name = "ImGui.NET Index Buffer";
			ProjMatrixBuffer = factory.CreateBuffer(new(sizeInBytes: 64, usage: BufferUsage.UniformBuffer | BufferUsage.Dynamic));
			ProjMatrixBuffer.Name = "ImGui.NET Projection Buffer";

			// create font resources
			unsafe
			{
				io.Fonts.GetTexDataAsRGBA32(out byte* fontPixels, out int fontWidth, out int fontHeight, out int fontBytesPerPixel);

				FontTexture = graphicsDevice.ResourceFactory.CreateTexture
				(
					TextureDescription.Texture2D
					(
						width: (uint)fontWidth,
						height: (uint)fontHeight,
						mipLevels: 1,
						arrayLayers: 1,
						format: PixelFormat.R8_G8_B8_A8_UNorm,
						usage: TextureUsage.Sampled
					)
				);

				FontTexture.Name = "ImGui.NET Font Texture";

				graphicsDevice.UpdateTexture
				(
					texture: FontTexture,
					source: (nint)fontPixels,
					sizeInBytes: (uint)(fontBytesPerPixel * fontWidth * fontHeight),
					x: 0,
					y: 0,
					z: 0,
					width: (uint)fontWidth,
					height: (uint)fontHeight,
					depth: 1,
					mipLevel: 0,
					arrayLayer: 0
				);
			}

			FontTextureView = graphicsDevice.ResourceFactory.CreateTextureView(FontTexture);

			io.Fonts.ClearTexData();

			// create shader resources
			string prefix = typeof(ImGuiApp).Assembly.GetName().Name!;
			byte[] vertexShaderBytes = LoadEmbeddedShaderCode(graphicsDevice.ResourceFactory, $"{prefix}.Shaders.imgui-vertex", ShaderStages.Vertex);
			byte[] fragmentShaderBytes = LoadEmbeddedShaderCode(graphicsDevice.ResourceFactory, $"{prefix}.Shaders.imgui-frag", ShaderStages.Fragment);

			VertexShader = factory.CreateShader(new(stage: ShaderStages.Vertex, shaderBytes: vertexShaderBytes, entryPoint: "VS"));
			FragmentShader = factory.CreateShader(new(stage: ShaderStages.Fragment, shaderBytes: fragmentShaderBytes, entryPoint: "FS"));

			var vertexLayouts = new[]
			{
				new VertexLayoutDescription
				(
					new VertexElementDescription("in_position", VertexElementSemantic.Position, VertexElementFormat.Float2),
					new VertexElementDescription("in_texCoord", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
					new VertexElementDescription("in_color", VertexElementSemantic.Color, VertexElementFormat.Byte4_Norm)
				)
			};

			MainResourceLayout = factory.CreateResourceLayout
			(
				new ResourceLayoutDescription
				(
					new ResourceLayoutElementDescription("ProjectionMatrixBuffer", ResourceKind.UniformBuffer, ShaderStages.Vertex),
					new ResourceLayoutElementDescription("MainSampler", ResourceKind.Sampler, ShaderStages.Fragment)
				)
			);

			TextureResourceLayout = factory.CreateResourceLayout
			(
				new ResourceLayoutDescription
				(
					elements: new ResourceLayoutElementDescription("MainTexture", ResourceKind.TextureReadOnly, ShaderStages.Fragment)
				)
			);

			Pipeline = factory.CreateGraphicsPipeline
			(
				new
				(
					blendState: BlendStateDescription.SingleAlphaBlend,
					depthStencilStateDescription: new
					(
						depthTestEnabled: false,
						depthWriteEnabled: false,
						comparisonKind: ComparisonKind.Always
					),
					rasterizerState: new
					(
						cullMode: FaceCullMode.None,
						fillMode: PolygonFillMode.Solid,
						frontFace: FrontFace.Clockwise,
						depthClipEnabled: false,
						scissorTestEnabled: true
					),
					primitiveTopology: PrimitiveTopology.TriangleList,
					shaderSet: new(vertexLayouts, new[] { VertexShader, FragmentShader }),
					resourceLayouts: new[] { MainResourceLayout, TextureResourceLayout },
					outputs: outputDescription
				)
			);

			MainResourceSet = factory.CreateResourceSet
			(
				new
				(
					MainResourceLayout,
					ProjMatrixBuffer,
					graphicsDevice.PointSampler
				)
			);

			FontTextureResourceSet = factory.CreateResourceSet
			(
				new
				(
					TextureResourceLayout,
					FontTextureView
				)
			);

			SetKeyMappings();

			SetPerFrameImGuiData(1f / 60f);

			ImGui.NewFrame();
			FrameBegun = true;
		}

		internal void WindowResized(int width, int height)
		{
			WindowWidth = width;
			WindowHeight = height;
		}

		/// <summary>
		/// Gets or creates a handle for a texture to be drawn with ImGui.
		/// Pass the returned handle to Image() or ImageButton().
		/// </summary>
		internal nint GetOrCreateImGuiBinding(ResourceFactory factory, TextureView textureView)
		{
			if (!SetsByView.TryGetValue(textureView, out var rsi))
			{
				var resourceSet = factory.CreateResourceSet(new(TextureResourceLayout, textureView));
				rsi = new(GetNextImGuiBindingID(), resourceSet);

				SetsByView.Add(textureView, rsi);
				ViewsById.Add(rsi.ImGuiBinding, rsi);
				OwnedResources.Add(resourceSet);
			}

			return rsi.ImGuiBinding;
		}

		private nint GetNextImGuiBindingID()
		{
			int newID = LastAssignedID++;
			return newID;
		}

		/// <summary>
		/// Gets or creates a handle for a texture to be drawn with ImGui.
		/// Pass the returned handle to Image() or ImageButton().
		/// </summary>
		internal nint GetOrCreateImGuiBinding(ResourceFactory factory, Texture texture)
		{
			if (!ViewsByTexture.TryGetValue(texture, out var textureView))
			{
				textureView = factory.CreateTextureView(texture);
				ViewsByTexture.Add(texture, textureView);
				OwnedResources.Add(textureView);
			}

			return GetOrCreateImGuiBinding(factory, textureView);
		}

		/// <summary>
		/// Retrieves the shader texture binding for the given helper handle.
		/// </summary>
		internal ResourceSet GetImageResourceSet(nint imGuiBinding)
		{
			return !ViewsById.TryGetValue(imGuiBinding, out var tvi)
				? throw new InvalidOperationException($"No registered ImGui binding with id {imGuiBinding}")
				: tvi.ResourceSet;
		}

		internal void ClearCachedImageResources()
		{
			foreach (var resource in OwnedResources)
			{
				resource.Dispose();
			}

			OwnedResources.Clear();
			SetsByView.Clear();
			ViewsById.Clear();
			ViewsByTexture.Clear();
			LastAssignedID = 100;
		}

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

				case GraphicsBackend.OpenGLES:
					throw new NotImplementedException();
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
			_ = (s?.Read(ret, 0, (int)length));
			return ret;
		}

		/// <summary>
		/// Renders the ImGui draw list data.
		/// This method requires a <see cref="Veldrid.GraphicsDevice"/> because it may create new DeviceBuffers if the size of vertex
		/// or index data has increased beyond the capacity of the existing buffers.
		/// A <see cref="CommandList"/> is needed to submit drawing and resource update commands.
		/// </summary>
		internal void Render(GraphicsDevice graphicsDevice, CommandList commandList)
		{
			if (FrameBegun)
			{
				FrameBegun = false;
				ImGui.Render();
				RenderImDrawData(ImGui.GetDrawData(), graphicsDevice, commandList);
			}
		}

		/// <summary>
		/// Updates ImGui input and IO configuration state.
		/// </summary>
		internal void Update(float deltaSeconds, InputSnapshot snapshot)
		{
			if (FrameBegun)
			{
				ImGui.Render();
			}

			SetPerFrameImGuiData(deltaSeconds);
			UpdateImGuiInput(snapshot);

			FrameBegun = true;
			ImGui.NewFrame();
		}

		/// <summary>
		/// Sets per-frame data based on the associated window.
		/// This is called by Update(float).
		/// </summary>
		private void SetPerFrameImGuiData(float deltaSeconds)
		{
			var io = ImGui.GetIO();
			io.DisplaySize = new
			(
				WindowWidth / ScaleFactor.X,
				WindowHeight / ScaleFactor.Y
			);
			io.DisplayFramebufferScale = ScaleFactor;
			io.DeltaTime = deltaSeconds; // DeltaTime is in seconds.
		}

		private void UpdateImGuiInput(InputSnapshot snapshot)
		{
			var io = ImGui.GetIO();

			var mousePosition = snapshot.MousePosition;

			// Determine if any of the mouse buttons were pressed during this snapshot period, even if they are no longer held.
			bool leftPressed = false;
			bool middlePressed = false;
			bool rightPressed = false;
			foreach (var mouseEvent in snapshot.MouseEvents)
			{
				if (mouseEvent.Down)
				{
					switch (mouseEvent.MouseButton)
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
						case MouseButton.Button1:
							break;
						case MouseButton.Button2:
							break;
						case MouseButton.Button3:
							break;
						case MouseButton.Button4:
							break;
						case MouseButton.Button5:
							break;
						case MouseButton.Button6:
							break;
						case MouseButton.Button7:
							break;
						case MouseButton.Button8:
							break;
						case MouseButton.Button9:
							break;
						case MouseButton.LastButton:
							break;
						default:
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
					ControlDown = keyEvent.Down;
				}

				if (keyEvent.Key == Key.ShiftLeft)
				{
					ShiftDown = keyEvent.Down;
				}

				if (keyEvent.Key == Key.AltLeft)
				{
					AltDown = keyEvent.Down;
				}

				if (keyEvent.Key == Key.WinLeft)
				{
					WinKeyDown = keyEvent.Down;
				}
			}

			io.KeyCtrl = ControlDown;
			io.KeyAlt = AltDown;
			io.KeyShift = ShiftDown;
			io.KeySuper = WinKeyDown;
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

		private void RenderImDrawData(ImDrawDataPtr drawData, GraphicsDevice graphicsDevice, CommandList commandList)
		{
			uint vertexOffsetInVertices = 0;
			uint indexOffsetInElements = 0;

			if (drawData.CmdListsCount == 0)
			{
				return;
			}

			uint totalVBSize = (uint)(drawData.TotalVtxCount * Unsafe.SizeOf<ImDrawVert>());
			if (totalVBSize > VertexBuffer?.SizeInBytes)
			{
				graphicsDevice.DisposeWhenIdle(VertexBuffer);
				VertexBuffer = graphicsDevice.ResourceFactory.CreateBuffer
				(
					new(sizeInBytes: (uint)(totalVBSize * 1.5f), usage: BufferUsage.VertexBuffer | BufferUsage.Dynamic)
				);
			}

			uint totalIBSize = (uint)(drawData.TotalIdxCount * sizeof(ushort));
			if (totalIBSize > IndexBuffer?.SizeInBytes)
			{
				graphicsDevice.DisposeWhenIdle(IndexBuffer);
				IndexBuffer = graphicsDevice.ResourceFactory.CreateBuffer
				(
					new(sizeInBytes: (uint)(totalIBSize * 1.5f), usage: BufferUsage.IndexBuffer | BufferUsage.Dynamic)
				);
			}

			for (int i = 0; i < drawData.CmdListsCount; i++)
			{
				var cmd_list = drawData.CmdListsRange[i];

				commandList.UpdateBuffer
				(
					buffer: VertexBuffer,
					bufferOffsetInBytes: vertexOffsetInVertices * (uint)Unsafe.SizeOf<ImDrawVert>(),
					source: cmd_list.VtxBuffer.Data,
					sizeInBytes: (uint)(cmd_list.VtxBuffer.Size * Unsafe.SizeOf<ImDrawVert>())
				);

				commandList.UpdateBuffer
				(
					buffer: IndexBuffer,
					bufferOffsetInBytes: indexOffsetInElements * sizeof(ushort),
					source: cmd_list.IdxBuffer.Data,
					sizeInBytes: (uint)(cmd_list.IdxBuffer.Size * sizeof(ushort))
				);

				vertexOffsetInVertices += (uint)cmd_list.VtxBuffer.Size;
				indexOffsetInElements += (uint)cmd_list.IdxBuffer.Size;
			}

			// Setup orthographic projection matrix into our constant buffer
			var io = ImGui.GetIO();
			var mvp = Matrix4x4.CreateOrthographicOffCenter
			(
				left: 0f,
				right: io.DisplaySize.X,
				bottom: io.DisplaySize.Y,
				top: 0.0f,
				zNearPlane: -1.0f,
				zFarPlane: 1.0f
			);

			GraphicsDevice.UpdateBuffer(buffer: ProjMatrixBuffer, bufferOffsetInBytes: 0, source: ref mvp);

			commandList.SetVertexBuffer(0, VertexBuffer);
			commandList.SetIndexBuffer(IndexBuffer, IndexFormat.UInt16);
			commandList.SetPipeline(Pipeline);
			commandList.SetGraphicsResourceSet(0, MainResourceSet);

			drawData.ScaleClipRects(io.DisplayFramebufferScale);

			// Render command lists
			int vtxOffset = 0;
			int idxOffset = 0;
			for (int n = 0; n < drawData.CmdListsCount; n++)
			{
				var cmdList = drawData.CmdListsRange[n];
				for (int cmdIdx = 0; cmdIdx < cmdList.CmdBuffer.Size; cmdIdx++)
				{
					var cmd = cmdList.CmdBuffer[cmdIdx];
					if (cmd.UserCallback != nint.Zero)
					{
						throw new NotImplementedException();
					}
					else
					{
						if (cmd.TextureId != nint.Zero)
						{
							if (cmd.TextureId == FontAtlasID)
							{
								commandList.SetGraphicsResourceSet(slot: 1, rs: FontTextureResourceSet);
							}
							else
							{
								commandList.SetGraphicsResourceSet(slot: 1, rs: GetImageResourceSet(cmd.TextureId));
							}
						}

						commandList.SetScissorRect(
							index: 0,
							x: (uint)cmd.ClipRect.X,
							y: (uint)cmd.ClipRect.Y,
							width: (uint)(cmd.ClipRect.Z - cmd.ClipRect.X),
							height: (uint)(cmd.ClipRect.W - cmd.ClipRect.Y));

						commandList.DrawIndexed(cmd.ElemCount, 1, (uint)idxOffset, vtxOffset, 0);
					}

					idxOffset += (int)cmd.ElemCount;
				}

				vtxOffset += cmdList.VtxBuffer.Size;
			}
		}

		/// <summary>
		/// Frees all graphics resources used by the renderer.
		/// </summary>
		public void Dispose()
		{
			VertexBuffer?.Dispose();
			IndexBuffer?.Dispose();
			ProjMatrixBuffer?.Dispose();
			FontTexture?.Dispose();
			FontTextureView?.Dispose();
			VertexShader?.Dispose();
			FragmentShader?.Dispose();
			MainResourceLayout?.Dispose();
			TextureResourceLayout?.Dispose();
			Pipeline?.Dispose();
			MainResourceSet?.Dispose();
			FontTextureResourceSet?.Dispose();

			foreach (var resource in OwnedResources)
			{
				resource?.Dispose();
			}
		}

		private readonly struct ResourceSetInfo
		{
			public nint ImGuiBinding { get; init; }
			public ResourceSet ResourceSet { get; init; }

			internal ResourceSetInfo(nint imGuiBinding, ResourceSet resourceSet)
			{
				ImGuiBinding = imGuiBinding;
				ResourceSet = resourceSet;
			}
		}
	}
}
