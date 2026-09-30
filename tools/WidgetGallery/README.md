# Widget gallery

Renders every widget in `ktsu.ImGui.Widgets` headlessly, one picture per widget, and composites the
pictures into captioned gallery images for the README. It runs on the same CPU rasterizer as the UI
test suites, so it needs no window, no GPU and no display.

```bash
dotnet run -c Release --project tools/WidgetGallery -- --material-icons path/to/MaterialIcons-Regular.ttf
```

A full run takes about fifteen seconds and writes to `docs/gallery/`:

| File | What it is |
|---|---|
| `widgets/<name>.png` | One tile per widget, cropped to what it drew plus an 8 px margin |
| `widgets.png` | Every tile, grouped under the README's feature-list headings |
| `widgets-<group>.png` | One sheet per group, for a README that shows a group at a time |

Options: `--out <dir>`, `--only <text>` (capture matching tiles only, and skip the composites),
`--width <pixels>` for the composites, and `--check`, which reports any `ImGuiWidgets` member with no
tile and exits 1 if there is one.

## Regenerated on main

`.github/workflows/widget-gallery.yml` reruns the tool whenever CI passes on a push to `main`, and
commits `docs/gallery/` back when the pictures changed. A pull request therefore
does not need to commit regenerated images, though it may to show a change in review. The workflow
downloads a pinned copy of Material Icons, so the icon-font tiles render real glyphs there.

## Adding a widget

Add a `GalleryEntry` to the file in `Catalog/` for its group, and name the `ImGuiWidgets` members it
shows in `Covers`. `--check` fails until a new widget has a tile, so a widget that lands later is
reported rather than silently missing. A member that is not a widget (a helper, a state class) goes
in `CatalogCoverage.NotWidgets` with the reason.

An entry draws its widget as a test would, and can then:

- `Bounds`: draw inside a child region of that size, for a widget that fills whatever space it gets.
- `Interact`: hover, click or show something before the picture is taken (`Combo` opens its popup,
  `Tooltip` hovers, the dialogs call `Show`).
- `Cleanup`: close whatever `Interact` opened. Hexa's dialogs are process-static, so a dialog left
  open would be drawn over every later dialog's tile.
- `InComposite = false`: write the tile but leave it out of the composites.

## How a tile is cropped

Each entry is drawn alone in a fresh harness. The crop is the union of the layout rectangle ImGui
reports around everything the entry submitted and the pixels that changed compared with a frame
drawn without it. The first catches widgets drawn in the window's own colour; the second catches
popups, tooltips and dialogs drawn outside the layout.

## Things to know

- **Fonts.** The harness renders in Dear ImGui's built-in bitmap font and never calls
  `OnConfigureFonts`. The gallery loads `ImGuiApp`'s own Nerd Font from `OnStart` instead, so the
  pictures look like an application does. Material Icons is not in the repository; without
  `--material-icons` the date picker, file tree and file dialogs show placeholder glyphs.
- **The file tree and pickers list the machine's drives.** They are opened on a generated sample
  folder, but their sidebars show whatever mounts the generating machine has.
- **Sample image.** Widgets that show an image get a generated sunset, not `ktsu.png`, which arrives
  as a Git LFS pointer in some checkouts.
- **Native asserts end the process.** A widget that trips a Dear ImGui assert (for example, a
  `HandleTrack` with nothing submitted after it) aborts the run rather than failing one tile.
