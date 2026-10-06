// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Probes;

/// <summary>
/// Provides custom ImGui widgets.
/// </summary>
public static partial class ImGuiWidgets
{
	/// <summary>
	/// Options for <see cref="AssetBrowser(string, int, Func{int, string}, Func{int, nint}, AssetBrowserState, AssetBrowserOptions?)"/>.
	/// </summary>
	public sealed class AssetBrowserOptions
	{
		/// <summary>The longest payload type Dear ImGui accepts; its payload type buffer is 32 characters plus a terminator.</summary>
		internal const int MaxPayloadTypeLength = 32;

		/// <summary>The tile size used when <see cref="TileSize"/> is not a positive, finite number.</summary>
		internal const float DefaultTileSize = 96f;

		/// <summary>
		/// Gets or sets the width and thumbnail height of one tile, in pixels. Ctrl+wheel over the browser
		/// changes it, between <see cref="MinTileSize"/> and <see cref="MaxTileSize"/>.
		/// </summary>
		public float TileSize { get; set; } = DefaultTileSize;

		/// <summary>Gets the smallest tile Ctrl+wheel will shrink to.</summary>
		public float MinTileSize { get; init; } = 32f;

		/// <summary>Gets the largest tile Ctrl+wheel will grow to.</summary>
		public float MaxTileSize { get; init; } = 256f;

		/// <summary>Gets the size of the browser. Either axis may be zero to take the available space.</summary>
		public Vector2 Size { get; init; }

		/// <summary>
		/// Gets the drag-and-drop payload type a drag out of the browser carries. At most 32 characters,
		/// which is Dear ImGui's own limit. Read a drop with <see cref="TryAcceptAssetPayload"/>.
		/// </summary>
		public string DragDropPayloadType { get; init; } = "KTSU_ASSETS";

		/// <summary>Gets the tooltip for an item, or <see langword="null"/> to show a clipped label in full.</summary>
		public Func<int, string>? Tooltip { get; init; }

		/// <summary>Gets the callback raised when an item is double-clicked, or Enter is pressed on the focused one.</summary>
		public Action<int>? OnActivate { get; init; }
	}

	/// <summary>
	/// Draws a virtualised grid of thumbnails with a label under each, with file-manager selection:
	/// click, Ctrl+click and Shift+click, the arrow keys, Home and End, Ctrl+A and Escape. Ctrl+wheel
	/// resizes the tiles, and dragging a tile drags the whole selection out as a payload.
	/// </summary>
	/// <param name="label">The identifier, which the browser also marks itself under for probes.</param>
	/// <param name="itemCount">The number of items.</param>
	/// <param name="getLabel">Returns the label for an item. Called only for tiles on screen.</param>
	/// <param name="resolveThumbnail">
	/// Returns the texture for an item, or zero for none, which draws an empty frame. Called only for
	/// tiles on screen, so it can load lazily. An exception it throws is not caught.
	/// </param>
	/// <param name="state">The selection and focus, which the caller owns.</param>
	/// <param name="options">The options, or <see langword="null"/> for defaults kept per browser.</param>
	/// <returns><see langword="true"/> on a frame the selection changed.</returns>
	/// <remarks>
	/// Only the rows in view are submitted, so the cost of a frame follows the height of the browser
	/// rather than <paramref name="itemCount"/>. Every tile is marked <c>{label}/[i]</c>.
	/// </remarks>
	/// <exception cref="ArgumentNullException">A required argument is <see langword="null"/>.</exception>
	/// <exception cref="ArgumentException">The payload type is longer than 32 characters.</exception>
	public static bool AssetBrowser(
		string label,
		int itemCount,
		Func<int, string> getLabel,
		Func<int, nint> resolveThumbnail,
		AssetBrowserState state,
		AssetBrowserOptions? options = null)
	{
		Ensure.NotNull(label);
		Ensure.NotNull(getLabel);
		Ensure.NotNull(resolveThumbnail);
		Ensure.NotNull(state);

		return AssetBrowserImpl.Show(label, itemCount, getLabel, resolveThumbnail, state, options);
	}

