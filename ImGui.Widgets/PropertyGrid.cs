// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;

using Hexa.NET.ImGui;

using ktsu.ImGui.Probes;

/// <summary>
/// Provides custom ImGui widgets.
/// </summary>
public static partial class ImGuiWidgets
{
	/// <summary>
	/// What a path row is asking the host to browse for.
	/// </summary>
	public enum PropertyPathKind
	{
		/// <summary>An existing file.</summary>
		File,

		/// <summary>An existing directory.</summary>
		Directory,

		/// <summary>An image file, whose thumbnail the row previews.</summary>
		Image,
	}

	/// <summary>
	/// A path row's request for the host to choose a path, raised when its browse button is pressed.
	/// </summary>
	/// <remarks>
	/// Browsing is asynchronous: a file dialog stays open across many frames, long after the row that
	/// opened it has returned. The request therefore carries no reference to the value being edited —
	/// the host calls <see cref="Complete"/> whenever the choice is made, and the row picks the result
	/// up and writes it through the next time it is drawn.
	/// </remarks>
	public sealed class PropertyPathRequest
	{
		private readonly uint rowId;

		internal PropertyPathRequest(uint rowId, string label, PropertyPathKind kind, string current)
		{
			this.rowId = rowId;
			Label = label;
			Kind = kind;
			Current = current;
		}

		/// <summary>Gets the visible label of the row that asked, for use as a dialog title.</summary>
		public string Label { get; }

		/// <summary>Gets what the row is asking for.</summary>
		public PropertyPathKind Kind { get; }

		/// <summary>Gets the path the row currently holds, which may be empty.</summary>
		public string Current { get; }

		/// <summary>
		/// Hands the chosen path back to the row, which adopts it the next time it is drawn.
		/// </summary>
		/// <param name="path">
		/// The chosen path, or <see langword="null"/> or empty when the host cancelled, which leaves
		/// the row's value alone.
		/// </param>
		/// <remarks>May be called from any thread, and from any frame after the request was raised.</remarks>
		public void Complete(string? path) => PropertyGrid.CompleteBrowse(rowId, path);
	}

	/// <summary>
	/// A two-component double-precision vector, for property rows whose value is measured rather
	/// than rendered.
	/// </summary>
	/// <param name="X">The first component.</param>
	/// <param name="Y">The second component.</param>
	public readonly record struct DoubleVector2(double X, double Y);

	/// <summary>
	/// A three-component double-precision vector, for property rows whose value is measured rather
	/// than rendered.
	/// </summary>
	/// <param name="X">The first component.</param>
	/// <param name="Y">The second component.</param>
	/// <param name="Z">The third component.</param>
	public readonly record struct DoubleVector3(double X, double Y, double Z);

	/// <summary>
	/// Layout and behavior settings shared by every row of a <see cref="PropertyGrid"/>.
	/// </summary>
	public sealed class PropertyGridOptions
	{
		/// <summary>
		/// Gets the share of the grid's width given to the label column, between zero and one.
		/// Ignored when <see cref="LabelColumnWidth"/> is set.
		/// </summary>
		public float LabelColumnWeight { get; init; } = 0.4f;

		/// <summary>
		/// Gets a fixed width in pixels for the label column. Zero, the default, stretches the column
		/// by <see cref="LabelColumnWeight"/> instead.
		/// </summary>
		public float LabelColumnWidth { get; init; }

		/// <summary>
		/// Gets a value indicating whether every row is drawn disabled. Rows still draw their value,
		/// they just cannot be edited.
		/// </summary>
		public bool ReadOnly { get; init; }

		/// <summary>Gets the printf-style format single-precision rows display their value with.</summary>
		public string FloatFormat { get; init; } = "%.3f";

		/// <summary>Gets the printf-style format double-precision rows display their value with.</summary>
		public string DoubleFormat { get; init; } = "%.6f";

		/// <summary>Gets a value indicating whether a list row starts with its elements shown.</summary>
		public bool ListsStartExpanded { get; init; } = true;

		/// <summary>Gets the size an image row's thumbnail is drawn at.</summary>
		public Vector2 ThumbnailSize { get; init; } = new(96f, 96f);

		/// <summary>
		/// Gets the callback that turns an image row's path into a texture id, or zero when the path
		/// has no preview. Without one, an image row draws an empty preview frame.
		/// </summary>
		/// <remarks>
		/// This library knows nothing about texture upload — that lives in <c>ktsu.ImGui.App</c>, which
		/// it deliberately does not reference. An application using ImGuiApp passes
		/// <c>path =&gt; ImGuiApp.GetOrLoadTexture(path).TextureId</c>.
		/// </remarks>
		public Func<string, nint>? ThumbnailResolver { get; init; }

		/// <summary>
		/// Gets the callback invoked when a path row's browse button is pressed. Without one, the
		/// browse button is disabled and paths can only be typed.
		/// </summary>
		public Action<PropertyPathRequest>? OnBrowse { get; init; }
	}

