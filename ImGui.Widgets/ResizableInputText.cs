// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;

using Hexa.NET.ImGui;

/// <summary>
/// Text fields whose buffer grows with the text, for every <c>InputText</c> in the suite that edits a
/// <see cref="string"/>.
/// </summary>
/// <remarks>
/// <para>
/// Hexa's <c>InputText(label, ref string, maxLength)</c> tells Dear ImGui the buffer holds
/// <c>maxLength</c> bytes whatever the string's real length, so activating a field whose value is
/// already that long fails a native assertion and aborts the process, which .NET cannot catch
/// (#553). These overloads hand ImGui a native buffer sized from the value and set
/// <see cref="ImGuiInputTextFlags.CallbackResize"/>, so the buffer is reallocated whenever an edit
/// needs more room: there is no length a value can have, typed or pasted, that the field refuses.
/// </para>
/// <para>
/// The file is compiled into <c>ktsu.ImGui.Widgets</c>, whose internals <c>ktsu.ImGui.NodeEditor</c>
/// can see, and linked into <c>ktsu.ImGui.Popups</c>, which references neither.
/// </para>
/// </remarks>
internal static class ResizableInputText
{
	/// <summary>Room left beyond the value's own bytes, so typing rarely needs a resize.</summary>
	private const int Headroom = 64;

	private static readonly ImGuiInputTextCallback Callback = CreateCallback();

	/// <summary>A caller's own callback for the call in progress, given every event but the resize.</summary>
	[ThreadStatic]
	private static ImGuiInputTextCallback? forwardedCallback;

	/// <summary>Draws a single-line text field editing <paramref name="text"/>, however long it grows.</summary>
	/// <param name="label">The ImGui label.</param>
	/// <param name="text">The text being edited.</param>
	/// <param name="flags">Flags for the field. <see cref="ImGuiInputTextFlags.CallbackResize"/> is always added.</param>
	/// <param name="callback">
	/// An optional callback for the events <paramref name="flags"/> asks for. It is never given the
	/// resize event, which this class answers itself.
	/// </param>
	/// <returns>What <c>ImGui.InputText</c> returns: true when the text was edited, or when Enter was pressed with <see cref="ImGuiInputTextFlags.EnterReturnsTrue"/>.</returns>
	internal static bool InputText(string label, ref string text, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None, ImGuiInputTextCallback? callback = null) =>
		Draw(label, hint: null, ref text, flags, callback);

	/// <summary>Draws a single-line text field with a hint shown while it is empty.</summary>
	/// <param name="label">The ImGui label.</param>
	/// <param name="hint">The text shown while the field is empty.</param>
	/// <param name="text">The text being edited.</param>
	/// <param name="flags">Flags for the field. <see cref="ImGuiInputTextFlags.CallbackResize"/> is always added.</param>
	/// <returns>What <c>ImGui.InputTextWithHint</c> returns.</returns>
	internal static bool InputTextWithHint(string label, string hint, ref string text, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None) =>
		Draw(label, hint, ref text, flags, callback: null);

	/// <summary>The buffer's size for a value of <paramref name="byteCount"/> UTF-8 bytes: the bytes, the terminator, and some headroom.</summary>
	/// <param name="byteCount">The value's length in UTF-8 bytes.</param>
	/// <returns>The size to allocate, in bytes.</returns>
	internal static int InitialCapacity(int byteCount) => byteCount + 1 + Headroom;

	[SuppressMessage("Major Code Smell", "S6640:Make sure that using \"unsafe\" is safe here", Justification = "The buffer is allocated, grown and freed within this call; ImGui does not keep the pointer past it, and every write stays inside the size it is told.")]
	private static unsafe bool Draw(string label, string? hint, ref string text, ImGuiInputTextFlags flags, ImGuiInputTextCallback? callback)
	{
		Ensure.NotNull(label);
		string value = text ?? string.Empty;

		int byteCount = Encoding.UTF8.GetByteCount(value);
		Buffer buffer = new()
		{
			Capacity = InitialCapacity(byteCount),
		};
		buffer.Data = (byte*)NativeMemory.Alloc((nuint)buffer.Capacity);

		ImGuiInputTextCallback? outerCallback = forwardedCallback;
		try
		{
			int written = Encoding.UTF8.GetBytes(value, new Span<byte>(buffer.Data, buffer.Capacity));
			buffer.Data[written] = 0;

			forwardedCallback = callback;
			ImGuiInputTextFlags allFlags = flags | ImGuiInputTextFlags.CallbackResize;
			bool result = hint is null
				? ImGui.InputText(label, buffer.Data, (nuint)buffer.Capacity, allFlags, Callback, &buffer)
				: ImGui.InputTextWithHint(label, hint, buffer.Data, (nuint)buffer.Capacity, allFlags, Callback, &buffer);

			// The same rule Hexa's string overloads read the buffer back by: an edit, or an Enter-only
			// field left after an edit.
			if (result || ((flags & ImGuiInputTextFlags.EnterReturnsTrue) != 0 && ImGui.IsItemDeactivatedAfterEdit()))
			{
				int length = new ReadOnlySpan<byte>(buffer.Data, buffer.Capacity).IndexOf((byte)0);
				text = Encoding.UTF8.GetString(buffer.Data, length < 0 ? buffer.Capacity : length);
			}

			return result;
		}
		finally
		{
			forwardedCallback = outerCallback;
			NativeMemory.Free(buffer.Data);
		}
	}

	[SuppressMessage("Major Code Smell", "S6640:Make sure that using \"unsafe\" is safe here", Justification = "Converting the method group to the callback delegate needs an unsafe context because the delegate type takes a pointer, but no pointer is touched here.")]
	private static unsafe ImGuiInputTextCallback CreateCallback() => OnCallback;

	[SuppressMessage("Major Code Smell", "S6640:Make sure that using \"unsafe\" is safe here", Justification = "ImGui passes a pointer valid for the call, and the user data is the Buffer on the stack of the Draw call that is running it.")]
	private static unsafe int OnCallback(ImGuiInputTextCallbackData* data)
	{
		if (data->EventFlag != ImGuiInputTextFlags.CallbackResize)
		{
			return forwardedCallback?.Invoke(data) ?? 0;
		}

		// ImGui asks for at least BufSize bytes and copies the text in itself once Buf points at them.
		// Realloc keeps what the buffer already holds.
		Buffer* buffer = (Buffer*)data->UserData;
		int required = data->BufSize;
		if (required > buffer->Capacity)
		{
			int grown = Math.Max(required, buffer->Capacity * 2);
			buffer->Data = (byte*)NativeMemory.Realloc(buffer->Data, (nuint)grown);
			buffer->Capacity = grown;
		}

		data->Buf = buffer->Data;
		return 0;
	}

	/// <summary>The native buffer under edit, reachable from the resize callback through its user data.</summary>
	private unsafe struct Buffer
	{
		public byte* Data;
		public int Capacity;
	}
}
