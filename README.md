# Revolutionized-IoT2: InfluxDB Connector

The InfluxDB Connector ia a tool to enable data extraction from RIoT-system to InfluxDB. 
The data can then be visualized for instace by using the Grafana. This tutorial covers following steps:

1. Installing the InfluxDB 2
2. Installing the Connector
3. Installing and setting Grafana

> [!NOTE]  
> This tutorial assumes that RIoT2 is already setup and is running properly 

> [!NOTE]  
> Currently the connector extracts only number and boolean data. Entity data type is to be added later.


## 1. Installing InfluxDB
The first step is to install InfluxDB 2. The recommended way is to use Docker, but of course any Influx intallation can be used.
InfluxDB is available for x86_64 and ARM64 architectures, debending on where you deceide to run it.


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
The second step is to install the connector. The connector that will will extract data directly from the MQTT and push it to InfluxDB. 

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

You follow instructions from here:
https://grafana.com/docs/grafana/latest/setup-grafana/configure-docker/

Once Grafana is running, setup influxDB as datasource by following instructions from here:
https://grafana.com/docs/grafana/latest/datasources/influxdb/configure-influxdb-data-source/


Visualize your data by following instructions:
https://grafana.com/docs/grafana/latest/datasources/influxdb/configure-influxdb-data-source/


```
todo: riot2 datamodel 
```