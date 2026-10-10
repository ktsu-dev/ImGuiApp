// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App;

using System.Numerics;

using Silk.NET.Input;

/// <summary>
/// Files dropped onto the window from the file manager.
/// </summary>
public static partial class ImGuiApp
{
	/// <summary>
	/// Forwards the window's file-drop event to <see cref="ImGuiAppConfig.OnFilesDropped"/>.
	/// </summary>
	internal static void SetupWindowFileDropHandler() =>
		window!.FileDrop += paths => DeliverDroppedFiles(paths, DropPosition());

	/// <summary>
	/// Hands dropped paths to the application, skipping a drop that carried nothing.
	/// </summary>
	/// <param name="paths">The paths the windowing backend reported.</param>
	/// <param name="position">Where the pointer was, in ImGui's screen coordinates.</param>
	internal static void DeliverDroppedFiles(string[]? paths, Vector2 position)
	{
		if (paths is null || paths.Length == 0 || Config.OnFilesDropped is null)
		{
			return;
		}

		// A drop is the user doing something, so a throttled application should wake for it.
		OnUserInput();
		Config.OnFilesDropped(paths, position);
	}

	/// <summary>
	/// Reads where the pointer is now, which is where the drop landed.
	/// </summary>
	/// <remarks>
	/// The drop event carries no position, but every desktop backend moves the cursor to the drop
	/// point before raising it, and the mouse reads the cursor live rather than from the last move
	/// event, which the window may not have received while the drag belonged to another application.
	/// </remarks>
	/// <returns>The pointer position in framebuffer pixels, or the origin when there is no mouse.</returns>
	internal static Vector2 DropPosition()
	{
		if (inputContext is null || inputContext.Mice.Count == 0)
		{
			return Vector2.Zero;
		}

		IMouse mouse = inputContext.Mice[0];
		return mouse.Position * WindowToFramebufferScale;
	}
}
