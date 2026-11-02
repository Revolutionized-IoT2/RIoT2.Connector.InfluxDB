# AGENTS.md — RIoT2.Connector.InfluxDB

Applies to: this repository. Read the platform guide first:
[.github/AGENTS.md](https://github.com/Revolutionized-IoT2/.github/blob/main/AGENTS.md). It covers the
workspace map, platform-wide rules and the documentation rules. In the local workspace, every
`https://github.com/Revolutionized-IoT2/<Repo>/blob/main/<path>` link is the file
`C:\Src\RIoT2\<Repo>\<path>`; read the local file instead of fetching the URL.

## What this is

An ASP.NET Core connector that writes RIoT2 reports (and optionally commands) to InfluxDB 2: the
reference connector on the connector SDK (`RIoT2.Connector.Sdk` in RIoT2.Core,
[design 7.3](https://github.com/Revolutionized-IoT2/.github/blob/main/docs/design/connector-sdk.md),
[SDK README](https://github.com/Revolutionized-IoT2/RIoT2.Core/blob/main/Connector/README.md)). The SDK
does the settings, MQTT, the orchestrator's templates, the queue, batching, retry and metrics; this
repository has the InfluxDB sink and its settings.

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
- To build the container locally, put a GitHub Packages token in `$env:NUGET_AUTH_TOKEN`, then:
  `docker build --secret id=nuget_token,env=NUGET_AUTH_TOKEN -t riot2-influxdb .\RIoT2.Connector.InfluxDB`.
  The token is a BuildKit secret; never pass it as a build argument.
- Every push runs `.github/workflows/ci.yml` (build, image build, secret scan). To release, push
  a tag `x.y.z` on `main`: `release.yml` validates, then pushes
  `ghcr.io/revolutionized-iot2/riot2-influxdb:latest` and `:<tag>` ([CI guide](https://github.com/Revolutionized-IoT2/.github/blob/main/docs/guides/ci.md)).
- To try an unreleased Core (SDK) version, pack it into `C:\Src\RIoT2\.localfeed` and restore with
  that folder as an extra NuGet source.

## Layout

| Path | Contents |
|---|---|
| `Program.cs` | `AddRIoT2Connector<InfluxSink, InfluxConnectorOptions>(...)` and `MapRIoT2Connector()` (SDK) |
| `Services/InfluxConnectorOptions.cs` | The `RIOT2_INFLUXDB_*` settings; `Load` reads them with the SDK's shared settings, as the start does |
| `Services/InfluxSink.cs` | `IConnectorSink`: maps a batch and writes it; InfluxDB's answers become retry or rejection |
| `Services/InfluxPointMapper.cs` | Report or command to an InfluxDB point |
| `Services/EntityFlattener.cs` | Recursive entity field flattening |
| `Dockerfile` | `net10.0` image, web port 8080; base images pinned by digest (Renovate updates them) |

## Contracts consumed here

- [mqtt-topics.md](https://github.com/Revolutionized-IoT2/.github/blob/main/docs/contracts/mqtt-topics.md),
  [configuration.md](https://github.com/Revolutionized-IoT2/.github/blob/main/docs/contracts/configuration.md)
  and [http-api-services.md](https://github.com/Revolutionized-IoT2/.github/blob/main/docs/contracts/http-api-services.md):
  through the SDK (`ConnectorMqttService`, `TemplateCatalog`, `/health`).
- [env-vars.md](https://github.com/Revolutionized-IoT2/.github/blob/main/docs/contracts/env-vars.md):
  `InfluxConnectorOptions` and the SDK's `ConnectorOptions`; nothing else reads the environment.
- [metrics.md](https://github.com/Revolutionized-IoT2/.github/blob/main/docs/contracts/metrics.md):
  the SDK records `riot2.connector.*`; the sink calls `ConnectorMetrics.RecordIgnored` for messages
  it doesn't write.

## Rules

- Do not publish commands to MQTT from this connector. It is a sink; connectors that write back use
  the SDK's planned `IRiot2CommandClient` and `POST /api/v2/commands`.
- Generic behaviour (queue, batching, retry, shutdown, MQTT, templates) belongs in the SDK, not
  here. This repository maps and writes.
- The sink throws `TransientSinkException` for anything that may pass (network, timeouts, 401,
  403, 404, 429, 5xx, templates not loaded) and `SinkRejectedException` only when InfluxDB refuses
  the data (400, 413, 422). A wrong exception type either loses data or stalls the queue.
- The line protocol is a contract: RIoT2.Tests `GoldenInfluxTests` pins it for every golden message
  (`Golden/influx-line-protocol.txt`). Commands use `ConnectorMessage.ReceivedUtc`.
- Only write boolean, numeric and entity boolean/numeric leaf values. Top-level text/text-array
  values and non-numeric entity leaves are skipped.
- Reference Core only through `RIoT2.Connector.Sdk` (`1.20.0`, set in `Directory.Packages.props`);
  don't add the `RIoT2.Core` facade or `RIoT2.Core.Node`. Keep `PackageReference` items versionless.
- Do not commit or document real InfluxDB tokens or MQTT credentials.
- Logging and metrics come from the SDK (`AddRIoT2Logging`, `AddRIoT2Telemetry`): don't add Serilog
  sinks, logging providers or `Logging` sections to `appsettings.json`.

## Pitfalls

- `RIOT2_HANDLE_COMMANDS` uses `bool.TryParse`; only values such as `true` enable command writes.
- The connector announces itself as `InfluxDBConnector` with `NodeType` `Unknown`;
  `NodeType.Connector` comes with design 7.3 phase 3.
- The queue holds 1000 messages in memory: a long outage refuses new ones, and a restart loses what
  is queued, until the spool (design 7.3 phase 2, I38).
- The health endpoints only prove the process is running; they don't check MQTT, the templates or
  InfluxDB.

## Related work

- [Backlog item 2](https://github.com/Revolutionized-IoT2/.github/blob/main/docs/backlog/open-issues.md):
  the remaining outage loss, fixed by the spool (I38).
- [M7](https://github.com/Revolutionized-IoT2/.github/blob/main/docs/plans/m07-contract-integration-tests.md):
  golden messages; the connector isn't part of the end-to-end tests yet.
- [Design 7.1](https://github.com/Revolutionized-IoT2/.github/blob/main/docs/design/reliable-delivery.md):
  the command API future bidirectional connectors use.
- [Design 7.3](https://github.com/Revolutionized-IoT2/.github/blob/main/docs/design/connector-sdk.md):
  the connector SDK.
