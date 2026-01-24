# PSBuild Module for .NET CI/CD
# Author: ktsu.dev
# License: MIT
#
# A comprehensive PowerShell module for automating the build, test, package,
# and release process for .NET applications using Git-based versioning.
# See README.md for detailed documentation and usage examples.

# Set Strict Mode
Set-StrictMode -Version Latest

#region Environment and Configuration

function Initialize-BuildEnvironment {
    <#
    .SYNOPSIS
        Initializes the build environment with standard settings.
    .DESCRIPTION
        Sets up environment variables for .NET SDK and initializes other required build settings.
    #>
    [CmdletBinding()]
    param()

    $env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
    $env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
    $env:DOTNET_NOLOGO = 'true'

    Write-Information "Build environment initialized" -Tags "Initialize-BuildEnvironment"
}

function Get-BuildConfiguration {
    <#
    .SYNOPSIS
        Gets the build configuration based on Git status and environment.
    .DESCRIPTION
        Determines if this is a release build, checks Git status, and sets up build paths.
        Returns a configuration object containing all necessary build settings and paths.
    .PARAMETER ServerUrl
        The server URL to use for the build.
    .PARAMETER GitRef
        The Git reference (branch/tag) being built.
    .PARAMETER GitSha
        The Git commit SHA being built.
    .PARAMETER GitHubOwner
        The GitHub owner of the repository.
    .PARAMETER GitHubRepo
        The GitHub repository name.
    .PARAMETER GithubToken
        The GitHub token for API operations.
    .PARAMETER NuGetApiKey
        The NuGet API key for package publishing. Optional - if not provided or empty, NuGet publishing will be skipped.
    .PARAMETER KtsuPackageKey
        The Ktsu package key for package publishing. Optional - if not provided or empty, Ktsu publishing will be skipped.
    .PARAMETER WorkspacePath
        The path to the workspace/repository root.
    .PARAMETER ExpectedOwner
        The expected owner/organization of the official repository.
    .PARAMETER ChangelogFile
        The path to the changelog file.
    .PARAMETER LatestChangelogFile
        The path to the file containing only the latest version's changelog. Defaults to "LATEST_CHANGELOG.md".
    .PARAMETER AssetPatterns
        Array of glob patterns for release assets.
    .OUTPUTS
        PSCustomObject containing build configuration data with Success, Error, and Data properties.
    #>
    [CmdletBinding()]
    [OutputType([PSCustomObject])]
    param (
        [Parameter(Mandatory=$true)]
        [string]$ServerUrl,
        [Parameter(Mandatory=$true)]
        [string]$GitRef,
        [Parameter(Mandatory=$true)]
        [string]$GitSha,
        [Parameter(Mandatory=$true)]
        [string]$GitHubOwner,
        [Parameter(Mandatory=$true)]
        [string]$GitHubRepo,
        [Parameter(Mandatory=$true)]
        [string]$GithubToken,
        [Parameter(Mandatory=$false)]
        [AllowEmptyString()]
        [string]$NuGetApiKey = "",
        [Parameter(Mandatory=$false)]
        [AllowEmptyString()]
        [string]$KtsuPackageKey = "",
        [Parameter(Mandatory=$true)]
        [string]$WorkspacePath,
        [Parameter(Mandatory=$true)]
        [string]$ExpectedOwner,
        [Parameter(Mandatory=$true)]
        [string]$ChangelogFile,
        [Parameter(Mandatory=$false)]
        [string]$LatestChangelogFile = "LATEST_CHANGELOG.md",
        [Parameter(Mandatory=$true)]
        [string[]]$AssetPatterns
    )

    # Determine if this is an official repo (verify owner and ensure it's not a fork)
    $IS_OFFICIAL = $false
    if ($GithubToken) {
        try {
            $env:GH_TOKEN = $GithubToken
            $repoInfo = "gh repo view --json owner,nameWithOwner,isFork 2>`$null" | Invoke-ExpressionWithLogging -Tags "Get-BuildConfiguration" | ConvertFrom-Json
            if ($repoInfo) {
                # Consider it official only if it's not a fork AND belongs to the expected owner
                $IS_OFFICIAL = (-not $repoInfo.isFork) -and ($repoInfo.owner.login -eq $ExpectedOwner)
                Write-Information "Repository: $($repoInfo.nameWithOwner), Is Fork: $($repoInfo.isFork), Owner: $($repoInfo.owner.login)" -Tags "Get-BuildConfiguration"
            } else {
                Write-Information "Could not retrieve repository information. Assuming unofficial build." -Tags "Get-BuildConfiguration"
            }
        }
        catch {
            Write-Information "Failed to check repository status: $_. Assuming unofficial build." -Tags "Get-BuildConfiguration"
        }
    }

    Write-Information "Is Official: $IS_OFFICIAL" -Tags "Get-BuildConfiguration"

    # Determine if this is main branch and not tagged
    $IS_MAIN = $GitRef -eq "refs/heads/main"
    $IS_TAGGED = "(git show-ref --tags -d | Out-String).Contains(`"$GitSha`")" | Invoke-ExpressionWithLogging -Tags "Get-BuildConfiguration"
    $SHOULD_RELEASE = ($IS_MAIN -AND -NOT $IS_TAGGED -AND $IS_OFFICIAL)

    # Check for .csx files (dotnet-script)
    $csx = @(Get-ChildItem -Path $WorkspacePath -Recurse -Filter *.csx -ErrorAction SilentlyContinue)
    $USE_DOTNET_SCRIPT = $csx.Count -gt 0

    # Setup paths
    $OUTPUT_PATH = Join-Path $WorkspacePath 'output'
    $STAGING_PATH = Join-Path $WorkspacePath 'staging'

    # Setup artifact patterns
    $PACKAGE_PATTERN = Join-Path $STAGING_PATH "*.nupkg"
    $SYMBOLS_PATTERN = Join-Path $STAGING_PATH "*.snupkg"
    $APPLICATION_PATTERN = Join-Path $STAGING_PATH "*.zip"

    # Set build arguments
    $BUILD_ARGS = ""
    if ($USE_DOTNET_SCRIPT) {
        $BUILD_ARGS = "-maxCpuCount:1"
    }

    # Create configuration object with standard format
    $config = [PSCustomObject]@{
        Success = $true
        Error = ""
        Data = @{
            IsOfficial = $IS_OFFICIAL
            IsMain = $IS_MAIN
            IsTagged = $IS_TAGGED
            ShouldRelease = $SHOULD_RELEASE
            UseDotnetScript = $USE_DOTNET_SCRIPT
            OutputPath = $OUTPUT_PATH
            StagingPath = $STAGING_PATH
            PackagePattern = $PACKAGE_PATTERN
            SymbolsPattern = $SYMBOLS_PATTERN
            ApplicationPattern = $APPLICATION_PATTERN
            BuildArgs = $BUILD_ARGS
            WorkspacePath = $WorkspacePath
            DotnetVersion = $script:DOTNET_VERSION
            ServerUrl = $ServerUrl
            GitRef = $GitRef
            GitSha = $GitSha
            GitHubOwner = $GitHubOwner
            GitHubRepo = $GitHubRepo
            GithubToken = $GithubToken
            NuGetApiKey = $NuGetApiKey
            KtsuPackageKey = $KtsuPackageKey
            ExpectedOwner = $ExpectedOwner
            Version = "1.0.0-pre.0"
            ReleaseHash = $GitSha
            ChangelogFile = $ChangelogFile
            LatestChangelogFile = $LatestChangelogFile
            AssetPatterns = $AssetPatterns
        }
    }

    return $config
}

#endregion

#region Version Management

function Get-GitTags {
    <#
    .SYNOPSIS
        Gets sorted git tags from the repository.
    .DESCRIPTION
        Retrieves a list of git tags sorted by version in descending order.
        Returns a default tag if no tags exist.
    #>
    [CmdletBinding()]
    [OutputType([string[]])]
    param ()

    # Configure git versionsort to correctly handle prereleases
    $suffixes = @('-alpha', '-beta', '-rc', '-pre')
    foreach ($suffix in $suffixes) {
        "git config versionsort.suffix `"$suffix`"" | Invoke-ExpressionWithLogging -Tags "Get-GitTags" | Write-InformationStream -Tags "Get-GitTags"
    }

    Write-Information "Getting sorted tags..." -Tags "Get-GitTags"
    # Get tags
    $output = "git tag --list --sort=-v:refname" | Invoke-ExpressionWithLogging -Tags "Get-GitTags"

    # Ensure we always return an array
    if ($null -eq $output) {
        Write-Information "No tags found, returning empty array" -Tags "Get-GitTags"
        return @()
    }

    # Convert to array if it's not already
    if ($output -isnot [array]) {
        if ([string]::IsNullOrWhiteSpace($output)) {
            Write-Information "No tags found, returning empty array" -Tags "Get-GitTags"
            return @()
        }
        $output = @($output)
    }

    if ($output.Count -eq 0) {
        Write-Information "No tags found, returning empty array" -Tags "Get-GitTags"
        return @()
    }

    Write-Information "Found $($output.Count) tags" -Tags "Get-GitTags"
    return $output
}

function Get-VersionType {
    <#
    .SYNOPSIS
        Determines the type of version bump needed based on commit history and public API changes
    .DESCRIPTION
        Analyzes commit messages and code changes to determine whether the next version should be:
        - Major (1.0.0 → 2.0.0): Breaking changes, indicated by [major] tags in commits
        - Minor (1.0.0 → 1.1.0): Non-breaking public API changes (additions, modifications, removals)
        - Patch (1.0.0 → 1.0.1): Bug fixes and changes that don't modify the public API
        - Prerelease (1.0.0 → 1.0.1-pre.1): Small changes or no significant changes
        - Skip: Only [skip ci] commits or no significant changes requiring a version bump

        Version bump determination follows these rules in order:
        1. Explicit tags in commit messages: [major], [minor], [patch], [pre]
        2. Public API changes detection via regex patterns (triggers minor bump)
        3. Code changes that don't modify public API (triggers patch bump)
        4. Default to prerelease bump for minimal changes
        5. If only [skip ci] commits are found, suggest skipping the release
    .PARAMETER Range
        The git commit range to analyze (e.g., "v1.0.0...HEAD" or a specific commit range)
    .OUTPUTS
        Returns a PSCustomObject with 'Type' and 'Reason' properties explaining the version increment decision.
    #>
    [CmdletBinding()]
    [OutputType([PSCustomObject])]
    param (
        [Parameter(Mandatory=$true)]
        [string]$Range
    )

    # Initialize to the most conservative version bump
    $versionType = "prerelease"
    $reason = "No significant changes detected"

    # Bot and PR patterns to exclude
    $EXCLUDE_BOTS = '^(?!.*(\[bot\]|github|ProjectDirector|SyncFileContents)).*$'
    $EXCLUDE_PRS = '^.*(Merge pull request|Merge branch ''main''|Updated packages in|Update.*package version).*$'

    # First check for explicit version markers in commit messages
    $messages = "git log --format=format:%s `"$Range`"" | Invoke-ExpressionWithLogging -Tags "Get-VersionType"

    # Ensure messages is always an array
    if ($null -eq $messages) {
        $messages = @()
    } elseif ($messages -isnot [array]) {
        $messages = @($messages)
    }

    # Check if we have any commits at all
    if (@($messages).Count -eq 0) {
        return [PSCustomObject]@{
            Type = "skip"
            Reason = "No commits found in the specified range"
        }
    }

    # Check if all commits are skip ci commits
    $skipCiPattern = '\[skip ci\]|\[ci skip\]'
    $skipCiCommits = $messages | Where-Object { $_ -match $skipCiPattern }

    if (@($skipCiCommits).Count -eq @($messages).Count -and @($messages).Count -gt 0) {
        return [PSCustomObject]@{
            Type = "skip"
            Reason = "All commits contain [skip ci] tag, skipping release"
        }
    }

    foreach ($message in $messages) {
        if ($message.Contains('[major]')) {
            $versionType = 'major'
            $reason = "Explicit [major] tag found in commit message: $message"
            # Return immediately for major version bumps
            return [PSCustomObject]@{
                Type = $versionType
                Reason = $reason
            }
        } elseif ($message.Contains('[minor]') -and $versionType -ne 'major') {
            $versionType = 'minor'
            $reason = "Explicit [minor] tag found in commit message: $message"
        } elseif ($message.Contains('[patch]') -and $versionType -notin @('major', 'minor')) {
            $versionType = 'patch'
            $reason = "Explicit [patch] tag found in commit message: $message"
        } elseif ($message.Contains('[pre]') -and $versionType -eq 'prerelease') {
            # Keep as prerelease, but update reason
            $reason = "Explicit [pre] tag found in commit message: $message"
        }
    }

    # If no explicit version markers, check for code changes
    if ($versionType -eq "prerelease") {
        # Check for any commits that would warrant at least a patch version
        $patchCommits = "git log -n 1 --topo-order --perl-regexp --regexp-ignore-case --format=format:%H --committer=`"$EXCLUDE_BOTS`" --author=`"$EXCLUDE_BOTS`" --grep=`"$EXCLUDE_PRS`" --invert-grep `"$Range`"" | Invoke-ExpressionWithLogging -Tags "Get-VersionType"

        if ($patchCommits) {
            $versionType = "patch"
            $reason = "Found changes warranting at least a patch version"

            # Check for public API changes that would warrant a minor version

            # First, check if we can detect public API changes via git diff
            $apiChangePatterns = @(
                # C# public API patterns
                '^\+\s*(public|protected)\s+(class|interface|enum|struct|record)\s+\w+',  # Added public types
                '^\+\s*(public|protected)\s+\w+\s+\w+\s*\(',                             # Added public methods
                '^\+\s*(public|protected)\s+\w+(\s+\w+)*\s*{',                          # Added public properties
                '^\-\s*(public|protected)\s+(class|interface|enum|struct|record)\s+\w+', # Removed public types
                '^\-\s*(public|protected)\s+\w+\s+\w+\s*\(',                            # Removed public methods
                '^\-\s*(public|protected)\s+\w+(\s+\w+)*\s*{',                          # Removed public properties
                '^\+\s*public\s+const\s',                                              # Added public constants
                '^\-\s*public\s+const\s'                                               # Removed public constants
            )

            # Combine patterns for git diff
            $apiChangePattern = "(" + ($apiChangePatterns -join ")|(") + ")"

            # Search for API changes
            $apiDiffCmd = "git diff `"$Range`" -- `"*.cs`" | Select-String -Pattern `"$apiChangePattern`" -SimpleMatch"
            $apiChanges = Invoke-Expression $apiDiffCmd

            if ($apiChanges) {
                $versionType = "minor"
                $reason = "Public API changes detected (additions, removals, or modifications)"
                return [PSCustomObject]@{
                    Type = $versionType
                    Reason = $reason
                }
            }
        }
    }

    return [PSCustomObject]@{
        Type = $versionType
        Reason = $reason
    }
}

