# CI: parallel test matrix across platforms

Restructures `.github/workflows/dotnet.yml` so tests fan out across platforms and projects instead of running serially on one Windows runner. The immediate reason is that CI already fails: two runs were cancelled at the 20-minute cap, including the merge of PR #322.

This file is propagated to every ktsu repo by `ktsu sync`, so the design has to work for a repo with one test project as well as one with ten.

## The problem

`ktsubuild ci` runs restore, build, and every test project in sequence on `windows-latest`, inside a job capped at 20 minutes. Merging the five demo UI test projects added 107 headless tests. Measured against `ImGuiMarkdownDemo.UITests`, those cost about 5.4 seconds each — roughly 9.6 minutes of new work in a job that already ran 13.6 to 19.9 minutes.

The cap can't simply be raised. It's the wrong shape: everything is serial, one platform is tested, and the budget is shared between building, testing, analysis, and release.

## Decisions

**Keep calling `ktsubuild ci`, with its two new skip flags.** (Revised 2026-08-21. The original decision was to stop calling `ci` and invoke restore, build, and test directly, on the grounds that `ktsubuild release` covered everything else. That was wrong, and the correction is recorded under Revisions.) The release job runs `ktsubuild ci --no-test --no-release` inside the SonarCloud begin and end window, then `ktsubuild release` after the gate passes. `ci` remains the only place that updates and commits the metadata files, updates the repository topics, applies the version gate that makes `[skip ci]` work, honors a forced version bump, and writes the `version`, `release_hash`, `should_release`, and `build_skipped` step outputs.

**Fan out over platform × test project.** Each cell restores, builds, and runs one test project on one platform. This is what fixes the timeout: each cell carries its own budget, and the wall clock becomes the slowest single project rather than the sum of all of them.

**Each cell is self-contained. No build artifacts move between jobs.** An earlier shape built once on Linux and shipped the output to every platform. It doesn't work: `dotnet test --no-build` reads `obj/project.assets.json`, which holds absolute paths into the NuGet cache, and `/home/runner/.nuget/packages` doesn't exist on a Windows runner. Working around that means either running assemblies directly or restoring on every leg, and both are more machinery than a rebuild is worth. A build costs a minute or two against nine minutes of UI tests.

**Test on Linux, Windows, and macOS on every run.** Cost isn't the binding constraint — self-hosted runners are available if the minutes bite — so the matrix runs on every trigger rather than being gated to `main`. A dependabot bump that breaks only on Windows is caught by the bump, not by the merge.

**One Sonar analysis, in the release job.** SonarCloud replaces the previous analysis for a project key rather than merging, so a Sonar block inside each cell would submit 15 competing analyses of the same project. Whichever finished last would win and report every project it didn't run as uncovered. Instead each cell uploads its coverage XML, and one job imports them all.

**Sonar and release are one job.** Both need a build on Linux, so merging them saves one. Pack and push run after `sonarscanner end`, which means a failed quality gate stops the release — a change from today, where `ktsubuild ci` releases first and Sonar reports afterward. A release that fails the project's own quality bar is worth stopping, and nothing has consumed it at that point.

## Job graph

```
discover (ubuntu)   list test projects -> matrix JSON

test (matrix: {ubuntu, windows, macos} x N projects)
                    restore -> build -> test one project
                    upload coverage-<platform>-<project>.xml

release (ubuntu, needs: test)
                    download every coverage artifact
                    sonar begin
                    -> ktsubuild ci --no-test --no-release
                       (metadata, topics, version gate, restore, build, step outputs)
                    -> sonar end (imports the downloaded coverage)
                    -> ktsubuild release   [if should_release == 'true']

winget   (windows, needs: release)  unchanged apart from where it reads its outputs
security (windows, needs: release)  unchanged apart from where it reads its outputs
ios      (separate workflow)        unchanged
```

`winget` and `security` both gate on `should_release` and both check out `release_hash`. Today they read those from the `build` job. They must be repointed at whichever job replaces it, or they stop running and report nothing.

