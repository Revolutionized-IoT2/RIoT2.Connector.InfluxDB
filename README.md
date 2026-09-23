# Revolutionized-IoT2: InfluxDB Connector

The InfluxDB Connector is a tool to enable data extraction from the RIoT2 system to InfluxDB.
The data can then be visualized, for instance, by using Grafana. This tutorial covers the following steps:

1. Installing InfluxDB 2
2. Installing the Connector
3. Installing and setting up Grafana

> [!NOTE]  
> This tutorial assumes that RIoT2 is already set up and running properly.

> [!NOTE]  
> Only `Boolean` and `Number` leaf values are extracted. `Text` and `TextArray` values are currently skipped.

## How it works

Source builds require `RIoT2.Core` package `0.1.41` from the private feed. This version preserves
large integer and JSON-looking text token types when decoding MQTT reports and commands.

The connector subscribes to the RIoT2 MQTT broker and listens for `report` and `command` messages.
When the orchestrator publishes its configuration, the connector downloads the report, command, and
variable templates from the orchestrator's API (`{ApiBaseUrl}/api/nodes/report/templates`, `.../command/templates`,
`.../variable/templates`). These templates map message IDs to a device/node name, which are used to
tag the data points written to InfluxDB.

Templates become ready only after all three responses succeed and validate. Refreshes publish one
complete snapshot; a failed refresh logs an error and retains the last complete snapshot. Until the
first successful load, reports and commands are skipped with a warning. The connector reannounces
presence after reconnecting so the orchestrator can resend configuration.

Report timestamps use the report's Unix-seconds `TimeStamp`, including entity points; command
timestamps use receipt time because commands have no source timestamp. Scalar numeric fields remain
InfluxDB floating-point fields for compatibility with existing buckets, now accepting large and
scientific-notation values without narrowing to Int32. IEEE-754 precision limits still apply (integers
above 2^53 need an explicit schema migration if exact integer storage is required).

`Boolean` and `Number` values are written as a single field named `value`. `Entity` values (nested objects)
are flattened recursively, and each boolean/number leaf property becomes its own field on the same point,
named after its dot-separated path (e.g. `Temperature`, `Nested.Battery`). Non-numeric/boolean leaves
(strings, arrays, null) inside an entity, as well as top-level `Text`/`TextArray` values, are skipped.


## 1. Installing InfluxDB
The first step is to install InfluxDB 2. The recommended way is to use Docker, but any InfluxDB installation can be used.
InfluxDB is available for x86_64 and ARM64 architectures, depending on where you decide to run it.


Pull the image from the repo by running following command:

```
docker pull influxdb:2-alpine
```

start the container:
```
docker run \
 --name influxdb2 \
 --publish 8086:8086 \
 --mount type=volume,source=influxdb2-data,target=/var/lib/influxdb2 \
 --mount type=volume,source=influxdb2-config,target=/etc/influxdb2 \
 --env DOCKER_INFLUXDB_INIT_MODE=setup \
 --env DOCKER_INFLUXDB_INIT_USERNAME=ADMIN_USERNAME \
 --env DOCKER_INFLUXDB_INIT_PASSWORD=ADMIN_PASSWORD \
 --env DOCKER_INFLUXDB_INIT_ORG=ORG_NAME \
 --env DOCKER_INFLUXDB_INIT_BUCKET=BUCKET_NAME \
 influxdb:2
 ```

 After the container is running, you can connect to influx UI with your credentials.

 In UI create the token for the connector application.


## 2. Installing the connector
The second step is to install the connector. The connector will extract data directly from MQTT and push it to InfluxDB.

Pull the image from the container registry:
```
docker pull ghcr.io/revolutionized-iot2/riot2-influxdb:latest
```

Start the container with the following command. Update the environment variables according to your settings:

```
docker run -d --restart=on-failure:5 \
 --env RIOT2_MQTT_IP=192.168.0.30 \
 --env RIOT2_MQTT_PASSWORD=password \
 --env RIOT2_MQTT_USERNAME=user \
 --env RIOT2_CONNECTOR_ID=B68A6865-7B63-4EC8-AF08-3FC382C955E6 \
 --env RIOT2_HANDLE_COMMANDS=FALSE \
 --env RIOT2_INFLUXDB_HOST=http://192.168.0.34:8086 \
 --env RIOT2_INFLUXDB_TOKEN=YYY \
 --env RIOT2_INFLUXDB_BUCKET=riot-data \
 --env RIOT2_INFLUXDB_ORGANIZATION=riot-org \
 --env TZ=Europe/Helsinki \
 ghcr.io/revolutionized-iot2/riot2-influxdb:latest
```

### Environment variables

| Variable | Description |
|---|---|
| `RIOT2_MQTT_IP` | Address (host/IP) of the RIoT2 MQTT broker |
| `RIOT2_MQTT_USERNAME` | MQTT username |
| `RIOT2_MQTT_PASSWORD` | MQTT password |
| `RIOT2_CONNECTOR_ID` | Unique client/connector ID used to identify this connector on the MQTT bus |
| `RIOT2_HANDLE_COMMANDS` | `TRUE`/`FALSE` — when `TRUE`, command messages are also written to InfluxDB in addition to reports |
| `RIOT2_INFLUXDB_HOST` | URL of the InfluxDB instance, e.g. `http://192.168.0.34:8086` |
| `RIOT2_INFLUXDB_TOKEN` | InfluxDB API token with write access to the target bucket |
| `RIOT2_INFLUXDB_BUCKET` | InfluxDB bucket to write data points to |
| `RIOT2_INFLUXDB_ORGANIZATION` | InfluxDB organization name |
| `TZ` | Container timezone, e.g. `Europe/Helsinki` |

## 3. Installing and setting up Grafana
The final step is to install Grafana and set it up to visualize the data in your RIoT2 system.

Follow the instructions here to install Grafana:
https://grafana.com/docs/grafana/latest/setup-grafana/configure-docker/

Once Grafana is running, set up InfluxDB as a datasource by following the instructions here:
https://grafana.com/docs/grafana/latest/datasources/influxdb/configure-influxdb-data-source/

Visualize your data by creating dashboards/panels, following the instructions here:
https://grafana.com/docs/grafana/latest/panels-visualizations/

Each data point written by the connector uses the following schema:

```
Tags:
    message  - "report" or "command"
    device   - device name from the template
    node     - node name from the template
    id       - message/template id

Fields:
    value                - for Boolean/Number values
    <dot.separated.path> - one field per boolean/number leaf, for Entity values
```