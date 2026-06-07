## v2.12.0 (minor)

Changes since v2.11.0:

- docs(ios): mark Task 4 (Metal renderer) complete in the port plan ([@Claude](https://github.com/Claude))
- chore(ios): remove renderer bring-up diagnostics ([@Claude](https://github.com/Claude))
- fix(ios): use TextUnformatted to avoid the variadic igText ARM64 crash ([@Claude](https://github.com/Claude))
- diag(ios): trace OnRender draw calls + log font atlas dims ([@Claude](https://github.com/Claude))
- diag(ios): trace the frame loop to localise the render SIGSEGV ([@Claude](https://github.com/Claude))
- fix(ios): use non-normalized UChar4 for the ImGui vertex colour ([@Claude](https://github.com/Claude))
- fix(ios): write cimgui.dylib to an absolute path (root cause) ([@Claude](https://github.com/Claude))
- fix(ios): stash cimgui.dylib in RUNNER_TEMP so the embed step finds it ([@Claude](https://github.com/Claude))
- fix(ios): copy cimgui.dylib into the .app and dlopen by bundle path ([@Claude](https://github.com/Claude))
- fix(ios): ship cimgui as an embedded dynamic library, dlopen it ([@Claude](https://github.com/Claude))
- diag(ios): inspect the app binary for cimgui link/export status ([@Claude](https://github.com/Claude))
- fix(ios): export dynamic symbols so dlsym resolves static cimgui ([@Claude](https://github.com/Claude))
- diag(ios): probe cimgui symbol resolution before first ImGui call ([@Claude](https://github.com/Claude))
- fix(ios): use ImGui.GetVersionS() for the smoke version probe ([@Claude](https://github.com/Claude))
- fix(ios): pin cimgui to a consistent 1.92.3 docking commit ([@Claude](https://github.com/Claude))
- feat(ios): statically link cimgui so ImGui runs on iOS ([@Claude](https://github.com/Claude))
- fix(ios): satisfy KTSU0003/CA2000 analyzers in the Metal backend ([@Claude](https://github.com/Claude))
- feat(ios): Metal renderer backend (Task 4) - stand up ImGui frames on iOS ([@Claude](https://github.com/Claude))
- wip(ios): begin Metal renderer (Task 4) - shader + frame-loop scaffolding ([@Claude](https://github.com/Claude))

