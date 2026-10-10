// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.ImGuiController;

using System;

/// <summary>
/// Places the Win32 input method manager's windows at a caret.
/// </summary>
/// <param name="imm">The input method manager calls; <see cref="NativeImmApi"/> outside tests.</param>
internal sealed class Win32ImeTarget(IImmApi imm) : IImeTarget
{
	/// <summary>
	/// Creates a target that calls <c>imm32.dll</c>.
	/// </summary>
	public Win32ImeTarget() : this(new NativeImmApi())
	{
	}

	/// <summary>
	/// Builds the composition window's position: forced to the caret, where an input method that
	/// draws its own pre-edit text draws it.
	/// </summary>
	/// <param name="placement">The caret, in window client pixels.</param>
	/// <returns>The form to send.</returns>
	internal static NativeMethods.COMPOSITIONFORM CompositionAt(ImePlacement placement) => new()
	{
		dwStyle = NativeMethods.CFS_FORCE_POSITION,
		ptCurrentPos = TopLeft(placement),
	};

	/// <summary>
	/// Builds the candidate list's position: anchored at the caret and kept off the caret's line, so
	/// it never covers the text being composed.
	/// </summary>
	/// <param name="placement">The caret, in window client pixels.</param>
	/// <returns>The form to send.</returns>
	internal static NativeMethods.CANDIDATEFORM CandidateAt(ImePlacement placement)
	{
		NativeMethods.POINT top = TopLeft(placement);
		int bottom = top.Y + Math.Max(1, (int)MathF.Ceiling(placement.LineHeight));
		return new()
		{
			dwIndex = 0,
			dwStyle = NativeMethods.CFS_EXCLUDE,
			ptCurrentPos = top,
			rcArea = new() { Left = top.X, Top = top.Y, Right = top.X + 1, Bottom = bottom },
		};
	}

	/// <inheritdoc />
	public void Place(nint windowHandle, ImePlacement placement)
	{
		nint context = imm.GetContext(windowHandle);
		if (context == 0)
		{
			return;
		}

		try
		{
			imm.SetCompositionWindow(context, CompositionAt(placement));
			imm.SetCandidateWindow(context, CandidateAt(placement));
		}
		finally
		{
			imm.ReleaseContext(windowHandle, context);
		}
	}

	private static NativeMethods.POINT TopLeft(ImePlacement placement) => new()
	{
		X = (int)MathF.Round(placement.Position.X),
		Y = (int)MathF.Round(placement.Position.Y),
	};
}
