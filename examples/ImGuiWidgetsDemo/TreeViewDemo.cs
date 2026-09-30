// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Examples.Widgets;

using Hexa.NET.ImGui;
using ktsu.ImGui.Widgets;

/// <summary>
/// The tree connector lines.
/// </summary>
internal static class TreeViewDemo
{
	/// <summary>
	/// Returns this section's state to its starting values. The demo keeps its state in statics, which
	/// outlive a harness.
	/// </summary>
	internal static void ResetState()
	{
		// Nothing to reset: the section keeps no state.
	}

	/// <summary>Draws the demo section.</summary>
	internal static void Show()
	{
		if (DemoProbe.Header("Tree View"))
		{
			ImGui.TextUnformatted("Collapsible branches and terminal leaves. A collapsed branch never runs its body:");
			ImGui.Separator();

			ImGuiWidgets.Tree.Branch("Fruit", ImGuiTreeNodeFlags.DefaultOpen, () =>
			{
				ImGuiWidgets.Tree.Branch("Citrus", ImGuiTreeNodeFlags.DefaultOpen, () =>
				{
					ImGuiWidgets.Tree.Leaf(() => DemoProbe.Button("Lemon"));
					ImGuiWidgets.Tree.Leaf(() => DemoProbe.Button("Lime"));
				});

				ImGuiWidgets.Tree.Leaf(() => DemoProbe.Button("Apple"));
			});

			// The state overloads hand the callback what it needs, so the lambdas capture nothing and no
			// closure is allocated per node per frame. Worth reaching for once a tree gets large.
			ImGuiWidgets.Tree.Branch("Vegetables", ImGuiTreeNodeFlags.DefaultOpen, "Carrot", static label =>
				ImGuiWidgets.Tree.Leaf(label, static text => DemoProbe.Button(text)));

			ImGui.Separator();
			ImGui.TextUnformatted("The scope form, for drawing rows without a callback:");
			ImGui.Separator();

			using ImGuiWidgets.Tree tree = new();
			for (int i = 0; i < 3; i++)
			{
				using (tree.Child)
				{
					DemoProbe.Button($"Parent Node {i + 1}");

					using ImGuiWidgets.Tree subtree = new();
					for (int j = 0; j < 2; j++)
					{
						using (subtree.Child)
						{
							DemoProbe.Button($"Child {j + 1}");

							if (i == 0 && j == 0) // Show deeper nesting for first item
							{
								using ImGuiWidgets.Tree deepTree = new();
								using (deepTree.Child)
								{
									DemoProbe.Button("Grandchild");
								}
							}
						}
					}
				}
			}
		}
	}
}
