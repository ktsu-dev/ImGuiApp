// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Monsoon;

using Hexa.NET.ImGui;
using ktsu.ImGui.Probes;

/// <summary>Ends a <see cref="MonsoonWidgets.GlassPanel"/> when disposed.</summary>
public sealed class MonsoonGlassPanel : IDisposable
{
	private readonly string id;
	private bool disposed;

	internal MonsoonGlassPanel(string id, bool isVisible)
	{
		this.id = id;
		IsVisible = isVisible;
	}

	/// <summary>Gets a value indicating whether the panel's contents are visible, so work for them is worth doing.</summary>
	public bool IsVisible { get; }

	/// <summary>Ends the panel.</summary>
	public void Dispose()
	{
		if (disposed)
		{
			return;
		}

		disposed = true;
		ImGui.EndChild();
		ImGuiProbes.MarkItem(id);
	}
}
