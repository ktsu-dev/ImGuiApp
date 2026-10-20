// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Popups;

using System.Collections.Generic;
using ktsu.Semantics.Paths;

/// <summary>
/// The back and forward lists of a file browser, as a web browser keeps them: going somewhere new
/// forgets what was ahead, and going back or forward moves one folder between the two lists.
/// </summary>
internal sealed class FilesystemBrowserHistory
{
	private readonly Stack<AbsoluteDirectoryPath> back = new();
	private readonly Stack<AbsoluteDirectoryPath> forward = new();

	/// <summary>Gets a value indicating whether there is a folder to go back to.</summary>
	public bool CanGoBack => back.Count > 0;

	/// <summary>Gets a value indicating whether there is a folder to go forward to.</summary>
	public bool CanGoForward => forward.Count > 0;

	/// <summary>Records leaving <paramref name="from"/> for somewhere new.</summary>
	/// <param name="from">The folder being left.</param>
	public void Visit(AbsoluteDirectoryPath from)
	{
		back.Push(from);
		forward.Clear();
	}

	/// <summary>Goes back one folder.</summary>
	/// <param name="current">The folder being left, which becomes the next one forward.</param>
	/// <param name="previous">The folder to show.</param>
	/// <returns><see langword="true"/> if there was a folder to go back to.</returns>
	public bool TryGoBack(AbsoluteDirectoryPath current, out AbsoluteDirectoryPath previous)
	{
		if (!back.TryPop(out AbsoluteDirectoryPath? found))
		{
			previous = current;
			return false;
		}

		forward.Push(current);
		previous = found;
		return true;
	}

	/// <summary>Goes forward one folder.</summary>
	/// <param name="current">The folder being left, which becomes the next one back.</param>
	/// <param name="next">The folder to show.</param>
	/// <returns><see langword="true"/> if there was a folder to go forward to.</returns>
	public bool TryGoForward(AbsoluteDirectoryPath current, out AbsoluteDirectoryPath next)
	{
		if (!forward.TryPop(out AbsoluteDirectoryPath? found))
		{
			next = current;
			return false;
		}

		back.Push(current);
		next = found;
		return true;
	}

	/// <summary>Forgets both lists, as a browser does each time it opens.</summary>
	public void Clear()
	{
		back.Clear();
		forward.Clear();
	}
}
