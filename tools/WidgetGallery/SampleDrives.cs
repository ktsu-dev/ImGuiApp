// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.WidgetGallery;

using System.Diagnostics.CodeAnalysis;
using System.Reflection;

using Hexa.NET.ImGui.Widgets.Dialogs;

/// <summary>
/// Stands the sample folder in for the machine's drives in Hexa's file tree, for the length of one capture.
/// </summary>
/// <remarks>
/// The file tree and the file pickers list every mounted drive under "Computer", read from the
/// machine the gallery runs on. That put the build runner's mount points into committed images,
/// different on every runner image. Hexa keeps the list in a private static field it fills once, and
/// <see cref="FileSystemHelper.ClearCache"/> is its own way of reading the real drives again, so the
/// substitution is undone by the library rather than by restoring a copy. If a later Hexa renames
/// the field, nothing is substituted and the tiles show the machine's drives, as they did before.
/// </remarks>
internal static class SampleDrives
{
	/// <summary>The name the sample folder is listed under, standing for a drive.</summary>
	public const string DriveName = "home";

	/// <summary>Lists the sample folder as the only drive.</summary>
	[SuppressMessage("Major Code Smell", "S3011:Reflection should not be used to increase accessibility of classes, methods, or fields", Justification = "Hexa exposes no way to set the drive list, and the gallery must not photograph the build machine's mounts.")]
	public static void Use()
	{
		FieldInfo? drives = typeof(FileSystemHelper).GetField("logicalDrives", BindingFlags.NonPublic | BindingFlags.Static);
		drives?.SetValue(null, new[] { new FileSystemItem(SampleFolder.Root, string.Empty, DriveName, FileSystemItemFlags.Folder) });
	}

	/// <summary>Puts the machine's drives back.</summary>
	public static void Restore() => FileSystemHelper.ClearCache();
}
