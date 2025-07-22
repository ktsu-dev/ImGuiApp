#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Downloads and installs Nerd Fonts for ImGuiApp project with interactive TUI.

.DESCRIPTION
    This script automates the process of downloading Nerd Fonts from the official
    Nerd Fonts repository, extracting the required font file, and replacing the current font
    file in the ImGuiApp/Resources directory. Preserves any manually placed emoji fonts.
    Features an interactive TUI for font selection. The selected font will be installed as
    "NerdFont.ttf" with automatic build detection. Place "NotoEmoji.ttf" manually for emoji support.

.PARAMETER FontName
    The name of the Nerd Font to download. If not specified, launches interactive TUI.

.PARAMETER Interactive
    Force launch of the interactive TUI even if FontName is specified.

.PARAMETER Force
    Force download and replacement even if font files already exist.

.PARAMETER Cleanup
    Remove temporary download files after installation. Enabled by default.

.EXAMPLE
    .\Update-NerdFont.ps1
    Launches interactive TUI to choose from available fonts.

.EXAMPLE
    .\Update-NerdFont.ps1 -Interactive
    Forces interactive mode even if other parameters are provided.

.EXAMPLE
    .\Update-NerdFont.ps1 -FontName "JetBrainsMono" -Force
    Directly installs JetBrains Mono Nerd Font, bypassing TUI.
#>

[CmdletBinding()]
param(
	[string]$FontName,
	[switch]$Interactive,
	[switch]$Force,
	[switch]$Cleanup = $true
)

# Set error handling
$ErrorActionPreference = "Stop"

# Define paths
$ProjectRoot = Split-Path -Parent $PSScriptRoot
$ResourcesPath = Join-Path $ProjectRoot "ImGuiApp" "Resources"
$TempPath = Join-Path $env:TEMP "NerdFont_Download"

# Standardized font filename - this is what the build system will look for
$StandardFontFilename = "NerdFont.ttf"
$EmojiFilename = "NotoEmoji.ttf"  # Preserve manually placed emoji fonts

# Enhanced font mappings with descriptions and popularity (sorted alphabetically)
$FontMappings = @{
	"CascadiaCode"   = @{
		"DownloadName" = "CascadiaCode"
		"DisplayName"  = "Cascadia Code"
		"Description"  = "Microsoft's modern programming font with ligatures."
		"Features"     = "Programming ligatures, Windows Terminal default, modern design"
		"Popularity"   = "⭐⭐⭐⭐"
		"Category"     = "Modern"
	}
	"DejaVuSansMono" = @{
		"DownloadName" = "DejaVuSansMono"
		"DisplayName"  = "DejaVu Sans Mono"
		"Description"  = "Classic monospace font with extensive Unicode coverage."
		"Features"     = "Wide Unicode support, familiar design, cross-platform"
		"Popularity"   = "⭐⭐⭐"
		"Category"     = "Classic"
	}
	"FiraCode"       = @{
		"DownloadName" = "FiraCode"
		"DisplayName"  = "Fira Code"
		"Description"  = "Popular programming font with beautiful ligatures for code symbols."
		"Features"     = "Programming ligatures (->), arrow symbols, clean design"
		"Popularity"   = "⭐⭐⭐⭐⭐"
		"Category"     = "Programming"
	}
	"Hack"           = @{
		"DownloadName" = "Hack"
		"DisplayName"  = "Hack"
		"Description"  = "Clean, readable monospace font designed for source code."
		"Features"     = "High legibility, optimized for screens, minimal design"
		"Popularity"   = "⭐⭐⭐⭐"
		"Category"     = "Programming"
	}
	"Inconsolata"    = @{
		"DownloadName" = "Inconsolata"
		"DisplayName"  = "Inconsolata"
		"Description"  = "Humanist monospace font designed for printed code listings."
		"Features"     = "Distinctive character shapes, print-optimized, elegant"
		"Popularity"   = "⭐⭐⭐"
		"Category"     = "Classic"
	}
	"JetBrainsMono"  = @{
		"DownloadName" = "JetBrainsMono"
		"DisplayName"  = "JetBrains Mono"
		"Description"  = "Modern monospace font designed for developers. Clear character distinction."
		"Features"     = "Excellent readability, programming optimized, 0/O distinction"
		"Popularity"   = "⭐⭐⭐⭐⭐"
		"Category"     = "Programming"
	}
	"SourceCodePro"  = @{
		"DownloadName" = "SourceCodePro"
		"DisplayName"  = "Source Code Pro"
		"Description"  = "Adobe's open-source monospace font family for coding environments."
		"Features"     = "Professional design, multiple weights, excellent hinting"
		"Popularity"   = "⭐⭐⭐⭐"
		"Category"     = "Programming"
	}
	"UbuntuMono"     = @{
		"DownloadName" = "UbuntuMono"
		"DisplayName"  = "Ubuntu Mono"
		"Description"  = "Ubuntu's distinctive monospace font with rounded characters."
		"Features"     = "Distinctive rounded design, Linux-native, friendly appearance"
		"Popularity"   = "⭐⭐⭐"
		"Category"     = "Linux"
	}
}