	/// <summary>
	/// Accepts an asset browser's drag payload on the current drop target, between
	/// <c>BeginDragDropTarget</c> and <c>EndDragDropTarget</c>.
	/// </summary>
	/// <param name="payloadType">The payload type the browser was given.</param>
	/// <param name="indices">The dragged item indices, ascending, on the frame the drop is delivered; otherwise empty.</param>
	/// <returns><see langword="true"/> on the frame the drop is delivered.</returns>
	public static bool TryAcceptAssetPayload(string payloadType, out IReadOnlyList<int> indices)
	{
		Ensure.NotNull(payloadType);
		return AssetBrowserImpl.TryAccept(payloadType, out indices);
	}

	internal static class AssetBrowserImpl
	{
		private const float WheelStep = 1.1f;
		private const float DragThumbnailSize = 48f;
		private const float SizeTolerance = 1e-3f;

		private static readonly Dictionary<uint, AssetBrowserOptions> DefaultOptions = [];

		internal static bool Show(
			string label,
			int itemCount,
			Func<int, string> getLabel,
			Func<int, nint> resolveThumbnail,
			AssetBrowserState state,
			AssetBrowserOptions? options)
		{
			options ??= OptionsFor(label);

			if (options.DragDropPayloadType.Length > AssetBrowserOptions.MaxPayloadTypeLength)
			{
				throw new ArgumentException(
					$"The drag-and-drop payload type is {options.DragDropPayloadType.Length} characters; Dear ImGui accepts at most {AssetBrowserOptions.MaxPayloadTypeLength}.",
					nameof(options));
			}

			int count = Math.Max(itemCount, 0);
			bool changed = state.ItemCountChanged(count);

			// Keyboard navigation is the browser's own, so ImGui's would move a second focus through the
			// tiles on the same key press if a host turned it on.
			ImGui.BeginChild(label, options.Size, ImGuiChildFlags.Borders, ImGuiWindowFlags.NoNavInputs);
			using (new ScopedId(label))
			{
				changed |= DrawContents(count, getLabel, resolveThumbnail, state, options);
			}

			ImGui.EndChild();
			ImGuiProbes.MarkItem(label);

			return changed;
		}

		[SuppressMessage("Major Code Smell", "S6640:Make sure that using \"unsafe\" is safe here", Justification = "Reads ImGui's own payload buffer, bounded by the DataSize ImGui reports, and copies it out before returning; no pointer is retained.")]
		internal static bool TryAccept(string payloadType, out IReadOnlyList<int> indices)
		{
			ImGuiPayloadPtr payload = ImGui.AcceptDragDropPayload(payloadType);
			indices = [];

			unsafe
			{
				if (payload.Handle is null || !payload.IsDelivery() || payload.DataSize % sizeof(int) != 0)
				{
					return false;
				}

				indices = Decode(new ReadOnlySpan<byte>(payload.Data, payload.DataSize));
			}

			return true;
		}

		/// <summary>Writes indices as consecutive little-endian 32-bit integers.</summary>
		internal static byte[] Encode(IReadOnlyList<int> indices)
		{
			byte[] bytes = new byte[indices.Count * sizeof(int)];
			for (int i = 0; i < indices.Count; i++)
			{
				BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(i * sizeof(int)), indices[i]);
			}

			return bytes;
		}

		/// <summary>Reads what <see cref="Encode"/> wrote.</summary>
		internal static int[] Decode(ReadOnlySpan<byte> bytes)
		{
			int[] indices = new int[bytes.Length / sizeof(int)];
			for (int i = 0; i < indices.Length; i++)
			{
				indices[i] = BinaryPrimitives.ReadInt32LittleEndian(bytes[(i * sizeof(int))..]);
			}

			return indices;
		}

		/// <summary>Returns the tile size to draw at: the option when it is usable, the default when it is not.</summary>
		internal static float EffectiveTileSize(AssetBrowserOptions options) =>
			float.IsFinite(options.TileSize) && options.TileSize > 0f ? options.TileSize : AssetBrowserOptions.DefaultTileSize;

