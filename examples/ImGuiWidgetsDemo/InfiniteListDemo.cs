// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Examples.Widgets;

using System.Numerics;

using Hexa.NET.ImGui;
using ktsu.ImGui.Widgets;

/// <summary>
/// An infinite list: a feed that loads another page as the scroll nears its end.
/// </summary>
internal static class InfiniteListDemo
{
	private const int PageSize = 20;
	private const int Pages = 5;

	// How long a pretend load takes, in seconds.
	private const float LoadTime = 0.6f;

	private static float loadingFor = -1f;

	/// <summary>Gets how many posts the feed holds.</summary>
	internal static int Count { get; private set; } = PageSize;

	/// <summary>Gets how many pages have been asked for.</summary>
	internal static int Loads { get; private set; }

	/// <summary>
	/// Returns this section's state to its starting values. The demo keeps its state in statics, which
	/// outlive a harness.
	/// </summary>
	internal static void ResetState()
	{
		Count = PageSize;
		loadingFor = -1f;
		Loads = 0;
	}

	/// <summary>Draws the demo section.</summary>
	internal static void Show()
	{
		if (DemoProbe.Header("Mobile - Infinite List"))
		{
			ImGui.TextWrapped("Scroll the feed to its end and the next page loads. There are five pages in all.");

			bool loading = loadingFor >= 0f;
			if (loading)
			{
				loadingFor += ImGui.GetIO().DeltaTime;
				if (loadingFor >= LoadTime)
				{
					Count += PageSize;
					loadingFor = -1f;
					loading = false;
				}
			}

			ImGuiWidgets.InfiniteList(
				"demoFeed",
				new Vector2(0f, 200f),
				Count,
				i => ImGui.TextUnformatted($"Post {i + 1}"),
				() =>
				{
					Loads++;
					loadingFor = 0f;
				},
				loading,
				hasMore: Count < PageSize * Pages,
				new InfiniteListOptions { EndText = "That's everything." });
		}
	}
}
