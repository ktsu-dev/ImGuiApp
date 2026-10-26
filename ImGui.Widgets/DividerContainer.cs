// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System.Collections.ObjectModel;
using System.Drawing;
using System.Numerics;

using Extensions;

using Hexa.NET.ImGui;

using ktsu.ImGui.Color;
using ktsu.ImGui.Probes;
using ktsu.Semantics.Color;

/// <summary>
/// Provides custom ImGui widgets.
/// </summary>
public static partial class ImGuiWidgets
{
	/// <summary>
	/// An enum to specify the layout direction of the divider container.
	/// </summary>
	public enum DividerLayout
	{
		/// <summary>
		/// The container will be laid out in columns.
		/// </summary>
		Columns,
		/// <summary>
		/// The container will be laid out in rows.
		/// </summary>
		Rows,
	}

	/// <summary>
	/// A container that can be divided into dragable zones.
	/// Useful for creating resizable layouts.
	/// Containers can be nested to create complex layouts.
	/// </summary>
	/// <remarks>
	/// Create a new divider container with a callback for when the container is resized and a specified layout direction.
	/// </remarks>
	/// <param name="id">The ID of the container.</param>
	/// <param name="onResized">A callback for when the container is resized.</param>
	/// <param name="layout">The layout direction of the container.</param>
	/// <param name="zones">The zones to add to the container.</param>
	public class DividerContainer(string id, Action<DividerContainer>? onResized, DividerLayout layout, IEnumerable<DividerZone> zones)
	{
		/// <summary>
		/// An ID for the container.
		/// </summary>
		public string Id { get; init; } = id;
		private int DragIndex { get; set; } = -1;
		private List<DividerZone> Zones { get; init; } = [.. zones];

		/// <summary>
		/// Create a new divider container with a callback for when the container is resized and a specified layout direction.
		/// </summary>
		/// <param name="id">The ID of the container.</param>
		/// <param name="onResized">A callback for when the container is resized.</param>
		/// <param name="layout">The layout direction of the container.</param>
		public DividerContainer(string id, Action<DividerContainer>? onResized, DividerLayout layout)
			: this(id, onResized, layout, [])
		{
		}

		/// <summary>
		/// Create a new divider container with the default layout direction of columns.
		/// </summary>
		/// <param name="id">The ID of the container.</param>
		public DividerContainer(string id)
			: this(id, null, DividerLayout.Columns)
		{
		}

		/// <summary>
		/// Create a new divider container with a specified layout direction.
		/// </summary>
		/// <param name="id">The ID of the container.</param>
		/// <param name="layout">The layout direction of the container.</param>
		public DividerContainer(string id, DividerLayout layout)
			: this(id, null, layout)
		{
		}

		/// <summary>
		/// Create a new divider container with a callback for when the container is resized and the default layout direction of columns.
		/// </summary>
		/// <param name="id">The ID of the container.</param>
		/// <param name="onResized">A callback for when the container is resized.</param>
		public DividerContainer(string id, Action<DividerContainer>? onResized)
			: this(id, onResized, DividerLayout.Columns)
		{
		}