function Get-VersionInfoFromGit {
    <#
    .SYNOPSIS
        Gets comprehensive version information based on Git tags and commit analysis.
    .DESCRIPTION
        Finds the most recent version tag, analyzes commit history, and determines the next version
        following semantic versioning principles. Returns a rich object with all version components.
    .PARAMETER CommitHash
        The Git commit hash being built.
    .PARAMETER InitialVersion
        The version to use if no tags exist. Defaults to "1.0.0".
    #>
    [CmdletBinding()]
    [OutputType([PSCustomObject])]
    param (
        [Parameter(Mandatory=$true)]
        [string]$CommitHash,
        [string]$InitialVersion = "1.0.0"
    )

    Write-StepHeader "Analyzing Version Information" -Tags "Get-VersionInfoFromGit"
    Write-Information "Analyzing repository for version information..." -Tags "Get-VersionInfoFromGit"
    Write-Information "Commit hash: $CommitHash" -Tags "Get-VersionInfoFromGit"

    # Get all tags
    $tags = Get-GitTags

    # Ensure tags is always an array
    if ($null -eq $tags) {
        $tags = @()
    } elseif ($tags -isnot [array]) {
        $tags = @($tags)
    }

    Write-Information "Found $(@($tags).Count) tag(s)" -Tags "Get-VersionInfoFromGit"

    # Get the last tag and its commit
    $usingFallbackTag = $false
    $lastTag = ""

    if (@($tags).Count -eq 0) {
        $lastTag = "v$InitialVersion-pre.0"
        $usingFallbackTag = $true
        Write-Information "No tags found. Using fallback: $lastTag" -Tags "Get-VersionInfoFromGit"
    } else {
        $lastTag = $tags[0]
        Write-Information "Using last tag: $lastTag" -Tags "Get-VersionInfoFromGit"
    }

    # Extract the version without 'v' prefix
    $lastVersion = $lastTag -replace 'v', ''
    Write-Information "Last version: $lastVersion" -Tags "Get-VersionInfoFromGit"

    # Parse previous version
    $wasPrerelease = $lastVersion.Contains('-')
    $cleanVersion = $lastVersion -replace '-alpha.*$', '' -replace '-beta.*$', '' -replace '-rc.*$', '' -replace '-pre.*$', ''

    $parts = $cleanVersion -split '\.'
    $lastMajor = [int]$parts[0]
    $lastMinor = [int]$parts[1]
    $lastPatch = [int]$parts[2]
    $lastPrereleaseNum = 0

    # Extract prerelease number if applicable
    if ($wasPrerelease -and $lastVersion -match '-(?:pre|alpha|beta|rc)\.(\d+)') {
        $lastPrereleaseNum = [int]$Matches[1]
    }

    # Determine version increment type based on commit range
    Write-Information "$($script:lineEnding)Getting commits to analyze..." -Tags "Get-VersionInfoFromGit"

    # Get the first commit in repo for fallback
    $firstCommit = "git rev-list HEAD" | Invoke-ExpressionWithLogging -Tags "Get-VersionInfoFromGit"
    if ($firstCommit -is [array] -and @($firstCommit).Count -gt 0) {
        $firstCommit = $firstCommit[-1]
    }
    Write-Information "First commit: $firstCommit" -Tags "Get-VersionInfoFromGit"

    # Find the last tag's commit
    $lastTagCommit = ""
    if ($usingFallbackTag) {
        $lastTagCommit = $firstCommit
        Write-Information "Using first commit as starting point: $firstCommit" -Tags "Get-VersionInfoFromGit"
    } else {
        $lastTagCommit = "git rev-list -n 1 $lastTag" | Invoke-ExpressionWithLogging -Tags "Get-VersionInfoFromGit"
        Write-Information "Last tag commit: $lastTagCommit" -Tags "Get-VersionInfoFromGit"
    }

    # Define the commit range to analyze
    $commitRange = "$lastTagCommit..$CommitHash"
    Write-Information "Analyzing commit range: $commitRange" -Tags "Get-VersionInfoFromGit"

    # Get the increment type
    $incrementInfo = Get-VersionType -Range $commitRange
    $incrementType = $incrementInfo.Type
    $incrementReason = $incrementInfo.Reason

    # If type is "skip", return the current version without bumping
    if ($incrementType -eq "skip") {
        Write-Information "Version increment type: $incrementType" -Tags "Get-VersionInfoFromGit"
        Write-Information "Reason: $incrementReason" -Tags "Get-VersionInfoFromGit"

        # Use the same version, don't increment
        $newVersion = $lastVersion

        return [PSCustomObject]@{
            Success = $true
            Error = ""
            Data = [PSCustomObject]@{
                Version = $newVersion
                Major = $lastMajor
                Minor = $lastMinor
                Patch = $lastPatch
                IsPrerelease = $wasPrerelease
                PrereleaseNumber = $lastPrereleaseNum
                PrereleaseLabel = if ($wasPrerelease) { ($lastVersion -split '-')[1].Split('.')[0] } else { "pre" }
                LastTag = $lastTag
                LastVersion = $lastVersion
                LastVersionMajor = $lastMajor
                LastVersionMinor = $lastMinor
                LastVersionPatch = $lastPatch
                WasPrerelease = $wasPrerelease
                LastVersionPrereleaseNumber = $lastPrereleaseNum
                VersionIncrement = $incrementType
                IncrementReason = $incrementReason
                FirstCommit = $firstCommit
                LastCommit = $CommitHash
                LastTagCommit = $lastTagCommit
                UsingFallbackTag = $usingFallbackTag
                CommitRange = $commitRange
            }
        }
    }

    # Initialize new version with current values
    $newMajor = $lastMajor
    $newMinor = $lastMinor
    $newPatch = $lastPatch
    $newPrereleaseNum = 0
    $isPrerelease = $false
    $prereleaseLabel = "pre"

    Write-Information "$($script:lineEnding)Calculating new version..." -Tags "Get-VersionInfoFromGit"

    # Calculate new version based on increment type
    switch ($incrementType) {
        'major' {
            $newMajor = $lastMajor + 1
            $newMinor = 0
            $newPatch = 0
            Write-Information "Incrementing major version: $lastMajor.$lastMinor.$lastPatch -> $newMajor.0.0" -Tags "Get-VersionInfoFromGit"
        }
        'minor' {
            $newMinor = $lastMinor + 1
            $newPatch = 0
            Write-Information "Incrementing minor version: $lastMajor.$lastMinor.$lastPatch -> $lastMajor.$newMinor.0" -Tags "Get-VersionInfoFromGit"
        }
        'patch' {
            if (-not $wasPrerelease) {
                $newPatch = $lastPatch + 1
                Write-Information "Incrementing patch version: $lastMajor.$lastMinor.$lastPatch -> $lastMajor.$lastMinor.$newPatch" -Tags "Get-VersionInfoFromGit"
            } else {
                Write-Information "Converting prerelease to stable version: $lastVersion -> $lastMajor.$lastMinor.$lastPatch" -Tags "Get-VersionInfoFromGit"
            }
        }
        'prerelease' {
            if ($wasPrerelease) {
                # Bump prerelease number
                $newPrereleaseNum = $lastPrereleaseNum + 1
                $isPrerelease = $true
                Write-Information "Incrementing prerelease: $lastVersion -> $lastMajor.$lastMinor.$lastPatch-$prereleaseLabel.$newPrereleaseNum" -Tags "Get-VersionInfoFromGit"
            } else {
                # Start new prerelease series
                $newPatch = $lastPatch + 1
                $newPrereleaseNum = 1
                $isPrerelease = $true
                Write-Information "Starting new prerelease: $lastVersion -> $lastMajor.$lastMinor.$newPatch-$prereleaseLabel.1" -Tags "Get-VersionInfoFromGit"
            }
        }
    }

    # Build version string
    $newVersion = "$newMajor.$newMinor.$newPatch"
    if ($isPrerelease) {
        $newVersion += "-$prereleaseLabel.$newPrereleaseNum"
    }

    Write-Information "$($script:lineEnding)Version decision:" -Tags "Get-VersionInfoFromGit"
    Write-Information "Previous version: $lastVersion" -Tags "Get-VersionInfoFromGit"
    Write-Information "New version: $newVersion" -Tags "Get-VersionInfoFromGit"
    Write-Information "Reason: $incrementReason" -Tags "Get-VersionInfoFromGit"

    try {
        # Return comprehensive object with standard format
        return [PSCustomObject]@{
            Success = $true
            Error = ""
            Data = [PSCustomObject]@{
                Version = $newVersion
                Major = $newMajor
                Minor = $newMinor
                Patch = $newPatch
                IsPrerelease = $isPrerelease
                PrereleaseNumber = $newPrereleaseNum
                PrereleaseLabel = $prereleaseLabel
                LastTag = $lastTag
                LastVersion = $lastVersion
                LastVersionMajor = $lastMajor
                LastVersionMinor = $lastMinor
                LastVersionPatch = $lastPatch
                WasPrerelease = $wasPrerelease
                LastVersionPrereleaseNumber = $lastPrereleaseNum
                VersionIncrement = $incrementType
                IncrementReason = $incrementReason
                FirstCommit = $firstCommit
                LastCommit = $CommitHash
                LastTagCommit = $lastTagCommit
                UsingFallbackTag = $usingFallbackTag
                CommitRange = $commitRange
            }
        }
    }
    catch {
        return [PSCustomObject]@{
            Success = $false
            Error = $_.ToString()
            Data = [PSCustomObject]@{
                ErrorDetails = $_.Exception.Message
                StackTrace = $_.ScriptStackTrace
            }
            StackTrace = $_.ScriptStackTrace
        }
    }
}

function New-Version {
    <#
    .SYNOPSIS
        Creates a new version file and sets environment variables.
    .DESCRIPTION
        Generates a new version number based on git history, writes it to version files,
        and optionally sets GitHub environment variables for use in Actions.
    .PARAMETER CommitHash
        The Git commit hash being built.
    .PARAMETER OutputPath
        Optional path to write the version file to. Defaults to workspace root.
    #>
    [CmdletBinding()]
    [OutputType([string])]
    param (
        [Parameter(Mandatory=$true)]
        [string]$CommitHash,
        [string]$OutputPath = ""
    )

    # Get complete version information object
    $versionInfo = Get-VersionInfoFromGit -CommitHash $CommitHash

    # Write version file with correct line ending
    $filePath = if ($OutputPath) { Join-Path $OutputPath "VERSION.md" } else { "VERSION.md" }
    $version = $versionInfo.Data.Version.Trim()
    [System.IO.File]::WriteAllText($filePath, $version + $script:lineEnding, [System.Text.UTF8Encoding]::new($false)) | Write-InformationStream -Tags "New-Version"

    Write-Information "Previous version: $($versionInfo.Data.LastVersion), New version: $($versionInfo.Data.Version)" -Tags "New-Version"

    return $versionInfo.Data.Version
}

