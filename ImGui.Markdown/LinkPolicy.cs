// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Markdown;

using System;
using System.Diagnostics;

/// <summary>
/// Determines how markdown links are activated and which schemes may be opened by the OS.
/// </summary>
internal static class LinkPolicy
{
	private static readonly string[] AutoOpenSchemes = ["http://", "https://", "mailto:"];

	/// <summary>
	/// Returns whether the given URL uses a scheme this renderer is willing to hand to the
	/// operating system default handler.
	/// </summary>
	/// <param name="url">The URL to test.</param>
	/// <returns><see langword="true"/> for http, https, and mailto; otherwise <see langword="false"/>.</returns>
	public static bool ShouldAutoOpen(string url)
	{
		if (string.IsNullOrEmpty(url))
		{
			return false;
		}

		foreach (string scheme in AutoOpenSchemes)
		{
			if (url.StartsWith(scheme, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// Activates a clicked link: invokes the callback when supplied, otherwise opens
	/// auto-openable schemes with the OS default handler. Never throws.
	/// </summary>
	/// <param name="url">The clicked URL.</param>
	/// <param name="onClicked">Optional user callback that takes precedence over auto-open.</param>
	public static void Activate(string url, Action<string>? onClicked)
	{
		if (string.IsNullOrEmpty(url))
		{
			return;
		}

		if (onClicked is not null)
		{
			onClicked(url);
			return;
		}

		if (!ShouldAutoOpen(url))
		{
			return;
		}

		try
		{
			using Process? process = Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
		}
		catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException or FileNotFoundException)
		{
			// Opening a URL is best-effort; swallow launcher failures so the render loop is unaffected.
		}
	}
}
