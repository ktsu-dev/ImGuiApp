// Copyright (c) 2023-2026 ktsu.dev contributors

namespace ktsu.ImGui.Popups.Tests;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using ktsu.Semantics.Paths;
using ktsu.Semantics.Strings;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class FilesystemBrowserDriveTests
{
	[TestMethod]
	public void CurrentDriveOf_ReturnsRootOfDirectory()
	{
		string root = Path.GetPathRoot(Path.GetTempPath())!;
		AbsoluteDirectoryPath directory = Path.Combine(Path.GetTempPath(), "some", "nested", "directory").As<AbsoluteDirectoryPath>();

		Assert.AreEqual(root, ImGuiPopups.FilesystemBrowser.CurrentDriveOf(directory, "fallback"));
	}

	[TestMethod]
	public void CurrentDriveOf_WithoutDeterminableRoot_ReturnsFallback()
	{
		AbsoluteDirectoryPath directory = string.Empty.As<AbsoluteDirectoryPath>();

		Assert.AreEqual("fallback", ImGuiPopups.FilesystemBrowser.CurrentDriveOf(directory, "fallback"));
	}

	[TestMethod]
	public void GetNavigableDrives_IsSortedAndDeduplicated()
	{
		List<string> drives = [.. ImGuiPopups.FilesystemBrowser.GetNavigableDrives()];

		CollectionAssert.AreEqual(drives.Distinct(StringComparer.OrdinalIgnoreCase).ToList(), drives, "Drives should be deduplicated.");
		CollectionAssert.AreEqual(drives.OrderBy(d => d, StringComparer.OrdinalIgnoreCase).ToList(), drives, "Drives should be sorted.");
	}

	[TestMethod]
	public void GetNavigableDrives_IncludesRootOfCurrentDirectory()
	{
		string root = Path.GetPathRoot(Environment.CurrentDirectory)!;

		List<string> drives = [.. ImGuiPopups.FilesystemBrowser.GetNavigableDrives()];

		Assert.IsTrue(
			drives.Contains(root, StringComparer.OrdinalIgnoreCase),
			$"Expected the root of the current directory ('{root}') among [{string.Join(", ", drives)}].");
	}

	[TestMethod]
	[TestCategory("OS-Specific")]
	public void IsNavigableDrive_OnUnix_ExcludesPseudoFilesystems()
	{
		if (OperatingSystem.IsWindows())
		{
			Assert.Inconclusive("Unix-only mount point filtering.");
			return;
		}

		Assert.IsFalse(ImGuiPopups.FilesystemBrowser.IsNavigableDrive("/proc"));
		Assert.IsFalse(ImGuiPopups.FilesystemBrowser.IsNavigableDrive("/proc/sys/fs/binfmt_misc"));
		Assert.IsFalse(ImGuiPopups.FilesystemBrowser.IsNavigableDrive("/sys/kernel/debug"));
		Assert.IsFalse(ImGuiPopups.FilesystemBrowser.IsNavigableDrive("/dev/shm"));
		Assert.IsFalse(ImGuiPopups.FilesystemBrowser.IsNavigableDrive("/run/user/1000/gvfs"));
	}

	[TestMethod]
	[TestCategory("OS-Specific")]
	public void IsNavigableDrive_OnUnix_KeepsRealStorage()
	{
		if (OperatingSystem.IsWindows())
		{
			Assert.Inconclusive("Unix-only mount point filtering.");
			return;
		}

		Assert.IsTrue(ImGuiPopups.FilesystemBrowser.IsNavigableDrive("/"));
		Assert.IsTrue(ImGuiPopups.FilesystemBrowser.IsNavigableDrive("/home"));
		Assert.IsTrue(ImGuiPopups.FilesystemBrowser.IsNavigableDrive("/tmp"));
		Assert.IsTrue(ImGuiPopups.FilesystemBrowser.IsNavigableDrive("/mnt/data"));
		Assert.IsTrue(ImGuiPopups.FilesystemBrowser.IsNavigableDrive("/media/usb"));
	}

	[TestMethod]
	[TestCategory("OS-Specific")]
	public void IsNavigableDrive_OnUnix_KeepsRemovableMediaUnderRun()
	{
		if (OperatingSystem.IsWindows())
		{
			Assert.Inconclusive("Unix-only mount point filtering.");
			return;
		}

		Assert.IsTrue(ImGuiPopups.FilesystemBrowser.IsNavigableDrive("/run/media/user/USB"));
	}

	[TestMethod]
	[TestCategory("OS-Specific")]
	public void IsNavigableDrive_OnWindows_KeepsEveryDrive()
	{
		if (!OperatingSystem.IsWindows())
		{
			Assert.Inconclusive("Windows-only drive handling.");
			return;
		}

		Assert.IsTrue(ImGuiPopups.FilesystemBrowser.IsNavigableDrive(@"C:\"));
		Assert.IsTrue(ImGuiPopups.FilesystemBrowser.IsNavigableDrive(@"Z:\"));
	}
}
