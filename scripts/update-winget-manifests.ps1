#Requires -Version 7.0
<#
.SYNOPSIS
    Updates winget manifest files with new version and SHA256 hashes from GitHub releases.

.DESCRIPTION
    This script automates the process of updating winget manifest files when a new version
    is released. It fetches the SHA256 hashes from the GitHub releases and updates the
    manifest files accordingly. Settings are automatically inferred from the repository.

.PARAMETER Version
    The version to update the manifests for (e.g., "1.0.3")

.PARAMETER GitHubRepo
    The GitHub repository in the format "owner/repo" (optional - will be detected from git remote)

.PARAMETER PackageId
    The package identifier (e.g., "company.Product") - optional

.PARAMETER ArtifactNamePattern
    Pattern for artifact filenames, with {version} and {arch} placeholders - optional

.PARAMETER ExecutableName
    Name of the executable in the zip file - optional

.PARAMETER CommandAlias
    Command alias for the executable - optional

.PARAMETER ConfigFile
    Path to a JSON configuration file with project-specific settings (optional)

.EXAMPLE
    .\update-winget-manifests.ps1 -Version "1.0.3"

.EXAMPLE
    .\update-winget-manifests.ps1 -Version "1.0.3" -GitHubRepo "myorg/myrepo" -PackageId "myorg.MyApp"
#>

param(
    [Parameter(Mandatory = $true)]
    [string]$Version,

    [Parameter(Mandatory = $false)]
    [string]$GitHubRepo,

    [Parameter(Mandatory = $false)]
    [string]$PackageId,

    [Parameter(Mandatory = $false)]
    [string]$ArtifactNamePattern,

    [Parameter(Mandatory = $false)]
    [string]$ExecutableName,

    [Parameter(Mandatory = $false)]
    [string]$CommandAlias,

    [Parameter(Mandatory = $false)]
    [string]$ConfigFile
)

$ErrorActionPreference = "Stop"

# ----- Helper Functions -----

function Test-IsLibraryOnlyProject {
    param (
        [string]$RootDir,
        [hashtable]$ProjectInfo
    )

    $hasApplications = $false
    $hasLibraries = $false
    $isMainProjectLibrary = $false

    # Get the repository name to identify the main project
    $repoName = (Get-Item -Path $RootDir).Name

    # Check for generated NuGet packages in bin directories (indicator, not definitive)
    $nupkgFiles = Get-ChildItem -Path $RootDir -Filter "*.nupkg" -Recurse -File -ErrorAction SilentlyContinue |
                  Where-Object { $_.Directory.Name -eq "Release" -or $_.Directory.Name -eq "Debug" }
    if ($nupkgFiles.Count -gt 0) {
        Write-Host "Detected NuGet package files" -ForegroundColor Yellow
        $hasLibraries = $true
    }

    # Check for C# projects
    if ($ProjectInfo.type -eq "csharp") {
        $csprojFiles = Get-ChildItem -Path $RootDir -Filter "*.csproj" -Recurse -File -Depth 3

        foreach ($csprojFile in $csprojFiles) {
            $csprojContent = Get-Content -Path $csprojFile.FullName -Raw
            $projectName = $csprojFile.BaseName

            # Check if this is the main project (matches repository name or starts with it)
            # For multi-project solutions like "Semantics.Strings" in repo "Semantics"
            # Also handle naming variations like "ImGui.App" in repo "ImGuiApp"
            $normalizedRepoName = $repoName -replace '[\.\-_]', ''
            $normalizedProjectName = $projectName -replace '[\.\-_]', ''
            $isMainProject = ($projectName -eq $repoName -or
                             $projectName.StartsWith("$repoName.") -or
                             $normalizedProjectName -eq $normalizedRepoName -or
                             $normalizedProjectName.StartsWith($normalizedRepoName))

            # Skip test projects
            $isTestProject = ($csprojContent -match 'Sdk="[^"]*\.Test["/]' -or
                            $csprojContent -match 'Sdk="[^"]*Sdk\.Test["/]' -or
                            $csprojContent -match 'Sdk="[^"]*Test[^"]*"' -or
                            $projectName -match "Test" -or
                            $projectName -match "\.Tests$")

            # Skip demo/example projects
            $isDemoProject = ($projectName -match "Demo|Example|Sample" -or
                            $projectName.Contains("Demo") -or
                            $projectName.Contains("Example") -or
                            $projectName.Contains("Sample"))

            if ($isTestProject -or $isDemoProject) {
                continue
            }

            # Explicitly check if it's an executable
            $isExecutable = ($csprojContent -match "<OutputType>\s*Exe\s*</OutputType>" -or
                           $csprojContent -match "<OutputType>\s*WinExe\s*</OutputType>" -or
                           $csprojContent -match 'Sdk="[^"]*\.App["/]' -or
                           $csprojContent -match 'Sdk="[^"]*Sdk\.App["/]')

            # Check if it's a library (explicit markers or implicit)
            $isLibrary = ($csprojContent -match "<OutputType>\s*Library\s*</OutputType>" -or
                         $csprojContent -match "<PackageId>" -or
                         $csprojContent -match "<GeneratePackageOnBuild>\s*true\s*</GeneratePackageOnBuild>" -or
                         $csprojContent -match "<IsPackable>\s*true\s*</IsPackable>" -or
                         $csprojContent -match 'Sdk="[^"]*\.Lib["/]' -or
                         $csprojContent -match 'Sdk="[^"]*Sdk\.Lib["/]' -or
                         $csprojContent -match 'Sdk="[^"]*Library[^"]*"' -or
                         $csprojContent -match "<TargetFrameworks>" -or  # Multiple target frameworks often = library
                         (-not $isExecutable))  # No explicit exe = library by default

            if ($isLibrary) {
                $hasLibraries = $true
                if ($isMainProject) {
                    $isMainProjectLibrary = $true
                }
            }

            if ($isExecutable) {
                $hasApplications = $true
            }
        }
    }

    # Check for Node.js library patterns
    if ($ProjectInfo.type -eq "node") {
        $packageJsonPath = Join-Path -Path $RootDir -ChildPath "package.json"
        if (Test-Path $packageJsonPath) {
            $packageJson = Get-Content -Path $packageJsonPath -Raw | ConvertFrom-Json
            # Check if it's a library (no bin field, or private: true)
            if (-not $packageJson.bin -or $packageJson.private -eq $true) {
                $hasLibraries = $true
            } else {
                $hasApplications = $true
            }
        }
    }

    # Check for standalone NuGet package indicators (separate from project files)
    $nuspecFiles = Get-ChildItem -Path $RootDir -Filter "*.nuspec" -Recurse -File -Depth 2
    if ($nuspecFiles.Count -gt 0) {
        $hasLibraries = $true
    }

    # Return true if the main project is a library and we have no main applications (demos don't count)
    return $isMainProjectLibrary -and -not $hasApplications
}