#endregion

#region License Management

function New-License {
    <#
    .SYNOPSIS
        Creates a license file from template.
    .DESCRIPTION
        Generates a LICENSE.md file using the template and repository information.
    .PARAMETER ServerUrl
        The GitHub server URL.
    .PARAMETER Owner
        The repository owner/organization.
    .PARAMETER Repository
        The repository name.
    .PARAMETER OutputPath
        Optional path to write the license file to. Defaults to workspace root.
    #>
    [CmdletBinding()]
    param (
        [Parameter(Mandatory=$true)]
        [string]$ServerUrl,
        [Parameter(Mandatory=$true)]
        [string]$Owner,
        [Parameter(Mandatory=$true)]
        [string]$Repository,
        [string]$OutputPath = ""
    )

    if (-not (Test-Path $script:LICENSE_TEMPLATE)) {
        throw "License template not found at: $script:LICENSE_TEMPLATE"
    }

    $year = (Get-Date).Year
    $content = Get-Content $script:LICENSE_TEMPLATE -Raw

    # Project URL
    $projectUrl = "$ServerUrl/$Repository"
    $content = $content.Replace('{PROJECT_URL}', $projectUrl)

    # Copyright line
    $copyright = "Copyright (c) 2023-$year $Owner"
    $content = $content.Replace('{COPYRIGHT}', $copyright)

    # Normalize line endings
    $content = $content.ReplaceLineEndings($script:lineEnding)

    $copyrightFilePath = if ($OutputPath) { Join-Path $OutputPath "COPYRIGHT.md" } else { "COPYRIGHT.md" }
    [System.IO.File]::WriteAllText($copyrightFilePath, $copyright + $script:lineEnding, [System.Text.UTF8Encoding]::new($false)) | Write-InformationStream -Tags "New-License"

    $filePath = if ($OutputPath) { Join-Path $OutputPath "LICENSE.md" } else { "LICENSE.md" }
    [System.IO.File]::WriteAllText($filePath, $content, [System.Text.UTF8Encoding]::new($false)) | Write-InformationStream -Tags "New-License"

    Write-Information "License file created at: $filePath" -Tags "New-License"
}

#endregion

#region Changelog Management

function ConvertTo-FourComponentVersion {
    <#
    .SYNOPSIS
        Converts a version tag to a four-component version for comparison.
    .DESCRIPTION
        Standardizes version tags to a four-component version (major.minor.patch.prerelease) for easier comparison.
    .PARAMETER VersionTag
        The version tag to convert.
    #>
    [CmdletBinding()]
    [OutputType([string])]
    param (
        [Parameter(Mandatory=$true)]
        [string]$VersionTag
    )

    $version = $VersionTag -replace 'v', ''
    $version = $version -replace '-alpha', '' -replace '-beta', '' -replace '-rc', '' -replace '-pre', ''
    $versionComponents = $version -split '\.'
    $versionMajor = [int]$versionComponents[0]
    $versionMinor = [int]$versionComponents[1]
    $versionPatch = [int]$versionComponents[2]
    $versionPrerelease = 0

    if (@($versionComponents).Count -gt 3) {
        $versionPrerelease = [int]$versionComponents[3]
    }

    return "$versionMajor.$versionMinor.$versionPatch.$versionPrerelease"
}

function Get-VersionNotes {
    <#
    .SYNOPSIS
        Generates changelog notes for a specific version range.
    .DESCRIPTION
        Creates formatted changelog entries for commits between two version tags.
    .PARAMETER Tags
        All available tags in the repository.
    .PARAMETER FromTag
        The starting tag of the range.
    .PARAMETER ToTag
        The ending tag of the range.
    .PARAMETER ToSha
        Optional specific commit SHA to use as the range end.
    #>
    [CmdletBinding()]
    [OutputType([string])]
    param (
        [Parameter(Mandatory=$true)]
        [AllowEmptyCollection()]
        [string[]]$Tags,
        [Parameter(Mandatory=$true)]
        [string]$FromTag,
        [Parameter(Mandatory=$true)]
        [string]$ToTag,
        [Parameter()]
        [string]$ToSha = ""
    )

    # Define common patterns used for filtering commits
    $EXCLUDE_BOTS = '^(?!.*(\[bot\]|github|ProjectDirector|SyncFileContents)).*$'
    $EXCLUDE_PRS = '^.*(Merge pull request|Merge branch ''main''|Updated packages in|Update.*package version).*$'

    # Convert tags to comparable versions
    $toVersion = ConvertTo-FourComponentVersion -VersionTag $ToTag
    $fromVersion = ConvertTo-FourComponentVersion -VersionTag $FromTag

    # Parse components for comparison
    $toVersionComponents = $toVersion -split '\.'
    $toVersionMajor = [int]$toVersionComponents[0]
    $toVersionMinor = [int]$toVersionComponents[1]
    $toVersionPatch = [int]$toVersionComponents[2]
    $toVersionPrerelease = [int]$toVersionComponents[3]

    $fromVersionComponents = $fromVersion -split '\.'
    $fromVersionMajor = [int]$fromVersionComponents[0]
    $fromVersionMinor = [int]$fromVersionComponents[1]
    $fromVersionPatch = [int]$fromVersionComponents[2]
    $fromVersionPrerelease = [int]$fromVersionComponents[3]

    # Calculate previous version numbers for finding the correct tag
    $fromMajorVersionNumber = $toVersionMajor - 1
    $fromMinorVersionNumber = $toVersionMinor - 1
    $fromPatchVersionNumber = $toVersionPatch - 1
    $fromPrereleaseVersionNumber = $toVersionPrerelease - 1

    # Determine version type and search tag
    $searchTag = $FromTag
    $versionType = "unknown"

    if ($toVersionPrerelease -ne 0) {
        $versionType = "prerelease"
        $searchTag = "$toVersionMajor.$toVersionMinor.$toVersionPatch.$fromPrereleaseVersionNumber"
    }
    else {
        if ($toVersionPatch -gt $fromVersionPatch) {
            $versionType = "patch"
            $searchTag = "$toVersionMajor.$toVersionMinor.$fromPatchVersionNumber.0"
        }
        if ($toVersionMinor -gt $fromVersionMinor) {
            $versionType = "minor"
            $searchTag = "$toVersionMajor.$fromMinorVersionNumber.0.0"
        }
        if ($toVersionMajor -gt $fromVersionMajor) {
            $versionType = "major"
            $searchTag = "$fromMajorVersionNumber.0.0.0"
        }
    }

    # Handle case where version is same but prerelease was dropped
    if ($toVersionMajor -eq $fromVersionMajor -and
        $toVersionMinor -eq $fromVersionMinor -and
        $toVersionPatch -eq $fromVersionPatch -and
        $toVersionPrerelease -eq 0 -and
        $fromVersionPrerelease -ne 0) {
        $versionType = "patch"
        $searchTag = "$toVersionMajor.$toVersionMinor.$fromPatchVersionNumber.0"
    }

    if ($searchTag.Contains("-")) {
        $searchTag = $FromTag
    }

    $searchVersion = ConvertTo-FourComponentVersion -VersionTag $searchTag

    if ($FromTag -ne "v0.0.0") {
        $foundSearchTag = $false
        $Tags | ForEach-Object {
            if (-not $foundSearchTag) {
                $otherTag = $_
                $otherVersion = ConvertTo-FourComponentVersion -VersionTag $otherTag
                if ($searchVersion -eq $otherVersion) {
                    $foundSearchTag = $true
                    $searchTag = $otherTag
                }
            }
        }

        if (-not $foundSearchTag) {
            $searchTag = $FromTag
        }
    }

    $rangeFrom = $searchTag
    if ($rangeFrom -eq "v0.0.0" -or $rangeFrom -eq "0.0.0.0" -or $rangeFrom -eq "1.0.0.0") {
        $rangeFrom = ""
    }

    $rangeTo = $ToSha
    if ($rangeTo -eq "") {
        $rangeTo = $ToTag
    }

    # Determine proper commit range
    $isNewestVersion = $false
    if ($ToSha -ne "") {
        # If ToSha is provided, this is likely the newest version being generated
        $isNewestVersion = $true
    }

    # Get the actual commit SHA for the from tag if it exists
    $range = ""
    $fromSha = ""
    $gitSuccess = $true

    if ($rangeFrom -ne "") {
        try {
            # Try to get the SHA for the from tag, but don't error if it doesn't exist
            $fromSha = "git rev-list -n 1 $rangeFrom 2>`$null" | Invoke-ExpressionWithLogging -ErrorAction SilentlyContinue
            if ($LASTEXITCODE -ne 0) {
                Write-Information "Warning: Could not find SHA for tag $rangeFrom. Using fallback range." -Tags "Get-VersionNotes"
                $gitSuccess = $false
                $fromSha = ""
            }

            # For the newest version with SHA provided (not yet tagged):
            if ($isNewestVersion -and $ToSha -ne "" -and $gitSuccess) {
                $range = "$fromSha..$ToSha"
            } elseif ($gitSuccess) {
                # For already tagged versions, get the SHA for the to tag
                $toShaResolved = "git rev-list -n 1 $rangeTo 2>`$null" | Invoke-ExpressionWithLogging -ErrorAction SilentlyContinue
                if ($LASTEXITCODE -ne 0) {
                    Write-Information "Warning: Could not find SHA for tag $rangeTo. Using fallback range." -Tags "Get-VersionNotes"
                    $gitSuccess = $false
                }
                else {
                    $range = "$fromSha..$toShaResolved"
                }
            }
        }
        catch {
            Write-Information "Error getting commit SHAs: $_" -Tags "Get-VersionNotes"
            $gitSuccess = $false
        }
    }

    # Handle case with no FROM tag (first version) or failed git commands
    if ($rangeFrom -eq "" -or -not $gitSuccess) {
        if ($ToSha -ne "") {
            $range = $ToSha
        } else {
            try {
                $toShaResolved = "git rev-list -n 1 $rangeTo 2>`$null" | Invoke-ExpressionWithLogging -ErrorAction SilentlyContinue
                if ($LASTEXITCODE -eq 0) {
                    $range = $toShaResolved
                } else {
                    # If we can't resolve either tag, use HEAD as fallback
                    $range = "HEAD"
                }
            }
            catch {
                Write-Information "Error resolving tag SHA: $_. Using HEAD instead." -Tags "Get-VersionNotes"
                $range = "HEAD"
            }
        }
    }

    # Debug output
    Write-Information "Processing range: $range (From: $rangeFrom, To: $rangeTo)" -Tags "Get-VersionNotes"

    # For repositories with no valid tags or no commits between tags, handle gracefully
    if ([string]::IsNullOrWhiteSpace($range) -or $range -eq ".." -or $range -match '^\s*$') {
        Write-Information "No valid commit range found. Creating a placeholder entry." -Tags "Get-VersionNotes"
        $versionType = "initial" # Mark as initial release
        $versionChangelog = "## $ToTag (initial release)$script:lineEnding$script:lineEnding"
        $versionChangelog += "Initial version.$script:lineEnding$script:lineEnding"
        return ($versionChangelog.Trim() + $script:lineEnding)
    }

    # Try with progressively more relaxed filtering to ensure we show commits
    $rawCommits = @()

    try {
        # Get full commit info with hash to ensure uniqueness
        $format = '%h|%s|%aN'

        # First try with standard filters
        $rawCommitsResult = "git log --pretty=format:`"$format`" --perl-regexp --regexp-ignore-case --grep=`"$EXCLUDE_PRS`" --invert-grep --committer=`"$EXCLUDE_BOTS`" --author=`"$EXCLUDE_BOTS`" `"$range`"" | Invoke-ExpressionWithLogging -ErrorAction SilentlyContinue

        # Safely convert to array and handle any errors
        $rawCommits = ConvertTo-ArraySafe -InputObject $rawCommitsResult

        # Additional safety check - ensure we have a valid array with Count property
        if ($null -eq $rawCommits) {
            Write-Information "rawCommits is null, creating empty array" -Tags "Get-VersionNotes"
            $rawCommits = @()
        }

        # Use @() subexpression to safely get count
        $rawCommitsCount = @($rawCommits).Count

        # If no commits found, try with just PR exclusion but no author filtering
        if ($rawCommitsCount -eq 0) {
            Write-Information "No commits found with standard filters, trying with relaxed author/committer filters..." -Tags "Get-VersionNotes"
            $rawCommitsResult = "git log --pretty=format:`"$format`" --perl-regexp --regexp-ignore-case --grep=`"$EXCLUDE_PRS`" --invert-grep `"$range`"" | Invoke-ExpressionWithLogging -ErrorAction SilentlyContinue

            # Safely convert to array and handle any errors
            $rawCommits = ConvertTo-ArraySafe -InputObject $rawCommitsResult

            # Additional safety check
            if ($null -eq $rawCommits) {
                Write-Information "rawCommits is null, creating empty array" -Tags "Get-VersionNotes"
                $rawCommits = @()
            }
        }

        # Use @() subexpression to safely get count
        $rawCommitsCount = @($rawCommits).Count

        # If still no commits, try with no filtering at all - show everything in the range
        if ($rawCommitsCount -eq 0) {
            Write-Information "Still no commits found, trying with no filters..." -Tags "Get-VersionNotes"
            $rawCommitsResult = "git log --pretty=format:`"$format`" `"$range`"" | Invoke-ExpressionWithLogging -ErrorAction SilentlyContinue

            # Safely convert to array and handle any errors
            $rawCommits = ConvertTo-ArraySafe -InputObject $rawCommitsResult

            # Additional safety check
            if ($null -eq $rawCommits) {
                Write-Information "rawCommits is null, creating empty array" -Tags "Get-VersionNotes"
                $rawCommits = @()
            }

            # Use @() subexpression to safely get count
            $rawCommitsCount = @($rawCommits).Count

            # If it's a prerelease version, include also version update commits
            if ($versionType -eq "prerelease" -and $rawCommitsCount -eq 0) {
                Write-Information "Looking for version update commits for prerelease..." -Tags "Get-VersionNotes"
                $rawCommitsResult = "git log --pretty=format:`"$format`" --grep=`"Update VERSION to`" `"$range`"" | Invoke-ExpressionWithLogging -ErrorAction SilentlyContinue

                # Safely convert to array and handle any errors
                $rawCommits = ConvertTo-ArraySafe -InputObject $rawCommitsResult

                # Additional safety check
                if ($null -eq $rawCommits) {
                    Write-Information "rawCommits is null, creating empty array" -Tags "Get-VersionNotes"
                    $rawCommits = @()
                }
            }
        }
    }
    catch {
        Write-Information "Error during git log operations: $_" -Tags "Get-VersionNotes"
        $rawCommits = @()
    }

    # Process raw commits into structured format
    $structuredCommits = @()
    foreach ($commit in $rawCommits) {
        $parts = $commit -split '\|'
        # Use @() subexpression to safely get count
        if (@($parts).Count -ge 3) {
            $structuredCommits += [PSCustomObject]@{
                Hash = $parts[0]
                Subject = $parts[1]
                Author = $parts[2]
                FormattedEntry = "$($parts[1]) ([@$($parts[2])](https://github.com/$($parts[2])))"
            }
        }
    }

    # Get unique commits based on hash (ensures unique commits)
    $uniqueCommits = ConvertTo-ArraySafe -InputObject ($structuredCommits | Sort-Object -Property Hash -Unique | ForEach-Object { $_.FormattedEntry })

    # Use @() subexpression to safely get count
    $uniqueCommitsCount = @($uniqueCommits).Count
    Write-Information "Found $uniqueCommitsCount commits for $ToTag" -Tags "Get-VersionNotes"

    # Format changelog entry
    $versionChangelog = ""
    if ($uniqueCommitsCount -gt 0) {
        $versionChangelog = "## $ToTag"
        if ($versionType -ne "unknown") {
            $versionChangelog += " ($versionType)"
        }
        $versionChangelog += "$script:lineEnding$script:lineEnding"

        if ($rangeFrom -ne "") {
            $versionChangelog += "Changes since ${rangeFrom}:$script:lineEnding$script:lineEnding"
        }

        # Only filter out version updates for non-prerelease versions
        if ($versionType -ne "prerelease") {
            $filteredCommits = $uniqueCommits | Where-Object { -not $_.Contains("Update VERSION to") -and -not $_.Contains("[skip ci]") }
        } else {
            $filteredCommits = $uniqueCommits | Where-Object { -not $_.Contains("[skip ci]") }
        }

        foreach ($commit in $filteredCommits) {
            $versionChangelog += "- $commit$script:lineEnding"
        }
        $versionChangelog += "$script:lineEnding"
    } elseif ($versionType -eq "prerelease") {
        # For prerelease versions with no detected commits, include a placeholder entry
        $versionChangelog = "## $ToTag (prerelease)$script:lineEnding$script:lineEnding"
        $versionChangelog += "Incremental prerelease update.$script:lineEnding$script:lineEnding"
    } else {
        # For all other versions with no commits, create a placeholder message
        $versionChangelog = "## $ToTag"
        if ($versionType -ne "unknown") {
            $versionChangelog += " ($versionType)"
        }
        $versionChangelog += "$script:lineEnding$script:lineEnding"

        if ($FromTag -eq "v0.0.0") {
            $versionChangelog += "Initial release.$script:lineEnding$script:lineEnding"
        } else {
            $versionChangelog += "No significant changes detected since $FromTag.$script:lineEnding$script:lineEnding"
        }
    }

    return ($versionChangelog.Trim() + $script:lineEnding)
}

