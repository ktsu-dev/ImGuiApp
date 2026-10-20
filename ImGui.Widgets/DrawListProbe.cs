// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Probes;

/// <summary>
/// Marks what a drawing call painted, for widgets that leave no clean last item behind them.
/// </summary>
internal static class DrawListProbe
{
	/// <summary>
	/// Runs <paramref name="draw"/> and, while probes are recording, marks the bounding box of the
	/// vertices it added to the window draw list, clamped to the current clip rectangle.
	/// </summary>
	/// <remarks>
	/// The argument is passed through rather than captured so a caller can hand over a method group
	/// and allocate no closure.
	/// </remarks>
	/// <typeparam name="TArg">The drawing call's argument type.</typeparam>
	/// <param name="name">The probe name to record.</param>
	/// <param name="draw">The drawing call.</param>
	/// <param name="arg">The argument to pass to <paramref name="draw"/>.</param>
	public static void MarkDrawn<TArg>(string name, Action<TArg> draw, TArg arg)
	{
		DrawnSpan span = Begin();
		draw(arg);
		span.MarkDrawn(name);
	}

	/// <summary>
	/// Starts measuring what is drawn from here on, so a widget that submits items and also paints
	/// outside them can be marked around both.
	/// </summary>
	/// <returns>The measurement, which costs nothing while probes are not recording.</returns>
	public static DrawnSpan Begin() =>
		ImGuiProbes.IsRecording
			? new DrawnSpan(true, ImGui.GetCursorScreenPos(), ImGui.GetWindowDrawList().VtxBuffer.Size)
			: default;

	/// <summary>A measurement started by <see cref="Begin"/>.</summary>
	/// <param name="Recording">Whether probes were recording when it started.</param>
	/// <param name="Origin">The cursor position when it started.</param>
	/// <param name="FirstVertex">The window draw list's vertex count when it started.</param>
	internal readonly record struct DrawnSpan(bool Recording, Vector2 Origin, int FirstVertex)
	{
		/// <summary>Marks the bounds of the vertices drawn since <see cref="Begin"/>, when there are any.</summary>
		/// <param name="name">The probe name to record.</param>
		public void MarkDrawn(string name)
		{
			if (Recording && TryGetDrawnBounds(out Vector2 min, out Vector2 max))
			{
				ImGuiProbes.MarkRegion(name, min, max);
			}
		}

		/// <summary>
		/// Marks from the cursor position at <see cref="Begin"/> to the last item's far corner, grown to
		/// take in every vertex drawn since.
		/// </summary>
		/// <param name="name">The probe name to record.</param>
		public void MarkSpan(string name)
		{
			if (!Recording)
			{
				return;
			}

			Vector2 min = Origin;
			Vector2 max = ImGui.GetItemRectMax();
			if (TryGetDrawnBounds(out Vector2 drawnMin, out Vector2 drawnMax))
			{
				min = Vector2.Min(min, drawnMin);
				max = Vector2.Max(max, drawnMax);
			}

			ImGuiProbes.MarkRegion(name, min, max);
		}

		private bool TryGetDrawnBounds(out Vector2 min, out Vector2 max)
		{
			// The draw list belongs to the window, so it is the same list as at the start.
			ImDrawListPtr drawList = ImGui.GetWindowDrawList();
			ImVector<ImDrawVert> vertices = drawList.VtxBuffer;
			min = new(float.MaxValue);
			max = new(float.MinValue);
			if (vertices.Size <= FirstVertex)
			{
				return false;
			}

			for (int i = FirstVertex; i < vertices.Size; i++)
			{
				Vector2 position = vertices[i].Pos;
				min = Vector2.Min(min, position);
				max = Vector2.Max(max, position);
			}

			Vector2 clipMin = ImGui.GetClipRectMin(drawList);
			Vector2 clipMax = ImGui.GetClipRectMax(drawList);
			min = Vector2.Clamp(min, clipMin, clipMax);
			max = Vector2.Clamp(max, clipMin, clipMax);
			return true;
		}
	}
}