function Write-Status {
	param([string]$Message, [string]$Color = "Cyan")
	Write-Host "🚀 $Message" -ForegroundColor $Color
}

function Write-Success {
	param([string]$Message)
	Write-Host "✅ $Message" -ForegroundColor Green
}

function Write-Warning {
	param([string]$Message)
	Write-Host "⚠️  $Message" -ForegroundColor Yellow
}

function Write-Error {
	param([string]$Message)
	Write-Host "❌ $Message" -ForegroundColor Red
}

function Show-SingleVariantSelectionTUI {
	param(
		[string]$ExtractPath,
		[string]$FontName
	)

	# Find all .ttf files in the extracted directory
	$allTtfFiles = Get-ChildItem -Path $ExtractPath -Filter "*.ttf" -Recurse | Sort-Object Name

	if ($allTtfFiles.Count -eq 0) {
		Write-Error "No TTF files found in extracted archive"
		return $null
	}

	Clear-Host
	Write-Host @"
╔══════════════════════════════════════════════════════════════════════════════╗
║                          📦 Font Variant Selection                           ║
║                    Select ONE variant to install for $FontName               ║
║                   The selected font will be saved as NerdFont.ttf            ║
╚══════════════════════════════════════════════════════════════════════════════╝
"@ -ForegroundColor Cyan

	# Filter to show only the most useful variants for coding
	$ttfFiles = $allTtfFiles | Where-Object {
		$name = $_.BaseName

		# Prioritize "Mono" variants (true monospace) over "Propo" (proportional)
		$isMonoVariant = $name -match "Mono" -and $name -notmatch "Propo"

		# Include common weights that are actually useful for coding
		$hasUsefulWeight = $name -match "(Regular|Medium|Bold|SemiBold|Light)(?!.*Italic)" -or
		$name -match "(Thin|ExtraLight|ExtraBold)(?!.*Italic)"

		# Exclude uncommon variants like Italic, ExtraBlack, Black, etc.
		$isCommonVariant = $name -notmatch "(Italic|Black|ExtraBlack|Condensed|Extended|Narrow|Wide)"

		return $isMonoVariant -and $hasUsefulWeight -and $isCommonVariant
	}

	# If filtering resulted in too few options, fall back to a broader filter
	if ($ttfFiles.Count -eq 0) {
		$ttfFiles = $allTtfFiles | Where-Object {
			$name = $_.BaseName
			# At minimum, exclude italic and very uncommon weights
			return $name -notmatch "(Italic|Black|ExtraBlack|Condensed|Extended|Narrow|Wide|Propo)"
		}
	}

	# If still no results, show all files
	if ($ttfFiles.Count -eq 0) {
		$ttfFiles = $allTtfFiles
		Write-Host "⚠️  Showing all variants (filtering didn't find suitable matches)" -ForegroundColor Yellow
	}

	Write-Host ""
	Write-Host "📊 Showing $($ttfFiles.Count) recommended variants (filtered from $($allTtfFiles.Count) total)" -ForegroundColor Cyan
	if ($ttfFiles.Count -lt $allTtfFiles.Count) {
		Write-Host "💡 Focused on monospace variants with common weights for coding" -ForegroundColor Gray
	}
	Write-Host ""
	Write-Host "Available font variants:" -ForegroundColor Yellow
	Write-Host ""

	# Smart sorting: prefer Mono variants, then by weight preference
	$preferredWeights = @("Medium", "Regular", "SemiBold", "Bold", "Light", "Thin", "ExtraLight", "ExtraBold")
	$sortedFiles = @()

	# First, sort by whether it's a Mono variant, then by weight preference
	foreach ($weight in $preferredWeights) {
		# Mono variants first
		$monoMatches = $ttfFiles | Where-Object {
			$_.BaseName -match "Mono" -and $_.BaseName -like "*$weight*"
		} | Sort-Object Name
		$sortedFiles += $monoMatches

		# Then non-mono variants
		$nonMonoMatches = $ttfFiles | Where-Object {
			$_.BaseName -notmatch "Mono" -and $_.BaseName -like "*$weight*"
		} | Sort-Object Name
		$sortedFiles += $nonMonoMatches
	}

	# Add any remaining files
	$remaining = $ttfFiles | Where-Object { $sortedFiles -notcontains $_ }
	$sortedFiles += $remaining

	$index = 1
	$defaultSelection = 1  # Default to first (most preferred) option

	foreach ($file in $sortedFiles) {
		$sizeKB = [math]::Round($file.Length / 1024, 1)
		$isDefault = ($index -eq $defaultSelection)
		$defaultMark = if ($isDefault) { " [recommended]" } else { "" }
		$color = if ($isDefault) { "Green" } else { "White" }

		# Extract weight/style info for cleaner display
		$displayName = $file.Name
		$baseName = $file.BaseName

		# Try to extract the style part (e.g., "Medium", "Regular", etc.)
		if ($baseName -match "(?:NerdFont|Nerd)(?:Mono)?-?(.+)$") {
			$style = $Matches[1]
			$displayName = "$($file.Name.Split('-')[0]) $style"
		}

		Write-Host ("  {0,2}. {1} ({2} KB){3}" -f $index, $displayName, $sizeKB, $defaultMark) -ForegroundColor $color

		# Add style hints for better understanding
		if ($baseName -match "Mono") {
			Write-Host ("      🔤 Monospace (fixed-width for coding)") -ForegroundColor DarkGray
		}
		if ($baseName -match "Medium") {
			Write-Host ("      ⚖️  Medium weight (good balance of readability and boldness)") -ForegroundColor DarkGray
		}
		elseif ($baseName -match "Regular") {
			Write-Host ("      📝 Regular weight (standard thickness)") -ForegroundColor DarkGray
		}
		elseif ($baseName -match "Bold") {
			Write-Host ("      💪 Bold weight (thicker, high contrast)") -ForegroundColor DarkGray
		}
		elseif ($baseName -match "Light") {
			Write-Host ("      🪶 Light weight (thinner, subtle)") -ForegroundColor DarkGray
		}

		$index++
	}

	Write-Host ""
	Write-Host "💡 The selected font will be installed as '$StandardFontFilename' for automatic build detection" -ForegroundColor Yellow
	Write-Host "😀 Any existing '$EmojiFilename' will be preserved for emoji support" -ForegroundColor Yellow
	if ($ttfFiles.Count -lt $allTtfFiles.Count) {
		Write-Host "🔧 Type 'all' to see all $($allTtfFiles.Count) variants, or 'filter' to change filtering" -ForegroundColor Cyan
	}
	Write-Host ""

	$selectedFile = $null

	do {
		$selection = Read-Host "Select font variant (1-$($sortedFiles.Count)), 'all', 'filter', or press Enter for recommended"
		$selection = $selection.Trim()

		if ([string]::IsNullOrWhiteSpace($selection)) {
			$selectedFile = $sortedFiles[$defaultSelection - 1]
			break
		}

		switch ($selection.ToLower()) {
			'all' {
				Write-Host ""
				Write-Host "📋 All $($allTtfFiles.Count) available variants:" -ForegroundColor Yellow
				$allIndex = 1
				foreach ($file in ($allTtfFiles | Sort-Object Name)) {
					$sizeKB = [math]::Round($file.Length / 1024, 1)
					Write-Host ("  {0,3}. {1} ({2} KB)" -f $allIndex, $file.Name, $sizeKB) -ForegroundColor Gray
					$allIndex++
				}
				Write-Host ""
				$allSelection = Read-Host "Select from all variants (1-$($allTtfFiles.Count)) or press Enter to return to filtered list"
				if ($allSelection -match '^\d+$') {
					$allSelectionNum = [int]$allSelection
					if ($allSelectionNum -ge 1 -and $allSelectionNum -le $allTtfFiles.Count) {
						$selectedFile = ($allTtfFiles | Sort-Object Name)[$allSelectionNum - 1]
						break  # This will now break the outer loop
					}
				}
				# Return to filtered list (continue loop)
			}
			'filter' {
				Write-Host ""
				Write-Host "🔍 Filter Options:" -ForegroundColor Yellow
				Write-Host "  1. Monospace only (current default)"
				Write-Host "  2. Include proportional fonts"
				Write-Host "  3. Include italic variants"
				Write-Host "  4. Show all weights"
				Write-Host ""
				$filterChoice = Read-Host "Select filter option (1-4) or press Enter to continue with current filter"
				# For now, just continue with current filtering
			}
			default {
				if ($selection -match '^\d+$') {
					$selectionNum = [int]$selection
					if ($selectionNum -ge 1 -and $selectionNum -le $sortedFiles.Count) {
						$selectedFile = $sortedFiles[$selectionNum - 1]
						break  # This will now break the outer loop
					}
				}
				Write-Host "❌ Invalid selection. Enter a number (1-$($sortedFiles.Count)), 'all', 'filter', or press Enter" -ForegroundColor Red
			}
		}
	} while ($selectedFile -eq $null)

	$sizeKB = [math]::Round($selectedFile.Length / 1024, 1)
	Write-Host ""
	Write-Host "✅ Selected: $($selectedFile.Name) ($sizeKB KB)" -ForegroundColor Green
	Write-Host "   Will be installed as: $StandardFontFilename" -ForegroundColor Gray

	return $selectedFile
}

