# Changelog

All notable changes to `RIoT2.Connector.InfluxDB`. A version is released by pushing a git tag. CI
then builds and pushes the Docker image to GitHub Container Registry.

## [Unreleased]

- Changed target framework and Docker base images to .NET 10 (`aspnet:10.0-alpine` and
  `sdk:10.0-alpine`).
- Changed the Core dependency to `RIoT2.Core` 0.1.45 and moved package versions to
  `Directory.Packages.props`.
- Removed the unused `Microsoft.VisualStudio.Azure.Containers.Tools.Targets` package.
- Documentation: `AGENTS.md` is the AI instruction file, `CLAUDE.md` imports it, and durable
  README notes were kept without copying hub contracts.

## Earlier versions

Tag `0.1.1`. See `git log` and the tag; there are no release notes for it.
