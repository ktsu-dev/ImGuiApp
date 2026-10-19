## v3.75.3 (patch)

Changes since v3.75.2:

- Pick only clipboard image types the decoder can read on Linux [patch] (Claude)
- Name Tab as Tab in KeyChordMatcher, not its NamedKeyBegin alias [patch] (Claude)
- Move the JPEG coefficient bound into a helper (Claude)
- Reject oversized image headers as InvalidImageDataException [patch] (Claude)
- Decode 16-bit greyscale TGA as grey plus alpha instead of RGB555 [patch] (Claude)
- Split the glued-word grouping into tokenizing and grouping (matt-edmondson)
- Stop Markdown inserting a space where inline runs touch [patch] (matt-edmondson)

