// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Phosphor;

using System.Collections.Generic;
using System.Numerics;

using Hexa.NET.ImGui;
using ktsu.ScopedAction;

/// <summary>
/// Applies the phosphor look to Dear ImGui's own widgets and remembers which tint the widgets in
/// <see cref="ImGuiPhosphor"/> draw in.
/// </summary>
/// <remarks>
/// A phosphor screen has one colour at several brightnesses, so the whole style is drawn from one
/// <see cref="PhosphorPalette"/>: text in the bright level, frames and borders in the normal and dim
/// levels, and the screen behind everything. Corners are square and every frame has a one pixel border,
/// which is what makes stock buttons and inputs read as vector outlines rather than filled panels.
/// </remarks>
public static class PhosphorStyle
{
	private static readonly Stack<PhosphorTint> tints = new();

	/// <summary>Gets the tint widgets draw in: the innermost <see cref="Scope"/>, else the last <see cref="Apply"/>.</summary>
	public static PhosphorTint Tint => tints.Count > 0 ? tints.Peek() : AppliedTint;

	/// <summary>Gets the palette for <see cref="Tint"/>.</summary>
	public static PhosphorPalette Palette => PhosphorPalette.For(Tint);

	private static PhosphorTint AppliedTint { get; set; } = PhosphorTint.Green;

	/// <summary>
	/// Writes the phosphor colours and geometry into the current ImGui style, permanently. Call once after
	/// the context exists, or whenever the tint changes.
	/// </summary>
	/// <param name="tint">Which phosphor to show.</param>
	public static void Apply(PhosphorTint tint = PhosphorTint.Green)
	{
		AppliedTint = tint;
		ImGuiStylePtr style = ImGui.GetStyle();
		foreach ((ImGuiCol col, Vector4 color) in Colors(PhosphorPalette.For(tint)))
		{
			style.Colors[(int)col] = color;
		}

		foreach ((ImGuiStyleVar var, float value) in FloatVars)
		{
			SetFloat(style, var, value);
		}
	}

	/// <summary>
	/// Pushes the phosphor colours and geometry for the lifetime of the returned scope, so a single window
	/// can show a phosphor screen inside an otherwise different interface.
	/// </summary>
	/// <param name="tint">Which phosphor to show.</param>
	/// <returns>A scope that pops everything it pushed when disposed.</returns>
	public static ScopedAction Scope(PhosphorTint tint = PhosphorTint.Green) => new PhosphorScope(tint);

	internal static IReadOnlyList<(ImGuiCol Col, Vector4 Color)> Colors(PhosphorPalette p)
	{
		Vector4 clear = Vector4.Zero;
		return
		[
			(ImGuiCol.Text, p.Bright),
			(ImGuiCol.TextDisabled, p.Dim),
			(ImGuiCol.TextLink, p.Hot),
			(ImGuiCol.TextSelectedBg, PhosphorPalette.WithAlpha(p.Normal, 0.45f)),
			(ImGuiCol.InputTextCursor, p.Hot),
			(ImGuiCol.WindowBg, p.Screen),
			(ImGuiCol.ChildBg, clear),
			(ImGuiCol.PopupBg, p.Screen),
			(ImGuiCol.MenuBarBg, p.Screen),
			(ImGuiCol.Border, p.Dim),
			(ImGuiCol.BorderShadow, clear),
			(ImGuiCol.FrameBg, p.Faint),
			(ImGuiCol.FrameBgHovered, PhosphorPalette.WithAlpha(p.Dim, 0.8f)),
			(ImGuiCol.FrameBgActive, p.Dim),
			(ImGuiCol.TitleBg, p.Screen),
			(ImGuiCol.TitleBgActive, p.Faint),
			(ImGuiCol.TitleBgCollapsed, p.Screen),
			(ImGuiCol.ScrollbarBg, p.Screen),
			(ImGuiCol.ScrollbarGrab, p.Dim),
			(ImGuiCol.ScrollbarGrabHovered, p.Normal),
			(ImGuiCol.ScrollbarGrabActive, p.Bright),
			(ImGuiCol.CheckMark, p.Hot),
			(ImGuiCol.SliderGrab, p.Normal),
			(ImGuiCol.SliderGrabActive, p.Hot),
			(ImGuiCol.Button, p.Faint),
			(ImGuiCol.ButtonHovered, p.Dim),
			(ImGuiCol.ButtonActive, p.Normal),
			(ImGuiCol.Header, p.Faint),
			(ImGuiCol.HeaderHovered, p.Dim),
			(ImGuiCol.HeaderActive, p.Normal),
			(ImGuiCol.Separator, p.Dim),
			(ImGuiCol.SeparatorHovered, p.Normal),
			(ImGuiCol.SeparatorActive, p.Bright),
			(ImGuiCol.ResizeGrip, p.Faint),
			(ImGuiCol.ResizeGripHovered, p.Dim),
			(ImGuiCol.ResizeGripActive, p.Normal),
			(ImGuiCol.Tab, p.Screen),
			(ImGuiCol.TabHovered, p.Dim),
			(ImGuiCol.TabSelected, p.Faint),
			(ImGuiCol.TabSelectedOverline, p.Bright),
			(ImGuiCol.TabDimmed, p.Screen),
			(ImGuiCol.TabDimmedSelected, p.Faint),
			(ImGuiCol.TabDimmedSelectedOverline, p.Dim),
			(ImGuiCol.PlotLines, p.Bright),
			(ImGuiCol.PlotLinesHovered, p.Hot),
			(ImGuiCol.PlotHistogram, p.Normal),
			(ImGuiCol.PlotHistogramHovered, p.Hot),
			(ImGuiCol.TableHeaderBg, p.Faint),
			(ImGuiCol.TableBorderStrong, p.Dim),
			(ImGuiCol.TableBorderLight, p.Faint),
			(ImGuiCol.TableRowBg, clear),
			(ImGuiCol.TableRowBgAlt, PhosphorPalette.WithAlpha(p.Faint, 0.5f)),
			(ImGuiCol.TreeLines, p.Dim),
			(ImGuiCol.DragDropTarget, p.Hot),
			(ImGuiCol.NavCursor, p.Hot),
			(ImGuiCol.NavWindowingHighlight, p.Bright),
			(ImGuiCol.NavWindowingDimBg, PhosphorPalette.WithAlpha(p.Screen, 0.6f)),
			(ImGuiCol.ModalWindowDimBg, PhosphorPalette.WithAlpha(p.Screen, 0.6f)),
		];
	}

