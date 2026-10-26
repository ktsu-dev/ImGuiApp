## v3.79.0 (minor)

Changes since v3.78.0:

- Size text fields from their value in managed code, with no native buffer (Claude)
- Grow every text field's buffer with its text instead of giving it a fixed capacity (Claude)
- Give every DividerContainer zone the same gap, whatever the zone count or layout (Claude)
- Let StickyHeaderList's pinned header take the pointer from the rows beneath it (Claude)
- Keep a TabPanel's tab across a hidden strip and tab removal (Claude)
- [patch] Keep a value slider's minus sign readable in every culture and font (Claude)
- [patch] Apply a window icon set after launch again once the window shows (Claude)
- [patch] Reopen a window saved maximized as maximized (#725) (matt-edmondson)
- [patch] Reopen a window saved maximized as maximized (Claude)

