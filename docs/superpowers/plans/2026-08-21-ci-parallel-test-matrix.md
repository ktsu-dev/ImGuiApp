# CI parallel test matrix Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Restructure `.github/workflows/dotnet.yml` so test projects fan out across Linux, Windows, and macOS in parallel instead of running serially in one Windows job that already exceeds its timeout.

**Architecture:** Three jobs replace the single `build` job. `discover` asks `ktsubuild test list` what test projects exist and emits a matrix. `test` runs one project on one platform per cell and uploads that cell's coverage. `release` downloads every cell's coverage, runs `ktsubuild ci --no-test --no-release` inside the SonarCloud begin and end window, then releases only if the quality gate passed and the version moved. The existing `winget` and `security` jobs are repointed at `release`.

**Tech Stack:** GitHub Actions, `ktsu.KtsuBuild.Tool` 2.3.0 or later, SonarCloud, jq.

**Spec:** `docs/superpowers/specs/2026-08-21-ci-parallel-test-matrix-design.md` — read it, including its Revisions section, which records three corrections made after the original design.

## Global Constraints

- **This file propagates verbatim to roughly sixty repositories** through `ktsu sync`, which hashes every copy of a filename and pushes the chosen version to all the others. Nothing in the workflow may be specific to ImGuiApp: no hardcoded project names, no hardcoded test counts, no repository name outside the `${{ github.* }}` expressions GitHub already substitutes. The matrix must be discovered at runtime for correctness, not for elegance.
- **It must work for a repo with one test project and for a repo with fifteen, and for a repo with none.** When there are no test projects the matrix is empty, the `test` job is skipped, and `release` must still run.
- **Never pass `--nologo` to `dotnet test` or to any `ktsubuild` command.** It makes the runner report `total: 0` with exit code 5 while every test passes. A step that passes it looks green and tests nothing.
- **`ktsubuild test list` writes its errors to stdout, not stderr, and exits 1.** Always check the exit code before parsing stdout, or the workflow will try to parse an error message as JSON.
- **A step that skips its own work and reports success is the specific failure this design exists to remove.** Where a shell step can silently do nothing, make it fail loudly instead.
- YAML indents with two spaces. Match the existing file's style, including its comment voice.
- US English throughout.
- Prose in comments must not use em dashes, en dashes, or semicolons joining clauses.
- **Building rewrites `.editorconfig`.** Run `git checkout .editorconfig` before every commit, and stage files by name. Never `git add -A`.
- Commit messages carry a version tag such as `[patch]` or `[minor]`. Do not add Co-Authored-By lines.
- Do not touch `.github/workflows/ios.yml`, `.github/workflows/dependabot-merge.yml`, or `.github/workflows/update-sdks.yml`.

## What exists today

`.github/workflows/dotnet.yml` has three jobs:

- `build` on `windows-latest`, `timeout-minutes: 20`. Sets up JDK 17, checks out with full history, sets up .NET, caches and installs the SonarCloud scanner, installs `ktsubuild`, runs `dotnet-sonarscanner begin`, runs `ktsubuild ci`, runs `dotnet-sonarscanner end`, and uploads `./coverage/*`. Its outputs are `version`, `release_hash`, and `should_release`, all read from `steps.pipeline.outputs`.
- `winget`, `needs: build`, `if: needs.build.outputs.should_release == 'true'`, checks out `needs.build.outputs.release_hash`.
- `security`, `needs: build`, same condition, same checkout ref.

The `End SonarQube` step is conditioned on `steps.pipeline.outputs.build_skipped != 'true'`. `ktsubuild ci` always writes `build_skipped=false`, and still will.

## File Structure

| File | Responsibility |
| --- | --- |
| `.github/workflows/dotnet.yml` (modify) | The whole change. Three jobs replace `build`, and two jobs are repointed. |

Everything lands in one file, so the tasks below split by reviewable unit rather than by file: the discovery contract first, then the matrix that consumes it, then the release job, then the live validation the spec demands.

