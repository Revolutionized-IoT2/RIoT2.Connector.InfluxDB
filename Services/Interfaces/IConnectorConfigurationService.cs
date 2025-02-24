using RIoT2.Connector.InfluxDB.Models;

namespace RIoT2.Connector.InfluxDB.Services.Interfaces
{
    public interface IConnectorConfigurationService
    {
        public ConnectorConfiguration Configuration { get; }
    }
}
