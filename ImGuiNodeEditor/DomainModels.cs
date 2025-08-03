// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGuiNodeEditor;
using System.Collections.Generic;
using System.Numerics;
using ktsu.Semantics;

/// <summary>
/// Represents a node in the editor
/// </summary>
public record Node(
	int Id,
	Vector2 Position,
	string Name,
	List<Pin> InputPins,
	List<Pin> OutputPins,
	Vector2 Dimensions = default,
	Vector2 Velocity = default,
	Vector2 Force = default
);

/// <summary>
/// Represents a connection between two pins
/// </summary>
public record Link(
	int Id,
	int OutputPinId,
	int InputPinId
);

/// <summary>
/// Represents a pin on a node
/// </summary>
public record Pin(
	int Id,
	PinDirection Direction,
	string Name,
	string? DisplayName = null
)
{
	/// <summary>
	/// Gets the name to display in the UI, preferring DisplayName over Name
	/// </summary>
	public string EffectiveDisplayName => DisplayName ?? Name;
};

/// <summary>
/// Direction of pin (input or output)
/// </summary>
public enum PinDirection
{
	/// <inheritdoc/>
	Input,
	/// <inheritdoc/>
	Output
}

/// <summary>
/// Physics simulation settings using semantic strong types
/// </summary>
public record PhysicsSettings
{
	public bool Enabled { get; init; } = false;
	public Force<float> RepulsionStrength { get; init; } = Force<float>.FromNewtons(1000.0f);
	public float LinkSpringStrength { get; init; } = 0.5f; // Dimensionless spring constant
	public Force<float> GravityStrength { get; init; } = Force<float>.FromNewtons(50.0f);
	public float DampingFactor { get; init; } = 0.9f; // Dimensionless damping coefficient
	public Length<float> MinRepulsionDistance { get; init; } = Length<float>.FromMeters(100.0f);
	public Length<float> RestLinkLength { get; init; } = Length<float>.FromMeters(200.0f);
	public Force<float> MaxForce { get; init; } = Force<float>.FromNewtons(500.0f);
	public Velocity<float> MaxVelocity { get; init; } = Velocity<float>.FromMetersPerSecond(200.0f);
	public Frequency<float> TargetPhysicsHz { get; init; } = Frequency<float>.FromHertz(120.0f);
}
