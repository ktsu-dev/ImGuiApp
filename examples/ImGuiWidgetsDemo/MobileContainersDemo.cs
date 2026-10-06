// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Examples.Widgets;

using System.Numerics;
using Hexa.NET.ImGui;
using ktsu.ImGui.Widgets;

/// <summary>
/// The mobile containers and loaders: card, PIN input and skeletons.
/// </summary>
internal static class MobileContainersDemo
{
	private static string pinValue = string.Empty;
	private static string otpValue = string.Empty;
	private static bool skeletonLoading = true;

	/// <summary>
	/// Returns this section's state to its starting values. The demo keeps its state in statics, which
	/// outlive a harness.
	/// </summary>
	internal static void ResetState()
	{
		pinValue = string.Empty;
		otpValue = string.Empty;
		skeletonLoading = true;
	}

	/// <summary>Draws the demo section.</summary>
	internal static void Show()
	{
		if (DemoProbe.Header("Mobile - Containers & Loaders"))
		{
			ImGui.TextUnformatted("Card (scoped, shadowed, elevated container):");
			ImGui.Separator();

			using (new ImGuiWidgets.Card(width: 280.0f))
			{
				ImGui.TextUnformatted("Card title");
				ImGui.TextWrapped("Cards group related content on an elevated surface with a soft drop shadow. Padding and rounding scale with the theme.");
				if (DemoProbe.Button("Action##cardAction"))
				{
					MobileDecoratorsDemo.NotificationCount++;
				}
			}

			ImGui.Separator();
			ImGui.TextUnformatted("PIN / OTP input (auto-advancing boxes):");
			ImGuiWidgets.PinInput("Pin##pin", ref pinValue, length: 4);
			ImGui.TextUnformatted($"PIN: {(pinValue.Length == 4 ? pinValue : "(incomplete)")}");

			ImGui.TextUnformatted("Masked, 6 digits:");
			ImGuiWidgets.PinInput("Otp##otp", ref otpValue, length: 6, masked: true);

			ImGui.Separator();
			DemoProbe.Checkbox("Loading##skeletonToggle", ref skeletonLoading);
			ImGui.TextUnformatted("Skeleton loaders (shimmer placeholders):");
			if (skeletonLoading)
			{
				ImGuiWidgets.SkeletonCircle("SkelAvatar");
				ImGui.SameLine();
				ImGui.BeginGroup();
				ImGuiWidgets.SkeletonLine("SkelLine1", width: 180.0f);
				ImGui.Spacing();
				ImGuiWidgets.SkeletonLine("SkelLine2", width: 120.0f);
				ImGui.EndGroup();
				ImGui.Spacing();
				ImGuiWidgets.SkeletonRect("SkelThumb", new Vector2(220.0f, 80.0f));
			}
			else
			{
				ImGui.TextUnformatted("Content loaded.");
			}
		}
	}
}