function Exit-GracefullyForLibrary {
    param (
        [string]$Message = "Detected library project - no executable artifacts expected."
    )

    Write-Host $Message -ForegroundColor Yellow
    Write-Host "Skipping winget manifest generation as this appears to be a library/NuGet package." -ForegroundColor Yellow
    Write-Host "Winget manifests are intended for executable applications, not libraries." -ForegroundColor Cyan
    exit 0
}

function Get-MSBuildProperties {
    param (
        [string]$ProjectPath
    )

    try {
        # Check if MSBuild is available
        $msbuild = $null
        $vsWhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"

        if (Test-Path $vsWhere) {
            # Use Visual Studio installation path
            $vsPath = & $vsWhere -latest -products * -requires Microsoft.Component.MSBuild -property installationPath
            if ($vsPath) {
                $msbuildPath = Join-Path $vsPath "MSBuild\Current\Bin\MSBuild.exe"
                if (-not (Test-Path $msbuildPath)) {
                    # Try older VS versions
                    $msbuildPath = Join-Path $vsPath "MSBuild\15.0\Bin\MSBuild.exe"
                }

                if (Test-Path $msbuildPath) {
                    $msbuild = $msbuildPath
                }
            }
        }

        # If VS installation not found, try .NET SDK's MSBuild
        if (-not $msbuild) {
            $dotnetPath = Get-Command dotnet -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Source
            if ($dotnetPath) {
                $msbuild = "dotnet msbuild"
            }
        }

        if (-not $msbuild) {
            Write-Host "MSBuild not found. Falling back to XML parsing." -ForegroundColor Yellow
            return $null
        }

        # Create temporary file to store properties
        $tempFile = [System.IO.Path]::GetTempFileName()

        # Prepare MSBuild command
        $propertiesToEvaluate = "AssemblyName;RootNamespace;PackageId;Product;Authors;Version;Description;RepositoryUrl;Copyright;PackageTags"
        $msbuildArgs = @(
            "`"$ProjectPath`"",
            "/nologo",
            "/t:_GetProjectProperties",
            "/p:PropertiesToEvaluate=$propertiesToEvaluate",
            "/p:OutputFile=`"$tempFile`""
        )

        # Create target file with task to write properties to file
        $targetFile = [System.IO.Path]::GetTempFileName() + ".targets"

@"
<Project>
    <Target Name="_GetProjectProperties">
        <ItemGroup>
            <_PropertiesToWrite Include="`$(PropertiesToEvaluate)" />
        </ItemGroup>

        <PropertyGroup>
            <_PropOutput></_PropOutput>
        </PropertyGroup>

        <CreateItem Include="`$(PropertiesToEvaluate)">
            <Output TaskParameter="Include" ItemName="PropertiesToWrite" />
        </CreateItem>

        <!-- Evaluate and write each property -->
        <CreateProperty Value="`$(_PropOutput)%0A$([System.Environment]::NewLine)%(PropertiesToWrite.Identity)=$([System.Environment]::NewLine)$([Microsoft.Build.Evaluation.ProjectProperty]::GetPropertyValue('%(PropertiesToWrite.Identity)'))">
            <Output TaskParameter="Value" PropertyName="_PropOutput" />
        </CreateProperty>

        <WriteLinesToFile File="`$(OutputFile)" Lines="`$(_PropOutput)" Overwrite="true" />

        <Message Text="Project properties written to `$(OutputFile)" Importance="high" />
    </Target>
</Project>
"@ | Out-File -FilePath $targetFile -Encoding UTF8

        # Run MSBuild
        if ($msbuild -eq "dotnet msbuild") {
            $result = & dotnet msbuild @msbuildArgs "/p:CustomBeforeMicrosoftCommonTargets=$targetFile"
        } else {
            $result = & $msbuild @msbuildArgs "/p:CustomBeforeMicrosoftCommonTargets=$targetFile"
        }

        # Check if output file was created
        if (-not (Test-Path $tempFile)) {
            Write-Host "MSBuild did not generate properties file. Falling back to XML parsing." -ForegroundColor Yellow
            return $null
        }

        # Read properties
        $propertiesText = Get-Content $tempFile -Raw
        $properties = @{}

        foreach ($line in ($propertiesText -split "`n")) {
            $line = $line.Trim()
            if ($line -match "^([^=]+)=(.*)$") {
                $propName = $Matches[1].Trim()
                $propValue = $Matches[2].Trim()
                if ($propName -and $propValue) {
                    $properties[$propName] = $propValue
                }
            }
        }

        # Clean up temp files
        Remove-Item $tempFile -Force -ErrorAction SilentlyContinue
        Remove-Item $targetFile -Force -ErrorAction SilentlyContinue

        return $properties
    }
    catch {
        Write-Host "Error evaluating MSBuild properties: $_" -ForegroundColor Yellow
        Write-Host "Falling back to XML parsing." -ForegroundColor Yellow
        return $null
    }
}

