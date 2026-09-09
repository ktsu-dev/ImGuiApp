// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Color;
using ktsu.ImGui.Probes;
using ktsu.Semantics.Color;

/// <summary>
/// Provides custom ImGui widgets.
/// </summary>
public static partial class ImGuiWidgets
{
	public sealed partial class PropertyGrid
	{
		// How many characters a text row accepts. Generous for a name, and comfortably longer than
		// any path a filesystem will hand back.
		private const uint TextLength = 512u;

		/// <summary>Draws a checkbox row.</summary>
		/// <param name="label">The property name; text after <c>##</c> is hidden but used for the id.</param>
		/// <param name="value">The value being edited.</param>
		/// <returns><see langword="true"/> if the value changed this frame; otherwise <see langword="false"/>.</returns>
		public bool Value(string label, ref bool value)
		{
			if (!BeginRow(label))
			{
				return false;
			}

			bool changed = ImGui.Checkbox(Hidden(label), ref value);
			return EndRow(label, changed);
		}

		/// <summary>Draws an integer row.</summary>
		/// <param name="label">The property name; text after <c>##</c> is hidden but used for the id.</param>
		/// <param name="value">The value being edited.</param>
		/// <returns><see langword="true"/> if the value changed this frame; otherwise <see langword="false"/>.</returns>
		public bool Value(string label, ref int value)
		{
			if (!BeginRow(label))
			{
				return false;
			}

			bool changed = ImGui.InputInt(Hidden(label), ref value, 0, 0);
			return EndRow(label, changed);
		}

		/// <summary>Draws an integer row whose value is clamped to a range.</summary>
		/// <param name="label">The property name; text after <c>##</c> is hidden but used for the id.</param>
		/// <param name="value">The value being edited. Clamped to the range whenever it changes.</param>
		/// <param name="min">The inclusive lower bound.</param>
		/// <param name="max">The inclusive upper bound.</param>
		/// <returns><see langword="true"/> if the value changed this frame; otherwise <see langword="false"/>.</returns>
		public bool Value(string label, ref int value, int min, int max)
		{
			if (min > max)
			{
				(min, max) = (max, min);
			}

			if (!Value(label, ref value))
			{
				return false;
			}

			value = Math.Clamp(value, min, max);
			return true;
		}

		/// <summary>Draws a 64-bit integer row.</summary>
		/// <param name="label">The property name; text after <c>##</c> is hidden but used for the id.</param>
		/// <param name="value">The value being edited.</param>
		/// <returns><see langword="true"/> if the value changed this frame; otherwise <see langword="false"/>.</returns>
		[SuppressMessage("Major Code Smell", "S6640:Make sure that using \"unsafe\" is safe here", Justification = "Required for the pointer-based ImGui scalar input; the pointer is scoped to the call and not retained.")]
		public bool Value(string label, ref long value)
		{
			if (!BeginRow(label))
			{
				return false;
			}

			bool changed;
			unsafe
			{
				fixed (long* pValue = &value)
				{
					changed = ImGui.InputScalar(Hidden(label), ImGuiDataType.S64, pValue);
				}
			}

			return EndRow(label, changed);
		}

		/// <summary>Draws a 64-bit integer row whose value is clamped to a range.</summary>
		/// <param name="label">The property name; text after <c>##</c> is hidden but used for the id.</param>
		/// <param name="value">The value being edited. Clamped to the range whenever it changes.</param>
		/// <param name="min">The inclusive lower bound.</param>
		/// <param name="max">The inclusive upper bound.</param>
		/// <returns><see langword="true"/> if the value changed this frame; otherwise <see langword="false"/>.</returns>
		public bool Value(string label, ref long value, long min, long max)
		{
			if (min > max)
			{
				(min, max) = (max, min);
			}

			if (!Value(label, ref value))
			{
				return false;
			}

			value = Math.Clamp(value, min, max);
			return true;
		}

		/// <summary>Draws a single-precision floating point row.</summary>
		/// <param name="label">The property name; text after <c>##</c> is hidden but used for the id.</param>
		/// <param name="value">The value being edited.</param>
		/// <returns><see langword="true"/> if the value changed this frame; otherwise <see langword="false"/>.</returns>
		public bool Value(string label, ref float value)
		{
			if (!BeginRow(label))
			{
				return false;
			}

			bool changed = ImGui.InputFloat(Hidden(label), ref value, 0f, 0f, Options.FloatFormat);
			return EndRow(label, changed);
		}

