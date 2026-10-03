# RIoT2.Connector.InfluxDB

InfluxDB 2 connector for the [RIoT2](https://github.com/Revolutionized-IoT2) platform. It listens
to RIoT2 MQTT reports, optionally listens to commands, resolves template metadata from the
orchestrator, and writes time-series points for Grafana and other InfluxDB consumers.

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
timestamp. Commands have no source timestamp, so the connector stamps them when they are mapped.

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
- `RIOT2_MQTT_USERNAME` and `RIOT2_MQTT_PASSWORD` (optional for anonymous brokers)
- `RIOT2_INFLUXDB_HOST`
- `RIOT2_INFLUXDB_TOKEN`
- `RIOT2_INFLUXDB_BUCKET`
- `RIOT2_INFLUXDB_ORGANIZATION`
- `RIOT2_HANDLE_COMMANDS` (optional, `true` enables command writes)

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
  ghcr.io/revolutionized-iot2/riot2-influxdb:latest
```

The image runs as the non-root `app` user and exposes `GET /health` and `GET /healthz`. Those
endpoints confirm the process is running; they do not prove MQTT or InfluxDB delivery is healthy.
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

The writer owns one InfluxDB client for the application lifetime. MQTT callbacks enqueue points to
a bounded in-memory queue of 1000 points. A single worker writes batches of up to 100 points in
order. Failed HTTP writes are logged and not retried; there is no durable replay yet.

Durable spooling and the connector SDK are planned in
[design 7.3](https://github.com/Revolutionized-IoT2/.github/blob/main/docs/design/connector-sdk.md).

## Versions and releases

- Release notes are in [CHANGELOG.md](CHANGELOG.md).
- To release, push a tag `x.y.z` on `main`. CI publishes the Docker image to GitHub Container
  Registry.
- This repository references the `RIoT2.Core` package `1.0.1` from GitHub Packages.
- [M8](https://github.com/Revolutionized-IoT2/.github/blob/main/docs/plans/m08-dotnet10-migration.md)
  completed the target-framework migration; nullable and threading-analyzer practice steps remain
  open in the platform plan.

## Contributing

- Instructions for AI coding agents: [AGENTS.md](AGENTS.md).
- Platform documentation: [.github/docs](https://github.com/Revolutionized-IoT2/.github/blob/main/docs/README.md).

## License

See [LICENSE](LICENSE).
