// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using Hexa.NET.ImGui;

using ktsu.ImGui.App.Testing;

using ktsu.ImGui.Widgets.Overlays;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>Drives <see cref="ImGuiWidgets.Tutorial"/> on its own.</summary>
[TestClass]
public sealed class TutorialTests : WidgetTest
{
	private const string Id = "tour";

	private static readonly CoachMarkStep[] Steps =
	[
		new("save", "Save", "Saves your work."),
		new("share", "Share", "Sends it to someone."),
	];

	private readonly OverlayHost host = new();
	private ImGuiWidgets.Tutorial tutorial = null!;
	private bool? finished;
	private int saves;

	private void Draw()
	{
		if (ImGui.Button("Save"))
		{
			saves++;
		}

		Mark("Save");
		tutorial.Target("save");
		ImGui.SameLine();
		ImGui.Button("Share");
		Mark("Share");
		tutorial.Target("share");
		host.Render();
	}

	private static bool Surrounds(Rectangle outer, Rectangle inner) =>
		outer.MinX <= inner.MinX && outer.MinY <= inner.MinY && outer.MaxX >= inner.MaxX && outer.MaxY >= inner.MaxY;

	private void StartTutorial(CoachMarkStep[]? steps = null)
	{
		tutorial = new ImGuiWidgets.Tutorial(host, Id, steps ?? Steps) { Finished = completed => finished = completed };
		Start(Draw);
		tutorial.Start();
		Step(20);
	}

	[TestMethod]
	public void Start_LightsTheFirstTarget()
	{
		StartTutorial();

		Rectangle spotlight = RectOf($"{Id}/spotlight");
		Rectangle save = RectOf("Save");
		Assert.IsTrue(Surrounds(spotlight, save), $"{spotlight} does not surround {save}.");
		Assert.IsTrue(IsVisible($"{Id}/callout"));
	}

	[TestMethod]
	public void Next_MovesToTheNextTarget()
	{
		StartTutorial();

		Click($"{Id}/next");
		Step(30);

		Assert.AreEqual(1, tutorial.StepIndex);
		Assert.IsTrue(Surrounds(RectOf($"{Id}/spotlight"), RectOf("Share")));
	}

	[TestMethod]
	public void Done_OnTheLastStep_FinishesIt()
	{
		StartTutorial();

		Click($"{Id}/next");
		Step(5);
		Click($"{Id}/next");
		Step(20);

		Assert.AreEqual(true, finished);
		Assert.IsFalse(tutorial.IsActive);
		Assert.IsFalse(host.HasOverlays);
	}

	[TestMethod]
	public void Skip_EndsItEarly()
	{
		StartTutorial();

		Click($"{Id}/skip");
		Step(20);

		Assert.AreEqual(false, finished);
		Assert.IsFalse(IsVisible($"{Id}/callout"));
	}

	[TestMethod]
	public void WhileShowing_TheWindowBeneathCannotBeClicked()
	{
		StartTutorial();

		Click("Save");

		Assert.AreEqual(0, saves);
	}

	[TestMethod]
	public void AStepWhoseTargetIsNotDrawn_ShowsItsCalloutWithNothingLit()
	{
		StartTutorial([new("missing", "Somewhere", "Not on screen.")]);

		Assert.IsTrue(IsVisible($"{Id}/callout"));
		Assert.IsFalse(IsVisible($"{Id}/spotlight"));
	}
}
