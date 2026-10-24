## v3.78.1 (patch)

Changes since v3.78.0:

- [patch] Reopen a window saved maximized as maximized (#725) (matt-edmondson)
- [patch] Reopen a window saved maximized as maximized (Claude)

## v3.78.0 (minor)

Changes since v3.77.0:

- [minor] Present in HDR on Windows and macOS, with half-float textures (#723) (matt-edmondson)
- Say how to cut the replacement release, since a Markdown-only commit starts no CI (Claude)
- Note how a half-failed release republishes the same version [patch] (Claude)
- Answer the three new Sonar findings on the repeated-masks fix (Claude)
- Read the bitmap Windows makes from a copied image the right way up [patch] (Claude)
- Test the image clipboard against real desktops, and stop xclip holding the app's output [patch] (Claude)

## v3.77.3 (patch)

Changes since v3.77.2:

- Say how to cut the replacement release, since a Markdown-only commit starts no CI (Claude)

## v3.77.2 (patch)

Changes since v3.77.1:

- Note how a half-failed release republishes the same version [patch] (Claude)

## v3.77.1 (patch)

Changes since v3.77.0:

- Answer the three new Sonar findings on the repeated-masks fix (Claude)
- Read the bitmap Windows makes from a copied image the right way up [patch] (Claude)
- Test the image clipboard against real desktops, and stop xclip holding the app's output [patch] (Claude)

## v3.77.0 (minor)

Changes since v3.76.0:

- Keep the file browser inside the window it opens in [patch] (#718) (matt-edmondson)
- Put OpenGLRenderer's GL calls behind a seam and test it with real frames (Claude)
- Make OpenGLRenderer's GL field readonly (Claude)
- Split ImGuiController into a GL-free context host and an OpenGL renderer (Claude)

## v3.76.1 (patch)

Changes since v3.76.0:

- Keep the file browser inside the window it opens in [patch] (#718) (matt-edmondson)

## v3.76.0 (minor)

Changes since v3.75.0:

- Give ImGui.Rendering the package validation suppressions the others carry [patch] (#717) (matt-edmondson)
- Clear main's quality gate: tolerances instead of exact float comparisons (#716) (matt-edmondson)
- [minor] FilesystemBrowser: breadcrumbs, back/forward, places, search, sortable columns and thumbnails (#715) (matt-edmondson)
- [minor] Two-finger touch scrolling, extension docs, and trim analysis for Widgets/Popups/Styler (#703) (matt-edmondson)
- Merge main into claude/project-thread-m8gl9z (matt-edmondson)
- [minor] Add the phase 4 mobile polish widgets (#710) (matt-edmondson)
- [minor] Add the phase 3 mobile overlay and navigation widgets (#709) (matt-edmondson)
- Pick a curve point's fill in its own method rather than a nested conditional (Claude)
- [minor] Add the phase 2 mobile gesture widgets (#708) (matt-edmondson)
- Fix Escape in nested modals, path text in the save box, and Enter on a search with no match [patch] (#693) (matt-edmondson)
- [minor] Add LogView, a virtualized log with level filters and search (#706) (matt-edmondson)
- Keep the active frame rate while an animation runs [minor] (#692) (matt-edmondson)
- [minor] Mark every widget for UI probes (#707) (matt-edmondson)
- Add ImGuiWidgets.Viewport3D and move the 3D contract into ktsu.ImGui.Rendering [minor] (#705) (matt-edmondson)
- Merge remote-tracking branch 'origin/main' into fix699 (matt-edmondson)
- [patch] Fix GradientEditor selection on reorder and two TransformBox bugs (#690) (matt-edmondson)
- [patch] Fix three SyntaxHighlighting tokenizer bugs that recolored the rest of a document (#691) (matt-edmondson)
- Compare curve range spans against epsilon rather than zero (Claude)
- Reach long lines in code blocks and diff hunks, draw only visible code lines, keep rebuilt hunks collapsed [patch] (#704) (matt-edmondson)
- Dim GradientEditor, SwatchPalette and themed code inside a disabled scope [patch] (#701) (matt-edmondson)
- Draw the curve and bezier editors with ImGui and drop Hexa Extras [minor] (Claude)
- Stop a hidden DataTable column from taking edits or filtering rows (#700) (matt-edmondson)
- Node editor: re-measure rebuilt graphs, edit long pins, pin nodes from the UI [minor] (Claude)
- Fix markdown fonts, link IDs, linked images, image placeholders and code spans [patch] (#698) (matt-edmondson)
- ReorderableTree: let the last child leave its group, and leave slider drags to the slider (#697) (matt-edmondson)
- Save the last frame of a failing UI test, and cover CapturedFrame and the frame wrapper order (#696) (matt-edmondson)
- Fix the extra step after a held repeat button and snap marks to frames [patch] (#695) (matt-edmondson)
- Honour markdown table column alignment and scroll long code lines [patch] (#694) (matt-edmondson)
- Read TGA colour map entries with the descriptor's attribute bits and treat an all-zero 32-bit map as opaque [patch] (#689) (matt-edmondson)
- Merge main into fix/680-knob-visible-title (matt-edmondson)
- Show a Knob's title without its ## ID suffix (Claude)
- Bound a JPEG's coefficient allocation by its scan data instead of its frame header [patch] (Claude)
- Refuse a TGA header with no pixel data before allocating the image [patch] (Claude)
- Drive the theme menu and browser with a theme applied (Claude)
- Suppress S6640 on FormatValue, as the Knob's other native interop is (Claude)
- Draw caller and theme text as it is instead of as a printf format [patch] (Claude)
- Move the new Knob tests to the end of the class so they don't touch the lines #673 adds to (Claude)
- Show the Knob's value in its tooltip, and drop the stray "f" from int knobs [patch] (Claude)
- Round a Knob drag without throwing on a format of more than 6 decimals [patch] (Claude)
- Read the Knob's format precision without throwing on "%.f" or a truncated format [patch] (Claude)
- Merge main into the TGA first-entry branch (Claude)
- Merge main into the hard line break branch (Claude)
- End the line at a Markdown hard line break [patch] (Claude)
- Pick only clipboard image types the decoder can read on Linux [patch] (Claude)
- Name Tab as Tab in KeyChordMatcher, not its NamedKeyBegin alias [patch] (Claude)
- Honour the TGA colour map's first entry index [patch] (Claude)
- Move the JPEG coefficient bound into a helper (Claude)
- Reject oversized image headers as InvalidImageDataException [patch] (Claude)
- Decode 16-bit greyscale TGA as grey plus alpha instead of RGB555 [patch] (Claude)
- Write a true minus sign on a value slider's face (#645) [patch] (matt-edmondson)
- Split the glued-word grouping into tokenizing and grouping (matt-edmondson)
- Stop Markdown inserting a space where inline runs touch [patch] (matt-edmondson)
- Let the syntax highlighting packages pass package validation (#638) [patch] (matt-edmondson)
- Let the syntax highlighting packages pass package validation [patch] (Claude)
- Report node size changes made while the node editor is zoomed (#637) (matt-edmondson)
- Report node size changes made while the node editor is zoomed (Claude)
- Keep a DiffView hunk's collapse with that hunk, not its position [patch] (Claude)

## v3.75.3 (patch)

Changes since v3.75.2:

- Pick only clipboard image types the decoder can read on Linux [patch] (Claude)
- Name Tab as Tab in KeyChordMatcher, not its NamedKeyBegin alias [patch] (Claude)
- Move the JPEG coefficient bound into a helper (Claude)
- Reject oversized image headers as InvalidImageDataException [patch] (Claude)
- Decode 16-bit greyscale TGA as grey plus alpha instead of RGB555 [patch] (Claude)
- Split the glued-word grouping into tokenizing and grouping (matt-edmondson)
- Stop Markdown inserting a space where inline runs touch [patch] (matt-edmondson)

## v3.75.2 (patch)

Changes since v3.75.1:

- Write a true minus sign on a value slider's face (#645) [patch] (matt-edmondson)

## v3.75.2-pre.1 (prerelease)

Changes since v3.75.1:

- Bump Markdig from 1.4.0 to 1.4.1 (dependabot[bot])

## v3.75.1 (patch)

Changes since v3.75.0:

- Let the syntax highlighting packages pass package validation (#638) [patch] (matt-edmondson)
- Let the syntax highlighting packages pass package validation [patch] (Claude)
- Report node size changes made while the node editor is zoomed (#637) (matt-edmondson)
- Report node size changes made while the node editor is zoomed (Claude)
- Keep a DiffView hunk's collapse with that hunk, not its position [patch] (Claude)

## v3.75.0 (minor)

Changes since v3.74.0:

- [minor] Copy and paste images through the system clipboard (#631) (matt-edmondson)

## v3.74.0 (minor)

Changes since v3.73.0:

- [minor] Keep a value slider's number in one color, and name arrows and color plainly (#629) (matt-edmondson)

## v3.73.0 (minor)

Changes since v3.72.0:

- [minor] Publish the chord matcher, interface scale, linear colour edits, prompt answers, overwrite confirmation and crop guides (#628) (matt-edmondson)
- [patch] Stop ValueSlider reporting a change on held frames that move nothing (Claude)

## v3.72.1 (patch)

Changes since v3.72.0:

- [patch] Stop ValueSlider reporting a change on held frames that move nothing (Claude)

## v3.72.0 (minor)

Changes since v3.71.0:

- Add a prefix option to ValueSlider for labels inside the frame (Claude)
- [minor] Add a logarithmic mode to ValueSlider and keep its number clear near zero (Claude)

## v3.71.0 (minor)

Changes since v3.70.0:

- Merge remote-tracking branch 'origin/main' into claude/project-thread-9lzc74 (Claude)
- Compare slider values bit for bit rather than with Equals (Claude)
- [minor] Add ImGuiApp.EnableExtensions to turn on ImGui extensions after launch (Claude)
- Guard the bipolar fill test's difference bounds with a pattern (Claude)
- [minor] Add ValueSlider, a slider whose number stays readable (Claude)
- Match the added font with a pattern rather than .Value (Claude)
- Drop an unneeded unsafe modifier (Claude)
- Let an application draw its interface in another face [minor] (Claude)
- Let an application leave out the built-in Accessibility and Debug menus [minor] (Claude)

## v3.70.0 (minor)

Changes since v3.69.0:

- Drop a cref to a type the iOS build excludes (Claude)
- Let an application choose which ImGui extensions are set up [minor] (Claude)
- Document the interface scale (Claude)
- [patch] Apply the global scale to spacing once, and scale TransformBox and GradientEditor handles (Claude)

## v3.69.1 (patch)

Changes since v3.69.0:

- Document the interface scale (Claude)
- [patch] Apply the global scale to spacing once, and scale TransformBox and GradientEditor handles (Claude)

## v3.69.0 (minor)

Changes since v3.68.0:

- [minor] Distort the transform box by Ctrl+Shift-dragging a corner (Claude)

## v3.68.0 (minor)

Changes since v3.67.0:

- [minor] Let a host draw GradientEditor's stop colour editor (Claude)

## v3.67.0 (minor)

Changes since v3.66.0:

- [minor] Deliver files dropped onto the window to OnFilesDropped (#614) (matt-edmondson)
- Stop a divider handle taking clicks meant for a popup over it (#613) (matt-edmondson)
- Guard the skew divisions by magnitude rather than float equality (Claude)
- [minor] Skew the transform box by Ctrl-dragging an edge (Claude)
- Leave the Silk.NET file-drop wiring out of the iOS head (Claude)
- Deliver files dropped onto the window to OnFilesDropped [minor] (Claude)

## v3.66.0 (minor)

Changes since v3.65.0:

- Cover Show() reaching the windowing backend without a Win32 handle (Claude)
- Put the imm32 calls behind a seam and test what the Win32 target sends (Claude)
- Bring the window forward from Show() on Linux and macOS too (Claude)
- Read the harness's IME placement through a checked local (Claude)
- Leave io.WantTextInput alone when a widget reports its caret (Claude)
- [minor] Place the input method's candidate window at the text caret (Claude)

## v3.65.0 (minor)

Changes since v3.64.0:

- Fix the widget gallery tiles and four widget defects they exposed (Claude)
- [patch] Keep comment boxes with their nodes when the view pans or fits (Claude)
- [patch] Draw HTML entities in Markdown as their characters, not a type name (Claude)

## v3.64.2-pre.1 (prerelease)

Changes since v3.64.1:

- Bump the ktsu group with 9 updates (dependabot[bot])

## v3.64.1 (patch)

Changes since v3.64.0:

- [patch] Keep comment boxes with their nodes when the view pans or fits (Claude)
- [patch] Draw HTML entities in Markdown as their characters, not a type name (Claude)

## v3.64.1-pre.1 (prerelease)

Changes since v3.64.0:

- Bump the ktsu group with 5 updates (dependabot[bot])

## v3.64.0 (minor)

Changes since v3.63.0:

- Answer the code-quality notes on the deferred rasterization tests (Claude)
- Rasterize headless frames only when their pixels are read [minor] (Claude)

## v3.63.0 (minor)

Changes since v3.62.0:

- Cover the controller's 3D forwards and exclude the GL pass-through from coverage (Claude)
- Keep a DockedWindow registered when it is collapsed or tabbed behind another [patch] (Claude)
- Implement IRenderer3D on the OpenGL backend [minor] (Claude)

## v3.62.1-pre.1 (prerelease)

Changes since v3.62.0:

- Bump MSTest.Sdk from 4.4.1 to 4.5.1 (dependabot[bot])
- Bump the ktsu group with 7 updates (dependabot[bot])

## v3.62.0 (minor)

Changes since v3.61.0:

- [minor] Show the mouse cursor widgets ask for (Claude)

## v3.61.0 (minor)

Changes since v3.60.0:

- Simplify a negated comparison in TransformBoxState (Claude)
- Add TransformBox, move and scale handles on a rectangle in its own frame [minor] (Claude)
- [patch] Cover the taskbar icon's update-tick hook (matt-edmondson)
- [patch] Show the window icon on the Windows taskbar (matt-edmondson)

## v3.60.1 (patch)

Changes since v3.60.0:

- [patch] Cover the taskbar icon's update-tick hook (matt-edmondson)
- [patch] Show the window icon on the Windows taskbar (matt-edmondson)

## v3.60.0 (minor)

Changes since v3.59.0:

- Read the resolved drop through a non-null local in the tree state tests (Claude)
- Add ReorderableTree, a drag-reorderable tree of caller-drawn rows [minor] (Claude)
- Drop the redundant ToArray and cast that MSTest.Sdk 4.4.1's analyzers reject (Claude)
- Merge remote-tracking branch 'origin/main' into dependabot/nuget/MSTest.Sdk-4.4.1 (Claude)
- [patch] Run the widget gallery from CI rather than workflow_run (Claude)

## v3.59.2 (patch)

Changes since v3.59.1:

- Drop the redundant ToArray and cast that MSTest.Sdk 4.4.1's analyzers reject (Claude)
- Merge remote-tracking branch 'origin/main' into dependabot/nuget/MSTest.Sdk-4.4.1 (Claude)

## v3.59.2-pre.3 (prerelease)

Changes since v3.59.2-pre.2:

- Bump Polyfill from 11.4.1 to 11.4.3 (dependabot[bot])
- Bump the ktsu group with 2 updates (dependabot[bot])

## v3.59.2-pre.2 (prerelease)

Changes since v3.59.2-pre.1:

- Test that Enter with nothing typed and no choice confirms the first item ([@Copilot](https://github.com/Copilot))
- Re-rank SearchableList as the user types and confirm the best match on Enter ([@Copilot](https://github.com/Copilot))
- Initial plan ([@Copilot](https://github.com/Copilot))

## v3.59.2-pre.1 (prerelease)

Changes since v3.59.1:

- Bump the ktsu group with 4 updates (dependabot[bot])

## v3.59.1 (patch)

Changes since v3.59.0:

- [patch] Run the widget gallery from CI rather than workflow_run (Claude)

## v3.59.0 (minor)

Changes since v3.58.0:

- Merge remote-tracking branch 'origin/main' into claude/project-thread-5le24s (Claude)
- Add GlyphSize to toolbars and toolbar buttons (Claude)
- Centre toolbar glyphs by their drawn pixels, and fix the gallery tile's Bold icon (Claude)

## v3.58.0 (minor)

Changes since v3.57.0:

- Merge branch 'widget/stack-loupe' into claude/project-thread-wmrk2b (Claude)
- Pick a bar colour without a nested ternary (Claude)
- Merge branch 'widget/stack-crop' into widget/stack-loupe (Claude)
- Merge branch 'widget/stack-compare' into widget/stack-crop (Claude)
- Compare ImageCompare's split by its bits, not float equality (Claude)
- Clamp SwatchPalette's floored index with a float constant (Claude)
- Merge branch 'widget/stack-swatch' into widget/stack-compare (Claude)
- Compare GradientEditor's stop positions by their bits, not float equality (Claude)
- Merge branch 'widget/stack-gradient' into widget/stack-swatch (Claude)
- Put the stack's demo tests back where the base keeps them (Claude)
- Merge the branch below into this one, bringing in main (Claude)
- Merge the branch below into this one, bringing in main (Claude)
- Merge the branch below into this one, bringing in main (Claude)
- Merge the branch below into this one, bringing in main (Claude)
- Merge the branch below into this one, bringing in main (Claude)
- Merge main into the gradient editor branch, now that the Toolbar has landed (Claude)
- Add FrameTimeGraph and FrameTimeHistory [minor] (Claude)
- [minor] Add Toolbar and ToolbarButton widgets (Claude)
- Add PixelLoupe: a magnified pixel readout beside an ImageCanvas (Claude)
- Add CropOverlay: a crop rectangle drawn over an ImageCanvas (Claude)
- Add ImageCompare: a before/after wipe or side-by-side view (Claude)
- [minor] Add SwatchPalette: a selectable, drag-reorderable grid of colour swatches (Claude)
- Add GradientEditor, a colour-stop gradient editor (Claude)
- [minor] Add AssetBrowser, a virtualized thumbnail grid with multi-select and drag-out (Claude)
- Add StepGrid step-sequencer widget [minor] (Claude)
- Add PianoKeyboard widget (#503) (Claude)
- Add LevelsControl, a histogram with input, gamma and output handles (Claude)
- Add ChannelFader: audio-taper gain fader with a meter [minor] (Claude)

## v3.57.0 (minor)

Changes since v3.56.0:

- Regenerate the widget gallery after CI rather than on the push (Claude)
- Regenerate the widget gallery on pushes to main (Claude)
- Merge main into the widget branch, registering its demo as a DemoSections entry (Claude)
- Restore the worktree ignore lines the SDK build dropped from .gitignore (Claude)
- Merge main into the stereo meters branch, bringing in ParametricEq (Claude)
- Merge main into the demo split, registering ParametricEqDemo as its own section (Claude)
- [patch] Map symbol-spelled chord keys and pin Tab to the named-key bound (Claude)
- Merge main's metadata update (Claude)
- Merge main into claude/project-thread-wmrk2b (Claude)
- Merge main into the demo split, registering TimecodeFieldDemo in DemoSections (Claude)
- Merge main into the parametric EQ branch, adding TimecodeField to the widget list (Claude)
- Merge main into claude/project-thread-g9k1ol-parametric-eq (Claude)
- Merge main into claude/project-thread-wmrk2b (Claude)
- Merge main into the demo split, registering TransportScrubberDemo in DemoSections (Claude)
- Restore the worktree ignore lines the merge from main dropped (Claude)
- Merge remote-tracking branch 'origin/main' into claude/project-thread-g9k1ol-parametric-eq (Claude)
- Merge remote-tracking branch 'origin/main' into claude/project-thread-wmrk2b (Claude)
- Merge remote-tracking branch 'origin/main' into claude/project-thread-026l9x (Claude)
- Merge main into the parametric-eq branch (Claude)
- Expect the data table in the registry's advanced demo order (Claude)
- Merge main into the demo split branch (Claude)
- Compare whole EqBands rather than floats for change detection (Claude)
- Address code-quality findings on the stereo meters (Claude)
- Reuse one StringBuilder when generating the grid demo's strings [patch] (Claude)
- Simplify ParametricEq guards flagged by code scanning (Claude)
- Add ParametricEq widget and extract LogFrequencyAxis (#505) (Claude)
- Add StereoMeters: gain-reduction, correlation and goniometer meters [minor] (Claude)
- Split the widgets demo into one class per section [patch] (Claude)

## v3.56.2 (patch)

Changes since v3.56.1:

- Restore the worktree ignore lines the SDK build dropped from .gitignore (Claude)
- Merge main into the demo split, registering ParametricEqDemo as its own section (Claude)
- Merge main into the demo split, registering TimecodeFieldDemo in DemoSections (Claude)
- Merge main into the demo split, registering TransportScrubberDemo in DemoSections (Claude)
- Merge remote-tracking branch 'origin/main' into claude/project-thread-026l9x (Claude)
- Expect the data table in the registry's advanced demo order (Claude)
- Merge main into the demo split branch (Claude)
- Reuse one StringBuilder when generating the grid demo's strings [patch] (Claude)
- Split the widgets demo into one class per section [patch] (Claude)

## v3.56.1 (patch)

Changes since v3.56.0:

- [patch] Map symbol-spelled chord keys and pin Tab to the named-key bound (Claude)
- Merge main's metadata update (Claude)
- Merge main into the parametric EQ branch, adding TimecodeField to the widget list (Claude)
- Merge main into claude/project-thread-g9k1ol-parametric-eq (Claude)
- Merge remote-tracking branch 'origin/main' into claude/project-thread-g9k1ol-parametric-eq (Claude)
- Merge main into the parametric-eq branch (Claude)
- Compare whole EqBands rather than floats for change detection (Claude)
- Simplify ParametricEq guards flagged by code scanning (Claude)
- Add ParametricEq widget and extract LogFrequencyAxis (#505) (Claude)

## v3.56.0 (minor)

Changes since v3.55.0:

- Merge main into TimecodeField (Claude)
- Merge main into TimecodeField (Claude)
- Add TimecodeField widget and Timecode helpers (#525) (Claude)

## v3.55.0 (minor)

Changes since v3.54.0:

- Merge main into TransportScrubber via the Waveform zoom branch (Claude)
- Merge main into the waveform zoom branch (Claude)
- Merge main into the transport scrubber branch (Claude)
- Merge main into the waveform zoom branch (Claude)
- [patch] Add the data table to the widget gallery (matt-edmondson)
- [patch] Stamp the gallery's sample folders with Directory.SetLastWriteTime (matt-edmondson)
- Compare TransportScrubber positions with an epsilon helper (Claude)
- Compare TransportScrubber in/out points without float equality (Claude)
- Add TransportScrubber: ruler, thumbnail strip, playhead and in/out range [minor] (Claude)
- Add EnvelopeEditor, a DAHDSR envelope editor (#510) (Claude)
- Waveform: zoom and scroll over a shared TimelineView [minor] (Claude)

## v3.54.0 (minor)

Changes since v3.53.0:

- Merge main into the data table branch and give DataTable a gallery tile (matt-edmondson)
- Check the curve editor's drawn bounds without a nullable dereference (Claude)
- Compare floats with tolerances where the code-quality bot flagged them (Claude)
- [patch] Show the data table demo once, with the advanced demos (matt-edmondson)
- Regenerate the widget gallery with the fixed renderer and widgets (Claude)
- Fix four widgets that drew wrongly in the gallery (Claude)
- Make the software rasterizer follow the GPU rules ImGui relies on (Claude)
- [patch] Read an empty clipboard without throwing and address code quality findings (matt-edmondson)
- Merge main into the data table branch (matt-edmondson)
- Merge main into the data table branch (Claude)
- [patch] Drop a semicolon from the data table's Key Files line (matt-edmondson)
- [patch] Skip parts already in stock when the demo marks them in stock (matt-edmondson)
- [patch] Drive the data table through a keymap in the UI tests (matt-edmondson)
- [patch] Require Shift for a chord that names it even with Shift ignored (matt-edmondson)
- [patch] Begin a data table edit from a character typed with AltGr (matt-edmondson)
- [patch] Report no edit for a null string committed unchanged (matt-edmondson)
- [minor] Let a caller set a data table's selection and active cell (matt-edmondson)
- [patch] Rebuild a data table over a list replaced after Refresh (matt-edmondson)
- [patch] Commit a data table edit on keypad Enter and keep the edited row drawn (matt-edmondson)
- [patch] Give the data table's clipper the row pitch with cell padding (matt-edmondson)
- [patch] State which frameworks ktsu.Keybinding brings System.Text.Json to (matt-edmondson)
- [patch] Show the data table in the widgets demo and document it (matt-edmondson)
- [minor] Open a caller-drawn context menu on a data table cell (matt-edmondson)
- [patch] Leave the clipboard as found and ignore characters a cell can't start from (matt-edmondson)
- [minor] Drive a data table from the keyboard and edit cells in place (matt-edmondson)
- [minor] Filter a data table's columns from a row under the headers (matt-edmondson)
- [minor] Draw a data table with sortable headers and clickable cells (matt-edmondson)
- [minor] Declare the data table's keyboard commands for ktsu.Keybinding (matt-edmondson)
- [minor] Edit one data table cell at a time and report each edit (matt-edmondson)
- [minor] Move a data table's active cell and select its rows (matt-edmondson)
- [minor] Sort and filter a data table's rows into a view (matt-edmondson)
- [patch] Document the data table edit sessions' members (matt-edmondson)
- [minor] Add typed data table columns and their edit sessions (matt-edmondson)
- [patch] Share the key chord matcher and let held chords repeat (matt-edmondson)
- [patch] Plan the data table widget implementation (matt-edmondson)
- [patch] Design a data table widget with sorting, filtering, and cell editing (matt-edmondson)

## v3.53.0 (minor)

Changes since v3.52.0:

- Add colour wheel tiles and show the widget gallery in the README (Claude)
- Add a spectrum analyzer tile to the widget gallery (Claude)
- Move CI onto the shared ci-shared.yml pipeline (Claude)
- Merge remote-tracking branch 'origin/main' into claude/widget-gallery-harness-ouh9b5 (Claude)
- Test the widget gallery tool and clear its SonarCloud findings (Claude)
- Merge main into the colour wheels branch (Claude)
- Compute test body positions in floating point (Claude)
- Re-resolve ForceLayout edges by body id when the body set changes (Claude)
- Don't type-check link ends typed with open generics (Claude)
- Seed a duplicated pin's default from the original so reset works on copies (Claude)
- Merge main into the colour wheels branch (Claude)
- Add a waveform tile to the widget gallery (Claude)
- Merge remote-tracking branch 'origin/main' into claude/widget-gallery-harness-ouh9b5 (Claude)
- Add the generated widget gallery images (Claude)
- Add a headless widget gallery tool for README images (Claude)
- [patch] Compare colour wheel master values with a tolerance (Claude)
- [minor] Add lift/gamma/gain colour wheels (Claude)

## v3.52.0 (minor)

Changes since v3.51.0:

- Merge main into the spectrum analyzer branch (Claude)
- Spell out the NaN cases in SpectrumAnalyzerState's guards (Claude)
- [minor] Add SpectrumAnalyzer widget (Claude)

## v3.51.0 (minor)

Changes since v3.50.0:

- Compare waveform positions without float equality (Claude)
- [minor] Add Waveform overview widget with playhead and loop region (Claude)
- Add domain widget shortlist plan (Claude)

## v3.50.1-pre.1 (prerelease)

Changes since v3.50.0:

- Bump Polyfill from 11.4.0 to 11.4.1 (dependabot[bot])
- Bump the ktsu group with 12 updates (dependabot[bot])

## v3.50.0 (minor)

Changes since v3.49.0:

- [minor] Type-check pins when creating a link (Claude)
- [patch] Keep the new demo code free of unsafe blocks (Claude)
- [patch] Drop the float equality guard on the viewport's wheel dolly (Claude)
- [patch] Cover recent features in the ImGuiApp demo (Claude)

## v3.49.2 (patch)

Changes since v3.49.1:

- Bump the ktsu group with 9 updates (dependabot[bot])

## v3.49.1 (patch)

Changes since v3.49.0:

- [patch] Keep the new demo code free of unsafe blocks (Claude)
- [patch] Drop the float equality guard on the viewport's wheel dolly (Claude)
- [patch] Cover recent features in the ImGuiApp demo (Claude)

## v3.49.0 (minor)

Changes since v3.48.0:

- Address code-quality findings in the node editor history and comment boxes (Claude)
- Restore the .gitignore entries the SDK build dropped (Claude)
- [minor] NodeEditor: undo/redo, keymap commands, grid snapping and comment boxes (Claude)

## v3.48.0 (minor)

Changes since v3.47.0:

- [patch] Put back the worktree ignores the SDK build removed (matt-edmondson)
- [minor] Name the diff view's selection helpers for what they select (matt-edmondson)
- [minor] Size, cache and probe the diff view, and publish its selection helpers (matt-edmondson)
- [patch] Tint the paired view's number column too (matt-edmondson)
- [patch] Pin the demo's second diff view to side by side (matt-edmondson)
- [minor] Draw a diff side by side as well as unified (matt-edmondson)
- [patch] Make the summary bar's style color pushes exception-safe (matt-edmondson)
- [patch] Correct the remaining clipper reference in the plan (matt-edmondson)
- [patch] Use the clipper type this binding actually has (matt-edmondson)
- [minor] Draw a unified diff with selectable hunks (matt-edmondson)
- [patch] Gate the drawing tasks on the headless UI harness (matt-edmondson)
- [patch] Correct the plan's null guard constraint (matt-edmondson)
- [minor] Add the diff view's model and its rules (matt-edmondson)
- [patch] Plan the diff view widget implementation (matt-edmondson)
- [patch] Design a diff view widget with selectable hunks (matt-edmondson)
- [patch] Ignore worktree and agent scratch directories (matt-edmondson)

## v3.47.0 (minor)

Changes since v3.46.0:

- Compare the GL context, not the ImGui context, when detecting a context change (Claude)

## v3.46.1 (patch)

Changes since v3.46.0:

- Bump Moq from 4.20.72 to 4.21.0 (dependabot[bot])
- Bump the ktsu group with 11 updates (dependabot[bot])

## v3.46.0 (minor)

Changes since v3.45.0:

- Assert the dolly bound with IsLessThan rather than IsTrue (Claude)
- Cover the render-to-target round trip, the one path Viewport3D is made of (Claude)
- Add Viewport3DState, the ImGui-free orbit camera behind Viewport3D (Claude)

## v3.45.0 (minor)

Changes since v3.44.0:

- [minor] Create and update textures from BGRA, RGB, BGR and greyscale pixels with row padding (Claude)
- Pump worker-thread texture tests without Thread.Sleep (Claude)
- [patch] Marshal UpdateTexture's in-place path to the window thread and document dynamic textures (Claude)

## v3.44.1 (patch)

Changes since v3.44.0:

- Pump worker-thread texture tests without Thread.Sleep (Claude)
- [patch] Marshal UpdateTexture's in-place path to the window thread and document dynamic textures (Claude)

## v3.44.0 (minor)

Changes since v3.43.0:

- Split ProcessInputEvents, one method per request kind (matt-edmondson)
- Merge main, and carry pin values onto a duplicated node (matt-edmondson)
- Test the WantTextInput guard on the duplicate chord (matt-edmondson)
- [minor] Duplicate selected nodes with Ctrl+D (matt-edmondson)

## v3.43.0 (minor)

Changes since v3.42.0:

- Test the WantTextInput guard, which nothing exercised (matt-edmondson)
- [minor] Delete selected nodes with Delete or Backspace (matt-edmondson)
- Merge the branch's own remote, which had gained a second merge of main (matt-edmondson)
- Merge main into the node-parameters branch (matt-edmondson)
- Describe both homes a parameter can have (matt-edmondson)
- Drive every pin value kind on both editing surfaces (matt-edmondson)
- Drop two constant conditions the quality bot flagged (matt-edmondson)
- Give a pin one home, whether or not it has an instance (matt-edmondson)
- Merge origin/main into node parameter editing (matt-edmondson)
- Cheap wording and cleanup fixes from the branch review (matt-edmondson)
- Fix enum editors silently misrepresenting unmatched or null values (matt-edmondson)
- Amend the design spec to name the shipped CreateNodeFromSpecs (matt-edmondson)
- Document the parameter-editing surface and its dependency cost in CLAUDE.md (matt-edmondson)
- Coerce a mistyped declared pin default before seeding it (matt-edmondson)
- Fix output pin labels detaching from their pins with inline editors (matt-edmondson)
- [patch] Show parameter editing in the demo and the README (matt-edmondson)
- [patch] Fix enum rows in the node inspector silently discarding edits (matt-edmondson)
- [minor] Draw one node's parameters as a property grid (matt-edmondson)
- [minor] Report which nodes are selected (matt-edmondson)
- [minor] Draw an editor on an unconnected input pin (matt-edmondson)
- [minor] Name the pin types that can be edited (matt-edmondson)
- [minor] Carry a declared pin's type and default onto the graph (matt-edmondson)
- [minor] Give the engine a value per pin (matt-edmondson)
- [minor] Hold a value and a default per pin (matt-edmondson)
- [minor] Let a pin declare the type it carries (matt-edmondson)
- [patch] Plan node parameter editing (matt-edmondson)
- [patch] Design node parameter editing (matt-edmondson)

## v3.42.0 (minor)

Changes since v3.41.0:

- Merge remote-tracking branch 'origin/main' into claude/imguiapp-409-textfilter-glob (Claude)
- refactor: filter the selected link ids explicitly (matt-edmondson)
- feat: delete selected links with Delete or Backspace [minor] (matt-edmondson)
- fix: keep a path-tracked texture published across UpdateTexture's recreate [patch] (matt-edmondson)
- refactor: filter the file browser's listing with ktsu.TextFilter [patch] (Claude)

## v3.41.2 (patch)

Changes since v3.41.1:

- Merge remote-tracking branch 'origin/main' into claude/imguiapp-409-textfilter-glob (Claude)
- refactor: filter the file browser's listing with ktsu.TextFilter [patch] (Claude)

## v3.41.1 (patch)

Changes since v3.41.0:

- Bump the ktsu group with 16 updates (dependabot[bot])

## v3.41.0 (minor)

Changes since v3.40.0:

- Group the node-id lookups with the other GetNodeDefinition overloads (Claude)
- Bind each created node to an instance its parameters live on [minor] (Claude)

## v3.40.0 (minor)

Changes since v3.39.0:

- Split the 3D triangle fill into setup and per-pixel shading (Claude)
- Add IRenderer3D and its software implementation [minor] (Claude)
- [patch] Assert the node body hook's reach in order, per MSTEST0068 (Claude)
- [patch] Cover the prototype-reading paths, and drop two catches that cannot fire (Claude)
- [patch] Say plainly that NodeGraph declares execution rather than running it (Claude)
- [minor] Give NodeEditorRenderer a hook for host content in a node body (Claude)
- [patch] Read an input pin's default from its C# initializer (Claude)

## v3.39.0 (minor)

Changes since v3.38.0:

- Add a virtualized table widget [minor] (Claude)
- [patch] Correct the stale Hexa Widgets.Extras note in CLAUDE.md (Claude)
- [patch] Clear the three SonarCloud findings on this branch (matt-edmondson)
- [patch] Release the GDI+ graphics context when reading the DPI fails (matt-edmondson)
- [patch] Find the repository root from the test binaries, not CallerFilePath (matt-edmondson)
- [patch] Make rooted item specs explicit instead of relying on Path.Combine (matt-edmondson)
- [patch] Pin ImGui.App's Resources.resx manifest name, and guard every .resx (matt-edmondson)
- [patch] Run window teardown when the run loop throws (matt-edmondson)
- [patch] Keep integrated-GPU font heuristics off discrete Intel Arc (Claude)
- [patch] Clear the window on teardown so Start can run again (matt-edmondson)

## v3.38.9 (patch)

Changes since v3.38.8:

- [patch] Correct the stale Hexa Widgets.Extras note in CLAUDE.md (Claude)

## v3.38.8 (patch)

Changes since v3.38.7:

- Bump Markdig from 1.3.2 to 1.4.0 (dependabot[bot])
- Bump Polyfill from 11.3.0 to 11.4.0 (dependabot[bot])
- Bump the ktsu group with 19 updates (dependabot[bot])

## v3.38.7 (patch)

Changes since v3.38.6:

- [patch] Clear the three SonarCloud findings on this branch (matt-edmondson)
- [patch] Release the GDI+ graphics context when reading the DPI fails (matt-edmondson)

## v3.38.6 (patch)

Changes since v3.38.5:

- [patch] Find the repository root from the test binaries, not CallerFilePath (matt-edmondson)
- [patch] Make rooted item specs explicit instead of relying on Path.Combine (matt-edmondson)
- [patch] Pin ImGui.App's Resources.resx manifest name, and guard every .resx (matt-edmondson)

## v3.38.5 (patch)

Changes since v3.38.4:

- Bump the ktsu group with 20 updates (dependabot[bot])

## v3.38.4 (patch)

Changes since v3.38.3:

- Bump the ktsu group with 8 updates (dependabot[bot])

## v3.38.3 (patch)

Changes since v3.38.2:

- [patch] Run window teardown when the run loop throws (matt-edmondson)

## v3.38.2 (patch)

Changes since v3.38.1:

- [patch] Keep integrated-GPU font heuristics off discrete Intel Arc (Claude)

## v3.38.1 (patch)

Changes since v3.38.0:

- [patch] Clear the window on teardown so Start can run again (matt-edmondson)

## v3.38.0 (minor)

Changes since v3.37.0:

- [minor] Cover Branch and Leaf, and give the state overload a flags form (matt-edmondson)
- [patch] Pin the ImGuiAppDemo resource name so its lookups resolve again (matt-edmondson)
- [minor] Add collapsible Branch and Leaf nodes to the Tree widget (matt-edmondson)
- Upload one GPU texture per path under concurrent first access (Claude)
- Gate Dependabot auto-merge on CI actually being green (Claude)

## v3.37.0 (minor)

Changes since v3.36.0:

- Remove generic catch from SetWindowIcon test ([@Copilot](https://github.com/Copilot))
- Address Sonar gate coverage for macOS icon change ([@Copilot](https://github.com/Copilot))
- Fix macOS app icon handling ([@Copilot](https://github.com/Copilot))
- Initial plan ([@Copilot](https://github.com/Copilot))

## v3.36.0 (minor)

Changes since v3.35.0:

- Fix CRLF handling for unterminated single-line literals ([@Copilot](https://github.com/Copilot))
- Initial plan ([@Copilot](https://github.com/Copilot))

## v3.35.0 (minor)

Changes since v3.34.0:

- Handle malformed JPEG spectral selectors and TGA color-map depth ([@Copilot](https://github.com/Copilot))
- Add recentering stand-down tests for pinned and frozen bodies ([@Copilot](https://github.com/Copilot))
- Sync ForceDirectedLayout and NodeEditor README tuning values ([@Copilot](https://github.com/Copilot))
- Initial plan ([@Copilot](https://github.com/Copilot))
- Initial plan ([@Copilot](https://github.com/Copilot))
- Initial plan ([@Copilot](https://github.com/Copilot))

## v3.34.0 (minor)

Changes since v3.33.0:

- ci: adopt the consolidated .NET workflow [patch] (Claude)
- ci: tolerate a SonarQube Cloud outage instead of failing the build [patch] (Claude)

## v3.33.1 (patch)

Changes since v3.33.0:

- ci: adopt the consolidated .NET workflow [patch] (Claude)
- ci: tolerate a SonarQube Cloud outage instead of failing the build [patch] (Claude)

## v3.33.0 (minor)

Changes since v3.32.0:

- refactor: take two review findings on CurveTrack [patch] (matt-edmondson)
- feat: add CurveTrack, a tone curve widget [minor] (matt-edmondson)

## v3.32.3 (patch)

Changes since v3.32.2:

- Bump the ktsu group with 8 updates (dependabot[bot])

## v3.32.2 (patch)

Changes since v3.32.1:

- Bump Polyfill from 11.2.0 to 11.3.0 (dependabot[bot])
- Bump the ktsu group with 6 updates (dependabot[bot])

## v3.32.1 (patch)

Changes since v3.32.0:

- Bump the ktsu group with 2 updates (dependabot[bot])

## v3.32.0 (minor)

Changes since v3.31.0:

- Centre a settled graph on the origin, separately from gravity (Claude)

## v3.31.0 (minor)

Changes since v3.30.0:

- Assert the section's open state as a condition, not an equality (matt-edmondson)
- Close a property grid section with a scope instead of a paired call (matt-edmondson)
- Clear the property grid's static analysis findings (matt-edmondson)
- Advance the property grid's tick atomically, and filter the sweep explicitly (matt-edmondson)
- Add a property grid to the widget library (matt-edmondson)

## v3.30.0 (minor)

Changes since v3.29.0:

- Use a comparison assert for the untwist separation bound (Claude)
- Tune the layout defaults against the benchmark corpus (Claude)
- Keep repulsion finite when MinRepulsionDistance is zero (Claude)
- Add a scoring and tuning layer over the layout benchmark harness (Claude)

## v3.29.0 (minor)

Changes since v3.28.0:

- Use the comparison asserts rather than Assert.IsTrue on a comparison (Claude)
- Compute the fan-in target's height in double (Claude)
- Add a benchmark harness for iterating on layout behaviour (Claude)

## v3.28.0 (minor)

Changes since v3.27.0:

- Compare a reach as a sorted sequence (Claude)
- Walk the graph backward as well as forward (Claude)
- Find the pin's node rather than walking every node past it (Claude)
- [minor] Answer a hovered node with the links and reach it has (Claude)

## v3.27.0 (minor)

Changes since v3.26.0:

- Measure repulsion across the clear space between node boxes (Claude)

## v3.26.1 (patch)

Changes since v3.26.0:

- Bump the system group with 1 update (dependabot[bot])
- Bump the microsoft group with 1 update (dependabot[bot])
- Bump the ktsu group with 6 updates (dependabot[bot])

## v3.26.0 (minor)

Changes since v3.25.0:

- Use Ensure.NotNull for the panel's argument guards (Claude)
- [minor] Put the whole layout tuning surface on a reusable panel (Claude)

## v3.25.0 (minor)

Changes since v3.24.0:

- [minor] Untwist links that cross where they meet at a node (Claude)

## v3.24.0 (minor)

Changes since v3.23.0:

- [minor] Raise MaxVelocity so a graph settles in seconds, not minutes (Claude)

## v3.23.0 (minor)

Changes since v3.22.0:

- [minor] Measure links between their pins rather than between node centres (Claude)

## v3.22.0 (minor)

Changes since v3.21.0:

- [minor] Pull edges towards horizontal, not just out of their own way (Claude)

## v3.21.0 (minor)

Changes since v3.20.0:

- Record reordering in the pass that already walks the edges (Claude)
- Share the backward-edge tests' setup to clear the duplication gate (Claude)
- [minor] Let a backward edge's endpoints slide around each other to reorder (Claude)
- Give every published package its own DESCRIPTION.md (Claude)
- Cover the new link-flattening lines to clear the Sonar quality gate (Claude)
- [minor] Add a link-flattening force to keep steep edges' curves visible (Claude)

## v3.20.0 (minor)

Changes since v3.19.0:

- Merge main into the node editor zoom branch (Claude)
- Merge remote-tracking branch 'origin/main' into claude/imguiapp-issue-354-7ftjju (Claude)
- fix: use Ensure.NotNull for the renderer's argument checks [patch] (Claude)
- feat: zoom and fit-to-view in the node editor [minor] (Claude)
- Guard with Ensure.NotNull, as KTSU0003 requires (Claude)
- Address the code-quality findings on the IDCT basis and the temp path (Claude)
- Merge remote-tracking branch 'origin/main' into claude/imguiapp-issue-354-7ftjju (Claude)
- [minor] Replace SixLabors.ImageSharp with a self-contained image decoder (Claude)

## v3.19.0 (minor)

Changes since v3.18.0:

- Stop dereferencing nullables in the node editor tests (Claude)
- Cover the node editor so the rename can pass the coverage gate (Claude)
- Cover the block-comment form of a language hint (Claude)
- Resolve the embedded rule and its language in one expression (Claude)
- Address code-quality findings in the embedded-language expander (Claude)
- [minor] Split tokenizing into ktsu.SyntaxHighlighting and highlight embedded languages (Claude)
- Rename ImGuiNodeEditor to ImGui.NodeEditor, and inventory the widgets (Claude)
- Merge remote-tracking branch 'origin/main' into claude/syntax-highlighting-library-3dbkoo (Claude)
- Add the missing package READMEs and audit the rest (Claude)
- Credit the Hexa.NET libraries in the package READMEs (Claude)

## v3.18.0 (minor)

Changes since v3.17.0:

- Address code-quality findings on the highlighting renderer and demo test (Claude)
- [minor] Add ImGui.SyntaxHighlighting, a sibling library to ImGui.Markdown (Claude)

## v3.17.0 (minor)

Changes since v3.16.0:

- feat: keep bodies off one another in the layout core [minor] (Claude)
- fix: stop macOS remapping the harness's Ctrl to Super [patch] (Claude)
- ci: make the iOS target framework opt-in and put macOS back in the matrix [patch] (Claude)
- [patch] Point the Linux coverage report at the workspace it is analysed in (matt-edmondson)
- [patch] Run the new harness suites on both hosts so their coverage counts (matt-edmondson)
- [patch] Cover the searchable list and node rendering with UI tests (matt-edmondson)
- @ [patch] Expect the searchable list to confirm on the item click (matt-edmondson)
- Fix empty node rendering by adding a dummy item for nodes without pins (matt-edmondson)
- Enhance SearchableList functionality with item selection confirmation and improved item display handling (matt-edmondson)
- [patch] Record the Hexa vs ktsu widget verdict (Claude)

## v3.16.11 (patch)

Changes since v3.16.10:

- Bump the ktsu group with 7 updates (dependabot[bot])

## v3.16.10 (patch)

Changes since v3.16.9:

- fix: stop macOS remapping the harness's Ctrl to Super [patch] (Claude)
- ci: make the iOS target framework opt-in and put macOS back in the matrix [patch] (Claude)

## v3.16.9 (patch)

Changes since v3.16.8:

- [patch] Point the Linux coverage report at the workspace it is analysed in (matt-edmondson)

## v3.16.8 (patch)

Changes since v3.16.7:

- [patch] Run the new harness suites on both hosts so their coverage counts (matt-edmondson)
- [patch] Cover the searchable list and node rendering with UI tests (matt-edmondson)
- @ [patch] Expect the searchable list to confirm on the item click (matt-edmondson)
- Fix empty node rendering by adding a dummy item for nodes without pins (matt-edmondson)
- Enhance SearchableList functionality with item selection confirmation and improved item display handling (matt-edmondson)

## v3.16.7 (patch)

Changes since v3.16.6:

- Bump the ktsu group with 10 updates (dependabot[bot])

## v3.16.6 (patch)

Changes since v3.16.5:

- Bump the ktsu group with 7 updates (dependabot[bot])

## v3.16.5 (patch)

Changes since v3.16.4:

- Bump MSTest.Sdk from 4.3.3 to 4.4.0 (dependabot[bot])
- Bump the ktsu group with 6 updates (dependabot[bot])

## v3.16.4 (patch)

Changes since v3.16.3:

- Bump the ktsu group with 6 updates (dependabot[bot])

## v3.16.3 (patch)

No significant changes detected since v3.16.2.

## v3.16.2 (patch)

Changes since v3.16.1:

- Bump the ktsu group with 3 updates (dependabot[bot])

## v3.16.1 (patch)

Changes since v3.16.0:

- [patch] Record the Hexa vs ktsu widget verdict (Claude)

## v3.16.0 (minor)

Changes since v3.15.0:

- Cover the close-cancel and title-drop paths the tests had missed (Claude)
- Give iOS the window-title API so the shared config's cref resolves (Claude)
- [minor] Allow the window title to change, and the close to be cancelled (Claude)

## v3.15.1 (patch)

Changes since v3.15.0:

- Bump the ktsu group with 6 updates (dependabot[bot])

## v3.15.0 (minor)

Changes since v3.14.0:

- Address code-quality findings in the widget UI tests (Claude)
- Add per-widget isolation UI tests for ktsu.ImGui.Widgets (Claude)

## v3.14.1 (patch)

Changes since v3.14.0:

- Bump the ktsu group with 9 updates (dependabot[bot])

## v3.14.0 (minor)

Changes since v3.13.0:

- Revert "Give each coverage report a distinct name before analysis [patch]" (matt-edmondson)
- ci: make the SonarQube quality gate opt in [patch] (matt-edmondson)
- ci: adopt the unified dotnet workflow [patch] (matt-edmondson)
- Give each coverage report a distinct name before analysis [patch] (matt-edmondson)
- Cover the reconciler and harness guards that had no test [patch] (matt-edmondson)
- Drive the texture uploads through IGL so they can be tested [patch] (matt-edmondson)
- Justify the reconciler's unsafe blocks [patch] (matt-edmondson)
- Separate texture reconciliation from the OpenGL calls [patch] (matt-edmondson)
- Merge remote-tracking branch 'origin/main' into feat/dynamic-font-textures (matt-edmondson)
- ci: run the UI suites on Linux only [patch] (matt-edmondson)
- test: settle in one frame and prove a section really expanded [patch] (matt-edmondson)
- perf: take the flat-fill path through the rasterizer [patch] (matt-edmondson)
- [minor] Enable ImGui 1.92 dynamic font textures (matt-edmondson)

## v3.13.2 (patch)

Changes since v3.13.1:

- ci: adopt the unified dotnet workflow [patch] (matt-edmondson)

## v3.13.1 (patch)

Changes since v3.13.0:

- ci: run the UI suites on Linux only [patch] (matt-edmondson)
- test: settle in one frame and prove a section really expanded [patch] (matt-edmondson)
- perf: take the flat-fill path through the rasterizer [patch] (matt-edmondson)

## v3.13.0 (minor)

Changes since v3.12.0:

- Add BlockSpacing class and tests for paragraph spacing logic (matt-edmondson)

## v3.12.1 (patch)

Changes since v3.12.0:

- Bump the ktsu group with 17 updates (dependabot[bot])

## v3.12.0 (minor)

Changes since v3.11.0:

- ci: test against the host runtime only [minor] (matt-edmondson)
- ci: use ktsubuild build for the test jobs [patch] (matt-edmondson)
- ci: drop --no-build after it found zero tests on the hosted runner [patch] (matt-edmondson)
- ci: build once per platform and test each project without rebuilding [minor] (matt-edmondson)
- ci: restore the quality gate as blocking [patch] (matt-edmondson)

## v3.11.1 (patch)

Changes since v3.11.0:

- ci: restore the quality gate as blocking [patch] (matt-edmondson)

## v3.11.0 (minor)

Changes since v3.10.0:

- ci: make the quality gate advisory for one release while the period bootstraps [patch] (matt-edmondson)
- ci: pass the computed version to the Sonar scanner [patch] (matt-edmondson)
- fix: resolve the two security conditions failing the quality gate [patch] (matt-edmondson)
- docs: record the measured results of the parallel test matrix [patch] (matt-edmondson)
- ci: drop macOS from the matrix pending the iOS workload problem [patch] (matt-edmondson)
- ci: gate the release on the quality gate and harden the release conditions [patch] (matt-edmondson)
- ci: analyze and release after the matrix, gated on the quality gate [minor] (matt-edmondson)
- ci: fan tests out across platform and project [minor] (matt-edmondson)
- ci: add a job that discovers test projects into a matrix [patch] (matt-edmondson)
- docs: plan the ci parallel test matrix (matt-edmondson)
- docs: keep the release job on windows to preserve the buildable project set [patch] (matt-edmondson)
- docs: correct the spec's release job design and add the security job [patch] (matt-edmondson)
- docs: drop the --json flag, list emits JSON only (matt-edmondson)
- docs: extend ktsubuild instead of reimplementing its rules in YAML (matt-edmondson)
- docs: design a parallel test matrix for CI (matt-edmondson)
- feat: add headless UI test projects for every demo [minor] (matt-edmondson)

## v3.10.0 (minor)

Changes since v3.9.0:

- docs: demonstrate the histogram and handle track together [patch] (matt-edmondson)
- docs: fix D2 file mix-up and a missed British spelling [patch] (matt-edmondson)
- docs: fix false statements and house-rule violations from review [patch] (matt-edmondson)
- fix: hold HandleTrackState's Normalize settle pass to the lower bound [patch] (matt-edmondson)
- docs: list the histogram and handle track widgets [patch] (matt-edmondson)
- fix: scope Histogram's peak search to the drawn range, not the raw span [patch] (matt-edmondson)
- feat: add the Histogram widget [minor] (matt-edmondson)
- docs: note inverted lower/upper are sorted, not collapsed [patch] (matt-edmondson)
- refactor: move RangeSlider onto HandleTrackState [patch] (matt-edmondson)
- feat: add the HandleTrack widget [minor] (matt-edmondson)
- fix: extract argument normalization to shared helper for Normalize and Drag [patch] (matt-edmondson)
- fix: apply minGap only between handles, not to track bounds; accept inverted bounds [patch] (matt-edmondson)
- fix: correct Drag floor/ceiling calculation to prevent handles leaving track [patch] (matt-edmondson)
- feat: add activation and dragging to HandleTrackState [minor] (matt-edmondson)
- fix: clamp minGap to fit all handles and update tests [patch] (matt-edmondson)
- feat: add HandleTrackState.Normalize [minor] (matt-edmondson)
- fix: centred becomes centered, not centerd [patch] (matt-edmondson)
- chore: use US English throughout [patch] (matt-edmondson)
- Stop Update SDKs failing when there is nothing to update (matt-edmondson)

## v3.9.3 (patch)

Changes since v3.9.2:

- Bump the ktsu group with 17 updates (dependabot[bot])

## v3.9.2 (patch)

Changes since v3.9.1:

- fix: centred becomes centered, not centerd [patch] (matt-edmondson)
- chore: use US English throughout [patch] (matt-edmondson)
- Stop Update SDKs failing when there is nothing to update (matt-edmondson)

## v3.9.1 (patch)

Changes since v3.9.0:

- Bump the ktsu group with 20 updates (dependabot[bot])

## v3.9.0 (minor)

Changes since v3.8.0:

- docs: refer to the host in plain code so the iOS target compiles [patch] (matt-edmondson)
- feat: let a headless session install its own renderer backend [minor] (matt-edmondson)
- chore: refresh the API compatibility suppressions [patch] (matt-edmondson)

## v3.8.1 (patch)

Changes since v3.8.0:

- chore: refresh the API compatibility suppressions [patch] (matt-edmondson)

## v3.8.0 (minor)

Changes since v3.7.0:

- fix: stop two ktsu packages colliding over their package data [patch] (matt-edmondson)
- docs: scope build badge to the default branch (matt-edmondson)
- fix: guard probe marking against a missing ImGui context [patch] (matt-edmondson)
- docs: document the harness and probe packages, reconcile the spec [patch] (matt-edmondson)
- feat: qualify probe names by window and scope [minor] (matt-edmondson)
- feat: mark probes from the widget and popup libraries by default [minor] (matt-edmondson)
- feat: address widgets by name through item probes [minor] (matt-edmondson)
- test: prove the harness renders deterministically [patch] (matt-edmondson)
- feat: add the headless harness with input, stepping and capture [minor] (matt-edmondson)
- feat: add a headless ImGui context with a software font atlas [minor] (matt-edmondson)
- feat: let a host drive ImGuiApp frames without a window [minor] (matt-edmondson)
- feat: render ImGui draw data through the software renderer [minor] (matt-edmondson)
- feat: rasterize textured, blended, scissored triangles in software [minor] (matt-edmondson)
- feat: add an RGBA pixel buffer with dependency-free PNG encoding [minor] (matt-edmondson)
- feat: scaffold the ktsu.ImGui.App.Testing package [minor] (matt-edmondson)
- docs: plan the headless UI test harness implementation [patch] (matt-edmondson)
- docs: design a headless UI test harness [patch] (matt-edmondson)
- fix: match every pattern in a semicolon-separated glob [patch] (matt-edmondson)

## v3.7.3 (patch)

Changes since v3.7.2:

- fix: match every pattern in a semicolon-separated glob [patch] (matt-edmondson)

## v3.7.2 (patch)

Changes since v3.7.1:

- Bump the ktsu group with 7 updates (dependabot[bot])

## v3.7.1 (patch)

Changes since v3.7.0:

- Bump Hexa.NET.Math from 2.0.6 to 2.0.8 (dependabot[bot])
- Bump the ktsu group with 9 updates (dependabot[bot])

## v3.7.0 (minor)

Changes since v3.6.0:

- feat: add ImageCanvas pan and zoom widget [minor] (matt-edmondson)
- feat: add ImageCanvasState pan and zoom view math [minor] (matt-edmondson)
- feat: public in-memory texture create and update on ImGuiApp [minor] (matt-edmondson)
- [patch] Give the widgets demo a top-level tab bar (matt-edmondson)
- feat: add IRendererBackend.UpdateTexture for in-place texture upload [minor] (matt-edmondson)

## v3.6.1 (patch)

Changes since v3.6.0:

- [patch] Give the widgets demo a top-level tab bar (matt-edmondson)

## v3.6.0 (minor)

Changes since v3.5.0:

- Make the curve editor's selection guarantee true and stop it collapsing (matt-edmondson)
- [patch] Pin the curve editor selection and guard it against vendor index defects (matt-edmondson)
- Pin Sequencer's ref parameters individually and redirect out-of-range Get writes to a scratch slot (matt-edmondson)
- Document the Tier 3 editors (matt-edmondson)
- Demo the Tier 3 editors (matt-edmondson)
- Add CurveSource and the multi-curve editor (matt-edmondson)
- Add the single-curve editor field (matt-edmondson)
- Fix FillRanges oversized-buffer indexing and document range-diff truncation (matt-edmondson)
- Add the sequencer adapter and entry point (matt-edmondson)
- Add SequenceSource and the pure sequencer range helpers (matt-edmondson)
- Add the bezier easing curve editor (matt-edmondson)
- Add CurveData wrapping the vendor curve representation (matt-edmondson)
- Add the Tier 3 editor enum mirrors and their mappings (matt-edmondson)
- Add the Tier 3 Hexa widgets implementation plan (matt-edmondson)
- Add the Tier 3 Hexa widgets design spec (matt-edmondson)
- [patch] Fix dialog wedges, rename outcome and dockspace in Hexa tier 2 (matt-edmondson)
- Give the Dialogs tab its own MessageOK instance to avoid cross-pane popup ID collisions (matt-edmondson)
- Document the Tier 2 dialogs and the deferred-drawing pumps (matt-edmondson)
- Demo DockedWindow and the docked pump in ImGuiAppDemo (matt-edmondson)
- Make every ktsu column in the Dialogs comparison tab live (matt-edmondson)
- Rework DockedWindow to compose Hexa's ImWindow instead of inheriting it (matt-edmondson)
- Add the dialog comparison rows and wire up the pump (matt-edmondson)
- Add the DockedWindow base type (matt-edmondson)
- Add the message box wrappers (matt-edmondson)
- Add the rename dialog wrapper (matt-edmondson)
- Add an unpumped fallback so ToggleSwitch keeps animating without a pump (matt-edmondson)
- Add the file and folder dialog wrappers (matt-edmondson)
- Add the deferred-drawing pumps and retire the animation stopgap (matt-edmondson)
- Add DialogOutcome and the mapping from Hexa's two result enums (matt-edmondson)
- Add the Tier 2 Hexa widgets implementation plan (matt-edmondson)
- Add the Tier 2 Hexa widgets design spec (matt-edmondson)

## v3.5.2 (patch)

Changes since v3.5.1:

- [patch] Pin the curve editor selection and guard it against vendor index defects (matt-edmondson)
- Pin Sequencer's ref parameters individually and redirect out-of-range Get writes to a scratch slot (matt-edmondson)
- Document the Tier 3 editors (matt-edmondson)
- Demo the Tier 3 editors (matt-edmondson)
- Add CurveSource and the multi-curve editor (matt-edmondson)
- Add the single-curve editor field (matt-edmondson)
- Fix FillRanges oversized-buffer indexing and document range-diff truncation (matt-edmondson)
- Add the sequencer adapter and entry point (matt-edmondson)
- Add SequenceSource and the pure sequencer range helpers (matt-edmondson)
- Add the bezier easing curve editor (matt-edmondson)
- Add CurveData wrapping the vendor curve representation (matt-edmondson)
- Add the Tier 3 editor enum mirrors and their mappings (matt-edmondson)
- Add the Tier 3 Hexa widgets implementation plan (matt-edmondson)
- Add the Tier 3 Hexa widgets design spec (matt-edmondson)

## v3.5.1 (patch)

Changes since v3.5.0:

- [patch] Fix dialog wedges, rename outcome and dockspace in Hexa tier 2 (matt-edmondson)
- Give the Dialogs tab its own MessageOK instance to avoid cross-pane popup ID collisions (matt-edmondson)
- Document the Tier 2 dialogs and the deferred-drawing pumps (matt-edmondson)
- Demo DockedWindow and the docked pump in ImGuiAppDemo (matt-edmondson)
- Make every ktsu column in the Dialogs comparison tab live (matt-edmondson)
- Rework DockedWindow to compose Hexa's ImWindow instead of inheriting it (matt-edmondson)
- Add the dialog comparison rows and wire up the pump (matt-edmondson)
- Add the DockedWindow base type (matt-edmondson)
- Add the message box wrappers (matt-edmondson)
- Add the rename dialog wrapper (matt-edmondson)
- Add an unpumped fallback so ToggleSwitch keeps animating without a pump (matt-edmondson)
- Add the file and folder dialog wrappers (matt-edmondson)
- Add the deferred-drawing pumps and retire the animation stopgap (matt-edmondson)
- Add DialogOutcome and the mapping from Hexa's two result enums (matt-edmondson)
- Add the Tier 2 Hexa widgets implementation plan (matt-edmondson)
- Add the Tier 2 Hexa widgets design spec (matt-edmondson)

## v3.5.0 (minor)

Changes since v3.4.0:

- Advance Hexa's animation clock so ToggleSwitch renders the right way round (matt-edmondson)
- Disable the iOS simulator smoke test on automatic triggers (matt-edmondson)
- Migrate the three icon.png files into LFS (matt-edmondson)
- Suppress S6640 on the demo OnConfigureFonts handlers (matt-edmondson)

## v3.4.3 (patch)

Changes since v3.4.2:

- Disable the iOS simulator smoke test on automatic triggers (matt-edmondson)

## v3.4.2 (patch)

Changes since v3.4.1:

- Migrate the three icon.png files into LFS (matt-edmondson)

## v3.4.1 (patch)

Changes since v3.4.0:

- Suppress S6640 on the demo OnConfigureFonts handlers (matt-edmondson)

## v3.4.0 (minor)

Changes since v3.3.0:

- Set PrivateAssets="all" on Polyfill in the example projects (matt-edmondson)
- Merge remote-tracking branch 'origin/main' into feature/hexa-widgets-tier1 (matt-edmondson)
- Split font-handle cleanup from glyph-range cache reset (matt-edmondson)
- Address final whole-branch review findings (matt-edmondson)
- Fix null-pointer writes in the FlameGraph interop callback (matt-edmondson)
- Add the image-centering comparison row to the Hexa vs ktsu tab (matt-edmondson)
- Document the OnConfigureFonts hook in CLAUDE.md (matt-edmondson)
- Add OnConfigureFonts hook so custom glyph ranges are rasterized before atlas build (matt-edmondson)
- Document Hexa-backed widgets (matt-edmondson)
- Add Material Icons font registration example to ImGuiAppDemo (matt-edmondson)
- Add Hexa vs ktsu widget comparison demo tab (matt-edmondson)
- Add FlameGraph widget with managed sample marshalling (matt-edmondson)
- Add FileTreeView widget backed by Hexa (matt-edmondson)
- Add DatePicker and YearPicker widgets backed by Hexa (matt-edmondson)
- Add EnumCombo widget backed by Hexa (matt-edmondson)
- Add toggle, transparent and inline button widgets backed by Hexa (matt-edmondson)
- Add horizontal and vertical splitter widgets backed by Hexa (matt-edmondson)
- Add Breadcrumb widget backed by Hexa (matt-edmondson)
- Add image alignment helpers backed by Hexa (matt-edmondson)
- Add text alignment and tooltip helpers backed by Hexa (matt-edmondson)
- Add IconTreeNode widget backed by Hexa (matt-edmondson)
- Add Spinner and BufferingBar widgets backed by Hexa (matt-edmondson)
- Pass KtsuSyncStyleConfigFiles=false in plan build commands to avoid SDK editorconfig race (matt-edmondson)
- Add Material Icons glyph range support to FontHelper (matt-edmondson)
- Add Hexa.NET.ImGui.Widgets package references (matt-edmondson)
- Align file headers with the SDK-generated editorconfig template (matt-edmondson)
- Update license headers and simplify qualified references (matt-edmondson)
- Add Hexa widgets Tier 1 implementation plan (matt-edmondson)
- Correct Hexa Tier 1 spec: nint textures, context-free test strategy (matt-edmondson)
- Add design spec for Hexa.NET.ImGui.Widgets integration (Tier 1) (matt-edmondson)
- Sync .github\workflows\dotnet.yml (KtsuTools)
- Sync .editorconfig (KtsuTools)
- Sync global.json (KtsuTools)
- Sync .github\workflows\dotnet.yml (KtsuTools)
- chore: update ktsu.Sdk to 2.21.1 [patch] (matt-edmondson)
- Sync examples\ImGuiAppDemo.iOS\icon.png (KtsuTools)
- Sync examples\ImGuiAppDemo\icon.png (KtsuTools)
- Sync icon.png (KtsuTools)
- Mark Polyfill as private in library csproj (Simon Cropp)
- [patch] Fix crash navigating to the parent directory in the filesystem browser (matt-edmondson)
- [patch] Upgrade to ktsu.Semantics 2.8.0 and simplify path access (matt-edmondson)
- [patch] Fix file dialog crash and drive list on Linux (matt-edmondson)

## v3.3.12 (patch)

Changes since v3.3.11:

- Mark Polyfill as private in library csproj (Simon Cropp)

## v3.3.11 (patch)

Changes since v3.3.10:

- Bump the system group with 1 update (dependabot[bot])
- Bump the microsoft group with 1 update (dependabot[bot])
- Bump the ktsu group with 6 updates (dependabot[bot])

## v3.3.10 (patch)

Changes since v3.3.9:

- chore: update ktsu.Sdk to 2.21.1 [patch] (matt-edmondson)
- Sync examples\ImGuiAppDemo.iOS\icon.png (KtsuTools)
- Sync examples\ImGuiAppDemo\icon.png (KtsuTools)
- Sync icon.png (KtsuTools)

## v3.3.9 (patch)

Changes since v3.3.8:

- Sync .github\workflows\dotnet.yml (ktsu[bot])
- Sync global.json (ktsu[bot])

## v3.3.8 (patch)

Changes since v3.3.7:

- Bump the ktsu group with 19 updates (dependabot[bot])
- Sync .github\workflows\update-sdks.yml (ktsu[bot])
- Sync .github\workflows\dotnet.yml (ktsu[bot])
- Sync .github\workflows\dependabot-merge.yml (ktsu[bot])

## v3.3.8-pre.1 (prerelease)

Changes since v3.3.7:

- Sync .github\workflows\update-sdks.yml (ktsu[bot])
- Sync .github\workflows\dotnet.yml (ktsu[bot])
- Sync .github\workflows\dependabot-merge.yml (ktsu[bot])

## v3.3.7 (patch)

Changes since v3.3.6:

- Bump the ktsu group with 2 updates (dependabot[bot])

## v3.3.6 (patch)

Changes since v3.3.5:

- [patch] Fix crash navigating to the parent directory in the filesystem browser (matt-edmondson)

## v3.3.5 (patch)

Changes since v3.3.4:

- Bump the ktsu group with 6 updates (dependabot[bot])

## v3.3.4 (patch)

Changes since v3.3.3:

- Bump the ktsu group with 2 updates (dependabot[bot])

## v3.3.3 (patch)

Changes since v3.3.2:

- Bump the ktsu group with 5 updates (dependabot[bot])

## v3.3.2 (patch)

Changes since v3.3.1:

- [patch] Upgrade to ktsu.Semantics 2.8.0 and simplify path access (matt-edmondson)

## v3.3.1 (patch)

Changes since v3.3.0:

- [patch] Fix file dialog crash and drive list on Linux (matt-edmondson)

## v3.3.0 (minor)

Changes since v3.2.0:

- Fix rendering and window geometry under tiling/Wayland compositors (matt-edmondson)
- Update gitattributes to match editorconfig for cs files (matt-edmondson)
- test(app): cover macOS DPI scale math, exclude native orchestration (matt-edmondson)
- Merge remote-tracking branch 'origin/main' into fix/macos-dpi-detection (matt-edmondson)
- [patch] Add native macOS DPI detection path (matt-edmondson)

## v3.2.6 (patch)

Changes since v3.2.5:

- Update gitattributes to match editorconfig for cs files (matt-edmondson)

## v3.2.5 (patch)

Changes since v3.2.4:

- Bump the ktsu group with 9 updates (dependabot[bot])

## v3.2.4 (patch)

Changes since v3.2.3:

- Bump MSTest.Sdk from 4.3.2 to 4.3.3 (dependabot[bot])
- Bump the ktsu group with 6 updates (dependabot[bot])

## v3.2.3 (patch)

Changes since v3.2.2:

- Bump the ktsu group with 6 updates (dependabot[bot])

## v3.2.2 (patch)

Changes since v3.2.1:

- Bump HexaGen.Runtime from 1.1.21 to 1.1.24 (dependabot[bot])
- Bump the ktsu group with 7 updates (dependabot[bot])

## v3.2.1 (patch)

Changes since v3.2.0:

- test(app): cover macOS DPI scale math, exclude native orchestration (matt-edmondson)
- Merge remote-tracking branch 'origin/main' into fix/macos-dpi-detection (matt-edmondson)
- [patch] Add native macOS DPI detection path (matt-edmondson)

## v3.2.0 (minor)

Changes since v3.1.0:

- ci(ios): pin newest installed Xcode instead of hardcoded 26.5 (matt-edmondson)
- fix(app): add direct HexaGen.Runtime reference for iOS build (matt-edmondson)
- fix(markdown): correct list marker gutter and code-block vertical padding (matt-edmondson)
- fix(markdown): correct inline cursor reservation, table ids, role-aware measure; drop unused deps (matt-edmondson)
- docs(markdown): add package README and update suite docs (matt-edmondson)
- feat(markdown): add ImGuiMarkdownDemo example (matt-edmondson)
- fix(markdown): task-list text, image line height, table columns; drop dead heading branch (matt-edmondson)
- feat(markdown): add block/inline renderers and public Render entry points (matt-edmondson)
- fix(markdown): guard System.Threading.Lock for net9+ so net9 build passes (matt-edmondson)
- feat(markdown): add theme color resolution and scoped font pushing (matt-edmondson)
- feat(markdown): flatten AST inlines into styled runs (matt-edmondson)
- feat(markdown): add pure sizing and list-marker helpers (matt-edmondson)
- feat(markdown): add Markdig pipeline, bounded parse cache, MarkdownDocument (matt-edmondson)
- feat(markdown): add pure InlineLayout word-wrap engine (matt-edmondson)
- fix(markdown): broaden LinkPolicy exception filter and align test usings (matt-edmondson)
- feat(markdown): add LinkPolicy for scheme filtering and safe OS-open (matt-edmondson)
- feat(markdown): add MarkdownConfig, MarkdownFontRole, MarkdownImageResult (matt-edmondson)
- feat(markdown): scaffold ktsu.ImGui.Markdown package with Markdig (matt-edmondson)
- Add implementation plan for ktsu.ImGui.Markdown (matt-edmondson)
- Add design spec for ktsu.ImGui.Markdown package (matt-edmondson)
- [patch] Use WCAG relative luminance to pick contrasting text color (matt-edmondson)
- [patch] Fix CI annotations: package validation, workflow deprecations, code smells (matt-edmondson)
- Refactor color conversion methods to use ToSrgb; add new ToHsl and ToImGuiVector4 methods (matt-edmondson)

## v3.1.10 (patch)

Changes since v3.1.9:

- Bump the ktsu group with 9 updates (dependabot[bot])

## v3.1.9 (patch)

Changes since v3.1.8:

- Bump the ktsu group with 9 updates (dependabot[bot])

## v3.1.8 (patch)

Changes since v3.1.7:

- Bump the ktsu group with 14 updates (dependabot[bot])

## v3.1.7 (patch)

Changes since v3.1.6:

- Bump the ktsu group with 6 updates (dependabot[bot])

## v3.1.6 (patch)

Changes since v3.1.5:

- Bump the ktsu group with 9 updates (dependabot[bot])

## v3.1.5 (patch)

Changes since v3.1.4:

- Bump the ktsu group with 12 updates (dependabot[bot])

## v3.1.4 (patch)

Changes since v3.1.3:

- Bump the ktsu group with 8 updates (dependabot[bot])

## v3.1.3 (patch)

Changes since v3.1.2:

- [patch] Use WCAG relative luminance to pick contrasting text color (matt-edmondson)

## v3.1.2 (patch)

Changes since v3.1.1:

- [patch] Fix CI annotations: package validation, workflow deprecations, code smells (matt-edmondson)

## v3.1.1 (patch)

Changes since v3.1.0:

- Refactor color conversion methods to use ToSrgb; add new ToHsl and ToImGuiVector4 methods (matt-edmondson)

## v3.1.0 (minor)

Changes since v3.0.0:

- [minor] Strengthen color-vector typing with ImGuiVector4; docs (matt-edmondson)
- [patch] Replace ImColors factory with Srgb/Color bridge conversions (matt-edmondson)
- [patch] Flatten palette to Palette, drop alias, add ImColor.ToImGuiU32 (matt-edmondson)
- [patch] Route library colors through the semantic color bridge (matt-edmondson)

## v3.0.1 (patch)

Changes since v3.0.0:

- Bump the system group with 1 update (dependabot[bot])
- Bump the microsoft group with 1 update (dependabot[bot])
- Bump the ktsu group with 4 updates (dependabot[bot])

## v3.0.0 (major)

Changes since v2.0.0:

- [major] Extract color adapter to ImGui.Color; thin Styler to theming (matt-edmondson)
- Renormalize icon LFS pointers (matt-edmondson)
- [minor] Add FullWidth option to SearchBox (matt-edmondson)
- [minor] Add ReturnAllWhenEmpty option to SearchBox (matt-edmondson)
- [minor] Refactor SearchBox to options-record API (matt-edmondson)
- feat(color)!: migrate ImGui.Styler to ktsu.Semantics.Color via ThemeProvider 2.0 (matt-edmondson)
- test(app): de-flake high-precision sleep timing assertions (matt-edmondson)
- feat(imgui-color): add ktsu.ImGui.Color adapter (Color \<-\> ImColor/Vector4) (matt-edmondson)
- chore: remove unused SourceLink package versions (matt-edmondson)
- chore: remove redundant SourceLink package references (matt-edmondson)
- Add compatibility suppressions for new APIs in ImGui libraries (matt-edmondson)
- Add tests for multi-line icon size calculations (matt-edmondson)
- Refactor Icon widget methods to use 'textBlock' parameter and update demo to reflect changes (matt-edmondson)
- Fix collection initialisation (Damon3000s)
- Add required system.text.json package (Damon3000s)
- Always "correctly" align grids in demo (Damon3000s)
- Update demo to show off multi line label Icon widget (Damon3000s)
- Support multiple lines for Icon widget (Damon3000s)
- [patch] Revert SixLabors.ImageSharp to 3.1.12 to avoid license key (matt-edmondson)
- Remove stale files (matt-edmondson)
- Add direct PackageReferences to example projects (Claude)
- Add direct PackageReferences for directly-used transitive packages (Claude)
- Remove unused package versions from Directory.Packages.props (Claude)
- Make divider container respect the ImGui draw cursor (Claude)
- Move iOS jobs out of dotnet.yml into a dedicated ios.yml workflow (Claude)
- [patch] Fix FilesystemBrowser crash on open (matt-edmondson)
- ci: supply SixLabors.ImageSharp 4 license key via SIXLABORS_LICENSE_KEY (Claude)
- Fix floating-point equality reliability issue in XYPad (Claude)
- Fix analyzer errors in embedded hosting and audio widgets (Claude)
- Add embedded-window hosting and audio widgets (Claude)
- feat(ios): curated ImGuiAppDemo.iOS showcase + simulator CI (Task 8, part 3) (#213) (matt-edmondson)
- fix(ios): import ktsu.Semantics.Strings for string.As\<AbsoluteFilePath\> (Claude)
- feat(ios): public texture loading via the Metal backend (Task 8, part 2) (Claude)
- feat(ios): AutoDiscoverExtensions flag (Task 8, part 1) (#211) (matt-edmondson)
- feat(ios): app menu (iPad) + Stop() semantics + no-op surface (Task 7) (#210) (matt-edmondson)
- feat(ios): font parity + imgui.ini redirect (Task 6) (#209) (matt-edmondson)
- ci: fix intermittent coverage broken-pipe flake (exit code 7) (#208) (matt-edmondson)
- feat(ios): touch + keyboard input (Task 5) (#207) (matt-edmondson)
- docs(ios): mark Task 4 (Metal renderer) complete in the port plan (Claude)
- chore(ios): remove renderer bring-up diagnostics (Claude)
- fix(ios): use TextUnformatted to avoid the variadic igText ARM64 crash (Claude)
- diag(ios): trace OnRender draw calls + log font atlas dims (Claude)
- diag(ios): trace the frame loop to localise the render SIGSEGV (Claude)
- fix(ios): use non-normalized UChar4 for the ImGui vertex colour (Claude)
- fix(ios): write cimgui.dylib to an absolute path (root cause) (Claude)
- fix(ios): stash cimgui.dylib in RUNNER_TEMP so the embed step finds it (Claude)
- fix(ios): copy cimgui.dylib into the .app and dlopen by bundle path (Claude)
- fix(ios): ship cimgui as an embedded dynamic library, dlopen it (Claude)
- diag(ios): inspect the app binary for cimgui link/export status (Claude)
- fix(ios): export dynamic symbols so dlsym resolves static cimgui (Claude)
- diag(ios): probe cimgui symbol resolution before first ImGui call (Claude)
- fix(ios): use ImGui.GetVersionS() for the smoke version probe (Claude)
- fix(ios): pin cimgui to a consistent 1.92.3 docking commit (Claude)
- feat(ios): statically link cimgui so ImGui runs on iOS (Claude)
- fix(ios): satisfy KTSU0003/CA2000 analyzers in the Metal backend (Claude)
- feat(ios): Metal renderer backend (Task 4) - stand up ImGui frames on iOS (Claude)
- wip(ios): begin Metal renderer (Task 4) - shader + frame-loop scaffolding (Claude)
- ci(ios): iOS-simulator smoke test for the lifecycle (#205) (matt-edmondson)
- ci: re-trigger (flaky ForceDirectedLayout test-host abort) (Claude)
- test: drop redundant (nint) casts on int literals (IDE0004) (Claude)
- [minor] Make GPU texture handles nint end-to-end for the Metal backend (Claude)
- iOS: satisfy analyzers/nullability in the UIKit lifecycle (Claude)
- docs: record resolved iOS-port design decisions (Claude)
- iOS: native UIKit lifecycle (UIApplicationDelegate + CADisplayLink) (Claude)
- ci: re-trigger workflow (flaky unrelated tests) (Claude)
- iOS: fix net10.0-ios compile errors from the config decoupling (Claude)
- iOS: make the config surface platform-neutral and align Start signature (Claude)
- Exclude ImGuiAppBlend.cs from the iOS build (Claude)
- Honor ImGui draw-command callbacks; add per-region blend modes (Claude)
- Remap canvas and restore window on overlay enter/exit (Claude)
- Refactor SuppressMessage attributes for static fields in ImGuiApp (matt-edmondson)
- [patch] Fix "Renderer backend is not initialized" when loading textures from OnStart (matt-edmondson)
- [minor] Fix SonarQube issues: safe fixes and justified suppressions (round 2/2) (matt-edmondson)
- [minor] Fix SonarQube issues: safe fixes and justified suppressions (round 1/2) (matt-edmondson)
- Exclude native C ABI shim from code coverage to fix CI test crash (matt-edmondson)
- Fix IDE0055 formatting in demo overlay settings (Claude)
- [minor] Add canonical overlay-mode window support (Claude)
- Fix typo in library name in README.md (matt-edmondson)
- docs: correct package names and audit documentation (Claude)
- Remove version number from VERSION.md (matt-edmondson)
- style: drop unused using and use id for skeleton shimmer offset (Claude)
- [minor] Add hidden-start and hide-on-close window support (matt-edmondson)
- feat: add Card, SkeletonLoader, and PinInput mobile widgets (Claude)
- style: remove redundant parentheses in Avatar hue calc (IDE0047) (Claude)
- feat: add mobile decorator widgets (Avatar, Badge, Rating, PageIndicator) (Claude)
- ci: re-trigger after flaky code-coverage pipe-disconnect at test session end (Claude)
- ci: re-trigger after flaky NodeGraph.Tests coverage pipe disconnect (Claude)
- style: drop redundant parentheses in RangeSlider tooltip guard (IDE0047) (Claude)
- Fix knob drag value accumulation and stale indicator (Claude)
- feat: add mobile form-control widgets [minor] (Claude)
- feat: add OverlayHost z-ordered overlay manager for ImGui.Widgets (Claude)
- ci: re-trigger after flaky PidFrameLimiter sleep-timing test (Claude)
- style: drop unused System using from InertialScrollTests (Claude)
- style: split inline if-statements in InertialScrollTests for IDE2001 (Claude)
- feat: add InertialScroll helper for ImGui.Widgets (Claude)
- docs: degrade ImGuiController cref to \<c\> for iOS-tfm doc-build (Claude)
- ci: make the iOS stub actually compile on macos-14 (Claude)
- fix: collapse Spring construction into object initializers (IDE0017) (Claude)
- ci: also clear ktsu.Sdk's forced RuntimeIdentifiers on the macOS iOS build (Claude)
- ci: scope iOS restore/build to net10.0-ios only (Claude)
- feat: add Tween, Spring, and Easing animation primitives (Claude)
- ci: add macos-14 job that compile-checks net10.0-ios (Claude)
- feat: add gesture detection foundation for ImGui.Widgets [minor] (Claude)
- docs: plan for mobile UI widgets in ImGui.Widgets (Claude)
- refactor: introduce IRendererBackend seam for the iOS port (Claude)
- docs: design plan for iOS platform port (Claude)
- fix: downgrade SixLabors.ImageSharp to 3.1.12 to restore CI (Claude)
- feat: scaffold net10.0-ios target for ImGui.App (Claude)
- fix: resolve IDE0221 and IDE0380 warnings treated as errors (Claude)
- fix: downgrade SixLabors.ImageSharp from 4.0.0 to 3.1.12 (Claude)
- fix: exclude NativeExports.cs from all SonarCloud analysis (Claude)
- fix: add InternalsVisibleTo for test project (KTSU0002) (Claude)
- fix: suppress CA1823 for intentional ABI struct padding fields (Claude)
- fix: exclude NativeExports.cs from SonarCloud coverage analysis (Claude)
- fix: address MSTest and code analysis violations in ForceDirectedLayout.Tests (Claude)
- fix: convert array initializers to collection expressions in ForceLayoutTests (Claude)
- Fix CI exit code propagation and SonarCloud quality gate failures (Claude)
- Add C ABI surface and AOT-friendly double-precision core (Claude)
- Extract force-directed layout into ktsu.ForceDirectedLayout (Claude)
- Update base directory path (Damon3000s)
- Copy ktsu.png to output directory for ImGuiWidgetsDemo (Damon3000s)
- Fix ImGuiPopupsDemo csproj inclusion (Damon3000s)
- Missed file from dotnet format (Damon3000s)
- Results from dotnet format (Damon3000s)
- Add DESCRIPTION.md and TAGS.md files; update README.md with comprehensive library details and usage examples (matt-edmondson)
- Add SonarLint configuration for connected mode (matt-edmondson)
- Sync .github\workflows\dotnet.yml (KtsuTools)
- feat(timers): Add countdown and count-up timer demos with radial progress indicators (matt-edmondson)
- Remove legacy build scripts (matt-edmondson)
- Increase MaxForce values in PhysicsSettings and demo to enhance simulation capabilities (matt-edmondson)
- Add directional bias setting and calculation for horizontal link forces (matt-edmondson)
- Update physics settings: adjust repulsion strength, origin anchor weight, damping factor, and link length for improved simulation dynamics (matt-edmondson)
- Enhance physics settings: add OriginAnchorWeight for gravity target blending and initialize world origin to centroid for improved simulation accuracy (matt-edmondson)
- Refactor gravity force calculation: simplify magnitude computation by removing distance factor (matt-edmondson)
- Update repulsion strength in physics settings for enhanced simulation performance (matt-edmondson)
- Adjust repulsion strength limits in physics settings for improved simulation accuracy (matt-edmondson)
- Refactor gravity calculations: update to use centroid for cohesion force and improve rendering of gravity center (matt-edmondson)
- Refine physics settings: adjust damping factor description and clamp minimum repulsion distance to prevent force explosions (matt-edmondson)
- Enhance physics simulation: add node pinning and stability detection features (matt-edmondson)
- [patch] Guard BeginFrame against calling native extensions without ImGui context (matt-edmondson)
- [minor] Add CleanImNodesDemo with physics simulation and attribute-based node editor (matt-edmondson)
- [minor] Add ImGuiNodeEditor with physics simulation and attribute-based node factory (matt-edmondson)
- [minor] Add NodeGraph test suite with 106 tests covering attributes, pins, type system, and validation (matt-edmondson)
- [minor] Add NodeGraph library with attribute-based node definitions (matt-edmondson)
- [minor] Refactor demo app with modular tab-based architecture and extension demos (matt-edmondson)
- [minor] Add unit tests for ImGuiExtensionManager (matt-edmondson)
- [minor] Integrate ImGuiExtensionManager into ImGuiController lifecycle (matt-edmondson)
- [minor] Add ImGuiExtensionManager for optional ImGuizmo, ImNodes, and ImPlot support (matt-edmondson)
- [minor] Add Hexa.NET.ImGuizmo, ImNodes, and ImPlot package references (matt-edmondson)
- Add visibility control for tabs in TabPanel (matt-edmondson)
- Exclude test projects from packaging and publishing processes in Invoke-DotNetPack and Invoke-DotNetPublish functions (matt-edmondson)
- Add compatibility suppressions for DefaultInterpolatedStringHandler in multiple modules (matt-edmondson)
- Add compatibility suppressions for DynamicallyAccessedMemberTypes and ExperimentalAttribute in ImGui.Popups, ImGui.Styler, and ImGui.Widgets for .NET 10.0 (matt-edmondson)
- Refine glyph area calculations and atlas fitting checks for improved memory management (matt-edmondson)
- Refactor null checks to use Ensure.NotNull for improved readability and consistency (matt-edmondson)
- Remove .github\workflows\project.yml (matt-edmondson)
- Enhance project name matching to handle variations in repository naming conventions (matt-edmondson)
- Enhance CalculateOptimalPixelSize to consider global accessibility scale for improved rendering (matt-edmondson)
- Refactor null argument checks to use Ensure.NotNull for improved readability (matt-edmondson)
- Improve search box hint display logic based on available width (matt-edmondson)
- Add CLAUDE.md for project guidance and architecture overview (matt-edmondson)
- migrate to dotnet 10 (matt-edmondson)
- Dont show the close button on tabs inside a non-closable tab bar (matt-edmondson)
- Refactor glyph calculation for improved readability (matt-edmondson)
- [minor] Add dynamic atlas sizing and glyph limit calculation (matt-edmondson)
- Fix gpu detection priority (matt-edmondson)
- Update tests/ImGui.App.Tests/FontMemoryGuardTests.cs (matt-edmondson)
- Update ImGui.App/FontMemoryGuard.cs to improve null checking (matt-edmondson)
- Update ImGui.App/FontMemoryGuard.cs to have more specific matching criteria (matt-edmondson)
- Update variable name ImGui.App/ImGuiApp.cs (matt-edmondson)
- Enhance font initialization with memory management features (matt-edmondson)
- Fix missing package references (matt-edmondson)
- Increase timeout for build job to 20 minutes (matt-edmondson)
- Enhance project structure and testing: Added new dependencies in Directory.Packages.props, introduced a new Tests project in the solution, and updated project references. Refactored namespaces for consistency across multiple files. Updated test configurations and example projects to align with the new structure. (matt-edmondson)
- Update project structure and dependencies: Added new package versions in Directory.Packages.props, updated SDK versions in global.json, and refactored namespaces across multiple files for consistency. Removed the ImGui.Popups.Credential project and adjusted related references in the solution and project files. Enhanced test project configurations and updated example projects to reflect the new structure. (matt-edmondson)
- Initial combined commit (matt-edmondson)
- Fix NuGet package source URL in Invoke-NuGetPublish function: Updated the source URL to ensure correct package publishing to packages.ktsu.dev. (matt-edmondson)
- Enhance .NET CI workflow: Added support for skipped releases in the GitHub Actions workflow. Updated conditions for SonarQube execution, coverage report upload, and Winget manifest updates to account for skipped releases, improving control over the release process. (matt-edmondson)
- Add Ktsu package key support in build configuration: Updated the .NET CI workflow and PowerShell script to include an optional Ktsu package key for publishing. Enhanced documentation for the new parameter and added conditional publishing logic for Ktsu.dev. (matt-edmondson)
- Implement modern DPI awareness handling in Windows: Updated ForceDpiAware to utilize the latest DPI awareness APIs for better compatibility with windowing libraries. Added fallback mechanisms for older Windows versions and enhanced NativeMethods with new DPI awareness context functions. (matt-edmondson)
- Enhance window position validation logic: Implemented performance optimizations to skip unnecessary checks when window position and size remain unchanged. Added methods for better multi-monitor support, ensuring windows are relocated when insufficiently visible. Updated tests to verify new behavior and performance improvements. (matt-edmondson)
- Update package versions and clean up validation logic: Bump versions for Hexa.NET.ImGui, ktsu.ScopedAction, SixLabors.ImageSharp, System.Text.Json, and MSTest packages. Remove redundant validation checks from ImGuiApp configuration and corresponding tests. (matt-edmondson)
- Add manual trigger support to GitHub Actions workflow: Enabled workflow_dispatch to allow manual execution of the .NET CI pipeline. (matt-edmondson)
- Refactor SonarQube token handling in GitHub Actions: Updated conditional checks to use environment variables for SONAR_TOKEN, ensuring consistent access across caching and installation steps. (matt-edmondson)
- Refactor ImGuiApp configuration handling: Introduced AdjustConfigForStartup method to automatically convert minimized window state to normal during startup, improving application reliability. Updated tests to validate this new behavior. (matt-edmondson)
- Update ImGuiApp configuration validation: Automatically convert minimized and fullscreen window states to normal during startup to prevent issues. Updated tests to reflect this change, ensuring proper state handling without exceptions. (matt-edmondson)
- Refactor SonarQube conditional checks in GitHub Actions: Updated syntax for SONAR_TOKEN checks to use the correct expression format, ensuring proper execution of caching and installation steps. (matt-edmondson)
- Enhance SonarQube integration in GitHub Actions: Added conditional checks for SONAR_TOKEN to ensure caching and installation steps only execute when the token is available, improving workflow reliability. (matt-edmondson)
- Additional tests (matt-edmondson)
- Move debug logger into its own file and make it output to the appdata dir (matt-edmondson)
- Move debug logger into its own file and make it output to the appdata dir (matt-edmondson)
- [minor] Implement PID-based frame limiting in ImGuiApp: Introduced a new PidFrameLimiter class for precise frame rate control, enhancing performance optimization. Updated documentation to reflect new features, including auto-tuning capabilities and real-time diagnostics. Adjusted rendering settings to disable VSync for improved frame limiting accuracy. (matt-edmondson)
- Enhance ImGuiApp documentation and features: Updated project overview, added detailed descriptions for performance optimization, debug logging, and Unicode support. Introduced performance monitoring capabilities with real-time FPS tracking and throttling visualization. Improved font management and DPI handling. Refactored configuration settings for better usability. Updated demo application to showcase new features. (matt-edmondson)
- Refactor performance settings: remove Ups, add NotVisibleFps and flags (Cursor Agent)
- Auto-commit pending changes before rebase - PR synchronize (Cursor Agent)
- Merge remote-tracking branch 'origin/main' into cursor/increase-imguiapp-test-coverage-c9d4 (matt-edmondson)
- Fix test paths using Path.GetFullPath for consistent texture testing (Cursor Agent)
- Add test for preventing multiple ImGuiApp starts (Cursor Agent)
- Cleanup (matt-edmondson)
- Enhance performance throttling with lowest-rate selection logic (Cursor Agent)
- Increase NotVisibleFps from 0.2 to 2.0 for better background performance (Cursor Agent)
- Add real-time FPS graph with throttling state visualization (Cursor Agent)
- Add NotVisibleFps setting for ultra-low frame rate when minimized (Cursor Agent)
- Adjust not visible frame rate to 0.2 FPS for better resource conservation (Cursor Agent)
- Refactor ImGuiApp tests to use Assert.ThrowsException method (Cursor Agent)
- Implement sleep-based frame rate throttling and remove UPS settings (Cursor Agent)
- Checkpoint before follow-up message (Cursor Agent)
- Update ImGuiFontConfig test to allow empty font path (Cursor Agent)
- Use PackageReleaseNotesFile to handle changelog release notes more robustly (Cursor Agent)
- Improve performance throttling with multi-condition rate selection (Cursor Agent)
- Remove debug throttling properties and simplify focus handling (Cursor Agent)
- Fix input focus detection and add throttling debug info (Cursor Agent)
- Checkpoint before follow-up message (Cursor Agent)
- Add comprehensive test coverage for ImGuiApp components and edge cases (Cursor Agent)
- Remove focus checks from input event handlers (Cursor Agent)
- Fix input focus detection to prevent incorrect idle state management (Cursor Agent)
- Cleanup (matt-edmondson)
- Improve window focus detection and add debug logging for throttling (Cursor Agent)
- Refactor test suite into focused, organized test classes (Cursor Agent)
- Checkpoint before follow-up message (Cursor Agent)
- Cleanup (matt-edmondson)
- Implement lowest frame rate throttling with comprehensive condition evaluation (Cursor Agent)
- Checkpoint before follow-up message (Cursor Agent)
- Add comprehensive unit tests for ImGuiApp and related classes (Cursor Agent)
- Cleanup (matt-edmondson)
- Add window visibility throttling for ultra-low resource usage (Cursor Agent)
- Simplify performance update logic and remove unnecessary tracking (Cursor Agent)
- Cleanup (matt-edmondson)
- Remove documentation for deferred FPS/UPS update fix. (Cursor Agent)
- Implement deferred performance updates to prevent mid-cycle rate changes (Cursor Agent)
- Style cleanup (matt-edmondson)
- Cleanup (matt-edmondson)
- Merge branch 'cursor/investigate-unfocused-app-resource-usage-045c' of https://github.com/ktsu-dev/ImGuiApp into cursor/investigate-unfocused-app-resource-usage-045c (matt-edmondson)
- Fix performance rate sync and update throttling to prevent ImGui crashes (Cursor Agent)
- Remove blank lines (matt-edmondson)
- Remove VSync throttling configuration and related code (Cursor Agent)
- Simplify VSync management and remove unnecessary context checks (Cursor Agent)
- Checkpoint before follow-up message (Cursor Agent)
- Fix VSync to prevent resource spikes when unfocused; update .NET SDK. (Cursor Agent)
- Add test coverage for ImGuiApp and related components (Cursor Agent)
- Improve VSync and resource management for unfocused application states (Cursor Agent)
- Update default font size check in ImGuiApp to use FontAppearance.DefaultFontPointSize for improved consistency in font handling. (matt-edmondson)
- Refactor FontHelper and ImGuiApp to simplify character addition and update default font key for compatibility. Removed unnecessary type checks in FontHelper for character ranges and adjusted font index storage in ImGuiApp to dynamically reflect the default font point size. (matt-edmondson)
- Update default font point size in FontAppearance to 14 for improved readability. (matt-edmondson)
- Refactor font loading in ImGuiApp to utilize pre-allocated memory for both main and emoji fonts. This change improves memory management by reusing allocated handles, enhancing performance and reducing memory overhead during font loading. Updated related methods to reflect the new memory handling approach. (matt-edmondson)
- Enhance FontHelper by adding support for extended Unicode and emoji glyph ranges. Introduced initialization flags and cleanup methods to manage memory more effectively. This refactor improves glyph range handling and prevents memory deallocation issues. (matt-edmondson)
- Refactor FontHelper to simplify glyph range additions by removing unnecessary type casting to ushort. This change enhances the handling of character ranges for emoji and Latin Extended characters, improving code clarity and maintainability. (matt-edmondson)
- Enhance emoji font support in ImGuiApp. Introduced LoadEmojiFont method to merge emoji fonts with main fonts, ensuring proper display of emojis. Updated FontHelper to manage emoji-specific glyph ranges separately, improving clarity and avoiding conflicts with main font symbols. Updated ImGuiAppDemo to showcase full emoji range support. (matt-edmondson)
- Add NotoEmoji font support to ImGuiApp. Introduced NotoEmoji.ttf as a resource for emoji display and updated related resource files. Enhanced PowerShell script to preserve manually placed emoji fonts during Nerd Font installation, ensuring full emoji support in the application. (matt-edmondson)
- Replace RobotoMonoNerdFont with NerdFont in ImGuiApp configuration. Add PowerShell script for interactive Nerd Font installation and management, including backup and recovery features. Update resource files to reflect new font integration. (matt-edmondson)
- Add Nerd Font tab to ImGuiAppDemo, showcasing various icon sets including Powerline, Font Awesome, Material Design, Weather, Devicons, Octicons, and Brand Logos. Enhanced user guidance for using Nerd Fonts effectively. (matt-edmondson)
- Add support for Nerd Font icon ranges in FontHelper. Introduced AddNerdFontRanges method to include various icon sets such as Font Awesome, Material Design Icons, and Weather Icons, enhancing glyph range management. (matt-edmondson)
- Refactor FontHelper to modularize glyph range additions for Latin Extended and emoji characters. Updated ImGuiApp to utilize the new methods for improved clarity and maintainability. (matt-edmondson)
- Refactor FontHelper to streamline Unicode and emoji range handling. Removed unused methods and improved memory management for glyph ranges. Updated ImGuiApp to utilize FontHelper for extended Unicode support. (matt-edmondson)
- Merge branch 'cursor/address-question-mark-glyphs-8940' of https://github.com/ktsu-dev/ImGuiApp into cursor/address-question-mark-glyphs-8940 (matt-edmondson)
- Refactor ImGuiFontConfig to enhance Unicode support by consolidating glyph range additions and improving code clarity. Removed redundant comments and streamlined the builder initialization process. (matt-edmondson)
- Refactor ImGuiAppDemo to streamline tab rendering and remove redundant performance tab code (matt-edmondson)
- Improve font memory management with custom font handle tracking (Cursor Agent)
- Merge remote-tracking branch 'origin/main' into cursor/address-question-mark-glyphs-d05e (matt-edmondson)
- Add Reset method tests and reset performance-related state fields (Cursor Agent)
- Fix scissor rectangle calculations in ImGuiController to ensure non-negative dimensions, preventing potential rendering issues. (matt-edmondson)
- Add launch settings for ImGuiAppDemo with native debugging enabled (matt-edmondson)
- Enhance ImGuiApp configuration with debugging options (matt-edmondson)
- Checkpoint before follow-up message (Cursor Agent)
- Refactor ImGuiController for improved code clarity (matt-edmondson)
- Improve rendering precision and pixel-perfect techniques in ImGui rendering (Cursor Agent)
- Improve VSync handling during frame rate throttling (Cursor Agent)
- Refactor Performance tab into separate method and reorder tabs (Cursor Agent)
- Fix merge conflict in performance tab text and update FPS description (Cursor Agent)
- Changes from background agent bc-34f5e701-6497-49ba-b614-0b4bc857f398 (Cursor Agent)
- Add performance throttling with configurable rendering and idle detection (Cursor Agent)
- Refactor demo app with tabbed interface and improved Unicode/emoji display (Cursor Agent)
- Add Unicode and emoji support with configurable font rendering (Cursor Agent)
- Enable Unicode and emoji support by default in ImGuiApp (Cursor Agent)
- Add Reset method tests and reset performance-related state fields (Cursor Agent)
- Merge main into feature branch and integrate performance tab (Cursor Agent)
- Improve VSync handling during frame rate throttling (Cursor Agent)
- Add emoji support to Unicode character ranges in ImGuiApp (Cursor Agent)
- Checkpoint before follow-up message (Cursor Agent)
- Fix scissor rectangle calculations in ImGuiController to ensure non-negative dimensions, preventing potential rendering issues. (matt-edmondson)
- Add launch settings for ImGuiAppDemo with native debugging enabled (matt-edmondson)
- Enhance ImGuiApp configuration with debugging options (matt-edmondson)
- Refactor FontHelper for flexible Unicode support with user-configured fonts (Cursor Agent)
- Add Unicode and emoji font support with cross-platform detection (Cursor Agent)
- Enhance ImGuiAppDemo with new features and UI updates (matt-edmondson)
- Update default performance settings for better resource efficiency (Cursor Agent)
- Add performance throttling with configurable rendering and idle detection (Cursor Agent)
- Refactor ImGuiController for improved code clarity (matt-edmondson)
- Improve rendering precision and pixel-perfect techniques in ImGui rendering (Cursor Agent)
- Fix NuGet version retrieval and update package versions (matt-edmondson)
- Add Directory.Packages.props and global.json for centralized package management (matt-edmondson)
- Add GitHub Actions workflow for automatic SDK updates (matt-edmondson)
- Update CLAUDE.md with additional testing and build instructions; enhance PSBuild script to improve release notes truncation logic for compliance with NuGet character limits, including detailed logging for better traceability. (matt-edmondson)
- Refactor Invoke-DotNetPack function in PSBuild script to improve handling of release notes. Updated logic to create a temporary file for truncated content exceeding NuGet's 35,000 character limit, ensuring compliance and enhancing logging for better traceability. (matt-edmondson)
- Enhance Invoke-DotNetPack function in PSBuild script to handle release notes exceeding NuGet's 35,000 character limit. Added logic to truncate long release notes and create a temporary file for compliance, with appropriate logging and cleanup of temporary files after packaging. (matt-edmondson)
- Enhance Test-IsLibraryOnlyProject function in update-winget-manifests.ps1 (matt-edmondson)
- Enhance New-Changelog function in PSBuild script to truncate release notes exceeding NuGet's 35,000 character limit. This addition ensures compliance with NuGet requirements while providing informative logging about truncation. (matt-edmondson)

## v2.17.2 (patch)

Changes since v2.17.1:

- Renormalize icon LFS pointers (matt-edmondson)

## v2.17.1 (patch)

Changes since v2.17.0:

- Bump MSTest.Sdk from 4.3.0 to 4.3.2 (dependabot[bot])
- Bump the ktsu group with 1 update (dependabot[bot])

## v2.17.0 (minor)

Changes since v2.16.0:

- [minor] Add FullWidth option to SearchBox (matt-edmondson)
- [minor] Add ReturnAllWhenEmpty option to SearchBox (matt-edmondson)
- [minor] Refactor SearchBox to options-record API (matt-edmondson)
- feat(color)!: migrate ImGui.Styler to ktsu.Semantics.Color via ThemeProvider 2.0 (matt-edmondson)

## v2.16.10 (patch)

Changes since v2.16.9:

- Bump the ktsu group with 4 updates (dependabot[bot])

## v2.16.9 (patch)

Changes since v2.16.8:

- Bump the ktsu group with 7 updates (dependabot[bot])
- Bump MSTest.Sdk from 4.2.3 to 4.3.0 (dependabot[bot])
- Bump the ktsu group with 6 updates (dependabot[bot])

## v2.16.8 (patch)

Changes since v2.16.7:

- Bump the ktsu group with 6 updates (dependabot[bot])

## v2.16.7 (patch)

Changes since v2.16.6:

- Bump the ktsu group with 6 updates (dependabot[bot])

## v2.16.6 (patch)

Changes since v2.16.5:

- Bump the ktsu group with 5 updates (dependabot[bot])

## v2.16.5 (patch)

Changes since v2.16.4:

- Bump the ktsu group with 11 updates (dependabot[bot])

## v2.16.4 (patch)

Changes since v2.16.3:

- Bump the ktsu group with 19 updates (dependabot[bot])

## v2.16.3 (patch)

Changes since v2.16.2:

- Bump Polyfill from 10.11.1 to 10.11.2 (dependabot[bot])
- Bump the ktsu group with 3 updates (dependabot[bot])

## v2.16.2 (patch)

Changes since v2.16.1:

- feat(color)!: migrate ImGui.Styler to ktsu.Semantics.Color via ThemeProvider 2.0 (matt-edmondson)

## v2.16.1 (patch)

Changes since v2.16.0:

- Bump Polyfill from 10.11.0 to 10.11.1 (dependabot[bot])
- Bump the ktsu group with 18 updates (dependabot[bot])

## v2.16.0 (minor)

Changes since v2.15.0:

- test(app): de-flake high-precision sleep timing assertions (matt-edmondson)
- feat(imgui-color): add ktsu.ImGui.Color adapter (Color \<-\> ImColor/Vector4) (matt-edmondson)
- chore: remove unused SourceLink package versions (matt-edmondson)
- chore: remove redundant SourceLink package references (matt-edmondson)
- Add compatibility suppressions for new APIs in ImGui libraries (matt-edmondson)
- Add tests for multi-line icon size calculations (matt-edmondson)
- Refactor Icon widget methods to use 'textBlock' parameter and update demo to reflect changes (matt-edmondson)
- Fix collection initialisation (Damon3000s)
- Add required system.text.json package (Damon3000s)
- Always "correctly" align grids in demo (Damon3000s)
- Update demo to show off multi line label Icon widget (Damon3000s)
- Support multiple lines for Icon widget (Damon3000s)
- [patch] Revert SixLabors.ImageSharp to 3.1.12 to avoid license key (matt-edmondson)
- Remove stale files (matt-edmondson)
- Add direct PackageReferences to example projects (Claude)
- Add direct PackageReferences for directly-used transitive packages (Claude)
- Remove unused package versions from Directory.Packages.props (Claude)
- Make divider container respect the ImGui draw cursor (Claude)
- Move iOS jobs out of dotnet.yml into a dedicated ios.yml workflow (Claude)
- [patch] Fix FilesystemBrowser crash on open (matt-edmondson)
- ci: supply SixLabors.ImageSharp 4 license key via SIXLABORS_LICENSE_KEY (Claude)

## v2.15.5 (patch)

Changes since v2.15.4:

- Merge remote-tracking branch 'refs/remotes/origin/main' (ktsu[bot])
- Sync .github\workflows\dotnet.yml (ktsu[bot])
- Sync global.json (ktsu[bot])

## v2.15.4 (patch)

Changes since v2.15.3:

- chore: remove unused SourceLink package versions (matt-edmondson)
- chore: remove redundant SourceLink package references (matt-edmondson)

## v2.15.3 (patch)

Changes since v2.15.2:

- Add compatibility suppressions for new APIs in ImGui libraries (matt-edmondson)
- Add tests for multi-line icon size calculations (matt-edmondson)
- Refactor Icon widget methods to use 'textBlock' parameter and update demo to reflect changes (matt-edmondson)
- Fix collection initialisation (Damon3000s)
- Add required system.text.json package (Damon3000s)
- Always "correctly" align grids in demo (Damon3000s)
- Update demo to show off multi line label Icon widget (Damon3000s)
- Support multiple lines for Icon widget (Damon3000s)
- [patch] Revert SixLabors.ImageSharp to 3.1.12 to avoid license key (matt-edmondson)
- Remove stale files (matt-edmondson)
- Add direct PackageReferences to example projects (Claude)
- Add direct PackageReferences for directly-used transitive packages (Claude)
- Remove unused package versions from Directory.Packages.props (Claude)
- Make divider container respect the ImGui draw cursor (Claude)
- Move iOS jobs out of dotnet.yml into a dedicated ios.yml workflow (Claude)

## v2.15.2 (patch)

Changes since v2.15.1:

- [patch] Fix FilesystemBrowser crash on open (matt-edmondson)

## v2.15.1 (patch)

Changes since v2.15.0:

- ci: supply SixLabors.ImageSharp 4 license key via SIXLABORS_LICENSE_KEY (Claude)

## v2.15.0 (minor)

Changes since v2.14.0:

- Fix floating-point equality reliability issue in XYPad (Claude)
- Fix analyzer errors in embedded hosting and audio widgets (Claude)
- Add embedded-window hosting and audio widgets (Claude)
- feat(ios): curated ImGuiAppDemo.iOS showcase + simulator CI (Task 8, part 3) (#213) (matt-edmondson)
- fix(ios): import ktsu.Semantics.Strings for string.As\<AbsoluteFilePath\> (Claude)
- feat(ios): public texture loading via the Metal backend (Task 8, part 2) (Claude)

## v2.14.3 (patch)

Changes since v2.14.2:

- Bump Polyfill from 10.8.0 to 10.8.1 (dependabot[bot])
- Bump the ktsu group with 3 updates (dependabot[bot])

## v2.14.2 (patch)

Changes since v2.14.1:

- feat(ios): curated ImGuiAppDemo.iOS showcase + simulator CI (Task 8, part 3) (#213) (matt-edmondson)

## v2.14.1 (patch)

Changes since v2.14.0:

- fix(ios): import ktsu.Semantics.Strings for string.As\<AbsoluteFilePath\> (Claude)
- feat(ios): public texture loading via the Metal backend (Task 8, part 2) (Claude)

## v2.14.0 (minor)

Changes since v2.13.0:

- feat(ios): AutoDiscoverExtensions flag (Task 8, part 1) (#211) (matt-edmondson)
- feat(ios): app menu (iPad) + Stop() semantics + no-op surface (Task 7) (#210) (matt-edmondson)
- feat(ios): font parity + imgui.ini redirect (Task 6) (#209) (matt-edmondson)
- ci: fix intermittent coverage broken-pipe flake (exit code 7) (#208) (matt-edmondson)

## v2.13.3 (patch)

Changes since v2.13.2:

- feat(ios): app menu (iPad) + Stop() semantics + no-op surface (Task 7) (#210) (matt-edmondson)

## v2.13.2 (patch)

Changes since v2.13.1:

- feat(ios): font parity + imgui.ini redirect (Task 6) (#209) (matt-edmondson)

## v2.13.1 (patch)

Changes since v2.13.0:

- ci: fix intermittent coverage broken-pipe flake (exit code 7) (#208) (matt-edmondson)

## v2.13.0 (minor)

Changes since v2.12.0:

- feat(ios): touch + keyboard input (Task 5) (#207) (matt-edmondson)

## v2.12.0 (minor)

Changes since v2.11.0:

- docs(ios): mark Task 4 (Metal renderer) complete in the port plan (Claude)
- chore(ios): remove renderer bring-up diagnostics (Claude)
- fix(ios): use TextUnformatted to avoid the variadic igText ARM64 crash (Claude)
- diag(ios): trace OnRender draw calls + log font atlas dims (Claude)
- diag(ios): trace the frame loop to localise the render SIGSEGV (Claude)
- fix(ios): use non-normalized UChar4 for the ImGui vertex colour (Claude)
- fix(ios): write cimgui.dylib to an absolute path (root cause) (Claude)
- fix(ios): stash cimgui.dylib in RUNNER_TEMP so the embed step finds it (Claude)
- fix(ios): copy cimgui.dylib into the .app and dlopen by bundle path (Claude)
- fix(ios): ship cimgui as an embedded dynamic library, dlopen it (Claude)
- diag(ios): inspect the app binary for cimgui link/export status (Claude)
- fix(ios): export dynamic symbols so dlsym resolves static cimgui (Claude)
- diag(ios): probe cimgui symbol resolution before first ImGui call (Claude)
- fix(ios): use ImGui.GetVersionS() for the smoke version probe (Claude)
- fix(ios): pin cimgui to a consistent 1.92.3 docking commit (Claude)
- feat(ios): statically link cimgui so ImGui runs on iOS (Claude)
- fix(ios): satisfy KTSU0003/CA2000 analyzers in the Metal backend (Claude)
- feat(ios): Metal renderer backend (Task 4) - stand up ImGui frames on iOS (Claude)
- wip(ios): begin Metal renderer (Task 4) - shader + frame-loop scaffolding (Claude)

## v2.11.0 (minor)

Changes since v2.10.0:

- ci(ios): iOS-simulator smoke test for the lifecycle (#205) (matt-edmondson)
- ci: re-trigger (flaky ForceDirectedLayout test-host abort) (Claude)
- test: drop redundant (nint) casts on int literals (IDE0004) (Claude)
- [minor] Make GPU texture handles nint end-to-end for the Metal backend (Claude)

## v2.10.0 (minor)

Changes since v2.9.0:

- iOS: satisfy analyzers/nullability in the UIKit lifecycle (Claude)
- docs: record resolved iOS-port design decisions (Claude)
- iOS: native UIKit lifecycle (UIApplicationDelegate + CADisplayLink) (Claude)
- ci: re-trigger workflow (flaky unrelated tests) (Claude)
- iOS: fix net10.0-ios compile errors from the config decoupling (Claude)
- iOS: make the config surface platform-neutral and align Start signature (Claude)

## v2.9.1 (patch)

Changes since v2.9.0:

- Bump Polyfill from 10.7.0 to 10.8.0 (dependabot[bot])

## v2.9.0 (minor)

Changes since v2.8.0:

- Exclude ImGuiAppBlend.cs from the iOS build (Claude)
- Honor ImGui draw-command callbacks; add per-region blend modes (Claude)

## v2.8.0 (minor)

Changes since v2.7.0:

- Remap canvas and restore window on overlay enter/exit (Claude)

## v2.7.0 (minor)

Changes since v2.6.0:

- Refactor SuppressMessage attributes for static fields in ImGuiApp (matt-edmondson)
- [patch] Fix "Renderer backend is not initialized" when loading textures from OnStart (matt-edmondson)
- [minor] Fix SonarQube issues: safe fixes and justified suppressions (round 2/2) (matt-edmondson)
- [minor] Fix SonarQube issues: safe fixes and justified suppressions (round 1/2) (matt-edmondson)

## v2.6.0 (minor)

Changes since v2.5.0:

- Exclude native C ABI shim from code coverage to fix CI test crash (matt-edmondson)
- Fix IDE0055 formatting in demo overlay settings (Claude)
- [minor] Add canonical overlay-mode window support (Claude)
- Fix typo in library name in README.md (matt-edmondson)
- docs: correct package names and audit documentation (Claude)

## v2.5.0 (minor)

Changes since v2.4.0:

- Remove version number from VERSION.md (matt-edmondson)
- style: drop unused using and use id for skeleton shimmer offset (Claude)
- [minor] Add hidden-start and hide-on-close window support (matt-edmondson)
- feat: add Card, SkeletonLoader, and PinInput mobile widgets (Claude)
- style: remove redundant parentheses in Avatar hue calc (IDE0047) (Claude)
- feat: add mobile decorator widgets (Avatar, Badge, Rating, PageIndicator) (Claude)
- ci: re-trigger after flaky code-coverage pipe-disconnect at test session end (Claude)
- ci: re-trigger after flaky NodeGraph.Tests coverage pipe disconnect (Claude)
- style: drop redundant parentheses in RangeSlider tooltip guard (IDE0047) (Claude)
- Fix knob drag value accumulation and stale indicator (Claude)
- feat: add mobile form-control widgets [minor] (Claude)
- feat: add OverlayHost z-ordered overlay manager for ImGui.Widgets (Claude)
- ci: re-trigger after flaky PidFrameLimiter sleep-timing test (Claude)
- style: drop unused System using from InertialScrollTests (Claude)
- style: split inline if-statements in InertialScrollTests for IDE2001 (Claude)
- feat: add InertialScroll helper for ImGui.Widgets (Claude)
- docs: degrade ImGuiController cref to \<c\> for iOS-tfm doc-build (Claude)
- ci: make the iOS stub actually compile on macos-14 (Claude)
- fix: collapse Spring construction into object initializers (IDE0017) (Claude)
- ci: also clear ktsu.Sdk's forced RuntimeIdentifiers on the macOS iOS build (Claude)
- ci: scope iOS restore/build to net10.0-ios only (Claude)
- feat: add Tween, Spring, and Easing animation primitives (Claude)
- ci: add macos-14 job that compile-checks net10.0-ios (Claude)
- feat: add gesture detection foundation for ImGui.Widgets [minor] (Claude)
- docs: plan for mobile UI widgets in ImGui.Widgets (Claude)
- refactor: introduce IRendererBackend seam for the iOS port (Claude)
- docs: design plan for iOS platform port (Claude)
- fix: downgrade SixLabors.ImageSharp to 3.1.12 to restore CI (Claude)
- feat: scaffold net10.0-ios target for ImGui.App (Claude)
- fix: resolve IDE0221 and IDE0380 warnings treated as errors (Claude)
- fix: downgrade SixLabors.ImageSharp from 4.0.0 to 3.1.12 (Claude)
- fix: exclude NativeExports.cs from all SonarCloud analysis (Claude)
- fix: add InternalsVisibleTo for test project (KTSU0002) (Claude)
- fix: suppress CA1823 for intentional ABI struct padding fields (Claude)
- fix: exclude NativeExports.cs from SonarCloud coverage analysis (Claude)
- fix: address MSTest and code analysis violations in ForceDirectedLayout.Tests (Claude)
- fix: convert array initializers to collection expressions in ForceLayoutTests (Claude)
- Fix CI exit code propagation and SonarCloud quality gate failures (Claude)
- Add C ABI surface and AOT-friendly double-precision core (Claude)
- Extract force-directed layout into ktsu.ForceDirectedLayout (Claude)
- Update base directory path (Damon3000s)
- Copy ktsu.png to output directory for ImGuiWidgetsDemo (Damon3000s)
- Fix ImGuiPopupsDemo csproj inclusion (Damon3000s)
- Missed file from dotnet format (Damon3000s)
- Results from dotnet format (Damon3000s)
- Add DESCRIPTION.md and TAGS.md files; update README.md with comprehensive library details and usage examples (matt-edmondson)
- Add SonarLint configuration for connected mode (matt-edmondson)
- Sync .github\workflows\dotnet.yml (KtsuTools)

## v2.4.0 (minor)

Changes since v2.3.0:

- feat(timers): Add countdown and count-up timer demos with radial progress indicators (matt-edmondson)
- Remove legacy build scripts (matt-edmondson)
- Increase MaxForce values in PhysicsSettings and demo to enhance simulation capabilities (matt-edmondson)
- Add directional bias setting and calculation for horizontal link forces (matt-edmondson)
- Update physics settings: adjust repulsion strength, origin anchor weight, damping factor, and link length for improved simulation dynamics (matt-edmondson)
- Enhance physics settings: add OriginAnchorWeight for gravity target blending and initialize world origin to centroid for improved simulation accuracy (matt-edmondson)
- Refactor gravity force calculation: simplify magnitude computation by removing distance factor (matt-edmondson)
- Update repulsion strength in physics settings for enhanced simulation performance (matt-edmondson)
- Adjust repulsion strength limits in physics settings for improved simulation accuracy (matt-edmondson)
- Refactor gravity calculations: update to use centroid for cohesion force and improve rendering of gravity center (matt-edmondson)
- Refine physics settings: adjust damping factor description and clamp minimum repulsion distance to prevent force explosions (matt-edmondson)
- Enhance physics simulation: add node pinning and stability detection features (matt-edmondson)

## v2.3.3 (patch)

Changes since v2.3.2:

- Remove legacy build scripts (matt-edmondson)

## v2.3.2 (patch)

Changes since v2.3.1:

- Sync .github\workflows\dotnet.yml (ktsu[bot])
- Sync global.json (ktsu[bot])

## v2.3.1 (patch)

Changes since v2.3.0:

- Increase MaxForce values in PhysicsSettings and demo to enhance simulation capabilities (matt-edmondson)
- Add directional bias setting and calculation for horizontal link forces (matt-edmondson)
- Update physics settings: adjust repulsion strength, origin anchor weight, damping factor, and link length for improved simulation dynamics (matt-edmondson)
- Enhance physics settings: add OriginAnchorWeight for gravity target blending and initialize world origin to centroid for improved simulation accuracy (matt-edmondson)
- Refactor gravity force calculation: simplify magnitude computation by removing distance factor (matt-edmondson)
- Update repulsion strength in physics settings for enhanced simulation performance (matt-edmondson)
- Adjust repulsion strength limits in physics settings for improved simulation accuracy (matt-edmondson)
- Refactor gravity calculations: update to use centroid for cohesion force and improve rendering of gravity center (matt-edmondson)
- Refine physics settings: adjust damping factor description and clamp minimum repulsion distance to prevent force explosions (matt-edmondson)
- Enhance physics simulation: add node pinning and stability detection features (matt-edmondson)

## v2.3.0 (minor)

Changes since v2.2.0:

- [patch] Guard BeginFrame against calling native extensions without ImGui context (matt-edmondson)
- [minor] Add CleanImNodesDemo with physics simulation and attribute-based node editor (matt-edmondson)
- [minor] Add ImGuiNodeEditor with physics simulation and attribute-based node factory (matt-edmondson)
- [minor] Add NodeGraph test suite with 106 tests covering attributes, pins, type system, and validation (matt-edmondson)
- [minor] Add NodeGraph library with attribute-based node definitions (matt-edmondson)
- [minor] Refactor demo app with modular tab-based architecture and extension demos (matt-edmondson)
- [minor] Add unit tests for ImGuiExtensionManager (matt-edmondson)
- [minor] Integrate ImGuiExtensionManager into ImGuiController lifecycle (matt-edmondson)
- [minor] Add ImGuiExtensionManager for optional ImGuizmo, ImNodes, and ImPlot support (matt-edmondson)
- [minor] Add Hexa.NET.ImGuizmo, ImNodes, and ImPlot package references (matt-edmondson)
- Add visibility control for tabs in TabPanel (matt-edmondson)
- Exclude test projects from packaging and publishing processes in Invoke-DotNetPack and Invoke-DotNetPublish functions (matt-edmondson)
- Add compatibility suppressions for DefaultInterpolatedStringHandler in multiple modules (matt-edmondson)
- Add compatibility suppressions for DynamicallyAccessedMemberTypes and ExperimentalAttribute in ImGui.Popups, ImGui.Styler, and ImGui.Widgets for .NET 10.0 (matt-edmondson)
- Refine glyph area calculations and atlas fitting checks for improved memory management (matt-edmondson)
- Refactor null checks to use Ensure.NotNull for improved readability and consistency (matt-edmondson)
- Remove .github\workflows\project.yml (matt-edmondson)
- Enhance project name matching to handle variations in repository naming conventions (matt-edmondson)
- Enhance CalculateOptimalPixelSize to consider global accessibility scale for improved rendering (matt-edmondson)
- Refactor null argument checks to use Ensure.NotNull for improved readability (matt-edmondson)
- Improve search box hint display logic based on available width (matt-edmondson)
- Add CLAUDE.md for project guidance and architecture overview (matt-edmondson)
- migrate to dotnet 10 (matt-edmondson)
- Dont show the close button on tabs inside a non-closable tab bar (matt-edmondson)

## v2.2.12-pre.1 (prerelease)

Changes since v2.2.11:

- Fix all formatting errors to make build green ([@Copilot](https://github.com/Copilot))
- Change default direction to clockwise and add StartAtBottom option ([@Copilot](https://github.com/Copilot))
- Update README example function name to follow conventions ([@Copilot](https://github.com/Copilot))
- Add input validation and optimize string allocation ([@Copilot](https://github.com/Copilot))
- Fix clockwise/counter-clockwise logic and remove empty if block ([@Copilot](https://github.com/Copilot))
- Add RadialProgressBar widget implementation and demo ([@Copilot](https://github.com/Copilot))
- Initial plan ([@Copilot](https://github.com/Copilot))

## v2.2.11 (patch)

Changes since v2.2.10:

- Add visibility control for tabs in TabPanel (matt-edmondson)

## v2.2.11-pre.2 (prerelease)

Changes since v2.2.11-pre.1:

- Merge remote-tracking branch 'refs/remotes/origin/main' (ktsu[bot])
- Sync scripts\update-winget-manifests.ps1 (ktsu[bot])

## v2.2.11-pre.1 (prerelease)

Changes since v2.2.10:

- Sync scripts\update-winget-manifests.ps1 (ktsu[bot])
- Sync scripts\PSBuild.psm1 (ktsu[bot])
- Sync global.json (ktsu[bot])

## v2.2.10 (patch)

Changes since v2.2.9:

- Exclude test projects from packaging and publishing processes in Invoke-DotNetPack and Invoke-DotNetPublish functions (matt-edmondson)

## v2.2.10-pre.2 (prerelease)

Changes since v2.2.10-pre.1:

- Merge remote-tracking branch 'refs/remotes/origin/main' (ktsu[bot])
- Sync global.json (ktsu[bot])

## v2.2.10-pre.1 (prerelease)

Changes since v2.2.9:

- Fix workflow to skip metadata commits in forked repos ([@Copilot](https://github.com/Copilot))
- Initial plan ([@Copilot](https://github.com/Copilot))

## v2.2.9 (patch)

Changes since v2.2.8:

- Add compatibility suppressions for DefaultInterpolatedStringHandler in multiple modules (matt-edmondson)

## v2.2.9-pre.2 (prerelease)

Changes since v2.2.9-pre.1:

- Merge remote-tracking branch 'refs/remotes/origin/main' (ktsu[bot])
- Sync global.json (ktsu[bot])
- Sync COPYRIGHT.md (ktsu[bot])

## v2.2.9-pre.1 (prerelease)

Changes since v2.2.8:

- Merge remote-tracking branch 'refs/remotes/origin/main' (ktsu[bot])
- Sync scripts\PSBuild.psm1 (ktsu[bot])

## v2.2.8 (patch)

Changes since v2.2.7:

- Add compatibility suppressions for DynamicallyAccessedMemberTypes and ExperimentalAttribute in ImGui.Popups, ImGui.Styler, and ImGui.Widgets for .NET 10.0 (matt-edmondson)
- Refine glyph area calculations and atlas fitting checks for improved memory management (matt-edmondson)
- Refactor null checks to use Ensure.NotNull for improved readability and consistency (matt-edmondson)

## v2.2.7 (patch)

Changes since v2.2.6:

- Remove .github\workflows\project.yml (matt-edmondson)

## v2.2.6 (patch)

Changes since v2.2.5:

- Enhance project name matching to handle variations in repository naming conventions (matt-edmondson)

## v2.2.5 (patch)

Changes since v2.2.4:

- Enhance CalculateOptimalPixelSize to consider global accessibility scale for improved rendering (matt-edmondson)

## v2.2.4 (patch)

Changes since v2.2.3:

- Refactor null argument checks to use Ensure.NotNull for improved readability (matt-edmondson)

## v2.2.3 (patch)

Changes since v2.2.2:

- Improve search box hint display logic based on available width (matt-edmondson)
- Add CLAUDE.md for project guidance and architecture overview (matt-edmondson)

## v2.2.2 (patch)

Changes since v2.2.1:

- migrate to dotnet 10 (matt-edmondson)

## v2.2.1 (patch)

Changes since v2.2.0:

- Dont show the close button on tabs inside a non-closable tab bar (matt-edmondson)

## v2.2.1-pre.1 (prerelease)

Changes since v2.2.0:

- Sync scripts\update-winget-manifests.ps1 (ktsu[bot])

## v2.2.0 (minor)

Changes since v2.1.0:

- Refactor glyph calculation for improved readability (matt-edmondson)
- [minor] Add dynamic atlas sizing and glyph limit calculation (matt-edmondson)
- Fix gpu detection priority (matt-edmondson)
- Update tests/ImGui.App.Tests/FontMemoryGuardTests.cs (matt-edmondson)
- Update ImGui.App/FontMemoryGuard.cs to improve null checking (matt-edmondson)
- Update ImGui.App/FontMemoryGuard.cs to have more specific matching criteria (matt-edmondson)
- Update variable name ImGui.App/ImGuiApp.cs (matt-edmondson)
- Enhance font initialization with memory management features (matt-edmondson)
- Fix missing package references (matt-edmondson)
- Increase timeout for build job to 20 minutes (matt-edmondson)
- Enhance project structure and testing: Added new dependencies in Directory.Packages.props, introduced a new Tests project in the solution, and updated project references. Refactored namespaces for consistency across multiple files. Updated test configurations and example projects to align with the new structure. (matt-edmondson)
- Update project structure and dependencies: Added new package versions in Directory.Packages.props, updated SDK versions in global.json, and refactored namespaces across multiple files for consistency. Removed the ImGui.Popups.Credential project and adjusted related references in the solution and project files. Enhanced test project configurations and updated example projects to reflect the new structure. (matt-edmondson)
- Initial combined commit (matt-edmondson)
- Fix NuGet package source URL in Invoke-NuGetPublish function: Updated the source URL to ensure correct package publishing to packages.ktsu.dev. (matt-edmondson)
- Enhance .NET CI workflow: Added support for skipped releases in the GitHub Actions workflow. Updated conditions for SonarQube execution, coverage report upload, and Winget manifest updates to account for skipped releases, improving control over the release process. (matt-edmondson)
- Add Ktsu package key support in build configuration: Updated the .NET CI workflow and PowerShell script to include an optional Ktsu package key for publishing. Enhanced documentation for the new parameter and added conditional publishing logic for Ktsu.dev. (matt-edmondson)
- Implement modern DPI awareness handling in Windows: Updated ForceDpiAware to utilize the latest DPI awareness APIs for better compatibility with windowing libraries. Added fallback mechanisms for older Windows versions and enhanced NativeMethods with new DPI awareness context functions. (matt-edmondson)
- Enhance window position validation logic: Implemented performance optimizations to skip unnecessary checks when window position and size remain unchanged. Added methods for better multi-monitor support, ensuring windows are relocated when insufficiently visible. Updated tests to verify new behavior and performance improvements. (matt-edmondson)
- Update package versions and clean up validation logic: Bump versions for Hexa.NET.ImGui, ktsu.ScopedAction, SixLabors.ImageSharp, System.Text.Json, and MSTest packages. Remove redundant validation checks from ImGuiApp configuration and corresponding tests. (matt-edmondson)
- Add manual trigger support to GitHub Actions workflow: Enabled workflow_dispatch to allow manual execution of the .NET CI pipeline. (matt-edmondson)
- Refactor SonarQube token handling in GitHub Actions: Updated conditional checks to use environment variables for SONAR_TOKEN, ensuring consistent access across caching and installation steps. (matt-edmondson)
- Refactor ImGuiApp configuration handling: Introduced AdjustConfigForStartup method to automatically convert minimized window state to normal during startup, improving application reliability. Updated tests to validate this new behavior. (matt-edmondson)
- Update ImGuiApp configuration validation: Automatically convert minimized and fullscreen window states to normal during startup to prevent issues. Updated tests to reflect this change, ensuring proper state handling without exceptions. (matt-edmondson)
- Refactor SonarQube conditional checks in GitHub Actions: Updated syntax for SONAR_TOKEN checks to use the correct expression format, ensuring proper execution of caching and installation steps. (matt-edmondson)
- Enhance SonarQube integration in GitHub Actions: Added conditional checks for SONAR_TOKEN to ensure caching and installation steps only execute when the token is available, improving workflow reliability. (matt-edmondson)
- Additional tests (matt-edmondson)
- Move debug logger into its own file and make it output to the appdata dir (matt-edmondson)
- Move debug logger into its own file and make it output to the appdata dir (matt-edmondson)

## v2.1.10 (patch)

Changes since v2.1.9:

- Fix gpu detection priority (matt-edmondson)
- Update tests/ImGui.App.Tests/FontMemoryGuardTests.cs (matt-edmondson)
- Update ImGui.App/FontMemoryGuard.cs to improve null checking (matt-edmondson)
- Update ImGui.App/FontMemoryGuard.cs to have more specific matching criteria (matt-edmondson)
- Update variable name ImGui.App/ImGuiApp.cs (matt-edmondson)
- Enhance font initialization with memory management features (matt-edmondson)

## v2.1.10-pre.2 (prerelease)

Changes since v2.1.10-pre.1:

- Sync scripts\PSBuild.psm1 (ktsu[bot])
- Sync scripts\update-winget-manifests.ps1 (ktsu[bot])
- Sync .github\workflows\update-sdks.yml (ktsu[bot])
- Sync .github\workflows\dotnet.yml (ktsu[bot])
- Sync .github\dependabot.yml (ktsu[bot])
- Sync .editorconfig (ktsu[bot])
- Sync .gitattributes (ktsu[bot])
- Sync .runsettings (ktsu[bot])

## v2.1.10-pre.1 (prerelease)

Changes since v2.1.9:

- Add comprehensive unit tests for GlobalScale feature ([@Copilot](https://github.com/Copilot))
- Add OnGlobalScaleChanged callback example to demo app ([@Copilot](https://github.com/Copilot))
- Add global scale control for accessibility ([@Copilot](https://github.com/Copilot))
- Fix bounce animation alignment by centering offset range ([@Copilot](https://github.com/Copilot))
- Skip committing metadata changes in fork repositories ([@Copilot](https://github.com/Copilot))
- Initial plan ([@Copilot](https://github.com/Copilot))
- Initial plan ([@Copilot](https://github.com/Copilot))
- Initial plan ([@Copilot](https://github.com/Copilot))
- Add GitHub Copilot instructions ([@Copilot](https://github.com/Copilot))
- Initial plan ([@Copilot](https://github.com/Copilot))

## v2.1.9 (patch)

Changes since v2.1.8:

- Fix missing package references (matt-edmondson)

## v2.1.8 (patch)

Changes since v2.1.7:

- Increase timeout for build job to 20 minutes (matt-edmondson)
- Enhance project structure and testing: Added new dependencies in Directory.Packages.props, introduced a new Tests project in the solution, and updated project references. Refactored namespaces for consistency across multiple files. Updated test configurations and example projects to align with the new structure. (matt-edmondson)
- Update project structure and dependencies: Added new package versions in Directory.Packages.props, updated SDK versions in global.json, and refactored namespaces across multiple files for consistency. Removed the ImGui.Popups.Credential project and adjusted related references in the solution and project files. Enhanced test project configurations and updated example projects to reflect the new structure. (matt-edmondson)
- Initial combined commit (matt-edmondson)

## v2.1.7 (patch)

Changes since v2.1.6:

- Fix NuGet package source URL in Invoke-NuGetPublish function: Updated the source URL to ensure correct package publishing to packages.ktsu.dev. (matt-edmondson)
- Enhance .NET CI workflow: Added support for skipped releases in the GitHub Actions workflow. Updated conditions for SonarQube execution, coverage report upload, and Winget manifest updates to account for skipped releases, improving control over the release process. (matt-edmondson)

## v2.1.6 (patch)

Changes since v2.1.5:

- Add Ktsu package key support in build configuration: Updated the .NET CI workflow and PowerShell script to include an optional Ktsu package key for publishing. Enhanced documentation for the new parameter and added conditional publishing logic for Ktsu.dev. (matt-edmondson)

## v2.1.5 (patch)

Changes since v2.1.4:

- Implement modern DPI awareness handling in Windows: Updated ForceDpiAware to utilize the latest DPI awareness APIs for better compatibility with windowing libraries. Added fallback mechanisms for older Windows versions and enhanced NativeMethods with new DPI awareness context functions. (matt-edmondson)

## v2.1.4 (patch)

Changes since v2.1.3:

- Enhance window position validation logic: Implemented performance optimizations to skip unnecessary checks when window position and size remain unchanged. Added methods for better multi-monitor support, ensuring windows are relocated when insufficiently visible. Updated tests to verify new behavior and performance improvements. (matt-edmondson)
- Update package versions and clean up validation logic: Bump versions for Hexa.NET.ImGui, ktsu.ScopedAction, SixLabors.ImageSharp, System.Text.Json, and MSTest packages. Remove redundant validation checks from ImGuiApp configuration and corresponding tests. (matt-edmondson)

## v2.1.3 (patch)

Changes since v2.1.2:

- Add manual trigger support to GitHub Actions workflow: Enabled workflow_dispatch to allow manual execution of the .NET CI pipeline. (matt-edmondson)

## v2.1.2 (patch)

Changes since v2.1.1:

- Refactor SonarQube token handling in GitHub Actions: Updated conditional checks to use environment variables for SONAR_TOKEN, ensuring consistent access across caching and installation steps. (matt-edmondson)
- Refactor ImGuiApp configuration handling: Introduced AdjustConfigForStartup method to automatically convert minimized window state to normal during startup, improving application reliability. Updated tests to validate this new behavior. (matt-edmondson)
- Update ImGuiApp configuration validation: Automatically convert minimized and fullscreen window states to normal during startup to prevent issues. Updated tests to reflect this change, ensuring proper state handling without exceptions. (matt-edmondson)
- Refactor SonarQube conditional checks in GitHub Actions: Updated syntax for SONAR_TOKEN checks to use the correct expression format, ensuring proper execution of caching and installation steps. (matt-edmondson)
- Enhance SonarQube integration in GitHub Actions: Added conditional checks for SONAR_TOKEN to ensure caching and installation steps only execute when the token is available, improving workflow reliability. (matt-edmondson)

## v2.1.1 (patch)

Changes since v2.1.0:

- Additional tests (matt-edmondson)
- Move debug logger into its own file and make it output to the appdata dir (matt-edmondson)
- Move debug logger into its own file and make it output to the appdata dir (matt-edmondson)

## v2.1.0 (minor)

Changes since v2.0.0:

- [minor] Implement PID-based frame limiting in ImGuiApp: Introduced a new PidFrameLimiter class for precise frame rate control, enhancing performance optimization. Updated documentation to reflect new features, including auto-tuning capabilities and real-time diagnostics. Adjusted rendering settings to disable VSync for improved frame limiting accuracy. (matt-edmondson)
- Enhance ImGuiApp documentation and features: Updated project overview, added detailed descriptions for performance optimization, debug logging, and Unicode support. Introduced performance monitoring capabilities with real-time FPS tracking and throttling visualization. Improved font management and DPI handling. Refactored configuration settings for better usability. Updated demo application to showcase new features. (matt-edmondson)
- Refactor performance settings: remove Ups, add NotVisibleFps and flags (Cursor Agent)
- Auto-commit pending changes before rebase - PR synchronize (Cursor Agent)
- Merge remote-tracking branch 'origin/main' into cursor/increase-imguiapp-test-coverage-c9d4 (matt-edmondson)
- Fix test paths using Path.GetFullPath for consistent texture testing (Cursor Agent)
- Add test for preventing multiple ImGuiApp starts (Cursor Agent)
- Cleanup (matt-edmondson)
- Enhance performance throttling with lowest-rate selection logic (Cursor Agent)
- Increase NotVisibleFps from 0.2 to 2.0 for better background performance (Cursor Agent)
- Add real-time FPS graph with throttling state visualization (Cursor Agent)
- Add NotVisibleFps setting for ultra-low frame rate when minimized (Cursor Agent)
- Adjust not visible frame rate to 0.2 FPS for better resource conservation (Cursor Agent)
- Refactor ImGuiApp tests to use Assert.ThrowsException method (Cursor Agent)
- Implement sleep-based frame rate throttling and remove UPS settings (Cursor Agent)
- Checkpoint before follow-up message (Cursor Agent)
- Update ImGuiFontConfig test to allow empty font path (Cursor Agent)
- Use PackageReleaseNotesFile to handle changelog release notes more robustly (Cursor Agent)
- Improve performance throttling with multi-condition rate selection (Cursor Agent)
- Remove debug throttling properties and simplify focus handling (Cursor Agent)
- Fix input focus detection and add throttling debug info (Cursor Agent)
- Checkpoint before follow-up message (Cursor Agent)
- Add comprehensive test coverage for ImGuiApp components and edge cases (Cursor Agent)
- Remove focus checks from input event handlers (Cursor Agent)
- Fix input focus detection to prevent incorrect idle state management (Cursor Agent)
- Cleanup (matt-edmondson)
- Improve window focus detection and add debug logging for throttling (Cursor Agent)
- Refactor test suite into focused, organized test classes (Cursor Agent)
- Checkpoint before follow-up message (Cursor Agent)
- Cleanup (matt-edmondson)
- Implement lowest frame rate throttling with comprehensive condition evaluation (Cursor Agent)
- Checkpoint before follow-up message (Cursor Agent)
- Add comprehensive unit tests for ImGuiApp and related classes (Cursor Agent)
- Cleanup (matt-edmondson)
- Add window visibility throttling for ultra-low resource usage (Cursor Agent)
- Simplify performance update logic and remove unnecessary tracking (Cursor Agent)
- Cleanup (matt-edmondson)
- Remove documentation for deferred FPS/UPS update fix. (Cursor Agent)
- Implement deferred performance updates to prevent mid-cycle rate changes (Cursor Agent)
- Style cleanup (matt-edmondson)
- Cleanup (matt-edmondson)
- Merge branch 'cursor/investigate-unfocused-app-resource-usage-045c' of https://github.com/ktsu-dev/ImGuiApp into cursor/investigate-unfocused-app-resource-usage-045c (matt-edmondson)
- Fix performance rate sync and update throttling to prevent ImGui crashes (Cursor Agent)
- Remove blank lines (matt-edmondson)
- Remove VSync throttling configuration and related code (Cursor Agent)
- Simplify VSync management and remove unnecessary context checks (Cursor Agent)
- Checkpoint before follow-up message (Cursor Agent)
- Fix VSync to prevent resource spikes when unfocused; update .NET SDK. (Cursor Agent)
- Add test coverage for ImGuiApp and related components (Cursor Agent)
- Improve VSync and resource management for unfocused application states (Cursor Agent)
- Update default font size check in ImGuiApp to use FontAppearance.DefaultFontPointSize for improved consistency in font handling. (matt-edmondson)
- Refactor FontHelper and ImGuiApp to simplify character addition and update default font key for compatibility. Removed unnecessary type checks in FontHelper for character ranges and adjusted font index storage in ImGuiApp to dynamically reflect the default font point size. (matt-edmondson)
- Update default font point size in FontAppearance to 14 for improved readability. (matt-edmondson)
- Refactor font loading in ImGuiApp to utilize pre-allocated memory for both main and emoji fonts. This change improves memory management by reusing allocated handles, enhancing performance and reducing memory overhead during font loading. Updated related methods to reflect the new memory handling approach. (matt-edmondson)
- Enhance FontHelper by adding support for extended Unicode and emoji glyph ranges. Introduced initialization flags and cleanup methods to manage memory more effectively. This refactor improves glyph range handling and prevents memory deallocation issues. (matt-edmondson)
- Refactor FontHelper to simplify glyph range additions by removing unnecessary type casting to ushort. This change enhances the handling of character ranges for emoji and Latin Extended characters, improving code clarity and maintainability. (matt-edmondson)
- Enhance emoji font support in ImGuiApp. Introduced LoadEmojiFont method to merge emoji fonts with main fonts, ensuring proper display of emojis. Updated FontHelper to manage emoji-specific glyph ranges separately, improving clarity and avoiding conflicts with main font symbols. Updated ImGuiAppDemo to showcase full emoji range support. (matt-edmondson)
- Add NotoEmoji font support to ImGuiApp. Introduced NotoEmoji.ttf as a resource for emoji display and updated related resource files. Enhanced PowerShell script to preserve manually placed emoji fonts during Nerd Font installation, ensuring full emoji support in the application. (matt-edmondson)
- Replace RobotoMonoNerdFont with NerdFont in ImGuiApp configuration. Add PowerShell script for interactive Nerd Font installation and management, including backup and recovery features. Update resource files to reflect new font integration. (matt-edmondson)
- Add Nerd Font tab to ImGuiAppDemo, showcasing various icon sets including Powerline, Font Awesome, Material Design, Weather, Devicons, Octicons, and Brand Logos. Enhanced user guidance for using Nerd Fonts effectively. (matt-edmondson)
- Add support for Nerd Font icon ranges in FontHelper. Introduced AddNerdFontRanges method to include various icon sets such as Font Awesome, Material Design Icons, and Weather Icons, enhancing glyph range management. (matt-edmondson)
- Refactor FontHelper to modularize glyph range additions for Latin Extended and emoji characters. Updated ImGuiApp to utilize the new methods for improved clarity and maintainability. (matt-edmondson)
- Refactor FontHelper to streamline Unicode and emoji range handling. Removed unused methods and improved memory management for glyph ranges. Updated ImGuiApp to utilize FontHelper for extended Unicode support. (matt-edmondson)
- Merge branch 'cursor/address-question-mark-glyphs-8940' of https://github.com/ktsu-dev/ImGuiApp into cursor/address-question-mark-glyphs-8940 (matt-edmondson)
- Refactor ImGuiFontConfig to enhance Unicode support by consolidating glyph range additions and improving code clarity. Removed redundant comments and streamlined the builder initialization process. (matt-edmondson)
- Refactor ImGuiAppDemo to streamline tab rendering and remove redundant performance tab code (matt-edmondson)
- Improve font memory management with custom font handle tracking (Cursor Agent)
- Merge remote-tracking branch 'origin/main' into cursor/address-question-mark-glyphs-d05e (matt-edmondson)
- Add Reset method tests and reset performance-related state fields (Cursor Agent)
- Fix scissor rectangle calculations in ImGuiController to ensure non-negative dimensions, preventing potential rendering issues. (matt-edmondson)
- Add launch settings for ImGuiAppDemo with native debugging enabled (matt-edmondson)
- Enhance ImGuiApp configuration with debugging options (matt-edmondson)
- Checkpoint before follow-up message (Cursor Agent)
- Refactor ImGuiController for improved code clarity (matt-edmondson)
- Improve rendering precision and pixel-perfect techniques in ImGui rendering (Cursor Agent)
- Improve VSync handling during frame rate throttling (Cursor Agent)
- Refactor Performance tab into separate method and reorder tabs (Cursor Agent)
- Fix merge conflict in performance tab text and update FPS description (Cursor Agent)
- Changes from background agent bc-34f5e701-6497-49ba-b614-0b4bc857f398 (Cursor Agent)
- Add performance throttling with configurable rendering and idle detection (Cursor Agent)
- Refactor demo app with tabbed interface and improved Unicode/emoji display (Cursor Agent)
- Add Unicode and emoji support with configurable font rendering (Cursor Agent)
- Enable Unicode and emoji support by default in ImGuiApp (Cursor Agent)
- Add Reset method tests and reset performance-related state fields (Cursor Agent)
- Merge main into feature branch and integrate performance tab (Cursor Agent)
- Improve VSync handling during frame rate throttling (Cursor Agent)
- Add emoji support to Unicode character ranges in ImGuiApp (Cursor Agent)
- Checkpoint before follow-up message (Cursor Agent)
- Fix scissor rectangle calculations in ImGuiController to ensure non-negative dimensions, preventing potential rendering issues. (matt-edmondson)
- Add launch settings for ImGuiAppDemo with native debugging enabled (matt-edmondson)
- Enhance ImGuiApp configuration with debugging options (matt-edmondson)
- Refactor FontHelper for flexible Unicode support with user-configured fonts (Cursor Agent)
- Add Unicode and emoji font support with cross-platform detection (Cursor Agent)
- Enhance ImGuiAppDemo with new features and UI updates (matt-edmondson)
- Update default performance settings for better resource efficiency (Cursor Agent)
- Add performance throttling with configurable rendering and idle detection (Cursor Agent)
- Refactor ImGuiController for improved code clarity (matt-edmondson)
- Improve rendering precision and pixel-perfect techniques in ImGui rendering (Cursor Agent)
- Fix NuGet version retrieval and update package versions (matt-edmondson)
- Add Directory.Packages.props and global.json for centralized package management (matt-edmondson)
- Add GitHub Actions workflow for automatic SDK updates (matt-edmondson)
- Update CLAUDE.md with additional testing and build instructions; enhance PSBuild script to improve release notes truncation logic for compliance with NuGet character limits, including detailed logging for better traceability. (matt-edmondson)
- Refactor Invoke-DotNetPack function in PSBuild script to improve handling of release notes. Updated logic to create a temporary file for truncated content exceeding NuGet's 35,000 character limit, ensuring compliance and enhancing logging for better traceability. (matt-edmondson)
- Enhance Invoke-DotNetPack function in PSBuild script to handle release notes exceeding NuGet's 35,000 character limit. Added logic to truncate long release notes and create a temporary file for compliance, with appropriate logging and cleanup of temporary files after packaging. (matt-edmondson)
- Enhance Test-IsLibraryOnlyProject function in update-winget-manifests.ps1 (matt-edmondson)
- Enhance New-Changelog function in PSBuild script to truncate release notes exceeding NuGet's 35,000 character limit. This addition ensures compliance with NuGet requirements while providing informative logging about truncation. (matt-edmondson)

## v2.0.12 (patch)

Changes since v2.0.11:

- Refactor performance settings: remove Ups, add NotVisibleFps and flags (Cursor Agent)
- Auto-commit pending changes before rebase - PR synchronize (Cursor Agent)
- Merge remote-tracking branch 'origin/main' into cursor/increase-imguiapp-test-coverage-c9d4 (matt-edmondson)
- Fix test paths using Path.GetFullPath for consistent texture testing (Cursor Agent)
- Add test for preventing multiple ImGuiApp starts (Cursor Agent)
- Refactor ImGuiApp tests to use Assert.ThrowsException method (Cursor Agent)
- Checkpoint before follow-up message (Cursor Agent)
- Update ImGuiFontConfig test to allow empty font path (Cursor Agent)
- Add comprehensive test coverage for ImGuiApp components and edge cases (Cursor Agent)
- Refactor test suite into focused, organized test classes (Cursor Agent)
- Add comprehensive unit tests for ImGuiApp and related classes (Cursor Agent)
- Add test coverage for ImGuiApp and related components (Cursor Agent)

## v2.0.11 (patch)

Changes since v2.0.10:

- Cleanup (matt-edmondson)
- Enhance performance throttling with lowest-rate selection logic (Cursor Agent)
- Increase NotVisibleFps from 0.2 to 2.0 for better background performance (Cursor Agent)
- Add real-time FPS graph with throttling state visualization (Cursor Agent)
- Add NotVisibleFps setting for ultra-low frame rate when minimized (Cursor Agent)
- Adjust not visible frame rate to 0.2 FPS for better resource conservation (Cursor Agent)
- Implement sleep-based frame rate throttling and remove UPS settings (Cursor Agent)
- Improve performance throttling with multi-condition rate selection (Cursor Agent)
- Remove debug throttling properties and simplify focus handling (Cursor Agent)
- Fix input focus detection and add throttling debug info (Cursor Agent)
- Checkpoint before follow-up message (Cursor Agent)
- Remove focus checks from input event handlers (Cursor Agent)
- Fix input focus detection to prevent incorrect idle state management (Cursor Agent)
- Cleanup (matt-edmondson)
- Improve window focus detection and add debug logging for throttling (Cursor Agent)
- Checkpoint before follow-up message (Cursor Agent)
- Cleanup (matt-edmondson)
- Implement lowest frame rate throttling with comprehensive condition evaluation (Cursor Agent)
- Checkpoint before follow-up message (Cursor Agent)
- Cleanup (matt-edmondson)
- Add window visibility throttling for ultra-low resource usage (Cursor Agent)
- Simplify performance update logic and remove unnecessary tracking (Cursor Agent)
- Cleanup (matt-edmondson)
- Remove documentation for deferred FPS/UPS update fix. (Cursor Agent)
- Implement deferred performance updates to prevent mid-cycle rate changes (Cursor Agent)
- Style cleanup (matt-edmondson)
- Cleanup (matt-edmondson)
- Merge branch 'cursor/investigate-unfocused-app-resource-usage-045c' of https://github.com/ktsu-dev/ImGuiApp into cursor/investigate-unfocused-app-resource-usage-045c (matt-edmondson)
- Fix performance rate sync and update throttling to prevent ImGui crashes (Cursor Agent)
- Remove blank lines (matt-edmondson)
- Remove VSync throttling configuration and related code (Cursor Agent)
- Simplify VSync management and remove unnecessary context checks (Cursor Agent)
- Checkpoint before follow-up message (Cursor Agent)
- Fix VSync to prevent resource spikes when unfocused; update .NET SDK. (Cursor Agent)
- Improve VSync and resource management for unfocused application states (Cursor Agent)

## v2.0.10 (patch)

Changes since v2.0.9:

- Use PackageReleaseNotesFile to handle changelog release notes more robustly (Cursor Agent)

## v2.0.9 (patch)

Changes since v2.0.8:

- Update default font size check in ImGuiApp to use FontAppearance.DefaultFontPointSize for improved consistency in font handling. (matt-edmondson)
- Refactor FontHelper and ImGuiApp to simplify character addition and update default font key for compatibility. Removed unnecessary type checks in FontHelper for character ranges and adjusted font index storage in ImGuiApp to dynamically reflect the default font point size. (matt-edmondson)
- Update default font point size in FontAppearance to 14 for improved readability. (matt-edmondson)
- Refactor font loading in ImGuiApp to utilize pre-allocated memory for both main and emoji fonts. This change improves memory management by reusing allocated handles, enhancing performance and reducing memory overhead during font loading. Updated related methods to reflect the new memory handling approach. (matt-edmondson)
- Enhance FontHelper by adding support for extended Unicode and emoji glyph ranges. Introduced initialization flags and cleanup methods to manage memory more effectively. This refactor improves glyph range handling and prevents memory deallocation issues. (matt-edmondson)
- Refactor FontHelper to simplify glyph range additions by removing unnecessary type casting to ushort. This change enhances the handling of character ranges for emoji and Latin Extended characters, improving code clarity and maintainability. (matt-edmondson)
- Enhance emoji font support in ImGuiApp. Introduced LoadEmojiFont method to merge emoji fonts with main fonts, ensuring proper display of emojis. Updated FontHelper to manage emoji-specific glyph ranges separately, improving clarity and avoiding conflicts with main font symbols. Updated ImGuiAppDemo to showcase full emoji range support. (matt-edmondson)
- Add NotoEmoji font support to ImGuiApp. Introduced NotoEmoji.ttf as a resource for emoji display and updated related resource files. Enhanced PowerShell script to preserve manually placed emoji fonts during Nerd Font installation, ensuring full emoji support in the application. (matt-edmondson)
- Replace RobotoMonoNerdFont with NerdFont in ImGuiApp configuration. Add PowerShell script for interactive Nerd Font installation and management, including backup and recovery features. Update resource files to reflect new font integration. (matt-edmondson)
- Add Nerd Font tab to ImGuiAppDemo, showcasing various icon sets including Powerline, Font Awesome, Material Design, Weather, Devicons, Octicons, and Brand Logos. Enhanced user guidance for using Nerd Fonts effectively. (matt-edmondson)
- Add support for Nerd Font icon ranges in FontHelper. Introduced AddNerdFontRanges method to include various icon sets such as Font Awesome, Material Design Icons, and Weather Icons, enhancing glyph range management. (matt-edmondson)
- Refactor FontHelper to modularize glyph range additions for Latin Extended and emoji characters. Updated ImGuiApp to utilize the new methods for improved clarity and maintainability. (matt-edmondson)
- Refactor FontHelper to streamline Unicode and emoji range handling. Removed unused methods and improved memory management for glyph ranges. Updated ImGuiApp to utilize FontHelper for extended Unicode support. (matt-edmondson)
- Merge branch 'cursor/address-question-mark-glyphs-8940' of https://github.com/ktsu-dev/ImGuiApp into cursor/address-question-mark-glyphs-8940 (matt-edmondson)
- Refactor ImGuiFontConfig to enhance Unicode support by consolidating glyph range additions and improving code clarity. Removed redundant comments and streamlined the builder initialization process. (matt-edmondson)
- Refactor ImGuiAppDemo to streamline tab rendering and remove redundant performance tab code (matt-edmondson)
- Improve font memory management with custom font handle tracking (Cursor Agent)
- Merge remote-tracking branch 'origin/main' into cursor/address-question-mark-glyphs-d05e (matt-edmondson)
- Add Reset method tests and reset performance-related state fields (Cursor Agent)
- Fix scissor rectangle calculations in ImGuiController to ensure non-negative dimensions, preventing potential rendering issues. (matt-edmondson)
- Add launch settings for ImGuiAppDemo with native debugging enabled (matt-edmondson)
- Enhance ImGuiApp configuration with debugging options (matt-edmondson)
- Checkpoint before follow-up message (Cursor Agent)
- Refactor ImGuiController for improved code clarity (matt-edmondson)
- Improve rendering precision and pixel-perfect techniques in ImGui rendering (Cursor Agent)
- Improve VSync handling during frame rate throttling (Cursor Agent)
- Refactor Performance tab into separate method and reorder tabs (Cursor Agent)
- Fix merge conflict in performance tab text and update FPS description (Cursor Agent)
- Changes from background agent bc-34f5e701-6497-49ba-b614-0b4bc857f398 (Cursor Agent)
- Add performance throttling with configurable rendering and idle detection (Cursor Agent)
- Refactor demo app with tabbed interface and improved Unicode/emoji display (Cursor Agent)
- Add Unicode and emoji support with configurable font rendering (Cursor Agent)
- Enable Unicode and emoji support by default in ImGuiApp (Cursor Agent)
- Add emoji support to Unicode character ranges in ImGuiApp (Cursor Agent)
- Checkpoint before follow-up message (Cursor Agent)
- Refactor FontHelper for flexible Unicode support with user-configured fonts (Cursor Agent)
- Add Unicode and emoji font support with cross-platform detection (Cursor Agent)

## v2.0.8 (patch)

Changes since v2.0.7:

- Add Reset method tests and reset performance-related state fields (Cursor Agent)
- Merge main into feature branch and integrate performance tab (Cursor Agent)
- Improve VSync handling during frame rate throttling (Cursor Agent)
- Update default performance settings for better resource efficiency (Cursor Agent)
- Add performance throttling with configurable rendering and idle detection (Cursor Agent)

## v2.0.7 (patch)

Changes since v2.0.6:

- Fix scissor rectangle calculations in ImGuiController to ensure non-negative dimensions, preventing potential rendering issues. (matt-edmondson)
- Add launch settings for ImGuiAppDemo with native debugging enabled (matt-edmondson)
- Enhance ImGuiApp configuration with debugging options (matt-edmondson)
- Enhance ImGuiAppDemo with new features and UI updates (matt-edmondson)
- Refactor ImGuiController for improved code clarity (matt-edmondson)
- Improve rendering precision and pixel-perfect techniques in ImGui rendering (Cursor Agent)
- Fix NuGet version retrieval and update package versions (matt-edmondson)

## v2.0.7-pre.1 (prerelease)

Changes since v2.0.6:

- Fix NuGet version retrieval and update package versions (matt-edmondson)

## v2.0.6 (patch)

Changes since v2.0.5:

- Add Directory.Packages.props and global.json for centralized package management (matt-edmondson)
- Add GitHub Actions workflow for automatic SDK updates (matt-edmondson)

## v2.0.5 (patch)

Changes since v2.0.4:

- Update CLAUDE.md with additional testing and build instructions; enhance PSBuild script to improve release notes truncation logic for compliance with NuGet character limits, including detailed logging for better traceability. (matt-edmondson)

## v2.0.4 (patch)

Changes since v2.0.3:

- Refactor Invoke-DotNetPack function in PSBuild script to improve handling of release notes. Updated logic to create a temporary file for truncated content exceeding NuGet's 35,000 character limit, ensuring compliance and enhancing logging for better traceability. (matt-edmondson)

## v2.0.3 (patch)

Changes since v2.0.2:

- Enhance Invoke-DotNetPack function in PSBuild script to handle release notes exceeding NuGet's 35,000 character limit. Added logic to truncate long release notes and create a temporary file for compliance, with appropriate logging and cleanup of temporary files after packaging. (matt-edmondson)

## v2.0.2 (patch)

Changes since v2.0.1:

- Enhance Test-IsLibraryOnlyProject function in update-winget-manifests.ps1 (matt-edmondson)

## v2.0.1 (patch)

Changes since v2.0.0:

- Enhance New-Changelog function in PSBuild script to truncate release notes exceeding NuGet's 35,000 character limit. This addition ensures compliance with NuGet requirements while providing informative logging about truncation. (matt-edmondson)

## v2.0.0 (major)

Changes since v1.0.0:

- [major] New major version because of switching to Hexa.NET.ImGui (matt-edmondson)
- Enhance Get-GitRemoteInfo function in update-winget-manifests.ps1 (matt-edmondson)
- Remove redundant release process logging from Invoke-DotNetPublish function in PSBuild script. This cleanup enhances code clarity by eliminating unnecessary information output related to NuGet package publishing. (matt-edmondson)
- Enhance PSBuild script to make NuGet API key optional (matt-edmondson)
- Update GitHub Actions workflow and enhance PSBuild script (matt-edmondson)
- Update ImGuiApp to use Hexa.NET.ImGui for Dear ImGui bindings (matt-edmondson)
- Refactor font loading logic in ImGuiApp (matt-edmondson)
- Enhance font management and loading in ImGuiApp (matt-edmondson)
- Enhance font atlas handling and logging in ImGuiController (matt-edmondson)
- Refactor texture handling and enhance logging in ImGuiApp (matt-edmondson)
- Implement dynamic font scaling and introduce debug logging in ImGuiApp (matt-edmondson)
- Enhance font configuration and memory management in ImGuiApp (matt-edmondson)
- Remove imgui.ini files and add configuration option for saving window settings (matt-edmondson)
- Refactor mouse button handling in ImGuiController (matt-edmondson)
- Update .NET workflow to restrict push triggers and enhance NuGet API key handling (matt-edmondson)
- Enhance font handling and error management in ImGuiApp (matt-edmondson)
- Fix font rendering issues with Hexa.NET.ImGui (matt-edmondson)
- Migrate from ImGui.NET to Hexa.NET.ImGui and fix line endings (matt-edmondson)
- Update font size handling and mapping in ImGuiApp (matt-edmondson)
- Refactor DPI detection logic in ForceDpiAware (matt-edmondson)
- Enhance DPI detection and scaling for Linux and WSL (matt-edmondson)
- Enhance DPI scaling and font rendering for cross-platform compatibility (matt-edmondson)
- Enhance font rendering settings for cross-platform compatibility (matt-edmondson)
- Add DPI scaling support for Wayland in ForceDpiAware (matt-edmondson)
- Refactor ImGuiApp to support Linux and Windows console behavior (matt-edmondson)
- Update .editorconfig, .gitignore, and .runsettings; refactor ImGuiApp code for type consistency (matt-edmondson)
- Fix GitHub Actions build failures on forked repositories (matt-edmondson)
- Remove obsolete build configuration files and scripts, including Directory.Build.props, Directory.Build.targets, and various PowerShell scripts for metadata and version management. Introduce a new PowerShell module, PSBuild, for automating the build, test, package, and release processes for .NET applications. (matt-edmondson)
- Update project SDK references to ktsu.Sdk version 1.8.0 (matt-edmondson)
- Add Moq package and enhance ImGuiAppTests with new unit tests (matt-edmondson)
- Add unit tests for ForceDpiAware and ImGuiApp functionality (matt-edmondson)
- Update copyright headers in ImGuiApp files to reflect ownership and licensing. Ensure consistent formatting across multiple files, enhancing clarity and compliance with licensing requirements. (matt-edmondson)
- Refactor variable declarations in ImGuiApp and related files to use 'var' for improved readability and consistency. Update .editorconfig to change the suggestion level for 'dotnet_style_prefer_auto_properties' from silent to suggestion. (matt-edmondson)
- Enhance README.md with a new "Getting Started" section detailing prerequisites for .NET 8.0 and Windows OS. Clean up code formatting in the Program class by removing unnecessary blank lines for improved readability. (matt-edmondson)
- Implement texture caching and memory optimization in ImGuiApp. Introduced a concurrent dictionary for texture management, added a method to load textures with pooled memory usage, and implemented a cleanup function for unused textures. Refactored the texture upload process for improved performance and reliability. (matt-edmondson)
- Add additional demo features to ImGuiApp, including a Style Editor, Metrics window, and About section. Enhanced the main demo window with new widgets and improved layout options. Implemented real-time plotting functionality and updated menu items for better navigation. (matt-edmondson)
- Refactor ImGuiApp startup process by consolidating window initialization and event handler setup into dedicated methods. This improves code organization and readability while maintaining functionality. (matt-edmondson)
- Update DESCRIPTION.md to clarify the purpose of the library as a .NET application scaffolding tool for Dear ImGui, utilizing Silk.NET and ImGui.NET. (matt-edmondson)
- Add VSCode configuration files for .NET Core development (matt-edmondson)
- Refactor font management in ImGuiApp to improve memory handling and performance. Added logic to prevent unnecessary font reloads based on scale factor changes. Implemented cleanup for pinned font data during window closing and enhanced font initialization process. (matt-edmondson)
- Style conformance (matt-edmondson)
- Enhance README.md with improved structure and content. Added new sections for Introduction, API Reference, and Acknowledgements. Updated features list for clarity and detail. Included usage examples for application setup and texture management. (matt-edmondson)
- Update .gitignore to include additional IDE and OS-specific files (matt-edmondson)
- Update packages (matt-edmondson)
- Update build scripts (matt-edmondson)
- Add LICENSE template (matt-edmondson)
- Update packages (matt-edmondson)
- Try a different method of memory marshaling and reorder some thigns to try fix the font crash (matt-edmondson)
- Throw an exception if the font is not ready yet for some reason (matt-edmondson)
- Update README.md (matt-edmondson)
- Replace the Silk.NET window invoker with ktsu.Invoker (matt-edmondson)
- Font system cleanup (matt-edmondson)
- Add an image to the demo app to test the texture upload (matt-edmondson)
- Reuse the texture upload from ImGuiController to remove code duplication (matt-edmondson)
- Add an overload to ImGuiController.Texture to allow specifying the pixel format (matt-edmondson)
- Re-add icon to fix LFS (matt-edmondson)
- Remove icon to fix LFS (matt-edmondson)
- Refactor font loading to use unmanaged memory (matt-edmondson)
- Update license links in README.md (matt-edmondson)
- Enhance documentation (matt-edmondson)
- Renamed several classes and moved them to their own source files (matt-edmondson)
- Add constructors and documentation to FontAppearance class (matt-edmondson)
- Add better font management to ImGuiApp and icon to demo project (matt-edmondson)
- Fix crash on shutdown when imgui would try to free memory owned by dotnet (matt-edmondson)
- Remove unnesscessary threading protections that were speculative fixes for the font ownership crash (matt-edmondson)
- Refactor ImGuiApp and related classes for thread safety (matt-edmondson)
- Apply new editorconfig (matt-edmondson)
- Always call ImGui.End for ImGui.Begin (Damon3000s)
- Call EndChild instead of just End (Damon3000s)
- Update files from Resources.resx (Damon3000s)
- Replace deprecated ImGui enum value (Damon3000s)
- Enable debug tracing in versioning and changelog scripts; handle empty tag scenarios (matt-edmondson)
- Remove spelling check ignore comments in ImGuiApp files (matt-edmondson)
- Fix version script to exclude merge commits and order logs correctly (matt-edmondson)
- Add automation scripts for metadata and version management (matt-edmondson)
- Add Silk.NET packages for enhanced graphics and input support (matt-edmondson)
- Renamed metadata files (matt-edmondson)
- Replace LICENSE file with LICENSE.md and update copyright information (matt-edmondson)
- Review feedback (Damon3000s)
- Fix mouse wheel scrolling and improve API usage (Damon3000s)
- Add conditional compilation for contextLock in ImGuiController (matt-edmondson)

## v1.12.16-pre.1 (prerelease)

No significant changes detected since v1.12.15.

## v1.12.15 (patch)

Changes since v1.12.14:

- Merge af4aa521a0ebbcbb7f312c4c4fbd3ad7413e3a94 into f9eb032f6ac465a58571e6a30c440f7981e0ac88 (matt-edmondson)
- Update ImGuiApp to use Hexa.NET.ImGui for Dear ImGui bindings (matt-edmondson)

## v1.12.14 (patch)

Changes since v1.12.13:

- Merge 388804afbf1a15272b8c8e717b53c1a4de452cae into f9eb032f6ac465a58571e6a30c440f7981e0ac88 (matt-edmondson)
- Refactor font loading logic in ImGuiApp (matt-edmondson)

## v1.12.13 (patch)

Changes since v1.12.12:

- Merge 53f66b9aa6d195eac21d66c0abf9495b42c966cb into f9eb032f6ac465a58571e6a30c440f7981e0ac88 (matt-edmondson)
- Enhance font management and loading in ImGuiApp (matt-edmondson)
- Enhance font atlas handling and logging in ImGuiController (matt-edmondson)

## v1.12.12 (patch)

Changes since v1.12.11:

- Merge 6a783eeee5c4626e891e5b42c8b3a53281b8f423 into f9eb032f6ac465a58571e6a30c440f7981e0ac88 (matt-edmondson)
- Refactor texture handling and enhance logging in ImGuiApp (matt-edmondson)
- Implement dynamic font scaling and introduce debug logging in ImGuiApp (matt-edmondson)

## v1.12.12-pre.3 (prerelease)

No significant changes detected since v1.12.12-pre.2.

## v1.12.12-pre.2 (prerelease)

No significant changes detected since v1.12.12-pre.1.

## v1.12.12-pre.1 (prerelease)

Changes since v1.12.11:

- Merge 0aa9505c62c546778ed2242018d32dbd8f19814b into 879630f8ad5f5c68c42945b69136d1663b963154 (dependabot[bot])
- Bump the system group with 1 update (dependabot[bot])

## v1.12.11 (patch)

Changes since v1.12.10:

- Merge ffa308db9aa11ddef02bdad0ebf5ca4a6b228c09 into 88b27d0745a0e160aa76589e53f760c9b0d01ec7 (matt-edmondson)
- Enhance font configuration and memory management in ImGuiApp (matt-edmondson)

## v1.12.10 (patch)

Changes since v1.12.9:

- Merge 53984ea6dc883d363fc8b6437778a305cd8e83b1 into 88b27d0745a0e160aa76589e53f760c9b0d01ec7 (matt-edmondson)
- Remove imgui.ini files and add configuration option for saving window settings (matt-edmondson)

## v1.12.9 (patch)

Changes since v1.12.8:

- Merge 111ca0ee1f1af6b009d9790a459b4f99a17f2fa8 into 88b27d0745a0e160aa76589e53f760c9b0d01ec7 (matt-edmondson)
- Refactor mouse button handling in ImGuiController (matt-edmondson)

## v1.12.8 (patch)

Changes since v1.12.7:

- Merge 80b7ec8c06293b5873e152fbaca4f4bf3dd085a8 into 88b27d0745a0e160aa76589e53f760c9b0d01ec7 (matt-edmondson)
- Update .NET workflow to restrict push triggers and enhance NuGet API key handling (matt-edmondson)

## v1.12.7 (patch)

Changes since v1.12.6:

- Merge b5e2ebfabb71fc4fd7f8ebf8a0a6bbdbe3fddec3 into 88b27d0745a0e160aa76589e53f760c9b0d01ec7 (matt-edmondson)
- Enhance font handling and error management in ImGuiApp (matt-edmondson)
- Fix font rendering issues with Hexa.NET.ImGui (matt-edmondson)
- Migrate from ImGui.NET to Hexa.NET.ImGui and fix line endings (matt-edmondson)
- Update font size handling and mapping in ImGuiApp (matt-edmondson)
- Refactor DPI detection logic in ForceDpiAware (matt-edmondson)
- Enhance DPI detection and scaling for Linux and WSL (matt-edmondson)
- Enhance DPI scaling and font rendering for cross-platform compatibility (matt-edmondson)
- Enhance font rendering settings for cross-platform compatibility (matt-edmondson)
- Add DPI scaling support for Wayland in ForceDpiAware (matt-edmondson)
- Refactor ImGuiApp to support Linux and Windows console behavior (matt-edmondson)
- Update .editorconfig, .gitignore, and .runsettings; refactor ImGuiApp code for type consistency (matt-edmondson)
- Fix GitHub Actions build failures on forked repositories (matt-edmondson)

## v1.12.6 (patch)

Changes since v1.12.5:

- Merge 2dc4ac3bbee667abc58cc7444aadaae5644ec7a0 into 59e503e7a17da7814da84abd4e4d6047980f8102 (matt-edmondson)
- Refactor font configuration handling in ImGui classes (matt-edmondson)
- Enhance font configuration in ImGuiApp and ImGuiController by adding RasterizerDensity and improving texture data handling. Updated font loading to utilize ImFontConfig directly, ensuring better rendering quality and memory management. (matt-edmondson)
- Refactor ImGuiApp and ImGuiController to improve font handling and context management. Updated font configuration to use ImFontConfig directly, removed unnecessary casts, and ensured proper context tracking. Adjusted image loading in demo to eliminate casting issues. (matt-edmondson)
- Fix ImGuiController font loading and buffer data handling by updating glyph range and removing unnecessary casts (matt-edmondson)
- Update ImGui.NET references to Hexa.NET.ImGui and bump project SDK versions (matt-edmondson)

## v1.12.6-pre.10 (prerelease)

No significant changes detected since v1.12.6-pre.9.

## v1.12.6-pre.9 (prerelease)

No significant changes detected since v1.12.6-pre.8.

## v1.12.6-pre.8 (prerelease)

No significant changes detected since v1.12.6-pre.7.

## v1.12.6-pre.7 (prerelease)

No significant changes detected since v1.12.6-pre.6.

## v1.12.6-pre.6 (prerelease)

No significant changes detected since v1.12.6-pre.5.

## v1.12.6-pre.5 (prerelease)

No significant changes detected since v1.12.6-pre.4.

## v1.12.6-pre.4 (prerelease)

No significant changes detected since v1.12.6-pre.3.

## v1.12.6-pre.3 (prerelease)

Changes since v1.12.6-pre.2:

- Bump the ktsu group with 2 updates (dependabot[bot])

## v1.12.6-pre.2 (prerelease)

No significant changes detected since v1.12.6-pre.1.

## v1.12.6-pre.1 (prerelease)

Changes since v1.12.5:

- Sync scripts\PSBuild.psm1 (ktsu[bot])

## v1.12.5 (patch)

Changes since v1.12.4:

- Remove obsolete build configuration files and scripts, including Directory.Build.props, Directory.Build.targets, and various PowerShell scripts for metadata and version management. Introduce a new PowerShell module, PSBuild, for automating the build, test, package, and release processes for .NET applications. (matt-edmondson)
- Update project SDK references to ktsu.Sdk version 1.8.0 (matt-edmondson)
- Add Moq package and enhance ImGuiAppTests with new unit tests (matt-edmondson)
- Add unit tests for ForceDpiAware and ImGuiApp functionality (matt-edmondson)
- Update copyright headers in ImGuiApp files to reflect ownership and licensing. Ensure consistent formatting across multiple files, enhancing clarity and compliance with licensing requirements. (matt-edmondson)
- Refactor variable declarations in ImGuiApp and related files to use 'var' for improved readability and consistency. Update .editorconfig to change the suggestion level for 'dotnet_style_prefer_auto_properties' from silent to suggestion. (matt-edmondson)
- Enhance README.md with a new "Getting Started" section detailing prerequisites for .NET 8.0 and Windows OS. Clean up code formatting in the Program class by removing unnecessary blank lines for improved readability. (matt-edmondson)
- Implement texture caching and memory optimization in ImGuiApp. Introduced a concurrent dictionary for texture management, added a method to load textures with pooled memory usage, and implemented a cleanup function for unused textures. Refactored the texture upload process for improved performance and reliability. (matt-edmondson)
- Add additional demo features to ImGuiApp, including a Style Editor, Metrics window, and About section. Enhanced the main demo window with new widgets and improved layout options. Implemented real-time plotting functionality and updated menu items for better navigation. (matt-edmondson)
- Refactor ImGuiApp startup process by consolidating window initialization and event handler setup into dedicated methods. This improves code organization and readability while maintaining functionality. (matt-edmondson)
- Update DESCRIPTION.md to clarify the purpose of the library as a .NET application scaffolding tool for Dear ImGui, utilizing Silk.NET and ImGui.NET. (matt-edmondson)
- Add VSCode configuration files for .NET Core development (matt-edmondson)
- Refactor font management in ImGuiApp to improve memory handling and performance. Added logic to prevent unnecessary font reloads based on scale factor changes. Implemented cleanup for pinned font data during window closing and enhanced font initialization process. (matt-edmondson)
- Style conformance (matt-edmondson)
- Enhance README.md with improved structure and content. Added new sections for Introduction, API Reference, and Acknowledgements. Updated features list for clarity and detail. Included usage examples for application setup and texture management. (matt-edmondson)

## v1.12.5-pre.13 (prerelease)

Changes since v1.12.5-pre.12:

- Bump SixLabors.ImageSharp from 3.1.7 to 3.1.8 (dependabot[bot])
- Bump Moq from 4.20.70 to 4.20.72 (dependabot[bot])

## v1.12.5-pre.12 (prerelease)

Changes since v1.12.5-pre.11:

- Update buffer configuration in ImGuiApp class (github-actions[bot])

## v1.12.5-pre.11 (prerelease)

Changes since v1.12.5-pre.10:

- Add OpenGL context change handling and texture reloading in ImGuiApp (github-actions[bot])
- Add texture reloading test to ImGuiAppTests (github-actions[bot])

## v1.12.5-pre.10 (prerelease)

Changes since v1.12.5-pre.9:

- Refactor image handling in ImGuiApp for improved memory management (github-actions[bot])

## v1.12.5-pre.9 (prerelease)

Changes since v1.12.5-pre.8:

- Add TryGetTexture methods to ImGuiApp for texture retrieval (github-actions[bot])

## v1.12.5-pre.8 (prerelease)

Changes since v1.12.5-pre.7:

- Enhance ImGuiApp configuration validation and testing (github-actions[bot])
- Enhance ImGuiApp configuration and pixel conversion methods (github-actions[bot])

## v1.12.5-pre.7 (prerelease)

Changes since v1.12.5-pre.6:

- Enhance EnsureWindowPositionIsValid test to validate window positioning (github-actions[bot])

## v1.12.5-pre.6 (prerelease)

Changes since v1.12.5-pre.5:

- Add Moq package and enhance ImGuiAppTests with new unit tests (matt-edmondson)
- Add unit tests for ForceDpiAware and ImGuiApp functionality (matt-edmondson)
- Update copyright headers in ImGuiApp files to reflect ownership and licensing. Ensure consistent formatting across multiple files, enhancing clarity and compliance with licensing requirements. (matt-edmondson)
- Refactor variable declarations in ImGuiApp and related files to use 'var' for improved readability and consistency. Update .editorconfig to change the suggestion level for 'dotnet_style_prefer_auto_properties' from silent to suggestion. (matt-edmondson)
- Enhance README.md with a new "Getting Started" section detailing prerequisites for .NET 8.0 and Windows OS. Clean up code formatting in the Program class by removing unnecessary blank lines for improved readability. (matt-edmondson)
- Implement texture caching and memory optimization in ImGuiApp. Introduced a concurrent dictionary for texture management, added a method to load textures with pooled memory usage, and implemented a cleanup function for unused textures. Refactored the texture upload process for improved performance and reliability. (matt-edmondson)
- Add additional demo features to ImGuiApp, including a Style Editor, Metrics window, and About section. Enhanced the main demo window with new widgets and improved layout options. Implemented real-time plotting functionality and updated menu items for better navigation. (matt-edmondson)
- Refactor ImGuiApp startup process by consolidating window initialization and event handler setup into dedicated methods. This improves code organization and readability while maintaining functionality. (matt-edmondson)
- Update DESCRIPTION.md to clarify the purpose of the library as a .NET application scaffolding tool for Dear ImGui, utilizing Silk.NET and ImGui.NET. (matt-edmondson)
- Add VSCode configuration files for .NET Core development (matt-edmondson)
- Refactor font management in ImGuiApp to improve memory handling and performance. Added logic to prevent unnecessary font reloads based on scale factor changes. Implemented cleanup for pinned font data during window closing and enhanced font initialization process. (matt-edmondson)
- Style conformance (matt-edmondson)
- Enhance README.md with improved structure and content. Added new sections for Introduction, API Reference, and Acknowledgements. Updated features list for clarity and detail. Included usage examples for application setup and texture management. (matt-edmondson)

## v1.12.5-pre.5 (prerelease)

Changes since v1.12.5-pre.4:

- Sync .github\workflows\dotnet.yml (ktsu[bot])
- Sync .editorconfig (ktsu[bot])
- Sync .runsettings (ktsu[bot])

## v1.12.5-pre.4 (prerelease)

Changes since v1.12.5-pre.3:

- Bump ktsu.ScopedAction from 1.1.0 to 1.1.1 in the ktsu group (dependabot[bot])

## v1.12.5-pre.3 (prerelease)

Changes since v1.12.5-pre.2:

- Bump System.Text.Json from 9.0.3 to 9.0.4 in the system group (dependabot[bot])
- Bump Microsoft.DotNet.ILCompiler in the microsoft group (dependabot[bot])

## v1.12.5-pre.2 (prerelease)

Changes since v1.12.5-pre.1:

- Sync .editorconfig (ktsu[bot])

## v1.12.5-pre.1 (prerelease)

Changes since v1.12.4:

- Sync .editorconfig (ktsu[bot])

## v1.12.4 (patch)

Changes since v1.12.3:

- Update .gitignore to include additional IDE and OS-specific files (matt-edmondson)

## v1.12.3 (patch)

Changes since v1.12.2:

- Update packages (matt-edmondson)

## v1.12.2 (patch)

Changes since v1.12.1:

- Update build scripts (matt-edmondson)
- Add LICENSE template (matt-edmondson)

## v1.12.1 (patch)

Changes since v1.12.0:

- Update packages (matt-edmondson)

## v1.12.0 (minor)

Changes since v1.11.0:

- Try a different method of memory marshaling and reorder some thigns to try fix the font crash (matt-edmondson)
- Throw an exception if the font is not ready yet for some reason (matt-edmondson)
- Update README.md (matt-edmondson)

## v1.11.1-pre.1 (prerelease)

Changes since v1.11.0:

- Update README.md (matt-edmondson)

## v1.11.0 (minor)

Changes since v1.10.0:

- Replace the Silk.NET window invoker with ktsu.Invoker (matt-edmondson)

## v1.10.0 (minor)

Changes since v1.9.0:

- Font system cleanup (matt-edmondson)

## v1.9.0 (minor)

Changes since v1.8.0:

- Add an image to the demo app to test the texture upload (matt-edmondson)
- Reuse the texture upload from ImGuiController to remove code duplication (matt-edmondson)
- Add an overload to ImGuiController.Texture to allow specifying the pixel format (matt-edmondson)
- Re-add icon to fix LFS (matt-edmondson)
- Remove icon to fix LFS (matt-edmondson)

## v1.8.1 (patch)

Changes since v1.8.0:

- Re-add icon to fix LFS (matt-edmondson)
- Remove icon to fix LFS (matt-edmondson)

## v1.8.0 (minor)

Changes since v1.7.0:

- Refactor font loading to use unmanaged memory (matt-edmondson)
- Update license links in README.md (matt-edmondson)
- Enhance documentation (matt-edmondson)
- Renamed several classes and moved them to their own source files (matt-edmondson)
- Add constructors and documentation to FontAppearance class (matt-edmondson)

## v1.7.1 (patch)

Changes since v1.7.0:

- Update license links in README.md (matt-edmondson)
- Enhance documentation (matt-edmondson)
- Renamed several classes and moved them to their own source files (matt-edmondson)
- Add constructors and documentation to FontAppearance class (matt-edmondson)

## v1.7.0 (minor)

Changes since v1.6.0:

- Add better font management to ImGuiApp and icon to demo project (matt-edmondson)
- Fix crash on shutdown when imgui would try to free memory owned by dotnet (matt-edmondson)

## v1.6.1-pre.1 (prerelease)

Changes since v1.6.0:

- Fix crash on shutdown when imgui would try to free memory owned by dotnet (matt-edmondson)

## v1.6.0 (minor)

Changes since v1.5.0:

- Remove unnesscessary threading protections that were speculative fixes for the font ownership crash (matt-edmondson)

## v1.5.1-pre.1 (prerelease)

Changes since v1.5.0:

- Bump System.Text.Json from 9.0.2 to 9.0.3 in the system group (dependabot[bot])
- Bump Microsoft.DotNet.ILCompiler in the microsoft group (dependabot[bot])

## v1.5.0 (minor)

Changes since v1.4.0:

- Refactor ImGuiApp and related classes for thread safety (matt-edmondson)

## v1.4.1-pre.3 (prerelease)

Changes since v1.4.1-pre.2:

- Bump SixLabors.ImageSharp from 3.1.6 to 3.1.7 (dependabot[bot])

## v1.4.1-pre.2 (prerelease)

Changes since v1.4.1-pre.1:

- Bump ktsu.StrongPaths from 1.1.50 to 1.2.0 in the ktsu group (dependabot[bot])

## v1.4.1-pre.1 (prerelease)

Changes since v1.4.0:

- Bump ktsu.ScopedAction from 1.0.14 to 1.0.15 in the ktsu group (dependabot[bot])

## v1.4.0 (minor)

Changes since v1.3.0:

- Apply new editorconfig (matt-edmondson)

## v1.3.1-pre.1 (prerelease)

Changes since v1.3.0:

- Bump System.Text.Json from 9.0.1 to 9.0.2 in the system group (dependabot[bot])
- Bump Microsoft.DotNet.ILCompiler in the microsoft group (dependabot[bot])

## v1.3.0 (minor)

Changes since v1.2.0:

- Always call ImGui.End for ImGui.Begin (Damon3000s)

## v1.2.1-pre.1 (prerelease)

Changes since v1.2.0:

- Sync scripts\make-version.ps1 (ktsu[bot])
- Sync scripts\make-changelog.ps1 (ktsu[bot])

## v1.2.0 (minor)

Changes since v1.1.0:

- Call EndChild instead of just End (Damon3000s)
- Update files from Resources.resx (Damon3000s)
- Replace deprecated ImGui enum value (Damon3000s)
- Enable debug tracing in versioning and changelog scripts; handle empty tag scenarios (matt-edmondson)

## v1.1.0 (minor)

Changes since v1.0.0:

- Remove spelling check ignore comments in ImGuiApp files (matt-edmondson)
- Fix version script to exclude merge commits and order logs correctly (matt-edmondson)
- Add automation scripts for metadata and version management (matt-edmondson)
- Add Silk.NET packages for enhanced graphics and input support (matt-edmondson)
- Renamed metadata files (matt-edmondson)
- Replace LICENSE file with LICENSE.md and update copyright information (matt-edmondson)
- Review feedback (Damon3000s)
- Fix mouse wheel scrolling and improve API usage (Damon3000s)
- Add conditional compilation for contextLock in ImGuiController (matt-edmondson)

## v1.0.12

No significant changes detected since v1.0.12-pre.11.

## v1.0.12-pre.11 (prerelease)

Changes since v1.0.12-pre.10:

- Bump ktsu.StrongPaths from 1.1.49 to 1.1.50 in the ktsu group (dependabot[bot])

## v1.0.12-pre.10 (prerelease)

Changes since v1.0.12-pre.9:

- Fix version script to exclude merge commits and order logs correctly (matt-edmondson)

## v1.0.12-pre.9 (prerelease)

Changes since v1.0.12-pre.8:

- Review feedback (Damon3000s)
- Fix mouse wheel scrolling and improve API usage (Damon3000s)

## v1.0.12-pre.8 (prerelease)

Changes since v1.0.12-pre.7:

- Sync scripts\make-changelog.ps1 (ktsu[bot])

## v1.0.12-pre.7 (prerelease)

Changes since v1.0.12-pre.6:

- Add automation scripts for metadata and version management (matt-edmondson)

## v1.0.12-pre.6 (prerelease)

Changes since v1.0.12-pre.5:

- Bump coverlet.collector from 6.0.2 to 6.0.3 (dependabot[bot])

## v1.0.12-pre.5 (prerelease)

No significant changes detected since v1.0.12-pre.4.

## v1.0.12-pre.4 (prerelease)

No significant changes detected since v1.0.12-pre.3.

## v1.0.12-pre.3 (prerelease)

Changes since v1.0.12-pre.2:

- Add Silk.NET packages for enhanced graphics and input support (matt-edmondson)

## v1.0.12-pre.2 (prerelease)

Changes since v1.0.12-pre.1:

- Sync .github\workflows\dotnet.yml (ktsu[bot])

## v1.0.12-pre.1 (prerelease)

Changes since v1.0.11-pre.1:

- Sync .github\workflows\dotnet.yml (ktsu[bot])

## v1.0.11-pre.1 (prerelease)

Changes since v1.0.10-pre.1:

- Sync .github\workflows\dotnet.yml (ktsu[bot])
- Sync .github\workflows\dotnet.yml (ktsu[bot])

## v1.0.10-pre.1 (prerelease)

Changes since v1.0.9:

- Renamed metadata files (matt-edmondson)

## v1.0.9 (patch)

Changes since v1.0.8:

- Sync icon.png (ktsu[bot])

## v1.0.8 (patch)

Changes since v1.0.7:

- Replace LICENSE file with LICENSE.md and update copyright information (matt-edmondson)

## v1.0.7 (patch)

Changes since v1.0.6:

- Bump ktsu.StrongPaths from 1.1.40 to 1.1.41 in the ktsu group (dependabot[bot])

## v1.0.6 (patch)

Changes since v1.0.5:

- Bump the ktsu group with 2 updates (dependabot[bot])

## v1.0.5 (patch)

Changes since v1.0.4:

- Sync .github\workflows\dotnet.yml (ktsu[bot])

## v1.0.4 (patch)

Changes since v1.0.3:

- Add conditional compilation for contextLock in ImGuiController (matt-edmondson)

## v1.0.3 (patch)

Changes since v1.0.2:

- Update VERSION to 1.0.3 (matt-edmondson)
- Updated packages in ImGuiApp.csproj (matt-edmondson)

## v1.0.2 (patch)

Changes since v1.0.1:

- Bump ktsu.StrongPaths from 1.1.35 to 1.1.36 in the ktsu group (dependabot[bot])

## v1.0.1 (patch)

Changes since v1.0.0:

- Bump ktsu.ScopedAction from 1.0.0 to 1.0.1 in the ktsu group (dependabot[bot])

## v1.0.0

Changes since v1.0.0-alpha.76:

- Reformat ImGuiController instantiation for readability (matt-edmondson)
- Documentation and warning fixes (matt-edmondson)

## v1.0.0-alpha.76 (prerelease)

Changes since v1.0.0-alpha.75:

- Update VERSION to 1.0.0-alpha.76 (matt-edmondson)

## v1.0.0-alpha.75 (prerelease)

Changes since v1.0.0-alpha.74:

- Update VERSION to 1.0.0-alpha.75 (matt-edmondson)

## v1.0.0-alpha.74 (prerelease)

Changes since v1.0.0-alpha.73:

- Update VERSION to 1.0.0-alpha.74 (matt-edmondson)

## v1.0.0-alpha.73 (prerelease)

Changes since v1.0.0-alpha.72:

- Update VERSION to 1.0.0-alpha.73 (matt-edmondson)

## v1.0.0-alpha.72 (prerelease)

Changes since v1.0.0-alpha.71:

- Update VERSION to 1.0.0-alpha.72 (matt-edmondson)

## v1.0.0-alpha.71 (prerelease)

Changes since v1.0.0-alpha.70:

- Update VERSION to 1.0.0-alpha.71 (matt-edmondson)
- Refactor ImGuiController usage and update package (matt-edmondson)
- Bump ktsu.StrongPaths from 1.1.29 to 1.1.30 (matt-edmondson)
- Refactor namespace and update package versions in ImGuiApp (matt-edmondson)

## v1.0.0-alpha.70 (prerelease)

Changes since v1.0.0-alpha.69:

- Update VERSION to 1.0.0-alpha.70 (matt-edmondson)

## v1.0.0-alpha.69 (prerelease)

Changes since v1.0.0-alpha.68:

- Update VERSION to 1.0.0-alpha.69 (matt-edmondson)

## v1.0.0-alpha.68 (prerelease)

Changes since v1.0.0-alpha.67:

- Update VERSION to 1.0.0-alpha.68 (matt-edmondson)

## v1.0.0-alpha.67 (prerelease)

Changes since v1.0.0-alpha.66:

- Update VERSION to 1.0.0-alpha.67 (matt-edmondson)

## v1.0.0-alpha.66 (prerelease)

Changes since v1.0.0-alpha.65:

- Update VERSION to 1.0.0-alpha.66 (matt-edmondson)

## v1.0.0-alpha.65 (prerelease)

Changes since v1.0.0-alpha.64:

- Update VERSION to 1.0.0-alpha.65 (matt-edmondson)
- Set System.Text.Json to 8.0.5 (Damon3000s)
- More detailed exception message in TranslateInputKeyToImGuiKey (Damon3000s)

## v1.0.0-alpha.64 (prerelease)

Changes since v1.0.0-alpha.63:

- Update VERSION to 1.0.0-alpha.64 (matt-edmondson)

## v1.0.0-alpha.63 (prerelease)

Changes since v1.0.0-alpha.62:

- Update VERSION to 1.0.0-alpha.63 (matt-edmondson)

## v1.0.0-alpha.62 (prerelease)

Changes since v1.0.0-alpha.61:

- Update VERSION to 1.0.0-alpha.62 (matt-edmondson)

## v1.0.0-alpha.61 (prerelease)

Changes since v1.0.0-alpha.60:

- Update VERSION to 1.0.0-alpha.61 (matt-edmondson)

## v1.0.0-alpha.60 (prerelease)

Changes since v1.0.0-alpha.59:

- Update VERSION to 1.0.0-alpha.60 (matt-edmondson)

## v1.0.0-alpha.59 (prerelease)

Changes since v1.0.0-alpha.58:

- Update VERSION to 1.0.0-alpha.59 (matt-edmondson)

## v1.0.0-alpha.58 (prerelease)

Changes since v1.0.0-alpha.57:

- Update VERSION to 1.0.0-alpha.58 (matt-edmondson)

## v1.0.0-alpha.57 (prerelease)

Changes since v1.0.0-alpha.56:

- Update VERSION to 1.0.0-alpha.57 (matt-edmondson)

## v1.0.0-alpha.56 (prerelease)

Changes since v1.0.0-alpha.55:

- Update VERSION to 1.0.0-alpha.56 (matt-edmondson)

## v1.0.0-alpha.55 (prerelease)

Changes since v1.0.0-alpha.54:

- Update VERSION to 1.0.0-alpha.55 (matt-edmondson)

## v1.0.0-alpha.54 (prerelease)

Changes since v1.0.0-alpha.53:

- Update VERSION to 1.0.0-alpha.54 (matt-edmondson)

## v1.0.0-alpha.53 (prerelease)

Changes since v1.0.0-alpha.52:

- Update VERSION to 1.0.0-alpha.53 (matt-edmondson)

## v1.0.0-alpha.52 (prerelease)

Changes since v1.0.0-alpha.51:

- Update VERSION to 1.0.0-alpha.52 (matt-edmondson)

## v1.0.0-alpha.51 (prerelease)

Changes since v1.0.0-alpha.50:

- Update VERSION to 1.0.0-alpha.51 (matt-edmondson)

## v1.0.0-alpha.50 (prerelease)

Changes since v1.0.0-alpha.49:

- Update VERSION to 1.0.0-alpha.50 (matt-edmondson)

## v1.0.0-alpha.49 (prerelease)

Changes since v1.0.0-alpha.48:

- Update VERSION to 1.0.0-alpha.49 (matt-edmondson)

## v1.0.0-alpha.48 (prerelease)

Changes since v1.0.0-alpha.47:

- Update VERSION to 1.0.0-alpha.48 (matt-edmondson)

## v1.0.0-alpha.47 (prerelease)

Changes since v1.0.0-alpha.46:

- Update VERSION to 1.0.0-alpha.47 (matt-edmondson)

## v1.0.0-alpha.46 (prerelease)

Changes since v1.0.0-alpha.45:

- Update VERSION to 1.0.0-alpha.46 (matt-edmondson)

## v1.0.0-alpha.45 (prerelease)

Changes since v1.0.0-alpha.44:

- Update VERSION to 1.0.0-alpha.45 (matt-edmondson)

## v1.0.0-alpha.44 (prerelease)

Changes since v1.0.0-alpha.43:

- Update VERSION to 1.0.0-alpha.44 (matt-edmondson)

## v1.0.0-alpha.43 (prerelease)

Changes since v1.0.0-alpha.42:

- Update VERSION to 1.0.0-alpha.43 (matt-edmondson)

## v1.0.0-alpha.42 (prerelease)

No significant changes detected since v1.0.0-alpha.41.

## v1.0.0-alpha.41 (prerelease)

No significant changes detected since v1.0.0-alpha.40.

## v1.0.0-alpha.40 (prerelease)

No significant changes detected since v1.0.0-alpha.39.

## v1.0.0-alpha.39 (prerelease)

No significant changes detected since v1.0.0-alpha.38.

## v1.0.0-alpha.38 (prerelease)

Changes since v1.0.0-alpha.37:

- Update ImGui.NET (matt-edmondson)
- Copy ImGuiController from Silk.NET (matt-edmondson)

## v1.0.0-alpha.37 (prerelease)

Changes since v1.0.0-alpha.36:

- Sync workflows (matt-edmondson)

## v1.0.0-alpha.36 (prerelease)

Changes since v1.0.0-alpha.15:

- Migrate ktsu.io to ktsu namespace (matt-edmondson)
- Update VERSION (matt-edmondson)
- Migrate ktsu.io to ktsu namespace (matt-edmondson)
- Update VERSION (matt-edmondson)
- Update README.md (matt-edmondson)
- Update LICENSE (matt-edmondson)
- Update AUTHORS (matt-edmondson)
- Update ImGuiAppDemo.cs (matt-edmondson)
- Update UIScaler.cs (matt-edmondson)
- Update ImGuiApp.cs (matt-edmondson)
- Update GdiPlusHelper.cs (matt-edmondson)
- Update ForceDpiAware.cs (matt-edmondson)
- Update ImGuiApp.csproj (matt-edmondson)
- Update VERSION (matt-edmondson)
- Tell imgui were responsible for owning the font atlas data and then free it ourselves on shutdown (matt-edmondson)
- Destroy and clear fonts on shutdown before the imgui context gets destroyed (matt-edmondson)
- Update VERSION (matt-edmondson)
- Reposition the window if it becomes completely offscreen (matt-edmondson)
- Add a method to convert ems to pixels based on the current font size (matt-edmondson)
- Speculative fix for crash on shutdown (matt-edmondson)
- Add medium font (matt-edmondson)
- Take a lock during the closing delegate (matt-edmondson)
- Update VERSION (matt-edmondson)
- Expose the Scale Factor to the public api so that clients can scale their custom things accordingly (matt-edmondson)
- Add dpi dependent content scaling (matt-edmondson)
- Reduce background tick rates (matt-edmondson)
- Update VERSION (matt-edmondson)
- Add frame limiting (matt-edmondson)
- Also dont render if the window is not visible (matt-edmondson)
- Separate application update and render so we dont render when the application is minimized (matt-edmondson)
- Add more locks for GL (matt-edmondson)
- Use a concurrent dictionary for textures (matt-edmondson)
- Added methods for loading textures (matt-edmondson)
- Add the ability to set the window icon (matt-edmondson)
- Rollback net6 imports (matt-edmondson)
- Implement net6 DllImports (matt-edmondson)
- Minor code style changes (matt-edmondson)

## v1.0.0-alpha.15 (prerelease)

- Update Directory.Build.targets (matt-edmondson)
- Update dotnet.yml (matt-edmondson)
- Update dotnet.yml (matt-edmondson)
- Update dotnet.yml (matt-edmondson)
- Update Directory.Build.targets (matt-edmondson)
- Update Directory.Build.props (matt-edmondson)
- Migrate demo to net8 (matt-edmondson)
- Update build config (matt-edmondson)
- Update LICENSE (matt-edmondson)
- Update nuget.config (matt-edmondson)
- Update nuget.config (matt-edmondson)
- Add github package support (matt-edmondson)
- Update VERSION to 1.0.0-alpha.15 (matt-edmondson)
- Prevent drawing an extra border around the main window (matt-edmondson)
- Update README.md (matt-edmondson)
- Update README.md (matt-edmondson)
- Update README.md (matt-edmondson)
- Update README.md (matt-edmondson)
- Update VERSION to 1.0.0-alpha.14 (matt-edmondson)
- #1 Track the state of the window when its in the Normal state (matt-edmondson)
- Update VERSION to 1.0.0-alpha.13 (matt-edmondson)
- Move OnStart callsite to a place where font loading works with Silk.NET, and add a demo project (matt-edmondson)
- Update LICENSE (matt-edmondson)
- Update VERSION to 1.0.0-alpha.12 (matt-edmondson)
- Fix build and remove obsolete files and settings (matt-edmondson)
- Fix issue with OnStart being invoked too early (matt-edmondson)
- Update build actions (matt-edmondson)
- Update VERSION to 1.0.0-alpha.11 (matt-edmondson)
- Add an onStart delegate (matt-edmondson)
- Read from AUTHORS file during build (matt-edmondson)
- Read PackageDescription from DESCRIPTION file (matt-edmondson)
- Create VERSION (matt-edmondson)
- Read from VERSION when building (matt-edmondson)
- Assign dependabot PRs to matt (matt-edmondson)
- Dont try to push packages when building pull requests (matt-edmondson)
- Avoid double upload of symbols package (matt-edmondson)
- Enable sourcelink (matt-edmondson)
- v1.0.0-alpha.9 (matt-edmondson)
- Consume the resize and move events from silk to actually call the resize delegate (matt-edmondson)
- Create dependabot.yml (matt-edmondson)
- Add a property for getting the current window state from the app and correctly set the position and layout from the initial window state (matt-edmondson)
- Remove windows only flags and migrate to nested Directory.Build.props (matt-edmondson)
- Fix a crash calling an ImGui style too early in the initialization (matt-edmondson)
- Migrate to Silk.NET for windowing/graphics backend (matt-edmondson)
- Add Stop() method to quit the application (matt-edmondson)
- Remove DividerContainers and zones and Extensions which have been moved to their own respective libraries (matt-edmondson)
- Fix namespacing to include ImGuiApp and make it filescoped and apply code style rules (matt-edmondson)
- Update project properties to enable windows targeting to allow building on linux build servers (matt-edmondson)
- Add action to publish to nuget (matt-edmondson)
- Update README.md (matt-edmondson)
- Cleanup and reduce code duplication (matt-edmondson)
- Add methods to allow saving and restoring divider states in bulk and add a delegate that gets called when a zone is resized (matt-edmondson)
- Add divider widgets to ImGuiApp (matt-edmondson)
- Roll back code style changes to a working version (matt-edmondson)
- Cleanup and force demo window open for debug (matt-edmondson)
- Initial commit (matt-edmondson)
- Initial commit (matt-edmondson)