		/// <summary>
		/// Tick the container to update and draw its contents.
		/// </summary>
		/// <param name="dt">The delta time since the last tick.</param>
		/// <exception cref="NotImplementedException">Thrown if the layout direction is not supported.</exception>
		public void Tick(float dt)
		{
			ImGuiStylePtr style = ImGui.GetStyle();
			ImDrawListPtr drawList = ImGui.GetWindowDrawList();
			//lay the container out from the current draw cursor using the remaining content
			//region, so it respects anything drawn before it instead of always filling the window
			Vector2 containerSize = ImGui.GetContentRegionAvail();

			Vector2 layoutMask = layout switch
			{
				DividerLayout.Columns => new Vector2(1, 0),
				DividerLayout.Rows => new Vector2(0, 1),
				_ => throw new NotImplementedException(),
			};

			Vector2 layoutMaskInverse = Vector2.One - layoutMask;

			// The gap between zones is the window padding along the container's axis, so a rows
			// layout follows the vertical padding and a columns layout the horizontal.
			float gap = Vector2.Dot(style.WindowPadding, layoutMask);
			float length = Vector2.Dot(containerSize, layoutMask);
			DividerZoneSpan[] spans = DividerContainerLayout.Spans([.. Zones.Select(z => z.Size)], length, gap);

			Vector2 origin = ImGui.GetCursorScreenPos();

			// The zones are placed by position rather than by the cursor, which still moves past each
			// of them, so the container itself must never scroll or show a scrollbar for that.
			ImGui.SetNextWindowPos(origin);
			ImGui.BeginChild(Id, containerSize, ImGuiChildFlags.None, ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);

			foreach ((DividerZone z, int i) in Zones.WithIndex())
			{
				Vector2 zoneSize = (spans[i].Length * layoutMask) + (containerSize * layoutMaskInverse);
				ImGui.SetNextWindowPos(origin + (spans[i].Start * layoutMask));
				ImGui.BeginChild(z.Id, zoneSize, ImGuiChildFlags.Borders, ImGuiWindowFlags.NoSavedSettings);
				z.Tick(dt);
				ImGui.EndChild();
				ImGuiProbes.MarkItem(Id, $"zone/{z.Id}");
			}

			ImGui.EndChild();
			ImGuiProbes.MarkItem(Id);

			//render the handles last otherwise they'll be covered by the other zones windows and wont receive hover events
			float resize = 0;
			Vector2 mousePos = ImGui.GetMousePos();
			bool resetSize = false;
			for (int i = 0; i < Zones.Count - 1; i++)
			{
				float divider = DividerContainerLayout.DividerPosition(spans, i);
				DrawDividerHandle(i, origin, divider, gap, containerSize, layoutMask, layoutMaskInverse, mousePos, drawList, ref resize, ref resetSize);
			}

			//do the actual resize at the end of the tick so that we don't mess with the dimensions of the layout mid rendering
			ApplyResize(resize, resetSize);
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S107:Methods should not have too many parameters", Justification = "Private rendering helper extracted from Tick to reduce cognitive complexity; the parameters thread the immediate-mode layout state (masks, gap, draw list) computed once by the caller, and bundling them would not improve readability.")]
		private void DrawDividerHandle(int i, Vector2 origin, float divider, float gap, Vector2 containerSize, Vector2 layoutMask, Vector2 layoutMaskInverse, Vector2 mousePos, ImDrawListPtr drawList, ref float resize, ref bool resetSize)
		{
			DividerZone z = Zones[i];
			Vector2 lineA = origin + (divider * layoutMask);
			Vector2 lineB = lineA + (containerSize * layoutMaskInverse);
			float lineWidth = gap * 0.5f;
			float grabWidth = gap * 2;
			Vector2 grabBox = new Vector2(grabWidth, grabWidth) * 0.5f;
			Vector2 grabMin = lineA - (grabBox * layoutMask);
			Vector2 grabMax = lineB + (grabBox * layoutMask);
			Vector2 grabSize = grabMax - grabMin;
			RectangleF handleRect = new(grabMin.X, grabMin.Y, grabSize.X, grabSize.Y);
			ImGuiProbes.MarkRegion($"{Id}/divider/{z.Id}", grabMin, grabMin + grabSize);
			// The handle is hit-tested by rectangle rather than submitted as an item, so it has to ask
			// whether anything is drawn over it: a popup or window opened above the divider would
			// otherwise have its clicks taken by the handle underneath, which starts a resize and moves
			// whatever the click was meant for. Popups opened from a zone are excluded from the
			// container's hierarchy for the same reason.
			bool handleHovered = handleRect.Contains(mousePos.X, mousePos.Y)
				&& (DragIndex == i || ImGui.IsWindowHovered(ImGuiHoveredFlags.ChildWindows | ImGuiHoveredFlags.NoPopupHierarchy));
			bool mouseClickedThisFrame = ImGui.IsMouseClicked(ImGuiMouseButton.Left);
			bool handleClicked = handleHovered && mouseClickedThisFrame;
			bool handleDoubleClicked = handleHovered && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left);

			if (handleClicked)
			{
				DragIndex = i;
			}

			if (handleDoubleClicked)
			{
				resetSize = true;
			}
			else if (DragIndex == i)
			{
				UpdateDragResize(i, origin, gap, containerSize, layoutMask, mousePos, ref resize);
			}

			ImColor lineColor = GetHandleLineColor(i, handleHovered);
			drawList.AddLine(lineA, lineB, lineColor.ToImGuiU32(), lineWidth);

			if (handleHovered || DragIndex == i)
			{
				ImGui.SetMouseCursor(layout switch
				{
					DividerLayout.Columns => ImGuiMouseCursor.ResizeEw,
					DividerLayout.Rows => ImGuiMouseCursor.ResizeNs,
					_ => throw new NotImplementedException(),
				});
			}
		}

		private void UpdateDragResize(int i, Vector2 origin, float gap, Vector2 containerSize, Vector2 layoutMask, Vector2 mousePos, ref float resize)
		{
			if (!ImGui.IsMouseDown(ImGuiMouseButton.Left))
			{
				DragIndex = -1;
				return;
			}

			// The zone takes whatever share puts its divider under the pointer.
			float position = Vector2.Dot(mousePos - origin, layoutMask);
			float length = Vector2.Dot(containerSize, layoutMask);
			float requestedSize = DividerContainerLayout.FractionForDividerAt([.. Zones.Select(z => z.Size)], i, position, length, gap);
			resize = Math.Clamp(requestedSize, 0.1f, 0.9f);
		}

