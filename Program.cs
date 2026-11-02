using RIoT2.Connector.InfluxDB.Services;
using RIoT2.Connector.Sdk;

var builder = WebApplication.CreateBuilder(args);

// The connector SDK (design 7.3): every RIOT2_* setting validated at once, logging, telemetry, the security
// seams, MQTT, the orchestrator's templates, the queue with batching, retry and the disk spool, and /health.
// InfluxSink writes the batches. RIOT2_CONNECTOR_NAME replaces the name shown in the UI.
builder.AddRIoT2Connector<InfluxSink, InfluxConnectorOptions>(InfluxConnectorOptions.Component, InfluxConnectorOptions.Read,
    options => options.Name = "InfluxDB");

var app = builder.Build();
app.MapRIoT2Connector();
app.Run();
