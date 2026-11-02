using RIoT2.Connector.Sdk;
using RIoT2.Core.Configuration;

namespace RIoT2.Connector.InfluxDB.Services
{
    /// <summary>
    /// The connector's own settings (plan M4). The shared ones (<c>RIOT2_CONNECTOR_ID</c>, <c>RIOT2_HANDLE_COMMANDS</c>,
    /// MQTT, logging, telemetry, security) are the SDK's <see cref="ConnectorOptions"/>.
    /// </summary>
    public sealed class InfluxConnectorOptions
    {
        public const string Component = "InfluxDB connector";
        public const string HostVariable = "RIOT2_INFLUXDB_HOST";
        public const string TokenVariable = "RIOT2_INFLUXDB_TOKEN";
        public const string BucketVariable = "RIOT2_INFLUXDB_BUCKET";
        public const string OrganizationVariable = "RIOT2_INFLUXDB_ORGANIZATION";

        // Read sets the required values; ThrowIfInvalid throws when one is missing, so they are never null.
        public string InfluxHost { get; set; } = null!;
        public string InfluxToken { get; set; } = null!;
        public string InfluxBucket { get; set; } = null!;
        public string InfluxOrganization { get; set; } = null!;

        public static InfluxConnectorOptions Read(SettingsReader settings)
        {
            ArgumentNullException.ThrowIfNull(settings);
            return new InfluxConnectorOptions
            {
                InfluxHost = settings.RequiredHttpUrl(HostVariable),
                InfluxToken = settings.Required(TokenVariable),
                InfluxBucket = settings.Required(BucketVariable),
                InfluxOrganization = settings.Required(OrganizationVariable)
            };
        }

        /// <summary>
        /// Reads and validates every setting the connector uses, as its start does, and throws
        /// <see cref="ConfigurationValidationException"/> listing every problem when any is missing or invalid.
        /// </summary>
        public static (ConnectorOptions Connector, InfluxConnectorOptions Influx) Load(IConfiguration configuration, ILogger? logger = null) =>
            ConnectorOptions.Load(configuration, Component, Read, logger);
    }
}
