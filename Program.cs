using RIoT2.Connector.InfluxDB.Services;
using RIoT2.Connector.Sdk;

var builder = WebApplication.CreateBuilder(args);

// The connector SDK (design 7.3): every RIOT2_* setting validated at once, logging, telemetry, the security
// seams, MQTT, the orchestrator's templates and the queue with batching and retry. InfluxSink writes the batches.
builder.AddRIoT2Connector<InfluxSink, InfluxConnectorOptions>(InfluxConnectorOptions.Component, InfluxConnectorOptions.Read,
    options => options.Name = "InfluxDBConnector");

var app = builder.Build();
app.MapRIoT2Connector();
app.Run();