function Show-FontSelectionTUI {
	Clear-Host

	Write-Host @"
╔══════════════════════════════════════════════════════════════════════════════╗
║                        🎨 ImGuiApp Nerd Font Selector 🎨                     ║
║                         Choose Your Programming Font                         ║
╚══════════════════════════════════════════════════════════════════════════════╝
"@ -ForegroundColor Cyan

	Write-Host ""
	Write-Host "Available Nerd Fonts:" -ForegroundColor Yellow
	Write-Host ""

	# Group fonts by category
	$categories = $FontMappings.Values | Group-Object Category | Sort-Object Name

	$fontOptions = @()
	$index = 1

	foreach ($category in $categories) {
		Write-Host "  $($category.Name) Fonts:" -ForegroundColor Magenta

		foreach ($font in ($category.Group | Sort-Object DisplayName)) {
			# Find the correct key by matching DisplayName
			$fontKey = $null
			foreach ($key in $FontMappings.Keys) {
				if ($FontMappings[$key].DisplayName -eq $font.DisplayName) {
					$fontKey = $key
					break
				}
			}

			$fontOptions += @{
				Index = $index
				Key   = $fontKey
				Font  = $font
			}

			Write-Host ("    {0,2}. {1} {2}" -f $index, $font.DisplayName, $font.Popularity) -ForegroundColor White
			Write-Host ("        {0}" -f $font.Description) -ForegroundColor Gray
			Write-Host ("        Features: {0}" -f $font.Features) -ForegroundColor DarkGray
			Write-Host ""

			$index++
		}
	}

	Write-Host "  Other Options:" -ForegroundColor Magenta
	Write-Host ("    {0,2}. 🔄 Refresh available fonts from GitHub" -f $index) -ForegroundColor White
	$refreshIndex = $index
	$index++
	Write-Host ("    {0,2}. 📋 Show current font info" -f $index) -ForegroundColor White
	$infoIndex = $index
	$index++
	Write-Host ("    {0,2}. ❌ Exit" -f $index) -ForegroundColor White
	$exitIndex = $index

	Write-Host ""
	Write-Host "💡 Tip: All fonts include full Nerd Font icon support!" -ForegroundColor Green
	Write-Host ""

	while ($true) {
		$selection = Read-Host "Please select a font (1-$index) or press Enter for JetBrains Mono"

		if ([string]::IsNullOrWhiteSpace($selection)) {
			return "JetBrainsMono"
		}

		if ($selection -match '^\d+$') {
			$selectionNum = [int]$selection

			if ($selectionNum -eq $refreshIndex) {
				Show-RefreshFontsFromGitHub
				$result = Show-FontSelectionTUI  # Restart TUI
				return $result
			}
			elseif ($selectionNum -eq $infoIndex) {
				Show-CurrentFontInfo
				Read-Host "Press Enter to continue"
				$result = Show-FontSelectionTUI  # Restart TUI
				return $result
			}
			elseif ($selectionNum -eq $exitIndex) {
				Write-Host "👋 Goodbye!" -ForegroundColor Yellow
				exit 0
			}
			elseif ($selectionNum -ge 1 -and $selectionNum -le $fontOptions.Count) {
				$selected = $fontOptions[$selectionNum - 1]
				$selectedKey = $selected.Key

				Write-Host ""
				Write-Host "✨ You selected: $($selected.Font.DisplayName)" -ForegroundColor Green
				Write-Host "   $($selected.Font.Description)" -ForegroundColor Gray
				Write-Host "   Font key: $selectedKey" -ForegroundColor DarkGray
				Write-Host ""

				do {
					$confirm = Read-Host "Proceed with installation? (Y/n)"
					$confirm = $confirm.Trim()

					if ([string]::IsNullOrWhiteSpace($confirm) -or $confirm -match '^[Yy]') {
						Write-Host "✅ Installing $($selected.Font.DisplayName)..." -ForegroundColor Green
						return $selectedKey
					}
					elseif ($confirm -match '^[Nn]') {
						Write-Host "❌ Installation cancelled. Returning to font selection..." -ForegroundColor Yellow
						$result = Show-FontSelectionTUI  # Restart TUI
						return $result
					}
					else {
						Write-Host "❌ Please enter Y (yes) or N (no)." -ForegroundColor Red
					}
				} while ($true)
			}
		}

		Write-Host "❌ Invalid selection. Please choose a number between 1 and $index." -ForegroundColor Red
		Write-Host ""
	}
}

