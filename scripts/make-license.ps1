param (
    [Parameter(Mandatory, Position=0)]
    [string]$github_server_url = 'https://github.com', # URL of the GitHub server
    [Parameter(Mandatory, Position=1)]
    [string]$github_repository_owner = 'github_owner', # owner of the repository
    [Parameter(Mandatory, Position=2)]
    [string]$github_repository = 'github_repository' # name of the repository
)

$AUTHORS = $github_repository_owner.Replace('-', '.') + ' contributors'
$COPYRIGHT = "Copyright (c) 2023-$(Get-Date -Format 'yyyy') $AUTHORS"
$PROJECT_URL = "$github_server_url/$github_repository"
$AUTHORS_URL = "$github_server_url/$github_repository_owner"

$LICENSE = @"
MIT License

$PROJECT_URL

$COPYRIGHT

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the `"Software`"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED `"AS IS`", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
"@

# output files
$LICENSE | Out-File -FilePath LICENSE.md -Encoding utf8
$AUTHORS | Out-File -FilePath AUTHORS.md -Encoding utf8
$COPYRIGHT | Out-File -FilePath COPYRIGHT.md -Encoding utf8
$PROJECT_URL | Out-File -FilePath PROJECT_URL.url -Encoding utf8
$AUTHORS_URL | Out-File -FilePath AUTHORS.url -Encoding utf8

# output the metadata
Write-Host "AUTHORS: $AUTHORS"
Write-Host "COPYRIGHT: $COPYRIGHT"
Write-Host "LICENSE: $LICENSE"
Write-Host "PROJECT_URL: $PROJECT_URL"
Write-Host "AUTHORS_URL: $AUTHORS_URL"

# set the environment variables
"AUTHORS=$AUTHORS" | Out-File -FilePath $Env:GITHUB_ENV -Encoding utf8 -Append
"COPYRIGHT=$COPYRIGHT" | Out-File -FilePath $Env:GITHUB_ENV -Encoding utf8 -Append
"PROJECT_URL=$PROJECT_URL" | Out-File -FilePath $Env:GITHUB_ENV -Encoding utf8 -Append
"AUTHORS_URL=$AUTHORS_URL" | Out-File -FilePath $Env:GITHUB_ENV -Encoding utf8 -Append

$global:LASTEXITCODE = 0