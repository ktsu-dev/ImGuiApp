// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App;

using System.Numerics;

using Hexa.NET.ImGui;

/// <summary>
/// Tells the operating system's input method where text is being typed, so its candidate list
/// opens at the caret rather than in a corner of the screen.
/// </summary>
/// <remarks>
/// <para>
/// Dear ImGui's own text fields already do this. A widget that edits text without
/// <c>ImGui.InputText</c>, reading keys and <c>InputQueueCharacters</c> itself, has to say where
/// its caret is, or an input method for Chinese, Japanese or Korean opens its candidates wherever
/// the window last left them. Call <see cref="Set"/> once per frame for as long as the caret is
/// shown; the request lapses at the next frame, exactly as ImGui's own does.
/// </para>
/// <para>
/// The request also sets <c>io.WantTextInput</c> from the next frame, as a focused ImGui text field
/// does, so an application that suppresses shortcuts while text is being typed treats this caret
/// the same way.
/// </para>
/// <para>
/// Where the request goes depends on the platform. On Windows the desktop backend moves the
/// composition and candidate windows of the window's input context to the caret. GLFW, which the
/// desktop backend runs on, has no input method API on Linux or macOS, so there the request is
/// recorded and goes no further; <see cref="Requested"/> still reports it, which is what a headless
/// UI test asserts against.
/// </para>
/// </remarks>
public static class ImeCaret
{
	/// <summary>
	/// Reports the caret for the current frame.
	/// </summary>
	/// <param name="position">The caret's top-left corner, in ImGui's screen coordinates, as
	/// <c>ImGui.GetItemRectMin</c> and the draw lists use them. For text drawn rotated, pass the
	/// screen position the caret ends up at.</param>
	/// <param name="lineHeight">The height of the caret's line, so the candidate list opens below
	/// the line rather than over it.</param>
	public static void Set(Vector2 position, float lineHeight)
	{
		ref ImGuiPlatformImeData data = ref ImGui.GetCurrentContext().PlatformImeData;
		data.WantVisible = 1;
		data.WantTextInput = 1;
		data.InputPos = position;
		data.InputLineHeight = lineHeight;
		data.ViewportId = ImGui.GetMainViewport().ID;
	}

	/// <summary>
	/// Gets where the current frame has asked the input method to open, by this class or by a
	/// focused ImGui text field, or null when nothing has.
	/// </summary>
	/// <remarks>
	/// Read once a frame has ended, this is the request the backend carries to the platform.
	/// </remarks>
	public static ImePlacement? Requested
	{
		get
		{
			ref ImGuiPlatformImeData data = ref ImGui.GetCurrentContext().PlatformImeData;
			return data.WantVisible != 0 ? new ImePlacement(data.InputPos, data.InputLineHeight) : null;
		}
	}
}
