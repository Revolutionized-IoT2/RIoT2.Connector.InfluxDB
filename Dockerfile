# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS base
WORKDIR /app
RUN apk add --upgrade --no-cache tzdata
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV ASPNETCORE_HTTP_PORTS=80
EXPOSE 80

# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
ARG BUILD_CONFIGURATION=Release
ARG NUGET_AUTH_TOKEN=token
ARG NUGET_URL=https://nuget.pkg.github.com/Revolutionized-IoT2/index.json
WORKDIR /src
COPY ["RIoT2.Connector.InfluxDB.csproj", "."]
RUN dotnet nuget add source -n github -u AZ -p $NUGET_AUTH_TOKEN --store-password-in-clear-text $NUGET_URL
RUN dotnet restore "./RIoT2.Connector.InfluxDB.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "./RIoT2.Connector.InfluxDB.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./RIoT2.Connector.InfluxDB.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RIoT2.Connector.InfluxDB.dll"]

#Set default environment variables
ENV RIOT2_MQTT_IP=192.168.0.30
ENV RIOT2_MQTT_PASSWORD=password
ENV RIOT2_MQTT_USERNAME=user
ENV RIOT2_CONNECTOR_ID=B68A6865-7B63-4EC8-AF08-3FC382C955E6
ENV RIOT2_HANDLE_COMMANDS=FALSE
ENV RIOT2_INFLUXDB_HOST=http://192.168.0.34:8086
ENV RIOT2_INFLUXDB_TOKEN=YYY
ENV RIOT2_INFLUXDB_BUCKET=riot-data
ENV RIOT2_INFLUXDB_ORGANIZATION=riot-org
ENV TZ=Europe/Helsinki