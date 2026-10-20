// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Examples.Widgets;

using System.Numerics;
using Hexa.NET.ImGui;
using ktsu.ImGui.Widgets;

/// <summary>
/// Pull to refresh: a feed that loads newer posts when pulled down from its top.
/// </summary>
internal static class PullToRefreshDemo
{
	// How long the pretend refresh takes, in seconds.
	private const double LoadSeconds = 1.5;

	private static List<string> posts = [.. InitialPosts()];
	private static double refreshStarted = -1;
	private static int nextPost = 6;

	/// <summary>Gets the posts in the feed, newest first.</summary>
	internal static IReadOnlyList<string> Posts => posts;

	/// <summary>Gets a value indicating whether the pretend refresh is under way.</summary>
	internal static bool IsRefreshing => refreshStarted >= 0;

	/// <summary>
	/// Returns this section's state to its starting values. The demo keeps its state in statics, which
	/// outlive a harness.
	/// </summary>
	internal static void ResetState()
	{
		posts = [.. InitialPosts()];
		refreshStarted = -1;
		nextPost = 6;
	}

	/// <summary>Draws the demo section.</summary>
	internal static void Show()
	{
		if (DemoProbe.Header("Mobile - Pull to Refresh"))
		{
			ImGui.TextWrapped("Drag the feed down from its top and let go once the spinner is whole. The refresh takes a moment, then adds a post.");

			double now = ImGui.GetTime();
			if (IsRefreshing && now - refreshStarted >= LoadSeconds)
			{
				posts.Insert(0, $"Post {nextPost++}");
				refreshStarted = -1;
			}

			using ImGuiWidgets.PullToRefresh feed = new("demoFeed", new Vector2(MathF.Min(ImGui.GetContentRegionAvail().X, 360f), 200f), IsRefreshing);
			if (feed.Triggered)
			{
				refreshStarted = now;
			}

			foreach (string post in posts)
			{
				ImGui.TextUnformatted(post);
				ImGui.Separator();
			}
		}
	}

	private static IEnumerable<string> InitialPosts()
	{
		for (int i = 5; i >= 1; i--)
		{
			yield return $"Post {i}";
		}
	}
}
