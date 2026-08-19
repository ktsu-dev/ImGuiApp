// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Probes;

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;

using Hexa.NET.ImGui;

/// <summary>
/// Records where named user interface items were drawn, so an automated test can address a widget by
/// name rather than by pixel position.
/// </summary>
/// <remarks>
/// <para>
/// This lives in its own dependency-free package on purpose. Marking is a cross-cutting concern:
/// widget libraries, dialog libraries and the application host all want to mark items, and none of
/// them should have to depend on another to do it. Putting the registry in any one of those packages
/// would force the others into a dependency they have no other reason to take.
/// </para>
/// <para>
/// Marking costs one null check when nothing is recording, which is the case in every production
/// application, so a library can mark unconditionally.
/// </para>
/// </remarks>
public static class ImGuiProbes
{
	private static Action<string, Vector2, Vector2>? probe;

	/// <summary>
	/// Gets or sets a value indicating whether marking is enabled at all. Defaults to true.
	/// </summary>
	/// <remarks>
	/// A master switch, independent of whether a probe is installed. Setting it false suppresses
	/// every mark without disturbing the installed callback, which is useful to exclude a hot path
	/// from recording, or to drive a scenario twice and confirm the application behaves the same
	/// whether or not it is being observed.
	/// </remarks>
	public static bool Enabled { get; set; } = true;

	/// <summary>Gets a value indicating whether marks are currently being recorded.</summary>
	/// <remarks>
	/// True only when marking is enabled and a probe is installed. Worth checking before composing a
	/// name that costs something to build, since calling <see cref="MarkItem(string)"/> directly is
	/// already cheap.
	/// </remarks>
	public static bool IsRecording => Enabled && probe is not null;

	/// <summary>
	/// Installs a callback receiving the name and rectangle of each marked item, or clears it when
	/// null. Intended for test hosts.
	/// </summary>
	/// <param name="callback">The callback, or null to stop recording.</param>
	public static void SetProbe(Action<string, Vector2, Vector2>? callback) => probe = callback;

	private static readonly List<string> scopes = [];

	/// <summary>
	/// Pushes a name scope, so items marked until the matching <see cref="PopScope"/> are qualified
	/// by it. Mirrors ImGui's own identifier stack, which is what keeps two identically labelled
	/// widgets apart.
	/// </summary>
	/// <param name="scope">The scope name.</param>
	public static void PushScope(string scope) => scopes.Add(scope);

	/// <summary>Pops the most recently pushed name scope.</summary>
	public static void PopScope()
	{
		if (scopes.Count > 0)
		{
			scopes.RemoveAt(scopes.Count - 1);
		}
	}

	/// <summary>
	/// Builds the fully qualified name for an item: the ImGui window it is being drawn into, then any
	/// pushed scopes, then the item's own name, separated by forward slashes.
	/// </summary>
	/// <remarks>
	/// Qualification matters because a bare label is not unique. ImGui keeps two widgets with the same
	/// label apart through its identifier stack, and a probe name that ignored that context would
	/// collide exactly where ImGui does not. Tests do not have to write the whole path: a probe
	/// resolves a trailing portion of it as long as only one item matches.
	/// </remarks>
	/// <param name="name">The item's own name.</param>
	/// <returns>The qualified name.</returns>
	public static string Qualify(string name)
	{
		string window = CurrentWindowName();

		return scopes.Count == 0
			? string.IsNullOrEmpty(window) ? name : $"{window}/{name}"
			: string.IsNullOrEmpty(window)
				? $"{string.Join("/", scopes)}/{name}"
				: $"{window}/{string.Join("/", scopes)}/{name}";
	}

	private static unsafe string CurrentWindowName()
	{
		ImGuiWindowPtr window = ImGuiP.GetCurrentWindowRead();

		return window.Handle is null || window.Name is null
			? string.Empty
			: Marshal.PtrToStringUTF8((nint)window.Name) ?? string.Empty;
	}

	/// <summary>
	/// Records the most recently submitted ImGui item under a stable name. Call immediately after
	/// submitting the widget, while it is still the current item.
	/// </summary>
	/// <param name="name">A stable name for the item.</param>
	public static void MarkItem(string name)
	{
		if (!IsRecording)
		{
			return;
		}

		probe!(Qualify(name), ImGui.GetItemRectMin(), ImGui.GetItemRectMax());
	}

	/// <summary>
	/// Records the most recently submitted item under a name built from a prefix and a label, using
	/// the convention that a qualified name is separated by a forward slash.
	/// </summary>
	/// <param name="prefix">The owning component, for example a popup title or widget kind.</param>
	/// <param name="label">The item's own label or identifier.</param>
	public static void MarkItem(string prefix, string label)
	{
		if (!IsRecording)
		{
			return;
		}

		probe!(Qualify($"{prefix}/{label}"), ImGui.GetItemRectMin(), ImGui.GetItemRectMax());
	}

	/// <summary>
	/// Records an explicit rectangle under a name, for a component that knows its own bounds without
	/// them being the current ImGui item.
	/// </summary>
	/// <param name="name">A stable name for the region.</param>
	/// <param name="min">Top left corner in display pixels.</param>
	/// <param name="max">Bottom right corner in display pixels.</param>
	public static void MarkRegion(string name, Vector2 min, Vector2 max)
	{
		if (!IsRecording)
		{
			return;
		}

		probe!(Qualify(name), min, max);
	}
}
