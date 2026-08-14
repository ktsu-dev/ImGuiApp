// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

// Note: no `using HexaComboEnumHelper = ...` alias here. C# cannot alias an open generic type,
// so Hexa's ComboEnumHelper<T> is written out in full at each call site below.

[SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "ImGuiWidgets is a partial class aggregating many widgets; moving EnumCombo's bodies into a nested EnumComboImpl (see Icon.cs, Avatar.cs, Switch.cs, HexaButtons.cs) was not enough to bring the merged type back under the coupling ceiling. Consider refactoring in the future.")]
public static partial class ImGuiWidgets
{
	/// <summary>
	/// Returns the display names Hexa's enum combo shows, in declaration order.
	/// </summary>
	/// <typeparam name="T">The enum type to enumerate.</typeparam>
	/// <returns>The display name of every declared member of <typeparamref name="T"/>.</returns>
	internal static IReadOnlyList<string> EnumComboNames<T>() where T : struct, Enum => EnumComboImpl.Names<T>();

	/// <summary>
	/// Draws a combo box listing every member of an enum type.
	/// </summary>
	/// <typeparam name="T">The enum type to list.</typeparam>
	/// <param name="label">Label for display and identity.</param>
	/// <param name="value">The selected value, updated in place when a new member is chosen.</param>
	/// <returns><see langword="true"/> if a new value was selected this frame.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="label"/> is <see langword="null"/>.</exception>
	public static bool EnumCombo<T>(string label, ref T value) where T : struct, Enum => EnumComboImpl.Combo(label, ref value);

	/// <summary>
	/// Contains the implementation details for the enum combo helpers, kept off the
	/// <see cref="ImGuiWidgets"/> partial class itself to stay under its CA1506 class-coupling ceiling.
	/// </summary>
	internal static class EnumComboImpl
	{
		/// <summary>
		/// Returns the display names Hexa's enum combo shows, in declaration order.
		/// </summary>
		/// <typeparam name="T">The enum type to enumerate.</typeparam>
		/// <returns>The display name of every declared member of <typeparamref name="T"/>.</returns>
		internal static IReadOnlyList<string> Names<T>() where T : struct, Enum
		{
			T[] values = Enum.GetValues<T>();
			List<string> names = new(values.Length);
			foreach (T value in values)
			{
				names.Add(Hexa.NET.ImGui.Widgets.ComboEnumHelper<T>.GetName(value));
			}

			return names;
		}

		/// <summary>
		/// Draws a combo box listing every member of an enum type.
		/// </summary>
		/// <typeparam name="T">The enum type to list.</typeparam>
		/// <param name="label">Label for display and identity.</param>
		/// <param name="value">The selected value, updated in place when a new member is chosen.</param>
		/// <returns><see langword="true"/> if a new value was selected this frame.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="label"/> is <see langword="null"/>.</exception>
		internal static bool Combo<T>(string label, ref T value) where T : struct, Enum
		{
			Ensure.NotNull(label);
			return Hexa.NET.ImGui.Widgets.ComboEnumHelper<T>.Combo(label, ref value);
		}
	}
}