	internal static IReadOnlyList<(ImGuiStyleVar Var, float Value)> FloatVars { get; } =
	[
		(ImGuiStyleVar.WindowRounding, 0f),
		(ImGuiStyleVar.ChildRounding, 0f),
		(ImGuiStyleVar.FrameRounding, 0f),
		(ImGuiStyleVar.PopupRounding, 0f),
		(ImGuiStyleVar.ScrollbarRounding, 0f),
		(ImGuiStyleVar.GrabRounding, 0f),
		(ImGuiStyleVar.TabRounding, 0f),
		(ImGuiStyleVar.WindowBorderSize, 1f),
		(ImGuiStyleVar.ChildBorderSize, 1f),
		(ImGuiStyleVar.PopupBorderSize, 1f),
		(ImGuiStyleVar.FrameBorderSize, 1f),
		(ImGuiStyleVar.TabBorderSize, 1f),
	];

	private static void SetFloat(ImGuiStylePtr style, ImGuiStyleVar var, float value)
	{
		switch (var)
		{
			case ImGuiStyleVar.WindowRounding:
				style.WindowRounding = value;
				break;
			case ImGuiStyleVar.ChildRounding:
				style.ChildRounding = value;
				break;
			case ImGuiStyleVar.FrameRounding:
				style.FrameRounding = value;
				break;
			case ImGuiStyleVar.PopupRounding:
				style.PopupRounding = value;
				break;
			case ImGuiStyleVar.ScrollbarRounding:
				style.ScrollbarRounding = value;
				break;
			case ImGuiStyleVar.GrabRounding:
				style.GrabRounding = value;
				break;
			case ImGuiStyleVar.TabRounding:
				style.TabRounding = value;
				break;
			case ImGuiStyleVar.WindowBorderSize:
				style.WindowBorderSize = value;
				break;
			case ImGuiStyleVar.ChildBorderSize:
				style.ChildBorderSize = value;
				break;
			case ImGuiStyleVar.PopupBorderSize:
				style.PopupBorderSize = value;
				break;
			case ImGuiStyleVar.FrameBorderSize:
				style.FrameBorderSize = value;
				break;
			case ImGuiStyleVar.TabBorderSize:
				style.TabBorderSize = value;
				break;
			default:
				break;
		}
	}

	private sealed class PhosphorScope : ScopedAction
	{
		public PhosphorScope(PhosphorTint tint)
		{
			IReadOnlyList<(ImGuiCol Col, Vector4 Color)> colors = Colors(PhosphorPalette.For(tint));
			foreach ((ImGuiCol col, Vector4 color) in colors)
			{
				ImGui.PushStyleColor(col, color);
			}

			foreach ((ImGuiStyleVar var, float value) in FloatVars)
			{
				ImGui.PushStyleVar(var, value);
			}

			int colorCount = colors.Count;
			int varCount = FloatVars.Count;
			tints.Push(tint);
			OnClose = () =>
			{
				ImGui.PopStyleVar(varCount);
				ImGui.PopStyleColor(colorCount);
				tints.Pop();
			};
		}
	}
}
