// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Probes;
using ktsu.Semantics.Color;

/// <summary>
/// Provides custom ImGui widgets.
/// </summary>
public static partial class ImGuiWidgets
{
	/// <summary>
	/// Draws one editor for a value of type <typeparamref name="T"/> as a property grid row.
	/// </summary>
	/// <remarks>
	/// Every row method of <see cref="ImGuiWidgets.PropertyGrid"/> that takes a label and a value has
	/// this shape, so <c>grid.Int</c>, <c>grid.Color</c> and the rest convert to it directly and a
	/// list of them needs no adapter.
	/// </remarks>
	/// <typeparam name="T">The type of the value being edited.</typeparam>
	/// <param name="label">The row's label.</param>
	/// <param name="value">The value being edited.</param>
	/// <returns><see langword="true"/> if the value changed this frame; otherwise <see langword="false"/>.</returns>
	public delegate bool PropertyRow<T>(string label, ref T value);

	public sealed partial class PropertyGrid
	{
		/// <summary>
		/// Draws a collapsible list row: one editor per element, a button to append an element, and a
		/// button on each element to remove it.
		/// </summary>
		/// <remarks>
		/// The elements are edited in place through <paramref name="drawItem"/>, which is any row
		/// method of this grid, so a list of a supported type needs nothing written for it. Additions
		/// and removals are applied to <paramref name="items"/> after the loop that drew it, so the
		/// collection is never modified while it is being read.
		/// </remarks>
		/// <typeparam name="T">The element type.</typeparam>
		/// <param name="label">The property name; text after <c>##</c> is hidden but used for the id.</param>
		/// <param name="items">The list being edited.</param>
		/// <param name="drawItem">Draws one element's editor.</param>
		/// <param name="createItem">Produces the element an append adds.</param>
		/// <returns><see langword="true"/> if an element was edited, added or removed this frame; otherwise <see langword="false"/>.</returns>
		public bool List<T>(string label, IList<T> items, PropertyRow<T> drawItem, Func<T> createItem)
		{
			Ensure.NotNull(label);
			Ensure.NotNull(items);
			Ensure.NotNull(drawItem);
			Ensure.NotNull(createItem);

			if (!CanDraw)
			{
				return false;
			}

			float buttonWidth = ImGui.GetFrameHeight();
			float spacing = ImGui.GetStyle().ItemInnerSpacing.X;

			ImGui.TableNextRow();
			ImGui.TableSetColumnIndex(0);
			ImGui.AlignTextToFramePadding();
			bool open = ImGui.TreeNodeEx(label, Options.ListsStartExpanded ? ImGuiTreeNodeFlags.DefaultOpen : ImGuiTreeNodeFlags.None);
			ImGuiProbes.MarkItem(VisibleLabel(label));

			ImGui.TableSetColumnIndex(1);
			ImGui.AlignTextToFramePadding();
			ImGui.TextUnformatted(items.Count == 1
				? "1 item"
				: string.Create(CultureInfo.CurrentCulture, $"{items.Count} items"));

			bool changed = false;
			int removeAt = -1;

			using (new ScopedDisable(Options.ReadOnly))
			{
				ImGui.SameLine(0f, spacing);
				ImGui.SetCursorPosX(ImGui.GetCursorPosX() + Math.Max(0f, ImGui.GetContentRegionAvail().X - buttonWidth));

				// Labelled per list for the same reason the element buttons are labelled per index:
				// the add buttons of two lists in one grid would otherwise be one item.
				if (ImGui.Button($"+##{label}/add", new Vector2(buttonWidth, 0f)))
				{
					items.Add(createItem());
					changed = true;
				}

				ImGuiProbes.MarkItem($"{VisibleLabel(label)}/add");
			}

			if (open)
			{
				using (new ScopedId(label))
				{
					for (int i = 0; i < items.Count; i++)
					{
						T item = items[i];
						string itemLabel = string.Create(CultureInfo.InvariantCulture, $"[{i}]");

						// Keep the element's editor clear of the remove button drawn after it.
						trailingReserve = buttonWidth + spacing;
						bool itemChanged = drawItem(itemLabel, ref item);
						trailingReserve = 0f;

						if (itemChanged)
						{
							items[i] = item;
							changed = true;
						}

						using (new ScopedDisable(Options.ReadOnly))
						{
							ImGui.SameLine(0f, spacing);
							if (ImGui.Button($"x##remove{i}", new Vector2(buttonWidth, 0f)))
							{
								removeAt = i;
							}

							ImGuiProbes.MarkItem($"{itemLabel}/remove");
						}
					}
				}

				ImGui.TreePop();
			}

			if (removeAt >= 0)
			{
				items.RemoveAt(removeAt);
				changed = true;
			}

			Changed |= changed;
			return changed;
		}

		/// <summary>Draws a list of checkboxes.</summary>
		/// <param name="label">The property name; text after <c>##</c> is hidden but used for the id.</param>
		/// <param name="items">The list being edited.</param>
		/// <returns><see langword="true"/> if the list changed this frame; otherwise <see langword="false"/>.</returns>
		public bool List(string label, IList<bool> items) => List(label, items, Value, CreateDefault<bool>);

		/// <summary>Draws a list of integers.</summary>
		/// <param name="label">The property name; text after <c>##</c> is hidden but used for the id.</param>
		/// <param name="items">The list being edited.</param>
		/// <returns><see langword="true"/> if the list changed this frame; otherwise <see langword="false"/>.</returns>
		public bool List(string label, IList<int> items) => List(label, items, Value, CreateDefault<int>);

