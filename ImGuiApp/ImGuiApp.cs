// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGuiApp;

using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Numerics;
using System.Runtime.CompilerServices;
using Hexa.NET.ImGui;
using Hexa.NET.KittyUI;
using Hexa.NET.KittyUI.ImGuiBackend;
using Hexa.NET.KittyUI.UI;
using ktsu.Extensions;
using ktsu.Invoker;
using ktsu.StrongPaths;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

/// <summary>
/// Provides static methods and properties to manage the ImGui application.
/// </summary>
/// <remarks>
/// This class serves as the primary interface for working with ImGui applications.
/// It handles window management, texture loading, font handling, and provides methods
/// for interacting with the application's graphical resources and UI components.
/// 
/// Core functionalities include:
/// <list type="bullet">
/// <item><description>Application startup and configuration</description></item>
/// <item><description>Window state management</description></item>
/// <item><description>Texture loading and caching</description></item>
/// <item><description>Font handling and management</description></item>
/// <item><description>Event handling for window resizing and moving</description></item>
/// </list>
/// </remarks>
/// <example>
/// Start an ImGui application with a basic configuration:
/// <code>
/// var config = new ImGuiAppConfig {
///     Title = "My ImGui Application",
///     InitialWindowState = new ImGuiAppWindowState {
///         Size = new Vector2(1280, 720)
///     }
/// };
/// ImGuiApp.Start(config);
/// </code>
/// </example>
public static partial class ImGuiApp
{
	/// <summary>
	/// Gets the main application window instance.
	/// </summary>
	/// <remarks>
	/// This property provides access to the primary window of the ImGui application.
	/// It is initialized during application startup and can be used to interact with
	/// the main window, including accessing window properties, handling window events,
	/// and managing window content.
	/// </remarks>
	/// <value>
	/// An instance of <see cref="MainWindow"/> representing the application's main window.
	/// </value>
	public static MainWindow MainWindow { get; private set; } = null!;

	/// <summary>
	/// Gets the current state of the application window, including size and position.
	/// </summary>
	/// <remarks>
	/// Returns a new instance of <see cref="ImGuiAppWindowState"/> with the current size and position
	/// of the main window. This can be used to save window state for future application sessions.
	/// </remarks>
	/// <returns>An <see cref="ImGuiAppWindowState"/> object containing the current window properties.</returns>
	public static ImGuiAppWindowState WindowState
	{
		get => new()
		{
			Size = MainWindow.Size,
			Position = MainWindow.Position,
		};
	}

	/// <summary>
	/// Gets an instance of the <see cref="Invoker"/> class to delegate tasks to the window thread.
	/// </summary>
	public static Invoker Invoker { get; private set; } = null!;

	internal static ConcurrentDictionary<AbsoluteFilePath, ImGuiAppTextureInfo> Textures { get; } = [];

	internal static ImGuiAppConfig Config { get; private set; } = new();

	internal static void OnInit(MainWindow mainWindow)
	{
		MainWindow = mainWindow;
		MainWindow.SizeChanged += OnResize;
		if (!string.IsNullOrEmpty(Config.IconPath))
		{
			SetWindowIcon(Config.IconPath);
		}
	}

	internal static void OnConfigure(ImGuiContextPtr context, ImGuiIOPtr io)
	{
		UIScaler.UpdateDpiScale();
		Config.OnStart?.Invoke();
	}

	internal static void OnResize(object? sender, Vector2 from, Vector2 to)
	{
		UIScaler.UpdateDpiScale();
		Config.OnResize?.Invoke();
	}

	internal static void OnMove() => Config.OnMove?.Invoke();

	/// <summary>
	/// Starts the ImGui application with the specified configuration.
	/// </summary>
	/// <param name="config">The configuration settings for the ImGui application.</param>
	public static void Start(ImGuiAppConfig config)
	{
		ArgumentNullException.ThrowIfNull(config);

		Invoker = new();
		Config = config;

		ValidateConfig(config);

		UIScaler.Init();

		AppBuilder.Create()
			.AddWindow<MainWindow>()
			.EnableDebugTools(true)
			.EnableLogging(true)
			.EnableImPlot()
			.AddTitleBar<TitleBar>()
			.SetTitle(config.Title)
			.StyleColorsDark()
			.ImGuiConfigure(OnConfigure)
			.AddFont(BuildFonts)
			.Run();
	}

	private static void BuildFonts(ImGuiFontBuilder builder)
	{
		var fontsToLoad = Config.Fonts.Concat(Config.DefaultFonts);
		var fontIndex = 0;

		foreach (var (name, fontBytes) in fontsToLoad)
		{
			foreach (var size in UIScaler.SupportedPixelFontSizes)
			{
				unsafe
				{
					fixed (byte* fontPtr = &fontBytes[0])
					{
						builder.AddFontFromMemoryTTF(fontPtr, fontBytes.Length, size);
						UIScaler.RegisterFont(name, size, fontIndex);
						fontIndex++;
					}
				}
			}
		}
	}

