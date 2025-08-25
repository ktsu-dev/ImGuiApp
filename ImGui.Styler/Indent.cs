// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGuiStyler;

using Hexa.NET.ImGui;

using ktsu.ScopedAction;

/// <summary>
/// Provides methods to create scoped indents for ImGui.
/// </summary>
public static class Indent
{
	/// <summary>
	/// Creates a scoped indent with the default width.
	/// </summary>
	/// <returns>A new instance of <see cref="ScopedIndent"/>.</returns>
	public static ScopedIndent ByDefault() => new();

	/// <summary>
	/// Creates a scoped indent with a specified width.
	/// </summary>
	/// <param name="width">The width of the indent.</param>
	/// <returns>A new instance of <see cref="ScopedIndentBy"/>.</returns>
	public static ScopedIndentBy By(float width) => new(width);

	/// <summary>
	/// Represents a scoped indent action.
	/// </summary>
	public class ScopedIndent : ScopedAction
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ScopedIndent"/> class.
		/// </summary>
		public ScopedIndent() : base(ImGui.Indent, ImGui.Unindent) { }
	}

	/// <summary>
	/// Represents a scoped indent action with a specified width.
	/// </summary>
	public class ScopedIndentBy : ScopedStyleVar
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ScopedIndentBy"/> class with a specified width.
		/// </summary>
		/// <param name="width">The width of the indent.</param>
		public ScopedIndentBy(float width) : base(ImGuiStyleVar.IndentSpacing, width)
		{
			ImGui.Indent(width);
			Action? onClose = OnClose;
			OnClose = () =>
			{
				ImGui.Unindent(width);
				onClose?.Invoke();
			};
		}
	}
}
