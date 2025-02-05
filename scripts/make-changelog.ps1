param (
    [Parameter(Mandatory, Position=0)]
    [string]$COMMIT_VERSION,
    [Parameter(Mandatory, Position=1)]
    [string]$COMMIT_SHA
)

Set-PSDebug -Trace 1

function TranslateTagTo4ComponentVersion {
    param (
        [Parameter(Mandatory, Position=0)]
        [string]$TRANSLATE_TAG
    )

    $TRANSLATE_VERSION = $TRANSLATE_TAG -replace 'v', ''
    $TRANSLATE_VERSION = $TRANSLATE_VERSION -replace '-alpha', ''
    $TRANSLATE_VERSION = $TRANSLATE_VERSION -replace '-beta', ''
    $TRANSLATE_VERSION = $TRANSLATE_VERSION -replace '-rc', ''
    $TRANSLATE_VERSION = $TRANSLATE_VERSION -replace '-pre', ''
    $TRANSLATE_VERSION_COMPONENTS = $TRANSLATE_VERSION -split '\.'
    $TRANSLATE_VERSION_MAJOR = [int]$TRANSLATE_VERSION_COMPONENTS[0]
    $TRANSLATE_VERSION_MINOR = [int]$TRANSLATE_VERSION_COMPONENTS[1]
    $TRANSLATE_VERSION_PATCH = [int]$TRANSLATE_VERSION_COMPONENTS[2]
    $TRANSLATE_VERSION_PRERELEASE = 0
    if ($TRANSLATE_VERSION_COMPONENTS.Length -gt 3) {
      $TRANSLATE_VERSION_PRERELEASE = [int]$TRANSLATE_VERSION_COMPONENTS[3]
    }

    "$TRANSLATE_VERSION_MAJOR.$TRANSLATE_VERSION_MINOR.$TRANSLATE_VERSION_PATCH.$TRANSLATE_VERSION_PRERELEASE"
  }

