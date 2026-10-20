// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Probes;
using ktsu.ImGui.Widgets.Overlays;

/// <summary>One step of a <see cref="ImGuiWidgets.Tutorial"/>: the widget to light up and what to say about it.</summary>
/// <param name="Target">The name the widget was registered under with <see cref="ImGuiWidgets.Tutorial.Target"/>.</param>
/// <param name="Title">The callout's heading.</param>
/// <param name="Text">The callout's explanation.</param>
public sealed record CoachMarkStep(string Target, string Title, string Text);

/// <summary>
/// Provides custom ImGui widgets.
/// </summary>
public static partial class ImGuiWidgets
{
	/// <summary>
	/// A walkthrough of coach marks: each step dims the window except one widget, and explains it in a
	/// callout beside it with Next and Skip. A single coach mark is a tutorial of one step.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Register each widget a step points at by calling <see cref="Target"/> straight after drawing it,
	/// every frame; the tutorial lights up whatever ImGui drew last before the call. The tutorial draws
	/// through an <see cref="OverlayHost"/>, on <see cref="OverlayLayer.Tooltip"/>, so call the host's
	/// <see cref="OverlayHost.Render"/> once a frame after the window. While a step shows, the window
	/// beneath cannot be clicked. A step whose target was not drawn shows its callout in the middle of
	/// the window, with nothing lit.
	/// </para>
	/// <para>
	/// The callout is probe-marked as <c>{id}/callout</c>, its buttons as <c>{id}/next</c> and
	/// <c>{id}/skip</c>, and the lit area as <c>{id}/spotlight</c>.
	/// </para>
	/// <code>
	/// ImGui.Button("Save");
	/// tour.Target("save");
	/// ...
	/// host.Render();
	/// </code>
	/// </remarks>
	/// <param name="host">The overlay host that draws the tutorial.</param>
	/// <param name="id">A unique id for the tutorial.</param>
	/// <param name="steps">The steps, in order.</param>
	public sealed class Tutorial(OverlayHost host, string id, IReadOnlyList<CoachMarkStep> steps)
	{
		private readonly TutorialState state = new();
		private readonly string key = $"{nameof(Tutorial)}/{id}";
		private readonly Dictionary<string, (Vector2 Min, Vector2 Max, int Frame)> targets = new(StringComparer.Ordinal);
		private bool opening;

		/// <summary>Gets or sets the label of the button that moves on a step.</summary>
		public string NextLabel { get; set; } = "Next";

		/// <summary>Gets or sets the label of that button on the last step.</summary>
		public string DoneLabel { get; set; } = "Done";

		/// <summary>Gets or sets the label of the button that ends the tutorial early.</summary>
		public string SkipLabel { get; set; } = "Skip";

		/// <summary>Gets or sets what to call when the tutorial ends: with true when the last step was finished, false when it was skipped.</summary>
		public Action<bool>? Finished { get; set; }

		/// <summary>Gets a value indicating whether the tutorial is running.</summary>
		public bool IsActive => state.IsActive;

		/// <summary>Gets the step showing, or -1 before the tutorial has started.</summary>
		public int StepIndex => state.Index;

		/// <summary>Registers the widget ImGui drew last as a target the steps can point at. Call straight after drawing it, every frame.</summary>
		/// <param name="name">The name steps refer to it by.</param>
		public void Target(string name)
		{
			Ensure.NotNull(name);
			targets[name] = (ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), ImGui.GetFrameCount());
		}

		/// <summary>Starts the tutorial.</summary>
		/// <param name="step">The step to start at.</param>
		public void Start(int step = 0)
		{
			Ensure.NotNull(steps);
			if (state.Start(steps.Count, step))
			{
				opening = true;
				host.Show(key, Draw, OverlayLayer.Tooltip);
			}
		}

		/// <summary>Moves on a step, as the Next button does, finishing the tutorial after the last.</summary>
		public void Next()
		{
			if (state.Next())
			{
				Finished?.Invoke(true);
			}
		}

