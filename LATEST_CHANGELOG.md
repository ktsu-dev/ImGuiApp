## v2.0.4 (patch)

Changes since v2.0.3:

- Refactor Invoke-DotNetPack function in PSBuild script to improve handling of release notes. Updated logic to create a temporary file for truncated content exceeding NuGet's 35,000 character limit, ensuring compliance and enhancing logging for better traceability. ([@matt-edmondson](https://github.com/matt-edmondson))
