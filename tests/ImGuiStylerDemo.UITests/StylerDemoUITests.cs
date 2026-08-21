// Copyright (c) 2023-2026 ktsu-dev contributors

// ImGui contexts are global and the harness refuses to start while another is live, so every test
// in this assembly must have the process to itself.
[assembly: Microsoft.VisualStudio.TestTools.UnitTesting.DoNotParallelize]

namespace ktsu.examples.ImGuiStylerDemo.UITests;

using ktsu.ImGui.App;
using ktsu.ImGui.App.Testing;
using ktsu.ImGui.Examples.Styler;
using ktsu.ThemeProvider;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Drives ImGuiStylerDemo through the headless harness: visits every tab, applies and resets themes
/// from the gallery, and exercises the widget showcase and the validated form.
/// </summary>
[TestClass]
public sealed class StylerDemoUITests
{
	// The demo is a tab bar over long pages, and its lower content sits well past the harness
	// default of 720. A viewport this size keeps the whole of each tab reachable by a click.
	private static readonly HarnessOptions DemoViewport = new() { Width = 1600, Height = 1200 };

	// Emoji are spelled as escapes so the tab labels stay readable next to the surrounding code and
	// survive any editor that would otherwise re-encode them.
	private const string ThemeGalleryTab = "\U0001F3A8 Theme Gallery";
	private const string ColorPalettesTab = "\U0001F3A8 Color Palettes";
	private const string CompletePaletteTab = "\U0001F50D Complete Theme Palette";
	private const string WidgetShowcaseTab = "\U0001F5B1️ Widget Showcase";
	private const string InteractiveTab = "\U0001F4A1 Interactive Examples";
	private const string DocumentationTab = "\U0001F4DA Documentation";

	private static readonly string[] AllTabs =
	[
		ThemeGalleryTab, ColorPalettesTab, CompletePaletteTab,
		WidgetShowcaseTab, InteractiveTab, DocumentationTab,
	];

	private ImGuiAppHarness harness = null!;

	[TestInitialize]
	public void SetUp()
	{
		// Demo state lives in statics that outlive a harness, so each test starts from a known slate.
		ImGuiStylerDemo.ResetState();
		harness = ImGuiAppHarness.Start(ImGuiStylerDemo.BuildConfig(), DemoViewport);
		harness.Step(3);
	}

	[TestCleanup]
	public void TearDown() => harness?.Dispose();

	/// <summary>
	/// Reports whether an item was drawn in the frame just rendered. Probe.Rect remembers the last
	/// position an item ever occupied, so it answers "was this ever drawn" rather than "is this on
	/// screen now" -- and with a tab bar, most items are off screen most of the time.
	/// </summary>
	private bool IsVisible(string name) => harness.Probe.WasSeenInFrame(name, harness.FrameCount - 1);

	private void OpenTab(string tab)
	{
		harness.Click(tab);
		harness.Step(3);
	}

	[TestMethod]
	public void Config_WiresUpTheDemoCallbacks()
	{
		ImGuiAppConfig config = ImGuiStylerDemo.BuildConfig();

		Assert.AreEqual("ImGuiStyler Demo - Comprehensive Theme & Color Showcase", config.Title);
		Assert.IsNotNull(config.OnRender);
		Assert.IsNotNull(config.OnAppMenu);
		Assert.IsNotNull(config.OnStart);
		Assert.IsNotNull(config.FrameWrapperFactory, "The demo previews themes through a frame wrapper.");
		Assert.IsFalse(config.SaveIniSettings);
	}

	[TestMethod]
	public void EveryTab_IsRendered()
	{
		foreach (string tab in AllTabs)
		{
			Assert.IsTrue(IsVisible(tab), $"Tab '{tab}' was never rendered.");
		}
	}

	[TestMethod]
	public void EveryTab_CanBeOpenedWithoutError()
	{
		foreach (string tab in AllTabs)
		{
			OpenTab(tab);

			Assert.IsTrue(IsVisible(tab), $"Tab '{tab}' vanished after being selected.");
			Assert.IsNotNull(harness.Capture().FindBounds(p => p.A > 0), $"Tab '{tab}' rendered a blank frame.");
		}
	}

