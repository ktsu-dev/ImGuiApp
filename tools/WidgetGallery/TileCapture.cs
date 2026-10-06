// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.WidgetGallery;

using System;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.App;
using ktsu.ImGui.App.Testing;

/// <summary>Photographs one gallery entry on its own.</summary>
internal static class TileCapture
{
	/// <summary>The margin left around what an entry drew, in pixels.</summary>
	private const int Margin = 8;

	/// <summary>How far the entry is drawn in from the window's edge, in pixels.</summary>
	private const int Inset = 16;

	/// <summary>Frames drawn without the entry, to settle the window before the baseline is taken.</summary>
	private const int BaselineFrames = 3;

	/// <summary>
	/// Draws an entry in a fresh harness and returns the cropped picture of it.
	/// </summary>
	/// <param name="entry">The entry to draw.</param>
	/// <param name="settings">How the gallery is being rendered.</param>
	/// <returns>The tile, cropped to what the entry drew plus a margin.</returns>
	public static CapturedTile Capture(GalleryEntry entry, GallerySettings settings)
	{
		bool active = false;
		Rectangle? layout = null;
		GalleryContext? context = null;
		Vector2 captionSize = default;
		float lineHeight = 0f;

		void Render()
		{
			if (!active || context is null)
			{
				return;
			}

			// A group around the entry gives the layout rectangle of everything it submitted, which
			// is the crop for any widget drawn in the window's own colour. A widget that fills its
			// available space is given a child region to fill instead of the whole window.
			// Inset from the window's edge so the crop's margin shows window background rather than
			// the window's border and the menu bar's bottom edge.
			ImGui.SetCursorPos(ImGui.GetCursorPos() + new Vector2(Inset));
			ImGui.BeginGroup();

			if (entry.Bounds is Vector2 bounds)
			{
				if (ImGui.BeginChild("##gallery-bounds", bounds, ImGuiChildFlags.None, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
				{
					entry.Draw(context);
				}

				ImGui.EndChild();
			}
			else
			{
				entry.Draw(context);
			}

			ImGui.EndGroup();

			// Measured here, in the same font, so the compositor can lay the gallery out
			// before it starts drawing it.
			captionSize = ImGui.CalcTextSize(entry.Name);
			lineHeight = ImGui.GetTextLineHeight();

			Vector2 min = ImGui.GetItemRectMin();
			Vector2 max = ImGui.GetItemRectMax();
			layout = max.X - min.X >= 1f && max.Y - min.Y >= 1f
				? new Rectangle((int)MathF.Floor(min.X), (int)MathF.Floor(min.Y), (int)MathF.Ceiling(max.X), (int)MathF.Ceiling(max.Y))
				: null;
		}

		HarnessOptions options = new()
		{
			Width = entry.ViewportWidth,
			Height = entry.ViewportHeight,
		};

		using ImGuiAppHarness harness = ImGuiAppHarness.Start(
			new ImGuiAppConfig
			{
				Title = entry.Name,
				OnRender = _ => Render(),
				OnStart = settings.LoadFonts,
				EnableDocking = entry.EnableDocking,
				SaveIniSettings = false,
			},
			options);

		context = new GalleryContext(harness);

		// Park the pointer off screen so nothing starts out hovered.
		harness.Mouse.MoveTo(-100f, -100f);
		harness.Step(BaselineFrames);
		byte[] baseline = harness.Target.Pixels.ToArray();
		context.Baseline = baseline;

		active = true;
		harness.Step(entry.SettleFrames);
		entry.Interact?.Invoke(context);
		harness.Step();

		Rectangle? changed = BoundsOfDifference(baseline, harness.Target);
		Rectangle? crop = Union(layout, changed);

		if (crop is not Rectangle region)
		{
			throw new InvalidOperationException($"'{entry.Name}' drew nothing, so there is nothing to photograph.");
		}

		Bitmap32 tile = Crop(harness.Target, region, Margin);

		entry.Cleanup?.Invoke(context);

		return new CapturedTile(entry, tile, captionSize, lineHeight);
	}

	/// <summary>Copies a rectangle out of a bitmap, grown by a margin and clamped to its edges.</summary>
	/// <param name="source">The bitmap to copy from.</param>
	/// <param name="region">The rectangle to copy.</param>
	/// <param name="margin">How far to grow the rectangle on every side.</param>
	/// <returns>The copied pixels.</returns>
	internal static Bitmap32 Crop(Bitmap32 source, Rectangle region, int margin)
	{
		int minX = Math.Max(region.MinX - margin, 0);
		int minY = Math.Max(region.MinY - margin, 0);
		int maxX = Math.Min(region.MaxX + margin, source.Width);
		int maxY = Math.Min(region.MaxY + margin, source.Height);

		Bitmap32 cropped = new(maxX - minX, maxY - minY);
		int rowBytes = cropped.Width * 4;

		for (int y = minY; y < maxY; y++)
		{
			source.Pixels.Slice(((y * source.Width) + minX) * 4, rowBytes)
				.CopyTo(cropped.Pixels.Slice((y - minY) * rowBytes, rowBytes));
		}

		return cropped;
	}

	private static Rectangle? Union(Rectangle? a, Rectangle? b)
	{
		if (a is not Rectangle first)
		{
			return b;
		}

		if (b is not Rectangle second)
		{
			return first;
		}

		return new Rectangle(
			Math.Min(first.MinX, second.MinX),
			Math.Min(first.MinY, second.MinY),
			Math.Max(first.MaxX, second.MaxX),
			Math.Max(first.MaxY, second.MaxY));
	}

	/// <summary>Finds the tight rectangle around every pixel that differs from a baseline.</summary>
	/// <param name="baseline">The earlier pixels.</param>
	/// <param name="now">The current frame.</param>
	/// <returns>The rectangle, or null when nothing differs.</returns>
	internal static Rectangle? BoundsOfDifference(byte[] baseline, Bitmap32 now)
	{
		Span<byte> pixels = now.Pixels;
		int minX = int.MaxValue;
		int minY = int.MaxValue;
		int maxX = int.MinValue;
		int maxY = int.MinValue;

		for (int y = 0; y < now.Height; y++)
		{
			for (int x = 0; x < now.Width; x++)
			{
				int i = ((y * now.Width) + x) * 4;

				if (baseline[i] == pixels[i]
					&& baseline[i + 1] == pixels[i + 1]
					&& baseline[i + 2] == pixels[i + 2]
					&& baseline[i + 3] == pixels[i + 3])
				{
					continue;
				}

				minX = Math.Min(minX, x);
				minY = Math.Min(minY, y);
				maxX = Math.Max(maxX, x + 1);
				maxY = Math.Max(maxY, y + 1);
			}
		}

		return minX == int.MaxValue ? null : new Rectangle(minX, minY, maxX, maxY);
	}
}

/// <summary>A photographed entry, with the measurements the compositor needs to lay it out.</summary>
/// <param name="Entry">The entry that was drawn.</param>
/// <param name="Pixels">The cropped picture.</param>
/// <param name="CaptionSize">The size of the entry's caption in the gallery's font.</param>
/// <param name="LineHeight">The height of one line of text in the gallery's font.</param>
internal sealed record CapturedTile(GalleryEntry Entry, Bitmap32 Pixels, Vector2 CaptionSize, float LineHeight);
