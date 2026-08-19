// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Popups;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json.Serialization;
using Hexa.NET.ImGui;
using ktsu.Extensions;
using ktsu.Semantics.Paths;
using ktsu.Semantics.Strings;
using Microsoft.Extensions.FileSystemGlobbing;

/// <summary>
/// Partial class containing various ImGui popup implementations.
/// </summary>
public partial class ImGuiPopups
{
	/// <summary>
	/// Defines the mode of operation for the filesystem browser.
	/// </summary>
	public enum FilesystemBrowserMode
	{
		/// <summary>
		/// Browser operates in file/directory open mode, allowing selection of existing items.
		/// </summary>
		Open,

		/// <summary>
		/// Browser operates in save mode, allowing selection of existing items or input of new filenames.
		/// </summary>
		Save
	}

	/// <summary>
	/// Defines the target type for the filesystem browser.
	/// </summary>
	public enum FilesystemBrowserTarget
	{
		/// <summary>
		/// Target is a file.
		/// </summary>
		File,

		/// <summary>
		/// Target is a directory.
		/// </summary>
		Directory
	}

	/// <summary>
	/// A class for displaying a filesystem browser popup window.
	/// </summary>
	public class FilesystemBrowser
	{
		/// <summary>
		/// Mount point prefixes that are hidden from the drive list on Unix-like systems.
		/// </summary>
		/// <remarks>
		/// <see cref="Environment.GetLogicalDrives"/> reports every mount point on Unix, so the raw list is
		/// dominated by kernel pseudo filesystems (<c>/proc</c>, <c>/sys</c>, <c>/dev</c>) and runtime state
		/// (<c>/run</c>) that hold nothing a user would browse to. Removable media is commonly mounted under
		/// <c>/run/media</c>, so that subtree is kept — see <see cref="RemovableMediaPrefix"/>.
		/// </remarks>
		private static readonly string[] HiddenUnixMountPrefixes = ["/proc", "/sys", "/dev", "/run"];

		/// <summary>
		/// The mount point prefix for removable media, kept even though it sits under a hidden prefix.
		/// </summary>
		private const string RemovableMediaPrefix = "/run/media";

		/// <summary>
		/// Gets or sets the mode of the browser (Open or Save).
		/// </summary>
		private FilesystemBrowserMode BrowserMode { get; set; }

		/// <summary>
		/// Gets or sets the target type of the browser (File or Directory).
		/// </summary>
		private FilesystemBrowserTarget BrowserTarget { get; set; }

		/// <summary>
		/// Action to invoke when a file is chosen.
		/// </summary>
		private Action<AbsoluteFilePath> OnChooseFile { get; set; } = (f) => { };

		/// <summary>
		/// Action to invoke when a directory is chosen.
		/// </summary>
		private Action<AbsoluteDirectoryPath> OnChooseDirectory { get; set; } = (d) => { };

		/// <summary>
		/// Gets or sets the current directory being displayed.
		/// </summary>
		[JsonInclude]
		private AbsoluteDirectoryPath CurrentDirectory { get; set; } = Environment.CurrentDirectory.As<AbsoluteDirectoryPath>();

		/// <summary>
		/// Collection of current contents (files and directories) in the current directory.
		/// </summary>
		private Collection<IAbsolutePath> CurrentContents { get; set; } = [];

		/// <summary>
		/// The currently selected item.
		/// </summary>
		private IAbsolutePath ChosenItem { get; set; } = AbsolutePath.Create("");

		/// <summary>
		/// Collection of logical drives available.
		/// </summary>
		private Collection<string> Drives { get; set; } = [];

		/// <summary>
		/// The glob pattern used for filtering files.
		/// </summary>
		private string Glob { get; set; } = "*";

		/// <summary>
		/// Matcher used for file globbing.
		/// </summary>
		private Matcher Matcher { get; set; } = new();

		/// <summary>
		/// The filename entered by the user.
		/// </summary>
		private FileName FileName { get; set; } = new();

		/// <summary>
		/// The modal instance for displaying the browser popup.
		/// </summary>
		private Modal Modal { get; } = new();

