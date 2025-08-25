// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGuiWidgets;

using System.Collections.ObjectModel;
using System.Drawing;
using System.Numerics;

using Extensions;

using Hexa.NET.ImGui;

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
			Vector2 containerSize = ImGui.GetWindowSize() - (windowPadding * 2.0f);

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

			Vector2 windowPos = ImGui.GetWindowPos();
			Vector2 advance = windowPos + windowPadding;

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
			advance = windowPos + windowPadding;
			float resize = 0;
			Vector2 mousePos = ImGui.GetMousePos();
			bool resetSize = false;
			foreach ((DividerZone z, int i) in Zones.WithIndex())
			{
				//draw the grab handle if we're not the last zone
				if (z != Zones.Last())
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
						if (ImGui.IsMouseDown(ImGuiMouseButton.Left))
						{
							Vector2 mousePosLocal = mousePos - advance;

							DividerZone first = Zones.First();
							DividerZone last = Zones.Last();
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
						else
						{
							DragIndex = -1;
						}
					}

					Vector4 lineColor = DragIndex == i ? new Vector4(1, 1, 1, 0.7f) : handleHovered ? new Vector4(1, 1, 1, 0.5f) : new Vector4(1, 1, 1, 0.3f);
					//drawList.AddRectFilled(grabMin, grabMax, ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, 0.3f)));
					drawList.AddLine(lineA, lineB, ImGui.ColorConvertFloat4ToU32(lineColor), lineWidth);

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

				advance += CalculateAdvance(z, windowPadding, containerSize, layoutMask);
			}

			//do the actual resize at the end of the tick so that we don't mess with the dimensions of the layout mid rendering
			if (DragIndex > -1)
			{
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
		}

		private Vector2 CalculateZoneSize(DividerZone z, Vector2 windowPadding, Vector2 containerSize, Vector2 layoutMask, Vector2 layoutMaskInverse)
		{
			Vector2 zoneSize = (containerSize * z.Size * layoutMask) + (containerSize * layoutMaskInverse);

			DividerZone first = Zones.First();
			DividerZone last = Zones.Last();
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

			DividerZone first = Zones.First();
			DividerZone last = Zones.Last();
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
			if (zone != null)
			{
				zone.Size = size;
			}
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
			ArgumentNullException.ThrowIfNull(sizes, nameof(sizes));

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
