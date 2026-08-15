// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Text;

using Hexa.NET.ImGui;

using HexaButton = Hexa.NET.ImGui.Widgets.ImGuiButton;
using HexaInlineButtonFlags = Hexa.NET.ImGui.Widgets.InlineButtonFlags;

/// <summary>
/// Placement options for <see cref="ImGuiWidgets.InlineButton"/>.
/// </summary>
[Flags]
public enum InlineButtonPlacement
{
	/// <summary>
	/// Default placement: rounded corners, sized to the label.
	/// </summary>
	None = 0,

	/// <summary>
	/// Draws the button with square corners.
	/// </summary>
	NoRounding = 1 << 0,

	/// <summary>
	/// Expands the button to fill the supplied bounds.
	/// </summary>
	FillSpace = 1 << 1,
}

public static partial class ImGuiWidgets
{
	/// <summary>
	/// Draws a sliding on/off switch.
	/// </summary>
	/// <param name="label">Label for display and identity.</param>
	/// <param name="selected">The switch state, updated in place when toggled.</param>
	/// <returns><see langword="true"/> if the state changed this frame.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="label"/> is <see langword="null"/>.</exception>
	public static bool ToggleSwitch(string label, ref bool selected)
	{
		Ensure.NotNull(label);
		return HexaButtonImpl.ToggleSwitch(label, ref selected);
	}

	/// <summary>
	/// Draws a button that shows a highlight ring while <paramref name="selected"/> is set.
	/// </summary>
	/// <param name="label">Label for display and identity.</param>
	/// <param name="selected">The toggle state, updated in place when clicked.</param>
	/// <param name="size">Button size in pixels, or <see langword="default"/> to size to the label.</param>
	/// <returns><see langword="true"/> if the state changed this frame.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="label"/> is <see langword="null"/>.</exception>
	public static bool ToggleButton(string label, ref bool selected, Vector2 size = default)
	{
		Ensure.NotNull(label);
		return HexaButtonImpl.ToggleButton(label, ref selected, size);
	}

	/// <summary>
	/// Draws a button with no background until it is hovered.
	/// </summary>
	/// <param name="label">Label for display and identity.</param>
	/// <param name="size">Button size in pixels, or <see langword="default"/> to size to the label.</param>
	/// <returns><see langword="true"/> if the button was clicked this frame.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="label"/> is <see langword="null"/>.</exception>
	public static bool TransparentButton(string label, Vector2 size = default)
	{
		Ensure.NotNull(label);
		return HexaButtonImpl.TransparentButton(label, size);
	}

	/// <summary>
	/// Draws a compact button anchored inside an existing rectangle, for use inside rows and headers.
	/// </summary>
	/// <param name="label">Label for display and identity.</param>
	/// <param name="min">Top-left corner of the bounds to anchor within, in screen space.</param>
	/// <param name="max">Bottom-right corner of the bounds to anchor within, in screen space.</param>
	/// <param name="anchor">Normalised anchor point within the bounds, where (0,0) is top-left and (1,1) is bottom-right.</param>
	/// <param name="placement">Placement options.</param>
	/// <returns><see langword="true"/> if the button was clicked this frame.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="label"/> is <see langword="null"/>.</exception>
	public static bool InlineButton(string label, Vector2 min, Vector2 max, Vector2 anchor, InlineButtonPlacement placement = InlineButtonPlacement.None)
	{
		Ensure.NotNull(label);
		return HexaButtonImpl.InlineButton(label, min, max, anchor, placement);
	}

	/// <summary>
	/// Contains the implementation details for rendering Hexa-backed buttons. Isolated behind a nested type so the
	/// Hexa interop surface (<see cref="HexaButton"/>, <see cref="Encoding"/>, <see cref="ImRect"/>, ...) does not
	/// count against the class coupling of the public <see cref="ImGuiWidgets"/> facade.
	/// </summary>
	internal static class HexaButtonImpl
	{
		internal static bool ToggleSwitch(string label, ref bool selected)
		{
			// ToggleSwitch is animated, and Hexa's animation clock only advances when something
			// ticks it. Without this the switch renders inverted after its first click; see
			// HexaAnimationPump.
			HexaAnimationPump.TickOncePerFrame();
			return HexaButton.ToggleSwitch(label, ref selected);
		}

		internal static bool ToggleButton(string label, ref bool selected, Vector2 size) => HexaButton.ToggleButton(label, ref selected, size, ImGuiButtonFlags.None);

		[SuppressMessage("Major Code Smell", "S6640:Make sure that using \"unsafe\" is safe here", Justification = "Required to reach Hexa's sized TransparentButton overload; the buffer is stack-scoped to the call and not retained.")]
		internal static unsafe bool TransparentButton(string label, Vector2 size)
		{
			if (size == default)
			{
				return HexaButton.TransparentButton(label);
			}

			int byteCount = Encoding.UTF8.GetByteCount(label);
			Span<byte> buffer = byteCount < 256 ? stackalloc byte[byteCount + 1] : new byte[byteCount + 1];
			Encoding.UTF8.GetBytes(label, buffer);
			buffer[byteCount] = 0;
			fixed (byte* pLabel = buffer)
			{
				return HexaButton.TransparentButton(pLabel, size, ImGuiButtonFlags.None);
			}
		}

		internal static bool InlineButton(string label, Vector2 min, Vector2 max, Vector2 anchor, InlineButtonPlacement placement)
		{
			int byteCount = Encoding.UTF8.GetByteCount(label);
			Span<byte> buffer = byteCount < 256 ? stackalloc byte[byteCount + 1] : new byte[byteCount + 1];
			Encoding.UTF8.GetBytes(label, buffer);
			buffer[byteCount] = 0;

			ImRect bounds = new(min, max);
			return HexaButton.InlineButton(buffer, bounds, anchor, (HexaInlineButtonFlags)placement);
		}
	}
}