function Show-RefreshFontsFromGitHub {
	Clear-Host
	Write-Host "🔄 Checking GitHub for available Nerd Fonts..." -ForegroundColor Cyan

	try {
		$release = Get-LatestNerdFontRelease
		$availableFonts = $release.assets | Where-Object { $_.name -like "*.zip" -and $_.name -ne "NerdFontsSymbolsOnly.zip" } |
		Select-Object -ExpandProperty name | ForEach-Object { $_.Replace('.zip', '') } | Sort-Object

		Write-Host ""
		Write-Host "📦 Available fonts on GitHub ($($release.tag_name)):" -ForegroundColor Green
		Write-Host ""

		$columns = 3
		$currentColumn = 0

		foreach ($font in $availableFonts) {
			$isSupported = $FontMappings.ContainsKey($font)
			$status = if ($isSupported) { "✅" } else { "⚪" }
			$color = if ($isSupported) { "Green" } else { "Gray" }

			Write-Host ("  {0} {1,-20}" -f $status, $font) -ForegroundColor $color -NoNewline

			$currentColumn++
			if ($currentColumn -eq $columns) {
				Write-Host ""
				$currentColumn = 0
			}
		}

		if ($currentColumn -ne 0) { Write-Host "" }

		Write-Host ""
		Write-Host "Legend: ✅ = Supported by this script, ⚪ = Available but not configured" -ForegroundColor Yellow
		Write-Host ""
		Write-Host "💡 To add support for more fonts, they need to be added to the script configuration." -ForegroundColor Cyan
	}
	catch {
		Write-Host "❌ Failed to fetch font list from GitHub: $($_.Exception.Message)" -ForegroundColor Red
	}

	Write-Host ""
	Read-Host "Press Enter to return to font selection"
}