---

### Task 1: Add the discover job

**Files:**
- Modify: `.github/workflows/dotnet.yml`

**Interfaces:**
- Consumes: `ktsubuild test list --workspace <path>`, which prints one line of JSON to stdout, an array of `{"project": "<forward-slash path relative to the workspace>", "platform": "neutral"|"windows"|"ios"}`, sorted by project. On failure it prints a message to stdout and exits 1.
- Produces: job outputs `matrix` (a JSON object with a single `include` key) and `has_tests` (the string `"true"` or `"false"`). Task 2 consumes both by those exact names. Each `include` entry has the keys `os`, `project`, `name`, and `slug`.

- [ ] **Step 1: Read the current workflow**

```bash
cd /c/dev/ktsu-dev/ImGuiApp
cat .github/workflows/dotnet.yml
```

Note the `env:` block defining `DOTNET_VERSION`, the `Install KtsuBuild` step (it appears twice and is identical both times), and the exact `actions/*` versions in use. Reuse those versions rather than introducing new ones.

- [ ] **Step 2: Insert the discover job**

Add this as the FIRST job under `jobs:`, before the existing `build` job. Do not modify `build` in this task.

```yaml
  discover:
    name: Discover Test Projects
    runs-on: ubuntu-latest
    timeout-minutes: 10

    outputs:
      matrix: ${{ steps.discover.outputs.matrix }}
      has_tests: ${{ steps.discover.outputs.has_tests }}

    steps:
      - name: Checkout Repository
        uses: actions/checkout@v7

      - name: Setup .NET SDK ${{ env.DOTNET_VERSION }}
        uses: actions/setup-dotnet@v6
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}.x

      - name: Install KtsuBuild
        shell: bash
        run: |
          dotnet tool install ktsu.KtsuBuild.Tool --tool-path "${{ runner.temp }}/ktsubuild"
          echo "${{ runner.temp }}/ktsubuild" >> "$GITHUB_PATH"

      # `test list` reports every test project regardless of the host it runs on, unlike the
      # filter `build` and `ci` apply, so one Linux job can enumerate cells that Windows and
      # macOS runners will execute. It writes failures to stdout rather than stderr, so the
      # exit code is the only reliable signal and has to be checked before parsing.
      - name: Discover Test Projects
        id: discover
        shell: bash
        run: |
          set -euo pipefail

          if ! projects=$(ktsubuild test list --workspace "$GITHUB_WORKSPACE"); then
            echo "::error::ktsubuild test list failed:"
            echo "$projects"
            exit 1
          fi

          echo "Discovered test projects:"
          echo "$projects" | jq .

          # An unrecognized platform must stop the run rather than drop the project. Dropping
          # it would produce a smaller matrix that still reports success, which is the failure
          # this design exists to remove.
          unknown=$(echo "$projects" | jq -r '[.[] | select(.platform as $p | ["neutral","windows","ios"] | index($p) | not) | .platform] | unique | join(", ")')
          if [ -n "$unknown" ]; then
            echo "::error::ktsubuild test list reported unrecognized platform(s): $unknown"
            exit 1
          fi

          matrix=$(echo "$projects" | jq -c '
            {
              include: [
                .[]
                | . as $p
                | {
                    neutral: ["ubuntu-latest", "windows-latest", "macos-latest"],
                    windows: ["windows-latest"],
                    ios: ["macos-latest"]
                  }[$p.platform][]
                | {
                    os: .,
                    project: $p.project,
                    name: ($p.project | split("/") | last | rtrimstr(".csproj")),
                    slug: ($p.project | rtrimstr(".csproj") | gsub("[^A-Za-z0-9]"; "-"))
                  }
              ]
            }')

          count=$(echo "$matrix" | jq '.include | length')
          echo "Matrix has $count cell(s)."
          echo "$matrix" | jq .

          echo "matrix=$matrix" >> "$GITHUB_OUTPUT"
          if [ "$count" -gt 0 ]; then
            echo "has_tests=true" >> "$GITHUB_OUTPUT"
          else
            echo "has_tests=false" >> "$GITHUB_OUTPUT"
          fi
```

