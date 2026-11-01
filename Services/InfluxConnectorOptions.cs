using RIoT2.Core.Configuration;

namespace RIoT2.Connector.InfluxDB.Services
{
    /// <summary>
    /// The connector's settings. Every <c>RIOT2_*</c> name it reads is mapped here (plan M4).
    /// </summary>
    public sealed class InfluxConnectorOptions
    {
        public const string IdVariable = "RIOT2_CONNECTOR_ID";
        public const string HostVariable = "RIOT2_INFLUXDB_HOST";
        public const string TokenVariable = "RIOT2_INFLUXDB_TOKEN";
        public const string BucketVariable = "RIOT2_INFLUXDB_BUCKET";
        public const string OrganizationVariable = "RIOT2_INFLUXDB_ORGANIZATION";
        public const string HandleCommandsVariable = "RIOT2_HANDLE_COMMANDS";

        /// <summary>Unique id; also the MQTT client id.</summary>
        public string Id { get; set; }

        public string InfluxHost { get; set; }
        public string InfluxToken { get; set; }
        public string InfluxBucket { get; set; }
        public string InfluxOrganization { get; set; }

        /// <summary>Also store commands (<c>riot2/node/+/command</c>), not only reports.</summary>
        public bool HandleCommands { get; set; }

        public MqttOptions Mqtt { get; set; } = new();

        /// <summary><c>RIOT2_LOG_FORMAT</c> and <c>RIOT2_LOG_LEVEL</c>, applied by <c>AddRIoT2Logging()</c> (design 7.4).</summary>
        public LoggingOptions Logging { get; set; } = new();

        /// <summary><c>OTEL_EXPORTER_OTLP_ENDPOINT</c> and <c>RIOT2_TRACING</c>, applied by <c>AddRIoT2Telemetry()</c> (design 7.4).</summary>
        public TelemetryOptions Telemetry { get; set; } = new();

        /// <summary>
        /// Reads and validates the settings, and throws <see cref="ConfigurationValidationException"/>
        /// listing every problem (logged as critical) when any is missing or invalid.
        /// </summary>
        public static InfluxConnectorOptions Load(IConfiguration configuration, ILogger logger = null)
        {
            var settings = new SettingsReader(configuration);
            var options = new InfluxConnectorOptions
            {
                Id = settings.Required(IdVariable),
                InfluxHost = settings.RequiredHttpUrl(HostVariable),
                InfluxToken = settings.Required(TokenVariable),
                InfluxBucket = settings.Required(BucketVariable),
                InfluxOrganization = settings.Required(OrganizationVariable),
                HandleCommands = settings.Flag(HandleCommandsVariable),
                Mqtt = MqttOptions.Read(settings),
                Logging = LoggingOptions.Read(settings),
                Telemetry = TelemetryOptions.Read(settings)
            };
            settings.ThrowIfInvalid("InfluxDB connector", logger);
            return options;
        }
    }
}
