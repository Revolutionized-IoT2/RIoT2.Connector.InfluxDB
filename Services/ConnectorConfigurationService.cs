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
                HandleCommands = Environment.GetEnvironmentVariable("RIOT2_HANDLE_COMMANDS")?.ToLower() == "true",
                InfluxHost = Environment.GetEnvironmentVariable("RIOT2_INFLUXDB_HOST"),
                InfluxToken = Environment.GetEnvironmentVariable("RIOT2_INFLUXDB_TOKEN"),
                InfluxBucket = Environment.GetEnvironmentVariable("RIOT2_INFLUXDB_BUCKET"),
                InfluxOrganization = Environment.GetEnvironmentVariable("RIOT2_INFLUXDB_ORGANIZATION"),
                Mqtt = new Core.Models.MqttConfiguration() 
                {
                    ClientId = Environment.GetEnvironmentVariable("RIOT2_CONNECTOR_ID"),
                    Password = Environment.GetEnvironmentVariable("RIOT2_MQTT_PASSWORD"),
                    ServerUrl = Environment.GetEnvironmentVariable("RIOT2_MQTT_IP"),
                    Username = Environment.GetEnvironmentVariable("RIOT2_MQTT_USERNAME")
                }
            };
        }

        public ConnectorConfiguration Configuration 
        {
            get { return _configuration; }
        }
    }
}