`slug` exists because artifact names cannot contain `/`, and two test projects in different directories can share a basename. The slug is derived from the whole path, so it stays unique.

- [ ] **Step 3: Validate the jq locally before pushing**

The jq program is the part most likely to be wrong, and a CI round trip to find out is slow. Run it against real input first.

**Every check in this step was run on 2026-08-21 and passed, with the results given below.** They are here so you can confirm the code still behaves that way after transcription, not to discover it for the first time. If any result differs, the transcription is wrong, not the expectation.

Two local-environment traps, both hit while writing this plan:

- **The globally installed `ktsubuild` is likely stale.** It was 2.0.1 on the author's machine, which predates `test list` entirely and fails with `'test' was not matched`. Install the current tool to a scratch path and call it explicitly rather than relying on whatever is on `PATH`:

```bash
TOOLS="$(mktemp -d)/ktsubuild"
dotnet tool install ktsu.KtsuBuild.Tool --tool-path "$TOOLS"
KB="$TOOLS/ktsubuild"
"$KB" --version   # must be 2.3.0 or later
```

- **`jq` is not installed on this Windows machine** and is not on `PATH` under Git Bash, though every GitHub runner has it. Fetch a standalone binary rather than installing anything:

```bash
JQ="$(mktemp -d)/jq.exe"
curl -sL -o "$JQ" https://github.com/jqlang/jq/releases/latest/download/jq-windows-amd64.exe
"$JQ" --version
```

Use `"$KB"` and `"$JQ"` in place of `ktsubuild` and `jq` in the commands below.

```bash
cd /c/dev/ktsu-dev/ImGuiApp
"$KB" test list --workspace . > /tmp/projects.json || echo "EXIT NONZERO"
cat /tmp/projects.json | "$JQ" -c '
  {
    include: [
      .[]
      | . as $p
      | {
          neutral: ["ubuntu-latest", "windows-latest", "macos-latest"],
          windows: ["windows-latest"],
          ios: ["macos-latest"]
        }[$p.platform][]
      | {
          os: .,
          project: $p.project,
          name: ($p.project | split("/") | last | rtrimstr(".csproj")),
          slug: ($p.project | rtrimstr(".csproj") | gsub("[^A-Za-z0-9]"; "-"))
        }
    ]
  }' | "$JQ" '.include | length'
```

Expected: `42`. ImGuiApp has 14 test projects and all are `neutral`, so 14 × 3 = 42. If you get 14, the platform expansion is not expanding. If you get 0, the lookup returned null.

Then check the slugs are unique, which is what keeps artifact names from colliding:

```bash
cd /c/dev/ktsu-dev/ImGuiApp
"$KB" test list --workspace . | "$JQ" -r '.[] | .project | rtrimstr(".csproj") | gsub("[^A-Za-z0-9]"; "-")' | sort | uniq -d
```

Expected: no output. Any line printed is a duplicate slug and a real bug.

Also exercise the empty case, because a repo with no test projects must produce an empty matrix rather than an error:

```bash
echo '[]' | "$JQ" -c '{include: [.[] | . as $p | {neutral: ["ubuntu-latest","windows-latest","macos-latest"], windows: ["windows-latest"], ios: ["macos-latest"]}[$p.platform][] | {os: ., project: $p.project, name: ($p.project | split("/") | last | rtrimstr(".csproj")), slug: ($p.project | rtrimstr(".csproj") | gsub("[^A-Za-z0-9]"; "-"))}]}'
```

Expected: `{"include":[]}`.

And confirm the unknown-platform guard fires, which is the check that prevents a silently smaller matrix:

```bash
echo '[{"project":"tests/A/A.csproj","platform":"solaris"}]' | "$JQ" -r '[.[] | select(.platform as $p | ["neutral","windows","ios"] | index($p) | not) | .platform] | unique | join(", ")'
```

