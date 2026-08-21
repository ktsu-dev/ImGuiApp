# CI: parallel test matrix across platforms

Restructures `.github/workflows/dotnet.yml` so tests fan out across platforms and projects instead of running serially on one Windows runner. The immediate reason is that CI already fails: two runs were cancelled at the 20-minute cap, including the merge of PR #322.

This file is propagated to every ktsu repo by `ktsu sync`, so the design has to work for a repo with one test project as well as one with ten.

## The problem

`ktsubuild ci` runs restore, build, and every test project in sequence on `windows-latest`, inside a job capped at 20 minutes. Merging the five demo UI test projects added 107 headless tests. Measured against `ImGuiMarkdownDemo.UITests`, those cost about 5.4 seconds each — roughly 9.6 minutes of new work in a job that already ran 13.6 to 19.9 minutes.

The cap can't simply be raised. It's the wrong shape: everything is serial, one platform is tested, and the budget is shared between building, testing, analysis, and release.

## Decisions

**Stop calling `ktsubuild ci`; call restore, build, and test directly.** `CiCommand` is a thin orchestrator — version, restore, build, test, release — with no way to skip or scope the test step. Calling the parts directly lets the workflow decide what runs where. `ktsubuild release` still handles pack, publish, and release, so no release automation is lost.

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
                    sonar begin -> restore -> build -> sonar end (imports coverage)
                    -> ktsubuild release   [only when the release condition holds]

winget (windows, needs: release)    unchanged
ios (separate workflow)             unchanged
```

With three platforms and N test projects the matrix runs 3N builds, plus one in the release job. For ImGuiApp that's 30 plus 1 rather than the single build today. Wall clock doesn't suffer, because they're parallel, but the runner minutes are real.

The lever, if that cost bites before self-hosted runners land: build once per platform and have that platform's test cells download its output. The transfer is same-OS, so the `project.assets.json` paths still resolve and none of the cross-platform problems return. It trades 3N builds for 3 builds plus 3N artifact round-trips, and it reintroduces artifact machinery this design deliberately avoided. Not worth doing until the numbers say so.

## Discovering the test projects

The matrix can't be hardcoded, because this file lands in every ktsu repo. A `discover` job lists the projects in the solution and selects the test ones by the rules `KtsuBuild.DotNetService.IsTestProject` already uses: a file or directory name ending in `.Test` or `.Tests`, a directory named exactly `Test` or `Tests`, or project content containing `<IsTestProject>true</IsTestProject>`, `Sdk="Microsoft.NET.Sdk.Test"`, or an MSTest SDK reference.

That last clause matters here: the five `*.UITests` projects match on content, not on name.

**Two implementations of one rule is the failure this repo has already paid for.** The preferred fix is a `ktsubuild list-test-projects --json` command, so the workflow and the build tool share one definition. Until that exists, the discovery step reimplements the rule in PowerShell, and drift between the two is a recorded risk rather than a surprise.

When the solution has no test projects, `discover` emits an empty matrix and the test job is skipped. The release job must not require a non-empty matrix.

## Running the tests

Each cell runs `dotnet test <project> --coverage --coverage-output-format xml`, scoped to a single project.

**Do not pass `--nologo`.** It makes the runner report `total: 0` with exit code 5 while every test passes — verified on 2026-08-21 against `ImageGui.Core.Tests`, which reports 163 passing without the flag and zero with it. A CI step that passes it looks green and tests nothing.

Fanning out also sidesteps a flake that `KtsuBuild` currently retries around: the Microsoft.CodeCoverage collector intermittently drops its instrumentation IPC pipe during teardown when several test assemblies run in one invocation, surfacing as exit code 7 despite every test passing. One assembly per invocation removes the condition rather than retrying it.

## Risks

**Rule drift in test discovery.** The workflow's PowerShell filter and `KtsuBuild.IsTestProject` must agree. If they diverge, a project silently stops being tested — the failure is invisible, because a skipped project reports nothing. Closing this properly means the `ktsubuild list-test-projects` command above.

**Platform-tied projects can't build everywhere.** `GetBuildableProjects` exists because some projects are tied to a host: iOS needs macOS, `net10.0-windows` needs Windows. ImGuiApp has no Windows-tied targets, and its iOS targets are added conditionally on macOS hosts and covered by a separate workflow. Across sixty repos that won't hold universally.

Handle it at discovery, not in the cell. The `discover` job classifies each test project the way `GetProjectPlatform` does — neutral, Windows, or iOS — and emits the platform pairs that can actually build, so a Windows-only test project produces one cell rather than three, two of which fail. A cell that skips its own work reports green while testing nothing, which is the failure mode this whole design is trying to remove.

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

## Out of scope

The iOS workflow, the winget job, the dependabot-merge workflow, and any change to `KtsuBuild` beyond the `list-test-projects` command named above as the preferred fix for rule drift.
