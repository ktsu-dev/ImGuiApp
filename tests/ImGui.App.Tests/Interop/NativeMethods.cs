// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Tests.Interop;

using System.Runtime.InteropServices;

/// <summary>
/// Native calls the tests need for themselves. In a namespace of their own so this class does not
/// shadow the library's <c>NativeMethods</c>, which other tests inspect by that name.
/// </summary>
internal static partial class NativeMethods
{
	/// <summary>The desktop window, which is always visible on Windows, even on a headless agent.</summary>
	[LibraryImport("user32.dll")]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	internal static partial nint GetDesktopWindow();
}
