# Changelog

All notable changes to `RIoT2.Connector.InfluxDB`. A version is released by pushing a git tag. CI
then builds and pushes the Docker image to GitHub Container Registry.

## [Unreleased]

## [1.0.0] - 2026-10-12

Built on `RIoT2.Core` 1.0.1 and .NET 10. See the
[upgrade notes](https://github.com/Revolutionized-IoT2/.github/blob/main/docs/guides/upgrading.md).

- Changed target framework and Docker base images to .NET 10 (`aspnet:10.0-alpine` and
  `sdk:10.0-alpine`).
- Changed the Core dependency to `RIoT2.Core` 1.0.1 and moved package versions to
  `Directory.Packages.props`.
- Removed the unused `Microsoft.VisualStudio.Azure.Containers.Tools.Targets` package.
- Documentation: `AGENTS.md` is the AI instruction file, `CLAUDE.md` imports it, and durable
  README notes were kept without copying hub contracts.

## Earlier versions

Tag `0.1.1`. See `git log` and the tag; there are no release notes for it.
