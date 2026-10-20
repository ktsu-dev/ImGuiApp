// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;
using System.Collections.Generic;

/// <summary>
/// Where a sticky header sits, with no ImGui calls. The header of the section the top of the view is
/// in stays pinned to the top, until the next section's header arrives and pushes it up and out.
/// </summary>
internal static class StickyHeaderGeometry
{
	/// <summary>Works out which header is pinned, and where.</summary>
	/// <param name="headerTops">The top of each section's header, in content space, in order.</param>
	/// <param name="headerHeight">How tall a header is.</param>
	/// <param name="scroll">The top of the view, in content space.</param>
	/// <returns>
	/// The pinned section's index, or -1 when the view is above every section or the section's own
	/// header is in view where it belongs; and the pinned header's top, in content space.
	/// </returns>
	public static (int Index, float Top) Pin(IReadOnlyList<float> headerTops, float headerHeight, float scroll)
	{
		Ensure.NotNull(headerTops);

		int index = -1;
		for (int i = 0; i < headerTops.Count && headerTops[i] < scroll; i++)
		{
			index = i;
		}

		if (index < 0)
		{
			return (-1, 0f);
		}

		float top = scroll;
		if (index + 1 < headerTops.Count)
		{
			top = Math.Min(top, headerTops[index + 1] - headerHeight);
		}

		return (index, top);
	}
}
