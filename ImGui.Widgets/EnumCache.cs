// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

internal static class EnumCache<TEnum> where TEnum : Enum
{
	internal static readonly TEnum[] Values = ReadValues();
	internal static readonly string[] Names = Enum.GetNames(typeof(TEnum));

	/// <summary>
	/// Reads the values in the order <see cref="Enum.GetValues(Type)"/> gives them, without its
	/// runtime-built array, which native AOT can't promise for an arbitrary enum type. Callers
	/// constrain only to <see cref="Enum"/>, so the generic <c>GetValues&lt;TEnum&gt;</c> isn't reachable.
	/// </summary>
	private static TEnum[] ReadValues()
	{
		Array underlying = Enum.GetValuesAsUnderlyingType(typeof(TEnum));
		TEnum[] values = new TEnum[underlying.Length];
		for (int i = 0; i < values.Length; i++)
		{
			values[i] = (TEnum)Enum.ToObject(typeof(TEnum), underlying.GetValue(i)!);
		}

		return values;
	}
}
