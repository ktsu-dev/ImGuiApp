// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Bulkhead;

/// <summary>Which ends of a bar or button are rounded into a semicircular cap.</summary>
/// <remarks>
/// Start and end follow the shape's long axis: left and right for a wide shape, top and bottom for a
/// tall one.
/// </remarks>
[Flags]
public enum BulkheadCaps
{
	/// <summary>Square at both ends, for a block that butts against its neighbours.</summary>
	None = 0,

	/// <summary>Rounded at the left, or the top of a tall shape.</summary>
	Start = 1,

	/// <summary>Rounded at the right, or the bottom of a tall shape.</summary>
	End = 2,

	/// <summary>Rounded at both ends: a pill.</summary>
	Both = Start | End,
}

/// <summary>Which corner of its rectangle an elbow turns through.</summary>
public enum BulkheadCorner
{
	/// <summary>The column runs down the left edge and the bar along the top.</summary>
	TopLeft,

	/// <summary>The column runs down the right edge and the bar along the top.</summary>
	TopRight,

	/// <summary>The column runs up the left edge and the bar along the bottom.</summary>
	BottomLeft,

	/// <summary>The column runs up the right edge and the bar along the bottom.</summary>
	BottomRight,
}
