// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Sortie;

using System;
using System.Collections.Generic;
using System.Numerics;

using Hexa.NET.ImGui;
using ktsu.ScopedAction;

/// <summary>
/// Applies the command-display look to Dear ImGui's own widgets and remembers which condition the widgets in
/// <see cref="ImGuiSortie"/> draw in.
/// </summary>
/// <remarks>
/// A command display has one leading warning colour at several strengths, so the whole style is drawn from one
/// <see cref="SortiePalette"/>: text in its own warm tint, frames and borders in the lead colour at its dim and faint
/// strengths, and the black field behind everything. Corners are square and every frame has a one pixel border,
/// which is what makes stock buttons and inputs read as hard-edged warning panels.
/// </remarks>
public static class SortieStyle
{
	private static readonly Stack<SortieCondition> conditions = new();

	/// <summary>Gets the condition widgets draw in: the innermost <see cref="Scope"/>, else the last <see cref="Apply"/>.</summary>
	public static SortieCondition Condition => conditions.Count > 0 ? conditions.Peek() : AppliedCondition;

	/// <summary>Gets the palette for <see cref="Condition"/>.</summary>
	public static SortiePalette Palette => SortiePalette.For(Condition);

	private static SortieCondition AppliedCondition { get; set; } = SortieCondition.Standby;

	/// <summary>
	/// Writes the command-display colours and geometry into the current ImGui style, permanently. Call once after
	/// the context exists, or whenever the condition changes.
	/// </summary>
	/// <param name="condition">Which condition to show.</param>
	public static void Apply(SortieCondition condition = SortieCondition.Standby)
	{
		AppliedCondition = condition;
		ImGuiStylePtr style = ImGui.GetStyle();
		foreach ((ImGuiCol col, Vector4 color) in Colors(SortiePalette.For(condition)))
		{
			style.Colors[(int)col] = color;
		}

		foreach ((ImGuiStyleVar var, float value) in FloatVars)
		{
			SetFloat(style, var, value);
		}
	}

	/// <summary>
	/// Pushes the command-display colours and geometry for the lifetime of the returned scope, so a single window
	/// can show a command display inside an otherwise different interface.
	/// </summary>
	/// <param name="condition">Which condition to show.</param>
	/// <returns>A scope that pops everything it pushed when disposed.</returns>
	public static IDisposable Scope(SortieCondition condition = SortieCondition.Standby) => new SortieScope(condition);

	internal static IReadOnlyList<(ImGuiCol Col, Vector4 Color)> Colors(SortiePalette p)
	{
		Vector4 clear = Vector4.Zero;
		return
		[
			(ImGuiCol.Text, p.Text),
			(ImGuiCol.TextDisabled, p.LeadDim),
			(ImGuiCol.TextLink, p.LeadBright),
			(ImGuiCol.TextSelectedBg, SortiePalette.WithAlpha(p.Lead, 0.45f)),
			(ImGuiCol.InputTextCursor, p.LeadBright),
			(ImGuiCol.WindowBg, p.Field),
			(ImGuiCol.ChildBg, clear),
			(ImGuiCol.PopupBg, p.Field),
			(ImGuiCol.MenuBarBg, p.Field),
			(ImGuiCol.Border, p.LeadDim),
			(ImGuiCol.BorderShadow, clear),
			(ImGuiCol.FrameBg, p.LeadFaint),
			(ImGuiCol.FrameBgHovered, SortiePalette.WithAlpha(p.LeadDim, 0.8f)),
			(ImGuiCol.FrameBgActive, p.LeadDim),
			(ImGuiCol.TitleBg, p.Field),
			(ImGuiCol.TitleBgActive, p.LeadFaint),
			(ImGuiCol.TitleBgCollapsed, p.Field),
			(ImGuiCol.ScrollbarBg, p.Field),
			(ImGuiCol.ScrollbarGrab, p.LeadDim),
			(ImGuiCol.ScrollbarGrabHovered, p.Lead),
			(ImGuiCol.ScrollbarGrabActive, p.Text),
			(ImGuiCol.CheckMark, p.LeadBright),
			(ImGuiCol.SliderGrab, p.Lead),
			(ImGuiCol.SliderGrabActive, p.LeadBright),
			(ImGuiCol.Button, p.LeadFaint),
			(ImGuiCol.ButtonHovered, p.LeadDim),
			(ImGuiCol.ButtonActive, p.Lead),
			(ImGuiCol.Header, p.LeadFaint),
			(ImGuiCol.HeaderHovered, p.LeadDim),
			(ImGuiCol.HeaderActive, p.Lead),
			(ImGuiCol.Separator, p.LeadDim),
			(ImGuiCol.SeparatorHovered, p.Lead),
			(ImGuiCol.SeparatorActive, p.Text),
			(ImGuiCol.ResizeGrip, p.LeadFaint),
			(ImGuiCol.ResizeGripHovered, p.LeadDim),
			(ImGuiCol.ResizeGripActive, p.Lead),
			(ImGuiCol.Tab, p.Field),
			(ImGuiCol.TabHovered, p.LeadDim),
			(ImGuiCol.TabSelected, p.LeadFaint),
			(ImGuiCol.TabSelectedOverline, p.Text),
			(ImGuiCol.TabDimmed, p.Field),
			(ImGuiCol.TabDimmedSelected, p.LeadFaint),
			(ImGuiCol.TabDimmedSelectedOverline, p.LeadDim),
			(ImGuiCol.PlotLines, p.Text),
			(ImGuiCol.PlotLinesHovered, p.LeadBright),
			(ImGuiCol.PlotHistogram, p.Lead),
			(ImGuiCol.PlotHistogramHovered, p.LeadBright),
			(ImGuiCol.TableHeaderBg, p.LeadFaint),
			(ImGuiCol.TableBorderStrong, p.LeadDim),
			(ImGuiCol.TableBorderLight, p.LeadFaint),
			(ImGuiCol.TableRowBg, clear),
			(ImGuiCol.TableRowBgAlt, SortiePalette.WithAlpha(p.LeadFaint, 0.5f)),
			(ImGuiCol.TreeLines, p.LeadDim),
			(ImGuiCol.DragDropTarget, p.LeadBright),
			(ImGuiCol.NavCursor, p.LeadBright),
			(ImGuiCol.NavWindowingHighlight, p.Text),
			(ImGuiCol.NavWindowingDimBg, SortiePalette.WithAlpha(p.Field, 0.6f)),
			(ImGuiCol.ModalWindowDimBg, SortiePalette.WithAlpha(p.Field, 0.6f)),
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

	private sealed class SortieScope : ScopedAction
	{
		public SortieScope(SortieCondition condition)
		{
			IReadOnlyList<(ImGuiCol Col, Vector4 Color)> colors = Colors(SortiePalette.For(condition));
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
			conditions.Push(condition);
			OnClose = () =>
			{
				ImGui.PopStyleVar(varCount);
				ImGui.PopStyleColor(colorCount);
				conditions.Pop();
			};
		}
	}
}
