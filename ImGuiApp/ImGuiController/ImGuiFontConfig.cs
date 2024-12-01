namespace ktsu.ImGuiApp.ImGuiController;

//Adapted from https://github.com/dotnet/Silk.NET/blob/main/src/OpenGL/Extensions/Silk.NET.OpenGL.Extensions.ImGui/ImGuiFontConfig.cs

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using ImGuiNET;

public readonly struct ImGuiFontConfig
{
	public ImGuiFontConfig(string fontPath, int fontSize, Func<ImGuiIOPtr, IntPtr>? getGlyphRange = null)
	{
		if (fontSize <= 0)
			throw new ArgumentOutOfRangeException(nameof(fontSize));
		FontPath = fontPath ?? throw new ArgumentNullException(nameof(fontPath));
		FontSize = fontSize;
		GetGlyphRange = getGlyphRange;
	}

	public string FontPath { get; }
	public int FontSize { get; }
	public Func<ImGuiIOPtr, IntPtr>? GetGlyphRange { get; }
}
