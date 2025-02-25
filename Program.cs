using RIoT2.Connector.InfluxDB.Services;
using RIoT2.Connector.InfluxDB.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSingleton<IConnectorConfigurationService, ConnectorConfigurationService>();
builder.Services.AddSingleton<IInfluxDBService, InfluxDBService>();
builder.Services.AddSingleton<ITemplateService, TemplateService>();
builder.Services.AddSingleton<IMqttMessageHandlerService, MqttMessageHandlerService>();
builder.Services.AddSingleton<IConnectorMqttService, ConnectorMqttService>();

builder.Services.AddHostedService<MqttBackgroundService>();

var app = builder.Build();

// Configure the HTTP request pipeline.


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