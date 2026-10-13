using Microsoft.Extensions.Options;
using RIoT2.Connector.InfluxDB.Models;
using RIoT2.Connector.InfluxDB.Services.Interfaces;

namespace RIoT2.Connector.InfluxDB.Services
{
    public class ConnectorConfigurationService : IConnectorConfigurationService
    {
        private readonly ConnectorConfiguration _configuration;

        public ConnectorConfigurationService(IOptions<InfluxConnectorOptions> options)
        {
            ArgumentNullException.ThrowIfNull(options);
            var settings = options.Value;
            _configuration = new ConnectorConfiguration()
            {
                HandleCommands = settings.HandleCommands,
                InfluxHost = settings.InfluxHost,
                InfluxToken = settings.InfluxToken,
                InfluxBucket = settings.InfluxBucket,
                InfluxOrganization = settings.InfluxOrganization,
                Mqtt = settings.Mqtt.ToConfiguration(settings.Id)
            };
        }

        public ConnectorConfiguration Configuration 
        {
            get { return _configuration; }
        }
    }
}