		/// <summary>
		/// The popup message for displaying alerts.
		/// </summary>
		private MessageOK PopupMessageOK { get; } = new();

		/// <summary>
		/// Opens the file open dialog with the specified title and callback.
		/// </summary>
		/// <param name="title">The title of the dialog.</param>
		/// <param name="onChooseFile">Callback invoked when a file is chosen.</param>
		/// <param name="glob">Glob pattern for filtering files. Several patterns may be separated by semicolons, for example <c>*.png;*.jpg</c>.</param>
		public void FileOpen(string title, Action<AbsoluteFilePath> onChooseFile, string glob = "*") => FileOpen(title, onChooseFile, customSize: Vector2.Zero, glob);

		/// <summary>
		/// Opens the file open dialog with the specified title, callback, and custom size.
		/// </summary>
		/// <param name="title">The title of the dialog.</param>
		/// <param name="onChooseFile">Callback invoked when a file is chosen.</param>
		/// <param name="customSize">Custom size of the dialog.</param>
		/// <param name="glob">Glob pattern for filtering files. Several patterns may be separated by semicolons, for example <c>*.png;*.jpg</c>.</param>
		public void FileOpen(string title, Action<AbsoluteFilePath> onChooseFile, Vector2 customSize, string glob = "*") => File(title, FilesystemBrowserMode.Open, onChooseFile, customSize, glob);

		/// <summary>
		/// Opens the file save dialog with the specified title and callback.
		/// </summary>
		/// <param name="title">The title of the dialog.</param>
		/// <param name="onChooseFile">Callback invoked when a file is chosen.</param>
		/// <param name="glob">Glob pattern for filtering files. Several patterns may be separated by semicolons, for example <c>*.png;*.jpg</c>.</param>
		public void FileSave(string title, Action<AbsoluteFilePath> onChooseFile, string glob = "*") => FileSave(title, onChooseFile, customSize: Vector2.Zero, glob);

		/// <summary>
		/// Opens the file save dialog with the specified title, callback, and custom size.
		/// </summary>
		/// <param name="title">The title of the dialog.</param>
		/// <param name="onChooseFile">Callback invoked when a file is chosen.</param>
		/// <param name="customSize">Custom size of the dialog.</param>
		/// <param name="glob">Glob pattern for filtering files. Several patterns may be separated by semicolons, for example <c>*.png;*.jpg</c>.</param>
		public void FileSave(string title, Action<AbsoluteFilePath> onChooseFile, Vector2 customSize, string glob = "*") => File(title, FilesystemBrowserMode.Save, onChooseFile, customSize, glob);

		/// <summary>
		/// Opens the filesystem browser popup with the specified parameters.
		/// </summary>
		/// <param name="title">The title of the popup.</param>
		/// <param name="mode">The mode of the browser (Open or Save).</param>
		/// <param name="onChooseFile">Callback for when a file is chosen.</param>
		/// <param name="customSize">Custom size of the popup.</param>
		/// <param name="glob">Glob pattern for filtering files. Several patterns may be separated by semicolons, for example <c>*.png;*.jpg</c>.</param>
		private void File(string title, FilesystemBrowserMode mode, Action<AbsoluteFilePath> onChooseFile, Vector2 customSize, string glob) => OpenPopup(title, mode, FilesystemBrowserTarget.File, onChooseFile, (d) => { }, customSize, glob);

		/// <summary>
		/// Opens the directory chooser dialog with the specified title and callback.
		/// </summary>
		/// <param name="title">The title of the dialog.</param>
		/// <param name="onChooseDirectory">Callback invoked when a directory is chosen.</param>
		public void ChooseDirectory(string title, Action<AbsoluteDirectoryPath> onChooseDirectory) => ChooseDirectory(title, onChooseDirectory, customSize: Vector2.Zero);

		/// <summary>
		/// Opens the directory chooser dialog with the specified title, callback, and custom size.
		/// </summary>
		/// <param name="title">The title of the dialog.</param>
		/// <param name="onChooseDirectory">Callback invoked when a directory is chosen.</param>
		/// <param name="customSize">Custom size of the dialog.</param>
		public void ChooseDirectory(string title, Action<AbsoluteDirectoryPath> onChooseDirectory, Vector2 customSize) => OpenPopup(title, FilesystemBrowserMode.Open, FilesystemBrowserTarget.Directory, (d) => { }, onChooseDirectory, customSize, "*");

