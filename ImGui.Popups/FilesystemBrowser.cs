// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGuiPopups;

using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json.Serialization;

using Hexa.NET.ImGui;

using ktsu.Extensions;
using ktsu.StrongPaths;

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
		private AbsoluteDirectoryPath CurrentDirectory { get; set; } = (AbsoluteDirectoryPath)Environment.CurrentDirectory;

		/// <summary>
		/// Collection of current contents (files and directories) in the current directory.
		/// </summary>
		private Collection<AnyAbsolutePath> CurrentContents { get; set; } = [];

		/// <summary>
		/// The currently selected item.
		/// </summary>
		private AnyAbsolutePath ChosenItem { get; set; } = new();

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
		/// <param name="glob">Glob pattern for filtering files.</param>
		public void FileOpen(string title, Action<AbsoluteFilePath> onChooseFile, string glob = "*") => FileOpen(title, onChooseFile, customSize: Vector2.Zero, glob);

		/// <summary>
		/// Opens the file open dialog with the specified title, callback, and custom size.
		/// </summary>
		/// <param name="title">The title of the dialog.</param>
		/// <param name="onChooseFile">Callback invoked when a file is chosen.</param>
		/// <param name="customSize">Custom size of the dialog.</param>
		/// <param name="glob">Glob pattern for filtering files.</param>
		public void FileOpen(string title, Action<AbsoluteFilePath> onChooseFile, Vector2 customSize, string glob = "*") => File(title, FilesystemBrowserMode.Open, onChooseFile, customSize, glob);

		/// <summary>
		/// Opens the file save dialog with the specified title and callback.
		/// </summary>
		/// <param name="title">The title of the dialog.</param>
		/// <param name="onChooseFile">Callback invoked when a file is chosen.</param>
		/// <param name="glob">Glob pattern for filtering files.</param>
		public void FileSave(string title, Action<AbsoluteFilePath> onChooseFile, string glob = "*") => FileSave(title, onChooseFile, customSize: Vector2.Zero, glob);

		/// <summary>
		/// Opens the file save dialog with the specified title, callback, and custom size.
		/// </summary>
		/// <param name="title">The title of the dialog.</param>
		/// <param name="onChooseFile">Callback invoked when a file is chosen.</param>
		/// <param name="customSize">Custom size of the dialog.</param>
		/// <param name="glob">Glob pattern for filtering files.</param>
		public void FileSave(string title, Action<AbsoluteFilePath> onChooseFile, Vector2 customSize, string glob = "*") => File(title, FilesystemBrowserMode.Save, onChooseFile, customSize, glob);

		/// <summary>
		/// Opens the filesystem browser popup with the specified parameters.
		/// </summary>
		/// <param name="title">The title of the popup.</param>
		/// <param name="mode">The mode of the browser (Open or Save).</param>
		/// <param name="onChooseFile">Callback for when a file is chosen.</param>
		/// <param name="customSize">Custom size of the popup.</param>
		/// <param name="glob">Glob pattern for filtering files.</param>
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
		/// <param name="glob">Glob pattern for filtering files.</param>
		private void OpenPopup(string title, FilesystemBrowserMode mode, FilesystemBrowserTarget target, Action<AbsoluteFilePath> onChooseFile, Action<AbsoluteDirectoryPath> onChooseDirectory, Vector2 customSize, string glob)
		{
			FileName = new();
			BrowserMode = mode;
			BrowserTarget = target;
			OnChooseFile = onChooseFile;
			OnChooseDirectory = onChooseDirectory;
			Glob = glob;
			Matcher = new();
			Matcher.AddInclude(Glob);
			Drives.Clear();
			Environment.GetLogicalDrives().ForEach(Drives.Add);
			RefreshContents();
			Modal.Open(title, ShowContent, customSize);
		}

		/// <summary>
		/// Displays the content of the filesystem browser popup.
		/// </summary>
		private void ShowContent()
		{
			if (Drives.Count != 0)
			{
				if (ImGui.BeginCombo("##Drives", Drives[0]))
				{
					string currentDrive = CurrentDirectory.Split(Path.VolumeSeparatorChar).First() + Path.VolumeSeparatorChar + Path.DirectorySeparatorChar;
					foreach (string drive in Drives)
					{
						if (ImGui.Selectable(drive, drive == currentDrive))
						{
							CurrentDirectory = (AbsoluteDirectoryPath)drive;
							RefreshContents();
						}
					}

					ImGui.EndCombo();
				}
			}

			ImGui.TextUnformatted($"{CurrentDirectory}{Path.DirectorySeparatorChar}{Glob}");
			if (ImGui.BeginChild("FilesystemBrowser", new(500, 400), ImGuiChildFlags.None))
			{
				if (ImGui.BeginTable(nameof(FilesystemBrowser), 1, ImGuiTableFlags.Borders))
				{
					ImGui.TableSetupColumn("Path", ImGuiTableColumnFlags.WidthStretch, 40);
					//ImGui.TableSetupColumn("Size", ImGuiTableColumnFlags.None, 3);
					//ImGui.TableSetupColumn("Modified", ImGuiTableColumnFlags.None, 3);
					ImGui.TableHeadersRow();

					ImGuiSelectableFlags flags = ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowDoubleClick | ImGuiSelectableFlags.NoAutoClosePopups;
					ImGui.TableNextRow();
					ImGui.TableNextColumn();
					if (ImGui.Selectable("..", false, flags))
					{
						if (ImGui.IsMouseDoubleClicked(0))
						{
							string? newPath = Path.GetDirectoryName(CurrentDirectory.WeakString.Trim(Path.DirectorySeparatorChar));
							if (newPath is not null)
							{
								CurrentDirectory = (AbsoluteDirectoryPath)newPath;
								RefreshContents();
							}
						}
					}

					foreach (AnyAbsolutePath? path in CurrentContents.OrderBy(p => p is not AbsoluteDirectoryPath).ThenBy(p => p).ToCollection())
					{
						ImGui.TableNextRow();
						ImGui.TableNextColumn();
						AbsoluteDirectoryPath? directory = path as AbsoluteDirectoryPath;
						AbsoluteFilePath? file = path as AbsoluteFilePath;
						string displayPath = path.WeakString;
						displayPath = displayPath.RemovePrefix(CurrentDirectory).Trim(Path.DirectorySeparatorChar);

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

					ImGui.EndTable();
				}
			}

			ImGui.EndChild();

			if (BrowserMode == FilesystemBrowserMode.Save)
			{
				string fileName = FileName;
				ImGui.InputText("##SaveAs", ref fileName, 256);
				FileName = (FileName)fileName;
			}

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

			PopupMessageOK.ShowIfOpen();
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

				OnChooseFile((AbsoluteFilePath)Path.GetFullPath(chosenFile));
			}
			else if (BrowserTarget == FilesystemBrowserTarget.Directory && ChosenItem is AbsoluteDirectoryPath directory)
			{
				OnChooseDirectory((AbsoluteDirectoryPath)Path.GetFullPath(directory));
			}

			ImGui.CloseCurrentPopup();
		}

		/// <summary>
		/// Refreshes the contents of the current directory based on the target and glob pattern.
		/// </summary>
		private void RefreshContents()
		{
			ChosenItem = new();
			CurrentContents.Clear();
			CurrentDirectory.Contents.ForEach(p =>
			{
				if (BrowserTarget == FilesystemBrowserTarget.File || (BrowserTarget == FilesystemBrowserTarget.Directory && p is AbsoluteDirectoryPath))
				{
					if (p is AbsoluteDirectoryPath || Matcher.Match(Path.GetFileName(p)).HasMatches)
					{
						CurrentContents.Add(p);
					}
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
