// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Probes;
using ktsu.ImGui.Widgets.Overlays;

/// <summary>How long a toast stays and what its button does.</summary>
public sealed record ToastOptions
{
	/// <summary>Gets how long the toast stays once it is in, in seconds. Zero or less stays until dismissed.</summary>
	public float Duration { get; init; } = 4f;

	/// <summary>Gets the label of the toast's button, which makes it a snackbar. Null draws no button.</summary>
	public string? ActionLabel { get; init; }

	/// <summary>Gets what the toast's button does. The toast goes away after it runs.</summary>
	public Action? OnAction { get; init; }
}

/// <summary>
/// Provides custom ImGui widgets.
/// </summary>
public static partial class ImGuiWidgets
{
	/// <summary>
	/// Short messages that slide up from the bottom of the window, stay a few seconds and go away by
	/// themselves, one at a time and in the order they were asked for. A message with an action label
	/// is a snackbar: it carries a button, which runs its action and dismisses it.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Toasts are drawn through an <see cref="OverlayHost"/>, on <see cref="OverlayLayer.Toast"/>, so
	/// call the host's <see cref="OverlayHost.Render"/> once a frame. The queue registers itself while
	/// it has something to show and removes itself when it runs dry, so an idle queue costs nothing.
	/// </para>
	/// <para>
	/// A toast never takes focus or blocks anything beneath it except where it is drawn. Resting the
	/// pointer on it pauses its countdown. The toast is probe-marked under the queue's id, its message
	/// under <c>id/message</c> and its button under <c>id/action</c>.
	/// </para>
	/// </remarks>
	/// <param name="host">The overlay host that draws the toasts.</param>
	/// <param name="id">A unique id for the queue.</param>
	public sealed class Toaster(OverlayHost host, string id = "toasts")
	{
		private readonly ToastQueueState state = new();
		private readonly string key = $"{nameof(Toaster)}/{id}";
		private bool hovered;

		/// <summary>Gets how many toasts are on screen or waiting.</summary>
		public int Count => state.Count;

		/// <summary>Gets the message on screen, or null when none is.</summary>
		public string? CurrentMessage => state.Current?.Message;

		/// <summary>Adds a message to the queue. It shows once every message before it has gone.</summary>
		/// <param name="message">The text to show.</param>
		/// <param name="options">How long it stays and what its button does; null for a plain toast.</param>
		public void Show(string message, ToastOptions? options = null)
		{
			Ensure.NotNull(message);
			state.Enqueue(new ToastRequest(message, options ?? new ToastOptions()));
			host.Show(key, Draw, OverlayLayer.Toast);
		}

		/// <summary>Sends the toast on screen away early. The next one in the queue follows it.</summary>
		public void DismissCurrent() => state.Dismiss();

		/// <summary>Drops every waiting toast and sends the one on screen away.</summary>
		public void Clear() => state.Clear();

		private void Draw()
		{
			state.Update(ImGui.GetIO().DeltaTime, hovered);
			ToastRequest? toast = state.Current;
			if (toast is null)
			{
				hovered = false;
				if (state.Count == 0)
				{
					host.Dismiss(key);
				}

				return;
			}

			float shown = Animation.Easing.OutCubic(state.Visibility);
			ToastLayout layout = ToastLayout.Measure(toast);
			ImGuiViewportPtr viewport = ImGui.GetMainViewport();
			float margin = InterfaceScale.Pixels(24f);
			float bottom = viewport.Pos.Y + viewport.Size.Y - margin;
			Vector2 position = new(
				viewport.Pos.X + ((viewport.Size.X - layout.Size.X) * 0.5f),
				bottom - layout.Size.Y + ((1f - shown) * (layout.Size.Y + margin)));

			ImGui.SetNextWindowPos(position);
			ImGui.SetNextWindowSize(layout.Size);
			ImGui.PushStyleVar(ImGuiStyleVar.Alpha, shown);
			ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, layout.Padding);
			ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, InterfaceScale.Pixels(8f));
			// Opaque, since the snackbar sits over the page; the style alpha pushed above still fades it.
			Vector4 surface = ImGui.GetStyle().Colors[(int)ImGuiCol.PopupBg];
			surface.W = 1f;
			ImGui.PushStyleColor(ImGuiCol.WindowBg, surface);
			ImGui.Begin($"##{key}", ToastLayout.WindowFlags);
			OverlaySurface.BringToFront();
			ImGuiProbes.MarkRegion(id, position, position + layout.Size);
			hovered = ImGui.IsWindowHovered();