function Show-CurrentFontInfo {
	Clear-Host
	Write-Host "📋 Current Font Information" -ForegroundColor Cyan
	Write-Host ""

	$currentFonts = Get-ChildItem -Path $ResourcesPath -Filter "*NerdFont*.ttf" -ErrorAction SilentlyContinue

	if ($currentFonts) {
		Write-Host "Currently installed fonts:" -ForegroundColor Green
		foreach ($font in $currentFonts) {
			$sizeInfo = "{0:N2} MB" -f ($font.Length / 1MB)
			$modified = $font.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss")
			Write-Host "  📁 $($font.Name) ($sizeInfo, modified: $modified)" -ForegroundColor White
		}

		# Try to determine which font family is installed
		$fontFamily = "Unknown"
		foreach ($key in $FontMappings.Keys) {
			$config = $FontMappings[$key]
			if ($currentFonts.Name -contains $config.RegularFile) {
				$fontFamily = $config.DisplayName
				Write-Host ""
				Write-Host "🎯 Detected font family: $fontFamily" -ForegroundColor Yellow
				Write-Host "   $($config.Description)" -ForegroundColor Gray
				break
			}
		}
	}
	else {
		Write-Host "❌ No Nerd Font files found in Resources directory!" -ForegroundColor Red
		Write-Host "   Path: $ResourcesPath" -ForegroundColor Gray
	}

	# Check for backups
	$backupDirs = Get-ChildItem -Path $ResourcesPath -Directory -Filter "backup_*" -ErrorAction SilentlyContinue
	if ($backupDirs) {
		Write-Host ""
		Write-Host "💾 Available backups:" -ForegroundColor Magenta
		foreach ($backup in ($backupDirs | Sort-Object Name -Descending)) {
			$backupFonts = Get-ChildItem -Path $backup.FullName -Filter "*.ttf" -ErrorAction SilentlyContinue
			Write-Host "  📦 $($backup.Name) ($($backupFonts.Count) fonts)" -ForegroundColor White
		}
	}

	Write-Host ""
}