Expected: `solaris`. If this prints nothing, the guard does not discriminate and would let an unknown platform through.

Then run the same guard against the real project list, which is the positive control that stops the check from being vacuous:

```bash
cd /c/dev/ktsu-dev/ImGuiApp
"$KB" test list --workspace . | "$JQ" -r '[.[] | select(.platform as $p | ["neutral","windows","ios"] | index($p) | not) | .platform] | unique | join(", ")'
```

Expected: empty. A guard that fires on `solaris` and stays silent on real input is discriminating. One that fires on both, or neither, is not.

- [ ] **Step 4: Validate the YAML parses**

```bash
cd /c/dev/ktsu-dev/ImGuiApp
python -c "import yaml,sys; d=yaml.safe_load(open('.github/workflows/dotnet.yml')); print('jobs:', list(d['jobs'].keys()))"
```

Expected: `jobs: ['discover', 'build', 'winget', 'security']`.

- [ ] **Step 5: Commit**

```bash
cd /c/dev/ktsu-dev/ImGuiApp
git checkout .editorconfig
git add .github/workflows/dotnet.yml
git commit -m "ci: add a job that discovers test projects into a matrix [patch]"
```

---

### Task 2: Replace the build job with the test matrix

**Files:**
- Modify: `.github/workflows/dotnet.yml`

**Interfaces:**
- Consumes: `needs.discover.outputs.matrix` and `needs.discover.outputs.has_tests` from Task 1, and `matrix.os`, `matrix.project`, `matrix.name`, `matrix.slug` within each cell.
- Produces: artifacts named `coverage-<slug>-<os>`, each containing that cell's `coverage/` directory, so the artifact root holds `coverage.xml` and `TestResults/`. Task 3 downloads them with the pattern `coverage-*` and must not merge them, because every one contains a file named `coverage.xml`.

- [ ] **Step 1: Add the test job**

Insert after `discover` and before the existing `build` job.

```yaml
  test:
    name: Test ${{ matrix.name }} (${{ matrix.os }})
    needs: discover
    if: needs.discover.outputs.has_tests == 'true'
    runs-on: ${{ matrix.os }}
    timeout-minutes: 30

    strategy:
      # One platform's failure must not cancel the others. Knowing that a project fails on
      # macOS only is the point of running the matrix at all.
      fail-fast: false
      matrix: ${{ fromJson(needs.discover.outputs.matrix) }}

    steps:
      - name: Checkout Repository
        uses: actions/checkout@v7
        with:
          lfs: true
          submodules: recursive

      - name: Setup .NET SDK ${{ env.DOTNET_VERSION }}
        uses: actions/setup-dotnet@v6
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}.x
          cache: true
          cache-dependency-path: |
            **/*.csproj
            **/Directory.Packages.props
            **/global.json

      - name: Install KtsuBuild
        shell: bash
        run: |
          dotnet tool install ktsu.KtsuBuild.Tool --tool-path "${{ runner.temp }}/ktsubuild"
          echo "${{ runner.temp }}/ktsubuild" >> "$GITHUB_PATH"

      # One test project per invocation. Besides parallelizing the suite, this removes the
      # condition behind a coverage-collector flake that KtsuBuild otherwise retries around:
      # the instrumentation pipe drops during teardown when several assemblies run together.
      - name: Run Tests
        shell: bash
        run: |
          set -euo pipefail
          ktsubuild test run --project "${{ matrix.project }}" --workspace "$GITHUB_WORKSPACE" --verbose

      - name: Upload Coverage
        uses: actions/upload-artifact@v7
        if: always()
        with:
          name: coverage-${{ matrix.slug }}-${{ matrix.os }}
          path: ./coverage/*
          retention-days: 7
          if-no-files-found: warn
```

- [ ] **Step 2: Delete the old build job**