With three platforms and N test projects the matrix runs 3N builds, plus one in the release job. For ImGuiApp that's 30 plus 1 rather than the single build today. Wall clock doesn't suffer, because they're parallel, but the runner minutes are real.

The lever, if that cost bites before self-hosted runners land: build once per platform and have that platform's test cells download its output. The transfer is same-OS, so the `project.assets.json` paths still resolve and none of the cross-platform problems return. It trades 3N builds for 3 builds plus 3N artifact round-trips, and it reintroduces artifact machinery this design deliberately avoided. Not worth doing until the numbers say so.

## Extending ktsubuild rather than working around it

The workflow needs three things `ktsubuild` doesn't expose yet. All three are already implemented inside it — they're just not reachable from a command line. Reimplementing them in the workflow would put a second copy of each rule in a YAML file, and the second copy is the one that goes stale.

**`ktsubuild test list`** emits the test projects with the platform each is tied to:

```json
[
  { "project": "tests/ImGui.Widgets.Tests/ImGui.Widgets.Tests.csproj", "platform": "neutral" },
  { "project": "tests/ImGui.App.iOS.SmokeTest/ImGui.App.iOS.SmokeTest.csproj", "platform": "ios" }
]
```

`DotNetService.IsTestProject` and `GetProjectPlatform` already produce both fields. The `discover` job turns this into the matrix, expanding neutral projects across all three platforms and platform-tied ones only onto hosts that can build them.

Detection matters more than it looks: the five `*.UITests` projects don't end in `.Test` or `.Tests`, so they match only on project content. A name-based filter in YAML would silently drop all 107 tests — green, and testing nothing.

**`ktsubuild test run --project <path>`** runs one project with the coverage flags the pipeline already uses, instead of the workflow assembling a `dotnet test` invocation of its own. `TestAsync` takes the project list already; this exposes a scoped call.

**`ktsubuild build --no-test`** restores and builds without testing. Shipped in 2.2.0, and useful generally, though the release job uses `ci --no-test --no-release` instead so that it keeps the metadata, version-gate, and step-output behavior that lives only in `ci`.

**`ktsubuild ci --no-test` and `ktsubuild ci --no-release`** run the full pipeline while suppressing one step each. `--no-release` suppresses this run's release without changing what the `should_release` output reports, because the job reading that output is the one performing the release. Shipped in 2.3.0.

Each is a thin command over existing service methods. None changes what `ci` does, so repos still on `ci` are unaffected.

When a solution has no test projects, `test list` emits an empty array, the matrix is empty, and the test job is skipped. The release job must not require a non-empty matrix.

## Running the tests

Each cell runs `ktsubuild test run --project <path>`, which applies the same coverage flags the current pipeline uses.

**Do not pass `--nologo`.** It makes the runner report `total: 0` with exit code 5 while every test passes — verified on 2026-08-21 against `ImageGui.Core.Tests`, which reports 163 passing without the flag and zero with it. A CI step that passes it looks green and tests nothing.

Fanning out also sidesteps a flake that `KtsuBuild` currently retries around: the Microsoft.CodeCoverage collector intermittently drops its instrumentation IPC pipe during teardown when several test assemblies run in one invocation, surfacing as exit code 7 despite every test passing. One assembly per invocation removes the condition rather than retrying it.

## Risks

**The workflow can't use commands that aren't published yet.** CI installs the tool with `dotnet tool install ktsu.KtsuBuild.Tool`, so the three new commands must be released to nuget.org before any repo's workflow calls them. That makes this a two-repo delivery in a fixed order: extend and release KtsuBuild, then restructure the workflow. A workflow synced ahead of the tool release fails everywhere at once.

**Platform-tied projects can't build everywhere.** `GetBuildableProjects` exists because some projects are tied to a host: iOS needs macOS, `net10.0-windows` needs Windows. ImGuiApp has no Windows-tied targets, and its iOS targets are added conditionally on macOS hosts and covered by a separate workflow. Across sixty repos that won't hold universally.

