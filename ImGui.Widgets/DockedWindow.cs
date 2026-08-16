// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using HexaImWindow = Hexa.NET.ImGui.Widgets.ImWindow;

public static partial class ImGuiWidgets
{
	/// <summary>
	/// A floating window that the user can drag into the dockspace <see cref="DrawDeferredDocked"/>
	/// creates. Subclass it and override <see cref="Title"/> and <see cref="DrawContent"/>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Only drawn by <see cref="DrawDeferredDocked"/>. Under <see cref="DrawDeferred"/> the window
	/// is registered but never rendered, because Hexa's widget manager - which owns both the
	/// dockspace and the list of registered windows - is only driven by that pump.
	/// </para>
	/// <para>
	/// The window is dockable, not auto-docked: it opens floating and stays there until the user
	/// drags it in. Hexa only pins a window to its dockspace when the window is marked as embedded,
	/// which it does not do for windows registered through <see cref="Show"/>.
	/// </para>
	/// <para>
	/// <see cref="Title"/> is both the window caption and its identity, so two windows sharing a
	/// title will collide.
	/// </para>
	/// </remarks>
	public abstract class DockedWindow
	{
		/// <summary>
		/// Adapts this <see cref="DockedWindow"/> to Hexa's window base class so it can be
		/// registered with Hexa's internal widget manager. Composition, not inheritance, keeps
		/// the vendor type out of this library's public surface.
		/// </summary>
		private readonly Adapter adapter;

		/// <summary>
		/// Initializes a new instance of the <see cref="DockedWindow"/> class.
		/// </summary>
		protected DockedWindow() => adapter = new Adapter(this);

		/// <summary>
		/// Gets the window caption, which is also its identity.
		/// </summary>
		protected abstract string Title { get; }

		/// <summary>
		/// Draws the window's contents. Called once per frame while the window is open.
		/// </summary>
		protected abstract void DrawContent();

		/// <summary>
		/// Registers the window so it is drawn by <see cref="DrawDeferredDocked"/>.
		/// </summary>
		public void Show() => adapter.Show();

		/// <summary>
		/// Unregisters the window so it is no longer drawn.
		/// </summary>
		public void Close() => adapter.Close();

		/// <summary>
		/// Forwards Hexa's window callbacks to the owning <see cref="DockedWindow"/>. Never
		/// exposed outside this file.
		/// </summary>
		private sealed class Adapter(DockedWindow owner) : HexaImWindow
		{
			/// <inheritdoc/>
			public override string Name => owner.Title;

			/// <inheritdoc/>
			public override void DrawContent() => owner.DrawContent();
		}
	}
}