	private static void ValidateConfig(ImGuiAppConfig config)
	{
		if (config.InitialWindowState.Size.X <= 0 || config.InitialWindowState.Size.Y <= 0)
		{
			throw new ArgumentException("Initial window size must be greater than zero.", nameof(config));
		}

		if (config.InitialWindowState.Position.X < 0 || config.InitialWindowState.Position.Y < 0)
		{
			throw new ArgumentException("Initial window position must be non-negative.", nameof(config));
		}

		if (!string.IsNullOrEmpty(config.IconPath) && !File.Exists(config.IconPath))
		{
			throw new FileNotFoundException("Icon file not found.", config.IconPath);
		}

		foreach (var font in config.Fonts)
		{
			if (string.IsNullOrEmpty(font.Key) || font.Value == null)
			{
				throw new ArgumentException("Font name and data must be specified.", nameof(config));
			}
		}

		if (config.DefaultFonts.Count == 0)
		{
			throw new ArgumentException("At least one default font must be specified in the configuration.", nameof(config));
		}

		foreach (var font in config.DefaultFonts)
		{
			if (string.IsNullOrEmpty(font.Key) || font.Value == null)
			{
				throw new ArgumentException("Default font name and data must be specified.", nameof(config));
			}
		}
	}

	/// <summary>
	/// Sets the window icon using the specified icon file path.
	/// </summary>
	/// <param name="iconPath">The file path to the icon image.</param>
	public static void SetWindowIcon(string iconPath)
	{
		using var stream = File.OpenRead(iconPath);
		using var sourceImage = Image.Load<Rgba32>(stream);

		int[] iconSizes = [128, 64, 48, 32, 28, 24, 22, 20, 18, 16];

		var icons = new Collection<Silk.NET.Core.RawImage>();

		foreach (var size in iconSizes)
		{
			var resizeImage = sourceImage.Clone();
			var sourceSize = Math.min(sourceImage.Width, sourceImage.Height);
			resizeImage.Mutate(x => x.Crop(sourceSize, sourceSize).Resize(size, size, KnownResamplers.Welch));

			UseImageBytes(resizeImage, bytes =>
			{
				// Create a permanent copy since RawImage needs to keep the data
				var iconData = new byte[bytes.Length];
				Array.Copy(bytes, iconData, bytes.Length);
				icons.Add(new(size, size, new Memory<byte>(iconData)));
			});
		}

		//Invoker.Invoke(() => window?.SetWindowIcon([.. icons]));
	}

	private static readonly ArrayPool<byte> _bytePool = ArrayPool<byte>.Shared;

	/// <summary>
	/// Gets or loads a texture from the specified file path with optimized memory usage.
	/// </summary>
	public static ImGuiAppTextureInfo GetOrLoadTexture(AbsoluteFilePath path)
	{
		// Check if the texture is already loaded
		if (Textures.TryGetValue(path, out var existingTexture))
		{
			return existingTexture;
		}

		using var image = Image.Load<Rgba32>(path);

		var textureInfo = new ImGuiAppTextureInfo
		{
			Path = path,
			Width = image.Width,
			Height = image.Height
		};

		UseImageBytes(image, bytes => textureInfo.TextureId = UploadTextureRGBA(bytes, image.Width, image.Height));

		Textures[path] = textureInfo;
		return textureInfo;
	}

	/// <summary>
	/// Executes an action with temporary access to image bytes using pooled memory for efficiency.
	/// The bytes are returned to the pool after the action completes.
	/// </summary>
	/// <param name="image">The image to process.</param>
	/// <param name="action">The action to perform with the image bytes.</param>
	public static void UseImageBytes(Image<Rgba32> image, Action<byte[]> action)
	{
		ArgumentNullException.ThrowIfNull(image);
		ArgumentNullException.ThrowIfNull(action);

		var bufferSize = image.Width * image.Height * Unsafe.SizeOf<Rgba32>();

		// Rent buffer from pool
		var pooledBuffer = _bytePool.Rent(bufferSize);
		try
		{
			// Copy the image data to the pooled buffer
			image.CopyPixelDataTo(pooledBuffer.AsSpan(0, bufferSize));

			// Execute the action with the pooled buffer
			action(pooledBuffer);
		}
		finally
		{
			// Always return the buffer to the pool
			_byte_pool.Return(pooledBuffer);
		}
	}

	/// <summary>
	/// Tries to get a texture from the texture dictionary without loading it.
	/// </summary>
	/// <param name="path">The path to the texture file.</param>
	/// <param name="textureInfo">When this method returns, contains the texture information if the texture is found; otherwise, null.</param>
	/// <returns>true if the texture was found; otherwise, false.</returns>
	public static bool TryGetTexture(AbsoluteFilePath path, out ImGuiAppTextureInfo? textureInfo) => Textures.TryGetValue(path, out textureInfo);

	/// <summary>
	/// Tries to get a texture from the texture dictionary without loading it.
	/// </summary>
	/// <param name="path">The path to the texture file as a string.</param>
	/// <param name="textureInfo">When this method returns, contains the texture information if the texture is found; otherwise, null.</param>
	/// <returns>true if the texture was found; otherwise, false.</returns>
	public static bool TryGetTexture(string path, out ImGuiAppTextureInfo? textureInfo) => TryGetTexture(path.As<AbsoluteFilePath>(), out textureInfo);

	/// <summary>
	/// Uploads a texture to the GPU using the specified RGBA byte array, width, and height.
	/// </summary>
	private static uint UploadTextureRGBA(byte[] bytes, int width, int height)
	{
		return Invoker.Invoke(() =>
		{
		});
	}

	/// <summary>
	/// Deletes the specified texture from the GPU.
	/// </summary>
	/// <param name="textureId">The OpenGL texture ID to delete.</param>
	/// <exception cref="InvalidOperationException">Thrown if the OpenGL context is not initialized.</exception>
	public static void DeleteTexture(uint textureId)
	{
		Invoker.Invoke(() =>
		{
		});
	}

	/// <summary>
	/// Resets all static state for testing purposes.
	/// </summary>
	internal static void Reset()
	{
		Invoker = null!;
		Textures.Clear();
		Config = new();
	}
}
