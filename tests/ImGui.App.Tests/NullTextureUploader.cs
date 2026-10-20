// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Tests;

using Hexa.NET.ImGui;

using ktsu.ImGui.App.ImGuiController;

/// <summary>An uploader that names every texture 1 and stores nothing.</summary>
internal sealed class NullTextureUploader : ITextureUploader
{
	public nint Create(int width, int height, nint pixels) => 1;

	public void Update(nint textureId, int width, int height, ImTextureRect rect, nint pixels)
	{
	}

	public void Destroy(nint textureId)
	{
	}
}