function Get-GitRemoteInfo {
    param (
        [string]$RootDir
    )

    try {
        # Get the GitHub URL from git remote
        $remoteUrl = git remote get-url origin 2>$null
        if ($remoteUrl) {
            # Extract owner and repo from different Git URL formats (HTTPS or SSH)
            if ($remoteUrl -match "github\.com[:/]([^/]+)/([^/.]+)(\.git)?$") {
                $owner = $Matches[1]
                $repo = $Matches[2]
                return "$owner/$repo"
            }
        }
    }
    catch {
        # Ignore errors if git is not available
        Write-Host "Git command failed, cannot auto-detect repository info: $_" -ForegroundColor Yellow
    }

    # Try to extract from PROJECT_URL.url file if available
    if ($RootDir) {
        $projectUrlFile = Join-Path -Path $RootDir -ChildPath "PROJECT_URL.url"
        if (Test-Path $projectUrlFile) {
            $content = Get-Content -Path $projectUrlFile -Raw
            if ($content -match "URL=https://github.com/([^/]+)/([^/\r\n]+)") {
                return "$($Matches[1])/$($Matches[2])"
            }
        }
    }

    return $null
}

function Get-FileContent {
    param (
        [string]$FilePath
    )

    if (Test-Path $FilePath) {
        return Get-Content -Path $FilePath -Raw
    }

    return $null
}

function Get-FirstLine {
    param (
        [string]$Text
    )

    if ($Text) {
        $lines = $Text -split "`n"
        return $lines[0].Trim()
    }

    return $null
}

function Get-ShortDescription {
    param (
        [string]$Text
    )

    if (-not $Text) { return $null }

    # Try to find quoted text that might be a short description
    if ($Text -match ">\s*(.+?)(?=\r?\n|$)") {
        return $Matches[1].Trim()
    }

    # Or try the first non-empty line after title
    $lines = $Text -split "`n"
    foreach ($line in $lines | Select-Object -Skip 1) {
        $trimmed = $line.Trim()
        if ($trimmed -and -not $trimmed.StartsWith('#')) {
            return $trimmed
        }
    }

    return $null
}

function ConvertFrom-TagsList {
    param (
        [string]$TagsText
    )

    if (-not $TagsText) { return @() }

    $tags = @()
    $tagsList = $TagsText -split ";" | ForEach-Object { $_.Trim() }

    foreach ($tag in $tagsList) {
        if ($tag) {
            # Replace spaces and hyphens with underscores, and take only the first word
            $cleanTag = $tag -replace "[\s\-]+", "-"
            # Limit to first 3 words
            $cleanTag = ($cleanTag -split "-" | Select-Object -First 3) -join "-"
            $tags += $cleanTag
        }
    }

    # Return top tags (most relevant for winget)
    return $tags | Select-Object -First 10
}

