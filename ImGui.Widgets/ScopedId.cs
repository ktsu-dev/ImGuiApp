// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGuiWidgets;

using Hexa.NET.ImGui;
using ktsu.ScopedAction;

/// <summary>
/// Provides custom ImGui widgets.
/// </summary>
public static partial class ImGuiWidgets
{
	/// <summary>
	/// Represents a scoped ID for Dear ImGui. This class ensures that the ID is pushed when the object is created
	/// and popped when the object is disposed.
	/// </summary>
	public class ScopedId : ScopedAction
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ScopedId"/> class with a string ID.
		/// </summary>
		/// <param name="id">The string ID to push to ImGui.</param>
		public ScopedId(string id)
		{
			ImGui.PushID(id);
			OnClose = ImGui.PopID;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ScopedId"/> class with an integer ID.
		/// </summary>
		/// <param name="id">The integer ID to push to ImGui.</param>
		public ScopedId(int id)
		{
			ImGui.PushID(id);
			OnClose = ImGui.PopID;
		}
	}
}
