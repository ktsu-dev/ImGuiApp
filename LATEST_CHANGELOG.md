## v3.66.0 (minor)

Changes since v3.65.0:

- Cover Show() reaching the windowing backend without a Win32 handle ([@Claude](https://github.com/Claude))
- Put the imm32 calls behind a seam and test what the Win32 target sends ([@Claude](https://github.com/Claude))
- Bring the window forward from Show() on Linux and macOS too ([@Claude](https://github.com/Claude))
- Read the harness's IME placement through a checked local ([@Claude](https://github.com/Claude))
- Leave io.WantTextInput alone when a widget reports its caret ([@Claude](https://github.com/Claude))
- [minor] Place the input method's candidate window at the text caret ([@Claude](https://github.com/Claude))