function Find-ProjectInfo {
    param (
        [string]$RootDir
    )

    $projectInfo = @{
        name = ""
        type = "unknown"
        executableName = ""
        fileExtensions = @()
        tags = @()
        version = ""
        shortDescription = ""
        description = ""
        publisher = ""
    }

    # Try to get version from VERSION.md
    $versionFile = Join-Path -Path $RootDir -ChildPath "VERSION.md"
    if (Test-Path $versionFile) {
        $versionContent = Get-Content -Path $versionFile -Raw
        if ($versionContent) {
            $projectInfo.version = $versionContent.Trim()
        }
    }

    # Try to get publisher info from AUTHORS.md
    $authorsFile = Join-Path -Path $RootDir -ChildPath "AUTHORS.md"
    if (Test-Path $authorsFile) {
        $authorsContent = Get-Content -Path $authorsFile -Raw
        if ($authorsContent -and $authorsContent.Trim()) {
            $projectInfo.publisher = $authorsContent.Trim().Split("`n")[0].Trim()
        }
    }

    # Try to get short description from README.md
    $readmeFile = Join-Path -Path $RootDir -ChildPath "README.md"
    if (Test-Path $readmeFile) {
        $readmeContent = Get-Content -Path $readmeFile -Raw

        # Extract name from README title
        if ($readmeContent -match "^#\s+(.+?)(?=\r?\n|$)") {
            $projectInfo.name = $Matches[1].Trim()
        }

        # Extract short description
        $shortDesc = Get-ShortDescription -Text $readmeContent
        if ($shortDesc) {
            $projectInfo.shortDescription = $shortDesc
        }
    }

    # Try to get detailed description from DESCRIPTION.md
    $descriptionFile = Join-Path -Path $RootDir -ChildPath "DESCRIPTION.md"
    if (Test-Path $descriptionFile) {
        $descContent = Get-Content -Path $descriptionFile -Raw
        if ($descContent -and $descContent.Trim()) {
            $projectInfo.description = $descContent.Trim()
        }
        elseif ($projectInfo.shortDescription) {
            # Use short description as fallback
            $projectInfo.description = $projectInfo.shortDescription
        }
    }
    elseif ($projectInfo.shortDescription) {
        # Use short description as fallback
        $projectInfo.description = $projectInfo.shortDescription
    }

    # Try to get tags from TAGS.md
    $tagsFile = Join-Path -Path $RootDir -ChildPath "TAGS.md"
    if (Test-Path $tagsFile) {
        $tagsContent = Get-Content -Path $tagsFile -Raw
        if ($tagsContent -and $tagsContent.Trim()) {
            $projectInfo.tags = ConvertFrom-TagsList -TagsText $tagsContent.Trim()
        }
    }

    # Check for .csproj files (C# projects)
    $csprojFiles = Get-ChildItem -Path $RootDir -Filter "*.csproj" -Recurse -File -Depth 3
    if ($csprojFiles.Count -gt 0) {
        $projectInfo.type = "csharp"
        $csproj = $csprojFiles[0]

        # Try to use MSBuild to extract project properties
        $msBuildProps = Get-MSBuildProperties -ProjectPath $csproj.FullName

        if ($msBuildProps -and $msBuildProps.Count -gt 0) {
            Write-Host "Successfully extracted MSBuild properties from project" -ForegroundColor Green

            # Extract project properties
            if (-not $projectInfo.name -and $msBuildProps.Product) {
                $projectInfo.name = $msBuildProps.Product
            } elseif (-not $projectInfo.name -and $msBuildProps.AssemblyName) {
                $projectInfo.name = $msBuildProps.AssemblyName
            }

            if (-not $projectInfo.version -and $msBuildProps.Version) {
                $projectInfo.version = $msBuildProps.Version
            }

            if (-not $projectInfo.description -and $msBuildProps.Description) {
                $projectInfo.description = $msBuildProps.Description
                if (-not $projectInfo.shortDescription) {
                    $projectInfo.shortDescription = $msBuildProps.Description.Split('.')[0] + '.'
                }
            }

            if (-not $projectInfo.publisher -and $msBuildProps.Authors) {
                $projectInfo.publisher = $msBuildProps.Authors.Split(',')[0].Trim()
            }

            if ($msBuildProps.PackageTags) {
                $packageTags = $msBuildProps.PackageTags.Split(';').Trim() | Where-Object { $_ }
                if ($packageTags -and $packageTags.Count -gt 0) {
                    foreach ($tag in $packageTags) {
                        if ($tag -and -not $projectInfo.tags.Contains($tag)) {
                            $projectInfo.tags += $tag
                        }
                    }
                }
            }
        } else {
            # Fallback to parsing the csproj XML
            Write-Host "Falling back to parsing csproj XML" -ForegroundColor Yellow

            # Extract project name from csproj if not already set
            if (-not $projectInfo.name) {
                $csprojContent = Get-Content -Path $csproj.FullName -Raw
                if ($csprojContent -match "<AssemblyName>(.*?)</AssemblyName>") {
                    $projectInfo.name = $Matches[1]
                }
                elseif ($csproj.BaseName) {
                    $projectInfo.name = $csproj.BaseName
                }

                # Try to extract other properties
                if ($csprojContent -match "<Version>(.*?)</Version>") {
                    $projectInfo.version = $Matches[1]
                }

                if ($csprojContent -match "<Description>(.*?)</Description>") {
                    $projectInfo.description = $Matches[1]
                    if (-not $projectInfo.shortDescription) {
                        $projectInfo.shortDescription = $Matches[1].Split('.')[0] + '.'
                    }
                }

                if ($csprojContent -match "<Authors>(.*?)</Authors>") {
                    $projectInfo.publisher = $Matches[1].Split(',')[0].Trim()
                }
            }
        }

                # Attempt to find supported file extensions from project
        $projectFileExtensions = @()

        # Try to find file extensions from .csproj
        # Look for ItemGroups that might contain extensions the app handles
        $csprojContent = Get-Content -Path $csproj.FullName -Raw
        if ($csprojContent -match "<FileExtension[s]?>(.*?)</FileExtension[s]?>") {
            $extensions = $Matches[1] -split "[,;]" | ForEach-Object { $_.Trim() }
            foreach ($ext in $extensions) {
                # Remove any dots and ensure lowercase
                $ext = $ext.TrimStart(".").ToLower()
                if ($ext -and -not [string]::IsNullOrWhiteSpace($ext)) {
                    $projectFileExtensions += $ext
                }
            }
        }

        # Look for <ItemGroup><None Include="**/*.extension" /> patterns
        $extensionMatches = [regex]::Matches($csprojContent, '<None Include="[^"]*\.\w+"')
        foreach ($match in $extensionMatches) {
            if ($match.Value -match '\.(\w+)"$') {
                $ext = $Matches[1].ToLower()
                if ($ext -and -not $projectFileExtensions.Contains($ext)) {
                    $projectFileExtensions += $ext
                }
            }
        }

        # Add common file extensions for C# projects if not already set or found
        if ($projectFileExtensions.Count -gt 0) {
            $projectInfo.fileExtensions = $projectFileExtensions
        } elseif ($projectInfo.fileExtensions.Count -eq 0) {
            $projectInfo.fileExtensions = @("cs", "json", "xml", "config", "txt")
        }

        # Add C# tags if not set
        if ($projectInfo.tags.Count -eq 0) {
            $projectInfo.tags = @("dotnet", "csharp")
        }
        else {
            # Add C# tags to existing tags
            $projectInfo.tags += @("dotnet", "csharp")
            $projectInfo.tags = $projectInfo.tags | Select-Object -Unique
        }

        # Look for custom MSBuild properties for winget
        if ($csprojContent -match "<WinGetPackageExecutable>(.*?)</WinGetPackageExecutable>") {
            $projectInfo.executableName = $Matches[1]
        } else {
            $projectInfo.executableName = "$($projectInfo.name).exe"
        }

        # Look for command alias
        if ($csprojContent -match "<WinGetCommandAlias>(.*?)</WinGetCommandAlias>") {
            $projectInfo.commandAlias = $Matches[1]
        }
    }

    # Check for package.json (Node.js projects)
    $packageJsonPath = Join-Path -Path $RootDir -ChildPath "package.json"
    if (Test-Path $packageJsonPath) {
        $projectInfo.type = "node"
        $packageJson = Get-Content -Path $packageJsonPath -Raw | ConvertFrom-Json

        # Extract name from package.json if not already set
        if (-not $projectInfo.name -and $packageJson.name) {
            $projectInfo.name = $packageJson.name
        }

        # Extract description if not already set
        if (-not $projectInfo.shortDescription -and $packageJson.description) {
            $projectInfo.shortDescription = $packageJson.description
        }

        # Add common file extensions for Node.js projects if not set
        if ($projectInfo.fileExtensions.Count -eq 0) {
            $projectInfo.fileExtensions = @("js", "json", "ts", "html", "css")
        }

        # Add Node.js tags if not set
        if ($projectInfo.tags.Count -eq 0) {
            $projectInfo.tags = @("nodejs", "javascript")
        }
        else {
            # Add Node.js tags to existing tags
            $projectInfo.tags += @("nodejs", "javascript")
            $projectInfo.tags = $projectInfo.tags | Select-Object -Unique
        }

        $projectInfo.executableName = "$($projectInfo.name).js"
    }

    # Check for Cargo.toml (Rust projects)
    $cargoTomlPath = Join-Path -Path $RootDir -ChildPath "Cargo.toml"
    if (Test-Path $cargoTomlPath) {
        $projectInfo.type = "rust"
        $cargoContent = Get-Content -Path $cargoTomlPath -Raw

        # Extract name from Cargo.toml if not already set
        if (-not $projectInfo.name -and $cargoContent -match "\[package\][\s\S]*?name\s*=\s*""([^""]+)""") {
            $projectInfo.name = $Matches[1]
        }

        # Add common file extensions for Rust projects if not set
        if ($projectInfo.fileExtensions.Count -eq 0) {
            $projectInfo.fileExtensions = @("rs", "toml")
        }

        # Add Rust tags if not set
        if ($projectInfo.tags.Count -eq 0) {
            $projectInfo.tags = @("rust")
        }
        else {
            # Add Rust tags to existing tags
            $projectInfo.tags += @("rust")
            $projectInfo.tags = $projectInfo.tags | Select-Object -Unique
        }

        $projectInfo.executableName = $projectInfo.name
    }

    # Find executables in common build directories
    if (-not $projectInfo.executableName -or -not (Test-Path -Path "$RootDir/bin/$($projectInfo.executableName)")) {
        # Look for executables in common build directories
        $buildDirs = @("bin", "publish", "target/release", "target/debug", "dist", "build", "out")

        foreach ($dir in $buildDirs) {
            $exeFiles = Get-ChildItem -Path "$RootDir/$dir" -Filter "*.exe" -File -Recurse -ErrorAction SilentlyContinue
            if ($exeFiles.Count -gt 0) {
                $projectInfo.executableName = $exeFiles[0].Name
                break
            }
        }
    }

    # If we still don't have a project name, use the directory name
    if (-not $projectInfo.name) {
        $projectInfo.name = (Get-Item -Path $RootDir).Name
    }

    return $projectInfo
}

