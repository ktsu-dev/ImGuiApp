// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;
using System.Collections.Generic;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Probes;
using ktsu.ImGui.Widgets.Overlays;

/// <summary>
/// Provides custom ImGui widgets.
/// </summary>
public static partial class ImGuiWidgets
{
	/// <summary>
	/// A scrolling list in sections, whose section headers stick to the top of the view while their
	/// section scrolls beneath, the next header pushing the last one up and out as it arrives. Draw the
	/// content inside a <c>using</c> block, starting each section with <see cref="Section"/>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Each header is probe-marked as <c>{id}/{title}</c> where it sits in the list, and the header
	/// pinned to the top, while one is, as <c>{id}/pinned</c>. <see cref="PinnedTitle"/> reports which
	/// section that was, once the block has ended.
	/// </para>
	/// <code>
	/// using (ImGuiWidgets.StickyHeaderList contacts = new("contacts", new Vector2(0f, 300f)))
	/// {
	///     foreach (IGrouping&lt;char, string&gt; letter in names)
	///     {
	///         contacts.Section(letter.Key.ToString());
	///         foreach (string name in letter) { ImGui.TextUnformatted(name); }
	///     }
	/// }
	/// </code>
	/// </remarks>
	public sealed class StickyHeaderList : IDisposable
	{
		private readonly string id;
		private readonly List<float> tops = [];
		private readonly List<string> titles = [];
		private readonly float headerHeight;
		private float headerLeft;
		private float headerWidth;
		private bool disposed;

		/// <summary>Begins a list. Call inside a <c>using</c> block.</summary>
		/// <param name="id">A unique id for the list, also its probe name.</param>
		/// <param name="size">The list's size. A zero component fills the space available, as for a child window.</param>
		public StickyHeaderList(string id, Vector2 size)
		{
			Ensure.NotNull(id);
			this.id = id;
			headerHeight = ImGui.GetFrameHeight();
			ImGui.BeginChild(id, size, ImGuiChildFlags.None, ImGuiWindowFlags.None);
			Vector2 windowMin = ImGui.GetWindowPos();
			ImGuiProbes.MarkRegion(id, windowMin, windowMin + ImGui.GetWindowSize());
		}

		/// <summary>Gets the title of the section whose header was pinned to the top, or null when none was. Known once the block has ended.</summary>
		public string? PinnedTitle { get; private set; }

		/// <summary>Starts a section, drawing its header.</summary>
		/// <param name="title">The section's title.</param>
		public void Section(string title)
		{
			Ensure.NotNull(title);
			ObjectDisposedException.ThrowIf(disposed, this);

			tops.Add(ImGui.GetCursorPosY());
			titles.Add(title);
			Vector2 min = ImGui.GetCursorScreenPos();
			float width = Math.Max(ImGui.GetContentRegionAvail().X, 1f);
			headerLeft = min.X;
			headerWidth = width;
			ImGui.Dummy(new Vector2(width, headerHeight));
			ImGuiProbes.MarkItem(id, title);
			DrawHeader(min, width, title);
		}

		/// <summary>Ends the list, pinning the header of the section at the top of the view.</summary>
		public void Dispose()
		{
			if (disposed)
			{
				return;
			}

			disposed = true;
			float scroll = ImGui.GetScrollY();
			(int index, float top) = StickyHeaderGeometry.Pin(tops, headerHeight, scroll);
			if (index >= 0)
			{
				PinnedTitle = titles[index];
				Vector2 min = new(headerLeft, ImGui.GetWindowPos().Y + top - scroll);
				DrawHeader(min, headerWidth, titles[index]);
				ImGuiProbes.MarkRegion($"{id}/pinned", min, min + new Vector2(headerWidth, headerHeight));
			}

			ImGui.EndChild();
		}

		private void DrawHeader(Vector2 min, float width, string title)
		{
			ImDrawListPtr drawList = ImGui.GetWindowDrawList();
			Vector2 max = min + new Vector2(width, headerHeight);
			drawList.AddRectFilled(min, max, OverlaySurface.Opaque(ImGuiCol.WindowBg));
			drawList.AddRectFilled(min, max, ImGui.GetColorU32(ImGuiCol.TableHeaderBg));
			Vector2 textSize = ImGui.CalcTextSize(title);
			drawList.AddText(new Vector2(min.X + ImGui.GetStyle().FramePadding.X, min.Y + ((headerHeight - textSize.Y) * 0.5f)), ImGui.GetColorU32(ImGuiCol.Text), title);
			drawList.AddLine(new Vector2(min.X, max.Y - 0.5f), new Vector2(max.X, max.Y - 0.5f), ImGui.GetColorU32(ImGuiCol.TableBorderLight));
		}
	}
}
