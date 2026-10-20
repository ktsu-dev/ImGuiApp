// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Probes;
using ktsu.ImGui.Widgets.Overlays;
using ktsu.TextFilter;

/// <summary>
/// Provides custom ImGui widgets.
/// </summary>
public static partial class ImGuiWidgets
{
	/// <summary>
	/// A search that covers the whole window: a search box along the top, with a back button, and the
	/// matching items listed beneath it, as a phone's search screen is.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The overlay is drawn through an <see cref="OverlayHost"/>, on <see cref="OverlayLayer.Dialog"/>, so
	/// call the host's <see cref="OverlayHost.Render"/> once a frame. It opens with an empty query and the
	/// keyboard in the search box, filters with <see cref="SearchBox{T}"/>'s fuzzy filter, and lists every
	/// item while nothing has been typed. Clicking a result, or Enter on the highlighted one, reports it and
	/// closes the overlay; the up and down arrows move the highlight. The back button or Escape closes it
	/// without an answer.
	/// </para>
	/// <para>
	/// The overlay is probe-marked under its id, the search box under <c>id/query</c>, the back button under
	/// <c>id/back</c> and each result under <c>id/text</c>.
	/// </para>
	/// </remarks>
	/// <param name="host">The overlay host that draws the overlay.</param>
	/// <param name="id">A unique id for the overlay.</param>
	public sealed class SearchOverlay(OverlayHost host, string id)
	{
		private readonly SearchOverlayState state = new();
		private readonly string key = $"{nameof(SearchOverlay)}/{id}";
		private SearchBoxOptions options = new();
		private List<string> items = [];
		private List<string> results = [];
		private Action<string>? onChosen;
		private bool opening;

		/// <summary>Gets a value indicating whether the overlay is open.</summary>
		public bool IsOpen => state.IsOpen;

		/// <summary>Gets what has been typed.</summary>
		public string Query => state.Query;

		/// <summary>Opens the overlay over a set of items.</summary>
		/// <param name="items">The items to search. Repeated items are listed once.</param>
		/// <param name="onChosen">Called with the item chosen. Not called when the overlay is closed without a choice.</param>
		/// <param name="hint">The text shown in the empty search box.</param>
		public void Open(IEnumerable<string> items, Action<string> onChosen, string hint = "Search")
		{
			Ensure.NotNull(items);
			Ensure.NotNull(onChosen);

			this.items = [.. items.Distinct(StringComparer.Ordinal)];
			this.onChosen = onChosen;
			options = new SearchBoxOptions(
				Label: $"##{id}query",
				Hint: hint,
				FilterType: TextFilterType.Fuzzy,
				ShowTooltip: false,
				ShowContextMenu: false,
				ReturnAllWhenEmpty: true);
			results = [.. this.items];
			opening = true;
			state.Open();
			host.Show(key, Draw, OverlayLayer.Dialog);
		}

		/// <summary>Closes the overlay without an answer.</summary>
		public void Close()
		{
			onChosen = null;
			state.Close();
		}

		private void Choose(string item)
		{
			Action<string>? callback = onChosen;
			Close();
			callback?.Invoke(item);
		}

		private void Draw()
		{
			if (state.Transition.Update(ImGui.GetIO().DeltaTime) || !state.Transition.IsVisible)
			{
				host.Dismiss(key);
				return;
			}

			bool focus = opening;
			OverlaySurface.BeginFullscreen($"##{key}", focus);
			opening = false;

			float shown = state.Transition.Eased;
			ImGuiViewportPtr viewport = ImGui.GetMainViewport();
			ImGui.GetWindowDrawList().AddRectFilled(viewport.Pos, viewport.Pos + viewport.Size, OverlaySurface.Opaque(ImGuiCol.WindowBg, shown));
			ImGuiProbes.MarkRegion(id, viewport.Pos, viewport.Pos + viewport.Size);

			ImGui.PushStyleVar(ImGuiStyleVar.Alpha, shown);
			Vector2 padding = ImGui.GetStyle().WindowPadding;
			ImGui.SetCursorScreenPos(viewport.Pos + padding - new Vector2(0f, (1f - shown) * InterfaceScale.Pixels(24f)));
			ImGui.BeginGroup();
			DrawSearchRow(focus, viewport.Size.X - (padding.X * 2f));
			ImGui.Separator();
			DrawResults(new Vector2(viewport.Size.X - (padding.X * 2f), MathF.Max(1f, ImGui.GetContentRegionAvail().Y - padding.Y)));
			ImGui.EndGroup();
			ImGui.PopStyleVar();

			if (state.IsOpen && ImGui.IsKeyPressed(ImGuiKey.Escape))
			{
				Close();
			}

			ImGui.End();
		}

		private void DrawSearchRow(bool focus, float width)
		{
			if (ImGui.ArrowButton($"##{id}back", ImGuiDir.Left) && state.IsOpen)
			{
				Close();
			}

			ImGuiProbes.MarkItem(id, "back");
			ImGui.SameLine();

			if (focus)
			{
				ImGui.SetKeyboardFocusHere();
			}

			string query = state.Query;
			ImGui.PushItemWidth(width - ImGui.GetFrameHeight() - ImGui.GetStyle().ItemSpacing.X);
			List<string> matches = [.. SearchBox(ref options, ref query, items, item => item)];
			ImGuiProbes.MarkItem(id, "query");
			ImGui.PopItemWidth();

			if (query != state.Query || matches.Count != results.Count)
			{
				state.Query = query;
				results = matches;
				state.ResultsChanged(results.Count);
			}

			if (!state.IsOpen)
			{
				return;
			}

			if (ImGui.IsKeyPressed(ImGuiKey.DownArrow))
			{
				state.MoveHighlight(1, results.Count);
			}

			if (ImGui.IsKeyPressed(ImGuiKey.UpArrow))
			{
				state.MoveHighlight(-1, results.Count);
			}

			if ((ImGui.IsKeyPressed(ImGuiKey.Enter) || ImGui.IsKeyPressed(ImGuiKey.KeypadEnter)) && state.Highlighted >= 0)
			{
				Choose(results[state.Highlighted]);
			}
		}

		private void DrawResults(Vector2 size)
		{
			if (!ImGui.BeginChild("##results", size, ImGuiChildFlags.None, ImGuiWindowFlags.NoBackground))
			{
				ImGui.EndChild();
				return;
			}

			float rowHeight = InterfaceScale.Pixels(36f);
			for (int i = 0; i < results.Count; i++)
			{
				string item = results[i];
				bool highlighted = i == state.Highlighted;
				ImGui.PushID(i);
				if (ImGui.Selectable(item, highlighted, ImGuiSelectableFlags.None, new Vector2(0f, rowHeight)) && state.IsOpen)
				{
					Choose(item);
				}

				ImGui.PopID();
				ImGuiProbes.MarkItem(id, item);
				if (highlighted && (ImGui.IsKeyPressed(ImGuiKey.DownArrow) || ImGui.IsKeyPressed(ImGuiKey.UpArrow)))
				{
					ImGui.SetScrollHereY();
				}
			}

			if (results.Count == 0)
			{
				ImGui.TextDisabled(items.Count == 0 ? "Nothing to search" : "No matches");
			}

			ImGui.EndChild();
		}
	}
}