function Get-FileDescription {
    param (
        [string]$ProjectType,
        [string]$FallbackDescription
    )

    # Return fallback if provided
    if ($FallbackDescription) {
        return $FallbackDescription
    }

    # Otherwise return a generic description based on project type
    switch ($ProjectType) {
        "csharp" { return ".NET application" }
        "node" { return "Node.js application" }
        "rust" { return "Rust application" }
        default { return "Software application" }
    }
}

# ----- Main Script -----

# Set root directory to the parent of the scripts folder
$rootDir = Join-Path $PSScriptRoot ".."
$manifestDir = Join-Path $rootDir "winget"

# Create the directory if it doesn't exist
if (-not (Test-Path $manifestDir)) {
    New-Item -Path $manifestDir -ItemType Directory | Out-Null
}

# Load configuration from file if it exists and specified
$config = @{}
if ($ConfigFile -and (Test-Path $ConfigFile)) {
    Write-Host "Loading configuration from $ConfigFile..." -ForegroundColor Yellow
    $config = Get-Content -Path $ConfigFile -Raw | ConvertFrom-Json -AsHashtable
}

# Detect repository info if not provided
if (-not $GitHubRepo) {
    $detectedRepo = Get-GitRemoteInfo -RootDir $rootDir
    if ($detectedRepo) {
        $GitHubRepo = $detectedRepo
        Write-Host "Detected GitHub repository: $GitHubRepo" -ForegroundColor Green
    }
    elseif ($config.githubRepo) {
        $GitHubRepo = $config.githubRepo
    }
    else {
        Write-Error "Could not detect GitHub repository. Please specify it using -GitHubRepo parameter."
        exit 1
    }
}

