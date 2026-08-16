// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

public static partial class ImGuiWidgets
{
	/// <summary>
	/// Guards a stateful dialog wrapper against being shown twice.
	/// </summary>
	/// <remarks>
	/// Hexa's <c>Dialog.Show()</c> unconditionally adds the instance to its static manager, so
	/// showing one instance twice registers it twice. Both entries draw into the same ImGui window,
	/// and closing removes only one of them; the survivor is never drawn, never closed and never
	/// removed, which latches <c>WidgetManager.BlockInput</c> on for the life of the process. This
	/// guard turns that unrecoverable state into an immediate, diagnosable exception.
	/// </remarks>
	/// <param name="dialogName">The wrapper type's name, used in the exception message.</param>
	internal sealed class DialogShowGuard(string dialogName)
	{
		/// <summary>
		/// Gets a value indicating whether the dialog is currently shown.
		/// </summary>
		internal bool IsShown { get; private set; }

		/// <summary>
		/// Marks the dialog as shown.
		/// </summary>
		/// <exception cref="InvalidOperationException">The dialog is already shown.</exception>
		internal void Enter()
		{
			if (IsShown)
			{
				throw new InvalidOperationException(
					$"{dialogName}.Show() was called while the dialog is already shown. Hexa registers " +
					"each Show() with its dialog manager unconditionally, so showing one instance twice " +
					"registers it twice and leaves an entry that is never drawn, never closed and never " +
					"removed - which blocks input for the life of the process. Wait for the close " +
					"callback, or create a new instance per showing.");
			}

			IsShown = true;
		}

		/// <summary>
		/// Marks the dialog as no longer shown, so it may be shown again.
		/// </summary>
		internal void Exit() => IsShown = false;
	}
}
