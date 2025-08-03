// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

// Geometric utility nodes.

namespace ktsu.NodeGraph.Library.Utilities;
using System.ComponentModel;

/// <summary>
/// Example node with constructor parameters that will get implicit input pins.
/// </summary>
/// <remarks>
/// Constructor with required parameters - these will become input pins automatically.
/// </remarks>
[Node] // Display name will be "Rectangle" (removes "Node" suffix automatically)
[Description("Creates a rectangle with width and height")]
public class RectangleNode(float width, float height)
{
	public float Width { get; } = width;
	public float Height { get; } = height;

	[OutputPin("Area")]
	[Description("The area of the rectangle")]
	public float Area => Width * Height;

	[OutputPin("Perimeter")]
	[Description("The perimeter of the rectangle")]
	public float Perimeter => 2 * (Width + Height);
}