function Get-LatestNerdFontRelease {
	Write-Status "Fetching latest Nerd Font release information..."

	try {
		$apiUrl = "https://api.github.com/repos/ryanoasis/nerd-fonts/releases/latest"
		$release = Invoke-RestMethod -Uri $apiUrl -Headers @{"User-Agent" = "ImGuiApp-Font-Updater" }

		Write-Success "Found latest release: $($release.tag_name)"
		return $release
	}
	catch {
		Write-Error "Failed to fetch release information: $($_.Exception.Message)"
		throw
	}
}

function Download-NerdFont {
	param(
		[string]$FontName,
		[string]$DownloadUrl,
		[string]$OutputPath
	)

	Write-Status "Downloading $FontName Nerd Font..."
	Write-Host "  Source: $DownloadUrl" -ForegroundColor Gray
	Write-Host "  Target: $OutputPath" -ForegroundColor Gray

	try {
		# Create temp directory
		if (-not (Test-Path $TempPath)) {
			New-Item -ItemType Directory -Path $TempPath -Force | Out-Null
		}

		# Download with progress
		$webClient = New-Object System.Net.WebClient
		$webClient.DownloadFile($DownloadUrl, $OutputPath)
		$webClient.Dispose()

		Write-Success "Download completed: $(Get-Item $OutputPath | ForEach-Object { '{0:N2} MB' -f ($_.Length / 1MB) })"
	}
	catch {
		Write-Error "Download failed: $($_.Exception.Message)"
		throw
	}
}

# Emoji download removed - use manually placed emoji fonts in Resources/

function Extract-FontFiles {
	param(
		[string]$ZipPath,
		[hashtable]$FontConfig
	)

	Write-Status "Extracting font files..."

	try {
		# Extract to temp directory
		$extractPath = Join-Path $TempPath "extracted"
		if (Test-Path $extractPath) {
			Remove-Item $extractPath -Recurse -Force
		}

		Add-Type -AssemblyName System.IO.Compression.FileSystem
		[System.IO.Compression.ZipFile]::ExtractToDirectory($ZipPath, $extractPath)

		# Let user select which variant to use
		$selectedFile = Show-SingleVariantSelectionTUI -ExtractPath $extractPath -FontName $FontConfig.DisplayName

		if (-not $selectedFile) {
			Write-Warning "No file selected. Installation cancelled."
			return $null
		}

		Write-Host ""
		Write-Host "📦 Selected file for installation:" -ForegroundColor Green
		$sizeKB = [math]::Round($selectedFile.Length / 1024, 1)
		Write-Success "Installing: $($selectedFile.Name) → $StandardFontFilename ($sizeKB KB)"

		return $selectedFile
	}
	catch {
		Write-Error "Extraction failed: $($_.Exception.Message)"
		throw
	}
}

function Backup-CurrentFonts {
	Write-Status "Creating backup of current fonts..."

	$backupPath = Join-Path $ResourcesPath "backup_$(Get-Date -Format 'yyyyMMdd_HHmmss')"

	if (-not (Test-Path $backupPath)) {
		New-Item -ItemType Directory -Path $backupPath -Force | Out-Null
	}

	# Backup existing TTF files
	$existingFonts = Get-ChildItem -Path $ResourcesPath -Filter "*.ttf"
	foreach ($font in $existingFonts) {
		Copy-Item $font.FullName -Destination $backupPath
		Write-Success "Backed up: $($font.Name)"
	}

	return $backupPath
}

# Resource file functions removed - build system now automatically handles NerdFont.ttf

# Configuration update function removed - application uses standardized NerdFont.ttf automatically

