# AGENTS.md — RIoT2.Connector.InfluxDB

Applies to: this repository. Read the platform guide first:
[.github/AGENTS.md](https://github.com/Revolutionized-IoT2/.github/blob/main/AGENTS.md). It covers the
workspace map, platform-wide rules and the documentation rules. In the local workspace, every
`https://github.com/Revolutionized-IoT2/<Repo>/blob/main/<path>` link is the file
`C:\Src\RIoT2\<Repo>\<path>`; read the local file instead of fetching the URL.

## What this is

An ASP.NET Core connector that subscribes to RIoT2 MQTT reports and optionally commands, maps them
through orchestrator templates, and writes numeric, boolean and entity leaf values to InfluxDB 2.
It is the current reference connector until the planned connector SDK exists.

## Commands

Run from the workspace root (`C:\Src\RIoT2`), in PowerShell:

```powershell
dotnet restore .\RIoT2.Connector.InfluxDB\RIoT2.Connector.InfluxDB.sln
dotnet build .\RIoT2.Connector.InfluxDB\RIoT2.Connector.InfluxDB.csproj
dotnet test .\RIoT2.Tests\RIoT2.Tests.csproj
dotnet run --project .\RIoT2.Connector.InfluxDB\RIoT2.Connector.InfluxDB.csproj
```

- The run command requires the connector environment variables from
  [env-vars.md](https://github.com/Revolutionized-IoT2/.github/blob/main/docs/contracts/env-vars.md).
  Do not copy values from `Properties/launchSettings.json`.
- To build the container locally:
  `docker build -t riot2-influxdb .\RIoT2.Connector.InfluxDB --build-arg NUGET_AUTH_TOKEN=<github-packages-token>`.
- To release, push a tag `x.y.z` on `main`. CI (`.github/workflows/main.yml`) builds and pushes
  `ghcr.io/revolutionized-iot2/riot2-influxdb:latest` and `:<tag>`.

## Layout

| Path | Contents |
|---|---|
| `Program.cs` | Service registrations and `/health`, `/healthz` endpoints |
| `Models/` | Connector configuration and template snapshot models |
| `Services/ConnectorConfigurationService.cs` | Environment variable loading and validation |
| `Services/ConnectorMqttService.cs` | MQTT subscriptions, online announcement and message dispatch |
| `Services/TemplateService.cs` | Orchestrator template catalog loading |
| `Services/MqttMessageHandlerService.cs` | Report/command to InfluxDB point mapping |
| `Services/EntityFlattener.cs` | Recursive entity field flattening |
| `Services/InfluxDBService.cs` | Bounded in-memory queue and batch writer |
| `Dockerfile` | `net8.0` image, web port 8080 |

## Contracts consumed here

- [mqtt-topics.md](https://github.com/Revolutionized-IoT2/.github/blob/main/docs/contracts/mqtt-topics.md):
  `ConnectorMqttService` subscribes to `riot2/node/+/report`, optionally
  `riot2/node/+/command`, connector configuration, and orchestrator online messages.
- [configuration.md](https://github.com/Revolutionized-IoT2/.github/blob/main/docs/contracts/configuration.md):
  `TemplateService` loads report, command and variable templates from the orchestrator before
  mapping messages.
- [http-api.md](https://github.com/Revolutionized-IoT2/.github/blob/main/docs/contracts/http-api.md):
  `TemplateService` consumes `/api/nodes/report/templates`, `/api/nodes/command/templates` and
  `/api/nodes/variable/templates`; `Program.cs` serves `/health` and `/healthz`.
- [env-vars.md](https://github.com/Revolutionized-IoT2/.github/blob/main/docs/contracts/env-vars.md):
  `ConnectorConfigurationService` defines the connector and InfluxDB variables.

## Rules

- Do not publish commands directly to MQTT from this connector. It is a sink; bidirectional
  connector work belongs to the planned SDK and command API.
- Do not invent retries or durable replay without following [design 7.3](https://github.com/Revolutionized-IoT2/.github/blob/main/docs/design/connector-sdk.md).
  The current writer has an explicit volatile delivery policy.
- Keep writes non-blocking from MQTT callbacks. MQTT handlers enqueue points and let
  `InfluxDBService` own batching and HTTP writes.
- Keep the template snapshot atomic: reports/commands/variables become visible together only after
  all three HTTP responses succeed and validate.
- Only write boolean, numeric and entity boolean/numeric leaf values. Top-level text/text-array
  values and non-numeric entity leaves are skipped.
- Keep `RIoT2.Core` as a package reference. This repository currently pins `RIoT2.Core` `0.1.41`.
- Do not commit or document real InfluxDB tokens or MQTT credentials.

## Pitfalls

- `RIOT2_HANDLE_COMMANDS` uses `bool.TryParse`; only values such as `true` enable command writes.
  Anything else means false.
- The connector announces itself with `Name = "InfluxDBConnector"` and leaves `NodeType` as the
  Core default (`Unknown`). Planned `NodeType.Connector` does not exist yet.
- Reports use the report's Unix-seconds timestamp. Commands have no source timestamp and are
  stamped at receipt/mapping time.
- The queue is bounded to 1000 points and batches up to 100. Failed HTTP writes are logged and not
  retried; shutdown can abandon queued or in-flight points.
- The health endpoints only prove the ASP.NET Core process is running; they do not prove MQTT,
  template loading or InfluxDB delivery.
- This project still targets `net8.0`, which reaches end of support on 10 November 2026. Plan
  [M8](https://github.com/Revolutionized-IoT2/.github/blob/main/docs/plans/m08-dotnet10-migration.md)
  covers the move to `net10.0`.
- `RIoT2.Core` `0.1.41` has no tag in `RIoT2.Core`; only `0.1.39`, `0.1.43` and `0.1.44` are
  tagged around it. Treat this as maintainer action
  [MA2](https://github.com/Revolutionized-IoT2/.github/blob/main/docs/backlog/README.md#ma2-cut-a-core-release-and-align-all-consumers).

## Related work

- [Backlog item 2](https://github.com/Revolutionized-IoT2/.github/blob/main/docs/backlog/open-issues.md):
  Influx writes are lost on outage until a durable spool exists.
- [Backlog item 14](https://github.com/Revolutionized-IoT2/.github/blob/main/docs/backlog/open-issues.md):
  typed validated configuration.
- [Backlog item 17](https://github.com/Revolutionized-IoT2/.github/blob/main/docs/backlog/open-issues.md):
  cross-repository contract and integration tests.
- [M4](https://github.com/Revolutionized-IoT2/.github/blob/main/docs/plans/m04-typed-configuration.md):
  common options and validation.
- [M7](https://github.com/Revolutionized-IoT2/.github/blob/main/docs/plans/m07-contract-integration-tests.md):
  golden messages and in-process integration harness.
- [M8](https://github.com/Revolutionized-IoT2/.github/blob/main/docs/plans/m08-dotnet10-migration.md):
  move this `net8.0` service and image to `net10.0`.
- [Design 7.1](https://github.com/Revolutionized-IoT2/.github/blob/main/docs/design/reliable-delivery.md):
  command API and delivery semantics that future bidirectional connectors should use.
- [Design 7.3](https://github.com/Revolutionized-IoT2/.github/blob/main/docs/design/connector-sdk.md):
  connector SDK; this repository becomes the reference connector.
