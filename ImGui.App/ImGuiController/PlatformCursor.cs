// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.ImGuiController;

using Hexa.NET.ImGui;
using Silk.NET.Input;

/// <summary>
/// Shows the cursor a frame asked for on the operating system's own pointer.
/// </summary>
/// <remarks>
/// Dear ImGui decides a cursor every frame, from a hovered resize grip or text field or from a
/// widget's own <c>ImGui.SetMouseCursor</c> call, but it only records the decision; a platform
/// backend has to carry it to the window. Without this, every cursor a widget chose was dropped
/// and the pointer stayed an arrow over a splitter as over everything else.
/// </remarks>
internal sealed class PlatformCursor
{
	private ImGuiMouseCursor? applied;

	/// <summary>
	/// Maps a Dear ImGui cursor to the standard cursor that looks like it.
	/// </summary>
	/// <param name="cursor">The cursor the frame asked for.</param>
	/// <returns>The standard cursor, or null when the pointer should be hidden.</returns>
	internal static StandardCursor? ToStandardCursor(ImGuiMouseCursor cursor) => cursor switch
	{
		ImGuiMouseCursor.None => null,
		ImGuiMouseCursor.TextInput => StandardCursor.IBeam,
		ImGuiMouseCursor.ResizeAll => StandardCursor.ResizeAll,
		ImGuiMouseCursor.ResizeNs => StandardCursor.VResize,
		ImGuiMouseCursor.ResizeEw => StandardCursor.HResize,
		ImGuiMouseCursor.ResizeNesw => StandardCursor.NeswResize,
		ImGuiMouseCursor.ResizeNwse => StandardCursor.NwseResize,
		ImGuiMouseCursor.Hand => StandardCursor.Hand,
		ImGuiMouseCursor.NotAllowed => StandardCursor.NotAllowed,
		ImGuiMouseCursor.Wait => StandardCursor.Wait,
		ImGuiMouseCursor.Progress => StandardCursor.WaitArrow,
		_ => StandardCursor.Arrow,
	};

	/// <summary>
	/// Applies the cursor the frame asked for, writing to the platform only when it changed.
	/// </summary>
	/// <param name="cursor">The platform cursor of the window's mouse.</param>
	/// <param name="requested">What <c>ImGui.GetMouseCursor()</c> reported once the frame ended.</param>
	/// <param name="imGuiDrawsCursor">True when <c>io.MouseDrawCursor</c> is set, so ImGui draws the pointer itself.</param>
	/// <param name="changesDisabled">True when <c>ImGuiConfigFlags.NoMouseCursorChange</c> is set.</param>
	internal void Apply(ICursor cursor, ImGuiMouseCursor requested, bool imGuiDrawsCursor, bool changesDisabled)
	{
		if (changesDisabled)
		{
			return;
		}

		// An application that captured the mouse (a camera that hides and locks the pointer) has
		// taken it over; showing a cursor shape there would undo that.
		if (cursor.CursorMode is CursorMode.Disabled or CursorMode.Raw)
		{
			applied = null;
			return;
		}

		// ImGui drawing its own pointer means the system one would be a second arrow on top of it.
		ImGuiMouseCursor effective = imGuiDrawsCursor ? ImGuiMouseCursor.None : requested;
		if (applied == effective)
		{
			return;
		}

		applied = effective;

		StandardCursor? standard = ToStandardCursor(effective);
		if (standard is null)
		{
			cursor.CursorMode = CursorMode.Hidden;
			return;
		}

		cursor.CursorMode = CursorMode.Normal;
		cursor.Type = CursorType.Standard;
		cursor.StandardCursor = cursor.IsSupported(standard.Value) ? standard.Value : StandardCursor.Default;
	}
}
