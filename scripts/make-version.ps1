param (
    [Parameter(Mandatory, Position=0)]
    [string]$github_sha = "" # SHA of the commit
)

Set-PSDebug -Trace 1

git config versionsort.suffix "-alpha"
git config versionsort.suffix "-beta"
git config versionsort.suffix "-rc"
git config versionsort.suffix "-pre"

# find the last version that was released
$ALL_TAGS = git tag --list --sort=-v:refname
if ($null -eq $ALL_TAGS) {
    $LAST_TAG = 'v1.0.0-pre.0'
} elseif ($ALL_TAGS -is [array]) {
    $LAST_TAG = $ALL_TAGS[0]
} else {
    $LAST_TAG = $ALL_TAGS
}

Write-Host $LAST_TAG

$LAST_VERSION = $LAST_TAG -replace 'v', ''

Write-Host $LAST_VERSION

$IS_PRERELEASE = $LAST_VERSION.Contains('-')

$LAST_VERSION = $LAST_VERSION -replace '-alpha', ''
$LAST_VERSION = $LAST_VERSION -replace '-beta', ''
$LAST_VERSION = $LAST_VERSION -replace '-rc', ''
$LAST_VERSION = $LAST_VERSION -replace '-pre', ''

Write-Host $LAST_VERSION

$LAST_VERSION_COMPONENTS = $LAST_VERSION -split '\.'
$LAST_VERSION_MAJOR = [int]$LAST_VERSION_COMPONENTS[0]
$LAST_VERSION_MINOR = [int]$LAST_VERSION_COMPONENTS[1]
$LAST_VERSION_PATCH = [int]$LAST_VERSION_COMPONENTS[2]
$LAST_VERSION_PRERELEASE = 0
if ($LAST_VERSION_COMPONENTS.Length -gt 3) {
    $LAST_VERSION_PRERELEASE = [int]$LAST_VERSION_COMPONENTS[3]
}

# calculate which increment is needed

$EXCLUDE_BOTS = '^(?!.*(\[bot\]|github|ProjectDirector|SyncFileContents)).*$'
$EXCLUDE_HIDDEN_FILES = ":(icase,exclude)*/.*"
$EXCLUDE_MARKDOWN_FILES = ":(icase,exclude)*/*.md"
$EXCLUDE_TEXT_FILES = ":(icase,exclude)*/*.txt"
$EXCLUDE_SOLUTIONS_FILES = ":(icase,exclude)*/*.sln"
$EXCLUDE_PROJECTS_FILES = ":(icase,exclude)*/*.*proj"
$EXCLUDE_URL_FILES = ":(icase,exclude)*/*.url"
$EXCLUDE_BUILD_FILES = ":(icase,exclude)*/Directory.Build.*"
$EXCLUDE_CI_FILES = ":(icase,exclude).github/workflows/*"
$EXCLUDE_PS_FILES = ":(icase,exclude)*/*.ps1"

$EXCLUDE_PRS = @'
^.*(Merge pull request|Merge branch 'main'|Updated packages in|Update.*package version).*$
'@

$INCLUDE_ALL_FILES = "*/*.*"

$FIRST_COMMIT = (git rev-list HEAD)[-1]
$LAST_COMMIT = $github_sha

$COMMITS = "$FIRST_COMMIT...$LAST_COMMIT"

# note: use date order for this one or else we will get the wrong commit when there are merges from main
$LAST_NON_MERGE_COMMIT = git log -n 1 --date-order --perl-regexp --regexp-ignore-case --format=format:%H --grep="$EXCLUDE_PRS" --invert-grep $COMMITS

$COMMITS = "$FIRST_COMMIT...$LAST_NON_MERGE_COMMIT"

#note: use topo order for the rest so that branched commits are grouped together
$LAST_PATCH_COMMIT = git log -n 1 --topo-order --perl-regexp --regexp-ignore-case --format=format:%H --committer="$EXCLUDE_BOTS" --author="$EXCLUDE_BOTS" --grep="$EXCLUDE_PRS" --invert-grep $COMMITS

