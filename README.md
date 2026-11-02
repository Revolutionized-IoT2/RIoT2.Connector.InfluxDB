# RIoT2.Connector.InfluxDB

InfluxDB 2 connector for the [RIoT2](https://github.com/Revolutionized-IoT2) platform. It listens
to RIoT2 MQTT reports, optionally listens to commands, resolves template metadata from the
orchestrator, and writes time-series points for Grafana and other InfluxDB consumers. It is the
reference connector on the [connector SDK](https://github.com/Revolutionized-IoT2/RIoT2.Core/blob/main/Connector/README.md) (Core 1.21.0, [design 7.3](https://github.com/Revolutionized-IoT2/.github/blob/main/docs/design/connector-sdk.md)): the SDK
does the settings, MQTT, templates, queue and disk spool; this repository has the InfluxDB sink.

- Type: ASP.NET Core service
- Target framework: .NET 10
- Image: `ghcr.io/revolutionized-iot2/riot2-influxdb`
- Root namespace: `RIoT2.Connector.InfluxDB`

How connectors fit into the platform: [architecture overview](https://github.com/Revolutionized-IoT2/.github/blob/main/docs/architecture/overview.md).

## What it writes

The connector writes these values:

- Boolean and number reports.
- Boolean and number commands when `RIOT2_HANDLE_COMMANDS=true`.
- Entity reports or commands after recursively flattening boolean and number leaves.

Text, text arrays, nulls and non-numeric entity leaves are skipped. Reports use their source Unix
timestamp. Commands have no source timestamp, so they get the time the connector received them.

Each point uses the template name as the measurement and these tags:

| Tag | Value |
| --- | --- |
| `message` | `report` or `command` |
| `device` | Device name from the template |
| `node` | Node name from the template |
| `id` | Report, command or variable template id |

Scalar values are written as field `value`. Entity leaf fields use their dot-separated path.

## Configuration

Set these variables:

- `RIOT2_CONNECTOR_ID`
- `RIOT2_MQTT_IP`
- `RIOT2_MQTT_USERNAME` and `RIOT2_MQTT_PASSWORD` (optional for anonymous brokers);
  `RIOT2_MQTT_TLS` and `RIOT2_MQTT_CA_FILE` (optional, TLS to the broker)
- `RIOT2_SECURITY_MODE` (optional, only `off` yet) and `RIOT2_API_KEY` (optional, sent to the
  orchestrator): the optional security mode ([design 7.5](https://github.com/Revolutionized-IoT2/.github/blob/main/docs/design/security-mode.md))
- `RIOT2_INFLUXDB_HOST`
- `RIOT2_INFLUXDB_TOKEN`
- `RIOT2_INFLUXDB_BUCKET`
- `RIOT2_INFLUXDB_ORGANIZATION`
- `RIOT2_HANDLE_COMMANDS` (optional, `true` enables command writes)
- `RIOT2_CONNECTOR_SPOOL` (optional, default `true`), `RIOT2_CONNECTOR_SPOOL_MAX_MB` (optional,
  default 100) and `RIOT2_CONNECTOR_SPOOL_MAX_AGE_DAYS` (optional, default 7): the disk spool, see
  [Delivery behaviour](#delivery-behaviour)
- `RIOT2_LOG_FORMAT` (optional, `text` or `json`) and `RIOT2_LOG_LEVEL` (optional, default
  `Information`)
- `OTEL_EXPORTER_OTLP_ENDPOINT` (optional; the OTLP collector for metrics, such as
  `http://otel-collector:4317`) and `RIOT2_TRACING` (optional, `true` also exports traces)

Logs go to the console and to `Logs/RIoT2.log` (`/app/Logs` in the container), rolled daily and at
50 MB, 14 files kept, through the SDK (`AddRIoT2Logging()` from `RIoT2.Core.Hosting`).

Metrics go out through `AddRIoT2Telemetry()` when the endpoint is set: `riot2.connector.messages`
(by type and result), `riot2.connector.writes` (messages, by outcome), `riot2.connector.queue_depth`,
`riot2.connector.spool_depth`, `riot2.connector.spool_bytes`, `riot2.connector.spool_dropped`,
`riot2.mqtt.*`, ASP.NET Core and the runtime. Every name is in the
[metrics contract](https://github.com/Revolutionized-IoT2/.github/blob/main/docs/contracts/metrics.md).

The full environment contract, ports and image details are in
[env-vars.md](https://github.com/Revolutionized-IoT2/.github/blob/main/docs/contracts/env-vars.md).
MQTT topics and payloads are in
[mqtt-topics.md](https://github.com/Revolutionized-IoT2/.github/blob/main/docs/contracts/mqtt-topics.md).

## Build, test and run

From the workspace root (`C:\Src\RIoT2`):

```powershell
dotnet restore .\RIoT2.Connector.InfluxDB\RIoT2.Connector.InfluxDB.sln
dotnet build .\RIoT2.Connector.InfluxDB\RIoT2.Connector.InfluxDB.csproj
dotnet test .\RIoT2.Tests\RIoT2.Tests.csproj
dotnet run --project .\RIoT2.Connector.InfluxDB\RIoT2.Connector.InfluxDB.csproj
```

Add `-p:CI=true` to `dotnet build` to reproduce CI analyzer settings locally. Package versions are
centralized in `Directory.Packages.props`; `PackageReference` items do not carry versions.

The test suite lives in [RIoT2.Tests](https://github.com/Revolutionized-IoT2/RIoT2.Tests), which
project-references this connector and the orchestrator.

## Docker

Run the published image:

```powershell
docker run -d --restart=on-failure:5 `
  --publish 8080:8080 `
  --env RIOT2_MQTT_IP=192.168.0.30 `
  --env RIOT2_MQTT_USERNAME=user `
  --env RIOT2_MQTT_PASSWORD=<mqtt-password> `
  --env RIOT2_CONNECTOR_ID=<connector-id> `
  --env RIOT2_HANDLE_COMMANDS=false `
  --env RIOT2_INFLUXDB_HOST=http://<influx-host>:8086 `
  --env RIOT2_INFLUXDB_TOKEN=<influx-token> `
  --env RIOT2_INFLUXDB_BUCKET=riot-data `
  --env RIOT2_INFLUXDB_ORGANIZATION=riot-org `
  --mount type=volume,source=riot2-influxdb-data,target=/app/Data `
  ghcr.io/revolutionized-iot2/riot2-influxdb:latest
```

The image runs as the non-root `app` user. Mount `/app/Data` so the spool survives a recreated
container. `GET /health` reports MQTT, InfluxDB (its `/ping`) and the spool, and answers 503 while
MQTT is disconnected; `GET /healthz` only confirms the process is running
([http-api-services.md](https://github.com/Revolutionized-IoT2/.github/blob/main/docs/contracts/http-api-services.md#influxdb-connector)).
The Dockerfile uses `mcr.microsoft.com/dotnet/aspnet:10.0-alpine` for runtime and
`mcr.microsoft.com/dotnet/sdk:10.0-alpine` for build.

## InfluxDB and Grafana

Use any InfluxDB 2 deployment that the connector can reach. A minimal Docker setup (x86_64 or
ARM64):

```bash
docker run -d --name influxdb2 -p 8086:8086 \
  --mount type=volume,source=influxdb2-data,target=/var/lib/influxdb2 \
  --mount type=volume,source=influxdb2-config,target=/etc/influxdb2 \
  --env DOCKER_INFLUXDB_INIT_MODE=setup \
  --env DOCKER_INFLUXDB_INIT_USERNAME=<admin-user> \
  --env DOCKER_INFLUXDB_INIT_PASSWORD=<admin-password> \
  --env DOCKER_INFLUXDB_INIT_ORG=<org> \
  --env DOCKER_INFLUXDB_INIT_BUCKET=<bucket> \
  influxdb:2
```

Then open the InfluxDB UI on port 8086 and create a token with write access to the bucket. Pass
it as `RIOT2_INFLUXDB_TOKEN`. For Grafana, follow the official documentation:

- [InfluxDB Docker guide](https://docs.influxdata.com/influxdb/v2/install/use-docker-compose/)
- [Grafana Docker setup](https://grafana.com/docs/grafana/latest/setup-grafana/configure-docker/)
- [Grafana InfluxDB data source](https://grafana.com/docs/grafana/latest/datasources/influxdb/configure-influxdb-data-source/)

## Delivery behaviour

The SDK queues each readable message as received and hands `InfluxSink` batches of up to 100, in
order, after at most 1 s. The sink maps them (`InfluxPointMapper`) and writes them with one
InfluxDB client for the application lifetime.

- InfluxDB down, slow, 401/403/404, 429 or 5xx: the batch is retried, 1 s doubling to 60 s. With
  the spool (the default), the batch and everything waiting move to `/app/Data/spool`, new messages
  follow them there, and all are written in order once InfluxDB answers, also after a restart.
  Beyond `RIOT2_CONNECTOR_SPOOL_MAX_MB` or `RIOT2_CONNECTOR_SPOOL_MAX_AGE_DAYS` the oldest are
  dropped (`riot2.connector.spool_dropped`). Without the spool, new messages wait in memory and
  beyond 1000 are refused (`riot2.connector.messages`, result `failed`).
- 400, 413 or 422: InfluxDB refuses the data; the batch goes to
  `/app/Data/spool/dead-letter.jsonl` with the error (logged and dropped without the spool).
- A message that can't be mapped is skipped and counted `ignored`; the rest of its batch is written.
- Before the orchestrator's templates arrive, messages wait for them.
- Stopping finishes the write in flight (up to 10 s) and spools what is left; without the spool it
  writes what is queued for up to 10 s and discards the rest.
- A batch interrupted by stopping is written again at the next start, so InfluxDB may get it twice;
  the same point overwrites itself.

## Versions and releases

- Release notes are in [CHANGELOG.md](CHANGELOG.md).
- To release, push a tag `x.y.z` on `main`. The release workflow validates, then publishes the
  Docker image to GitHub Container Registry ([CI guide](https://github.com/Revolutionized-IoT2/.github/blob/main/docs/guides/ci.md)).
- This repository references `RIoT2.Connector.Sdk` `1.21.0` from GitHub Packages, which brings
  the contract, the MQTT client and the hosting, without the device SDK, the node runtime or Quartz.
- [M8](https://github.com/Revolutionized-IoT2/.github/blob/main/docs/plans/m08-dotnet10-migration.md)
  completed the target-framework migration; nullable reference types (I32) and the threading
  analyzers (I31) fail CI on a warning.

## Contributing

- Instructions for AI coding agents: [AGENTS.md](AGENTS.md).
- Platform documentation: [.github/docs](https://github.com/Revolutionized-IoT2/.github/blob/main/docs/README.md).

## License

See [LICENSE](LICENSE).
