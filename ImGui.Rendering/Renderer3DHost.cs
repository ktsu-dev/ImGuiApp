// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Rendering;

using System;
using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Where a library that draws 3D finds the renderer the application host installed.
/// </summary>
/// <remarks>
/// <para>
/// A widget library cannot ask the application host directly: <c>ktsu.ImGui.Widgets</c> does not
/// reference <c>ktsu.ImGui.App</c>, deliberately, because that would put windowing, OpenGL and the
/// image decoders under every consumer of the widgets. So the host publishes how to reach its
/// renderer here, and a widget asks here. <c>ktsu.ImGui.App</c> installs a resolver that answers
/// with whatever backend is installed at the moment of asking, which is the same answer
/// <c>ImGuiApp.TryGetRenderer3D</c> gives.
/// </para>
/// <para>
/// A resolver rather than a stored renderer, so a host never has to remember to clear it: the
/// backend changes every time a headless harness starts, and a stored reference would outlive the
/// renderer it named.
/// </para>
/// </remarks>
public static class Renderer3DHost
{
	/// <summary>
	/// Gets or sets the function answering which 3D renderer is installed, or null when there is none.
	/// </summary>
	/// <remarks>Set by the application host. Null means no host has said anything, which reads as no renderer.</remarks>
	public static Func<IRenderer3D?>? Resolver { get; set; }

	/// <summary>Asks the host for its 3D renderer.</summary>
	/// <param name="renderer3D">The renderer, when there is one.</param>
	/// <returns><see langword="true"/> when the host has a renderer that draws 3D.</returns>
	public static bool TryGet([NotNullWhen(true)] out IRenderer3D? renderer3D)
	{
		renderer3D = Resolver?.Invoke();
		return renderer3D is not null;
	}
}