		/// <summary>Goes back a step, if there is one.</summary>
		public void Back() => state.Back();

		/// <summary>Ends the tutorial early, as the Skip button does.</summary>
		public void Skip()
		{
			if (state.Skip())
			{
				Finished?.Invoke(false);
			}
		}

		private (Vector2 Min, Vector2 Max)? CurrentTarget()
		{
			if (state.Index < 0 || state.Index >= steps.Count)
			{
				return null;
			}

			// Registered this frame or the last, since the host may render before or after the widgets.
			return targets.TryGetValue(steps[state.Index].Target, out (Vector2 Min, Vector2 Max, int Frame) target)
				&& ImGui.GetFrameCount() - target.Frame <= 1
				? (target.Min, target.Max)
				: null;
		}

		private void Draw()
		{
			float deltaTime = ImGui.GetIO().DeltaTime;
			if (state.Update(deltaTime) || !state.Transition.IsVisible)
			{
				host.Dismiss(key);
				return;
			}

			state.TrackSpotlight(CurrentTarget(), deltaTime);
			float shown = state.Transition.Eased;
			OverlaySurface.BeginFullscreen($"##{key}", opening);
			opening = false;

			ImGuiViewportPtr viewport = ImGui.GetMainViewport();
			Vector2 screenMin = viewport.Pos;
			Vector2 screenMax = viewport.Pos + viewport.Size;
			float pad = InterfaceScale.Pixels(6f);
			Vector2? holeMin = state.HasSpotlight ? state.SpotlightMin - new Vector2(pad) : null;
			Vector2? holeMax = state.HasSpotlight ? state.SpotlightMax + new Vector2(pad) : null;
			DrawDimming(screenMin, screenMax, holeMin, holeMax, shown);

			if (holeMin is { } lit && holeMax is { } litMax)
			{
				ImGuiProbes.MarkRegion($"{id}/spotlight", lit, litMax);
			}

			ImGui.PushStyleVar(ImGuiStyleVar.Alpha, ImGui.GetStyle().Alpha * shown);
			DrawCallout(screenMin, screenMax, holeMin, holeMax);
			ImGui.PopStyleVar();

			if (state.IsActive && ImGui.IsKeyPressed(ImGuiKey.Escape))
			{
				Skip();
			}

			ImGui.End();
		}

		private static void DrawDimming(Vector2 screenMin, Vector2 screenMax, Vector2? holeMin, Vector2? holeMax, float shown)
		{
			ImDrawListPtr drawList = ImGui.GetWindowDrawList();
			uint dim = OverlaySurface.Color(ImGuiCol.ModalWindowDimBg, shown);
			if (holeMin is not { } min || holeMax is not { } max)
			{
				drawList.AddRectFilled(screenMin, screenMax, dim);
				return;
			}

			// Four bands around the lit rectangle, so the widget shows through untouched.
			drawList.AddRectFilled(screenMin, new Vector2(screenMax.X, min.Y), dim);
			drawList.AddRectFilled(new Vector2(screenMin.X, max.Y), screenMax, dim);
			drawList.AddRectFilled(new Vector2(screenMin.X, min.Y), new Vector2(min.X, max.Y), dim);
			drawList.AddRectFilled(new Vector2(max.X, min.Y), new Vector2(screenMax.X, max.Y), dim);
			drawList.AddRect(min, max, ImGui.GetColorU32(ImGuiCol.CheckMark, shown), InterfaceScale.Pixels(6f), ImDrawFlags.None, InterfaceScale.Pixels(2f));
		}

