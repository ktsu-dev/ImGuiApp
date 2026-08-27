// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using System;

using ktsu.ImGui.App;
using ktsu.ImGui.App.Testing;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>Drives <c>ImGuiWidgets.Avatar</c> on its own.</summary>
[TestClass]
public sealed class AvatarTests : WidgetTest
{
	private const string Id = "profile";

	private AvatarStatus status = AvatarStatus.None;
	private float diameter;
	private bool clicked;

	private void DrawInitialsAvatar(string displayName) =>
		clicked |= ImGuiWidgets.Avatar(Id, displayName, diameter, status);

	[TestMethod]
	public void Avatar_IsDrawnAndMarksItself()
	{
		Start(() => DrawInitialsAvatar("Grace Hopper"));

		Assert.IsTrue(IsVisible(Id), "The avatar marked no probe item.");
		AssertSomethingWasDrawn("the avatar");
	}

	[TestMethod]
	public void Avatar_IsCircularAtTheRequestedDiameter()
	{
		diameter = 72f;
		Start(() => DrawInitialsAvatar("Grace Hopper"));

		Rectangle rect = RectOf(Id);

		Assert.IsTrue(Math.Abs(rect.Width - 72) <= 2, $"The avatar reserved {rect.Width}px rather than 72.");
		Assert.AreEqual(rect.Width, rect.Height, "The avatar's area was not square.");
	}

	[TestMethod]
	public void Avatar_ReportsAClick()
	{
		Start(() => DrawInitialsAvatar("Grace Hopper"));

		Click(Id);

		Assert.IsTrue(clicked, "The avatar did not report a click.");
	}

	[TestMethod]
	public void Avatar_DrawsInitialsForADisplayName()
	{
		Start(() => DrawInitialsAvatar("Grace Hopper"));
		MoveAway();
		byte[] withInitials = Snapshot();

		// A different name means different initials and a different derived background color.
		DisposeHarness();
		Start(() => DrawInitialsAvatar("Ada Lovelace"));
		MoveAway();

		Assert.IsTrue(PixelsChangedSince(withInitials) > 0, "Two different names drew the same avatar.");
	}

	[TestMethod]
	public void Avatar_StatusDotChangesWhatIsDrawn()
	{
		status = AvatarStatus.None;
		Start(() => DrawInitialsAvatar("Grace Hopper"));
		MoveAway();
		byte[] withoutDot = Snapshot();

		status = AvatarStatus.Online;
		Step(2);
		MoveAway();

		Assert.IsTrue(PixelsChangedSince(withoutDot) > 0, "An online avatar drew no status dot.");
	}

	[TestMethod]
	public void Avatar_TextureOverload_DrawsTheImage()
	{
		// The texture belongs to the renderer the harness installs, so it is created on the first
		// frame rather than before the harness exists.
		ImGuiAppTextureInfo? texture = null;

		Start(() =>
		{
			texture ??= CreateTestTexture();
			ImGuiWidgets.Avatar(Id, texture.TextureId, 64f);
		});

		Assert.IsTrue(IsVisible(Id), "The texture avatar marked no probe item.");
		AssertSomethingWasDrawn("the texture avatar");
	}

	[TestMethod]
	public void Initials_AbbreviatesADisplayName()
	{
		Assert.AreEqual("GH", ImGuiWidgets.Initials("Grace Hopper"));
		Assert.AreEqual("A", ImGuiWidgets.Initials("Ada"));
		Assert.AreEqual("?", ImGuiWidgets.Initials("   "));
	}
}
