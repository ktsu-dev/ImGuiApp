// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App;

using System.Diagnostics.CodeAnalysis;

/// <summary>
/// The Dear ImGui extensions an application wants <c>ImGuiExtensionManager</c> to set up. On iOS
/// the extension manager is not built, so none is ever set up whatever this says.
/// </summary>
/// <remarks>
/// A flag only asks for an extension: one whose assembly is not referenced is still skipped, so
/// <see cref="All"/> means "whatever is available", which is the auto-detection every application
/// had before this setting existed. A flag left out is never loaded, reflected over or given a
/// context, which is where the start-up time goes.
/// </remarks>
[Flags]
[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "Plural of extension, not the Extensions suffix CA1711 reserves for extension-method classes.")]
public enum ImGuiAppExtensions
{
	/// <summary>Set up no extension.</summary>
	None = 0,

	/// <summary>ImGuizmo (Hexa.NET.ImGuizmo): shares the ImGui context and begins a frame each frame.</summary>
	ImGuizmo = 1,

	/// <summary>ImNodes (Hexa.NET.ImNodes): a context of its own with the dark style. <c>ktsu.ImGui.NodeEditor</c> needs it.</summary>
	ImNodes = 2,

	/// <summary>ImPlot (Hexa.NET.ImPlot): a context of its own with the dark style.</summary>
	ImPlot = 4,

	/// <summary>Every extension that is referenced. The default.</summary>
	All = ImGuizmo | ImNodes | ImPlot,
}
