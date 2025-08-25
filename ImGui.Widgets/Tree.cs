// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGuiWidgets;

using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGuiStyler;
using ktsu.ScopedAction;

/// <summary>
/// Provides custom ImGui widgets.
/// </summary>
public static partial class ImGuiWidgets
{
	/// <summary>
	/// Represents a tree structure widget in ImGui with custom drawing logic.
	/// </summary>
	public class Tree : ScopedAction
	{
		private Vector2 CursorStart { get; init; }
		private Vector2 CursorEnd { get; set; }
		private float IndentWidth { get; init; }
		private float HalfIndentWidth => IndentWidth / 2f;
		private float FrameHeight { get; init; }
		private float HalfFrameHeight => FrameHeight / 2f;
		private float ItemSpacingY { get; init; }
		private float Left { get; init; }
		private float Top { get; init; }
		private const float LineThickness = 2f;
		private const float HalfLineThickness = LineThickness / 2f;

		/// <summary>
		/// Initializes a new instance of the <see cref="Tree"/> class.
		/// Sets up the initial cursor position, indent width, item spacing, frame height, and drawing logic for the tree structure.
		/// </summary>
		public Tree() : base()
		{
			ImGui.Indent();
			CursorStart = ImGui.GetCursorScreenPos();
			IndentWidth = ImGui.GetStyle().IndentSpacing;
			ItemSpacingY = ImGui.GetStyle().ItemSpacing.Y;
			FrameHeight = ImGui.GetFrameHeight();
			Left = CursorStart.X - HalfIndentWidth;
			Top = CursorStart.Y - ItemSpacingY - HalfLineThickness;

			OnClose = () =>
			{
				ImGui.SameLine();
				float bottom = CursorEnd.Y + HalfFrameHeight + HalfLineThickness;
				Vector2 a = new(Left, Top);
				Vector2 b = new(Left, bottom);
				ImGui.GetWindowDrawList().AddLine(a, b, ImGui.GetColorU32(Color.Palette.Neutral.Gray.Value), LineThickness);
				ImGui.NewLine();
				ImGui.Unindent();
			};
		}

		/// <summary>
		/// Gets a new instance of the <see cref="TreeChild"/> class, representing a child node in the tree structure.
		/// </summary>
		public TreeChild Child => new(this);

		/// <summary>
		/// Represents a child node in the tree structure.
		/// </summary>
		/// <param name="parent">The parent tree node.</param>
		public class TreeChild(Tree parent) : ScopedAction(
			onOpen: () =>
				{
					Vector2 cursor = ImGui.GetCursorScreenPos();
					parent.CursorEnd = cursor;
					float right = cursor.X;
					float y = cursor.Y + parent.HalfFrameHeight;

					Vector2 a = new(parent.Left, y);
					Vector2 b = new(right, y);

					ImGui.GetWindowDrawList().AddLine(a, b, ImGui.GetColorU32(Color.Palette.Neutral.Gray.Value), LineThickness);
				},
			onClose: null)
		{
		}
	}
}
