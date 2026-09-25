using RIoT2.Connector.InfluxDB.Models;
using RIoT2.Connector.InfluxDB.Services.Interfaces;

namespace RIoT2.Connector.InfluxDB.Services
{
    public class ConnectorConfigurationService : IConnectorConfigurationService
    {
        private ConnectorConfiguration _configuration;

        public ConnectorConfigurationService() 
        {
            _configuration = new ConnectorConfiguration()
            {
                HandleCommands = bool.TryParse(Environment.GetEnvironmentVariable("RIOT2_HANDLE_COMMANDS"), out var handleCommands) && handleCommands,
                InfluxHost = RequireAbsoluteHttpUrl("RIOT2_INFLUXDB_HOST"),
                InfluxToken = Require("RIOT2_INFLUXDB_TOKEN"),
                InfluxBucket = Require("RIOT2_INFLUXDB_BUCKET"),
                InfluxOrganization = Require("RIOT2_INFLUXDB_ORGANIZATION"),
                Mqtt = new Core.Models.MqttConfiguration() 
                {
                    ClientId = Require("RIOT2_CONNECTOR_ID"),
                    // Username/password stay optional so brokers that allow anonymous clients keep working.
                    Password = Environment.GetEnvironmentVariable("RIOT2_MQTT_PASSWORD") ?? "",
                    ServerUrl = Require("RIOT2_MQTT_IP"),
                    Username = Environment.GetEnvironmentVariable("RIOT2_MQTT_USERNAME") ?? ""
                }
            };
        }

        public ConnectorConfiguration Configuration 
        {
            get { return _configuration; }
        }

        private static string Require(string name)
        {
            var value = Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidOperationException($"{name} must be configured.");
            return value;
        }

        private static string RequireAbsoluteHttpUrl(string name)
        {
            var value = Require(name);
            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                throw new InvalidOperationException($"{name} must be an absolute HTTP or HTTPS URL.");
            return value;
        }
    }
}
