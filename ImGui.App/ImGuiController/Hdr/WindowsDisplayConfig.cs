// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.ImGuiController.Hdr;

using System;
using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

/// <summary>
/// Reads the SDR white level Windows shows a screen's SDR content at, which the HDR settings call
/// "SDR content brightness", through the display configuration API.
/// </summary>
/// <remarks>
/// DXGI reports a screen's peak but not this, and it is what ImGui's interface and every SDR window
/// beside this one are shown at, so the window places its own white there too.
/// </remarks>
internal static partial class WindowsDisplayConfig
{
	private const uint OnlyActivePaths = 2;
	private const int PathSize = 72;
	private const int ModeSize = 64;
	private const int GetSourceName = 1;
	private const int GetSdrWhiteLevel = 11;
	private const int HeaderSize = 20;
	private const int SourceNameSize = HeaderSize + 64;
	private const int SdrWhiteSize = HeaderSize + 4;

	/// <summary>Finds the SDR white level of the screen a GDI device name, such as <c>\\.\DISPLAY1</c>, names.</summary>
	/// <param name="gdiDeviceName">The device name, as DXGI's output description gives it.</param>
	/// <returns>The SDR white in nits, or null when Windows doesn't say.</returns>
	[SupportedOSPlatform("windows")]
	[ExcludeFromCodeCoverage(Justification = "Calls the Windows display configuration API, which the test hosts do not have; the packets it builds and reads are tested through Parse.")]
	public static double? SdrWhiteNits(string gdiDeviceName)
	{
		if (GetDisplayConfigBufferSizes(OnlyActivePaths, out uint pathCount, out uint modeCount) != 0)
		{
			return null;
		}

		byte[] paths = new byte[pathCount * PathSize];
		byte[] modes = new byte[modeCount * ModeSize];
		if (QueryDisplayConfig(OnlyActivePaths, ref pathCount, paths, ref modeCount, modes, 0) != 0)
		{
			return null;
		}

		for (int path = 0; path < pathCount; path++)
		{
			ReadOnlySpan<byte> entry = paths.AsSpan(path * PathSize, PathSize);
			byte[] source = Packet(GetSourceName, SourceNameSize, entry[..8], BinaryPrimitives.ReadUInt32LittleEndian(entry[8..]));
			if (DisplayConfigGetDeviceInfo(source) != 0 || !string.Equals(SourceName(source), gdiDeviceName, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}

			byte[] white = Packet(GetSdrWhiteLevel, SdrWhiteSize, entry[20..28], BinaryPrimitives.ReadUInt32LittleEndian(entry[28..]));
			return DisplayConfigGetDeviceInfo(white) == 0 ? WhiteNits(white) : null;
		}

		return null;
	}

	/// <summary>Builds a request packet: its header, then room for the answer.</summary>
	/// <param name="type">What is asked for.</param>
	/// <param name="size">The packet's size.</param>
	/// <param name="adapter">The adapter's LUID, eight bytes.</param>
	/// <param name="id">The source or target id.</param>
	/// <returns>The packet.</returns>
	internal static byte[] Packet(int type, int size, ReadOnlySpan<byte> adapter, uint id)
	{
		byte[] packet = new byte[size];
		BinaryPrimitives.WriteInt32LittleEndian(packet, type);
		BinaryPrimitives.WriteInt32LittleEndian(packet.AsSpan(4), size);
		adapter.CopyTo(packet.AsSpan(8, 8));
		BinaryPrimitives.WriteUInt32LittleEndian(packet.AsSpan(16), id);
		return packet;
	}

	/// <summary>Reads the GDI device name from an answered source name packet.</summary>
	/// <param name="packet">The packet.</param>
	/// <returns>The name.</returns>
	internal static string SourceName(byte[] packet)
	{
		string name = Encoding.Unicode.GetString(packet, HeaderSize, 64);
		int end = name.IndexOf('\0', StringComparison.Ordinal);
		return end < 0 ? name : name[..end];
	}

	/// <summary>Reads the SDR white from an answered white level packet: thousandths of 80 nits.</summary>
	/// <param name="packet">The packet.</param>
	/// <returns>The SDR white in nits.</returns>
	internal static double WhiteNits(byte[] packet) =>
		BinaryPrimitives.ReadUInt32LittleEndian(packet.AsSpan(HeaderSize)) / 1000.0 * HdrDisplayInfo.ReferenceWhiteNits;

	[LibraryImport("user32.dll")]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	private static partial int GetDisplayConfigBufferSizes(uint flags, out uint pathCount, out uint modeCount);

	[LibraryImport("user32.dll")]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	private static partial int QueryDisplayConfig(uint flags, ref uint pathCount, [Out] byte[] paths, ref uint modeCount, [Out] byte[] modes, nint topology);

	[LibraryImport("user32.dll")]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	private static partial int DisplayConfigGetDeviceInfo([In, Out] byte[] packet);
}