Remove the entire `build:` job, from its `build:` key through the end of its `Upload Coverage Report` step, leaving `winget:` and `security:` in place. Task 3 adds the `release` job that takes over its remaining responsibilities.

At the end of this step the workflow is deliberately broken: `winget` and `security` still say `needs: build`. Task 3 fixes that. Do not attempt to run CI between these two tasks.

- [ ] **Step 3: Validate the YAML parses and the references resolve**

```bash
cd /c/dev/ktsu-dev/ImGuiApp
python -c "import yaml; d=yaml.safe_load(open('.github/workflows/dotnet.yml')); print('jobs:', list(d['jobs'].keys()))"
```

Expected: `jobs: ['discover', 'test', 'winget', 'security']`.

- [ ] **Step 4: Commit**

```bash
cd /c/dev/ktsu-dev/ImGuiApp
git checkout .editorconfig
git add .github/workflows/dotnet.yml
git commit -m "ci: fan tests out across platform and project [minor]"
```

---

### Task 3: Add the release job and repoint the dependent jobs

**Files:**
- Modify: `.github/workflows/dotnet.yml`

**Interfaces:**
- Consumes: the `coverage-*` artifacts from Task 2, and `needs.discover.outputs.has_tests` from Task 1.
- Produces: job outputs `version`, `release_hash`, `should_release`, consumed by `winget` and `security`.

- [ ] **Step 1: Add the release job**

Insert after `test` and before `winget`. This job keeps `windows-latest`, which is what the deleted `build` job used. The spec's Revisions section explains why: `GetBuildableProjects` filters by host, so a Linux release job would build and publish fewer projects in any repo that has a Windows-tied target framework, and this file reaches sixty repos.

The first eight steps are byte-identical to the deleted `build` job's, including the `Install KtsuBuild` step, which this job needs just as much as the matrix cells do. They are reproduced in full below rather than referenced, because a silently dropped setup step fails late and confusingly.

