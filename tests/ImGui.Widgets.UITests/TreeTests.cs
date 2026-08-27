// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using Hexa.NET.ImGui;

using ktsu.ImGui.App.Testing;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>Drives <see cref="ImGuiWidgets.Tree"/> and <see cref="ImGuiWidgets.IconTreeNode"/> on their own.</summary>
[TestClass]
public sealed class TreeTests : WidgetTest
{
	private void DrawTree()
	{
		ImGui.TextUnformatted("Root");
		Mark("Root");

		using ImGuiWidgets.Tree tree = new();

		foreach (string child in new[] { "Alpha", "Beta" })
		{
			using (tree.Child)
			{
				ImGui.TextUnformatted(child);
				Mark(child);
			}
		}
	}

	[TestMethod]
	public void Tree_IndentsItsChildrenPastTheRoot()
	{
		Start(DrawTree);

		Rectangle root = RectOf("Root");
		Rectangle child = RectOf("Alpha");

		Assert.IsTrue(child.MinX > root.MinX, $"A child was drawn at x={child.MinX}, no further in than the root at x={root.MinX}.");
	}

	[TestMethod]
	public void Tree_StacksItsChildrenInOrder()
	{
		Start(DrawTree);

		Rectangle first = RectOf("Alpha");
		Rectangle second = RectOf("Beta");

		Assert.IsTrue(second.MinY > first.MinY, "The second child was not drawn below the first.");
		Assert.AreEqual(first.MinX, second.MinX, "The two children were not indented to the same depth.");
	}

	[TestMethod]
	public void Tree_DrawsConnectingLines()
	{
		bool show = false;

		Start(() =>
		{
			if (show)
			{
				DrawTree();
			}
			else
			{
				ImGui.TextUnformatted("Root");
				ImGui.TextUnformatted("Alpha");
				ImGui.TextUnformatted("Beta");
			}
		});

		byte[] plainText = Snapshot();
		show = true;
		Step(2);

		Assert.IsNotNull(BoundsOfDifference(plainText), "The tree drew no connecting lines around its children.");
	}

	[TestMethod]
	public void IconTreeNode_StartsClosed()
	{
		Start(() =>
		{
			if (ImGuiWidgets.IconTreeNode("Assets", "A", ktsu.Semantics.Color.Color.FromHex("#ffcc00")))
			{
				ImGui.TextUnformatted("child");
				Mark("child");
				ImGui.TreePop();
			}

			Mark("Assets");
		});

		Assert.IsFalse(IsVisible("child"), "A closed icon tree node drew its children.");
	}

	[TestMethod]
	public void IconTreeNode_OpensWhenClicked()
	{
		Start(() =>
		{
			if (ImGuiWidgets.IconTreeNode("Assets", "A", ktsu.Semantics.Color.Color.FromHex("#ffcc00")))
			{
				ImGui.TextUnformatted("child");
				Mark("child");
				ImGui.TreePop();
			}

			Mark("Assets");
		});

		Click("Assets");

		Assert.IsTrue(IsVisible("child"), "Clicking the node did not reveal its children.");
	}

	[TestMethod]
	public void IconTreeNode_DrawsItsIconBeforeTheLabel()
	{
		string icon = "A";

		Start(() =>
		{
			if (ImGuiWidgets.IconTreeNode("Assets", icon, ktsu.Semantics.Color.Color.FromHex("#ffcc00"), ImGuiTreeNodeFlags.NoTreePushOnOpen))
			{
				// Nothing to draw; the flag suppresses the identifier push, so no pop is needed.
			}

			Mark("Assets");
		});

		byte[] withIcon = Snapshot();
		icon = "B";
		Step(2);

		Assert.IsTrue(PixelsChangedSince(withIcon) > 0, "Changing the icon glyph changed nothing on screen.");
	}
}
