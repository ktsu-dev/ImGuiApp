// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Popups;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json.Serialization;
using Hexa.NET.ImGui;
using ktsu.Extensions;
using ktsu.Semantics.Paths;
using ktsu.Semantics.Strings;
using ktsu.ImGui.Probes;
using ktsu.ImGui.Widgets;
using ktsu.TextFilter;

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
	/// <remarks>
	/// The browser shows a toolbar (back, forward, up, refresh, a breadcrumb path that becomes a text box
	/// when clicked, and a search box), a sidebar of places, recent folders and drives, and the folder's
	/// contents as a sortable table or as a grid of thumbnails. Pictures for the grid come from the host
	/// through <see cref="ThumbnailResolver"/>, because this package uploads no textures.
	/// </remarks>
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

		/// <summary>A date whose every field is two digits wide, for measuring the Modified column.</summary>
		private static readonly DateTime SampleDate = new(2028, 12, 28, 23, 58, 58, DateTimeKind.Local);

		/// <summary>How many recent folders the sidebar keeps.</summary>
		private const int MaximumRecentFolders = 8;

		/// <summary>Name prefix for probe marks, so tests address rows and buttons by name.</summary>
		private const string ProbePrefix = "filesystem-browser";

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

		/// <summary>What the current folder holds, as read; filtering and sorting happen as it is drawn.</summary>
		private Collection<FilesystemEntry> entries = [];

		/// <summary>The entries in the order drawn, rebuilt when the listing, search or sort changes.</summary>
		private Collection<FilesystemEntry> visibleEntries = [];

		/// <summary>Why the current folder could not be read, or <see langword="null"/>.</summary>
		private string? listingError;

		/// <summary>
		/// The currently selected item.
		/// </summary>
		private IAbsolutePath ChosenItem { get; set; } = AbsolutePath.Create("");

		/// <summary>
		/// Collection of logical drives available.
		/// </summary>
		private Collection<string> Drives { get; set; } = [];

		/// <summary>The user's own folders, found each time the browser opens.</summary>
		private Collection<FilesystemBrowserPlace> standardPlaces = [];

		/// <summary>Back and forward through the folders visited since the browser opened.</summary>
		private readonly FilesystemBrowserHistory history = new();

		/// <summary>
		/// The glob pattern used for filtering files. Several patterns may be separated by
		/// semicolons, for example <c>*.png;*.jpg</c>.
		/// </summary>
		private string Glob { get; set; } = "*";

		/// <summary>
		/// The filename entered by the user, as typed.
		/// </summary>
		/// <remarks>
		/// Kept as text rather than a <see cref="FileName"/> while it is edited, because a path typed or
		/// pasted into the box passes through characters a file name cannot hold, and converting on every
		/// frame threw on the first of them (#662). It is converted once, when the user confirms.
		/// </remarks>
		private string fileNameText = string.Empty;

		/// <summary>The text in the search box.</summary>
		private string searchText = string.Empty;

		/// <summary>The text in the path box while the breadcrumb path is being typed over.</summary>
		private string pathText = string.Empty;

		/// <summary>Whether the breadcrumb path is shown as a text box.</summary>
		private bool editingPath;

		/// <summary>Set on the frame the path box appears, so it takes the keyboard before it is asked whether it lost it.</summary>
		private bool focusPathBox;

		/// <summary>The column the table is sorted by.</summary>
		private FilesystemSortColumn sortColumn = FilesystemSortColumn.Name;

		/// <summary>Whether the table's order ascends.</summary>
		private bool sortAscending = true;

		/// <summary>Set whenever <see cref="visibleEntries"/> no longer reflects the listing, search or sort.</summary>
		private bool visibleEntriesStale = true;

		/// <summary>The search text <see cref="visibleEntries"/> was built for.</summary>
		private string visibleSearch = string.Empty;

		/// <summary>The size the browser was opened at, or zero for its default.</summary>
		private Vector2 requestedSize;

		/// <summary>
		/// The modal instance for displaying the browser popup.
		/// </summary>
		private Modal Modal { get; } = new();

		/// <summary>
		/// The popup message for displaying alerts.
		/// </summary>
		private MessageOK PopupMessageOK { get; } = new();

		/// <summary>The question asked before a save replaces a file that already exists.</summary>
		private Prompt OverwritePrompt { get; } = new();

		/// <summary>Asks for the name of a new folder.</summary>
		private InputString NewFolderInput { get; } = new();

		/// <summary>Set when the overwrite prompt is answered Replace, so the browser closes behind it.</summary>
		private bool closeRequested;

		/// <summary>
		/// Gets or sets a value indicating whether a save asks before choosing a file that already exists.
		/// </summary>
		/// <remarks>
		/// Off by default, so an application that already confirms a replacement itself does not ask
		/// twice. When on, choosing an existing file in <see cref="FilesystemBrowserMode.Save"/> asks
		/// "Replace it?": Replace chooses the file and closes the browser, and Cancel, Escape or closing
		/// the question leave the browser open on the same folder and name. The question's buttons are
		/// probed as <c>filesystem-browser/overwrite/replace</c> and <c>filesystem-browser/overwrite/cancel</c>.
		/// </remarks>
		public bool ConfirmOverwrite { get; set; }

		/// <summary>
		/// Gets or sets how the browser supplies a picture of a file for the thumbnail grid and the preview.
		/// </summary>
		/// <remarks>
		/// Called for each file tile on screen, every frame it is on screen, so it must be cheap: look the
		/// picture up in a cache and return <see langword="null"/> while it is still being made, rather
		/// than decoding the file on the spot. It must not throw. Without a resolver, or while it returns
		/// <see langword="null"/>, a tile shows the file's extension in an empty frame.
		/// </remarks>
		public Func<AbsoluteFilePath, FilesystemBrowserThumbnail?>? ThumbnailResolver { get; set; }

		/// <summary>Gets or sets how the browser lays out a folder. It keeps whichever the user picked last.</summary>
		public FilesystemBrowserView View { get; set; } = FilesystemBrowserView.List;

		/// <summary>Gets or sets the edge of a thumbnail in the grid, in pixels at 100% interface scale.</summary>
		public float ThumbnailSize { get; set; } = 96f;

		/// <summary>Gets or sets a value indicating whether hidden files and folders are listed.</summary>
		public bool ShowHiddenFiles { get; set; }

		/// <summary>Gets the host's own folders, listed in the sidebar above the user's standard folders.</summary>
		public Collection<FilesystemBrowserPlace> Places { get; } = [];

		/// <summary>
		/// Gets the folders something was last opened from or saved to, most recent first. The browser adds
		/// to it as choices are made; a host may fill it on start and save it on exit.
		/// </summary>
		public Collection<AbsoluteDirectoryPath> RecentFolders { get; } = [];

		/// <summary>Gets the folder the browser is showing, or will show when it next opens.</summary>
		public AbsoluteDirectoryPath Directory => CurrentDirectory;

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
		/// Shows <paramref name="directory"/>, whether the browser is open or not. Going there from an open
		/// browser can be undone with Back.
		/// </summary>
		/// <param name="directory">The folder to show.</param>
		public void NavigateTo(AbsoluteDirectoryPath directory)
		{
			Ensure.NotNull(directory);
			if (Modal.WasOpen)
			{
				history.Visit(CurrentDirectory);
			}

			ShowDirectory(directory);
		}

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
			fileNameText = string.Empty;
			searchText = string.Empty;
			editingPath = false;
			BrowserMode = mode;
			BrowserTarget = target;
			OnChooseFile = onChooseFile;
			OnChooseDirectory = onChooseDirectory;
			Glob = glob;
			requestedSize = customSize;
			Drives.Clear();
			GetNavigableDrives().ForEach(Drives.Add);
			standardPlaces = FilesystemPlaces.Standard();
			history.Clear();
			RefreshContents();

			// The browser lays itself out from the size asked for, so the modal is left to fit it.
			Modal.Open(title, ShowContent, Vector2.Zero);
		}

		/// <summary>
		/// Tests a file name against a glob string. Several patterns may be supplied separated by
		/// semicolons, for example <c>*.png;*.jpg</c>, and the name matches if any one of them
		/// matches. The semicolon split is ours because <see cref="TextFilter"/> reserves the space
		/// as its own token separator, so a combined string cannot be handed to it whole.
		/// </summary>
		/// <param name="fileName">The file name to test.</param>
		/// <param name="glob">Glob pattern, or several separated by semicolons.</param>
		/// <returns><c>true</c> if the name matches any pattern supplied; otherwise <c>false</c>.</returns>
		internal static bool MatchesGlob(string fileName, string glob)
		{
			// Each pattern is matched on its own against the whole name: ByWordAny would split the
			// name on spaces too, so "a.jpg b.txt" would match "*.jpg" on its first word alone.
			// File dialogs have always matched case insensitively, which is not the glob default.
			return glob.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
				.Any(pattern => TextFilter.IsMatch(
					fileName,
					pattern,
					TextFilterType.Glob,
					TextFilterMatchOptions.ByWholeString,
					TextFilterCaseSensitivity.CaseInsensitive));
		}

		/// <summary>
		/// Displays the content of the filesystem browser popup.
		/// </summary>
		private void ShowContent()
		{
			Vector2 body = BodySize();

			HandleShortcuts();
			DrawToolbar(body.X);
			DrawSidebar(body.Y);
			ImGui.SameLine();
			DrawContents(body);
			DrawSummary();

			if (BrowserMode == FilesystemBrowserMode.Save)
			{
				DrawFileNameRow(body.X);
			}

			DrawFooter();

			PopupMessageOK.ShowIfOpen();
			OverwritePrompt.ShowIfOpen();
			NewFolderInput.ShowIfOpen();

			// Replace was answered inside the question; the browser is the current popup again here.
			if (closeRequested)
			{
				closeRequested = false;
				ImGui.CloseCurrentPopup();
			}
		}

		/// <summary>Works out the size of the sidebar and contents together, from the size asked for or a default in lines of text.</summary>
		/// <remarks>
		/// Never larger than the window the browser opens in leaves room for, so its buttons stay on
		/// screen at a large interface scale or in a small window.
		/// </remarks>
		/// <returns>The width and height of the area below the toolbar.</returns>
		private Vector2 BodySize()
		{
			float em = ImGui.GetFontSize();
			Vector2 minimum = new(36f * em, 14f * em);

			// Whatever is not toolbar, summary, name box or buttons belongs to the listing.
			Vector2 padding = ImGui.GetStyle().WindowPadding * 2;
			float chrome = ImGui.GetFrameHeightWithSpacing() * (BrowserMode == FilesystemBrowserMode.Save ? 5 : 4);
			Vector2 wanted = requestedSize == Vector2.Zero
				? new(56f * em, 28f * em)
				: new(requestedSize.X - padding.X, requestedSize.Y - padding.Y - chrome);

			Vector2 window = ImGui.GetMainViewport().WorkSize;
			Vector2 room = new(window.X - padding.X, window.Y - padding.Y - chrome - ImGui.GetFrameHeight());
			return Vector2.Max(Vector2.Min(minimum, room), Vector2.Min(wanted, room));
		}

		/// <summary>Reacts to the browser's keys, unless a text box has the keyboard.</summary>
		private void HandleShortcuts()
		{
			// A text box keeps its keys, and a popup over the browser (a message, a question, the folded
			// path) has the keyboard to itself.
			if (ImGui.IsAnyItemActive() || ImGui.IsPopupOpen(string.Empty, ImGuiPopupFlags.AnyPopupId))
			{
				return;
			}

			ImGuiIOPtr io = ImGui.GetIO();
			if (io.KeyAlt && ImGui.IsKeyPressed(ImGuiKey.LeftArrow))
			{
				GoBack();
			}
			else if (io.KeyAlt && ImGui.IsKeyPressed(ImGuiKey.RightArrow))
			{
				GoForward();
			}
			else if ((io.KeyAlt && ImGui.IsKeyPressed(ImGuiKey.UpArrow)) || ImGui.IsKeyPressed(ImGuiKey.Backspace))
			{
				GoUp();
			}
			else if (ImGui.IsKeyPressed(ImGuiKey.F5))
			{
				RefreshContents();
			}
			else if (io.KeyCtrl && ImGui.IsKeyPressed(ImGuiKey.L))
			{
				BeginEditingPath();
			}
			else if (ImGui.IsKeyPressed(ImGuiKey.Enter) || ImGui.IsKeyPressed(ImGuiKey.KeypadEnter))
			{
				OpenChosenItem();
			}
		}

		/// <summary>Draws back, forward, up and refresh, the breadcrumb path and the search box.</summary>
		/// <param name="width">The width the toolbar fills.</param>
		private void DrawToolbar(float width)
		{
			float startX = ImGui.GetCursorPosX();

			DrawArrowButton("back", ImGuiDir.Left, history.CanGoBack, GoBack, "Back (Alt+Left)");
			ImGui.SameLine();
			DrawArrowButton("forward", ImGuiDir.Right, history.CanGoForward, GoForward, "Forward (Alt+Right)");
			ImGui.SameLine();
			DrawArrowButton("up", ImGuiDir.Up, !IsRoot(CurrentDirectory), GoUp, "Up one folder (Alt+Up)");
			ImGui.SameLine();
			bool refresh = ImGui.Button("Refresh");
			ImGuiProbes.MarkItem(ProbePrefix, "refresh");
			SetTooltip("Read the folder again (F5)");
			if (refresh)
			{
				RefreshContents();
			}

			float searchWidth = 12f * ImGui.GetFontSize();
			float spacing = ImGui.GetStyle().ItemSpacing.X;
			ImGui.SameLine();
			float pathWidth = MathF.Max(ImGui.GetFontSize() * 6f, width - (ImGui.GetCursorPosX() - startX) - searchWidth - spacing);
			if (editingPath)
			{
				DrawPathBox(pathWidth);
			}
			else
			{
				DrawBreadcrumbs(pathWidth);
			}

			ImGui.SameLine();
			ImGui.SetNextItemWidth(searchWidth);
			if (ResizableInputText.InputTextWithHint("##search", "Search this folder", ref searchText))
			{
				visibleEntriesStale = true;
			}

			ImGuiProbes.MarkItem(ProbePrefix, "search");
		}

		/// <summary>Draws one of the navigation arrows, greyed out when there is nowhere for it to go.</summary>
		/// <param name="probe">The probe name, which is also the button's id.</param>
		/// <param name="direction">Which way the arrow points.</param>
		/// <param name="enabled">Whether the button does anything now.</param>
		/// <param name="onClick">What a click does.</param>
		/// <param name="tooltip">What the button does, shown on hover.</param>
		private static void DrawArrowButton(string probe, ImGuiDir direction, bool enabled, Action onClick, string tooltip)
		{
			ImGui.BeginDisabled(!enabled);
			bool clicked = ImGui.ArrowButton($"##{probe}", direction);
			ImGui.EndDisabled();
			ImGuiProbes.MarkItem(ProbePrefix, probe);
			SetTooltip(tooltip);
			if (clicked)
			{
				onClick();
			}
		}

		/// <summary>Shows a tooltip for the last item while it is hovered, even when it is disabled.</summary>
		/// <param name="text">The tooltip.</param>
		private static void SetTooltip(string text)
		{
			if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled | ImGuiHoveredFlags.DelayNormal))
			{
				ImGui.SetTooltip(text);
			}
		}

		/// <summary>
		/// Draws the current folder as a row of buttons, one per folder from the root down. When they do not
		/// all fit, the ones nearest the root fold into a menu at the start. The space after the last one
		/// turns the row into a text box for typing a path.
		/// </summary>
		/// <param name="width">The width the row may take.</param>
		private void DrawBreadcrumbs(float width)
		{
			Collection<PathCrumb> crumbs = PathCrumbs.For(CurrentDirectory);
			ImGuiStylePtr style = ImGui.GetStyle();
			float separatorWidth = ImGui.CalcTextSize(">").X + (style.ItemSpacing.X * 2);
			float overflowWidth = ImGui.CalcTextSize("...").X + (style.FramePadding.X * 2) + separatorWidth;
			float startX = ImGui.GetCursorPosX();

			// Keep the deepest folders, adding towards the root while they fit and leave room to click
			// for a path box after them.
			int first = crumbs.Count;
			float used = 3f * ImGui.GetFontSize();
			for (int i = crumbs.Count - 1; i >= 0; i--)
			{
				float crumbWidth = ImGui.CalcTextSize(crumbs[i].Label).X + (style.FramePadding.X * 2) + (i < crumbs.Count - 1 ? separatorWidth : 0);
				float reserve = i > 0 ? overflowWidth : 0;
				if (used + crumbWidth + reserve > width && first < crumbs.Count)
				{
					break;
				}

				used += crumbWidth;
				first = i;
			}

			if (first > 0)
			{
				DrawFoldedCrumbs(crumbs, first);
				ImGui.SameLine();
				ImGui.TextDisabled(">");
				ImGui.SameLine();
			}

			for (int i = first; i < crumbs.Count; i++)
			{
				if (i > first)
				{
					ImGui.SameLine();
					ImGui.TextDisabled(">");
					ImGui.SameLine();
				}

				// The id is the step's depth, not its label, so it does not change with the folder's name.
				bool clicked = ImGui.Button($"{crumbs[i].Label}###crumb{i}");
				ImGuiProbes.MarkItem(ProbePrefix, $"crumb/{i}");
				if (clicked && i < crumbs.Count - 1)
				{
					NavigateTo(crumbs[i].Directory);
				}
			}

			// Clicking the empty part of the row types a path, as it does in a file manager.
			ImGui.SameLine(0, 0);
			float rest = width - (ImGui.GetCursorPosX() - startX);
			if (rest > 1f)
			{
				bool edit = ImGui.InvisibleButton("##path", new Vector2(rest, ImGui.GetFrameHeight()));
				ImGuiProbes.MarkItem(ProbePrefix, "path");
				SetTooltip("Type a path (Ctrl+L)");
				if (edit)
				{
					BeginEditingPath();
				}
			}
		}

		/// <summary>Draws the button that lists the folders nearest the root that did not fit.</summary>
		/// <param name="crumbs">Every step of the path.</param>
		/// <param name="first">The first step drawn in full.</param>
		private void DrawFoldedCrumbs(Collection<PathCrumb> crumbs, int first)
		{
			if (ImGui.Button("...###crumbs-folded"))
			{
				ImGui.OpenPopup("##crumbs-folded");
			}

			ImGuiProbes.MarkItem(ProbePrefix, "crumbs-folded");

			if (ImGui.BeginPopup("##crumbs-folded"))
			{
				for (int i = first - 1; i >= 0; i--)
				{
					bool clicked = ImGui.Selectable($"{crumbs[i].Label}###folded{i}");
					ImGuiProbes.MarkItem(ProbePrefix, $"crumb/{i}");
					if (clicked)
					{
						NavigateTo(crumbs[i].Directory);
					}
				}

				ImGui.EndPopup();
			}
		}

		/// <summary>Turns the breadcrumb path into a text box holding the current folder.</summary>
		private void BeginEditingPath()
		{
			editingPath = true;
			focusPathBox = true;
			pathText = CurrentDirectory.WeakString;
		}

		/// <summary>Draws the path box, going to what was typed on Enter and back to breadcrumbs when it loses the keyboard.</summary>
		/// <param name="width">The width the box may take.</param>
		private void DrawPathBox(float width)
		{
			if (focusPathBox)
			{
				ImGui.SetKeyboardFocusHere();
			}

			ImGui.SetNextItemWidth(width);
			bool entered = ResizableInputText.InputText("##path-box", ref pathText, ImGuiInputTextFlags.EnterReturnsTrue | ImGuiInputTextFlags.AutoSelectAll);
			ImGuiProbes.MarkItem(ProbePrefix, "path");

			if (entered)
			{
				editingPath = false;
				GoToTypedPath(pathText);
			}
			else if (!focusPathBox && !ImGui.IsItemActive())
			{
				editingPath = false;
			}

			focusPathBox = false;
		}

		/// <summary>
		/// Goes to a typed path: into it when it is a folder, or to its folder and on to choosing it when it
		/// is a file the browser could choose. A leading <c>~</c> stands for the home folder.
		/// </summary>
		/// <param name="typed">The path as typed.</param>
		internal void GoToTypedPath(string typed)
		{
			string path = ExpandHome(typed.Trim().Trim('"'));
			if (path.Length == 0)
			{
				return;
			}

			try
			{
				path = Path.GetFullPath(path, CurrentDirectory.WeakString);
			}
			catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
			{
				PopupMessageOK.Open("Folder Not Found", $"{typed} is not a path this system can read.");
				return;
			}

			if (System.IO.Directory.Exists(path))
			{
				NavigateTo(path.As<AbsoluteDirectoryPath>());
			}
			else if (BrowserTarget == FilesystemBrowserTarget.File && (System.IO.File.Exists(path) || BrowserMode == FilesystemBrowserMode.Save) && System.IO.Directory.Exists(Path.GetDirectoryName(path)))
			{
				AbsoluteDirectoryPath folder = Path.GetDirectoryName(path)!.As<AbsoluteDirectoryPath>();
				if (folder != CurrentDirectory)
				{
					NavigateTo(folder);
				}

				fileNameText = Path.GetFileName(path);
				ChooseItem();
			}
			else
			{
				PopupMessageOK.Open("Folder Not Found", $"There is no folder at\n{path}");
			}
		}

		/// <summary>Replaces a leading <c>~</c> with the home folder.</summary>
		/// <param name="path">The path as typed.</param>
		/// <returns>The path with the home folder in place of <c>~</c>.</returns>
		internal static string ExpandHome(string path)
		{
			if (path == "~" || path.StartsWith("~/", StringComparison.Ordinal) || path.StartsWith("~\\", StringComparison.Ordinal))
			{
				return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + path[1..];
			}

			return path;
		}

		/// <summary>Draws the sidebar: the host's places, the user's folders, recent folders and drives, and a preview of the chosen file.</summary>
		/// <param name="height">The sidebar's height.</param>
		private void DrawSidebar(float height)
		{
			float width = 11f * ImGui.GetFontSize();
			if (ImGui.BeginChild("##places", new Vector2(width, height), ImGuiChildFlags.Borders))
			{
				DrawPlaceGroup("Places", Places.Concat(standardPlaces), "place");
				DrawPlaceGroup("Recent", RecentFolders.Select(folder => new FilesystemBrowserPlace(NameOf(folder), folder)), "recent");
				DrawDrives();
				DrawPreview(width);
			}

			ImGui.EndChild();
		}

		/// <summary>Draws one group of the sidebar, if it has anything in it.</summary>
		/// <param name="heading">The group's heading.</param>
		/// <param name="places">The folders in it.</param>
		/// <param name="probe">The probe segment its entries are named under.</param>
		private void DrawPlaceGroup(string heading, IEnumerable<FilesystemBrowserPlace> places, string probe)
		{
			bool first = true;
			foreach (FilesystemBrowserPlace place in places)
			{
				if (first)
				{
					ImGui.SeparatorText(heading);
					first = false;
				}

				bool clicked = ImGui.Selectable($"{place.Name}##{probe}{place.Directory.WeakString}", place.Directory == CurrentDirectory, ImGuiSelectableFlags.NoAutoClosePopups);
				ImGuiProbes.MarkItem(ProbePrefix, $"{probe}/{place.Name}");
				SetTooltip(place.Directory.WeakString);
				if (clicked && place.Directory != CurrentDirectory)
				{
					if (System.IO.Directory.Exists(place.Directory.WeakString))
					{
						NavigateTo(place.Directory);
					}
					else
					{
						PopupMessageOK.Open("Folder Not Found", $"{place.Directory} is no longer there.");
					}
				}
			}
		}

		/// <summary>Draws the drives, which the browser used to offer as a combo box above the listing.</summary>
		private void DrawDrives()
		{
			if (Drives.Count == 0)
			{
				return;
			}

			ImGui.SeparatorText("Drives");
			string currentDrive = CurrentDriveOf(CurrentDirectory, Drives[0]);
#pragma warning disable S3267 // Explicit loop is clearer; LINQ rewrite would add unnecessary allocation and complicate ImGui immediate-mode usage.
			foreach (string drive in Drives)
			{
				bool driveClicked = ImGui.Selectable(drive, string.Equals(drive, currentDrive, StringComparison.OrdinalIgnoreCase), ImGuiSelectableFlags.NoAutoClosePopups);
				ImGuiProbes.MarkItem(ProbePrefix, $"drive/{drive}");
				if (driveClicked)
				{
					NavigateTo(drive.As<AbsoluteDirectoryPath>());
				}
			}
#pragma warning restore S3267
		}

		/// <summary>Draws the chosen file's picture under the sidebar's lists, when the host supplies pictures.</summary>
		/// <param name="width">The sidebar's width.</param>
		private void DrawPreview(float width)
		{
			if (ThumbnailResolver is null || ChosenItem is not AbsoluteFilePath file)
			{
				return;
			}

			ImGui.SeparatorText("Preview");
			float edge = width - (ImGui.GetStyle().WindowPadding.X * 2);
			Vector2 origin = ImGui.GetCursorScreenPos();
			ImGui.Dummy(new Vector2(edge, edge));
			ImGuiProbes.MarkItem(ProbePrefix, "preview");
			DrawThumbnail(ImGui.GetWindowDrawList(), file, origin, edge);
		}

		/// <summary>Draws the folder's contents as a table or a grid.</summary>
		/// <param name="body">The size of the sidebar and contents together.</param>
		private void DrawContents(Vector2 body)
		{
			float width = body.X - (11f * ImGui.GetFontSize()) - ImGui.GetStyle().ItemSpacing.X;
			Vector2 size = new(width, body.Y);
			RebuildVisibleEntries();

			if (listingError is not null || visibleEntries.Count == 0)
			{
				DrawEmptyContents(size);
				return;
			}

			if (View == FilesystemBrowserView.Thumbnails)
			{
				DrawGrid(size);
			}
			else
			{
				DrawTable(size);
			}
		}

		/// <summary>Brings <see cref="visibleEntries"/> up to date with the listing, the search and the sort.</summary>
		private void RebuildVisibleEntries()
		{
			if (!visibleEntriesStale && visibleSearch == searchText)
			{
				return;
			}

			visibleEntries = FilesystemListing.Sort(FilesystemListing.Filter(entries, searchText), sortColumn, sortAscending);
			visibleSearch = searchText;
			visibleEntriesStale = false;
		}

		/// <summary>Draws why there is nothing to show: the folder could not be read, is empty, or nothing matches the search.</summary>
		/// <param name="size">The area to draw in.</param>
		private void DrawEmptyContents(Vector2 size)
		{
			if (ImGui.BeginChild("FilesystemBrowser", size, ImGuiChildFlags.Borders))
			{
				// The parent row stays, so a folder that cannot be read or holds nothing is not a dead end.
				if (View == FilesystemBrowserView.List && !IsRoot(CurrentDirectory))
				{
					DrawParentDirectoryRow(ImGuiSelectableFlags.AllowDoubleClick | ImGuiSelectableFlags.NoAutoClosePopups, inTable: false);
				}

				string message = listingError is not null
					? $"This folder can't be read.\n{listingError}"
					: searchText.Trim().Length > 0 ? $"Nothing here matches \"{searchText.Trim()}\"." : "This folder is empty.";
				ImGui.TextDisabled(message);
				ImGuiProbes.MarkItem(ProbePrefix, "empty");
			}

			ImGui.EndChild();
		}

		/// <summary>Draws the folder as a table of names, sizes and dates, sorted by whichever header was clicked.</summary>
		/// <param name="size">The table's size.</param>
		private void DrawTable(Vector2 size)
		{
			const ImGuiTableFlags TableFlags = ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.Sortable | ImGuiTableFlags.Resizable | ImGuiTableFlags.SizingFixedFit;
			if (!ImGui.BeginTable(nameof(FilesystemBrowser), 3, TableFlags, size))
			{
				return;
			}

			// The fixed columns are as wide as the longest thing they can hold, measured in the current font.
			float padding = ImGui.GetStyle().CellPadding.X * 2;
			float sizeWidth = ImGui.CalcTextSize("1,023 bytes").X + padding;
			float modifiedWidth = ImGui.CalcTextSize(SampleDate.ToString("g", CultureInfo.CurrentCulture)).X + padding;
			ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch | ImGuiTableColumnFlags.DefaultSort | ImGuiTableColumnFlags.NoHide);
			ImGui.TableSetupColumn("Size", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.PreferSortDescending, sizeWidth);
			ImGui.TableSetupColumn("Modified", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.PreferSortDescending, modifiedWidth);
			ImGui.TableSetupScrollFreeze(0, 1);
			ImGui.TableHeadersRow();
			ReadSort();

			ImGuiSelectableFlags flags = ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowDoubleClick | ImGuiSelectableFlags.NoAutoClosePopups;
			if (!IsRoot(CurrentDirectory))
			{
				DrawParentDirectoryRow(flags, inTable: true);
			}

			foreach (FilesystemEntry entry in visibleEntries)
			{
				DrawContentRow(entry, flags);
			}

			ImGui.EndTable();
		}

		/// <summary>Takes a click on a header as the new sort.</summary>
		private void ReadSort()
		{
			ImGuiTableSortSpecsPtr specs = ImGui.TableGetSortSpecs();
			if (specs.IsNull || !specs.SpecsDirty)
			{
				return;
			}

			if (specs.SpecsCount > 0)
			{
				ImGuiTableColumnSortSpecsPtr primary = specs.Specs;
				sortColumn = primary.ColumnIndex switch
				{
					1 => FilesystemSortColumn.Size,
					2 => FilesystemSortColumn.Modified,
					_ => FilesystemSortColumn.Name,
				};
				sortAscending = primary.SortDirection != ImGuiSortDirection.Descending;
			}

			specs.SpecsDirty = false;
			visibleEntriesStale = true;
			RebuildVisibleEntries();
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
		/// <param name="inTable">Whether the row is drawn into the listing's table.</param>
		private void DrawParentDirectoryRow(ImGuiSelectableFlags flags, bool inTable)
		{
			if (inTable)
			{
				ImGui.TableNextRow();
				ImGui.TableNextColumn();
			}

			bool parentClicked = ImGui.Selectable("..", false, flags);
			ImGuiProbes.MarkItem(ProbePrefix, "..");
			if (parentClicked && ImGui.IsMouseDoubleClicked(0))
			{
				GoUp();
			}
		}

		/// <summary>
		/// Draws a single filesystem entry row and handles its selection.
		/// </summary>
		/// <param name="entry">The entry represented by the row.</param>
		/// <param name="flags">The selectable flags to apply to the row.</param>
		private void DrawContentRow(FilesystemEntry entry, ImGuiSelectableFlags flags)
		{
			ImGui.TableNextRow();
			ImGui.TableNextColumn();
			string displayName = entry.DisplayName;

			bool rowClicked = ImGui.Selectable(displayName, ChosenItem == entry.Path, flags);

			// Rows are marked by their displayed name, so a test can select a known file without
			// counting rows or naming a pixel position.
			ImGuiProbes.MarkItem(ProbePrefix, displayName);
			DrawEntryTooltip(entry);

			if (rowClicked)
			{
				Select(entry, ImGui.IsMouseDoubleClicked(0));
			}

			// Details are dimmed, except on the selected row, where dim text on the highlight is hard to read.
			ImGuiCol detail = ChosenItem == entry.Path ? ImGuiCol.Text : ImGuiCol.TextDisabled;
			ImGui.PushStyleColor(ImGuiCol.Text, ImGui.GetColorU32(detail));
			ImGui.TableNextColumn();
			if (entry.Size is long size)
			{
				ImGui.TextUnformatted(FilesystemListing.FormatSize(size));
			}

			ImGui.TableNextColumn();
			if (entry.Modified != DateTime.MinValue)
			{
				ImGui.TextUnformatted(entry.Modified.ToString("g", CultureInfo.CurrentCulture));
			}

			ImGui.PopStyleColor();
		}

		/// <summary>Selects an entry, and opens or chooses it when it was double-clicked.</summary>
		/// <param name="entry">The entry clicked.</param>
		/// <param name="doubleClicked">Whether the click was the second of a double-click.</param>
		private void Select(FilesystemEntry entry, bool doubleClicked)
		{
			ChosenItem = entry.Path;
			if (entry.Path is AbsoluteFilePath file)
			{
				fileNameText = file.FileName;
			}

			if (doubleClicked)
			{
				OpenChosenItem();
			}
		}

		/// <summary>Opens the chosen folder, or chooses the chosen file; what Enter and a double-click do.</summary>
		private void OpenChosenItem()
		{
			if (ChosenItem is AbsoluteDirectoryPath directory)
			{
				NavigateTo(directory);
			}
			else if (ChosenItem is AbsoluteFilePath || BrowserMode == FilesystemBrowserMode.Save)
			{
				ChooseItem();
			}
		}

		/// <summary>Shows an entry's full name, size and date while it is hovered.</summary>
		/// <param name="entry">The entry under the pointer.</param>
		private static void DrawEntryTooltip(FilesystemEntry entry)
		{
			if (!ImGui.IsItemHovered(ImGuiHoveredFlags.DelayNormal))
			{
				return;
			}

			string details = entry.Size is long size ? $"\n{FilesystemListing.FormatSize(size)}" : string.Empty;
			if (entry.Modified != DateTime.MinValue)
			{
				details += $"\nModified {entry.Modified.ToString("g", CultureInfo.CurrentCulture)}";
			}

			ImGui.SetTooltip(entry.Name + details);
		}

		/// <summary>Draws the folder as a grid of tiles, each a thumbnail over a name.</summary>
		/// <param name="size">The grid's size.</param>
		private void DrawGrid(Vector2 size)
		{
			if (ImGui.BeginChild("FilesystemBrowser", size, ImGuiChildFlags.Borders))
			{
				float scale = ImGui.GetFontSize() / 13f;
				float edge = MathF.Round(ThumbnailSize * scale);
				ImGuiStylePtr style = ImGui.GetStyle();
				Vector2 tile = new(edge + (style.FramePadding.X * 2), edge + ImGui.GetTextLineHeight() + (style.FramePadding.Y * 3));
				int columns = Math.Max(1, (int)((ImGui.GetContentRegionAvail().X + style.ItemSpacing.X) / (tile.X + style.ItemSpacing.X)));

				for (int i = 0; i < visibleEntries.Count; i++)
				{
					if (i % columns != 0)
					{
						ImGui.SameLine();
					}

					DrawTile(visibleEntries[i], tile, edge);
				}
			}

			ImGui.EndChild();
		}

		/// <summary>Draws one tile of the grid and handles clicks on it.</summary>
		/// <param name="entry">The entry the tile shows.</param>
		/// <param name="tile">The tile's size.</param>
		/// <param name="edge">The thumbnail's edge.</param>
		private void DrawTile(FilesystemEntry entry, Vector2 tile, float edge)
		{
			Vector2 origin = ImGui.GetCursorScreenPos();

			// Only the id is hashed, so the tile's id is its name and nothing drawn changes it.
			bool clicked = ImGui.Selectable($"###{entry.DisplayName}", ChosenItem == entry.Path, ImGuiSelectableFlags.AllowDoubleClick | ImGuiSelectableFlags.NoAutoClosePopups, tile);
			ImGuiProbes.MarkItem(ProbePrefix, entry.DisplayName);
			DrawEntryTooltip(entry);
			if (clicked)
			{
				Select(entry, ImGui.IsMouseDoubleClicked(0));
			}

			if (!ImGui.IsItemVisible())
			{
				return;
			}

			ImDrawListPtr drawList = ImGui.GetWindowDrawList();
			ImGuiStylePtr style = ImGui.GetStyle();
			Vector2 picture = origin + new Vector2((tile.X - edge) * 0.5f, style.FramePadding.Y);
			if (entry.Path is AbsoluteFilePath file)
			{
				DrawThumbnail(drawList, file, picture, edge);
			}
			else
			{
				DrawFolderIcon(drawList, picture, edge);
			}

			string label = Ellipsize(entry.Name, tile.X - (style.FramePadding.X * 2));
			float labelWidth = ImGui.CalcTextSize(label).X;
			Vector2 labelAt = new(origin.X + ((tile.X - labelWidth) * 0.5f), picture.Y + edge + style.FramePadding.Y);
			drawList.AddText(labelAt, ImGui.GetColorU32(ImGuiCol.Text), label);
		}

		/// <summary>Draws a file's picture fitted into a square, or its extension in an empty frame when there is none.</summary>
		/// <param name="drawList">Where to draw.</param>
		/// <param name="file">The file.</param>
		/// <param name="origin">The square's top-left corner.</param>
		/// <param name="edge">The square's edge.</param>
		private void DrawThumbnail(ImDrawListPtr drawList, AbsoluteFilePath file, Vector2 origin, float edge)
		{
			FilesystemBrowserThumbnail? thumbnail = ThumbnailResolver?.Invoke(file);
			if (thumbnail is { } found && found.TextureId != 0 && found.Size.X > 0 && found.Size.Y > 0)
			{
				float fit = edge / MathF.Max(found.Size.X, found.Size.Y);
				Vector2 drawn = found.Size * fit;
				Vector2 min = origin + ((new Vector2(edge) - drawn) * 0.5f);
				ImTextureRef texture = default;
				texture.TexID = found.TextureId;
				drawList.AddImage(texture, min, min + drawn);
				return;
			}

			uint frame = ImGui.GetColorU32(ImGuiCol.Border);
			drawList.AddRect(origin, origin + new Vector2(edge), frame, ImGui.GetStyle().FrameRounding);
			string extension = Path.GetExtension(file.FileName.WeakString).TrimStart('.').ToUpperInvariant();
			if (extension.Length > 0)
			{
				extension = Ellipsize(extension, edge);
				Vector2 textSize = ImGui.CalcTextSize(extension);
				drawList.AddText(origin + ((new Vector2(edge) - textSize) * 0.5f), ImGui.GetColorU32(ImGuiCol.TextDisabled), extension);
			}
		}

		/// <summary>Draws a folder: a tab above a body, in the theme's header colour.</summary>
		/// <param name="drawList">Where to draw.</param>
		/// <param name="origin">The square's top-left corner.</param>
		/// <param name="edge">The square's edge.</param>
		private static void DrawFolderIcon(ImDrawListPtr drawList, Vector2 origin, float edge)
		{
			uint fill = ImGui.GetColorU32(ImGuiCol.HeaderActive);
			uint outline = ImGui.GetColorU32(ImGuiCol.Border);
			float rounding = edge * 0.05f;
			Vector2 tabMin = origin + new Vector2(edge * 0.1f, edge * 0.2f);
			Vector2 tabMax = origin + new Vector2(edge * 0.45f, edge * 0.32f);
			Vector2 bodyMin = origin + new Vector2(edge * 0.1f, edge * 0.28f);
			Vector2 bodyMax = origin + new Vector2(edge * 0.9f, edge * 0.8f);
			drawList.AddRectFilled(tabMin, tabMax, fill, rounding);
			drawList.AddRectFilled(bodyMin, bodyMax, fill, rounding);
			drawList.AddRect(bodyMin, bodyMax, outline, rounding);
		}

		/// <summary>Shortens text to fit a width, ending it with an ellipsis when it had to be cut.</summary>
		/// <param name="text">The text.</param>
		/// <param name="width">The width it must fit.</param>
		/// <returns>The text, or as much of it as fits followed by an ellipsis.</returns>
		private static string Ellipsize(string text, float width)
		{
			if (ImGui.CalcTextSize(text).X <= width)
			{
				return text;
			}

			const string Ellipsis = "...";
			int length = text.Length;
			while (length > 0 && ImGui.CalcTextSize(string.Concat(text.AsSpan(0, length), Ellipsis)).X > width)
			{
				length--;
			}

			return string.Concat(text.AsSpan(0, length), Ellipsis);
		}

		/// <summary>Draws what the folder holds and which files it offers, under the listing.</summary>
		private void DrawSummary()
		{
			string summary = listingError is null ? FilesystemListing.Summarize(visibleEntries) : "Unreadable folder";
			string shown = BrowserTarget == FilesystemBrowserTarget.File && Glob != "*"
				? $"Showing {Glob.Replace(";", ", ", StringComparison.Ordinal)}"
				: string.Empty;
			string line = shown.Length == 0 ? summary : $"{summary}   {shown}";

			// A host offering many formats can list more than fits, so the line is cut to the window
			// and the whole list is a tooltip away.
			string fitted = Ellipsize(line, ImGui.GetContentRegionAvail().X);
			ImGui.TextDisabled(fitted);
			ImGuiProbes.MarkItem(ProbePrefix, "summary");
			if (!ReferenceEquals(fitted, line) && ImGui.IsItemHovered())
			{
				ImGui.BeginTooltip();
				ImGui.PushTextWrapPos(ImGui.GetFontSize() * 30f);
				ImGui.TextUnformatted(shown);
				ImGui.PopTextWrapPos();
				ImGui.EndTooltip();
			}
		}

		/// <summary>Draws the name a save is given, filling the width of the browser.</summary>
		/// <param name="width">The browser's width.</param>
		private void DrawFileNameRow(float width)
		{
			float startX = ImGui.GetCursorPosX();
			ImGui.AlignTextToFramePadding();
			ImGui.TextUnformatted("File name");
			ImGui.SameLine();
			ImGui.SetNextItemWidth(width - (ImGui.GetCursorPosX() - startX));
			bool entered = ResizableInputText.InputText("##SaveAs", ref fileNameText, ImGuiInputTextFlags.EnterReturnsTrue);
			ImGuiProbes.MarkItem(ProbePrefix, "filename");
			if (entered)
			{
				ChooseItem();
			}
		}

		/// <summary>Draws new folder, hidden files and the view on the left, and the confirm and cancel buttons on the right.</summary>
		private void DrawFooter()
		{
			if (BrowserMode == FilesystemBrowserMode.Save || BrowserTarget == FilesystemBrowserTarget.Directory)
			{
				bool newFolder = ImGui.Button("New Folder");
				ImGuiProbes.MarkItem(ProbePrefix, "new-folder");
				if (newFolder)
				{
					NewFolderInput.Open("New Folder", "Name", "New folder", CreateFolder);
				}

				ImGui.SameLine();
			}

			bool showHidden = ShowHiddenFiles;
			if (ImGui.Checkbox("Hidden files", ref showHidden))
			{
				ShowHiddenFiles = showHidden;
				RefreshContents();
			}

			ImGuiProbes.MarkItem(ProbePrefix, "hidden");

			ImGui.SameLine();
			DrawViewChoice("List", FilesystemBrowserView.List, "view/list");
			ImGui.SameLine();
			DrawViewChoice("Thumbnails", FilesystemBrowserView.Thumbnails, "view/thumbnails");

			if (View == FilesystemBrowserView.Thumbnails)
			{
				ImGui.SameLine();
				ImGui.SetNextItemWidth(7f * ImGui.GetFontSize());
				float thumbnailSize = ThumbnailSize;
				if (ImGui.SliderFloat("##thumbnail-size", ref thumbnailSize, 48f, 256f, "%.0f px"))
				{
					ThumbnailSize = thumbnailSize;
				}

				ImGuiProbes.MarkItem(ProbePrefix, "thumbnail-size");
			}

			DrawConfirmButtons();
		}

		/// <summary>Draws one of the view choices.</summary>
		/// <param name="label">The choice's label.</param>
		/// <param name="view">The view it picks.</param>
		/// <param name="probe">Its probe name.</param>
		private void DrawViewChoice(string label, FilesystemBrowserView view, string probe)
		{
			if (ImGui.RadioButton(label, View == view))
			{
				View = view;
			}

			ImGuiProbes.MarkItem(ProbePrefix, probe);
		}

		/// <summary>
		/// Draws the confirm and cancel buttons for the browser, against the right edge.
		/// </summary>
		private void DrawConfirmButtons()
		{
			string confirmText = BrowserTarget == FilesystemBrowserTarget.Directory
				? "Select Folder"
				: BrowserMode == FilesystemBrowserMode.Save ? "Save" : "Open";

			ImGuiStylePtr style = ImGui.GetStyle();
			float buttonsWidth = ImGui.CalcTextSize(confirmText).X + ImGui.CalcTextSize("Cancel").X + (style.FramePadding.X * 4) + style.ItemSpacing.X;
			ImGui.SameLine();
			float available = ImGui.GetContentRegionAvail().X;
			if (available > buttonsWidth)
			{
				ImGui.SetCursorPosX(ImGui.GetCursorPosX() + available - buttonsWidth);
			}

			bool confirmClicked = ImGui.Button(confirmText);
			ImGuiProbes.MarkItem(ProbePrefix, "confirm");
			if (confirmClicked)
			{
				ChooseItem();
			}

			ImGui.SameLine();
			bool cancelClicked = ImGui.Button("Cancel");
			ImGuiProbes.MarkItem(ProbePrefix, "cancel");
			if (cancelClicked)
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
				if (!TryGetFileName(fileNameText, out FileName? fileName))
				{
					PopupMessageOK.Open("Invalid File Name", "The file name contains a folder separator or a character\nthis system does not allow in file names.");
					return;
				}

				if (!MatchesGlob(fileName, Glob))
				{
					PopupMessageOK.Open("Invalid File Name", "The file name does not match the glob pattern.");
					return;
				}

				if (!TryGetFullPath(CurrentDirectory, fileName, out AbsoluteFilePath? fullPath))
				{
					PopupMessageOK.Open("Invalid File Name", "The file name is too long, or makes a path\nthis system does not allow.");
					return;
				}

				if (BrowserMode == FilesystemBrowserMode.Save && ConfirmOverwrite && System.IO.File.Exists(fullPath))
				{
					AskToReplace(fullPath);
					return;
				}

				Remember(CurrentDirectory);
				OnChooseFile(fullPath);
			}
			else if (BrowserTarget == FilesystemBrowserTarget.Directory)
			{
				// With no folder selected, the folder being shown is the one chosen.
				AbsoluteDirectoryPath directory = ChosenItem as AbsoluteDirectoryPath ?? CurrentDirectory;
				AbsoluteDirectoryPath fullPath = Path.GetFullPath(directory).As<AbsoluteDirectoryPath>();
				Remember(fullPath);
				OnChooseDirectory(fullPath);
			}

			ImGui.CloseCurrentPopup();
		}

		/// <summary>Puts a folder at the top of <see cref="RecentFolders"/>.</summary>
		/// <param name="folder">The folder something was chosen in.</param>
		private void Remember(AbsoluteDirectoryPath folder)
		{
			RecentFolders.Remove(folder);
			RecentFolders.Insert(0, folder);
			while (RecentFolders.Count > MaximumRecentFolders)
			{
				RecentFolders.RemoveAt(RecentFolders.Count - 1);
			}
		}

		/// <summary>
		/// Converts typed text to a <see cref="FileName"/>, reporting text that is not one rather than throwing.
		/// </summary>
		/// <param name="text">The text from the file name box.</param>
		/// <param name="fileName">The file name, when the text is one.</param>
		/// <returns><see langword="true"/> if the text is a valid file name; otherwise <see langword="false"/>.</returns>
		internal static bool TryGetFileName(string text, [NotNullWhen(true)] out FileName? fileName)
		{
			try
			{
				fileName = text.As<FileName>();
				return true;
			}
			catch (ArgumentException)
			{
				fileName = null;
				return false;
			}
		}

		/// <summary>
		/// Puts a file name in a folder, reporting a path that cannot be one rather than throwing. The name
		/// box takes text of any length, so a name can be valid on its own and still make a path the
		/// system refuses, most often by being too long.
		/// </summary>
		/// <param name="folder">The folder the file is in.</param>
		/// <param name="fileName">The file's name.</param>
		/// <param name="fullPath">The file's full path, when there is one.</param>
		/// <returns>True when the two make a valid path.</returns>
		internal static bool TryGetFullPath(AbsoluteDirectoryPath folder, FileName fileName, [NotNullWhen(true)] out AbsoluteFilePath? fullPath)
		{
			try
			{
				AbsoluteFilePath chosenFile = folder / fileName;
				fullPath = Path.GetFullPath(chosenFile).As<AbsoluteFilePath>();
				return true;
			}
			catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
			{
				fullPath = null;
				return false;
			}
		}

		/// <summary>Asks whether a save may replace a file that already exists.</summary>
		/// <param name="file">The file the save would replace.</param>
		private void AskToReplace(AbsoluteFilePath file)
		{
			Action<AbsoluteFilePath> choose = OnChooseFile;
			AbsoluteDirectoryPath folder = CurrentDirectory;
			OverwritePrompt.Open(
				"Replace File",
				$"{file.FileName} already exists.\nDo you want to replace it?",
				[
					new PromptButton("Replace", () =>
					{
						Remember(folder);
						choose(file);
						closeRequested = true;
					}, $"{ProbePrefix}/overwrite/replace"),
					new PromptButton("Cancel", null, $"{ProbePrefix}/overwrite/cancel", IsCancel: true),
				]);
		}

		/// <summary>Makes a folder in the current one and selects it.</summary>
		/// <param name="name">The name typed for it.</param>
		private void CreateFolder(string name)
		{
			if (!TryGetFileName(name.Trim(), out FileName? folderName))
			{
				PopupMessageOK.Open("Invalid Folder Name", "The folder name contains a separator or a character\nthis system does not allow in names.");
				return;
			}

			string path = Path.Join(CurrentDirectory.WeakString, folderName.WeakString);
			if (System.IO.Directory.Exists(path) || System.IO.File.Exists(path))
			{
				PopupMessageOK.Open("Folder Not Created", $"Something called {folderName} is already here.");
				return;
			}

			try
			{
				System.IO.Directory.CreateDirectory(path);
			}
			catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
			{
				PopupMessageOK.Open("Folder Not Created", ex.Message);
				return;
			}

			RefreshContents();
			ChosenItem = path.As<AbsoluteDirectoryPath>();
		}

		/// <summary>Goes back to the folder shown before this one.</summary>
		private void GoBack()
		{
			if (history.TryGoBack(CurrentDirectory, out AbsoluteDirectoryPath previous))
			{
				ShowDirectory(previous);
			}
		}

		/// <summary>Goes forward again after going back.</summary>
		private void GoForward()
		{
			if (history.TryGoForward(CurrentDirectory, out AbsoluteDirectoryPath next))
			{
				ShowDirectory(next);
			}
		}

		/// <summary>Goes to the folder that holds this one.</summary>
		/// <remarks>
		/// The parent comes from the path type rather than from trimming separators off the string:
		/// trimming also removes the leading separator, which is the root itself on Unix, so the "parent"
		/// came back relative and failed <see cref="AbsoluteDirectoryPath"/> validation
		/// (ktsu-dev/ImGuiApp#281). A root is its own parent, so this stays put once there is nowhere left to go.
		/// </remarks>
		private void GoUp()
		{
			if (!IsRoot(CurrentDirectory))
			{
				AbsoluteDirectoryPath child = CurrentDirectory;
				NavigateTo(CurrentDirectory.Parent);

				// Coming up out of a folder leaves it selected, so Enter goes straight back in.
				ChosenItem = child;
			}
		}

		/// <summary>Determines whether a folder is the root of its drive.</summary>
		/// <param name="directory">The folder.</param>
		/// <returns><see langword="true"/> when the folder has no parent.</returns>
		private static bool IsRoot(AbsoluteDirectoryPath directory) => Path.GetDirectoryName(Path.TrimEndingDirectorySeparator(directory.WeakString)) is null;

		/// <summary>Gets the name a folder is listed under in the sidebar.</summary>
		/// <param name="directory">The folder.</param>
		/// <returns>Its own name, or the whole path for a root.</returns>
		private static string NameOf(AbsoluteDirectoryPath directory)
		{
			string name = Path.GetFileName(Path.TrimEndingDirectorySeparator(directory.WeakString));
			return string.IsNullOrEmpty(name) ? directory.WeakString : name;
		}

		/// <summary>Shows a folder without touching the history.</summary>
		/// <param name="directory">The folder to show.</param>
		private void ShowDirectory(AbsoluteDirectoryPath directory)
		{
			CurrentDirectory = directory;
			searchText = string.Empty;
			RefreshContents();
		}

		/// <summary>
		/// Refreshes the contents of the current directory based on the target and glob pattern.
		/// </summary>
		private void RefreshContents()
		{
			ChosenItem = AbsolutePath.Create("");
			entries = FilesystemListing.Read(CurrentDirectory, BrowserTarget == FilesystemBrowserTarget.File, Glob, ShowHiddenFiles, out listingError);
			visibleEntriesStale = true;
		}

		/// <summary>
		/// Shows the modal popup if it is open.
		/// </summary>
		/// <returns>True if the modal is open; otherwise, false.</returns>
		public bool ShowIfOpen() => Modal.ShowIfOpen();
	}
}