Handle it at discovery, not in the cell. The `discover` job classifies each test project the way `GetProjectPlatform` does — neutral, Windows, or iOS — and emits the platform pairs that can actually build, so a Windows-only test project produces one cell rather than three, two of which fail. A cell that skips its own work reports green while testing nothing, which is the failure mode this whole design is trying to remove.

**The synced file propagates verbatim.** `ktsu sync` hashes every copy of a filename across the tree and propagates whichever group is chosen, so the restructured workflow reaches sixty repos byte for byte. Nothing in it may be specific to ImGuiApp: no hardcoded project names, no hardcoded test counts, no repo name outside the expressions GitHub already substitutes. The matrix has to be discovered at runtime for correctness, not only for elegance.

**NuGet has two indexes and they lag apart.** Verified on 2026-08-21 releasing 2.3.0: the package blob appeared in nuget.org's flat container 210 seconds after the GitHub release, but `dotnet tool install` still could not resolve it, and needed roughly another 30 seconds for the registration index the client reads. A workflow synced immediately after a tool release can fail to resolve a version whose release page already says published. The install step should tolerate that rather than fail the run outright.

**Blast radius.** This restructure reaches every ktsu repo on the next `ktsu sync`. A repo with two test projects pays six builds and six runner starts to parallelize work that took two minutes serially, and gets slower in wall clock once job startup is counted. The shape is right for repos with heavy suites and mildly negative for small ones — which is most of them. Worth deciding whether that trade holds org-wide before syncing, rather than after.

**Quality gate now blocks release.** Deliberate, and a behavior change. A SonarCloud outage would stop publishing; `continue-on-error` on the `end` step is the escape hatch if that ever happens.

**macOS runner cost.** Billed at a premium relative to Linux. Named here so that moving the matrix to self-hosted runners is a planned response rather than a reaction to a bill.

## Validating it

CI changes can't be proven by reasoning about YAML. Before this syncs anywhere:

- Run it on a branch in ImGuiApp and confirm the matrix expands to 3 platforms × the real test projects.
- Confirm the reported test counts match what the projects produce locally. A step that runs no tests and exits zero is the specific failure this design is most exposed to.
- Confirm SonarCloud receives coverage from all three platforms' uploads and reports a figure comparable to today's.
- Confirm the release job runs `ktsubuild release` only under the existing release condition, and that a PR run analyses without publishing.
- Confirm the wall clock is under the cap with room to spare, and record the number.

## Revisions

**2026-08-21, the release job.** The original decision to stop calling `ktsubuild ci` rested on the claim that "`ktsubuild release` still handles pack, publish, and release, so no release automation is lost." Reading `CiCommand` against `ReleaseCommand` showed that false. `ci` does five things `release` does not: update and commit the metadata files, update the repository topics from TAGS.md, gate the release on the version increment (which is how `[skip ci]` works), honor a forced version bump, and write the four step outputs. `ReleaseService.ExecuteReleaseAsync` packs, publishes, and creates the GitHub release unconditionally once called, gated only on `ShouldRelease`, which means "on main, untagged, official repo" and nothing about whether the version moved.

Dropping the step outputs would have silently disabled the `winget` and `security` jobs, since both gate on `should_release`. That is the failure mode this design exists to remove, so `ci` stays and gained two skip flags instead. Shipped in `ktsu.KtsuBuild.Tool` 2.3.0.

**2026-08-21, the `security` job.** The original job graph omitted it. It exists, it is `needs: build`, and it consumes the same two outputs `winget` does.

## Sequence

1. `KtsuBuild`: add `test list`, `test run --project`, and `build --no-test`. Release to nuget.org.
2. `ImGuiApp`: restructure `dotnet.yml` against the released tool. Validate on a branch.
3. Sync to the other repos once the numbers from step 2 are known.

## Out of scope

The iOS workflow, the winget job, the dependabot-merge workflow, and any change to what `ktsubuild ci` does — it stays as it is for repos that haven't moved.
