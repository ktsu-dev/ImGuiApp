# PSBuild Module

A comprehensive PowerShell module for automating the build, test, package, and release process for .NET applications using Git-based versioning.

## Features

- Semantic versioning based on git history and commit messages
- Automatic version calculation from commit analysis
- Metadata file generation and management
- Comprehensive build, test, and package pipeline
- NuGet package creation and publishing
- GitHub release creation with assets
- Proper line ending handling based on git config

## Installation

1. Copy the `PSBuild.psm1` file to your project's `scripts` directory
2. Import the module in your PowerShell session:
   ```powershell
   Import-Module ./scripts/PSBuild.psm1
   ```

## Usage

The main entry point is `Invoke-CIPipeline`, which handles the complete build, test, package, and release process:

```powershell
# First, get the build configuration
$buildConfig = Get-BuildConfiguration `
    -ServerUrl "https://github.com" `
    -GitRef "refs/heads/main" `
    -GitSha "abc123" `
    -GitHubOwner "myorg" `
    -GitHubRepo "myrepo" `
    -GithubToken $env:GITHUB_TOKEN `
    -NuGetApiKey $env:NUGET_API_KEY `  # Optional - can be empty to skip NuGet.org publishing
    -WorkspacePath "." `
    -ExpectedOwner "myorg" `
    -ChangelogFile "CHANGELOG.md" `
    -AssetPatterns @("staging/*.nupkg", "staging/*.zip")

# Then run the pipeline
$result = Invoke-CIPipeline -BuildConfiguration $buildConfig

if ($result.Success) {
    Write-Host "Pipeline completed successfully!"
    Write-Host "Version: $($result.Version)"
    Write-Host "Release Hash: $($result.ReleaseHash)"
}
```

## Object Model

The module consistently uses PSCustomObjects for return values and data storage, providing several benefits:

- Easy property access with dot notation
- Better IntelliSense support in modern editors
- Consistent patterns throughout the codebase
- More efficient memory usage
- Clearer code structure

Each function returns a standardized object with at least:
- `Success`: Boolean indicating operation success
- `Error`: Error message if operation failed
- `Data`: Object containing function-specific results

## Managed Files

The module manages several metadata files in your repository:

| File | Description |
|------|-------------|
| VERSION.md | Contains the current semantic version |
| LICENSE.md | MIT license with project URL and copyright |
| COPYRIGHT.md | Copyright notice with year range and owner |
| AUTHORS.md | List of contributors from git history |
| CHANGELOG.md | Auto-generated changelog from git history |
| PROJECT_URL.url | Link to project repository |
| AUTHORS.url | Link to organization/owner |

## Version Control

### Version Tags

Commits can include the following tags to control version increments:

| Tag | Description | Example |
|-----|-------------|---------|
| [major] | Triggers a major version increment | 2.0.0 |
| [minor] | Triggers a minor version increment | 1.2.0 |
| [patch] | Triggers a patch version increment | 1.1.2 |
| [pre] | Creates/increments pre-release version | 1.1.2-pre.1 |

### Automatic Version Calculation

The module analyzes commit history to determine appropriate version increments, following semantic versioning principles:

1. Checks for explicit version tags in commit messages ([major], [minor], [patch], [pre])
2. Detects public API changes by analyzing code diffs
   - Adding, modifying, or removing public classes, interfaces, enums, structs, or records
   - Changes to public methods, properties, or constants
   - Any public API surface change triggers a minor version bump
3. Non-API changing code commits trigger patch version increments
4. Minimal changes default to prerelease increments

This approach ensures that:
- Breaking changes are always major version increments (manually tagged)
- Public API additions or modifications are minor version increments (automatically detected)
- Bug fixes and internal changes are patch version increments
- Trivial changes result in prerelease increments

### Public API Detection

The module automatically analyzes code changes to detect modifications to the public API surface:

- Added, modified, or removed public/protected classes, interfaces, enums, structs, or records
- Added, modified, or removed public/protected methods
- Added, modified, or removed public/protected properties
- Added or removed public constants

When any of these changes are detected, the module automatically triggers a minor version increment, following semantic versioning best practices where non-breaking API changes warrant a minor version bump.

## Build Configuration

The `Get-BuildConfiguration` function returns a configuration object with the following key properties:

| Property | Description |
|----------|-------------|
| IsOfficial | Whether this is an official repository build |
| IsMain | Whether building from main branch |
| IsTagged | Whether the current commit is tagged |
| ShouldRelease | Whether a release should be created |
| UseDotnetScript | Whether .NET script files are present |
| OutputPath | Path for build outputs |
| StagingPath | Path for staging artifacts |
| PackagePattern | Pattern for NuGet packages |
| SymbolsPattern | Pattern for symbol packages |
| ApplicationPattern | Pattern for application archives |
| Version | Current version number |
| ReleaseHash | Hash of the release commit |

## Advanced Usage

The module provides several functions for advanced scenarios:

### Build and Release Functions
- `Initialize-BuildEnvironment`: Sets up the build environment
- `Get-BuildConfiguration`: Creates the build configuration object
- `Invoke-BuildWorkflow`: Runs the build and test process
- `Invoke-ReleaseWorkflow`: Handles package creation and publishing

### Version Management Functions
- `Get-GitTags`: Gets sorted list of version tags
- `Get-VersionType`: Determines version increment type
- `Get-VersionInfoFromGit`: Gets comprehensive version information
- `New-Version`: Creates a new version file

### Package and Release Functions
- `Invoke-DotNetRestore`: Restores NuGet packages
- `Invoke-DotNetBuild`: Builds the solution
- `Invoke-DotNetTest`: Runs unit tests with coverage
- `Invoke-DotNetPack`: Creates NuGet packages
- `Invoke-DotNetPublish`: Publishes applications
- `Invoke-NuGetPublish`: Publishes packages to repositories
- `New-GitHubRelease`: Creates GitHub release with assets

### Utility Functions
- `Assert-LastExitCode`: Verifies command execution success
- `Write-StepHeader`: Creates formatted step headers in logs
- `Test-AnyFiles`: Tests for existence of files matching a pattern
- `Get-GitLineEnding`: Determines correct line endings based on git config
- `Set-GitIdentity`: Configures git user identity for automated operations
- `Write-InformationStream`: Streams output to the information stream
- `Invoke-ExpressionWithLogging`: Executes commands with proper logging

## Line Ending Handling

The module respects git's line ending settings when generating files:

1. Uses git's `core.eol` setting if defined
2. Falls back to `core.autocrlf` setting
3. Defaults to OS-specific line endings if no git settings are found

## Git Status Handling

The module carefully handles git status to prevent empty commits:

1. Only attempts commits when there are actual changes
2. Properly captures and interprets the git status output
3. Reports clear status messages during metadata updates
4. Preserves the correct commit hash for both success and no-change scenarios

## Contributing

1. Fork the repository
2. Create a feature branch
3. Commit your changes with appropriate version tags
4. Create a pull request

## License

MIT License - See LICENSE.md for details