# Extract owner and repo name from GitHub repo string
$ownerRepo = $GitHubRepo.Split('/')
$owner = $ownerRepo[0]
$repo = $ownerRepo[1]

# Detect project info from repository
$projectInfo = Find-ProjectInfo -RootDir $rootDir
Write-Host "Detected project: $($projectInfo.name) (Type: $($projectInfo.type))" -ForegroundColor Green

# Early check for library-only projects to avoid unnecessary processing
if (Test-IsLibraryOnlyProject -RootDir $rootDir -ProjectInfo $projectInfo) {
    Exit-GracefullyForLibrary -Message "Detected library-only solution with no applications."
}

# Check for explicit version provided
if ($projectInfo.version -and -not $Version) {
    $Version = $projectInfo.version
    Write-Host "Using detected version: $Version" -ForegroundColor Green
}

# Build configuration object with detected and provided values
$config = @{
    packageId = if ($PackageId) { $PackageId } elseif ($config.packageId) { $config.packageId } else { "$owner.$repo" }
    githubRepo = $GitHubRepo
    artifactNamePattern = if ($ArtifactNamePattern) { $ArtifactNamePattern } elseif ($config.artifactNamePattern) { $config.artifactNamePattern } else { "$repo-{version}-{arch}.zip" }
    executableName = if ($ExecutableName) { $ExecutableName } elseif ($config.executableName) { $config.executableName } elseif ($projectInfo.executableName) { $projectInfo.executableName } else { "$repo.exe" }
    commandAlias = if ($CommandAlias) { $CommandAlias } elseif ($config.commandAlias) { $config.commandAlias } else { $repo.ToLower() }
    packageName = if ($config.packageName) { $config.packageName } else { $projectInfo.name -replace "^$owner\.", "" }
    publisher = if ($config.publisher) { $config.publisher } elseif ($projectInfo.publisher) { $projectInfo.publisher } else { $owner }
    shortDescription = if ($config.shortDescription) { $config.shortDescription } elseif ($projectInfo.shortDescription) { $projectInfo.shortDescription } else { "A $($projectInfo.type) application" }
    description = if ($config.description) { $config.description } elseif ($projectInfo.description) { $projectInfo.description } else { Get-FileDescription -ProjectType $projectInfo.type }
    fileExtensions = if ($config.fileExtensions -and $config.fileExtensions.Count -gt 0) { $config.fileExtensions } elseif ($projectInfo.fileExtensions.Count -gt 0) { $projectInfo.fileExtensions } else { @("txt", "md", "json") }
    tags = if ($config.tags -and $config.tags.Count -gt 0) { $config.tags } elseif ($projectInfo.tags.Count -gt 0) { $projectInfo.tags } else { @("utility", "application") }
}

Write-Host "Configuration:" -ForegroundColor Yellow
Write-Host "  Package ID: $($config.packageId)" -ForegroundColor Cyan
Write-Host "  GitHub Repo: $($config.githubRepo)" -ForegroundColor Cyan
Write-Host "  Package Name: $($config.packageName)" -ForegroundColor Cyan
Write-Host "  Publisher: $($config.publisher)" -ForegroundColor Cyan
Write-Host "  Artifact Pattern: $($config.artifactNamePattern)" -ForegroundColor Cyan
Write-Host "  Executable: $($config.executableName)" -ForegroundColor Cyan
Write-Host "  Command Alias: $($config.commandAlias)" -ForegroundColor Cyan
Write-Host "  Description: $($config.shortDescription)" -ForegroundColor Cyan
Write-Host "  Tags: $($config.tags -join ', ')" -ForegroundColor Cyan

# GitHub API configuration
$releaseUrl = "https://api.github.com/repos/$($config.githubRepo)/releases/tags/v$Version"
$downloadBaseUrl = "https://github.com/$($config.githubRepo)/releases/download/v$Version"

# Build headers for GitHub API requests (with optional authentication)
$githubHeaders = @{
    "User-Agent" = "Winget-Manifest-Updater"
    "Accept" = "application/vnd.github.v3+json"
}