function New-Changelog {
    <#
    .SYNOPSIS
        Creates a complete changelog file.
    .DESCRIPTION
        Generates a comprehensive CHANGELOG.md with entries for all versions.
    .PARAMETER Version
        The current version number being released.
    .PARAMETER CommitHash
        The Git commit hash being released.
    .PARAMETER OutputPath
        Optional path to write the changelog file to. Defaults to workspace root.
    .PARAMETER IncludeAllVersions
        Whether to include all previous versions in the changelog. Defaults to $true.
    .PARAMETER LatestChangelogFile
        Optional path to write the latest version's changelog to. Defaults to "LATEST_CHANGELOG.md".
    #>
    [CmdletBinding()]
    param (
        [Parameter(Mandatory=$true)]
        [string]$Version,
        [Parameter(Mandatory=$true)]
        [string]$CommitHash,
        [string]$OutputPath = "",
        [bool]$IncludeAllVersions = $true,
        [string]$LatestChangelogFile = "LATEST_CHANGELOG.md"
    )

    # Configure git versionsort to correctly handle prereleases
    $suffixes = @('-alpha', '-beta', '-rc', '-pre')
    foreach ($suffix in $suffixes) {
        "git config versionsort.suffix `"$suffix`"" | Invoke-ExpressionWithLogging -Tags "Get-GitTags" | Write-InformationStream -Tags "Get-GitTags"
    }

    # Get all tags sorted by version
    $tags = Get-GitTags
    $changelog = ""

    # Make sure tags is always an array
    $tags = ConvertTo-ArraySafe -InputObject $tags

    # Check if we have any tags at all
    $hasTags = $tags.Count -gt 0

    # For first release, there's no previous tag to compare against
    $previousTag = 'v0.0.0'

    # If we have tags, find the most recent one to compare against
    if ($hasTags) {
        $previousTag = $tags[0]  # Most recent tag
    }

    # Always add entry for current/new version (comparing current commit to previous tag or initial state)
    $currentTag = "v$Version"
    Write-Information "Generating changelog from $previousTag to $currentTag (commit: $CommitHash)" -Tags "New-Changelog"
    $versionNotes = Get-VersionNotes -Tags $tags -FromTag $previousTag -ToTag $currentTag -ToSha $CommitHash

    # Store the latest version's notes for later use in GitHub releases
    $latestVersionNotes = ""

    # If we have changes, add them to the changelog
    if (-not [string]::IsNullOrWhiteSpace($versionNotes)) {
        $changelog += $versionNotes
        $latestVersionNotes = $versionNotes
    } else {
        # Handle no changes detected case - add a minimal entry
        $minimalEntry = "## $currentTag$script:lineEnding$script:lineEnding"
        $minimalEntry += "Initial release or no significant changes since $previousTag.$script:lineEnding$script:lineEnding"

        $changelog += $minimalEntry
        $latestVersionNotes = $minimalEntry
    }

    # Add entries for all previous versions if requested
    if ($IncludeAllVersions -and $hasTags) {
        $tagIndex = 0

        foreach ($tag in $tags) {
            if ($tag -like "v*") {
                $previousTag = "v0.0.0"
                if ($tagIndex -lt $tags.Count - 1) {
                    $previousTag = $tags[$tagIndex + 1]
                }

                if (-not ($previousTag -like "v*")) {
                    $previousTag = "v0.0.0"
                }

                $versionNotes = Get-VersionNotes -Tags $tags -FromTag $previousTag -ToTag $tag
                $changelog += $versionNotes
            }
            $tagIndex++
        }
    }

    # Write changelog to file
    $filePath = if ($OutputPath) { Join-Path $OutputPath "CHANGELOG.md" } else { "CHANGELOG.md" }

    # Normalize line endings in changelog content
    $changelog = $changelog.ReplaceLineEndings($script:lineEnding)

    [System.IO.File]::WriteAllText($filePath, $changelog, [System.Text.UTF8Encoding]::new($false)) | Write-InformationStream -Tags "New-Changelog"

    # Write latest version's changelog to separate file for GitHub releases
    $latestPath = if ($OutputPath) { Join-Path $OutputPath $LatestChangelogFile } else { $LatestChangelogFile }
    $latestVersionNotes = $latestVersionNotes.ReplaceLineEndings($script:lineEnding)

    # Truncate release notes if they exceed NuGet's 35,000 character limit
    $maxLength = 35000
    if ($latestVersionNotes.Length -gt $maxLength) {
        Write-Information "Release notes exceed $maxLength characters ($($latestVersionNotes.Length)). Truncating to fit NuGet limit." -Tags "New-Changelog"
        $truncationMessage = "$script:lineEnding$script:lineEnding... (truncated due to NuGet length limits)"
        $targetLength = $maxLength - $truncationMessage.Length - 10  # Extra buffer for safety
        $truncatedNotes = $latestVersionNotes.Substring(0, $targetLength)
        $truncatedNotes += $truncationMessage
        $latestVersionNotes = $truncatedNotes
        Write-Information "Truncated release notes to $($latestVersionNotes.Length) characters" -Tags "New-Changelog"

        # Final safety check - ensure we never exceed the limit
        if ($latestVersionNotes.Length -gt $maxLength) {
            Write-Warning "Truncated release notes still exceed limit ($($latestVersionNotes.Length) > $maxLength). Further truncating..." -Tags "New-Changelog"
            $latestVersionNotes = $latestVersionNotes.Substring(0, $maxLength - 50) + "... (truncated)"
        }
    }

    [System.IO.File]::WriteAllText($latestPath, $latestVersionNotes, [System.Text.UTF8Encoding]::new($false)) | Write-InformationStream -Tags "New-Changelog"
    Write-Information "Latest version changelog saved to: $latestPath" -Tags "New-Changelog"

    $versionCount = if ($hasTags) { $tags.Count + 1 } else { 1 }
    Write-Information "Changelog generated with entries for $versionCount versions" -Tags "New-Changelog"
}

#endregion

#region Metadata Management

function Update-ProjectMetadata {
    <#
    .SYNOPSIS
        Updates project metadata files based on build configuration.
    .DESCRIPTION
        Generates and updates version information, license, changelog, and other metadata files for a project.
        This function centralizes all metadata generation to ensure consistency across project documentation.
    .PARAMETER BuildConfiguration
        The build configuration object containing paths, version info, and GitHub details.
        Should be obtained from Get-BuildConfiguration.
    .PARAMETER Authors
        Optional array of author names to include in the AUTHORS.md file.
    .PARAMETER CommitMessage
        Optional commit message to use when committing metadata changes.
        Defaults to "[bot][skip ci] Update Metadata".
    .EXAMPLE
        $config = Get-BuildConfiguration -GitRef "refs/heads/main" -GitSha "abc123" -GitHubOwner "myorg" -GitHubRepo "myproject"
        Update-ProjectMetadata -BuildConfiguration $config
    .EXAMPLE
        Update-ProjectMetadata -BuildConfiguration $config -Authors @("Developer 1", "Developer 2") -CommitMessage "Update project documentation"
    .OUTPUTS
        PSCustomObject with Success, Error, and Data properties.
        Data contains Version, ReleaseHash, and HasChanges information.
    #>
    [CmdletBinding()]
    [OutputType([PSCustomObject])]
    param(
        [Parameter(Mandatory = $true)]
        [PSCustomObject]$BuildConfiguration,
        [Parameter(Mandatory = $false)]
        [string[]]$Authors = @(),
        [Parameter(Mandatory = $false)]
        [string]$CommitMessage = "[bot][skip ci] Update Metadata"
    )

    try {
        Write-Information "Generating version information..." -Tags "Update-ProjectMetadata"
        $version = New-Version -CommitHash $BuildConfiguration.ReleaseHash
        Write-Information "Version: $version" -Tags "Update-ProjectMetadata"

        Write-Information "Generating license..." -Tags "Update-ProjectMetadata"
        New-License -ServerUrl $BuildConfiguration.ServerUrl -Owner $BuildConfiguration.GitHubOwner -Repository $BuildConfiguration.GitHubRepo | Write-InformationStream -Tags "Update-ProjectMetadata"

        Write-Information "Generating changelog..." -Tags "Update-ProjectMetadata"
        # Generate both full changelog and latest version changelog
        try {
            New-Changelog -Version $version -CommitHash $BuildConfiguration.ReleaseHash -LatestChangelogFile $BuildConfiguration.LatestChangelogFile | Write-InformationStream -Tags "Update-ProjectMetadata"
        }
        catch {
            $errorMessage = $_.ToString()
            Write-Information "Failed to generate complete changelog: $errorMessage" -Tags "Update-ProjectMetadata"
            Write-Information "Creating minimal changelog instead..." -Tags "Update-ProjectMetadata"

            # Create a minimal changelog
            $minimalChangelog = "## v$version$($script:lineEnding)$($script:lineEnding)"
            $minimalChangelog += "Initial release or repository with no prior history.$($script:lineEnding)$($script:lineEnding)"

            [System.IO.File]::WriteAllText("CHANGELOG.md", $minimalChangelog, [System.Text.UTF8Encoding]::new($false)) | Write-InformationStream -Tags "Update-ProjectMetadata"
            [System.IO.File]::WriteAllText($BuildConfiguration.LatestChangelogFile, $minimalChangelog, [System.Text.UTF8Encoding]::new($false)) | Write-InformationStream -Tags "Update-ProjectMetadata"
        }

        # Create AUTHORS.md if authors are provided
        if (@($Authors).Count -gt 0) {
            Write-Information "Generating authors file..." -Tags "Update-ProjectMetadata"
            $authorsContent = "# Project Authors$script:lineEnding$script:lineEnding"
            foreach ($author in $Authors) {
                $authorsContent += "* $author$script:lineEnding"
            }
            [System.IO.File]::WriteAllText("AUTHORS.md", $authorsContent, [System.Text.UTF8Encoding]::new($false)) | Write-InformationStream -Tags "Update-ProjectMetadata"
        }

        # Create AUTHORS.url
        $authorsUrl = "[InternetShortcut]$($script:lineEnding)URL=$($BuildConfiguration.ServerUrl)/$($BuildConfiguration.GitHubOwner)"
        [System.IO.File]::WriteAllText("AUTHORS.url", $authorsUrl, [System.Text.UTF8Encoding]::new($false)) | Write-InformationStream -Tags "Update-ProjectMetadata"

        # Create PROJECT_URL.url
        $projectUrl = "[InternetShortcut]$($script:lineEnding)URL=$($BuildConfiguration.ServerUrl)/$($BuildConfiguration.GitHubRepo)"
        [System.IO.File]::WriteAllText("PROJECT_URL.url", $projectUrl, [System.Text.UTF8Encoding]::new($false)) | Write-InformationStream -Tags "Update-ProjectMetadata"

        Write-Information "Adding files to git..." -Tags "Update-ProjectMetadata"
        $filesToAdd = @(
            "VERSION.md",
            "LICENSE.md",
            "AUTHORS.md",
            "CHANGELOG.md",
            "COPYRIGHT.md",
            "PROJECT_URL.url",
            "AUTHORS.url"
        )

        # Add latest changelog if it exists
        if (Test-Path $BuildConfiguration.LatestChangelogFile) {
            $filesToAdd += $BuildConfiguration.LatestChangelogFile
        }
        Write-Information "Files to add: $($filesToAdd -join ", ")" -Tags "Update-ProjectMetadata"
        "git add $filesToAdd" | Invoke-ExpressionWithLogging | Write-InformationStream -Tags "Update-ProjectMetadata"

        Write-Information "Checking for changes to commit..." -Tags "Update-ProjectMetadata"
        $postStatus = "git status --porcelain" | Invoke-ExpressionWithLogging -Tags "Update-ProjectMetadata" | Out-String
        $hasChanges = -not [string]::IsNullOrWhiteSpace($postStatus)
        $statusMessage = if ($hasChanges) { 'Changes detected' } else { 'No changes' }
        Write-Information "Git status: $statusMessage" -Tags "Update-ProjectMetadata"

        # Get the current commit hash regardless of whether we make changes
        $currentHash = "git rev-parse HEAD" | Invoke-ExpressionWithLogging
        Write-Information "Current commit hash: $currentHash" -Tags "Update-ProjectMetadata"

        if (-not [string]::IsNullOrWhiteSpace($postStatus)) {
            # Configure git user before committing
            Set-GitIdentity | Write-InformationStream -Tags "Update-ProjectMetadata"

            Write-Information "Committing changes..." -Tags "Update-ProjectMetadata"
            "git commit -m `"$CommitMessage`"" | Invoke-ExpressionWithLogging | Write-InformationStream -Tags "Update-ProjectMetadata"

            Write-Information "Pushing changes..." -Tags "Update-ProjectMetadata"
            "git push" | Invoke-ExpressionWithLogging | Write-InformationStream -Tags "Update-ProjectMetadata"

            Write-Information "Getting release hash..." -Tags "Update-ProjectMetadata"
            $releaseHash = "git rev-parse HEAD" | Invoke-ExpressionWithLogging
            Write-Information "Metadata committed as $releaseHash" -Tags "Update-ProjectMetadata"

            Write-Information "Metadata update completed successfully with changes" -Tags "Update-ProjectMetadata"
            Write-Information "Version: $version" -Tags "Update-ProjectMetadata"
            Write-Information "Release Hash: $releaseHash" -Tags "Update-ProjectMetadata"

            return [PSCustomObject]@{
                Success = $true
                Error = ""
                Data = [PSCustomObject]@{
                    Version = $version
                    ReleaseHash = $releaseHash
                    HasChanges = $true
                }
            }
        }
        else {
            Write-Information "No changes to commit" -Tags "Update-ProjectMetadata"
            Write-Information "Version: $version" -Tags "Update-ProjectMetadata"
            Write-Information "Using current commit hash: $currentHash" -Tags "Update-ProjectMetadata"

            return [PSCustomObject]@{
                Success = $true
                Error = ""
                Data = [PSCustomObject]@{
                    Version = $version
                    ReleaseHash = $currentHash
                    HasChanges = $false
                }
            }
        }
    }
    catch {
        $errorMessage = $_.ToString()
        Write-Information "Failed to update metadata: $errorMessage" -Tags "Update-ProjectMetadata"
        return [PSCustomObject]@{
            Success = $false
            Error = $errorMessage
            Data = [PSCustomObject]@{
                Version = $null
                ReleaseHash = $null
                HasChanges = $false
                StackTrace = $_.ScriptStackTrace
            }
        }
    }
}