		/// <summary>Draws a single-precision floating point row whose value is clamped to a range.</summary>
		/// <param name="label">The property name; text after <c>##</c> is hidden but used for the id.</param>
		/// <param name="value">The value being edited. Clamped to the range whenever it changes.</param>
		/// <param name="min">The inclusive lower bound.</param>
		/// <param name="max">The inclusive upper bound.</param>
		/// <returns><see langword="true"/> if the value changed this frame; otherwise <see langword="false"/>.</returns>
		public bool Value(string label, ref float value, float min, float max)
		{
			if (min > max)
			{
				(min, max) = (max, min);
			}

			if (!Value(label, ref value))
			{
				return false;
			}

			value = Math.Clamp(value, min, max);
			return true;
		}

		/// <summary>Draws a double-precision floating point row.</summary>
		/// <param name="label">The property name; text after <c>##</c> is hidden but used for the id.</param>
		/// <param name="value">The value being edited.</param>
		/// <returns><see langword="true"/> if the value changed this frame; otherwise <see langword="false"/>.</returns>
		public bool Value(string label, ref double value)
		{
			if (!BeginRow(label))
			{
				return false;
			}

			bool changed = ImGui.InputDouble(Hidden(label), ref value, 0d, 0d, Options.DoubleFormat);
			return EndRow(label, changed);
		}

		/// <summary>Draws a double-precision floating point row whose value is clamped to a range.</summary>
		/// <param name="label">The property name; text after <c>##</c> is hidden but used for the id.</param>
		/// <param name="value">The value being edited. Clamped to the range whenever it changes.</param>
		/// <param name="min">The inclusive lower bound.</param>
		/// <param name="max">The inclusive upper bound.</param>
		/// <returns><see langword="true"/> if the value changed this frame; otherwise <see langword="false"/>.</returns>
		public bool Value(string label, ref double value, double min, double max)
		{
			if (min > max)
			{
				(min, max) = (max, min);
			}

			if (!Value(label, ref value))
			{
				return false;
			}

			value = Math.Clamp(value, min, max);
			return true;
		}

		/// <summary>Draws a text row.</summary>
		/// <param name="label">The property name; text after <c>##</c> is hidden but used for the id.</param>
		/// <param name="value">The value being edited.</param>
		/// <returns><see langword="true"/> if the value changed this frame; otherwise <see langword="false"/>.</returns>
		public bool Value(string label, ref string value)
		{
			if (!BeginRow(label))
			{
				return false;
			}

			value ??= string.Empty;
			bool changed = ImGui.InputText(Hidden(label), ref value, TextLength);
			return EndRow(label, changed);
		}

		/// <summary>Draws a combo box row over the values of an enumeration.</summary>
		/// <typeparam name="TEnum">The enumeration type.</typeparam>
		/// <param name="label">The property name; text after <c>##</c> is hidden but used for the id.</param>
		/// <param name="value">The value being edited.</param>
		/// <returns><see langword="true"/> if the value changed this frame; otherwise <see langword="false"/>.</returns>
		public bool Enum<TEnum>(string label, ref TEnum value) where TEnum : struct, System.Enum
		{
			if (!BeginRow(label))
			{
				return false;
			}

			string[] names = System.Enum.GetNames<TEnum>();
			TEnum[] values = System.Enum.GetValues<TEnum>();
			int index = Array.IndexOf(values, value);

			bool changed = ImGui.Combo(Hidden(label), ref index, names, names.Length);
			if (changed && index >= 0 && index < values.Length)
			{
				value = values[index];
			}

			return EndRow(label, changed);
		}

		/// <summary>Draws a two-component single-precision vector row.</summary>
		/// <param name="label">The property name; text after <c>##</c> is hidden but used for the id.</param>
		/// <param name="value">The value being edited.</param>
		/// <returns><see langword="true"/> if the value changed this frame; otherwise <see langword="false"/>.</returns>
		public bool Value(string label, ref Vector2 value)
		{
			if (!BeginRow(label))
			{
				return false;
			}

			bool changed = ImGui.InputFloat2(Hidden(label), ref value, Options.FloatFormat);
			return EndRow(label, changed);
		}

