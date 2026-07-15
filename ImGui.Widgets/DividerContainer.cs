// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Widgets;

using System.Collections.ObjectModel;
using System.Drawing;
using System.Numerics;

using Extensions;

using Hexa.NET.ImGui;

using ktsu.ImGui.Color;
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
			Vector2 windowPadding = style.WindowPadding;
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

			Vector2 layoutMaskInverse = layout switch
			{
				DividerLayout.Columns => new Vector2(0, 1),
				DividerLayout.Rows => new Vector2(1, 0),
				_ => throw new NotImplementedException(),
			};

			Vector2 cursorScreenPos = ImGui.GetCursorScreenPos();
			Vector2 advance = cursorScreenPos;

			ImGui.SetNextWindowPos(advance);
			ImGui.BeginChild(Id, containerSize, ImGuiChildFlags.None, ImGuiWindowFlags.NoSavedSettings);

			foreach (DividerZone z in Zones)
			{
				Vector2 zoneSize = CalculateZoneSize(z, windowPadding, containerSize, layoutMask, layoutMaskInverse);
				ImGui.SetNextWindowPos(advance);
				ImGui.BeginChild(z.Id, zoneSize, ImGuiChildFlags.Borders, ImGuiWindowFlags.NoSavedSettings);
				z.Tick(dt);
				ImGui.EndChild();

				advance += CalculateAdvance(z, windowPadding, containerSize, layoutMask);
			}

			ImGui.EndChild();

			//render the handles last otherwise they'll be covered by the other zones windows and wont receive hover events

			//reset the advance to the top left of the container
			advance = cursorScreenPos;
			float resize = 0;
			Vector2 mousePos = ImGui.GetMousePos();
			bool resetSize = false;
			foreach ((DividerZone z, int i) in Zones.WithIndex())
			{
				//draw the grab handle if we're not the last zone
				if (z != Zones[^1])
				{
					DrawDividerHandle(z, i, advance, style, windowPadding, containerSize, layoutMask, layoutMaskInverse, mousePos, drawList, ref resize, ref resetSize);
				}

				advance += CalculateAdvance(z, windowPadding, containerSize, layoutMask);
			}

			//do the actual resize at the end of the tick so that we don't mess with the dimensions of the layout mid rendering
			ApplyResize(resize, resetSize);
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S107:Methods should not have too many parameters", Justification = "Private rendering helper extracted from Tick to reduce cognitive complexity; the parameters thread the immediate-mode layout state (masks, padding, draw list) computed once by the caller, and bundling them would not improve readability.")]
		private void DrawDividerHandle(DividerZone z, int i, Vector2 advance, ImGuiStylePtr style, Vector2 windowPadding, Vector2 containerSize, Vector2 layoutMask, Vector2 layoutMaskInverse, Vector2 mousePos, ImDrawListPtr drawList, ref float resize, ref bool resetSize)
		{
			Vector2 zoneSize = CalculateZoneSize(z, windowPadding, containerSize, layoutMask, layoutMaskInverse);
			Vector2 lineA = advance + (zoneSize * layoutMask) + (windowPadding * 0.5f * layoutMask);
			Vector2 lineB = lineA + (zoneSize * layoutMaskInverse);
			float lineWidth = style.WindowPadding.X * 0.5f;
			float grabWidth = style.WindowPadding.X * 2;
			Vector2 grabBox = new Vector2(grabWidth, grabWidth) * 0.5f;
			Vector2 grabMin = lineA - (grabBox * layoutMask);
			Vector2 grabMax = lineB + (grabBox * layoutMask);
			Vector2 grabSize = grabMax - grabMin;
			RectangleF handleRect = new(grabMin.X, grabMin.Y, grabSize.X, grabSize.Y);
			bool handleHovered = handleRect.Contains(mousePos.X, mousePos.Y);
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
				UpdateDragResize(z, advance, windowPadding, containerSize, layoutMask, mousePos, ref resize);
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

		private void UpdateDragResize(DividerZone z, Vector2 advance, Vector2 windowPadding, Vector2 containerSize, Vector2 layoutMask, Vector2 mousePos, ref float resize)
		{
			if (!ImGui.IsMouseDown(ImGuiMouseButton.Left))
			{
				DragIndex = -1;
				return;
			}

			Vector2 mousePosLocal = mousePos - advance;

			DividerZone first = Zones[0];
			DividerZone last = Zones[^1];
			if (first != last && z != first)
			{
				mousePosLocal += windowPadding * 0.5f * layoutMask;
			}

			float requestedSize = layout switch
			{
				DividerLayout.Columns => mousePosLocal.X / containerSize.X,
				DividerLayout.Rows => mousePosLocal.Y / containerSize.Y,
				_ => throw new NotImplementedException(),
			};
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

		private Vector2 CalculateZoneSize(DividerZone z, Vector2 windowPadding, Vector2 containerSize, Vector2 layoutMask, Vector2 layoutMaskInverse)
		{
			Vector2 zoneSize = (containerSize * z.Size * layoutMask) + (containerSize * layoutMaskInverse);

			DividerZone first = Zones[0];
			DividerZone last = Zones[^1];
			if (first != last)
			{
				if (z == first || z == last)
				{
					zoneSize -= windowPadding * 0.5f * layoutMask;
				}
				else
				{
					zoneSize -= windowPadding * layoutMask;
				}
			}

			return new Vector2((float)Math.Floor(zoneSize.X), (float)Math.Floor(zoneSize.Y));
		}

		private Vector2 CalculateAdvance(DividerZone z, Vector2 windowPadding, Vector2 containerSize, Vector2 layoutMask)
		{
			Vector2 advance = containerSize * z.Size * layoutMask;

			DividerZone first = Zones[0];
			DividerZone last = Zones[^1];
			if (first != last && z == first)
			{
				advance += windowPadding * 0.5f * layoutMask;
			}

			return new Vector2((float)Math.Round(advance.X), (float)Math.Round(advance.Y));
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
