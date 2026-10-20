// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Examples.Widgets;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Widgets;

/// <summary>
/// Demonstrates <see cref="ImGuiWidgets.LogView"/> over a log large enough that filtering and
/// virtualization are the visible features, with a stream to show following.
/// </summary>
/// <remarks>
/// One class per section, registered in <see cref="DemoSections"/>; see "Adding Components" in
/// CLAUDE.md for the rule.
/// </remarks>
internal static class LogViewDemo
{
	private const int SeedCount = 2000;
	private const int StreamPerFrame = 5;
	private const int DropCount = 500;

	private static readonly string[] Categories = ["render", "audio", "io", "net", "script"];
	private static readonly DateTime Origin = new(2026, 1, 1, 9, 0, 0, DateTimeKind.Unspecified);
	private static readonly List<LogEntry> Entries = [];
	private static readonly ImGuiWidgets.LogViewOptions Options = new() { Size = new Vector2(0f, 300f) };

	private static bool stream;
	private static bool seeded;
	private static int written;

	/// <summary>Gets the demo's view state, for tests to read back what the filter did.</summary>
	internal static ImGuiWidgets.LogViewState State { get; private set; } = new();

	/// <summary>Gets the demo's log.</summary>
	internal static IReadOnlyList<LogEntry> Log => Entries;

	/// <summary>
	/// Returns this section's state to its starting values. The demo keeps its state in statics, which
	/// outlive a harness.
	/// </summary>
	internal static void ResetState()
	{
		Entries.Clear();
		written = 0;
		stream = false;
		seeded = false;
		State = new ImGuiWidgets.LogViewState();
	}

	private static void Seed()
	{
		for (int i = 0; i < SeedCount; i++)
		{
			if (i == 700)
			{
				Write(
					LogViewLevel.Error,
					"script",
					"NullReferenceException: Object reference not set to an instance of an object.\n   at Game.Update() in Game.cs:line 42\n   at Engine.Tick()");
			}
			else if (i % 97 is 10 or 11 or 12)
			{
				Write(LogViewLevel.Warning, "render", "frame hitch: 41 ms");
			}
			else
			{
				WriteSynthetic();
			}
		}
	}

	private static void Write(LogViewLevel level, string category, string message)
	{
		Entries.Add(new LogEntry(level, message, Origin.AddMilliseconds(written * 137.0), category));
		written++;
	}

	/// <summary>Writes one plausible line, chosen from the running count so the log is the same every run.</summary>
	private static void WriteSynthetic()
	{
		int n = written;
		LogViewLevel level = (n % 50) switch
		{
			0 => LogViewLevel.Critical,
			< 4 => LogViewLevel.Error,
			< 10 => LogViewLevel.Warning,
			< 30 => LogViewLevel.Information,
			< 42 => LogViewLevel.Debug,
			_ => LogViewLevel.Trace,
		};
		string category = Categories[n % Categories.Length];
		string message = category switch
		{
			"render" => string.Create(CultureInfo.InvariantCulture, $"drew {200 + (n % 900)} batches"),
			"audio" => string.Create(CultureInfo.InvariantCulture, $"mixed {n % 32} voices"),
			"io" => string.Create(CultureInfo.InvariantCulture, $"loaded textures/tile_{n % 400:D3}.png"),
			"net" => string.Create(CultureInfo.InvariantCulture, $"packet {n} acknowledged in {n % 80} ms"),
			_ => string.Create(CultureInfo.InvariantCulture, $"ran script tick {n}"),
		};

		Write(level, category, message);
	}

	/// <summary>Draws the demo section.</summary>
	public static void Show()
	{
		if (!DemoProbe.Header("Log View"))
		{
			return;
		}

		if (!seeded)
		{
			Seed();
			seeded = true;
		}

		ImGui.TextDisabled("Type in the filter, toggle levels, scroll up to stop following, and select rows to copy.");

		DemoProbe.Checkbox("Stream", ref stream);
		ImGui.SameLine();

		if (DemoProbe.Button("Add error"))
		{
			Write(LogViewLevel.Error, "net", "connection reset by peer");
		}

		ImGui.SameLine();

		if (DemoProbe.Button("Clear"))
		{
			Entries.Clear();
		}

		ImGui.SameLine();

		if (DemoProbe.Button("Drop oldest 500"))
		{
			int count = Math.Min(DropCount, Entries.Count);
			Entries.RemoveRange(0, count);
			State.EntriesRemovedFromStart(count);
		}

		if (stream)
		{
			for (int i = 0; i < StreamPerFrame; i++)
			{
				WriteSynthetic();
			}
		}

		ImGuiWidgets.LogView("logDemo", Entries, State, Options);

		ImGui.TextUnformatted(string.Create(
			CultureInfo.InvariantCulture,
			$"{State.SelectedSourceIndices.Count} selected"));
	}
}