```yaml
  release:
    name: Analyze & Release
    needs: [discover, test]
    # `always()` is required because `test` is skipped when a repo has no test projects, and a
    # skipped dependency would otherwise skip this job too. The explicit result checks are what
    # keep a genuine test failure from releasing anyway.
    if: |
      always()
      && needs.discover.result == 'success'
      && (needs.test.result == 'success' || needs.test.result == 'skipped')
    runs-on: windows-latest
    timeout-minutes: 30
    permissions:
      contents: write # For creating releases and committing metadata
      packages: write # For publishing packages

    outputs:
      version: ${{ steps.pipeline.outputs.version }}
      release_hash: ${{ steps.pipeline.outputs.release_hash }}
      should_release: ${{ steps.pipeline.outputs.should_release }}

    steps:
      - name: Set up JDK 17
        uses: actions/setup-java@v5
        with:
          java-version: 17
          distribution: "zulu" # Alternative distribution options are available.

      - name: Checkout Repository
        uses: actions/checkout@v7
        with:
          fetch-depth: 0 # Full history for versioning
          fetch-tags: true
          lfs: true
          submodules: recursive
          persist-credentials: true

      - name: Setup .NET SDK ${{ env.DOTNET_VERSION }}
        uses: actions/setup-dotnet@v6
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}.x
          cache: true
          cache-dependency-path: |
            **/*.csproj
            **/Directory.Packages.props
            **/global.json

      # Ensure NuGet packages directory exists for caching (prevents error when pipeline exits early)
      - name: Ensure NuGet cache directory exists
        run: New-Item -Path "$env:USERPROFILE\.nuget\packages" -ItemType Directory -Force
        shell: pwsh

      - name: Cache SonarQube Cloud packages
        if: ${{ env.SONAR_TOKEN != '' }}
        uses: actions/cache@v6
        env:
          SONAR_TOKEN: ${{ secrets.SONAR_TOKEN }}
        with:
          path: ~\sonar\cache
          key: ${{ runner.os }}-sonar
          restore-keys: ${{ runner.os }}-sonar

      - name: Cache SonarQube Cloud scanner
        if: ${{ env.SONAR_TOKEN != '' }}
        id: cache-sonar-scanner
        uses: actions/cache@v6
        env:
          SONAR_TOKEN: ${{ secrets.SONAR_TOKEN }}
        with:
          path: .\.sonar\scanner
          key: ${{ runner.os }}-sonar-scanner
          restore-keys: ${{ runner.os }}-sonar-scanner

      - name: Install SonarQube Cloud scanner
        if: ${{ env.SONAR_TOKEN != '' && steps.cache-sonar-scanner.outputs.cache-hit != 'true' }}
        env:
          SONAR_TOKEN: ${{ secrets.SONAR_TOKEN }}
        shell: pwsh
        run: |
          New-Item -Path .\.sonar\scanner -ItemType Directory
          dotnet tool update dotnet-sonarscanner --tool-path .\.sonar\scanner

      - name: Install KtsuBuild
        shell: pwsh
        run: |
          dotnet tool install ktsu.KtsuBuild.Tool --tool-path "${{ runner.temp }}/ktsubuild"
          if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
          "${{ runner.temp }}/ktsubuild" >> $env:GITHUB_PATH

      # Every cell wrote a file named coverage.xml, so the downloads must stay in their own
      # per-artifact directories. Merging them would leave one file and report the coverage of
      # a single test project as though it were the whole repository's.
      - name: Download Coverage
        if: needs.discover.outputs.has_tests == 'true'
        uses: actions/download-artifact@v7
        with:
          pattern: coverage-*
          path: coverage

      - name: Begin SonarQube
        if: ${{ env.SONAR_TOKEN != '' }}
        env:
          SONAR_TOKEN: ${{ secrets.SONAR_TOKEN }}
        shell: pwsh
        run: |
          .\.sonar\scanner\dotnet-sonarscanner begin /k:"${{ github.repository_owner }}_${{ github.event.repository.name }}" /o:"${{ github.repository_owner }}" /d:sonar.token="$env:SONAR_TOKEN" /d:sonar.host.url="https://sonarcloud.io" /d:sonar.projectBaseDir="${{ github.workspace }}" /d:sonar.cs.vscoveragexml.reportsPaths="coverage/**/coverage.xml" /d:sonar.coverage.exclusions="**/*Test*.cs,**/*.Tests.cs,**/*.Tests/**/*,**/obj/**/*,**/*.dll,**/NativeExports.cs" /d:sonar.cs.vstest.reportsPaths="coverage/**/*.trx" /d:sonar.exclusions="**/NativeExports.cs"

      # `ci` rather than restore and build directly, because it is the only place that updates
      # and commits the metadata files, updates the repository topics, applies the version gate
      # behind `[skip ci]`, and writes the step outputs the winget and security jobs read.
      # The tests already ran in the matrix, and the release waits for the quality gate below.
      - name: Run KtsuBuild Pipeline
        id: pipeline
        shell: pwsh
        env:
          GH_TOKEN: ${{ github.token }}
          NUGET_API_KEY: ${{ secrets.NUGET_KEY }}
          KTSU_PACKAGE_KEY: ${{ secrets.KTSU_PACKAGE_KEY }}
          EXPECTED_OWNER: ktsu-dev
        run: |
          $versionBump = "${{ github.event.inputs.version-bump }}"

          $args = @("ci", "--workspace", "${{ github.workspace }}", "--no-test", "--no-release", "--verbose")
          if (![string]::IsNullOrEmpty($versionBump) -and $versionBump -ne "auto") {
            $args += @("--version-bump", $versionBump)
          }

          & ktsubuild @args
          if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

      - name: End SonarQube
        if: env.SONAR_TOKEN != '' && steps.pipeline.outputs.build_skipped != 'true'
        env:
          SONAR_TOKEN: ${{ secrets.SONAR_TOKEN }}
        shell: pwsh
        run: |
          .\.sonar\scanner\dotnet-sonarscanner end /d:sonar.token="$env:SONAR_TOKEN"

      # After the gate, not before. A failed quality gate fails the step above and this never
      # runs, which is a deliberate change from the old shape where `ci` released first and
      # Sonar reported afterward.
      - name: Release
        if: steps.pipeline.outputs.should_release == 'true'
        shell: pwsh
        env:
          GH_TOKEN: ${{ github.token }}
          NUGET_API_KEY: ${{ secrets.NUGET_KEY }}
          KTSU_PACKAGE_KEY: ${{ secrets.KTSU_PACKAGE_KEY }}
          EXPECTED_OWNER: ktsu-dev
        run: |
          ktsubuild release --workspace "${{ github.workspace }}" --verbose
          if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

      - name: Upload Coverage Report
        uses: actions/upload-artifact@v7
        if: always()
        with:
          name: coverage-report
          path: |
            ./coverage/*
          retention-days: 7
          if-no-files-found: ignore
```