$LAST_MINOR_COMMIT = git log -n 1 --topo-order --perl-regexp --regexp-ignore-case --format=format:%H --committer="$EXCLUDE_BOTS" --author="$EXCLUDE_BOTS" --grep="$EXCLUDE_PRS" --invert-grep $COMMITS `
    -- `
    $INCLUDE_ALL_FILES `
    $EXCLUDE_HIDDEN_FILES `
    $EXCLUDE_MARKDOWN_FILES `
    $EXCLUDE_TEXT_FILES `
    $EXCLUDE_SOLUTIONS_FILES `
    $EXCLUDE_PROJECTS_FILES `
    $EXCLUDE_URL_FILES `
    $EXCLUDE_BUILD_FILES `
    $EXCLUDE_PS_FILES `
    $EXCLUDE_CI_FILES

$VERSION_INCREMENT = 'prerelease'
if ($LAST_NON_MERGE_COMMIT -eq $LAST_PATCH_COMMIT) {
    $VERSION_INCREMENT = 'patch'
}

if ($LAST_NON_MERGE_COMMIT -eq $LAST_MINOR_COMMIT) {
    $VERSION_INCREMENT = 'minor'
}

$LAST_NON_MERGE_COMMIT_MESSAGE = git log -n 1 --format=format:%s $LAST_NON_MERGE_COMMIT

if ($LAST_NON_MERGE_COMMIT_MESSAGE.Contains('[major]')) {
    $VERSION_INCREMENT = 'major'
} elseif ($LAST_NON_MERGE_COMMIT_MESSAGE.Contains('[minor]')) {
    $VERSION_INCREMENT = 'minor'
} elseif ($LAST_NON_MERGE_COMMIT_MESSAGE.Contains('[patch]')) {
    $VERSION_INCREMENT = 'patch'
} elseif ($LAST_NON_MERGE_COMMIT_MESSAGE.Contains('[pre]')) {
    $VERSION_INCREMENT = 'prerelease'
}

if ($IS_PRERELEASE) {
    if ($VERSION_INCREMENT -eq 'prerelease') {
    $NEW_PRERELEASE = $LAST_VERSION_PRERELEASE + 1
    $VERSION = "$LAST_VERSION_MAJOR.$LAST_VERSION_MINOR.$LAST_VERSION_PATCH-pre.$NEW_PRERELEASE"
    } elseif ($VERSION_INCREMENT -eq 'patch') {
    $VERSION = "$LAST_VERSION_MAJOR.$LAST_VERSION_MINOR.$LAST_VERSION_PATCH"
    }
} else {
    if ($VERSION_INCREMENT -eq 'prerelease') {
    $NEW_PATCH = $LAST_VERSION_PATCH + 1
    $VERSION = "$LAST_VERSION_MAJOR.$LAST_VERSION_MINOR.$NEW_PATCH-pre.1"
    } elseif ($VERSION_INCREMENT -eq 'patch') {
    $NEW_PATCH = $LAST_VERSION_PATCH + 1
    $VERSION = "$LAST_VERSION_MAJOR.$LAST_VERSION_MINOR.$NEW_PATCH"
    }
}

if ($VERSION_INCREMENT -eq 'minor') {
    $NEW_MINOR = $LAST_VERSION_MINOR + 1
    $VERSION = "$LAST_VERSION_MAJOR.$NEW_MINOR.0"
}

# Output the version information
Write-Host "LAST_VERSION: $LAST_VERSION"
Write-Host "LAST_VERSION_MAJOR: $LAST_VERSION_MAJOR"
Write-Host "LAST_VERSION_MINOR: $LAST_VERSION_MINOR"
Write-Host "LAST_VERSION_PATCH: $LAST_VERSION_PATCH"
Write-Host "LAST_VERSION_PRERELEASE: $LAST_VERSION_PRERELEASE"
Write-Host "IS_PRERELEASE: $IS_PRERELEASE"
Write-Host "FIRST_COMMIT: $FIRST_COMMIT"
Write-Host "LAST_COMMIT: $LAST_COMMIT"
Write-Host "LAST_NON_MERGE_COMMIT: $LAST_NON_MERGE_COMMIT"
Write-Host "LAST_NON_MERGE_COMMIT_MESSAGE: $LAST_NON_MERGE_COMMIT_MESSAGE"
Write-Host "LAST_PATCH_COMMIT: $LAST_PATCH_COMMIT"
Write-Host "LAST_MINOR_COMMIT: $LAST_MINOR_COMMIT"
Write-Host "VERSION_INCREMENT: $VERSION_INCREMENT"
Write-Host "VERSION: $VERSION"

# set the environment variables
"LAST_VERSION=$LAST_VERSION" | Out-File -FilePath $Env:GITHUB_ENV -Encoding utf8 -Append
"LAST_VERSION_MAJOR=$LAST_VERSION_MAJOR" | Out-File -FilePath $Env:GITHUB_ENV -Encoding utf8 -Append
"LAST_VERSION_MINOR=$LAST_VERSION_MINOR" | Out-File -FilePath $Env:GITHUB_ENV -Encoding utf8 -Append
"LAST_VERSION_PATCH=$LAST_VERSION_PATCH" | Out-File -FilePath $Env:GITHUB_ENV -Encoding utf8 -Append
"LAST_VERSION_PRERELEASE=$LAST_VERSION_PRERELEASE" | Out-File -FilePath $Env:GITHUB_ENV -Encoding utf8 -Append
"IS_PRERELEASE=$IS_PRERELEASE" | Out-File -FilePath $Env:GITHUB_ENV -Encoding utf8 -Append
"FIRST_COMMIT=$FIRST_COMMIT" | Out-File -FilePath $Env:GITHUB_ENV -Encoding utf8 -Append
"LAST_COMMIT=$LAST_COMMIT" | Out-File -FilePath $Env:GITHUB_ENV -Encoding utf8 -Append
"LAST_NON_MERGE_COMMIT=$LAST_NON_MERGE_COMMIT" | Out-File -FilePath $Env:GITHUB_ENV -Encoding utf8 -Append
"LAST_NON_MERGE_COMMIT_MESSAGE=$LAST_NON_MERGE_COMMIT_MESSAGE" | Out-File -FilePath $Env:GITHUB_ENV -Encoding utf8 -Append
"LAST_PATCH_COMMIT=$LAST_PATCH_COMMIT" | Out-File -FilePath $Env:GITHUB_ENV -Encoding utf8 -Append
"LAST_MINOR_COMMIT=$LAST_MINOR_COMMIT" | Out-File -FilePath $Env:GITHUB_ENV -Encoding utf8 -Append
"VERSION_INCREMENT=$VERSION_INCREMENT" | Out-File -FilePath $Env:GITHUB_ENV -Encoding utf8 -Append
"VERSION=$VERSION" | Out-File -FilePath $Env:GITHUB_ENV -Encoding utf8 -Append

# output files
$VERSION | Out-File -FilePath VERSION.md -Encoding utf8

$global:LASTEXITCODE = 0