#endregion

#region Build Operations

function Invoke-DotNetRestore {
    <#
    .SYNOPSIS
        Restores NuGet packages.
    .DESCRIPTION
        Runs dotnet restore to get all dependencies.
    #>
    [CmdletBinding()]
    param()

    Write-StepHeader "Restoring Dependencies" -Tags "Invoke-DotNetRestore"

    # Execute command and stream output directly to console
    "dotnet restore --locked-mode -logger:`"Microsoft.Build.Logging.ConsoleLogger,Microsoft.Build;Summary;ForceNoAlign;ShowTimestamp;ShowCommandLine;Verbosity=quiet`"" | Invoke-ExpressionWithLogging | Write-InformationStream -Tags "Invoke-DotNetRestore"
    Assert-LastExitCode "Restore failed"
}

function Invoke-DotNetBuild {
    <#
    .SYNOPSIS
        Builds the .NET solution.
    .DESCRIPTION
        Runs dotnet build with specified configuration.
    .PARAMETER Configuration
        The build configuration (Debug/Release).
    .PARAMETER BuildArgs
        Additional build arguments.
    #>
    [CmdletBinding()]
    param (
        [string]$Configuration = "Release",
        [string]$BuildArgs = ""
    )

    Write-StepHeader "Building Solution" -Tags "Invoke-DotNetBuild"

    try {
        # First attempt with quiet verbosity - stream output directly
        "dotnet build --configuration $Configuration -logger:`"Microsoft.Build.Logging.ConsoleLogger,Microsoft.Build;Summary;ForceNoAlign;ShowTimestamp;ShowCommandLine;Verbosity=quiet`" --no-incremental $BuildArgs --no-restore" | Invoke-ExpressionWithLogging | Write-InformationStream -Tags "Invoke-DotNetBuild"

        if ($LASTEXITCODE -ne 0) {
            Write-Information "Build failed with exit code $LASTEXITCODE. Retrying with detailed verbosity..." -Tags "Invoke-DotNetBuild"

            # Retry with more detailed verbosity - stream output directly
            "dotnet build --configuration $Configuration -logger:`"Microsoft.Build.Logging.ConsoleLogger,Microsoft.Build;Summary;ForceNoAlign;ShowTimestamp;ShowCommandLine;Verbosity=quiet`" --no-incremental $BuildArgs --no-restore" | Invoke-ExpressionWithLogging | Write-InformationStream -Tags "Invoke-DotNetBuild"

            # Still failed, show diagnostic info and throw error
            if ($LASTEXITCODE -ne 0) {
                Write-Information "Checking for common build issues:" -Tags "Invoke-DotNetBuild"

                # Check for project files
                $projectFiles = @(Get-ChildItem -Recurse -Filter *.csproj)
                Write-Information "Found $($projectFiles.Count) project files" -Tags "Invoke-DotNetBuild"

                foreach ($proj in $projectFiles) {
                    Write-Information "  - $($proj.FullName)" -Tags "Invoke-DotNetBuild"
                }

                Assert-LastExitCode "Build failed"
            }
        }
    }
    catch {
        Write-Information "Exception during build process: $_" -Tags "Invoke-DotNetBuild"
        throw
    }
}

function Invoke-DotNetTest {
    <#
    .SYNOPSIS
        Runs dotnet test with code coverage collection.
    .DESCRIPTION
        Runs dotnet test with code coverage collection.
    .PARAMETER Configuration
        The build configuration to use.
    .PARAMETER CoverageOutputPath
        The path to output code coverage results.
    #>
    [CmdletBinding()]
    param (
        [string]$Configuration = "Release",
        [string]$CoverageOutputPath = "coverage"
    )

    Write-StepHeader "Running Tests with Coverage" -Tags "Invoke-DotNetTest"

    # Check if there are any test projects in the solution
    $testProjects = @(Get-ChildItem -Recurse -Filter "*.csproj" | Where-Object {
        $_.Name -match "\.Test\.csproj$" -or
        $_.Directory.Name -match "\.Test$" -or
        $_.Directory.Name -eq "Test" -or
        (Select-String -Path $_.FullName -Pattern "<IsTestProject>true</IsTestProject>" -Quiet)
    })

    if ($testProjects.Count -eq 0) {
        Write-Information "No test projects found in solution. Skipping test execution." -Tags "Invoke-DotNetTest"
        return
    }

    Write-Information "Found $($testProjects.Count) test project(s)" -Tags "Invoke-DotNetTest"

    # Ensure the TestResults directory exists
    $testResultsPath = Join-Path $CoverageOutputPath "TestResults"
    New-Item -Path $testResultsPath -ItemType Directory -Force | Out-Null

    # Run tests with both coverage collection and TRX logging for SonarQube
    "dotnet test --configuration $Configuration --coverage --coverage-output-format xml --coverage-output `"coverage.xml`" --results-directory `"$testResultsPath`" --report-trx --report-trx-filename TestResults.trx" | Invoke-ExpressionWithLogging | Write-InformationStream -Tags "Invoke-DotNetTest"
    Assert-LastExitCode "Tests failed"

    # Find and copy coverage file to expected location for SonarQube
    $coverageFiles = @(Get-ChildItem -Path . -Recurse -Filter "coverage.xml" -ErrorAction SilentlyContinue)
    if ($coverageFiles.Count -gt 0) {
        $latestCoverageFile = $coverageFiles | Sort-Object LastWriteTime -Descending | Select-Object -First 1
        $targetCoverageFile = Join-Path $CoverageOutputPath "coverage.xml"
        Copy-Item -Path $latestCoverageFile.FullName -Destination $targetCoverageFile -Force
        Write-Information "Coverage file copied to: $targetCoverageFile" -Tags "Invoke-DotNetTest"
    } else {
        Write-Information "Warning: No coverage file found" -Tags "Invoke-DotNetTest"
    }
}

function Invoke-DotNetPack {
    <#
    .SYNOPSIS
        Creates NuGet packages.
    .DESCRIPTION
        Runs dotnet pack to create NuGet packages.
    .PARAMETER Configuration
        The build configuration (Debug/Release).
    .PARAMETER OutputPath
        The path to output packages to.
    .PARAMETER Project
        Optional specific project to package. If not provided, all projects are packaged.
    .PARAMETER LatestChangelogFile
        Optional path to the latest changelog file to use for PackageReleaseNotesFile. Defaults to "LATEST_CHANGELOG.md".
    #>
    [CmdletBinding()]
    param (
        [string]$Configuration = "Release",
        [Parameter(Mandatory=$true)]
        [string]$OutputPath,
        [string]$Project = "",
        [string]$LatestChangelogFile = "LATEST_CHANGELOG.md"
    )

    Write-StepHeader "Packaging Libraries" -Tags "Invoke-DotNetPack"

    # Ensure output directory exists
    New-Item -Path $OutputPath -ItemType Directory -Force | Write-InformationStream -Tags "Invoke-DotNetPack"

    # Check if any projects exist
    $projectFiles = @(Get-ChildItem -Recurse -Filter *.csproj -ErrorAction SilentlyContinue)
    if ($projectFiles.Count -eq 0) {
        Write-Information "No .NET library projects found to package" -Tags "Invoke-DotNetPack"
        return
    }

    try {
        # Override PackageReleaseNotes to use LATEST_CHANGELOG.md instead of full CHANGELOG.md
        # Use PackageReleaseNotesFile property to avoid command line length limits and escaping issues
        $releaseNotesProperty = ""

        if (Test-Path $LatestChangelogFile) {
            # Get absolute path to the changelog file for MSBuild
            $absoluteChangelogPath = (Resolve-Path $LatestChangelogFile).Path
            Write-Information "Using release notes from file: $absoluteChangelogPath" -Tags "Invoke-DotNetPack"

            # Use PackageReleaseNotesFile property instead of PackageReleaseNotes to avoid command line issues
            $releaseNotesProperty = "-p:PackageReleaseNotesFile=`"$absoluteChangelogPath`""
            Write-Information "Overriding PackageReleaseNotesFile with latest changelog file path" -Tags "Invoke-DotNetPack"
        } else {
            Write-Information "No latest changelog found, SDK will use full CHANGELOG.md (automatically truncated if needed)" -Tags "Invoke-DotNetPack"
        }

        # Build either a specific project or all projects
        if ([string]::IsNullOrWhiteSpace($Project)) {
            Write-Information "Packaging all projects in solution..." -Tags "Invoke-DotNetPack"
            "dotnet pack --configuration $Configuration -logger:`"Microsoft.Build.Logging.ConsoleLogger,Microsoft.Build;Summary;ForceNoAlign;ShowTimestamp;ShowCommandLine;Verbosity=quiet`" --no-build --output $OutputPath $releaseNotesProperty" | Invoke-ExpressionWithLogging | Write-InformationStream -Tags "Invoke-DotNetPack"
        } else {
            Write-Information "Packaging project: $Project" -Tags "Invoke-DotNetPack"
            "dotnet pack $Project --configuration $Configuration -logger:`"Microsoft.Build.Logging.ConsoleLogger,Microsoft.Build;Summary;ForceNoAlign;ShowTimestamp;ShowCommandLine;Verbosity=quiet`" --no-build --output $OutputPath $releaseNotesProperty" | Invoke-ExpressionWithLogging | Write-InformationStream -Tags "Invoke-DotNetPack"
        }

        if ($LASTEXITCODE -ne 0) {
            # Get more details about what might have failed
            Write-Information "Packaging failed with exit code $LASTEXITCODE, trying again with quiet verbosity..." -Tags "Invoke-DotNetPack"
            "dotnet pack --configuration $Configuration -logger:`"Microsoft.Build.Logging.ConsoleLogger,Microsoft.Build;Summary;ForceNoAlign;ShowTimestamp;ShowCommandLine;Verbosity=quiet`" --no-build --output $OutputPath $releaseNotesProperty" | Invoke-ExpressionWithLogging | Write-InformationStream -Tags "Invoke-DotNetPack"

            throw "Library packaging failed with exit code $LASTEXITCODE"
        }

        # Report on created packages
        $packages = @(Get-ChildItem -Path $OutputPath -Filter *.nupkg -ErrorAction SilentlyContinue)
        if ($packages.Count -gt 0) {
            Write-Information "Created $($packages.Count) packages in $OutputPath" -Tags "Invoke-DotNetPack"
            foreach ($package in $packages) {
                Write-Information "  - $($package.Name)" -Tags "Invoke-DotNetPack"
            }
        } else {
            Write-Information "No packages were created (projects may not be configured for packaging)" -Tags "Invoke-DotNetPack"
        }
    }
    catch {
        $originalException = $_.Exception
        Write-Information "Package creation failed: $originalException" -Tags "Invoke-DotNetPack"
        throw "Library packaging failed: $originalException"
    }
}

function Invoke-DotNetPublish {
    <#
    .SYNOPSIS
        Publishes .NET applications and creates winget-compatible packages.
    .DESCRIPTION
        Runs dotnet publish and creates zip archives for applications.
        Also creates winget-compatible packages for multiple architectures if console applications are found.
        Uses the build configuration to determine output paths and version information.
    .PARAMETER Configuration
        The build configuration (Debug/Release). Defaults to "Release".
    .PARAMETER BuildConfiguration
        The build configuration object containing output paths, version, and other settings.
        This object should be obtained from Get-BuildConfiguration.
    .OUTPUTS
        None. Creates published applications, zip archives, and winget packages in the specified output paths.
    #>
    [CmdletBinding()]
    param (
        [string]$Configuration = "Release",
        [Parameter(Mandatory=$true)]
        [PSCustomObject]$BuildConfiguration
    )

    Write-StepHeader "Publishing Applications" -Tags "Invoke-DotNetPublish"

    # Find all projects
    $projectFiles = @(Get-ChildItem -Recurse -Filter *.csproj -ErrorAction SilentlyContinue)
    if ($projectFiles.Count -eq 0) {
        Write-Information "No .NET application projects found to publish" -Tags "Invoke-DotNetPublish"
        return
    }

    # Clean output directory if it exists
    if (Test-Path $BuildConfiguration.OutputPath) {
        Remove-Item -Recurse -Force $BuildConfiguration.OutputPath | Write-InformationStream -Tags "Invoke-DotNetPublish"
    }

    # Ensure staging directory exists
    New-Item -Path $BuildConfiguration.StagingPath -ItemType Directory -Force | Write-InformationStream -Tags "Invoke-DotNetPublish"

    $publishedCount = 0
    $version = $BuildConfiguration.Version

    # Define target architectures for comprehensive publishing across all platforms
    $architectures = @(
        # Windows
        "win-x64", "win-x86", "win-arm64",
        # Linux
        "linux-x64", "linux-arm64",
        # macOS
        "osx-x64", "osx-arm64"
    )

    foreach ($csproj in $projectFiles) {
        $projName = [System.IO.Path]::GetFileNameWithoutExtension($csproj)
        Write-Information "Publishing $projName..." -Tags "Invoke-DotNetPublish"

        foreach ($arch in $architectures) {
            $outDir = Join-Path $BuildConfiguration.OutputPath "$projName-$arch"

            # Create output directory
            New-Item -Path $outDir -ItemType Directory -Force | Write-InformationStream -Tags "Invoke-DotNetPublish"

            # Publish application with optimized settings for both general use and winget compatibility
            "dotnet publish `"$csproj`" --configuration $Configuration --runtime $arch --self-contained true --output `"$outDir`" -p:PublishSingleFile=true -p:PublishTrimmed=false -p:EnableCompressionInSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=none -p:DebugSymbols=false -logger:`"Microsoft.Build.Logging.ConsoleLogger,Microsoft.Build;Summary;ForceNoAlign;ShowTimestamp;ShowCommandLine;Verbosity=quiet`"" | Invoke-ExpressionWithLogging | Write-InformationStream -Tags "Invoke-DotNetPublish"

            if ($LASTEXITCODE -eq 0) {
                # Create general application zip archive for all platforms
                $stageFile = Join-Path $BuildConfiguration.StagingPath "$projName-$version-$arch.zip"
                Compress-Archive -Path "$outDir/*" -DestinationPath $stageFile -Force | Write-InformationStream -Tags "Invoke-DotNetPublish"

                $publishedCount++
                Write-Information "Successfully published $projName for $arch" -Tags "Invoke-DotNetPublish"
            } else {
                Write-Information "Failed to publish $projName for $arch" -Tags "Invoke-DotNetPublish"
                continue
            }
        }
    }

    # Generate SHA256 hashes for all published packages
    $allPackages = @(Get-ChildItem -Path $BuildConfiguration.StagingPath -Filter "*.zip" -ErrorAction SilentlyContinue)

    if ($allPackages.Count -gt 0) {
        Write-Information "Generating SHA256 hashes for all published packages..." -Tags "Invoke-DotNetPublish"

        foreach ($package in $allPackages) {
            # Calculate and store SHA256 hash
            $hash = Get-FileHash -Path $package.FullName -Algorithm SHA256
            Write-Information "SHA256 for $($package.Name): $($hash.Hash)" -Tags "Invoke-DotNetPublish"

            # Store hash for integrity verification and distribution use
            "$($package.Name)=$($hash.Hash)" | Out-File -FilePath (Join-Path $BuildConfiguration.StagingPath "hashes.txt") -Append -Encoding UTF8
        }
    }

    if ($publishedCount -gt 0) {
        Write-Information "Published $publishedCount application packages across all platforms and architectures" -Tags "Invoke-DotNetPublish"

        # Report hash generation results
        if ($allPackages.Count -gt 0) {
            Write-Information "Generated SHA256 hashes for $($allPackages.Count) published packages" -Tags "Invoke-DotNetPublish"
        }
    } else {
        Write-Information "No applications were published (projects may not be configured as executables)" -Tags "Invoke-DotNetPublish"
    }
}

#endregion

#region Publishing and Release

function Invoke-NuGetPublish {
    <#
    .SYNOPSIS
        Publishes NuGet packages.
    .DESCRIPTION
        Publishes packages to GitHub Packages and NuGet.org.
        Uses the build configuration to determine package paths and authentication details.
    .PARAMETER BuildConfiguration
        The build configuration object containing package patterns, GitHub token, and NuGet API key.
        This object should be obtained from Get-BuildConfiguration.
    .OUTPUTS
        None. Publishes packages to the configured package repositories.
    #>
    [CmdletBinding()]
    param (
        [Parameter(Mandatory=$true)]
        [PSCustomObject]$BuildConfiguration
    )

    # Check if there are any packages to publish
    $packages = @(Get-Item -Path $BuildConfiguration.PackagePattern -ErrorAction SilentlyContinue)
    if ($packages.Count -eq 0) {
        Write-Information "No packages found to publish" -Tags "Invoke-NuGetPublish"
        return
    }

    Write-Information "Found $($packages.Count) package(s) to publish" -Tags "Invoke-NuGetPublish"

    Write-StepHeader "Publishing to GitHub Packages" -Tags "Invoke-NuGetPublish"

    # Execute the command and stream output
    "dotnet nuget push `"$($BuildConfiguration.PackagePattern)`" --api-key `"$($BuildConfiguration.GithubToken)`" --source `"https://nuget.pkg.github.com/$($BuildConfiguration.GithubOwner)/index.json`" --skip-duplicate" | Invoke-ExpressionWithLogging | Write-InformationStream -Tags "Invoke-NuGetPublish"
    Assert-LastExitCode "GitHub package publish failed"

    # Only publish to NuGet.org if API key is provided
    if (-not [string]::IsNullOrWhiteSpace($BuildConfiguration.NuGetApiKey)) {
        Write-StepHeader "Publishing to NuGet.org" -Tags "Invoke-NuGetPublish"

        # Execute the command and stream output
        "dotnet nuget push `"$($BuildConfiguration.PackagePattern)`" --api-key `"$($BuildConfiguration.NuGetApiKey)`" --source `"https://api.nuget.org/v3/index.json`" --skip-duplicate" | Invoke-ExpressionWithLogging | Write-InformationStream -Tags "Invoke-NuGetPublish"
        Assert-LastExitCode "NuGet.org package publish failed"
    } else {
        Write-Information "Skipping NuGet.org publishing - no API key provided" -Tags "Invoke-NuGetPublish"
    }

    # Only publish to Ktsu.dev if API key is provided
    if (-not [string]::IsNullOrWhiteSpace($BuildConfiguration.KtsuPackageKey)) {
        Write-StepHeader "Publishing to packages.ktsu.dev" -Tags "Invoke-NuGetPublish"

        # Execute the command and stream output
        "dotnet nuget push `"$($BuildConfiguration.PackagePattern)`" --api-key `"$($BuildConfiguration.KtsuPackageKey)`" --source `"https://packages.ktsu.dev/v3/index.json`" --skip-duplicate" | Invoke-ExpressionWithLogging | Write-InformationStream -Tags "Invoke-NuGetPublish"
        Assert-LastExitCode "packages.ktsu.dev package publish failed"
    } else {
        Write-Information "Skipping packages.ktsu.dev publishing - no API key provided" -Tags "Invoke-NuGetPublish"
    }
}

function New-GitHubRelease {
    <#
    .SYNOPSIS
        Creates a new GitHub release.
    .DESCRIPTION
        Creates a new GitHub release with the specified version, creates and pushes a git tag,
        and uploads release assets. Uses the GitHub CLI (gh) for release creation.
    .PARAMETER BuildConfiguration
        The build configuration object containing version, commit hash, GitHub token, and asset patterns.
        This object should be obtained from Get-BuildConfiguration.
    .OUTPUTS
        None. Creates a GitHub release and uploads specified assets.
    #>
    [CmdletBinding()]
    param (
        [Parameter(Mandatory=$true)]
        [PSCustomObject]$BuildConfiguration
    )

    # Set GitHub token for CLI
    $env:GH_TOKEN = $BuildConfiguration.GithubToken

    # Configure git user
    Set-GitIdentity | Write-InformationStream -Tags "New-GitHubRelease"

    # Create and push the tag first
    Write-Information "Creating and pushing tag v$($BuildConfiguration.Version)..." -Tags "New-GitHubRelease"
    "git tag -a `"v$($BuildConfiguration.Version)`" `"$($BuildConfiguration.ReleaseHash)`" -m `"Release v$($BuildConfiguration.Version)`"" | Invoke-ExpressionWithLogging | Write-InformationStream -Tags "New-GitHubRelease"
    Assert-LastExitCode "Failed to create git tag"

    "git push origin `"v$($BuildConfiguration.Version)`"" | Invoke-ExpressionWithLogging | Write-InformationStream -Tags "New-GitHubRelease"
    Assert-LastExitCode "Failed to push git tag"

    # Collect all assets
    $assets = @()
    foreach ($pattern in $BuildConfiguration.AssetPatterns) {
        $matched = Get-Item -Path $pattern -ErrorAction SilentlyContinue
        if ($matched) {
            $assets += $matched.FullName
        }
    }

    # Create release
    Write-StepHeader "Creating GitHub Release v$($BuildConfiguration.Version)" -Tags "New-GitHubRelease"

    $releaseArgs = @(
        "release",
        "create",
        "v$($BuildConfiguration.Version)"
    )

    # Add target commit
    $releaseArgs += "--target"
    $releaseArgs += $BuildConfiguration.ReleaseHash.ToString()

    # Add notes generation
    $releaseArgs += "--generate-notes"

    # First check for latest changelog file (preferred for releases)
    $latestChangelogPath = "LATEST_CHANGELOG.md"
    if (Test-Path $latestChangelogPath) {
        Write-Information "Using latest version changelog from $latestChangelogPath" -Tags "New-GitHubRelease"
        $releaseArgs += "--notes-file"
        $releaseArgs += $latestChangelogPath
    }
    # Fall back to full changelog if specified in config and latest not found
    elseif (Test-Path $BuildConfiguration.ChangelogFile) {
        Write-Information "Using full changelog from $($BuildConfiguration.ChangelogFile)" -Tags "New-GitHubRelease"
        $releaseArgs += "--notes-file"
        $releaseArgs += $BuildConfiguration.ChangelogFile
    }

    # Add assets as positional arguments
    $releaseArgs += $assets

    # Join the arguments into a single string
    $releaseArgs = $releaseArgs -join ' '

    "gh $releaseArgs" | Invoke-ExpressionWithLogging | Write-InformationStream -Tags "New-GitHubRelease"
    Assert-LastExitCode "Failed to create GitHub release"
}

#endregion

#region Utility Functions

function Assert-LastExitCode {
    <#
    .SYNOPSIS
        Verifies that the last command executed successfully.
    .DESCRIPTION
        Throws an exception if the last command execution resulted in a non-zero exit code.
        This function is used internally to ensure each step completes successfully.
    .PARAMETER Message
        The error message to display if the exit code check fails.
    .PARAMETER Command
        Optional. The command that was executed, for better error reporting.
    .EXAMPLE
        dotnet build
        Assert-LastExitCode "The build process failed" -Command "dotnet build"
    .NOTES
        Author: ktsu.dev
    #>
    [CmdletBinding()]
    param (
        [string]$Message = "Command failed",
        [string]$Command = ""
    )

    if ($LASTEXITCODE -ne 0) {
        $errorDetails = "Exit code: $LASTEXITCODE"
        if (-not [string]::IsNullOrWhiteSpace($Command)) {
            $errorDetails += " | Command: $Command"
        }

        $fullMessage = "$Message$script:lineEnding$errorDetails"
        Write-Information $fullMessage -Tags "Assert-LastExitCode"
        throw $fullMessage
    }
}

function Write-StepHeader {
    <#
    .SYNOPSIS
        Writes a formatted step header to the console.
    .DESCRIPTION
        Creates a visually distinct header for build steps in the console output.
        Used to improve readability of the build process logs.
    .PARAMETER Message
        The header message to display.
    .EXAMPLE
        Write-StepHeader "Restoring Packages"
    .NOTES
        Author: ktsu.dev
    #>
    [CmdletBinding()]
    param (
        [Parameter(Mandatory=$true)]
        [string]$Message,
        [Parameter()]
        [AllowEmptyCollection()]
        [string[]]$Tags = @("Write-StepHeader")
    )
    Write-Information "$($script:lineEnding)=== $Message ===$($script:lineEnding)" -Tags $Tags
}

function Test-AnyFiles {
    <#
    .SYNOPSIS
        Tests if any files match the specified pattern.
    .DESCRIPTION
        Tests if any files exist that match the given glob pattern. This is useful for
        determining if certain file types (like packages) exist before attempting operations
        on them.
    .PARAMETER Pattern
        The glob pattern to check for matching files.
    .EXAMPLE
        if (Test-AnyFiles -Pattern "*.nupkg") {
            Write-Host "NuGet packages found!"
        }
    .NOTES
        Author: ktsu.dev
    #>
    [CmdletBinding()]
    [OutputType([bool])]
    param (
        [Parameter(Mandatory=$true)]
        [string]$Pattern
    )

    # Use array subexpression to ensure consistent collection handling
    $matchingFiles = @(Get-Item -Path $Pattern -ErrorAction SilentlyContinue)
    return $matchingFiles.Count -gt 0
}

function Write-InformationStream {
    <#
    .SYNOPSIS
        Streams output to the console.
    .DESCRIPTION
        Streams output to the console.
    .PARAMETER Object
        The object to write to the console.
    .EXAMPLE
        & git status | Write-InformationStream
    .NOTES
        Author: ktsu.dev
    #>
    [CmdletBinding()]
    param (
        [Parameter(ValueFromPipeline=$true, ParameterSetName="Object")]
        [object]$Object,
        [Parameter()]
        [AllowEmptyCollection()]
        [string[]]$Tags = @("Write-InformationStream")
    )

    process {
        # Use array subexpression to ensure consistent collection handling
        $Object | ForEach-Object {
            Write-Information $_ -Tags $Tags
        }
    }
}

function Invoke-ExpressionWithLogging {
    <#
    .SYNOPSIS
        Invokes an expression and logs the result to the console.
    .DESCRIPTION
        Invokes an expression and logs the result to the console.
    .PARAMETER ScriptBlock
        The script block to execute.
    .PARAMETER Command
        A string command to execute, which will be converted to a script block.
    .PARAMETER Tags
        Optional tags to include in the logging output for filtering and organization.
    .OUTPUTS
        The result of the expression.
    .NOTES
        Author: ktsu.dev
        This function is useful for debugging expressions that are not returning the expected results.
    #>
    [CmdletBinding()]
    param (
        [Parameter(ValueFromPipeline=$true, ParameterSetName="ScriptBlock")]
        [scriptblock]$ScriptBlock,

        [Parameter(ValueFromPipeline=$true, ParameterSetName="Command")]
        [string]$Command,

        [Parameter()]
        [AllowEmptyCollection()]
        [string[]]$Tags = @("Invoke-ExpressionWithLogging")
    )

    process {
        # Convert command string to scriptblock if needed
        if ($PSCmdlet.ParameterSetName -eq "Command" -and -not [string]::IsNullOrWhiteSpace($Command)) {
            Write-Information "Executing command: $Command" -Tags $Tags
            $ScriptBlock = [scriptblock]::Create($Command)
        }
        else {
            Write-Information "Executing script block: $ScriptBlock" -Tags $Tags
        }

        if ($ScriptBlock) {
            # Execute the expression and return its result
            & $ScriptBlock | ForEach-Object {
                Write-Output $_
            }
        }
    }
}

function Get-GitLineEnding {
    <#
    .SYNOPSIS
        Gets the correct line ending based on git config.
    .DESCRIPTION
        Determines whether to use LF or CRLF based on the git core.autocrlf and core.eol settings.
        Falls back to system default line ending if no git settings are found.
    .OUTPUTS
        String. Returns either "`n" for LF or "`r`n" for CRLF line endings.
    .NOTES
        The function checks git settings in the following order:
        1. core.eol setting (if set to 'lf' or 'crlf')
        2. core.autocrlf setting ('true', 'input', or 'false')
        3. System default line ending
    #>
    [CmdletBinding()]
    [OutputType([string])]
    param()

    $autocrlf = "git config --get core.autocrlf" | Invoke-ExpressionWithLogging
    $eol = "git config --get core.eol" | Invoke-ExpressionWithLogging

    # If core.eol is set, use that
    if ($LASTEXITCODE -eq 0 -and $eol -in @('lf', 'crlf')) {
        return if ($eol -eq 'lf') { "`n" } else { "`r`n" }
    }

    # Otherwise use autocrlf setting
    if ($LASTEXITCODE -eq 0) {
        switch ($autocrlf.ToLower()) {
            'true' { return "`n" }  # Git will convert to CRLF on checkout
            'input' { return "`n" } # Always use LF
            'false' {
                # Use OS default
                return [System.Environment]::NewLine
            }
            default {
                # Default to OS line ending if setting is not recognized
                return [System.Environment]::NewLine
            }
        }
    }

    # If git config fails or no setting found, use OS default
    return [System.Environment]::NewLine
}

function Set-GitIdentity {
    <#
    .SYNOPSIS
        Configures git user identity for automated operations.
    .DESCRIPTION
        Sets up git user name and email globally for GitHub Actions or other automated processes.
    #>
    [CmdletBinding()]
    param()

    Write-Information "Configuring git user for GitHub Actions..." -Tags "Set-GitIdentity"
    "git config --global user.name `"Github Actions`"" | Invoke-ExpressionWithLogging | Write-InformationStream -Tags "Set-GitIdentity"
    Assert-LastExitCode "Failed to configure git user name"
    "git config --global user.email `"actions@users.noreply.github.com`"" | Invoke-ExpressionWithLogging | Write-InformationStream -Tags "Set-GitIdentity"
    Assert-LastExitCode "Failed to configure git user email"
}

function ConvertTo-ArraySafe {
    <#
    .SYNOPSIS
        Safely converts an object to an array, even if it's already an array, a single item, or null.
    .DESCRIPTION
        Ensures that the returned object is always an array, handling PowerShell's behavior
        where single item arrays are automatically unwrapped. Also handles error objects and other edge cases.
    .PARAMETER InputObject
        The object to convert to an array.
    .OUTPUTS
        Returns an array, even if the input is null or a single item.
    #>
    [CmdletBinding()]
    [OutputType([object[]])]
    param (
        [Parameter(ValueFromPipeline=$true)]
        [AllowNull()]
        [object]$InputObject
    )

    # Handle null or empty input
    if ($null -eq $InputObject -or [string]::IsNullOrEmpty($InputObject)) {
        return ,[object[]]@()
    }

    # Handle error objects - return empty array for safety
    if ($InputObject -is [System.Management.Automation.ErrorRecord]) {
        Write-Information "ConvertTo-ArraySafe: Received error object, returning empty array" -Tags "ConvertTo-ArraySafe"
        return ,[object[]]@()
    }

    # Handle empty strings
    if ($InputObject -is [string] -and [string]::IsNullOrWhiteSpace($InputObject)) {
        return ,[object[]]@()
    }

    try {
        # Always force array context using the comma operator and explicit array subexpression
        if ($InputObject -is [array]) {
            # Ensure we return a proper array even if it's a single-item array
            return ,[object[]]@($InputObject)
        }
        elseif ($InputObject -is [string] -and $InputObject.Contains("`n")) {
            # Handle multi-line strings by splitting them
            $lines = $InputObject -split "`n" | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
            return ,[object[]]@($lines)
        }
        else {
            # Single item, make it an array using explicit array operators
            return ,[object[]]@($InputObject)
        }
    }
    catch {
        Write-Information "ConvertTo-ArraySafe: Error converting object to array: $_" -Tags "ConvertTo-ArraySafe"
        return ,[object[]]@()
    }
}

#endregion

#region High-Level Workflows

function Invoke-BuildWorkflow {
    <#
    .SYNOPSIS
        Executes the main build workflow.
    .DESCRIPTION
        Runs the complete build, test, and package process.
    .PARAMETER Configuration
        The build configuration (Debug/Release).
    .PARAMETER BuildArgs
        Additional build arguments.
    .PARAMETER BuildConfiguration
        The build configuration object from Get-BuildConfiguration.
    #>
    [CmdletBinding()]
    [OutputType([PSCustomObject])]
    param (
        [string]$Configuration = "Release",
        [string]$BuildArgs = "",
        [Parameter(Mandatory=$true)]
        [PSCustomObject]$BuildConfiguration
    )

    try {
        # Setup
        Initialize-BuildEnvironment | Write-InformationStream -Tags "Invoke-BuildWorkflow"

        # Install dotnet-script if needed
        if ($BuildConfiguration.UseDotnetScript) {
            Write-StepHeader "Installing dotnet-script" -Tags "Invoke-DotnetScript"
            "dotnet tool install -g dotnet-script" | Invoke-ExpressionWithLogging | Write-InformationStream -Tags "Invoke-DotnetScript"
            Assert-LastExitCode "Failed to install dotnet-script"
        }

        # Build and Test
        Invoke-DotNetRestore | Write-InformationStream -Tags "Invoke-BuildWorkflow"
        Invoke-DotNetBuild -Configuration $Configuration -BuildArgs $BuildArgs | Write-InformationStream -Tags "Invoke-BuildWorkflow"
        Invoke-DotNetTest -Configuration $Configuration -CoverageOutputPath "coverage" | Write-InformationStream -Tags "Invoke-BuildWorkflow"

        return [PSCustomObject]@{
            Success = $true
            Error = ""
            Data = [PSCustomObject]@{
                Configuration = $Configuration
                BuildArgs = $BuildArgs
            }
        }
    }
    catch {
        Write-Information "Build workflow failed: $_" -Tags "Invoke-BuildWorkflow"
        return [PSCustomObject]@{
            Success = $false
            Error = $_.ToString()
            Data = [PSCustomObject]@{}
            StackTrace = $_.ScriptStackTrace
        }
    }
}

function Invoke-ReleaseWorkflow {
    <#
    .SYNOPSIS
        Executes the release workflow.
    .DESCRIPTION
        Generates metadata, packages, and creates a release.
    .PARAMETER Configuration
        The build configuration (Debug/Release). Defaults to "Release".
    .PARAMETER BuildConfiguration
        The build configuration object from Get-BuildConfiguration.
    .OUTPUTS
        PSCustomObject with Success, Error, and Data properties.
    #>
    [CmdletBinding()]
    [OutputType([PSCustomObject])]
    param (
        [string]$Configuration = "Release",
        [Parameter(Mandatory=$true)]
        [PSCustomObject]$BuildConfiguration
    )

    try {
        Write-StepHeader "Starting Release Process" -Tags "Invoke-ReleaseWorkflow"

        # Package and publish if not skipped
        $packagePaths = @()

        # Create NuGet packages
        try {
            Write-StepHeader "Packaging Libraries" -Tags "Invoke-DotNetPack"
            Invoke-DotNetPack -Configuration $Configuration -OutputPath $BuildConfiguration.StagingPath -LatestChangelogFile $BuildConfiguration.LatestChangelogFile | Write-InformationStream -Tags "Invoke-DotNetPack"

            # Add package paths if they exist
            if (Test-Path $BuildConfiguration.PackagePattern) {
                $packagePaths += $BuildConfiguration.PackagePattern
            }
            if (Test-Path $BuildConfiguration.SymbolsPattern) {
                $packagePaths += $BuildConfiguration.SymbolsPattern
            }
        }
        catch {
            Write-Information "Library packaging failed: $_" -Tags "Invoke-DotNetPack"
            Write-Information "Continuing with release process without NuGet packages." -Tags "Invoke-DotNetPack"
        }

        # Create application packages
        try {
            Invoke-DotNetPublish -Configuration $Configuration -BuildConfiguration $BuildConfiguration | Write-InformationStream -Tags "Invoke-DotNetPublish"

            # Add application paths if they exist
            if (Test-Path $BuildConfiguration.ApplicationPattern) {
                $packagePaths += $BuildConfiguration.ApplicationPattern
            }

            # Note: hashes.txt is now stored in staging directory alongside packages
        }
        catch {
            Write-Information "Application publishing failed: $_" -Tags "Invoke-DotNetPublish"
            Write-Information "Continuing with release process without application packages." -Tags "Invoke-DotNetPublish"
        }

        # Publish packages if we have any and NuGet key is provided AND this is a release build
        $packages = @(Get-Item -Path $BuildConfiguration.PackagePattern -ErrorAction SilentlyContinue)
        if ($packages.Count -gt 0 -and -not [string]::IsNullOrWhiteSpace($BuildConfiguration.NuGetApiKey) -and $BuildConfiguration.ShouldRelease) {
            Write-StepHeader "Publishing NuGet Packages" -Tags "Invoke-NuGetPublish"
            try {
                Invoke-NuGetPublish -BuildConfiguration $BuildConfiguration | Write-InformationStream -Tags "Invoke-NuGetPublish"
            }
            catch {
                Write-Information "NuGet package publishing failed: $_" -Tags "Invoke-NuGetPublish"
                Write-Information "Continuing with release process." -Tags "Invoke-NuGetPublish"
            }
        } elseif ($packages.Count -gt 0 -and -not $BuildConfiguration.ShouldRelease) {
            Write-Information "Packages found but skipping publication (not a release build: ShouldRelease=$($BuildConfiguration.ShouldRelease))" -Tags "Invoke-ReleaseWorkflow"
        }

        # Create GitHub release only if this is a release build
        if ($BuildConfiguration.ShouldRelease) {
            Write-StepHeader "Creating GitHub Release" -Tags "New-GitHubRelease"
            Write-Information "Creating release for version $($BuildConfiguration.Version)..." -Tags "New-GitHubRelease"
            New-GitHubRelease -BuildConfiguration $BuildConfiguration | Write-InformationStream -Tags "New-GitHubRelease"
        } else {
            Write-Information "Skipping GitHub release creation (not a release build: ShouldRelease=$($BuildConfiguration.ShouldRelease))" -Tags "Invoke-ReleaseWorkflow"
        }

        Write-StepHeader "Release Process Completed" -Tags "Invoke-ReleaseWorkflow"
        Write-Information "Release process completed successfully!" -Tags "Invoke-ReleaseWorkflow"
        return [PSCustomObject]@{
            Success = $true
            Error = ""
            Data = [PSCustomObject]@{
                Version = $BuildConfiguration.Version
                ReleaseHash = $BuildConfiguration.ReleaseHash
                PackagePaths = $packagePaths
            }
        }
    }
    catch {
        Write-Information "Release workflow failed: $_" -Tags "Invoke-ReleaseWorkflow"
        return [PSCustomObject]@{
            Success = $false
            Error = $_.ToString()
            Data = [PSCustomObject]@{
                ErrorDetails = $_.Exception.Message
                PackagePaths = @()
            }
            StackTrace = $_.ScriptStackTrace
        }
    }
}

function Invoke-CIPipeline {
    <#
    .SYNOPSIS
        Executes the CI/CD pipeline.
    .DESCRIPTION
        Executes the CI/CD pipeline, including metadata updates and build workflow.
    .PARAMETER BuildConfiguration
        The build configuration to use.
    #>
    [CmdletBinding()]
    [OutputType([PSCustomObject])]
    param (
        [Parameter(Mandatory=$true)]
        [PSCustomObject]$BuildConfiguration
    )

    Write-Information "BuildConfiguration: $($BuildConfiguration | ConvertTo-Json -Depth 10)" -Tags "Invoke-CIPipeline"

    try {
        Write-Information "Updating metadata..." -Tags "Invoke-CIPipeline"
        $metadata = Update-ProjectMetadata `
            -BuildConfiguration $BuildConfiguration

        if ($null -eq $metadata) {
            Write-Information "Metadata update returned null" -Tags "Invoke-CIPipeline"
            return [PSCustomObject]@{
                Success = $false
                Error = "Metadata update returned null"
                StackTrace = $_.ScriptStackTrace
            }
        }

        Write-Information "Metadata: $($metadata | ConvertTo-Json -Depth 10)" -Tags "Invoke-CIPipeline"

        $BuildConfiguration.Version = $metadata.Data.Version
        $BuildConfiguration.ReleaseHash = $metadata.Data.ReleaseHash

        if (-not $metadata.Success) {
            Write-Information "Failed to update metadata: $($metadata.Error)" -Tags "Invoke-CIPipeline"
            return [PSCustomObject]@{
                Success = $false
                Error = "Failed to update metadata: $($metadata.Error)"
                StackTrace = $_.ScriptStackTrace
            }
        }

        # Get the version increment info to check if we should skip the release
        Write-Information "Checking for significant changes..." -Tags "Invoke-CIPipeline"
        $versionInfo = Get-VersionInfoFromGit -CommitHash $BuildConfiguration.ReleaseHash

        if ($versionInfo.Data.VersionIncrement -eq "skip") {
            Write-Information "Skipping release: $($versionInfo.Data.IncrementReason)" -Tags "Invoke-CIPipeline"
            return [PSCustomObject]@{
                Success = $true
                Error = ""
                Data = [PSCustomObject]@{
                    Version = $metadata.Data.Version
                    ReleaseHash = $metadata.Data.ReleaseHash
                    SkippedRelease = $true
                    SkipReason = $versionInfo.Data.IncrementReason
                }
            }
        }

        Write-Information "Running build workflow..." -Tags "Invoke-CIPipeline"
        $result = Invoke-BuildWorkflow -BuildConfiguration $BuildConfiguration
        if (-not $result.Success) {
            Write-Information "Build workflow failed: $($result.Error)" -Tags "Invoke-CIPipeline"
            return [PSCustomObject]@{
                Success = $false
                Error = "Build workflow failed: $($result.Error)"
                StackTrace = $_.ScriptStackTrace
            }
        }

        Write-Information "Running release workflow..." -Tags "Invoke-CIPipeline"
        $result = Invoke-ReleaseWorkflow -BuildConfiguration $BuildConfiguration
        if (-not $result.Success) {
            Write-Information "Release workflow failed: $($result.Error)" -Tags "Invoke-CIPipeline"
            return [PSCustomObject]@{
                Success = $false
                Error = "Release workflow failed: $($result.Error)"
                StackTrace = $_.ScriptStackTrace
            }
        }

        Write-Information "CI/CD pipeline completed successfully" -Tags "Invoke-CIPipeline"
        return [PSCustomObject]@{
            Success = $true
            Version = $metadata.Data.Version
            ReleaseHash = $metadata.Data.ReleaseHash
        }
    }
    catch {
        Write-Information "CI/CD pipeline failed: $_" -Tags "Invoke-CIPipeline"
        return [PSCustomObject]@{
            Success = $false
            Error = "CI/CD pipeline failed: $_"
            StackTrace = $_.ScriptStackTrace
        }
    }
}

#endregion

# Export public functions
# Core build and environment functions
Export-ModuleMember -Function Initialize-BuildEnvironment,
                             Get-BuildConfiguration

# Version management functions
Export-ModuleMember -Function Get-GitTags,
                             Get-VersionType,
                             Get-VersionInfoFromGit,
                             New-Version

# Version comparison and conversion functions
Export-ModuleMember -Function ConvertTo-FourComponentVersion,
                             Get-VersionNotes

# Metadata and documentation functions
Export-ModuleMember -Function New-Changelog,
                             Update-ProjectMetadata,
                             New-License

# .NET SDK operations
Export-ModuleMember -Function Invoke-DotNetRestore,
                             Invoke-DotNetBuild,
                             Invoke-DotNetTest,
                             Invoke-DotNetPack,
                             Invoke-DotNetPublish

# Release and publishing functions
Export-ModuleMember -Function Invoke-NuGetPublish,
                             New-GitHubRelease

# Utility functions
Export-ModuleMember -Function Assert-LastExitCode,
                             Write-StepHeader,
                             Test-AnyFiles,
                             Get-GitLineEnding,
                             Set-GitIdentity,
                             Write-InformationStream,
                             Invoke-ExpressionWithLogging,
                             ConvertTo-ArraySafe

# High-level workflow functions
Export-ModuleMember -Function Invoke-BuildWorkflow,
                             Invoke-ReleaseWorkflow,
                             Invoke-CIPipeline

#region Module Variables
$script:DOTNET_VERSION = '9.0'
$script:LICENSE_TEMPLATE = Join-Path $PSScriptRoot "LICENSE.template"

# Set PowerShell preferences
$ErrorActionPreference = 'Stop'
$WarningPreference = 'Stop'
$InformationPreference = 'Continue'
$DebugPreference = 'Ignore'
$VerbosePreference = 'Ignore'
$ProgressPreference = 'Ignore'

# Get the line ending for the current system
$script:lineEnding = Get-GitLineEnding
#endregion
