# Revolutionized-IoT2: InfluxDB Connector

The InfluxDB Connector ia a tool to enable data extraction from RIoT-system to InfluxDB. 
The data can then be visualized by using the Grafana. This tutorial covers following steps:

1. Install InfluxDB 2
2. Install the connector
3. Install and setup Grafana

> [!NOTE]  
> This tutorial assumes that RIoT2 is already setup and is running properly 

> [!NOTE]  
> Currently the connector extracts only number and boolean data


## 1. Installing InfluxDB 3
The first step is to install InfluxDB 3. The recommended way is to use Docker, but any Influx intallation can be used.
InfluxDB is available for x86_64 and ARM64 architectures, debending on where you deceide to run it.


First, Pull the image from the repo by running following command:

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

 After this you can connect to influx UI with your credentials. 

 In UI create the token for the connector application.


## 2. Installing the connector
The second step is installing the connector that will connet directly to RIoT2 MQTT and will listen any Report sent by a Node and send reported data to InfluxDB. 

Pull image from container
 ```
 todo
 ```

 Start the container with following command. Update environment variables to according your settings:

  ```
 todo
 ```

## 3. Installing and settting up Grafana
The final step is to install Grafana and set it up to visualize the data in your RIoT2 -system

https://grafana.com/docs/grafana/latest/setup-grafana/configure-docker/


Pull image from the container:
```
docker run -d -p 3000:3000 --name=grafana \
  --volume grafana-storage:/var/lib/grafana \
  grafana/grafana-enterprise
```

Start the container:
 ```
 todo
 ```

Update Grafana settings


Connect to UI


Add InfluxDB as datasource to grafana:

https://docs.influxdata.com/influxdb3/core/visualize-data/grafana/#influxdb-data-source

https://grafana.com/docs/grafana/latest/datasources/#add-a-data-source

Visualize your data

 ```
 todo -> Datamodel created by the connector
 ```




 -------------------------------

```
##Authentication #  
allow_anonymous false  
password_file /mosquitto/config/password.txt  
  
##Listeners #  
listener 1883 192.168.0.30  
listener 9001 192.168.0.30  
protocol websockets  
```

> [!NOTE]  
> The websocket protocol is required for the UI.

A good guide for setting up Mosquitto broker with Docker => https://github.com/sukesh-ak/setup-mosquitto-with-docker/blob/main/README.md

### 2. Setting up the Orchestrator
Build (or pull) the orchestrator container and set it up:
```
docker pull ghcr.io/revolutionized-iot2/riot2-orchestrator:latest
```

Set the following container environment parameters: 
- RIOT2_MQTT_IP - IP address for MQTT server  
- RIOT2_MQTT_PASSWORD - MQTT password set in password.txt  
- RIOT2_MQTT_USERNAME - MQTT username set in password.txt  
- RIOT2_ORCHESTRATOR_ID - Unique ID for Orchestrator across the whole system. GUID is recommended 
- RIOT2_ORCHESTRATOR_URL - Orchestrator endpoint URL. E.g. http://192.168.0.32
- TZ - Timezone for Orchestrator. E.g. Europe/Helsinki  
 