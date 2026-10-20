// Copyright (c) 2023-2026 ktsu-dev contributors

#if !IOS
namespace ktsu.ImGui.App;

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using ktsu.ImGui.Rendering;

/// <summary>
/// Tells <see cref="Renderer3DHost"/> how to reach this host's renderer, so a widget library that
/// cannot reference this one still finds the 3D path.
/// </summary>
internal static class Renderer3DRegistration
{
	/// <summary>Installs the resolver before anything in this assembly can install a backend.</summary>
	/// <remarks>
	/// A module initializer rather than a call from <c>ImGuiApp.Start</c>, because a backend is
	/// installed from several places — <c>Start</c>, the embedded host, a headless harness through
	/// <see cref="ImGuiApp.BeginExternalFrameSession(IRendererBackend)"/>, and tests assigning it
	/// directly — and every one of them runs code in this module first. The resolver reads the
	/// backend on each call, so it never needs updating afterwards.
	/// </remarks>
	[ModuleInitializer]
	[SuppressMessage("Usage", "CA2255:The 'ModuleInitializer' attribute should not be used in libraries", Justification = "The resolver must be in place before any backend is installed, by whichever of the several paths installs one, and it does nothing but store a delegate.")]
	internal static void Install() => Renderer3DHost.Resolver = static () => ImGuiApp.TryGetRenderer3D(out IRenderer3D? renderer3D) ? renderer3D : null;
}
#endif