	/// <summary>
	/// A two-column grid of labelled, editable properties: name on the left, editor on the right.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The grid is immediate mode like everything else here — it holds no model, and each row edits a
	/// variable passed by reference. Open one in a <c>using</c> statement and call a row method per
	/// property. One overloaded <c>Value</c> row covers every scalar, vector and color type, and the
	/// path rows and <c>List</c> rows cover the rest:
	/// </para>
	/// <code>
	/// using (ImGuiWidgets.PropertyGrid grid = new("Settings"))
	/// {
	///     grid.Value("Name", ref name);
	///     grid.Value("Count", ref count);
	///     grid.Value("Tint", ref tint);
	///     grid.ImagePath("Icon", ref iconPath);
	///     grid.List("Tags", tags);
	/// }
	/// </code>
	/// <para>
	/// Every row returns whether it changed this frame, and <see cref="Changed"/> accumulates those
	/// answers so the whole grid can be tested once.
	/// </para>
	/// </remarks>
	public sealed partial class PropertyGrid : IDisposable
	{
		private const ImGuiTableFlags TableFlags =
			ImGuiTableFlags.Resizable | ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.PadOuterX | ImGuiTableFlags.SizingStretchProp;

		// Completed browse results, keyed by the ImGui id of the row that asked. An entry is consumed
		// by the next draw of that row; the tick stamp is what lets an entry whose row never draws
		// again be swept, so a host that browses in a torn-down panel cannot grow this without bound.
		private static readonly Dictionary<uint, PendingPath> PendingPaths = [];

		// A host may well answer a browse from whatever thread its dialog completed on, so the table
		// is guarded even though everything that reads it runs on the UI thread.
		private static readonly Lock PendingLock = new();
		private static long tick;

		// A pending result is dropped once this many grids have been constructed since it arrived,
		// which at one grid per frame is a comfortable few seconds.
		private const long PendingLifetime = 600;

		private static readonly PropertyGridOptions DefaultOptions = new();

		// Width the current row leaves clear at its right-hand end, for a trailing button drawn by
		// whatever is composing the row. Set by the list rows around each element's editor.
		private float trailingReserve;

		private bool disposed;

		/// <summary>
		/// Begins a property grid. Draws nothing until a row method is called.
		/// </summary>
		/// <param name="id">A label unique within the enclosing window; used as the grid's ImGui id and not drawn.</param>
		public PropertyGrid(string id) : this(id, null)
		{
		}

		/// <summary>
		/// Begins a property grid with explicit options.
		/// </summary>
		/// <param name="id">A label unique within the enclosing window; used as the grid's ImGui id and not drawn.</param>
		/// <param name="options">Layout and behavior settings, or <see langword="null"/> for the defaults.</param>
		public PropertyGrid(string id, PropertyGridOptions? options)
		{
			Options = options ?? DefaultOptions;
			tick++;

			ImGui.PushID(id);
			ImGuiProbes.PushScope(id);

			IsDrawing = ImGui.BeginTable("##propertyGrid", 2, TableFlags);
			if (IsDrawing)
			{
				if (Options.LabelColumnWidth > 0f)
				{
					ImGui.TableSetupColumn("##name", ImGuiTableColumnFlags.WidthFixed, Options.LabelColumnWidth);
				}
				else
				{
					ImGui.TableSetupColumn("##name", ImGuiTableColumnFlags.WidthStretch, Math.Clamp(Options.LabelColumnWeight, 0.05f, 0.95f));
				}

				ImGui.TableSetupColumn("##value", ImGuiTableColumnFlags.WidthStretch, 1f);
			}
		}

		/// <summary>Gets the settings this grid was opened with.</summary>
		public PropertyGridOptions Options { get; }

		/// <summary>
		/// Gets a value indicating whether the grid's table opened. It does not when the grid is
		/// entirely clipped, and every row is then a no-op.
		/// </summary>
		public bool IsDrawing { get; }

		/// <summary>Gets a value indicating whether any row of this grid changed its value this frame.</summary>
		public bool Changed { get; private set; }

		/// <summary>Closes the grid's table and restores the id stack.</summary>
		public void Dispose()
		{
			if (disposed)
			{
				return;
			}

			disposed = true;

			if (IsDrawing)
			{
				ImGui.EndTable();
			}

			ImGuiProbes.PopScope();
			ImGui.PopID();
		}

		/// <summary>
		/// Draws a collapsible section header spanning both columns. Rows drawn while it is open are
		/// indented beneath it.
		/// </summary>
		/// <remarks>
		/// Follows the <c>TreeNode</c>/<c>TreePop</c> convention: call <see cref="EndSection"/> only
		/// when this returned <see langword="true"/>.
		/// </remarks>
		/// <param name="label">The section caption; text after <c>##</c> is hidden but used for the id.</param>
		/// <param name="defaultOpen">Whether the section starts expanded.</param>
		/// <returns><see langword="true"/> while the section is expanded.</returns>
		public bool Section(string label, bool defaultOpen = true)
		{
			Ensure.NotNull(label);

			if (!IsDrawing)
			{
				return false;
			}

			ImGui.TableNextRow();
			ImGui.TableSetColumnIndex(0);

			ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.SpanAllColumns;
			if (defaultOpen)
			{
				flags |= ImGuiTreeNodeFlags.DefaultOpen;
			}

			bool open = ImGui.TreeNodeEx(label, flags);
			ImGuiProbes.MarkItem(VisibleLabel(label));
			return open;
		}

