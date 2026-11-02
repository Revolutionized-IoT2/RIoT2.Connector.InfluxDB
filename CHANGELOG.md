# Changelog

All notable changes to `RIoT2.Connector.InfluxDB`. A version is released by pushing a git tag. CI
then builds and pushes the Docker image to GitHub Container Registry.

## [Unreleased]

## [1.9.0] - 2026-11-02

The disk spool ([I38](https://github.com/Revolutionized-IoT2/.github/blob/main/docs/increments/phase-3-4.md#i38-connector-spool-m), [design 7.3](https://github.com/Revolutionized-IoT2/.github/blob/main/docs/design/connector-sdk.md) phase 2), with
RIoT2.Connector.Sdk 1.21.0. No port or user changes; the line protocol is unchanged.

- Added: while InfluxDB is down, messages wait in `/app/Data/spool` and are written in order once it
  answers, also after a restart. `RIOT2_CONNECTOR_SPOOL` (default `true`),
  `RIOT2_CONNECTOR_SPOOL_MAX_MB` (100) and `RIOT2_CONNECTOR_SPOOL_MAX_AGE_DAYS` (7); beyond the limits
  the oldest are dropped. Stopping spools what waits in memory.
- Added: batches InfluxDB refuses go to `/app/Data/spool/dead-letter.jsonl` with the error.
- Changed: `/health` reports MQTT, InfluxDB (`/ping`) and the spool, and answers 503 while MQTT is
  disconnected. `/healthz` is unchanged.
- Added: `riot2.connector.spool_depth`, `riot2.connector.spool_bytes`,
  `riot2.connector.spool_dropped`.
- Build: the image creates `/app/Data/spool` for the `app` user; mount `/app/Data` as a volume
  (compose: `influxdb-connector-data`).

## [1.8.0] - 2026-11-02

The connector SDK ([I37](https://github.com/Revolutionized-IoT2/.github/blob/main/docs/increments/phase-3-4.md#i37-connector-sdk-m), [design 7.3](https://github.com/Revolutionized-IoT2/.github/blob/main/docs/design/connector-sdk.md) phase 1),
with RIoT2.Connector.Sdk 1.20.0. No port, user, volume or variable changes; the line protocol is
unchanged.

- Changed: the generic code (settings, MQTT, templates, queue, metrics) moved into the SDK.
  `Program.cs` is `AddRIoT2Connector<InfluxSink, InfluxConnectorOptions>(...)`; `InfluxSink` maps
  and writes, with `InfluxPointMapper`. `InfluxConnectorOptions` holds only the `RIOT2_INFLUXDB_*`
  settings.
- Changed: writes are batched (up to 100, or after 1 s). A failed write is retried with backoff
  instead of dropped (400, 413 and 422 are dropped), and reports received before the templates
  are written once they arrive instead of being skipped. A message that can't be mapped (no value,
  a time stamp out of range) is skipped and counted `ignored`; the rest of its batch is written.
- Changed: commands are stamped with the time they were received, not when they were mapped.
- Changed: `riot2.connector.writes` counts messages by `written`, `retried`, `rejected` and
  `discarded` (it counted points by `written`, `failed` and `discarded`); `ignored` messages are
  counted when the sink skips them.

## [1.7.0] - 2026-11-02

The optional security mode's seams ([I34](https://github.com/Revolutionized-IoT2/.github/blob/main/docs/increments/phase-2.md#i34-security-seams-with-the-mode-fixed-at-off-m), [design 7.5](https://github.com/Revolutionized-IoT2/.github/blob/main/docs/design/security-mode.md) phase S0), with RIoT2.Core 1.18.0. No port, user,
volume or JSON changes; with the default mode `off` nothing is enforced.

- Added: `RIOT2_SECURITY_MODE` (only `off` is accepted; `audit` or `on` stops the start),
  `RIOT2_SECURITY_FEATURES` and `RIOT2_API_KEY`, sent in `X-RIoT2-Key` when the templates are
  loaded from the orchestrator. `/health` and `/healthz` stay anonymous.
- Added: `RIOT2_MQTT_TLS` and `RIOT2_MQTT_CA_FILE` connect to a TLS broker.
- Changed: the template client passes the outbound URL policy (allow-all while `off`).

## [1.6.3] - 2026-11-02

Supply chain ([I33](https://github.com/Revolutionized-IoT2/.github/blob/main/docs/increments/phase-2.md#i33-supply-chain-s)). No port, user, volume, variable or JSON changes.

- Build: the Dockerfile's base images are pinned by digest (`<image>:<tag>@sha256:...`), rebuilt on
  the current .NET 10 / base image patches. Renovate updates the digests.
- The image carries an SPDX SBOM and a provenance attestation; the tag now names an image index.
  `docker pull` and compose are unaffected.
- Build: `renovate.json` extends the shared Renovate preset ([I33](https://github.com/Revolutionized-IoT2/.github/blob/main/docs/increments/phase-2.md#i33-supply-chain-s)). Renovate opens grouped
  update pull requests once the app is installed (MA7).

## [1.6.2] - 2026-11-02

With RIoT2.Core 1.17.0. No port, user, volume, variable or JSON changes.

- Build: nullable reference types ([I32](https://github.com/Revolutionized-IoT2/.github/blob/main/docs/increments/phase-2.md#i32-nullable-reference-types-m)) are enabled, and CI fails on a nullable warning.
  Serializer-populated models (`Models/`) turn off CS8618 in a nested `.editorconfig`. No behaviour
  change.
- Build: the threading analyzers ([I31](https://github.com/Revolutionized-IoT2/.github/blob/main/docs/increments/phase-2.md#i31-async-cleanup-part-2-and-threading-analyzers-m)) run in every project from the shared
  `Directory.Build.props` and `.editorconfig`: a blocking wait, `async void` or an unobserved task
  fails the CI build. No change to the shipped binaries.

## [1.6.1] - 2026-11-01

- Fixed: the image starts again (1.6.0 failed with `FileNotFoundException` for
  `Microsoft.Bcl.AsyncInterfaces`): built with RIoT2.Core 1.14.1, whose `RIoT2.Core.Mqtt` declares
  that dependency ([I28](https://github.com/Revolutionized-IoT2/.github/blob/main/docs/increments/phase-2.md#i28-narrow-references-and-generated-models-m)). Don't run 1.6.0.

## [1.6.0] - 2026-11-01

Narrow Core references ([I28](https://github.com/Revolutionized-IoT2/.github/blob/main/docs/increments/phase-2.md#i28-narrow-references-and-generated-models-m), plan M1 step 4), with RIoT2.Core 1.14.0. No port, user, volume,
variable or JSON changes.

- Changed: references `RIoT2.Core.Contracts`, `RIoT2.Core.Mqtt` and `RIoT2.Core.Hosting` instead of
  the `RIoT2.Core` facade, so the image no longer carries the device SDK, the node runtime or Quartz.

## [1.5.0] - 2026-11-01

System.Text.Json on the wire ([I25](https://github.com/Revolutionized-IoT2/.github/blob/main/docs/increments/phase-2.md#i25-systemtextjson-on-the-wire-m)), with RIoT2.Core 1.11.0. No port, user, volume or
variable changes; the reports, templates and line protocol are the same (the InfluxDB golden
tests pass unchanged).

## [1.4.0] - 2026-11-01

Metrics ([I22](https://github.com/Revolutionized-IoT2/.github/blob/main/docs/increments/phase-2.md#i22-metrics-m), design 7.4 phase 4), with RIoT2.Core 1.10.0.
No port, user or volume changes.

- Added: the meter `RIoT2.Connector`: `riot2.connector.messages` (reports and commands, by type and
  result: queued, ignored, invalid, failed), `riot2.connector.writes` (points, by outcome: written,
  failed, discarded) and `riot2.connector.queue_depth`. The spool metrics come with the spool.
- Added: `OTEL_EXPORTER_OTLP_ENDPOINT` exports them, with `riot2.mqtt.*`, ASP.NET Core and runtime
  metrics, over OTLP; `RIOT2_TRACING=true` adds traces. Unset, nothing is exported.

## [1.3.0] - 2026-11-01

Consistent logging ([I20](https://github.com/Revolutionized-IoT2/.github/blob/main/docs/increments/phase-2.md#i20-consistent-logging-s), design 7.4 phase 2), with RIoT2.Core 1.9.0.

- Changed: the connector logs through `AddRIoT2Logging()` (RIoT2.Core.Hosting) like the other
  services: Serilog to the console and to `Logs/RIoT2.log` (`/app/Logs`; the image creates
  it), rolled daily and at 50 MB, 14 files kept, text or JSON (`RIOT2_LOG_FORMAT`), at
  `RIOT2_LOG_LEVEL`. Before, it logged to the console only. An invalid setting is now also
  logged as critical.
- Removed: the `Logging` sections of `appsettings.json`.

## [1.2.0] - 2026-10-13

MQTT client robustness ([I08](https://github.com/Revolutionized-IoT2/.github/blob/main/docs/increments/phase-0-1.md#i08-mqtt-client-robustness-m)). Built on `RIoT2.Core` 1.3.0. No deployment change is needed.

- Changed: the connector publishes and subscribes at QoS 1, keeps at most 10,000 outgoing messages
  while the broker is unreachable (the oldest are dropped and logged), and logs a failing MQTT
  handler instead of losing it.
- Added `RIOT2_MQTT_PORT` (default 1883) for a broker on another port.
- Changed: MQTT messages are handled in arrival order, so the templates from a configuration
  message are loaded before the next report is mapped; they used to be handled in parallel
  background tasks.

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
