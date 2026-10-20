## v3.75.4 (patch)

Changes since v3.75.3:

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
- Honour the TGA colour map's first entry index [patch] (Claude)