function Install-FontFiles {
	param(
		[System.IO.FileInfo]$SelectedFile,
		[hashtable]$FontConfig
	)

	Write-Status "Installing font file to ImGuiApp Resources..."

	try {
		# Check if emoji font exists before cleanup
		$emojiPath = Join-Path $ResourcesPath $EmojiFilename
		$hasExistingEmoji = Test-Path $emojiPath
		$emojiBackup = $null

		if ($hasExistingEmoji) {
			# Temporarily backup emoji font to preserve it
			$emojiBackup = Join-Path $env:TEMP "temp_emoji_backup.ttf"
			Copy-Item $emojiPath -Destination $emojiBackup -Force
			Write-Status "Preserving existing emoji font: $EmojiFilename"
		}

		# Remove old font files (excluding the emoji font we want to preserve)
		$oldFonts = Get-ChildItem -Path $ResourcesPath -Filter "*.ttf" | Where-Object { $_.Name -ne $EmojiFilename }
		foreach ($oldFont in $oldFonts) {
			Remove-Item $oldFont.FullName -Force
			Write-Success "Removed old font: $($oldFont.Name)"
		}

		# Copy selected main font file with standardized name
		$mainTargetPath = Join-Path $ResourcesPath $StandardFontFilename
		Copy-Item $SelectedFile.FullName -Destination $mainTargetPath -Force
		Write-Success "Installed: $($SelectedFile.Name) → $StandardFontFilename"

		# Restore emoji font if it existed
		if ($emojiBackup -and (Test-Path $emojiBackup)) {
			Copy-Item $emojiBackup -Destination $emojiPath -Force
			Remove-Item $emojiBackup -Force
			$emojiSize = Get-Item $emojiPath | ForEach-Object { '{0:N1} MB' -f ($_.Length / 1MB) }
			Write-Success "Preserved emoji font: $EmojiFilename ($emojiSize)"
		}

		Write-Host ""
		$installedStatus = if (Test-Path $emojiPath) { "main font + emoji support" } else { "main font only" }
		Write-Success "Successfully installed $installedStatus"

		if (-not (Test-Path $emojiPath)) {
			Write-Host "💡 To add emoji support, manually place a monochrome emoji font as '$EmojiFilename'" -ForegroundColor Cyan
		}

		# Note: Application configuration uses the standardized font files automatically
	}
	catch {
		Write-Error "Installation failed: $($_.Exception.Message)"
		throw
	}
}

function Test-ProjectBuild {
	Write-Status "Testing project build..."

	try {
		Push-Location $ProjectRoot
		$buildResult = dotnet build --verbosity quiet 2>&1

		if ($LASTEXITCODE -eq 0) {
			Write-Success "Project builds successfully with new fonts!"
		}
		else {
			Write-Warning "Build completed with warnings. Output:"
			Write-Host $buildResult -ForegroundColor Yellow
		}
	}
	catch {
		Write-Error "Build test failed: $($_.Exception.Message)"
		throw
	}
	finally {
		Pop-Location
	}
}

function Cleanup-TempFiles {
	if ($Cleanup -and (Test-Path $TempPath)) {
		Write-Status "Cleaning up temporary files..."
		Remove-Item $TempPath -Recurse -Force
		Write-Success "Temporary files cleaned up"
	}
}

