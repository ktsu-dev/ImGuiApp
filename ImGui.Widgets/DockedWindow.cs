// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using HexaImWindow = Hexa.NET.ImGui.Widgets.ImWindow;

public static partial class ImGuiWidgets
{
	/// <summary>
	/// A window that docks into the dockspace created by <see cref="DrawDeferredDocked"/>.
	/// Subclass it and override <see cref="Title"/> and <see cref="DrawContent"/>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Only drawn by <see cref="DrawDeferredDocked"/>. Under <see cref="DrawDeferred"/> the window
	/// is registered but never rendered, because the dockspace it attaches to does not exist.
	/// </para>
	/// <para>
	/// <see cref="Title"/> is both the window caption and its identity, so two windows sharing a
	/// title will collide.
	/// </para>
	/// </remarks>
	public abstract class DockedWindow : HexaImWindow
	{
		/// <summary>
		/// Gets the window caption, which is also its identity.
		/// </summary>
		protected abstract string Title { get; }

		/// <summary>
		/// Gets the window caption. Forwards to <see cref="Title"/>.
		/// </summary>
		public sealed override string Name => Title;

		/// <summary>
		/// Draws the window's contents. Called once per frame while the window is open.
		/// </summary>
		public abstract override void DrawContent();
	}
}