			ImGui.PushTextWrapPos(ImGui.GetCursorPosX() + layout.WrapWidth);
			ImGui.SetCursorPosY(ImGui.GetCursorPosY() + ((layout.ContentHeight - layout.TextHeight) * 0.5f));
			ImGui.TextUnformatted(toast.Message);
			ImGuiProbes.MarkItem(id, "message");
			ImGui.PopTextWrapPos();

			if (toast.Options.ActionLabel is { } label)
			{
				ImGui.SetCursorPos(new Vector2(
					layout.Size.X - layout.Padding.X - layout.ButtonWidth,
					layout.Padding.Y + ((layout.ContentHeight - ImGui.GetFrameHeight()) * 0.5f)));
				if (ImGui.Button($"{label}##{id}action") && state.Phase != ToastPhase.Leaving)
				{
					toast.Options.OnAction?.Invoke();
					state.Dismiss();
				}

				ImGuiProbes.MarkItem(id, "action");
			}

			ImGui.End();
			ImGui.PopStyleColor();
			ImGui.PopStyleVar(3);
		}

		private readonly record struct ToastLayout(Vector2 Size, Vector2 Padding, float WrapWidth, float TextHeight, float ContentHeight, float ButtonWidth)
		{
			internal const ImGuiWindowFlags WindowFlags = ImGuiWindowFlags.NoDecoration
				| ImGuiWindowFlags.NoSavedSettings
				| ImGuiWindowFlags.NoMove
				| ImGuiWindowFlags.NoResize
				| ImGuiWindowFlags.NoFocusOnAppearing
				| ImGuiWindowFlags.NoNav
				| ImGuiWindowFlags.NoScrollbar
				| ImGuiWindowFlags.NoScrollWithMouse;

			internal static ToastLayout Measure(ToastRequest toast)
			{
				ImGuiViewportPtr viewport = ImGui.GetMainViewport();
				ImGuiStylePtr style = ImGui.GetStyle();
				Vector2 padding = new(InterfaceScale.Pixels(16f), InterfaceScale.Pixels(12f));
				float maxWidth = MathF.Min(viewport.Size.X - InterfaceScale.Pixels(32f), InterfaceScale.Pixels(480f));

				float buttonWidth = toast.Options.ActionLabel is { } label
					? ImGui.CalcTextSize(label).X + (style.FramePadding.X * 2f)
					: 0f;
				float gap = buttonWidth > 0f ? InterfaceScale.Pixels(16f) : 0f;
				float textRoom = MathF.Max(InterfaceScale.Pixels(40f), maxWidth - (padding.X * 2f) - buttonWidth - gap);
				float textWidth = MathF.Min(ImGui.CalcTextSize(toast.Message).X, textRoom);
				float textHeight = ImGui.CalcTextSize(toast.Message, false, textWidth + 1f).Y;
				float contentHeight = MathF.Max(textHeight, buttonWidth > 0f ? ImGui.GetFrameHeight() : 0f);

				Vector2 size = new(textWidth + gap + buttonWidth + (padding.X * 2f), contentHeight + (padding.Y * 2f));
				return new ToastLayout(size, padding, textWidth + 1f, textHeight, contentHeight, buttonWidth);
			}
		}
	}
}