function MakeNotesForRange {
    param (
        [Parameter(Mandatory, Position=0)]
        [AllowEmptyCollection()]
        [string[]]$SEARCH_TAGS,
        [Parameter(Mandatory, Position=1)]
        [string]$FROM_TAG,
        [Parameter(Mandatory, Position=2)]
        [string]$TO_TAG,
        [Parameter(Position=3)]
        [string]$TO_SHA = ""
    )

    $TO_VERSION = TranslateTagTo4ComponentVersion $TO_TAG
    $FROM_VERSION = TranslateTagTo4ComponentVersion $FROM_TAG

    $TO_VERSION_COMPONENTS = $TO_VERSION -split '\.'
    $TO_VERSION_MAJOR = [int]$TO_VERSION_COMPONENTS[0]
    $TO_VERSION_MINOR = [int]$TO_VERSION_COMPONENTS[1]
    $TO_VERSION_PATCH = [int]$TO_VERSION_COMPONENTS[2]
    $TO_VERSION_PRERELEASE = [int]$TO_VERSION_COMPONENTS[3]

    $FROM_VERSION_COMPONENTS = $FROM_VERSION -split '\.'
    $FROM_VERSION_MAJOR = [int]$FROM_VERSION_COMPONENTS[0]
    $FROM_VERSION_MINOR = [int]$FROM_VERSION_COMPONENTS[1]
    $FROM_VERSION_PATCH = [int]$FROM_VERSION_COMPONENTS[2]
    $FROM_VERSION_PRERELEASE = [int]$FROM_VERSION_COMPONENTS[3]

    $FROM_MAJOR_VERSION_NUMBER = $TO_VERSION_MAJOR - 1;
    $FROM_MINOR_VERSION_NUMBER = $TO_VERSION_MINOR - 1;
    $FROM_PATCH_VERSION_NUMBER = $TO_VERSION_PATCH - 1;
    $FROM_PRERELEASE_VERSION_NUMBER = $TO_VERSION_PRERELEASE - 1;


    $SEARCH_TAG = $FROM_TAG
    $VERSION_TYPE = "unknown"
    if ($TO_VERSION_PRERELEASE -gt $FROM_VERSION_PRERELEASE) {
        $VERSION_TYPE = "prerelease"
        $SEARCH_TAG = "$TO_VERSION_MAJOR.$TO_VERSION_MINOR.$TO_VERSION_PATCH.$FROM_PRERELEASE_VERSION_NUMBER"
    }
    if ($TO_VERSION_PATCH -gt $FROM_VERSION_PATCH) {
        $VERSION_TYPE = "patch"
        $SEARCH_TAG = "$TO_VERSION_MAJOR.$TO_VERSION_MINOR.$FROM_PATCH_VERSION_NUMBER.0"
    }
    if ($TO_VERSION_MINOR -gt $FROM_VERSION_MINOR) {
        $VERSION_TYPE = "minor"
        $SEARCH_TAG = "$TO_VERSION_MAJOR.$FROM_MINOR_VERSION_NUMBER.0.0"
    }
    if ($TO_VERSION_MAJOR -gt $FROM_VERSION_MAJOR) {
        $VERSION_TYPE = "major"
        $SEARCH_TAG = "$FROM_MAJOR_VERSION_NUMBER.0.0.0"
    }

    # Handle the case where the version is the same but the prerelease number has been dropped
    if ($TO_VERSION_MAJOR -eq $FROM_VERSION_MAJOR -and $TO_VERSION_MINOR -eq $FROM_VERSION_MINOR -and $TO_VERSION_PATCH -eq $FROM_VERSION_PATCH -and $TO_VERSION_PRERELEASE -eq "0" -and $FROM_VERSION_PRERELEASE -ne "0") {
        $VERSION_TYPE = "patch"
        $SEARCH_TAG = "$TO_VERSION_MAJOR.$TO_VERSION_MINOR.$FROM_PATCH_VERSION_NUMBER.0"
    }

    if ($SEARCH_TAG.Contains("-")) {
        $SEARCH_TAG = $FROM_TAG
    }

    $SEARCH_VERSION = TranslateTagTo4ComponentVersion $SEARCH_TAG

    if ($FROM_TAG -ne "v0.0.0") {
        $FOUND_SEARCH_TAG = $false
        $SEARCH_TAGS | ForEach-Object {
            if (-not $FOUND_SEARCH_TAG) {
                $OTHER_TAG = $_
                $OTHER_VERSION = TranslateTagTo4ComponentVersion $OTHER_TAG
                if ($SEARCH_VERSION -eq $OTHER_VERSION) {
                    $FOUND_SEARCH_TAG = $true
                    $SEARCH_TAG = $OTHER_TAG
                }
            }
        }

        if (-not $FOUND_SEARCH_TAG) {
            $SEARCH_TAG = $FROM_TAG
        }
    }

    $EXCLUDE_BOTS = '^(?!.*(\[bot\]|github|ProjectDirector|SyncFileContents)).*$'
    $EXCLUDE_PRS = @'
^.*(Merge pull request|Merge branch 'main'|Updated packages in|Update.*package version).*$
'@

    $RANGE_FROM = $SEARCH_TAG
    if ($RANGE_FROM -eq "v0.0.0" -or $RANGE_FROM -eq "0.0.0.0") {
        $RANGE_FROM = ""
    }

    $RANGE_TO = $TO_SHA
    if ($RANGE_TO -eq "") {
        $RANGE_TO = $TO_TAG
    }

    $RANGE = $RANGE_TO
    if ($RANGE_FROM -ne "") {
        $RANGE = "$RANGE_FROM...$RANGE_TO"
    }

    $COMMITS = git log --pretty=format:"%s ([@%aN](https://github.com/%aN))" --perl-regexp --regexp-ignore-case --grep="$EXCLUDE_PRS" --invert-grep --committer="$EXCLUDE_BOTS" --author="$EXCLUDE_BOTS" $RANGE | Sort-Object | Get-Unique

    if ($VERSION_TYPE -ne "prerelease" -and $COMMITS.Length -gt 0) {
        $VERSION_CHANGELOG = "## $TO_TAG ($VERSION_TYPE)"
        $VERSION_CHANGELOG += "`n"
        $VERSION_CHANGELOG += "`n"
        $VERSION_CHANGELOG += "Changes since ${SEARCH_TAG}:"
        $VERSION_CHANGELOG += "`n"
        $VERSION_CHANGELOG += "`n"

        $COMMITS | Where-Object { -not $_.Contains("Update VERSION to") -and -not $_.Contains("[skip ci]") } | ForEach-Object {
            $COMMIT = $_
            $VERSION_CHANGELOG += "- $COMMIT"
            $VERSION_CHANGELOG += "`n"
        }
        $VERSION_CHANGELOG += "`n"
    }

    $VERSION_CHANGELOG
}

$CHANGELOG = ""

$TAG_INDEX = 0

git config versionsort.suffix "-alpha"
git config versionsort.suffix "-beta"
git config versionsort.suffix "-rc"
git config versionsort.suffix "-pre"

$TAGS = git tag --list --sort=-v:refname

if ($null -eq $TAGS) {
    $PREVIOUS_TAG = 'v0.0.0'
    $TAGS = @()
} else {
    $PREVIOUS_TAG = $TAGS[0]
}

$TAG = "v$COMMIT_VERSION"

$CHANGELOG += MakeNotesForRange $TAGS $PREVIOUS_TAG $TAG $COMMIT_SHA

$TAGS | ForEach-Object {
    $TAG = $_
    if ($TAG -like "v*") {
        $PREVIOUS_TAG = "v0.0.0"
        if ($TAG_INDEX -lt $TAGS.Length - 1) {
        $PREVIOUS_TAG = $TAGS[$TAG_INDEX + 1]
        }

        if (-not ($PREVIOUS_TAG -like "v*")) {
        $PREVIOUS_TAG = "v0.0.0"
        }

        $CHANGELOG += MakeNotesForRange $TAGS $PREVIOUS_TAG $TAG
    }
    $TAG_INDEX += 1
}

Write-Host "CHANGELOG: $CHANGELOG"
$CHANGELOG | Out-File -FilePath CHANGELOG.md -Encoding utf8

$global:LASTEXITCODE = 0