		/// <summary>Draws a three-component single-precision vector row.</summary>
		/// <param name="label">The property name; text after <c>##</c> is hidden but used for the id.</param>
		/// <param name="value">The value being edited.</param>
		/// <returns><see langword="true"/> if the value changed this frame; otherwise <see langword="false"/>.</returns>
		public bool Value(string label, ref Vector3 value)
		{
			if (!BeginRow(label))
			{
				return false;
			}

			bool changed = ImGui.InputFloat3(Hidden(label), ref value, Options.FloatFormat);
			return EndRow(label, changed);
		}

		/// <summary>Draws a two-component double-precision vector row.</summary>
		/// <param name="label">The property name; text after <c>##</c> is hidden but used for the id.</param>
		/// <param name="value">The value being edited.</param>
		/// <returns><see langword="true"/> if the value changed this frame; otherwise <see langword="false"/>.</returns>
		[SuppressMessage("Major Code Smell", "S6640:Make sure that using \"unsafe\" is safe here", Justification = "Required for the pointer-based ImGui scalar input; the pointer is scoped to the call and not retained.")]
		public bool Value(string label, ref DoubleVector2 value)
		{
			if (!BeginRow(label))
			{
				return false;
			}

			bool changed;
			unsafe
			{
				double* components = stackalloc double[2];
				components[0] = value.X;
				components[1] = value.Y;

				changed = ImGui.InputScalarN(Hidden(label), ImGuiDataType.Double, components, 2);
				if (changed)
				{
					value = new DoubleVector2(components[0], components[1]);
				}
			}

			return EndRow(label, changed);
		}

		/// <summary>Draws a three-component double-precision vector row.</summary>
		/// <param name="label">The property name; text after <c>##</c> is hidden but used for the id.</param>
		/// <param name="value">The value being edited.</param>
		/// <returns><see langword="true"/> if the value changed this frame; otherwise <see langword="false"/>.</returns>
		[SuppressMessage("Major Code Smell", "S6640:Make sure that using \"unsafe\" is safe here", Justification = "Required for the pointer-based ImGui scalar input; the pointer is scoped to the call and not retained.")]
		public bool Value(string label, ref DoubleVector3 value)
		{
			if (!BeginRow(label))
			{
				return false;
			}

			bool changed;
			unsafe
			{
				double* components = stackalloc double[3];
				components[0] = value.X;
				components[1] = value.Y;
				components[2] = value.Z;

				changed = ImGui.InputScalarN(Hidden(label), ImGuiDataType.Double, components, 3);
				if (changed)
				{
					value = new DoubleVector3(components[0], components[1], components[2]);
				}
			}

			return EndRow(label, changed);
		}

		/// <summary>
		/// Draws a color row: a swatch that opens ImGui's color picker.
		/// </summary>
		/// <remarks>
		/// The value is the semantic linear <see cref="ktsu.Semantics.Color.Color"/>; the picker edits the
		/// sRGB-encoded form, which is what a person reading hex digits expects to see.
		/// </remarks>
		/// <param name="label">The property name; text after <c>##</c> is hidden but used for the id.</param>
		/// <param name="value">The value being edited.</param>
		/// <returns><see langword="true"/> if the value changed this frame; otherwise <see langword="false"/>.</returns>
		public bool Value(string label, ref Color value)
		{
			if (!BeginRow(label))
			{
				return false;
			}

			Vector4 srgb = value.ToSrgbVector4();
			bool changed = ImGui.ColorEdit4(Hidden(label), ref srgb, ImGuiColorEditFlags.AlphaBar | ImGuiColorEditFlags.AlphaPreviewHalf);
			if (changed)
			{
				value = ColorImGuiExtensions.FromImGuiVector4(srgb);
			}

			return EndRow(label, changed);
		}

		/// <summary>Draws a file path row: a text field and a browse button.</summary>
		/// <param name="label">The property name; text after <c>##</c> is hidden but used for the id.</param>
		/// <param name="value">The value being edited.</param>
		/// <returns><see langword="true"/> if the value changed this frame; otherwise <see langword="false"/>.</returns>
		public bool FilePath(string label, ref string value) => PathRow(label, ref value, PropertyPathKind.File);

