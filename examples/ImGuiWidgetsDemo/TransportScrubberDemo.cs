// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Examples.Widgets;

using System;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.App;
using ktsu.ImGui.Widgets;

/// <summary>
/// Shows the transport scrubber over a synthesized clip, with a thumbnail strip and a waveform that
/// share its view.
/// </summary>
internal static class TransportScrubberDemo
{
	private const float ClipSeconds = 30.0f;
	private const float FramesPerSecond = 24.0f;
	private const int SampleRate = 2000;
	private const int ThumbnailCount = 8;
	private const int ThumbnailWidth = 64;
	private const int ThumbnailHeight = 36;
	private const float StartIn = 4.0f;
	private const float StartOut = 10.0f;

	private static readonly ImGuiWidgets.WaveformPeakCache Peaks = BuildPeaks();

	// The textures belong to the ImGui context that uploaded them, and a harness makes a new context,
	// so ResetState drops them and the next Show uploads them again.
	private static nint[]? textures;
	private static ImGuiWidgets.TimelineView view = new();
	private static float playhead;
	private static float inPoint = StartIn;
	private static float outPoint = StartOut;
	private static bool playing;
	private static TransportScrubberChange lastChange;

	private static readonly ImGuiWidgets.TransportScrubberOptions Options = new()
	{
		FrameDuration = 1.0f / FramesPerSecond,
		ThumbnailResolver = Thumbnail,
	};

	/// <summary>Gets the playhead's position, in seconds.</summary>
	internal static float Playhead => playhead;

	/// <summary>
	/// Returns this section's state to its starting values. The demo keeps its state in statics,
	/// which outlive a harness.
	/// </summary>
	internal static void ResetState()
	{
		textures = null;
		view = new();
		playhead = 0.0f;
		inPoint = StartIn;
		outPoint = StartOut;
		playing = false;
		lastChange = TransportScrubberChange.None;
	}

	public static void Show()
	{
		if (!DemoProbe.Header("Transport Scrubber"))
		{
			return;
		}

		textures ??= CreateTextures();

		ImGui.TextUnformatted("A 30-second clip at 24 fps: a ruler, a thumbnail strip, a playhead and an in/out range.");
		ImGui.Separator();

		if (playing)
		{
			Advance(ImGui.GetIO().DeltaTime);
		}

		// The scope qualifies the probe names, so a test addresses this one as
		// "Transport Scrubber/scrubber".
		using (new ImGuiWidgets.ScopedId("Transport Scrubber"))
		{
			TransportScrubberChange change = ImGuiWidgets.TransportScrubber(
				"scrubber",
				ClipSeconds,
				view,
				ref playhead,
				ref inPoint,
				ref outPoint,
				Options,
				new Vector2(560.0f, 0.0f));

			if (change != TransportScrubberChange.None)
			{
				lastChange = change;
			}

			// The same view, so zooming or scrolling either one moves both.
			ImGuiWidgets.Waveform("waveform", Peaks, view, ref playhead, new Vector2(560.0f, 80.0f));
		}

		if (DemoProbe.Button(playing ? "Pause##transport" : "Play##transport"))
		{
			playing = !playing;
		}

		ImGui.SameLine();
		if (DemoProbe.Button("Show all##transport"))
		{
			view.ShowAll();
		}

		ImGui.TextUnformatted(
			$"Playhead: {playhead:0.000}s   In: {inPoint:0.000}s   Out: {outPoint:0.000}s   Last change: {lastChange}");
		ImGui.TextUnformatted("I/O mark in/out, arrows step a frame (Shift ×10), right-click for more.");
	}

	// Plays through the in/out range when there is one, and through the whole clip otherwise.
	private static void Advance(float deltaSeconds)
	{
		playhead += deltaSeconds;
		bool hasRange = outPoint > inPoint;
		if (hasRange && (playhead >= outPoint || playhead < inPoint))
		{
			playhead = inPoint;
		}
		else if (playhead >= ClipSeconds)
		{
			playhead = 0.0f;
			playing = false;
		}
	}

	private static nint Thumbnail(float time)
	{
		if (textures is null)
		{
			return 0;
		}

		int index = Math.Clamp((int)(time / ClipSeconds * ThumbnailCount), 0, ThumbnailCount - 1);
		return textures[index];
	}

	// Stand-ins for decoded frames: eight flat hues with a darker band, one per eighth of the clip.
	private static nint[] CreateTextures()
	{
		nint[] ids = new nint[ThumbnailCount];
		byte[] rgba = new byte[ThumbnailWidth * ThumbnailHeight * 4];

		for (int t = 0; t < ThumbnailCount; t++)
		{
			Vector3 color = Hue(t / (float)ThumbnailCount);
			for (int y = 0; y < ThumbnailHeight; y++)
			{
				float shade = y > ThumbnailHeight * 2 / 3 ? 0.6f : 1.0f;
				for (int x = 0; x < ThumbnailWidth; x++)
				{
					int i = ((y * ThumbnailWidth) + x) * 4;
					rgba[i] = (byte)(color.X * shade * 255.0f);
					rgba[i + 1] = (byte)(color.Y * shade * 255.0f);
					rgba[i + 2] = (byte)(color.Z * shade * 255.0f);
					rgba[i + 3] = 255;
				}
			}

			ids[t] = ImGuiApp.CreateTexture(rgba, ThumbnailWidth, ThumbnailHeight).TextureId;
		}

		return ids;
	}

	// A fully saturated colour at a hue from 0 to 1.
	private static Vector3 Hue(float hue)
	{
		float r = Math.Clamp(MathF.Abs((hue * 6.0f) - 3.0f) - 1.0f, 0.0f, 1.0f);
		float g = Math.Clamp(2.0f - MathF.Abs((hue * 6.0f) - 2.0f), 0.0f, 1.0f);
		float b = Math.Clamp(2.0f - MathF.Abs((hue * 6.0f) - 4.0f), 0.0f, 1.0f);
		return new Vector3(r, g, b);
	}

	// A pulse that swells and fades over the clip, so zooming in shows the individual beats.
	private static ImGuiWidgets.WaveformPeakCache BuildPeaks()
	{
		float[] samples = new float[(int)(SampleRate * ClipSeconds)];
		for (int i = 0; i < samples.Length; i++)
		{
			float t = i / (float)SampleRate;
			float swell = 0.4f + (0.35f * MathF.Sin(t * 0.3f));
			float envelope = MathF.Exp(-(t % 0.5f) * 10.0f);
			samples[i] = swell * envelope * MathF.Sin(2.0f * MathF.PI * 60.0f * t);
		}

		return new ImGuiWidgets.WaveformPeakCache(samples, ClipSeconds);
	}
}