		/// <summary>Draws a list of 64-bit integers.</summary>
		/// <param name="label">The property name; text after <c>##</c> is hidden but used for the id.</param>
		/// <param name="items">The list being edited.</param>
		/// <returns><see langword="true"/> if the list changed this frame; otherwise <see langword="false"/>.</returns>
		public bool List(string label, IList<long> items) => List(label, items, Value, CreateDefault<long>);

		/// <summary>Draws a list of single-precision values.</summary>
		/// <param name="label">The property name; text after <c>##</c> is hidden but used for the id.</param>
		/// <param name="items">The list being edited.</param>
		/// <returns><see langword="true"/> if the list changed this frame; otherwise <see langword="false"/>.</returns>
		public bool List(string label, IList<float> items) => List(label, items, Value, CreateDefault<float>);

		/// <summary>Draws a list of double-precision values.</summary>
		/// <param name="label">The property name; text after <c>##</c> is hidden but used for the id.</param>
		/// <param name="items">The list being edited.</param>
		/// <returns><see langword="true"/> if the list changed this frame; otherwise <see langword="false"/>.</returns>
		public bool List(string label, IList<double> items) => List(label, items, Value, CreateDefault<double>);

		/// <summary>Draws a list of text fields.</summary>
		/// <param name="label">The property name; text after <c>##</c> is hidden but used for the id.</param>
		/// <param name="items">The list being edited.</param>
		/// <returns><see langword="true"/> if the list changed this frame; otherwise <see langword="false"/>.</returns>
		public bool List(string label, IList<string> items) => List(label, items, Value, CreateEmptyString);

		/// <summary>Draws a list of two-component single-precision vectors.</summary>
		/// <param name="label">The property name; text after <c>##</c> is hidden but used for the id.</param>
		/// <param name="items">The list being edited.</param>
		/// <returns><see langword="true"/> if the list changed this frame; otherwise <see langword="false"/>.</returns>
		public bool List(string label, IList<Vector2> items) => List(label, items, Value, CreateDefault<Vector2>);

		/// <summary>Draws a list of three-component single-precision vectors.</summary>
		/// <param name="label">The property name; text after <c>##</c> is hidden but used for the id.</param>
		/// <param name="items">The list being edited.</param>
		/// <returns><see langword="true"/> if the list changed this frame; otherwise <see langword="false"/>.</returns>
		public bool List(string label, IList<Vector3> items) => List(label, items, Value, CreateDefault<Vector3>);

		/// <summary>Draws a list of two-component double-precision vectors.</summary>
		/// <param name="label">The property name; text after <c>##</c> is hidden but used for the id.</param>
		/// <param name="items">The list being edited.</param>
		/// <returns><see langword="true"/> if the list changed this frame; otherwise <see langword="false"/>.</returns>
		public bool List(string label, IList<DoubleVector2> items) => List(label, items, Value, CreateDefault<DoubleVector2>);

		/// <summary>Draws a list of three-component double-precision vectors.</summary>
		/// <param name="label">The property name; text after <c>##</c> is hidden but used for the id.</param>
		/// <param name="items">The list being edited.</param>
		/// <returns><see langword="true"/> if the list changed this frame; otherwise <see langword="false"/>.</returns>
		public bool List(string label, IList<DoubleVector3> items) => List(label, items, Value, CreateDefault<DoubleVector3>);

		/// <summary>Draws a list of colors.</summary>
		/// <param name="label">The property name; text after <c>##</c> is hidden but used for the id.</param>
		/// <param name="items">The list being edited.</param>
		/// <returns><see langword="true"/> if the list changed this frame; otherwise <see langword="false"/>.</returns>
		public bool List(string label, IList<Color> items) => List(label, items, Value, CreateWhite);

		/// <summary>Draws a list of file paths.</summary>
		/// <param name="label">The property name; text after <c>##</c> is hidden but used for the id.</param>
		/// <param name="items">The list being edited.</param>
		/// <returns><see langword="true"/> if the list changed this frame; otherwise <see langword="false"/>.</returns>
		public bool FilePathList(string label, IList<string> items) => List(label, items, FilePath, CreateEmptyString);

		/// <summary>Draws a list of directory paths.</summary>
		/// <param name="label">The property name; text after <c>##</c> is hidden but used for the id.</param>
		/// <param name="items">The list being edited.</param>
		/// <returns><see langword="true"/> if the list changed this frame; otherwise <see langword="false"/>.</returns>
		public bool DirectoryPathList(string label, IList<string> items) => List(label, items, DirectoryPath, CreateEmptyString);

		/// <summary>Draws a list of image paths, each with its own thumbnail.</summary>
		/// <param name="label">The property name; text after <c>##</c> is hidden but used for the id.</param>
		/// <param name="items">The list being edited.</param>
		/// <returns><see langword="true"/> if the list changed this frame; otherwise <see langword="false"/>.</returns>
		public bool ImagePathList(string label, IList<string> items) => List(label, items, ImagePath, CreateEmptyString);

		/// <summary>Produces the zero value an appended element of a value type starts at.</summary>
		/// <typeparam name="T">The element type.</typeparam>
		/// <returns>The default value.</returns>
		private static T CreateDefault<T>() where T : struct => default;

		/// <summary>Produces the empty string an appended text or path element starts at.</summary>
		/// <returns>An empty string.</returns>
		private static string CreateEmptyString() => string.Empty;

		/// <summary>Produces the opaque white an appended color element starts at.</summary>
		/// <returns>Opaque white.</returns>
		private static Color CreateWhite() => Color.FromSrgb(1f, 1f, 1f, 1f);
	}
}
