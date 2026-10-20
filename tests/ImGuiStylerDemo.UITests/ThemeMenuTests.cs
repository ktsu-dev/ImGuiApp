// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.examples.ImGuiStylerDemo.UITests;

using System.Linq;

using Hexa.NET.ImGui;

using ktsu.ImGui.App;
using ktsu.ImGui.App.Testing;
using ktsu.ImGui.App.Testing.Support;
using ktsu.ImGui.Probes;
using ktsu.ImGui.Styler;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Drives <see cref="Theme.RenderThemeSelectorMenu"/> and the theme browser on their own, with a theme applied,
/// so the lines that name the current theme are drawn. Those lines are written with <c>TextUnformatted</c>
/// rather than the printf-style <c>ImGui.Text</c> (#670).
/// </summary>
[TestClass]
public sealed class ThemeMenuTests
{
	private const string MenuLabel = "Theme";
	private const string MenuProbe = "theme-menu";

	private ImGuiAppHarness harness = null!;

	/// <summary>Gets or sets the context MSTest supplies for the running test.</summary>
	public TestContext TestContext { get; set; } = null!;

	[TestInitialize]
	public void SetUp()
	{
		harness = ImGuiAppHarness.Start(
			new ImGuiAppConfig
			{
				Title = nameof(ThemeMenuTests),
				SaveIniSettings = false,
				OnAppMenu = () =>
				{
					Theme.RenderThemeSelectorMenu(MenuLabel);
					ImGuiProbes.MarkItem(MenuProbe);
				},
				OnRender = _ => Theme.RenderThemeSelector(),
			},
			new HarnessOptions { Width = 1280, Height = 900 });

		// Applying a theme writes the ImGui style, so it needs the harness's context.
		string themeName = Theme.AllThemes[0].Name;
		Assert.IsTrue(Theme.Apply(themeName), $"Theme '{themeName}' could not be applied.");
		harness.Step(3);
	}

	[TestCleanup]
	public void TearDown()
	{
		TestContext.KeepFailureFrame(harness);
		Theme.ResetToDefault();
		harness?.Dispose();
	}

	[TestMethod]
	public void TheMenuNamesTheAppliedTheme()
	{
		byte[] closed = harness.Target.Pixels.ToArray();

		harness.Click(MenuProbe);
		harness.Step(3);

		Assert.IsFalse(closed.SequenceEqual(harness.Target.Pixels.ToArray()), "Clicking the theme menu did not open it.");
	}

	[TestMethod]
	public void TheBrowserNamesTheAppliedTheme()
	{
		byte[] closed = harness.Target.Pixels.ToArray();

		Theme.ShowThemeSelector();
		harness.Step(3);

		Assert.IsFalse(closed.SequenceEqual(harness.Target.Pixels.ToArray()), "The theme browser did not appear.");

		harness.Keyboard.Press(ImGuiKey.Escape);
		harness.Step(2);
	}
}
