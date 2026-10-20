// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;
using System.Collections.Generic;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Probes;
using ktsu.ImGui.Styler;
using ktsu.ImGui.Widgets.Overlays;

/// <summary>One button of an action sheet.</summary>
/// <param name="Label">The button's text.</param>
/// <param name="IsDestructive">Whether the button deletes or discards something, which draws it in the error colour.</param>
public readonly record struct ActionSheetButton(string Label, bool IsDestructive = false);

/// <summary>
/// Provides custom ImGui widgets.
/// </summary>
public static partial class ImGuiWidgets
{
	/// <summary>
	/// A stack of buttons that slides up from the bottom of the window with a separate Cancel button
	/// beneath it, as a phone asks which of a few things to do.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The sheet is drawn through an <see cref="OverlayHost"/>, on <see cref="OverlayLayer.ActionSheet"/>,
	/// so call the host's <see cref="OverlayHost.Render"/> once a frame. While it is open everything
	/// behind it is dimmed and cannot be clicked. Choosing a button answers with its index; Cancel, a tap
	/// on the dimming or Escape answers with -1. The answer is given once, on the frame it is made, and
	/// the sheet then slides away.
	/// </para>
	/// <para>
	/// The panel is probe-marked under the sheet's id, each button under <c>id/label</c> and the cancel
	/// button under <c>id/cancel</c>.
	/// </para>
	/// </remarks>
	/// <param name="host">The overlay host that draws the sheet.</param>
	/// <param name="id">A unique id for the sheet.</param>
	public sealed class ActionSheet(OverlayHost host, string id)
	{
		private readonly ActionSheetState state = new();
		private readonly string key = $"{nameof(ActionSheet)}/{id}";
		private IReadOnlyList<ActionSheetButton> buttons = [];
		private Action<int>? onAnswer;
		private string? title;
		private bool opening;

		/// <summary>Gets or sets the cancel button's label.</summary>
		public string CancelLabel { get; set; } = "Cancel";

		/// <summary>Gets a value indicating whether the sheet is open and waiting for an answer.</summary>
		public bool IsOpen => state.IsOpen;

		/// <summary>Opens the sheet.</summary>
		/// <param name="buttons">The buttons, top to bottom, not counting Cancel.</param>
		/// <param name="onAnswer">Called once with the chosen button's index, or -1 when the sheet is cancelled.</param>
		/// <param name="title">A line of text above the buttons, or null for none.</param>
		public void Open(IReadOnlyList<ActionSheetButton> buttons, Action<int> onAnswer, string? title = null)
		{
			Ensure.NotNull(buttons);
			Ensure.NotNull(onAnswer);

			// A sheet reopened before it answered answers the first question with a cancel.
			if (state.IsOpen)
			{
				Answer(ActionSheetState.Cancelled);
			}

			this.buttons = buttons;
			this.onAnswer = onAnswer;
			this.title = title;
			opening = true;
			state.Open(buttons.Count);
			host.Show(key, Draw, OverlayLayer.ActionSheet);
		}

		/// <summary>Cancels the sheet, as its Cancel button does.</summary>
		public void Cancel() => Answer(ActionSheetState.Cancelled);

		private void Answer(int index)
		{
			if (state.TryAnswer(index))
			{
				Action<int>? callback = onAnswer;
				onAnswer = null;
				callback?.Invoke(index);
			}
		}

		private void Draw()
		{
			if (state.Update(ImGui.GetIO().DeltaTime) || !state.Transition.IsVisible)
			{
				host.Dismiss(key);
				return;
			}

			float shown = state.Transition.Eased;
			OverlaySurface.BeginFullscreen($"##{key}", opening);
			opening = false;
			OverlaySurface.DrawScrim(shown);

			ImGuiViewportPtr viewport = ImGui.GetMainViewport();
			float margin = InterfaceScale.Pixels(8f);
			float rowHeight = InterfaceScale.Pixels(44f);
			float width = MathF.Min(viewport.Size.X - (margin * 2f), InterfaceScale.Pixels(420f));
			float titleHeight = title is null ? 0f : rowHeight;
			float stackHeight = titleHeight + (buttons.Count * rowHeight);
			float panelHeight = stackHeight + margin + rowHeight;
			float left = viewport.Pos.X + ((viewport.Size.X - width) * 0.5f);
			float top = viewport.Pos.Y + viewport.Size.Y - margin - panelHeight + ((1f - shown) * (panelHeight + margin));
			Vector2 panelMin = new(left, top);
			Vector2 panelMax = new(left + width, top + panelHeight);
			ImGuiProbes.MarkRegion(id, panelMin, panelMax);

			if (state.IsOpen && (OverlaySurface.ScrimPressed(panelMin, panelMax) || ImGui.IsKeyPressed(ImGuiKey.Escape)))
			{
				Cancel();
			}

			ImDrawListPtr drawList = ImGui.GetWindowDrawList();
			float rounding = InterfaceScale.Pixels(12f);
			uint surface = OverlaySurface.Opaque(ImGuiCol.PopupBg);
			drawList.AddRectFilled(panelMin, new Vector2(panelMax.X, top + stackHeight), surface, rounding);
			drawList.AddRectFilled(new Vector2(left, top + stackHeight + margin), panelMax, surface, rounding);

			if (title is not null)
			{
				Vector2 titleSize = ImGui.CalcTextSize(title);
				drawList.AddText(
					new Vector2(left + ((width - titleSize.X) * 0.5f), top + ((rowHeight - titleSize.Y) * 0.5f)),
					ImGui.GetColorU32(ImGuiCol.TextDisabled),
					title);
			}

			ImGui.PushStyleColor(ImGuiCol.Button, 0u);
			ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, rounding);
			for (int i = 0; i < buttons.Count; i++)
			{
				float y = top + titleHeight + (i * rowHeight);
				if (i > 0 || title is not null)
				{
					drawList.AddLine(new Vector2(left, y), new Vector2(left + width, y), ImGui.GetColorU32(ImGuiCol.Separator));
				}

				ActionSheetButton button = buttons[i];
				if (DrawRow(new Vector2(left, y), new Vector2(width, rowHeight), $"{button.Label}##{id}{i}", button.IsDestructive))
				{
					Answer(i);
				}

				ImGuiProbes.MarkItem(id, button.Label);
			}

			if (DrawRow(new Vector2(left, top + stackHeight + margin), new Vector2(width, rowHeight), $"{CancelLabel}##{id}cancel", false))
			{
				Cancel();
			}

			ImGuiProbes.MarkItem(id, "cancel");
			ImGui.PopStyleVar();
			ImGui.PopStyleColor();
			ImGui.End();
		}

		private static bool DrawRow(Vector2 position, Vector2 size, string label, bool destructive)
		{
			ImGui.SetCursorScreenPos(position);
			if (destructive)
			{
				ImGui.PushStyleColor(ImGuiCol.Text, Palette.Semantic.Error.Value);
			}

			bool clicked = ImGui.Button(label, size);
			if (destructive)
			{
				ImGui.PopStyleColor();
			}

			return clicked;
		}
	}
}
