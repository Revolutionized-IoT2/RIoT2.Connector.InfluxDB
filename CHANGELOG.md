# Changelog

All notable changes to `RIoT2.Connector.InfluxDB`. A version is released by pushing a git tag. CI
then builds and pushes the Docker image to GitHub Container Registry.

## [Unreleased]

## [1.1.0] - 2026-10-13

Typed configuration ([I07](https://github.com/Revolutionized-IoT2/.github/blob/main/docs/increments/phase-0-1.md#i07-typed-configuration-s)). Built on `RIoT2.Core` 1.2.0. No deployment change: the variable names
are the same.

- Changed: `InfluxConnectorOptions` reads and validates every setting at startup, before the host
  starts. A start with missing or invalid settings fails with one message that lists them all
  instead of the first one, for example `Invalid InfluxDB connector configuration:
  RIOT2_INFLUXDB_TOKEN is required; RIOT2_MQTT_IP is required.` `ConnectorConfigurationService`
  receives `IOptions<InfluxConnectorOptions>`.
- Changed: with `RIOT2_MQTT_USERNAME` unset, the connector now connects without a user name, like
  the orchestrator and the node; it used to send an empty one. Brokers that allow anonymous clients
  accept both.

## [1.0.1] - 2026-10-12

No deployment change.

- Changed the Dockerfile: the GitHub Packages token is the BuildKit secret `nuget_token` and is no
  longer stored in an image layer. The `NUGET_AUTH_TOKEN` build argument is gone.
- CI: every push runs the build, the image build and a secret scan; a tag validates before it
  pushes the image.

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