		/// <summary>
		/// Opens the filesystem browser popup with the specified parameters.
		/// </summary>
		/// <param name="title">The title of the popup.</param>
		/// <param name="mode">The mode of the browser (Open or Save).</param>
		/// <param name="target">The target type (File or Directory).</param>
		/// <param name="onChooseFile">Callback for when a file is chosen.</param>
		/// <param name="onChooseDirectory">Callback for when a directory is chosen.</param>
		/// <param name="customSize">Custom size of the popup.</param>
		/// <param name="glob">Glob pattern for filtering files. Several patterns may be separated by semicolons, for example <c>*.png;*.jpg</c>.</param>
		private void OpenPopup(string title, FilesystemBrowserMode mode, FilesystemBrowserTarget target, Action<AbsoluteFilePath> onChooseFile, Action<AbsoluteDirectoryPath> onChooseDirectory, Vector2 customSize, string glob)
		{
			FileName = new();
			BrowserMode = mode;
			BrowserTarget = target;
			OnChooseFile = onChooseFile;
			OnChooseDirectory = onChooseDirectory;
			Glob = glob;
			Matcher = BuildMatcher(glob);
			Drives.Clear();
			GetNavigableDrives().ForEach(Drives.Add);
			RefreshContents();
			Modal.Open(title, ShowContent, customSize);
		}