		/// <summary>Closes the section opened by a <see cref="Section"/> call that returned true.</summary>
		public void EndSection()
		{
			if (IsDrawing)
			{
				ImGui.TreePop();
			}
		}

		/// <summary>
		/// Starts a row: writes the label into the first column and sizes the editor to fill the
		/// second, less anything a composing row asked to keep clear.
		/// </summary>
		/// <param name="label">The row's label.</param>
		/// <param name="reserveRight">Extra width to leave clear at the right of the editor.</param>
		/// <returns><see langword="false"/> when the grid never opened, in which case nothing was drawn.</returns>
		private bool BeginRow(string label, float reserveRight = 0f)
		{
			if (!IsDrawing)
			{
				return false;
			}

			ImGui.TableNextRow();
			ImGui.TableSetColumnIndex(0);
			ImGui.AlignTextToFramePadding();
			ImGui.TextUnformatted(VisibleLabel(label));

			ImGui.TableSetColumnIndex(1);

			if (Options.ReadOnly)
			{
				ImGui.BeginDisabled();
			}

			SetEditorWidth(reserveRight);
			return true;
		}

		/// <summary>
		/// Sizes the next item to the width left in the value column, less the trailing reservations.
		/// </summary>
		/// <param name="reserveRight">Extra width to leave clear beyond the row's own reservation.</param>
		private void SetEditorWidth(float reserveRight)
		{
			float available = ImGui.GetContentRegionAvail().X - trailingReserve - reserveRight;
			ImGui.SetNextItemWidth(Math.Max(available, ImGui.GetFrameHeight()));
		}

		/// <summary>
		/// Ends a row: records the editor for the probes, and folds its result into <see cref="Changed"/>.
		/// </summary>
		/// <param name="label">The row's label, whose visible part names the probe.</param>
		/// <param name="changed">Whether the editor reported a change.</param>
		/// <returns><paramref name="changed"/>, so a row method can return this directly.</returns>
		private bool EndRow(string label, bool changed)
		{
			ImGuiProbes.MarkItem(VisibleLabel(label));
			return FinishRow(changed);
		}

		/// <summary>
		/// Ends a row whose editor was already recorded, because it drew more than one item and marked
		/// them itself.
		/// </summary>
		/// <param name="changed">Whether the editor reported a change.</param>
		/// <returns>Whether the row changed, which is never true for a read-only grid.</returns>
		private bool FinishRow(bool changed)
		{
			if (Options.ReadOnly)
			{
				ImGui.EndDisabled();
				changed = false;
			}

			Changed |= changed;
			return changed;
		}

		/// <summary>Returns the label an editor is given so its text is not drawn beside it.</summary>
		private static string Hidden(string label) => "##" + label;

		private readonly record struct PendingPath(string? Path, long Tick);

		/// <summary>Records a browse result for a row to pick up on its next draw.</summary>
		/// <param name="rowId">The ImGui id of the row that asked.</param>
		/// <param name="path">The chosen path, or null when the host cancelled.</param>
		internal static void CompleteBrowse(uint rowId, string? path)
		{
			lock (PendingLock)
			{
				SweepPendingPaths();
				PendingPaths[rowId] = new PendingPath(path, tick);
			}
		}

		/// <summary>Takes the browse result waiting for a row, if there is one.</summary>
		/// <param name="rowId">The ImGui id of the row.</param>
		/// <param name="path">The chosen path, which is null when the host cancelled.</param>
		/// <returns><see langword="true"/> when a result was waiting.</returns>
		private static bool TryTakePendingPath(uint rowId, out string? path)
		{
			lock (PendingLock)
			{
				if (PendingPaths.TryGetValue(rowId, out PendingPath pending))
				{
					PendingPaths.Remove(rowId);
					path = pending.Path;
					return true;
				}
			}

			path = null;
			return false;
		}

		/// <summary>
		/// Drops results whose row has not drawn for long enough that it probably never will. Called
		/// with <see cref="PendingLock"/> held.
		/// </summary>
		private static void SweepPendingPaths()
		{
			if (PendingPaths.Count == 0)
			{
				return;
			}

			// Copied out first: the dictionary cannot be written to while it is being enumerated.
			uint[] keys = new uint[PendingPaths.Count];
			PendingPaths.Keys.CopyTo(keys, 0);

			foreach (uint key in keys)
			{
				if (tick - PendingPaths[key].Tick > PendingLifetime)
				{
					PendingPaths.Remove(key);
				}
			}
		}
	}
}
