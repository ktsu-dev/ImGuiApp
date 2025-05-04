@{
    # Module information
    RootModule = 'PSBuild.psm1'
    ModuleVersion = '1.1.0'
    GUID = '15dd2bfc-0f11-4c8a-b98a-f2529558f423'
    Author = 'ktsu.dev'
    CompanyName = 'ktsu.dev'
    Copyright = '(c) 2023-2025 ktsu.dev. All rights reserved.'
    Description = 'A comprehensive PowerShell module for automating the build, test, package, and release process for .NET applications using Git-based versioning.'

    # PowerShell version required
    PowerShellVersion = '5.1'

    # Functions to export
    FunctionsToExport = @(
        # Core build and environment functions
        'Initialize-BuildEnvironment',
        'Get-BuildConfiguration',

        # Version management functions
        'Get-GitTags',
        'Get-VersionType',
        'Get-VersionInfoFromGit',
        'New-Version',

        # Version comparison and conversion functions
        'ConvertTo-FourComponentVersion',
        'Get-VersionNotes',

        # Metadata and documentation functions
        'New-Changelog',
        'Update-ProjectMetadata',
        'New-License',

        # .NET SDK operations
        'Invoke-DotNetRestore',
        'Invoke-DotNetBuild',
        'Invoke-DotNetTest',
        'Invoke-DotNetPack',
        'Invoke-DotNetPublish',

        # Release and publishing functions
        'Invoke-NuGetPublish',
        'New-GitHubRelease',

        # Utility functions
        'Assert-LastExitCode',
        'Write-StepHeader',
        'Test-AnyFiles',
        'Get-GitLineEnding',
        'Set-GitIdentity',
        'Write-InformationStream',
        'Invoke-ExpressionWithLogging',

        # High-level workflow functions
        'Invoke-BuildWorkflow',
        'Invoke-ReleaseWorkflow',
        'Invoke-CIPipeline'
    )

    # Variables to export
    VariablesToExport = @()

    # Aliases to export
    AliasesToExport = @()

    # Tags for PowerShell Gallery
    PrivateData = @{
        PSData = @{
            Tags = @(
                'build',
                'dotnet',
                'ci',
                'cd',
                'nuget',
                'github',
                'versioning',
                'release',
                'automation'
            )
            LicenseUri = 'https://github.com/ktsu-dev/PSBuild/blob/main/LICENSE.md'
            ProjectUri = 'https://github.com/ktsu-dev/PSBuild'
            ReleaseNotes = @'
v1.1.0:
- Improved object model using PSCustomObjects instead of hashtables
- Enhanced git status detection and commit handling
- Fixed logging and variable capture issues
- Added comprehensive help comments to all functions
- Added utility functions to the exported functions list

v1.0.0:
- Initial release of PSBuild module featuring:
- Semantic versioning based on git history
- Automatic version calculation from commit analysis
- Metadata file generation and management
- Comprehensive build, test, and package pipeline
- NuGet package creation and publishing
- GitHub release creation with assets
- Proper line ending handling based on git config
'@
        }
    }
}
