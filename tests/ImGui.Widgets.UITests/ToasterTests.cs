// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using Hexa.NET.ImGui;

using ktsu.ImGui.App.Testing;

using ktsu.ImGui.Widgets.Overlays;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>Drives <see cref="ImGuiWidgets.Toaster"/> on its own.</summary>
[TestClass]
public sealed class ToasterTests : WidgetTest
{
	private const string Id = "toasts";

	private readonly OverlayHost host = new();
	private ImGuiWidgets.Toaster toaster = null!;
	private int buttonClicks;

	private void Draw()
	{
		if (ImGui.Button("Underneath"))
		{
			buttonClicks++;
		}

		Mark("Underneath");
		host.Render();
	}

	private void StartToaster()
	{
		toaster = new ImGuiWidgets.Toaster(host, Id);
		Start(Draw);
	}

	[TestMethod]
	public void Toast_ShowsItsMessage()
	{
		StartToaster();

		toaster.Show("Saved");
		Step(20);

		Assert.IsTrue(IsVisible($"{Id}/message"));
		Assert.AreEqual("Saved", toaster.CurrentMessage);
	}

	[TestMethod]
	public void Toast_GoesAwayByItself()
	{
		StartToaster();

		toaster.Show("Saved", new ToastOptions { Duration = 0.5f });
		Step(80);

		Assert.IsFalse(IsVisible($"{Id}/message"));
		Assert.AreEqual(0, toaster.Count);
		Assert.IsFalse(host.HasOverlays, "An empty toaster should leave the host.");
	}

	[TestMethod]
	public void Toasts_FollowOneAnother()
	{
		StartToaster();

		toaster.Show("First", new ToastOptions { Duration = 0.5f });
		toaster.Show("Second", new ToastOptions { Duration = 0.5f });
		Step(10);
		Assert.AreEqual("First", toaster.CurrentMessage);

		Step(50);
		Assert.AreEqual("Second", toaster.CurrentMessage);
	}

	[TestMethod]
	public void SnackbarButton_RunsItsAction_AndDismisses()
	{
		StartToaster();
		int undone = 0;
		toaster.Show("Deleted", new ToastOptions { Duration = 0f, ActionLabel = "Undo", OnAction = () => undone++ });
		Step(20);

		Click($"{Id}/action");
		Step(20);

		Assert.AreEqual(1, undone);
		Assert.AreEqual(0, toaster.Count);
	}

	[TestMethod]
	public void Toast_LeavesTheRestOfTheWindowClickable()
	{
		StartToaster();
		toaster.Show("Still usable", new ToastOptions { Duration = 0f });
		Step(20);

		Click("Underneath");

		Assert.AreEqual(1, buttonClicks);
	}

	[TestMethod]
	public void Toast_SitsAtTheBottomCentre()
	{
		StartToaster();
		toaster.Show("Where am I?", new ToastOptions { Duration = 0f });
		Step(20);

		Rectangle rect = RectOf(Id);
		Assert.AreEqual(320f, rect.MinX + (rect.Width / 2f), 2f);
		Assert.IsTrue(rect.MaxY is > 400 and < 480, $"The toast ended at {rect.MaxY}.");
	}
}