- [ ] **Step 2: Repoint winget and security**

In both jobs, change `needs: build` to `needs: release`, and change every `needs.build.outputs.` to `needs.release.outputs.`. There are two such references in each job: `should_release` in the `if:`, and `release_hash` in the checkout `ref:`.

```bash
cd /c/dev/ktsu-dev/ImGuiApp
grep -n "needs: build\|needs.build.outputs" .github/workflows/dotnet.yml
```

Expected before the edit: four lines. Expected after: no output.

- [ ] **Step 3: Verify no reference to the deleted job survives**

```bash
cd /c/dev/ktsu-dev/ImGuiApp
grep -n "needs.build\|needs: build" .github/workflows/dotnet.yml && echo "STALE REFERENCE FOUND" || echo "clean"
python -c "
import yaml
d = yaml.safe_load(open('.github/workflows/dotnet.yml'))
jobs = d['jobs']
print('jobs:', list(jobs.keys()))
for name, job in jobs.items():
    needs = job.get('needs')
    print(' ', name, '<- needs:', needs)
    for dep in ([needs] if isinstance(needs, str) else (needs or [])):
        assert dep in jobs, f'{name} needs missing job {dep}'
print('all needs resolve')
"
```

Expected: `clean`, then the five jobs `discover`, `test`, `release`, `winget`, `security`, then `all needs resolve`.

- [ ] **Step 4: Commit**

```bash
cd /c/dev/ktsu-dev/ImGuiApp
git checkout .editorconfig
git add .github/workflows/dotnet.yml
git commit -m "ci: analyze and release after the matrix, gated on the quality gate [minor]"
```

---

### Task 4: Validate on a branch

The spec is explicit that CI changes cannot be proven by reasoning about YAML. This task is the proof, and it is the reason the plan exists in a repo rather than in a template.

**Files:** none.

- [ ] **Step 1: Push and start a run**

The workflow's triggers are `push` on `main` and `develop`, and `pull_request`. A push to a feature branch fires neither, so opening the pull request is what starts the run.

```bash
cd /c/dev/ktsu-dev/ImGuiApp
git push -u origin feat/ci-parallel-test-matrix
gh pr create --base main --head feat/ci-parallel-test-matrix \
  --title "Fan CI tests out across platforms and projects" \
  --body "Replaces the serial windows-latest test run with a discover, matrix, release shape. Validation results are recorded in the plan's task 4."
```

- [ ] **Step 2: Watch the run and record what happened**

Capture the run id once and reuse it, since every check below needs it.

```bash
cd /c/dev/ktsu-dev/ImGuiApp
RUN_ID=$(gh run list --branch feat/ci-parallel-test-matrix --workflow ".NET Workflow" --limit 1 --json databaseId --jq '.[0].databaseId')
echo "RUN_ID=$RUN_ID"
gh run watch "$RUN_ID" --exit-status
```

`gh run watch` exits non-zero when the run fails. A failure here is information, not a reason to stop: read the failing job's log and fix the workflow, because that is what this task is for.

