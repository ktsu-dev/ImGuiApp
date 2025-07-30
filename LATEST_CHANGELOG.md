## v2.1.2 (patch)

Changes since v2.1.1:

- Refactor SonarQube conditional checks in GitHub Actions: Updated syntax for SONAR_TOKEN checks to use the correct expression format, ensuring proper execution of caching and installation steps. ([@matt-edmondson](https://github.com/matt-edmondson))
- Refactor SonarQube token handling in GitHub Actions: Updated conditional checks to use environment variables for SONAR_TOKEN, ensuring consistent access across caching and installation steps. ([@matt-edmondson](https://github.com/matt-edmondson))
- Enhance SonarQube integration in GitHub Actions: Added conditional checks for SONAR_TOKEN to ensure caching and installation steps only execute when the token is available, improving workflow reliability. ([@matt-edmondson](https://github.com/matt-edmondson))
- Refactor ImGuiApp configuration handling: Introduced AdjustConfigForStartup method to automatically convert minimized window state to normal during startup, improving application reliability. Updated tests to validate this new behavior. ([@matt-edmondson](https://github.com/matt-edmondson))
- Update ImGuiApp configuration validation: Automatically convert minimized and fullscreen window states to normal during startup to prevent issues. Updated tests to reflect this change, ensuring proper state handling without exceptions. ([@matt-edmondson](https://github.com/matt-edmondson))
