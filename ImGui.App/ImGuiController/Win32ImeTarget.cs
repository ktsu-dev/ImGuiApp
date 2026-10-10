// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.ImGuiController;

using System;

/// <summary>
/// Places the Win32 input method manager's windows at a caret.
/// </summary>
internal sealed class Win32ImeTarget : IImeTarget
{
	/// <inheritdoc />
	public void Place(nint windowHandle, ImePlacement placement)
	{
		nint context = NativeMethods.ImmGetContext(windowHandle);
		if (context == 0)
		{
			return;
		}

		try
		{
			int left = (int)MathF.Round(placement.Position.X);
			int top = (int)MathF.Round(placement.Position.Y);
			int bottom = top + Math.Max(1, (int)MathF.Ceiling(placement.LineHeight));

			// The composition window (the inline pre-edit some input methods draw themselves) at
			// the caret, and the candidate list kept off the caret's line so it never covers the
			// text being composed.
			NativeMethods.COMPOSITIONFORM composition = new()
			{
				dwStyle = NativeMethods.CFS_FORCE_POSITION,
				ptCurrentPos = new() { X = left, Y = top },
			};
			_ = NativeMethods.ImmSetCompositionWindow(context, ref composition);

			NativeMethods.CANDIDATEFORM candidate = new()
			{
				dwIndex = 0,
				dwStyle = NativeMethods.CFS_EXCLUDE,
				ptCurrentPos = new() { X = left, Y = top },
				rcArea = new() { Left = left, Top = top, Right = left + 1, Bottom = bottom },
			};
			_ = NativeMethods.ImmSetCandidateWindow(context, ref candidate);
		}
		finally
		{
			_ = NativeMethods.ImmReleaseContext(windowHandle, context);
		}
	}
}