- [ ] **Step 3: Confirm each of the spec's five validation claims, by evidence**

Record the actual numbers. Each of these has a specific failure it is looking for.

1. **The matrix expanded to the real projects.** Read the `discover` job's log and confirm it printed 42 cells for ImGuiApp (14 projects × 3 platforms). A count of 14 means the platform expansion failed. A count of 0 means discovery failed and the run is testing nothing.

```bash
cd /c/dev/ktsu-dev/ImGuiApp
gh run view "$RUN_ID" --log --job "$(gh run view "$RUN_ID" --json jobs --jq '.jobs[] | select(.name=="Discover Test Projects") | .databaseId')" | grep -E "Matrix has|Discovered"
```

2. **The reported test counts match what the projects produce locally.** This is the check the spec calls the design's most exposed failure. Pick three cells, including one `*.UITests` project, and compare the count in the CI log against a local run:

```bash
cd /c/dev/ktsu-dev/ImGuiApp
ktsubuild test run --project tests/ImGuiWidgetsDemo.UITests/ImGuiWidgetsDemo.UITests.csproj --workspace . 2>&1 | grep -E "total:|succeeded:"
```

A CI cell reporting `total: 0` while passing is the exact failure `--nologo` causes and the reason it is banned. If any cell reports zero tests, stop and investigate rather than accepting a green run.

3. **SonarCloud received coverage from all three platforms.** Confirm the `End SonarQube` step logged the number of coverage files it imported, and that the SonarCloud project page reports a coverage figure comparable to the last `main` run. A figure far below the previous one means the fan-in globbed nothing and Sonar treated the unmatched files as uncovered.

4. **The release job did not publish from a pull request.** Confirm `should_release` was `false` and the `Release` step was skipped, while `Begin`/`End SonarQube` still ran. A PR run must analyze without publishing.

5. **Wall clock is under the cap with room to spare.** Record the total run duration and the slowest single cell.

```bash
cd /c/dev/ktsu-dev/ImGuiApp
gh run view "$RUN_ID" --json jobs --jq '.jobs[] | {name, startedAt, completedAt, conclusion}'
```

- [ ] **Step 4: Write the numbers into the spec**

The spec's Sequence step 3 says to sync to the other repos "once the numbers from step 2 are known". Record them so that decision has evidence:

Append a `## Measured` section to `docs/superpowers/specs/2026-08-21-ci-parallel-test-matrix-design.md` giving the cell count, the total wall clock, the slowest cell, the SonarCloud coverage figure before and after, and the runner minutes consumed. State plainly whether the blast-radius trade the spec worried about looks acceptable for a small repo, since that is what gates the sync.

```bash
cd /c/dev/ktsu-dev/ImGuiApp
git checkout .editorconfig
git add docs/superpowers/specs/2026-08-21-ci-parallel-test-matrix-design.md
git commit -m "docs: record the measured results of the parallel test matrix [patch]"
git push
```

- [ ] **Step 5: Stop and report**

Merging is the repository owner's call, and so is syncing this file to the other repos. Report the five validation results with their numbers, and say explicitly whether any of them fell short.

---

## Notes carried in from the KtsuBuild work

- `ktsubuild test list` reports 14 test projects for ImGuiApp, not 15. `ImGui.App.iOS.SmokeTest` is a console executable with no test framework and is correctly excluded, and `ios.yml` runs it separately on a simulator.
- `test run` passes the project path positionally to `dotnet test`. A path that does not exist reports zero tests rather than failing, which is why the matrix must come from `test list` rather than from hand-written paths.
- `ci --no-test` does not suppress the iOS validation build, because that is a build rather than a test. The release job runs on Windows, where it reports the detected heads and skips.
- NuGet has two indexes and they lag apart. Releasing 2.3.0, the package blob reached the flat container after 210 seconds but `dotnet tool install` could not resolve it for roughly another 30. An unpinned `dotnet tool install` picks up the latest resolvable version, which is what the workflow does today and should keep doing.
