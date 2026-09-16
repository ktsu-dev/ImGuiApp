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

	private void DrawFruitBranch(Action<string>? onBody = null)
	{
		ImGuiWidgets.Tree.Branch("Fruit", () =>
		{
			onBody?.Invoke("Fruit");
			ImGuiWidgets.Tree.Leaf(() =>
			{
				ImGui.TextUnformatted("Lemon");
				Mark("Lemon");
			});
		});

		Mark("Fruit");
	}

	[TestMethod]
	public void Branch_StartsClosedAndDoesNotRunItsBody()
	{
		bool bodyRan = false;

		Start(() => DrawFruitBranch(_ => bodyRan = true));

		Assert.IsFalse(bodyRan, "A collapsed branch ran its body.");
		Assert.IsFalse(IsVisible("Lemon"), "A collapsed branch drew its children.");
	}

	[TestMethod]
	public void Branch_RunsItsBodyOnceOpened()
	{
		bool bodyRan = false;

		Start(() => DrawFruitBranch(_ => bodyRan = true));

		Click("Fruit");

		Assert.IsTrue(bodyRan, "An expanded branch did not run its body.");
		Assert.IsTrue(IsVisible("Lemon"), "An expanded branch did not draw its children.");
	}

	[TestMethod]
	public void Branch_FlagsOverload_CanStartOpen()
	{
		Start(() => ImGuiWidgets.Tree.Branch("Fruit", ImGuiTreeNodeFlags.DefaultOpen, () =>
		{
			ImGui.TextUnformatted("Lemon");
			Mark("Lemon");
		}));

		Assert.IsTrue(IsVisible("Lemon"), "DefaultOpen did not reach the underlying node.");
	}

	// A branch with nothing enclosing it is a root row. It has nothing above it to join to, so it draws
	// no connector, and it must not need a wrapper to work at all.
	[TestMethod]
	public void Branch_WithNoEnclosingBranch_StillDraws()
	{
		Start(() => ImGuiWidgets.Tree.Branch("Fruit", ImGuiTreeNodeFlags.DefaultOpen, () =>
		{
			ImGui.TextUnformatted("Lemon");
			Mark("Lemon");
		}));

		Assert.IsTrue(IsVisible("Lemon"), "A branch with no enclosing branch drew nothing.");
	}

	[TestMethod]
	public void Branch_IndentsEachLevelFurtherThanTheLast()
	{
		Start(() =>
		{
			ImGui.TextUnformatted("Above");
			Mark("Above");

			ImGuiWidgets.Tree.Branch("Fruit", ImGuiTreeNodeFlags.DefaultOpen, () =>
			{
				ImGuiWidgets.Tree.Leaf(() =>
				{
					ImGui.TextUnformatted("Apple");
					Mark("Apple");
				});

				ImGuiWidgets.Tree.Branch("Citrus", ImGuiTreeNodeFlags.DefaultOpen, () =>
					ImGuiWidgets.Tree.Leaf(() =>
					{
						ImGui.TextUnformatted("Lemon");
						Mark("Lemon");
					}));
			});
		});

		Rectangle above = RectOf("Above");
		Rectangle apple = RectOf("Apple");
		Rectangle lemon = RectOf("Lemon");

		Assert.IsTrue(apple.MinX > above.MinX, $"A leaf at x={apple.MinX} was no further in than the row above it at x={above.MinX}.");
		Assert.IsTrue(lemon.MinX > apple.MinX, $"A nested leaf at x={lemon.MinX} was no further in than its parent's leaf at x={apple.MinX}.");
	}

	[TestMethod]
	public void Branch_DrawsConnectingLinesAroundItsChildren()
	{
		bool useBranch = false;

		Start(() =>
		{
			if (useBranch)
			{
				ImGuiWidgets.Tree.Branch("Fruit", ImGuiTreeNodeFlags.DefaultOpen, () =>
					ImGuiWidgets.Tree.Leaf(() => ImGui.TextUnformatted("Lemon")));
			}
			else
			{
				ImGui.TextUnformatted("Fruit");
				ImGui.TextUnformatted("Lemon");
			}
		});

		byte[] plainText = Snapshot();
		useBranch = true;
		Step(2);

		Assert.IsNotNull(BoundsOfDifference(plainText), "The branch drew no connecting lines around its children.");
	}

	[TestMethod]
	public void Branch_StateOverload_HandsTheStateToItsBody()
	{
		string seenByBranch = string.Empty;
		string seenByLeaf = string.Empty;

		Start(() =>
		{
			ImGuiWidgets.Tree.Branch("Fruit", "Lemon", label =>
			{
				seenByBranch = label;
				ImGuiWidgets.Tree.Leaf(label, text =>
				{
					seenByLeaf = text;
					ImGui.TextUnformatted(text);
					Mark("Lemon");
				});
			});

			Mark("Fruit");
		});

		Click("Fruit");

		Assert.AreEqual("Lemon", seenByBranch, "The branch did not hand its state to the body.");
		Assert.AreEqual("Lemon", seenByLeaf, "The leaf did not hand its state to the body.");
		Assert.IsTrue(IsVisible("Lemon"), "The leaf drew nothing with the state it was given.");
	}

	// The open-tree stack is what lets a branch find its parent, so an escaping exception must not leave
	// it holding a tree that has gone. Everything drawn afterwards would be parented to a stale entry.
	[TestMethod]
	public void Branch_BodyThrowing_LeavesTheNextBranchDrawable()
	{
		Start(() =>
		{
			try
			{
				ImGuiWidgets.Tree.Branch("Throws", ImGuiTreeNodeFlags.DefaultOpen, static () => throw new InvalidOperationException("thrown by the test"));
			}
			catch (InvalidOperationException)
			{
				// Expected. What matters is the state of the stack afterwards, not the throw itself.
			}

			ImGuiWidgets.Tree.Branch("Fruit", ImGuiTreeNodeFlags.DefaultOpen, () =>
			{
				ImGui.TextUnformatted("Lemon");
				Mark("Lemon");
			});
		});

		Assert.IsTrue(IsVisible("Lemon"), "A branch whose body threw left the tree stack unbalanced.");
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