# Main execution
try {
	# Determine if we should show TUI
	$shouldShowTUI = $Interactive -or [string]::IsNullOrWhiteSpace($FontName)

	if ($shouldShowTUI) {
		Write-Host "🎨 Launching interactive font selector..." -ForegroundColor Cyan
		$FontName = Show-FontSelectionTUI
		Write-Host "🔍 TUI returned font name: '$FontName'" -ForegroundColor DarkGray

		if ([string]::IsNullOrWhiteSpace($FontName)) {
			Write-Error "No font selected from TUI"
			exit 1
		}
	}

	Write-Host @"
╔══════════════════════════════════════════════════════════════════════════════╗
║                            ImGuiApp Font Updater                             ║
║                        Downloading and Installing Nerd Font                  ║
║              Preserving manually placed emoji fonts (NotoEmoji.ttf)          ║
╚══════════════════════════════════════════════════════════════════════════════╝
"@ -ForegroundColor Cyan

	# Validate font selection
	if (-not $FontMappings.ContainsKey($FontName)) {
		Write-Error "Unsupported font: $FontName"
		Write-Host "Supported fonts: $($FontMappings.Keys -join ', ')" -ForegroundColor Yellow
		Write-Host "Run with -Interactive to see all available options." -ForegroundColor Cyan
		exit 1
	}

	$fontConfig = $FontMappings[$FontName]
	Write-Status "Selected font: $($fontConfig.DisplayName)"
	Write-Host "  Description: $($fontConfig.Description)" -ForegroundColor Gray
	Write-Host "  Features: $($fontConfig.Features)" -ForegroundColor Gray

	# Validate project structure
	if (-not (Test-Path $ResourcesPath)) {
		Write-Error "ImGuiApp Resources directory not found: $ResourcesPath"
		Write-Host "Make sure you're running this script from the project root or scripts directory." -ForegroundColor Yellow
		exit 1
	}

	# Check if font already exists and Force is not specified
	$targetFile = Join-Path $ResourcesPath $StandardFontFilename

	if (-not $Force -and (Test-Path $targetFile)) {
		Write-Warning "Font file already exists: $StandardFontFilename. Use -Force to overwrite."

		$response = Read-Host "Continue anyway? (y/N)"
		if ($response -notmatch '^[Yy]') {
			Write-Host "Operation cancelled by user." -ForegroundColor Yellow
			exit 0
		}
	}

	# Get latest release
	$release = Get-LatestNerdFontRelease

	# Find download URL
	$downloadUrl = $release.assets | Where-Object { $_.name -eq "$($fontConfig.DownloadName).zip" } | Select-Object -ExpandProperty browser_download_url

	if (-not $downloadUrl) {
		Write-Error "Download URL not found for $($fontConfig.DownloadName)"
		Write-Host "Available assets:" -ForegroundColor Yellow
		$release.assets | Select-Object name | Format-Table -AutoSize
		exit 1
	}

	# Download font
	$zipPath = Join-Path $TempPath "$($fontConfig.DownloadName).zip"
	Download-NerdFont -FontName $fontConfig.DisplayName -DownloadUrl $downloadUrl -OutputPath $zipPath

	# Extract font - this returns a single file
	$fontFile = Extract-FontFiles -ZipPath $zipPath -FontConfig $fontConfig

	if (-not $fontFile) {
		Write-Error "No font file selected"
		exit 1
	}

	# Backup current fonts
	$backupPath = Backup-CurrentFonts
	Write-Success "Backup created: $backupPath"

	# Install new font (preserving any manually placed emoji fonts)
	Install-FontFiles -SelectedFile $fontFile -FontConfig $fontConfig

	# Test build
	Test-ProjectBuild

	# Cleanup
	Cleanup-TempFiles

		$emojiPath = Join-Path $ResourcesPath $EmojiFilename
	$emojiStatus = if (Test-Path $emojiPath) { "✅ With emoji support!" } else { "💡 Add monochrome emoji font manually" }

	Write-Host @"

╔══════════════════════════════════════════════════════════════════════════════╗
║                              🎉 SUCCESS! 🎉                                  ║
║                                                                              ║
║  $($fontConfig.DisplayName) Nerd Font has been successfully installed!       ║
║  Installed as: $StandardFontFilename                                         ║
║  Emoji support: $EmojiFilename $emojiStatus                                  ║
║                                                                              ║
║  Next steps:                                                                 ║
║  1. Run: dotnet build                                                        ║
║  2. Run: dotnet run --project ImGuiAppDemo                                   ║
║  3. Check the "Nerd Fonts" tab to see your new icons and emojis! 😀🚀       ║
║                                                                              ║
║  Backup location: $($backupPath.Split('\')[-1])                              ║
╚══════════════════════════════════════════════════════════════════════════════╝
"@ -ForegroundColor Green

}
catch {
	Write-Error "Font update failed: $($_.Exception.Message)"

	Write-Host @"

╔══════════════════════════════════════════════════════════════════════════════╗
║                              ❌ FAILED ❌                                    ║
║                                                                              ║
║  The font update process failed. Check the error messages above.             ║
║  Your original fonts have been backed up and should still work.              ║
║                                                                              ║
║  For manual installation:                                                    ║
║  1. Download from: https://github.com/ryanoasis/nerd-fonts/releases          ║
║  2. Extract $FontName.zip                                                    ║
║  3. Copy the desired TTF file to ImGuiApp/Resources/ as NerdFont.ttf         ║
╚══════════════════════════════════════════════════════════════════════════════╝
"@ -ForegroundColor Red

	exit 1
}
finally {
	# Always try to cleanup on exit
	if (Test-Path $TempPath) {
		try {
			Remove-Item $TempPath -Recurse -Force -ErrorAction SilentlyContinue
		}
		catch {
			# Ignore cleanup errors
		}
	}
}
