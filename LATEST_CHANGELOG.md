## v2.0.3 (patch)

Changes since v2.0.2:

- Enhance Invoke-DotNetPack function in PSBuild script to handle release notes exceeding NuGet's 35,000 character limit. Added logic to truncate long release notes and create a temporary file for compliance, with appropriate logging and cleanup of temporary files after packaging. ([@matt-edmondson](https://github.com/matt-edmondson))
