// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Clipboard;

using System.Runtime.InteropServices;
using System.Runtime.Versioning;

/// <summary>The Win32 clipboard and global memory calls the Windows clipboard needs.</summary>
/// <remarks>Handles and pointers stay <see cref="nint"/> and are copied with <see cref="Marshal"/>, so nothing here is unsafe.</remarks>
[SupportedOSPlatform("windows")]
internal static partial class NativeMethods
{
	/// <summary><c>CF_DIB</c>: a <c>BITMAPINFO</c> followed by the pixels.</summary>
	public const uint DibFormat = 8;

	/// <summary><c>CF_DIBV5</c>: a <c>BITMAPV5HEADER</c> followed by the pixels.</summary>
	public const uint DibV5Format = 17;

	/// <summary><c>GMEM_MOVEABLE</c>, which memory handed to the clipboard has to be.</summary>
	public const uint Moveable = 0x0002;

	[LibraryImport("user32.dll", SetLastError = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static partial bool OpenClipboard(nint owner);

	[LibraryImport("user32.dll")]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static partial bool CloseClipboard();

	[LibraryImport("user32.dll")]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static partial bool EmptyClipboard();

	[LibraryImport("user32.dll")]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static partial bool IsClipboardFormatAvailable(uint format);

	[LibraryImport("user32.dll")]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	public static partial nint GetClipboardData(uint format);

	[LibraryImport("user32.dll")]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	public static partial nint SetClipboardData(uint format, nint memory);

	[LibraryImport("user32.dll", EntryPoint = "RegisterClipboardFormatW", StringMarshalling = StringMarshalling.Utf16)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	public static partial uint RegisterClipboardFormat(string name);

	[LibraryImport("kernel32.dll")]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	public static partial nint GlobalAlloc(uint flags, nuint bytes);

	[LibraryImport("kernel32.dll")]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	public static partial nint GlobalFree(nint memory);

	[LibraryImport("kernel32.dll")]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	public static partial nint GlobalLock(nint memory);

	[LibraryImport("kernel32.dll")]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static partial bool GlobalUnlock(nint memory);

	[LibraryImport("kernel32.dll")]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	public static partial nuint GlobalSize(nint memory);
}
