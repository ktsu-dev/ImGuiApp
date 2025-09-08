// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Styler;

using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ScopedAction;

/// <summary>
/// Provides methods for aligning content within a container.
/// </summary>
public static class Alignment
{
	/// <summary>
	/// Centers the content within the available content region.
	/// </summary>
	/// <param name="contentSize">The size of the content to be centered.</param>
	public class Center(Vector2 contentSize) : CenterWithin(contentSize, new Vector2(ImGui.GetContentRegionAvail().X, contentSize.Y))
	{
	}

	/// <summary>
	/// Centers the content within a specified container size.
	/// </summary>
	public class CenterWithin : ScopedAction
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="CenterWithin"/> class.
		/// </summary>
		/// <param name="contentSize">The size of the content to be centered.</param>
		/// <param name="containerSize">The size of the container within which the content will be centered.</param>
		public CenterWithin(Vector2 contentSize, Vector2 containerSize)
		{
			// We need to manipulate the cursor a lot to support the layout of this widget and
			// integrate with the layout methods of ImGui (eg. SameLine). Because contentDrawDelegate
			// is called after the Dummy() it means that CursorPosPrevLine is set to an unexpected value
			// so we "abuse" setting the cursor and calling NewLine to force CursorPosPrevLine to be what we need.

			// - Move the cursor to the top left of the content
			// - Move the cursor to the top right of the container
			// - Make a dummy with the height of the container so that the cursor will advance to
			//   a new line after the container size, while support ImGui.NewLine() to work as expected
			Vector2 cursorContainerTopLeft = ImGui.GetCursorScreenPos();
			Vector2 clippedsize = new(Math.Min(contentSize.X, containerSize.X), Math.Min(contentSize.Y, containerSize.Y));
			Vector2 offset = (containerSize - clippedsize) / 2;
			Vector2 cursorContentStart = cursorContainerTopLeft + offset;
			ImGui.SetCursorScreenPos(cursorContentStart);

			OnClose = () =>
			{
				ImGui.SetCursorScreenPos(cursorContainerTopLeft + new Vector2(containerSize.X, 0f));
				ImGui.Dummy(new Vector2(0, containerSize.Y));
			};
		}
	}
}
