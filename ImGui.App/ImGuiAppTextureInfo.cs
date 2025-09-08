// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.App;
using Hexa.NET.ImGui;
using ktsu.Semantics.Paths;

/// <summary>
/// Represents information about a texture, including its file path, texture ID, width, and height.
/// </summary>
public class ImGuiAppTextureInfo
{
	/// <summary>
	/// Gets or sets the file path of the texture.
	/// </summary>
	public AbsoluteFilePath Path { get; set; } = new();

	/// <summary>
	/// Gets or sets the OpenGL texture ID.
	/// </summary>
	public uint TextureId { get; set; }

	/// <summary>
	/// Gets or sets the ImGui texture reference for ImGui 1.92+ texture system.
	/// </summary>
	public ImTextureRef TextureRef { get; set; }

	/// <summary>
	/// Gets or sets the width of the texture.
	/// </summary>
	public int Width { get; set; }

	/// <summary>
	/// Gets or sets the height of the texture.
	/// </summary>
	public int Height { get; set; }
}