# Check for GITHUB_TOKEN environment variable for authenticated requests (higher rate limit)
$githubToken = $env:GITHUB_TOKEN
if (-not $githubToken) {
    $githubToken = $env:GH_TOKEN
}
if ($githubToken) {
    $githubHeaders["Authorization"] = "Bearer $githubToken"
    Write-Host "Using authenticated GitHub API requests" -ForegroundColor Green
} else {
    Write-Host "Warning: No GITHUB_TOKEN found. API requests may be rate-limited." -ForegroundColor Yellow
    Write-Host "Set GITHUB_TOKEN environment variable for authenticated requests." -ForegroundColor Yellow
}

Write-Host "Updating winget manifests for $($config.packageName) version $Version..." -ForegroundColor Green

# Fetch release information from GitHub
try {
    Write-Host "Fetching release information from GitHub..." -ForegroundColor Yellow
    $release = Invoke-RestMethod -Uri $releaseUrl -Headers $githubHeaders

    Write-Host "Found release: $($release.name)" -ForegroundColor Green
    $releaseDate = [DateTime]::Parse($release.published_at).ToString("yyyy-MM-dd")
} catch {
    # Check if this might be a library-only project before failing
    if (Test-IsLibraryOnlyProject -RootDir $rootDir -ProjectInfo $projectInfo) {
        Exit-GracefullyForLibrary -Message "Failed to fetch release information, but detected library-only solution."
    }

    Write-Error "Failed to fetch release information: $_"
    exit 1
}

# Download and calculate SHA256 hashes for each architecture
$architectures = @("win-x64", "win-x86", "win-arm64")
$sha256Hashes = @{}

# First, try to read hashes from local file if available (from recent build)
$localHashesFile = Join-Path $rootDir "staging" "hashes.txt"
$localHashes = @{}

if (Test-Path $localHashesFile) {
    Write-Host "Reading hashes from local build output..." -ForegroundColor Yellow
    Get-Content $localHashesFile | ForEach-Object {
        if ($_ -match '^(.+)=(.+)$') {
            $localHashes[$Matches[1]] = $Matches[2]
        }
    }
}

foreach ($arch in $architectures) {
    # Replace placeholders in artifact name pattern
    $fileName = $config.artifactNamePattern -replace '{name}', $repo -replace '{version}', $Version -replace '{arch}', $arch

    # Try to use local hash first
    if ($localHashes.ContainsKey($fileName)) {
        $sha256Hashes[$arch] = $localHashes[$fileName].ToUpper()
        Write-Host "  $arch`: $($sha256Hashes[$arch]) (from local build)" -ForegroundColor Cyan
        continue
    }

    # Fall back to downloading and calculating hash
    $downloadUrl = "$downloadBaseUrl/$fileName"
    $tempFile = Join-Path $env:TEMP $fileName

    try {
        Write-Host "Downloading $fileName to calculate SHA256..." -ForegroundColor Yellow
        Invoke-WebRequest -Uri $downloadUrl -OutFile $tempFile -UseBasicParsing

        $hash = Get-FileHash -Path $tempFile -Algorithm SHA256
        $sha256Hashes[$arch] = $hash.Hash.ToUpper()

        Write-Host "  $arch`: $($hash.Hash)" -ForegroundColor Cyan

        # Clean up temp file
        Remove-Item $tempFile -Force
    } catch {
        Write-Host "Warning: Failed to download or hash $fileName`: $_" -ForegroundColor Yellow
        Write-Host "Skipping this architecture. If required, please provide the correct artifact name pattern." -ForegroundColor Yellow
    }
}

# Check if we have at least one hash
if ($sha256Hashes.Count -eq 0) {
    # Check if this appears to be a library-only project (no executable artifacts)
    if (Test-IsLibraryOnlyProject -RootDir $rootDir -ProjectInfo $projectInfo) {
        Exit-GracefullyForLibrary
    } else {
        Write-Error "Could not obtain any SHA256 hashes. Please check that the artifact name pattern matches your release files."
        exit 1
    }
}

# Update version manifest
$versionManifestPath = Join-Path $manifestDir "$($config.packageId).yaml"
Write-Host "Updating version manifest: $versionManifestPath" -ForegroundColor Yellow

$versionContent = @"
# yaml-language-server: `$schema=https://aka.ms/winget-manifest.version.1.10.0.schema.json
PackageIdentifier: $($config.packageId)
PackageVersion: $Version
DefaultLocale: en-US
ManifestType: version
ManifestVersion: 1.10.0
"@

Set-Content -Path $versionManifestPath -Value $versionContent -Encoding UTF8

# Update locale manifest
$localeManifestPath = Join-Path $manifestDir "$($config.packageId).locale.en-US.yaml"
Write-Host "Updating locale manifest: $localeManifestPath" -ForegroundColor Yellow

# Generate tags string for YAML
$tagsYaml = ""
if ($config.tags -and $config.tags.Count -gt 0) {
    $tagsYaml = "Tags:`n"
    foreach ($tag in $config.tags) {
        $tagsYaml += "- $tag`n"
    }
}

