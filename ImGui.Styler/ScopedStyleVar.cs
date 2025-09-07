// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Styler;

using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ScopedAction;

/// <summary>
/// Represents a scoped style variable for ImGui. This class ensures that the style variable is popped when disposed.
/// </summary>
public class ScopedStyleVar : ScopedAction
{
	/// <summary>
	/// Initializes a new instance of the <see cref="ScopedStyleVar"/> class with a vector value.
	/// </summary>
	/// <param name="target">The style variable to be pushed.</param>
	/// <param name="vector">The vector value to be applied to the style variable.</param>
	public ScopedStyleVar(ImGuiStyleVar target, Vector2 vector)
	{
		ImGui.PushStyleVar(target, vector);
		OnClose = ImGui.PopStyleVar;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ScopedStyleVar"/> class with a scalar value.
	/// </summary>
	/// <param name="target">The style variable to be pushed.</param>
	/// <param name="scalar">The scalar value to be applied to the style variable.</param>
	public ScopedStyleVar(ImGuiStyleVar target, float scalar)
	{
		ImGui.PushStyleVar(target, scalar);
		OnClose = ImGui.PopStyleVar;
	}
}
