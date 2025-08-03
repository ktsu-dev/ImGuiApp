// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

// Data generator utility nodes.

namespace ktsu.NodeGraph.Library.Utilities;
using System;
using System.ComponentModel;

/// <summary>
/// Random number generator node.
/// </summary>
[Node] // Display name will be "Random"
[Description("Generates random numbers within a specified range")]
public class RandomNode
{
	private static readonly Random random = new();

	[InputPin]
	[Description("Minimum value (inclusive)")]
	public double Min { get; set; } = 0.0;

	[InputPin]
	[Description("Maximum value (exclusive)")]
	public double Max { get; set; } = 1.0;

	[OutputPin]
	[Description("Random value between Min and Max")]
	public double Value => (random.NextDouble() * (Max - Min)) + Min;

	[OutputPin]
	[Description("Random integer between Min and Max-1")]
	public int IntValue => random.Next((int)Min, (int)Max);

	[OutputPin]
	[Description("Random boolean value")]
	public bool Boolean => random.NextDouble() >= 0.5;

	[OutputPin]
	[Description("Random GUID")]
	public string Guid => System.Guid.NewGuid().ToString();
}

/// <summary>
/// Timer node for time-based operations.
/// </summary>
[Node] // Display name will be "Timer"
[Description("Provides current time and time-based calculations")]
public class TimerNode
{
	[OutputPin]
	[Description("Current UTC time")]
	public DateTime UtcNow => DateTime.UtcNow;

	[OutputPin]
	[Description("Current local time")]
	public DateTime Now => DateTime.Now;

	[OutputPin]
	[Description("Unix timestamp (seconds since epoch)")]
	public long UnixTimestamp => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

	[OutputPin]
	[Description("Milliseconds since epoch")]
	public long Milliseconds => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

	[InputPin]
	[Description("Start time for elapsed calculation")]
	public DateTime? StartTime { get; set; }

	[OutputPin]
	[Description("Elapsed time since StartTime")]
	public TimeSpan Elapsed => StartTime.HasValue ? DateTime.UtcNow - StartTime.Value : TimeSpan.Zero;
}
