using Microsoft.Extensions.Options;
using RIoT2.Connector.InfluxDB.Services;
using RIoT2.Connector.InfluxDB.Services.Interfaces;
using RIoT2.Core.Hosting;
using RIoT2.Core.Security;

var builder = WebApplication.CreateBuilder(args);

// Console and Logs/RIoT2.log, text or JSON (RIOT2_LOG_FORMAT, RIOT2_LOG_LEVEL; design 7.4), the same
// way as the other RIoT2 services.
var loggerFactory = builder.AddRIoT2Logging("connector");

// Every RIOT2_* setting is read and validated here, before anything uses it; an invalid setting
// stops the start with one message that lists every problem.
var connectorOptions = InfluxConnectorOptions.Load(builder.Configuration, loggerFactory.CreateLogger("RIoT2.Connector.InfluxDB"));
builder.Services.AddSingleton(Options.Create(connectorOptions));

// The optional security mode's seams (design 7.5, phase S0); with RIOT2_SECURITY_MODE off, the only mode
// this version accepts, nothing is enforced.
builder.Services.AddRIoT2Security(connectorOptions.Security);

// Metrics (riot2.connector.*, riot2.mqtt.*) and, with RIOT2_TRACING=true, traces over OTLP, only when
// OTEL_EXPORTER_OTLP_ENDPOINT is set (design 7.4).
builder.AddRIoT2Telemetry("connector", builder.Configuration[InfluxConnectorOptions.IdVariable], RIoT2.Core.Diagnostics.MeterNames.Connector);

// Add services to the container.
builder.Services.AddSingleton<IConnectorConfigurationService, ConnectorConfigurationService>();
builder.Services.AddSingleton<InfluxDBService>();
builder.Services.AddSingleton<IInfluxDBService>(services => services.GetRequiredService<InfluxDBService>());
// Template requests to the orchestrator send X-RIoT2-Key when RIOT2_API_KEY is set, and pass the
// outbound URL policy.
builder.Services.AddHttpClient("templates", client => client.Timeout = TimeSpan.FromSeconds(30))
    .AddHttpMessageHandler(() => new ApiKeyHandler(connectorOptions.Security.ApiKey))
    .AddHttpMessageHandler(services => new OutboundUrlPolicyHandler(services.GetRequiredService<IOutboundUrlPolicy>(), OutboundPurposes.Orchestrator));
builder.Services.AddSingleton<ITemplateService>(services =>
    new TemplateService(services.GetRequiredService<IHttpClientFactory>().CreateClient("templates")));
builder.Services.AddSingleton<IMqttMessageHandlerService, MqttMessageHandlerService>();
builder.Services.AddSingleton<IConnectorMqttService, ConnectorMqttService>();

builder.Services.AddHostedService<InfluxDBService>(services => services.GetRequiredService<InfluxDBService>());
builder.Services.AddHostedService<MqttBackgroundService>();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseRIoT2Security();

var health = () => Results.Ok(new { status = "ok" });
app.MapGet("/health", health).AllowAnonymous();
app.MapGet("/healthz", health).AllowAnonymous();

app.Run();


/* example task to write to influxdb
public Task Invoke()
{
    _service.Write(write =>
    {
        var point = PointData.Measurement("altitude")
            .Tag("plane", "test-plane")
            .Field("value", _random.Next(1000, 5000))
            .Timestamp(DateTime.UtcNow, WritePrecision.Ns);

        write.WritePoint("test-bucket", "organization", point);
    });

    return Task.CompletedTask;
*/