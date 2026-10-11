// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using Hexa.NET.ImGui;

using ktsu.ImGui.App;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// The scale the widgets measure their pixel lengths by, against <see cref="ImGuiApp.SetGlobalScale"/>:
/// the font, the style's spacing and <see cref="InterfaceScale.Current"/> all grow by the same factor.
/// </summary>
[TestClass]
public sealed class InterfaceScaleTests : WidgetTest
{
	private float fontSize;
	private float itemSpacing;
	private float scale;

	private void Draw()
	{
		fontSize = ImGui.GetFontSize();
		itemSpacing = ImGui.GetStyle().ItemSpacing.X;
		scale = InterfaceScale.Current;
		ImGui.TextUnformatted("scaled");
	}

	[TestCleanup]
	public void ResetScale() => ImGuiApp.SetGlobalScale(1f);

	[TestMethod]
	public void GlobalScale_ScalesTheFontAndTheSpacingByTheSameFactor()
	{
		Start(Draw);
		(float font, float spacing) = (fontSize, itemSpacing);
		Assert.AreEqual(1f, scale, 1e-3f, "At 100% the widgets should draw at the size they were designed.");

		ImGuiApp.SetGlobalScale(2f);
		Step(2);

		Assert.AreEqual(font * 2f, fontSize, 1f);
		Assert.AreEqual(spacing * 2f, itemSpacing, 0.01f, "The spacing should grow with the font, not by the square of the scale.");
		Assert.AreEqual(2f, scale, 0.05f);
	}
}
