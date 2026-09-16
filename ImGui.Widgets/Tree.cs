// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Color;
using ktsu.ImGui.Styler;
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
				ImGui.GetWindowDrawList().AddLine(a, b, Palette.Neutral.Gray.ToImGuiU32(), LineThickness);
				ImGui.NewLine();
				ImGui.Unindent();
				_ = openTrees!.Pop();
			};

			(openTrees ??= new Stack<Tree>()).Push(this);
		}

		/// <summary>
		/// Gets a new instance of the <see cref="TreeChild"/> class, representing a child node in the tree structure.
		/// </summary>
		public TreeChild Child => new(this);

		// The trees currently being drawn, innermost last, so Branch and Leaf can find the one they
		// belong to without every call site passing it down. ImGui is itself an ambient-context API, and
		// a tree's shape is already lexical, so a handle threaded through the callbacks would carry no
		// information the nesting does not already show. Thread-static because a context belongs to one
		// thread, and the scopes keep it balanced even when a body throws.
		[ThreadStatic]
		private static Stack<Tree>? openTrees;

		/// <summary>
		/// Gets the innermost tree currently being drawn, or null when none is open.
		/// </summary>
		private static Tree? Current => openTrees is { Count: > 0 } open ? open.Peek() : null;

		/// <summary>
		/// Draws a collapsible branch, invoking <paramref name="content"/> only while it is expanded.
		/// Nest further <see cref="Branch(string, Action)"/> and <see cref="Leaf(Action)"/> calls inside
		/// <paramref name="content"/> to build the level below it.
		/// </summary>
		/// <param name="label">Label for display and identity. Use the <c>label###id</c> form to keep a
		/// branch's identity stable while its visible text changes.</param>
		/// <param name="content">Drawn while the branch is expanded, and not called at all while it is
		/// collapsed.</param>
		public static void Branch(string label, Action content) =>
			Branch(label, ImGuiTreeNodeFlags.SpanAvailWidth, content);

		/// <summary>
		/// Draws a collapsible branch with explicit tree node flags, invoking <paramref name="content"/>
		/// only while it is expanded.
		/// </summary>
		/// <param name="label">Label for display and identity.</param>
		/// <param name="flags">Flags for the underlying tree node. These replace the default of
		/// <see cref="ImGuiTreeNodeFlags.SpanAvailWidth"/> rather than adding to it.</param>
		/// <param name="content">Drawn while the branch is expanded, and not called at all while it is
		/// collapsed.</param>
		public static void Branch(string label, ImGuiTreeNodeFlags flags, Action content)
		{
			Ensure.NotNull(content);

			// A branch with no enclosing tree is a root row, and a root row has nothing above it to join
			// to, so it draws no connector. A using over a null scope is a no-op, which is exactly that.
			using (Current?.Child)
			{
				// NoTreePushOnOpen always: the nested tree indents its own contents, and ImGui's tree
				// push would indent them a second time, marching every level further right than the
				// connector lines are drawn. It also means there is no TreePop to pair.
				if (ImGui.TreeNodeEx(label, flags | ImGuiTreeNodeFlags.NoTreePushOnOpen))
				{
					using (Tree subtree = new())
					{
						content();
					}
				}
			}
		}

		/// <summary>
		/// Draws a collapsible branch, passing <paramref name="state"/> to <paramref name="content"/> so
		/// the callback can be static and capture nothing. Prefer this in trees large enough for one
		/// closure allocation per node per frame to matter.
		/// </summary>
		/// <typeparam name="TState">Type of the state handed to <paramref name="content"/>.</typeparam>
		/// <param name="label">Label for display and identity.</param>
		/// <param name="state">Passed to <paramref name="content"/> untouched.</param>
		/// <param name="content">Drawn while the branch is expanded, and not called at all while it is
		/// collapsed.</param>
		public static void Branch<TState>(string label, TState state, Action<TState> content)
		{
			Ensure.NotNull(content);

			using (Current?.Child)
			{
				if (ImGui.TreeNodeEx(label, ImGuiTreeNodeFlags.SpanAvailWidth | ImGuiTreeNodeFlags.NoTreePushOnOpen))
				{
					using (Tree subtree = new())
					{
						content(state);
					}
				}
			}
		}

		/// <summary>
		/// Draws a terminal row joined to the enclosing tree by a connector. A leaf has no children and
		/// does not collapse, so <paramref name="content"/> is always drawn.
		/// </summary>
		/// <param name="content">Draws the row.</param>
		public static void Leaf(Action content)
		{
			Ensure.NotNull(content);

			using (Current?.Child)
			{
				content();
			}
		}

		/// <summary>
		/// Draws a terminal row, passing <paramref name="state"/> to <paramref name="content"/> so the
		/// callback can be static and capture nothing.
		/// </summary>
		/// <typeparam name="TState">Type of the state handed to <paramref name="content"/>.</typeparam>
		/// <param name="state">Passed to <paramref name="content"/> untouched.</param>
		/// <param name="content">Draws the row.</param>
		public static void Leaf<TState>(TState state, Action<TState> content)
		{
			Ensure.NotNull(content);

			using (Current?.Child)
			{
				content(state);
			}
		}

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

					ImGui.GetWindowDrawList().AddLine(a, b, Palette.Neutral.Gray.ToImGuiU32(), LineThickness);
				},
			onClose: null)
		{
		}
	}
}
