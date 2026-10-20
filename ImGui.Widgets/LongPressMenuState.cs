// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;

using ktsu.ImGui.Widgets.Animation;

/// <summary>
/// The press-and-hold animation of a long-press menu's target, with no ImGui calls, so it can be tested
/// on its own.
/// </summary>
/// <remarks>
/// While the pointer is held on the target it shrinks steadily, reaching <see cref="PressedScale"/> just
/// as the hold becomes a long press, so the user can see the menu coming. When the menu opens the target
/// springs back with a little overshoot, the "haptic" bump; a press let go too soon springs back without
/// one.
/// </remarks>
internal sealed class LongPressMenuState
{
	/// <summary>How small the target gets at the moment the menu opens, as a fraction of its size.</summary>
	internal const float PressedScale = 0.94f;

	private const float Stiffness = 600f;

	// Under-damped for the bump when the menu opens, critically damped when a press is let go.
	private const float BumpDamping = 18f;

	private readonly Spring spring = new(1f, Stiffness, 2f * MathF.Sqrt(Stiffness)) { RestThreshold = 0.0005f };
	private bool holding;
	private bool spent;

	/// <summary>Gets the target's scale, 1 at rest.</summary>
	public float Scale => spring.Value;

	/// <summary>Follows a press on the target.</summary>
	/// <param name="deltaTime">Seconds since the last frame.</param>
	/// <param name="pressed">Whether the pointer is held on the target.</param>
	/// <param name="pressDuration">How long it has been held, in seconds.</param>
	/// <param name="longPressDuration">How long a hold must last to open the menu, in seconds.</param>
	/// <param name="opened">Whether the menu opened on this frame.</param>
	public void Update(float deltaTime, bool pressed, float pressDuration, float longPressDuration, bool opened)
	{
		if (opened)
		{
			// The press that opened the menu is spent: holding on afterwards does not shrink it again.
			holding = false;
			spent = true;
			spring.Damping = BumpDamping;
			spring.Target = 1f;
		}
		else if (pressed && !spent)
		{
			holding = true;
			float progress = longPressDuration <= 0f ? 1f : Math.Clamp(pressDuration / longPressDuration, 0f, 1f);
			spring.SnapTo(1f - ((1f - PressedScale) * progress));
			return;
		}
		else if (holding)
		{
			holding = false;
			spring.Damping = 2f * MathF.Sqrt(Stiffness);
			spring.Target = 1f;
		}

		if (!pressed)
		{
			spent = false;
		}

		_ = spring.Update(deltaTime);
	}
}
