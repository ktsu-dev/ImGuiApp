// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;
using System.Text;

using Hexa.NET.ImGui;

/// <summary>
/// Text fields whose capacity follows the text, for every <c>InputText</c> in the suite that edits a
/// <see cref="string"/>.
/// </summary>
/// <remarks>
/// <para>
/// Hexa's <c>InputText(label, ref string, maxLength)</c> allocates room for the larger of the string
/// and <c>maxLength</c>, but tells Dear ImGui the buffer is <c>maxLength</c> bytes. With a fixed
/// <c>maxLength</c>, activating a field whose value is already that long fails a native assertion and
/// aborts the process, which .NET cannot catch (#553).
/// </para>
/// <para>
/// These overloads pass a capacity worked out every frame from the value's own UTF-8 length: the
/// value, its terminator, and headroom of <see cref="MinimumHeadroom"/> bytes or the value's own
/// length, whichever is larger. The value always fits, so the assertion cannot fire however long it
/// is.
/// </para>
/// <para>
/// The trade-off: Dear ImGui fixes a field's capacity when the field is activated and does not take a
/// larger one while it stays active, so one edit has room for the value it started with plus the
/// headroom. Text typed or pasted past that within the same edit is cut, never an abort, and the next
/// edit starts from the longer value with headroom again. Lifting that limit means growing the buffer
/// inside the frame through <see cref="ImGuiInputTextFlags.CallbackResize"/>, which needs a native
/// buffer and pointer code, which the quality gate refuses, so this stays managed.
/// </para>
/// <para>
/// The file is compiled into <c>ktsu.ImGui.Widgets</c>, whose internals <c>ktsu.ImGui.NodeEditor</c>
/// can see, and linked into <c>ktsu.ImGui.Popups</c>, which references neither.
/// </para>
/// </remarks>
internal static class ResizableInputText
{
	/// <summary>The least room, in UTF-8 bytes, an edit has beyond the value it started with.</summary>
	internal const int MinimumHeadroom = 1024;

	/// <summary>Draws a single-line text field editing <paramref name="text"/>, whatever its length.</summary>
	/// <param name="label">The ImGui label.</param>
	/// <param name="text">The text being edited.</param>
	/// <param name="flags">Flags for the field.</param>
	/// <returns>What <c>ImGui.InputText</c> returns: true when the text was edited, or when Enter was pressed with <see cref="ImGuiInputTextFlags.EnterReturnsTrue"/>.</returns>
	internal static bool InputText(string label, ref string text, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
	{
		text ??= string.Empty;
		return ImGui.InputText(label, ref text, Capacity(text), flags);
	}

	/// <summary>Draws a single-line text field with a callback for the events <paramref name="flags"/> asks for.</summary>
	/// <param name="label">The ImGui label.</param>
	/// <param name="text">The text being edited.</param>
	/// <param name="flags">Flags for the field.</param>
	/// <param name="callback">The callback for the events <paramref name="flags"/> asks for.</param>
	/// <returns>What <c>ImGui.InputText</c> returns.</returns>
	internal static bool InputText(string label, ref string text, ImGuiInputTextFlags flags, ImGuiInputTextCallback callback)
	{
		text ??= string.Empty;
		return ImGui.InputText(label, ref text, Capacity(text), flags, callback);
	}

	/// <summary>Draws a single-line text field with a hint shown while it is empty.</summary>
	/// <param name="label">The ImGui label.</param>
	/// <param name="hint">The text shown while the field is empty.</param>
	/// <param name="text">The text being edited.</param>
	/// <param name="flags">Flags for the field.</param>
	/// <returns>What <c>ImGui.InputTextWithHint</c> returns.</returns>
	internal static bool InputTextWithHint(string label, string hint, ref string text, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
	{
		text ??= string.Empty;
		return ImGui.InputTextWithHint(label, hint, ref text, Capacity(text), flags);
	}

	/// <summary>
	/// The capacity to declare for <paramref name="text"/>: its UTF-8 bytes, the terminator, and
	/// headroom of <see cref="MinimumHeadroom"/> or the text's own length, whichever is larger.
	/// </summary>
	/// <param name="text">The text the field holds this frame.</param>
	/// <returns>The capacity in bytes, always larger than the text.</returns>
	internal static nuint Capacity(string text)
	{
		Ensure.NotNull(text);
		int byteCount = Encoding.UTF8.GetByteCount(text);
		return (nuint)(byteCount + 1 + Math.Max(MinimumHeadroom, byteCount));
	}
}
