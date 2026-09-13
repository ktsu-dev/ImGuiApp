// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

internal static class EnumCache<TEnum> where TEnum : Enum
{
	internal static readonly TEnum[] Values = (TEnum[])Enum.GetValues(typeof(TEnum));
	internal static readonly string[] Names = Enum.GetNames(typeof(TEnum));
}