		private ImColor GetHandleLineColor(int i, bool handleHovered)
		{
			if (DragIndex == i)
			{
				return new Srgb(1f, 1f, 1f).ToImColor(0.7f);
			}

			if (handleHovered)
			{
				return new Srgb(1f, 1f, 1f).ToImColor(0.5f);
			}

			return new Srgb(1f, 1f, 1f).ToImColor(0.3f);
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S1244:Do not check floating point inequality with exact values, use a range instead.", Justification = "Exact comparison is intentional here (detecting whether resize actually changed the zone's size value); a tolerance would suppress legitimate resize callbacks.")]
		private void ApplyResize(float resize, bool resetSize)
		{
			if (DragIndex <= -1)
			{
				return;
			}

			if (resetSize)
			{
				resize = Zones[DragIndex].InitialSize;
			}

			DividerZone resizedZone = Zones[DragIndex];
			DividerZone neighbourZone = Zones[DragIndex + 1];
			float combinedSize = resizedZone.Size + neighbourZone.Size;
			float maxSize = combinedSize - 0.1f;
			resize = Math.Clamp(resize, 0.1f, maxSize);
			bool sizeDidChange = resizedZone.Size != resize;
			resizedZone.Size = resize;
			neighbourZone.Size = combinedSize - resize;
			if (sizeDidChange)
			{
				onResized?.Invoke(this);
			}

			if (resetSize)
			{
				DragIndex = -1;
			}
		}

		/// <summary>
		/// Add a zone to the container.
		/// </summary>
		/// <param name="id">The ID of the zone.</param>
		/// <param name="size">The size of the zone.</param>
		/// <param name="resizable">Whether the zone is resizable.</param>
		/// <param name="tickDelegate">The delegate to call when the zone is ticked.</param>
		public void Add(string id, float size, bool resizable, Action<float> tickDelegate) => Zones.Add(new(id, size, resizable, tickDelegate));

		/// <summary>
		/// Add a zone to the container.
		/// </summary>
		/// <param name="id">The ID of the zone.</param>
		/// <param name="size">The size of the zone.</param>
		/// <param name="tickDelegate">The delegate to call when the zone is ticked.</param>
		public void Add(string id, float size, Action<float> tickDelegate) => Zones.Add(new(id, size, tickDelegate));

		/// <summary>
		/// Add a zone to the container.
		/// </summary>
		/// <param name="id">The ID of the zone.</param>
		/// <param name="tickDelegate">The delegate to call when the zone is ticked.</param>
		public void Add(string id, Action<float> tickDelegate)
		{
			float size = 1.0f / (Zones.Count + 1);
			Zones.Add(new(id, size, tickDelegate));
		}

		/// <summary>
		/// Add a zone to the container.
		/// </summary>
		/// <param name="zone">The zone to add</param>
		public void Add(DividerZone zone) => Zones.Add(zone);

		/// <summary>
		/// Remove a zone from the container.
		/// </summary>
		/// <param name="id">The ID of the zone to remove.</param>
		public void Remove(string id)
		{
			DividerZone? zone = Zones.FirstOrDefault(z => z.Id == id);
			if (zone != null)
			{
				Zones.Remove(zone);
			}
		}

		/// <summary>
		/// Remome all zones from the container.
		/// </summary>
		public void Clear() => Zones.Clear();

		/// <summary>
		/// Set the size of a zone by its ID.
		/// </summary>
		/// <param name="id">The ID of the zone to set the size of.</param>
		/// <param name="size">The size to set.</param>
		public void SetSize(string id, float size)
		{
			DividerZone? zone = Zones.FirstOrDefault(z => z.Id == id);
			zone?.Size = size;
		}

		/// <summary>
		/// Set the size of a zone by its index.
		/// </summary>
		/// <param name="index">The index of the zone to set the size of.</param>
		/// <param name="size">The size to set.</param>
		public void SetSize(int index, float size)
		{
			if (index >= 0 && index < Zones.Count)
			{
				Zones[index].Size = size;
			}
		}

		/// <summary>
		/// Set the sizes of all zones in this container from a collection of sizes.
		/// </summary>
		/// <param name="sizes">The collection of sizes to set.</param>
		/// <exception cref="ArgumentException"></exception>
		public void SetSizesFromList(ICollection<float> sizes)
		{
			Ensure.NotNull(sizes);

			if (sizes.Count != Zones.Count)
			{
				throw new ArgumentException("List of sizes must be the same length as the zones list");
			}

			foreach ((float s, int i) in sizes.WithIndex())
			{
				Zones[i].Size = s;
			}
		}

		/// <summary>
		/// Get a collection of the sizes of all zones in this container.
		/// </summary>
		/// <returns>A collection of the sizes of all zones in this container.</returns>
		public Collection<float> GetSizes() => Zones.Select(z => z.Size).ToCollection();
	}
}
