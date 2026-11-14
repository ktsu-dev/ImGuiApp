// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Monsoon;

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Numerics;

using Hexa.NET.ImGui;

/// <summary>
/// Applies the Monsoon look to Dear ImGui's own widgets: square corners, hairline neon borders,
/// cyan for structure, magenta for whatever is being touched, amber for what needs reading.
/// </summary>
/// <remarks>
/// The colours come from <see cref="MonsoonPalette"/> alone. <see cref="Colors"/> is the whole
/// mapping, one entry for every <see cref="ImGuiCol"/>, so nothing is left at Dear ImGui's default
/// blue when the theme is applied.
/// </remarks>
public static class MonsoonTheme
{
	private static readonly ReadOnlyDictionary<ImGuiCol, Vector4> ColorMap = new(BuildColors());

	/// <summary>
	/// Gets the colour the theme gives every <see cref="ImGuiCol"/> except
	/// <see cref="ImGuiCol.Count"/>.
	/// </summary>
	/// <returns>The mapping, read-only.</returns>
	public static IReadOnlyDictionary<ImGuiCol, Vector4> Colors() => ColorMap;

	/// <summary>
	/// Applies the theme's colours and spacing to the current context's style, permanently.
	/// </summary>
	/// <remarks>
	/// Call it once after the context exists, from <c>ImGuiAppConfig.OnStart</c> for an
	/// application. Use <see cref="Scoped"/> instead to theme one region of an otherwise
	/// differently styled interface.
	/// </remarks>
	public static void Apply()
	{
		ImGuiStylePtr style = ImGui.GetStyle();
		foreach (KeyValuePair<ImGuiCol, Vector4> entry in ColorMap)
		{
			style.Colors[(int)entry.Key] = entry.Value;
		}

		style.WindowRounding = 0f;
		style.ChildRounding = 0f;
		style.FrameRounding = 0f;
		style.PopupRounding = 0f;
		style.ScrollbarRounding = 0f;
		style.GrabRounding = 0f;
		style.TabRounding = 0f;
		style.WindowBorderSize = 1f;
		style.ChildBorderSize = 1f;
		style.PopupBorderSize = 1f;
		style.FrameBorderSize = 1f;
		style.ItemSpacing = new Vector2(6f, 4f);
		style.ScrollbarSize = 10f;
		style.GrabMinSize = 8f;
	}