		/// <summary>Draws a directory path row: a text field and a browse button.</summary>
		/// <param name="label">The property name; text after <c>##</c> is hidden but used for the id.</param>
		/// <param name="value">The value being edited.</param>
		/// <returns><see langword="true"/> if the value changed this frame; otherwise <see langword="false"/>.</returns>
		public bool DirectoryPath(string label, ref string value) => PathRow(label, ref value, PropertyPathKind.Directory);

		/// <summary>
		/// Draws an image path row: a text field, a browse button, and a thumbnail of the image
		/// beneath them.
		/// </summary>
		/// <remarks>
		/// The thumbnail comes from <see cref="PropertyGridOptions.ThumbnailResolver"/>. Without one,
		/// or when it reports no texture for the path, an empty preview frame is drawn in its place so
		/// the row keeps its height.
		/// </remarks>
		/// <param name="label">The property name; text after <c>##</c> is hidden but used for the id.</param>
		/// <param name="value">The value being edited.</param>
		/// <returns><see langword="true"/> if the value changed this frame; otherwise <see langword="false"/>.</returns>
		public bool ImagePath(string label, ref string value) => PathRow(label, ref value, PropertyPathKind.Image);

		/// <summary>
		/// Draws a text field with a trailing browse button, and for an image, a thumbnail beneath it.
		/// </summary>
		/// <param name="label">The property name.</param>
		/// <param name="value">The path being edited.</param>
		/// <param name="kind">What the browse button asks the host for.</param>
		/// <returns><see langword="true"/> if the value changed this frame; otherwise <see langword="false"/>.</returns>
		private bool PathRow(string label, ref string value, PropertyPathKind kind)
		{
			float browseWidth = ImGui.GetFrameHeight() * 1.5f;
			float spacing = ImGui.GetStyle().ItemInnerSpacing.X;

			if (!BeginRow(label, browseWidth + spacing))
			{
				return false;
			}

			value ??= string.Empty;

			// A browse the host completed while this row was not drawing is waiting under the row's
			// id; adopt it before the field is submitted, so the field shows the chosen path at once.
			uint rowId = ImGui.GetID(label);
			bool changed = false;
			if (TryTakePendingPath(rowId, out string? chosen)
				&& !Options.ReadOnly
				&& !string.IsNullOrEmpty(chosen)
				&& !string.Equals(chosen, value, StringComparison.Ordinal))
			{
				value = chosen;
				changed = true;
			}

			changed |= ImGui.InputText(Hidden(label), ref value, TextLength);
			ImGuiProbes.MarkItem(VisibleLabel(label));

			ImGui.SameLine(0f, spacing);
			using (new ScopedDisable(Options.OnBrowse is null))
			{
				// The id carries the row's label: every browse button in a grid shares one id stack,
				// so a constant one would make them all the same item to ImGui.
				if (ImGui.Button($"...##{label}/browse", new Vector2(browseWidth, 0f)))
				{
					Options.OnBrowse?.Invoke(new PropertyPathRequest(rowId, VisibleLabel(label), kind, value));
				}
			}

			ImGuiProbes.MarkItem($"{VisibleLabel(label)}/browse");

			if (kind == PropertyPathKind.Image)
			{
				DrawThumbnail(label, value);
			}

			return FinishRow(changed);
		}

		/// <summary>
		/// Draws the preview beneath an image row: the resolved texture, or an empty frame when there
		/// is nothing to show.
		/// </summary>
		/// <param name="label">The row's label, which names the preview for the probes.</param>
		/// <param name="path">The path to preview.</param>
		private void DrawThumbnail(string label, string path)
		{
			Vector2 size = Options.ThumbnailSize;
			nint texture = string.IsNullOrWhiteSpace(path) ? 0 : Options.ThumbnailResolver?.Invoke(path) ?? 0;

			if (texture != 0)
			{
				Image(texture, size);
			}
			else
			{
				Vector2 origin = ImGui.GetCursorScreenPos();
				ImGui.Dummy(size);
				ImGui.GetWindowDrawList().AddRect(
					origin,
					new Vector2(origin.X + size.X, origin.Y + size.Y),
					ImGui.GetColorU32(ImGuiCol.Border),
					ImGui.GetStyle().FrameRounding);
			}

			ImGuiProbes.MarkItem($"{VisibleLabel(label)}/thumbnail");
		}
	}
}
