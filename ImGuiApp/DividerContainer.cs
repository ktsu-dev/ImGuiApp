using System.Numerics;
using ImGuiNET;

namespace ktsu.io
{
	public enum DividerLayout
	{
		Columns,
		Rows,
	}

	public class DividerContainer
	{
		public string Id { get; init; }
		private DividerLayout Layout { get; }
		private int DragIndex { get; set; } = -1;
		private List<DividerZone> Zones { get; } = new();
		private Action<DividerContainer>? OnResized { get; set; }

		public DividerContainer(string id, Action<DividerContainer>? onResized, DividerLayout layout)
		{
			Id = id;
			Layout = layout;
			OnResized = onResized;
		}

		public DividerContainer(string id)
			: this(id, null, DividerLayout.Columns)
		{
		}

		public DividerContainer(string id, DividerLayout layout)
			: this(id, null, layout)
		{
		}

		public DividerContainer(string id, Action<DividerContainer>? onResized)
			: this(id, onResized, DividerLayout.Columns)
		{
		}

		public void Tick(float dt)
		{
			var style = ImGui.GetStyle();
			var windowPadding = style.WindowPadding;
			var drawList = ImGui.GetWindowDrawList();
			var containerSize = ImGui.GetWindowSize() - (windowPadding * 2.0f);

			var layoutMask = Layout switch
			{
				DividerLayout.Columns => new Vector2(1, 0),
				DividerLayout.Rows => new Vector2(0, 1),
				_ => throw new NotImplementedException(),
			};

			var layoutMaskInverse = Layout switch
			{
				DividerLayout.Columns => new Vector2(0, 1),
				DividerLayout.Rows => new Vector2(1, 0),
				_ => throw new NotImplementedException(),
			};

			var windowPos = ImGui.GetWindowPos();
			var advance = windowPos + windowPadding;

			Vector2 CalculateAdvance(DividerZone z)
			{
				var advance = containerSize * z.Size * layoutMask;

				var first = Zones.First();
				var last = Zones.Last();
				if (first != last && z == first)
				{
					advance += windowPadding * 0.5f * layoutMask;
				}

				return new Vector2((float)Math.Round(advance.X), (float)Math.Round(advance.Y));
			}

			Vector2 CalculateZoneSize(DividerZone z)
			{
				var zoneSize = (containerSize * z.Size * layoutMask) + (containerSize * layoutMaskInverse);

				var first = Zones.First();
				var last = Zones.Last();
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

			ImGui.SetNextWindowPos(advance);
			ImGui.BeginChild(Id, containerSize, false, ImGuiWindowFlags.NoSavedSettings);

			foreach (var z in Zones)
			{
				var zoneSize = CalculateZoneSize(z);
				ImGui.SetNextWindowPos(advance);
				ImGui.BeginChild(z.Id, zoneSize, true, ImGuiWindowFlags.NoSavedSettings);
				z.Tick(dt);
				ImGui.EndChild();

				advance += CalculateAdvance(z);
			}

			ImGui.EndChild();

			//render the handles last otherwise they'll be covered by the other zones windows and wont receive hover events

			//reset the advance to the top left of the container
			advance = windowPos + windowPadding;
			float resize = 0;
			var mousePos = ImGui.GetMousePos();
			foreach (var (z, i) in Zones.WithIndex())
			{
				//draw the grab handle if we're not the last zone
				if (z != Zones.Last())
				{
					var zoneSize = CalculateZoneSize(z);
					var lineA = advance + (zoneSize * layoutMask) + (windowPadding * 0.5f * layoutMask);
					var lineB = lineA + (zoneSize * layoutMaskInverse);
					float lineWidth = style.WindowPadding.X * 0.5f;
					float grabWidth = style.WindowPadding.X * 2;
					var grabBox = new Vector2(grabWidth, grabWidth) * 0.5f;
					var grabMin = lineA - (grabBox * layoutMask);
					var grabMax = lineB + (grabBox * layoutMask);
					var grabSize = grabMax - grabMin;
					RectangleF handleRect = new(grabMin.X, grabMin.Y, grabSize.X, grabSize.Y);
					bool handleHovered = handleRect.Contains(mousePos.X, mousePos.Y);
					bool mouseClickedThisFrame = ImGui.IsMouseClicked(ImGuiMouseButton.Left);
					if (handleHovered && mouseClickedThisFrame)
					{
						DragIndex = i;
					}

					if (DragIndex == i)
					{
						if (ImGui.IsMouseDown(ImGuiMouseButton.Left))
						{
							var mousePosLocal = mousePos - advance;

							var first = Zones.First();
							var last = Zones.Last();
							if (first != last && z != first)
							{
								mousePosLocal += windowPadding * 0.5f * layoutMask;
							}

							float requestedSize = Layout switch
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

					var lineColor = DragIndex == i ? new Vector4(1, 1, 1, 0.7f) : handleHovered ? new Vector4(1, 1, 1, 0.5f) : new Vector4(1, 1, 1, 0.3f);
					//drawList.AddRectFilled(grabMin, grabMax, ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, 0.3f)));
					drawList.AddLine(lineA, lineB, ImGui.ColorConvertFloat4ToU32(lineColor), lineWidth);

					if (handleHovered || DragIndex == i)
					{
						ImGui.SetMouseCursor(Layout switch
						{
							DividerLayout.Columns => ImGuiMouseCursor.ResizeEW,
							DividerLayout.Rows => ImGuiMouseCursor.ResizeNS,
							_ => throw new NotImplementedException(),
						});
					}
				}

				advance += CalculateAdvance(z);
			}

			//do the actual resize at the end of the tick so that we don't mess with the dimensions of the layout mid rendering
			if (DragIndex > -1)
			{
				var resizedZone = Zones[DragIndex];
				var neighbourZone = Zones[DragIndex + 1];
				float combinedSize = resizedZone.Size + neighbourZone.Size;
				float maxSize = combinedSize - 0.1f;
				resize = Math.Clamp(resize, 0.1f, maxSize);
				bool sizeDidChange = resizedZone.Size != resize;
				resizedZone.Size = resize;
				neighbourZone.Size = combinedSize - resize;
				if (sizeDidChange)
				{
					OnResized?.Invoke(this);
				}
			}
		}

		public void Add(string id, float size, bool resizable, Action<float> tickDelegate) => Zones.Add(new(id, size, resizable, tickDelegate));

		public void Add(string id, float size, Action<float> tickDelegate) => Zones.Add(new(id, size, tickDelegate));

		public void Add(string id, Action<float> tickDelegate)
		{
			float size = 1.0f / (Zones.Count + 1);
			Zones.Add(new(id, size, tickDelegate));
		}

		public void Remove(string id)
		{
			var zone = Zones.FirstOrDefault(z => z.Id == id);
			if (zone != null)
			{
				Zones.Remove(zone);
			}
		}

		public void Clear() => Zones.Clear();

		public void SetSize(string id, float size)
		{
			var zone = Zones.FirstOrDefault(z => z.Id == id);
			if (zone != null)
			{
				zone.Size = size;
			}
		}

		public void SetSize(int index, float size)
		{
			if (index >= 0 && index < Zones.Count)
			{
				Zones[index].Size = size;
			}
		}

		public void SetSizesFromList(List<float> sizes)
		{
			ArgumentNullException.ThrowIfNull(sizes);

			if (sizes.Count != Zones.Count)
			{
				throw new ArgumentException("List of sizes must be the same length as the zones list");
			}

			for (int i = 0; i < sizes.Count; i++)
			{
				Zones[i].Size = sizes[i];
			}
		}

		public List<float> GetSizes() => Zones.Select(z => z.Size).ToList();
	}
}
