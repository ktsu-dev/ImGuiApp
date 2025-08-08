## v2.1.7 (patch)

Changes since v2.1.6:

- Fix NuGet package source URL in Invoke-NuGetPublish function: Updated the source URL to ensure correct package publishing to packages.ktsu.dev. ([@matt-edmondson](https://github.com/matt-edmondson))
- Enhance .NET CI workflow: Added support for skipped releases in the GitHub Actions workflow. Updated conditions for SonarQube execution, coverage report upload, and Winget manifest updates to account for skipped releases, improving control over the release process. ([@matt-edmondson](https://github.com/matt-edmondson))
