// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.SyntaxHighlighting;

using System;

using Hexa.NET.ImGui;

/// <summary>
/// Pushes the code font at the target pixel size and pops it on dispose. With no resolver the
/// current font is kept, still at the target size, so code renders (proportionally spaced) rather
/// than not at all.
/// </summary>
internal sealed class ScopedCodeFont : IDisposable
{
	private bool disposed;

	/// <summary>The pixel size the font was pushed at.</summary>
	public float PixelSize { get; }

	/// <summary>Pushes the resolved code font.</summary>
	/// <param name="pixelSize">The target pixel size (already scaled).</param>
	/// <param name="config">The active config providing the optional resolver.</param>
	public ScopedCodeFont(float pixelSize, SyntaxHighlightConfig config)
	{
		Ensure.NotNull(config);
		PixelSize = pixelSize;
		ImFontPtr? resolved = config.FontResolver?.Invoke(pixelSize);
		ImGui.PushFont(resolved.HasValue && !resolved.Value.IsNull ? resolved.Value : ImGui.GetFont(), pixelSize);
	}

	/// <summary>Pops the pushed font.</summary>
	public void Dispose()
	{
		if (!disposed)
		{
			disposed = true;
			ImGui.PopFont();
		}
	}
}