	/// <summary>
	/// Pushes the theme's colours and spacing until the returned scope is disposed.
	/// </summary>
	/// <returns>The scope; dispose it to pop everything it pushed.</returns>
	public static IDisposable Scoped()
	{
		foreach (KeyValuePair<ImGuiCol, Vector4> entry in ColorMap)
		{
			ImGui.PushStyleColor(entry.Key, entry.Value);
		}

		ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0f);
		ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, 0f);
		ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 0f);
		ImGui.PushStyleVar(ImGuiStyleVar.PopupRounding, 0f);
		ImGui.PushStyleVar(ImGuiStyleVar.GrabRounding, 0f);
		ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 1f);
		ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(6f, 4f));
		return new StyleScope(ColorMap.Count, 7);
	}

	private static Dictionary<ImGuiCol, Vector4> BuildColors()
	{
		Vector4 night = MonsoonPalette.Background;
		Vector4 glass = MonsoonPalette.Panel;
		Vector4 surface = MonsoonPalette.Surface;
		Vector4 cyan = MonsoonPalette.Cyan;
		Vector4 magenta = MonsoonPalette.Magenta;
		Vector4 amber = MonsoonPalette.Amber;
		Vector4 violet = MonsoonPalette.Violet;
		Vector4 text = MonsoonPalette.Text;
		Vector4 dim = MonsoonPalette.TextDim;

		static Vector4 A(Vector4 c, float a) => MonsoonPalette.WithAlpha(c, a);
		static Vector4 M(Vector4 a, Vector4 b, float t) => MonsoonPalette.Mix(a, b, t);

		return new Dictionary<ImGuiCol, Vector4>
		{
			[ImGuiCol.Text] = text,
			[ImGuiCol.TextDisabled] = dim,
			[ImGuiCol.WindowBg] = night,
			[ImGuiCol.ChildBg] = A(glass, 0.72f),
			[ImGuiCol.PopupBg] = A(glass, 0.96f),
			[ImGuiCol.Border] = A(cyan, 0.35f),
			[ImGuiCol.BorderShadow] = A(night, 0f),
			[ImGuiCol.FrameBg] = A(surface, 0.9f),
			[ImGuiCol.FrameBgHovered] = M(surface, cyan, 0.18f),
			[ImGuiCol.FrameBgActive] = M(surface, magenta, 0.25f),
			[ImGuiCol.TitleBg] = night,
			[ImGuiCol.TitleBgActive] = M(night, magenta, 0.22f),
			[ImGuiCol.TitleBgCollapsed] = A(night, 0.8f),
			[ImGuiCol.MenuBarBg] = glass,
			[ImGuiCol.ScrollbarBg] = A(night, 0.6f),
			[ImGuiCol.ScrollbarGrab] = A(cyan, 0.3f),
			[ImGuiCol.ScrollbarGrabHovered] = A(cyan, 0.55f),
			[ImGuiCol.ScrollbarGrabActive] = magenta,
			[ImGuiCol.CheckMark] = magenta,
			[ImGuiCol.SliderGrab] = cyan,
			[ImGuiCol.SliderGrabActive] = magenta,
			[ImGuiCol.Button] = A(surface, 0.9f),
			[ImGuiCol.ButtonHovered] = M(surface, magenta, 0.35f),
			[ImGuiCol.ButtonActive] = M(surface, magenta, 0.6f),
			[ImGuiCol.Header] = M(surface, violet, 0.3f),
			[ImGuiCol.HeaderHovered] = M(surface, magenta, 0.35f),
			[ImGuiCol.HeaderActive] = M(surface, magenta, 0.55f),
			[ImGuiCol.Separator] = A(cyan, 0.3f),
			[ImGuiCol.SeparatorHovered] = A(magenta, 0.7f),
			[ImGuiCol.SeparatorActive] = magenta,
			[ImGuiCol.ResizeGrip] = A(cyan, 0.2f),
			[ImGuiCol.ResizeGripHovered] = A(magenta, 0.6f),
			[ImGuiCol.ResizeGripActive] = magenta,
			[ImGuiCol.InputTextCursor] = amber,
			[ImGuiCol.TabHovered] = M(surface, magenta, 0.4f),
			[ImGuiCol.Tab] = surface,
			[ImGuiCol.TabSelected] = M(surface, magenta, 0.28f),
			[ImGuiCol.TabSelectedOverline] = magenta,
			[ImGuiCol.TabDimmed] = M(night, surface, 0.6f),
			[ImGuiCol.TabDimmedSelected] = M(surface, violet, 0.25f),
			[ImGuiCol.TabDimmedSelectedOverline] = A(violet, 0.8f),
			[ImGuiCol.DockingPreview] = A(magenta, 0.5f),
			[ImGuiCol.DockingEmptyBg] = night,
			[ImGuiCol.PlotLines] = cyan,
			[ImGuiCol.PlotLinesHovered] = magenta,
			[ImGuiCol.PlotHistogram] = amber,
			[ImGuiCol.PlotHistogramHovered] = magenta,
			[ImGuiCol.TableHeaderBg] = M(surface, cyan, 0.12f),
			[ImGuiCol.TableBorderStrong] = A(cyan, 0.45f),
			[ImGuiCol.TableBorderLight] = A(cyan, 0.18f),
			[ImGuiCol.TableRowBg] = A(night, 0f),
			[ImGuiCol.TableRowBgAlt] = A(cyan, 0.04f),
			[ImGuiCol.TextLink] = cyan,
			[ImGuiCol.TextSelectedBg] = A(violet, 0.45f),
			[ImGuiCol.TreeLines] = A(cyan, 0.35f),
			[ImGuiCol.DragDropTarget] = amber,
			[ImGuiCol.NavCursor] = magenta,
			[ImGuiCol.NavWindowingHighlight] = A(cyan, 0.7f),
			[ImGuiCol.NavWindowingDimBg] = A(night, 0.6f),
			[ImGuiCol.ModalWindowDimBg] = A(night, 0.7f),
		};
	}

	private sealed class StyleScope(int colorCount, int varCount) : IDisposable
	{
		private bool disposed;

		public void Dispose()
		{
			if (disposed)
			{
				return;
			}

			disposed = true;
			ImGui.PopStyleVar(varCount);
			ImGui.PopStyleColor(colorCount);
		}
	}
}