		/// <summary>Returns the tile size after <paramref name="wheel"/> wheel clicks, inside the configured bounds.</summary>
		internal static float ResizedTile(float tileSize, float wheel, AssetBrowserOptions options)
		{
			float low = Math.Min(options.MinTileSize, options.MaxTileSize);
			float high = Math.Max(options.MinTileSize, options.MaxTileSize);
			return Math.Clamp(tileSize * MathF.Pow(WheelStep, wheel), low, high);
		}

		private static AssetBrowserOptions OptionsFor(string label)
		{
			uint id = ImGui.GetID(label);
			if (!DefaultOptions.TryGetValue(id, out AssetBrowserOptions? options))
			{
				options = new();
				DefaultOptions[id] = options;
			}

			return options;
		}

		private static bool DrawContents(
			int count,
			Func<int, string> getLabel,
			Func<int, nint> resolveThumbnail,
			AssetBrowserState state,
			AssetBrowserOptions options)
		{
			ImGuiStylePtr style = ImGui.GetStyle();
			float tileSize = EffectiveTileSize(options);
			Grid grid = Grid.Measure(tileSize, style);

			// Resizing is not a selection change, so it does not count towards the return value.
			if (HandleResize(options, tileSize, grid, style))
			{
				grid = Grid.Measure(EffectiveTileSize(options), style);
			}

			if (count == 0)
			{
				return false;
			}

			bool focused = ImGui.IsWindowFocused();
			int rows = AssetBrowserState.RowCount(count, grid.Columns);
			bool changed = false;

			ImGuiListClipper clipper = default;
			clipper.Begin(rows, grid.RowHeight);

			while (clipper.Step())
			{
				for (int row = clipper.DisplayStart; row < clipper.DisplayEnd; row++)
				{
					int first = row * grid.Columns;
					int last = Math.Min(first + grid.Columns, count);
					for (int index = first; index < last; index++)
					{
						if (index > first)
						{
							ImGui.SameLine(0f, style.ItemSpacing.X);
						}

						changed |= DrawTile(index, getLabel, resolveThumbnail, state, options, grid, focused);
					}
				}
			}

			clipper.End();

			if (ImGui.IsWindowHovered() && !ImGui.IsAnyItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
			{
				changed |= state.Click(-1, ctrl: false, shift: false);
			}

			if (focused)
			{
				changed |= HandleKeys(count, state, options, grid);
			}

			return changed;
		}

		private static bool DrawTile(
			int index,
			Func<int, string> getLabel,
			Func<int, nint> resolveThumbnail,
			AssetBrowserState state,
			AssetBrowserOptions options,
			Grid grid,
			bool focused)
		{
			string id = "[" + index.ToString(CultureInfo.InvariantCulture) + "]";
			Vector2 min = ImGui.GetCursorScreenPos();

			_ = ImGui.InvisibleButton(id, new Vector2(grid.TileSize, grid.TileHeight));
			ImGuiProbes.MarkItem(id);

			bool changed = HandleTileMouse(index, state, options);

			bool hovered = ImGui.IsItemHovered();
			bool selected = state.IsSelected(index);
			Vector2 max = min + new Vector2(grid.TileSize, grid.TileHeight);

			ImDrawListPtr drawList = ImGui.GetWindowDrawList();
			ImGuiStylePtr style = ImGui.GetStyle();

			if (selected || hovered)
			{
				drawList.AddRectFilled(min, max, ImGui.GetColorU32(selected ? ImGuiCol.Header : ImGuiCol.HeaderHovered), style.FrameRounding);
			}

			if (focused && index == state.FocusIndex)
			{
				drawList.AddRect(min, max, ImGui.GetColorU32(ImGuiCol.NavCursor), style.FrameRounding, ImDrawFlags.None, 1f);
			}

			float padding = style.FramePadding.X;
			float thumbnail = Math.Max(grid.TileSize - (2f * padding), 1f);
			Vector2 thumbMin = min + new Vector2((grid.TileSize - thumbnail) * 0.5f, padding);
			DrawThumbnail(drawList, resolveThumbnail(index), thumbMin, thumbMin + new Vector2(thumbnail, thumbnail));

			string text = getLabel(index) ?? string.Empty;
			float lineHeight = ImGui.GetTextLineHeight();
			string shown = TextImpl.Clip(text, new Vector2(grid.TileSize, lineHeight));
			float shownWidth = ImGui.CalcTextSize(shown).X;
			Vector2 textPos = new(min.X + ((grid.TileSize - shownWidth) * 0.5f), max.Y - lineHeight);
			drawList.AddText(textPos, ImGui.GetColorU32(ImGuiCol.Text), shown);

			if (ImGui.IsItemHovered(ImGuiHoveredFlags.DelayNormal) && !ImGuiP.IsDragDropActive())
			{
				string? tooltip = options.Tooltip?.Invoke(index) ?? (shown == text ? null : text);
				if (!string.IsNullOrEmpty(tooltip) && ImGui.BeginTooltip())
				{
					ImGui.TextUnformatted(tooltip);
					ImGui.EndTooltip();
				}
			}

			changed |= HandleDragSource(index, state, options, resolveThumbnail);

			return changed;
		}

		private static bool HandleTileMouse(int index, AssetBrowserState state, AssetBrowserOptions options)
		{
			bool changed = false;
			ImGuiIOPtr io = ImGui.GetIO();

			if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
			{
				// A plain press on a tile that is already selected waits for the release: it may be the
				// start of a drag, which carries the whole selection and must not collapse it first.
				if (!io.KeyCtrl && !io.KeyShift && state.IsSelected(index))
				{
					state.PendingClick = index;
					state.DragStarted = false;
				}
				else
				{
					changed |= state.Click(index, io.KeyCtrl, io.KeyShift);
				}
			}

			if (state.PendingClick == index && ImGui.IsItemDeactivated())
			{
				if (!state.DragStarted)
				{
					changed |= state.Click(index, ctrl: false, shift: false);
				}

				state.PendingClick = -1;
				state.DragStarted = false;
			}

			if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
			{
				options.OnActivate?.Invoke(index);
			}

			return changed;
		}

		[SuppressMessage("Major Code Smell", "S6640:Make sure that using \"unsafe\" is safe here", Justification = "SetDragDropPayload takes a pointer; ImGui copies the bytes before the fixed block ends and retains no pointer to them.")]
		private static bool HandleDragSource(int index, AssetBrowserState state, AssetBrowserOptions options, Func<int, nint> resolveThumbnail)
		{
			if (!ImGui.BeginDragDropSource(ImGuiDragDropFlags.None))
			{
				return false;
			}

			bool changed = false;
			if (!state.IsSelected(index))
			{
				changed = state.Click(index, ctrl: false, shift: false);
			}

			state.DragStarted = true;

			byte[] payload = Encode(state.SelectedIndices);
			unsafe
			{
				fixed (byte* data = payload)
				{
					_ = ImGui.SetDragDropPayload(options.DragDropPayloadType, data, (nuint)payload.Length);
				}
			}

			nint thumbnail = resolveThumbnail(index);
			if (thumbnail != 0)
			{
				unsafe
				{
					ImGui.Image(new ImTextureRef(texId: thumbnail), new Vector2(DragThumbnailSize, DragThumbnailSize));
				}

				ImGui.SameLine();
			}

			int dragged = state.SelectedIndices.Count;
			ImGui.TextUnformatted(dragged == 1 ? "1 item" : dragged.ToString(CultureInfo.InvariantCulture) + " items");

			ImGui.EndDragDropSource();
			return changed;
		}

		[SuppressMessage("Major Code Smell", "S6640:Make sure that using \"unsafe\" is safe here", Justification = "Required for native Hexa.NET.ImGui image interop (ImTextureRef); the block is scoped to the draw call and retains no pointers.")]
		private static void DrawThumbnail(ImDrawListPtr drawList, nint texture, Vector2 min, Vector2 max)
		{
			if (texture == 0)
			{
				drawList.AddRect(min, max, ImGui.GetColorU32(ImGuiCol.Border));
				return;
			}

			unsafe
			{
				drawList.AddImage(new ImTextureRef(texId: texture), min, max);
			}
		}

		private static bool HandleKeys(int count, AssetBrowserState state, AssetBrowserOptions options, Grid grid)
		{
			ImGuiIOPtr io = ImGui.GetIO();
			if (io.WantTextInput)
			{
				return false;
			}

			bool shift = io.KeyShift;
			int focusBefore = state.FocusIndex;
			bool changed = false;

			if (ImGui.IsKeyPressed(ImGuiKey.LeftArrow, true))
			{
				changed |= state.Move(-1, count, shift);
			}

			if (ImGui.IsKeyPressed(ImGuiKey.RightArrow, true))
			{
				changed |= state.Move(1, count, shift);
			}

			if (ImGui.IsKeyPressed(ImGuiKey.UpArrow, true))
			{
				changed |= state.Move(-grid.Columns, count, shift);
			}

			if (ImGui.IsKeyPressed(ImGuiKey.DownArrow, true))
			{
				changed |= state.Move(grid.Columns, count, shift);
			}

			if (ImGui.IsKeyPressed(ImGuiKey.Home, true))
			{
				changed |= state.Move(-count, count, shift);
			}

			if (ImGui.IsKeyPressed(ImGuiKey.End, true))
			{
				changed |= state.Move(count, count, shift);
			}

			if (io.KeyCtrl && ImGui.IsKeyPressed(ImGuiKey.A, false))
			{
				changed |= state.SelectAll(count);
			}

			if (ImGui.IsKeyPressed(ImGuiKey.Escape, false))
			{
				changed |= state.Clear();
			}

			if ((ImGui.IsKeyPressed(ImGuiKey.Enter, false) || ImGui.IsKeyPressed(ImGuiKey.KeypadEnter, false)) && state.FocusIndex >= 0)
			{
				options.OnActivate?.Invoke(state.FocusIndex);
			}

			if (state.FocusIndex != focusBefore && state.FocusIndex >= 0)
			{
				int row = AssetBrowserState.RowOf(state.FocusIndex, grid.Columns);
				ImGui.SetScrollY(AssetBrowserState.ScrollToReveal(row, grid.RowHeight, ImGui.GetScrollY(), ImGui.GetWindowHeight()));
			}

			return changed;
		}

		/// <summary>
		/// Applies Ctrl+wheel to the tile size, keeping the row that was at the top of the view at the top.
		/// </summary>
		/// <returns><see langword="true"/> when the tile size changed.</returns>
		private static bool HandleResize(AssetBrowserOptions options, float tileSize, Grid grid, ImGuiStylePtr style)
		{
			ImGuiIOPtr io = ImGui.GetIO();
			if (!io.KeyCtrl || !ImGui.IsWindowHovered())
			{
				return false;
			}

			// Claims the wheel for as long as Ctrl is held over the browser, so the next wheel click
			// resizes the tiles instead of also scrolling them. Ownership lapses on its own a frame after
			// it stops being claimed.
			ImGuiP.SetKeyOwner(ImGuiKey.MouseWheelY, ImGui.GetID("##resize"));

			float wheel = io.MouseWheel;
			if (MathF.Abs(wheel) < float.Epsilon)
			{
				return false;
			}

			float resized = ResizedTile(tileSize, wheel, options);
			// Pinned at a size limit, the wheel moves nothing.
			if (MathF.Abs(resized - tileSize) < SizeTolerance)
			{
				return false;
			}

			int firstVisible = (int)(ImGui.GetScrollY() / grid.RowHeight) * grid.Columns;
			options.TileSize = resized;

			Grid after = Grid.Measure(resized, style);
			ImGui.SetScrollY(AssetBrowserState.RowOf(firstVisible, after.Columns) * after.RowHeight);
			return true;
		}

		/// <summary>The measurements every tile in one frame shares.</summary>
		private readonly record struct Grid(float TileSize, float TileHeight, float RowHeight, int Columns)
		{
			internal static Grid Measure(float tileSize, ImGuiStylePtr style)
			{
				float tileHeight = tileSize + ImGui.GetTextLineHeight() + style.ItemInnerSpacing.Y;
				float rowHeight = tileHeight + style.ItemSpacing.Y;
				int columns = AssetBrowserState.ColumnCount(ImGui.GetContentRegionAvail().X, tileSize, style.ItemSpacing.X);
				return new Grid(tileSize, tileHeight, rowHeight, columns);
			}
		}
	}
}