		private void DrawCallout(Vector2 screenMin, Vector2 screenMax, Vector2? holeMin, Vector2? holeMax)
		{
			CoachMarkStep step = steps[state.Index];
			ImGuiStylePtr style = ImGui.GetStyle();
			float margin = InterfaceScale.Pixels(12f);
			Vector2 padding = new(InterfaceScale.Pixels(14f), InterfaceScale.Pixels(12f));
			float width = MathF.Min(InterfaceScale.Pixels(320f), screenMax.X - screenMin.X - (margin * 2f));
			float wrap = width - (padding.X * 2f);

			string counter = string.Format(CultureInfo.InvariantCulture, "{0} of {1}", state.Index + 1, steps.Count);
			string nextLabel = state.IsLastStep ? DoneLabel : NextLabel;
			float titleHeight = ImGui.CalcTextSize(step.Title, false, wrap).Y;
			float textHeight = ImGui.CalcTextSize(step.Text, false, wrap).Y;
			float rowHeight = ImGui.GetFrameHeight();
			float height = (padding.Y * 2f) + titleHeight + style.ItemSpacing.Y + textHeight + (style.ItemSpacing.Y * 2f) + rowHeight;

			Vector2 min = CalloutPosition(new Vector2(width, height), margin, screenMin, screenMax, holeMin, holeMax);
			Vector2 max = min + new Vector2(width, height);
			ImDrawListPtr drawList = ImGui.GetWindowDrawList();
			float rounding = InterfaceScale.Pixels(10f);
			drawList.AddRectFilled(min, max, OverlaySurface.Opaque(ImGuiCol.PopupBg, style.Alpha), rounding);
			drawList.AddRect(min, max, ImGui.GetColorU32(ImGuiCol.Border), rounding);
			ImGuiProbes.MarkRegion($"{id}/callout", min, max);

			ImGui.SetCursorScreenPos(min + padding);
			ImGui.BeginGroup();
			ImGui.PushTextWrapPos(min.X + padding.X + wrap - ImGui.GetWindowPos().X);
			ImGui.TextUnformatted(step.Title);
			ImGui.PushStyleColor(ImGuiCol.Text, style.Colors[(int)ImGuiCol.TextDisabled]);
			ImGui.TextUnformatted(step.Text);
			ImGui.PopStyleColor();
			ImGui.PopTextWrapPos();
			ImGui.Dummy(new Vector2(0f, style.ItemSpacing.Y));

			float rowTop = max.Y - padding.Y - rowHeight;
			ImGui.SetCursorScreenPos(new Vector2(min.X + padding.X, rowTop + style.FramePadding.Y));
			ImGui.TextDisabled(counter);

			float nextWidth = ImGui.CalcTextSize(nextLabel).X + (style.FramePadding.X * 2f);
			float skipWidth = ImGui.CalcTextSize(SkipLabel).X + (style.FramePadding.X * 2f);
			float right = max.X - padding.X;
			if (!state.IsLastStep)
			{
				ImGui.SetCursorScreenPos(new Vector2(right - nextWidth - style.ItemSpacing.X - skipWidth, rowTop));
				if (ImGui.Button(SkipLabel) && state.IsActive)
				{
					Skip();
				}

				ImGuiProbes.MarkItem(id, "skip");
			}

			ImGui.SetCursorScreenPos(new Vector2(right - nextWidth, rowTop));
			if (ImGui.Button(nextLabel) && state.IsActive)
			{
				Next();
			}

			ImGuiProbes.MarkItem(id, "next");
			ImGui.EndGroup();
		}

		private static Vector2 CalloutPosition(Vector2 size, float margin, Vector2 screenMin, Vector2 screenMax, Vector2? holeMin, Vector2? holeMax)
		{
			if (holeMin is not { } min || holeMax is not { } max)
			{
				return screenMin + ((screenMax - screenMin - size) * 0.5f);
			}

			// Below the target when it fits, above when that fits better, centred on the target across.
			float below = max.Y + margin;
			float above = min.Y - margin - size.Y;
			float top = below + size.Y <= screenMax.Y - margin || above < screenMin.Y + margin ? below : above;
			float left = ((min.X + max.X) * 0.5f) - (size.X * 0.5f);
			left = Math.Clamp(left, screenMin.X + margin, Math.Max(screenMin.X + margin, screenMax.X - margin - size.X));
			top = Math.Clamp(top, screenMin.Y + margin, Math.Max(screenMin.Y + margin, screenMax.Y - margin - size.Y));
			return new Vector2(left, top);
		}
	}
}
