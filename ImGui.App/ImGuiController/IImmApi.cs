// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.ImGuiController;

/// <summary>
/// The four Win32 input method manager calls <see cref="Win32ImeTarget"/> makes, behind a seam so
/// what it sends can be checked without Windows.
/// </summary>
internal interface IImmApi
{
	/// <summary>Returns the input method context of a window, or zero when it has none.</summary>
	/// <param name="windowHandle">The window.</param>
	/// <returns>The context.</returns>
	public nint GetContext(nint windowHandle);

	/// <summary>Releases a context obtained from <see cref="GetContext"/>.</summary>
	/// <param name="windowHandle">The window.</param>
	/// <param name="context">The context.</param>
	public void ReleaseContext(nint windowHandle, nint context);

	/// <summary>Positions the composition window.</summary>
	/// <param name="context">The context.</param>
	/// <param name="form">Where it goes.</param>
	public void SetCompositionWindow(nint context, NativeMethods.COMPOSITIONFORM form);

	/// <summary>Positions the candidate list.</summary>
	/// <param name="context">The context.</param>
	/// <param name="form">Where it goes.</param>
	public void SetCandidateWindow(nint context, NativeMethods.CANDIDATEFORM form);
}