	[TestMethod]
	public void ThemeGallery_ShowsItsPreviewWidgets()
	{
		OpenTab(ThemeGalleryTab);

		// The preview lives in a child window, so these names are qualified by it; matching on the
		// trailing segment keeps the test off ImGui's generated child-window id suffix.
		foreach (string widget in new[]
		{
			"Sample Button", "Sample Checkbox", "Sample Slider", "Sample Input",
			"Option 1", "Option 2", "Option 3",
		})
		{
			Assert.IsTrue(IsVisible(widget), $"The theme preview is missing '{widget}'.");
		}
	}

	[TestMethod]
	public void ThemeGallery_OffersEveryRegisteredTheme()
	{
		OpenTab(ThemeGalleryTab);

		Assert.IsGreaterThan(0, ImGuiStylerDemo.availableThemes.Count, "The demo found no themes to show.");

		// The grid scrolls, so only the first screenful is drawn. Proving the first few cards are
		// addressable is enough to show the gallery is wired to the registry.
		foreach (ThemeRegistry.ThemeInfo theme in ImGuiStylerDemo.availableThemes.Take(3))
		{
			Assert.IsTrue(IsVisible($"theme-card/{theme.Name}"), $"Theme card '{theme.Name}' was never drawn.");
		}
	}

	[TestMethod]
	public void ThemeGallery_ClickingACardAppliesThatTheme()
	{
		OpenTab(ThemeGalleryTab);
		ThemeRegistry.ThemeInfo theme = ImGuiStylerDemo.availableThemes[0];

		harness.Click($"theme-card/{theme.Name}");
		harness.Step(3);

		Assert.IsNotNull(ImGuiStylerDemo.currentSelectedTheme, "Clicking a theme card should select a theme.");
		Assert.AreEqual(theme.Name, ImGuiStylerDemo.currentSelectedTheme!.Name);
	}

	[TestMethod]
	public void ThemeGallery_ApplyingAThemeChangesWhatIsDrawn()
	{
		OpenTab(ThemeGalleryTab);
		byte[] beforeTheme = harness.Target.Pixels.ToArray();

		harness.Click($"theme-card/{ImGuiStylerDemo.availableThemes[0].Name}");
		harness.Step(3);

		Assert.IsFalse(beforeTheme.AsSpan().SequenceEqual(harness.Target.Pixels),
			"Applying a theme should visibly restyle the demo.");
	}

	[TestMethod]
	public void ThemeGallery_ResetToDefaultClearsTheSelection()
	{
		OpenTab(ThemeGalleryTab);
		harness.Click($"theme-card/{ImGuiStylerDemo.availableThemes[0].Name}");
		harness.Step(3);
		Assert.IsNotNull(ImGuiStylerDemo.currentSelectedTheme, "Precondition: a theme is applied.");

		harness.Click("Reset to Default");
		harness.Step(3);

		Assert.IsNull(ImGuiStylerDemo.currentSelectedTheme, "Reset should return the demo to default styling.");
	}

	[TestMethod]
	public void ThemeGallery_FamilyFilterIsOffered()
	{
		OpenTab(ThemeGalleryTab);

		Assert.IsTrue(IsVisible("Family"), "The family filter was never drawn.");
		Assert.IsGreaterThan(0, ImGuiStylerDemo.availableFamilies.Count, "The demo found no theme families.");
	}

	[TestMethod]
	public void WidgetShowcase_RendersEveryControl()
	{
		OpenTab(WidgetShowcaseTab);

		foreach (string widget in new[]
		{
			"Standard Button", "Checkbox", "Checkbox 2",
			"Radio A", "Radio B", "Radio C",
			"Slider", "Slider 2", "Int Slider", "Text Input", "Color",
		})
		{
			Assert.IsTrue(IsVisible(widget), $"The widget showcase is missing '{widget}'.");
		}
	}

	[TestMethod]
	public void WidgetShowcase_ControlsRespondToClicks()
	{
		OpenTab(WidgetShowcaseTab);
		byte[] beforeClicking = harness.Target.Pixels.ToArray();

		harness.Click("Checkbox 2");
		harness.Step(2);

		Assert.IsFalse(beforeClicking.AsSpan().SequenceEqual(harness.Target.Pixels),
			"Toggling a checkbox should change what is drawn.");
	}

	[TestMethod]
	public void WidgetShowcase_RadioSelectionMoves()
	{
		OpenTab(WidgetShowcaseTab);

		harness.Click("Radio C");
		harness.Step(2);
		byte[] afterC = harness.Target.Pixels.ToArray();

		harness.Click("Radio A");
		harness.Step(2);

		Assert.IsFalse(afterC.AsSpan().SequenceEqual(harness.Target.Pixels),
			"Moving the radio selection should change what is drawn.");
	}

