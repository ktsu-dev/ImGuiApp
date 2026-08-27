// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using System;
using System.Numerics;

using ktsu.ImGui.App;
using ktsu.ImGui.App.Testing;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>Drives <c>ImGuiWidgets.Icon</c> on its own.</summary>
[TestClass]
public sealed class IconTests : WidgetTest
{
	private const string Caption = "Documents";
	private const string Name = "icon";
	private const float IconSize = 48f;

	private ImGuiAppTextureInfo? texture;
	private ImGuiWidgets.IconAlignment alignment = ImGuiWidgets.IconAlignment.Horizontal;
	private int clicks;
	private int doubleClicks;

	private void Draw()
	{
		texture ??= CreateTestTexture();
		ImGuiWidgets.Icon(Caption, texture.TextureId, IconSize, alignment);
		Mark(Name);
	}

	private void DrawWithOptions()
	{
		texture ??= CreateTestTexture();

		ImGuiWidgets.Icon(
			Caption,
			texture.TextureId,
			IconSize,
			alignment,
			new ImGuiWidgets.IconOptions
			{
				OnClick = () => clicks++,
				OnDoubleClick = () => doubleClicks++,
			});

		Mark(Name);
	}

	[TestMethod]
	public void Icon_DrawsTheImageAndItsCaption()
	{
		Start(Draw);

		Rectangle rect = RectOf(Name);

		Assert.IsTrue(rect.Width >= IconSize, $"The icon reserved {rect.Width}px, less than its {IconSize}px image.");
		AssertSomethingWasDrawn("the icon");
	}

	[TestMethod]
	public void Icon_HorizontalAlignment_IsWiderThanItIsTall()
	{
		alignment = ImGuiWidgets.IconAlignment.Horizontal;
		Start(Draw);

		Rectangle rect = RectOf(Name);

		Assert.IsTrue(rect.Width > rect.Height, $"A horizontal icon was {rect.Width}x{rect.Height}.");
	}

	[TestMethod]
	public void Icon_VerticalAlignment_StacksTheCaptionUnderTheImage()
	{
		alignment = ImGuiWidgets.IconAlignment.Vertical;
		Start(Draw);

		Rectangle rect = RectOf(Name);

		Assert.IsTrue(rect.Height > IconSize, $"A vertical icon was only {rect.Height}px tall, no room for a caption.");
	}

	[TestMethod]
	public void Icon_ReportsAClickThroughItsOptions()
	{
		Start(DrawWithOptions);

		Click(Name);

		Assert.AreEqual(1, clicks, "The icon's click callback did not fire exactly once.");
	}

	[TestMethod]
	public void Icon_DoubleClickIsReportedSeparately()
	{
		Start(DrawWithOptions);

		Vector2 center = CenterOf(Name);
		Harness.Mouse.Click(center.X, center.Y);
		Harness.Mouse.Click(center.X, center.Y);
		Step();

		Assert.AreEqual(1, doubleClicks, "Two rapid clicks did not report one double click.");
	}

	[TestMethod]
	public void Icon_LeftAlone_FiresNoCallbacks()
	{
		Start(DrawWithOptions);

		Step(5);

		Assert.AreEqual(0, clicks, "The icon reported a click nobody made.");
		Assert.AreEqual(0, doubleClicks, "The icon reported a double click nobody made.");
	}

	[TestMethod]
	public void CalcIconSize_MatchesWhatTheIconReserves()
	{
		Vector2 calculated = default;

		Start(() =>
		{
			texture ??= CreateTestTexture();
			calculated = ImGuiWidgets.CalcIconSize(
				Caption,
				IconSize,
				ImGuiWidgets.IconAlignment.Horizontal,
				Hexa.NET.ImGui.ImGui.GetStyle().ItemSpacing,
				Hexa.NET.ImGui.ImGui.GetStyle().FramePadding);

			ImGuiWidgets.Icon(Caption, texture.TextureId, IconSize, ImGuiWidgets.IconAlignment.Horizontal);
			Mark(Name);
		});

		Rectangle rect = RectOf(Name);

		Assert.IsTrue(
			Math.Abs(calculated.X - rect.Width) <= 2f,
			$"CalcIconSize reported {calculated.X}px of width for an icon that reserved {rect.Width}px.");
	}
}