		/// <summary>
		/// Builds a matcher from a glob string. Several patterns may be supplied separated by
		/// semicolons, for example <c>*.png;*.jpg</c>. The underlying globbing library treats a
		/// whole string as a single pattern and has no separator of its own, so each pattern has
		/// to be added as its own include or the combined string matches nothing at all.
		/// </summary>
		/// <param name="glob">Glob pattern, or several separated by semicolons.</param>
		/// <returns>A matcher including every pattern supplied.</returns>
		internal static Matcher BuildMatcher(string glob)
		{
			Matcher matcher = new();
			foreach (string pattern in glob.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
			{
				matcher.AddInclude(pattern);
			}

			return matcher;
		}

		/// <summary>
		/// Displays the content of the filesystem browser popup.
		/// </summary>
		private void ShowContent()
		{
			DrawDrivesCombo();

			ImGui.TextUnformatted($"{CurrentDirectory}{Path.DirectorySeparatorChar}{Glob}");
			DrawFileTable();

			if (BrowserMode == FilesystemBrowserMode.Save)
			{
				string fileName = FileName;
				ImGui.InputText("##SaveAs", ref fileName, 256);
				FileName = fileName.As<FileName>();
			}

			DrawConfirmButtons();

			PopupMessageOK.ShowIfOpen();
		}

		/// <summary>
		/// Draws the drive selection combo box for switching between logical drives.
		/// </summary>
		private void DrawDrivesCombo()
		{
			if (Drives.Count == 0)
			{
				return;
			}

			string currentDrive = CurrentDriveOf(CurrentDirectory, Drives[0]);

			if (ImGui.BeginCombo("##Drives", currentDrive))
			{
#pragma warning disable S3267 // Explicit loop is clearer; LINQ rewrite would add unnecessary allocation and complicate ImGui immediate-mode usage.
				foreach (string drive in Drives)
				{
					if (ImGui.Selectable(drive, string.Equals(drive, currentDrive, StringComparison.OrdinalIgnoreCase)))
					{
						CurrentDirectory = drive.As<AbsoluteDirectoryPath>();
						RefreshContents();
					}
				}
#pragma warning restore S3267

				ImGui.EndCombo();
			}
		}

		/// <summary>
		/// Gets the logical drives worth offering in the drive combo, deduplicated and ordered.
		/// </summary>
		/// <returns>The drives to display.</returns>
		internal static IEnumerable<string> GetNavigableDrives() =>
			Environment.GetLogicalDrives()
				.Where(IsNavigableDrive)
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.OrderBy(drive => drive, StringComparer.OrdinalIgnoreCase);

		/// <summary>
		/// Determines whether a logical drive is user-navigable storage rather than a pseudo filesystem.
		/// </summary>
		/// <param name="drive">The drive reported by <see cref="Environment.GetLogicalDrives"/>.</param>
		/// <returns><see langword="true"/> if the drive should be listed; otherwise, <see langword="false"/>.</returns>
		internal static bool IsNavigableDrive(string drive)
		{
			if (OperatingSystem.IsWindows())
			{
				return true;
			}

			return drive.StartsWith(RemovableMediaPrefix, StringComparison.Ordinal)
				|| !HiddenUnixMountPrefixes.Any(prefix => drive.StartsWith(prefix, StringComparison.Ordinal));
		}

		/// <summary>
		/// Gets the root of the given directory in the same form <see cref="Environment.GetLogicalDrives"/> reports,
		/// so it can be matched against <see cref="Drives"/>.
		/// </summary>
		/// <param name="directory">The directory to take the root of.</param>
		/// <param name="fallback">The value to return when the directory has no determinable root.</param>
		/// <returns>The directory's root, or <paramref name="fallback"/> when the root cannot be determined.</returns>
		internal static string CurrentDriveOf(AbsoluteDirectoryPath directory, string fallback)
		{
			string? root = Path.GetPathRoot(directory.WeakString);
			return string.IsNullOrEmpty(root) ? fallback : root;
		}

		/// <summary>
		/// Draws the table listing the parent directory entry and the current directory contents.
		/// </summary>
		private void DrawFileTable()
		{
			if (ImGui.BeginChild("FilesystemBrowser", new(500, 400), ImGuiChildFlags.None) && ImGui.BeginTable(nameof(FilesystemBrowser), 1, ImGuiTableFlags.Borders))
			{
				ImGui.TableSetupColumn("Path", ImGuiTableColumnFlags.WidthStretch, 40);
				ImGui.TableHeadersRow();

				ImGuiSelectableFlags flags = ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowDoubleClick | ImGuiSelectableFlags.NoAutoClosePopups;
				DrawParentDirectoryRow(flags);

				foreach (IAbsolutePath path in SortContents(CurrentContents))
				{
					DrawContentRow(path, flags);
				}

				ImGui.EndTable();
			}

			ImGui.EndChild();
		}

		/// <summary>
		/// Orders filesystem entries for display, listing directories before files and sorting each group by name.
		/// </summary>
		/// <param name="contents">The filesystem entries to order.</param>
		/// <returns>The ordered entries.</returns>
		/// <remarks>
		/// The secondary sort key is the entry's string value rather than the entry itself. Sorting by the
		/// entry makes LINQ fall back to <see cref="Comparer{T}.Default"/>, because <see cref="IAbsolutePath"/>
		/// implements neither <see cref="IComparable{T}"/> of itself nor of <see cref="IPath"/> — the
		/// <see cref="IComparable{T}"/> that ktsu.Semantics.Paths declares on <see cref="IPath"/> only helps
		/// collections typed as <see cref="IPath"/> exactly. Every comparison would therefore box through the
		/// non-generic <see cref="IComparable.CompareTo(object)"/>, which additionally threw
		/// <see cref="ArgumentException"/> before ktsu.Semantics.Strings 2.7.11 (ktsu-dev/ImGuiApp#273).
		/// Sorting on the string value also gives case-insensitive display order.
		/// </remarks>
		internal static Collection<IAbsolutePath> SortContents(IEnumerable<IAbsolutePath> contents) =>
			contents
				.OrderBy(p => p is not AbsoluteDirectoryPath)
				.ThenBy(p => p.WeakString, StringComparer.OrdinalIgnoreCase)
				.ThenBy(p => p.WeakString, StringComparer.Ordinal)
				.ToCollection();

		/// <summary>
		/// Draws the ".." row that navigates to the parent directory.
		/// </summary>
		/// <param name="flags">The selectable flags to apply to the row.</param>
		private void DrawParentDirectoryRow(ImGuiSelectableFlags flags)
		{
			ImGui.TableNextRow();
			ImGui.TableNextColumn();
			if (ImGui.Selectable("..", false, flags) && ImGui.IsMouseDoubleClicked(0))
			{
				// Take the parent from the path type rather than trimming separators off the string: trimming
				// also removes the leading separator, which is the root itself on Unix, so the "parent" came
				// back relative and failed AbsoluteDirectoryPath validation (ktsu-dev/ImGuiApp#281). A root is
				// its own parent, so this stays put once there is nowhere left to go.
				CurrentDirectory = CurrentDirectory.Parent;
				RefreshContents();
			}
		}

		/// <summary>
		/// Draws a single filesystem entry row and handles its selection.
		/// </summary>
		/// <param name="path">The path represented by the row.</param>
		/// <param name="flags">The selectable flags to apply to the row.</param>
		private void DrawContentRow(IAbsolutePath path, ImGuiSelectableFlags flags)
		{
			ImGui.TableNextRow();
			ImGui.TableNextColumn();
			AbsoluteDirectoryPath? directory = path as AbsoluteDirectoryPath;
			AbsoluteFilePath? file = path as AbsoluteFilePath;
			string displayPath = path.WeakString.RemovePrefix(CurrentDirectory.WeakString).Trim(Path.DirectorySeparatorChar);

			if (directory is not null)
			{
				displayPath += Path.DirectorySeparatorChar;
			}

			if (ImGui.Selectable(displayPath, ChosenItem == path, flags))
			{
				if (directory is not null)
				{
					ChosenItem = directory;
					if (ImGui.IsMouseDoubleClicked(0))
					{
						CurrentDirectory = directory;
						RefreshContents();
					}
				}
				else if (file is not null)
				{
					ChosenItem = file;
					FileName = file.FileName;
					if (ImGui.IsMouseDoubleClicked(0))
					{
						ChooseItem();
					}
				}
			}
		}

		/// <summary>
		/// Draws the confirm and cancel buttons for the browser.
		/// </summary>
		private void DrawConfirmButtons()
		{
			string confirmText = BrowserMode switch
			{
				FilesystemBrowserMode.Open => "Open",
				FilesystemBrowserMode.Save => "Save",
				_ => "Choose"
			};
			if (ImGui.Button(confirmText))
			{
				ChooseItem();
			}

			ImGui.SameLine();
			if (ImGui.Button("Cancel"))
			{
				ImGui.CloseCurrentPopup();
			}
		}

		/// <summary>
		/// Handles the selection of an item (file or directory) based on the current target.
		/// </summary>
		private void ChooseItem()
		{
			if (BrowserTarget == FilesystemBrowserTarget.File)
			{
				AbsoluteFilePath chosenFile = CurrentDirectory / FileName;
				if (!Matcher.Match(FileName).HasMatches)
				{
					PopupMessageOK.Open("Invalid File Name", "The file name does not match the glob pattern.");
					return;
				}

				OnChooseFile(Path.GetFullPath(chosenFile).As<AbsoluteFilePath>());
			}
			else if (BrowserTarget == FilesystemBrowserTarget.Directory && ChosenItem is AbsoluteDirectoryPath directory)
			{
				OnChooseDirectory(Path.GetFullPath(directory).As<AbsoluteDirectoryPath>());
			}

			ImGui.CloseCurrentPopup();
		}

		/// <summary>
		/// Refreshes the contents of the current directory based on the target and glob pattern.
		/// </summary>
		private void RefreshContents()
		{
			ChosenItem = AbsolutePath.Create("");
			CurrentContents.Clear();
			CurrentDirectory.GetContents().ForEach(p =>
			{
				switch (p)
				{
					case AbsoluteDirectoryPath directory:
						CurrentContents.Add(directory);
						break;

					case AbsoluteFilePath file when BrowserTarget == FilesystemBrowserTarget.File && Matcher.Match(file.FileName).HasMatches:
						CurrentContents.Add(file);
						break;

					default:
						break;
				}
			});
		}

		/// <summary>
		/// Shows the modal popup if it is open.
		/// </summary>
		/// <returns>True if the modal is open; otherwise, false.</returns>
		public bool ShowIfOpen() => Modal.ShowIfOpen();
	}
}
