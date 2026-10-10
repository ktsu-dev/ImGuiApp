// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.ImGuiController;

/// <summary>
/// The input method manager calls as <c>imm32.dll</c> makes them.
/// </summary>
internal sealed class NativeImmApi : IImmApi
{
	/// <inheritdoc />
	public nint GetContext(nint windowHandle) => NativeMethods.ImmGetContext(windowHandle);

	/// <inheritdoc />
	public void ReleaseContext(nint windowHandle, nint context) => _ = NativeMethods.ImmReleaseContext(windowHandle, context);

	/// <inheritdoc />
	public void SetCompositionWindow(nint context, NativeMethods.COMPOSITIONFORM form) => _ = NativeMethods.ImmSetCompositionWindow(context, ref form);

	/// <inheritdoc />
	public void SetCandidateWindow(nint context, NativeMethods.CANDIDATEFORM form) => _ = NativeMethods.ImmSetCandidateWindow(context, ref form);
}
