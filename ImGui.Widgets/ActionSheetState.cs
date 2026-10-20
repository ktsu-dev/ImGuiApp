// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using ktsu.ImGui.Widgets.Overlays;

/// <summary>
/// An action sheet's open, choose and cancel rules, with no ImGui calls: a sheet answers once, with a
/// button's index or with -1 for cancel, and ignores anything pressed while it slides away.
/// </summary>
internal sealed class ActionSheetState
{
	/// <summary>The answer a cancelled sheet gives.</summary>
	internal const int Cancelled = -1;

	/// <summary>Gets the slide in and out.</summary>
	public OverlayTransition Transition { get; } = new();

	/// <summary>Gets how many buttons the sheet has, not counting Cancel.</summary>
	public int ButtonCount { get; private set; }

	/// <summary>Gets a value indicating whether the sheet is waiting for an answer.</summary>
	public bool IsOpen => Transition.IsOpen;

	/// <summary>Opens the sheet with so many buttons.</summary>
	/// <param name="buttonCount">How many buttons, not counting Cancel.</param>
	public void Open(int buttonCount)
	{
		ButtonCount = buttonCount;
		Transition.Open();
	}

	/// <summary>Answers the sheet, which then slides away.</summary>
	/// <param name="index">A button's index, or <see cref="Cancelled"/>.</param>
	/// <returns>True when the answer was taken; false when the sheet had already answered or the index is not a button.</returns>
	public bool TryAnswer(int index)
	{
		if (!IsOpen || index < Cancelled || index >= ButtonCount)
		{
			return false;
		}

		Transition.Close();
		return true;
	}

	/// <summary>Advances the slide.</summary>
	/// <param name="deltaTime">Seconds since the last update.</param>
	/// <returns>True on the update the sheet finishes sliding away.</returns>
	public bool Update(float deltaTime) => Transition.Update(deltaTime);
}