$localeContent = @"
# yaml-language-server: `$schema=https://aka.ms/winget-manifest.defaultLocale.1.10.0.schema.json
PackageIdentifier: $($config.packageId)
PackageVersion: $Version
PackageLocale: en-US
Publisher: $($config.publisher)
PublisherUrl: https://github.com/$owner
PublisherSupportUrl: https://github.com/$($config.githubRepo)/issues
# PrivacyUrl:
Author: $($config.publisher)
PackageName: $($config.packageName)
PackageUrl: https://github.com/$($config.githubRepo)
License: MIT
LicenseUrl: https://github.com/$($config.githubRepo)/blob/main/LICENSE.md
Copyright: Copyright (c) $($config.publisher)
# CopyrightUrl:
ShortDescription: $($config.shortDescription)
Description: $($config.description)
Moniker: $($config.commandAlias)
$tagsYaml
ReleaseNotes: |-
  See full changelog at: https://github.com/$($config.githubRepo)/blob/main/CHANGELOG.md
ReleaseNotesUrl: https://github.com/$($config.githubRepo)/releases/tag/v$Version
# PurchaseUrl:
# InstallationNotes:
Documentations:
- DocumentLabel: README
  DocumentUrl: https://github.com/$($config.githubRepo)/blob/main/README.md
ManifestType: defaultLocale
ManifestVersion: 1.10.0
"@

Set-Content -Path $localeManifestPath -Value $localeContent -Encoding UTF8

# Update installer manifest
$installerManifestPath = Join-Path $manifestDir "$($config.packageId).installer.yaml"
Write-Host "Updating installer manifest: $installerManifestPath" -ForegroundColor Yellow

# Generate file extensions string for YAML
$fileExtensionsYaml = ""
if ($config.fileExtensions -and $config.fileExtensions.Count -gt 0) {
    $fileExtensionsYaml = "FileExtensions:`n"
    foreach ($ext in $config.fileExtensions) {
        $fileExtensionsYaml += "- $ext`n"
    }
}

# Generate commands string for YAML
$commandsYaml = "Commands:`n- $($config.commandAlias)"
if ($config.executableName -ne $config.commandAlias) {
    $commandsYaml += "`n- $($config.executableName.Replace('.exe', ''))"
}

$installerContent = @"
# yaml-language-server: `$schema=https://aka.ms/winget-manifest.installer.1.10.0.schema.json
PackageIdentifier: $($config.packageId)
PackageVersion: $Version
Platform:
- Windows.Desktop
MinimumOSVersion: 10.0.17763.0
InstallerType: zip
InstallModes:
- interactive
- silent
UpgradeBehavior: install
$commandsYaml
$fileExtensionsYaml
ReleaseDate: $releaseDate
Dependencies:
  PackageDependencies:
"@

# Add .NET dependency based on project type
if ($projectInfo.type -eq "csharp") {
    $installerContent += "    - PackageIdentifier: Microsoft.DotNet.DesktopRuntime.9`n"
}

$installerContent += "Installers:`n"

foreach ($arch in $sha256Hashes.Keys) {
    # Replace placeholders in artifact name pattern
    $fileName = $config.artifactNamePattern -replace '{name}', $repo -replace '{version}', $Version -replace '{arch}', $arch

    # Add installer entry for this architecture
    $installerContent += @"
- Architecture: $($arch.Replace('win-', ''))
  InstallerUrl: $downloadBaseUrl/$fileName
  InstallerSha256: $($sha256Hashes[$arch])
  NestedInstallerType: portable
  NestedInstallerFiles:
  - RelativeFilePath: $($config.executableName)
    PortableCommandAlias: $($config.commandAlias)

"@
}

$installerContent += @"
ManifestType: installer
ManifestVersion: 1.10.0
"@

Set-Content -Path $installerManifestPath -Value $installerContent -Encoding UTF8

# Try to upload manifest files to GitHub release if gh CLI is available
try {
    $ghCommand = Get-Command gh -ErrorAction SilentlyContinue
    if ($ghCommand) {
        Write-Host "Uploading manifest files to GitHub release..." -ForegroundColor Yellow
        gh release upload v$Version $versionManifestPath $localeManifestPath $installerManifestPath --repo $($config.githubRepo)
        Write-Host "Manifest files uploaded to release." -ForegroundColor Green
    }
} catch {
    # Check if upload failure might be due to missing release artifacts for library-only projects
    if ($_.Exception.Message -match "not found|404" -and (Test-IsLibraryOnlyProject -RootDir $rootDir -ProjectInfo $projectInfo)) {
        Exit-GracefullyForLibrary -Message "Release upload failed, likely due to library-only solution having no executable artifacts."
    }

    Write-Host "GitHub CLI not available or error uploading files: $_" -ForegroundColor Yellow
    Write-Host "Manifest files were created but not uploaded to the release." -ForegroundColor Yellow
}

Write-Host "`n✅ Winget manifests updated successfully!" -ForegroundColor Green
Write-Host "Files updated:" -ForegroundColor Yellow
Write-Host "  - $versionManifestPath" -ForegroundColor Cyan
Write-Host "  - $localeManifestPath" -ForegroundColor Cyan
Write-Host "  - $installerManifestPath" -ForegroundColor Cyan

Write-Host "`nNext steps:" -ForegroundColor Yellow
Write-Host "1. Review the updated manifest files" -ForegroundColor White
Write-Host "2. Test the manifests locally with: winget install --manifest $manifestDir" -ForegroundColor White
Write-Host "3. Submit to winget-pkgs repository: https://github.com/microsoft/winget-pkgs" -ForegroundColor White
Write-Host "4. Create a PR following the winget contribution guidelines" -ForegroundColor White