	[TestMethod]
	public void InteractiveExamples_RendersTheThemedControls()
	{
		OpenTab(InteractiveTab);

		foreach (string widget in new[]
		{
			"Normal Button", "Normal Slider", "Danger Button", "Success Button",
			"Themed Button", "Small Themed", "Themed Checkbox", "Themed Slider", "Normal Button Again",
		})
		{
			Assert.IsTrue(IsVisible(widget), $"The interactive examples are missing '{widget}'.");
		}
	}

	[TestMethod]
	public void ValidatedForm_StartsDisabledWhileEmpty()
	{
		OpenTab(InteractiveTab);

		Assert.AreEqual("", ImGuiStylerDemo.formUsername);
		Assert.AreEqual("", ImGuiStylerDemo.formEmail);
		Assert.IsTrue(IsVisible("Submit (Complete form first)"),
			"An empty form should offer the disabled submit button.");
	}

	[TestMethod]
	public void ValidatedForm_AcceptingValidInputEnablesSubmission()
	{
		OpenTab(InteractiveTab);

		// Both fields live in the FormExample child window, and the same labels appear again in the
		// main window further down the tab, so the child qualifies the name for us.
		string username = harness.Probe.Matches("Username").Single(n => n.Contains("FormExample", StringComparison.Ordinal));
		string email = harness.Probe.Matches("Email").Single(n => n.Contains("FormExample", StringComparison.Ordinal));

		harness.Click(username);
		harness.Keyboard.Type("testuser");
		harness.Step(2);

		harness.Click(email);
		harness.Keyboard.Type("test@example.com");
		harness.Step(3);

		Assert.AreEqual("testuser", ImGuiStylerDemo.formUsername);
		Assert.AreEqual("test@example.com", ImGuiStylerDemo.formEmail);
		Assert.IsTrue(IsVisible("Submit Registration"),
			"A valid username and email should enable submission.");
		Assert.IsFalse(IsVisible("Submit (Complete form first)"),
			"The disabled submit button should be gone once the form validates.");
	}

	[TestMethod]
	public void ValidatedForm_RejectsAnAddressWithoutAnAtSign()
	{
		OpenTab(InteractiveTab);

		string username = harness.Probe.Matches("Username").Single(n => n.Contains("FormExample", StringComparison.Ordinal));
		string email = harness.Probe.Matches("Email").Single(n => n.Contains("FormExample", StringComparison.Ordinal));

		harness.Click(username);
		harness.Keyboard.Type("testuser");
		harness.Step(2);

		harness.Click(email);
		harness.Keyboard.Type("not-an-address");
		harness.Step(3);

		Assert.IsTrue(IsVisible("Submit (Complete form first)"),
			"An invalid address must leave submission disabled.");
	}

	[TestMethod]
	public void ColorPalettes_RendersItsCustomColorControl()
	{
		OpenTab(ColorPalettesTab);

		Assert.IsTrue(IsVisible("Custom Color"), "The color palette tab is missing its color editor.");
	}

	[TestMethod]
	public void CompleteThemePalette_DrawsSomething()
	{
		// The tab is a read-only swatch sheet with no addressable controls, so the claim worth
		// making is that selecting it produces a populated frame rather than an empty one.
		OpenTab(CompletePaletteTab);

		Assert.IsGreaterThan(10000, harness.Capture().CountPixels(p => p.A > 0),
			"The complete palette should fill the page with swatches.");
	}

	[TestMethod]
	public void Documentation_DrawsSomething()
	{
		OpenTab(DocumentationTab);

		Assert.IsGreaterThan(10000, harness.Capture().CountPixels(p => p.A > 0),
			"The documentation tab should render its text.");
	}

	[TestMethod]
	public void Demo_RendersIdenticallyAcrossRuns()
	{
		byte[] first = harness.Target.Pixels.ToArray();

		harness.Dispose();
		ImGuiStylerDemo.ResetState();
		harness = ImGuiAppHarness.Start(ImGuiStylerDemo.BuildConfig(), DemoViewport);
		harness.Step(3);

		CollectionAssert.AreEqual(first, harness.Target.Pixels.ToArray(), "Two runs of the same scenario should match.");
	}
}
