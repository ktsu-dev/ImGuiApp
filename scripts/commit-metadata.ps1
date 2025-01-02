git config --global user.name "Github Actions"
git config --global user.email "actions@users.noreply.github.com"
git add VERSION.md LICENSE.md AUTHORS.md COPYRIGHT.md CHANGELOG.md PROJECT_URL.url AUTHORS.url
git commit -m "[bot][skip ci] Update Metadata"
git push

$RELEASE_HASH = (git rev-parse HEAD)
Write-Host "RELEASE_HASH: $RELEASE_HASH"
"RELEASE_HASH=$RELEASE_HASH" | Out-File -FilePath $Env:GITHUB_ENV -Encoding utf8 -Append